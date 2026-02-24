using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages mutator unlock state (persistent) and active selection (per-run).
/// Static class — no MonoBehaviour needed.
/// </summary>
public static class MutatorManager
{
    private const string PREFS_KEY = "UnlockedMutators";
    private const float MAX_SCORE_MULTIPLIER = 5.0f;

    private static HashSet<string> unlockedIds;
    private static HashSet<string> activeIds = new HashSet<string>();

    // --- Unlock persistence ---

    private static HashSet<string> LoadUnlocked()
    {
        if (unlockedIds != null) return unlockedIds;

        unlockedIds = new HashSet<string>();
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                if (wrapper != null && wrapper.items != null)
                {
                    foreach (var id in wrapper.items)
                        unlockedIds.Add(id);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MutatorManager] Failed to parse unlocked mutators: {e.Message}");
            }
        }
        Debug.Log($"[MutatorManager] Loaded {unlockedIds.Count} unlocked mutators.");
        return unlockedIds;
    }

    private static void SaveUnlocked()
    {
        var wrapper = new StringListWrapper { items = new List<string>(LoadUnlocked()) };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    [Serializable]
    private class StringListWrapper
    {
        public List<string> items;
    }

    public static bool IsUnlocked(string mutatorId)
    {
        return LoadUnlocked().Contains(mutatorId);
    }

    public static void Unlock(string mutatorId)
    {
        if (LoadUnlocked().Add(mutatorId))
        {
            SaveUnlocked();
            Debug.Log($"[MutatorManager] Unlocked mutator: {mutatorId}");
        }
    }

    /// <summary>Unlock all mutators (debug/testing).</summary>
    public static void UnlockAll()
    {
        foreach (var def in MutatorDefs.All)
            LoadUnlocked().Add(def.id);
        SaveUnlocked();
        Debug.Log("[MutatorManager] All mutators unlocked.");
    }

    public static void ClearUnlocks()
    {
        unlockedIds = new HashSet<string>();
        SaveUnlocked();
        activeIds.Clear();
        Debug.Log("[MutatorManager] All unlocks cleared.");
    }

    // --- Active mutators (per-run) ---

    public static IReadOnlyCollection<string> ActiveMutatorIds => activeIds;

    public static bool IsActive(string mutatorId)
    {
        return activeIds.Contains(mutatorId);
    }

    /// <summary>
    /// Toggle a mutator on/off for the next run. Returns false if locked or incompatible.
    /// </summary>
    public static bool ToggleActive(string mutatorId)
    {
        if (activeIds.Contains(mutatorId))
        {
            activeIds.Remove(mutatorId);
            Debug.Log($"[MutatorManager] Deactivated mutator: {mutatorId}");
            return true;
        }

        if (!IsUnlocked(mutatorId))
        {
            Debug.LogWarning($"[MutatorManager] Cannot activate locked mutator: {mutatorId}");
            return false;
        }

        // Check incompatibilities
        var def = MutatorDefs.GetById(mutatorId);
        if (def == null) return false;

        foreach (var incompat in def.Value.incompatibleWith)
        {
            if (activeIds.Contains(incompat))
            {
                activeIds.Remove(incompat);
                Debug.Log($"[MutatorManager] Auto-disabled incompatible mutator: {incompat}");
            }
        }

        activeIds.Add(mutatorId);
        Debug.Log($"[MutatorManager] Activated mutator: {mutatorId}. Active count: {activeIds.Count}");
        return true;
    }

    public static void SetActive(string mutatorId, bool active)
    {
        if (active && !activeIds.Contains(mutatorId))
            ToggleActive(mutatorId);
        else if (!active && activeIds.Contains(mutatorId))
        {
            activeIds.Remove(mutatorId);
            Debug.Log($"[MutatorManager] Deactivated mutator: {mutatorId}");
        }
    }

    public static void ClearActive()
    {
        activeIds.Clear();
        Debug.Log("[MutatorManager] Active mutators cleared for new run.");
    }

    // --- Query helpers for gameplay code ---

    /// <summary>Combined score multiplier from all active mutators, capped at 5.0x.</summary>
    public static float GetScoreMultiplier()
    {
        float mult = 1f;
        foreach (var id in activeIds)
        {
            var def = MutatorDefs.GetById(id);
            if (def != null) mult *= def.Value.scoreMultiplier;
        }
        return Mathf.Min(mult, MAX_SCORE_MULTIPLIER);
    }

    /// <summary>Returns active mutator count.</summary>
    public static int ActiveCount => activeIds.Count;

    /// <summary>Returns a display-friendly list of active mutator names.</summary>
    public static string GetActiveNamesDisplay()
    {
        if (activeIds.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var id in activeIds)
        {
            var def = MutatorDefs.GetById(id);
            if (def != null)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append(def.Value.name);
            }
        }
        return sb.ToString();
    }

    /// <summary>Log active mutators at run start.</summary>
    public static void LogActiveState()
    {
        if (activeIds.Count == 0)
        {
            Debug.Log("[MutatorManager] No active mutators.");
            return;
        }
        Debug.Log($"[MutatorManager] Active mutators ({activeIds.Count}): {GetActiveNamesDisplay()}, score mult={GetScoreMultiplier():F2}x");
    }
}
