using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum MenialState
{
    Idle,
    Collecting,
    Returning,
    EnteringTower,
    PickingUp, // Brief pause while grabbing a loot item
    ClearingVegetation, // Pausing to clear vegetation blocking path
    Fleeing // Running away from nearby enemies
}

[RequireComponent(typeof(NavMeshAgent))]
public class Menial : MonoBehaviour
{
    [SerializeField] private int maxHP = 15;
    [SerializeField] private float moveSpeed = 4f;
    [Header("Model Override")]
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private RuntimeAnimatorController animatorController;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private TreasurePickup targetLoot;
    private int carriedTreasure;

    // Pickup radius — menial grabs any loot within this distance
    private const float PICKUP_RADIUS = 2.0f;
    // Time to pause per loot item collected
    private const float PICKUP_DELAY = 0.25f;
    private float pickupTimer;
    private MenialState stateBeforePickup; // state to resume after pickup delay

    // Vegetation clearing
    private Vegetation targetVegetation;
    private Vector3 clearingCenter;   // center of player-assigned clearing area
    private float clearingRadius;     // radius of the clearing area (0 = no assignment)
    private float clearingTimer;
    private MenialState stateBeforeClearing;
    private const float VEGETATION_CLEAR_RANGE = 1.5f;

    // Fleeing from enemies
    private MenialState stateBeforeFleeing;
    private float dangerScanTimer;
    private const float DANGER_SCAN_INTERVAL = 0.15f;
    private const float FLEE_SPEED_MULTIPLIER = 1.5f;
    private float normalSpeed;

    // Wandering
    private float wanderTimer;
    private const float WANDER_INTERVAL_MIN = 3f;
    private const float WANDER_INTERVAL_MAX = 8f;
    private const float WANDER_RADIUS = 2.5f;

    // Scan throttle — don't scan every frame
    private float scanTimer;
    private const float SCAN_INTERVAL = 0.15f;

    public static event System.Action OnMenialDied;

    public MenialState CurrentState { get; private set; } = MenialState.Idle;
    public bool IsOutsideWalls { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsIdle => CurrentState == MenialState.Idle;
    /// <summary>True if menial can accept a new assignment (idle, or returning inside the courtyard).</summary>
    public bool IsAvailable => !IsDead && (CurrentState == MenialState.Idle || CurrentState == MenialState.Returning);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        RefreshSpeed();
        currentHP = maxHP;

        // Swap model if a custom model is assigned
        if (modelPrefab != null)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var newModel = Instantiate(modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = newModel.AddComponent<Animator>();
            if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;

            Debug.Log("[Menial] Custom model loaded");
        }
    }

    private void OnEnable()
    {
        // Re-acquire references after domain reload (Awake is not re-called)
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            Debug.Log("[Menial] Re-acquired NavMeshAgent in OnEnable (domain reload).");
        }
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Reapply move speed with the current daily event multiplier.
    /// Called by DailyEventManager when the event changes.
    /// </summary>
    public void RefreshSpeed()
    {
        if (agent == null) return;
        float dailySpeed = DailyEventManager.Instance != null ? DailyEventManager.Instance.MenialSpeedMultiplier : 1f;
        agent.speed = moveSpeed * dailySpeed;
    }

    private void Start()
    {
        wanderTimer = Random.Range(1f, WANDER_INTERVAL_MAX);
        Debug.Log($"[Menial] Spawned at {transform.position}, HP={maxHP}, speed={moveSpeed}, state={CurrentState}");
    }

    private System.Action onEnteredTower;

