using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AchievementProgress
{
    public string id;
    public int value;
    public int tier; // 0=none, 1=bronze, 2=silver, 3=gold
}

[Serializable]
public class AchievementSaveData
{
    public List<AchievementProgress> achievements = new List<AchievementProgress>();
}

/// <summary>
/// Manages achievement progress and persistence via PlayerPrefs JSON.
/// Static class — no MonoBehaviour needed.
/// </summary>
public static class AchievementManager
{
    private const string PREFS_KEY = "Achievements";

    private static Dictionary<string, AchievementProgress> progressMap;

    /// <summary>Fired when a new tier is earned. Args: achievement id, new tier.</summary>
    public static event Action<string, AchievementTier> OnTierEarned;

    private static Dictionary<string, AchievementProgress> Load()
    {
        if (progressMap != null) return progressMap;

        progressMap = new Dictionary<string, AchievementProgress>();
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var save = JsonUtility.FromJson<AchievementSaveData>(json);
                if (save != null && save.achievements != null)
                {
                    foreach (var p in save.achievements)
                        progressMap[p.id] = p;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AchievementManager] Failed to parse achievements: {e.Message}");
            }
        }
        Debug.Log($"[AchievementManager] Loaded {progressMap.Count} achievement records.");
        return progressMap;
    }

    private static void Save()
    {
        if (progressMap == null) return;
        var save = new AchievementSaveData();
        save.achievements = new List<AchievementProgress>(progressMap.Values);
        string json = JsonUtility.ToJson(save);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    public static AchievementTier GetTier(string achievementId)
    {
        var map = Load();
        if (map.TryGetValue(achievementId, out var progress))
            return (AchievementTier)progress.tier;
        return AchievementTier.None;
    }

    public static int GetProgress(string achievementId)
    {
        var map = Load();
        if (map.TryGetValue(achievementId, out var progress))
            return progress.value;
        return 0;
    }

    /// <summary>
    /// Set the progress value for an achievement and check for tier advancement.
    /// Returns newly earned tier (None if no new tier earned).
    /// </summary>
    public static AchievementTier SetProgress(string achievementId, int value)
    {
        var map = Load();
        if (!map.TryGetValue(achievementId, out var progress))
        {
            progress = new AchievementProgress { id = achievementId, value = 0, tier = 0 };
            map[achievementId] = progress;
        }

        progress.value = value;

        var def = AchievementDefs.GetById(achievementId);
        if (def == null) return AchievementTier.None;

        AchievementTier newTier = EvaluateTier(def.Value, value);
        AchievementTier oldTier = (AchievementTier)progress.tier;

        if (newTier > oldTier)
        {
            progress.tier = (int)newTier;
            Save();
            Debug.Log($"[AchievementManager] Achievement '{def.Value.name}' advanced to {newTier}! (value={value})");

            // Unlock mutator at gold tier
            if (newTier == AchievementTier.Gold && !string.IsNullOrEmpty(def.Value.mutatorUnlock))
            {
                MutatorManager.Unlock(def.Value.mutatorUnlock);
                Debug.Log($"[AchievementManager] Mutator '{def.Value.mutatorUnlock}' unlocked via gold achievement '{def.Value.name}'!");
            }

            OnTierEarned?.Invoke(achievementId, newTier);
            return newTier;
        }

        Save();
        return AchievementTier.None;
    }

    /// <summary>
    /// Evaluate all achievements at end of run using run stats and lifetime data.
    /// Returns list of (achievementId, newTier) pairs for newly earned tiers.
    /// </summary>
    public static List<(string id, AchievementTier tier)> EvaluateRunEnd(RunRecord record, Difficulty difficulty)
    {
        var earned = new List<(string, AchievementTier)>();
        var lifetime = LifetimeStatsManager.GetData();

        foreach (var def in AchievementDefs.All)
        {
            int value = GetStatValue(def, record, lifetime, difficulty);
            var newTier = SetProgress(def.id, value);
            if (newTier != AchievementTier.None)
                earned.Add((def.id, newTier));
        }

        if (earned.Count > 0)
            Debug.Log($"[AchievementManager] Run end: {earned.Count} new achievement tier(s) earned.");
        else
            Debug.Log("[AchievementManager] Run end: no new achievement tiers earned.");

        return earned;
    }

    private static int GetStatValue(AchievementDef def, RunRecord record, LifetimeStatsData lifetime, Difficulty difficulty)
    {
        switch (def.id)
        {
            // Survival
            case "stand_your_ground": return record.days;
            case "nightwatch": return lifetime.totalDays;
            case "against_all_odds":
                return (difficulty == Difficulty.Hard || difficulty == Difficulty.Nightmare) ? record.days : GetProgress(def.id);
            case "veteran": return lifetime.totalRuns;

            // Combat
            case "orc_slayer": return record.kills;
            case "sharpshooter": return lifetime.totalBallistaShotsFired;
            case "boss_hunter": return lifetime.totalBossKills;
            case "exterminator": return lifetime.totalKills;

            // Economy
            case "treasure_hoarder": return record.goldEarned;
            case "refugee_savior": return lifetime.totalRefugeesSaved;
            case "builder": return lifetime.totalWallsBuilt;
            case "land_clearer": return lifetime.totalVegetationCleared;

            // Defender
            case "army_builder": return record.hires;
            case "specialist": return lifetime.totalHires;
            case "menial_guardian": return record.menialsLost; // thresholds are inverted
            case "peak_commander": return record.peakDefendersAlive;

            // Special
            case "score_legend": return record.compositeScore;
            case "speed_runner": return record.firstBossKillTime > 0 ? Mathf.RoundToInt(record.firstBossKillTime) : 99999;
            case "wall_repair_master": return lifetime.totalWallHPRepaired;
            case "iron_will":
                return difficulty == Difficulty.Nightmare ? record.days : GetProgress(def.id);

            default:
                Debug.LogWarning($"[AchievementManager] Unknown achievement id: {def.id}");
                return 0;
        }
    }

    private static AchievementTier EvaluateTier(AchievementDef def, int value)
    {
        // Special inverted thresholds: lower is better
        bool inverted = def.id == "menial_guardian" || def.id == "speed_runner";

        if (inverted)
        {
            // For inverted: gold < silver < bronze (lower value = better)
            // But we only award if the player has completed at least one run with the stat tracked
            if (value <= 0 && def.id == "speed_runner") return AchievementTier.None; // no boss killed
            if (value <= def.goldThreshold) return AchievementTier.Gold;
            if (value <= def.silverThreshold) return AchievementTier.Silver;
            if (value <= def.bronzeThreshold) return AchievementTier.Bronze;
            return AchievementTier.None;
        }
        else
        {
            if (value >= def.goldThreshold) return AchievementTier.Gold;
            if (value >= def.silverThreshold) return AchievementTier.Silver;
            if (value >= def.bronzeThreshold) return AchievementTier.Bronze;
            return AchievementTier.None;
        }
    }

    /// <summary>Get the next threshold for an achievement given its current tier.</summary>
    public static int GetNextThreshold(AchievementDef def, AchievementTier currentTier)
    {
        switch (currentTier)
        {
            case AchievementTier.None: return def.bronzeThreshold;
            case AchievementTier.Bronze: return def.silverThreshold;
            case AchievementTier.Silver: return def.goldThreshold;
            default: return def.goldThreshold;
        }
    }

    public static int GetTotalTiersEarned()
    {
        int count = 0;
        foreach (var def in AchievementDefs.All)
        {
            var tier = GetTier(def.id);
            if (tier != AchievementTier.None) count++;
        }
        return count;
    }

    public static int GetGoldCount()
    {
        int count = 0;
        foreach (var def in AchievementDefs.All)
        {
            if (GetTier(def.id) == AchievementTier.Gold) count++;
        }
        return count;
    }

    public static void ClearAll()
    {
        progressMap = new Dictionary<string, AchievementProgress>();
        Save();
        Debug.Log("[AchievementManager] All achievement progress cleared.");
    }
}
