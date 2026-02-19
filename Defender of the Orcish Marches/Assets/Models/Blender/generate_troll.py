"""
Blender 4.x Python script — Generate a rigged & animated low-poly "Troll"
A massive wall-breaker enemy: barrel chest, stone armor, big stone maul.
Visually distinct from orcs — wider, thicker, more brutish proportions.

Usage:
  1. Open Blender 4.2
  2. Switch to the Scripting workspace
  3. Open this file and click "Run Script"
  4. The troll appears at the origin with 3 animation clips
  5. Export as FBX → Assets/Models/Troll.fbx  (for Unity)
     - Scale: 1.0, Forward: -Z Forward, Up: Y Up
     - Check "Apply Transform"
     - Under Armature: check "Add Leaf Bones" OFF
     - Under Animation: check "All Actions"

Animations:  Walk (24 fr loop), Attack (24 fr overhead smash), Die (30 fr)
The model uses rigid-body bone parenting (no mesh deformation) —
each body part moves as a solid block, matching the art style.
"""

import bpy
import math
from mathutils import Vector, Euler

# ──────────────────────────────────────────────
#  Utility helpers
# ──────────────────────────────────────────────

def clear_scene():
    """Remove everything, including fake-user actions from prior runs."""
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
    """Create a simple Principled BSDF material."""
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
    """Add a cube, apply rotation+scale, assign material."""
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
    """Create a 4-sided cone (wedge) for ears/fangs."""
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
    """Add a cylinder, apply rotation+scale, assign material."""
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


