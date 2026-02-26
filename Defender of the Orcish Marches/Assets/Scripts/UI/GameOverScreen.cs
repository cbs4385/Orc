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

    [Header("Meta-Progression")]
    [SerializeField] private MilestoneNotificationUI milestoneNotification;

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

        // Record lifetime stats (counts ALL runs, not just top 20)
        float gameTime = GameManager.Instance != null ? GameManager.Instance.GameTime : 0;
        int peakDef = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.PeakDefendersAlive : 0;
        float firstBoss = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.FirstBossKillTime : 0;
        LifetimeStatsManager.RecordRunEnd(current, GameSettings.CurrentDifficulty, gameTime, peakDef, firstBoss);

        // Record bestiary kills
        BestiaryManager.RecordRunKills(current);

        // Award War Trophies
        int trophiesEarned = MetaProgressionManager.CalculateRunTrophies(current);
        MetaProgressionManager.AwardTrophies(trophiesEarned);

        // Check milestones
        int relicsCollected = RelicManager.Instance != null ? RelicManager.Instance.CollectedCount : 0;
        var newMilestones = MilestoneManager.CheckRunMilestones(current, GameSettings.CurrentDifficulty, relicsCollected);

        // Show milestone notifications
        if (milestoneNotification != null)
        {
            milestoneNotification.ShowMilestones(newMilestones, trophiesEarned);
        }

        // Evaluate achievements
        var newAchievements = AchievementManager.EvaluateRunEnd(current, GameSettings.CurrentDifficulty);

        // Award legacy points
        int legacyEarned = LegacyProgressionManager.AddPointsFromScore(current.compositeScore);

        // Save and get rank
        int rank = RunHistoryManager.SaveRun(current);

        // Build stats display — compact two-column layout
        if (statsText != null)
        {
            var sb = new System.Text.StringBuilder();

            float time = GameManager.Instance != null ? GameManager.Instance.GameTime : 0;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);

            sb.AppendFormat("{0}:{1:00}  |  Days: {2}", minutes, seconds, current.days);
            if (hasPrevRuns && current.days > prevBest.days) sb.Append(NEW_BEST);
            sb.AppendLine();

            sb.AppendFormat("Kills: {0}", current.kills);
            if (hasPrevRuns && current.kills > prevBest.kills) sb.Append(NEW_BEST);
            if (current.bossKills > 0)
            {
                sb.AppendFormat("  |  Bosses: {0}", current.bossKills);
                if (hasPrevRuns && current.bossKills > prevBest.bossKills) sb.Append(NEW_BEST);
            }
            sb.AppendLine();

            sb.AppendFormat("Gold: {0}", current.goldEarned);
            if (hasPrevRuns && current.goldEarned > prevBest.goldEarned) sb.Append(NEW_BEST);
            sb.AppendFormat("  |  Hires: {0}", current.hires);
            sb.AppendFormat("  |  Lost: {0}", current.menialsLost);

            statsText.text = sb.ToString();
        }

        // Build score display — condensed to fit
        if (scoreText != null)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendFormat("<size=130%>SCORE: {0:N0}</size>", current.compositeScore);
            if (hasPrevRuns && current.compositeScore > prevBest.compositeScore) sb.Append(NEW_BEST);
            sb.AppendLine();

            // Rank and best on one line
            if (rank >= 0)
            {
                sb.AppendFormat("<size=85%>Rank #{0}/{1}", rank + 1, RunHistoryManager.GetRunCount());
                if (hasPrevRuns && current.compositeScore <= prevBest.compositeScore)
                    sb.AppendFormat("  |  Best: {0:N0}", prevBest.compositeScore);
                sb.Append("</size>");
            }

            // Modifiers line — mutators, commander, relics all on one line
            var modParts = new System.Collections.Generic.List<string>();
            if (MutatorManager.ActiveCount > 0)
                modParts.Add($"Mutators ({MutatorManager.GetScoreMultiplier():F1}x)");
            if (CommanderManager.HasCommander)
                modParts.Add(CommanderManager.GetActiveDisplayName());
            if (RelicManager.Instance != null && RelicManager.Instance.CollectedCount > 0)
            {
                string relicStr = $"Relics x{RelicManager.Instance.CollectedCount}";
                if (RelicManager.Instance.ActiveSynergyCount > 0)
                    relicStr += $" <color=#FFD700>[{RelicManager.Instance.ActiveSynergyCount} Synergy]</color>";
                modParts.Add(relicStr);
            }
            if (modParts.Count > 0)
                sb.AppendFormat("\n<size=80%>{0}</size>", string.Join("  |  ", modParts));

            // Legacy points
            if (legacyEarned > 0)
                sb.AppendFormat("\n<size=85%><color=#B89030>+{0} Legacy</color> ({1})</size>", legacyEarned, LegacyProgressionManager.GetCurrentRankTitle());

            // Achievements — inline, capped at 3 on one line
            if (newAchievements.Count > 0)
            {
                sb.Append("\n<size=85%>");
                int shown = 0;
                foreach (var (achId, achTier) in newAchievements)
                {
                    if (shown >= 3) break;
                    var achDef = AchievementDefs.GetById(achId);
                    if (achDef != null)
                    {
                        string tierColor = achTier == AchievementTier.Gold ? "#FFD700" :
                                           achTier == AchievementTier.Silver ? "#C0C0CC" : "#CD7F32";
                        if (shown > 0) sb.Append("  ");
                        sb.AppendFormat("<color={0}>[{1}]</color> {2}", tierColor, achTier, achDef.Value.name);
                        shown++;
                    }
                }
                if (newAchievements.Count > 3)
                    sb.AppendFormat(" +{0} more", newAchievements.Count - 3);
                sb.Append("</size>");
            }

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
