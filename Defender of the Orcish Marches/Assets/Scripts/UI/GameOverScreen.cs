using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    private SceneLoader sceneLoader;
    private bool subscribed;

    private void Start()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();

        if (panelRoot != null) panelRoot.SetActive(false);
        TrySubscribe();

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnEnable()
    {
        subscribed = false;
        TrySubscribe();

        // Re-add button listeners (lost after domain reload)
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
            exitButton.onClick.AddListener(OnExitClicked);
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

    private const string NEW_BEST = " <color=#FFD700>NEW BEST!</color>";

    private void ShowGameOver()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "WALLS BREACHED!\nGAME OVER";

        // Get previous bests BEFORE saving this run
        var prevBest = RunHistoryManager.GetBestByCategory();
        bool hasPrevRuns = RunHistoryManager.GetRunCount() > 0;

        // Build current run record
        RunRecord current;
        if (RunStatsTracker.Instance != null)
        {
            current = RunStatsTracker.Instance.ToRecord();
        }
        else
        {
            // Fallback if tracker is missing
            int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.EnemyKills : 0;
            current = new RunRecord
            {
                days = dayNumber,
                kills = kills,
                compositeScore = (dayNumber * 1000) + (kills * 10)
            };
        }

        // Save and get rank
        int rank = RunHistoryManager.SaveRun(current);

        // Build stats display
        if (statsText != null)
        {
            var sb = new System.Text.StringBuilder();

            float time = GameManager.Instance != null ? GameManager.Instance.GameTime : 0;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);

            sb.AppendFormat("Survival Time: {0}:{1:00}\n", minutes, seconds);
            sb.AppendFormat("Days Survived: {0}", current.days);
            if (hasPrevRuns && current.days > prevBest.days) sb.Append(NEW_BEST);
            sb.AppendLine();

            sb.AppendFormat("Enemies Killed: {0}", current.kills);
            if (hasPrevRuns && current.kills > prevBest.kills) sb.Append(NEW_BEST);
            sb.AppendLine();

            if (current.bossKills > 0 || (hasPrevRuns && prevBest.bossKills > 0))
            {
                sb.AppendFormat("Bosses Slain: {0}", current.bossKills);
                if (hasPrevRuns && current.bossKills > prevBest.bossKills) sb.Append(NEW_BEST);
                sb.AppendLine();
            }

            sb.AppendFormat("Gold Earned: {0}", current.goldEarned);
            if (hasPrevRuns && current.goldEarned > prevBest.goldEarned) sb.Append(NEW_BEST);
            sb.AppendLine();

            sb.AppendFormat("Hirelings Recruited: {0}", current.hires);
            if (hasPrevRuns && current.hires > prevBest.hires) sb.Append(NEW_BEST);
            sb.AppendLine();

            sb.AppendFormat("Menials Lost: {0}", current.menialsLost);
            if (hasPrevRuns && current.menialsLost < prevBest.menialsLost) sb.Append(NEW_BEST);

            statsText.text = sb.ToString();
        }

        // Build score display
        if (scoreText != null)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendFormat("<size=130%>SCORE: {0:N0}</size>", current.compositeScore);
            if (hasPrevRuns && current.compositeScore > prevBest.compositeScore) sb.Append(NEW_BEST);
            sb.AppendLine();

            if (rank >= 0)
                sb.AppendFormat("Rank #{0} of {1} runs", rank + 1, RunHistoryManager.GetRunCount());

            if (hasPrevRuns && current.compositeScore <= prevBest.compositeScore)
                sb.AppendFormat("\nBest: {0:N0}", prevBest.compositeScore);

            scoreText.text = sb.ToString();
        }

        Debug.Log($"[GameOverScreen] Game over. Score={current.compositeScore}, Rank={rank + 1}, Days={current.days}, Kills={current.kills}, BossKills={current.bossKills}, Gold={current.goldEarned}, Hires={current.hires}, MenialsLost={current.menialsLost}");
    }

    private void OnRestartClicked()
    {
        Debug.Log("[GameOverScreen] Restart clicked.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnExitClicked()
    {
        Debug.Log("[GameOverScreen] Exit clicked — returning to main menu.");
        if (sceneLoader != null)
            sceneLoader.LoadMainMenu();
    }
}
