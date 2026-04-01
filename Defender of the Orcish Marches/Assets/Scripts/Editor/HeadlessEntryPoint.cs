using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Entry point for headless ML training via -executeMethod.
/// Called by Unity when launched with: -executeMethod HeadlessEntryPoint.StartTraining
/// Fully self-bootstrapping — creates ML objects, saves scene, enters play mode.
/// Must live in Editor/ folder since it uses EditorApplication and editor-only setup classes.
/// </summary>
public static class HeadlessEntryPoint
{
    /// <summary>
    /// Sets up the scene for ML training and enters play mode.
    /// Uses EditorApplication.update loop to keep Unity alive in batch mode
    /// (otherwise -batchmode would quit after the method returns).
    /// </summary>
    public static void StartTraining()
    {
        Debug.Log("[HeadlessEntryPoint] Headless ML training entry point invoked.");

        string scenePath = "Assets/Scenes/GameScene.unity";

        Debug.Log($"[HeadlessEntryPoint] Opening scene: {scenePath}");
        EditorSceneManager.OpenScene(scenePath);

        // Run full ML scene setup + headless setup
        Debug.Log("[HeadlessEntryPoint] Running ML scene setup...");
        MLSceneSetup.SetupTrainingScene();
        HeadlessMLSetup.SetupHeadlessTraining();

        // Save the scene so changes persist
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HeadlessEntryPoint] Scene saved.");

        // Enter play mode — async, Unity needs to tick to actually start play mode
        Debug.Log("[HeadlessEntryPoint] Entering play mode...");
        EditorApplication.isPlaying = true;

        // Keep Unity alive in batch mode by hooking update.
        // Without this, -batchmode exits after the executeMethod returns.
        // The update hook runs forever, preventing the editor from quitting.
        EditorApplication.update += KeepAlive;
    }

    private static void KeepAlive()
    {
        // This prevents batch mode from exiting.
        // Unity stays alive as long as this delegate is registered.
        // Training ends when the process is killed externally.
    }
}
