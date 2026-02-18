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

        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            // Burst damage: deal half damage to all enemies in radius
            if (burstDamage && burstRadius > 0)
            {
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
            }

            Destroy(gameObject);
        }
    }
}
