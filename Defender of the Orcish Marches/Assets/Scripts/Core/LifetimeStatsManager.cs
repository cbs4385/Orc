using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LifetimeStatsData
{
    // Counters
    public int totalRuns;
    public int totalDays;
    public int totalKills;
    public int totalBossKills;
    public int totalGoldEarned;
    public int totalGoldSpent;
    public int totalMenialsLost;
    public int totalWallsBuilt;
    public int totalWallHPRepaired;
    public int totalVegetationCleared;
    public int totalRefugeesSaved;
    public int totalBallistaShotsFired;
    public int totalHires;

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

    // Personal records
    public int recordLongestRun;
    public int recordHighestScore;
    public int recordMostKills;
    public int recordMostGold;
    public int recordMostDefendersAlive;
    public float recordFastestBossKill; // 0 = none
    public int recordLongestRunEasy;
    public int recordLongestRunNormal;
    public int recordLongestRunHard;
    public int recordLongestRunNightmare;

    // Game-over context: which enemy types were active at each game-over
    public int gameOversWithMelee;
    public int gameOversWithRanged;
    public int gameOversWithWallBreaker;
    public int gameOversWithSuicide;
    public int gameOversWithArtillery;

    // Trend data: last 10 composite scores
    public List<int> recentScores = new List<int>();
}

/// <summary>
/// Persists cumulative lifetime stats in PlayerPrefs JSON.
/// Separate from RunHistoryManager (which only keeps top 20).
/// </summary>
public static class LifetimeStatsManager
{
    private const string PREFS_KEY = "LifetimeStats";

    private static LifetimeStatsData cached;

