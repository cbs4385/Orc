"""
Blender 4.x Python script — Generate a rigged & animated "Menial"
Human worker who collects treasure from the battlefield.

Visual identity: peach-skinned human in blue tunic (matching Menial.mat color),
with a satchel for carrying loot. Unarmed, simple clothing, leather boots.

Animations:
  Walk   (24 fr loop) — standard walk cycle (BasicOrc/Troll template)
  Attack (20 fr)      — bend down to pick up loot (mapped to "Attack" trigger)
  Die    (30 fr)      — stagger and topple backward (BasicOrc/Troll template)

Single armature. Rigid-body bone parenting (no mesh deformation).
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

MAT_SKIN = MAT_SKIN_DK = MAT_HAIR = MAT_EYES = None
MAT_MOUTH = MAT_TUNIC = MAT_TUNIC_DK = MAT_BELT = None
MAT_LEATHER = MAT_SATCHEL = MAT_BOOTS = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_HAIR, MAT_EYES
    global MAT_MOUTH, MAT_TUNIC, MAT_TUNIC_DK, MAT_BELT
    global MAT_LEATHER, MAT_SATCHEL, MAT_BOOTS
    # Human skin tones
    MAT_SKIN    = make_material("MenialSkin",     (0.76, 0.57, 0.42, 1.0))
    MAT_SKIN_DK = make_material("MenialSkinDark", (0.62, 0.44, 0.32, 1.0))
    MAT_HAIR    = make_material("MenialHair",     (0.25, 0.15, 0.08, 1.0))
    MAT_EYES    = make_material("MenialEyes",     (0.85, 0.85, 0.90, 1.0))
    MAT_MOUTH   = make_material("MenialMouth",    (0.55, 0.25, 0.20, 1.0))
    # Blue tunic matching Menial.mat (0.2, 0.4, 0.8)
    MAT_TUNIC    = make_material("MenialTunic",    (0.20, 0.40, 0.80, 1.0))
    MAT_TUNIC_DK = make_material("MenialTunicDk",  (0.14, 0.28, 0.60, 1.0))
    MAT_BELT     = make_material("MenialBelt",     (0.30, 0.18, 0.08, 1.0))
    MAT_LEATHER  = make_material("MenialLeather",  (0.35, 0.22, 0.10, 1.0))
    MAT_SATCHEL  = make_material("MenialSatchel",  (0.40, 0.25, 0.12, 1.0))
    MAT_BOOTS    = make_material("MenialBoots",    (0.25, 0.15, 0.07, 1.0))


# ──────────────────────────────────────────────
#  Body parts
# ──────────────────────────────────────────────

Z_OFF = 0.08  # raise model so feet touch ground

def build_body_parts():
    """Build all mesh groups for the Menial (human worker).
    Returns dict: bone_name -> mesh object."""
    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso in blue tunic + belt + satchel) ──
    parts = []
    # Torso (slimmer than orc)
    parts.append(add_cube("Torso", (0, 0, z(0.34)),
                          (0.20, 0.14, 0.20), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Blue tunic (covers torso)
    parts.append(add_cube("Tunic", (0, -0.005, z(0.34)),
                          (0.22, 0.16, 0.22), MAT_TUNIC))
    bevel_object(parts[-1], 0.02)
    # Tunic skirt (hangs below waist)
    parts.append(add_cube("TunicSkirt", (0, 0, z(0.20)),
                          (0.20, 0.14, 0.10), MAT_TUNIC))
    bevel_object(parts[-1], 0.01)
    # Belt
    parts.append(add_cube("Belt", (0, 0, z(0.25)),
                          (0.24, 0.16, 0.04), MAT_BELT))
    bevel_object(parts[-1], 0.01)
    # Belt buckle
    parts.append(add_cube("BeltBuckle", (0, -0.08, z(0.25)),
                          (0.04, 0.02, 0.04), MAT_LEATHER))
    # Satchel on back (for carrying loot)
    parts.append(add_cube("Satchel", (0, 0.09, z(0.32)),
                          (0.12, 0.08, 0.14), MAT_SATCHEL))
    bevel_object(parts[-1], 0.02)
    # Satchel strap (across chest)
    parts.append(add_cube("Strap", (-0.05, -0.03, z(0.38)),
                          (0.03, 0.12, 0.16), MAT_LEATHER))
    bevel_object(parts[-1], 0.01)

    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (human face — no tusks, no pointed ears) ──
    parts = []
    # Head (slightly rounder than orc, narrower jaw)
    parts.append(add_cube("Head", (0, 0, z(0.52)),
                          (0.16, 0.16, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    # Hair (cap of dark hair on top and back)
    parts.append(add_cube("HairTop", (0, 0.01, z(0.59)),
                          (0.17, 0.17, 0.08), MAT_HAIR))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("HairBack", (0, 0.06, z(0.54)),
                          (0.15, 0.08, 0.14), MAT_HAIR))
    bevel_object(parts[-1], 0.02)
    # Eyes (white with dark iris — no glow)
    parts.append(add_cube("EyeL", (-0.05, -0.08, z(0.54)),
                          (0.04, 0.02, 0.03), MAT_EYES))
    parts.append(add_cube("EyeR", (0.05, -0.08, z(0.54)),
                          (0.04, 0.02, 0.03), MAT_EYES))
    # Nose (small, human)
    parts.append(add_cube("Nose", (0, -0.09, z(0.51)),
                          (0.03, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Mouth
    parts.append(add_cube("Mouth", (0, -0.08, z(0.46)),
                          (0.08, 0.02, 0.02), MAT_MOUTH))
    # Ears (small, flat, human)
    parts.append(add_cube("EarL", (-0.09, 0, z(0.53)),
                          (0.03, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("EarR", (0.09, 0, z(0.53)),
                          (0.03, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)

    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM (tunic sleeve) ──
    parts = []
    parts.append(add_cube("ArmLU", (-0.14, 0, z(0.38)),
                          (0.08, 0.08, 0.12), MAT_TUNIC))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_UpperArm"] = join_objects(parts, "Grp_L_UpperArm")

    # ── LEFT FOREARM + hand ──
    parts = []
    parts.append(add_cube("ArmLL", (-0.15, -0.02, z(0.28)),
                          (0.07, 0.07, 0.10), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandL", (-0.15, -0.03, z(0.22)),
                          (0.06, 0.06, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── RIGHT UPPER ARM (tunic sleeve) ──
    parts = []
    parts.append(add_cube("ArmRU", (0.14, 0, z(0.38)),
                          (0.08, 0.08, 0.12), MAT_TUNIC))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_UpperArm"] = join_objects(parts, "Grp_R_UpperArm")

    # ── RIGHT FOREARM + hand ──
    parts = []
    parts.append(add_cube("ArmRL", (0.15, -0.02, z(0.28)),
                          (0.07, 0.07, 0.10), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandR", (0.15, -0.03, z(0.22)),
                          (0.06, 0.06, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── LEFT UPPER LEG (tunic skirt covers upper leg) ──
    p = add_cube("LegLU", (-0.06, 0, z(0.12)),
                 (0.08, 0.09, 0.12), MAT_TUNIC_DK)
    bevel_object(p, 0.02)
    apply_modifiers(p)
    groups["L_UpperLeg"] = p

    # ── LEFT LOWER LEG + boot ──
    parts = []
    parts.append(add_cube("LegLL", (-0.06, 0, z(0.02)),
                          (0.07, 0.08, 0.10), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Leather boot
    parts.append(add_cube("BootL", (-0.06, -0.02, z(-0.04)),
                          (0.08, 0.12, 0.05), MAT_BOOTS))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG ──
    p = add_cube("LegRU", (0.06, 0, z(0.12)),
                 (0.08, 0.09, 0.12), MAT_TUNIC_DK)
    bevel_object(p, 0.02)
    apply_modifiers(p)
    groups["R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + boot ──
    parts = []
    parts.append(add_cube("LegRL", (0.06, 0, z(0.02)),
                          (0.07, 0.08, 0.10), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("BootR", (0.06, -0.02, z(-0.04)),
                          (0.08, 0.12, 0.05), MAT_BOOTS))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_LowerLeg"] = join_objects(parts, "Grp_R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def create_armature():
    """Build skeleton for the Menial (human proportions)."""
    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "MenialArmature"
    arm = arm_obj.data
    arm.name = "MenialRig"

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

    # Root (hip level)
    add_bone("Root", (0, 0, z+0.18), (0, 0, z+0.22))

    # Spine (torso)
    add_bone("Spine", (0, 0, z+0.18), (0, 0, z+0.44), "Root", connect=True)

    # Head
    add_bone("Head", (0, 0, z+0.44), (0, 0, z+0.62), "Spine", connect=True)

    # Left arm
    add_bone("L_UpperArm", (-0.11, 0, z+0.42), (-0.15, 0, z+0.38), "Spine")
    add_bone("L_ForeArm",  (-0.15, 0, z+0.38), (-0.16, 0, z+0.22), "L_UpperArm", connect=True)

    # Right arm
    add_bone("R_UpperArm", (0.11, 0, z+0.42), (0.15, 0, z+0.38), "Spine")
    add_bone("R_ForeArm",  (0.15, 0, z+0.38), (0.16, 0, z+0.22), "R_UpperArm", connect=True)

    # Left leg
    add_bone("L_UpperLeg", (-0.06, 0, z+0.18), (-0.06, 0, z+0.06), "Root")
    add_bone("L_LowerLeg", (-0.06, 0, z+0.06), (-0.06, 0, z-0.05), "L_UpperLeg", connect=True)

    # Right leg
    add_bone("R_UpperLeg", (0.06, 0, z+0.18), (0.06, 0, z+0.06), "Root")
    add_bone("R_LowerLeg", (0.06, 0, z+0.06), (0.06, 0, z-0.05), "R_UpperLeg", connect=True)

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


def create_walk_cycle(arm_obj):
    """Worker walk — BasicOrc/Troll template."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 30   # leg swing angle
    arm_sw = 20  # arm counter-swing
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
    set_bone_rot(pb["Spine"], 0, 0, 3)   # slight torso twist
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

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'

    action.use_fake_user = True
    print("  Walk cycle created (frames 1-25, loop)")
    return action


