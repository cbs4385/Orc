using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class NavMeshSetup
{
    private const string ENEMY_AGENT_NAME = "Enemy";
    private const float ENEMY_RADIUS = 0.7f;
    private const float ENEMY_HEIGHT = 2f;
    private const float ENEMY_SLOPE = 45f;
    private const float ENEMY_CLIMB = 0.75f;
    private const string WALLS_LAYER_NAME = "Walls";

    [MenuItem("Tools/Setup Dual NavMesh")]
    public static void SetupDualNavMesh()
    {
        // --- 1. Find or create the Enemy agent type ---
        int enemyAgentTypeID = FindAgentTypeByName(ENEMY_AGENT_NAME);

        if (enemyAgentTypeID == -1)
        {
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = ENEMY_RADIUS;
            settings.agentHeight = ENEMY_HEIGHT;
            settings.agentSlope = ENEMY_SLOPE;
            settings.agentClimb = ENEMY_CLIMB;
            enemyAgentTypeID = settings.agentTypeID;
            Debug.Log($"[NavMeshSetup] Created '{ENEMY_AGENT_NAME}' agent type: ID={enemyAgentTypeID}, radius={ENEMY_RADIUS}");
        }
        else
        {
            Debug.Log($"[NavMeshSetup] '{ENEMY_AGENT_NAME}' agent type already exists: ID={enemyAgentTypeID}");
        }

        // Also verify the settings match what we expect
        var existingSettings = NavMesh.GetSettingsByID(enemyAgentTypeID);
        if (!Mathf.Approximately(existingSettings.agentRadius, ENEMY_RADIUS))
        {
            Debug.LogWarning($"[NavMeshSetup] Enemy agent radius is {existingSettings.agentRadius}, expected {ENEMY_RADIUS}. Updating.");
            existingSettings.agentRadius = ENEMY_RADIUS;
        }

        // --- 2. Find or create NavMeshSurfaces GameObject ---
        GameObject surfacesGO = GameObject.Find("[NavMeshSurfaces]");
        if (surfacesGO == null)
        {
            surfacesGO = new GameObject("[NavMeshSurfaces]");
            Undo.RegisterCreatedObjectUndo(surfacesGO, "Create NavMeshSurfaces");
            Debug.Log("[NavMeshSetup] Created [NavMeshSurfaces] GameObject");
        }

        // --- 3. Ensure two NavMeshSurface components: Humanoid + Enemy ---
        var existingSurfaces = surfacesGO.GetComponents<NavMeshSurface>();
        NavMeshSurface friendlySurface = null;
        NavMeshSurface enemySurface = null;

        foreach (var surface in existingSurfaces)
        {
            if (surface.agentTypeID == 0)
                friendlySurface = surface;
            else if (surface.agentTypeID == enemyAgentTypeID)
                enemySurface = surface;
        }

        if (friendlySurface == null)
        {
            friendlySurface = Undo.AddComponent<NavMeshSurface>(surfacesGO);
            friendlySurface.agentTypeID = 0; // Humanoid (default)
            Debug.Log("[NavMeshSetup] Added Humanoid NavMeshSurface (agentTypeID=0)");
        }

        if (enemySurface == null)
        {
            enemySurface = Undo.AddComponent<NavMeshSurface>(surfacesGO);
            enemySurface.agentTypeID = enemyAgentTypeID;
            Debug.Log($"[NavMeshSetup] Added Enemy NavMeshSurface (agentTypeID={enemyAgentTypeID})");
        }

        // --- 4. Set up Walls layer and exclude from NavMesh bake ---
        // Walls must NOT be baked as static geometry — only NavMeshObstacle carving
        // should control wall blocking, so breaches open up when walls are destroyed.
        int wallsLayer = EnsureLayerExists(WALLS_LAYER_NAME);
        if (wallsLayer >= 0)
        {
            int excludeWallsMask = ~(1 << wallsLayer);
            friendlySurface.layerMask = excludeWallsMask;
            enemySurface.layerMask = excludeWallsMask;
            Debug.Log($"[NavMeshSetup] Excluding '{WALLS_LAYER_NAME}' layer ({wallsLayer}) from NavMesh bake. layerMask={excludeWallsMask}");

            // Move all Wall objects in the scene to the Walls layer
            var walls = Object.FindObjectsByType<Wall>(FindObjectsSortMode.None);
            foreach (var wall in walls)
                SetLayerRecursive(wall.gameObject, wallsLayer);
            Debug.Log($"[NavMeshSetup] Moved {walls.Length} wall GameObjects to '{WALLS_LAYER_NAME}' layer");
        }
        else
        {
            Debug.LogError($"[NavMeshSetup] Could not create '{WALLS_LAYER_NAME}' layer! Walls will be baked as static geometry.");
        }

        // --- 5. Bake both surfaces ---
        friendlySurface.BuildNavMesh();
        Debug.Log("[NavMeshSetup] Baked Humanoid NavMesh (friendly, radius=0.5)");

        enemySurface.BuildNavMesh();
        Debug.Log($"[NavMeshSetup] Baked Enemy NavMesh (radius={ENEMY_RADIUS})");

        // --- 6. Update Enemy prefab's NavMeshAgent to use Enemy agent type ---
        string prefabPath = "Assets/Prefabs/Enemies/Enemy.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            // Edit prefab contents directly
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(assetPath))
            {
                var agent = editScope.prefabContentsRoot.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.agentTypeID = enemyAgentTypeID;
                    Debug.Log($"[NavMeshSetup] Set Enemy.prefab NavMeshAgent agentTypeID={enemyAgentTypeID}");
                }
                else
                {
                    Debug.LogError("[NavMeshSetup] Enemy.prefab has no NavMeshAgent component!");
                }
            }
        }
        else
        {
            Debug.LogError($"[NavMeshSetup] Could not find prefab at {prefabPath}");
        }

        // --- 7. Mark scene dirty ---
        EditorUtility.SetDirty(surfacesGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[NavMeshSetup] Dual NavMesh setup complete!");
    }

    private static int FindAgentTypeByName(string name)
    {
        int count = NavMesh.GetSettingsCount();
        for (int i = 0; i < count; i++)
        {
            var settings = NavMesh.GetSettingsByIndex(i);
            string settingsName = NavMesh.GetSettingsNameFromID(settings.agentTypeID);
            if (settingsName == name)
                return settings.agentTypeID;
        }
        return -1;
    }

    /// <summary>
    /// Ensure a layer exists in the TagManager. Returns the layer index or -1 on failure.
    /// </summary>
    private static int EnsureLayerExists(string layerName)
    {
        // Check if it already exists
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        // Find a free user layer slot (8-31)
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[NavMeshSetup] Created layer '{layerName}' at index {i}");
                return i;
            }
        }

        Debug.LogError($"[NavMeshSetup] No free layer slots available for '{layerName}'!");
        return -1;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
