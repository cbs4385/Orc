using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI treasureText;
    [SerializeField] private TextMeshProUGUI menialText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseButtonText;

    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
    }

    private void OnDisable()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseClicked);
    }

    private void TrySubscribe()
    {
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged += UpdateTreasure;
            GameManager.Instance.OnMenialsChanged += UpdateMenials;
            GameManager.Instance.OnPauseChanged += UpdatePauseButton;
            subscribed = true;

            // Force initial display update
            UpdateTreasure(GameManager.Instance.Treasure);
            UpdateMenials(GameManager.Instance.MenialCount);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged -= UpdateTreasure;
            GameManager.Instance.OnMenialsChanged -= UpdateMenials;
            GameManager.Instance.OnPauseChanged -= UpdatePauseButton;
        }
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        if (GameManager.Instance == null) return;

        // SPACE to toggle pause
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GameManager.Instance.TogglePause();
        }

        // Update timer
        if (timerText != null)
        {
            float time = GameManager.Instance.GameTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }

        // Update enemy count
        if (enemyCountText != null && EnemySpawnManager.Instance != null)
        {
            enemyCountText.text = "Enemies: " + EnemySpawnManager.Instance.GetActiveEnemyCount();
        }
    }

    private void UpdateTreasure(int amount)
    {
        if (treasureText != null)
            treasureText.text = "Gold: " + amount;
    }

    private void UpdateMenials(int amount)
    {
        if (menialText != null)
        {
            int idle = GameManager.Instance != null ? GameManager.Instance.IdleMenialCount : 0;
            menialText.text = string.Format("Menials: {0}/{1}", idle, amount);
        }
    }

    private void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
    }

    private void UpdatePauseButton(bool isPaused)
    {
        if (pauseButtonText != null)
            pauseButtonText.text = isPaused ? "PLAY [Space]" : "PAUSE [Space]";
    }
}
