using UnityEngine;

public class RefugeeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject refugeePrefab;
    [SerializeField] private float mapRadius = 40f;
    [SerializeField] [Range(0f, 1f)] private float powerUpChance = 0.3f;

    private int spawnsThisDay;
    private int targetSpawnsPerDay;
    private float lastSpawnProgress; // PhaseProgress at which last spawn occurred

    private void Start()
    {
        targetSpawnsPerDay = GetTargetSpawnsPerDay();
        spawnsThisDay = 0;
        lastSpawnProgress = 0f;
        Debug.Log($"[RefugeeSpawner] Initialized. target={targetSpawnsPerDay}/day, powerUpChance={powerUpChance}");
    }

    private void OnEnable()
    {
        SubscribeDayStarted();
    }

    private void OnDisable()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
    }

    private bool subscribedToDayStarted;

    private void SubscribeDayStarted()
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
            DayNightCycle.Instance.OnDayStarted += HandleDayStarted;
            subscribedToDayStarted = true;
        }
        else
        {
            subscribedToDayStarted = false;
            Debug.LogWarning("[RefugeeSpawner] DayNightCycle.Instance null in OnEnable — will retry in Update.");
        }
    }

    private void HandleDayStarted()
    {
        spawnsThisDay = 0;
        lastSpawnProgress = 0f;
        Debug.Log($"[RefugeeSpawner] Day started. Target spawns={targetSpawnsPerDay}");
    }

    private int lastKnownDay = -1;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (DayNightCycle.Instance == null) return;

        // Retry subscription if it failed in OnEnable (DayNightCycle wasn't ready)
        if (!subscribedToDayStarted)
            SubscribeDayStarted();

        // Fallback day-change detection: only needed when OnDayStarted subscription failed
        int currentDay = DayNightCycle.Instance.DayNumber;
        if (currentDay != lastKnownDay)
        {
            if (!subscribedToDayStarted)
            {
                // Event handler didn't fire — reset manually
                spawnsThisDay = 0;
                lastSpawnProgress = 0f;
                Debug.Log($"[RefugeeSpawner] Day {currentDay} detected (fallback). Reset spawns. target={targetSpawnsPerDay}");
            }
            lastKnownDay = currentDay;
        }

        if (DayNightCycle.Instance.IsNight) return;
        if (spawnsThisDay >= targetSpawnsPerDay) return;

        // Use PhaseProgress (0..1) from DayNightCycle directly.
        // Center spawns in each time slot: 0.5/N, 1.5/N, 2.5/N, 3.5/N
        // For 4/day: 0.125, 0.375, 0.625, 0.875 (centered in each quarter)
        // For 1/day: 0.5 (mid-day)
        float progress = DayNightCycle.Instance.PhaseProgress;
        float nextSpawnThreshold = (spawnsThisDay + 0.5f) / targetSpawnsPerDay;

        if (progress >= nextSpawnThreshold)
        {
            SpawnRefugee();
            spawnsThisDay++;
            lastSpawnProgress = progress;
            Debug.Log($"[RefugeeSpawner] Spawned at progress={progress:F2}, threshold={nextSpawnThreshold:F2}, spawned={spawnsThisDay}/{targetSpawnsPerDay}");
        }
    }

    /// <summary>Number of refugees to spawn per day based on difficulty.</summary>
    private static int GetTargetSpawnsPerDay()
    {
        switch (GameSettings.CurrentDifficulty)
        {
            case Difficulty.Easy:      return 4;
            case Difficulty.Normal:    return 2;
            case Difficulty.Hard:      return 1;
            case Difficulty.Nightmare: return 1;
            default:                   return 2;
        }
    }

    private void SpawnRefugee()
    {
        if (refugeePrefab == null)
        {
            Debug.LogError("[RefugeeSpawner] refugeePrefab is null! Cannot spawn refugee.");
            return;
        }

        int side = Random.Range(0, 4);
        float offset = Random.Range(-mapRadius * 0.8f, mapRadius * 0.8f);
        Vector3 pos;
        switch (side)
        {
            case 0: pos = new Vector3(-mapRadius, 0, offset); break;
            case 1: pos = new Vector3(mapRadius, 0, offset); break;
            case 2: pos = new Vector3(offset, 0, -mapRadius); break;
            default: pos = new Vector3(offset, 0, mapRadius); break;
        }

        Debug.Log($"[RefugeeSpawner] Spawning refugee at {pos} (side={side})");
        var go = Instantiate(refugeePrefab, pos, Quaternion.identity);
        var refugee = go.GetComponent<Refugee>();

        // Randomly assign a power-up
        if (refugee != null && Random.value < powerUpChance)
        {
            var powerUps = new[] { RefugeePowerUp.DoubleShot, RefugeePowerUp.BurstDamage };
            var chosenPowerUp = powerUps[Random.Range(0, powerUps.Length)];
            refugee.SetPowerUp(chosenPowerUp);
            Debug.Log($"[RefugeeSpawner] Refugee assigned power-up: {chosenPowerUp}");
        }
    }
}
