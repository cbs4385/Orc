using System;
using System.Collections.Generic;

/// <summary>
/// All serializable data classes for the mid-game save system.
/// Position stored as float[3], rotation as float[4] for JsonUtility compatibility.
/// </summary>

[Serializable]
public class SaveSlotData
{
    // Metadata (for slot picker display)
    public int slotIndex;
    public string timestamp;
    public int metaDayNumber;
    public int metaDifficulty;
    public int metaTreasure;

    // Core game state
    public int treasure;
    public int menialCount;
    public int idleMenialCount;
    public float gameTime;
    public int enemyKills;

    // Day/Night cycle
    public int dayNumber;
    public int phase; // 0=Day, 1=Night
    public float phaseTimer;
    public bool isFirstDay;

    // Spawn state
    public int regularSpawnsRemaining;
    public int dayTotalEnemies;
    public int dayKills;
    public bool bossSpawnedThisDay;
    public bool clearedFiredThisDay;
    public float spawnTimer;
    public float dawnGraceTimer;
    public List<string> remnantEnemyNames = new List<string>();

    // Entities
    public List<SavedWall> walls = new List<SavedWall>();
    public List<SavedBallista> ballistas = new List<SavedBallista>();
    public int activeBallistaIndex;
    public List<SavedDefender> defenders = new List<SavedDefender>();
    public List<SavedEnemy> enemies = new List<SavedEnemy>();
    public List<SavedMenial> menials = new List<SavedMenial>();
    public List<SavedLoot> loot = new List<SavedLoot>();

    // Upgrades
    public List<SavedUpgradeCount> upgradeCounts = new List<SavedUpgradeCount>();

    // Mutators, commander, relics
    public List<string> activeMutatorIds = new List<string>();
    public string commanderId;
    public List<string> collectedRelicIds = new List<string>();
    public List<string> activeSynergyIds = new List<string>();

    // Daily event
    public int lastEventIndex;
    public float lootValueMultiplier;
    public float defenderDamageMultiplier;
    public float menialSpeedMultiplier;
    public float enemyDamageMultiplier;
    public float spawnRateMultiplier;
    public float enemyHPMultiplier;
    public float enemySpeedMultiplier;
    public float defenderAttackSpeedMultiplier;
    public string eventName;
    public string eventDescription;
    public int eventCategory;
    public bool hasActiveEvent;

    // Run stats
    public SavedRunStats runStats = new SavedRunStats();

    // Difficulty
    public int difficulty;

    // Build mode wall count (for wall cost scaling)
    public int buildModeWallCount;
}

[Serializable]
public class SavedWall
{
    public float[] position; // x,y,z
    public float[] rotation; // x,y,z,w
    public float scaleX;
    public int currentHP;
    public int maxHP;
    public bool isUnderConstruction;
}

[Serializable]
public class SavedBallista
{
    public float[] position;
    public float[] rotation;
    public int damage;
    public float fireRate;
    public bool hasDoubleShot;
    public bool hasBurstDamage;
}

[Serializable]
public class SavedDefender
{
    public string typeName; // e.g. "Engineer", "Pikeman", "Crossbowman", "Wizard"
    public float[] position;
    public float[] rotation;
    public int currentHP;
}

[Serializable]
public class SavedEnemy
{
    public string dataName; // EnemyData.enemyName to look up the data asset
    public float[] position;
    public float[] rotation;
    public int currentHP;
    public int scaledDamage;
    public bool isRetreating;
}

[Serializable]
public class SavedMenial
{
    public float[] position;
    public int currentHP;
    public int carriedTreasure;
}

[Serializable]
public class SavedLoot
{
    public float[] position;
    public int value;
    public float spawnGameTime;
}

[Serializable]
public class SavedUpgradeCount
{
    public string typeName; // UpgradeType enum name
    public int count;
}

[Serializable]
public class SavedRunStats
{
    public int days;
    public int kills;
    public int bossKills;
    public int goldEarned;
    public int hires;
    public int menialsLost;
    public int goldSpent;
    public int wallsBuilt;
    public int wallHPRepaired;
    public int vegetationCleared;
    public int refugeesSaved;
    public int ballistaShotsFired;
    public int peakDefendersAlive;
    public float firstBossKillTime;

    // Per-type kills
    public int killsMelee;
    public int killsRanged;
    public int killsWallBreaker;
    public int killsSuicide;
    public int killsArtillery;

    // Per-enemy-name kills
    public int killsOrcGrunt;
    public int killsBowOrc;
    public int killsTroll;
    public int killsSuicideGoblin;
    public int killsGoblinCannoneer;
    public int killsOrcWarBoss;

    // Per-type hires
    public int hiresEngineer;
    public int hiresPikeman;
    public int hiresCrossbowman;
    public int hiresWizard;
}
