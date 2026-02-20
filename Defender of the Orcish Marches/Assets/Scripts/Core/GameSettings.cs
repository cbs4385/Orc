using UnityEngine;

public enum Difficulty { Easy, Normal, Hard, Nightmare }

/// <summary>
/// Static PlayerPrefs wrapper for persistent game settings.
/// </summary>
public static class GameSettings
{
    // --- Audio ---
    private const string KEY_SFX_VOLUME = "SfxVolume";
    private const float DEFAULT_SFX_VOLUME = 0.5f;
    private const string KEY_MUSIC_VOLUME = "MusicVolume";
    private const float DEFAULT_MUSIC_VOLUME = 0.5f;

    // --- Difficulty ---
    private const string KEY_DIFFICULTY = "Difficulty";
    private const int DEFAULT_DIFFICULTY = (int)Difficulty.Normal;

    public static Difficulty CurrentDifficulty
    {
        get => (Difficulty)PlayerPrefs.GetInt(KEY_DIFFICULTY, DEFAULT_DIFFICULTY);
        set
        {
            PlayerPrefs.SetInt(KEY_DIFFICULTY, (int)value);
            PlayerPrefs.Save();
            Debug.Log($"[GameSettings] Difficulty set to {value}");
        }
    }

    public static int GetStartingGold()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 150;
            case Difficulty.Normal:    return 100;
            case Difficulty.Hard:      return 75;
            case Difficulty.Nightmare: return 50;
            default:                   return 100;
        }
    }

    public static int GetStartingMenials()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 4;
            case Difficulty.Normal:    return 3;
            case Difficulty.Hard:      return 2;
            case Difficulty.Nightmare: return 1;
            default:                   return 3;
        }
    }

    /// <summary>Multiplier on spawn timer. Lower = faster spawns = harder.</summary>
    public static float GetSpawnRateMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 1.5f;
            case Difficulty.Normal:    return 1.0f;
            case Difficulty.Hard:      return 0.75f;
            case Difficulty.Nightmare: return 0.5f;
            default:                   return 1.0f;
        }
    }

    /// <summary>Multiplier on enemy HP at spawn. Higher = tougher.</summary>
    public static float GetEnemyHPMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 0.8f;
            case Difficulty.Normal:    return 1.0f;
            case Difficulty.Hard:      return 1.25f;
            case Difficulty.Nightmare: return 1.5f;
            default:                   return 1.0f;
        }
    }

    /// <summary>Multiplier on day phase duration. Higher = longer days = easier.</summary>
    public static float GetDayDurationMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 1.3f;
            case Difficulty.Normal:    return 1.0f;
            case Difficulty.Hard:      return 0.8f;
            case Difficulty.Nightmare: return 0.7f;
            default:                   return 1.0f;
        }
    }

    /// <summary>Multiplier on night phase duration. Higher = longer nights = more repair time.</summary>
    public static float GetNightDurationMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 1.3f;
            case Difficulty.Normal:    return 1.0f;
            case Difficulty.Hard:      return 0.8f;
            case Difficulty.Nightmare: return 0.7f;
            default:                   return 1.0f;
        }
    }

    /// <summary>Multiplier on refugee spawn interval. Higher = longer waits = fewer refugees = harder.</summary>
    public static float GetRefugeeSpawnMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return 0.7f;
            case Difficulty.Normal:    return 1.0f;
            case Difficulty.Hard:      return 1.3f;
            case Difficulty.Nightmare: return 1.6f;
            default:                   return 1.0f;
        }
    }

    public static string GetDifficultyName()
    {
        switch (CurrentDifficulty)
        {
            case Difficulty.Easy:      return "Easy";
            case Difficulty.Normal:    return "Normal";
            case Difficulty.Hard:      return "Hard";
            case Difficulty.Nightmare: return "Nightmare";
            default:                   return "Normal";
        }
    }

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

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
        set
        {
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Debug.Log($"[GameSettings] MusicVolume set to {value:F2}");
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
            SoundManager.Instance.SetMusicVolume(MusicVolume);
            SoundManager.Instance.StartMusic();
            Debug.Log($"[GameSettings] Applied SfxVolume={SfxVolume:F2}, MusicVolume={MusicVolume:F2} to SoundManager.");
        }
    }

    public static void ResetToDefaults()
    {
        SfxVolume = DEFAULT_SFX_VOLUME;
        MusicVolume = DEFAULT_MUSIC_VOLUME;
        Fullscreen = true;
        CurrentDifficulty = (Difficulty)DEFAULT_DIFFICULTY;
        Debug.Log("[GameSettings] Reset to defaults.");
    }
}
