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

    protected override void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Check for danger before repair logic
        if (CheckForDanger()) return;

        repairTimer -= Time.deltaTime;

        // Find damaged or destroyed wall (destroyed walls = breaches to rebuild)
        if (targetWall == null || targetWall.CurrentHP >= targetWall.MaxHP)
        {
            if (WallManager.Instance != null)
            {
                targetWall = WallManager.Instance.GetNearestDamagedWall(transform.position);
            }
        }

        float repairRange = data != null ? data.range : 2f;

        if (targetWall != null)
        {
            // Move toward the inside face of the wall
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 wallPos = targetWall.transform.position;
                Vector3 toCenter = (GameManager.FortressCenter - wallPos).normalized;
                Vector3 insidePos = wallPos + toCenter * 0.6f;
                insidePos.y = 0;
                agent.SetDestination(insidePos);
            }

            // Repair when within range — wide enough for multiple engineers to work side-by-side
            float dist = Vector3.Distance(transform.position, targetWall.transform.position);
            if (dist < repairRange && repairTimer <= 0)
            {
                targetWall.Repair(repairAmount);
                repairTimer = repairInterval;
                Debug.Log($"[Engineer] Repaired {targetWall.name} for {repairAmount} HP (now {targetWall.CurrentHP}/{targetWall.MaxHP})");
            }
        }
    }

    /// <summary>
    /// Check for nearby enemies that have line-of-sight (no wall between us).
    /// Returns true if fleeing (caller should skip repair logic).
    /// </summary>
    private bool CheckForDanger()
    {
        dangerScanTimer -= Time.deltaTime;
        if (dangerScanTimer > 0) return isFleeing;
        dangerScanTimer = DANGER_SCAN_INTERVAL;

        bool threatFound = false;
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            float dangerRange = enemy.Data != null ? enemy.Data.attackRange + 1f : 2.5f;
            if (dist < dangerRange)
            {
                // Check wall occlusion — if a wall blocks line of sight, we're safe
                if (IsWallBetween(transform.position, enemy.transform.position))
                    continue;

                threatFound = true;
                break;
            }
        }

        if (threatFound && !isFleeing)
        {
            // Start fleeing
            isFleeing = true;
            Debug.Log($"[Engineer] Danger! Unobstructed enemy nearby. Fleeing toward courtyard!");
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(GameManager.FortressCenter);
            }
        }
        else if (!threatFound && isFleeing)
        {
            // Safe again — resume repair
            isFleeing = false;
            Debug.Log("[Engineer] Safe now. Resuming repairs.");
        }

        if (isFleeing)
        {
            // Keep heading toward courtyard center
            if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 1f)
            {
                agent.SetDestination(GameManager.FortressCenter);
            }
        }

        return isFleeing;
    }

    /// <summary>
    /// Raycast between two positions. Returns true if a wall collider blocks the line.
    /// </summary>
    private bool IsWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        // Cast at chest height to avoid ground hits
        Vector3 origin = from + Vector3.up * 0.5f;
        Vector3 target = to + Vector3.up * 0.5f;
        Vector3 direction = (target - origin).normalized;
        float castDist = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, castDist))
        {
            // Check if what we hit is a wall (has Wall component or is tagged)
            if (hit.collider.GetComponentInParent<Wall>() != null)
                return true;
        }
        return false;
    }
}
