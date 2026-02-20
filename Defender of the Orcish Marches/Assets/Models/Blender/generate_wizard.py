"""
Blender 4.x Python script — Generate a rigged & animated "Wizard"
Human defender who casts AoE fire missiles at enemies from long range.

Visual identity: human in purple robes with a pointed wizard hat, long
gray beard, and a staff with a glowing crystal held in the right hand.

Animations:
  Walk   (24 fr loop) — staff held upright in right hand, left arm swings
  Attack (20 fr)      — PLACEHOLDER for hand-posing via read_pose.py
  Die    (30 fr)      — stagger and topple backward

Single armature with R_Hand/L_Hand bones. Rigid-body bone parenting.
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

MAT_SKIN = MAT_SKIN_DK = MAT_BEARD = MAT_EYES = None
MAT_MOUTH = MAT_ROBE = MAT_ROBE_DK = MAT_HAT = None
MAT_BELT = MAT_LEATHER = MAT_SHOES = MAT_METAL = MAT_WOOD = None
MAT_CRYSTAL = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_BEARD, MAT_EYES
    global MAT_MOUTH, MAT_ROBE, MAT_ROBE_DK, MAT_HAT
    global MAT_BELT, MAT_LEATHER, MAT_SHOES, MAT_METAL, MAT_WOOD
    global MAT_CRYSTAL
    # Human skin tones
    MAT_SKIN    = make_material("WizSkin",     (0.76, 0.57, 0.42, 1.0))
    MAT_SKIN_DK = make_material("WizSkinDark", (0.62, 0.44, 0.32, 1.0))
    MAT_BEARD   = make_material("WizBeard",    (0.65, 0.65, 0.68, 1.0))
    MAT_EYES    = make_material("WizEyes",     (0.85, 0.85, 0.90, 1.0))
    MAT_MOUTH   = make_material("WizMouth",    (0.55, 0.25, 0.20, 1.0))
    # Purple robes matching bodyColor (0.53, 0.27, 0.8)
    MAT_ROBE    = make_material("WizRobe",     (0.53, 0.27, 0.80, 1.0))
    MAT_ROBE_DK = make_material("WizRobeDk",   (0.38, 0.18, 0.60, 1.0))
    MAT_HAT     = make_material("WizHat",      (0.48, 0.22, 0.72, 1.0))
    MAT_BELT    = make_material("WizBelt",     (0.30, 0.18, 0.08, 1.0))
    MAT_LEATHER = make_material("WizLeather",  (0.35, 0.22, 0.10, 1.0))
    MAT_SHOES   = make_material("WizShoes",    (0.30, 0.18, 0.10, 1.0))
    MAT_METAL   = make_material("WizMetal",    (0.55, 0.55, 0.58, 1.0), roughness=0.35)
    MAT_WOOD    = make_material("WizWood",     (0.28, 0.16, 0.06, 1.0))
    # Glowing crystal on staff
    MAT_CRYSTAL = make_material("WizCrystal",  (0.40, 0.70, 1.00, 1.0),
                                emission=5.0, roughness=0.2)


# ──────────────────────────────────────────────
#  Body parts
# ──────────────────────────────────────────────

Z_OFF = 0.08  # raise model so feet touch ground

def build_body_parts():
    """Build all mesh groups for the Wizard (human defender).
    Returns dict: bone_name -> mesh object."""
    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso in purple robe + belt + reagent pouches) ──
    parts = []
    # Torso
    parts.append(add_cube("Torso", (0, 0, z(0.34)),
                          (0.22, 0.16, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Purple robe — main body
    parts.append(add_cube("Robe", (0, -0.005, z(0.34)),
                          (0.25, 0.18, 0.26), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    # Robe collar (raised, around neck)
    parts.append(add_cube("RobeCollar", (0, 0, z(0.47)),
                          (0.20, 0.16, 0.04), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.01)
    # Robe front trim (decorative stripe down center)
    parts.append(add_cube("RobeTrim", (0, -0.10, z(0.34)),
                          (0.04, 0.02, 0.22), MAT_ROBE_DK))
    # Robe skirt (longer than other defenders — reaches lower legs)
    parts.append(add_cube("RobeSkirt", (0, 0, z(0.17)),
                          (0.24, 0.17, 0.16), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    # Belt (simple rope belt)
    parts.append(add_cube("Belt", (0, 0, z(0.24)),
                          (0.26, 0.18, 0.04), MAT_BELT))
    bevel_object(parts[-1], 0.01)
    # Belt knot
    parts.append(add_cube("BeltKnot", (0.08, -0.09, z(0.24)),
                          (0.04, 0.03, 0.06), MAT_BELT))
    # Belt tail (hanging rope end)
    parts.append(add_cube("BeltTail", (0.10, -0.08, z(0.19)),
                          (0.02, 0.02, 0.08), MAT_BELT))
    # Reagent pouch left
    parts.append(add_cube("PouchL", (-0.11, -0.06, z(0.22)),
                          (0.05, 0.04, 0.06), MAT_LEATHER))
    bevel_object(parts[-1], 0.01)
    # Scroll case right
    parts.append(add_cylinder("ScrollCase", (0.12, -0.04, z(0.22)),
                              (0.025, 0.025, 0.10), MAT_LEATHER,
                              rotation=(0, 0, math.radians(10)), vertices=6))
    bevel_object(parts[-1], 0.005)

    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (elderly face with long gray beard + pointed wizard hat) ──
    parts = []
    # Head
    parts.append(add_cube("Head", (0, 0, z(0.52)),
                          (0.16, 0.16, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    # Bushy eyebrows (gray, prominent)
    parts.append(add_cube("BrowL", (-0.04, -0.08, z(0.57)),
                          (0.06, 0.02, 0.02), MAT_BEARD))
    parts.append(add_cube("BrowR", (0.04, -0.08, z(0.57)),
                          (0.06, 0.02, 0.02), MAT_BEARD))
    # Eyes (slightly squinted/wise)
    parts.append(add_cube("EyeL", (-0.05, -0.08, z(0.54)),
                          (0.04, 0.02, 0.025), MAT_EYES))
    parts.append(add_cube("EyeR", (0.05, -0.08, z(0.54)),
                          (0.04, 0.02, 0.025), MAT_EYES))
    # Nose (larger, prominent)
    parts.append(add_cube("Nose", (0, -0.09, z(0.51)),
                          (0.035, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Mouth (hidden by beard)
    parts.append(add_cube("Mouth", (0, -0.08, z(0.46)),
                          (0.06, 0.02, 0.02), MAT_MOUTH))
    # Beard — main block hanging from chin
    parts.append(add_cube("BeardMain", (0, -0.06, z(0.42)),
                          (0.10, 0.08, 0.10), MAT_BEARD))
    bevel_object(parts[-1], 0.03)
    # Beard tip — pointed, extends down
    parts.append(add_wedge("BeardTip", (0, -0.07, z(0.34)),
                           (0.06, 0.06, 0.10), MAT_BEARD,
                           rotation=(math.radians(180), 0, 0)))
    # Mustache
    parts.append(add_cube("Mustache", (0, -0.09, z(0.47)),
                          (0.10, 0.03, 0.03), MAT_BEARD))
    bevel_object(parts[-1], 0.01)
    # Ears
    parts.append(add_cube("EarL", (-0.09, 0, z(0.53)),
                          (0.03, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("EarR", (0.09, 0, z(0.53)),
                          (0.03, 0.04, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Wizard hat brim (flat disc at top of head)
    parts.append(add_cube("HatBrim", (0, 0.01, z(0.61)),
                          (0.22, 0.22, 0.025), MAT_HAT))
    bevel_object(parts[-1], 0.02)
    # Wizard hat cone (pointed tip)
    parts.append(add_wedge("HatCone", (0, 0.01, z(0.70)),
                           (0.12, 0.12, 0.20), MAT_HAT))
    # Hat band (decorative ring at base of cone)
    parts.append(add_cube("HatBand", (0, 0.01, z(0.62)),
                          (0.13, 0.13, 0.025), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.005)

    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM (wide purple robe sleeve) ──
    parts = []
    parts.append(add_cube("ArmLU", (-0.15, 0, z(0.38)),
                          (0.10, 0.10, 0.12), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    # Wide sleeve cuff
    parts.append(add_cube("SleeveCuffL", (-0.15, 0, z(0.34)),
                          (0.11, 0.11, 0.04), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["L_UpperArm"] = join_objects(parts, "Grp_L_UpperArm")

    # ── LEFT FOREARM (robe sleeve + hand visible) ──
    parts = []
    parts.append(add_cube("ArmLL", (-0.16, -0.02, z(0.28)),
                          (0.08, 0.08, 0.10), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── LEFT HAND ──
    p = add_cube("HandL", (-0.16, -0.03, z(0.22)),
                  (0.06, 0.06, 0.05), MAT_SKIN_DK)
    bevel_object(p, 0.02)
    apply_modifiers(p)
    groups["L_Hand"] = p

    # ── RIGHT UPPER ARM (wide purple robe sleeve) ──
    parts = []
    parts.append(add_cube("ArmRU", (0.15, 0, z(0.38)),
                          (0.10, 0.10, 0.12), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    # Wide sleeve cuff
    parts.append(add_cube("SleeveCuffR", (0.15, 0, z(0.34)),
                          (0.11, 0.11, 0.04), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["R_UpperArm"] = join_objects(parts, "Grp_R_UpperArm")

    # ── RIGHT FOREARM (robe sleeve) ──
    parts = []
    parts.append(add_cube("ArmRL", (0.16, -0.02, z(0.28)),
                          (0.08, 0.08, 0.10), MAT_ROBE))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── RIGHT HAND + STAFF ──
    parts = []
    # Hand
    parts.append(add_cube("HandR", (0.16, -0.03, z(0.22)),
                          (0.06, 0.06, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)

    # Staff shaft (extends along -Y from hand; points +Z when forearm is horizontal)
    # Longer than pike — a tall wizard staff
    parts.append(add_cylinder("StaffShaft", (0.16, -0.22, z(0.22)),
                              (0.018, 0.018, 0.45), MAT_WOOD,
                              rotation=(math.radians(90), 0, 0), vertices=6))
    bevel_object(parts[-1], 0.005)
    # Staff head — metal mount/cage for the crystal
    parts.append(add_cube("StaffMount", (0.16, -0.45, z(0.22)),
                          (0.035, 0.035, 0.035), MAT_METAL))
    bevel_object(parts[-1], 0.005)
    # Prongs holding the crystal (4 small metal pieces)
    parts.append(add_cube("Prong1", (0.145, -0.47, z(0.22)),
                          (0.008, 0.008, 0.04), MAT_METAL))
    parts.append(add_cube("Prong2", (0.175, -0.47, z(0.22)),
                          (0.008, 0.008, 0.04), MAT_METAL))
    parts.append(add_cube("Prong3", (0.16, -0.47, z(0.205)),
                          (0.008, 0.008, 0.04), MAT_METAL))
    parts.append(add_cube("Prong4", (0.16, -0.47, z(0.235)),
                          (0.008, 0.008, 0.04), MAT_METAL))
    # Glowing crystal orb
    parts.append(add_cube("Crystal", (0.16, -0.48, z(0.22)),
                          (0.045, 0.045, 0.045), MAT_CRYSTAL))
    bevel_object(parts[-1], 0.015)
    # Staff ferrule (metal cap at bottom end)
    parts.append(add_cylinder("StaffFerrule", (0.16, 0.01, z(0.22)),
                              (0.012, 0.012, 0.03), MAT_METAL,
                              rotation=(math.radians(90), 0, 0), vertices=6))

    for p in parts:
        apply_modifiers(p)
    groups["R_Hand"] = join_objects(parts, "Grp_R_Hand")

    # ── LEFT UPPER LEG (covered by robe skirt) ──
    p = add_cube("LegLU", (-0.07, 0, z(0.12)),
                 (0.09, 0.10, 0.12), MAT_ROBE_DK)
    bevel_object(p, 0.02)
    apply_modifiers(p)
    groups["L_UpperLeg"] = p

    # ── LEFT LOWER LEG + soft shoe ──
    parts = []
    parts.append(add_cube("LegLL", (-0.07, 0, z(0.02)),
                          (0.08, 0.09, 0.10), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.02)
    # Soft shoe (lighter than heavy boots)
    parts.append(add_cube("ShoeL", (-0.07, -0.02, z(-0.04)),
                          (0.08, 0.12, 0.05), MAT_SHOES))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG ──
    p = add_cube("LegRU", (0.07, 0, z(0.12)),
                 (0.09, 0.10, 0.12), MAT_ROBE_DK)
    bevel_object(p, 0.02)
    apply_modifiers(p)
    groups["R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + soft shoe ──
    parts = []
    parts.append(add_cube("LegRL", (0.07, 0, z(0.02)),
                          (0.08, 0.09, 0.10), MAT_ROBE_DK))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("ShoeR", (0.07, -0.02, z(-0.04)),
                          (0.08, 0.12, 0.05), MAT_SHOES))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_LowerLeg"] = join_objects(parts, "Grp_R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def create_armature():
    """Build skeleton for the Wizard (human proportions).
    Includes R_Hand/L_Hand bones for hand-posing."""
    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "WizardArmature"
    arm = arm_obj.data
    arm.name = "WizardRig"

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
    add_bone("L_UpperArm", (-0.12, 0, z+0.42), (-0.16, 0, z+0.38), "Spine")
    add_bone("L_ForeArm",  (-0.16, 0, z+0.38), (-0.17, 0, z+0.22), "L_UpperArm", connect=True)
    add_bone("L_Hand",     (-0.17, 0, z+0.22), (-0.17, 0, z+0.17), "L_ForeArm", connect=True)

    # Right arm
    add_bone("R_UpperArm", (0.12, 0, z+0.42), (0.16, 0, z+0.38), "Spine")
    add_bone("R_ForeArm",  (0.16, 0, z+0.38), (0.17, 0, z+0.22), "R_UpperArm", connect=True)
    add_bone("R_Hand",     (0.17, 0, z+0.22), (0.17, 0, z+0.17), "R_ForeArm", connect=True)

    # Left leg
    add_bone("L_UpperLeg", (-0.07, 0, z+0.18), (-0.07, 0, z+0.06), "Root")
    add_bone("L_LowerLeg", (-0.07, 0, z+0.06), (-0.07, 0, z-0.05), "L_UpperLeg", connect=True)

    # Right leg
    add_bone("R_UpperLeg", (0.07, 0, z+0.18), (0.07, 0, z+0.06), "Root")
    add_bone("R_LowerLeg", (0.07, 0, z+0.06), (0.07, 0, z-0.05), "R_UpperLeg", connect=True)

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
    """Walk with staff held upright (+Z) in right hand.
    R_ForeArm X=-90° holds forearm horizontal, staff vertical.
    Left arm swings normally. Slower, stately pace."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 25    # leg swing angle (slower than pikeman)
    l_swing = 20  # left arm swing
    bob = 0.015

    # Frame 1: neutral — staff upright in right hand
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)   # forearm horizontal, staff +Z
    key_all_bones(arm_obj, 1)

    # Frame 7: left leg forward, right leg back
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    set_bone_rot(pb["L_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -swing*0.3, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  l_swing, 0, 0)
    set_bone_rot(pb["L_ForeArm"],  -l_swing*0.4, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    set_bone_rot(pb["Spine"], 0, 0, 2)
    key_all_bones(arm_obj, 7)

    # Frame 13: neutral
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    key_all_bones(arm_obj, 13)

    # Frame 19: mirror — right leg forward, left leg back
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -swing*0.3, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -l_swing, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    set_bone_rot(pb["Spine"], 0, 0, -2)
    key_all_bones(arm_obj, 19)

    # Frame 25: loop back to neutral
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    key_all_bones(arm_obj, 25)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'

    action.use_fake_user = True
    print("  Walk cycle created (frames 1-25, loop)")
    return action


def create_attack_anim(arm_obj):
    """Spell casting — PLACEHOLDER for hand-posing.
    Poses will be captured via read_pose.py and filled in here.
    Keyframes: 1 (rest), 6 (cast), 11 (channel), 16 (release), 20 (rest).
    """
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: rest — staff upright in right hand (same as walk neutral)
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    key_all_bones(arm_obj, 1)

    # Frame 6: raise staff — both arms rise, staff thrust forward
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"], 0, 0, -30)
    set_bone_rot(pb["R_ForeArm"], -75, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -20, 30, 20)
    set_bone_rot(pb["L_ForeArm"], -30, 0, 0)
    set_bone_rot(pb["Spine"], -5, 0, 0)
    key_all_bones(arm_obj, 6)

    # Frame 11: channel — staff extended, leaning into cast
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"], 0, 0, -45)
    set_bone_rot(pb["R_ForeArm"], -60, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -30, 35, 25)
    set_bone_rot(pb["L_ForeArm"], -40, 0, 0)
    set_bone_rot(pb["Spine"], -8, 0, 0)
    set_bone_rot(pb["Head"], 5, 0, 0)
    key_all_bones(arm_obj, 11)

    # Frame 16: release — recoil from spell launch
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"], 5, 0, -20)
    set_bone_rot(pb["R_ForeArm"], -80, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -10, 15, 10)
    set_bone_rot(pb["L_ForeArm"], -15, 0, 0)
    set_bone_rot(pb["Spine"], 3, 0, 0)
    key_all_bones(arm_obj, 16)

    # Frame 20: recover to rest
    reset_pose(arm_obj)
    set_bone_rot(pb["R_ForeArm"], -90, 0, 0)
    key_all_bones(arm_obj, 20)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Attack (spell cast) PLACEHOLDER created (frames 1-20)")
    return action


def create_die_anim(arm_obj):
    """Stagger and topple backward — same template as Pikeman."""
    action = bpy.data.actions.new("Die")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: alive
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 6: hit stagger
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
    set_bone_rot(pb["Spine"],       -80.0, 0.0, 5.0)
    set_bone_rot(pb["Head"],        -40.0, 0.0, -15.0)
    set_bone_rot(pb["L_UpperArm"],  161.5, -21.8, -92.9)
    set_bone_rot(pb["L_ForeArm"],   -10.0, 0.0, -20.0)
    set_bone_rot(pb["R_UpperArm"],  69.1, -41.8, -46.0)
    set_bone_rot(pb["R_ForeArm"],   -10.0, 0.0, 20.0)
    set_bone_rot(pb["L_UpperLeg"],  -67.7, 30.3, 23.3)
    set_bone_rot(pb["L_LowerLeg"],  10.0, 0.0, 0.0)
    set_bone_rot(pb["R_UpperLeg"],  -74.0, -16.0, -20.8)
    set_bone_rot(pb["R_LowerLeg"],  10.0, 0.0, 0.0)
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

    # Leave Attack active for posing
    anim_data.action = attack_action
    bpy.context.scene.frame_set(1)

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
    bpy.context.active_object.name = "WizardCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 30
    bpy.context.scene.render.fps = 24

    # Re-enter Pose Mode for posing
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')

    # Set Attack as active action at frame 1 for posing
    anim_data.action = attack_action
    bpy.context.scene.frame_set(1)

    print("=" * 50)
    print("  Wizard — rigged with R_Hand / L_Hand bones!")
    print("  Purple-robed AoE caster with staff and crystal")
    print("  Bones: Root, Spine, Head, L/R_UpperArm, L/R_ForeArm,")
    print("         L/R_Hand, L/R_UpperLeg, L/R_LowerLeg")
    print("")
    print("  Attack action is ACTIVE at frame 1 (placeholder).")
    print("  Staff + crystal mesh are on R_Hand bone.")
    print("  Pose the model, then run read_pose.py to capture transforms.")
    print("  Walk (1-25 loop) and Die (1-30) are in NLA tracks.")
    print("=" * 50)


if __name__ == "__main__":
    main()
