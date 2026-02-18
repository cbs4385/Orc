using UnityEngine;
using UnityEngine.AI;

public enum MenialState
{
    Idle,
    WalkingToGate,
    Collecting,
    Returning,
    EnteringTower,
    PickingUp // Brief pause while grabbing a loot item
}

[RequireComponent(typeof(NavMeshAgent))]
public class Menial : MonoBehaviour
{
    [SerializeField] private int maxHP = 15;
    [SerializeField] private float moveSpeed = 4f;

    private NavMeshAgent agent;
    private int currentHP;
    private TreasurePickup targetLoot;
    private int carriedTreasure;
    private Gate targetGate;

    // Pickup radius — menial grabs any loot within this distance
    private const float PICKUP_RADIUS = 2.0f;
    // Time to pause per loot item collected
    private const float PICKUP_DELAY = 0.25f;
    private float pickupTimer;
    private MenialState stateBeforePickup; // state to resume after pickup delay

    // Wandering
    private float wanderTimer;
    private const float WANDER_INTERVAL_MIN = 3f;
    private const float WANDER_INTERVAL_MAX = 8f;
    private const float WANDER_RADIUS = 2.5f;

    // Scan throttle — don't scan every frame
    private float scanTimer;
    private const float SCAN_INTERVAL = 0.15f;

    public MenialState CurrentState { get; private set; } = MenialState.Idle;
    public bool IsOutsideWalls { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsIdle => CurrentState == MenialState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        currentHP = maxHP;
    }

    private void Start()
    {
        wanderTimer = Random.Range(1f, WANDER_INTERVAL_MAX);
    }

    private System.Action onEnteredTower;

    private void Update()
    {
        if (IsDead) return;

        switch (CurrentState)
        {
            case MenialState.Idle:
                UpdateIdle();
                break;
            case MenialState.WalkingToGate:
                UpdateWalkingToGate();
                break;
            case MenialState.Collecting:
                UpdateCollecting();
                break;
            case MenialState.Returning:
                UpdateReturning();
                break;
            case MenialState.EnteringTower:
                UpdateEnteringTower();
                break;
            case MenialState.PickingUp:
                UpdatePickingUp();
                break;
        }

        // Track if we're outside the wall ring
        float distFromCenter = new Vector2(transform.position.x, transform.position.z).magnitude;
        IsOutsideWalls = distFromCenter > 4.5f;
    }