    private void Update()
    {
        if (IsDead) return;

        // Check for danger before any task logic
        if (CheckForDanger()) return;

        switch (CurrentState)
        {
            case MenialState.Idle:
                UpdateIdle();
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
            case MenialState.ClearingVegetation:
                UpdateClearingVegetation();
                break;
            case MenialState.Fleeing:
                UpdateFleeing();
                break;
        }

        // Track if we're outside the wall ring
        Vector3 fc = GameManager.FortressCenter;
        float distFromCenter = new Vector2(transform.position.x - fc.x, transform.position.z - fc.z).magnitude;
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

        // Keep wander targets in the courtyard ring (2.5-3.5 from center, outside tower)
        Vector3 fc = GameManager.FortressCenter;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(2.5f, 3.5f);
        Vector3 wanderTarget = fc + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(wanderTarget, out hit, 1f, NavMesh.AllAreas))
        {
            // Must be inside courtyard AND outside tower radius
            float hitDist = new Vector2(hit.position.x - fc.x, hit.position.z - fc.z).magnitude;
            if (hitDist >= 2f && hitDist < 4f)
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    public void AssignLoot(TreasurePickup loot)
    {
        if (!IsAvailable) return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[Menial] NOT ON NAVMESH - cannot path to loot!");
            return;
        }

        // Deposit any carried treasure before heading out again
        if (carriedTreasure > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddTreasure(carriedTreasure);
            Debug.Log($"[Menial] Deposited {carriedTreasure} gold before new assignment.");
            carriedTreasure = 0;
        }

        bool wasIdle = CurrentState == MenialState.Idle;
        targetLoot = loot;

        if (wasIdle && GameManager.Instance != null)
            GameManager.Instance.IdleMenialCount--;

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        CurrentState = MenialState.Collecting;
        agent.SetDestination(loot.transform.position);
        Debug.Log($"[Menial] Assigned to collect loot at {loot.transform.position}");
    }

    public void AssignVegetationArea(Vector3 center, float radius)
    {
        if (!IsAvailable) return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[Menial] NOT ON NAVMESH - cannot path to vegetation area!");
            return;
        }

        // Deposit any carried treasure before heading out again
        if (carriedTreasure > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddTreasure(carriedTreasure);
            Debug.Log($"[Menial] Deposited {carriedTreasure} gold before new assignment.");
            carriedTreasure = 0;
        }

        bool wasIdle = CurrentState == MenialState.Idle;
        clearingCenter = center;
        clearingRadius = radius;

