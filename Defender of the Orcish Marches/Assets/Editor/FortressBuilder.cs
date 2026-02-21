using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool to rebuild the initial fortress layout using FBX wall segments
/// with baked octagonal towers. Square layout with 4 walls per side.
/// Uses WALL_SPACING from the model geometry.
/// </summary>
public static class FortressBuilder
{
    private const float WALL_Y = 1f; // Wall center Y (bottom at ground level for height=2)

    // From generate_wall.py: adjacent wall centers are 2 * TOWER_OFFSET apart
    private const float S = WallCorners.WALL_SPACING; // 2.2072
    private const float SIDE_OFFSET = 2f * S;         // 4.4144 — distance from origin to side center line

    // Wall centers along a 4-wall side: -1.5S, -0.5S, +0.5S, +1.5S
    private static readonly float[] SLOTS = { -1.5f * S, -0.5f * S, 0.5f * S, 1.5f * S };

    [MenuItem("Tools/Rebuild Fortress")]
    public static void RebuildFortress()
    {
        var wallManager = Object.FindAnyObjectByType<WallManager>();
        if (wallManager == null)
        {
            Debug.LogError("[FortressBuilder] No WallManager found in scene!");
            return;
        }

        // Find the WallSegment prefab via serialized field
        var so = new SerializedObject(wallManager);
        var prefabProp = so.FindProperty("wallPrefab");
        GameObject wallPrefab = prefabProp.objectReferenceValue as GameObject;
        if (wallPrefab == null)
        {
            Debug.LogError("[FortressBuilder] WallManager.wallPrefab is null!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(wallManager.gameObject, "Rebuild Fortress");

        // Delete all existing Wall children
        var existingWalls = wallManager.GetComponentsInChildren<Wall>(true);
        Debug.Log($"[FortressBuilder] Removing {existingWalls.Length} existing wall(s).");
        for (int i = existingWalls.Length - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(existingWalls[i].gameObject);
        }

        int count = 0;

        // North side (z = +SIDE_OFFSET, Y rotation = 0): 4 walls
        for (int i = 0; i < 4; i++)
            count += PlaceWall(wallPrefab, wallManager.transform,
                new Vector3(SLOTS[i], WALL_Y, SIDE_OFFSET), 0f, $"Wall_N_{i}");

        // South side (z = -SIDE_OFFSET, Y rotation = 0): 4 walls
        for (int i = 0; i < 4; i++)
            count += PlaceWall(wallPrefab, wallManager.transform,
                new Vector3(SLOTS[i], WALL_Y, -SIDE_OFFSET), 0f, $"Wall_S_{i}");

        // West side (x = -SIDE_OFFSET, Y rotation = 90): 4 walls
        for (int i = 0; i < 4; i++)
            count += PlaceWall(wallPrefab, wallManager.transform,
                new Vector3(-SIDE_OFFSET, WALL_Y, SLOTS[i]), 90f, $"Wall_W_{i}");

        // East side (x = +SIDE_OFFSET, Y rotation = 90): 4 walls
        for (int i = 0; i < 4; i++)
            count += PlaceWall(wallPrefab, wallManager.transform,
                new Vector3(SIDE_OFFSET, WALL_Y, SLOTS[i]), 90f, $"Wall_E_{i}");

        Debug.Log($"[FortressBuilder] Placed {count} walls (16 expected). Fortress rebuilt.");
        EditorUtility.SetDirty(wallManager.gameObject);
    }

    private static int PlaceWall(GameObject prefab, Transform parent, Vector3 position, float rotY, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (go == null)
        {
            Debug.LogError($"[FortressBuilder] Failed to instantiate wall prefab for {name}!");
            return 0;
        }

        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        go.name = name;

        Undo.RegisterCreatedObjectUndo(go, "Place Wall " + name);
        return 1;
    }
}
