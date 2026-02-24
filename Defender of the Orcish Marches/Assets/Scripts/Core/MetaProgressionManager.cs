using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the meta-upgrade tree — permanent bonuses purchased with War Trophies between runs.
/// </summary>
[Serializable]
public struct MetaUpgradeDef
{
    public string id;
    public string name;
    public string description;
    public int cost; // War Trophies
    public int maxLevel;
}

[Serializable]
public class MetaProgressionData
{
    public int warTrophies;
    public int totalWarTrophiesEarned;
    public List<string> upgradeLevels = new List<string>(); // "id:level" pairs
}

/// <summary>
/// Persistent meta-progression system. War Trophies are earned per run and spent on
/// permanent upgrades that apply small bonuses at the start of each run.
/// </summary>
public static class MetaProgressionManager
{
    private const string PREFS_KEY = "MetaProgression";

    private static MetaProgressionData cached;

    // --- Meta upgrade definitions ---
    public static readonly MetaUpgradeDef[] AllUpgrades = new MetaUpgradeDef[]
    {
        new MetaUpgradeDef {
            id = "starting_gold_1",
            name = "War Coffers I",
            description = "Start each run with +10 gold",
            cost = 5,
            maxLevel = 1
        },
        new MetaUpgradeDef {
            id = "starting_gold_2",
            name = "War Coffers II",
            description = "Start each run with +25 gold (total)",
            cost = 15,
            maxLevel = 1
        },
        new MetaUpgradeDef {
            id = "starting_gold_3",
            name = "War Coffers III",
            description = "Start each run with +50 gold (total)",
            cost = 30,
            maxLevel = 1
        },
        new MetaUpgradeDef {
            id = "ballista_damage",
            name = "Forged Tips",
            description = "Ballista starts with +5 damage per level",
            cost = 10,
            maxLevel = 3
        },
        new MetaUpgradeDef {
            id = "ballista_rate",
            name = "Oiled Gears",
            description = "Ballista fire rate +10% per level",
            cost = 12,
            maxLevel = 2
        },
        new MetaUpgradeDef {
            id = "menial_speed",
            name = "Swift Boots",
            description = "Menial collection speed +10% per level",
            cost = 8,
            maxLevel = 3
        },
        new MetaUpgradeDef {
            id = "wall_hp",
            name = "Reinforced Foundations",
            description = "All walls start with +10% HP per level",
            cost = 10,
            maxLevel = 3
        },
        new MetaUpgradeDef {
            id = "loot_bonus",
            name = "Keen Eye",
            description = "Loot value +5% per level",
            cost = 8,
            maxLevel = 3
        },
        new MetaUpgradeDef {
            id = "starting_menial",
            name = "Volunteer Corps",
            description = "Start with +1 additional menial",
            cost = 20,
            maxLevel = 1
        }
    };

    // --- Persistence ---

