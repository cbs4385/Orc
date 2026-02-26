using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildModeManager : MonoBehaviour
{
    public static BuildModeManager Instance { get; private set; }

    public bool IsBuildMode { get; private set; }

    /// <summary>True when time is sped up 3x because there are no enemies and no loot.</summary>
    public bool IsIdleSpeedup { get; private set; }

    public float TimeMultiplier
    {
        get
        {
            if (IsBuildMode) return 0.1f;
            if (IsIdleSpeedup) return 3.0f;
            return 1.0f;
        }
    }

    public event Action OnBuildModeStarted;
    public event Action OnBuildModeEnded;

    private bool subscribedToSpawnManager;
    private bool subscribedToDayNight;

    /// <summary>Wall cost used by the continuous placement system.</summary>
    public int WallCost { get; private set; }

    // Track whether enemies have been cleared this night (for hint banner at nightfall)
    private bool enemiesCleared;
    private const float EXIT_BANNER_DURATION = 3f;
    private const float HINT_BANNER_DURATION = 4f;

    // Idle speedup tracking (periodic to avoid per-frame FindObjectsByType)
    private float lootCheckTimer;
    private const float LOOT_CHECK_INTERVAL = 0.5f;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[BuildModeManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[BuildModeManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.OnAllEnemiesCleared -= HandleAllEnemiesCleared;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted -= HandleDayStarted;
            DayNightCycle.Instance.OnNightStarted -= HandleNightStarted;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Late-subscribe to EnemySpawnManager (it may init after us)
        if (!subscribedToSpawnManager && EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.OnAllEnemiesCleared += HandleAllEnemiesCleared;
            subscribedToSpawnManager = true;
            Debug.Log("[BuildModeManager] Subscribed to EnemySpawnManager.OnAllEnemiesCleared.");
        }

        // Late-subscribe to DayNightCycle
        if (!subscribedToDayNight && DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnDayStarted += HandleDayStarted;
            DayNightCycle.Instance.OnNightStarted += HandleNightStarted;
            subscribedToDayNight = true;
            Debug.Log("[BuildModeManager] Subscribed to DayNightCycle events.");
        }

        // Update idle speedup check periodically
        UpdateIdleSpeedup();

        // Toggle build mode
        if (InputBindingManager.Instance != null && InputBindingManager.Instance.WasPressedThisFrame(GameAction.ToggleBuildMode))
        {
            if (IsBuildMode)
            {
                Debug.Log("[BuildModeManager] Build mode key pressed — exiting build mode.");
                ExitBuildMode();
            }
            else
            {
                TryEnterBuildMode();
            }
            return;
        }

        if (!IsBuildMode) return;

        // Keep cached wall cost up to date each frame (for HUD display)
        WallCost = GetWallCost();

        // Auto-exit if player can no longer afford walls
        if (!CanAffordWall())
        {
            Debug.Log("[BuildModeManager] Player can no longer afford walls — auto-exiting build mode.");
            ExitBuildMode();
        }
    }

    private void TryEnterBuildMode()
    {
        // Must be night
        if (DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight)
        {
            GameHUD.ShowBanner("Can only build at night", HINT_BANNER_DURATION);
            Debug.Log("[BuildModeManager] B key pressed — not night, cannot enter build mode.");
            return;
        }

        WallCost = GetWallCost();
        bool hasEngineer = HasLivingEngineer();
        bool canAfford = CanAffordWall();
        int currentGold = GameManager.Instance != null ? GameManager.Instance.Treasure : -1;

        if (!hasEngineer)
        {
            string upgKey = InputBindingManager.Instance != null
                ? InputBindingManager.Instance.GetKeyboardDisplayName(GameAction.ToggleUpgrades) : "U";
            GameHUD.ShowBanner($"No engineer — hire one from Upgrades ({upgKey})", HINT_BANNER_DURATION);
            Debug.Log("[BuildModeManager] B key pressed — no engineer.");
            return;
        }

        if (!canAfford)
        {
            GameHUD.ShowBanner($"Not enough gold for walls (need {WallCost}g, have {currentGold}g)", HINT_BANNER_DURATION);
            Debug.Log($"[BuildModeManager] B key pressed — can't afford walls. Need {WallCost}g, have {currentGold}g.");
            return;
        }

        EnterBuildMode();
    }

    private void UpdateIdleSpeedup()
    {
        lootCheckTimer -= Time.deltaTime;
        if (lootCheckTimer > 0f) return;
        lootCheckTimer = LOOT_CHECK_INTERVAL;

        // DayTotalEnemies > 0 ensures the spawn system has initialized for this day
        bool noEnemies = EnemySpawnManager.Instance != null
            && EnemySpawnManager.Instance.DayTotalEnemies > 0
            && EnemySpawnManager.Instance.DayEnemiesRemaining == 0;
        bool noLoot = !HasUncollectedLoot();
        bool canSpeedup = noEnemies && noLoot && !IsBuildMode;

        bool wasIdle = IsIdleSpeedup;
        IsIdleSpeedup = canSpeedup;

        if (IsIdleSpeedup && !wasIdle)
            Debug.Log("[BuildModeManager] Idle speedup activated (x3) — no enemies or loot.");
        else if (!IsIdleSpeedup && wasIdle)
            Debug.Log("[BuildModeManager] Idle speedup deactivated.");
    }

    private bool HasUncollectedLoot()
    {
        var pickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (p != null && !p.IsCollected) return true;
        }
        return false;
    }

    private void HandleAllEnemiesCleared()
    {
        enemiesCleared = true;
        Debug.Log("[BuildModeManager] All enemies cleared — press B to enter build mode.");

        // If already night, show hint banner immediately
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
        {
            ShowBuildHint();
        }
    }

    private void HandleNightStarted()
    {
        if (enemiesCleared)
        {
            ShowBuildHint();
        }
    }

    private void ShowBuildHint()
    {
        WallCost = GetWallCost();
        if (HasLivingEngineer() && CanAffordWall())
        {
            string buildKey = InputBindingManager.Instance != null
                ? InputBindingManager.Instance.GetKeyboardDisplayName(GameAction.ToggleBuildMode) : "B";
            GameHUD.ShowBanner($"Press {buildKey} to build walls", HINT_BANNER_DURATION);
            Debug.Log($"[BuildModeManager] Night hint: Press {buildKey} to build walls.");
        }
    }

    private void HandleDayStarted()
    {
        if (IsBuildMode)
        {
            Debug.Log("[BuildModeManager] Dawn arrived — auto-exiting build mode. timeScale=1.");
            // Direct exit without banner — dawn is the signal
            IsBuildMode = false;
            Time.timeScale = 1f;
            OnBuildModeEnded?.Invoke();
        }
        enemiesCleared = false;
        IsIdleSpeedup = false;
        GameHUD.HideBanner();
    }

    public void EnterBuildMode()
    {
        if (IsBuildMode) return;

        // Cache wall cost from the BuildWall UpgradeData
        WallCost = GetWallCost();

        IsBuildMode = true;
        IsIdleSpeedup = false;
        Time.timeScale = 0.1f;
        Debug.Log($"[BuildModeManager] Build mode STARTED. Wall cost={WallCost}g. timeScale=0.1. Press B to exit.");
        OnBuildModeStarted?.Invoke();

        // Auto-start ghost placement
        var wallPlacement = FindAnyObjectByType<WallPlacement>();
        if (wallPlacement != null)
        {
            wallPlacement.StartBuildModeGhost();
            Debug.Log("[BuildModeManager] Ghost placement started automatically.");
        }
    }

    public void ExitBuildMode()
    {
        if (!IsBuildMode) return;
        IsBuildMode = false;
        Time.timeScale = 1f;
        Debug.Log("[BuildModeManager] Build mode ENDED. timeScale=1.");
        GameHUD.ShowBanner("BUILD COMPLETE", EXIT_BANNER_DURATION);
        OnBuildModeEnded?.Invoke();
    }

    /// <summary>Can the player afford the next wall segment?</summary>
    public bool CanAffordWall()
    {
        if (GameManager.Instance == null) return false;
        // Use cached WallCost (updated every frame during build mode and after each placement)
        return GameManager.Instance.Treasure >= WallCost;
    }

    private int GetWallCost()
    {
        // Look up the NewWall UpgradeData to get scaled cost
        if (UpgradeManager.Instance != null)
        {
            foreach (var upgrade in UpgradeManager.Instance.AvailableUpgrades)
            {
                if (upgrade.upgradeType == UpgradeType.NewWall)
                {
                    var (treasure, _) = UpgradeManager.Instance.GetCurrentCost(upgrade);
                    return treasure;
                }
            }
        }
        return 20; // Fallback
    }

    public bool HasLivingEngineer()
    {
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        foreach (var d in defenders)
        {
            if (!d.IsDead && d.Data != null && d.Data.defenderType == DefenderType.Engineer)
                return true;
        }
        return false;
    }

    /// <summary>Called by WallPlacement after placing a wall to increment purchase count and re-check affordability.</summary>
    public void NotifyWallPlaced()
    {
        // Increment the NewWall purchase count so cost scaling works
        if (UpgradeManager.Instance != null)
        {
            foreach (var upgrade in UpgradeManager.Instance.AvailableUpgrades)
            {
                if (upgrade.upgradeType == UpgradeType.NewWall)
                {
                    UpgradeManager.Instance.IncrementPurchaseCountPublic(upgrade.upgradeType);
                    break;
                }
            }
        }

        // Update cached cost for next wall
        WallCost = GetWallCost();
        Debug.Log($"[BuildModeManager] Wall placed. Next wall cost={WallCost}g. Gold={GameManager.Instance?.Treasure}.");
    }
}
