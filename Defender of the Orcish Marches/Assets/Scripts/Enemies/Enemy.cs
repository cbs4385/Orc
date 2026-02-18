using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemySpawned;

    private Renderer bodyRenderer;
    private float damageFlashTimer;
    private Color originalColor;

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        CurrentHP = data.maxHP;

        // Apply visual
        bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = data.bodyColor;
            originalColor = data.bodyColor;
        }

        transform.localScale = data.bodyScale;
        OnEnemySpawned?.Invoke(this);
    }

    private void Update()
    {
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0 && bodyRenderer != null)
            {
                bodyRenderer.material.color = originalColor;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        CurrentHP -= damage;

        // Flash white on damage
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = Color.white;
            damageFlashTimer = 0.1f;
        }

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
