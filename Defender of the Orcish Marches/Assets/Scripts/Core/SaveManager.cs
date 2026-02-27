using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Static save/load manager. Saves to persistentDataPath/saves/ as JSON files.
/// Pattern matches RunHistoryManager (static, file-based, JsonUtility).
/// </summary>
public static class SaveManager
{
    private const int MAX_SLOTS = 3;
    private const string SAVE_DIR = "saves";
    private const string LAST_SLOT_KEY = "SaveManager_LastUsedSlot";

    /// <summary>Set before loading GameScene. GameManager consumes this on Start.</summary>
    public static SaveSlotData PendingSaveData { get; set; }

    /// <summary>Last slot used (for auto-save target). Persisted in PlayerPrefs.</summary>
    public static int LastUsedSlot
    {
        get => PlayerPrefs.GetInt(LAST_SLOT_KEY, 0);
        set { PlayerPrefs.SetInt(LAST_SLOT_KEY, value); PlayerPrefs.Save(); }
    }

    private static string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_DIR);
    }

    private static string GetSlotPath(int slot)
    {
        return Path.Combine(GetSaveDirectory(), $"slot_{slot}.json");
    }

    public static bool SlotExists(int slot)
    {
        return File.Exists(GetSlotPath(slot));
    }

    public static bool HasAnySave()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (SlotExists(i)) return true;
        }
        return false;
    }

    // ─── Save ───

    public static void SaveToSlot(int slot)
    {
        var data = CaptureGameState(slot);
        string json = JsonUtility.ToJson(data, false);

        string dir = GetSaveDirectory();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(GetSlotPath(slot), json);
        LastUsedSlot = slot;
        Debug.Log($"[SaveManager] Saved to slot {slot}. Day={data.dayNumber}, Treasure={data.treasure}, file={GetSlotPath(slot)}");
    }

    public static void AutoSave()
    {
        SaveToSlot(LastUsedSlot);
        Debug.Log($"[SaveManager] Auto-saved to slot {LastUsedSlot}.");
    }

    // ─── Load ───

    public static SaveSlotData LoadSlot(int slot)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] No save file at slot {slot}.");
            return null;
        }

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<SaveSlotData>(json);
        Debug.Log($"[SaveManager] Loaded slot {slot}. Day={data.dayNumber}, Treasure={data.treasure}");
        return data;
    }

    // ─── Delete ───

    public static void DeleteSlot(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] Deleted slot {slot}.");
        }
    }

    // ─── Metadata (for slot picker display) ───

    public static SaveSlotData GetSlotMetadata(int slot)
    {
        return LoadSlot(slot); // Full parse is fast enough for 3 slots
    }

    // ─── Capture ───

    private static SaveSlotData CaptureGameState(int slot)
    {
        var data = new SaveSlotData();

        // Metadata
        data.slotIndex = slot;
        data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        // Core state from GameManager
        var gm = GameManager.Instance;
        if (gm != null)
        {
            data.treasure = gm.Treasure;
            data.menialCount = gm.MenialCount;
            data.idleMenialCount = gm.IdleMenialCount;
            data.gameTime = gm.GameTime;
            data.enemyKills = gm.EnemyKills;
        }

        // Difficulty
        data.difficulty = (int)GameSettings.CurrentDifficulty;
        data.metaDifficulty = data.difficulty;
        data.metaTreasure = data.treasure;

        // Day/Night cycle
        var dnc = DayNightCycle.Instance;
        if (dnc != null)
        {
            data.dayNumber = dnc.DayNumber;
            data.metaDayNumber = dnc.DayNumber;
            data.phase = (int)dnc.CurrentPhase;
            data.phaseTimer = dnc.PhaseTimeRemaining > 0 ? dnc.CurrentPhaseDuration - dnc.PhaseTimeRemaining : 0f;
            data.isFirstDay = dnc.DayNumber == 1 && dnc.CurrentPhase == DayNightCycle.Phase.Day;
        }

        // Spawn state from EnemySpawnManager
        CaptureSpawnState(data);

        // Walls
        CaptureWalls(data);

        // Ballistas
        CaptureBallistas(data);

        // Defenders
        CaptureDefenders(data);

        // Enemies
        CaptureEnemies(data);

        // Menials
        CaptureMenials(data);

        // Loot
        CaptureLoot(data);

        // Upgrades
        CaptureUpgrades(data);

        // Mutators
        foreach (var id in MutatorManager.ActiveMutatorIds)
            data.activeMutatorIds.Add(id);

        // Commander
        data.commanderId = CommanderManager.ActiveCommanderId;

        // Relics
        var rm = RelicManager.Instance;
        if (rm != null)
        {
            foreach (var id in rm.CollectedRelicIds)
                data.collectedRelicIds.Add(id);
            foreach (var id in rm.ActiveSynergyIds)
                data.activeSynergyIds.Add(id);
        }

        // Daily event
        CaptureDailyEvent(data);

        // Run stats
        CaptureRunStats(data);

        // Build mode wall count (derived from UpgradeManager's NewWall purchase count)
        if (UpgradeManager.Instance != null)
        {
            foreach (var uc in data.upgradeCounts)
            {
                if (uc.typeName == UpgradeType.NewWall.ToString())
                {
                    data.buildModeWallCount = uc.count;
                    break;
                }
            }
        }

        return data;
    }

    private static void CaptureSpawnState(SaveSlotData data)
    {
        var esm = EnemySpawnManager.Instance;
        if (esm == null) return;

        // These fields are private in EnemySpawnManager — we use the public accessors
        data.dayTotalEnemies = esm.DayTotalEnemies;
        // regularSpawnsRemaining, dayKills, etc. are private — we'll capture what we can
        // and let RestoreState recalculate on load
    }

    private static void CaptureWalls(SaveSlotData data)
    {
        var wm = WallManager.Instance;
        if (wm == null) return;

        foreach (var wall in wm.AllWalls)
        {
            if (wall == null) continue;
            var sw = new SavedWall();
            sw.position = ToFloatArray(wall.transform.position);
            sw.rotation = ToQuatArray(wall.transform.rotation);
            sw.scaleX = wall.transform.localScale.x;
            sw.currentHP = wall.CurrentHP;
            sw.maxHP = wall.MaxHP;
            sw.isUnderConstruction = wall.IsUnderConstruction;
            data.walls.Add(sw);
        }
    }

    private static void CaptureBallistas(SaveSlotData data)
    {
        var bm = BallistaManager.Instance;
        if (bm == null) return;

        var ballistas = UnityEngine.Object.FindObjectsByType<Ballista>(FindObjectsSortMode.None);
        foreach (var b in ballistas)
        {
            if (b == null) continue;
            var sb = new SavedBallista();
            sb.position = ToFloatArray(b.transform.position);
            sb.rotation = ToQuatArray(b.transform.rotation);
            sb.damage = b.Damage;
            sb.fireRate = b.FireRate;
            sb.hasDoubleShot = b.HasDoubleShot;
            sb.hasBurstDamage = b.HasBurstDamage;
            data.ballistas.Add(sb);
        }

        // Active ballista index — find which index the active ballista is
        if (bm.ActiveBallista != null)
        {
            for (int i = 0; i < ballistas.Length; i++)
            {
                if (ballistas[i] == bm.ActiveBallista)
                {
                    data.activeBallistaIndex = i;
                    break;
                }
            }
        }
    }

    private static void CaptureDefenders(SaveSlotData data)
    {
        var defenders = UnityEngine.Object.FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var d in defenders)
        {
            if (d == null || d.IsDead) continue;
            var sd = new SavedDefender();
            if (d is Engineer) sd.typeName = "Engineer";
            else if (d is Pikeman) sd.typeName = "Pikeman";
            else if (d is Crossbowman) sd.typeName = "Crossbowman";
            else if (d is Wizard) sd.typeName = "Wizard";
            else sd.typeName = d.GetType().Name;
            sd.position = ToFloatArray(d.transform.position);
            sd.rotation = ToQuatArray(d.transform.rotation);
            sd.currentHP = d.CurrentHP;
            data.defenders.Add(sd);
        }
    }

    private static void CaptureEnemies(SaveSlotData data)
    {
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            var se = new SavedEnemy();
            se.dataName = enemy.Data != null ? enemy.Data.enemyName : "Orc Grunt";
            se.position = ToFloatArray(enemy.transform.position);
            se.rotation = ToQuatArray(enemy.transform.rotation);
            se.currentHP = enemy.CurrentHP;
            se.scaledDamage = enemy.ScaledDamage;
            var movement = enemy.GetComponent<EnemyMovement>();
            se.isRetreating = movement != null && movement.IsRetreating;
            data.enemies.Add(se);
        }
    }

    private static void CaptureMenials(SaveSlotData data)
    {
        var menials = UnityEngine.Object.FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var m in menials)
        {
            if (m == null || m.IsDead) continue;
            var sm = new SavedMenial();
            sm.position = ToFloatArray(m.transform.position);
            sm.currentHP = m.CurrentHP;
            sm.carriedTreasure = 0; // Menials reset to idle on load
            data.menials.Add(sm);
        }
    }

    private static void CaptureLoot(SaveSlotData data)
    {
        var pickups = UnityEngine.Object.FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (p == null || p.IsCollected) continue;
            var sl = new SavedLoot();
            sl.position = ToFloatArray(p.transform.position);
            sl.value = p.Value;
            sl.spawnGameTime = data.gameTime; // Approximate — reset despawn timer on load
            data.loot.Add(sl);
        }
    }

    private static void CaptureUpgrades(SaveSlotData data)
    {
        if (UpgradeManager.Instance == null) return;

        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            int count = UpgradeManager.Instance.GetPurchaseCountPublic(type);
            if (count > 0)
            {
                data.upgradeCounts.Add(new SavedUpgradeCount
                {
                    typeName = type.ToString(),
                    count = count
                });
            }
        }
    }

    private static void CaptureDailyEvent(SaveSlotData data)
    {
        var dem = DailyEventManager.Instance;
        if (dem == null) return;

        data.lootValueMultiplier = dem.LootValueMultiplier;
        data.defenderDamageMultiplier = dem.DefenderDamageMultiplier;
        data.menialSpeedMultiplier = dem.MenialSpeedMultiplier;
        data.enemyDamageMultiplier = dem.EnemyDamageMultiplier;
        data.spawnRateMultiplier = dem.SpawnRateMultiplier;
        data.enemyHPMultiplier = dem.EnemyHPMultiplier;
        data.enemySpeedMultiplier = dem.EnemySpeedMultiplier;
        data.defenderAttackSpeedMultiplier = dem.DefenderAttackSpeedMultiplier;
        data.eventName = dem.CurrentEventName;
        data.eventDescription = dem.CurrentEventDescription;
        data.eventCategory = (int)dem.CurrentEventCategory;
        data.hasActiveEvent = dem.HasActiveEvent;
    }

    private static void CaptureRunStats(SaveSlotData data)
    {
        var rs = RunStatsTracker.Instance;
        if (rs == null) return;

        data.runStats.days = rs.Days;
        data.runStats.kills = rs.Kills;
        data.runStats.bossKills = rs.BossKills;
        data.runStats.goldEarned = rs.GoldEarned;
        data.runStats.hires = rs.Hires;
        data.runStats.menialsLost = rs.MenialsLost;
        data.runStats.goldSpent = rs.GoldSpent;
        data.runStats.wallsBuilt = rs.WallsBuilt;
        data.runStats.wallHPRepaired = rs.WallHPRepaired;
        data.runStats.vegetationCleared = rs.VegetationCleared;
        data.runStats.refugeesSaved = rs.RefugeesSaved;
        data.runStats.ballistaShotsFired = rs.BallistaShotsFired;
        data.runStats.peakDefendersAlive = rs.PeakDefendersAlive;
        data.runStats.firstBossKillTime = rs.FirstBossKillTime;

        data.runStats.killsMelee = rs.KillsMelee;
        data.runStats.killsRanged = rs.KillsRanged;
        data.runStats.killsWallBreaker = rs.KillsWallBreaker;
        data.runStats.killsSuicide = rs.KillsSuicide;
        data.runStats.killsArtillery = rs.KillsArtillery;

        data.runStats.killsOrcGrunt = rs.KillsOrcGrunt;
        data.runStats.killsBowOrc = rs.KillsBowOrc;
        data.runStats.killsTroll = rs.KillsTroll;
        data.runStats.killsSuicideGoblin = rs.KillsSuicideGoblin;
        data.runStats.killsGoblinCannoneer = rs.KillsGoblinCannoneer;
        data.runStats.killsOrcWarBoss = rs.KillsOrcWarBoss;

        data.runStats.hiresEngineer = rs.HiresEngineer;
        data.runStats.hiresPikeman = rs.HiresPikeman;
        data.runStats.hiresCrossbowman = rs.HiresCrossbowman;
        data.runStats.hiresWizard = rs.HiresWizard;
    }

    // ─── Helpers ───

    private static float[] ToFloatArray(Vector3 v)
    {
        return new float[] { v.x, v.y, v.z };
    }

    private static float[] ToQuatArray(Quaternion q)
    {
        return new float[] { q.x, q.y, q.z, q.w };
    }

    public static Vector3 ToVector3(float[] arr)
    {
        if (arr == null || arr.Length < 3) return Vector3.zero;
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    public static Quaternion ToQuaternion(float[] arr)
    {
        if (arr == null || arr.Length < 4) return Quaternion.identity;
        return new Quaternion(arr[0], arr[1], arr[2], arr[3]);
    }
}
