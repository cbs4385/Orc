using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Defender : MonoBehaviour
{
    [SerializeField] protected DefenderData data;
    [SerializeField] protected float attackCooldown;

    protected Enemy currentTarget;
    protected NavMeshAgent agent;
    private int currentHP;
    private Animator animator;
    private float actionAnimTimer;
    private const float IDLE_WALK_FRAME = 0.25f; // frame 6 of 24

    // Tower state
    protected TowerPositionManager.TowerPosition assignedTower;
    protected bool isOnTower;
    private float towerSeekTimer;
    private const float TOWER_SEEK_INTERVAL = 1.0f;
    private bool isWalkingToTower;
    private float towerReassessTimer;
    private const float TOWER_REASSESS_INTERVAL = 3.0f;

    // Guard state (assigned dynamically when engineer moves to exposed area)
    private Engineer guardTarget;
    private bool isGuarding;
    private float guardFollowDist = 2.0f;
    private float guardStuckTimer;
    private float guardLastDist = float.MaxValue;
    private const float GUARD_STUCK_TIMEOUT = 5f;

    public DefenderData Data => data;
    public bool IsDead { get; private set; }
    public bool IsGuarding => isGuarding;
    public bool IsOnTower => isOnTower;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void OnEnable()
    {
        // Re-acquire references after domain reload
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    public virtual void Initialize(DefenderData defenderData)
    {
        data = defenderData;
        currentHP = data.maxHP;

        // Swap model if the DefenderData specifies a custom model
        if (data.modelPrefab != null)
        {
            // Destroy all existing visual children (primitive placeholders)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var newModel = Instantiate(data.modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = newModel.AddComponent<Animator>();
            if (data.animatorController != null)
                animator.runtimeAnimatorController = data.animatorController;
            animator.applyRootMotion = false;

            Debug.Log($"[Defender] Custom model loaded for {data.defenderName}");
        }
        else
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = data.bodyColor;
            }
        }

        if (agent != null)
        {
            agent.stoppingDistance = 0.3f;
        }

        FriendlyIndicator.Create(transform, new Color(0.3f, 0.5f, 1f)); // Blue
        Debug.Log($"[Defender] {data.defenderName} initialized, HP={currentHP}");
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        attackCooldown -= Time.deltaTime;
        UpdateIdleAnimation();
        FindTarget();

        if (currentTarget != null)
        {
            if (isOnTower)
            {
                // On tower: face target and attack if in range, don't move
                Vector3 toTarget = currentTarget.transform.position - transform.position;
                toTarget.y = 0;
                if (toTarget.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);

                float dist = GetDistanceToTarget(currentTarget);
                float range = data != null ? data.range : 5f;

                if (attackCooldown <= 0 && dist <= range)
                {
                    Attack();
                    if (animator != null)
                    {
                        animator.speed = 1f;
                        animator.SetTrigger("Attack");
                        actionAnimTimer = 1.0f;
                    }
                }
                else if (dist > range)
                {
                    // Target out of range — consider switching to a closer tower
                    ReassessTower();
                }
            }
            else if (isWalkingToTower && assignedTower != null)
            {
                // Walking to tower — keep going, mount when arrived
                if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    MountTower();
                }
            }
            else
            {
                // Not on tower, not walking to tower — should we get on one?
                if (ShouldUseTower() && !isGuarding && assignedTower == null)
                {
                    // Seek a tower even while enemies are present
                    SeekTowerPosition();
                }

                // Guarding: stay near engineer, but still fight if enemy is close
                if (isGuarding && guardTarget != null && !guardTarget.IsDead)
                {
                    FollowGuardTarget();
                }
                else if (!isWalkingToTower)
                {
                    MoveTowardTarget();
                }

                if (attackCooldown <= 0)
                {
                    float dist = GetDistanceToTarget(currentTarget);
                    float range = data != null ? data.range : 5f;
                    if (dist <= range)
                    {
                        Attack();
                        if (animator != null)
                        {
                            animator.speed = 1f;
                            animator.SetTrigger("Attack");
                            actionAnimTimer = 1.0f;
                        }
                    }
                }
            }
        }
        else
        {
            // No target
            if (isGuarding && guardTarget != null && !guardTarget.IsDead)
            {
                // Follow engineer even when no enemies around
                FollowGuardTarget();
            }
            else if (isOnTower)
            {
                // On tower with no target — reassess to move closer to enemies
                ReassessTower();
            }
            else if (ShouldUseTower() && !isGuarding)
            {
                SeekTowerPosition();
            }
            else if (!isGuarding)
            {
                // No target, no tower, no guard - stop moving
                // But never stop inside wall geometry (could be passing through a gap)
                if (agent != null && agent.isOnNavMesh)
                {
                    if (IsInsideWallGeometry(transform.position))
                    {
                        // Inside a wall — move toward fortress center to escape
                        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} inside wall geometry at {transform.position} — escaping to courtyard");
                        agent.isStopped = false;
                        agent.SetDestination(GameManager.FortressCenter);
                    }
                    else
                    {
                        agent.isStopped = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get distance to target, using XZ-only distance when on tower
    /// so elevation doesn't reduce effective range.
    /// </summary>
    protected float GetDistanceToTarget(Enemy enemy)
    {
        if (enemy == null) return float.MaxValue;
        Vector3 pos = transform.position;
        Vector3 ePos = enemy.transform.position;

        if (isOnTower)
        {
            return Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(ePos.x, ePos.z));
        }
        return Vector3.Distance(pos, ePos);
    }

    private Enemy lastLoggedTarget;

    protected virtual void FindTarget()
    {
        // Search wide enough to find enemies at walls from anywhere in the courtyard
        float searchRange = 15f;
        currentTarget = null;
        float bestDist = searchRange;
        int skippedLOS = 0;

        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                // Ground-level defenders need line-of-sight (tower defenders shoot over walls)
                if (!isOnTower && HasWallBetween(transform.position, enemy.transform.position))
                {
                    skippedLOS++;
                    continue;
                }

                bestDist = dist;
                currentTarget = enemy;
            }
        }

        if (currentTarget != lastLoggedTarget)
        {
            if (currentTarget != null)
                Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} targeting {currentTarget.name} at dist={bestDist:F1} (onTower={isOnTower}, skippedLOS={skippedLOS})");
            else if (lastLoggedTarget != null)
                Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} lost target (no enemies in range or LOS, skippedLOS={skippedLOS})");
            lastLoggedTarget = currentTarget;
        }
    }

    /// <summary>
    /// Returns true if a wall collider blocks the ray between two positions.
    /// </summary>
    protected bool HasWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 origin = new Vector3(from.x, 0.5f, from.z);
        Vector3 target = new Vector3(to.x, 0.5f, to.z);
        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist < 0.1f) return false;

        if (Physics.Raycast(origin, dir / dist, out RaycastHit hit, dist))
        {
            if (hit.collider.GetComponentInParent<Wall>() != null)
                return true;
        }
        return false;
    }

    protected virtual void MoveTowardTarget()
    {
        if (agent == null || !agent.isOnNavMesh || currentTarget == null) return;
        if (isWalkingToTower) return; // Don't override tower destination with courtyard clamp

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        float range = data != null ? data.range : 5f;

        if (dist > range)
        {
            // Path toward enemy but clamp destination inside the courtyard
            Vector3 targetPos = currentTarget.transform.position;
            Vector3 fc = GameManager.FortressCenter;
            Vector3 offset = targetPos - fc;
            float targetDistFromCenter = new Vector2(offset.x, offset.z).magnitude;
            if (targetDistFromCenter > 3.5f)
            {
                // Enemy is outside courtyard - move to the courtyard edge toward them
                Vector3 dir = offset.normalized;
                targetPos = fc + dir * 3f;
                targetPos.y = 0;
            }
            agent.isStopped = false;
            agent.SetDestination(targetPos);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    protected virtual void Attack()
    {
        if (MutatorManager.IsActive("pacifist_run")) return;
        if (data == null || currentTarget == null) return;
        float dailyAtkSpd = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderAttackSpeedMultiplier : 1f;
        attackCooldown = (1f / data.attackRate) / dailyAtkSpd;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderDamageMultiplier : 1f;
        int scaledDmg = Mathf.RoundToInt(data.damage * dailyDmg);
        if (MutatorManager.IsActive("glass_fortress")) scaledDmg = Mathf.RoundToInt(scaledDmg * 1.5f);
        Debug.Log($"[Defender] {data.defenderName} attacking {currentTarget.name} for {scaledDmg} damage at dist={GetDistanceToTarget(currentTarget):F1}");
        currentTarget.TakeDamage(scaledDmg);
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

    // --- Tower system ---

    /// <summary>
    /// Whether this defender type should mount tower tops. Override to false for Engineers.
    /// </summary>
    protected virtual bool ShouldUseTower()
    {
        return true;
    }

    /// <summary>
    /// Try to claim and walk to a tower position.
    /// </summary>
    private void SeekTowerPosition()
    {
        towerSeekTimer -= Time.deltaTime;
        if (towerSeekTimer > 0) return;
        towerSeekTimer = TOWER_SEEK_INTERVAL;

        if (TowerPositionManager.Instance == null) return;

        // Already claimed a tower, walk to it
        if (assignedTower != null)
        {
            if (isWalkingToTower && agent != null && agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    // Arrived at tower base — mount
                    MountTower();
                }
            }
            return;
        }

        // Claim a tower
        var tower = TowerPositionManager.Instance.GetBestTower(transform.position);
        if (tower == null) return;

        assignedTower = tower;
        TowerPositionManager.Instance.Claim(tower, this);

        // Walk to courtyard-side base of tower (1.0 unit toward FortressCenter from tower XZ)
        Vector3 towerXZ = new Vector3(tower.WorldPos.x, 0, tower.WorldPos.z);
        Vector3 toCenter = (GameManager.FortressCenter - towerXZ).normalized;
        Vector3 basePos = towerXZ + toCenter * 1.0f;

        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, 2f, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                isWalkingToTower = true;
                Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} walking to tower base at {hit.position}");
            }
        }
    }

    /// <summary>
    /// When on a tower and target is out of range, check if a better tower exists
    /// closer to the nearest enemy. Rate-limited to avoid thrashing.
    /// </summary>
    private void ReassessTower()
    {
        towerReassessTimer -= Time.deltaTime;
        if (towerReassessTimer > 0) return;
        towerReassessTimer = TOWER_REASSESS_INTERVAL;

        if (TowerPositionManager.Instance == null || assignedTower == null) return;

        // Find nearest enemy distance from current tower
        float currentNearestDist = float.MaxValue;
        Vector2 currentXZ = new Vector2(assignedTower.WorldPos.x, assignedTower.WorldPos.z);
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float d = Vector2.Distance(currentXZ,
                new Vector2(enemy.transform.position.x, enemy.transform.position.z));
            if (d < currentNearestDist) currentNearestDist = d;
        }

        float range = data != null ? data.range : 5f;
        if (currentNearestDist <= range) return; // Can reach nearest enemy — stay put

        // Temporarily release so GetBestTower considers all towers equally
        var savedTower = assignedTower;
        TowerPositionManager.Instance.Release(assignedTower);

        // Use fortress center so no tower gets a proximity bias
        var betterTower = TowerPositionManager.Instance.GetBestTower(GameManager.FortressCenter);

        if (betterTower != null && betterTower != savedTower)
        {
            // Check nearest enemy distance from the candidate tower
            float betterNearestDist = float.MaxValue;
            Vector2 betterXZ = new Vector2(betterTower.WorldPos.x, betterTower.WorldPos.z);
            foreach (var enemy in Enemy.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                float d = Vector2.Distance(betterXZ,
                    new Vector2(enemy.transform.position.x, enemy.transform.position.z));
                if (d < betterNearestDist) betterNearestDist = d;
            }

            if (betterNearestDist < currentNearestDist)
            {
                Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} switching tower: {savedTower.WorldPos} → {betterTower.WorldPos} (nearest enemy: {currentNearestDist:F1} → {betterNearestDist:F1})");
                assignedTower = null;
                isOnTower = false;
                isWalkingToTower = false;

                // Re-enable agent for walking
                if (agent != null)
                {
                    agent.enabled = true;
                    Vector3 groundPos = new Vector3(transform.position.x, 0, transform.position.z);
                    if (NavMesh.SamplePosition(groundPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                }

                // Claim the better tower and walk to it
                assignedTower = betterTower;
                TowerPositionManager.Instance.Claim(betterTower, this);

                Vector3 towerXZ = new Vector3(betterTower.WorldPos.x, 0, betterTower.WorldPos.z);
                Vector3 toCenter = (GameManager.FortressCenter - towerXZ).normalized;
                Vector3 basePos = towerXZ + toCenter * 1.0f;

                if (agent != null && agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(basePos, out NavMeshHit navHit, 2f, agent.areaMask))
                    {
                        agent.isStopped = false;
                        agent.SetDestination(navHit.position);
                        isWalkingToTower = true;
                    }
                }
                return;
            }
        }

        // No better option — re-claim current tower
        TowerPositionManager.Instance.Claim(savedTower, this);
        assignedTower = savedTower;
    }

    /// <summary>
    /// Disable NavMeshAgent and teleport to tower top.
    /// </summary>
    protected void MountTower()
    {
        if (assignedTower == null) return;

        // Safety: if someone else already occupies this tower, release and find another
        if (assignedTower.Occupant != null && assignedTower.Occupant != this)
        {
            Debug.LogWarning($"[Defender] {(data != null ? data.defenderName : name)} cannot mount tower at {assignedTower.WorldPos} — already occupied by {assignedTower.Occupant.name}. Finding another.");
            if (TowerPositionManager.Instance != null)
                TowerPositionManager.Instance.Release(assignedTower);
            assignedTower = null;
            isWalkingToTower = false;
            towerSeekTimer = 0; // Immediately seek a different tower
            return;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        transform.position = assignedTower.WorldPos;
        isOnTower = true;
        isWalkingToTower = false;

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} mounted tower at {assignedTower.WorldPos}");
    }

    /// <summary>
    /// Release tower, find ground, re-enable agent.
    /// </summary>
    protected void DismountTower()
    {
        if (assignedTower == null) return;

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} dismounting tower at {assignedTower.WorldPos}");

        Vector3 towerXZ = new Vector3(assignedTower.WorldPos.x, 0, assignedTower.WorldPos.z);
        Vector3 toCenter = (GameManager.FortressCenter - towerXZ).normalized;
        Vector3 groundPos = towerXZ + toCenter * 1.0f;
        groundPos.y = 0;

        TowerPositionManager.Instance.Release(assignedTower);
        assignedTower = null;
        isOnTower = false;
        isWalkingToTower = false;

        if (agent != null)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(groundPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    /// <summary>
    /// Called by TowerPositionManager after rebuilding tower list (domain reload).
    /// Re-links this defender's assignedTower to the new TowerPosition object.
    /// </summary>
    public void ReassignTower(TowerPositionManager.TowerPosition newTower)
    {
        assignedTower = newTower;
    }

    /// <summary>
    /// Called by TowerPositionManager when all contributing walls of a tower are destroyed.
    /// </summary>
    public void ForceDescend()
    {
        if (!isOnTower) return;
        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} force descending — tower walls destroyed!");
        DismountTower();
    }

    // --- Guard system (dynamic, based on engineer exposure) ---

    /// <summary>
    /// Find an available guard for an engineer. Prefers pikemen, then crossbowmen.
    /// Called by Engineer.EvaluateGuardNeed().
    /// </summary>
    public static Defender FindAndAssignGuard(Engineer engineer)
    {
        var allDefenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        NavMeshPath testPath = new NavMeshPath();

        // First pass: find an available pikeman that can reach the engineer
        foreach (var d in allDefenders)
        {
            if (d.IsDead || d.isGuarding || d is Engineer) continue;
            if (d is Pikeman)
            {
                // Verify the guard can actually path to the engineer
                if (d.agent != null && d.agent.isOnNavMesh)
                {
                    d.agent.CalculatePath(engineer.transform.position, testPath);
                    if (testPath.status != NavMeshPathStatus.PathComplete)
                    {
                        Debug.Log($"[Defender] {d.name} can't reach engineer (path {testPath.status}) — skipping");
                        continue;
                    }
                }
                d.StartGuarding(engineer);
                return d;
            }
        }
        // Second pass: find an available crossbowman that can reach the engineer
        foreach (var d in allDefenders)
        {
            if (d.IsDead || d.isGuarding || d is Engineer) continue;
            if (d is Crossbowman)
            {
                if (d.agent != null && d.agent.isOnNavMesh)
                {
                    d.agent.CalculatePath(engineer.transform.position, testPath);
                    if (testPath.status != NavMeshPathStatus.PathComplete)
                    {
                        Debug.Log($"[Defender] {d.name} can't reach engineer (path {testPath.status}) — skipping");
                        continue;
                    }
                }
                d.StartGuarding(engineer);
                return d;
            }
        }

        return null;
    }

    private void StartGuarding(Engineer engineer)
    {
        guardTarget = engineer;
        isGuarding = true;

        // Dismount tower if on one
        if (isOnTower)
        {
            Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} dismounting tower to guard {engineer.name}");
            DismountTower();
        }
        else if (isWalkingToTower && assignedTower != null)
        {
            Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} cancelling tower walk to guard {engineer.name}");
            if (TowerPositionManager.Instance != null)
                TowerPositionManager.Instance.Release(assignedTower);
            assignedTower = null;
            isWalkingToTower = false;
        }

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} starting guard duty for {engineer.name} at {transform.position}");
    }

    /// <summary>
    /// Called by Engineer when it returns to a safe area.
    /// </summary>
    public void ReleaseFromGuardDuty()
    {
        if (!isGuarding) return;

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} released from guard duty at {transform.position}. Will seek tower immediately.");
        guardTarget = null;
        isGuarding = false;
        hasGuardStandPos = false;

        // Immediately seek a tower now that guard duty is over
        towerSeekTimer = 0;
    }

    /// <summary>
    /// Follow the guarded engineer, staying within guardFollowDist.
    /// Finds a stand position with clear line-of-sight to the engineer
    /// so the guard is never separated by a wall.
    /// </summary>
    private Vector3 guardStandPos;
    private bool hasGuardStandPos;
    private Vector3 lastGuardSearchOrigin;
    private const float GUARD_RECOMPUTE_DIST = 1.5f; // Recompute stand pos when engineer moves this far
    private const int GUARD_SEARCH_ANGLES = 12;       // 12 directions (30° apart)

    private void FollowGuardTarget()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (guardTarget == null || guardTarget.IsDead)
        {
            isGuarding = false;
            guardTarget = null;
            guardStuckTimer = 0;
            hasGuardStandPos = false;
            return;
        }

        Vector3 engPos = guardTarget.transform.position;

        // Recompute stand position when engineer moves significantly or we don't have one
        if (!hasGuardStandPos || Vector3.Distance(engPos, lastGuardSearchOrigin) > GUARD_RECOMPUTE_DIST)
        {
            FindGuardStandPosition(engPos);
        }

        float dist = Vector3.Distance(transform.position, guardStandPos);
        bool insideWall = IsInsideWallGeometry(transform.position);

        if (dist > 0.5f || insideWall)
        {
            agent.isStopped = false;
            agent.SetDestination(guardStandPos);

            if (dist < guardLastDist - 0.1f)
            {
                guardStuckTimer = 0;
            }
            else
            {
                guardStuckTimer += Time.deltaTime;
            }
            guardLastDist = dist;

            if (guardStuckTimer > GUARD_STUCK_TIMEOUT)
            {
                Debug.LogWarning($"[Defender] {(data != null ? data.defenderName : name)} can't reach engineer (stuck for {GUARD_STUCK_TIMEOUT}s, path={agent.pathStatus}) — releasing guard duty");
                isGuarding = false;
                guardTarget = null;
                guardStuckTimer = 0;
                guardLastDist = float.MaxValue;
                hasGuardStandPos = false;
                return;
            }
        }
        else
        {
            agent.isStopped = true;
            guardStuckTimer = 0;
            guardLastDist = dist;
        }
    }

    /// <summary>
    /// Find a guard stand position between the nearest enemy and the engineer.
    /// The guard interposes itself to protect the engineer.
    /// Falls back to courtyard-side positioning if no enemies are visible.
    /// All positions must have clear LOS to the engineer.
    /// </summary>
    private void FindGuardStandPosition(Vector3 engPos)
    {
        lastGuardSearchOrigin = engPos;

        // Find the nearest enemy to the engineer (the threat to guard against)
        Vector3 threatDir = Vector3.zero;
        float nearestEnemyDist = float.MaxValue;
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float d = Vector3.Distance(engPos, enemy.transform.position);
            if (d < nearestEnemyDist)
            {
                nearestEnemyDist = d;
                threatDir = (enemy.transform.position - engPos);
                threatDir.y = 0;
            }
        }

        // If no enemy found, default to outward-from-courtyard direction
        if (threatDir.sqrMagnitude < 0.01f)
        {
            threatDir = engPos - GameManager.FortressCenter;
            threatDir.y = 0;
            if (threatDir.sqrMagnitude < 0.01f) threatDir = Vector3.forward;
        }

        // Start scanning from the threat direction (interpose between enemy and engineer)
        float startAngle = Mathf.Atan2(threatDir.z, threatDir.x);
        float[] distances = { guardFollowDist * 0.5f, guardFollowDist, guardFollowDist * 1.5f };

        foreach (float dist in distances)
        {
            for (int step = 0; step < GUARD_SEARCH_ANGLES; step++)
            {
                // Alternate left/right from threat direction: 0, +1, -1, +2, -2...
                int offset = (step + 1) / 2 * ((step % 2 == 0) ? 1 : -1);
                float angle = startAngle + offset * 2f * Mathf.PI / GUARD_SEARCH_ANGLES;
                Vector3 candidate = engPos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, agent.areaMask))
                    continue;

                Vector3 pos = navHit.position;

                if (IsInsideWallGeometry(pos))
                    continue;

                // Must have clear LOS to engineer
                if (HasWallBetween(pos, engPos))
                    continue;

                guardStandPos = pos;
                hasGuardStandPos = true;
                Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} guard interposing at {pos} (between threat and engineer at {engPos})");
                return;
            }
        }

        // No valid position found — stand at engineer's position as last resort
        guardStandPos = engPos;
        hasGuardStandPos = true;
        Debug.LogWarning($"[Defender] {(data != null ? data.defenderName : name)} no valid guard position found — standing at engineer pos {engPos}");
    }

    /// <summary>
    /// Check if a position is inside wall or tower geometry.
    /// Uses both physics overlap and manual bounds checks (catches the narrow
    /// NavMesh strip between tower colliders that physics alone misses).
    /// </summary>
    private static readonly Collider[] wallOverlapBuffer = new Collider[8];

    private bool IsInsideWallGeometry(Vector3 pos)
    {
        // Physics check for enabled colliders
        int count = Physics.OverlapSphereNonAlloc(pos + Vector3.up * 0.5f, 0.3f, wallOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (wallOverlapBuffer[i].GetComponentInParent<Wall>() != null)
                return true;
        }

        // Manual bounds check for all walls (catches narrow gap between tower colliders)
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
    private static bool IsInsideWallBounds(Vector3 worldPos, Wall wall)
    {
        Vector3 local = wall.transform.InverseTransformPoint(worldPos);
        float padding = 0.3f;

        // Wall body: local size (1, 2, 0.5) -> half extents (0.5, -, 0.25)
        float halfX = 0.5f + padding;
        float halfZ = 0.25f + padding;
        if (Mathf.Abs(local.x) < halfX && Mathf.Abs(local.z) < halfZ)
            return true;

        // Towers at +/-TOWER_OFFSET in local X, radius = OCT_APOTHEM
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

    // --- Recall ---

    /// <summary>
    /// Called by RecallManager. Dismounts tower, cancels guard duty, and navigates to courtyard.
    /// </summary>
    public void Recall()
    {
        if (IsDead) return;

        // Dismount tower if on one
        if (isOnTower)
            DismountTower();

        // Cancel tower walk
        if (isWalkingToTower && assignedTower != null)
        {
            if (TowerPositionManager.Instance != null)
                TowerPositionManager.Instance.Release(assignedTower);
            assignedTower = null;
            isWalkingToTower = false;
        }

        // Release guard duty
        if (isGuarding)
        {
            guardTarget = null;
            isGuarding = false;
            hasGuardStandPos = false;
        }

        // Navigate to courtyard
        if (agent != null && agent.isOnNavMesh)
        {
            Vector3 fc = GameManager.FortressCenter;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2f, 3f);
            Vector3 courtyard = fc + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            agent.isStopped = false;
            agent.SetDestination(courtyard);
        }

        // Suppress tower seeking so they actually walk to courtyard first
        towerSeekTimer = 5f;

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} recalled to courtyard");
    }

    // --- Damage & Death ---

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        currentHP -= damage;
        FloatingDamageNumber.Spawn(transform.position, damage, false);
        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} took {damage} damage, HP={currentHP}");
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        // Release tower before destroying
        if (assignedTower != null && TowerPositionManager.Instance != null)
        {
            TowerPositionManager.Instance.Release(assignedTower);
            assignedTower = null;
            isOnTower = false;
        }

        // Notify guarded engineer so it can request a new guard
        if (isGuarding && guardTarget != null && !guardTarget.IsDead)
        {
            guardTarget.OnGuardDied();
        }
        isGuarding = false;
        guardTarget = null;

        Debug.Log($"[Defender] {(data != null ? data.defenderName : name)} died at {transform.position}");

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
