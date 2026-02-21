using UnityEngine;
using UnityEngine.AI;

public class Pikeman : Defender
{
    private Enemy lastLoggedTarget;

    protected override void Attack()
    {
        base.Attack();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayPikemanAttack(transform.position);
        Debug.Log($"[Pikeman] Attacked {currentTarget?.name} at dist={Vector3.Distance(transform.position, currentTarget.transform.position):F1}");
    }

    // Pikeman finds any nearby enemy (wide search) and walks to the wall to engage
    protected override void FindTarget()
    {
        // Search wide — the pikeman should spot enemies at the walls from anywhere in the courtyard
        float searchRange = 15f;
        currentTarget = null;
        float bestDist = searchRange;

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            // Pikeman engages melee-range threats: grunts, trolls, suicide goblins
            if (enemy.Data.enemyType != EnemyType.Melee &&
                enemy.Data.enemyType != EnemyType.WallBreaker &&
                enemy.Data.enemyType != EnemyType.Suicide) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                currentTarget = enemy;
            }
        }

        if (currentTarget != null && currentTarget != lastLoggedTarget)
        {
            lastLoggedTarget = currentTarget;
            float d = Vector3.Distance(transform.position, currentTarget.transform.position);
            float r = data != null ? data.range : 3f;
            Debug.Log($"[Pikeman] Targeting {currentTarget.name} at dist={d:F1}, range={r:F1}, pos={transform.position}, enemyPos={currentTarget.transform.position}");
        }
    }

    // Override movement so the pikeman walks to the inner wall face
    // nearest the enemy, rather than staying clamped at courtyard center
    protected override void MoveTowardTarget()
    {
        if (agent == null || !agent.isOnNavMesh || currentTarget == null) return;

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        float range = data != null ? data.range : 3f;

        if (dist > range)
        {
            Vector3 enemyPos = currentTarget.transform.position;
            Vector3 fc = GameManager.FortressCenter;
            Vector3 enemyOffset = enemyPos - fc;
            float enemyDist = new Vector2(enemyOffset.x, enemyOffset.z).magnitude;

            Vector3 destination;
            if (enemyDist > 4f)
            {
                // Enemy is outside the walls — walk toward the inner wall face
                // Walls are at ~4.5 radius, inner face at ~4.2
                Vector3 dir = new Vector3(enemyOffset.x, 0, enemyOffset.z).normalized;
                destination = fc + dir * 4f;
                destination.y = 0;
            }
            else
            {
                // Enemy is inside — walk directly to them
                destination = enemyPos;
            }

            // Snap to valid NavMesh position to avoid pathing failures
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, 2f, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            agent.isStopped = true;
        }
    }
}
