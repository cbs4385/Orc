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

    // SphereCast radius for wall crossing detection — wide enough to bridge
    // the intentional gaps between wall segments so rays can't slip through
    private const float WALL_DETECT_RADIUS = 0.75f;

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

        // --- Evaluate all approach directions to find fewest wall rings ---
        // Stick with current wall target if alive
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
                return; // keep attacking the same wall
        }

        if (WallManager.Instance != null)
        {
            Wall bestWall = FindBestApproachWall(out bool hasOpenPath);
            if (hasOpenPath)
            {
                currentTarget = null;
                agent.SetDestination(TowerPosition);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} open approach found — pathing to tower (NavMesh routes through gap)");
                return;
            }
            if (bestWall != null)
            {
                currentTarget = bestWall.transform;
                NavigateToWallExterior(bestWall);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} targeting wall {bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP}) at {bestWall.transform.position}");
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

        // --- No units found: fall back to closest most-damaged wall ---
        if (WallManager.Instance != null)
        {
            Wall bestWall = null;
            float bestDist = float.MaxValue;
            int bestHP = int.MaxValue;

            foreach (var wall in WallManager.Instance.AllWalls)
            {
                if (wall.IsDestroyed || wall.IsUnderConstruction) continue;

                float dist = Vector3.Distance(transform.position, wall.transform.position);

                // Primary: closest. Secondary: most damaged (lowest HP).
                if (dist < bestDist ||
                    (Mathf.Approximately(dist, bestDist) && wall.CurrentHP < bestHP))
                {
                    bestDist = dist;
                    bestHP = wall.CurrentHP;
                    bestWall = wall;
                }
            }

            if (bestWall != null)
            {
                currentTarget = bestWall.transform;
                NavigateToWallExterior(bestWall);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} ranged no units — targeting closest wall {bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP}, dist={bestDist:F1})");
                return;
            }
        }

        // All walls destroyed — head to tower
        currentTarget = null;
        agent.SetDestination(TowerPosition);
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} ranged no units, no walls — heading to tower");
    }

    // ====================================================================
    //  SUICIDE / ARTILLERY (GOBLINS) — target walls directly
    // ====================================================================

    /// <summary>
    /// Goblins (suicide bombers and cannoneers) target walls to create breaches.
    /// Primary: most damaged wall (lowest HP). Ties broken by distance (closer wins).
    /// They do not chase hirelings or rush through breaches.
    /// </summary>
    private void FindTarget_Goblin()
    {
        if (WallManager.Instance == null)
        {
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            return;
        }

        // Stick with current wall target if alive
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
                return;
        }

        // Find the most damaged wall; ties broken by distance (closer wins)
        Wall bestWall = null;
        int lowestHP = int.MaxValue;
        float closestDist = float.MaxValue;

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall.IsDestroyed || wall.IsUnderConstruction) continue;

            float dist = Vector3.Distance(transform.position, wall.transform.position);

            if (wall.CurrentHP < lowestHP ||
                (wall.CurrentHP == lowestHP && dist < closestDist))
            {
                lowestHP = wall.CurrentHP;
                closestDist = dist;
                bestWall = wall;
            }
        }

        if (bestWall != null)
        {
            currentTarget = bestWall.transform;
            NavigateToWallExterior(bestWall);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} goblin targeting most-damaged wall {bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP}, dist={closestDist:F1})");
        }
        else
        {
            // All walls destroyed — head to tower
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} goblin all walls destroyed — heading to tower");
        }
    }

    // ====================================================================
    //  SHARED HELPERS
    // ====================================================================

    // Number of approach directions to evaluate from the tower
    private const int APPROACH_DIR_COUNT = 24;
    private const float WALL_PERP_TOLERANCE = 1.2f;
    private const float RING_GROUP_TOLERANCE = 2.0f;
    private const float APPROACH_SCAN_DIST = 15f;
    private static readonly float[] dirWallDists = new float[16];
    private static readonly Wall[] dirWallRefs = new Wall[16];

    /// <summary>
    /// Scan all approach directions from the tower outward and find the direction
    /// with the fewest wall "rings" (depth layers). If any direction is completely
    /// open (0 rings), returns null and sets hasOpenPath=true so the enemy paths
    /// directly to the tower via NavMesh.
    /// Otherwise returns the outermost wall on the best approach direction.
    /// Ties broken by distance to the enemy (closer approach wins).
    /// </summary>
    private Wall FindBestApproachWall(out bool hasOpenPath)
    {
        hasOpenPath = false;
        if (WallManager.Instance == null) { hasOpenPath = true; return null; }

        Vector3 enemyPos = transform.position;
        enemyPos.y = 0;

        int bestRings = int.MaxValue;
        float bestEnemyDist = float.MaxValue;
        Wall bestWall = null;

        for (int i = 0; i < APPROACH_DIR_COUNT; i++)
        {
            float angle = i * 2f * Mathf.PI / APPROACH_DIR_COUNT;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // Collect walls near this approach direction
            int wallCount = 0;
            foreach (var wall in WallManager.Instance.AllWalls)
            {
                if (wall.IsDestroyed || wall.IsUnderConstruction) continue;

                Vector3 toWall = wall.transform.position - TowerPosition;
                toWall.y = 0;

                float proj = Vector3.Dot(toWall, dir);
                if (proj < 0 || proj > APPROACH_SCAN_DIST) continue;

                float perp = Vector3.Cross(dir, toWall).magnitude;
                if (perp > WALL_PERP_TOLERANCE) continue;

                if (wallCount < dirWallDists.Length)
                {
                    dirWallDists[wallCount] = proj;
                    dirWallRefs[wallCount] = wall;
                    wallCount++;
                }
            }

            // Sort by distance from tower
            for (int a = 0; a < wallCount - 1; a++)
                for (int b = a + 1; b < wallCount; b++)
                    if (dirWallDists[b] < dirWallDists[a])
                    {
                        float td = dirWallDists[a]; dirWallDists[a] = dirWallDists[b]; dirWallDists[b] = td;
                        Wall tw = dirWallRefs[a]; dirWallRefs[a] = dirWallRefs[b]; dirWallRefs[b] = tw;
                    }

            // Count distinct rings (walls at similar distances = same ring)
            int rings = 0;
            float lastRingDist = -999f;
            Wall outermost = null;
            for (int w = 0; w < wallCount; w++)
            {
                if (dirWallDists[w] - lastRingDist > RING_GROUP_TOLERANCE)
                {
                    rings++;
                    lastRingDist = dirWallDists[w];
                }
                outermost = dirWallRefs[w];
            }

            if (rings == 0)
            {
                hasOpenPath = true;
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} open approach at {angle * Mathf.Rad2Deg:F0}° (no walls)");
                return null;
            }

            // Distance from enemy to the outer edge of this approach corridor
            float approachDist = wallCount > 0 ? dirWallDists[wallCount - 1] : 5f;
            Vector3 approachPoint = TowerPosition + dir * (approachDist + 1f);
            float enemyDist = Vector3.Distance(enemyPos, approachPoint);

            if (rings < bestRings || (rings == bestRings && enemyDist < bestEnemyDist))
            {
                bestRings = rings;
                bestEnemyDist = enemyDist;
                bestWall = outermost;
            }
        }

        if (bestWall != null)
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} best approach: rings={bestRings}, wall={bestWall.name} (HP={bestWall.CurrentHP}/{bestWall.MaxHP}, enemyDist={bestEnemyDist:F1})");

        return bestWall;
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
    /// Find the first intact wall between two world positions via sphere-swept ray.
    /// Uses WALL_DETECT_RADIUS so gaps between wall segments don't create false negatives.
    /// Returns null if no wall blocks the line.
    /// </summary>
    private Wall FindWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 rayOrigin = new Vector3(from.x, 1f, from.z);
        Vector3 rayTarget = new Vector3(to.x, 1f, to.z);
        Vector3 rayDir = (rayTarget - rayOrigin);
        float rayDist = rayDir.magnitude;
        if (rayDist < 0.1f) return null;
        rayDir /= rayDist;

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, WALL_DETECT_RADIUS, rayDir, rayDist);
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
