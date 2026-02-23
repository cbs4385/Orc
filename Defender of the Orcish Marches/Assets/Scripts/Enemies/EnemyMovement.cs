using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Enemy enemy;
    private Transform currentTarget;
    private float retargetTimer;
    private const float RETARGET_INTERVAL = 1f;

    // Tower position - enemies walk here when walls are breached
    private static Vector3 TowerPosition => GameManager.FortressCenter;

    public Transform CurrentTarget => currentTarget;
    public bool HasReachedTarget => agent != null && !agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance + 0.1f;

    // Retreat state
    public bool IsRetreating { get; private set; }
    private float retreatMapRadius;

    // Gap blocking — prevents enemies from squeezing through wall gaps
    private bool gapBlocked;

    /// <summary>Fired when a retreating enemy reaches the west map edge.</summary>
    public Action<Enemy> OnReachedRetreatEdge;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
    }

    // Larger avoidance radius for enemies. Note: NavMeshAgent.radius only affects
    // local avoidance (steering), NOT pathfinding clearance. Wall gaps are blocked
    // by the gap-detection logic in Update() which redirects enemies to attack walls.
    private const float ENEMY_NAV_RADIUS = 0.65f;

    private void Start()
    {
        if (enemy.Data != null)
        {
            float dailySpeed = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemySpeedMultiplier : 1f;
            agent.speed = enemy.Data.moveSpeed * dailySpeed;
            agent.stoppingDistance = enemy.Data.attackRange * 0.9f;
        }

        agent.radius = ENEMY_NAV_RADIUS;
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} started at {transform.position}, navRadius={ENEMY_NAV_RADIUS}, speed={agent.speed}, stoppingDist={agent.stoppingDistance}");
        FindTarget();
    }

    private void Update()
    {
        if (enemy.IsDead) { agent.isStopped = true; return; }

        // Retreat mode: walk west, check if reached edge
        if (IsRetreating)
        {
            if (agent.isOnNavMesh && !agent.pathPending &&
                transform.position.x <= -retreatMapRadius + 2f)
            {
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} reached retreat edge at {transform.position}");
                OnReachedRetreatEdge?.Invoke(enemy);
            }
            return;
        }

        // Prevent enemies from squeezing through gaps between intact wall segments.
        // NavMeshAgent.radius only controls avoidance, NOT pathfinding clearance, so
        // the pathfinder routes enemies through gaps that are physically too narrow.
        // If the enemy is close to an intact wall and NOT near any breach, redirect.
        if (agent.isOnNavMesh && WallManager.Instance != null && !gapBlocked)
        {
            Wall nearestIntactWall = WallManager.Instance.GetNearestWall(transform.position);
            if (nearestIntactWall != null)
            {
                float distToWall = Vector3.Distance(transform.position, nearestIntactWall.transform.position);
                if (distToWall < WallCorners.WALL_SPACING)
                {
                    // Near an intact wall — check if there's a breach nearby (destroyed wall).
                    // If there IS a nearby breach, the enemy is legitimately pathing through it.
                    bool nearBreach = IsNearDestroyedWall(transform.position);
                    if (!nearBreach)
                    {
                        gapBlocked = true;
                        currentTarget = nearestIntactWall.transform;
                        Vector3 wallPos = nearestIntactWall.transform.position;
                        Vector3 outward = (wallPos - TowerPosition).normalized;
                        Vector3 exteriorPoint = wallPos + outward * 1f;
                        exteriorPoint.y = 0;
                        agent.SetDestination(exteriorPoint);
                        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} blocked from wall gap near {nearestIntactWall.name} — attacking wall instead");
                    }
                }
            }
        }

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0)
        {
            retargetTimer = RETARGET_INTERVAL;
            gapBlocked = false; // Re-evaluate on next retarget cycle
            FindTarget();
        }

        // If any enemy reaches the tower area after a breach, game over
        if (agent.isOnNavMesh && WallManager.Instance != null && WallManager.Instance.HasBreach())
        {
            float distToTower = Vector3.Distance(transform.position, TowerPosition);
            if (distToTower < 3f)
            {
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} reached the tower at dist={distToTower:F1}! Triggering game over.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }
    }

    /// <summary>
    /// Command this enemy to retreat west off the map.
    /// </summary>
    public void Retreat(float mapRadius)
    {
        IsRetreating = true;
        retreatMapRadius = mapRadius;
        currentTarget = null;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0f;
            Vector3 retreatDest = new Vector3(-mapRadius, 0, transform.position.z);
            agent.SetDestination(retreatDest);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} retreating to {retreatDest}");
        }
    }


    // Range within which melee enemies will opportunistically attack hirelings/menials
    private const float OPPORTUNISTIC_RANGE = 4f;

    // Range within which melee enemies divert from a wall target to attack a unit
    private const float MELEE_DIVERT_RANGE = 2.5f;

    // Number of rays in fan-raycast for minimal wall crossing detection
    private const int FAN_RAY_COUNT = 16;

    // Half-angle of the fan sweep (radians) — 75 degrees each side of tower direction
    private const float FAN_HALF_ANGLE = 75f * Mathf.Deg2Rad;

    /// <summary>
    /// Dispatch targeting to type-specific logic.
    /// Melee/WallBreaker: shortest path with minimal wall crossings, opportunistic unit kills.
    /// Ranged (Bow Orcs): prioritize hirelings/menials, fall back to walls.
    /// Suicide/Artillery (Goblins): target walls directly.
    /// </summary>
    private void FindTarget()
    {
        if (!agent.isOnNavMesh) return;

        switch (enemy.Data.enemyType)
        {
            case EnemyType.Melee:
            case EnemyType.WallBreaker:
                FindTarget_Melee();
                break;
            case EnemyType.Ranged:
                FindTarget_Ranged();
                break;
            case EnemyType.Suicide:
            case EnemyType.Artillery:
                FindTarget_Goblin();
                break;
        }
    }

    // ====================================================================
    //  MELEE / WALLBREAKER — shortest path through fewest walls to tower
    // ====================================================================

    /// <summary>
    /// Melee enemies path toward the tower via the route that crosses the fewest wall
    /// segments. They attack walls blocking their path and opportunistically strike
    /// nearby hirelings/menials/refugees.
    /// </summary>
    private void FindTarget_Melee()
    {
        bool hasBreach = WallManager.Instance != null && WallManager.Instance.HasBreach();

        // --- Opportunistic targeting: attack nearby hirelings/menials ---
        Transform nearby = FindNearbyOpportunisticTarget();
        if (nearby != null)
        {
            float distToNearby = Vector3.Distance(transform.position, nearby.position);

            // If we have no wall target, or the unit is very close, divert to it
            bool hasWallTarget = currentTarget != null &&
                currentTarget.GetComponent<Wall>() != null &&
                !currentTarget.GetComponent<Wall>().IsDestroyed;

            if (!hasWallTarget || distToNearby < MELEE_DIVERT_RANGE)
            {
                currentTarget = nearby;
                agent.SetDestination(nearby.position);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} melee opportunistic target {nearby.name} at dist={distToNearby:F1}");
                return;
            }
        }

        // --- Breach exists: rush through to tower ---
        if (hasBreach)
        {
            Vector3 offset = transform.position - TowerPosition;
            float distFromCenter = new Vector2(offset.x, offset.z).magnitude;

            if (distFromCenter < 4.5f)
            {
                // Already inside walls — head to tower
                currentTarget = null;
                agent.SetDestination(TowerPosition);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} INSIDE walls (dist={distFromCenter:F1}), heading to tower");
                return;
            }

            // Path to nearest breach
            Vector3 breachPos = FindNearestBreachPosition();
            currentTarget = null;
            agent.SetDestination(breachPos);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} heading to breach at {breachPos} (distFromCenter={distFromCenter:F1})");
            return;
        }

        // --- No breach: check if direct line to tower is clear ---
        if (!CheckWallOnDirectLine())
        {
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} no walls blocking line to tower — heading directly");
            return;
        }

        // --- Walls blocking: stick with current wall target if alive ---
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
                return; // keep attacking the same wall
        }

        // --- Find the wall on the path with minimal wall crossings ---
        if (WallManager.Instance != null)
        {
            Wall bestWall = FindMinimalCrossingWall();
            if (bestWall != null)
            {
                currentTarget = bestWall.transform;
                NavigateToWallExterior(bestWall);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} minimal-crossing wall {bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP}) at {bestWall.transform.position}");
                return;
            }
        }

        // Fallback: head to tower
        currentTarget = null;
        agent.SetDestination(TowerPosition);
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} no blocking wall found, heading to tower");
    }

    // ====================================================================
    //  RANGED (BOW ORCS) — prioritize hirelings/menials, then walls
    // ====================================================================

    /// <summary>
    /// Bow orcs actively seek hirelings, menials, defenders, and refugees as primary
    /// targets. If a wall blocks the line to the unit, the bow orc attacks the wall
    /// to create a breach. Falls back to wall targeting if no units exist.
    /// </summary>
    private void FindTarget_Ranged()
    {
        // --- Primary: find nearest hireling/menial/defender/refugee ---
        Transform unitTarget = FindNearestUnit();

        if (unitTarget != null)
        {
            // Check if a wall stands between us and the unit
            Wall blockingWall = FindWallBetween(transform.position, unitTarget.position);

            if (blockingWall == null)
            {
                // Clear line — target the unit directly
                currentTarget = unitTarget;
                agent.SetDestination(unitTarget.position);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} ranged targeting unit {unitTarget.name} at dist={Vector3.Distance(transform.position, unitTarget.position):F1}");
                return;
            }
            else
            {
                // Wall blocking — attack the wall to get through to the unit
                currentTarget = blockingWall.transform;
                NavigateToWallExterior(blockingWall);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} ranged targeting wall {blockingWall.name} (HP={blockingWall.CurrentHP}/{blockingWall.MaxHP}) blocking unit {unitTarget.name}");
                return;
            }
        }

        // --- No units found: fall back to wall/tower targeting (same as goblins) ---
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} ranged found no units — falling back to wall targeting");
        FindTarget_Goblin();
    }

    // ====================================================================
    //  SUICIDE / ARTILLERY (GOBLINS) — target walls directly
    // ====================================================================

    /// <summary>
    /// Goblins (suicide bombers and cannoneers) target walls to create breaches.
    /// They do not chase hirelings or rush through breaches.
    /// </summary>
    private void FindTarget_Goblin()
    {
        // Check if direct line to tower is blocked by a wall
        if (!CheckWallOnDirectLine())
        {
            // No wall blocking — head to tower
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} goblin no walls blocking — heading to tower");
            return;
        }

        // Stick with current wall target if alive
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
                return;
        }

        // Find blocking wall using direct raycast
        if (WallManager.Instance != null)
        {
            Wall blockingWall = FindBlockingWall();
            if (blockingWall != null)
            {
                currentTarget = blockingWall.transform;
                NavigateToWallExterior(blockingWall);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} goblin targeting wall {blockingWall.name} (HP={blockingWall.CurrentHP}/{blockingWall.MaxHP})");
                return;
            }
        }

        // Fallback: head to tower
        currentTarget = null;
        agent.SetDestination(TowerPosition);
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} goblin no blocking wall found, heading to tower");
    }

    // ====================================================================
    //  SHARED HELPERS
    // ====================================================================

    /// <summary>
    /// Returns true if a physics raycast from the enemy to the tower hits an intact wall.
    /// </summary>
    private bool CheckWallOnDirectLine()
    {
        Vector3 rayOrigin = new Vector3(transform.position.x, 1f, transform.position.z);
        Vector3 rayTarget = new Vector3(TowerPosition.x, 1f, TowerPosition.z);
        Vector3 rayDir = (rayTarget - rayOrigin).normalized;
        float rayDist = Vector3.Distance(rayOrigin, rayTarget);

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit wallHit, rayDist))
        {
            var hitWall = wallHit.collider.GetComponentInParent<Wall>();
            if (hitWall != null && !hitWall.IsDestroyed && !hitWall.IsUnderConstruction)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Navigate the agent to the exterior side of a wall (attack position).
    /// </summary>
    private void NavigateToWallExterior(Wall wall)
    {
        Vector3 wallPos = wall.transform.position;
        Vector3 outward = (wallPos - TowerPosition).normalized;
        Vector3 exteriorPoint = wallPos + outward * 1f;
        exteriorPoint.y = 0;
        agent.SetDestination(exteriorPoint);
    }

    /// <summary>
    /// Find the first intact wall between two world positions via physics raycast.
    /// Returns null if no wall blocks the line.
    /// </summary>
    private Wall FindWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 rayOrigin = new Vector3(from.x, 1f, from.z);
        Vector3 rayTarget = new Vector3(to.x, 1f, to.z);
        Vector3 rayDir = (rayTarget - rayOrigin).normalized;
        float rayDist = Vector3.Distance(rayOrigin, rayTarget);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, rayDist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            Wall wall = hit.collider.GetComponentInParent<Wall>();
            if (wall != null && !wall.IsDestroyed && !wall.IsUnderConstruction)
                return wall;
        }
        return null;
    }

    /// <summary>
    /// Find nearby hirelings, menials, or refugees within opportunistic attack range.
    /// Used by melee enemies for opportunistic targeting.
    /// </summary>
    private Transform FindNearbyOpportunisticTarget()
    {
        Transform best = null;
        float bestDist = OPPORTUNISTIC_RANGE;

        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var d in defenders)
        {
            if (d.IsDead) continue;
            float dist = Vector3.Distance(transform.position, d.transform.position);
            if (dist < bestDist) { bestDist = dist; best = d.transform; }
        }

        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var m in menials)
        {
            if (m.IsDead) continue;
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist < bestDist) { bestDist = dist; best = m.transform; }
        }

        var refugees = FindObjectsByType<Refugee>(FindObjectsSortMode.None);
        foreach (var r in refugees)
        {
            float dist = Vector3.Distance(transform.position, r.transform.position);
            if (dist < bestDist) { bestDist = dist; best = r.transform; }
        }

        return best;
    }

    /// <summary>
    /// Find the nearest hireling, menial, defender, or refugee anywhere on the map.
    /// Used by ranged enemies (bow orcs) who actively seek unit targets.
    /// </summary>
    private Transform FindNearestUnit()
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var d in defenders)
        {
            if (d.IsDead) continue;
            float dist = Vector3.Distance(transform.position, d.transform.position);
            if (dist < bestDist) { bestDist = dist; best = d.transform; }
        }

        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var m in menials)
        {
            if (m.IsDead) continue;
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist < bestDist) { bestDist = dist; best = m.transform; }
        }

        var refugees = FindObjectsByType<Refugee>(FindObjectsSortMode.None);
        foreach (var r in refugees)
        {
            float dist = Vector3.Distance(transform.position, r.transform.position);
            if (dist < bestDist) { bestDist = dist; best = r.transform; }
        }

        if (best != null)
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} found nearest unit {best.name} at dist={bestDist:F1}");

        return best;
    }

    /// <summary>
    /// Find the wall that requires the fewest total wall crossings on the path from
    /// the enemy through that wall to the tower. Uses a fan of raycasts in a spread
    /// around the tower direction to evaluate multiple approach angles.
    /// For a single wall ring this degenerates to closest/lowest-HP wall; for complex
    /// layouts it picks the path that crosses fewer walls overall.
    /// </summary>
    private Wall FindMinimalCrossingWall()
    {
        Vector3 pos = transform.position;
        Vector3 toTower = TowerPosition - pos;
        toTower.y = 0;
        float distToTower = toTower.magnitude;
        if (distToTower < 0.1f) return null;
        Vector3 dirToTower = toTower / distToTower;

        float baseAngle = Mathf.Atan2(dirToTower.z, dirToTower.x);

        Wall bestWall = null;
        int bestCrossings = int.MaxValue;
        float bestScore = float.MaxValue;

        for (int i = 0; i < FAN_RAY_COUNT; i++)
        {
            float t = (float)i / (FAN_RAY_COUNT - 1); // 0..1
            float angle = baseAngle - FAN_HALF_ANGLE + 2f * FAN_HALF_ANGLE * t;
            Vector3 rayDir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            Vector3 rayOrigin = new Vector3(pos.x, 1f, pos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, distToTower + 5f);

            int wallCount = 0;
            Wall firstWall = null;
            float firstWallDist = float.MaxValue;

            foreach (var hit in hits)
            {
                Wall wall = hit.collider.GetComponentInParent<Wall>();
                if (wall == null || wall.IsDestroyed || wall.IsUnderConstruction) continue;
                wallCount++;
                if (hit.distance < firstWallDist)
                {
                    firstWall = wall;
                    firstWallDist = hit.distance;
                }
            }

            // Skip rays that found no walls (clear path — handled by breach/direct check)
            if (firstWall == null) continue;

            // Score: fewer wall crossings is paramount, then distance + HP
            float wallToTower = Vector3.Distance(firstWall.transform.position, TowerPosition);
            float totalDist = firstWallDist + wallToTower;
            float hpRatio = (float)firstWall.CurrentHP / firstWall.MaxHP;
            float score = wallCount * 1000f + totalDist + hpRatio * 10f;

            if (wallCount < bestCrossings || (wallCount == bestCrossings && score < bestScore))
            {
                bestCrossings = wallCount;
                bestScore = score;
                bestWall = firstWall;
            }
        }

        if (bestWall != null)
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} fan-ray found wall {bestWall.name} (crossings={bestCrossings}, HP={bestWall.CurrentHP}/{bestWall.MaxHP})");

        // If fan-ray didn't find anything, fall back to the direct-raycast method
        if (bestWall == null)
            bestWall = FindBlockingWall();

        return bestWall;
    }

    /// <summary>
    /// Find the wall blocking the direct path to the tower via single raycast.
    /// Falls back to scoring all walls by alignment, distance, and HP.
    /// Used by goblins and as fallback for melee.
    /// </summary>
    private Wall FindBlockingWall()
    {
        Vector3 pos = transform.position;
        Vector3 toTower = TowerPosition - pos;
        toTower.y = 0;
        float distToTower = toTower.magnitude;
        if (distToTower < 0.1f) return null;
        Vector3 dirToTower = toTower / distToTower;

        // Raycast from enemy toward tower at mid-wall height to find walls on the direct line
        Vector3 rayOrigin = new Vector3(pos.x, 1f, pos.z);
        Vector3 rayTarget = new Vector3(TowerPosition.x, 1f, TowerPosition.z);
        Vector3 rayDir = (rayTarget - rayOrigin).normalized;
        float rayDist = Vector3.Distance(rayOrigin, rayTarget);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, rayDist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Find the first (closest) wall on the direct line
        Wall directBlocker = null;
        float directBlockerDist = float.MaxValue;
        foreach (var hit in hits)
        {
            Wall wall = hit.collider.GetComponentInParent<Wall>();
            if (wall == null || wall.IsDestroyed || wall.IsUnderConstruction) continue;
            directBlocker = wall;
            directBlockerDist = hit.distance;
            break;
        }

        if (directBlocker != null)
        {
            // Check if any nearby wall (within WALL_SPACING) on the same line has lower HP
            // This lets enemies converge on the weakest wall in the same ring segment
            Wall bestWall = directBlocker;
            foreach (var hit in hits)
            {
                Wall wall = hit.collider.GetComponentInParent<Wall>();
                if (wall == null || wall == directBlocker || wall.IsDestroyed || wall.IsUnderConstruction) continue;
                // Only consider walls close to the first hit (same ring)
                if (hit.distance > directBlockerDist + WallCorners.WALL_SPACING) break;
                if (wall.CurrentHP < bestWall.CurrentHP)
                    bestWall = wall;
            }

            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} raycast found blocking wall {bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP})");
            return bestWall;
        }

        // Raycast didn't hit a wall (indirect blocking — e.g. walls form an L that blocks NavMesh).
        // Fall back to finding walls in our approach direction, heavily weighted by alignment and HP.
        Wall fallbackWall = null;
        float bestScore = float.MaxValue;

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall.IsDestroyed || wall.IsUnderConstruction) continue;
            Vector3 toWall = wall.transform.position - pos;
            toWall.y = 0;
            float dist = toWall.magnitude;
            if (dist < 0.1f) continue;
            toWall /= dist;

            float alignment = Vector3.Dot(toWall, dirToTower);
            // Only consider walls in our approach direction
            if (alignment < 0.3f) continue;

            float hpRatio = (float)wall.CurrentHP / wall.MaxHP;
            // Score: lower is better — prefer close, aligned, low-HP walls
            float score = dist * (2f - alignment) * (0.2f + hpRatio);
            if (score < bestScore)
            {
                bestScore = score;
                fallbackWall = wall;
            }
        }

        if (fallbackWall != null)
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} fallback targeting {fallbackWall.name} (HP={fallbackWall.CurrentHP}/{fallbackWall.MaxHP})");

        return fallbackWall;
    }

    private bool IsMeleeType()
    {
        if (enemy.Data == null) return true;
        var t = enemy.Data.enemyType;
        return t == EnemyType.Melee || t == EnemyType.WallBreaker;
    }

    /// <summary>
    /// Returns the position of the nearest destroyed wall (breach point).
    /// </summary>
    private Vector3 FindNearestBreachPosition()
    {
        Vector3 best = TowerPosition;
        float bestDist = float.MaxValue;
        string breachWallName = "none";

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (!wall.IsDestroyed) continue;
            float dist = Vector3.Distance(transform.position, wall.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall.transform.position;
                breachWallName = wall.name;
            }
        }

        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} nearest breach: {breachWallName} at {best}, dist={bestDist:F1}");
        return best;
    }

    /// <summary>
    /// Returns true if there is a destroyed wall within WALL_SPACING distance of pos.
    /// Used to distinguish wall gaps (no breach nearby) from breach areas.
    /// </summary>
    private bool IsNearDestroyedWall(Vector3 pos)
    {
        if (WallManager.Instance == null) return false;
        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (!wall.IsDestroyed) continue;
            if (Vector3.Distance(pos, wall.transform.position) < WallCorners.WALL_SPACING * 1.5f)
                return true;
        }
        return false;
    }

    public void Stop()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
    }

    public void Resume()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    private void OnDrawGizmos()
    {
        if (agent == null || !agent.hasPath) return;

        var corners = agent.path.corners;
        if (corners.Length < 2) return;

        // Color by enemy type and state
        if (IsRetreating)
            Gizmos.color = Color.yellow;
        else if (enemy != null && enemy.IsDead)
            return;
        else if (enemy != null && enemy.Data != null && enemy.Data.enemyType == EnemyType.Ranged)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange for bow orcs
        else
            Gizmos.color = Color.red;

        Vector3 lift = Vector3.up * 0.15f;
        for (int i = 0; i < corners.Length - 1; i++)
            Gizmos.DrawLine(corners[i] + lift, corners[i + 1] + lift);

        // Draw small sphere at destination
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(corners[corners.Length - 1] + lift, 0.2f);
    }
}
