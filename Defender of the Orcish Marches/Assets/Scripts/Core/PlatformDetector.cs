using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Static utility class for platform detection.
/// IsMobile = iOS/Android only. HasTouchscreen = any device with touch (includes Surface).
/// IsDesktop = Windows/Mac/Linux (includes Surface).
/// </summary>
public static class PlatformDetector
{
    private const string KEY_SHOW_ONSCREEN_CONTROLS = "ShowOnScreenControls";

#if UNITY_EDITOR
    /// <summary>Editor-only override for testing mobile mode. null = no override.</summary>
    public static bool? EditorMobileOverride { get; set; }
#endif

    /// <summary>True on iOS/Android only.</summary>
    public static bool IsMobile
    {
        get
        {
#if UNITY_EDITOR
            if (EditorMobileOverride.HasValue) return EditorMobileOverride.Value;
#endif
            return Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }

    /// <summary>True if a touchscreen is present (mobile, Surface, touch-capable laptops).</summary>
    public static bool HasTouchscreen
    {
        get
        {
#if UNITY_EDITOR
            if (EditorMobileOverride.HasValue && EditorMobileOverride.Value) return true;
#endif
            return Touchscreen.current != null;
        }
    }

    /// <summary>True on Windows/Mac/Linux (includes Surface).</summary>
    public static bool IsDesktop
    {
        get
        {
#if UNITY_EDITOR
            return !EditorMobileOverride.GetValueOrDefault(false);
#endif
#pragma warning disable CS0162
            return Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.OSXPlayer
                || Application.platform == RuntimePlatform.LinuxPlayer;
#pragma warning restore CS0162
        }
    }

    /// <summary>
    /// Whether on-screen touch controls should be shown.
    /// Defaults to true on mobile, false on desktop. Persisted via PlayerPrefs.
    /// </summary>
    public static bool ShowOnScreenControls
    {
        get
        {
            int defaultVal = IsMobile ? 1 : 0;
            return PlayerPrefs.GetInt(KEY_SHOW_ONSCREEN_CONTROLS, defaultVal) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(KEY_SHOW_ONSCREEN_CONTROLS, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[PlatformDetector] ShowOnScreenControls set to {value}");
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Toggle Mobile Override")]
    private static void ToggleMobileOverride()
    {
        if (EditorMobileOverride.HasValue && EditorMobileOverride.Value)
        {
            EditorMobileOverride = null;
            Debug.Log("[PlatformDetector] Editor mobile override DISABLED (using real platform).");
        }
        else
        {
            EditorMobileOverride = true;
            Debug.Log("[PlatformDetector] Editor mobile override ENABLED (simulating mobile).");
        }
    }
#endif
}
