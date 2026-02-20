using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public static class BuildScript
{
    private const string BuildRoot = "Builds";
    private const string ProductName = "DefenderOfTheOrcishMarches";

    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }

    /// <summary>
    /// Reads the version from the latest git commit message (expects "vX.Y.Z" format)
    /// and applies it to PlayerSettings.bundleVersion.
    /// </summary>
    private static void SyncVersionFromGit()
    {
        string version = GetGitVersion();
        if (string.IsNullOrEmpty(version))
        {
            Debug.LogWarning($"[BuildScript] Could not read version from git, keeping bundleVersion={PlayerSettings.bundleVersion}");
            return;
        }

        PlayerSettings.bundleVersion = version;
        Debug.Log($"[BuildScript] Set bundleVersion={version} from git");
    }

    private static string GetGitVersion()
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "log -1 --format=%s";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string subject = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // Extract version from commit message like "v0.4.0" or "v0.4.0 - description"
            var match = System.Text.RegularExpressions.Regex.Match(subject, @"^v?(\d+\.\d+\.\d+)");
            if (match.Success)
                return match.Groups[1].Value;

            Debug.LogWarning($"[BuildScript] Latest commit message '{subject}' does not start with a version number");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BuildScript] Failed to run git: {ex.Message}");
            return null;
        }
    }

    [MenuItem("Build/Build Windows", priority = 1)]
    public static void BuildWindows()
    {
        SyncVersionFromGit();
        string path = Path.Combine(BuildRoot, "Windows", ProductName + ".exe");
        Build(BuildTarget.StandaloneWindows64, path);
    }

    [MenuItem("Build/Build macOS", priority = 2)]
    public static void BuildMacOS()
    {
        SyncVersionFromGit();
        string path = Path.Combine(BuildRoot, "macOS", ProductName + ".app");
        Build(BuildTarget.StandaloneOSX, path);
    }

    [MenuItem("Build/Build All (Windows + macOS)", priority = 0)]
    public static void BuildAll()
    {
        SyncVersionFromGit();
        BuildWindows();
        BuildMacOS();
    }

    private static void Build(BuildTarget target, string locationPathName)
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No enabled scenes in Build Settings!");
            return;
        }

        Debug.Log($"[BuildScript] Building {target} v{PlayerSettings.bundleVersion} -> {locationPathName}");
        Debug.Log($"[BuildScript] Scenes: {string.Join(", ", scenes)}");

        string dir = Path.GetDirectoryName(locationPathName);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            float sizeMB = summary.totalSize / (1024f * 1024f);
            Debug.Log($"[BuildScript] {target} v{PlayerSettings.bundleVersion} build succeeded: {sizeMB:F1} MB, {summary.totalTime.TotalSeconds:F1}s");
        }
        else
        {
            Debug.LogError($"[BuildScript] {target} build failed: {summary.result} ({summary.totalErrors} errors)");
        }
    }
}
