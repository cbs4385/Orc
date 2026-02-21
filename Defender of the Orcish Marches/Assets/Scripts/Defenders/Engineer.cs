using UnityEngine;
using UnityEngine.AI;

public class Engineer : Defender
{
    [SerializeField] private int repairAmount = 5;
    [SerializeField] private float repairInterval = 1f;

    private float repairTimer;
    private Wall targetWall;

    protected override void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

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
}
