using UnityEngine;

public class Wizard : Defender
{
    [SerializeField] private GameObject fireMissilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float aoeRadius = 3f;

    protected override void Attack()
    {
        if (data == null || currentTarget == null) return;
        float dailyAtkSpd = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderAttackSpeedMultiplier : 1f;
        float relicAtkSpd = RelicManager.Instance != null ? RelicManager.Instance.GetDefenderAttackSpeedMultiplier() : 1f;
        attackCooldown = (1f / data.attackRate) / (dailyAtkSpd * relicAtkSpd);
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.DefenderDamageMultiplier : 1f;
        float commanderDmg = CommanderManager.GetDefenderDamageMultiplier();
        float relicDmg = RelicManager.Instance != null ? RelicManager.Instance.GetDefenderDamageMultiplier() : 1f;
        int scaledDmg = Mathf.RoundToInt(data.damage * dailyDmg * commanderDmg * relicDmg);

        if (fireMissilePrefab == null)
        {
            // Fallback to instant AoE damage if no prefab
            Vector3 center = currentTarget.transform.position;
            foreach (var enemy in Enemy.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                float dist = Vector3.Distance(center, enemy.transform.position);
                if (dist <= aoeRadius)
                {
                    enemy.TakeDamage(scaledDmg);
                }
            }
            return;
        }

        float spawnY = isOnTower ? transform.position.y + 0.8f : 0.8f;
        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
        Vector3 dir = (currentTarget.transform.position - spawnPos).normalized;
        var go = Instantiate(fireMissilePrefab, spawnPos, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<DefenderProjectile>();
        if (proj != null)
        {
            proj.Initialize(currentTarget.transform, projectileSpeed, scaledDmg, data.range + 5f, aoeRadius);
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWizardFire(transform.position);
        Debug.Log($"[Wizard] Fired missile at {currentTarget.name}, damage={scaledDmg}, aoe={aoeRadius}, dist={Vector3.Distance(transform.position, currentTarget.transform.position):F1}");
    }
}
