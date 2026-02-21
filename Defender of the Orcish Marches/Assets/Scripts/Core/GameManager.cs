using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>World-space center of the fortress. All fortress-relative calculations use this.</summary>
    public static readonly Vector3 FortressCenter = new Vector3(15f, 0f, 0f);

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
            GameTime += Time.deltaTime;
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
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Unpaused.");
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
}
