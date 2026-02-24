using UnityEngine;

/// <summary>
/// Legacy rank / meta-progression system. Cumulative play earns Legacy Points
/// that translate into small permanent bonuses. Points never decrease.
/// Static class — persists via PlayerPrefs.
/// </summary>
public static class LegacyProgressionManager
{
    private const string PREFS_KEY = "LegacyPoints";

    // Rank thresholds (cumulative legacy points needed)
    private static readonly int[] RankThresholds = { 0, 10, 30, 60, 100, 160, 240, 350, 500, 700 };
    private static readonly string[] RankTitles =
    {
        "Recruit",       // Rank 0
        "Militia",       // Rank 1
        "Sergeant",      // Rank 2
        "Captain",       // Rank 3
        "Commander",     // Rank 4
        "Warden",        // Rank 5
        "Champion",      // Rank 6
        "Marshal",       // Rank 7
        "Grand Marshal", // Rank 8
        "Legendary",     // Rank 9
        "Mythic"         // Rank 10
    };

    public static int GetLegacyPoints()
    {
        return PlayerPrefs.GetInt(PREFS_KEY, 0);
    }

    /// <summary>Add legacy points earned from a run. Points = floor(compositeScore / 1000).</summary>
    public static int AddPointsFromScore(int compositeScore)
    {
        int earned = Mathf.Max(0, compositeScore / 1000);
        if (earned <= 0) return 0;

        int current = GetLegacyPoints();
        int oldRank = GetRankFromPoints(current);
        int newTotal = current + earned;
        PlayerPrefs.SetInt(PREFS_KEY, newTotal);
        PlayerPrefs.Save();

        int newRank = GetRankFromPoints(newTotal);
        if (newRank > oldRank)
            Debug.Log($"[LegacyProgressionManager] RANK UP! {RankTitles[oldRank]} -> {RankTitles[newRank]} ({newTotal} points)");
        else
            Debug.Log($"[LegacyProgressionManager] +{earned} legacy points (total: {newTotal}, rank: {RankTitles[newRank]})");

        return earned;
    }

    public static int GetCurrentRank()
    {
        return GetRankFromPoints(GetLegacyPoints());
    }

    public static string GetCurrentRankTitle()
    {
        int rank = GetCurrentRank();
        return rank < RankTitles.Length ? RankTitles[rank] : RankTitles[RankTitles.Length - 1];
    }

    public static int GetMaxRank()
    {
        return RankThresholds.Length; // 10
    }

    /// <summary>Points needed for next rank. Returns 0 if at max rank.</summary>
    public static int GetPointsToNextRank()
    {
        int rank = GetCurrentRank();
        if (rank >= RankThresholds.Length) return 0;
        return RankThresholds[rank] - GetLegacyPoints();
    }

    /// <summary>Progress fraction (0-1) toward next rank.</summary>
    public static float GetProgressToNextRank()
    {
        int rank = GetCurrentRank();
        if (rank >= RankThresholds.Length) return 1f;

        int points = GetLegacyPoints();
        int prevThreshold = rank > 0 ? RankThresholds[rank - 1] : 0;
        int nextThreshold = RankThresholds[rank];
        int range = nextThreshold - prevThreshold;
        if (range <= 0) return 1f;
        return Mathf.Clamp01((float)(points - prevThreshold) / range);
    }

    private static int GetRankFromPoints(int points)
    {
        for (int i = RankThresholds.Length - 1; i >= 0; i--)
        {
            if (points >= RankThresholds[i]) return i + 1;
        }
        return 0;
    }

    // --- Bonus queries ---
    // Bonuses scale linearly with rank. At rank 10 (max), all bonuses are doubled.

    /// <summary>Bonus starting gold (0 at rank 0, +30 at rank 10).</summary>
    public static int GetBonusStartingGold()
    {
        return GetCurrentRank() * 3;
    }

    /// <summary>Menial speed multiplier bonus (0% at rank 0, +10% at rank 10).</summary>
    public static float GetMenialSpeedMultiplier()
    {
        return 1f + GetCurrentRank() * 0.01f;
    }

    /// <summary>Bonus starting menials (0 at rank 0, +2 at rank 10).</summary>
    public static int GetBonusStartingMenials()
    {
        return GetCurrentRank() / 5; // +1 at rank 5, +2 at rank 10
    }

    /// <summary>Ballista damage multiplier (1.0 at rank 0, 1.10 at rank 10).</summary>
    public static float GetBallistaDamageMultiplier()
    {
        return 1f + GetCurrentRank() * 0.01f;
    }

    /// <summary>Defender attack speed multiplier (1.0 at rank 0, 1.10 at rank 10).</summary>
    public static float GetDefenderAttackSpeedMultiplier()
    {
        return 1f + GetCurrentRank() * 0.01f;
    }

    /// <summary>Wall HP multiplier (1.0 at rank 0, 1.10 at rank 10).</summary>
    public static float GetWallHPMultiplier()
    {
        return 1f + GetCurrentRank() * 0.01f;
    }

    /// <summary>Loot value multiplier (1.0 at rank 0, 1.20 at rank 10).</summary>
    public static float GetLootValueMultiplier()
    {
        return 1f + GetCurrentRank() * 0.02f;
    }

    /// <summary>Returns a formatted string of all active bonuses.</summary>
    public static string GetBonusSummary()
    {
        int rank = GetCurrentRank();
        if (rank == 0) return "No bonuses yet.";

        var sb = new System.Text.StringBuilder();
        int gold = GetBonusStartingGold();
        if (gold > 0) sb.AppendLine($"+{gold} starting gold");
        int menials = GetBonusStartingMenials();
        if (menials > 0) sb.AppendLine($"+{menials} starting menial{(menials > 1 ? "s" : "")}");
        float menialSpd = (GetMenialSpeedMultiplier() - 1f) * 100f;
        if (menialSpd > 0) sb.AppendLine($"+{menialSpd:F0}% menial speed");
        float balDmg = (GetBallistaDamageMultiplier() - 1f) * 100f;
        if (balDmg > 0) sb.AppendLine($"+{balDmg:F0}% ballista damage");
        float defSpd = (GetDefenderAttackSpeedMultiplier() - 1f) * 100f;
        if (defSpd > 0) sb.AppendLine($"+{defSpd:F0}% defender attack speed");
        float wallHP = (GetWallHPMultiplier() - 1f) * 100f;
        if (wallHP > 0) sb.AppendLine($"+{wallHP:F0}% wall HP");
        float loot = (GetLootValueMultiplier() - 1f) * 100f;
        if (loot > 0) sb.AppendLine($"+{loot:F0}% loot value");
        return sb.ToString().TrimEnd();
    }

    public static void ClearProgress()
    {
        PlayerPrefs.SetInt(PREFS_KEY, 0);
        PlayerPrefs.Save();
        Debug.Log("[LegacyProgressionManager] Legacy progress cleared.");
    }
}
