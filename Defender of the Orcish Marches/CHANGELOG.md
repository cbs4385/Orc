# Changelog

##0.10.2

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
