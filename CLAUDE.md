# Defender of the Orcish Marches - Development Guidelines

## Logging Standards

All scripts MUST include comprehensive Debug.Log statements for diagnosing issues quickly. Follow these conventions:

### Log Format
- Use `[ClassName]` prefix for all log messages: `Debug.Log("[Enemy] message");`
- Include relevant variable values in log messages using string interpolation
- Include position data when relevant: `$"at {transform.position}"`

### When to Log
- **State changes**: spawning, dying, initializing, being destroyed
- **Important decisions**: target selection, pathfinding, state machine transitions
- **Resource flow**: gold gained/spent, loot spawned/collected, upgrades applied
- **Combat events**: damage dealt, damage taken, attacks triggered
- **Errors/warnings**: null references, missing components, failed operations
- **Wave/spawn events**: enemy spawned (type, position), wave started/ended

### Log Levels
- `Debug.Log()` - Normal game events (spawn, die, collect, attack)
- `Debug.LogWarning()` - Unexpected but recoverable situations (missing optional reference, fallback used)
- `Debug.LogError()` - Failures that prevent intended behavior (null prefab, missing component, broken reference)

### Example Patterns
```csharp
// Initialization
Debug.Log($"[Enemy] Initialized: {data.enemyName}, HP={data.maxHP}, type={data.enemyType}");

// State change
Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}. treasureDrop={data.treasureDrop}");

// Null guard with error
if (prefab == null)
{
    Debug.LogError("[EnemySpawnManager] enemyPrefab is null! Cannot spawn enemy.");
    return;
}

// Decision point
Debug.Log($"[EnemyMovement] Targeting {currentTarget.name} at dist={bestDist:F1}");
```

### Scripts That Must Have Logging
Every script in the project should have logging at key decision points. Priority scripts:
- Enemy.cs, EnemyMovement.cs, EnemyAttack.cs, EnemySpawnManager.cs
- GameManager.cs (state transitions, game over, score)
- Defender.cs and subclasses (targeting, attacking)
- Ballista.cs, BallistaProjectile.cs (firing, hitting)
- TreasurePickup.cs (spawning, collection)
- Menial.cs, MenialManager.cs (task assignment, death)
- Refugee.cs, RefugeeSpawner.cs (spawning, reaching gate)
- Wall.cs, WallManager.cs, Gate.cs (damage, breaches)
- UpgradeManager.cs (purchases, applications)
