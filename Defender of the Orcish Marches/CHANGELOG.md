# Changelog

## 0.11.2

### Enemy Targeting
- Replaced fan-raycast wall detection with approach-direction scanning — melee enemies now evaluate 24 directions from the tower to find the path crossing the fewest wall "rings" (depth layers)
- Open approach directions (no walls) are detected automatically, letting melee enemies path straight to the tower via NavMesh
- Switched FindWallBetween from thin raycasts to SphereCast (radius 0.75) so rays can't slip through gaps between wall segments
- Goblins now target the globally most-damaged wall (lowest HP, closest as tiebreaker) instead of raycasting for the blocking wall
- Ranged enemies (bow orcs) fall back to the closest wall when no units are found, instead of delegating to goblin targeting logic
- Removed obsolete methods: CheckWallOnDirectLine, FindMinimalCrossingWall, FindBlockingWall, IsMeleeType

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
