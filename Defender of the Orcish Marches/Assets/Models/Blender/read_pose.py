"""
Blender 4.x script — Read current pose bone transforms.
Prints all non-default bone rotations and locations, plus
copy-paste code for generate_pikeman.py animation keyframes.

Usage:
  1. Pose the armature in Pose Mode
  2. Run this script from Blender's Text Editor
  3. Check output in System Console (Window > Toggle System Console)
  4. Paste the set_bone_rot / set_bone_loc lines into generate_pikeman.py
"""

import bpy
import math

ROT_THRESHOLD = 0.5   # degrees
LOC_THRESHOLD = 0.001  # units


def read_current_pose():
    arm_obj = bpy.context.active_object
    if arm_obj is None or arm_obj.type != 'ARMATURE':
        for obj in bpy.data.objects:
            if obj.type == 'ARMATURE':
                arm_obj = obj
                break

    if arm_obj is None or arm_obj.type != 'ARMATURE':
        print("ERROR: No armature found!")
        return

    frame = bpy.context.scene.frame_current

    print("=" * 60)
    print(f"  Pose bone transforms at frame {frame}")
    print(f"  Armature: {arm_obj.name}")
    print("=" * 60)

    code_lines = []
    code_lines.append(f"    # Frame {frame}:")

    for pb in arm_obj.pose.bones:
        rx = math.degrees(pb.rotation_euler.x)
        ry = math.degrees(pb.rotation_euler.y)
        rz = math.degrees(pb.rotation_euler.z)
        lx, ly, lz = pb.location.x, pb.location.y, pb.location.z

        has_rot = (abs(rx) > ROT_THRESHOLD or
                   abs(ry) > ROT_THRESHOLD or
                   abs(rz) > ROT_THRESHOLD)
        has_loc = (abs(lx) > LOC_THRESHOLD or
                   abs(ly) > LOC_THRESHOLD or
                   abs(lz) > LOC_THRESHOLD)

        if has_rot or has_loc:
            print(f"  {pb.name}:")
        if has_rot:
            print(f"    rot=({rx:.1f}, {ry:.1f}, {rz:.1f})")
            code_lines.append(
                f'    set_bone_rot(pb["{pb.name}"], {rx:.1f}, {ry:.1f}, {rz:.1f})'
            )
        if has_loc:
            print(f"    loc=({lx:.4f}, {ly:.4f}, {lz:.4f})")
            code_lines.append(
                f'    set_bone_loc(pb["{pb.name}"], {lx:.4f}, {ly:.4f}, {lz:.4f})'
            )

    print("=" * 60)
    print("")
    print("  Copy-paste code for generate_pikeman.py:")
    print("  " + "-" * 40)
    for line in code_lines:
        print(line)
    print("  " + "-" * 40)
    print("")


if __name__ == "__main__":
    read_current_pose()
