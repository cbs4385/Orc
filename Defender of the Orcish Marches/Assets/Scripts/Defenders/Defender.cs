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

    public DefenderData Data => data;
    public bool IsDead { get; private set; }

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

        Debug.Log($"[Defender] {data.defenderName} initialized, HP={currentHP}");
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        attackCooldown -= Time.deltaTime;
        FindTarget();

        if (currentTarget != null)
        {
            MoveTowardTarget();

            if (attackCooldown <= 0)
            {
                float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
                float range = data != null ? data.range : 5f;
                if (dist <= range)
                {
                    Attack();
                    if (animator != null)
                        animator.SetTrigger("Attack");
                }
            }
        }
        else
        {
            // No target - stop moving
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }
    }

    protected virtual void FindTarget()
    {
        // Search wide enough to find enemies at walls from anywhere in the courtyard
        float searchRange = 15f;
        currentTarget = null;
        float bestDist = searchRange;

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                currentTarget = enemy;
            }
        }
    }

    protected virtual void MoveTowardTarget()
    {
        if (agent == null || !agent.isOnNavMesh || currentTarget == null) return;

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
        if (data == null || currentTarget == null) return;
        float dailyAtkSpd = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderAttackSpeedMultiplier : 1f;
        attackCooldown = (1f / data.attackRate) / dailyAtkSpd;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderDamageMultiplier : 1f;
        int scaledDmg = Mathf.RoundToInt(data.damage * dailyDmg);
        Debug.Log($"[Defender] {data.defenderName} attacking {currentTarget.name} for {scaledDmg} damage at dist={Vector3.Distance(transform.position, currentTarget.transform.position):F1}");
        currentTarget.TakeDamage(scaledDmg);
    }

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
