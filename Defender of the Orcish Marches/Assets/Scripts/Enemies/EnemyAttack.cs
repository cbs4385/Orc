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

        if (enemy == null)
            Debug.LogError($"[EnemyAttack] Missing Enemy component on {gameObject.name}!");
        if (movement == null)
            Debug.LogError($"[EnemyAttack] Missing EnemyMovement component on {gameObject.name}!");

        Debug.Log($"[EnemyAttack] Initialized on {gameObject.name}");
    }

    private void Update()
    {
        if (enemy.IsDead) return;
        if (movement.IsRetreating) return;
        attackCooldown -= Time.deltaTime;

        if (movement.HasReachedTarget && movement.CurrentTarget != null && attackCooldown <= 0)
        {
            // Verify actual distance — HasReachedTarget can be true on partial paths
            // where the agent can't reach the destination but remainingDistance is small
            float distToTarget = Vector3.Distance(transform.position, movement.CurrentTarget.position);
            if (distToTarget > enemy.Data.attackRange * 1.5f)
            {
                Debug.LogWarning($"[EnemyAttack] {enemy.Data.enemyName} HasReachedTarget=true but target {movement.CurrentTarget.name} is {distToTarget:F1} away (attackRange={enemy.Data.attackRange}). Skipping attack.");
                return;
            }
            Attack(movement.CurrentTarget);
        }
    }

    private void Attack(Transform target)
    {
        attackCooldown = 1f / enemy.Data.attackRate;

        Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} ({enemy.Data.enemyType}) attacking {target.name}, scaledDamage={enemy.ScaledDamage}");

        // Trigger attack animation
        enemy.TriggerAttackAnimation();

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
            Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} melee hit wall {target.name} for {enemy.ScaledDamage} damage (wallHP={wall.CurrentHP}/{wall.MaxHP})");
            wall.TakeDamage(enemy.ScaledDamage);
            return;
        }

        // Attack defender
        var defender = target.GetComponent<Defender>();
        if (defender != null)
        {
            Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} melee hit defender {target.name} for {enemy.ScaledDamage} damage");
            defender.TakeDamage(enemy.ScaledDamage);
            return;
        }

        // Attack menial
        var menial = target.GetComponent<Menial>();
        if (menial != null)
        {
            Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} melee hit menial {target.name} for {enemy.ScaledDamage} damage");
            menial.TakeDamage(enemy.ScaledDamage);
            return;
        }

        // Attack refugee
        var refugee = target.GetComponent<Refugee>();
        if (refugee != null)
        {
            Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} melee hit refugee {target.name} for {enemy.ScaledDamage} damage");
            refugee.TakeDamage(enemy.ScaledDamage);
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
            float projectileSpeed = 10f;
            Vector3 aimPos = GetLeadPosition(target, projectileSpeed);
            Vector3 dir = (aimPos - transform.position);
            dir.y = 0;
            dir.Normalize();
            Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} firing projectile at {target.name}, dir={dir}, range={enemy.Data.attackRange}, leadAim={aimPos}");
            var proj = Instantiate(enemy.Data.projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.LookRotation(dir));
            var projectile = proj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(dir, projectileSpeed, enemy.ScaledDamage, enemy.Data.attackRange * 1.5f);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemyAttack] {enemy.Data.enemyName} has no projectilePrefab! Falling back to melee.");
            // Fallback: instant damage if no projectile prefab
            MeleeAttack(target);
        }
    }

    /// <summary>
    /// Calculate the intercept point for a moving target.
    /// Solves the quadratic |T + V*t - P| = speed*t for the smallest positive t.
    /// Falls back to the target's current position if no valid solution exists.
    /// </summary>
    private Vector3 GetLeadPosition(Transform target, float projectileSpeed)
    {
        // Get target velocity from NavMeshAgent
        Vector3 targetVel = Vector3.zero;
        var targetAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (targetAgent != null && targetAgent.enabled)
            targetVel = targetAgent.velocity;

        // If target isn't moving, just aim at current position
        if (targetVel.sqrMagnitude < 0.01f)
            return target.position;

        // Solve in XZ plane
        Vector3 D = target.position - transform.position;
        D.y = 0;
        targetVel.y = 0;

        float a = targetVel.sqrMagnitude - projectileSpeed * projectileSpeed;
        float b = 2f * Vector3.Dot(D, targetVel);
        float c = D.sqrMagnitude;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0)
            return target.position;

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtDisc) / (2f * a);
        float t2 = (-b + sqrtDisc) / (2f * a);

        // Pick smallest positive time
        float t = -1f;
        if (t1 > 0.01f && t2 > 0.01f) t = Mathf.Min(t1, t2);
        else if (t1 > 0.01f) t = t1;
        else if (t2 > 0.01f) t = t2;

        if (t < 0)
            return target.position;

        Vector3 leadPos = target.position + targetVel * t;
        leadPos.y = target.position.y;
        return leadPos;
    }

    private void SuicideAttack(Transform target)
    {
        Debug.Log($"[EnemyAttack] {enemy.Data.enemyName} suicide attack on {target.name} at {transform.position}, damage={enemy.ScaledDamage}");

        if (SoundManager.Instance != null) SoundManager.Instance.PlayGoblinBomberExplode(transform.position);

        // Deal massive damage and die
        var wall = target.GetComponent<Wall>();
        if (wall != null)
        {
            Debug.Log($"[EnemyAttack] Suicide explosion hit wall {target.name} for {enemy.ScaledDamage}");
            wall.TakeDamage(enemy.ScaledDamage);
        }

        var defender = target.GetComponent<Defender>();
        if (defender != null)
        {
            Debug.Log($"[EnemyAttack] Suicide explosion hit defender {target.name} for {enemy.ScaledDamage}");
            defender.TakeDamage(enemy.ScaledDamage);
        }

        var menial = target.GetComponent<Menial>();
        if (menial != null)
        {
            Debug.Log($"[EnemyAttack] Suicide explosion hit menial {target.name} for {enemy.ScaledDamage}");
            menial.TakeDamage(enemy.ScaledDamage);
        }

        var refugee = target.GetComponent<Refugee>();
        if (refugee != null)
        {
            Debug.Log($"[EnemyAttack] Suicide explosion hit refugee {target.name} for {enemy.ScaledDamage}");
            refugee.TakeDamage(enemy.ScaledDamage);
        }

        // Self-destruct
        enemy.TakeDamage(9999);
    }
}
