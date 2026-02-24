using System;

public enum AchievementTier { None, Bronze, Silver, Gold }
public enum AchievementCategory { Survival, Combat, Economy, Defender, Special }

[Serializable]
public struct AchievementDef
{
    public string id;
    public string name;
    public string description;
    public AchievementCategory category;
    public int bronzeThreshold;
    public int silverThreshold;
    public int goldThreshold;
    public bool cumulative; // true = lifetime total, false = single-run best
    public string mutatorUnlock; // mutator id unlocked at gold tier (empty = none)
}

/// <summary>
/// Static registry of all achievement definitions grouped by category.
/// </summary>
public static class AchievementDefs
{
    public static readonly AchievementDef[] All = new AchievementDef[]
    {
        // --- Survival (4) ---
        new AchievementDef {
            id = "stand_your_ground", name = "Stand Your Ground",
            description = "Survive days in a single run",
            category = AchievementCategory.Survival,
            bronzeThreshold = 5, silverThreshold = 10, goldThreshold = 20,
            cumulative = false, mutatorUnlock = "glass_fortress"
        },
        new AchievementDef {
            id = "nightwatch", name = "Nightwatch",
            description = "Survive total days across all runs",
            category = AchievementCategory.Survival,
            bronzeThreshold = 20, silverThreshold = 50, goldThreshold = 100,
            cumulative = true, mutatorUnlock = "night_terrors"
        },
        new AchievementDef {
            id = "against_all_odds", name = "Against All Odds",
            description = "Survive days on Hard or Nightmare",
            category = AchievementCategory.Survival,
            bronzeThreshold = 3, silverThreshold = 7, goldThreshold = 15,
            cumulative = false, mutatorUnlock = "iron_march"
        },
        new AchievementDef {
            id = "veteran", name = "Veteran",
            description = "Complete total runs",
            category = AchievementCategory.Survival,
            bronzeThreshold = 5, silverThreshold = 15, goldThreshold = 30,
            cumulative = true, mutatorUnlock = ""
        },

        // --- Combat (4) ---
        new AchievementDef {
            id = "orc_slayer", name = "Orc Slayer",
            description = "Kill enemies in a single run",
            category = AchievementCategory.Combat,
            bronzeThreshold = 25, silverThreshold = 75, goldThreshold = 200,
            cumulative = false, mutatorUnlock = "blood_tide"
        },
        new AchievementDef {
            id = "sharpshooter", name = "Sharpshooter",
            description = "Fire ballista shots across all runs",
            category = AchievementCategory.Combat,
            bronzeThreshold = 50, silverThreshold = 200, goldThreshold = 500,
            cumulative = true, mutatorUnlock = "lone_ballista"
        },
        new AchievementDef {
            id = "boss_hunter", name = "Boss Hunter",
            description = "Kill bosses across all runs",
            category = AchievementCategory.Combat,
            bronzeThreshold = 1, silverThreshold = 5, goldThreshold = 15,
            cumulative = true, mutatorUnlock = "bounty_hunter"
        },
        new AchievementDef {
            id = "exterminator", name = "Exterminator",
            description = "Kill enemies across all runs",
            category = AchievementCategory.Combat,
            bronzeThreshold = 100, silverThreshold = 500, goldThreshold = 1500,
            cumulative = true, mutatorUnlock = ""
        },

        // --- Economy (4) ---
        new AchievementDef {
            id = "treasure_hoarder", name = "Treasure Hoarder",
            description = "Earn gold in a single run",
            category = AchievementCategory.Economy,
            bronzeThreshold = 100, silverThreshold = 300, goldThreshold = 750,
            cumulative = false, mutatorUnlock = "golden_horde"
        },
        new AchievementDef {
            id = "refugee_savior", name = "Refugee Savior",
            description = "Save refugees across all runs",
            category = AchievementCategory.Economy,
            bronzeThreshold = 10, silverThreshold = 30, goldThreshold = 75,
            cumulative = true, mutatorUnlock = ""
        },
        new AchievementDef {
            id = "builder", name = "Builder",
            description = "Build walls across all runs",
            category = AchievementCategory.Economy,
            bronzeThreshold = 10, silverThreshold = 30, goldThreshold = 80,
            cumulative = true, mutatorUnlock = ""
        },
        new AchievementDef {
            id = "land_clearer", name = "Land Clearer",
            description = "Clear vegetation across all runs",
            category = AchievementCategory.Economy,
            bronzeThreshold = 20, silverThreshold = 60, goldThreshold = 150,
            cumulative = true, mutatorUnlock = ""
        },

        // --- Defender (4) ---
        new AchievementDef {
            id = "army_builder", name = "Army Builder",
            description = "Hire defenders in a single run",
            category = AchievementCategory.Defender,
            bronzeThreshold = 3, silverThreshold = 8, goldThreshold = 15,
            cumulative = false, mutatorUnlock = "pacifist_run"
        },
        new AchievementDef {
            id = "specialist", name = "Specialist",
            description = "Hire defenders across all runs",
            category = AchievementCategory.Defender,
            bronzeThreshold = 10, silverThreshold = 30, goldThreshold = 75,
            cumulative = true, mutatorUnlock = "skeleton_crew"
        },
        new AchievementDef {
            id = "menial_guardian", name = "Menial Guardian",
            description = "Complete a run losing fewer than N menials",
            category = AchievementCategory.Defender,
            bronzeThreshold = 5, silverThreshold = 3, goldThreshold = 1,
            cumulative = false, mutatorUnlock = ""
        },
        new AchievementDef {
            id = "peak_commander", name = "Peak Commander",
            description = "Have defenders alive simultaneously",
            category = AchievementCategory.Defender,
            bronzeThreshold = 3, silverThreshold = 6, goldThreshold = 10,
            cumulative = false, mutatorUnlock = ""
        },

        // --- Special (4) ---
        new AchievementDef {
            id = "score_legend", name = "Score Legend",
            description = "Reach a composite score in a single run",
            category = AchievementCategory.Special,
            bronzeThreshold = 5000, silverThreshold = 15000, goldThreshold = 40000,
            cumulative = false, mutatorUnlock = "chaos_modifiers"
        },
        new AchievementDef {
            id = "speed_runner", name = "Speed Runner",
            description = "Kill a boss within N seconds of game start",
            category = AchievementCategory.Special,
            bronzeThreshold = 300, silverThreshold = 180, goldThreshold = 120,
            cumulative = false, mutatorUnlock = ""
        },
        new AchievementDef {
            id = "wall_repair_master", name = "Wall Repair Master",
            description = "Repair total wall HP across all runs",
            category = AchievementCategory.Special,
            bronzeThreshold = 100, silverThreshold = 500, goldThreshold = 1500,
            cumulative = true, mutatorUnlock = ""
        },
        new AchievementDef {
            id = "iron_will", name = "Iron Will",
            description = "Survive 10+ days on Nightmare difficulty",
            category = AchievementCategory.Special,
            bronzeThreshold = 5, silverThreshold = 10, goldThreshold = 15,
            cumulative = false, mutatorUnlock = ""
        }
    };

    public static AchievementDef? GetById(string id)
    {
        foreach (var a in All)
        {
            if (a.id == id) return a;
        }
        return null;
    }
}
