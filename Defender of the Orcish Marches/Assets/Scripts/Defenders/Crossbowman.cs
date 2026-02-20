using UnityEngine;

public class Crossbowman : Defender
{
    [SerializeField] private GameObject boltPrefab;
    [SerializeField] private float projectileSpeed = 20f;

    protected override void Attack()
    {
        if (data == null || currentTarget == null) return;
        float dailyAtkSpd = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderAttackSpeedMultiplier : 1f;
        attackCooldown = (1f / data.attackRate) / dailyAtkSpd;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderDamageMultiplier : 1f;
        int scaledDmg = Mathf.RoundToInt(data.damage * dailyDmg);

        if (boltPrefab == null)
        {
            // Fallback to instant damage if no prefab
            currentTarget.TakeDamage(scaledDmg);
            return;
        }

        Vector3 spawnPos = new Vector3(transform.position.x, 0.5f, transform.position.z);
        Vector3 dir = (currentTarget.transform.position - spawnPos).normalized;
        var go = Instantiate(boltPrefab, spawnPos, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<DefenderProjectile>();
        if (proj != null)
        {
            proj.Initialize(currentTarget.transform, projectileSpeed, scaledDmg, data.range + 5f);
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayCrossbowFire(transform.position);
        Debug.Log($"[Crossbowman] Fired bolt at {currentTarget.name}");
    }
}
