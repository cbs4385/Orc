# Changelog

## 0.13.0

### Unlockable Mutators
- Added 10 game-modifying mutators that can be toggled before a run from the main menu
- **Blood Tide** — 50% more enemies per wave, loot value +30% (×1.5 score)
- **Iron March** — enemies move 30% faster (×1.3 score)
- **Glass Fortress** — walls have half HP, defenders deal 50% more damage (×1.4 score)
- **Night Terrors** — full enemy waves spawn at night instead of harassment groups (×1.6 score)
- **Lone Ballista** — cannot hire defenders, ballista does double damage at 1.5× fire rate (×1.8 score)
- **Golden Horde** — enemies drop 3× loot, all costs doubled (×1.2 score)
- **Skeleton Crew** — start with 1 menial, refugees arrive 50% faster (×1.3 score)
- **Chaos Modifiers** — daily event multipliers are twice as extreme (×1.4 score)
- **Pacifist Run** — defenders cannot attack, score ×2.0
- **Bounty Hunter** — a boss enemy spawns every 5 days starting day 5 (×1.5 score)
- Mutators unlock based on gameplay achievements (e.g. survive 10 days, kill 100 enemies)
- Score multipliers stack multiplicatively, capped at 5.0×
- Incompatible mutators auto-disable when toggling (e.g. Lone Ballista ↔ Pacifist Run)
- Active mutators and score multiplier shown on the Game Over screen
- Mutator unlock state persists across sessions via PlayerPrefs
- Added MutatorUI panel with scrollable toggle list, lock/unlock states, and multiplier badges

### Lifetime Stats & Stats Dashboard
- Added LifetimeStatsManager tracking cumulative stats across all runs (enemies killed, gold earned, walls built, days survived, etc.)
- Added StatsDashboardPanel accessible from main menu with three tabs: Lifetime, Records, Computed
- Stats persist across sessions via PlayerPrefs JSON
- RunStatsTracker extended with 15+ new tracking properties feeding into lifetime stats

## 0.12.4

### Bug Fixes
- Fixed runaway defender spawn loop when hiring — an exception in FriendlyIndicator prevented menial cleanup code from running, causing the tower-entry callback to fire every frame and spawn hundreds of defenders; menial now marks itself dead before invoking the callback
- Fixed FriendlyIndicator shader not found in player builds — Shader.Find("Universal Render Pipeline/Unlit") returns null in builds when the shader isn't directly referenced; added Sprites/Default as a runtime fallback
- Fixed refugees arriving instantly upon spawning — arrival check used NavMeshAgent.remainingDistance (which is 0 before path computation), causing false arrival at the spawn point; now checks actual distance to fortress center
- Added guard in ConsumeMenials so the hire callback can only fire exactly once

## 0.12.3

### Visual
- Scaled character models (enemies, defenders, menials, refugees) to 2.5x for better visibility
- Increased wall and tower height by 1.5x (Y-axis only)
- Removed mouse-over hover magnification from TooltipSystem
- Idle characters now display frame 6 of the Walk animation as a static pose instead of looping

### Bug Fixes
- Fixed Bow Orc projectiles missing moving targets — ranged attacks now lead the shot using quadratic intercept prediction based on target velocity
- Fixed Bow Orc becoming static after target leaves attack range — enemies now immediately retarget when current target is destroyed or deactivated
- Fixed tower defenders not relocating when no enemies are in range — defenders on towers now reassess tower position even with no current target, moving closer to approaching enemies

## 0.12.2

### Manual Build Mode
- Build mode no longer auto-starts at nightfall — press **B** during night to enter build mode manually
- Shows "Press B to build walls" hint banner when night falls and enemies are cleared
- Pressing B when conditions aren't met shows the specific reason (no engineer, not enough gold, not night)
- Build mode still auto-exits at dawn

### Hover Magnification
- Hovering the cursor over enemies, defenders, menials, or refugees scales them 4x for easy identification
- Scale restores immediately when cursor moves away or the unit is destroyed