    private void UpdateIdle()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0)
        {
            wanderTimer = Random.Range(WANDER_INTERVAL_MIN, WANDER_INTERVAL_MAX);
            WanderNearby();
        }
    }

    private void WanderNearby()
    {
        if (!agent.isOnNavMesh) return;

        // Keep wander targets strictly inside the courtyard (radius 2-3 from center)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(2f, 3f);
        Vector3 wanderTarget = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(wanderTarget, out hit, 1f, NavMesh.AllAreas))
        {
            // Double-check the sampled position is inside the courtyard
            float hitDist = new Vector2(hit.position.x, hit.position.z).magnitude;
            if (hitDist < 3.5f)
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    public void AssignLoot(TreasurePickup loot)
    {
        if (CurrentState != MenialState.Idle || IsDead) return;

        targetLoot = loot;

        if (GameManager.Instance != null)
            GameManager.Instance.IdleMenialCount--;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[Menial] NOT ON NAVMESH - cannot path to loot!");
            return;
        }

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        // Find the nearest gate and walk toward it first
        targetGate = FindNearestGate(loot.transform.position);
        if (targetGate != null)
        {
            CurrentState = MenialState.WalkingToGate;
            // Path to the gate's inside face (toward center from gate position)
            Vector3 gatePos = targetGate.transform.position;
            Vector3 toCenter = -gatePos.normalized;
            Vector3 gateApproach = gatePos + toCenter * 1.5f;
            gateApproach.y = 0;
            agent.SetDestination(gateApproach);
        }
        else
        {
            // No gate found, try direct path
            CurrentState = MenialState.Collecting;
            agent.SetDestination(loot.transform.position);
        }
    }

    private Gate FindNearestGate(Vector3 lootPosition)
    {
        Gate nearest = null;
        float bestScore = float.MaxValue;

        var gates = FindObjectsByType<Gate>(FindObjectsSortMode.None);
        foreach (var gate in gates)
        {
            // Score based on distance from menial to gate + gate to loot
            float toGate = Vector3.Distance(transform.position, gate.transform.position);
            float gateToLoot = Vector3.Distance(gate.transform.position, lootPosition);
            float score = toGate + gateToLoot;
            if (score < bestScore)
            {
                bestScore = score;
                nearest = gate;
            }
        }
        return nearest;
    }

    private void UpdateWalkingToGate()
    {
        if (targetLoot == null || targetLoot.IsCollected)
        {
            // Original target gone — check if there's other loot nearby to collect
            targetLoot = null;
            ReturnHome();
            return;
        }

        if (!agent.isOnNavMesh) return;

        // Scan for loot along the way
        if (TryGrabNearbyLoot(MenialState.WalkingToGate)) return;

        // Check if we're close to the gate
        if (targetGate != null)
        {
            float distToGate = Vector3.Distance(transform.position, targetGate.transform.position);

            // Once the gate is open and we're near it, switch to collecting (path to loot)
            if (distToGate < 3f && targetGate.IsOpen)
            {
                CurrentState = MenialState.Collecting;
                agent.SetDestination(targetLoot.transform.position);
                return;
            }

            // If we've reached our approach point but gate isn't open yet, wait
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Re-path to approach point to stay near gate
                Vector3 gatePos = targetGate.transform.position;
                Vector3 toCenter = -gatePos.normalized;
                Vector3 gateApproach = gatePos + toCenter * 1.5f;
                gateApproach.y = 0;
                agent.SetDestination(gateApproach);
            }
        }
    }

    private void UpdateCollecting()
    {
        if (!agent.isOnNavMesh) return;

        // Scan for loot along the way (including the target)
        if (TryGrabNearbyLoot(MenialState.Collecting)) return;

        if (targetLoot == null || targetLoot.IsCollected)
        {
            // Original target gone — look for more loot nearby
            targetLoot = FindNearestUncollectedLoot();
            if (targetLoot != null)
            {
                agent.SetDestination(targetLoot.transform.position);
                return;
            }
            ReturnHome();
            return;
        }

        // Re-path if agent has no path or got stuck
        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 0.5f))
        {
            agent.SetDestination(targetLoot.transform.position);
        }
    }

    private void UpdateReturning()
    {
        if (!agent.isOnNavMesh) return;

        // Scan for loot along the way home
        if (TryGrabNearbyLoot(MenialState.Returning)) return;

        // Check if we're back inside the courtyard
        float distFromCenter = new Vector2(transform.position.x, transform.position.z).magnitude;
        if (distFromCenter < 3.5f)
        {
            // Arrived home - deposit treasure
            if (carriedTreasure > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddTreasure(carriedTreasure);
                Debug.Log($"[Menial] Deposited {carriedTreasure} gold!");
                carriedTreasure = 0;
            }
            CurrentState = MenialState.Idle;
            IsOutsideWalls = false;
            agent.stoppingDistance = 0f;
            if (GameManager.Instance != null)
                GameManager.Instance.IdleMenialCount++;
        }
        else if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 0.5f))
        {
            // Re-path to home
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float homeDist = Random.Range(2f, 3f);
            Vector3 homePos = new Vector3(Mathf.Cos(angle) * homeDist, 0, Mathf.Sin(angle) * homeDist);
            agent.SetDestination(homePos);
        }
    }

    /// <summary>
    /// Scan for loot within PICKUP_RADIUS. If found, collect it and enter PickingUp state
    /// with a brief delay. Returns true if a pickup was initiated.
    /// </summary>
    private bool TryGrabNearbyLoot(MenialState resumeState)
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0) return false;
        scanTimer = SCAN_INTERVAL;

        var allPickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        TreasurePickup closest = null;
        float closestDist = PICKUP_RADIUS;

        foreach (var pickup in allPickups)
        {
            if (pickup == null || pickup.IsCollected) continue;
            float dist = Vector3.Distance(transform.position, pickup.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }

        if (closest != null)
        {
            carriedTreasure += closest.Value;
            Debug.Log($"[Menial] Picked up {closest.Value} gold (carrying {carriedTreasure} total) at {closest.transform.position}");
            closest.Collect();

            if (closest == targetLoot)
                targetLoot = null;

            // Pause briefly
            stateBeforePickup = resumeState;
            CurrentState = MenialState.PickingUp;
            pickupTimer = PICKUP_DELAY;
            if (agent.isOnNavMesh)
                agent.isStopped = true;
            return true;
        }

        return false;
    }

    private void UpdatePickingUp()
    {
        pickupTimer -= Time.deltaTime;
        if (pickupTimer > 0) return;

        // Resume previous state
        CurrentState = stateBeforePickup;
        if (agent.isOnNavMesh)
            agent.isStopped = false;

        // After pickup, check if there's more loot in range — will be caught next frame's scan
        // If we were collecting and our target is gone, UpdateCollecting will find new loot or return home
    }

    /// <summary>
    /// Find the nearest uncollected loot in the world (used when original target is gone).
    /// </summary>
    private TreasurePickup FindNearestUncollectedLoot()
    {
        var allPickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        TreasurePickup best = null;
        float bestDist = 10f; // Only look within 10 units

        foreach (var pickup in allPickups)
        {
            if (pickup == null || pickup.IsCollected) continue;
            float dist = Vector3.Distance(transform.position, pickup.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = pickup;
            }
        }
        return best;
    }

    private void ReturnHome()
    {
        CurrentState = MenialState.Returning;
        targetGate = null;

        // Find nearest gate to return through
        Gate returnGate = FindNearestGateToSelf();
        if (returnGate != null)
        {
            // Path toward the gate first - it will open as we approach
            Vector3 gatePos = returnGate.transform.position;
            Vector3 toCenter = -gatePos.normalized;
            Vector3 homePos = gatePos + toCenter * 3f;
            homePos.y = 0;

            if (agent.isOnNavMesh)
            {
                agent.stoppingDistance = 0.5f;
                agent.SetDestination(homePos);
            }
        }
        else
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2f, 3f);
            Vector3 homePos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

            if (agent.isOnNavMesh)
            {
                agent.stoppingDistance = 0.5f;
                agent.SetDestination(homePos);
            }
        }
    }

    private Gate FindNearestGateToSelf()
    {
        Gate nearest = null;
        float bestDist = float.MaxValue;
        var gates = FindObjectsByType<Gate>(FindObjectsSortMode.None);
        foreach (var gate in gates)
        {
            float dist = Vector3.Distance(transform.position, gate.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = gate;
            }
        }
        return nearest;
    }

    public void SendToTower(System.Action callback)
    {
        if (IsDead) return;
        CurrentState = MenialState.EnteringTower;
        onEnteredTower = callback;

        if (GameManager.Instance != null && IsIdle)
            GameManager.Instance.IdleMenialCount--;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0.5f;
            agent.isStopped = false;
            agent.SetDestination(Vector3.zero);
        }
        Debug.Log($"[Menial] Heading to tower for conversion at {transform.position}");
    }

    private void UpdateEnteringTower()
    {
        if (!agent.isOnNavMesh) return;

        float distFromCenter = Vector3.Distance(transform.position, Vector3.zero);
        // Use generous threshold - ballista/tower occupies the center
        if (distFromCenter < 3f)
        {
            Debug.Log("[Menial] Entered tower, being consumed.");
            onEnteredTower?.Invoke();
            onEnteredTower = null;

            if (GameManager.Instance != null)
                GameManager.Instance.RemoveMenial();

            IsDead = true;
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMenialHit(transform.position);
        if (CurrentState == MenialState.Idle && GameManager.Instance != null)
            GameManager.Instance.IdleMenialCount--;

        if (GameManager.Instance != null)
            GameManager.Instance.RemoveMenial();

        Destroy(gameObject);
    }
}
