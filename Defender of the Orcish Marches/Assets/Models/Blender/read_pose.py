"""
Blender 4.x script — Print all pose bone transforms for the active armature.
Run this at the frame you want to capture (e.g. frame 30 of Die animation).
Copy the output and paste it back so the values can be used in generate_troll.py.
"""

import bpy
import math

arm_obj = bpy.context.active_object
if arm_obj is None or arm_obj.type != 'ARMATURE':
    # Try to find the armature
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            arm_obj = obj
            break

if arm_obj is None or arm_obj.type != 'ARMATURE':
    print("ERROR: No armature found!")
else:
    frame = bpy.context.scene.frame_current
    print(f"\n{'='*60}")
    print(f"  Pose bone transforms at frame {frame}")
    print(f"  Armature: {arm_obj.name}")
    print(f"{'='*60}")

    for pb in arm_obj.pose.bones:
        rx = math.degrees(pb.rotation_euler.x)
        ry = math.degrees(pb.rotation_euler.y)
        rz = math.degrees(pb.rotation_euler.z)
        lx, ly, lz = pb.location.x, pb.location.y, pb.location.z

        has_rot = abs(rx) > 0.01 or abs(ry) > 0.01 or abs(rz) > 0.01
        has_loc = abs(lx) > 0.001 or abs(ly) > 0.001 or abs(lz) > 0.001

        if has_rot or has_loc:
            print(f"  {pb.name}:")
            if has_rot:
                print(f"    rot=({rx:.1f}, {ry:.1f}, {rz:.1f})")
            if has_loc:
                print(f"    loc=({lx:.4f}, {ly:.4f}, {lz:.4f})")

    print(f"{'='*60}")