### Wall Torches
- Dynamically placed walls now receive torches at both tower endpoints when construction completes
- Torches match existing tower torch settings (range 6, intensity 1.5, warm color)
- Torches respect day/night cycle and are destroyed automatically when their wall is removed

### Bug Reports
- Large bug report zip attachments are now split into 24MB chunks to stay under Gmail's 25MB attachment limit
- Each chunk is sent as a separate email with "Part X/Y" in the subject line
- Status text shows per-part sending progress during multi-part submissions

## 0.12.1

### Bug Fixes — Issue 6: Enemies Walk Through Extension Walls
- Fixed Enemy NavMesh agent radius stuck at 0.5 instead of intended 0.7 — `NavMesh.CreateSettings()` returns a struct copy so property assignments never persisted; `NavMeshSetup.cs` now uses `SerializedObject` to write directly to `NavMeshAreas.asset`
- Renamed Enemy agent type from "New Agent 1" to "Enemy" in NavMesh project settings
- Widened wall NavMeshObstacle to cover full tower-to-tower span (`2×TOWER_OFFSET` width, `2×OCT_APOTHEM` depth) so enemies can't path through exposed tower geometry at unattached wall endpoints
- Fixed melee enemies detouring sideways after rounding extension walls — `PathingRayManager.GetBestRay()` now filters wall crossings by enemy distance from fortress center, ignoring walls the enemy has already passed; prevents inflated ray costs from outer wall layers causing unnecessary detours

### Diagnostics
- Added comprehensive game state snapshot system (`GameManager.LogGameSnapshot()`) that dumps all manager states for bug reproduction
- `EnemySpawnManager.LogSpawnState()` — active enemies, remnants, spawn counts
- `MenialManager.LogMenialState()` — per-menial state, position, task summary
- `UpgradeManager.LogUpgradeState()` — all purchase counts
- Snapshots fire automatically at day start, bug report submit, and scene init
- Added debug toggle `debugIssue6WestExtensions` on WallManager to reproduce the issue 6 wall layout (8 extension walls on west side with NW/SW corner angles)

## 0.12.0

### Recall System
- Press **R** or **Middle Mouse** to recall all defenders and menials to the courtyard
- Defenders dismount towers, cancel guard duty, and walk to the courtyard; resume normal behavior after arriving
- Menials drop their current task (collecting, clearing vegetation) and return home with any carried treasure
- Plays a recall horn sound effect on activation
- Recall is disabled during build mode

### Balance
- Hireling costs now scale on **living defender count** instead of cumulative purchases — replacing a fallen engineer costs the same as when you first had that many, not the total you ever bought

### Dual NavMesh — Enemies Can't Path Through Wall Gaps
- Added separate "Enemy" NavMesh agent type with radius 0.7 (vs 0.5 for friendlies), sealing gaps between wall segments at the pathfinding level
- Created `NavMeshSetup.cs` editor script (`Tools/Setup Dual NavMesh`) that creates the Enemy agent type, adds two NavMeshSurface components, bakes both, and assigns the Enemy agent type to the Enemy prefab
- Added "Walls" layer — wall meshes are excluded from the NavMesh bake so only NavMeshObstacle carving controls wall blocking; breaches now properly open when walls are destroyed
- Removed gap-blocking hack from `EnemyMovement.cs` (~35 lines): `gapBlocked` field, `ENEMY_NAV_RADIUS` override, gap-detection logic in `Update()`, and `IsNearDestroyedWall()` method
- Updated `NavMeshRebaker.cs` to mark all NavMeshSurface GameObjects dirty (was only marking the first)
- Friendlies (menials, defenders, refugees) use the default Humanoid NavMesh and can still pass through wall gaps

## 0.11.2

