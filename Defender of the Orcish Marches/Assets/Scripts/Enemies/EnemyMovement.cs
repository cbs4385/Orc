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
    private static readonly Vector3 TowerPosition = Vector3.zero;

    public Transform CurrentTarget => currentTarget;
    public bool HasReachedTarget => agent != null && !agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance + 0.1f;

    // Track if we're targeting a position (tower) vs a transform
    private bool targetingTowerPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
    }

    // Area mask excluding gate links (area 3) so enemies can't path through gates
    private int enemyAreaMask;

    private void Start()
    {
        if (enemy.Data != null)
        {
            agent.speed = enemy.Data.moveSpeed;
            agent.stoppingDistance = enemy.Data.attackRange * 0.9f;
        }

        // Exclude gate area (area 3) from enemy pathfinding
        enemyAreaMask = NavMesh.AllAreas & ~(1 << 3);
        agent.areaMask = enemyAreaMask;

        FindTarget();
    }

    private void Update()
    {
        if (enemy.IsDead) { agent.isStopped = true; return; }

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0)
        {
            retargetTimer = RETARGET_INTERVAL;
            FindTarget();
        }

        // If enemy reached the tower, game over
        if (targetingTowerPosition && agent.isOnNavMesh && !agent.pathPending)
        {
            float distToTower = Vector3.Distance(transform.position, TowerPosition);
            if (distToTower < 3f)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }
    }

    // How close an enemy must be to attack something in its path while rushing the tower
    private const float BREACH_AGGRO_RANGE = 3f;

    private void FindTarget()
    {
        Transform bestTarget = null;
        float bestDist = float.MaxValue;
        targetingTowerPosition = false;

        // PRIORITY 1: Menials outside walls — always highest priority (deny resources)
        //   Ranged enemies engage within attack range; melee chase without limit.
        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var menial in menials)
        {
            if (menial.IsOutsideWalls && !menial.IsDead)
            {
                float dist = Vector3.Distance(transform.position, menial.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = menial.transform;
                }
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            if (agent.isOnNavMesh)
                agent.SetDestination(currentTarget.position);
            Debug.Log($"[EnemyMovement] {enemy.Data.enemyName} targeting menial at dist={bestDist:F1}");
            return;
        }

        // PRIORITY 2: Breach rush — if a wall is breached, rush the tower
        if (WallManager.Instance != null && WallManager.Instance.HasBreach())
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.isOnNavMesh && NavMesh.CalculatePath(transform.position, TowerPosition, enemyAreaMask, path))
            {
                if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
                {
                    Vector3 finalPoint = path.corners[path.corners.Length - 1];
                    float distToCenter = Vector3.Distance(finalPoint, TowerPosition);
                    if (distToCenter < 3.5f)
                    {
                        // Before rushing tower, check for menials/defenders in our path
                        Transform blockingTarget = FindNearestBlockingTarget();
                        if (blockingTarget != null)
                        {
                            currentTarget = blockingTarget;
                            if (agent.isOnNavMesh)
                                agent.SetDestination(blockingTarget.position);
                            return;
                        }

                        // No one in the way — rush the tower
                        agent.SetDestination(TowerPosition);
                        targetingTowerPosition = true;
                        currentTarget = null;
                        return;
                    }
                }
            }
        }

        // PRIORITY 3: Check for refugees — only chase if close (within 8 units)
        const float REFUGEE_CHASE_RANGE = 8f;
        var refugees = FindObjectsByType<Refugee>(FindObjectsSortMode.None);
        foreach (var refugee in refugees)
        {
            float dist = Vector3.Distance(transform.position, refugee.transform.position);
            if (dist < REFUGEE_CHASE_RANGE && dist < bestDist)
            {
                bestDist = dist;
                bestTarget = refugee.transform;
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            if (agent.isOnNavMesh)
                agent.SetDestination(currentTarget.position);
            return;
        }

        // PRIORITY 4: Default — target nearest wall
        if (WallManager.Instance != null)
        {
            Wall wall = WallManager.Instance.GetNearestWall(transform.position);
            if (wall != null)
                bestTarget = wall.transform;
        }

        currentTarget = bestTarget;
        if (currentTarget != null && agent.isOnNavMesh)
            agent.SetDestination(currentTarget.position);
    }

    /// <summary>
    /// Find the nearest menial or defender between this enemy and the tower (within aggro range).
    /// </summary>
    private Transform FindNearestBlockingTarget()
    {
        Transform best = null;
        float bestDist = BREACH_AGGRO_RANGE;

        // Check all menials (not just outside walls — they may be inside the courtyard)
        var menials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var menial in menials)
        {
            if (menial.IsDead) continue;
            float dist = Vector3.Distance(transform.position, menial.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = menial.transform;
            }
        }

        // Check all defenders
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var defender in defenders)
        {
            if (defender.IsDead) continue;
            float dist = Vector3.Distance(transform.position, defender.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = defender.transform;
            }
        }

        return best;
    }

    public void Stop()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
    }

    public void Resume()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }
}
