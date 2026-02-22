using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool to test the wall gap fix.
/// Destroys Wall_W_1 and Wall_W_2 (west side breach) then spawns Orc Grunts
/// from various directions so they approach the wall gaps.
/// Run via Tools > Test Wall Gap Fix (must be in Play mode).
/// </summary>
public static class WallGapTest
{
    [MenuItem("Tools/Test Wall Gap Fix")]
    public static void RunTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[WallGapTest] Must be in Play mode to run this test.");
            return;
        }

        if (WallManager.Instance == null)
        {
            Debug.LogError("[WallGapTest] WallManager.Instance is null.");
            return;
        }

        if (EnemySpawnManager.Instance == null)
        {
            Debug.LogError("[WallGapTest] EnemySpawnManager.Instance is null.");
            return;
        }

        // 1. Create west-side breach (destroy Wall_W_1 and Wall_W_2)
        int destroyed = 0;
        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall.name == "Wall_W_1" || wall.name == "Wall_W_2")
            {
                wall.TakeDamage(9999);
                destroyed++;
                Debug.Log($"[WallGapTest] Destroyed {wall.name} to create breach.");
            }
        }
        Debug.Log($"[WallGapTest] Created west breach ({destroyed} walls destroyed).");

        // 2. Spawn Orc Grunts from multiple directions
        var spawnMgr = EnemySpawnManager.Instance;

        var prefabField = typeof(EnemySpawnManager).GetField("enemyPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dataField = typeof(EnemySpawnManager).GetField("orcGruntData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (prefabField == null || dataField == null)
        {
            Debug.LogError("[WallGapTest] Could not find enemyPrefab or orcGruntData fields.");
            return;
        }

        var prefab = prefabField.GetValue(spawnMgr) as GameObject;
        var data = dataField.GetValue(spawnMgr) as EnemyData;

        if (prefab == null || data == null)
        {
            Debug.LogError("[WallGapTest] enemyPrefab or orcGruntData is null on EnemySpawnManager.");
            return;
        }

        // Spawn enemies spread across the south side
        float[] xPositions = { -3f, -1f, 0f, 1f, 3f, 5f };
        foreach (float x in xPositions)
        {
            Vector3 spawnPos = new Vector3(x, 0, -30f);
            var go = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(data);
                Debug.Log($"[WallGapTest] Spawned Orc Grunt at {spawnPos}");
            }
        }

        Debug.Log("[WallGapTest] Test setup complete. Watch south wall gaps — enemies should attack walls, NOT squeeze through.");
    }
}
