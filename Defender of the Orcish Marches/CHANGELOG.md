# Changelog

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
