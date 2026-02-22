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

    // Build phase scheduling — defer to sunset
    private bool buildPhaseReady;   // enemies cleared, waiting for sunset
    private bool buildPhaseActive;  // build phase banner showing or build mode active
    private float buildPhaseBannerTimer;
    private const float BUILD_PHASE_BANNER_DURATION = 3f;
    private const float EXIT_BANNER_DURATION = 3f;

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

        // Build phase banner countdown — enter build mode when banner expires
        if (buildPhaseBannerTimer > 0f)
        {
            buildPhaseBannerTimer -= Time.deltaTime;
            if (buildPhaseBannerTimer <= 0f)
            {
                GameHUD.HideBanner();
                if (HasLivingEngineer() && CanAffordWall())
                {
                    EnterBuildMode();
                }
                else
                {
                    if (!HasLivingEngineer())
                        Debug.Log("[BuildModeManager] Build phase — no living engineer, skipping build mode.");
                    else
                        Debug.Log("[BuildModeManager] Build phase — can't afford walls, skipping build mode.");
                }
            }
            return; // don't process build mode inputs during banner
        }

        if (!IsBuildMode) return;

        // B key to exit build mode
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            Debug.Log("[BuildModeManager] B key pressed — exiting build mode.");
            ExitBuildMode();
            return;
        }

        // Keep cached wall cost up to date each frame (for HUD display)
        WallCost = GetWallCost();

        // Auto-exit if player can no longer afford walls
        if (!CanAffordWall())
        {
            Debug.Log("[BuildModeManager] Player can no longer afford walls — auto-exiting build mode.");
            ExitBuildMode();
        }
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
        bool canSpeedup = noEnemies && noLoot && !IsBuildMode && buildPhaseBannerTimer <= 0f;

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
        buildPhaseReady = true;
        Debug.Log("[BuildModeManager] All enemies cleared — build phase ready, waiting for sunset.");

        // If already night, start build phase now (enemies retreated during night)
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
        {
            StartBuildPhase();
        }
    }

    private void HandleNightStarted()
    {
        if (buildPhaseReady)
        {
            StartBuildPhase();
        }
    }

    private void StartBuildPhase()
    {
        if (buildPhaseActive) return;
        buildPhaseActive = true;
        IsIdleSpeedup = false;
        buildPhaseBannerTimer = BUILD_PHASE_BANNER_DURATION;
        GameHUD.ShowBanner("BUILD PHASE");
        Debug.Log($"[BuildModeManager] Build phase banner shown — build mode starts in {BUILD_PHASE_BANNER_DURATION}s.");
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
        buildPhaseReady = false;
        buildPhaseActive = false;
        buildPhaseBannerTimer = 0f;
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
        buildPhaseReady = false;
        buildPhaseActive = false;
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
