using System;
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

        // Initialize resources from difficulty settings (serialized values are fallbacks)
        Treasure = GameSettings.GetStartingGold();
        MenialCount = GameSettings.GetStartingMenials();
        IdleMenialCount = MenialCount;
        Debug.Log($"[GameManager] Difficulty={GameSettings.CurrentDifficulty}: gold={Treasure}, menials={MenialCount}");
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

    /// <summary>
    /// Logs a comprehensive game state snapshot for bug report reproduction.
    /// Call at day start, on bug report submit, and after initial scene setup.
    /// </summary>
    public void LogGameSnapshot()
    {
        Debug.Log("============================================================");
        Debug.Log("[GameManager] ========== GAME STATE SNAPSHOT ==========");

        // Core state
        Debug.Log($"[GameManager] SNAPSHOT: state={CurrentState} gameTime={GameTime:F1} treasure={Treasure} menials={MenialCount} idleMenials={IdleMenialCount} kills={EnemyKills} difficulty={GameSettings.GetDifficultyName()}");

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
