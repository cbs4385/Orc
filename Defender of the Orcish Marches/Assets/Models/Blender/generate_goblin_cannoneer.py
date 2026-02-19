"""
Blender 4.x Python script — Generate a rigged & animated "Goblin Cannoneer"
Two small goblins pushing a field cannon on wheels.

Layout (top view, -Y is forward toward fortress):
         -Y (forward)
          |
     ┌────┼────┐
     │ Cannon  │
     │ Barrel  │
     └────┼────┘
     O    |    O  (wheels)
          |
    GobA  |  GobB  (pushing from behind at +Y)
          |
         +Y

Animations:
  Walk  (24 fr loop) — goblins push cannon, wheels spin, legs stride
  Attack (24 fr)     — GobA lights fuse, GobB covers ears, cannon recoils
  Die   (30 fr)      — cannon tips over, goblins fall backward face-up, limbs spread

Single armature with prefixed bones: Cannon, Wheel_L/R, A_*/B_* for each goblin.
Rigid-body bone parenting (no mesh deformation).
"""

import bpy
import math
from mathutils import Vector, Euler

# ──────────────────────────────────────────────
#  Utility helpers
# ──────────────────────────────────────────────

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.armatures:
        if block.users == 0:
            bpy.data.armatures.remove(block)
    for block in list(bpy.data.actions):
        block.use_fake_user = False
        bpy.data.actions.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)


def make_material(name, color, emission=0.0, roughness=0.9):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    if emission > 0:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = emission
    return mat


