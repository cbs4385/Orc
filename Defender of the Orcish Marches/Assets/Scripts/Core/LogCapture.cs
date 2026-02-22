using UnityEngine;
using System;
using System.IO;
using System.Text;

public class LogCapture : MonoBehaviour
{
    public static LogCapture Instance { get; private set; }

    private StringBuilder buffer = new StringBuilder();
    private string logFilePath;
    private float flushTimer;
    private const float FLUSH_INTERVAL = 5f;
    private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        logFilePath = Path.Combine(Application.persistentDataPath, "game_log.txt");

        // Clear previous session's log
        try
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);
        }
        catch (Exception) { }

        Debug.Log($"[LogCapture] Initialized. Log file: {logFilePath}");

        // Write system info header
        buffer.AppendLine("=== Game Session Started ===");
        buffer.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        buffer.AppendLine($"Version: {Application.version}");
        buffer.AppendLine($"Platform: {Application.platform}");
        buffer.AppendLine($"OS: {SystemInfo.operatingSystem}");
        buffer.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        buffer.AppendLine($"RAM: {SystemInfo.systemMemorySize}MB");
        buffer.AppendLine($"Difficulty: {GameSettings.GetDifficultyName()}");
        buffer.AppendLine("============================");

        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Application.logMessageReceived += OnLogMessageReceived;
        }
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        FlushBuffer();
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        FlushBuffer();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        flushTimer += Time.unscaledDeltaTime;
        if (flushTimer >= FLUSH_INTERVAL)
        {
            flushTimer = 0f;
            FlushBuffer();
        }
    }

    private void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        string prefix;
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                prefix = "ERROR";
                break;
            case LogType.Warning:
                prefix = "WARN";
                break;
            default:
                prefix = "LOG";
                break;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        buffer.AppendLine($"[{timestamp}] [{prefix}] {message}");

        if (type == LogType.Error || type == LogType.Exception)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                buffer.AppendLine(stackTrace);
            }
        }
    }

    private void FlushBuffer()
    {
        if (buffer.Length == 0) return;

        try
        {
            File.AppendAllText(logFilePath, buffer.ToString());
            buffer.Clear();
            TruncateIfNeeded();
        }
        catch (Exception e)
        {
            // Can't use Debug.Log here — would cause recursion
            buffer.Clear();
            buffer.AppendLine($"[LogCapture] Failed to flush: {e.Message}");
        }
    }

    private void TruncateIfNeeded()
    {
        try
        {
            var info = new FileInfo(logFilePath);
            if (info.Exists && info.Length > MAX_FILE_SIZE)
            {
                string content = File.ReadAllText(logFilePath);
                int halfPoint = content.Length / 2;
                // Find the next newline after the halfway point
                int cutPoint = content.IndexOf('\n', halfPoint);
                if (cutPoint < 0) cutPoint = halfPoint;
                string truncated = "=== LOG TRUNCATED ===\n" + content.Substring(cutPoint + 1);
                File.WriteAllText(logFilePath, truncated);
            }
        }
        catch (Exception)
        {
            // Silently ignore truncation failures
        }
    }

    /// <summary>
    /// Flushes the buffer and returns the path to the log file.
    /// Call this before zipping the log for a bug report.
    /// </summary>
    public string FlushAndGetPath()
    {
        FlushBuffer();
        Debug.Log($"[LogCapture] Flushed log. File: {logFilePath}");
        return logFilePath;
    }
}
