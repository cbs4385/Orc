using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Enemy enemy;
    private Transform currentTarget;
    private Vector3 lastRetargetPos;
    private const float RETARGET_DISTANCE = 1f;
    private float stuckTimer;

    // Tower position - enemies walk here when walls are breached
    private static Vector3 TowerPosition => GameManager.FortressCenter;

    public Transform CurrentTarget => currentTarget;
    public bool HasReachedTarget => agent != null && !agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance + 0.1f &&
        agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete;

    // Retreat state
    public bool IsRetreating { get; private set; }
    private float retreatMapRadius;

    /// <summary>Fired when a retreating enemy reaches the west map edge.</summary>
    public Action<Enemy> OnReachedRetreatEdge;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (enemy.Data != null)
        {
            float dailySpeed = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemySpeedMultiplier : 1f;
            agent.speed = enemy.Data.moveSpeed * dailySpeed;
            agent.stoppingDistance = enemy.Data.attackRange * 0.9f;
        }

        lastRetargetPos = transform.position;
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} started at {transform.position}, speed={agent.speed}, stoppingDist={agent.stoppingDistance}");
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

        // Re-target if current target was destroyed (e.g. refugee arrived/died)
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            lastRetargetPos = transform.position;
            FindTarget();
        }

        float distMoved = Vector3.Distance(transform.position, lastRetargetPos);
        if (distMoved >= RETARGET_DISTANCE)
        {
            lastRetargetPos = transform.position;
            stuckTimer = 0f;
            FindTarget();
        }

        // Detect partial path (enemy can't reach destination) — retarget to find an alternative
        if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 1f)
            {
                Debug.LogWarning($"[EnemyMovement] {enemy.Data?.enemyName} stuck on partial path to {(currentTarget != null ? currentTarget.name : "tower")} — retargeting");
                stuckTimer = 0f;
                lastRetargetPos = transform.position;
                // Clear current target so FindTarget picks fresh
                currentTarget = null;
                FindTarget();
            }
        }
        else
        {
            stuckTimer = 0f;
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
    /// Melee/WallBreaker: use ray-based pathing — pick cheapest direction, attack blocking wall or walk to tower.
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
    //  MELEE / WALLBREAKER — always path to tower, attack blocking walls
    // ====================================================================

    /// <summary>
    /// Melee enemies always want to reach the central tower. PathingRayManager casts
    /// 360 rays from the tower outward and counts wall crossings per direction. The
    /// enemy picks the cheapest ray within ±60° of its direct approach (closest angle
    /// as tiebreaker). If the ray is clear, walk to tower. If a wall blocks it, attack
    /// the innermost wall on that ray. Nearby units are attacked opportunistically.
    /// </summary>
    private void FindTarget_Melee()
    {
        // --- Opportunistic targeting: attack nearby hirelings/menials ---
        Transform nearby = FindNearbyOpportunisticTarget();
        if (nearby != null)
        {
            float distToNearby = Vector3.Distance(transform.position, nearby.position);

            bool hasWallTarget = currentTarget != null &&
                currentTarget.GetComponent<Wall>() != null &&
                !currentTarget.GetComponent<Wall>().IsDestroyed;

            if (!hasWallTarget || distToNearby < MELEE_DIVERT_RANGE)
            {
                currentTarget = nearby;
                agent.SetDestination(nearby.position);
                Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} melee opportunistic → {nearby.name} dist={distToNearby:F1}");
                return;
            }
        }

        // --- Ray-based targeting via PathingRayManager ---
        if (PathingRayManager.Instance == null)
        {
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.LogWarning($"[EnemyMovement] {enemy.Data?.enemyName} PathingRayManager missing — heading to tower");
            return;
        }

        Vector3 toCenter = TowerPosition - transform.position;
        float directAngle = Mathf.Atan2(toCenter.z, toCenter.x) * Mathf.Rad2Deg;
        if (directAngle < 0f) directAngle += 360f;

        var (rayIndex, cost, firstWall) = PathingRayManager.Instance.GetBestRay(transform.position);

        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(directAngle, rayIndex));

        if (cost == 0 || firstWall == null)
        {
            // Clear path along cheapest ray — walk to tower
            currentTarget = null;
            agent.SetDestination(TowerPosition);
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} MELEE DECISION: pos={transform.position}, " +
                $"directAngle={directAngle:F1}°, chosenRay={rayIndex}° (angleDiff={angleDiff:F1}°), cost={cost}. " +
                $"ACTION: walk to tower (clear path)");
        }
        else
        {
            // Wall blocking on cheapest ray — attack the innermost wall
            currentTarget = firstWall.transform;
            NavigateToWallExterior(firstWall);
            Vector3 wallPos = firstWall.transform.position;
            Vector3 outward = (wallPos - TowerPosition).normalized;
            Vector3 exteriorPoint = wallPos + outward * 1f;
            Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} MELEE DECISION: pos={transform.position}, " +
                $"directAngle={directAngle:F1}°, chosenRay={rayIndex}° (angleDiff={angleDiff:F1}°), cost={cost}. " +
                $"ACTION: attack {firstWall.name} HP={firstWall.CurrentHP}/{firstWall.MaxHP} " +
                $"wallPos={wallPos}, wallRot={firstWall.transform.eulerAngles.y:F0}°, " +
                $"navigateTo={exteriorPoint}");
        }
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
        Debug.Log($"[EnemyMovement] {enemy.Data?.enemyName} NavigateToWallExterior: wall={wall.name} at {wallPos}, " +
            $"outwardDir={outward}, exteriorPoint={exteriorPoint}, distFromEnemy={Vector3.Distance(transform.position, exteriorPoint):F1}");
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
    /// Called when a wall is destroyed — forces every living enemy to recalculate
    /// its route immediately so they can exploit the new breach.
    /// </summary>
    public static void ForceAllRetarget()
    {
        Debug.Log($"[EnemyMovement] ForceAllRetarget TRIGGERED (callstack follows)");

        // Recalculate ray costs before retargeting so enemies use fresh data
        if (PathingRayManager.Instance != null)
            PathingRayManager.Instance.Recalculate();
        else
            Debug.LogWarning("[EnemyMovement] ForceAllRetarget — PathingRayManager.Instance is NULL, cannot recalculate rays");

        var all = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var em in all)
        {
            if (em.enemy.IsDead || em.IsRetreating) continue;
            string prevTarget = em.currentTarget != null ? em.currentTarget.name : "tower";
            em.FindTarget();
            string newTarget = em.currentTarget != null ? em.currentTarget.name : "tower";
            if (prevTarget != newTarget)
                Debug.Log($"[EnemyMovement] ForceAllRetarget: {em.enemy.Data?.enemyName} target changed {prevTarget} → {newTarget}");
            count++;
        }
        Debug.Log($"[EnemyMovement] ForceAllRetarget — {count} enemies retargeted");
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
