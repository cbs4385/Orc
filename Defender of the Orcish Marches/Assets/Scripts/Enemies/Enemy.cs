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
        Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}. treasureDrop={data.treasureDrop}");

        // Spawn loot
        if (data.treasureDrop > 0)
        {
            SpawnLoot();
        }
        else
        {
            Debug.LogWarning($"[Enemy] {data.enemyName} has treasureDrop=0, no loot spawned.");
        }

        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        // Find TreasurePickup prefab from EnemySpawnManager
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

        var loot = Instantiate(spawner.TreasurePrefab, transform.position, Quaternion.identity);
        Debug.Log($"[Enemy] Loot spawned at {transform.position}, value={data.treasureDrop}, obj={loot.name}");
        var pickup = loot.GetComponent<TreasurePickup>();
        if (pickup != null)
        {
            pickup.Initialize(data.treasureDrop);
        }
        else
        {
            Debug.LogError("[Enemy] SpawnLoot: TreasurePickup component missing from prefab!");
        }
    }
}
