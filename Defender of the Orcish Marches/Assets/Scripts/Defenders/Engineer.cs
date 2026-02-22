using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Engineer : Defender
{
    [SerializeField] private int repairAmount = 5;
    [SerializeField] private float repairInterval = 1f;

    private float repairTimer;
    private Wall targetWall;

    // Flee behavior
    private bool isFleeing;
    private float dangerScanTimer;
    private const float DANGER_SCAN_INTERVAL = 0.15f;

    // Track walls where no valid stand position could be found
    private HashSet<Wall> failedWalls = new HashSet<Wall>();
    private float failedWallResetTimer;
    private const float FAILED_WALL_RESET_INTERVAL = 10f;

    // Cached stand position for current target wall
    private Vector3 currentStandPos;
    private Wall standWall;

    // Clockwise stand position search
    private const int SEARCH_ANGLES = 16;           // 16 directions (22.5° apart)
    private const float ENGINEER_CHECK_RADIUS = 0.2f; // overlap sphere size for collision check
    private const float MIN_NAVMESH_CLEARANCE = 0.3f;  // min distance from NavMesh edge

    // Guard tracking
    private Defender assignedGuard;
    private bool lastExposedState;
    private float guardReevalTimer;
    private const float GUARD_REEVAL_INTERVAL = 3f;

    // Engineers stay on the ground to repair walls
    protected override bool ShouldUseTower() => false;

    // Enclosure ray check parameters
    private const int ENCLOSURE_RAY_COUNT = 24;     // 24 directions (15° apart)
    private const float ENCLOSURE_RAY_DIST = 20f;   // Far enough to escape any wall config

    protected override void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (CheckForDanger()) return;

        repairTimer -= Time.deltaTime;

        // Periodically allow retrying failed walls
        failedWallResetTimer -= Time.deltaTime;
        if (failedWallResetTimer <= 0)
        {
            failedWalls.Clear();
            failedWallResetTimer = FAILED_WALL_RESET_INTERVAL;
        }

        // Find a damaged wall to repair
        if (targetWall == null || targetWall.CurrentHP >= targetWall.MaxHP)
        {
            targetWall = GetNextDamagedWall();
            standWall = null;
        }

        // Periodically re-evaluate guard need — only when actively repairing
        if (targetWall != null)
        {
            guardReevalTimer -= Time.deltaTime;
            if (guardReevalTimer <= 0)
            {
                guardReevalTimer = GUARD_REEVAL_INTERVAL;
                Vector3 evalPos = standWall != null ? currentStandPos : transform.position;
                EvaluateGuardNeed(evalPos);
            }
        }

        float repairRange = data != null ? data.range : 2f;

        if (targetWall != null)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                // Calculate stand position when target changes
                if (standWall != targetWall)
                {
                    if (FindStandPosition(targetWall, out Vector3 pos))
                    {
                        currentStandPos = pos;
                        standWall = targetWall;
                        agent.isStopped = false;
                        agent.SetDestination(currentStandPos);
                        Debug.Log($"[Engineer] Moving to repair {targetWall.name} at standPos={currentStandPos}");
                        EvaluateGuardNeed(currentStandPos);
                    }
                    else
                    {
                        Debug.LogWarning($"[Engineer] No valid stand position for {targetWall.name} — skipping.");
                        failedWalls.Add(targetWall);
                        targetWall = null;
                        standWall = null;
                    }
                }
                else if (!agent.pathPending && agent.remainingDistance > 0.3f)
                {
                    agent.isStopped = false;
                }
            }

            // Repair when within range
            if (targetWall != null)
            {
                float dist = Vector3.Distance(transform.position, targetWall.transform.position);
                if (dist < repairRange && repairTimer <= 0)
                {
                    targetWall.Repair(repairAmount);
                    repairTimer = repairInterval;
                    Debug.Log($"[Engineer] Repaired {targetWall.name} for {repairAmount} HP (now {targetWall.CurrentHP}/{targetWall.MaxHP})");
                }
            }
        }
        else
        {
            // No wall to repair — retreat to courtyard and release guard
            if (agent != null && agent.isOnNavMesh)
            {
                float distFromCenter = Vector3.Distance(transform.position, GameManager.FortressCenter);
                if (distFromCenter > 2f)
                {
                    if (agent.isStopped)
                        Debug.Log($"[Engineer] Idle — retreating to courtyard from {transform.position} (dist={distFromCenter:F1})");
                    agent.isStopped = false;
                    agent.SetDestination(GameManager.FortressCenter);
                }
                else
                {
                    agent.isStopped = true;
                }
            }

            // No repair task means no need for a guard — release unconditionally
            if (assignedGuard != null && !assignedGuard.IsDead)
            {
                Debug.Log($"[Engineer] Idle, no repair task. Releasing guard {assignedGuard.name}.");
                assignedGuard.ReleaseFromGuardDuty();
                assignedGuard = null;
            }
            lastExposedState = false;
        }
    }

    /// <summary>
    /// Check if the engineer's destination is exposed (outside courtyard or breach exists).
    /// Request or release guard accordingly.
    /// </summary>
    public void EvaluateGuardNeed(Vector3 destination)
    {
        bool exposed = IsPositionExposed(destination);
        bool stateChanged = exposed != lastExposedState;
        lastExposedState = exposed;

        if (exposed)
        {
            // Request guard if we don't have one (or current guard died)
            // Re-request even without state change — a guard may have become available
            if (assignedGuard == null || assignedGuard.IsDead)
            {
                if (assignedGuard != null && assignedGuard.IsDead) assignedGuard = null;
                assignedGuard = Defender.FindAndAssignGuard(this);
                if (assignedGuard != null)
                {
                    Debug.Log($"[Engineer] Exposed at {destination}. Guard assigned: {assignedGuard.name}");
                }
                else if (stateChanged)
                {
                    Debug.Log($"[Engineer] Exposed at {destination}. No guard available.");
                }
            }
        }
        else
        {
            if (assignedGuard != null && !assignedGuard.IsDead)
            {
                Debug.Log($"[Engineer] Now inside enclosed area. Releasing guard {assignedGuard.name}.");
                assignedGuard.ReleaseFromGuardDuty();
                assignedGuard = null;
            }
        }
    }

    /// <summary>
    /// A position is exposed if enemies could physically reach it.
    /// Casts rays outward in all directions — if every ray hits a wall collider
    /// (including tower capsule colliders), the position is fully enclosed.
    /// If any ray escapes without hitting a wall, enemies can approach from that direction.
    /// Works with any wall configuration: standard fortress, custom enclosures, angled walls.
    /// Destroyed walls (inactive) and under-construction walls (disabled colliders) are
    /// correctly treated as openings.
    /// </summary>
    private bool IsPositionExposed(Vector3 pos)
    {
        Vector3 origin = new Vector3(pos.x, 1f, pos.z); // Mid-wall height for raycast

        for (int i = 0; i < ENCLOSURE_RAY_COUNT; i++)
        {
            float angle = i * 2f * Mathf.PI / ENCLOSURE_RAY_COUNT;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            bool hitWall = false;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, ENCLOSURE_RAY_DIST))
            {
                if (hit.collider.GetComponentInParent<Wall>() != null)
                    hitWall = true;
            }

            if (!hitWall)
            {
                Debug.Log($"[Engineer] IsPositionExposed({pos:F1}) = true (ray {i} at {angle * Mathf.Rad2Deg:F0}° escaped)");
                return true;
            }
        }

        // All rays hit walls — position is fully enclosed
        Debug.Log($"[Engineer] IsPositionExposed({pos:F1}) = false (all {ENCLOSURE_RAY_COUNT} rays hit walls — enclosed)");
        return false;
    }

    /// <summary>
    /// Called by Defender.Die() when our assigned guard dies.
    /// Clears the reference so we can request a new one.
    /// </summary>
    public void OnGuardDied()
    {
        Debug.Log($"[Engineer] Guard died — will request a new one.");
        assignedGuard = null;
    }

    /// <summary>
    /// Called when engineer dies — release guard.
    /// </summary>
    private void OnDestroy()
    {
        if (assignedGuard != null && !assignedGuard.IsDead)
        {
            assignedGuard.ReleaseFromGuardDuty();
            assignedGuard = null;
        }
    }

    /// <summary>
    /// Scan clockwise around the wall to find the first position where the engineer
    /// can stand on NavMesh without overlapping any wall/tower collider.
    /// Starts from the courtyard-facing direction and tries multiple distances.
    /// </summary>
    private bool FindStandPosition(Wall wall, out Vector3 result)
    {
        Vector3 wallPos = wall.transform.position;
        wallPos.y = 0;

        // Start scanning from the direction facing the courtyard
        Vector3 toCenter = GameManager.FortressCenter - wallPos;
        toCenter.y = 0;
        float startAngle = Mathf.Atan2(toCenter.z, toCenter.x);

        // Try at increasing distances — prefer close (adjacent) to far
        float[] distances = { 0.8f, 1.1f, 1.5f, 2.0f };

        foreach (float dist in distances)
        {
            for (int step = 0; step < SEARCH_ANGLES; step++)
            {
                // Clockwise: subtract angle each step
                float angle = startAngle - (step * 2f * Mathf.PI / SEARCH_ANGLES);
                Vector3 candidate = wallPos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

                // Must be on NavMesh
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
                    continue;

                Vector3 sampledPos = navHit.position;

                // Must have enough flat NavMesh area (not squeezed against an edge)
                if (NavMesh.FindClosestEdge(sampledPos, out NavMeshHit edgeHit, NavMesh.AllAreas))
                {
                    if (edgeHit.distance < MIN_NAVMESH_CLEARANCE)
                        continue;
                }

                // Must not overlap any wall or tower collider
                if (IsInsideWallGeometry(sampledPos))
                    continue;

                result = sampledPos;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Check if a position overlaps any wall body or tower collider.
    /// Uses Physics.OverlapSphere for walls with enabled colliders,
    /// plus manual bounds checks for ALL walls (catches under-construction walls with disabled colliders).
    /// </summary>
    private bool IsInsideWallGeometry(Vector3 pos)
    {
        // Physics check for walls with enabled colliders
        var overlaps = Physics.OverlapSphere(pos + Vector3.up * 0.5f, ENGINEER_CHECK_RADIUS);
        foreach (var col in overlaps)
        {
            if (col.GetComponentInParent<Wall>() != null)
                return true;
        }

        // Manual bounds check for ALL walls (including under-construction with disabled colliders)
        if (WallManager.Instance != null)
        {
            foreach (var wall in WallManager.Instance.AllWalls)
            {
                if (wall == null) continue;
                if (IsInsideWallBounds(pos, wall))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Manual geometry check: is the position inside the wall body or tower volumes?
    /// Works regardless of whether colliders are enabled.
    /// </summary>
    private bool IsInsideWallBounds(Vector3 worldPos, Wall wall)
    {
        Vector3 local = wall.transform.InverseTransformPoint(worldPos);
        float padding = ENGINEER_CHECK_RADIUS;

        // Wall body: local size (1, 2, 0.5) → half extents (0.5, -, 0.25)
        float halfX = 0.5f + padding;
        float halfZ = 0.25f + padding;
        if (Mathf.Abs(local.x) < halfX && Mathf.Abs(local.z) < halfZ)
            return true;

        // Towers at ±TOWER_OFFSET in local X, radius = OCT_APOTHEM
        float towerRadius = WallCorners.OCT_APOTHEM + padding;
        Vector2 localXZ = new Vector2(local.x, local.z);
        Vector2 leftTower = new Vector2(-WallCorners.TOWER_OFFSET, 0);
        Vector2 rightTower = new Vector2(WallCorners.TOWER_OFFSET, 0);

        if (Vector2.Distance(localXZ, leftTower) < towerRadius)
            return true;
        if (Vector2.Distance(localXZ, rightTower) < towerRadius)
            return true;

        return false;
    }

    private Wall GetNextDamagedWall()
    {
        if (WallManager.Instance == null) return null;

        Wall nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall == null) continue;
            if (wall.CurrentHP >= wall.MaxHP) continue;
            if (failedWalls.Contains(wall)) continue;
            float dist = Vector3.Distance(transform.position, wall.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = wall;
            }
        }

        // If all damaged walls are in the failed set, clear and retry
        if (nearest == null && failedWalls.Count > 0)
        {
            Debug.Log("[Engineer] All damaged walls in failed set — clearing and retrying.");
            failedWalls.Clear();
            return WallManager.Instance.GetNearestDamagedWall(transform.position);
        }

        return nearest;
    }

    /// <summary>
    /// Check for nearby enemies with line-of-sight. Returns true if fleeing.
    /// </summary>
    private bool CheckForDanger()
    {
        dangerScanTimer -= Time.deltaTime;
        if (dangerScanTimer > 0) return isFleeing;
        dangerScanTimer = DANGER_SCAN_INTERVAL;

        bool threatFound = false;
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            float dangerRange = enemy.Data != null ? enemy.Data.attackRange + 1f : 2.5f;
            if (dist < dangerRange)
            {
                if (IsWallBetween(transform.position, enemy.transform.position))
                    continue;
                threatFound = true;
                break;
            }
        }

        if (threatFound && !isFleeing)
        {
            isFleeing = true;
            Debug.Log("[Engineer] Danger! Unobstructed enemy nearby. Fleeing toward courtyard!");
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(GameManager.FortressCenter);
            }
            // Fleeing to courtyard center — evaluate guard need
            EvaluateGuardNeed(GameManager.FortressCenter);
        }
        else if (!threatFound && isFleeing)
        {
            isFleeing = false;
            targetWall = null;
            standWall = null;
            Debug.Log("[Engineer] Safe now. Resuming repairs.");
        }

        if (isFleeing)
        {
            if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 1f)
                agent.SetDestination(GameManager.FortressCenter);
        }

        return isFleeing;
    }

    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f) return false;

        Vector3 origin = from + Vector3.up * 0.5f;
        Vector3 target = to + Vector3.up * 0.5f;
        Vector3 direction = (target - origin).normalized;
        float castDist = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, castDist))
        {
            if (hit.collider.GetComponentInParent<Wall>() != null)
                return true;
        }
        return false;
    }
}
