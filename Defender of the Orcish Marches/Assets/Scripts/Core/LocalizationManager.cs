using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight JSON-based localization system.
/// Loads per-language string tables from Resources/Localization/{code}.json.
/// Usage: LocalizationManager.L("menu.play") or LocalizationManager.L("hud.gold", amount)
/// </summary>
public static class LocalizationManager
{
    public enum Language { English, Italian, Spanish, French, German }

    private static readonly string[] LanguageCodes = { "en", "it", "es", "fr", "de" };
    private static readonly string[] LanguageNativeNames = { "English", "Italiano", "Español", "Français", "Deutsch" };

    public static event Action OnLanguageChanged;

    private static Dictionary<string, string> currentTable;
    private static Dictionary<string, string> englishTable; // fallback
    private static bool initialized;

    public static Language CurrentLanguage { get; private set; }

    [Serializable]
    private class StringTable
    {
        public StringEntry[] entries;
    }

    [Serializable]
    private class StringEntry
    {
        public string k;
        public string v;
    }

    /// <summary>
    /// Initialize with saved language preference. Call early (e.g. from GameSettings.ApplySettings or RuntimeInitializeOnLoadMethod).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (initialized) return;
        initialized = true;
        Language saved = GameSettings.CurrentLanguage;
        LoadLanguage(saved, fireEvent: false);
        Debug.Log($"[LocalizationManager] Initialized with language: {saved} ({GetLanguageCode(saved)})");
    }

    /// <summary>
    /// Primary lookup. Returns localized string for key, falling back to English, then the key itself.
    /// </summary>
    public static string L(string key)
    {
        if (!initialized) Init();

        if (currentTable != null && currentTable.TryGetValue(key, out string value))
            return value;

        // Fallback to English
        if (englishTable != null && englishTable.TryGetValue(key, out string enValue))
            return enValue;

        // Key not found in any table — return key itself for easy debugging
        return key;
    }

    /// <summary>
    /// Format string lookup. Retrieves localized format string and applies args via string.Format.
    /// </summary>
    public static string L(string key, params object[] args)
    {
        string format = L(key);
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            Debug.LogWarning($"[LocalizationManager] Format error for key '{key}': '{format}' with {args.Length} args");
            return format;
        }
    }

    /// <summary>
    /// Change the active language. Reloads string table and fires OnLanguageChanged.
    /// </summary>
    public static void SetLanguage(Language lang)
    {
        if (initialized && lang == CurrentLanguage) return;
        GameSettings.CurrentLanguage = lang;
        LoadLanguage(lang, fireEvent: true);
        Debug.Log($"[LocalizationManager] Language changed to {lang} ({GetLanguageCode(lang)})");
    }

    public static string GetLanguageCode(Language lang)
    {
        return LanguageCodes[(int)lang];
    }

    public static string GetLanguageNativeName(Language lang)
    {
        return LanguageNativeNames[(int)lang];
    }

    public static string[] GetAllNativeNames()
    {
        return LanguageNativeNames;
    }

    public static int LanguageCount => LanguageCodes.Length;

    private static void LoadLanguage(Language lang, bool fireEvent)
    {
        CurrentLanguage = lang;
        string code = GetLanguageCode(lang);

        // Always ensure English is loaded as fallback
        if (englishTable == null)
            englishTable = LoadTable("en");

        if (lang == Language.English)
        {
            currentTable = englishTable;
        }
        else
        {
            currentTable = LoadTable(code);
            if (currentTable == null)
            {
                Debug.LogWarning($"[LocalizationManager] No string table found for '{code}', falling back to English.");
                currentTable = englishTable;
            }
        }

        if (fireEvent)
            OnLanguageChanged?.Invoke();
    }

    private static Dictionary<string, string> LoadTable(string code)
    {
        var asset = Resources.Load<TextAsset>($"Localization/{code}");
        if (asset == null)
        {
            Debug.LogWarning($"[LocalizationManager] Could not load Resources/Localization/{code}.json");
            return null;
        }

        try
        {
            var table = JsonUtility.FromJson<StringTable>(asset.text);
            if (table?.entries == null)
            {
                Debug.LogError($"[LocalizationManager] String table '{code}' has null entries.");
                return null;
            }

            var dict = new Dictionary<string, string>(table.entries.Length);
            foreach (var entry in table.entries)
            {
                if (!string.IsNullOrEmpty(entry.k))
                    dict[entry.k] = entry.v ?? "";
            }

            Debug.Log($"[LocalizationManager] Loaded {dict.Count} strings from '{code}.json'.");
            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalizationManager] Failed to parse '{code}.json': {e.Message}");
            return null;
        }
    }
}