def create_attack_anim(arm_obj):
    """Pick up loot — bend down, grab, stand back up. 20 frames.
    Named 'Attack' for AnimatorController trigger compatibility."""
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: standing
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 5: bending down — torso leans forward, arms reach down
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       35, 0, 0)      # deep forward bend
    set_bone_rot(pb["Head"],       -15, 0, 0)       # looking at ground
    set_bone_rot(pb["R_UpperArm"],  30, 0, 0)       # right arm reaching down
    set_bone_rot(pb["R_ForeArm"],   15, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  30, 0, 0)       # left arm reaching down
    set_bone_rot(pb["L_ForeArm"],   15, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.04, 0)            # crouch slightly
    set_bone_rot(pb["L_UpperLeg"],  15, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -20, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  15, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -20, 0, 0)
    key_all_bones(arm_obj, 5)

    # Frame 9: fully crouched — grabbing loot
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       50, 0, 0)       # deep bend
    set_bone_rot(pb["Head"],       -20, 0, 0)
    set_bone_rot(pb["R_UpperArm"],  45, 0, 10)      # arms far down
    set_bone_rot(pb["R_ForeArm"],   25, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  45, 0, -10)
    set_bone_rot(pb["L_ForeArm"],   25, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.06, -0.02)       # squat down
    set_bone_rot(pb["L_UpperLeg"],  25, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -35, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  25, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -35, 0, 0)
    key_all_bones(arm_obj, 9)

    # Frame 14: standing back up with loot
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       15, 0, 0)
    set_bone_rot(pb["Head"],        -5, 0, 0)
    set_bone_rot(pb["R_UpperArm"],  10, 0, 10)
    set_bone_rot(pb["R_ForeArm"],  -20, 0, 0)       # arms tucking loot
    set_bone_rot(pb["L_UpperArm"],  10, 0, -10)
    set_bone_rot(pb["L_ForeArm"],  -20, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.02, 0)
    key_all_bones(arm_obj, 14)

    # Frame 20: back to standing
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 20)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Attack (pickup) animation created (frames 1-20)")
    return action


