using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility class that collects normalized game state observations
/// shared by both ML agents. Uses List of float to avoid dependency on Unity.MLAgents.
/// Agent scripts convert these to VectorSensor observations.
/// </summary>
public static class MLGameStateObserver
{
    // Reusable list to avoid allocations
    private static readonly List<float> sharedObs = new List<float>(50);

    /// <summary>
    /// Collects ~42 shared observation values into a list.
    /// Both agents use these as their base observations.
    /// </summary>
    public static List<float> CollectSharedObservations()
    {
        sharedObs.Clear();

        // --- Global state (8 values) ---
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        float phaseProgress = DayNightCycle.Instance != null ? DayNightCycle.Instance.PhaseProgress : 0f;
        bool isDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.IsDay : true;
        var arcBounds = EnemySpawnManager.Instance != null
            ? EnemySpawnManager.Instance.GetCurrentArcBounds()
            : (centerAngle: 180f, halfSpread: 5f);
        int spawnsRemaining = EnemySpawnManager.Instance != null
            ? EnemySpawnManager.Instance.RegularSpawnsRemaining : 0;
        float hpMult = dayNumber > 1 ? 1f + 0.1f * (dayNumber - 1) : 1f;
        float dmgMult = dayNumber > 1 ? 1f + 0.05f * (dayNumber - 1) : 1f;
        int activeEnemyCount = EnemySpawnManager.Instance != null
            ? EnemySpawnManager.Instance.GetActiveEnemyCount() : 0;

        sharedObs.Add(Normalize(dayNumber, 30f));
        sharedObs.Add(phaseProgress);
        sharedObs.Add(isDay ? 1f : 0f);
        sharedObs.Add(Normalize(arcBounds.halfSpread, 180f));
        sharedObs.Add(Normalize(spawnsRemaining, 50f));
        sharedObs.Add(Normalize(hpMult, 5f));
        sharedObs.Add(Normalize(dmgMult, 3f));
        sharedObs.Add(Normalize(activeEnemyCount, 30f));

        // --- Fortress state (8 values) ---
        float wallHPFraction = GetTotalWallHPFraction();
        float breachFraction = GetBreachFraction();
        var defenderCounts = GetDefenderCountsByType();

        sharedObs.Add(wallHPFraction);
        sharedObs.Add(breachFraction);
        sharedObs.Add(Normalize(defenderCounts.total, 10f));
        sharedObs.Add(Normalize(defenderCounts.engineers, 5f));
        sharedObs.Add(Normalize(defenderCounts.pikemen, 5f));
        sharedObs.Add(Normalize(defenderCounts.crossbowmen, 5f));
        sharedObs.Add(Normalize(defenderCounts.wizards, 5f));
        int menialCount = GameManager.Instance != null ? GameManager.Instance.MenialCount : 0;
        sharedObs.Add(Normalize(menialCount, 10f));

        // --- Player resources (2 values) ---
        int gold = GameManager.Instance != null ? GameManager.Instance.Treasure : 0;
        int idleMenials = GameManager.Instance != null ? GameManager.Instance.IdleMenialCount : 0;
        sharedObs.Add(Normalize(gold, 500f));
        sharedObs.Add(Normalize(idleMenials, 10f));

        // --- Enemy composition (6 values) ---
        var enemyCounts = GetEnemyCountsByType();
        sharedObs.Add(Normalize(enemyCounts[0], 20f)); // Grunt
        sharedObs.Add(Normalize(enemyCounts[1], 20f)); // BowOrc
        sharedObs.Add(Normalize(enemyCounts[2], 10f)); // Troll
        sharedObs.Add(Normalize(enemyCounts[3], 10f)); // SuicideGoblin
        sharedObs.Add(Normalize(enemyCounts[4], 5f));  // Cannoneer
        sharedObs.Add(Normalize(enemyCounts[5], 1f));  // WarBoss

        // --- Spatial wall HP by cardinal side (4 values) ---
        var wallHP = GetWallHealthBySide();
        sharedObs.Add(wallHP[0]); // North
        sharedObs.Add(wallHP[1]); // East
        sharedObs.Add(wallHP[2]); // South
        sharedObs.Add(wallHP[3]); // West

        // --- Enemy density by quadrant (4 values) ---
        var enemyDensity = GetEnemyCountsByQuadrant();
        sharedObs.Add(Normalize(enemyDensity[0], 10f)); // NW
        sharedObs.Add(Normalize(enemyDensity[1], 10f)); // NE
        sharedObs.Add(Normalize(enemyDensity[2], 10f)); // SW
        sharedObs.Add(Normalize(enemyDensity[3], 10f)); // SE

        // --- Ballista state (2 values) ---
        int ballistaCount = BallistaManager.Instance != null ? BallistaManager.Instance.BallistaCount : 1;
        float avgDamage = 0f;
        if (BallistaManager.Instance != null && BallistaManager.Instance.ActiveBallista != null)
            avgDamage = BallistaManager.Instance.ActiveBallista.Damage;
        sharedObs.Add(Normalize(ballistaCount, 4f));
        sharedObs.Add(Normalize(avgDamage, 100f));

        // --- Type unlock mask (5 values) ---
        sharedObs.Add(1f); // Grunt always unlocked
        sharedObs.Add(dayNumber >= 2 ? 1f : 0f); // BowOrc
        sharedObs.Add(dayNumber >= 3 ? 1f : 0f); // Troll
        sharedObs.Add(dayNumber >= 4 ? 1f : 0f); // SuicideGoblin
        sharedObs.Add(dayNumber >= 5 ? 1f : 0f); // Cannoneer

        return sharedObs;
    }