### Enemy Targeting — Ray-Based Pathing
- Added PathingRayManager: casts 360 rays (one per degree) from the central tower outward, counting wall crossings per direction
- Melee enemies pick the ray with fewest wall crossings (closest angle as tiebreaker) and attack the outermost wall on that ray
- Clear rays (0 crossings) route enemies directly to the tower — breaches are exploited automatically
- Wall destruction and repair trigger full ray recalculation + enemy retarget
- New wall construction completion triggers ray recalculation so freshly built walls are accounted for
- Added gizmo visualization: 360 pathing rays colored by cost (green=clear, yellow=1 wall, red=2+ walls)
- Removed per-wall cost evaluation, NavMesh.CalculatePath-based approach scoring, and detour ratio heuristic
- Removed obsolete methods: CountBlockingWalls, CountPathWallCrossings, PathLength, FindNearestIntactWall

### Enemy Targeting — Unchanged
- Enemies retarget every 1 unit of movement instead of on a fixed timer
- Goblins target the globally most-damaged wall (lowest HP, closest as tiebreaker)
- Ranged enemies (bow orcs) fall back to the closest wall when no units are found

## 0.11.1

### Audio
- Music tracks now loaded dynamically from Resources/Music/ at launch — no manual inspector assignment needed
- Moved music files from Audio/Music to Resources/Music for runtime loading
- Added diagnostic logging to StartMusic early-return paths (null tracks, already playing)

## 0.11.0

### Defender AI
- Added static enemy registry (Enemy.ActiveEnemies) replacing per-frame FindObjectsByType calls across all defender scripts
- Ground-level defenders now check line-of-sight before targeting — no more shooting through walls
- Tower defenders remain exempt from LOS checks (they shoot over walls)
- Guard system: guards now interpose between nearest enemy and the guarded engineer
- Guard positioning validates NavMesh, wall geometry clearance, and LOS to engineer
- Guards detect stuck state (distance-based, 5s timeout) and release guard duty if unreachable
- Engineers retreat to courtyard when idle and unconditionally release their guard
- Guard re-evaluation only runs while engineer is actively repairing (prevents assign/release cycle)
- ReleaseFromGuardDuty resets tower seek timer for immediate tower remounting
- Guard death notifies engineer (OnGuardDied) so a replacement can be requested
- Pikeman walks to inner wall face nearest enemy instead of staying clamped at courtyard center

### Tower System
- Added TowerPositionManager for centralized tower position tracking and assignment
- Towers deduplicated by XZ proximity from wall endpoint positions
- Best tower selection considers enemy proximity (when enemies present) or spread coverage (when idle)
- Tower clipping prevention: adjacent occupied towers within 1.0 unit are skipped during assignment
- MountTower safety check prevents mounting a tower already occupied by another defender
- Domain reload re-links defenders to rebuilt tower positions
- Extension walls built by engineers now register tower positions via TowerPositionManager

### Performance
- Replaced FindObjectsByType<Enemy> with static HashSet registry in Defender, Pikeman, Wizard, TowerPositionManager
- Replaced Physics.OverlapSphere with OverlapSphereNonAlloc using shared buffer for wall geometry checks

### Bug Fixes
- Fixed defenders standing in narrow NavMesh strip between tower colliders (added manual wall bounds check)
- Fixed guard positioned behind wall with no LOS to engineer
- Fixed mount/dismount cycle caused by guard evaluation running during idle state
- Fixed guard not returning to tower after release (towerSeekTimer not reset)
- Fixed two defenders occupying same tower after domain reload
- Fixed Pikeman serialization warning (renamed shadowed lastLoggedTarget field)

### Diagnostics
- Added comprehensive logging to FindTarget (target acquisition/loss, LOS skip counts)
- Added logging to guard lifecycle (start, release, stuck, position computation)
- Added logging to tower mount/dismount, tower switching, wall geometry escape
- Added track advancement logging to SoundManager

## 0.10.4

### Bug Fixes
- Fixed enemies squeezing through gaps between intact wall segments by adding runtime gap detection in EnemyMovement that redirects enemies to attack the nearest wall instead
- Fixed Build Phase banner appearing when no living engineer is available or player can't afford walls

## 0.10.3

