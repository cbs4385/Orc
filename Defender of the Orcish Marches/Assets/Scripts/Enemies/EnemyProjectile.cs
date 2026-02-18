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
        var wall = other.GetComponent<Wall>();
        if (wall != null)
        {
            wall.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var menial = other.GetComponent<Menial>();
        if (menial != null)
        {
            menial.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var refugee = other.GetComponent<Refugee>();
        if (refugee != null)
        {
            refugee.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
