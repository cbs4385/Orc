using UnityEngine;
using UnityEngine.AI;

public class Engineer : Defender
{
    [SerializeField] private int repairAmount = 2;
    [SerializeField] private float repairInterval = 1f;

    private float repairTimer;
    private Wall targetWall;

    protected override void Update()
    {
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

        if (targetWall != null)
        {
            // Move toward the inside face of the wall (right up against it)
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 wallPos = targetWall.transform.position;
                Vector3 toCenter = (Vector3.zero - wallPos).normalized;
                Vector3 insidePos = wallPos + toCenter * 0.6f;
                insidePos.y = 0;
                agent.SetDestination(insidePos);
            }

            // Repair only when directly adjacent to wall
            float dist = Vector3.Distance(transform.position, targetWall.transform.position);
            if (dist < 1f && repairTimer <= 0)
            {
                targetWall.Repair(repairAmount);
                repairTimer = repairInterval;
            }
        }
    }
}
