using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Editor utility to fix StandaloneInputModule → InputSystemUIInputModule across all scenes,
/// and report any hardcoded English UI text in TextMeshPro components.
/// </summary>
public static class FixInputModule
{
    [MenuItem("Tools/Fix Input Modules In All Scenes")]
    public static void FixAllScenes()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/Options.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        int fixedCount = 0;
        foreach (var scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath) &&
                !System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", scenePath)))
            {
                Debug.Log($"[FixInputModule] Scene not found: {scenePath}, skipping.");
                continue;
            }

            Debug.Log($"[FixInputModule] Processing scene: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find all StandaloneInputModule components
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var oldModules = root.GetComponentsInChildren<StandaloneInputModule>(true);
                foreach (var oldModule in oldModules)
                {
                    var go = oldModule.gameObject;
                    Debug.Log($"[FixInputModule] Found StandaloneInputModule on '{go.name}' in {scenePath}. Removing...");
                    Object.DestroyImmediate(oldModule);

                    // Add InputSystemUIInputModule if not already present
                    var newModule = go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (newModule == null)
                    {
                        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                        Debug.Log($"[FixInputModule] Added InputSystemUIInputModule to '{go.name}'.");
                    }
                    fixedCount++;
                }
            }

            // Also report any TextMeshPro with suspicious hardcoded English text
            foreach (var root in rootObjects)
            {
                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    string text = tmp.text;
                    if (!string.IsNullOrEmpty(text) &&
                        (text.Contains("Toggle") || text.Contains("Right-click") ||
                         text.Contains("loot") || text.Contains("collect")))
                    {
                        Debug.LogWarning($"[FixInputModule] Possible hardcoded English text on '{tmp.gameObject.name}' " +
                                         $"(parent: {tmp.transform.parent?.name}): \"{text}\"");
                    }
                }
            }

            if (fixedCount > 0 || EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FixInputModule] Saved {scenePath}.");
            }
        }

        // Return to original scene
        if (!string.IsNullOrEmpty(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
            Debug.Log($"[FixInputModule] Returned to {currentScene}.");
        }

        Debug.Log($"[FixInputModule] Done. Fixed {fixedCount} StandaloneInputModule(s) across all scenes.");
    }
}
