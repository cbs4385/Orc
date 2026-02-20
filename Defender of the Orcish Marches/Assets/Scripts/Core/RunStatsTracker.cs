using UnityEngine;

/// <summary>
/// Tracks per-run statistics by subscribing to game events.
/// Lives on a scene GameObject; resets each game.
/// </summary>
public class RunStatsTracker : MonoBehaviour
{
    public static RunStatsTracker Instance { get; private set; }

    public int Days { get; private set; }
    public int Kills { get; private set; }
    public int BossKills { get; private set; }
    public int GoldEarned { get; private set; }
    public int Hires { get; private set; }
    public int MenialsLost { get; private set; }

    private bool subscribedGM;
    private bool subscribedDNC;
    private bool subscribedUpg;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Days = 1; // Start on day 1
        Debug.Log("[RunStatsTracker] Instance set in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[RunStatsTracker] Instance re-registered in OnEnable after domain reload.");
        }

        Enemy.OnEnemyDied += HandleEnemyDied;
        Menial.OnMenialDied += HandleMenialDied;

        subscribedGM = false;
        subscribedDNC = false;
        subscribedUpg = false;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
        Menial.OnMenialDied -= HandleMenialDied;

        if (GameManager.Instance != null)
            GameManager.Instance.OnTreasureGained -= HandleTreasureGained;
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnNewDay -= HandleNewDay;
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Late-subscribe to singletons that may initialize after us
        if (!subscribedGM && GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureGained += HandleTreasureGained;
            subscribedGM = true;
        }
        if (!subscribedDNC && DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnNewDay += HandleNewDay;
            subscribedDNC = true;
        }
        if (!subscribedUpg && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
            subscribedUpg = true;
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        Kills++;
        if (enemy.Data != null && enemy.Data.enemyName.Contains("Boss"))
        {
            BossKills++;
            Debug.Log($"[RunStatsTracker] Boss killed! Total boss kills: {BossKills}");
        }
    }

    private void HandleMenialDied()
    {
        MenialsLost++;
        Debug.Log($"[RunStatsTracker] Menial lost. Total lost: {MenialsLost}");
    }

    private void HandleTreasureGained(int amount)
    {
        GoldEarned += amount;
    }

    private void HandleNewDay(int dayNumber)
    {
        Days = dayNumber;
        Debug.Log($"[RunStatsTracker] New day: {Days}");
    }

    private void HandleUpgradePurchased(UpgradeData upgrade)
    {
        if (upgrade.upgradeType == UpgradeType.SpawnEngineer ||
            upgrade.upgradeType == UpgradeType.SpawnPikeman ||
            upgrade.upgradeType == UpgradeType.SpawnCrossbowman ||
            upgrade.upgradeType == UpgradeType.SpawnWizard)
        {
            Hires++;
            Debug.Log($"[RunStatsTracker] Hire made: {upgrade.upgradeName}. Total hires: {Hires}");
        }
    }

    public int ComputeScore()
    {
        return (Days * 1000) + (Kills * 10) + (GoldEarned * 2) + (Hires * 50) + (BossKills * 500) - (MenialsLost * 100);
    }

    public RunRecord ToRecord()
    {
        return new RunRecord
        {
            days = Days,
            kills = Kills,
            bossKills = BossKills,
            goldEarned = GoldEarned,
            hires = Hires,
            menialsLost = MenialsLost,
            compositeScore = ComputeScore(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
    }
}
