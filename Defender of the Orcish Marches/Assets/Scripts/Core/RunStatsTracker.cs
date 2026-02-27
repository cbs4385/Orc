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

    // Extended stats
    public int GoldSpent { get; private set; }
    public int WallsBuilt { get; private set; }
    public int WallHPRepaired { get; private set; }
    public int VegetationCleared { get; private set; }
    public int RefugeesSaved { get; private set; }
    public int BallistaShotsFired { get; private set; }
    public int PeakDefendersAlive { get; private set; }
    public float FirstBossKillTime { get; private set; }

    // Per-type kills
    public int KillsMelee { get; private set; }
    public int KillsRanged { get; private set; }
    public int KillsWallBreaker { get; private set; }
    public int KillsSuicide { get; private set; }
    public int KillsArtillery { get; private set; }

    // Per-enemy-name kills (individual enemy identities)
    public int KillsOrcGrunt { get; private set; }
    public int KillsBowOrc { get; private set; }
    public int KillsTroll { get; private set; }
    public int KillsSuicideGoblin { get; private set; }
    public int KillsGoblinCannoneer { get; private set; }
    public int KillsOrcWarBoss { get; private set; }

    // Per-type hires
    public int HiresEngineer { get; private set; }
    public int HiresPikeman { get; private set; }
    public int HiresCrossbowman { get; private set; }
    public int HiresWizard { get; private set; }

    private bool subscribedGM;
    private bool subscribedDNC;
    private bool subscribedUpg;
    private float peakDefenderCheckTimer;

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
        WallManager.OnWallBuilt += HandleWallBuilt;
        Wall.OnWallRepaired += HandleWallRepaired;
        Ballista.OnBallistaShotFired += HandleBallistaShotFired;
        Refugee.OnRefugeeSaved += HandleRefugeeSaved;
        Vegetation.OnVegetationCleared += HandleVegetationCleared;

        subscribedGM = false;
        subscribedDNC = false;
        subscribedUpg = false;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
        Menial.OnMenialDied -= HandleMenialDied;
        WallManager.OnWallBuilt -= HandleWallBuilt;
        Wall.OnWallRepaired -= HandleWallRepaired;
        Ballista.OnBallistaShotFired -= HandleBallistaShotFired;
        Refugee.OnRefugeeSaved -= HandleRefugeeSaved;
        Vegetation.OnVegetationCleared -= HandleVegetationCleared;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureGained -= HandleTreasureGained;
            GameManager.Instance.OnTreasureSpent -= HandleTreasureSpent;
        }
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
            GameManager.Instance.OnTreasureSpent += HandleTreasureSpent;
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

        // Track peak defenders alive every 2 seconds
        peakDefenderCheckTimer -= Time.deltaTime;
        if (peakDefenderCheckTimer <= 0)
        {
            peakDefenderCheckTimer = 2f;
            int currentDefenders = CountLiveDefenders();
            if (currentDefenders > PeakDefendersAlive)
                PeakDefendersAlive = currentDefenders;
        }
    }

    private int CountLiveDefenders()
    {
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var d in defenders)
        {
            if (d != null && !d.IsDead) count++;
        }
        return count;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        Kills++;

        // Per-type kill tracking
        if (enemy.Data != null)
        {
            switch (enemy.Data.enemyType)
            {
                case EnemyType.Melee: KillsMelee++; break;
                case EnemyType.Ranged: KillsRanged++; break;
                case EnemyType.WallBreaker: KillsWallBreaker++; break;
                case EnemyType.Suicide: KillsSuicide++; break;
                case EnemyType.Artillery: KillsArtillery++; break;
            }

            // Per-enemy-name kill tracking
            switch (enemy.Data.enemyName)
            {
                case "Orc Grunt": KillsOrcGrunt++; break;
                case "Bow Orc": KillsBowOrc++; break;
                case "Troll": KillsTroll++; break;
                case "Suicide Goblin": KillsSuicideGoblin++; break;
                case "Goblin Cannoneer": KillsGoblinCannoneer++; break;
                case "Orc War Boss": KillsOrcWarBoss++; break;
            }

            if (enemy.Data.enemyName.Contains("Boss"))
            {
                BossKills++;
                // Record first boss kill time
                if (FirstBossKillTime <= 0 && GameManager.Instance != null)
                {
                    FirstBossKillTime = GameManager.Instance.GameTime;
                    Debug.Log($"[RunStatsTracker] First boss killed at {FirstBossKillTime:F1}s!");
                }
                Debug.Log($"[RunStatsTracker] Boss killed! Total boss kills: {BossKills}");
            }
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

    private void HandleTreasureSpent(int amount)
    {
        GoldSpent += amount;
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

            // Per-type hire tracking
            switch (upgrade.upgradeType)
            {
                case UpgradeType.SpawnEngineer: HiresEngineer++; break;
                case UpgradeType.SpawnPikeman: HiresPikeman++; break;
                case UpgradeType.SpawnCrossbowman: HiresCrossbowman++; break;
                case UpgradeType.SpawnWizard: HiresWizard++; break;
            }

            Debug.Log($"[RunStatsTracker] Hire made: {upgrade.upgradeName}. Total hires: {Hires}");
        }
    }

    private void HandleWallBuilt()
    {
        WallsBuilt++;
    }

    private void HandleWallRepaired(int amount)
    {
        WallHPRepaired += amount;
    }

    private void HandleBallistaShotFired()
    {
        BallistaShotsFired++;
    }

    private void HandleRefugeeSaved()
    {
        RefugeesSaved++;
    }

    private void HandleVegetationCleared()
    {
        VegetationCleared++;
    }

    /// <summary>Restore all stats from a save file.</summary>
    public void RestoreState(SaveSlotData data)
    {
        var rs = data.runStats;
        if (rs == null) return;

        Days = rs.days;
        Kills = rs.kills;
        BossKills = rs.bossKills;
        GoldEarned = rs.goldEarned;
        Hires = rs.hires;
        MenialsLost = rs.menialsLost;
        GoldSpent = rs.goldSpent;
        WallsBuilt = rs.wallsBuilt;
        WallHPRepaired = rs.wallHPRepaired;
        VegetationCleared = rs.vegetationCleared;
        RefugeesSaved = rs.refugeesSaved;
        BallistaShotsFired = rs.ballistaShotsFired;
        PeakDefendersAlive = rs.peakDefendersAlive;
        FirstBossKillTime = rs.firstBossKillTime;

        KillsMelee = rs.killsMelee;
        KillsRanged = rs.killsRanged;
        KillsWallBreaker = rs.killsWallBreaker;
        KillsSuicide = rs.killsSuicide;
        KillsArtillery = rs.killsArtillery;

        KillsOrcGrunt = rs.killsOrcGrunt;
        KillsBowOrc = rs.killsBowOrc;
        KillsTroll = rs.killsTroll;
        KillsSuicideGoblin = rs.killsSuicideGoblin;
        KillsGoblinCannoneer = rs.killsGoblinCannoneer;
        KillsOrcWarBoss = rs.killsOrcWarBoss;

        HiresEngineer = rs.hiresEngineer;
        HiresPikeman = rs.hiresPikeman;
        HiresCrossbowman = rs.hiresCrossbowman;
        HiresWizard = rs.hiresWizard;

        Debug.Log($"[RunStatsTracker] Restored: days={Days}, kills={Kills}, gold={GoldEarned}");
    }

    public int ComputeScore()
    {
        int baseScore = (Days * 1000) + (Kills * 10) + (GoldEarned * 2) + (Hires * 50) + (BossKills * 500) - (MenialsLost * 100);
        float mutatorMult = MutatorManager.GetScoreMultiplier();
        return Mathf.RoundToInt(baseScore * mutatorMult);
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
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),

            // Extended stats
            difficulty = (int)GameSettings.CurrentDifficulty,
            goldSpent = GoldSpent,
            wallsBuilt = WallsBuilt,
            wallHPRepaired = WallHPRepaired,
            vegetationCleared = VegetationCleared,
            refugeesSaved = RefugeesSaved,
            ballistaShotsFired = BallistaShotsFired,
            peakDefendersAlive = PeakDefendersAlive,
            firstBossKillTime = FirstBossKillTime,

            // Per-type kills
            killsMelee = KillsMelee,
            killsRanged = KillsRanged,
            killsWallBreaker = KillsWallBreaker,
            killsSuicide = KillsSuicide,
            killsArtillery = KillsArtillery,

            // Per-type hires
            hiresEngineer = HiresEngineer,
            hiresPikeman = HiresPikeman,
            hiresCrossbowman = HiresCrossbowman,
            hiresWizard = HiresWizard,

            // Commander and relics
            commanderId = CommanderManager.ActiveCommanderId,
            relicsCollected = RelicManager.Instance != null ? RelicManager.Instance.CollectedCount : 0,
            warTrophiesEarned = MetaProgressionManager.CalculateRunTrophies(new RunRecord { days = Days, kills = Kills, bossKills = BossKills }),

            // Per-enemy-name kills
            killsOrcGrunt = KillsOrcGrunt,
            killsBowOrc = KillsBowOrc,
            killsTroll = KillsTroll,
            killsSuicideGoblin = KillsSuicideGoblin,
            killsGoblinCannoneer = KillsGoblinCannoneer,
            killsOrcWarBoss = KillsOrcWarBoss
        };
    }
}
