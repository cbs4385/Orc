using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum RefugeePowerUp
{
    None,
    DoubleShot,
    BurstDamage
}

[RequireComponent(typeof(NavMeshAgent))]
public class Refugee : MonoBehaviour
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private float moveSpeed = 3.5f;
    [Header("Model Override")]
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private RuntimeAnimatorController animatorController;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private float actionAnimTimer;
    private const float IDLE_WALK_FRAME = 0.25f; // frame 6 of 24
    private bool isDead;
    private bool arrived;

    private RefugeePowerUp carriedPowerUp = RefugeePowerUp.None;
    private Renderer[] bodyRenderers;
    private Color[] originalColors;

    // Pacing detection
    private Vector3 pacingSampleA;
    private Vector3 pacingSampleB;
    private Vector3 pacingSampleC;
    private int pacingSampleCount;
    private float pacingSampleTimer;
    private const float PACING_SAMPLE_INTERVAL = 1f;
    private const float PACING_RETURN_THRESHOLD = 2.0f;

    // Navigation progress logging
    private float navLogTimer;
    private const float NAV_LOG_INTERVAL = 3f;
    private bool loggedCrossingWallRing;
    private bool loggedInsideCourtyard;

    // Reroute through cardinal gap when direct path is blocked
    private bool usingWaypoint;
    private Vector3 currentWaypoint;
    private int waypointAttempts;

    public static event System.Action OnRefugeeSaved;
    public static event System.Action OnRefugeeDied;

    public RefugeePowerUp CarriedPowerUp => carriedPowerUp;
    public bool HasPowerUp => carriedPowerUp != RefugeePowerUp.None;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Size BoxCollider to match character body, not the default 1x1x1.
        // Must be narrow enough to fit through wall gaps (1.2 units between segments).
        var boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.size = new Vector3(0.4f, 1.0f, 0.4f);
            boxCol.center = new Vector3(0, 0.5f, 0);
        }
        agent.speed = moveSpeed;
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

            Debug.Log("[Refugee] Custom model loaded");
        }

        // Cache all renderers and their original colors for power-up tinting
        bodyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            originalColors[i] = bodyRenderers[i].material.color;
        }
    }

    private void Start()
    {
        FriendlyIndicator.Create(transform, new Color(0.9f, 0.9f, 0.2f)); // Yellow

        // Navigate to tower center
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(GameManager.FortressCenter);

            Debug.Log($"[Refugee] Start: pos={transform.position}, dest={GameManager.FortressCenter}, isOnNavMesh={agent.isOnNavMesh}");
        }
        else
        {
            Debug.LogError($"[Refugee] Start: NOT on NavMesh! pos={transform.position}. Cannot path to fortress.");
        }
    }

    public void SetPowerUp(RefugeePowerUp powerUp)
    {
        carriedPowerUp = powerUp;
        Debug.Log($"[Refugee] Power-up assigned: {powerUp} on {gameObject.name} at {transform.position}");

        // Visual indicator - power-up refugees tint all renderers
        if (bodyRenderers != null && powerUp != RefugeePowerUp.None)
        {
            Color tint = Color.white;
            switch (powerUp)
            {
                case RefugeePowerUp.DoubleShot:
                    tint = new Color(1f, 0.85f, 0f); // Gold
                    break;
                case RefugeePowerUp.BurstDamage:
                    tint = new Color(1f, 0.4f, 0.1f); // Orange-red
                    break;
            }
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].material.color = tint;
            }
        }
    }

    private void UpdateIdleAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (actionAnimTimer > 0) { actionAnimTimer -= Time.deltaTime; return; }

        bool isMoving = agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.1f;
        if (isMoving)
        {
            if (animator.speed < 0.01f) animator.speed = 1f;
        }
        else
        {
            animator.Play("Walk", 0, IDLE_WALK_FRAME);
            animator.speed = 0f;
        }
    }

    private void Update()
    {
        if (isDead || arrived) return;

        UpdateIdleAnimation();

        // Check actual distance to fortress center
        float distToFortress = Vector3.Distance(transform.position, GameManager.FortressCenter);
        if (distToFortress < 2f)
        {
            ArriveAtFortress(distToFortress);
            return;
        }

        // Navigation progress logging — log position, distance, velocity, path status every 3s
        navLogTimer -= Time.deltaTime;
        if (navLogTimer <= 0f)
        {
            navLogTimer = NAV_LOG_INTERVAL;
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            string pathStatus = agent != null ? agent.pathStatus.ToString() : "NoAgent";
            bool hasPath = agent != null && agent.hasPath;
            int corners = (agent != null && agent.path != null) ? agent.path.corners.Length : 0;
            Debug.Log($"[Refugee] NAV pos={transform.position}, dist={distToFortress:F1}, speed={speed:F1}, pathStatus={pathStatus}, hasPath={hasPath}, corners={corners}, waypoint={usingWaypoint}");
        }

        // Log crossing the wall ring (dist ~4.5) and entering courtyard (dist ~3.5)
        if (!loggedCrossingWallRing && distToFortress < 5f)
        {
            loggedCrossingWallRing = true;
            Debug.Log($"[Refugee] Approaching wall ring at {transform.position}, dist={distToFortress:F2}");
        }
        if (!loggedInsideCourtyard && distToFortress < 3.5f)
        {
            loggedInsideCourtyard = true;
            Debug.Log($"[Refugee] Inside courtyard at {transform.position}, dist={distToFortress:F2}");
        }

        // Pacing detection: sample position periodically.
        // If sample C returns to near sample A while B was different, agent is oscillating.
        pacingSampleTimer += Time.deltaTime;
        if (pacingSampleTimer >= PACING_SAMPLE_INTERVAL)
        {
            pacingSampleTimer = 0f;
            pacingSampleA = pacingSampleB;
            pacingSampleB = pacingSampleC;
            pacingSampleC = transform.position;
            pacingSampleCount++;

            if (pacingSampleCount >= 3)
            {
                float returnDist = Vector3.Distance(pacingSampleC, pacingSampleA);
                float travelDist = Vector3.Distance(pacingSampleA, pacingSampleB);
                float totalMovement = Vector3.Distance(pacingSampleA, pacingSampleB) + Vector3.Distance(pacingSampleB, pacingSampleC);
                bool isPacing = returnDist < PACING_RETURN_THRESHOLD && travelDist > PACING_RETURN_THRESHOLD;
                bool isStationary = totalMovement < 0.3f && distToFortress > 4f;
                if (isPacing || isStationary)
                {
                    string stuckType = isPacing ? "PACING" : "STATIONARY";
                    Debug.LogWarning($"[Refugee] {stuckType} at {transform.position}, dist={distToFortress:F2}. Trying cardinal gap waypoint.");
                    pacingSampleCount = 0;
                    TryCardinalWaypoint();
                }
            }
        }

        // If using a waypoint, check if we've reached it and switch to fortress center
        if (usingWaypoint && agent != null && agent.isOnNavMesh)
        {
            float distToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
            if (distToWaypoint < 3f)
            {
                usingWaypoint = false;
                agent.SetDestination(GameManager.FortressCenter);
                Debug.Log($"[Refugee] Reached cardinal waypoint, now heading to fortress center.");
            }
        }

        // If agent has no path, stale path, or empty path (degenerate state), retry
        if (agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            bool noPath = !agent.hasPath;
            bool stalePath = agent.isPathStale;
            bool emptyCorners = agent.path != null && agent.path.corners.Length == 0;
            bool needsRepath = noPath || stalePath || emptyCorners;
            if (needsRepath)
            {
                Vector3 dest = usingWaypoint ? currentWaypoint : GameManager.FortressCenter;
                agent.SetDestination(dest);
                Debug.Log($"[Refugee] Repath: noPath={noPath}, stale={stalePath}, emptyCorners={emptyCorners}, pos={transform.position}, waypoint={usingWaypoint}");
            }
        }
    }


    /// <summary>
    /// When stuck, try pathing to a cardinal gap position (S/N/W/E of fortress)
    /// instead of directly to center. The gap between wall segments on each side
    /// provides a walkable path that corners lack.
    /// </summary>
    private void TryCardinalWaypoint()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Cardinal positions just outside the wall ring (6 units from center)
        Vector3[] cardinalGaps = {
            new Vector3(0, 0, -6f),   // south
            new Vector3(0, 0, 6f),    // north
            new Vector3(-6f, 0, 0),   // west
            new Vector3(6f, 0, 0),    // east
        };

        // Find the nearest cardinal gap that has a valid path from our position
        Vector3 bestGap = Vector3.zero;
        float bestDist = float.MaxValue;
        bool foundValid = false;

        foreach (var gap in cardinalGaps)
        {
            var testPath = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, gap, NavMesh.AllAreas, testPath);
            if (testPath.status == NavMeshPathStatus.PathComplete)
            {
                float dist = Vector3.Distance(transform.position, gap);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestGap = gap;
                    foundValid = true;
                }
            }
        }

        if (foundValid)
        {
            usingWaypoint = true;
            currentWaypoint = bestGap;
            agent.SetDestination(bestGap);
            waypointAttempts++;
            Debug.Log($"[Refugee] Rerouting via cardinal gap at {bestGap} (dist={bestDist:F1}, attempt={waypointAttempts})");
        }
        else
        {
            Debug.LogWarning($"[Refugee] No valid cardinal gap path found from {transform.position}. Refugee is trapped.");
        }
    }

    private void ArriveAtFortress(float distToFortress)
    {
        arrived = true;
        Debug.Log($"[Refugee] Arrived at fortress center. Position={transform.position}, dist={distToFortress:F1}");
        OnRefugeeSaved?.Invoke();

        // Apply power-up to the active ballista
        if (carriedPowerUp != RefugeePowerUp.None && BallistaManager.Instance != null)
        {
            var ballista = BallistaManager.Instance.ActiveBallista;
            if (ballista != null)
            {
                Debug.Log($"[Refugee] Applying power-up {carriedPowerUp} to ballista {ballista.name}");
                ApplyPowerUp(ballista);
            }
            else
            {
                Debug.LogWarning($"[Refugee] Has power-up {carriedPowerUp} but no active ballista to apply it to!");
            }
        }

        // Also add a menial
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMenial();
        }
        if (MenialManager.Instance != null)
        {
            var newMenial = MenialManager.Instance.SpawnMenial();
            if (newMenial == null)
                Debug.LogError("[Refugee] SpawnMenial returned null! Count incremented but no menial created.");
        }
        else
        {
            Debug.LogError("[Refugee] MenialManager.Instance is null! Count incremented but no menial spawned.");
        }

        Destroy(gameObject);
    }

    private void ApplyPowerUp(Ballista ballista)
    {
        switch (carriedPowerUp)
        {
            case RefugeePowerUp.DoubleShot:
                ballista.EnableDoubleShot();
                Debug.Log("[Refugee] Power-up applied: DoubleShot to ballista");
                break;
            case RefugeePowerUp.BurstDamage:
                ballista.EnableBurstDamage();
                Debug.Log("[Refugee] Power-up applied: BurstDamage to ballista");
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHP -= damage;
        Debug.Log($"[Refugee] Took {damage} damage at {transform.position}. HP={currentHP}/{maxHP}");
        FloatingDamageNumber.Spawn(transform.position, damage, false);
        if (currentHP <= 0)
        {
            isDead = true;
            Debug.Log($"[Refugee] Died at {transform.position}");
            OnRefugeeDied?.Invoke();

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
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