        if (wasIdle && GameManager.Instance != null)
            GameManager.Instance.IdleMenialCount--;

        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        CurrentState = MenialState.Collecting;
        agent.SetDestination(center);
        Debug.Log($"[Menial] Assigned to clear vegetation area at {center}, radius={radius}.");
    }

    /// <summary>
    /// Find the nearest alive vegetation within the assigned clearing area.
    /// </summary>
    private Vegetation FindNextVegetationInArea()
    {
        if (clearingRadius <= 0) return null;

        var allVeg = FindObjectsByType<Vegetation>(FindObjectsSortMode.None);
        Vegetation best = null;
        float bestDist = float.MaxValue;

        foreach (var veg in allVeg)
        {
            if (veg == null || veg.IsDead) continue;
            // Must be within the clearing area
            if (Vector3.Distance(clearingCenter, veg.transform.position) > clearingRadius) continue;
            float dist = Vector3.Distance(transform.position, veg.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = veg;
            }
        }
        return best;
    }

    private void UpdateCollecting()
    {
        if (!agent.isOnNavMesh) return;

        // Scan for loot along the way (including the target)
        if (TryGrabNearbyLoot(MenialState.Collecting)) return;

        // Clear vegetation blocking path
        if (TryClearNearbyVegetation(MenialState.Collecting)) return;

        if (targetLoot == null || targetLoot.IsCollected)
        {
            // Original loot target gone — look for more loot nearby
            targetLoot = FindNearestUncollectedLoot();
            if (targetLoot != null)
            {
                agent.SetDestination(targetLoot.transform.position);
                return;
            }

            // No loot — check if there's more vegetation to clear in the assigned area
            if (clearingRadius > 0)
            {
                var nextVeg = FindNextVegetationInArea();
                if (nextVeg != null)
                {
                    // Walk toward next vegetation; TryClearNearbyVegetation handles clearing at 1.5m
                    if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 0.5f))
                    {
                        agent.SetDestination(nextVeg.transform.position);
                    }
                    return;
                }
                // Area is clear — done
                clearingRadius = 0;
                Debug.Log($"[Menial] Clearing area at {clearingCenter} is fully cleared. Heading home.");
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

        // Clear vegetation blocking path
        if (TryClearNearbyVegetation(MenialState.Returning)) return;

        // Check if we're back inside the courtyard
        Vector3 fc = GameManager.FortressCenter;
        float distFromCenter = new Vector2(transform.position.x - fc.x, transform.position.z - fc.z).magnitude;
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
            // Re-path to home (courtyard ring, outside tower)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float homeDist = Random.Range(2.5f, 3.5f);
            Vector3 homePos = fc + new Vector3(Mathf.Cos(angle) * homeDist, 0, Mathf.Sin(angle) * homeDist);
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

    private void UpdateClearingVegetation()
    {
        // If vegetation was destroyed externally, resume immediately
        if (targetVegetation == null || targetVegetation.IsDead)
        {
            Debug.Log("[Menial] Target vegetation gone, resuming previous state.");
            targetVegetation = null;
            CurrentState = stateBeforeClearing;
            if (agent.isOnNavMesh) agent.isStopped = false;
            return;
        }

        clearingTimer -= Time.deltaTime;
        if (clearingTimer <= 0)
        {
            Debug.Log($"[Menial] Finished clearing {targetVegetation.Type} at {targetVegetation.transform.position}");
            targetVegetation.Clear();
            targetVegetation = null;
            CurrentState = stateBeforeClearing;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    private bool TryClearNearbyVegetation(MenialState resumeState)
    {
        if (VegetationManager.Instance == null) return false;

        // Reuse the scan timer from loot scanning (already decremented in TryGrabNearbyLoot)
        // Only check when scan timer allows — but since scan timer is shared, we check here too
        // to avoid a separate timer. The scan interval is short enough.
        var veg = VegetationManager.Instance.FindNearestVegetation(transform.position, VEGETATION_CLEAR_RANGE);
        if (veg != null && !veg.IsDead)
        {
            targetVegetation = veg;
            clearingTimer = veg.ClearTimeRequired;
            stateBeforeClearing = resumeState;
            CurrentState = MenialState.ClearingVegetation;
            if (agent.isOnNavMesh) agent.isStopped = true;
            Debug.Log($"[Menial] Clearing {veg.Type} at {veg.transform.position} (time={clearingTimer}s)");
            return true;
        }

        return false;
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

    /// <summary>
    /// Scan for nearby enemies. If any enemy is within its attack range, enter Fleeing state.
    /// Returns true if currently fleeing (caller should skip normal task logic).
    /// </summary>
    private bool CheckForDanger()
    {
        // Already fleeing — let the switch dispatch to UpdateFleeing
        if (CurrentState == MenialState.Fleeing) return false;

        // Don't interrupt tower entry
        if (CurrentState == MenialState.EnteringTower) return false;

        dangerScanTimer -= Time.deltaTime;
        if (dangerScanTimer > 0) return false;
        dangerScanTimer = DANGER_SCAN_INTERVAL;

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            float dangerRange = enemy.Data != null ? enemy.Data.attackRange + 1f : 2.5f;
            if (dist < dangerRange)
            {
                EnterFleeState();
                Debug.Log($"[Menial] Danger! Enemy {enemy.Data?.enemyName} within {dist:F1}m (range={dangerRange:F1}). Fleeing toward courtyard!");
                return true;
            }
        }
        return false;
    }

    private void EnterFleeState()
    {
        stateBeforeFleeing = CurrentState;

        // If we were idle, update the idle count
        if (CurrentState == MenialState.Idle && GameManager.Instance != null)
            GameManager.Instance.IdleMenialCount--;

        CurrentState = MenialState.Fleeing;
        normalSpeed = agent.speed;
        float dailySpeed = DailyEventManager.Instance != null ? DailyEventManager.Instance.MenialSpeedMultiplier : 1f;
        agent.speed = moveSpeed * dailySpeed * FLEE_SPEED_MULTIPLIER;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.5f;
            // Flee toward courtyard ring, not tower center
            Vector3 fc = GameManager.FortressCenter;
            float fleeAngle = Mathf.Atan2(transform.position.z - fc.z, transform.position.x - fc.x);
            Vector3 fleeDest = fc + new Vector3(Mathf.Cos(fleeAngle) * 3f, 0, Mathf.Sin(fleeAngle) * 3f);
            agent.SetDestination(fleeDest);
        }
    }

    private void UpdateFleeing()
    {
        if (!agent.isOnNavMesh) return;

        // Check if still in danger
        dangerScanTimer -= Time.deltaTime;
        if (dangerScanTimer <= 0)
        {
            dangerScanTimer = DANGER_SCAN_INTERVAL;
            bool stillInDanger = false;

            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                float dangerRange = enemy.Data != null ? enemy.Data.attackRange + 1f : 2.5f;
                if (dist < dangerRange)
                {
                    stillInDanger = true;
                    break;
                }
            }

            if (!stillInDanger)
            {
                // Safe — resume previous task
                agent.speed = normalSpeed;
                Debug.Log($"[Menial] Safe now. Resuming state={stateBeforeFleeing}.");

                // If we were idle before, restore idle count
                if (stateBeforeFleeing == MenialState.Idle && GameManager.Instance != null)
                    GameManager.Instance.IdleMenialCount++;

                CurrentState = stateBeforeFleeing;

                // Re-path to appropriate destination based on resumed state
                switch (CurrentState)
                {
                    case MenialState.Collecting:
                        if (targetLoot != null && !targetLoot.IsCollected)
                            agent.SetDestination(targetLoot.transform.position);
                        else
                        {
                            var nextVeg = FindNextVegetationInArea();
                            if (nextVeg != null)
                                agent.SetDestination(nextVeg.transform.position);
                            else
                                ReturnHome();
                        }
                        break;
                    case MenialState.Returning:
                        ReturnHome();
                        break;
                }
                return;
            }
        }

        // Still fleeing — keep heading toward courtyard ring (not tower center)
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            Vector3 fc = GameManager.FortressCenter;
            float fleeAngle = Mathf.Atan2(transform.position.z - fc.z, transform.position.x - fc.x);
            Vector3 fleeDest = fc + new Vector3(Mathf.Cos(fleeAngle) * 3f, 0, Mathf.Sin(fleeAngle) * 3f);
            agent.SetDestination(fleeDest);
        }
    }

    private void ReturnHome()
    {
        Debug.Log($"[Menial] Returning home with {carriedTreasure} gold from {transform.position}");
        CurrentState = MenialState.Returning;
        clearingRadius = 0;

        Vector3 fc = GameManager.FortressCenter;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(2.5f, 3.5f);
        Vector3 homePos = fc + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

        if (agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(homePos);
        }
    }

    public void SendToTower(System.Action callback)
    {
        if (IsDead) return;
        bool wasIdle = IsIdle;
        CurrentState = MenialState.EnteringTower;
        onEnteredTower = callback;

        if (GameManager.Instance != null && wasIdle)
            GameManager.Instance.IdleMenialCount--;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0.5f;
            agent.isStopped = false;
            agent.SetDestination(GameManager.FortressCenter);
        }
        Debug.Log($"[Menial] Heading to tower for conversion at {transform.position}");
    }

    private void UpdateEnteringTower()
    {
        if (!agent.isOnNavMesh) return;

        float distFromCenter = Vector3.Distance(transform.position, GameManager.FortressCenter);
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
        Debug.Log($"[Menial] Took {damage} damage at {transform.position}. HP={currentHP}/{maxHP}, state={CurrentState}");
        FloatingDamageNumber.Spawn(transform.position, damage, false);
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

        OnMenialDied?.Invoke();
        Debug.Log($"[Menial] Menial died at {transform.position}. OnMenialDied fired.");

        if (animator != null)
        {
            animator.SetTrigger("Die");
            if (agent != null) agent.enabled = false;
            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
