"""
Blender 4.x script -- Export Gate as FBX.
Run this AFTER generate_gate.py has been run.
Exports armature + meshes, no animations (Gate.cs handles door rotation).
"""

import bpy

OUTPUT_PATH = r"C:\Users\chris\source\repos\Orc\Defender of the Orcish Marches\Assets\Models\Gate.fbx"


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

    # Select armature and meshes
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type in ('ARMATURE', 'MESH'):
            obj.select_set(True)
            print(f"  Exporting: {obj.name} ({obj.type})")

    # Remove any NLA tracks (shouldn't exist but be safe)
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE' and obj.animation_data:
            for track in list(obj.animation_data.nla_tracks):
                obj.animation_data.nla_tracks.remove(track)

    # Export FBX -- armature with bones, no animation baking
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_PATH,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        use_armature_deform_only=False,
        add_leaf_bones=False,
        bake_anim=False,
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='OFF',
    )

    print("=" * 50)
    print(f"  Exported to: {OUTPUT_PATH}")
    print("  Armature + meshes, no animations.")
    print("  Unity hierarchy: GateArmature > Root, LeftDoorPivot, RightDoorPivot")
    print("  Wire LeftDoorPivot and RightDoorPivot to Gate.cs fields.")
    print("=" * 50)


if __name__ == "__main__":
    clean_and_export()