    private static MetaProgressionData Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cached = JsonUtility.FromJson<MetaProgressionData>(json);
                if (cached.upgradeLevels == null)
                    cached.upgradeLevels = new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaProgressionManager] Failed to parse meta progression: {e.Message}");
                cached = new MetaProgressionData();
            }
        }
        else
        {
            cached = new MetaProgressionData();
        }
        return cached;
    }

    private static void Save()
    {
        if (cached == null) return;
        string json = JsonUtility.ToJson(cached);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    // --- War Trophies ---

    public static int WarTrophies => Load().warTrophies;
    public static int TotalWarTrophiesEarned => Load().totalWarTrophiesEarned;

    /// <summary>
    /// Calculate war trophies earned from a run.
    /// Formula: (days * 2) + (kills / 10) + (bossKills * 5)
    /// </summary>
    public static int CalculateRunTrophies(RunRecord record)
    {
        int trophies = (record.days * 2) + (record.kills / 10) + (record.bossKills * 5);
        return Mathf.Max(1, trophies); // Minimum 1 trophy per run
    }

    /// <summary>Award war trophies at end of run.</summary>
    public static void AwardTrophies(int amount)
    {
        var data = Load();
        data.warTrophies += amount;
        data.totalWarTrophiesEarned += amount;
        Save();
        Debug.Log($"[MetaProgressionManager] Awarded {amount} War Trophies. Balance: {data.warTrophies}, Lifetime: {data.totalWarTrophiesEarned}");
    }

    // --- Upgrade management ---

    public static int GetUpgradeLevel(string upgradeId)
    {
        var data = Load();
        string prefix = upgradeId + ":";
        foreach (var entry in data.upgradeLevels)
        {
            if (entry.StartsWith(prefix))
            {
                if (int.TryParse(entry.Substring(prefix.Length), out int level))
                    return level;
            }
        }
        return 0;
    }

    public static bool CanPurchaseUpgrade(string upgradeId)
    {
        var def = GetUpgradeDef(upgradeId);
        if (def == null) return false;

        int currentLevel = GetUpgradeLevel(upgradeId);
        if (currentLevel >= def.Value.maxLevel) return false;

        int cost = GetUpgradeCost(upgradeId);
        return Load().warTrophies >= cost;
    }

    /// <summary>Get the cost for the next level of an upgrade (cost increases per level).</summary>
    public static int GetUpgradeCost(string upgradeId)
    {
        var def = GetUpgradeDef(upgradeId);
        if (def == null) return 0;
        int currentLevel = GetUpgradeLevel(upgradeId);
        return def.Value.cost * (currentLevel + 1);
    }

    public static bool PurchaseUpgrade(string upgradeId)
    {
        if (!CanPurchaseUpgrade(upgradeId)) return false;

        int cost = GetUpgradeCost(upgradeId);
        var data = Load();
        data.warTrophies -= cost;

        int newLevel = GetUpgradeLevel(upgradeId) + 1;
        string prefix = upgradeId + ":";
        // Remove old entry
        data.upgradeLevels.RemoveAll(e => e.StartsWith(prefix));
        // Add new entry
        data.upgradeLevels.Add(upgradeId + ":" + newLevel);

        Save();
        Debug.Log($"[MetaProgressionManager] Purchased {upgradeId} level {newLevel} for {cost} trophies. Remaining: {data.warTrophies}");
        return true;
    }

    public static MetaUpgradeDef? GetUpgradeDef(string upgradeId)
    {
        foreach (var def in AllUpgrades)
        {
            if (def.id == upgradeId) return def;
        }
        return null;
    }

    // --- Gameplay effect queries (called at run start) ---

    /// <summary>Bonus starting gold from meta upgrades.</summary>
    public static int GetBonusStartingGold()
    {
        int bonus = 0;
        if (GetUpgradeLevel("starting_gold_1") > 0) bonus += 10;
        if (GetUpgradeLevel("starting_gold_2") > 0) bonus += 15;
        if (GetUpgradeLevel("starting_gold_3") > 0) bonus += 25;
        return bonus;
    }

    /// <summary>Bonus starting menials from meta upgrades.</summary>
    public static int GetBonusStartingMenials()
    {
        return GetUpgradeLevel("starting_menial");
    }

    /// <summary>Bonus ballista damage from meta upgrades.</summary>
    public static int GetBonusBallistaDamage()
    {
        return GetUpgradeLevel("ballista_damage") * 5;
    }

    /// <summary>Ballista fire rate multiplier from meta upgrades (1.0 = no change).</summary>
    public static float GetBallistaFireRateMultiplier()
    {
        return 1f + GetUpgradeLevel("ballista_rate") * 0.1f;
    }

    /// <summary>Menial speed multiplier from meta upgrades.</summary>
    public static float GetMenialSpeedMultiplier()
    {
        return 1f + GetUpgradeLevel("menial_speed") * 0.1f;
    }

    /// <summary>Wall HP multiplier from meta upgrades.</summary>
    public static float GetWallHPMultiplier()
    {
        return 1f + GetUpgradeLevel("wall_hp") * 0.1f;
    }

    /// <summary>Loot value multiplier from meta upgrades.</summary>
    public static float GetLootValueMultiplier()
    {
        return 1f + GetUpgradeLevel("loot_bonus") * 0.05f;
    }

    /// <summary>Log meta progression state.</summary>
    public static void LogState()
    {
        var data = Load();
        Debug.Log($"[MetaProgressionManager] War Trophies: {data.warTrophies} (lifetime: {data.totalWarTrophiesEarned})");
        foreach (var entry in data.upgradeLevels)
        {
            Debug.Log($"[MetaProgressionManager] Upgrade: {entry}");
        }
    }

    public static void ClearAll()
    {
        cached = new MetaProgressionData();
        Save();
        Debug.Log("[MetaProgressionManager] All meta progression cleared.");
    }
}
