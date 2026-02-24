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
    private const string ENEMY_BLOCK_LAYER_NAME = "EnemyBlock";

    [MenuItem("Tools/Setup Dual NavMesh")]
    public static void SetupDualNavMesh()
    {
        // --- 1. Find or create the Enemy agent type ---
        int enemyAgentTypeID = FindAgentTypeByName(ENEMY_AGENT_NAME);

        if (enemyAgentTypeID == -1)
        {
            // NavMesh.CreateSettings() returns a struct copy — property assignments
            // on the returned struct do NOT persist to the project settings.
            // We create the entry to get an ID, then fix properties via SerializedObject.
            var settings = NavMesh.CreateSettings();
            enemyAgentTypeID = settings.agentTypeID;
            Debug.Log($"[NavMeshSetup] Created '{ENEMY_AGENT_NAME}' agent type: ID={enemyAgentTypeID}");
        }
        else
        {
            Debug.Log($"[NavMeshSetup] '{ENEMY_AGENT_NAME}' agent type already exists: ID={enemyAgentTypeID}");
        }

        // Persist radius/height/slope/climb via SerializedObject (struct copies don't stick)
        UpdateAgentSettingsViaSerializedObject(enemyAgentTypeID);

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

        // Use PhysicsColliders for enemy surface so runtime BuildNavMesh() works
        // (RenderMeshes requires mesh Read/Write enabled, which fails at runtime)
        enemySurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        Debug.Log("[NavMeshSetup] Enemy surface useGeometry set to PhysicsColliders (runtime-safe).");

        // --- 4. Set up layers and configure NavMesh bake masks ---
        // "Walls" layer: wall meshes excluded from BOTH bakes. NavMeshObstacle carving
        // controls wall body blocking at runtime, so breaches open when walls are destroyed.
        // "EnemyBlock" layer: tower colliders included in enemy bake ONLY. This blocks
        // enemies at tower endpoints without sealing the humanoid passage gaps.
        int wallsLayer = EnsureLayerExists(WALLS_LAYER_NAME);
        int enemyBlockLayer = EnsureLayerExists(ENEMY_BLOCK_LAYER_NAME);
        if (wallsLayer >= 0)
        {
            // Humanoid: exclude Walls AND EnemyBlock (preserves gaps for friendlies)
            int humanoidExcludeMask = ~(1 << wallsLayer);
            if (enemyBlockLayer >= 0)
                humanoidExcludeMask &= ~(1 << enemyBlockLayer);
            friendlySurface.layerMask = humanoidExcludeMask;

            // Enemy: exclude Walls only — EnemyBlock is INCLUDED so tower geometry
            // bakes as obstacles in the enemy NavMesh
            int enemyExcludeMask = ~(1 << wallsLayer);
            enemySurface.layerMask = enemyExcludeMask;

            Debug.Log($"[NavMeshSetup] Layers: Walls={wallsLayer}, EnemyBlock={enemyBlockLayer}. " +
                $"Humanoid layerMask={humanoidExcludeMask}, Enemy layerMask={enemyExcludeMask}");

            // Move all Wall objects in the scene to the Walls layer
            // (tower collider children created at runtime will be set to EnemyBlock in Wall.Awake)
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

    /// <summary>
    /// Updates agent type properties via the serialized NavMeshProjectSettings asset.
    /// NavMesh.CreateSettings()/GetSettingsByID() return struct copies — modifying their
    /// fields does NOT persist. This method edits the YAML asset directly.
    /// </summary>
    private static void UpdateAgentSettingsViaSerializedObject(int agentTypeID)
    {
        var navMeshAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
        if (navMeshAssets.Length == 0)
        {
            Debug.LogError("[NavMeshSetup] Could not load NavMeshAreas.asset!");
            return;
        }

        var so = new SerializedObject(navMeshAssets[0]);
        var settingsArray = so.FindProperty("m_Settings");
        var namesArray = so.FindProperty("m_SettingNames");

        // m_Settings[0] is always the built-in Humanoid (agentTypeID=0).
        // Custom agent types start at index 1.
        // m_SettingNames maps 1:1 with m_Settings (including index 0 = "Humanoid").
        for (int i = 0; i < settingsArray.arraySize; i++)
        {
            var element = settingsArray.GetArrayElementAtIndex(i);
            if (element.FindPropertyRelative("agentTypeID").intValue == agentTypeID)
            {
                float oldRadius = element.FindPropertyRelative("agentRadius").floatValue;
                element.FindPropertyRelative("agentRadius").floatValue = ENEMY_RADIUS;
                element.FindPropertyRelative("agentHeight").floatValue = ENEMY_HEIGHT;
                element.FindPropertyRelative("agentSlope").floatValue = ENEMY_SLOPE;
                element.FindPropertyRelative("agentClimb").floatValue = ENEMY_CLIMB;

                // Rename to "Enemy" if it has a generic name
                if (i < namesArray.arraySize)
                {
                    string currentName = namesArray.GetArrayElementAtIndex(i).stringValue;
                    if (currentName != ENEMY_AGENT_NAME)
                    {
                        namesArray.GetArrayElementAtIndex(i).stringValue = ENEMY_AGENT_NAME;
                        Debug.Log($"[NavMeshSetup] Renamed agent type from '{currentName}' to '{ENEMY_AGENT_NAME}'");
                    }
                }

                so.ApplyModifiedProperties();
                Debug.Log($"[NavMeshSetup] Updated agent settings via SerializedObject: radius {oldRadius} -> {ENEMY_RADIUS}, height={ENEMY_HEIGHT}, slope={ENEMY_SLOPE}, climb={ENEMY_CLIMB}");
                return;
            }
        }

        Debug.LogError($"[NavMeshSetup] Could not find agentTypeID={agentTypeID} in NavMeshAreas.asset!");
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
