using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MutatorDef
{
    public string id;
    public string name;
    public string description;
    public string achievementRequired; // future: gold-tier achievement ID
    public float scoreMultiplier;
    public string[] incompatibleWith;
}

/// <summary>
/// Static registry of all mutator definitions.
/// </summary>
public static class MutatorDefs
{
    public static readonly MutatorDef[] All = new MutatorDef[]
    {
        new MutatorDef {
            id = "blood_tide",
            name = "Blood Tide",
            description = "Enemy count per day +50%, loot drop +30%",
            achievementRequired = "orc_slayer_gold",
            scoreMultiplier = 1.3f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "glass_fortress",
            name = "Glass Fortress",
            description = "Wall HP halved, defenders deal +50% damage",
            achievementRequired = "stand_your_ground_gold",
            scoreMultiplier = 1.4f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "lone_ballista",
            name = "Lone Ballista",
            description = "Cannot hire defenders; ballista damage +100%, fire rate +50%",
            achievementRequired = "sharpshooter_gold",
            scoreMultiplier = 1.8f,
            incompatibleWith = new string[] { "pacifist_run" }
        },
        new MutatorDef {
            id = "golden_horde",
            name = "Golden Horde",
            description = "All costs doubled, loot value tripled",
            achievementRequired = "treasure_hoarder_gold",
            scoreMultiplier = 1.2f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "night_terrors",
            name = "Night Terrors",
            description = "Full enemy waves also spawn at night (no safe period)",
            achievementRequired = "nightwatch_gold",
            scoreMultiplier = 1.6f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "skeleton_crew",
            name = "Skeleton Crew",
            description = "Start with 1 menial; refugees arrive 2x faster",
            achievementRequired = "specialist_gold",
            scoreMultiplier = 1.3f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "iron_march",
            name = "Iron March",
            description = "Enemies have +30% speed, cannot be slowed",
            achievementRequired = "against_all_odds_gold",
            scoreMultiplier = 1.5f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "bounty_hunter",
            name = "Bounty Hunter",
            description = "Boss spawns every 5 days instead of day 10+; boss loot = 50g",
            achievementRequired = "boss_hunter_gold",
            scoreMultiplier = 1.4f,
            incompatibleWith = new string[0]
        },
        new MutatorDef {
            id = "pacifist_run",
            name = "Pacifist Run",
            description = "Defenders cannot attack; they only repair and body-block",
            achievementRequired = "army_builder_gold",
            scoreMultiplier = 2.0f,
            incompatibleWith = new string[] { "lone_ballista" }
        },
        new MutatorDef {
            id = "chaos_modifiers",
            name = "Chaos Modifiers",
            description = "Daily event multipliers are 2x stronger",
            achievementRequired = "score_legend_gold",
            scoreMultiplier = 1.3f,
            incompatibleWith = new string[0]
        }
    };

    public static MutatorDef? GetById(string id)
    {
        foreach (var m in All)
        {
            if (m.id == id) return m;
        }
        return null;
    }
}