### Bug Reporting
- Added in-game bug report system accessible from the main menu ("REPORT BUG" button)
- Bug reports are emailed via SMTP with user comments and attached game logs
- BugReportPanel builds its own UI overlay with comments input, submit/cancel buttons, and sending status
- BugReportConfig ScriptableObject stores SMTP credentials and recipient settings
- Sending runs on a background thread to avoid freezing the UI

### Log Capture
- Added LogCapture singleton that persists across scenes (DontDestroyOnLoad)
- Captures all Debug.Log/Warning/Error messages to a persistent `game_log.txt` file
- Logs session header with version, platform, OS, GPU, RAM, and difficulty
- Auto-flushes every 5 seconds; truncates at 5MB to prevent unbounded growth
- Log file is zipped and attached to bug reports

### Diagnostics
- Added detailed combat logging to Defender, Crossbowman, and Wizard (damage values, distances)
- Added damage-taken logging to Enemy, Wall, and Menial (HP before/after, position)
- Added initialization and movement logging to EnemyAttack and EnemyMovement (targets, breach pathing, distances)
- Added logging to all EnemyAttack paths: melee, ranged, suicide (per-target-type breakdown)
- Added logging to Ballista (init stats, firing events, upgrades applied)
- Added logging to WallManager (wall registration, breach detection with wall names/positions)
- Added logging to RefugeeSpawner (init params, spawn positions, power-up assignments)
- Added logging to TreasurePickup (collection value and position)
- Added logging to Menial (spawn stats, return-home events, damage taken)
- Added null-guard error logs for missing prefabs in Crossbowman, Ballista, and RefugeeSpawner

### Build System
- Added link.xml to preserve assemblies needed for SMTP/email in IL2CPP builds

## 0.10.2

### Tutorial
- Added Nightmare Difficulty page explaining FPS camera, scorpio aiming, and gravity arc projectiles
- Updated Build Mode page to mention time slows to 10%

## 0.10.1

### Nightmare Mode
- Scorpio now visually tilts up/down with mouse pitch
- Starting menials increased from 1 to 3

### Audio
- Fixed music shuffle always playing the same first track on each restart (switched from System.Random to UnityEngine.Random)

### Visuals
- Removed central flagpole and banner from tower model
- Removed corner flagpole GameObjects from scene
- Added wooden platform beneath ballista on central tower
- Re-exported Tower.fbx with updated geometry

## 0.10.0

### Nightmare Mode (New)
- Added first-person camera (NightmareCamera) for Nightmare difficulty
- FPS camera attaches to active ballista with mouse-look aiming (yaw + pitch)
- Cursor locking/unlocking for FPS gameplay, pause, and game over
- Switches to overhead camera during build mode, returns to FPS on exit
- Ballista fires in 3D direction based on camera forward in Nightmare mode
- BallistaManager spawns NightmareCamera at runtime when in Nightmare difficulty
- BallistaManager.OnActiveBallistaChanged event now properly fires on Tab switch

### Ballista & Projectiles
- Ballista supports FPS yaw rotation via mouse delta in Nightmare mode
- Projectiles use gravity arc trajectory in Nightmare mode
- Projectiles deal AoE damage on impact in Nightmare mode

### Build Phase
- Units now move at 10% speed during build phase (Time.timeScale = 0.1)
- DayNightCycle and GameManager.GameTime use unscaledDeltaTime to prevent double time scaling
- Unpausing correctly restores timeScale (0.1 in build mode, 1.0 otherwise)
- Dawn auto-exit from build mode restores timeScale to 1.0

## 0.9.2

### Bug Fixes
- Fixed menial fleeing state: CheckForDanger no longer blocks UpdateFleeing dispatch
- Fixed refugee arrival not logging or handling null SpawnMenial result
- Fixed BurstDamageVFX particle systems not playing (playOnAwake disabled, explicit Play() call added)

### Diagnostics
- Added periodic menial count audit in MenialManager (logs mismatches between tracked and scene counts)
