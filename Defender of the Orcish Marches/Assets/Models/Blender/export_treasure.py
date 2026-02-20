"""
Blender 4.x script — Export Treasure Chest as FBX.
Run this AFTER generate_treasure.py has been run.
Static mesh only (no armature, no animations).
"""

import bpy

OUTPUT_PATH = r"C:\Users\chris\source\repos\Orc\Defender of the Orcish Marches\Assets\Models\TreasureChest.fbx"

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

    # Select all meshes for export
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            obj.select_set(True)

    # Export FBX — static mesh, no animation
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_PATH,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_anim=False,
        object_types={'MESH'},
        mesh_smooth_type='OFF',
    )

    print("=" * 50)
    print(f"  Exported to: {OUTPUT_PATH}")
    print("  Static mesh (no animations).")
    print("=" * 50)


if __name__ == "__main__":
    clean_and_export()
