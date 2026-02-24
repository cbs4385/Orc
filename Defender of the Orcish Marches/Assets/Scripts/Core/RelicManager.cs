using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages per-run relic collection. Relics stack multiplicatively.
/// Offers choices during night phase transitions.
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    private List<string> collectedRelicIds = new List<string>();
    private bool offerPending;

    /// <summary>Fired when relics are offered (UI should display choices).</summary>
    public event Action<RelicDef[]> OnRelicsOffered;

    /// <summary>Fired when a relic is chosen and applied.</summary>
    public event Action<RelicDef> OnRelicCollected;

    /// <summary>Number of relics collected this run.</summary>
    public int CollectedCount => collectedRelicIds.Count;

    /// <summary>IDs of all collected relics this run.</summary>
    public IReadOnlyList<string> CollectedRelicIds => collectedRelicIds;

    /// <summary>Whether there is a pending relic offer waiting for player choice.</summary>
    public bool IsOfferPending => offerPending;

    private RelicDef[] currentOffering;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[RelicManager] Instance set in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[RelicManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnNightStarted -= HandleNightStarted;
        if (Instance == this) Instance = null;
    }

    private bool dncSubscribed;

    private void Update()
    {
        if (!dncSubscribed && DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnNightStarted += HandleNightStarted;
            dncSubscribed = true;
        }
    }

    private void HandleNightStarted()
    {
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        // Offer relics starting after day 1 (first night)
        if (dayNumber >= 1)
        {
            OfferRelicChoices();
        }
    }

    /// <summary>
    /// Generate 3 random relic choices from the pool, avoiding already-collected relics
    /// when possible. Fires OnRelicsOffered for UI.
    /// </summary>
    public void OfferRelicChoices()
    {
        var pool = new List<RelicDef>();
        foreach (var relic in RelicDefs.All)
        {
            pool.Add(relic);
        }

        // Shuffle pool
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }

        // Pick up to 3
        int count = Mathf.Min(3, pool.Count);
        currentOffering = new RelicDef[count];
        for (int i = 0; i < count; i++)
        {
            currentOffering[i] = pool[i];
        }

        offerPending = true;
        Debug.Log($"[RelicManager] Offering {count} relics: {currentOffering[0].name}, {(count > 1 ? currentOffering[1].name : "")}, {(count > 2 ? currentOffering[2].name : "")}");
        OnRelicsOffered?.Invoke(currentOffering);
    }

    /// <summary>
    /// Player selects a relic from the current offering by index (0-2).
    /// </summary>
    public void SelectRelic(int choiceIndex)
    {
        if (currentOffering == null || choiceIndex < 0 || choiceIndex >= currentOffering.Length)
        {
            Debug.LogWarning($"[RelicManager] Invalid relic choice index: {choiceIndex}");
            return;
        }

        var chosen = currentOffering[choiceIndex];
        collectedRelicIds.Add(chosen.id);
        offerPending = false;
        currentOffering = null;

        // Apply instant effects
        if (chosen.instantGold > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddTreasure(chosen.instantGold);
            Debug.Log($"[RelicManager] Instant gold: +{chosen.instantGold}");
        }
        if (chosen.instantMenials > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMenial(chosen.instantMenials);
            Debug.Log($"[RelicManager] Instant menials: +{chosen.instantMenials}");
        }

        Debug.Log($"[RelicManager] Relic collected: {chosen.name} ({chosen.id}). Total relics: {collectedRelicIds.Count}");
        OnRelicCollected?.Invoke(chosen);
    }

    /// <summary>
    /// Player skips the relic offering (takes nothing).
    /// </summary>
    public void SkipOffer()
    {
        offerPending = false;
        currentOffering = null;
        Debug.Log("[RelicManager] Relic offer skipped.");
    }

    // --- Cumulative multiplier queries (all collected relics stack multiplicatively) ---

    public float GetDefenderDamageMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.defenderDamageMultiplier;
        }
        return mult;
    }

    public float GetWallDamageTakenMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.wallDamageTakenMultiplier;
        }
        return mult;
    }

    public float GetLootValueMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.lootValueMultiplier;
        }
        return mult;
    }

    public float GetSpawnCountMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.spawnCountMultiplier;
        }
        return mult;
    }

    public float GetEnemyHPMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.enemyHPMultiplier;
        }
        return mult;
    }

    public float GetEnemySpeedMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.enemySpeedMultiplier;
        }
        return mult;
    }

    public float GetBallistaDamageMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.ballistaDamageMultiplier;
        }
        return mult;
    }

    public float GetBallistaFireRateMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.ballistaFireRateMultiplier;
        }
        return mult;
    }

    public float GetMenialSpeedMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.menialSpeedMultiplier;
        }
        return mult;
    }

    public float GetDefenderAttackSpeedMultiplier()
    {
        float mult = 1f;
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null) mult *= def.Value.defenderAttackSpeedMultiplier;
        }
        return mult;
    }

    /// <summary>Returns a display-friendly list of collected relic names.</summary>
    public string GetCollectedNamesDisplay()
    {
        if (collectedRelicIds.Count == 0) return "None";
        var sb = new System.Text.StringBuilder();
        foreach (var id in collectedRelicIds)
        {
            var def = RelicDefs.GetById(id);
            if (def != null)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(def.Value.name);
            }
        }
        return sb.ToString();
    }

    /// <summary>Log collected relics state.</summary>
    public void LogRelicState()
    {
        Debug.Log($"[RelicManager] Collected relics ({collectedRelicIds.Count}): {GetCollectedNamesDisplay()}");
    }
}
