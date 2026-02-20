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

    [Header("Stat Scaling")]
    [Tooltip("Enemy HP increases by this fraction of base per day. 0.1 = +10% per day.")]
    [SerializeField] private float hpScalingPerDay = 0.1f;
    [Tooltip("Enemy damage increases by this fraction of base per day. 0.05 = +5% per day.")]
    [SerializeField] private float damageScalingPerDay = 0.05f;

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

    // Enemies that retreated at nightfall carry over to next day's spawn
    private List<EnemyData> remnantEnemies = new List<EnemyData>();

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
        {
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
            DayNightCycle.Instance.OnNightStarted -= HandleNightStarted;
        }
    }

    private void HandleDayStarted()
    {
        dawnGraceTimer = 2f;
        bossSpawnedThisDay = false;
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        float halfSpread = 5f + 10f * (dayNumber - 1);
        Debug.Log($"[EnemySpawnManager] Dawn grace period started (2s). Day {dayNumber}, spawn arc halfSpread={halfSpread:F0}deg. Remnants={remnantEnemies.Count}");

        // Spawn remnants from previous night — all at once, spread along the spawn arc
        if (remnantEnemies.Count > 0)
        {
            SpawnRemnants(dayNumber);
        }

        // Spawn boss from due west when arc reaches half-circle (day 10+)
        if (halfSpread >= 90f && !bossSpawnedThisDay && orcWarBossData != null)
        {
            SpawnBoss();
        }
    }

    private void SpawnRemnants(int dayNumber)
    {
        var positions = GetEvenlySpacedEdgePositions(remnantEnemies.Count, dayNumber);
        Debug.Log($"[EnemySpawnManager] Spawning {remnantEnemies.Count} remnant enemies from last night.");

        for (int i = 0; i < remnantEnemies.Count; i++)
        {
            if (enemyPrefab == null) continue;
            EnemyData data = remnantEnemies[i];
            if (data == null) continue;

            GameObject go = Instantiate(enemyPrefab, positions[i], Quaternion.identity);
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(data);
                ApplyDayScaling(enemy);
                activeEnemies.Add(enemy);
                Debug.Log($"[EnemySpawnManager] Remnant spawned: {data.enemyName} at {positions[i]}");
            }
        }
        remnantEnemies.Clear();
    }

    private List<Vector3> GetEvenlySpacedEdgePositions(int count, int dayNumber)
    {
        var positions = new List<Vector3>();
        float centerAngle = 180f;
        float halfSpread = Mathf.Min(5f + 10f * (dayNumber - 1), 180f);
        float totalArc = 2f * halfSpread;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = centerAngle - halfSpread + totalArc * (i + 0.5f) / count;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angleRad) * mapRadius, 0, Mathf.Sin(angleRad) * mapRadius);
            positions.Add(pos);
        }
        return positions;
    }

    private void HandleNightStarted()
    {
        int retreatingCount = 0;
        // Take a snapshot — the list will be modified as enemies reach the edge
        var snapshot = new List<Enemy>(activeEnemies);
        foreach (var enemy in snapshot)
        {
            if (enemy == null || enemy.IsDead) continue;
            var movement = enemy.GetComponent<EnemyMovement>();
            if (movement == null) continue;

            movement.OnReachedRetreatEdge = HandleEnemyRetreated;
            movement.Retreat(mapRadius);
            retreatingCount++;
        }
        Debug.Log($"[EnemySpawnManager] Night fell — {retreatingCount} enemies retreating west.");
    }

    private void HandleEnemyRetreated(Enemy enemy)
    {
        if (enemy != null && enemy.Data != null)
        {
            remnantEnemies.Add(enemy.Data);
            Debug.Log($"[EnemySpawnManager] {enemy.Data.enemyName} retreated off map. Remnants queued: {remnantEnemies.Count}");
        }
        activeEnemies.Remove(enemy);
        if (enemy != null) Destroy(enemy.gameObject);
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
            ApplyDayScaling(enemy);
            activeEnemies.Add(enemy);
            bossSpawnedThisDay = true;
            Debug.Log($"[EnemySpawnManager] WAR BOSS spawned at {spawnPos}! HP={enemy.CurrentHP}, Damage={enemy.ScaledDamage}");
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
            DayNightCycle.Instance.OnNightStarted += HandleNightStarted;
            dncSubscribed = true;
        }

        // Don't spawn at night
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight) return;

        // Noon cutoff: all enemies for the day finish spawning by midpoint of day phase
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.PhaseProgress > 0.5f) return;

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
            spawnTimer = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, difficulty) * GameSettings.GetSpawnRateMultiplier();
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
            ApplyDayScaling(enemy);
            activeEnemies.Add(enemy);
        }
    }

    private void ApplyDayScaling(Enemy enemy)
    {
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        if (dayNumber <= 1) return;
        float hpMult = 1f + hpScalingPerDay * (dayNumber - 1);
        float dmgMult = 1f + damageScalingPerDay * (dayNumber - 1);
        enemy.ApplyDayScaling(hpMult, dmgMult);
    }

    private EnemyData ChooseEnemyType()
    {
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

    /// <summary>
    /// Returns a preview of the wave for the given day number.
    /// </summary>
    public WavePreviewData GetWavePreview(int dayNumber)
    {
        var data = new WavePreviewData();
        data.dayNumber = dayNumber;

        // Enemy types available this day
        var types = new List<string>();
        var newTypes = new List<string>();

        types.Add(orcGruntData != null ? orcGruntData.enemyName : "Orc Grunt");
        if (dayNumber >= 2 && bowOrcData != null)
        {
            types.Add(bowOrcData.enemyName);
            if (dayNumber == 2) newTypes.Add(bowOrcData.enemyName);
        }
        if (dayNumber >= 3 && trollData != null)
        {
            types.Add(trollData.enemyName);
            if (dayNumber == 3) newTypes.Add(trollData.enemyName);
        }
        if (dayNumber >= 4 && suicideGoblinData != null)
        {
            types.Add(suicideGoblinData.enemyName);
            if (dayNumber == 4) newTypes.Add(suicideGoblinData.enemyName);
        }
        if (dayNumber >= 5 && goblinCannoneerData != null)
        {
            types.Add(goblinCannoneerData.enemyName);
            if (dayNumber == 5) newTypes.Add(goblinCannoneerData.enemyName);
        }
        data.enemyTypes = types.ToArray();
        data.newEnemyTypes = newTypes.ToArray();

        // Spawn direction
        float halfSpread = Mathf.Min(5f + 10f * (dayNumber - 1), 180f);
        data.spawnDirection = DescribeSpawnArc(halfSpread);

        // Boss
        data.hasBoss = halfSpread >= 90f && orcWarBossData != null;
        if (data.hasBoss)
        {
            data.bossName = orcWarBossData.enemyName;
        }

        // Stat scaling
        data.hpMultiplier = 1f + hpScalingPerDay * (dayNumber - 1);
        data.damageMultiplier = 1f + damageScalingPerDay * (dayNumber - 1);

        return data;
    }

    private string DescribeSpawnArc(float halfSpread)
    {
        if (halfSpread <= 15f) return "From the West";
        if (halfSpread <= 45f) return "From the West (wide arc)";
        if (halfSpread <= 75f) return "From Northwest to Southwest";
        if (halfSpread <= 105f) return "From North to South";
        if (halfSpread <= 150f) return "Nearly surrounded";
        return "From all directions";
    }
}

public struct WavePreviewData
{
    public int dayNumber;
    public string[] enemyTypes;
    public string[] newEnemyTypes;
    public string spawnDirection;
    public bool hasBoss;
    public string bossName;
    public float hpMultiplier;
    public float damageMultiplier;
}
