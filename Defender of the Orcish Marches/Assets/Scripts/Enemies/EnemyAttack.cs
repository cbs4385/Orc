using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private Enemy enemy;
    private EnemyMovement movement;
    private float attackCooldown;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        if (enemy.IsDead) return;
        attackCooldown -= Time.deltaTime;

        if (movement.HasReachedTarget && movement.CurrentTarget != null && attackCooldown <= 0)
        {
            Attack(movement.CurrentTarget);
        }
    }

    private void Attack(Transform target)
    {
        attackCooldown = 1f / enemy.Data.attackRate;

        switch (enemy.Data.enemyType)
        {
            case EnemyType.Melee:
            case EnemyType.WallBreaker:
                MeleeAttack(target);
                break;
            case EnemyType.Ranged:
            case EnemyType.Artillery:
                RangedAttack(target);
                break;
            case EnemyType.Suicide:
                SuicideAttack(target);
                break;
        }
    }

    private void MeleeAttack(Transform target)
    {
        // Attack wall
        var wall = target.GetComponent<Wall>();
        if (wall != null)
        {
            wall.TakeDamage(enemy.Data.damage);
            return;
        }

        // Attack defender
        var defender = target.GetComponent<Defender>();
        if (defender != null)
        {
            defender.TakeDamage(enemy.Data.damage);
            return;
        }

        // Attack menial
        var menial = target.GetComponent<Menial>();
        if (menial != null)
        {
            menial.TakeDamage(enemy.Data.damage);
            return;
        }

        // Attack refugee
        var refugee = target.GetComponent<Refugee>();
        if (refugee != null)
        {
            refugee.TakeDamage(enemy.Data.damage);
        }
    }

    private void RangedAttack(Transform target)
    {
        if (SoundManager.Instance != null)
        {
            if (enemy.Data.enemyType == EnemyType.Artillery)
                SoundManager.Instance.PlayGoblinCannonFire(transform.position);
            else
                SoundManager.Instance.PlayOrcArcherFire(transform.position);
        }

        if (enemy.Data.projectilePrefab != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;
            var proj = Instantiate(enemy.Data.projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.LookRotation(dir));
            var projectile = proj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(dir, 10f, enemy.Data.damage, enemy.Data.attackRange * 1.5f);
            }
        }
        else
        {
            // Fallback: instant damage if no projectile prefab
            MeleeAttack(target);
        }
    }

    private void SuicideAttack(Transform target)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayGoblinBomberExplode(transform.position);

        // Deal massive damage and die
        var wall = target.GetComponent<Wall>();
        if (wall != null) wall.TakeDamage(enemy.Data.damage);

        var defender = target.GetComponent<Defender>();
        if (defender != null) defender.TakeDamage(enemy.Data.damage);

        var menial = target.GetComponent<Menial>();
        if (menial != null) menial.TakeDamage(enemy.Data.damage);

        var refugee = target.GetComponent<Refugee>();
        if (refugee != null) refugee.TakeDamage(enemy.Data.damage);

        // Self-destruct
        enemy.TakeDamage(9999);
    }
}
