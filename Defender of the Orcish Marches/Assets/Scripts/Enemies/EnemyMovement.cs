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


    // Range within which enemies will opportunistically attack hirelings/menials
    private const float OPPORTUNISTIC_RANGE = 4f;

    private void FindTarget()
    {
        targetingTowerPosition = false;
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
                return;
            }

            // If already inside the walls, head straight for the tower
            float distFromCenter = new Vector2(transform.position.x, transform.position.z).magnitude;
            if (distFromCenter < 4.5f)
            {
                agent.SetDestination(TowerPosition);
            }
            else
            {
                // Path toward the nearest breach to get inside
                Vector3 breachPos = FindNearestBreachPosition();
                agent.SetDestination(breachPos);
            }
            targetingTowerPosition = true;
            currentTarget = null;
            return;
        }

        // Step 2: No breach — attack the wall blocking our path to the tower
        // Opportunistically attack nearby hirelings/menials first
        Transform nearbyTarget = FindNearbyOpportunisticTarget();
        if (nearbyTarget != null)
        {
            currentTarget = nearbyTarget;
            agent.SetDestination(nearbyTarget.position);
            return;
        }

        // Stick with current wall target if it's still alive
        if (currentTarget != null)
        {
            var curWall = currentTarget.GetComponent<Wall>();
            if (curWall != null && !curWall.IsDestroyed)
                return; // keep attacking the same wall
        }

        // Pick the best wall target (only runs when we have no wall or it was destroyed)
        if (WallManager.Instance != null)
        {
            Wall approachWall = FindBestWallTarget();
            if (approachWall != null)
            {
                currentTarget = approachWall.transform;
                // Target exterior face so NavMesh doesn't route around the wall
                Vector3 wallPos = approachWall.transform.position;
                Vector3 outward = wallPos.normalized;
                Vector3 exteriorPoint = wallPos + outward * 1f;
                exteriorPoint.y = 0;
                agent.SetDestination(exteriorPoint);
                return;
            }
        }

        // Fallback: walk toward the tower
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
    /// Find the best wall to attack: prefers damaged walls in our approach direction.
    /// Enemies converge on the weakest wall to breach it faster.
    /// </summary>
    private Wall FindBestWallTarget()
    {
        Vector3 pos = transform.position;
        Vector3 toTower = (TowerPosition - pos);
        toTower.y = 0;
        float distToTower = toTower.magnitude;
        if (distToTower < 0.1f) return WallManager.Instance.GetNearestWall(pos);
        toTower /= distToTower;

        Wall bestWall = null;
        float bestScore = float.MaxValue;

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall.IsDestroyed) continue;
            Vector3 toWall = wall.transform.position - pos;
            toWall.y = 0;
            float dist = toWall.magnitude;
            if (dist < 0.1f) continue;
            toWall /= dist;

            float alignment = Vector3.Dot(toWall, toTower);
            // Only consider walls roughly in our approach direction
            if (alignment < 0.2f) continue;

            // HP ratio: 0 = nearly dead, 1 = full health
            float hpRatio = (float)wall.CurrentHP / wall.MaxHP;

            // Score: lower is better.
            // Heavily favor damaged walls (hpRatio) and alignment with approach.
            // A wall at 10% HP in our direction scores much lower than a full wall.
            float score = dist * (2f - alignment) * (0.2f + hpRatio);
            if (score < bestScore)
            {
                bestScore = score;
                bestWall = wall;
            }
        }

        return bestWall != null ? bestWall : WallManager.Instance.GetNearestWall(pos);
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

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (!wall.IsDestroyed) continue;
            float dist = Vector3.Distance(transform.position, wall.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall.transform.position;
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
