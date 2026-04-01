using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float maxRange;
    private Vector3 startPosition;
    private bool initialized;

    public void Initialize(Vector3 dir, float spd, int dmg, float range)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        maxRange = range;
        startPosition = transform.position;
        initialized = true;
        Debug.Log($"[EnemyProjectile] Initialized: damage={dmg}, speed={spd}, range={range}, dir={dir.normalized}, pos={startPosition}");
    }

    private void Update()
    {
        if (!initialized) return;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Debug.Log($"[EnemyProjectile] Expired at max range ({maxRange}) at {transform.position}");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var wall = other.GetComponent<Wall>();
        if (wall != null)
        {
            Debug.Log($"[EnemyProjectile] Hit wall {wall.name} for {damage} damage at {transform.position}");
            wall.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var menial = other.GetComponent<Menial>();
        if (menial != null)
        {
            Debug.Log($"[EnemyProjectile] Hit menial for {damage} damage at {transform.position}");
            menial.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var refugee = other.GetComponent<Refugee>();
        if (refugee != null)
        {
            Debug.Log($"[EnemyProjectile] Hit refugee for {damage} damage at {transform.position}");
            refugee.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var veg = other.GetComponentInParent<Vegetation>();
        if (veg != null && !veg.IsDead)
        {
            veg.TakeDamage(damage);
            Debug.Log($"[EnemyProjectile] Hit {veg.Type} at {transform.position}");
            Destroy(gameObject);
        }
    }
}
