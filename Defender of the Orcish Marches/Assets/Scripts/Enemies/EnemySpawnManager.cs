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

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject treasurePickupPrefab;

    [Header("Enemy Data")]
    [SerializeField] private EnemyData orcGruntData;
    [SerializeField] private EnemyData bowOrcData;
    [SerializeField] private EnemyData trollData;
    [SerializeField] private EnemyData suicideGoblinData;
    [SerializeField] private EnemyData goblinCannoneerData;
    [SerializeField] private EnemyData orcWarBossData;

    public GameObject TreasurePrefab => treasurePickupPrefab;

    private float spawnTimer;
    private float dawnGraceTimer;
    private bool bossSpawnedThisDay;
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
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
    }

    private void HandleDayStarted()
    {
        dawnGraceTimer = 2f;
        bossSpawnedThisDay = false;
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        float halfSpread = 5f + 10f * (dayNumber - 1);
        Debug.Log($"[EnemySpawnManager] Dawn grace period started (2s). Day {dayNumber}, spawn arc halfSpread={halfSpread:F0}deg.");

        // Spawn boss from due west when arc reaches half-circle (day 10+)
        if (halfSpread >= 90f && !bossSpawnedThisDay && orcWarBossData != null)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        if (enemyPrefab == null || orcWarBossData == null) return;

        // Spawn from due west (180 degrees = -X)
        Vector3 spawnPos = new Vector3(-mapRadius, 0, 0);
        GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(orcWarBossData);
            activeEnemies.Add(enemy);
            bossSpawnedThisDay = true;
            Debug.Log($"[EnemySpawnManager] WAR BOSS spawned at {spawnPos}! HP={orcWarBossData.maxHP}, Damage={orcWarBossData.damage}");
        }
    }

    private bool dncSubscribed;

    private void Update()
    {
        if (!spawnEnabled) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Late-subscribe to DayNightCycle events (it may init after us)
        if (!dncSubscribed && DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted += HandleDayStarted;
            dncSubscribed = true;
        }

        // Don't spawn at night
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight) return;

        // Dawn grace period
        if (dawnGraceTimer > 0f)
        {
            dawnGraceTimer -= Time.deltaTime;
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnEnemy();
            int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
            float difficulty = Mathf.Clamp01((dayNumber - 1) / 10f);
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
        // DEBUG: force troll spawns for testing
        if (trollData != null) return trollData;

        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;

        // Progressive enemy type unlocks based on day number
        float roll = Random.value;

        // Day 5+: cannoneers (5%)
        if (dayNumber >= 5 && roll < 0.05f && goblinCannoneerData != null)
            return goblinCannoneerData;

        // Day 4+: suicide goblins (10%)
        if (dayNumber >= 4 && roll < 0.15f && suicideGoblinData != null)
            return suicideGoblinData;

        // Day 3+: trolls (15%)
        if (dayNumber >= 3 && roll < 0.25f && trollData != null)
            return trollData;

        // Day 2+: bow orcs (30%)
        if (dayNumber >= 2 && roll < 0.45f && bowOrcData != null)
            return bowOrcData;

        // Default: orc grunts
        return orcGruntData;
    }

    private Vector3 GetRandomEdgePosition()
    {
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;

        // West = 180 degrees in standard math coords (0deg = +X)
        float centerAngle = 180f;
        // Arc starts narrow (+/-5deg) and widens by 10deg per day, capped at full circle
        float halfSpread = Mathf.Min(5f + 10f * (dayNumber - 1), 180f);

        float angleDeg = Random.Range(centerAngle - halfSpread, centerAngle + halfSpread);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 pos = new Vector3(Mathf.Cos(angleRad) * mapRadius, 0, Mathf.Sin(angleRad) * mapRadius);
        return pos;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public int GetActiveEnemyCount() => activeEnemies.Count;
}
