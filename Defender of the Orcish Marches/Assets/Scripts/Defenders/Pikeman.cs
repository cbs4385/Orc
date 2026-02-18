using UnityEngine;

public class Pikeman : Defender
{
    // Pikeman attacks melee enemies near the wall it's stationed on
    protected override void FindTarget()
    {
        float searchRange = data != null ? data.range : 3f;
        currentTarget = null;
        float bestDist = searchRange;

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            if (enemy.Data.enemyType != EnemyType.Melee &&
                enemy.Data.enemyType != EnemyType.WallBreaker &&
                enemy.Data.enemyType != EnemyType.Suicide) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                currentTarget = enemy;
            }
        }
    }
}
