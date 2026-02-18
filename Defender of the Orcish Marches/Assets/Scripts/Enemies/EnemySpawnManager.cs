using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnEnabled = true;
    [SerializeField] private float mapRadius = 40f;
    [SerializeField] private float initialSpawnInterval = 3f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float difficultyRampTime = 300f; // 5 minutes to max difficulty

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject treasurePickupPrefab;

    [Header("Enemy Data")]
    [SerializeField] private EnemyData orcGruntData;
    [SerializeField] private EnemyData bowOrcData;
    [SerializeField] private EnemyData trollData;
    [SerializeField] private EnemyData suicideGoblinData;
    [SerializeField] private EnemyData goblinCannoneerData;

    public GameObject TreasurePrefab => treasurePickupPrefab;

    private float spawnTimer;
    private List<Enemy> activeEnemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[EnemySpawnManager] Instance set in Awake.");
    }

    private void OnEnable()
    {
        // Re-register singleton after domain reload (static fields reset but MonoBehaviour survives)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[EnemySpawnManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDestroy()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        if (!spawnEnabled) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnEnemy();
            float difficulty = Mathf.Clamp01(GameManager.Instance.GameTime / difficultyRampTime);
            spawnTimer = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, difficulty);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPos = GetRandomEdgePosition();
        EnemyData data = ChooseEnemyType();
        if (data == null) return;

        GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(data);
            activeEnemies.Add(enemy);
        }
    }

    private EnemyData ChooseEnemyType()
    {
        float gameTime = GameManager.Instance != null ? GameManager.Instance.GameTime : 0;

        // Progressive enemy type unlocks based on game time
        float roll = Random.value;

        // After 4 min: cannoneers (5%)
        if (gameTime > 240f && roll < 0.05f && goblinCannoneerData != null)
            return goblinCannoneerData;

        // After 3 min: suicide goblins (10%)
        if (gameTime > 180f && roll < 0.15f && suicideGoblinData != null)
            return suicideGoblinData;

        // After 2 min: trolls (15%)
        if (gameTime > 120f && roll < 0.25f && trollData != null)
            return trollData;

        // After 1 min: bow orcs (30%)
        if (gameTime > 60f && roll < 0.45f && bowOrcData != null)
            return bowOrcData;

        // Default: orc grunts
        return orcGruntData;
    }

    private Vector3 GetRandomEdgePosition()
    {
        int side = Random.Range(0, 4);
        float offset = Random.Range(-mapRadius, mapRadius);
        switch (side)
        {
            case 0: return new Vector3(-mapRadius, 0, offset);
            case 1: return new Vector3(mapRadius, 0, offset);
            case 2: return new Vector3(offset, 0, -mapRadius);
            case 3: return new Vector3(offset, 0, mapRadius);
            default: return new Vector3(-mapRadius, 0, offset);
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public int GetActiveEnemyCount() => activeEnemies.Count;
}
