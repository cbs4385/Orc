using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RunRecord
{
    public int days;
    public int kills;
    public int bossKills;
    public int goldEarned;
    public int hires;
    public int menialsLost;
    public int compositeScore;
    public string timestamp;

    // Extended stats (JsonUtility defaults missing fields to 0 for old saves)
    public int difficulty;
    public int goldSpent;
    public int wallsBuilt;
    public int wallHPRepaired;
    public int vegetationCleared;
    public int refugeesSaved;
    public int ballistaShotsFired;
    public int peakDefendersAlive;
    public float firstBossKillTime;

    // Per-type kills
    public int killsMelee;
    public int killsRanged;
    public int killsWallBreaker;
    public int killsSuicide;
    public int killsArtillery;

    // Per-type hires
    public int hiresEngineer;
    public int hiresPikeman;
    public int hiresCrossbowman;
    public int hiresWizard;

    // Commander and relics (added for replayability features)
    public string commanderId;
    public int relicsCollected;
    public int warTrophiesEarned;
}

[Serializable]
class RunHistoryData
{
    public List<RunRecord> runs = new List<RunRecord>();
}

/// <summary>
/// Persists top 20 runs via PlayerPrefs JSON.
/// </summary>
public static class RunHistoryManager
{
    private const string PREFS_KEY = "RunHistory";
    private const int MAX_RUNS = 20;

    private static RunHistoryData cached;

    private static RunHistoryData Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cached = JsonUtility.FromJson<RunHistoryData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunHistoryManager] Failed to parse run history: {e.Message}");
                cached = new RunHistoryData();
            }
        }
        else
        {
            cached = new RunHistoryData();
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

    /// <summary>
    /// Save a run. Returns the rank (0-based) in the leaderboard, or -1 if it didn't make the top 20.
    /// </summary>
    public static int SaveRun(RunRecord record)
    {
        var data = Load();
        data.runs.Add(record);
        data.runs.Sort((a, b) => b.compositeScore.CompareTo(a.compositeScore));
        if (data.runs.Count > MAX_RUNS)
            data.runs.RemoveRange(MAX_RUNS, data.runs.Count - MAX_RUNS);

        Save();

        int rank = data.runs.FindIndex(r => r.timestamp == record.timestamp && r.compositeScore == record.compositeScore);
        Debug.Log($"[RunHistoryManager] Run saved: score={record.compositeScore}, rank={rank + 1}/{data.runs.Count}");
        return rank;
    }

    public static List<RunRecord> GetTopRuns()
    {
        return new List<RunRecord>(Load().runs);
    }

    /// <summary>
    /// Returns the best values across all previous runs for NEW BEST comparison.
    /// </summary>
    public static RunRecord GetBestByCategory()
    {
        var data = Load();
        var best = new RunRecord();
        for (int i = 0; i < data.runs.Count; i++)
        {
            var r = data.runs[i];
            if (r.days > best.days) best.days = r.days;
            if (r.kills > best.kills) best.kills = r.kills;
            if (r.bossKills > best.bossKills) best.bossKills = r.bossKills;
            if (r.goldEarned > best.goldEarned) best.goldEarned = r.goldEarned;
            if (r.hires > best.hires) best.hires = r.hires;
            if (r.compositeScore > best.compositeScore) best.compositeScore = r.compositeScore;
            // menialsLost: "best" is lowest
            if (i == 0 || r.menialsLost < best.menialsLost) best.menialsLost = r.menialsLost;
        }
        return best;
    }

    public static int GetRunCount()
    {
        return Load().runs.Count;
    }

    public static void ClearHistory()
    {
        cached = new RunHistoryData();
        Save();
        Debug.Log("[RunHistoryManager] History cleared.");
    }
}
