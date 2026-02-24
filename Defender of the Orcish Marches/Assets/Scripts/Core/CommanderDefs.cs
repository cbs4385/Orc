using System;

[Serializable]
public struct CommanderDef
{
    public string id;
    public string name;
    public string description;

    // Multipliers (1.0 = no change)
    public float wallHPMultiplier;
    public float defenderDamageMultiplier;
    public float defenderCostMultiplier;
    public float ballistaCostMultiplier;
    public float lootValueMultiplier;
    public float enemyHPMultiplier;
    public float startingMenialModifier; // added to starting menials (can be negative)
    public float refugeeSpawnMultiplier;
    public float ballistaDamageMultiplier;
    public float ballistaFireRateMultiplier;
    public float wallCostMultiplier;
}

/// <summary>
/// Static registry of all commander class definitions.
/// </summary>
public static class CommanderDefs
{
    public const string NONE_ID = "none";

    public static readonly CommanderDef[] All = new CommanderDef[]
    {
        new CommanderDef {
            id = "warden",
            name = "Warden",
            description = "Walls have +30% HP and cost 20% less, but defenders cost 30% more",
            wallHPMultiplier = 1.3f,
            defenderDamageMultiplier = 1f,
            defenderCostMultiplier = 1.3f,
            ballistaCostMultiplier = 1f,
            lootValueMultiplier = 1f,
            enemyHPMultiplier = 1f,
            startingMenialModifier = 0f,
            refugeeSpawnMultiplier = 1f,
            ballistaDamageMultiplier = 1f,
            ballistaFireRateMultiplier = 1f,
            wallCostMultiplier = 0.8f
        },
        new CommanderDef {
            id = "captain",
            name = "Captain",
            description = "Defenders deal +20% damage and cost 20% less, but walls have -20% HP",
            wallHPMultiplier = 0.8f,
            defenderDamageMultiplier = 1.2f,
            defenderCostMultiplier = 0.8f,
            ballistaCostMultiplier = 1f,
            lootValueMultiplier = 1f,
            enemyHPMultiplier = 1f,
            startingMenialModifier = 0f,
            refugeeSpawnMultiplier = 1f,
            ballistaDamageMultiplier = 1f,
            ballistaFireRateMultiplier = 1f,
            wallCostMultiplier = 1f
        },
        new CommanderDef {
            id = "artificer",
            name = "Artificer",
            description = "Ballista upgrades cost 50% less, +25% ballista damage, but start with 1 fewer menial",
            wallHPMultiplier = 1f,
            defenderDamageMultiplier = 1f,
            defenderCostMultiplier = 1f,
            ballistaCostMultiplier = 0.5f,
            lootValueMultiplier = 1f,
            enemyHPMultiplier = 1f,
            startingMenialModifier = -1f,
            refugeeSpawnMultiplier = 1f,
            ballistaDamageMultiplier = 1.25f,
            ballistaFireRateMultiplier = 1f,
            wallCostMultiplier = 1f
        },
        new CommanderDef {
            id = "merchant",
            name = "Merchant",
            description = "Loot value +40% and refugees arrive 30% faster, but enemies have +15% HP",
            wallHPMultiplier = 1f,
            defenderDamageMultiplier = 1f,
            defenderCostMultiplier = 1f,
            ballistaCostMultiplier = 1f,
            lootValueMultiplier = 1.4f,
            enemyHPMultiplier = 1.15f,
            startingMenialModifier = 0f,
            refugeeSpawnMultiplier = 0.7f,
            ballistaDamageMultiplier = 1f,
            ballistaFireRateMultiplier = 1f,
            wallCostMultiplier = 1f
        }
    };

    public static CommanderDef? GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || id == NONE_ID) return null;
        foreach (var c in All)
        {
            if (c.id == id) return c;
        }
        return null;
    }
}
