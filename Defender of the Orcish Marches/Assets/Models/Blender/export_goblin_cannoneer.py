"""
Blender 4.x script — Remove preview objects and export Goblin Cannoneer as FBX.
Run this AFTER generate_goblin_cannoneer.py has been run.
"""

import bpy
import os

OUTPUT_PATH = r"C:\Users\chris\source\repos\Orc\Defender of the Orcish Marches\Assets\Models\GoblinCannoneer.fbx"

def clean_and_export():
    # Remove cameras and lights
    to_remove = []
    for obj in bpy.data.objects:
        if obj.type in ('CAMERA', 'LIGHT'):
            to_remove.append(obj)

    bpy.ops.object.select_all(action='DESELECT')
    for obj in to_remove:
        print(f"  Removing: {obj.name} ({obj.type})")
        obj.select_set(True)
    if to_remove:
        bpy.ops.object.delete()

    # Select armature and all meshes for export
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type in ('ARMATURE', 'MESH'):
            obj.select_set(True)

    # Remove ALL NLA tracks from armatures before export
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE' and obj.animation_data:
            obj.animation_data.action = None
            tracks = obj.animation_data.nla_tracks
            for i in range(len(tracks) - 1, -1, -1):
                print(f"  Removing NLA track: {tracks[i].name}")
                tracks.remove(tracks[i])

    # List actions that will be exported
    for action in bpy.data.actions:
        print(f"  Action: {action.name} (frames {action.frame_range[0]:.0f}-{action.frame_range[1]:.0f})")

    # Export FBX
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_PATH,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        use_armature_deform_only=False,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='OFF',
    )

    print("=" * 50)
    print(f"  Exported to: {OUTPUT_PATH}")
    print("  NLA tracks removed; actions exported individually.")
    print("=" * 50)


if __name__ == "__main__":
    clean_and_export()
