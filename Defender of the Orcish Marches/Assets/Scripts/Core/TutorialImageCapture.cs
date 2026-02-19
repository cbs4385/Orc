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

        // Wall + gate: looking at the east gate (the only gate)
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

        // Enemy
        CapturePrefab(cam, rt, fullDir, "tut_enemy",
            "Assets/Prefabs/Enemies/Enemy.prefab",
            studioCenter, new Vector3(15f, 2f, 12.5f), new Vector3(15f, 0.7f, 15f));

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

        // Disable components that might cause issues outside play mode
        foreach (var agent in instance.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            agent.enabled = false;

        Capture(cam, rt, dir, filename, camPos, camLookAt);

        Object.DestroyImmediate(instance);
    }

    static void CaptureDefenders(Camera cam, RenderTexture rt, string dir, Vector3 center)
    {
        string[] paths = new string[]
        {
            "Assets/Prefabs/Defenders/Engineer.prefab",
            "Assets/Prefabs/Defenders/Pikeman.prefab",
            "Assets/Prefabs/Defenders/Crossbowman.prefab",
            "Assets/Prefabs/Defenders/Wizard.prefab"
        };

        var instances = new GameObject[paths.Length];
        float spacing = 1.5f;
        float startX = center.x - (paths.Length - 1) * spacing * 0.5f;

        for (int i = 0; i < paths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"[TutorialImageCapture] Defender prefab not found: {paths[i]}");
                continue;
            }
            instances[i] = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instances[i].transform.position = new Vector3(startX + i * spacing, center.y, center.z);

            foreach (var agent in instances[i].GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
                agent.enabled = false;
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
}
#endif
