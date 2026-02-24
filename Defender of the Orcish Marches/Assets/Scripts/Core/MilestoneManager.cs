using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MilestoneDef
{
    public string id;
    public string name;
    public string description;
    public int trophyReward; // War Trophies awarded on first completion
}

[Serializable]
public class MilestoneData
{
    public List<string> completedIds = new List<string>();
}

/// <summary>
/// Tracks one-time milestone achievements. Awards War Trophies on first completion.
/// Milestones are checked at end of each run.
/// </summary>
public static class MilestoneManager
{
    private const string PREFS_KEY = "Milestones";

    private static MilestoneData cached;

    public static readonly MilestoneDef[] AllMilestones = new MilestoneDef[]
    {
        // Day survival milestones per difficulty
        new MilestoneDef { id = "easy_day5", name = "Easy Survivor", description = "Survive to day 5 on Easy", trophyReward = 3 },
        new MilestoneDef { id = "easy_day10", name = "Easy Veteran", description = "Survive to day 10 on Easy", trophyReward = 5 },
        new MilestoneDef { id = "easy_day15", name = "Easy Champion", description = "Survive to day 15 on Easy", trophyReward = 8 },
        new MilestoneDef { id = "easy_day20", name = "Easy Legend", description = "Survive to day 20 on Easy", trophyReward = 12 },

        new MilestoneDef { id = "normal_day5", name = "Normal Survivor", description = "Survive to day 5 on Normal", trophyReward = 5 },
        new MilestoneDef { id = "normal_day10", name = "Normal Veteran", description = "Survive to day 10 on Normal", trophyReward = 8 },
        new MilestoneDef { id = "normal_day15", name = "Normal Champion", description = "Survive to day 15 on Normal", trophyReward = 12 },
        new MilestoneDef { id = "normal_day20", name = "Normal Legend", description = "Survive to day 20 on Normal", trophyReward = 18 },

        new MilestoneDef { id = "hard_day5", name = "Hard Survivor", description = "Survive to day 5 on Hard", trophyReward = 8 },
        new MilestoneDef { id = "hard_day10", name = "Hard Veteran", description = "Survive to day 10 on Hard", trophyReward = 12 },
        new MilestoneDef { id = "hard_day15", name = "Hard Champion", description = "Survive to day 15 on Hard", trophyReward = 18 },
        new MilestoneDef { id = "hard_day20", name = "Hard Legend", description = "Survive to day 20 on Hard", trophyReward = 25 },

        new MilestoneDef { id = "nightmare_day5", name = "Nightmare Survivor", description = "Survive to day 5 on Nightmare", trophyReward = 12 },
        new MilestoneDef { id = "nightmare_day10", name = "Nightmare Veteran", description = "Survive to day 10 on Nightmare", trophyReward = 18 },
        new MilestoneDef { id = "nightmare_day15", name = "Nightmare Champion", description = "Survive to day 15 on Nightmare", trophyReward = 25 },
        new MilestoneDef { id = "nightmare_day20", name = "Nightmare Legend", description = "Survive to day 20 on Nightmare", trophyReward = 35 },

        // Kill milestones
        new MilestoneDef { id = "kills_100", name = "Centurion", description = "Kill 100 enemies in a single run", trophyReward = 5 },
        new MilestoneDef { id = "kills_500", name = "Slayer", description = "Kill 500 enemies in a single run", trophyReward = 10 },

        // Boss milestones
        new MilestoneDef { id = "first_boss", name = "Boss Hunter", description = "Kill your first boss", trophyReward = 8 },
        new MilestoneDef { id = "boss_3", name = "Boss Slayer", description = "Kill 3 bosses in a single run", trophyReward = 15 },

        // Economy milestones
        new MilestoneDef { id = "gold_500", name = "Prospector", description = "Earn 500 gold in a single run", trophyReward = 5 },
        new MilestoneDef { id = "gold_1000", name = "Tycoon", description = "Earn 1000 gold in a single run", trophyReward = 10 },

        // Relic milestones
        new MilestoneDef { id = "relics_5", name = "Collector", description = "Collect 5 relics in a single run", trophyReward = 8 },
        new MilestoneDef { id = "relics_10", name = "Hoarder", description = "Collect 10 relics in a single run", trophyReward = 15 },

        // Commander milestones
        new MilestoneDef { id = "play_warden", name = "The Warden's Path", description = "Complete a run as the Warden", trophyReward = 3 },
        new MilestoneDef { id = "play_captain", name = "The Captain's Path", description = "Complete a run as the Captain", trophyReward = 3 },
        new MilestoneDef { id = "play_artificer", name = "The Artificer's Path", description = "Complete a run as the Artificer", trophyReward = 3 },
        new MilestoneDef { id = "play_merchant", name = "The Merchant's Path", description = "Complete a run as the Merchant", trophyReward = 3 },
    };

