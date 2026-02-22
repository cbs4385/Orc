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

    // Gravity (Nightmare FPS mode)
    private bool useGravity;
    private float gravityStrength = 15f;
    private Vector3 velocity;
    private float totalDistanceTraveled;

    public void Initialize(Vector3 dir, float spd, int dmg, float range, bool burst = false, float burstRad = 0f, bool gravity = false)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        maxRange = range;
        startPosition = transform.position;
        burstDamage = burst;
        burstRadius = burstRad;
        useGravity = gravity;
        velocity = direction * speed;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        float step;
        Vector3 oldPos = transform.position;

        if (useGravity)
        {
            // Apply gravity to velocity
            velocity += Vector3.down * gravityStrength * Time.deltaTime;
            direction = velocity.normalized;
            step = velocity.magnitude * Time.deltaTime;

            // Orient projectile along velocity
            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(velocity);
        }
        else
        {
            step = speed * Time.deltaTime;
        }

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

            var veg = hit.collider.GetComponentInParent<Vegetation>();
            if (veg != null && !veg.IsDead)
            {
                transform.position = hit.point;
                veg.TakeDamage(damage);
                Debug.Log($"[BallistaProjectile] Hit {veg.Type} at {hit.point}");

                // Burst damage still splashes to nearby enemies
                if (burstDamage && burstRadius > 0)
                {
                    BurstDamageVFX.Spawn(transform.position, burstRadius);
                    var allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                    foreach (var nearby in allEnemies)
                    {
                        if (nearby.IsDead) continue;
                        float dist = Vector3.Distance(transform.position, nearby.transform.position);
                        if (dist <= burstRadius)
                        {
                            nearby.TakeDamage(damage / 2);
                        }
                    }
                }
                Destroy(gameObject);
                return;
            }
        }

        transform.position = oldPos + direction * step;

        if (useGravity)
        {
            totalDistanceTraveled += step;

            // Destroy if hit ground
            if (transform.position.y < 0.05f)
            {
                Debug.Log($"[BallistaProjectile] Hit ground at {transform.position}");
                if (burstDamage && burstRadius > 0)
                {
                    BurstDamageVFX.Spawn(transform.position, burstRadius);
                    var allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                    foreach (var nearby in allEnemies)
                    {
                        if (nearby.IsDead) continue;
                        float dist = Vector3.Distance(transform.position, nearby.transform.position);
                        if (dist <= burstRadius)
                        {
                            nearby.TakeDamage(damage / 2);
                        }
                    }
                }
                Destroy(gameObject);
                return;
            }

            // Range limit based on total distance traveled
            if (totalDistanceTraveled >= maxRange)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            if (Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            HandleHit(enemy);
            return;
        }

        var veg = other.GetComponentInParent<Vegetation>();
        if (veg != null && !veg.IsDead)
        {
            veg.TakeDamage(damage);
            Debug.Log($"[BallistaProjectile] Trigger hit {veg.Type} at {transform.position}");

            if (burstDamage && burstRadius > 0)
            {
                BurstDamageVFX.Spawn(transform.position, burstRadius);
                var allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                foreach (var nearby in allEnemies)
                {
                    if (nearby.IsDead) continue;
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
