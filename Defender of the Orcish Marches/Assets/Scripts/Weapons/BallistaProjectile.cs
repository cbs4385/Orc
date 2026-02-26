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

    // SphereCast radius for hit detection — prevents misses at steep angles
    private const float HIT_RADIUS = 0.3f;

    // Linger after impact
    private bool stopped;


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

        // Add trajectory trail
        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 1.5f;
        trail.startWidth = 0.08f;
        trail.endWidth = 0.02f;
        trail.numCapVertices = 2;
        trail.receiveShadows = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            var mat = new Material(shader);
            trail.material = mat;
        }
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    private void StopAndLinger(bool hitEnemy = false)
    {
        if (stopped) return;
        stopped = true;
        initialized = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        BoltImpactVFX.Spawn(transform.position, hitEnemy);
        Debug.Log($"[BallistaProjectile] Stopped at {transform.position}, hitEnemy={hitEnemy}, lingering 1s");
        Destroy(gameObject, 1f);
    }

    private void Update()
    {
        if (!initialized || stopped) return;

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

        // SphereCast along travel path — wider than a thin ray to catch enemies at steep angles
        if (Physics.SphereCast(oldPos, HIT_RADIUS, direction, out RaycastHit hit, step))
        {
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                transform.position = hit.point;
                HandleHit(enemy);
                return;
            }

            // In FPS/gravity mode, projectiles pass through friendly walls (fired from inside fortress)
            var wall = hit.collider.GetComponentInParent<Wall>();
            if (wall != null && !useGravity)
            {
                transform.position = hit.point;
                Debug.Log($"[BallistaProjectile] Blocked by wall {wall.name} at {hit.point}");
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
                StopAndLinger();
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
                StopAndLinger();
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
                StopAndLinger();
                return;
            }

            // Range limit based on total distance traveled
            if (totalDistanceTraveled >= maxRange)
            {
                StopAndLinger();
                return;
            }
        }
        else
        {
            if (Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                StopAndLinger();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stopped) return;
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            HandleHit(enemy);
            return;
        }

        // In FPS/gravity mode, projectiles pass through friendly walls
        var wall = other.GetComponentInParent<Wall>();
        if (wall != null && !useGravity)
        {
            Debug.Log($"[BallistaProjectile] Trigger blocked by wall {wall.name} at {transform.position}");
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
            StopAndLinger();
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
            StopAndLinger();
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

        StopAndLinger(hitEnemy: true);
    }
}
