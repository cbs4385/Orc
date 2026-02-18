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

    private void FindTarget()
    {
        Transform bestTarget = null;
        float bestDist = float.MaxValue;
        targetingTowerPosition = false;

        // PRIORITY 1: Check if there's a breach - rush the tower
        if (WallManager.Instance != null && WallManager.Instance.HasBreach())
        {
            // Try to path to the tower through the breach
            NavMeshPath path = new NavMeshPath();
            if (agent.isOnNavMesh && NavMesh.CalculatePath(transform.position, TowerPosition, enemyAreaMask, path))
            {
                // Accept both complete and partial paths - partial means we get close to the tower
                if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
                {
                    // Check if this path actually goes inside the wall ring (closer than ~3.5 units from center)
                    Vector3 finalPoint = path.corners[path.corners.Length - 1];
                    float distToCenter = Vector3.Distance(finalPoint, TowerPosition);
                    if (distToCenter < 3.5f)
                    {
                        // We can reach near the tower - go for it!
                        agent.SetDestination(TowerPosition);
                        targetingTowerPosition = true;
                        // Use a nearby wall as currentTarget for attack purposes
                        var nearestWall = WallManager.Instance.GetNearestWall(transform.position);
                        currentTarget = nearestWall != null ? nearestWall.transform : null;
                        return;
                    }
                }
            }
        }

        // PRIORITY 2: Check for menials outside walls (always chase - they're easy pickings)
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

        // If we found a menial outside walls, go for it
        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(currentTarget.position);
            }
            return;
        }

        // PRIORITY 3: Check for refugees - only chase if close (within 8 units)
        // Otherwise keep attacking walls. Uses effective distance that penalizes far refugees.
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

        // If we found a nearby refugee, chase them
        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(currentTarget.position);
            }
            return;
        }

        // PRIORITY 4: Default - target nearest wall
        if (WallManager.Instance != null)
        {
            Wall wall = WallManager.Instance.GetNearestWall(transform.position);
            if (wall != null)
            {
                bestTarget = wall.transform;
            }
        }

        currentTarget = bestTarget;
        if (currentTarget != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget.position);
        }
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