    // --- Persistence ---

    private static MilestoneData Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cached = JsonUtility.FromJson<MilestoneData>(json);
                if (cached.completedIds == null)
                    cached.completedIds = new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MilestoneManager] Failed to parse milestones: {e.Message}");
                cached = new MilestoneData();
            }
        }
        else
        {
            cached = new MilestoneData();
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

    public static bool IsCompleted(string milestoneId)
    {
        return Load().completedIds.Contains(milestoneId);
    }

    public static int CompletedCount => Load().completedIds.Count;
    public static int TotalCount => AllMilestones.Length;

    /// <summary>
    /// Check and award milestones for a completed run. Returns list of newly completed milestones.
    /// </summary>
    public static List<MilestoneDef> CheckRunMilestones(RunRecord record, Difficulty difficulty, int relicsCollected)
    {
        var newlyCompleted = new List<MilestoneDef>();

        // Day milestones per difficulty
        string diffPrefix = difficulty.ToString().ToLower();
        CheckDayMilestone(diffPrefix + "_day5", record.days, 5, newlyCompleted);
        CheckDayMilestone(diffPrefix + "_day10", record.days, 10, newlyCompleted);
        CheckDayMilestone(diffPrefix + "_day15", record.days, 15, newlyCompleted);
        CheckDayMilestone(diffPrefix + "_day20", record.days, 20, newlyCompleted);

        // Kill milestones
        if (record.kills >= 100) TryComplete("kills_100", newlyCompleted);
        if (record.kills >= 500) TryComplete("kills_500", newlyCompleted);

        // Boss milestones
        if (record.bossKills >= 1) TryComplete("first_boss", newlyCompleted);
        if (record.bossKills >= 3) TryComplete("boss_3", newlyCompleted);

        // Economy milestones
        if (record.goldEarned >= 500) TryComplete("gold_500", newlyCompleted);
        if (record.goldEarned >= 1000) TryComplete("gold_1000", newlyCompleted);

        // Relic milestones
        if (relicsCollected >= 5) TryComplete("relics_5", newlyCompleted);
        if (relicsCollected >= 10) TryComplete("relics_10", newlyCompleted);

        // Commander milestones
        string commanderId = CommanderManager.ActiveCommanderId;
        if (commanderId != CommanderDefs.NONE_ID)
        {
            TryComplete("play_" + commanderId, newlyCompleted);
        }

        if (newlyCompleted.Count > 0)
        {
            Save();
            Debug.Log($"[MilestoneManager] {newlyCompleted.Count} new milestones completed!");
        }

        return newlyCompleted;
    }

    private static void CheckDayMilestone(string id, int daysReached, int required, List<MilestoneDef> results)
    {
        if (daysReached >= required) TryComplete(id, results);
    }

    private static void TryComplete(string milestoneId, List<MilestoneDef> results)
    {
        if (IsCompleted(milestoneId)) return;

        foreach (var def in AllMilestones)
        {
            if (def.id == milestoneId)
            {
                Load().completedIds.Add(milestoneId);
                results.Add(def);

                // Award War Trophies
                if (def.trophyReward > 0)
                {
                    MetaProgressionManager.AwardTrophies(def.trophyReward);
                    Debug.Log($"[MilestoneManager] Milestone completed: {def.name} (+{def.trophyReward} War Trophies)");
                }
                return;
            }
        }
    }

    public static void ClearAll()
    {
        cached = new MilestoneData();
        Save();
        Debug.Log("[MilestoneManager] All milestones cleared.");
    }
}
