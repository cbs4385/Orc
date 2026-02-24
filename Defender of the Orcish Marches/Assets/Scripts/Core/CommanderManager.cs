using UnityEngine;

/// <summary>
/// Manages commander class selection (per-run) and provides multiplier queries.
/// Static class — no MonoBehaviour needed.
/// </summary>
public static class CommanderManager
{
    private const string PREFS_KEY = "SelectedCommander";
    private static string activeCommanderId = CommanderDefs.NONE_ID;

    /// <summary>Get the currently selected commander ID for this run.</summary>
    public static string ActiveCommanderId => activeCommanderId;

    /// <summary>Get the active commander definition, or null if none selected.</summary>
    public static CommanderDef? ActiveCommander => CommanderDefs.GetById(activeCommanderId);

    /// <summary>Set the commander for the next run. Persisted in PlayerPrefs.</summary>
    public static void SelectCommander(string commanderId)
    {
        activeCommanderId = commanderId ?? CommanderDefs.NONE_ID;
        PlayerPrefs.SetString(PREFS_KEY, activeCommanderId);
        PlayerPrefs.Save();
        Debug.Log($"[CommanderManager] Commander selected: {activeCommanderId}");
    }

    /// <summary>Load the saved commander selection from PlayerPrefs.</summary>
    public static void LoadSelection()
    {
        activeCommanderId = PlayerPrefs.GetString(PREFS_KEY, CommanderDefs.NONE_ID);
        Debug.Log($"[CommanderManager] Loaded commander: {activeCommanderId}");
    }

    /// <summary>Clear commander selection (no class).</summary>
    public static void ClearSelection()
    {
        activeCommanderId = CommanderDefs.NONE_ID;
        PlayerPrefs.SetString(PREFS_KEY, activeCommanderId);
        PlayerPrefs.Save();
        Debug.Log("[CommanderManager] Commander cleared.");
    }

    /// <summary>Is a commander currently active?</summary>
    public static bool HasCommander => activeCommanderId != CommanderDefs.NONE_ID && ActiveCommander != null;

    /// <summary>Get the display name of the active commander, or "None".</summary>
    public static string GetActiveDisplayName()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.name : "None";
    }

    // --- Multiplier queries used by gameplay systems ---

    public static float GetWallHPMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.wallHPMultiplier : 1f;
    }

    public static float GetDefenderDamageMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.defenderDamageMultiplier : 1f;
    }

    public static float GetDefenderCostMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.defenderCostMultiplier : 1f;
    }

    public static float GetBallistaCostMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.ballistaCostMultiplier : 1f;
    }

    public static float GetLootValueMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.lootValueMultiplier : 1f;
    }

    public static float GetEnemyHPMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.enemyHPMultiplier : 1f;
    }

    public static float GetStartingMenialModifier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.startingMenialModifier : 0f;
    }

    public static float GetRefugeeSpawnMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.refugeeSpawnMultiplier : 1f;
    }

    public static float GetBallistaDamageMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.ballistaDamageMultiplier : 1f;
    }

    public static float GetBallistaFireRateMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.ballistaFireRateMultiplier : 1f;
    }

    public static float GetWallCostMultiplier()
    {
        var def = ActiveCommander;
        return def != null ? def.Value.wallCostMultiplier : 1f;
    }

    /// <summary>Log active commander state at run start.</summary>
    public static void LogActiveState()
    {
        var def = ActiveCommander;
        if (def == null)
        {
            Debug.Log("[CommanderManager] No commander selected.");
            return;
        }
        Debug.Log($"[CommanderManager] Active commander: {def.Value.name} ({def.Value.id}) - {def.Value.description}");
    }
}
