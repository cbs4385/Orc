using UnityEngine;

public class Wizard : Defender
{
    [SerializeField] private float aoeRadius = 3f;

    protected override void Attack()
    {
        if (data == null || currentTarget == null) return;
        attackCooldown = 1f / data.attackRate;

        // AoE damage around the target
        Vector3 center = currentTarget.transform.position;
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            float dist = Vector3.Distance(center, enemy.transform.position);
            if (dist <= aoeRadius)
            {
                enemy.TakeDamage(data.damage);
            }
        }
    }
}
