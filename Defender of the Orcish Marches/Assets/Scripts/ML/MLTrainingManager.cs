using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that manages ML training vs human control mode.
/// Controls episode lifecycle, time scale, and mode switching.
/// Has NO dependency on Unity.MLAgents — communicates with agents via IMLAgent interface.
/// </summary>
public class MLTrainingManager : MonoBehaviour
{
    public static MLTrainingManager Instance { get; private set; }

    public enum ControlMode { Human, MLTraining, MLInference }

    [Header("Configuration")]
    [SerializeField] private ControlMode controlMode = ControlMode.Human;
    [SerializeField] private int maxDayForEpisode = 30;
    [SerializeField] private int startDay = 10;

    /// <summary>
    /// Called by MLCurriculumBridge to update max_day from the curriculum parameter.
    /// </summary>
    public void SetMaxDay(int value)
    {
        maxDayForEpisode = value;
        Debug.Log($"[MLTrainingManager] maxDayForEpisode updated to {value} via curriculum.");
    }

    public int StartDay => startDay;

    public ControlMode CurrentMode => controlMode;
    public int MaxDay => maxDayForEpisode;

    // Agent references set at runtime by the agents themselves
    private IMLAgent orcAgent;

    // Episode tracking
    private bool episodeFinished;

    /// <summary>Returns true if ML is controlling the game (training or inference).</summary>
    public static bool IsMLControlled
    {
        get
        {
            if (Instance == null) return false;
            return Instance.controlMode != ControlMode.Human;
        }
    }

    /// <summary>Agents register themselves here on Initialize.</summary>
    public void RegisterOrcAgent(IMLAgent agent)
    {
        orcAgent = agent;
        Debug.Log($"[MLTrainingManager] Orc agent registered: {agent}");
    }

    /// <summary>Returns true only during ML training (not inference or human).</summary>
    public static bool IsTraining
    {
        get
        {
            if (Instance == null) return false;
            return Instance.controlMode == ControlMode.MLTraining;
        }
    }

    /// <summary>Returns true when the OrcCommander brain controls enemy spawning (inference mode).</summary>
    public static bool IsInference
    {
        get
        {
            if (Instance == null) return false;
            return Instance.controlMode == ControlMode.MLInference;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Set difficulty BEFORE GameManager reads it in its own Awake/Start
        if (controlMode == ControlMode.MLTraining)
            GameSettings.CurrentDifficulty = Difficulty.Easy;
        // Survive scene reloads so ML-Agents communicator stays connected
        if (controlMode != ControlMode.Human)
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
        }
        Debug.Log($"[MLTrainingManager] Initialized. Mode={controlMode}");
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (controlMode == ControlMode.MLTraining)
        {
            // Set Easy difficulty for ML training — more gold, more menials, slower spawns
            GameSettings.CurrentDifficulty = Difficulty.Easy;
            Debug.Log($"[MLTrainingManager] Training mode active. Difficulty=Easy");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MLTrainingManager] Scene loaded: {scene.name}. Re-subscribing events.");
        didFirstDaySetup = false; // Reset for new episode
        episodeFinished = false;  // Allow game-over handling for new episode
        if (controlMode == ControlMode.MLTraining)
            GameSettings.CurrentDifficulty = Difficulty.Easy;
        // Re-subscribe after scene reload (singletons are new instances)
        StartCoroutine(ResubscribeAfterFrame());
    }

    private System.Collections.IEnumerator ResubscribeAfterFrame()
    {
        // Wait two frames for singletons to fully initialize (Awake + Start)
        yield return null;
        yield return null;
        SubscribeEvents();

        // Manually trigger first-day setup since OnDayStarted likely already fired
        HandleFirstDaySetup();
        Debug.Log("[MLTrainingManager] Post-reload setup complete.");
    }

