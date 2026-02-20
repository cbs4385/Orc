using UnityEngine;

public class BallistaProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float maxRange;
    private Vector3 startPosition;
    private bool initialized;

    // Burst damage
    private bool burstDamage;
    private float burstRadius;

    public void Initialize(Vector3 dir, float spd, int dmg, float range, bool burst = false, float burstRad = 0f)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        maxRange = range;
        startPosition = transform.position;
        burstDamage = burst;
        burstRadius = burstRad;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        float step = speed * Time.deltaTime;
        Vector3 oldPos = transform.position;

        // Raycast along travel path to catch fast-moving hits
        if (Physics.Raycast(oldPos, direction, out RaycastHit hit, step))
        {
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                transform.position = hit.point;
                HandleHit(enemy);
                return;
            }
        }

        transform.position = oldPos + direction * step;

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            HandleHit(enemy);
        }
    }

    private void HandleHit(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        // Burst damage: deal half damage to all enemies in radius
        if (burstDamage && burstRadius > 0)
        {
            BurstDamageVFX.Spawn(transform.position, burstRadius);

            var allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var nearby in allEnemies)
            {
                if (nearby == enemy || nearby.IsDead) continue;
                float dist = Vector3.Distance(transform.position, nearby.transform.position);
                if (dist <= burstRadius)
                {
                    nearby.TakeDamage(damage / 2);
                }
            }
            Debug.Log($"[BallistaProjectile] Burst damage at {transform.position}, radius={burstRadius}");
        }

        Destroy(gameObject);
    }
}
