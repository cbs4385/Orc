using UnityEngine;

/// <summary>
/// Static PlayerPrefs wrapper for persistent game settings.
/// </summary>
public static class GameSettings
{
    // --- Audio ---
    private const string KEY_SFX_VOLUME = "SfxVolume";
    private const float DEFAULT_SFX_VOLUME = 0.5f;

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
        set
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Debug.Log($"[GameSettings] SfxVolume set to {value:F2}");
        }
    }

    // --- Video ---
    private const string KEY_FULLSCREEN = "Fullscreen";

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = value;
            Debug.Log($"[GameSettings] Fullscreen set to {value}");
        }
    }

    /// <summary>
    /// Apply all saved settings to the running game systems.
    /// Call from game scene Start().
    /// </summary>
    public static void ApplySettings()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(SfxVolume);
            Debug.Log($"[GameSettings] Applied SfxVolume={SfxVolume:F2} to SoundManager.");
        }
    }

    public static void ResetToDefaults()
    {
        SfxVolume = DEFAULT_SFX_VOLUME;
        Fullscreen = true;
        Debug.Log("[GameSettings] Reset to defaults.");
    }
}