    private void SubscribeEvents()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += HandleGameOver;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnNewDay += HandleNewDay;
            DayNightCycle.Instance.OnDayStarted += HandleFirstDaySetup;
        }
    }

    private bool didFirstDaySetup;

    /// <summary>
    /// Called at the start of each episode. No auto-purchases — both agents start from scratch
    /// so the orc can learn to breach and the defender can learn to buy upgrades.
    /// </summary>
    private void HandleFirstDaySetup()
    {
        if (!IsMLControlled || didFirstDaySetup) return;
        didFirstDaySetup = true;
        Debug.Log("[MLTrainingManager] Episode started — no auto-purchases, agents decide everything.");
    }

    private void UnsubscribeEvents()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= HandleGameOver;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnNewDay -= HandleNewDay;
            DayNightCycle.Instance.OnDayStarted -= HandleFirstDaySetup;
        }
    }

    private void HandleGameOver()
    {
        if (!IsMLControlled) return;
        if (episodeFinished) return;
        episodeFinished = true;

        // In inference mode, let the normal game over flow handle it — no reset/rewards
        if (controlMode == ControlMode.MLInference)
        {
            Debug.Log("[MLTrainingManager] Game over in inference mode — orc brain wins!");
            return;
        }

        Debug.Log("[MLTrainingManager] Game over — orc wins! Ending episode.");

        ResetGameInPlace();

        // Orc WIN: MASSIVE reward — must dwarf all mid-game rewards.
        if (orcAgent != null)
        {
            orcAgent.GiveReward(100f);
            orcAgent.FinishEpisode();
        }

        Debug.Log("[MLTrainingManager] Episode ended. Agents will auto-begin new episode.");
    }

    private void HandleNewDay(int dayNumber)
    {
        if (!IsMLControlled) return;

        Debug.Log($"[MLTrainingManager] Day {dayNumber} started.");

        // Explicitly set spawn budget for this day — event subscriptions are unreliable
        // across in-place resets, so we force it here every day.
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.InitSpawnBudgetForDay(dayNumber);

        // In inference mode, just set the spawn budget — no rewards, no episode limits
        if (controlMode == ControlMode.MLInference)
            return;

        // Orc gets a small reward just for the game continuing (it has more time to deal damage)
        if (orcAgent != null)
            orcAgent.GiveReward(0.1f);

        // Check if max day reached — defender wins
        if (dayNumber >= maxDayForEpisode && !episodeFinished)
        {
            episodeFinished = true;
            Debug.Log($"[MLTrainingManager] Max day {maxDayForEpisode} reached — defender wins!");

            ResetGameInPlace();

            // Orc LOSS: negative penalty — the orc must feel losing.
            if (orcAgent != null)
            {
                orcAgent.GiveReward(-5f);
                orcAgent.FinishEpisode();
            }

            Debug.Log("[MLTrainingManager] Episode ended. Agents will auto-begin new episode.");
        }
    }

    /// <summary>
    /// Resets the game state in-place without reloading the scene.
    /// Called BEFORE EndEpisode() so the world is clean when ML-Agents
    /// auto-starts the new episode and collects first observations.
    /// </summary>
    private void ResetGameInPlace()
    {
        // Destroy all spawned enemies
        foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null) Object.Destroy(enemy.gameObject);
        }

        // Destroy all spawned menials
        foreach (var menial in Object.FindObjectsByType<Menial>(FindObjectsSortMode.None))
        {
            if (menial != null) Object.Destroy(menial.gameObject);
        }

        // Destroy all spawned refugees
        foreach (var refugee in Object.FindObjectsByType<Refugee>(FindObjectsSortMode.None))
        {
            if (refugee != null) Object.Destroy(refugee.gameObject);
        }

        // Destroy all spawned defenders
        foreach (var defender in Object.FindObjectsByType<Defender>(FindObjectsSortMode.None))
        {
            if (defender != null) Object.Destroy(defender.gameObject);
        }

        // Destroy all loot pickups
        foreach (var loot in Object.FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None))
        {
            if (loot != null) Object.Destroy(loot.gameObject);
        }

        // Destroy all ballista projectiles
        foreach (var proj in Object.FindObjectsByType<BallistaProjectile>(FindObjectsSortMode.None))
        {
            if (proj != null) Object.Destroy(proj.gameObject);
        }

        // Reset walls — reactivate destroyed walls, restore HP
        if (WallManager.Instance != null)
        {
            foreach (var wall in WallManager.Instance.AllWalls)
            {
                if (wall == null) continue;
                wall.gameObject.SetActive(true);
                wall.Repair(999); // Full heal
            }
        }

        // Reset GameManager state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetForMLEpisode();
        }

        // Reset DayNightCycle to startDay
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ResetToDay(startDay);
        }

        // Reset EnemySpawnManager and explicitly re-init spawn budget for startDay.
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.ResetForNewEpisode();
            EnemySpawnManager.Instance.InitSpawnBudgetForDay(startDay);
        }

        // Reset episode tracking
        didFirstDaySetup = false;
        episodeFinished = false;

        // Reset reward tracker state
        var rewardTracker = FindAnyObjectByType<MLRewardTracker>();
        if (rewardTracker != null)
            rewardTracker.ResetTracking();

        // Re-subscribe events (managers are the same instances, but state was reset)
        SubscribeEvents();

        // Set difficulty
        GameSettings.CurrentDifficulty = Difficulty.Easy;

        // Trigger first-day setup
        HandleFirstDaySetup();

        // Respawn starting menials
        if (MenialManager.Instance != null && GameManager.Instance != null)
        {
            for (int i = 0; i < GameManager.Instance.MenialCount; i++)
                MenialManager.Instance.SpawnMenial();
        }

        // In headless mode, disable renderers on newly spawned objects
        if (HeadlessMLBootstrap.IsHeadless)
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                r.enabled = false;
        }

        Debug.Log("[MLTrainingManager] In-place reset complete.");
    }
}

/// <summary>
/// Interface for ML agents to decouple MLTrainingManager from Unity.MLAgents types.
/// Implemented by OrcCommanderAgent.
/// </summary>
public interface IMLAgent
{
    void GiveReward(float reward);
    void FinishEpisode();
}
