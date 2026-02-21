using System;
using UnityEngine;

public enum DailyEventCategory { Beneficial, Detrimental, Mixed }

public struct DailyEventInfo
{
    public string name;
    public string description;
    public DailyEventCategory category;
    public float lootValueMultiplier;
    public float defenderDamageMultiplier;
    public float menialSpeedMultiplier;
    public float enemyDamageMultiplier;
    public float spawnRateMultiplier;
    public float enemyHPMultiplier;
    public float enemySpeedMultiplier;
    public float defenderAttackSpeedMultiplier;
}

public class DailyEventManager : MonoBehaviour
{
    public static DailyEventManager Instance { get; private set; }

    // Current multipliers (read by other systems)
    public float LootValueMultiplier { get; private set; } = 1f;
    public float DefenderDamageMultiplier { get; private set; } = 1f;
    public float MenialSpeedMultiplier { get; private set; } = 1f;
    public float EnemyDamageMultiplier { get; private set; } = 1f;
    public float SpawnRateMultiplier { get; private set; } = 1f;
    public float EnemyHPMultiplier { get; private set; } = 1f;
    public float EnemySpeedMultiplier { get; private set; } = 1f;
    public float DefenderAttackSpeedMultiplier { get; private set; } = 1f;

    public string CurrentEventName { get; private set; } = "";
    public string CurrentEventDescription { get; private set; } = "";
    public DailyEventCategory CurrentEventCategory { get; private set; }
    public bool HasActiveEvent { get; private set; }

    public event Action<DailyEventInfo> OnEventChanged;

    private bool dncSubscribed;
    private int lastEventIndex = -1;

    private static readonly DailyEventInfo[] AllEvents = new DailyEventInfo[]
    {
        // Beneficial (4)
        new DailyEventInfo {
            name = "Bountiful Harvest", description = "Loot value +50%",
            category = DailyEventCategory.Beneficial,
            lootValueMultiplier = 1.5f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Inspired Defenders", description = "Defender damage +30%",
            category = DailyEventCategory.Beneficial,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1.3f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Swift Menials", description = "Menial move speed +40%",
            category = DailyEventCategory.Beneficial,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1.4f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Fortified Walls", description = "Enemy damage to walls -30%",
            category = DailyEventCategory.Beneficial,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 0.7f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        // Detrimental (4)
        new DailyEventInfo {
            name = "Orcish War Drums", description = "Enemy spawn rate +40%",
            category = DailyEventCategory.Detrimental,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1.4f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Blood Rage", description = "Enemy HP +40%",
            category = DailyEventCategory.Detrimental,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1.4f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Swiftfoot Scouts", description = "Enemy speed +30%",
            category = DailyEventCategory.Detrimental,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1.3f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Siege Engines", description = "Enemy damage to walls +40%",
            category = DailyEventCategory.Detrimental,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1.4f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        // Mixed (4)
        new DailyEventInfo {
            name = "Merchant Caravan", description = "Loot +60%, spawn rate +30%",
            category = DailyEventCategory.Mixed,
            lootValueMultiplier = 1.6f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1.3f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Berserker's Brew", description = "Defender damage +40%, attack rate -25%",
            category = DailyEventCategory.Mixed,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1.4f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 0.75f
        },
        new DailyEventInfo {
            name = "Fog of War", description = "Enemy speed -25%, defender damage -25%",
            category = DailyEventCategory.Mixed,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 0.75f, menialSpeedMultiplier = 1f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1f,
            enemySpeedMultiplier = 0.75f, defenderAttackSpeedMultiplier = 1f
        },
        new DailyEventInfo {
            name = "Desperate Times", description = "Menial speed +50%, enemy HP +30%",
            category = DailyEventCategory.Mixed,
            lootValueMultiplier = 1f, defenderDamageMultiplier = 1f, menialSpeedMultiplier = 1.5f,
            enemyDamageMultiplier = 1f, spawnRateMultiplier = 1f, enemyHPMultiplier = 1.3f,
            enemySpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[DailyEventManager] Instance set in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[DailyEventManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
    }

    private void TrySubscribe()
    {
        if (dncSubscribed) return;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted += HandleDayStarted;
            dncSubscribed = true;
        }
    }

    private void Update()
    {
        if (!dncSubscribed) TrySubscribe();

        // Fallback: if DayNightCycle already started before we subscribed, pick now
        if (!HasActiveEvent && DayNightCycle.Instance != null)
        {
            int dayNumber = DayNightCycle.Instance.DayNumber;
            if (dayNumber <= 1)
            {
                // Day 1 has no event — mark as handled so we don't keep checking
                ResetMultipliers();
                HasActiveEvent = true;
                Debug.Log("[DailyEventManager] Day 1 fallback — no event (intro day).");
            }
            else
            {
                PickRandomEvent();
            }
        }
    }

    private void HandleDayStarted()
    {
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        if (dayNumber <= 1)
        {
            // Day 1 has no event — keep all multipliers at 1.0 for a consistent intro wave
            ResetMultipliers();
            Debug.Log("[DailyEventManager] Day 1 — no event (intro day).");
            return;
        }
        PickRandomEvent();
    }

    private void ResetMultipliers()
    {
        LootValueMultiplier = 1f;
        DefenderDamageMultiplier = 1f;
        MenialSpeedMultiplier = 1f;
        EnemyDamageMultiplier = 1f;
        SpawnRateMultiplier = 1f;
        EnemyHPMultiplier = 1f;
        EnemySpeedMultiplier = 1f;
        DefenderAttackSpeedMultiplier = 1f;
        CurrentEventName = "";
        CurrentEventDescription = "";
        HasActiveEvent = true;
    }

    private void PickRandomEvent()
    {
        // Avoid repeating the same event two days in a row
        int index;
        if (AllEvents.Length > 1)
        {
            do { index = UnityEngine.Random.Range(0, AllEvents.Length); }
            while (index == lastEventIndex);
        }
        else
        {
            index = 0;
        }
        lastEventIndex = index;
        var evt = AllEvents[index];

        // Apply multipliers
        LootValueMultiplier = evt.lootValueMultiplier;
        DefenderDamageMultiplier = evt.defenderDamageMultiplier;
        MenialSpeedMultiplier = evt.menialSpeedMultiplier;
        EnemyDamageMultiplier = evt.enemyDamageMultiplier;
        SpawnRateMultiplier = evt.spawnRateMultiplier;
        EnemyHPMultiplier = evt.enemyHPMultiplier;
        EnemySpeedMultiplier = evt.enemySpeedMultiplier;
        DefenderAttackSpeedMultiplier = evt.defenderAttackSpeedMultiplier;

        CurrentEventName = evt.name;
        CurrentEventDescription = evt.description;
        CurrentEventCategory = evt.category;
        HasActiveEvent = true;

        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        Debug.Log($"[DailyEventManager] Day {dayNumber} Event: {evt.name} ({evt.category}) - {evt.description}");

        // Update existing menial speeds
        RefreshMenialSpeeds();

        OnEventChanged?.Invoke(evt);
    }

    private void RefreshMenialSpeeds()
    {
        foreach (var menial in FindObjectsByType<Menial>(FindObjectsSortMode.None))
        {
            menial.RefreshSpeed();
        }
    }
}