def bevel_object(obj, width=0.02, segments=1):
    """Add a subtle bevel modifier."""
    mod = obj.modifiers.new("Bevel", 'BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(60)


def apply_modifiers(obj):
    """Apply all modifiers on an object."""
    bpy.context.view_layer.objects.active = obj
    for mod in obj.modifiers[:]:
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except Exception:
            pass


def join_objects(objects, name):
    """Join a list of objects into one, return the result."""
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.active_object
    result.name = name
    return result


def parent_to_bone(obj, armature, bone_name):
    """Parent an object to a specific bone using the operator
    so Blender computes the correct inverse matrix automatically."""
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
MAT_STONE = MAT_WOOD = MAT_TEETH = None
MAT_LEATHER = MAT_METAL = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_MOUTH, MAT_EYES
    global MAT_STONE, MAT_WOOD, MAT_TEETH
    global MAT_LEATHER, MAT_METAL
    # Olive-green skin — distinct from orc's brighter green
    MAT_SKIN    = make_material("TrollSkin",     (0.30, 0.50, 0.18, 1.0))
    MAT_SKIN_DK = make_material("TrollSkinDark", (0.20, 0.35, 0.10, 1.0))
    MAT_MOUTH   = make_material("TrollMouth",    (0.25, 0.08, 0.05, 1.0))
    MAT_EYES    = make_material("TrollEyes",     (1.0,  0.2,  0.05, 1.0), emission=3.0)
    MAT_STONE   = make_material("TrollStone",    (0.50, 0.48, 0.44, 1.0), roughness=1.0)
    MAT_WOOD    = make_material("TrollWood",     (0.30, 0.18, 0.06, 1.0))
    MAT_TEETH   = make_material("TrollTeeth",    (0.85, 0.80, 0.60, 1.0))
    MAT_LEATHER = make_material("TrollLeather",  (0.30, 0.20, 0.08, 1.0))
    MAT_METAL   = make_material("TrollMetal",    (0.35, 0.33, 0.30, 1.0), roughness=0.6)


# ──────────────────────────────────────────────
#  Body-part groups (joined per bone)
# ──────────────────────────────────────────────

# Troll is ~5% taller than orc but ~40% wider/thicker.
# Unity applies 1.5x bodyScale on top, making the troll
# tower over the standard orc in-game.

Z_OFF = 0.11  # raise all parts so feet touch ground

def build_body_parts():
    """Create all mesh parts, grouped by bone assignment.
    Returns dict: bone_name -> single joined mesh object."""

    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso + belly + belt + shoulder armor + loincloth + strap) ──
    parts = []
    # Barrel chest — much wider than orc
    parts.append(add_cube("Torso", (0, 0, z(0.42)),
                          (0.50, 0.34, 0.34), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    # Big pot belly
    parts.append(add_cube("Belly", (0, -0.16, z(0.34)),
                          (0.44, 0.14, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.05)
    # Stone belt / girdle
    parts.append(add_cube("Belt", (0, 0, z(0.24)),
                          (0.54, 0.38, 0.08), MAT_STONE))
    bevel_object(parts[-1], 0.02)
    # Belt buckle
    parts.append(add_cube("BeltBuckle", (0, -0.18, z(0.24)),
                          (0.08, 0.04, 0.06), MAT_METAL))
    # Leather loincloth hanging from belt
    parts.append(add_cube("Loincloth", (0, -0.14, z(0.14)),
                          (0.22, 0.06, 0.16), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    # Left stone shoulder pauldron
    parts.append(add_cube("ShoulderL", (-0.30, -0.02, z(0.58)),
                          (0.18, 0.14, 0.10), MAT_STONE))
    bevel_object(parts[-1], 0.02)
    # Right stone shoulder pauldron
    parts.append(add_cube("ShoulderR", (0.30, -0.02, z(0.58)),
                          (0.18, 0.14, 0.10), MAT_STONE))
    bevel_object(parts[-1], 0.02)
    # Leather chest strap
    parts.append(add_cube("ChestStrap", (0.06, -0.08, z(0.44)),
                          (0.08, 0.04, 0.28), MAT_LEATHER,
                          rotation=(0, 0, math.radians(20))))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (wide skull + heavy brow + small eyes + big jaw + tusks + ears) ──
    parts = []
    # Wide, slightly squashed skull
    parts.append(add_cube("Head", (0, 0, z(0.72)),
                          (0.38, 0.34, 0.28), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    # Heavy brow ridge
    parts.append(add_cube("Brow", (0, -0.16, z(0.78)),
                          (0.36, 0.08, 0.07), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.03)
    # Small, angry eyes
    parts.append(add_cube("EyeL", (-0.10, -0.17, z(0.74)),
                          (0.06, 0.04, 0.04), MAT_EYES))
    parts.append(add_cube("EyeR", ( 0.10, -0.17, z(0.74)),
                          (0.06, 0.04, 0.04), MAT_EYES))
    # Broad flat nose
    parts.append(add_cube("Nose", (0, -0.20, z(0.70)),
                          (0.10, 0.06, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Wide heavy jaw / underbite
    parts.append(add_cube("Jaw", (0, -0.14, z(0.62)),
                          (0.28, 0.10, 0.10), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("Mouth", (0, -0.18, z(0.63)),
                          (0.18, 0.04, 0.06), MAT_MOUTH))
    # Large tusks — prominent underbite
    parts.append(add_wedge("TuskL", (-0.08, -0.20, z(0.70)),
                           (0.05, 0.04, 0.10), MAT_TEETH))
    parts.append(add_wedge("TuskR", ( 0.08, -0.20, z(0.70)),
                           (0.05, 0.04, 0.10), MAT_TEETH))
    # Big pointed ears
    parts.append(add_wedge("EarL", (-0.26, 0, z(0.74)),
                           (0.08, 0.14, 0.16), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(-30))))
    parts.append(add_wedge("EarR", ( 0.26, 0, z(0.74)),
                           (0.08, 0.14, 0.16), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(30))))
    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM — thick ──
    p = add_cube("ArmLUpper", (-0.34, 0, z(0.50)),
                 (0.18, 0.18, 0.24), MAT_SKIN)
    bevel_object(p, 0.03); apply_modifiers(p)
    groups["L_UpperArm"] = p

    # ── LEFT FOREARM + HAND (fist) + WRISTGUARD ──
    parts = []
    parts.append(add_cube("ArmLLower", (-0.36, -0.04, z(0.32)),
                          (0.17, 0.17, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("HandL", (-0.36, -0.06, z(0.20)),
                          (0.14, 0.14, 0.10), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.03)
    # Stone wristguard
    parts.append(add_cube("WristL", (-0.36, -0.04, z(0.26)),
                          (0.19, 0.19, 0.06), MAT_STONE))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── RIGHT UPPER ARM — thick ──
    p = add_cube("ArmRUpper", (0.34, 0, z(0.50)),
                 (0.18, 0.18, 0.24), MAT_SKIN)
    bevel_object(p, 0.03); apply_modifiers(p)
    groups["R_UpperArm"] = p

    # ── RIGHT FOREARM + HAND + WRISTGUARD + STONE MAUL ──
    parts = []
    parts.append(add_cube("ArmRLower", (0.36, -0.04, z(0.32)),
                          (0.17, 0.17, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("HandR", (0.36, -0.06, z(0.20)),
                          (0.14, 0.14, 0.10), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.03)
    # Stone wristguard
    parts.append(add_cube("WristR", (0.36, -0.04, z(0.26)),
                          (0.19, 0.19, 0.06), MAT_STONE))
    bevel_object(parts[-1], 0.01)
    # Stone maul — held horizontally, extending forward (-Y) from hand
    # Handle rotated 90° on X so cylinder runs along Y axis
    parts.append(add_cylinder("MaulHandle", (0.36, -0.24, z(0.20)),
                              (0.04, 0.04, 0.30), MAT_WOOD,
                              rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Massive stone head at far end of handle
    parts.append(add_cube("MaulHead", (0.36, -0.42, z(0.20)),
                          (0.14, 0.16, 0.12), MAT_STONE))
    bevel_object(parts[-1], 0.03)
    # Iron bands around the stone head
    parts.append(add_cube("MaulBand1", (0.36, -0.35, z(0.20)),
                          (0.15, 0.02, 0.13), MAT_METAL))
    parts.append(add_cube("MaulBand2", (0.36, -0.49, z(0.20)),
                          (0.15, 0.02, 0.13), MAT_METAL))
    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── LEFT UPPER LEG — thick ──
    p = add_cube("LegLUpper", (-0.15, 0, z(0.12)),
                 (0.18, 0.20, 0.22), MAT_SKIN)
    bevel_object(p, 0.03); apply_modifiers(p)
    groups["L_UpperLeg"] = p

    # ── LEFT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegLLower", (-0.15, 0, z(-0.02)),
                          (0.16, 0.18, 0.16), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootL", (-0.15, -0.06, z(-0.08)),
                          (0.16, 0.22, 0.08), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG — thick ──
    p = add_cube("LegRUpper", (0.15, 0, z(0.12)),
                 (0.18, 0.20, 0.22), MAT_SKIN)
    bevel_object(p, 0.03); apply_modifiers(p)
    groups["R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegRLower", (0.15, 0, z(-0.02)),
                          (0.16, 0.18, 0.16), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootR", (0.15, -0.06, z(-0.08)),
                          (0.16, 0.22, 0.08), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_LowerLeg"] = join_objects(parts, "Grp_R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def create_armature():
    """Build skeleton — same bone names as orc for animator compatibility,
    but wider shoulder/hip positions for the troll's stocky build."""

    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "TrollArmature"
    arm = arm_obj.data
    arm.name = "TrollRig"

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

    # Root → Hips (slightly higher than orc)
    add_bone("Root",       (0, 0, z+0.24), (0, 0, z+0.28))
    # Spine (taller torso)
    add_bone("Spine",      (0, 0, z+0.24), (0, 0, z+0.58), "Root", connect=True)
    # Head
    add_bone("Head",       (0, 0, z+0.58), (0, 0, z+0.86), "Spine", connect=True)

    # Left arm — wider shoulder position
    add_bone("L_UpperArm", (-0.25, 0, z+0.55), (-0.35, 0, z+0.53), "Spine")
    add_bone("L_ForeArm",  (-0.35, 0, z+0.53), (-0.37, 0, z+0.26), "L_UpperArm", connect=True)

    # Right arm — wider shoulder position
    add_bone("R_UpperArm", (0.25, 0, z+0.55),  (0.35, 0, z+0.53),  "Spine")
    add_bone("R_ForeArm",  (0.35, 0, z+0.53),  (0.37, 0, z+0.26),  "R_UpperArm", connect=True)

    # Left leg — wider stance
    add_bone("L_UpperLeg", (-0.15, 0, z+0.24), (-0.15, 0, z+0.06), "Root")
    add_bone("L_LowerLeg", (-0.15, 0, z+0.06), (-0.15, 0, z-0.08), "L_UpperLeg", connect=True)

    # Right leg — wider stance
    add_bone("R_UpperLeg", (0.15, 0, z+0.24),  (0.15, 0, z+0.06),  "Root")
    add_bone("R_LowerLeg", (0.15, 0, z+0.06),  (0.15, 0, z-0.08),  "R_UpperLeg", connect=True)

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj


# ──────────────────────────────────────────────
#  Rig: parent mesh groups to bones
# ──────────────────────────────────────────────

def rig_model(arm_obj, groups):
    """Parent each mesh group to its corresponding bone."""
    for bone_name, mesh_obj in groups.items():
        parent_to_bone(mesh_obj, arm_obj, bone_name)


# ──────────────────────────────────────────────
#  Animations
# ──────────────────────────────────────────────

def set_bone_rot(pose_bone, x_deg, y_deg, z_deg):
    """Set rotation in Euler degrees."""
    pose_bone.rotation_mode = 'XYZ'
    pose_bone.rotation_euler = (
        math.radians(x_deg),
        math.radians(y_deg),
        math.radians(z_deg)
    )


def set_bone_loc(pose_bone, x, y, z_val):
    """Set bone location offset."""
    pose_bone.location = (x, y, z_val)


def key_all_bones(arm_obj, frame):
    """Insert keyframes for rotation & location on all pose bones."""
    for pb in arm_obj.pose.bones:
        pb.keyframe_insert(data_path="rotation_euler", frame=frame)
        pb.keyframe_insert(data_path="location", frame=frame)


def reset_pose(arm_obj):
    """Reset all pose bones to rest."""
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


def create_walk_cycle(arm_obj):
    """Create a looping walk cycle — 24 frames at 24fps = 1 second.
    Based on BasicOrc walk but with reduced swing for heavier gait."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 30   # leg swing angle (same as BasicOrc)
    arm_sw = 20  # arm counter-swing (same as BasicOrc)
    bob = 0.02   # slight up/down on root

    # Frame 1: neutral (start of loop)
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 7: left leg forward, right leg back
    reset_pose(arm_obj)
    set_bone_rot(pb["L_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -swing*0.3, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["R_LowerLeg"],  0, 0, 0)
    set_bone_rot(pb["R_UpperArm"],  arm_sw, 0, 0)
    set_bone_rot(pb["R_ForeArm"],  -arm_sw*0.4, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -arm_sw, 0, 0)
    set_bone_rot(pb["L_ForeArm"],   0, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    set_bone_rot(pb["Spine"], 0, 0, 3)  # slight torso twist
    key_all_bones(arm_obj, 7)

    # Frame 13: neutral (mid loop)
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 13)

    # Frame 19: right leg forward, left leg back (mirror of frame 7)
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -swing*0.3, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["L_LowerLeg"],  0, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  arm_sw, 0, 0)
    set_bone_rot(pb["L_ForeArm"],  -arm_sw*0.4, 0, 0)
    set_bone_rot(pb["R_UpperArm"], -arm_sw, 0, 0)
    set_bone_rot(pb["R_ForeArm"],   0, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    set_bone_rot(pb["Spine"], 0, 0, -3)
    key_all_bones(arm_obj, 19)

    # Frame 25: same as frame 1 for seamless loop
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 25)

    # Make all F-curves linear for snappy movement
    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'

    action.use_fake_user = True
    print("  Walk cycle created (frames 1-25, loop)")
    return action


def create_attack_anim(arm_obj):
    """Overhead club smash — based on BasicOrc attack pattern.
    Club arm raises beside head, then slams down. 20 frames."""
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: rest
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 5: wind up — raise club arm up beside head
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  0, 0, -70)   # raise arm up beside head
    set_bone_rot(pb["R_ForeArm"],  -30, 0, 0)    # bend forearm back behind head
    set_bone_rot(pb["Spine"],        0, 0, -5)    # slight lean back
    set_bone_rot(pb["Head"],         0, 0, 0)
    key_all_bones(arm_obj, 5)

    # Frame 8: peak of wind-up
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  0, 0, -85)   # arm fully raised
    set_bone_rot(pb["R_ForeArm"],  -40, 0, 0)    # forearm bent back
    set_bone_rot(pb["Spine"],       -5, 0, -8)    # lean back into swing
    set_bone_rot(pb["Head"],         5, 0, 0)
    key_all_bones(arm_obj, 8)

    # Frame 11: slam down — arm comes down past horizontal
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  15, 0, 30)   # arm swung down and forward
    set_bone_rot(pb["R_ForeArm"],   20, 0, 0)    # forearm extends
    set_bone_rot(pb["Spine"],        8, 0, 5)     # lunge forward
    set_bone_rot(pb["Head"],        -5, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.02, -0.03)    # crouch into swing
    key_all_bones(arm_obj, 11)

    # Frame 14: impact hold
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  10, 0, 25)   # arm low, impact position
    set_bone_rot(pb["R_ForeArm"],   15, 0, 0)
    set_bone_rot(pb["Spine"],        5, 0, 3)
    set_bone_loc(pb["Root"], 0, -0.02, -0.02)
    key_all_bones(arm_obj, 14)

    # Frame 20: recover to rest
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 20)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Attack animation created (frames 1-20)")
    return action


def create_die_anim(arm_obj):
    """Stagger and topple backward — 30 frames.
    Based on BasicOrc die but with arms ending in X/Y ground plane:
    R arm toward -X/+Y, L arm toward +X/-Y."""
    action = bpy.data.actions.new("Die")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: alive
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Root bone points up (+Z world), so bone local Y = world Z.
    # To move DOWN: negative Y.  To move BACKWARD: positive Z.

    # Frame 6: hit stagger — lurch forward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    15, 0, 0)
    set_bone_rot(pb["Head"],     10, 0, 5)
    set_bone_rot(pb["R_UpperArm"], 10, 0, 20)
    set_bone_rot(pb["L_UpperArm"], 10, 0, -20)
    set_bone_loc(pb["Root"], 0, -0.02, 0)
    key_all_bones(arm_obj, 6)

    # Frame 12: recoil backward — legs match spine tilt
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    -20, 0, 3)
    set_bone_rot(pb["Head"],     -15, 0, -5)
    set_bone_rot(pb["R_UpperArm"], -20, 0, 30)
    set_bone_rot(pb["R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -20, 0, -30)
    set_bone_rot(pb["L_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -20, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -20, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.05, 0.05)
    key_all_bones(arm_obj, 12)

    # Frame 20: falling — whole body rigid, legs follow spine
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    -50, 0, 5)
    set_bone_rot(pb["Head"],     -30, 0, -10)
    set_bone_rot(pb["R_UpperArm"], -40, 0, 45)
    set_bone_rot(pb["R_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -40, 0, -45)
    set_bone_rot(pb["L_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -50, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -50, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.20, 0.15)
    key_all_bones(arm_obj, 20)

    # Frame 30: on the ground — arms splayed in ground plane
    # Values captured from manual pose in Blender
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],      -80.0,   0.0,    5.0)
    set_bone_rot(pb["Head"],        -1.6,   8.7,  -21.9)
    set_bone_rot(pb["R_UpperArm"], -30.0,   0.0,   60.0)
    set_bone_rot(pb["R_ForeArm"],  -55.4, -43.6,  -33.8)
    set_bone_rot(pb["L_UpperArm"],  98.2,  57.5,   43.9)
    set_bone_rot(pb["L_ForeArm"],  -57.1,  -0.2,   25.2)
    set_bone_rot(pb["L_UpperLeg"], -80.0,   0.0,    0.0)
    set_bone_rot(pb["R_UpperLeg"], -80.0,   0.0,    0.0)
    set_bone_loc(pb["Root"], 0, -0.35, 0.30)
    key_all_bones(arm_obj, 30)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Die animation created (frames 1-30)")
    return action


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    # Build mesh groups
    groups = build_body_parts()

    # Create armature
    arm_obj = create_armature()

    # Rig: parent each mesh to its bone
    rig_model(arm_obj, groups)

    # Switch to pose mode for animation
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')

    # Create animation clips
    walk_action   = create_walk_cycle(arm_obj)
    attack_action = create_attack_anim(arm_obj)
    die_action    = create_die_anim(arm_obj)

    # Push each action onto its own NLA track so all export correctly
    anim_data = arm_obj.animation_data

    for action in [walk_action, attack_action, die_action]:
        track = anim_data.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, int(action.frame_range[0]), action)
        strip.name = action.name
        track.mute = True  # muted so NLA tracks don't blend together

    # Leave Die action active so user can preview/tweak the death end pose
    anim_data.action = die_action
    bpy.context.scene.frame_set(30)

    # Temporarily exit Pose Mode to add lights/camera
    bpy.ops.object.mode_set(mode='OBJECT')

    # Lighting for preview
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera — pulled back to frame the larger troll
    bpy.ops.object.camera_add(location=(5, -8, 0.8),
                               rotation=(math.radians(80), 0, math.radians(30)))
    bpy.context.active_object.name = "TrollCamera"
    bpy.context.scene.camera = bpy.context.active_object

    # Scene settings
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 30
    bpy.context.scene.render.fps = 24

    # Re-select armature and enter Pose Mode at frame 30 (Die end pose)
    # so user can preview and manually adjust the death animation
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    bpy.context.scene.frame_set(30)

    print("=" * 50)
    print("  Troll — rigged & animated!")
    print("  Actions: Walk (1-25 loop), Attack (1-20 smash), Die (1-30)")
    print("")
    print("  Die action is ACTIVE at frame 30 — you can adjust the death pose.")
    print("  Scrub the timeline to preview animations.")
    print("  To switch actions: select the armature, go to Action Editor,")
    print("  and pick Walk/Attack/Die from the dropdown.")
    print("")
    print("  To export for Unity, run export_troll.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
