using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public int CurrentHP { get; private set; }
    public int ScaledDamage { get; private set; }
    public bool IsDead { get; private set; }

    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemySpawned;

    // Static registry of living enemies — avoids FindObjectsByType every frame
    private static readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    public static IReadOnlyCollection<Enemy> ActiveEnemies => activeEnemies;

    private Renderer[] bodyRenderers;
    private Color[] originalColors;
    private float damageFlashTimer;
    private Animator animator;

    private void OnEnable()
    {
        activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        float dailyHP = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyHPMultiplier : 1f;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyDamageMultiplier : 1f;
        CurrentHP = Mathf.RoundToInt(data.maxHP * GameSettings.GetEnemyHPMultiplier() * dailyHP);
        ScaledDamage = Mathf.RoundToInt(data.damage * dailyDmg);

        // Swap model if the EnemyData specifies a custom model
        if (data.modelPrefab != null)
        {
            var existingModel = transform.Find("Model");
            if (existingModel != null)
            {
                Debug.Log($"[Enemy] Destroying default Model for {data.enemyName}, replacing with custom model");
                Destroy(existingModel.gameObject);
            }

            var newModel = Instantiate(data.modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = newModel.AddComponent<Animator>();
            if (data.animatorController != null)
                animator.runtimeAnimatorController = data.animatorController;
            animator.applyRootMotion = false;

            Debug.Log($"[Enemy] Custom model loaded for {data.enemyName}");
        }
        else
        {
            // Cache animator from existing model
            animator = GetComponentInChildren<Animator>();
        }

        // Cache all renderers and their original colors
        bodyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (animator != null)
            {
                // Model has its own materials — store them as-is
                originalColors[i] = bodyRenderers[i].material.color;
            }
            else
            {
                // Cube placeholder — apply bodyColor from data
                bodyRenderers[i].material.color = data.bodyColor;
                originalColors[i] = data.bodyColor;
            }
        }

        transform.localScale = data.bodyScale;
        Debug.Log($"[Enemy] Initialized: {data.enemyName}, HP={data.maxHP}, type={data.enemyType}");
        OnEnemySpawned?.Invoke(this);
    }

    public void ApplyDayScaling(float hpMultiplier, float damageMultiplier)
    {
        float dailyHP = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyHPMultiplier : 1f;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyDamageMultiplier : 1f;
        CurrentHP = Mathf.RoundToInt(data.maxHP * hpMultiplier * dailyHP);
        ScaledDamage = Mathf.RoundToInt(data.damage * damageMultiplier * dailyDmg);
        Debug.Log($"[Enemy] {data.enemyName} day-scaled: HP={CurrentHP} (base {data.maxHP}), Damage={ScaledDamage} (base {data.damage})");
    }

    private void Update()
    {
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0)
            {
                RestoreColors();
            }
        }
    }

    private void RestoreColors()
    {
        if (bodyRenderers == null) return;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = originalColors[i];
        }
    }

    private void FlashWhite()
    {
        if (bodyRenderers == null) return;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = Color.white;
        }
        damageFlashTimer = 0.1f;
    }

    public void TriggerAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        CurrentHP -= damage;
        Debug.Log($"[Enemy] {data?.enemyName} took {damage} damage at {transform.position}. HP={CurrentHP}");

        FloatingDamageNumber.Spawn(transform.position, damage, true);
        FlashWhite();

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        // Play death sound based on enemy type
        if (SoundManager.Instance != null && data != null)
        {
            if (data.enemyType == EnemyType.WallBreaker)
                SoundManager.Instance.PlayTrollHit(transform.position);
            else
                SoundManager.Instance.PlayOrcHit(transform.position);
        }

        // Spawn loot
        if (data.maxLootDrops > 0)
        {
            SpawnLoot();
        }
        else
        {
            Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}, no loot configured.");
        }

        OnEnemyDied?.Invoke(this);

        // Play die animation if available, then destroy after delay
        if (animator != null)
        {
            animator.SetTrigger("Die");
            // Disable movement and attack while dying
            var movement = GetComponent<EnemyMovement>();
            if (movement != null) movement.enabled = false;
            var attack = GetComponent<EnemyAttack>();
            if (attack != null) attack.enabled = false;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        // Die animation is 30 frames at 24fps ≈ 1.25 seconds
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        var spawner = EnemySpawnManager.Instance;
        if (spawner == null)
        {
            Debug.LogError("[Enemy] SpawnLoot failed: EnemySpawnManager.Instance is null!");
            return;
        }
        if (spawner.TreasurePrefab == null)
        {
            Debug.LogError("[Enemy] SpawnLoot failed: TreasurePrefab is null!");
            return;
        }

        int dropCount = UnityEngine.Random.Range(data.minLootDrops, data.maxLootDrops + 1);
        int totalValue = 0;

        for (int i = 0; i < dropCount; i++)
        {
            // Scatter loot around the death position
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, 0, offset.y);

            var loot = Instantiate(spawner.TreasurePrefab, spawnPos, Quaternion.identity);
            var pickup = loot.GetComponent<TreasurePickup>();
            if (pickup != null)
            {
                int lootValue = UnityEngine.Random.Range(data.minLootValue, data.maxLootValue + 1);
                pickup.Initialize(lootValue);
                totalValue += lootValue;
            }
            else
            {
                Debug.LogError("[Enemy] SpawnLoot: TreasurePickup component missing from prefab!");
            }
        }

        Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}. Dropped {dropCount} loot (total value={totalValue})");
    }
}
