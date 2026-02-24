using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BestiaryEntry
{
    public string enemyName;
    public string enemyType;
    public int killCount;
    public bool loreUnlocked; // Unlocked after killing enough
    public string loreTip; // Tactical tip revealed after unlock
}

[Serializable]
public class BestiaryData
{
    public List<BestiaryEntryData> entries = new List<BestiaryEntryData>();
}

[Serializable]
public class BestiaryEntryData
{
    public string enemyId;
    public int totalKills;
}

/// <summary>
/// Tracks lifetime kill counts per enemy type and unlocks lore/tactical tips.
/// Persistent via PlayerPrefs.
/// </summary>
public static class BestiaryManager
{
    private const string PREFS_KEY = "Bestiary";
    private const int KILLS_TO_UNLOCK = 25; // Kill 25 of a type to unlock its lore

    private static BestiaryData cached;

    // Lore entries for each enemy type
    private static readonly Dictionary<string, BestiaryLore> LoreEntries = new Dictionary<string, BestiaryLore>
    {
        { "Orc Grunt", new BestiaryLore {
            description = "The backbone of the orcish horde. Individually weak but dangerous in numbers.",
            tacticalTip = "Grunts are slow and predictable. Use chokepoints to maximize ballista efficiency against groups."
        }},
        { "Bow Orc", new BestiaryLore {
            description = "Cunning archers who strike from a distance, softening defenses before the main assault.",
            tacticalTip = "Bow Orcs stop to fire, making them vulnerable to ballista shots. Prioritize them before they whittle down your defenders."
        }},
        { "Troll", new BestiaryLore {
            description = "Massive brutes with an insatiable hunger for destruction. They target walls with single-minded fury.",
            tacticalTip = "Trolls ignore defenders and beeline for walls. Station pikemen near walls to intercept, or focus ballista fire early."
        }},
        { "Suicide Goblin", new BestiaryLore {
            description = "Deranged goblins strapped with volatile explosives. They cackle as they charge.",
            tacticalTip = "Kill them before they reach the walls. A single ballista shot is enough — don't let groups build up."
        }},
        { "Goblin Cannoneer", new BestiaryLore {
            description = "Goblin siege engineers who lob explosive shells from extreme range.",
            tacticalTip = "Cannoneers have low HP but attack from far away. Use crossbowmen or a well-aimed ballista to pick them off quickly."
        }},
        { "Orc War Boss", new BestiaryLore {
            description = "The mightiest warlords of the orc clans. Their presence rallies lesser orcs to fight harder.",
            tacticalTip = "Bosses have massive HP pools. Focus all firepower on the boss immediately — its high damage will shred walls fast."
        }}
    };

    private struct BestiaryLore
    {
        public string description;
        public string tacticalTip;
    }

    // --- Persistence ---

    private static BestiaryData Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cached = JsonUtility.FromJson<BestiaryData>(json);
                if (cached.entries == null)
                    cached.entries = new List<BestiaryEntryData>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BestiaryManager] Failed to parse bestiary: {e.Message}");
                cached = new BestiaryData();
            }
        }
        else
        {
            cached = new BestiaryData();
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

    /// <summary>Record kills at end of run from run stats.</summary>
    public static void RecordRunKills(RunRecord record)
    {
        AddKills("Orc Grunt", record.killsMelee);
        AddKills("Bow Orc", record.killsRanged);
        AddKills("Troll", record.killsWallBreaker);
        AddKills("Suicide Goblin", record.killsSuicide);
        AddKills("Goblin Cannoneer", record.killsArtillery);

        // Boss kills counted from bossKills field
        if (record.bossKills > 0)
            AddKills("Orc War Boss", record.bossKills);

        Save();
    }

    private static void AddKills(string enemyId, int count)
    {
        if (count <= 0) return;

        var data = Load();
        var entry = data.entries.Find(e => e.enemyId == enemyId);
        if (entry == null)
        {
            entry = new BestiaryEntryData { enemyId = enemyId, totalKills = 0 };
            data.entries.Add(entry);
        }

        bool wasUnlocked = entry.totalKills >= KILLS_TO_UNLOCK;
        entry.totalKills += count;
        bool nowUnlocked = entry.totalKills >= KILLS_TO_UNLOCK;

        if (!wasUnlocked && nowUnlocked)
        {
            Debug.Log($"[BestiaryManager] Lore unlocked for {enemyId}! (Total kills: {entry.totalKills})");
        }
    }

    /// <summary>Get the total kills for an enemy type.</summary>
    public static int GetKillCount(string enemyId)
    {
        var data = Load();
        var entry = data.entries.Find(e => e.enemyId == enemyId);
        return entry != null ? entry.totalKills : 0;
    }

    /// <summary>Check if lore is unlocked for an enemy type.</summary>
    public static bool IsLoreUnlocked(string enemyId)
    {
        return GetKillCount(enemyId) >= KILLS_TO_UNLOCK;
    }

    /// <summary>Get the progress towards unlocking lore (0.0 to 1.0).</summary>
    public static float GetUnlockProgress(string enemyId)
    {
        return Mathf.Clamp01((float)GetKillCount(enemyId) / KILLS_TO_UNLOCK);
    }

    /// <summary>Get a bestiary display entry for an enemy type.</summary>
    public static BestiaryEntry GetEntry(string enemyId)
    {
        int kills = GetKillCount(enemyId);
        bool unlocked = kills >= KILLS_TO_UNLOCK;

        string lore = "";
        string tip = "";
        if (unlocked && LoreEntries.TryGetValue(enemyId, out var loreDef))
        {
            lore = loreDef.description;
            tip = loreDef.tacticalTip;
        }

        return new BestiaryEntry
        {
            enemyName = enemyId,
            enemyType = GetEnemyTypeForId(enemyId),
            killCount = kills,
            loreUnlocked = unlocked,
            loreTip = unlocked ? tip : $"Kill {KILLS_TO_UNLOCK - kills} more to unlock lore"
        };
    }

    /// <summary>Get all bestiary entries for display.</summary>
    public static List<BestiaryEntry> GetAllEntries()
    {
        var result = new List<BestiaryEntry>();
        string[] allEnemies = { "Orc Grunt", "Bow Orc", "Troll", "Suicide Goblin", "Goblin Cannoneer", "Orc War Boss" };
        foreach (var id in allEnemies)
        {
            result.Add(GetEntry(id));
        }
        return result;
    }

    /// <summary>Count how many entries have lore unlocked.</summary>
    public static int UnlockedLoreCount()
    {
        int count = 0;
        string[] allEnemies = { "Orc Grunt", "Bow Orc", "Troll", "Suicide Goblin", "Goblin Cannoneer", "Orc War Boss" };
        foreach (var id in allEnemies)
        {
            if (IsLoreUnlocked(id)) count++;
        }
        return count;
    }

    private static string GetEnemyTypeForId(string enemyId)
    {
        switch (enemyId)
        {
            case "Orc Grunt": return "Melee";
            case "Bow Orc": return "Ranged";
            case "Troll": return "WallBreaker";
            case "Suicide Goblin": return "Suicide";
            case "Goblin Cannoneer": return "Artillery";
            case "Orc War Boss": return "Boss";
            default: return "Unknown";
        }
    }

    public static void ClearAll()
    {
        cached = new BestiaryData();
        Save();
        Debug.Log("[BestiaryManager] Bestiary cleared.");
    }
}
