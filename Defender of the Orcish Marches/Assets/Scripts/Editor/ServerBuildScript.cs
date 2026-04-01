using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds a standalone player for ML training.
/// Called via: Unity -batchmode -executeMethod ServerBuildScript.Build
/// The built exe is launched by mlagents-learn --env= with --no-graphics.
/// HeadlessMLBootstrap auto-detects headless mode at runtime.
/// </summary>
public static class ServerBuildScript
{
    public static void Build()
    {
        Debug.Log("[ServerBuild] Starting ML training build...");

        // Open the game scene first
        string scenePath = "Assets/Scenes/GameScene.unity";
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        // Run ML scene setup to ensure all ML objects are configured
        MLSceneSetup.SetupTrainingScene();
        HeadlessMLSetup.SetupHeadlessTraining();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        string buildPath = "Builds/MLTrainingServer/OrcTrainer.exe";

        // Use a standard player build — HeadlessMLBootstrap detects -nographics/-batchmode at runtime
        // and strips rendering, audio, UI automatically. No need for deprecated EnableHeadlessMode.
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None
        };

        Debug.Log("[ServerBuild] Building player...");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[ServerBuild] SUCCESS: {buildPath} ({report.summary.totalSize / 1024 / 1024}MB)");
            Debug.Log($"[ServerBuild] To train: mlagents-learn ml_training_config_headless.yaml --env={buildPath} --run-id=server_v1 --no-graphics --time-scale=5");
        }
        else
        {
            Debug.LogError($"[ServerBuild] FAILED: {report.summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Warning)
                        Debug.LogError($"[ServerBuild] {msg.content}");
                }
            }
            // Exit with error code so the calling script knows it failed
            EditorApplication.Exit(1);
        }
    }
}
