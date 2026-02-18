using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button restartButton;

    private bool subscribed;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        TrySubscribe();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnEnable()
    {
        subscribed = false;
        TrySubscribe();

        // Re-add button listener (lost after domain reload)
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += ShowGameOver;
            subscribed = true;

            // If game is already over (e.g. after domain reload), show screen immediately
            if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            {
                ShowGameOver();
            }
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOver;
        }
        subscribed = false;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }

    private void ShowGameOver()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "WALLS BREACHED!\nGAME OVER";

        if (statsText != null && GameManager.Instance != null)
        {
            float time = GameManager.Instance.GameTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            statsText.text = string.Format(
                "Survival Time: {0}:{1:00}\nGold Collected: {2}",
                minutes, seconds, GameManager.Instance.Treasure);
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("[GameOverScreen] Restart clicked.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}