    private static LifetimeStatsData Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cached = JsonUtility.FromJson<LifetimeStatsData>(json);
                if (cached.recentScores == null)
                    cached.recentScores = new List<int>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LifetimeStatsManager] Failed to parse lifetime stats: {e.Message}");
                cached = new LifetimeStatsData();
            }
        }
        else
        {
            cached = new LifetimeStatsData();
        }
        return cached;
    }

    private static void Save()
    {
        if (cached == null) return;
        string json = JsonUtility.ToJson(cached);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[LifetimeStatsManager] Lifetime stats saved.");
    }

    public static LifetimeStatsData GetData()
    {
        return Load();
    }

    /// <summary>
    /// Called once at game over. Accumulates all per-run data into lifetime totals.
    /// </summary>
    public static void RecordRunEnd(RunRecord record, Difficulty difficulty, float gameTime, int peakDefenders, float firstBossKillTime)
    {
        var data = Load();

        // Counters
        data.totalRuns++;
        data.totalDays += record.days;
        data.totalKills += record.kills;
        data.totalBossKills += record.bossKills;
        data.totalGoldEarned += record.goldEarned;
        data.totalGoldSpent += record.goldSpent;
        data.totalMenialsLost += record.menialsLost;
        data.totalWallsBuilt += record.wallsBuilt;
        data.totalWallHPRepaired += record.wallHPRepaired;
        data.totalVegetationCleared += record.vegetationCleared;
        data.totalRefugeesSaved += record.refugeesSaved;
        data.totalBallistaShotsFired += record.ballistaShotsFired;
        data.totalHires += record.hires;

        // Per-type kills
        data.killsMelee += record.killsMelee;
        data.killsRanged += record.killsRanged;
        data.killsWallBreaker += record.killsWallBreaker;
        data.killsSuicide += record.killsSuicide;
        data.killsArtillery += record.killsArtillery;

        // Per-type hires
        data.hiresEngineer += record.hiresEngineer;
        data.hiresPikeman += record.hiresPikeman;
        data.hiresCrossbowman += record.hiresCrossbowman;
        data.hiresWizard += record.hiresWizard;

        // Personal records
        if (record.days > data.recordLongestRun) data.recordLongestRun = record.days;
        if (record.compositeScore > data.recordHighestScore) data.recordHighestScore = record.compositeScore;
        if (record.kills > data.recordMostKills) data.recordMostKills = record.kills;
        if (record.goldEarned > data.recordMostGold) data.recordMostGold = record.goldEarned;
        if (peakDefenders > data.recordMostDefendersAlive) data.recordMostDefendersAlive = peakDefenders;
        if (firstBossKillTime > 0 && (data.recordFastestBossKill <= 0 || firstBossKillTime < data.recordFastestBossKill))
            data.recordFastestBossKill = firstBossKillTime;

        // Per-difficulty longest run
        switch (difficulty)
        {
            case Difficulty.Easy:
                if (record.days > data.recordLongestRunEasy) data.recordLongestRunEasy = record.days;
                break;
            case Difficulty.Normal:
                if (record.days > data.recordLongestRunNormal) data.recordLongestRunNormal = record.days;
                break;
            case Difficulty.Hard:
                if (record.days > data.recordLongestRunHard) data.recordLongestRunHard = record.days;
                break;
            case Difficulty.Nightmare:
                if (record.days > data.recordLongestRunNightmare) data.recordLongestRunNightmare = record.days;
                break;
        }

        // Game-over context: which enemy types are alive right now
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead || enemy.Data == null) continue;
            switch (enemy.Data.enemyType)
            {
                case EnemyType.Melee: data.gameOversWithMelee++; break;
                case EnemyType.Ranged: data.gameOversWithRanged++; break;
                case EnemyType.WallBreaker: data.gameOversWithWallBreaker++; break;
                case EnemyType.Suicide: data.gameOversWithSuicide++; break;
                case EnemyType.Artillery: data.gameOversWithArtillery++; break;
            }
        }

        // Trend data
        data.recentScores.Add(record.compositeScore);
        if (data.recentScores.Count > 10)
            data.recentScores.RemoveRange(0, data.recentScores.Count - 10);

        Save();

        Debug.Log($"[LifetimeStatsManager] Run #{data.totalRuns} recorded. Days={record.days}, Score={record.compositeScore}, Difficulty={difficulty}");
    }

    // --- Computed stat helpers ---

    public static float GetAverageDays()
    {
        var data = Load();
        return data.totalRuns > 0 ? (float)data.totalDays / data.totalRuns : 0;
    }

    public static float GetAverageKills()
    {
        var data = Load();
        return data.totalRuns > 0 ? (float)data.totalKills / data.totalRuns : 0;
    }

    public static float GetAverageGold()
    {
        var data = Load();
        return data.totalRuns > 0 ? (float)data.totalGoldEarned / data.totalRuns : 0;
    }

    public static float GetKDRatio()
    {
        var data = Load();
        return data.totalMenialsLost > 0 ? (float)data.totalKills / data.totalMenialsLost : 0;
    }

    /// <summary>
    /// Returns the name of the most-hired defender type, or "N/A" if no hires.
    /// </summary>
    public static string GetFavoriteDefender()
    {
        var data = Load();
        int max = 0;
        string favorite = "N/A";

        if (data.hiresEngineer > max) { max = data.hiresEngineer; favorite = "Engineer"; }
        if (data.hiresPikeman > max) { max = data.hiresPikeman; favorite = "Pikeman"; }
        if (data.hiresCrossbowman > max) { max = data.hiresCrossbowman; favorite = "Crossbowman"; }
        if (data.hiresWizard > max) { max = data.hiresWizard; favorite = "Wizard"; }

        return favorite;
    }

    /// <summary>
    /// Returns the enemy type most present at game-overs, or "N/A".
    /// </summary>
    public static string GetMostDangerousEnemy()
    {
        var data = Load();
        int max = 0;
        string dangerous = "N/A";

        if (data.gameOversWithMelee > max) { max = data.gameOversWithMelee; dangerous = "Melee"; }
        if (data.gameOversWithRanged > max) { max = data.gameOversWithRanged; dangerous = "Ranged"; }
        if (data.gameOversWithWallBreaker > max) { max = data.gameOversWithWallBreaker; dangerous = "Wall Breaker"; }
        if (data.gameOversWithSuicide > max) { max = data.gameOversWithSuicide; dangerous = "Suicide"; }
        if (data.gameOversWithArtillery > max) { max = data.gameOversWithArtillery; dangerous = "Artillery"; }

        return dangerous;
    }

    /// <summary>
    /// Returns percentage change: avg(last 5) vs avg(previous 5). 0 if not enough data.
    /// </summary>
    public static float GetScoreTrend()
    {
        var data = Load();
        if (data.recentScores.Count < 6) return 0;

        int count = data.recentScores.Count;
        float recentAvg = 0;
        float olderAvg = 0;

        int recentStart = Mathf.Max(0, count - 5);
        int olderStart = Mathf.Max(0, count - 10);
        int olderEnd = recentStart;

        for (int i = recentStart; i < count; i++)
            recentAvg += data.recentScores[i];
        recentAvg /= (count - recentStart);

        int olderCount = olderEnd - olderStart;
        if (olderCount <= 0) return 0;
        for (int i = olderStart; i < olderEnd; i++)
            olderAvg += data.recentScores[i];
        olderAvg /= olderCount;

        if (olderAvg <= 0) return 0;
        return ((recentAvg - olderAvg) / olderAvg) * 100f;
    }

    public static void ClearStats()
    {
        cached = new LifetimeStatsData();
        Save();
        Debug.Log("[LifetimeStatsManager] Lifetime stats cleared.");
    }
}
