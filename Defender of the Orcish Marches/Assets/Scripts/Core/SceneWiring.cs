using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneWiring : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Create ScriptableObjects")]
    public static void CreateScriptableObjects()
    {
        // Enemy Data
        CreateEnemyData("OrcGrunt", "Orc Grunt", EnemyType.Melee, 30, 3f, 5, 1.5f, 1f, 10, new Color(0.27f, 0.67f, 0.27f), Vector3.one);
        CreateEnemyData("BowOrc", "Bow Orc", EnemyType.Ranged, 20, 2.5f, 8, 8f, 0.8f, 15, new Color(0.2f, 0.53f, 0.2f), Vector3.one);
        CreateEnemyData("Troll", "Troll", EnemyType.WallBreaker, 150, 1.5f, 30, 2f, 0.5f, 40, new Color(0.13f, 0.4f, 0.13f), new Vector3(1.5f, 1.5f, 1.5f));
        CreateEnemyData("SuicideGoblin", "Suicide Goblin", EnemyType.Suicide, 15, 5f, 80, 1f, 1f, 5, new Color(0.53f, 0.67f, 0.13f), new Vector3(0.7f, 0.7f, 0.7f));
        CreateEnemyData("GoblinCannoneer", "Goblin Cannoneer", EnemyType.Artillery, 40, 1f, 50, 15f, 0.3f, 30, new Color(0.2f, 0.4f, 0.2f), Vector3.one);
        CreateEnemyData("OrcWarBoss", "Orc War Boss", EnemyType.Melee, 500, 1.5f, 50, 2.5f, 0.5f, 100, new Color(0.6f, 0.15f, 0.1f), new Vector3(2.5f, 2.5f, 2.5f));

        // Set boss loot drops (higher than default)
        var bossAsset = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/OrcWarBoss.asset");
        if (bossAsset != null)
        {
            bossAsset.minLootDrops = 5;
            bossAsset.maxLootDrops = 8;
            bossAsset.minLootValue = 10;
            bossAsset.maxLootValue = 25;
            EditorUtility.SetDirty(bossAsset);
        }

        // Defender Data
        CreateDefenderData("Engineer", DefenderType.Engineer, 2, 30, 0, 2f, 1f, new Color(0.2f, 0.4f, 0.8f));
        CreateDefenderData("Pikeman", DefenderType.Pikeman, 2, 40, 15, 3f, 1.2f, new Color(0.2f, 0.4f, 0.8f));
        CreateDefenderData("Crossbowman", DefenderType.Crossbowman, 2, 50, 12, 8f, 0.8f, new Color(0.2f, 0.4f, 0.8f));
        CreateDefenderData("Wizard", DefenderType.Wizard, 3, 100, 25, 12f, 0.5f, new Color(0.53f, 0.27f, 0.8f));

        // Upgrade Data (costScaling: each purchase adds baseCost * scaling to the price)
        CreateUpgradeData("HireEngineer", "Hire Engineer", "Engineer patrols and repairs walls", 30, 2, UpgradeType.SpawnEngineer, 0.25f);
        CreateUpgradeData("HirePikeman", "Hire Pikeman", "Melee defender on walls", 40, 2, UpgradeType.SpawnPikeman, 0.25f);
        CreateUpgradeData("HireCrossbowman", "Hire Crossbowman", "Ranged defender on walls", 50, 2, UpgradeType.SpawnCrossbowman, 0.25f);
        CreateUpgradeData("HireWizard", "Hire Wizard", "AoE spell caster on walls", 100, 3, UpgradeType.SpawnWizard, 0.25f);
        CreateUpgradeData("UpgBallistaDmg", "Ballista Damage+", "Increase ballista damage by 10", 60, 0, UpgradeType.BallistaDamage, 0.5f);
        CreateUpgradeData("UpgBallistaRate", "Ballista Speed+", "Increase fire rate", 50, 0, UpgradeType.BallistaFireRate, 0.5f);
        CreateUpgradeData("BuildWall", "Build Wall", "Place a new wall segment", 20, 0, UpgradeType.NewWall, 0.2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All ScriptableObjects created!");
    }

    static void CreateEnemyData(string fileName, string name, EnemyType type, int hp, float speed, int damage, float range, float atkRate, int treasure, Color color, Vector3 scale)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = name;
        data.enemyType = type;
        data.maxHP = hp;
        data.moveSpeed = speed;
        data.damage = damage;
        data.attackRange = range;
        data.attackRate = atkRate;
        data.treasureDrop = treasure;
        data.bodyColor = color;
        data.bodyScale = scale;

        // Assign projectile prefab for ranged types
        if (type == EnemyType.Ranged || type == EnemyType.Artillery)
        {
            data.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/EnemyProjectile.prefab");
        }

        AssetDatabase.CreateAsset(data, "Assets/ScriptableObjects/Enemies/" + fileName + ".asset");
    }

    static void CreateDefenderData(string fileName, DefenderType type, int menialCost, int treasureCost, int damage, float range, float atkRate, Color color)
    {
        var data = ScriptableObject.CreateInstance<DefenderData>();
        data.defenderName = fileName;
        data.defenderType = type;
        data.menialCost = menialCost;
        data.treasureCost = treasureCost;
        data.damage = damage;
        data.range = range;
        data.attackRate = atkRate;
        data.bodyColor = color;
        AssetDatabase.CreateAsset(data, "Assets/ScriptableObjects/Defenders/" + fileName + ".asset");
    }

    static void CreateUpgradeData(string fileName, string name, string desc, int treasureCost, int menialCost, UpgradeType type, float costScaling = 0f)
    {
        var data = ScriptableObject.CreateInstance<UpgradeData>();
        data.upgradeName = name;
        data.description = desc;
        data.treasureCost = treasureCost;
        data.menialCost = menialCost;
        data.upgradeType = type;
        data.repeatable = true;
        data.costScaling = costScaling;
        AssetDatabase.CreateAsset(data, "Assets/ScriptableObjects/Upgrades/" + fileName + ".asset");
    }

    [MenuItem("Game/Wire Scene")]
    public static void WireScene()
    {
        // Load prefabs
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy.prefab");
        var treasurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot/TreasurePickup.prefab");
        var menialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Menial.prefab");
        var refugeePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Refugee.prefab");
        var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/BallistaProjectile.prefab");
        var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fortress/WallSegment.prefab");

        // Load enemy data
        var orcGrunt = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/OrcGrunt.asset");
        var bowOrc = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/BowOrc.asset");
        var troll = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/Troll.asset");
        var suicideGoblin = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/SuicideGoblin.asset");
        var goblinCannoneer = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/GoblinCannoneer.asset");
        var orcWarBoss = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/OrcWarBoss.asset");

        // Load upgrade data
        var upgrades = new UpgradeData[] {
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/HireEngineer.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/HirePikeman.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/HireCrossbowman.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/HireWizard.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/UpgBallistaDmg.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/UpgBallistaRate.asset"),
            AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/ScriptableObjects/Upgrades/BuildWall.asset"),
        };

        // Load defender prefabs
        var engineerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Defenders/Engineer.prefab");
        var pikemanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Defenders/Pikeman.prefab");
        var crossbowmanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Defenders/Crossbowman.prefab");
        var wizardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Defenders/Wizard.prefab");

        // Load defender projectile prefabs
        var boltPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/CrossbowBolt.prefab");
        var fireMissilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/FireMissile.prefab");

        // Wire projectiles into defender prefabs
        if (crossbowmanPrefab != null && boltPrefab != null)
        {
            var xbow = crossbowmanPrefab.GetComponent<Crossbowman>();
            if (xbow != null)
            {
                var xbowSO = new SerializedObject(xbow);
                xbowSO.FindProperty("boltPrefab").objectReferenceValue = boltPrefab;
                xbowSO.ApplyModifiedProperties();
                Debug.Log("[SceneWiring] Wired CrossbowBolt prefab to Crossbowman.");
            }
        }
        if (wizardPrefab != null && fireMissilePrefab != null)
        {
            var wiz = wizardPrefab.GetComponent<Wizard>();
            if (wiz != null)
            {
                var wizSO = new SerializedObject(wiz);
                wizSO.FindProperty("fireMissilePrefab").objectReferenceValue = fireMissilePrefab;
                wizSO.ApplyModifiedProperties();
                Debug.Log("[SceneWiring] Wired FireMissile prefab to Wizard.");
            }
        }

        // --- Create/find GameManager ---
        var gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        // --- Wire EnemySpawnManager ---
        var spawnObj = GameObject.Find("EnemySpawnManager");
        if (spawnObj == null)
        {
            spawnObj = new GameObject("EnemySpawnManager");
            spawnObj.AddComponent<EnemySpawnManager>();
        }
        var spawner = spawnObj.GetComponent<EnemySpawnManager>();
        var spawnerSO = new SerializedObject(spawner);
        spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
        spawnerSO.FindProperty("treasurePickupPrefab").objectReferenceValue = treasurePrefab;
        spawnerSO.FindProperty("orcGruntData").objectReferenceValue = orcGrunt;
        spawnerSO.FindProperty("bowOrcData").objectReferenceValue = bowOrc;
        spawnerSO.FindProperty("trollData").objectReferenceValue = troll;
        spawnerSO.FindProperty("suicideGoblinData").objectReferenceValue = suicideGoblin;
        spawnerSO.FindProperty("goblinCannoneerData").objectReferenceValue = goblinCannoneer;
        spawnerSO.FindProperty("orcWarBossData").objectReferenceValue = orcWarBoss;
        spawnerSO.FindProperty("hpScalingPerDay").floatValue = 0.1f;
        spawnerSO.FindProperty("damageScalingPerDay").floatValue = 0.05f;
        spawnerSO.ApplyModifiedProperties();

        // --- Wire MenialManager ---
        var menialMgrObj = GameObject.Find("MenialManager");
        if (menialMgrObj == null)
        {
            menialMgrObj = new GameObject("MenialManager");
            menialMgrObj.AddComponent<MenialManager>();
        }
        var menialMgr = menialMgrObj.GetComponent<MenialManager>();
        var menialMgrSO = new SerializedObject(menialMgr);
        menialMgrSO.FindProperty("menialPrefab").objectReferenceValue = menialPrefab;
        menialMgrSO.ApplyModifiedProperties();

        // --- Wire RefugeeSpawner ---
        var refSpawnObj = GameObject.Find("RefugeeSpawner");
        if (refSpawnObj == null)
        {
            refSpawnObj = new GameObject("RefugeeSpawner");
            refSpawnObj.AddComponent<RefugeeSpawner>();
        }
        var refSpawner = refSpawnObj.GetComponent<RefugeeSpawner>();
        var refSpawnSO = new SerializedObject(refSpawner);
        refSpawnSO.FindProperty("refugeePrefab").objectReferenceValue = refugeePrefab;
        refSpawnSO.ApplyModifiedProperties();

        // --- Create Tower visual ---
        var towerObj = GameObject.Find("Tower");
        if (towerObj == null)
        {
            var towerFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Tower.fbx");
            if (towerFbx != null)
            {
                towerObj = (GameObject)PrefabUtility.InstantiatePrefab(towerFbx);
                towerObj.name = "Tower";
                towerObj.transform.position = Vector3.zero;
                // Make tower static for batching
                towerObj.isStatic = true;
                foreach (Transform child in towerObj.GetComponentsInChildren<Transform>())
                    child.gameObject.isStatic = true;
                Debug.Log("[SceneWiring] Created Tower from FBX model at origin.");
            }
            else
            {
                Debug.LogWarning("[SceneWiring] Tower.fbx not found, no tower visual created.");
            }
        }

        // --- Wire Ballista ---
        var ballistaObj = GameObject.Find("BallistaBase");
        if (ballistaObj == null) ballistaObj = GameObject.Find("ScorpioBase");
        if (ballistaObj == null) ballistaObj = GameObject.Find("Ballista");
        if (ballistaObj == null)
        {
            // Try to use Ballista.fbx model
            var ballistaFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ballista.fbx");
            if (ballistaFbx != null)
            {
                // Wrap FBX in a parent so Ballista.cs LookRotation doesn't
                // overwrite the FBX import rotation (-90° X) on the model.
                ballistaObj = new GameObject("Ballista");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(ballistaFbx);
                model.name = "BallistaModel";
                model.transform.SetParent(ballistaObj.transform, false);
                Debug.Log("[SceneWiring] Created Ballista from FBX model on tower.");
            }
            else
            {
                // Fallback to primitive cube
                ballistaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ballistaObj.name = "ScorpioBase";
                ballistaObj.transform.localScale = new Vector3(0.5f, 0.3f, 1.5f);
                var scorpioMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScorpioBase.mat");
                if (scorpioMat != null) ballistaObj.GetComponent<Renderer>().sharedMaterial = scorpioMat;
                Debug.Log("[SceneWiring] Created ScorpioBase (primitive) on tower.");
            }

            ballistaObj.transform.position = new Vector3(0, 3.2f, 0);
            ballistaObj.AddComponent<Ballista>();

            // Create firePoint — forward of center along the firing axis
            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(ballistaObj.transform, false);
            firePoint.transform.localPosition = new Vector3(0, 0, 0.75f);

            var ballistaComp = ballistaObj.GetComponent<Ballista>();
            var fpSO = new SerializedObject(ballistaComp);
            fpSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            fpSO.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            fpSO.ApplyModifiedProperties();
        }
        else
        {
            var ballista = ballistaObj.GetComponent<Ballista>();
            if (ballista == null) ballista = ballistaObj.AddComponent<Ballista>();
            var ballistaSO = new SerializedObject(ballista);
            ballistaSO.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            ballistaSO.ApplyModifiedProperties();
        }

        // --- Wire WallManager ---
        var wallMgrObj = GameObject.Find("WallManager");
        if (wallMgrObj != null)
        {
            var wallMgr = wallMgrObj.GetComponent<WallManager>();
            if (wallMgr != null)
            {
                var wallMgrSO = new SerializedObject(wallMgr);
                wallMgrSO.FindProperty("wallPrefab").objectReferenceValue = wallPrefab;
                wallMgrSO.ApplyModifiedProperties();
            }

            // Add WallPlacement
            if (wallMgrObj.GetComponent<WallPlacement>() == null)
                wallMgrObj.AddComponent<WallPlacement>();
        }

        // --- Wire UpgradeManager ---
        var upgMgrObj = GameObject.Find("UpgradeManager");
        if (upgMgrObj == null)
        {
            upgMgrObj = new GameObject("UpgradeManager");
            upgMgrObj.AddComponent<UpgradeManager>();
        }
        var upgMgr = upgMgrObj.GetComponent<UpgradeManager>();
        var upgMgrSO = new SerializedObject(upgMgr);
        var upgradeList = upgMgrSO.FindProperty("availableUpgrades");
        upgradeList.ClearArray();
        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == null) continue;
            upgradeList.InsertArrayElementAtIndex(i);
            upgradeList.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
        }
        upgMgrSO.FindProperty("engineerPrefab").objectReferenceValue = engineerPrefab;
        upgMgrSO.FindProperty("pikemanPrefab").objectReferenceValue = pikemanPrefab;
        upgMgrSO.FindProperty("crossbowmanPrefab").objectReferenceValue = crossbowmanPrefab;
        upgMgrSO.FindProperty("wizardPrefab").objectReferenceValue = wizardPrefab;
        upgMgrSO.ApplyModifiedProperties();

        // --- Wire BallistaManager ---
        var ballMgrObj = GameObject.Find("BallistaManager");
        if (ballMgrObj == null)
        {
            ballMgrObj = new GameObject("BallistaManager");
            ballMgrObj.AddComponent<BallistaManager>();
        }
        if (ballMgrObj.GetComponent<BallistaManager>() == null)
            ballMgrObj.AddComponent<BallistaManager>();
        // Parent ScorpioBase under BallistaManager so GetComponentsInChildren finds it
        if (ballistaObj != null && ballistaObj.transform.parent != ballMgrObj.transform)
        {
            ballistaObj.transform.SetParent(ballMgrObj.transform, true);
        }

        // --- Wire DayNightCycle ---
        var dncObj = GameObject.Find("DayNightCycle");
        if (dncObj == null)
        {
            dncObj = new GameObject("DayNightCycle");
            dncObj.AddComponent<DayNightCycle>();
        }
        var dnc = dncObj.GetComponent<DayNightCycle>();
        var dirLight = GameObject.FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            var dncSO = new SerializedObject(dnc);
            dncSO.FindProperty("directionalLight").objectReferenceValue = dirLight;
            dncSO.ApplyModifiedProperties();
            Debug.Log($"[SceneWiring] DayNightCycle wired with directional light: {dirLight.name}");
        }
        else
        {
            Debug.LogWarning("[SceneWiring] No directional light found for DayNightCycle!");
        }

        // --- Wire SoundManager ---
        var sndObj = GameObject.Find("SoundManager");
        if (sndObj == null)
        {
            sndObj = new GameObject("SoundManager");
            sndObj.AddComponent<SoundManager>();
        }
        var sndMgr = sndObj.GetComponent<SoundManager>();
        var sndSO = new SerializedObject(sndMgr);
        sndSO.FindProperty("scorpioFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/scorpio.wav");
        sndSO.FindProperty("crossbowFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/crossbow.wav");
        sndSO.FindProperty("pikemanAttack").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/pikeman.wav");
        sndSO.FindProperty("wizardFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/firebolt.wav");
        sndSO.FindProperty("orcArcherFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/orc_archer.wav");
        sndSO.FindProperty("goblinBomberExplode").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/goblin_bomber.wav");
        sndSO.FindProperty("goblinCannonFire").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/goblin_cannon.wav");
        sndSO.FindProperty("orcHit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/orc_hit.wav");
        sndSO.FindProperty("trollHit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/troll_hit.wav");
        sndSO.FindProperty("menialHit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/menial_hit.wav");
        sndSO.FindProperty("wallHit").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/wall_hit.wav");
        sndSO.FindProperty("wallCollapse").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/wall_collapse.wav");
        sndSO.FindProperty("attackStart").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/attack_start.wav");

        // Music tracks
        var musicProp = sndSO.FindProperty("musicTracks");
        var song1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/song1.mp3");
        var song2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/song2.mp3");
        int trackCount = (song1 != null ? 1 : 0) + (song2 != null ? 1 : 0);
        musicProp.ClearArray();
        int idx = 0;
        if (song1 != null) { musicProp.InsertArrayElementAtIndex(idx); musicProp.GetArrayElementAtIndex(idx).objectReferenceValue = song1; idx++; }
        if (song2 != null) { musicProp.InsertArrayElementAtIndex(idx); musicProp.GetArrayElementAtIndex(idx).objectReferenceValue = song2; idx++; }

        sndSO.ApplyModifiedProperties();
        Debug.Log($"[SceneWiring] SoundManager wired with 13 SFX clips and {trackCount} music tracks.");

        Debug.Log("Scene wired successfully!");
    }
#endif
}
