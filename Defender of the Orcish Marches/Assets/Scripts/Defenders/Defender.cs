using UnityEngine;
using UnityEngine.AI;

public class Defender : MonoBehaviour
{
    [SerializeField] protected DefenderData data;
    [SerializeField] protected float attackCooldown;

    protected Enemy currentTarget;
    protected NavMeshAgent agent;

    public DefenderData Data => data;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // Exclude gate area (area 3) so defenders cannot path through gates
            agent.areaMask = NavMesh.AllAreas & ~(1 << 3);
        }
    }

    public virtual void Initialize(DefenderData defenderData)
    {
        data = defenderData;
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = data.bodyColor;
        }

        if (agent != null)
        {
            agent.stoppingDistance = 0.3f;
        }
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        attackCooldown -= Time.deltaTime;
        FindTarget();

        if (currentTarget != null)
        {
            // Move toward target if out of range, but stay inside the courtyard
            if (agent != null && agent.isOnNavMesh)
            {
                float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
                float range = data != null ? data.range : 5f;
                if (dist > range)
                {
                    // Path toward enemy but clamp destination inside the courtyard
                    Vector3 targetPos = currentTarget.transform.position;
                    float targetDistFromCenter = new Vector2(targetPos.x, targetPos.z).magnitude;
                    if (targetDistFromCenter > 3.5f)
                    {
                        // Enemy is outside courtyard - move to the courtyard edge toward them
                        Vector3 dir = (targetPos - Vector3.zero).normalized;
                        targetPos = dir * 3f;
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

            if (attackCooldown <= 0)
            {
                Attack();
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

    protected virtual void Attack()
    {
        if (data == null || currentTarget == null) return;
        attackCooldown = 1f / data.attackRate;
        currentTarget.TakeDamage(data.damage);
    }
}