    public static float Normalize(float value, float max)
    {
        return Mathf.Clamp01(value / Mathf.Max(max, 0.001f));
    }

    public static float GetTotalWallHPFraction()
    {
        if (WallManager.Instance == null) return 1f;
        var walls = WallManager.Instance.AllWalls;
        if (walls.Count == 0) return 0f;

        float totalHP = 0f, totalMax = 0f;
        foreach (var wall in walls)
        {
            if (wall == null) continue;
            totalHP += wall.CurrentHP;
            totalMax += wall.MaxHP;
        }
        return totalMax > 0 ? totalHP / totalMax : 0f;
    }

    public static float GetBreachFraction()
    {
        if (WallManager.Instance == null) return 0f;
        var walls = WallManager.Instance.AllWalls;
        if (walls.Count == 0) return 0f;

        int destroyed = 0;
        foreach (var wall in walls)
        {
            if (wall == null || wall.IsDestroyed) destroyed++;
        }
        return (float)destroyed / walls.Count;
    }

    public struct DefenderCounts
    {
        public int total, engineers, pikemen, crossbowmen, wizards;
    }

    public static DefenderCounts GetDefenderCountsByType()
    {
        var counts = new DefenderCounts();
        var defenders = Object.FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var d in defenders)
        {
            if (d == null || d.IsDead || d.Data == null) continue;
            counts.total++;
            switch (d.Data.defenderType)
            {
                case DefenderType.Engineer: counts.engineers++; break;
                case DefenderType.Pikeman: counts.pikemen++; break;
                case DefenderType.Crossbowman: counts.crossbowmen++; break;
                case DefenderType.Wizard: counts.wizards++; break;
            }
        }
        return counts;
    }

    /// <summary>Returns enemy counts by type index (0-5).</summary>
    public static int[] GetEnemyCountsByType()
    {
        int[] counts = new int[6];
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead || enemy.Data == null) continue;
            switch (enemy.Data.enemyType)
            {
                case EnemyType.Melee:
                    if (enemy.Data.maxHP >= 200) counts[5]++;
                    else counts[0]++;
                    break;
                case EnemyType.Ranged: counts[1]++; break;
                case EnemyType.WallBreaker: counts[2]++; break;
                case EnemyType.Suicide: counts[3]++; break;
                case EnemyType.Artillery: counts[4]++; break;
            }
        }
        return counts;
    }

    /// <summary>Returns wall HP fractions by cardinal side: [N, E, S, W].</summary>
    public static float[] GetWallHealthBySide()
    {
        float[] hp = new float[4];
        float[] maxHp = new float[4];

        if (WallManager.Instance == null) return new float[] { 0f, 0f, 0f, 0f };

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall == null) continue;
            Vector3 pos = wall.transform.position;
            int side = GetCardinalSide(pos);
            hp[side] += wall.CurrentHP;
            maxHp[side] += wall.MaxHP;
        }

        float[] fractions = new float[4];
        for (int i = 0; i < 4; i++)
            fractions[i] = maxHp[i] > 0 ? hp[i] / maxHp[i] : 0f;
        return fractions;
    }

    /// <summary>Returns enemy counts by quadrant: [NW, NE, SW, SE].</summary>
    public static int[] GetEnemyCountsByQuadrant()
    {
        int[] counts = new int[4];
        Vector3 center = GameManager.FortressCenter;
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            Vector3 pos = enemy.transform.position;
            bool north = pos.z >= center.z;
            bool west = pos.x <= center.x;
            if (north && west) counts[0]++;
            else if (north) counts[1]++;
            else if (west) counts[2]++;
            else counts[3]++;
        }
        return counts;
    }

    /// <summary>Returns threat-sorted list of enemies (closest to fortress first).</summary>
    public static List<Enemy> GetThreatSortedEnemies(int maxCount = 5)
    {
        var sorted = new List<Enemy>();
        Vector3 center = GameManager.FortressCenter;

        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            sorted.Add(enemy);
        }

        sorted.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.transform.position, center);
            float distB = Vector3.Distance(b.transform.position, center);
            return distA.CompareTo(distB);
        });

        if (sorted.Count > maxCount)
            sorted.RemoveRange(maxCount, sorted.Count - maxCount);
        return sorted;
    }

    /// <summary>Returns nearest uncollected loot sorted by distance from fortress center.</summary>
    public static List<TreasurePickup> GetNearestLoot(int maxCount = 5)
    {
        var allLoot = Object.FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        var sorted = new List<TreasurePickup>();
        Vector3 center = GameManager.FortressCenter;

        foreach (var loot in allLoot)
        {
            if (loot == null || loot.IsCollected) continue;
            sorted.Add(loot);
        }

        sorted.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.transform.position, center);
            float distB = Vector3.Distance(b.transform.position, center);
            return distA.CompareTo(distB);
        });

        if (sorted.Count > maxCount)
            sorted.RemoveRange(maxCount, sorted.Count - maxCount);
        return sorted;
    }

    private static int GetCardinalSide(Vector3 pos)
    {
        Vector3 dir = pos - GameManager.FortressCenter;
        if (Mathf.Abs(dir.z) >= Mathf.Abs(dir.x))
            return dir.z >= 0 ? 0 : 2;
        return dir.x >= 0 ? 1 : 3;
    }
}