def add_cube(name, location, scale, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj


def add_wedge(name, location, scale, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=4, radius1=0.5, radius2=0.0, depth=1.0,
        location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj


def add_cylinder(name, location, scale, material, rotation=(0, 0, 0), vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=0.5, depth=1.0,
        location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj


def add_sphere(name, location, scale, material, segments=8, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings,
        radius=0.5, location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj


def bevel_object(obj, width=0.02, segments=1):
    mod = obj.modifiers.new("Bevel", 'BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(60)


def apply_modifiers(obj):
    bpy.context.view_layer.objects.active = obj
    for mod in obj.modifiers[:]:
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except Exception:
            pass


def join_objects(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.active_object
    result.name = name
    return result


def parent_to_bone(obj, armature, bone_name):
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    armature.data.bones.active = armature.data.bones[bone_name]
    bpy.ops.object.parent_set(type='BONE')


# ──────────────────────────────────────────────
#  Materials
# ──────────────────────────────────────────────

MAT_SKIN = MAT_SKIN_DK = MAT_MOUTH = MAT_EYES = None
MAT_CLOTH = MAT_WOOD = MAT_TEETH = None
MAT_IRON = MAT_IRON_DK = MAT_FUSE = MAT_BALL = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_MOUTH, MAT_EYES
    global MAT_CLOTH, MAT_WOOD, MAT_TEETH
    global MAT_IRON, MAT_IRON_DK, MAT_FUSE, MAT_BALL
    # Dark green goblin skin
    MAT_SKIN    = make_material("CannonGobSkin",     (0.20, 0.40, 0.20, 1.0))
    MAT_SKIN_DK = make_material("CannonGobSkinDark", (0.14, 0.28, 0.12, 1.0))
    MAT_MOUTH   = make_material("CannonGobMouth",    (0.25, 0.08, 0.05, 1.0))
    MAT_EYES    = make_material("CannonGobEyes",     (1.0,  0.8,  0.1,  1.0), emission=4.0)
    MAT_CLOTH   = make_material("CannonGobCloth",    (0.25, 0.15, 0.08, 1.0))
    MAT_WOOD    = make_material("CannonWood",        (0.35, 0.20, 0.08, 1.0))
    MAT_TEETH   = make_material("CannonGobTeeth",    (0.90, 0.85, 0.60, 1.0))
    MAT_IRON    = make_material("CannonIron",        (0.25, 0.25, 0.28, 1.0), roughness=0.5)
    MAT_IRON_DK = make_material("CannonIronDark",    (0.15, 0.15, 0.18, 1.0), roughness=0.6)
    MAT_FUSE    = make_material("CannonFuse",        (1.0,  0.5,  0.0,  1.0), emission=3.0)
    MAT_BALL    = make_material("CannonBall",        (0.10, 0.10, 0.12, 1.0), roughness=0.3)


# ──────────────────────────────────────────────
#  Cannon parts
# ──────────────────────────────────────────────

def build_cannon_parts():
    """Build cannon body (barrel, carriage, fuse, ammo) and wheels.
    Returns dict: bone_name -> mesh."""
    groups = {}

    # ── CANNON BODY — parented to "Cannon" bone ──
    parts = []

    # Barrel — iron cylinder, long axis along Y (forward), rotated 90° on X
    # center slightly above carriage, tilted slightly up
    parts.append(add_cylinder("Barrel", (0, -0.02, 0.20),
                              (0.14, 0.14, 0.40), MAT_IRON,
                              rotation=(math.radians(85), 0, 0), vertices=10))
    bevel_object(parts[-1], 0.005)

    # Barrel muzzle ring at front
    parts.append(add_cylinder("MuzzleRing", (0, -0.22, 0.22),
                              (0.16, 0.16, 0.03), MAT_IRON_DK,
                              rotation=(math.radians(85), 0, 0), vertices=10))

    # Barrel rear ring
    parts.append(add_cylinder("RearRing", (0, 0.16, 0.18),
                              (0.16, 0.16, 0.03), MAT_IRON_DK,
                              rotation=(math.radians(85), 0, 0), vertices=10))

    # Wooden carriage / gun frame
    parts.append(add_cube("Carriage", (0, 0.04, 0.10),
                          (0.28, 0.42, 0.06), MAT_WOOD))
    bevel_object(parts[-1], 0.01)

    # Carriage side rails (left and right)
    parts.append(add_cube("RailL", (-0.12, 0.12, 0.12),
                          (0.04, 0.34, 0.08), MAT_WOOD))
    bevel_object(parts[-1], 0.01)
    parts.append(add_cube("RailR", (0.12, 0.12, 0.12),
                          (0.04, 0.34, 0.08), MAT_WOOD))
    bevel_object(parts[-1], 0.01)

    # Carriage handle bars (for goblins to push) extending backward
    parts.append(add_cube("HandleL", (-0.10, 0.34, 0.10),
                          (0.04, 0.16, 0.04), MAT_WOOD))
    bevel_object(parts[-1], 0.005)
    parts.append(add_cube("HandleR", (0.10, 0.34, 0.10),
                          (0.04, 0.16, 0.04), MAT_WOOD))
    bevel_object(parts[-1], 0.005)

    # Axle — horizontal bar through the wheels
    parts.append(add_cube("Axle", (0, 0, 0.08),
                          (0.40, 0.04, 0.04), MAT_IRON_DK))

    # Fuse at rear top of barrel
    parts.append(add_cylinder("Fuse", (0, 0.20, 0.22),
                              (0.015, 0.015, 0.08), MAT_FUSE,
                              rotation=(math.radians(30), 0, 0), vertices=6))
    parts.append(add_sphere("FuseSpark", (0, 0.24, 0.26),
                            (0.025, 0.025, 0.025), MAT_FUSE, segments=6, rings=4))

    # Cannonball stack (3 balls near the carriage)
    parts.append(add_sphere("Ball1", (-0.06, 0.08, 0.16),
                            (0.06, 0.06, 0.06), MAT_BALL, segments=8, rings=6))
    parts.append(add_sphere("Ball2", (0.06, 0.08, 0.16),
                            (0.06, 0.06, 0.06), MAT_BALL, segments=8, rings=6))
    parts.append(add_sphere("Ball3", (0, 0.08, 0.22),
                            (0.06, 0.06, 0.06), MAT_BALL, segments=8, rings=6))

    for p in parts:
        apply_modifiers(p)
    groups["Cannon"] = join_objects(parts, "Grp_Cannon")

    # ── LEFT WHEEL ──
    p = add_cylinder("WheelL", (-0.18, 0, 0.08),
                     (0.16, 0.16, 0.04), MAT_WOOD,
                     rotation=(0, math.radians(90), 0), vertices=10)
    bevel_object(p, 0.005)
    # Hub
    hub = add_cylinder("HubL", (-0.18, 0, 0.08),
                       (0.06, 0.06, 0.05), MAT_IRON,
                       rotation=(0, math.radians(90), 0), vertices=8)
    for o in [p, hub]:
        apply_modifiers(o)
    groups["Wheel_L"] = join_objects([p, hub], "Grp_Wheel_L")

    # ── RIGHT WHEEL ──
    p = add_cylinder("WheelR", (0.18, 0, 0.08),
                     (0.16, 0.16, 0.04), MAT_WOOD,
                     rotation=(0, math.radians(90), 0), vertices=10)
    bevel_object(p, 0.005)
    hub = add_cylinder("HubR", (0.18, 0, 0.08),
                       (0.06, 0.06, 0.05), MAT_IRON,
                       rotation=(0, math.radians(90), 0), vertices=8)
    for o in [p, hub]:
        apply_modifiers(o)
    groups["Wheel_R"] = join_objects([p, hub], "Grp_Wheel_R")

    return groups


# ──────────────────────────────────────────────
#  Goblin body parts (reusable for both goblins)
# ──────────────────────────────────────────────

Z_OFF = 0.08  # raise goblins so feet touch ground

def build_goblin_parts(prefix, ox, oy):
    """Build one goblin's body parts at offset (ox, oy).
    prefix: "A_" or "B_" for bone name mapping.
    Returns dict: bone_name -> mesh."""
    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso + waist cloth + loincloth) ──
    parts = []
    parts.append(add_cube(f"{prefix}Torso", (ox, oy, z(0.32)),
                          (0.22, 0.16, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube(f"{prefix}WaistCloth", (ox, oy, z(0.21)),
                          (0.24, 0.18, 0.05), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)
    parts.append(add_cube(f"{prefix}Loincloth", (ox, oy-0.06, z(0.14)),
                          (0.12, 0.03, 0.10), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}Spine"] = join_objects(parts, f"Grp_{prefix}Spine")

    # ── HEAD (oversized goblin head) ──
    parts = []
    parts.append(add_cube(f"{prefix}Head", (ox, oy, z(0.50)),
                          (0.24, 0.20, 0.20), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    # Brow
    parts.append(add_cube(f"{prefix}Brow", (ox, oy-0.09, z(0.55)),
                          (0.22, 0.05, 0.04), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Eyes
    parts.append(add_cube(f"{prefix}EyeL", (ox-0.07, oy-0.10, z(0.52)),
                          (0.06, 0.04, 0.05), MAT_EYES))
    parts.append(add_cube(f"{prefix}EyeR", (ox+0.07, oy-0.10, z(0.52)),
                          (0.06, 0.04, 0.05), MAT_EYES))
    # Nose
    parts.append(add_wedge(f"{prefix}Nose", (ox, oy-0.14, z(0.49)),
                           (0.04, 0.06, 0.06), MAT_SKIN_DK,
                           rotation=(math.radians(-90), 0, 0)))
    # Mouth
    parts.append(add_cube(f"{prefix}Mouth", (ox, oy-0.10, z(0.43)),
                          (0.12, 0.03, 0.04), MAT_MOUTH))
    # Teeth
    parts.append(add_wedge(f"{prefix}ToothL", (ox-0.03, oy-0.11, z(0.45)),
                           (0.02, 0.02, 0.03), MAT_TEETH))
    parts.append(add_wedge(f"{prefix}ToothR", (ox+0.03, oy-0.11, z(0.45)),
                           (0.02, 0.02, 0.03), MAT_TEETH))
    # Ears
    parts.append(add_wedge(f"{prefix}EarL", (ox-0.16, oy, z(0.52)),
                           (0.04, 0.10, 0.12), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(-40))))
    parts.append(add_wedge(f"{prefix}EarR", (ox+0.16, oy, z(0.52)),
                           (0.04, 0.10, 0.12), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(40))))
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}Head"] = join_objects(parts, f"Grp_{prefix}Head")

    # ── LEFT UPPER ARM ──
    p = add_cube(f"{prefix}ArmLU", (ox-0.17, oy, z(0.36)),
                 (0.09, 0.09, 0.14), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups[f"{prefix}L_UpperArm"] = p

    # ── LEFT FOREARM + HAND ──
    parts = []
    parts.append(add_cube(f"{prefix}ArmLL", (ox-0.18, oy-0.02, z(0.24)),
                          (0.08, 0.08, 0.12), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube(f"{prefix}HandL", (ox-0.18, oy-0.03, z(0.17)),
                          (0.07, 0.07, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}L_ForeArm"] = join_objects(parts, f"Grp_{prefix}L_ForeArm")

    # ── RIGHT UPPER ARM ──
    p = add_cube(f"{prefix}ArmRU", (ox+0.17, oy, z(0.36)),
                 (0.09, 0.09, 0.14), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups[f"{prefix}R_UpperArm"] = p

    # ── RIGHT FOREARM + HAND ──
    parts = []
    parts.append(add_cube(f"{prefix}ArmRL", (ox+0.18, oy-0.02, z(0.24)),
                          (0.08, 0.08, 0.12), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube(f"{prefix}HandR", (ox+0.18, oy-0.03, z(0.17)),
                          (0.07, 0.07, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}R_ForeArm"] = join_objects(parts, f"Grp_{prefix}R_ForeArm")

    # ── LEFT UPPER LEG ──
    p = add_cube(f"{prefix}LegLU", (ox-0.07, oy, z(0.10)),
                 (0.09, 0.10, 0.14), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups[f"{prefix}L_UpperLeg"] = p

    # ── LEFT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube(f"{prefix}LegLL", (ox-0.07, oy, z(-0.01)),
                          (0.08, 0.09, 0.10), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube(f"{prefix}FootL", (ox-0.07, oy-0.03, z(-0.05)),
                          (0.09, 0.14, 0.05), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}L_LowerLeg"] = join_objects(parts, f"Grp_{prefix}L_LowerLeg")

    # ── RIGHT UPPER LEG ──
    p = add_cube(f"{prefix}LegRU", (ox+0.07, oy, z(0.10)),
                 (0.09, 0.10, 0.14), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups[f"{prefix}R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube(f"{prefix}LegRL", (ox+0.07, oy, z(-0.01)),
                          (0.08, 0.09, 0.10), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube(f"{prefix}FootR", (ox+0.07, oy-0.03, z(-0.05)),
                          (0.09, 0.14, 0.05), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups[f"{prefix}R_LowerLeg"] = join_objects(parts, f"Grp_{prefix}R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

# Goblin offsets from center
GA_X, GA_Y = -0.22, 0.28   # Goblin A: left, behind cannon
GB_X, GB_Y =  0.22, 0.28   # Goblin B: right, behind cannon

def create_armature():
    """Build skeleton for cannon + two goblins."""
    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "CannoneerArmature"
    arm = arm_obj.data
    arm.name = "CannoneerRig"

    for b in arm.edit_bones:
        arm.edit_bones.remove(b)

    def add_bone(name, head, tail, parent_name=None, connect=False):
        b = arm.edit_bones.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        if parent_name:
            b.parent = arm.edit_bones[parent_name]
            b.use_connect = connect
        return b

    # ── Root — master bone for whole unit ──
    add_bone("Root", (0, 0, 0.10), (0, 0, 0.14))

    # ── Cannon ──
    add_bone("Cannon", (0, 0, 0.14), (0, 0, 0.24), "Root")
    # Wheels (children of Cannon so they tip with it during die)
    add_bone("Wheel_L", (-0.18, 0, 0.08), (-0.20, 0, 0.08), "Cannon")
    add_bone("Wheel_R", ( 0.18, 0, 0.08), ( 0.20, 0, 0.08), "Cannon")

    # ── Goblin A (left) ──
    def add_goblin_bones(prefix, ox, oy):
        add_bone(f"{prefix}Root",  (ox, oy, z+0.16), (ox, oy, z+0.20), "Root")
        add_bone(f"{prefix}Spine", (ox, oy, z+0.16), (ox, oy, z+0.40), f"{prefix}Root", connect=True)
        add_bone(f"{prefix}Head",  (ox, oy, z+0.40), (ox, oy, z+0.60), f"{prefix}Spine", connect=True)

        add_bone(f"{prefix}L_UpperArm", (ox-0.11, oy, z+0.38), (ox-0.17, oy, z+0.36), f"{prefix}Spine")
        add_bone(f"{prefix}L_ForeArm",  (ox-0.17, oy, z+0.36), (ox-0.18, oy, z+0.18), f"{prefix}L_UpperArm", connect=True)

        add_bone(f"{prefix}R_UpperArm", (ox+0.11, oy, z+0.38), (ox+0.17, oy, z+0.36), f"{prefix}Spine")
        add_bone(f"{prefix}R_ForeArm",  (ox+0.17, oy, z+0.36), (ox+0.18, oy, z+0.18), f"{prefix}R_UpperArm", connect=True)

        add_bone(f"{prefix}L_UpperLeg", (ox-0.07, oy, z+0.16), (ox-0.07, oy, z+0.04), f"{prefix}Root")
        add_bone(f"{prefix}L_LowerLeg", (ox-0.07, oy, z+0.04), (ox-0.07, oy, z-0.05), f"{prefix}L_UpperLeg", connect=True)

        add_bone(f"{prefix}R_UpperLeg", (ox+0.07, oy, z+0.16), (ox+0.07, oy, z+0.04), f"{prefix}Root")
        add_bone(f"{prefix}R_LowerLeg", (ox+0.07, oy, z+0.04), (ox+0.07, oy, z-0.05), f"{prefix}R_UpperLeg", connect=True)

    add_goblin_bones("A_", GA_X, GA_Y)
    add_goblin_bones("B_", GB_X, GB_Y)

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj


# ──────────────────────────────────────────────
#  Rig
# ──────────────────────────────────────────────

def rig_model(arm_obj, all_groups):
    for bone_name, mesh_obj in all_groups.items():
        parent_to_bone(mesh_obj, arm_obj, bone_name)


# ──────────────────────────────────────────────
#  Animations
# ──────────────────────────────────────────────

def set_bone_rot(pose_bone, x_deg, y_deg, z_deg):
    pose_bone.rotation_mode = 'XYZ'
    pose_bone.rotation_euler = (
        math.radians(x_deg),
        math.radians(y_deg),
        math.radians(z_deg)
    )

def set_bone_loc(pose_bone, x, y, z_val):
    pose_bone.location = (x, y, z_val)

def key_all_bones(arm_obj, frame):
    for pb in arm_obj.pose.bones:
        pb.keyframe_insert(data_path="rotation_euler", frame=frame)
        pb.keyframe_insert(data_path="location", frame=frame)

def reset_pose(arm_obj):
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


def pose_push(pb, prefix, frame_frac, side):
    """Pose one goblin in pushing stance.
    side: -1 = left goblin (A), +1 = right goblin (B).
    Arms angle inward toward cannon handles."""
    inward = 10 * side  # Y rotation on spine to angle torso toward cannon

    if side == -1:  # Goblin A — left arm reaches across to cannon handle
        set_bone_rot(pb[f"{prefix}L_UpperArm"], -45.5, -19.3, 3.8)
        set_bone_rot(pb[f"{prefix}L_ForeArm"],  -15, 0, 0)
        set_bone_rot(pb[f"{prefix}R_UpperArm"], -30, 0, 0)
        set_bone_rot(pb[f"{prefix}R_ForeArm"],   2.8, -7.7, -50.1)
    else:           # Goblin B — symmetric push stance
        set_bone_rot(pb[f"{prefix}L_UpperArm"],  30, 0, 0)
        set_bone_rot(pb[f"{prefix}L_ForeArm"],  -15, 0, 0)
        set_bone_rot(pb[f"{prefix}R_UpperArm"], -30, 0, 0)
        set_bone_rot(pb[f"{prefix}R_ForeArm"],  -15, 0, 0)

    # Forward lean + turn inward toward cannon
    set_bone_rot(pb[f"{prefix}Spine"], 15, inward, 0)
    set_bone_rot(pb[f"{prefix}Head"], -10, 0, 0)


def create_walk_cycle(arm_obj):
    """Walk cycle — goblins push cannon, wheels spin, legs stride."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 25  # leg swing
    wheel_spin_per_frame = 15  # degrees per frame for wheel rotation

    # Frame 1: neutral push stance
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0, side)
    key_all_bones(arm_obj, 1)

    # Frame 7: left legs forward, right legs back
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0.25, side)
        set_bone_rot(pb[f"{prefix}L_UpperLeg"],  swing, 0, 0)
        set_bone_rot(pb[f"{prefix}L_LowerLeg"], -swing*0.3, 0, 0)
        set_bone_rot(pb[f"{prefix}R_UpperLeg"], -swing, 0, 0)
        set_bone_rot(pb[f"{prefix}R_LowerLeg"],  0, 0, 0)
    # Wheels spin forward — Y rotation (bone points along X, so local Y = axle roll)
    set_bone_rot(pb["Wheel_L"], 0, wheel_spin_per_frame * 7, 0)
    set_bone_rot(pb["Wheel_R"], 0, wheel_spin_per_frame * 7, 0)
    key_all_bones(arm_obj, 7)

    # Frame 13: neutral again
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0.5, side)
    set_bone_rot(pb["Wheel_L"], 0, wheel_spin_per_frame * 13, 0)
    set_bone_rot(pb["Wheel_R"], 0, wheel_spin_per_frame * 13, 0)
    key_all_bones(arm_obj, 13)

    # Frame 19: right legs forward, left legs back (mirror)
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0.75, side)
        set_bone_rot(pb[f"{prefix}R_UpperLeg"],  swing, 0, 0)
        set_bone_rot(pb[f"{prefix}R_LowerLeg"], -swing*0.3, 0, 0)
        set_bone_rot(pb[f"{prefix}L_UpperLeg"], -swing, 0, 0)
        set_bone_rot(pb[f"{prefix}L_LowerLeg"],  0, 0, 0)
    set_bone_rot(pb["Wheel_L"], 0, wheel_spin_per_frame * 19, 0)
    set_bone_rot(pb["Wheel_R"], 0, wheel_spin_per_frame * 19, 0)
    key_all_bones(arm_obj, 19)

    # Frame 25: loop back to frame 1
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 1.0, side)
    set_bone_rot(pb["Wheel_L"], 0, wheel_spin_per_frame * 25, 0)
    set_bone_rot(pb["Wheel_R"], 0, wheel_spin_per_frame * 25, 0)
    key_all_bones(arm_obj, 25)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'

    action.use_fake_user = True
    print("  Walk cycle created (frames 1-25, push loop)")
    return action


def create_attack_anim(arm_obj):
    """Attack — Goblin A lights the fuse, Goblin B covers ears, cannon recoils."""
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: push stance (at rest after stopping)
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0, side)
    key_all_bones(arm_obj, 1)

    # Frame 5: Gob A turns toward fuse, reaches with right arm
    #          Gob B steps back, starts raising hands
    reset_pose(arm_obj)
    # Goblin A — turn toward cannon fuse (fuse is at rear center)
    set_bone_rot(pb["A_Spine"],      10, 15, 0)     # lean forward, turn right toward fuse
    set_bone_rot(pb["A_Head"],       -5, 10, 0)     # look at fuse
    set_bone_rot(pb["A_R_UpperArm"], -40, 0, 0)     # right arm reaches forward
    set_bone_rot(pb["A_R_ForeArm"],  -20, 0, 0)     # forearm extended
    set_bone_rot(pb["A_L_UpperArm"],  10, 0, 0)     # left arm rests
    set_bone_rot(pb["A_L_ForeArm"],   0, 0, 0)
    # Goblin B — stepping back
    set_bone_rot(pb["B_Spine"],       5, 0, 0)
    set_bone_rot(pb["B_Head"],       -5, -10, 0)    # looking toward cannon
    set_bone_rot(pb["B_R_UpperArm"],  0, 0, -20)    # starting to raise arms
    set_bone_rot(pb["B_L_UpperArm"],  0, 0,  20)
    key_all_bones(arm_obj, 5)

    # Frame 9: Gob A touching fuse (lighting it), Gob B covering ears
    reset_pose(arm_obj)
    # Goblin A — lighting the fuse
    set_bone_rot(pb["A_Spine"],      15, 20, 0)
    set_bone_rot(pb["A_Head"],       -5, 15, 0)
    set_bone_rot(pb["A_R_UpperArm"], -50, 0, -10)   # reaching far forward to fuse
    set_bone_rot(pb["A_R_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["A_L_UpperArm"],  15, 0, 0)
    set_bone_rot(pb["A_L_ForeArm"],   0, 0, 0)
    # Goblin B — covering ears
    set_bone_rot(pb["B_Spine"],      -5, 0, 0)      # leaning back
    set_bone_rot(pb["B_Head"],        5, 0, 0)       # head up, bracing
    set_bone_rot(pb["B_R_UpperArm"],  0, 0, -70)    # arms up to cover ears
    set_bone_rot(pb["B_R_ForeArm"],  -40, 0, 0)
    set_bone_rot(pb["B_L_UpperArm"],  0, 0,  70)
    set_bone_rot(pb["B_L_ForeArm"],  -40, 0, 0)
    key_all_bones(arm_obj, 9)

    # Frame 12: FIRE! Cannon recoils backward
    reset_pose(arm_obj)
    # Cannon recoil
    set_bone_loc(pb["Root"], 0, 0.06, 0)             # whole unit jolts backward
    set_bone_rot(pb["Cannon"], -8, 0, 0)              # barrel kicks up
    # Goblin A — startled, thrown back slightly
    set_bone_rot(pb["A_Spine"],     -10, 10, 0)
    set_bone_rot(pb["A_Head"],       10, 0, 0)
    set_bone_rot(pb["A_R_UpperArm"], 15, 0, -30)
    set_bone_rot(pb["A_R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["A_L_UpperArm"], 15, 0, 30)
    set_bone_rot(pb["A_L_ForeArm"],  -20, 0, 0)
    set_bone_loc(pb["A_Root"], 0, 0.04, 0)
    # Goblin B — bracing from recoil, still covering ears
    set_bone_rot(pb["B_Spine"],      -8, 0, 0)
    set_bone_rot(pb["B_Head"],        8, 0, 0)
    set_bone_rot(pb["B_R_UpperArm"],  0, 0, -65)
    set_bone_rot(pb["B_R_ForeArm"],  -45, 0, 0)
    set_bone_rot(pb["B_L_UpperArm"],  0, 0,  65)
    set_bone_rot(pb["B_L_ForeArm"],  -45, 0, 0)
    set_bone_loc(pb["B_Root"], 0, 0.04, 0)
    key_all_bones(arm_obj, 12)

    # Frame 16: recovering — cannon settles, goblins recovering
    reset_pose(arm_obj)
    set_bone_loc(pb["Root"], 0, 0.03, 0)
    set_bone_rot(pb["Cannon"], -3, 0, 0)
    for prefix in ["A_", "B_"]:
        set_bone_rot(pb[f"{prefix}Spine"], -3, 0, 0)
        set_bone_rot(pb[f"{prefix}Head"],   5, 0, 0)
        set_bone_loc(pb[f"{prefix}Root"], 0, 0.02, 0)
    key_all_bones(arm_obj, 16)

    # Frame 24: back to push stance
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0, side)
    key_all_bones(arm_obj, 24)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Attack animation created (frames 1-24, cannon fire)")
    return action


def create_die_anim(arm_obj):
    """Cannon tips over, both goblins fall backward face-up, limbs spread."""
    action = bpy.data.actions.new("Die")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: alive, push stance
    reset_pose(arm_obj)
    for prefix, side in [("A_", -1), ("B_", 1)]:
        pose_push(pb, prefix, 0, side)
    key_all_bones(arm_obj, 1)

    # Frame 6: hit stagger — whole unit jolts
    reset_pose(arm_obj)
    set_bone_loc(pb["Root"], 0, 0.02, 0)
    set_bone_rot(pb["Cannon"], 5, 0, 3)
    for prefix in ["A_", "B_"]:
        set_bone_rot(pb[f"{prefix}Spine"], 5, 0, 0)
        set_bone_rot(pb[f"{prefix}Head"], 10, 0, 5)
        set_bone_rot(pb[f"{prefix}R_UpperArm"], 10, 0, 20)
        set_bone_rot(pb[f"{prefix}L_UpperArm"], 10, 0, -20)
    key_all_bones(arm_obj, 6)

    # Frame 12: cannon tipping, goblins recoiling backward and outward
    reset_pose(arm_obj)
    set_bone_rot(pb["Cannon"], 10, 0, 25)
    set_bone_loc(pb["Root"], 0, -0.02, -0.02)
    for prefix in ["A_", "B_"]:
        set_bone_rot(pb[f"{prefix}Spine"],     -25, 0, 0)
        set_bone_rot(pb[f"{prefix}Head"],      -15, 0, 0)
        set_bone_rot(pb[f"{prefix}R_UpperArm"], -15, 0, 35)
        set_bone_rot(pb[f"{prefix}R_ForeArm"],  -15, 0, 0)
        set_bone_rot(pb[f"{prefix}L_UpperArm"], -15, 0, -35)
        set_bone_rot(pb[f"{prefix}L_ForeArm"],  -15, 0, 0)
        set_bone_rot(pb[f"{prefix}L_UpperLeg"], -15, 0, 0)
        set_bone_rot(pb[f"{prefix}R_UpperLeg"], -15, 0, 0)
    # Goblins stumble outward and back (Y = bone-local Y = world Z for vertical parent)
    set_bone_loc(pb["A_Root"], -0.05, -0.06, -0.04)
    set_bone_loc(pb["B_Root"],  0.05, -0.06, -0.04)
    key_all_bones(arm_obj, 12)

    # Frame 20: cannon fallen to side, goblins falling flat outward
    reset_pose(arm_obj)
    set_bone_rot(pb["Cannon"], 15, 0, 60)
    set_bone_loc(pb["Root"], 0, -0.04, -0.02)
    # Goblin A — falling backward-left along diagonal
    set_bone_rot(pb["A_Spine"],     -55, -15, 0)    # angled outward left
    set_bone_rot(pb["A_Head"],      -30, 0, -10)
    set_bone_rot(pb["A_R_UpperArm"], -30, 0, 50)
    set_bone_rot(pb["A_R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["A_L_UpperArm"], -30, 0, -50)
    set_bone_rot(pb["A_L_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["A_L_UpperLeg"], -55, 0, 0)
    set_bone_rot(pb["A_R_UpperLeg"], -55, 0, 0)
    set_bone_loc(pb["A_Root"], -0.12, -0.14, -0.12)
    # Goblin B — falling backward-right along diagonal
    set_bone_rot(pb["B_Spine"],     -55, 15, 0)     # angled outward right
    set_bone_rot(pb["B_Head"],      -30, 0, 10)
    set_bone_rot(pb["B_R_UpperArm"], -30, 0, 50)
    set_bone_rot(pb["B_R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["B_L_UpperArm"], -30, 0, -50)
    set_bone_rot(pb["B_L_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["B_L_UpperLeg"], -55, 0, 0)
    set_bone_rot(pb["B_R_UpperLeg"], -55, 0, 0)
    set_bone_loc(pb["B_Root"],  0.12, -0.14, -0.12)
    key_all_bones(arm_obj, 20)

    # Frame 30: on the ground — cannon on its side,
    # goblins face-up at ground level, splayed along diagonals
    # Gob A along -X/+Y diagonal, Gob B along +X/+Y diagonal
    reset_pose(arm_obj)
    set_bone_rot(pb["Cannon"], 15, 0, 85)
    set_bone_loc(pb["Root"], 0, -0.06, 0)

    # Goblin A — lying flat on back, angled outward-left
    set_bone_rot(pb["A_Spine"],     -80, -25, 5)     # flat on back, turned outward-left
    set_bone_rot(pb["A_Head"],      -10, 0, -15)
    set_bone_rot(pb["A_R_UpperArm"], -30, 0, 65)
    set_bone_rot(pb["A_R_ForeArm"],  -15, 0, 0)
    set_bone_rot(pb["A_L_UpperArm"], -30, 0, -65)
    set_bone_rot(pb["A_L_ForeArm"],  -15, 0, 0)
    set_bone_rot(pb["A_L_UpperLeg"], -80, 0, -15)
    set_bone_rot(pb["A_R_UpperLeg"], -80, 0, 15)
    set_bone_loc(pb["A_Root"], -0.20, -0.22, -0.18)   # far outward-left, down to ground

    # Goblin B — lying flat on back, angled outward-right
    set_bone_rot(pb["B_Spine"],     -80, 25, -5)     # flat on back, turned outward-right
    set_bone_rot(pb["B_Head"],      -10, 0, 15)
    set_bone_rot(pb["B_R_UpperArm"], -30, 0, 65)
    set_bone_rot(pb["B_R_ForeArm"],  -15, 0, 0)
    set_bone_rot(pb["B_L_UpperArm"], -30, 0, -65)
    set_bone_rot(pb["B_L_ForeArm"],  -15, 0, 0)
    set_bone_rot(pb["B_L_UpperLeg"], -80, 0, -15)
    set_bone_rot(pb["B_R_UpperLeg"], -80, 0, 15)
    set_bone_loc(pb["B_Root"],  0.20, -0.22, -0.18)   # far outward-right, down to ground

    key_all_bones(arm_obj, 30)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Die animation created (frames 1-30, cannon tips, goblins fall)")
    return action


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    # Build all mesh groups
    cannon_groups = build_cannon_parts()
    goblin_a_groups = build_goblin_parts("A_", GA_X, GA_Y)
    goblin_b_groups = build_goblin_parts("B_", GB_X, GB_Y)

    # Merge all groups
    all_groups = {}
    all_groups.update(cannon_groups)
    all_groups.update(goblin_a_groups)
    all_groups.update(goblin_b_groups)

    # Create armature
    arm_obj = create_armature()

    # Rig everything
    rig_model(arm_obj, all_groups)

    # Switch to pose mode
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')

    # Create animations
    walk_action   = create_walk_cycle(arm_obj)
    attack_action = create_attack_anim(arm_obj)
    die_action    = create_die_anim(arm_obj)

    # Push to NLA tracks
    anim_data = arm_obj.animation_data
    for action in [walk_action, attack_action, die_action]:
        track = anim_data.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, int(action.frame_range[0]), action)
        strip.name = action.name
        track.mute = True

    # Leave Die active for preview
    anim_data.action = die_action
    bpy.context.scene.frame_set(30)

    # Exit pose mode for lights/camera
    bpy.ops.object.mode_set(mode='OBJECT')

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera — pulled back to frame the wider unit
    bpy.ops.object.camera_add(location=(4, -6, 0.5),
                               rotation=(math.radians(82), 0, math.radians(35)))
    bpy.context.active_object.name = "CannoneerCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 30
    bpy.context.scene.render.fps = 24

    # Re-enter Pose Mode for preview
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    bpy.context.scene.frame_set(30)

    print("=" * 50)
    print("  Goblin Cannoneer — rigged & animated!")
    print("  2 goblins + cannon, single armature")
    print("  Actions: Walk (1-25 push), Attack (1-24 fire), Die (1-30 collapse)")
    print("")
    print("  Die action is ACTIVE at frame 30.")
    print("  To export for Unity, run export_goblin_cannoneer.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
