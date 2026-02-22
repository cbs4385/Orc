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


    // Range within which enemies will opportunistically attack hirelings/menials
    private const float OPPORTUNISTIC_RANGE = 4f;

    private void FindTarget()
    {
        if (!agent.isOnNavMesh) return;

        // === GOAL: Always reach the tower. Walls are just obstacles. ===

        bool hasBreach = WallManager.Instance != null && WallManager.Instance.HasBreach();

        // Step 1: If walls are breached, melee enemies rush through the gap
        if (hasBreach && IsMeleeType())
        {
            // Opportunistically attack nearby targets while rushing
            Transform nearby = FindNearbyOpportunisticTarget();
            if (nearby != null)
            {
                currentTarget = nearby;
                agent.SetDestination(nearby.position);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} breach rush — opportunistic target {nearby.name} at dist={Vector3.Distance(transform.position, nearby.position):F1}");
                return;
            }

            // If already inside the walls, head straight for the tower
            Vector3 offset = transform.position - TowerPosition;
            float distFromCenter = new Vector2(offset.x, offset.z).magnitude;
            if (distFromCenter < 4.5f)
            {
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} INSIDE walls (dist={distFromCenter:F1}), heading to tower at {TowerPosition}");
                agent.SetDestination(TowerPosition);
            }
            else
            {
                // Path toward the nearest breach to get inside
                Vector3 breachPos = FindNearestBreachPosition();
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} heading to breach at {breachPos} (distFromCenter={distFromCenter:F1})");
                agent.SetDestination(breachPos);
            }

            currentTarget = null;
            return;
        }

        // Step 2: Check if there are walls between us and the tower (physics raycast).
        // We use physics instead of NavMesh because tower colliders block enemies physically
        // but the NavMesh has walkable gaps at tower positions (no NavMeshObstacles there).
        bool wallOnDirectLine = false;
        {
            Vector3 rayOrigin = new Vector3(transform.position.x, 1f, transform.position.z);
            Vector3 rayTarget = new Vector3(TowerPosition.x, 1f, TowerPosition.z);
            Vector3 rayDir = (rayTarget - rayOrigin).normalized;
            float rayDist = Vector3.Distance(rayOrigin, rayTarget);

            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit wallHit, rayDist))
            {
                var hitWall = wallHit.collider.GetComponentInParent<Wall>();
                if (hitWall != null && !hitWall.IsDestroyed && !hitWall.IsUnderConstruction)
                    wallOnDirectLine = true;
            }
        }

        if (!wallOnDirectLine)
        {
            // No wall between us and the tower — go directly
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} no walls blocking line to tower — heading directly");
            return;
        }

        // Step 3: Path blocked — stick with current wall target if it's still alive
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
            {
                // Only divert from wall target for very close opportunistic targets
                Transform nearby = FindNearbyOpportunisticTarget();
                if (nearby != null)
                {
                    float distToNearby = Vector3.Distance(transform.position, nearby.position);
                    if (distToNearby < 1.5f)
                    {
                        currentTarget = nearby;
                        agent.SetDestination(nearby.position);
                        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} diverted from wall to {nearby.name} at dist={distToNearby:F1}");
                        return;
                    }
                }
                return; // keep attacking the same wall
            }
        }

        // Step 4: Check for opportunistic targets before picking a wall
        Transform nearbyTarget = FindNearbyOpportunisticTarget();
        if (nearbyTarget != null)
        {
            currentTarget = nearbyTarget;
            agent.SetDestination(nearbyTarget.position);
            return;
        }

        // Step 5: Find the wall that's actually blocking our path to the tower
        if (WallManager.Instance != null)
        {
            Wall blockingWall = FindBlockingWall();
            if (blockingWall != null)
            {
                currentTarget = blockingWall.transform;
                Vector3 wallPos = blockingWall.transform.position;
                Vector3 outward = (wallPos - TowerPosition).normalized;
                Vector3 exteriorPoint = wallPos + outward * 1f;
                exteriorPoint.y = 0;
                agent.SetDestination(exteriorPoint);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} path blocked by {blockingWall.name} (HP={blockingWall.CurrentHP}/{blockingWall.MaxHP}) at {wallPos} — attacking");
                return;
            }
        }

        // Fallback: walk toward the tower
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} no blocking wall found, heading to tower");
        agent.SetDestination(TowerPosition);
    }

    /// <summary>
    /// Find nearby hirelings, menials, or refugees within opportunistic attack range.
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
    /// Find the wall that's actually blocking our direct path to the tower.
    /// Uses physics raycast to find walls on the direct line, then scores
    /// by distance and HP (lower HP = faster to breach = shorter time to reach tower).
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
        return t == EnemyType.Melee || t == EnemyType.WallBreaker || t == EnemyType.Suicide;
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

        // Color by enemy state
        if (IsRetreating)
            Gizmos.color = Color.yellow;
        else if (enemy != null && enemy.IsDead)
            return;
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
