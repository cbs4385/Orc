using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor script for configuring the training scene for headless ML training.
/// Adds HeadlessMLBootstrap and sets up all required ML components.
/// </summary>
public static class HeadlessMLSetup
{
    [MenuItem("Tools/ML/Setup Headless Training")]
    public static void SetupHeadlessTraining()
    {
        Debug.Log("[HeadlessMLSetup] Configuring scene for headless ML training...");

        // First, run the standard ML scene setup
        MLSceneSetup.SetupTrainingScene();

        // Add HeadlessMLBootstrap to the MLTrainingManager object
        var managerGO = GameObject.Find("MLTrainingManager");
        if (managerGO == null)
        {
            Debug.LogError("[HeadlessMLSetup] MLTrainingManager not found! Run 'Setup Training Scene' first.");
            return;
        }

        var bootstrap = managerGO.GetComponent<HeadlessMLBootstrap>();
        if (bootstrap == null)
            bootstrap = managerGO.AddComponent<HeadlessMLBootstrap>();

        // No need to configure time scale — mlagents-learn --time-scale handles it
        // HeadlessMLBootstrap just strips rendering, audio, and log overhead

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[HeadlessMLSetup] Headless training setup complete!");
        Debug.Log("[HeadlessMLSetup] To train headless:");
        Debug.Log("[HeadlessMLSetup]   1. Save the scene");
        Debug.Log("[HeadlessMLSetup]   2. Run: train_headless.bat (Windows) or train_headless.sh (Linux/Mac)");
    }

    [MenuItem("Tools/ML/Toggle Force Headless (Editor Testing)")]
    public static void ToggleForceHeadless()
    {
        var managerGO = GameObject.Find("MLTrainingManager");
        if (managerGO == null)
        {
            Debug.LogError("[HeadlessMLSetup] MLTrainingManager not found!");
            return;
        }

        var bootstrap = managerGO.GetComponent<HeadlessMLBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[HeadlessMLSetup] HeadlessMLBootstrap not found! Run 'Setup Headless Training' first.");
            return;
        }

        var so = new SerializedObject(bootstrap);
        var prop = so.FindProperty("forceHeadless");
        if (prop != null)
        {
            prop.boolValue = !prop.boolValue;
            so.ApplyModifiedProperties();
            Debug.Log($"[HeadlessMLSetup] forceHeadless = {prop.boolValue}");
        }
    }
}
