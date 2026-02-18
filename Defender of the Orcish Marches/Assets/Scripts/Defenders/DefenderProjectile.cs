using UnityEngine;

public class DefenderProjectile : MonoBehaviour
{
    private Transform target;
    private Vector3 lastTargetPos;
    private float speed;
    private int damage;
    private float aoeRadius;
    private Vector3 startPosition;
    private float maxRange;
    private bool initialized;

    public void Initialize(Transform targetTransform, float spd, int dmg, float range, float aoe = 0f)
    {
        target = targetTransform;
        lastTargetPos = target != null ? target.position : transform.position + transform.forward * 10f;
        speed = spd;
        damage = dmg;
        maxRange = range;
        aoeRadius = aoe;
        startPosition = transform.position;
        initialized = true;
        Debug.Log($"[DefenderProjectile] Fired at {(target != null ? target.name : "null")}, dmg={damage}, aoe={aoeRadius}");
    }

    private void Update()
    {
        if (!initialized) return;

        // Track living target, fall back to last known position
        Vector3 targetPos = (target != null && !target.GetComponentInParent<Enemy>().IsDead)
            ? target.position : lastTargetPos;

        if (target != null) lastTargetPos = targetPos;

        Vector3 direction = (targetPos - transform.position);
        direction.y = 0;

        // Arrived at target position
        if (direction.sqrMagnitude < 0.3f)
        {
            HitTarget();
            return;
        }

        direction.Normalize();
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Max range safety
        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void HitTarget()
    {
        if (aoeRadius > 0)
        {
            // AoE damage
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= aoeRadius)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"[DefenderProjectile] AoE hit {enemy.name} for {damage} at dist={dist:F1}");
                }
            }
        }
        else
        {
            // Single target
            if (target != null)
            {
                var enemy = target.GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"[DefenderProjectile] Hit {enemy.name} for {damage}");
                }
            }
        }
        Destroy(gameObject);
    }
}
