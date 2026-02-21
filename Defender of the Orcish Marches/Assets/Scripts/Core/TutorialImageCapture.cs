using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class TutorialImageCapture : MonoBehaviour
{
    private static int imgWidth = 640;
    private static int imgHeight = 480;

    [MenuItem("Game/Capture Tutorial Images")]
    public static void CaptureAllImages()
    {
        EditorSceneManager.SaveOpenScenes();

        // Create output folder
        string fullDir = Path.Combine(Application.dataPath, "Resources", "Tutorial");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        // Load GameScene for scene-based captures
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        // Create capture camera
        var camObj = new GameObject("_CaptureCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.04f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.fieldOfView = 45f;

        var rt = new RenderTexture(imgWidth, imgHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        // --- Scene captures ---

        // Overview: angled view of the full fortress
        Capture(cam, rt, fullDir, "tut_overview",
            new Vector3(12f, 15f, -12f), Vector3.zero);

        // Fortress: closer overhead showing tower + walls clearly
        Capture(cam, rt, fullDir, "tut_fortress",
            new Vector3(6f, 10f, 6f), new Vector3(0f, 1.5f, 0f));

        // Top-down: gameplay-style straight-down view
        Capture(cam, rt, fullDir, "tut_topdown",
            new Vector3(0f, 25f, -0.1f), Vector3.zero);

        // Side view: from the east looking west (shows enemy approach direction)
        Capture(cam, rt, fullDir, "tut_side",
            new Vector3(10f, 8f, 5f), new Vector3(0f, 1f, 0f));

        // Ballista: close-up of tower top
        Capture(cam, rt, fullDir, "tut_ballista",
            new Vector3(2.5f, 5.5f, 2.5f), new Vector3(0f, 3.2f, 0f));

        // Wall: looking at the east wall section
        Capture(cam, rt, fullDir, "tut_wallgate",
            new Vector3(8f, 5f, 0f), new Vector3(3.75f, 1f, 0f));

        // Breach: west wall with tower visible behind, showing the threat
        Capture(cam, rt, fullDir, "tut_breach",
            new Vector3(-10f, 6f, 5f), new Vector3(-2f, 1f, 0f));

        // --- Prefab studio captures ---
        // Place prefabs well inside the ground plane (100x100, centered at origin)
        Vector3 studioCenter = new Vector3(15f, 0f, 15f);

        // Treasure (raised slightly so it doesn't clip into the ground)
        CapturePrefab(cam, rt, fullDir, "tut_treasure",
            "Assets/Prefabs/Loot/TreasurePickup.prefab",
            studioCenter + Vector3.up * 0.15f, new Vector3(15f, 1.5f, 13.5f), new Vector3(15f, 0.3f, 15f));

        // Menial
        CapturePrefab(cam, rt, fullDir, "tut_menial",
            "Assets/Prefabs/Characters/Menial.prefab",
            studioCenter, new Vector3(15f, 2f, 12.5f), new Vector3(15f, 0.7f, 15f));

        // Enemy (Orc Grunt — use EnemyData to apply bodyColor)
        CaptureEnemy(cam, rt, fullDir, studioCenter);

        // Refugee
        CapturePrefab(cam, rt, fullDir, "tut_refugee",
            "Assets/Prefabs/Characters/Refugee.prefab",
            studioCenter, new Vector3(15f, 2f, 12.5f), new Vector3(15f, 0.7f, 15f));

        // Defenders group (all four, camera pulled back to avoid edge clipping)
        CaptureDefenders(cam, rt, fullDir, studioCenter);

        // --- Cleanup ---
        Object.DestroyImmediate(camObj);
        rt.Release();
        Object.DestroyImmediate(rt);

        // Refresh and set all textures to Sprite
        AssetDatabase.Refresh();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Tutorial" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[TutorialImageCapture] All tutorial images captured to Assets/Resources/Tutorial/.");
    }

    static void Capture(Camera cam, RenderTexture rt, string dir, string filename,
        Vector3 position, Vector3 lookAt)
    {
        cam.transform.position = position;
        cam.transform.LookAt(lookAt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();

        var tex = new Texture2D(imgWidth, imgHeight, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, imgWidth, imgHeight), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string fullPath = Path.Combine(dir, filename + ".png");
        File.WriteAllBytes(fullPath, bytes);

        RenderTexture.active = prev;
        Object.DestroyImmediate(tex);

        Debug.Log($"[TutorialImageCapture] Captured {filename}");
    }

    /// <summary>
    /// Instantiate a prefab and swap its model for capture in edit mode.
    /// In edit mode, Awake() uses Destroy() which is deferred — we must do the
    /// model swap manually with DestroyImmediate.
    /// </summary>
    static void CapturePrefab(Camera cam, RenderTexture rt, string dir, string filename,
        string prefabPath, Vector3 spawnPos, Vector3 camPos, Vector3 camLookAt)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[TutorialImageCapture] Prefab not found: {prefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = spawnPos;
        // Rotate to face camera (models default face +Z, camera is at -Z)
        instance.transform.rotation = Quaternion.Euler(0, 180f, 0);

        // Disable components that cause issues outside play mode
        DisableRuntimeComponents(instance);

        // Swap model in edit mode — read the modelPrefab field from the MonoBehaviour
        SwapModelEditMode(instance);

        Capture(cam, rt, dir, filename, camPos, camLookAt);

        Object.DestroyImmediate(instance);
    }

    /// <summary>
    /// Capture all enemy types side by side using their EnemyData models.
    /// Models are wrapped in parent GameObjects so SampleAnimation doesn't affect positioning.
    /// </summary>
    static void CaptureEnemy(Camera cam, RenderTexture rt, string dir, Vector3 center)
    {
        string[] dataPaths = new string[]
        {
            "Assets/ScriptableObjects/Enemies/OrcGrunt.asset",
            "Assets/ScriptableObjects/Enemies/BowOrc.asset",
            "Assets/ScriptableObjects/Enemies/SuicideGoblin.asset",
            "Assets/ScriptableObjects/Enemies/GoblinCannoneer.asset",
            "Assets/ScriptableObjects/Enemies/Troll.asset",
            "Assets/ScriptableObjects/Enemies/OrcWarBoss.asset"
        };

        var instances = new GameObject[dataPaths.Length];
        float spacing = 1.0f;
        float startX = center.x - (dataPaths.Length - 1) * spacing * 0.5f;

        for (int i = 0; i < dataPaths.Length; i++)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPaths[i]);
            if (data == null || data.modelPrefab == null)
            {
                Debug.LogWarning($"[TutorialImageCapture] Enemy data or model not found: {dataPaths[i]}");
                continue;
            }

            // Wrap model in parent so SampleAnimation only affects child bone transforms
            var parent = new GameObject($"Enemy_{data.enemyName}");
            parent.transform.position = new Vector3(startX + i * spacing, center.y, center.z);
            parent.transform.rotation = Quaternion.Euler(0, 180f, 0);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(data.modelPrefab, parent.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            if (data.animatorController != null)
                PoseAtWalk(model, data.animatorController);

            instances[i] = parent;
            Debug.Log($"[TutorialImageCapture] Placed enemy: {data.enemyName}");
        }

        // Camera framing all 6 enemies
        Vector3 camPos = new Vector3(center.x, 3f, center.z - 6f);
        Vector3 lookAt = new Vector3(center.x, 0.5f, center.z);
        Capture(cam, rt, dir, "tut_enemy", camPos, lookAt);

        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] != null)
                Object.DestroyImmediate(instances[i]);
        }
    }

    static void CaptureDefenders(Camera cam, RenderTexture rt, string dir, Vector3 center)
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Defenders/Engineer.prefab",
            "Assets/Prefabs/Defenders/Pikeman.prefab",
            "Assets/Prefabs/Defenders/Crossbowman.prefab",
            "Assets/Prefabs/Defenders/Wizard.prefab"
        };

        string[] dataPaths = new string[]
        {
            "Assets/ScriptableObjects/Defenders/Engineer.asset",
            "Assets/ScriptableObjects/Defenders/Pikeman.asset",
            "Assets/ScriptableObjects/Defenders/Crossbowman.asset",
            "Assets/ScriptableObjects/Defenders/Wizard.asset"
        };

        var instances = new GameObject[prefabPaths.Length];
        float spacing = 1.5f;
        float startX = center.x - (prefabPaths.Length - 1) * spacing * 0.5f;

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"[TutorialImageCapture] Defender prefab not found: {prefabPaths[i]}");
                continue;
            }
            instances[i] = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instances[i].transform.position = new Vector3(startX + i * spacing, center.y, center.z);
            // Rotate to face camera
            instances[i].transform.rotation = Quaternion.Euler(0, 180f, 0);

            DisableRuntimeComponents(instances[i]);

            // Apply DefenderData model swap
            var data = AssetDatabase.LoadAssetAtPath<DefenderData>(dataPaths[i]);
            if (data != null && data.modelPrefab != null)
            {
                // Destroy existing visual children (primitive placeholders)
                for (int c = instances[i].transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(instances[i].transform.GetChild(c).gameObject);

                var newModel = (GameObject)PrefabUtility.InstantiatePrefab(data.modelPrefab, instances[i].transform);
                newModel.name = "Model";
                newModel.transform.localPosition = Vector3.zero;
                newModel.transform.localRotation = Quaternion.identity;
                newModel.transform.localScale = Vector3.one;

                // Pose at Walk animation frame 1
                if (data.animatorController != null)
                    PoseAtWalk(newModel, data.animatorController);
            }
            else if (data != null)
            {
                // No custom model — apply bodyColor
                foreach (var rend in instances[i].GetComponentsInChildren<Renderer>())
                    rend.sharedMaterial = new Material(rend.sharedMaterial) { color = data.bodyColor };
            }
        }

        // Camera pulled back further to frame all four without edge clipping
        Vector3 camPos = new Vector3(center.x, 3f, center.z - 6f);
        Vector3 lookAt = new Vector3(center.x, 0.5f, center.z);
        Capture(cam, rt, dir, "tut_defenders", camPos, lookAt);

        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] != null)
                Object.DestroyImmediate(instances[i]);
        }
    }

    /// <summary>
    /// Reads the serialized modelPrefab field from supported MonoBehaviours
    /// (TreasurePickup, Menial, Refugee) and performs the model swap in edit mode
    /// using DestroyImmediate instead of Destroy.
    /// </summary>
    static void SwapModelEditMode(GameObject instance)
    {
        // Check each known type that has a modelPrefab serialized field
        SerializedObject so = null;
        SerializedProperty modelProp = null;

        // Try TreasurePickup
        var treasure = instance.GetComponent<TreasurePickup>();
        if (treasure != null)
        {
            so = new SerializedObject(treasure);
            modelProp = so.FindProperty("modelPrefab");
            if (modelProp != null && modelProp.objectReferenceValue != null)
            {
                var modelPrefab = (GameObject)modelProp.objectReferenceValue;

                // DestroyImmediate children
                for (int i = instance.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(instance.transform.GetChild(i).gameObject);

                // DestroyImmediate root mesh components (cube primitive)
                var rootRenderer = instance.GetComponent<MeshRenderer>();
                if (rootRenderer != null) Object.DestroyImmediate(rootRenderer);
                var rootFilter = instance.GetComponent<MeshFilter>();
                if (rootFilter != null) Object.DestroyImmediate(rootFilter);

                var newModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, instance.transform);
                newModel.name = "Model";
                newModel.transform.localPosition = Vector3.zero;
                newModel.transform.localRotation = Quaternion.identity;
                newModel.transform.localScale = Vector3.one;

                Debug.Log("[TutorialImageCapture] Swapped TreasurePickup model for capture");
            }
            return;
        }

        // Try Menial
        var menial = instance.GetComponent<Menial>();
        if (menial != null)
        {
            so = new SerializedObject(menial);
            modelProp = so.FindProperty("modelPrefab");
            if (modelProp != null && modelProp.objectReferenceValue != null)
            {
                var newModel = SwapCharacterModel(instance, (GameObject)modelProp.objectReferenceValue, "Menial");
                var animProp = so.FindProperty("animatorController");
                if (animProp != null && animProp.objectReferenceValue != null)
                    PoseAtWalk(newModel, (RuntimeAnimatorController)animProp.objectReferenceValue);
            }
            return;
        }

        // Try Refugee
        var refugee = instance.GetComponent<Refugee>();
        if (refugee != null)
        {
            so = new SerializedObject(refugee);
            modelProp = so.FindProperty("modelPrefab");
            if (modelProp != null && modelProp.objectReferenceValue != null)
            {
                var newModel = SwapCharacterModel(instance, (GameObject)modelProp.objectReferenceValue, "Refugee");
                var animProp = so.FindProperty("animatorController");
                if (animProp != null && animProp.objectReferenceValue != null)
                    PoseAtWalk(newModel, (RuntimeAnimatorController)animProp.objectReferenceValue);
            }
            return;
        }
    }

    /// <summary>
    /// Common model swap for character prefabs (Menial, Refugee) — destroy children,
    /// instantiate new model.
    /// </summary>
    static GameObject SwapCharacterModel(GameObject instance, GameObject modelPrefab, string label)
    {
        for (int i = instance.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(instance.transform.GetChild(i).gameObject);

        var newModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, instance.transform);
        newModel.name = "Model";
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;

        Debug.Log($"[TutorialImageCapture] Swapped {label} model for capture");
        return newModel;
    }

    /// <summary>
    /// Force a model into Walk pose at frame 0 using AnimationClip.SampleAnimation (works in edit mode).
    /// </summary>
    static void PoseAtWalk(GameObject model, RuntimeAnimatorController controller)
    {
        if (controller == null || model == null) return;

        AnimationClip walkClip = null;
        foreach (var clip in controller.animationClips)
        {
            if (clip.name.Contains("Walk") || clip.name.Contains("walk"))
            {
                walkClip = clip;
                break;
            }
        }

        if (walkClip != null)
        {
            walkClip.SampleAnimation(model, 0f);
            Debug.Log($"[TutorialImageCapture] Posed {model.name} at Walk frame 0");
        }
        else
        {
            Debug.LogWarning($"[TutorialImageCapture] No Walk clip found in {controller.name}");
        }
    }

    /// <summary>
    /// Disable NavMeshAgent and other runtime-only components on an instantiated prefab.
    /// </summary>
    static void DisableRuntimeComponents(GameObject instance)
    {
        foreach (var agent in instance.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            agent.enabled = false;
    }
}
#endif
