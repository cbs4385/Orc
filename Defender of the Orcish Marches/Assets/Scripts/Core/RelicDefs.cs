using System;

[Serializable]
public struct RelicDef
{
    public string id;
    public string name;
    public string description;

    // Multipliers (1.0 = no change, 0 = no effect on that stat)
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

    // Instant effects
    public int instantGold;
    public int instantMenials;
}

/// <summary>
/// Static registry of all relic/boon definitions offered between nights.
/// </summary>
public static class RelicDefs
{
    public static readonly RelicDef[] All = new RelicDef[]
    {
        // Offensive relics
        new RelicDef {
            id = "orcish_whetstone",
            name = "Orcish Whetstone",
            description = "Defenders deal +15% damage, but walls take +10% more damage",
            defenderDamageMultiplier = 1.15f,
            wallDamageTakenMultiplier = 1.1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "sharpened_bolts",
            name = "Sharpened Bolts",
            description = "Ballista damage +20%",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1.2f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "battle_fury",
            name = "Battle Fury",
            description = "Defender attack speed +20%, but enemy speed +10%",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1.1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1.2f
        },

        // Defensive relics
        new RelicDef {
            id = "reinforced_mortar",
            name = "Reinforced Mortar",
            description = "Walls take 15% less damage",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 0.85f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "slowing_wards",
            name = "Slowing Wards",
            description = "Enemy speed -15%",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 0.85f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },

        // Economy relics
        new RelicDef {
            id = "war_chest",
            name = "War Chest",
            description = "+30 gold immediately, but +20% more enemies next day",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1.2f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f,
            instantGold = 30
        },
        new RelicDef {
            id = "plunderers_charm",
            name = "Plunderer's Charm",
            description = "Loot value +20%",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1.2f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "refugee_beacon",
            name = "Refugee's Beacon",
            description = "+1 menial immediately, menials move 10% faster",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1.1f, defenderAttackSpeedMultiplier = 1f,
            instantMenials = 1
        },

        // Risk/reward relics
        new RelicDef {
            id = "blood_offering",
            name = "Blood Offering",
            description = "Enemy HP -10%, but enemies deal +15% damage to walls",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1.15f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 0.9f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "rapid_reload",
            name = "Rapid Reload",
            description = "Ballista fire rate +25%, but defender damage -10%",
            defenderDamageMultiplier = 0.9f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1.25f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "engineers_toolkit",
            name = "Engineer's Toolkit",
            description = "Menial speed +25%, loot value +10%",
            defenderDamageMultiplier = 1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1.1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1.25f, defenderAttackSpeedMultiplier = 1f
        },
        new RelicDef {
            id = "orcish_trophy",
            name = "Orcish Trophy",
            description = "All damage +10% (defenders and ballista)",
            defenderDamageMultiplier = 1.1f,
            wallDamageTakenMultiplier = 1f,
            lootValueMultiplier = 1f, spawnCountMultiplier = 1f,
            enemyHPMultiplier = 1f, enemySpeedMultiplier = 1f,
            ballistaDamageMultiplier = 1.1f, ballistaFireRateMultiplier = 1f,
            menialSpeedMultiplier = 1f, defenderAttackSpeedMultiplier = 1f
        }
    };

    public static RelicDef? GetById(string id)
    {
        foreach (var r in All)
        {
            if (r.id == id) return r;
        }
        return null;
    }
}