def create_die_anim(arm_obj):
    """Stagger and topple backward — BasicOrc/Troll template.
    Root bone local Y = world Z (down=negative), local Z = backward (positive)."""
    action = bpy.data.actions.new("Die")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: alive
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 6: hit stagger — lurch forward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       15, 0, 0)
    set_bone_rot(pb["Head"],        10, 0, 5)
    set_bone_rot(pb["R_UpperArm"],  10, 0, 20)
    set_bone_rot(pb["L_UpperArm"],  10, 0, -20)
    set_bone_loc(pb["Root"], 0, -0.02, 0)
    key_all_bones(arm_obj, 6)

    # Frame 12: recoil backward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       -20, 0, 3)
    set_bone_rot(pb["Head"],        -15, 0, -5)
    set_bone_rot(pb["R_UpperArm"],  -20, 0, 30)
    set_bone_rot(pb["R_ForeArm"],   -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  -20, 0, -30)
    set_bone_rot(pb["L_ForeArm"],   -20, 0, 0)
    set_bone_rot(pb["L_UpperLeg"],  -20, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  -20, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.05, 0.05)
    key_all_bones(arm_obj, 12)

    # Frame 20: falling backward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       -50, 0, 5)
    set_bone_rot(pb["Head"],        -30, 0, -10)
    set_bone_rot(pb["R_UpperArm"],  -40, 0, 45)
    set_bone_rot(pb["R_ForeArm"],   -30, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  -40, 0, -45)
    set_bone_rot(pb["L_ForeArm"],   -30, 0, 0)
    set_bone_rot(pb["L_UpperLeg"],  -50, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  -50, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.20, 0.15)
    key_all_bones(arm_obj, 20)

    # Frame 30: on the ground
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       -80, 0, 5)
    set_bone_rot(pb["Head"],        -40, 0, -15)
    set_bone_rot(pb["R_UpperArm"],  -30, 0, 60)
    set_bone_rot(pb["R_ForeArm"],   -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  -30, 0, -60)
    set_bone_rot(pb["L_ForeArm"],   -20, 0, 0)
    set_bone_rot(pb["L_UpperLeg"],  -80, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  -80, 0, 0)
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
    all_groups = build_body_parts()

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

    # Camera
    bpy.ops.object.camera_add(location=(1.2, -2.0, 0.5),
                               rotation=(math.radians(78), 0, math.radians(25)))
    bpy.context.active_object.name = "MenialCamera"
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
    print("  Menial — rigged & animated!")
    print("  Human worker in blue tunic with satchel")
    print("  Actions: Walk (1-25 loop), Attack/PickUp (1-20), Die (1-30)")
    print("")
    print("  Die action is ACTIVE at frame 30.")
    print("  To export for Unity, run export_menial.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
