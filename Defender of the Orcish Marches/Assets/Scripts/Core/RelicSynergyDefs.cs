using System;
using System.Collections.Generic;

[Serializable]
public struct RelicSynergyDef
{
    public string id;
    public string name;
    public string description;
    public string[] requiredRelicIds;

    // Bonus multipliers (1.0 = no bonus for that stat)
    public float defenderDamageMultiplier;
    public float wallDamageTakenMultiplier;
    public float lootValueMultiplier;
    public float spawnCountMultiplier;
    public float enemyHPMultiplier;
    public float enemySpeedMultiplier;
    public float ballistaDamageMultiplier;
    public float ballistaFireRateMultiplier;
    public float menialSpeedMultiplier;
    public float defenderAttackSpeedMultiplier;
}

/// <summary>
/// Static registry of relic synergy definitions. When a player collects all required
/// relics for a synergy, the bonus effects activate on top of individual relic bonuses.
/// </summary>
public static class RelicSynergyDefs
{
    public static readonly RelicSynergyDef[] All = new RelicSynergyDef[]
    {
        // --- 2-Relic Synergies ---

        new RelicSynergyDef
        {
            id = "berserkers_rage",
            name = "Berserker's Rage",
            description = "Your fury feeds on itself. Defenders deal +20% bonus damage.",
            requiredRelicIds = new[] { "orcish_whetstone", "battle_fury" },
            defenderDamageMultiplier = 1.2f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "siege_engine",
            name = "Siege Engine",
            description = "A finely tuned instrument of war. Ballista damage +15%.",
            requiredRelicIds = new[] { "sharpened_bolts", "rapid_reload" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1.15f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "unbreakable",
            name = "Unbreakable",
            description = "They can barely scratch the surface. Walls take 10% less damage.",
            requiredRelicIds = new[] { "reinforced_mortar", "slowing_wards" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 0.9f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "golden_age",
            name = "Golden Age",
            description = "Wealth begets wealth. Loot value +15%.",
            requiredRelicIds = new[] { "war_chest", "plunderers_charm" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1.15f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "industrious",
            name = "Industrious",
            description = "Many hands make light work. Menial speed +15%.",
            requiredRelicIds = new[] { "refugee_beacon", "engineers_toolkit" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1.15f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "calculated_risk",
            name = "Calculated Risk",
            description = "Sacrifice wisely, and the walls hold firm. Walls take 20% less damage.",
            requiredRelicIds = new[] { "blood_offering", "reinforced_mortar" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 0.8f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },

        // --- 3-Relic Synergies (more powerful, harder to assemble) ---

        new RelicSynergyDef
        {
            id = "orcish_arsenal",
            name = "Orcish Arsenal",
            description = "Every weapon honed to perfection. All damage +15%.",
            requiredRelicIds = new[] { "orcish_whetstone", "sharpened_bolts", "orcish_trophy" },
            defenderDamageMultiplier = 1.15f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1.15f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicSynergyDef
        {
            id = "war_machine",
            name = "War Machine",
            description = "An unstoppable engine of destruction. All attack speed +15%.",
            requiredRelicIds = new[] { "battle_fury", "rapid_reload", "orcish_trophy" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1.15f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1.15f
        },
        new RelicSynergyDef
        {
            id = "fortunes_gambit",
            name = "Fortune's Gambit",
            description = "The greater the risk, the greater the reward. Loot +25%, enemy HP -15%.",
            requiredRelicIds = new[] { "blood_offering", "war_chest", "plunderers_charm" },
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f, lootValueMultiplier = 1.25f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 0.85f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
    };

    /// <summary>
    /// Check if a synergy is active given the collected relic IDs.
    /// </summary>
    public static bool IsActive(RelicSynergyDef synergy, IReadOnlyList<string> collectedIds)
    {
        foreach (var reqId in synergy.requiredRelicIds)
        {
            bool found = false;
            for (int i = 0; i < collectedIds.Count; i++)
            {
                if (collectedIds[i] == reqId) { found = true; break; }
            }
            if (!found) return false;
        }
        return true;
    }

    /// <summary>
    /// Returns synergies that would become newly active if the given relic ID
    /// were added to the collection. Only returns synergies not already active.
    /// </summary>
    public static List<RelicSynergyDef> GetPendingSynergies(string candidateRelicId, IReadOnlyList<string> collectedIds)
    {
        var result = new List<RelicSynergyDef>();
        foreach (var synergy in All)
        {
            if (IsActive(synergy, collectedIds)) continue;

            // Check if candidate is one of the required relics
            bool candidateIsRequired = false;
            foreach (var reqId in synergy.requiredRelicIds)
            {
                if (reqId == candidateRelicId) { candidateIsRequired = true; break; }
            }
            if (!candidateIsRequired) continue;

            // Check if all OTHER required relics are already collected
            bool allOthersPresent = true;
            foreach (var reqId in synergy.requiredRelicIds)
            {
                if (reqId == candidateRelicId) continue;
                bool found = false;
                for (int i = 0; i < collectedIds.Count; i++)
                {
                    if (collectedIds[i] == reqId) { found = true; break; }
                }
                if (!found) { allOthersPresent = false; break; }
            }
            if (allOthersPresent) result.Add(synergy);
        }
        return result;
    }

    public static RelicSynergyDef? GetById(string id)
    {
        foreach (var s in All)
        {
            if (s.id == id) return s;
        }
        return null;
    }
}
