using UnityEngine;

public class Wizard : Defender
{
    [SerializeField] private GameObject fireMissilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float aoeRadius = 3f;

    protected override void Attack()
    {
        if (data == null || currentTarget == null) return;
        attackCooldown = 1f / data.attackRate;

        if (fireMissilePrefab == null)
        {
            // Fallback to instant AoE damage if no prefab
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
            return;
        }

        Vector3 spawnPos = new Vector3(transform.position.x, 0.8f, transform.position.z);
        Vector3 dir = (currentTarget.transform.position - spawnPos).normalized;
        var go = Instantiate(fireMissilePrefab, spawnPos, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<DefenderProjectile>();
        if (proj != null)
        {
            proj.Initialize(currentTarget.transform, projectileSpeed, data.damage, data.range + 5f, aoeRadius);
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWizardFire(transform.position);
        Debug.Log($"[Wizard] Fired missile at {currentTarget.name}, aoe={aoeRadius}");
    }
}
