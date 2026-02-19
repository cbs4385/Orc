"""
Blender 4.x script — Render key frames of the currently soloed NLA track
(or active action) and save images to the Model_Preview folder.

Usage:
  1. In the NLA Editor, solo (star icon) the track you want to preview
     — OR assign an action in the Action Editor
  2. Run this script from the Scripting workspace
  3. Images are saved to the Model_Preview folder

Output files are named:  orc_{action_name}_frame_{NN}.png
Any existing files with the same names are overwritten.
"""

import bpy
import os

# ──────────────────────────────────────────────
#  Config
# ──────────────────────────────────────────────

OUTPUT_DIR = r"C:\Users\chris\source\repos\Orc\Model_Preview"
FRAME_STEP = 5          # render every N frames
RESOLUTION_X = 960
RESOLUTION_Y = 540

# ──────────────────────────────────────────────
#  Detect which animation is active
# ──────────────────────────────────────────────

def get_active_animation_name():
    """Return the name of the current action or soloed NLA track."""
    arm = None
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE' and obj.animation_data:
            arm = obj
            break

    if arm is None:
        print("ERROR: No armature with animation data found.")
        return None, None, None

    anim_data = arm.animation_data

    # Check for soloed NLA track first (takes priority)
    for track in anim_data.nla_tracks:
        if track.is_solo:
            for strip in track.strips:
                start = int(strip.frame_start)
                end = int(strip.frame_end)
                return strip.action.name, start, end

    # Fall back to active action
    if anim_data.action:
        action = anim_data.action
        start = int(action.frame_range[0])
        end = int(action.frame_range[1])
        return action.name, start, end

    # Fallback: find first unmuted track
    for track in anim_data.nla_tracks:
        if not track.mute:
            for strip in track.strips:
                start = int(strip.frame_start)
                end = int(strip.frame_end)
                return strip.action.name, start, end

    print("ERROR: No active action or soloed NLA track found.")
    print("  → Solo a track (star icon) or assign an action first.")
    return None, None, None


# ──────────────────────────────────────────────
#  Render
# ──────────────────────────────────────────────

def render_frames():
    anim_name, frame_start, frame_end = get_active_animation_name()
    if anim_name is None:
        return

    # Sanitize name for filename
    safe_name = anim_name.lower().replace(" ", "_").replace(".", "_")

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # Configure render settings
    scene = bpy.context.scene
    scene.render.resolution_x = RESOLUTION_X
    scene.render.resolution_y = RESOLUTION_Y
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'

    # Use viewport shading for speed (solid mode render)
    # If you want full rendered images, comment out the next 2 lines
    scene.render.engine = 'BLENDER_EEVEE_NEXT'

    # Build list of frames to render
    frames = list(range(frame_start, frame_end + 1, FRAME_STEP))
    # Always include the last frame
    if frames[-1] != frame_end:
        frames.append(frame_end)

    print(f"Rendering '{anim_name}' — frames {frame_start}–{frame_end}, "
          f"step {FRAME_STEP} ({len(frames)} images)")
    print(f"Output: {OUTPUT_DIR}")
    print("-" * 40)

    for frame in frames:
        scene.frame_set(frame)
        filename = f"orc_{safe_name}_frame_{frame:02d}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        scene.render.filepath = filepath
        bpy.ops.render.render(write_still=True)
        print(f"  ✓ Frame {frame:02d} → {filename}")

    print("-" * 40)
    print(f"Done! {len(frames)} frames rendered to {OUTPUT_DIR}")


if __name__ == "__main__":
    render_frames()
