using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>World-space center of the fortress. All fortress-relative calculations use this.</summary>
    public static readonly Vector3 FortressCenter = Vector3.zero;

    public enum GameState { Playing, Paused, GameOver }

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public int Treasure { get; private set; }
    public int MenialCount { get; private set; }
    private int _idleMenialCount;
    public int IdleMenialCount
    {
        get => _idleMenialCount;
        set
        {
            _idleMenialCount = value;
            OnMenialsChanged?.Invoke(MenialCount);
        }
    }
    public float GameTime { get; private set; }
    public int EnemyKills { get; private set; }

    public event Action<int> OnTreasureChanged;
    public event Action<int> OnMenialsChanged;
    public event Action<int> OnKillsChanged;
    public event Action OnGameOver;
    public event Action<bool> OnPauseChanged;
    public event Action<int> OnTreasureGained;
    public event Action<int> OnTreasureSpent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[GameManager] Instance registered in Awake.");
        Application.runInBackground = true;

        // Initialize resources from difficulty settings + meta-progression + commander
        Treasure = GameSettings.GetStartingGold() + MetaProgressionManager.GetBonusStartingGold();
        MenialCount = GameSettings.GetStartingMenials() + MetaProgressionManager.GetBonusStartingMenials()
                    + Mathf.RoundToInt(CommanderManager.GetStartingMenialModifier());
        MenialCount = Mathf.Max(1, MenialCount); // Always start with at least 1
        if (MutatorManager.IsActive("skeleton_crew")) { MenialCount = 1; }
        IdleMenialCount = MenialCount;

        // Load commander selection for this run
        CommanderManager.LoadSelection();
        CommanderManager.LogActiveState();

        Debug.Log($"[GameManager] Difficulty={GameSettings.CurrentDifficulty}, Commander={CommanderManager.GetActiveDisplayName()}: gold={Treasure}, menials={MenialCount}");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Enemy.OnEnemyDied += HandleEnemyDied;
        OnTreasureChanged?.Invoke(Treasure);
        OnMenialsChanged?.Invoke(MenialCount);
        OnKillsChanged?.Invoke(EnemyKills);
        GameSettings.ApplySettings();

        // Check for pending save data (loaded game)
        if (SaveManager.PendingSaveData != null)
        {
            // Delay restore by one frame so all managers finish their Start()
            StartCoroutine(RestoreNextFrame());
        }
    }

    private System.Collections.IEnumerator RestoreNextFrame()
    {
        yield return null; // Wait one frame
        if (SaveManager.PendingSaveData != null)
        {
            RestoreGame(SaveManager.PendingSaveData);
            SaveManager.PendingSaveData = null;
        }
    }

    private void OnDestroy()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        EnemyKills++;
        Debug.Log($"[GameManager] Enemy killed: {enemy.Data?.enemyName}. Total kills={EnemyKills}");
        OnKillsChanged?.Invoke(EnemyKills);
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            // Use unscaledDeltaTime so build mode timeScale doesn't double-apply with TimeMultiplier
            float timeMult = BuildModeManager.Instance != null ? BuildModeManager.Instance.TimeMultiplier : 1f;
            GameTime += Time.unscaledDeltaTime * timeMult;
        }

        // ESC is handled by PauseMenu overlay
    }

    public void AddTreasure(int amount)
    {
        if (CurrentState != GameState.Playing) return;
        Treasure += amount;
        OnTreasureChanged?.Invoke(Treasure);
        OnTreasureGained?.Invoke(amount);
    }

    public bool SpendTreasure(int amount)
    {
        if (CurrentState != GameState.Playing || Treasure < amount) return false;
        Treasure -= amount;
        OnTreasureChanged?.Invoke(Treasure);
        OnTreasureSpent?.Invoke(amount);
        return true;
    }

    public void AddMenial(int count = 1)
    {
        if (CurrentState != GameState.Playing) return;
        MenialCount += count;
        IdleMenialCount += count;
    }

    public void RemoveMenial(int count = 1)
    {
        if (CurrentState != GameState.Playing) return;
        MenialCount = Mathf.Max(0, MenialCount - count);
        // Don't touch IdleMenialCount here — callers (Die, SendToTower, AssignLoot)
        // already handle idle count transitions before calling this.
        OnMenialsChanged?.Invoke(MenialCount);
    }

    public bool SpendMenials(int count)
    {
        if (CurrentState != GameState.Playing || IdleMenialCount < count) return false;
        MenialCount -= count;
        IdleMenialCount -= count;
        return true;
    }

    public bool CanAfford(int treasureCost, int menialCost)
    {
        return Treasure >= treasureCost && IdleMenialCount >= menialCost;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.GameOver) return;

        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            // Restore correct timeScale: 0.1 if in build mode, 1.0 otherwise
            Time.timeScale = (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode) ? 0.1f : 1f;
            Debug.Log($"[GameManager] Unpaused. timeScale={Time.timeScale}");
        }
        else
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Paused.");
        }
        OnPauseChanged?.Invoke(CurrentState == GameState.Paused);
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // ─── Auto-Save ───

    private void OnApplicationPause(bool paused)
    {
        if (paused && CurrentState != GameState.GameOver && CurrentState != GameState.Paused)
        {
            Debug.Log("[GameManager] App backgrounded — auto-saving.");
            SaveManager.AutoSave();
        }
    }

    // ─── Save/Load Restore ───

    /// <summary>
    /// Restore full game state from a save file. Called after normal init completes.
    /// Managers have already booted with defaults — we overwrite them here.
    /// </summary>
    private void RestoreGame(SaveSlotData data)
    {
        Debug.Log($"[GameManager] === RESTORING SAVE: Day {data.dayNumber}, Treasure={data.treasure} ===");

        // 1. Core state
        Treasure = data.treasure;
        MenialCount = data.menialCount;
        _idleMenialCount = data.idleMenialCount;
        GameTime = data.gameTime;
        EnemyKills = data.enemyKills;

        // Restore difficulty (it was set at menu before scene load, but just in case)
        GameSettings.CurrentDifficulty = (Difficulty)data.difficulty;

        // Restore mutators (static, already set before scene load — verify)
        // Mutators are set per-run before scene load, so they should already match.

        // Restore commander
        if (!string.IsNullOrEmpty(data.commanderId))
            CommanderManager.SelectCommander(data.commanderId);

        // 2. Day/Night cycle
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.RestoreState(data);

        // 3. Daily events (must be before enemy spawning since events affect HP/damage)
        if (DailyEventManager.Instance != null)
            DailyEventManager.Instance.RestoreState(data);

        // 4. Upgrade counts (must be before wall/ballista restore since costs depend on counts)
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.RestorePurchaseCounts(data);

        // 5. Walls — destroy defaults and place saved walls
        RestoreWalls(data);

        // 6. Ballistas
        if (BallistaManager.Instance != null)
            BallistaManager.Instance.RestoreState(data);

        // 7. Enemies — spawn manager restores counters and spawns saved enemies
        RestoreEnemies(data);

        // 8. Destroy default starting menials and spawn saved menials
        RestoreMenials(data);

        // 9. Spawn saved defenders
        RestoreDefenders(data);

        // 10. Spawn saved loot
        RestoreLoot(data);

        // 11. Relics
        if (RelicManager.Instance != null)
            RelicManager.Instance.RestoreState(data);

        // 12. Run stats
        if (RunStatsTracker.Instance != null)
            RunStatsTracker.Instance.RestoreState(data);

        // 13. Fire UI update events
        OnTreasureChanged?.Invoke(Treasure);
        OnMenialsChanged?.Invoke(MenialCount);
        OnKillsChanged?.Invoke(EnemyKills);

        // Rebake NavMesh after walls are placed
        if (WallManager.Instance != null)
            WallManager.Instance.RebakeEnemyNavMesh();

        Debug.Log("[GameManager] === SAVE RESTORE COMPLETE ===");
        LogGameSnapshot();
    }

    private void RestoreWalls(SaveSlotData data)
    {
        if (WallManager.Instance == null || data.walls == null) return;

        // Destroy all scene-default walls first
        WallManager.Instance.DestroyAllWalls();

        foreach (var sw in data.walls)
        {
            Vector3 pos = SaveManager.ToVector3(sw.position);
            Quaternion rot = SaveManager.ToQuaternion(sw.rotation);
            WallManager.Instance.PlaceWallWithHP(pos, rot, sw.scaleX, sw.currentHP, sw.maxHP, sw.isUnderConstruction);
        }
    }

    private void RestoreEnemies(SaveSlotData data)
    {
        if (EnemySpawnManager.Instance == null) return;

        // Kill any enemies that spawned during the default Start (unlikely but safe)
        var defaultEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in defaultEnemies)
        {
            if (e != null) Destroy(e.gameObject);
        }

        EnemySpawnManager.Instance.RestoreState(data);
    }

    private void RestoreMenials(SaveSlotData data)
    {
        if (MenialManager.Instance == null || data.menials == null) return;

        // Destroy default starting menials
        var defaultMenials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        foreach (var m in defaultMenials)
        {
            if (m != null) Destroy(m.gameObject);
        }

        // Spawn saved menials at their positions
        foreach (var sm in data.menials)
        {
            Vector3 pos = SaveManager.ToVector3(sm.position);
            var menial = MenialManager.Instance.SpawnMenialAtPosition(pos);
            if (menial != null && sm.currentHP > 0)
                menial.RestoreHP(sm.currentHP);
        }

        // Update counts to match
        MenialCount = data.menialCount;
        _idleMenialCount = data.idleMenialCount;
    }

    private void RestoreDefenders(SaveSlotData data)
    {
        if (UpgradeManager.Instance == null || data.defenders == null) return;

        foreach (var sd in data.defenders)
        {
            UpgradeType upgradeType;
            switch (sd.typeName)
            {
                case "Engineer": upgradeType = UpgradeType.SpawnEngineer; break;
                case "Pikeman": upgradeType = UpgradeType.SpawnPikeman; break;
                case "Crossbowman": upgradeType = UpgradeType.SpawnCrossbowman; break;
                case "Wizard": upgradeType = UpgradeType.SpawnWizard; break;
                default:
                    Debug.LogWarning($"[GameManager] Unknown defender type: {sd.typeName}");
                    continue;
            }

            var prefab = UpgradeManager.Instance.GetDefenderPrefab(upgradeType);
            if (prefab == null)
            {
                Debug.LogError($"[GameManager] No prefab for defender type: {sd.typeName}");
                continue;
            }

            Vector3 pos = SaveManager.ToVector3(sd.position);
            Quaternion rot = SaveManager.ToQuaternion(sd.rotation);
            var go = Instantiate(prefab, pos, rot);
            var defender = go.GetComponent<Defender>();
            if (defender != null)
            {
                defender.Initialize(defender.Data);
                defender.RestoreHP(sd.currentHP);
                Debug.Log($"[GameManager] Restored defender: {sd.typeName} at {pos}, HP={sd.currentHP}");
            }
        }
    }

    private void RestoreLoot(SaveSlotData data)
    {
        if (EnemySpawnManager.Instance == null || data.loot == null) return;

        var treasurePrefab = EnemySpawnManager.Instance.TreasurePrefab;
        if (treasurePrefab == null)
        {
            Debug.LogWarning("[GameManager] No treasure prefab for loot restore.");
            return;
        }

        foreach (var sl in data.loot)
        {
            Vector3 pos = SaveManager.ToVector3(sl.position);
            var go = Instantiate(treasurePrefab, pos, Quaternion.identity);
            var pickup = go.GetComponent<TreasurePickup>();
            if (pickup != null)
            {
                pickup.RestoreValue(sl.value);
                Debug.Log($"[GameManager] Restored loot: value={sl.value} at {pos}");
            }
        }
    }

    /// <summary>
    /// Logs a comprehensive game state snapshot for bug report reproduction.
    /// Call at day start, on bug report submit, and after initial scene setup.
    /// </summary>
    public void LogGameSnapshot()
    {
        Debug.Log("============================================================");
        Debug.Log("[GameManager] ========== GAME STATE SNAPSHOT ==========");

        // Core state
        Debug.Log($"[GameManager] SNAPSHOT: state={CurrentState} gameTime={GameTime:F1} treasure={Treasure} menials={MenialCount} idleMenials={IdleMenialCount} kills={EnemyKills} difficulty={GameSettings.GetDifficultyName()} commander={CommanderManager.GetActiveDisplayName()}");

        // Commander
        CommanderManager.LogActiveState();

        // Relics
        if (RelicManager.Instance != null)
            RelicManager.Instance.LogRelicState();

        // Day/Night cycle
        if (DayNightCycle.Instance != null)
        {
            var dnc = DayNightCycle.Instance;
            Debug.Log($"[GameManager] SNAPSHOT_DNC: day={dnc.DayNumber} phase={dnc.CurrentPhase} progress={dnc.PhaseProgress:F2} remaining={dnc.PhaseTimeRemaining:F1}s phaseDuration={dnc.CurrentPhaseDuration:F1}s");
        }

        // Daily event
        if (DailyEventManager.Instance != null && DailyEventManager.Instance.HasActiveEvent)
        {
            var evt = DailyEventManager.Instance;
            Debug.Log($"[GameManager] SNAPSHOT_EVENT: name={evt.CurrentEventName} category={evt.CurrentEventCategory}");
        }

        // Build mode
        if (BuildModeManager.Instance != null)
        {
            var bm = BuildModeManager.Instance;
            Debug.Log($"[GameManager] SNAPSHOT_BUILD: isBuildMode={bm.IsBuildMode} isIdleSpeedup={bm.IsIdleSpeedup} wallCost={bm.WallCost} timeScale={Time.timeScale}");
        }

        // Run stats
        if (RunStatsTracker.Instance != null)
        {
            var rs = RunStatsTracker.Instance;
            Debug.Log($"[GameManager] SNAPSHOT_STATS: days={rs.Days} kills={rs.Kills} bossKills={rs.BossKills} goldEarned={rs.GoldEarned} hires={rs.Hires} menialsLost={rs.MenialsLost} score={rs.ComputeScore()}");
        }

        // Walls
        if (WallManager.Instance != null)
            WallManager.Instance.LogAllWallState();

        // Defenders
        LogDefenderState();

        // Enemies / spawn state
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.LogSpawnState();

        // Menials
        if (MenialManager.Instance != null)
            MenialManager.Instance.LogMenialState();

        // Upgrades
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.LogUpgradeState();

        // Ballistas
        LogBallistaState();

        // Loot on ground
        LogLootState();

        Debug.Log("[GameManager] ========== END GAME STATE SNAPSHOT ==========");
        Debug.Log("============================================================");
    }

    private void LogDefenderState()
    {
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        int engineers = 0, pikemen = 0, crossbowmen = 0, wizards = 0;

        Debug.Log($"[GameManager] === DEFENDER STATE ({defenders.Length} total) ===");
        foreach (var d in defenders)
        {
            if (d == null || d.IsDead) continue;
            var t = d.transform;
            string typeName = d.Data != null ? d.Data.defenderName : d.GetType().Name;
            Debug.Log($"[GameManager] DEFENDER: type={typeName} pos=({t.position.x:F2},{t.position.y:F2},{t.position.z:F2}) onTower={d.IsOnTower} guarding={d.IsGuarding}");

            if (d is Engineer) engineers++;
            else if (d is Pikeman) pikemen++;
            else if (d is Crossbowman) crossbowmen++;
            else if (d is Wizard) wizards++;
        }
        Debug.Log($"[GameManager] DEFENDER_SUMMARY: engineers={engineers} pikemen={pikemen} crossbowmen={crossbowmen} wizards={wizards}");
        Debug.Log("[GameManager] === END DEFENDER STATE ===");
    }

    private void LogBallistaState()
    {
        if (BallistaManager.Instance == null) return;
        var ballista = BallistaManager.Instance.ActiveBallista;
        if (ballista == null) return;
        Debug.Log($"[GameManager] SNAPSHOT_BALLISTA: active={ballista.name} pos=({ballista.transform.position.x:F2},{ballista.transform.position.y:F2},{ballista.transform.position.z:F2})");
    }

    private void LogLootState()
    {
        var pickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        int uncollected = 0;
        int totalValue = 0;
        foreach (var p in pickups)
        {
            if (p == null || p.IsCollected) continue;
            uncollected++;
            totalValue += p.Value;
        }
        Debug.Log($"[GameManager] SNAPSHOT_LOOT: uncollected={uncollected} totalValue={totalValue}");
        if (uncollected > 0 && uncollected <= 20)
        {
            foreach (var p in pickups)
            {
                if (p == null || p.IsCollected) continue;
                var t = p.transform;
                Debug.Log($"[GameManager] LOOT: value={p.Value} pos=({t.position.x:F2},{t.position.y:F2},{t.position.z:F2})");
            }
        }
    }
}
