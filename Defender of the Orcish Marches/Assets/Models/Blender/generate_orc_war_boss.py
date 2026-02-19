"""
Blender 4.x Python script — Generate a rigged & animated "Orc War Boss"
Massive armored orc warlord wielding a great war hammer.

Stats context (from Unity EnemyData):
  HP=500, damage=50, speed=1.5, scale=2.5x, melee, attackRange=1.5
  bodyColor = RGB(0.6, 0.15, 0.1) — dark reddish-brown

Animations:
  Walk   (24 fr loop) — heavy lumbering stride, weapon at side, ground-shaking weight
  Attack (24 fr)      — overhead hammer slam with lunge
  Die    (30 fr)      — staggers, drops to knees, collapses face-down

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

MAT_SKIN = MAT_SKIN_DK = MAT_MOUTH = MAT_EYES = None
MAT_TEETH = MAT_CLOTH = MAT_FUR = None
MAT_ARMOR = MAT_ARMOR_DK = MAT_GOLD = MAT_WOOD = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_MOUTH, MAT_EYES
    global MAT_TEETH, MAT_CLOTH, MAT_FUR
    global MAT_ARMOR, MAT_ARMOR_DK, MAT_GOLD, MAT_WOOD
    # Dark reddish-brown orc skin (matches bodyColor 0.6, 0.15, 0.1)
    MAT_SKIN    = make_material("BossSkin",     (0.60, 0.15, 0.10, 1.0))
    MAT_SKIN_DK = make_material("BossSkinDark", (0.40, 0.10, 0.06, 1.0))
    MAT_MOUTH   = make_material("BossMouth",    (0.30, 0.05, 0.03, 1.0))
    MAT_EYES    = make_material("BossEyes",     (1.0,  0.3,  0.0,  1.0), emission=5.0)
    MAT_TEETH   = make_material("BossTeeth",    (0.85, 0.80, 0.55, 1.0))
    MAT_CLOTH   = make_material("BossCloth",    (0.20, 0.10, 0.06, 1.0))
    MAT_FUR     = make_material("BossFur",      (0.30, 0.18, 0.10, 1.0))
    MAT_ARMOR   = make_material("BossArmor",    (0.22, 0.22, 0.25, 1.0), roughness=0.4)
    MAT_ARMOR_DK = make_material("BossArmorDk", (0.12, 0.12, 0.15, 1.0), roughness=0.5)
    MAT_GOLD    = make_material("BossGold",     (0.75, 0.55, 0.15, 1.0), roughness=0.3)
    MAT_WOOD    = make_material("BossWood",     (0.35, 0.20, 0.08, 1.0))


# ──────────────────────────────────────────────
#  Body parts
# ──────────────────────────────────────────────

Z_OFF = 0.10  # raise model so feet rest on ground

def build_body_parts():
    """Build all mesh groups for the Orc War Boss.
    Returns dict: bone_name -> mesh object."""
    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso + chest armor + belt + loincloth + back plate) ──
    parts = []
    # Massive muscular torso
    parts.append(add_cube("Torso", (0, 0, z(0.38)),
                          (0.32, 0.22, 0.24), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    # Chest plate (front armor)
    parts.append(add_cube("ChestPlate", (0, -0.09, z(0.40)),
                          (0.26, 0.05, 0.18), MAT_ARMOR))
    bevel_object(parts[-1], 0.02)
    # Chest plate center ridge
    parts.append(add_cube("ChestRidge", (0, -0.12, z(0.40)),
                          (0.04, 0.02, 0.16), MAT_GOLD))
    # Back plate
    parts.append(add_cube("BackPlate", (0, 0.10, z(0.40)),
                          (0.24, 0.04, 0.16), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Fur mantle across shoulders
    parts.append(add_cube("FurMantle", (0, 0.02, z(0.50)),
                          (0.36, 0.20, 0.06), MAT_FUR))
    bevel_object(parts[-1], 0.02)
    # Belt
    parts.append(add_cube("Belt", (0, 0, z(0.27)),
                          (0.34, 0.24, 0.06), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Belt buckle (gold skull)
    parts.append(add_sphere("BeltSkull", (0, -0.12, z(0.27)),
                            (0.06, 0.04, 0.06), MAT_GOLD, segments=6, rings=4))
    # Loincloth front
    parts.append(add_cube("Loincloth", (0, -0.08, z(0.19)),
                          (0.16, 0.04, 0.12), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)
    # Loincloth back
    parts.append(add_cube("LoinclothBack", (0, 0.08, z(0.19)),
                          (0.16, 0.04, 0.10), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)

    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (large brutish head + helmet crown + tusks) ──
    parts = []
    # Head block
    parts.append(add_cube("Head", (0, 0, z(0.58)),
                          (0.24, 0.22, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    # Heavy brow ridge
    parts.append(add_cube("Brow", (0, -0.10, z(0.64)),
                          (0.24, 0.06, 0.04), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Eyes (menacing red-orange glow)
    parts.append(add_cube("EyeL", (-0.07, -0.11, z(0.61)),
                          (0.06, 0.04, 0.05), MAT_EYES))
    parts.append(add_cube("EyeR", (0.07, -0.11, z(0.61)),
                          (0.06, 0.04, 0.05), MAT_EYES))
    # Nose (broad, flat)
    parts.append(add_cube("Nose", (0, -0.13, z(0.57)),
                          (0.08, 0.05, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Jaw / lower face
    parts.append(add_cube("Jaw", (0, -0.08, z(0.50)),
                          (0.20, 0.16, 0.06), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Mouth
    parts.append(add_cube("Mouth", (0, -0.12, z(0.51)),
                          (0.14, 0.04, 0.04), MAT_MOUTH))
    # Left tusk (large, curving upward from jaw)
    parts.append(add_wedge("TuskL", (-0.08, -0.14, z(0.54)),
                           (0.04, 0.04, 0.08), MAT_TEETH,
                           rotation=(math.radians(10), 0, math.radians(15))))
    # Right tusk
    parts.append(add_wedge("TuskR", (0.08, -0.14, z(0.54)),
                           (0.04, 0.04, 0.08), MAT_TEETH,
                           rotation=(math.radians(10), 0, math.radians(-15))))
    # Ears (small for an orc, pointed)
    parts.append(add_wedge("EarL", (-0.15, 0, z(0.60)),
                           (0.04, 0.08, 0.10), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(-30))))
    parts.append(add_wedge("EarR", (0.15, 0, z(0.60)),
                           (0.04, 0.08, 0.10), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(30))))
    # Iron helmet band / crown
    parts.append(add_cube("HelmetBand", (0, 0, z(0.67)),
                          (0.26, 0.24, 0.04), MAT_ARMOR))
    bevel_object(parts[-1], 0.01)
    # Crown spikes (3 across top)
    parts.append(add_wedge("Spike1", (0, -0.02, z(0.72)),
                           (0.03, 0.03, 0.08), MAT_ARMOR))
    parts.append(add_wedge("Spike2", (-0.08, -0.02, z(0.71)),
                           (0.03, 0.03, 0.06), MAT_ARMOR,
                           rotation=(0, 0, math.radians(10))))
    parts.append(add_wedge("Spike3", (0.08, -0.02, z(0.71)),
                           (0.03, 0.03, 0.06), MAT_ARMOR,
                           rotation=(0, 0, math.radians(-10))))

    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM + shoulder pauldron ──
    parts = []
    parts.append(add_cube("ArmLU", (-0.20, 0, z(0.44)),
                          (0.12, 0.12, 0.16), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Shoulder pauldron (large, spiked)
    parts.append(add_cube("PauldronL", (-0.20, 0, z(0.51)),
                          (0.16, 0.16, 0.07), MAT_ARMOR))
    bevel_object(parts[-1], 0.02)
    # Pauldron spike
    parts.append(add_wedge("PauldronSpikeL", (-0.20, -0.06, z(0.56)),
                           (0.03, 0.03, 0.07), MAT_ARMOR))
    for p in parts:
        apply_modifiers(p)
    groups["L_UpperArm"] = join_objects(parts, "Grp_L_UpperArm")

    # ── LEFT FOREARM + fist ──
    parts = []
    parts.append(add_cube("ArmLL", (-0.23, -0.02, z(0.34)),
                          (0.11, 0.11, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Bracer armor
    parts.append(add_cube("BracerL", (-0.23, -0.04, z(0.36)),
                          (0.13, 0.08, 0.10), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Fist
    parts.append(add_cube("FistL", (-0.24, -0.03, z(0.26)),
                          (0.10, 0.10, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── RIGHT UPPER ARM + shoulder pauldron ──
    parts = []
    parts.append(add_cube("ArmRU", (0.20, 0, z(0.44)),
                          (0.12, 0.12, 0.16), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Shoulder pauldron (large, spiked)
    parts.append(add_cube("PauldronR", (0.20, 0, z(0.51)),
                          (0.16, 0.16, 0.07), MAT_ARMOR))
    bevel_object(parts[-1], 0.02)
    # Pauldron spike
    parts.append(add_wedge("PauldronSpikeR", (0.20, -0.06, z(0.56)),
                           (0.03, 0.03, 0.07), MAT_ARMOR))
    for p in parts:
        apply_modifiers(p)
    groups["R_UpperArm"] = join_objects(parts, "Grp_R_UpperArm")

    # ── RIGHT FOREARM + fist + WAR HAMMER ──
    parts = []
    parts.append(add_cube("ArmRL", (0.23, -0.02, z(0.34)),
                          (0.11, 0.11, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Bracer armor
    parts.append(add_cube("BracerR", (0.23, -0.04, z(0.36)),
                          (0.13, 0.08, 0.10), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Fist (gripping weapon)
    parts.append(add_cube("FistR", (0.24, -0.03, z(0.26)),
                          (0.10, 0.10, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # ── War Hammer (horizontal, extending forward like BasicOrc club) ──
    # Handle — rotated 90° X so long axis is along -Y (forward)
    parts.append(add_cylinder("HammerShaft", (0.24, -0.22, z(0.26)),
                              (0.05, 0.05, 0.40), MAT_WOOD,
                              rotation=(math.radians(90), 0, 0), vertices=6))
    bevel_object(parts[-1], 0.005)
    # Handle grip wrap (near fist)
    parts.append(add_cylinder("HammerGrip", (0.24, -0.08, z(0.26)),
                              (0.06, 0.06, 0.08), MAT_CLOTH,
                              rotation=(math.radians(90), 0, 0), vertices=6))
    # Hammer head — massive iron block at far end
    parts.append(add_cube("HammerHead", (0.24, -0.40, z(0.26)),
                          (0.20, 0.12, 0.16), MAT_ARMOR,
                          rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.02)
    # Hammer head iron bands
    parts.append(add_cube("HammerBand1", (0.24, -0.35, z(0.26)),
                          (0.22, 0.02, 0.17), MAT_ARMOR_DK,
                          rotation=(math.radians(90), 0, 0)))
    parts.append(add_cube("HammerBand2", (0.24, -0.45, z(0.26)),
                          (0.22, 0.02, 0.17), MAT_ARMOR_DK,
                          rotation=(math.radians(90), 0, 0)))
    # Hammer spike at far end, pointing forward
    parts.append(add_wedge("HammerSpike", (0.24, -0.49, z(0.26)),
                           (0.04, 0.04, 0.06), MAT_ARMOR,
                           rotation=(math.radians(90), 0, 0)))

    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── LEFT UPPER LEG + armor ──
    parts = []
    parts.append(add_cube("LegLU", (-0.10, 0, z(0.18)),
                          (0.13, 0.13, 0.16), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Thigh armor plate
    parts.append(add_cube("ThighArmorL", (-0.10, -0.06, z(0.18)),
                          (0.14, 0.05, 0.14), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["L_UpperLeg"] = join_objects(parts, "Grp_L_UpperLeg")

    # ── LEFT LOWER LEG + armored boot ──
    parts = []
    parts.append(add_cube("LegLL", (-0.10, 0, z(0.06)),
                          (0.11, 0.12, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Shin guard
    parts.append(add_cube("ShinGuardL", (-0.10, -0.05, z(0.06)),
                          (0.12, 0.05, 0.12), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Heavy boot
    parts.append(add_cube("BootL", (-0.10, -0.03, z(-0.03)),
                          (0.14, 0.18, 0.07), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG + armor ──
    parts = []
    parts.append(add_cube("LegRU", (0.10, 0, z(0.18)),
                          (0.13, 0.13, 0.16), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Thigh armor plate
    parts.append(add_cube("ThighArmorR", (0.10, -0.06, z(0.18)),
                          (0.14, 0.05, 0.14), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    for p in parts:
        apply_modifiers(p)
    groups["R_UpperLeg"] = join_objects(parts, "Grp_R_UpperLeg")

    # ── RIGHT LOWER LEG + armored boot ──
    parts = []
    parts.append(add_cube("LegRL", (0.10, 0, z(0.06)),
                          (0.11, 0.12, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Shin guard
    parts.append(add_cube("ShinGuardR", (0.10, -0.05, z(0.06)),
                          (0.12, 0.05, 0.12), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.01)
    # Heavy boot
    parts.append(add_cube("BootR", (0.10, -0.03, z(-0.03)),
                          (0.14, 0.18, 0.07), MAT_ARMOR_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_LowerLeg"] = join_objects(parts, "Grp_R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def create_armature():
    """Build skeleton for the Orc War Boss."""
    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "WarBossArmature"
    arm = arm_obj.data
    arm.name = "WarBossRig"

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
    add_bone("Root", (0, 0, z+0.26), (0, 0, z+0.30))

    # Spine (torso)
    add_bone("Spine", (0, 0, z+0.26), (0, 0, z+0.50), "Root", connect=True)

    # Head
    add_bone("Head", (0, 0, z+0.50), (0, 0, z+0.68), "Spine", connect=True)

    # Left arm
    add_bone("L_UpperArm", (-0.16, 0, z+0.48), (-0.22, 0, z+0.44), "Spine")
    add_bone("L_ForeArm",  (-0.22, 0, z+0.44), (-0.24, 0, z+0.26), "L_UpperArm", connect=True)

    # Right arm
    add_bone("R_UpperArm", (0.16, 0, z+0.48), (0.22, 0, z+0.44), "Spine")
    add_bone("R_ForeArm",  (0.22, 0, z+0.44), (0.24, 0, z+0.26), "R_UpperArm", connect=True)

    # Left leg
    add_bone("L_UpperLeg", (-0.10, 0, z+0.26), (-0.10, 0, z+0.12), "Root")
    add_bone("L_LowerLeg", (-0.10, 0, z+0.12), (-0.10, 0, z-0.04), "L_UpperLeg", connect=True)

    # Right leg
    add_bone("R_UpperLeg", (0.10, 0, z+0.26), (0.10, 0, z+0.12), "Root")
    add_bone("R_LowerLeg", (0.10, 0, z+0.12), (0.10, 0, z-0.04), "R_UpperLeg", connect=True)

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
    """Heavy lumbering walk — based on BasicOrc/Troll template but with
    reduced swing for heavier, more ponderous gait."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 22   # reduced leg swing (BasicOrc=30) for heavier feel
    arm_sw = 12  # reduced arm swing (BasicOrc=20) — carrying weapon
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
    print("  Walk cycle created (frames 1-25, heavy stomp loop)")
    return action


def create_attack_anim(arm_obj):
    """Overhead hammer slam — based on BasicOrc/Troll overhead club smash template."""
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: rest
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # Frame 5: wind up — raise weapon arm up beside head
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  0, 0, -70)    # raise arm up beside head
    set_bone_rot(pb["R_ForeArm"],  -30, 0, 0)     # bend forearm back behind head
    set_bone_rot(pb["Spine"],        0, 0, -5)     # slight lean back
    set_bone_rot(pb["Head"],         0, 0, 0)
    key_all_bones(arm_obj, 5)

    # Frame 8: peak of wind-up
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  0, 0, -85)    # arm fully raised
    set_bone_rot(pb["R_ForeArm"],  -40, 0, 0)     # forearm bent back
    set_bone_rot(pb["Spine"],       -5, 0, -8)     # lean back into swing
    set_bone_rot(pb["Head"],         5, 0, 0)
    key_all_bones(arm_obj, 8)

    # Frame 11: slam down — arm comes down past horizontal
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  15, 0, 30)    # arm swung down and forward
    set_bone_rot(pb["R_ForeArm"],   20, 0, 0)     # forearm extends
    set_bone_rot(pb["Spine"],        8, 0, 5)      # lunge forward
    set_bone_rot(pb["Head"],        -5, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.02, -0.03)     # crouch into swing
    key_all_bones(arm_obj, 11)

    # Frame 14: impact hold
    reset_pose(arm_obj)
    set_bone_rot(pb["R_UpperArm"],  10, 0, 25)    # arm low, impact position
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
    print("  Attack animation created (frames 1-20, overhead slam)")
    return action


def create_die_anim(arm_obj):
    """Stagger and topple backward — based on BasicOrc/Troll die template.
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

    # Frame 12: recoil backward — legs match spine tilt
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

    # Frame 20: falling — whole body rigid, legs follow spine
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

    # Frame 30: on the ground — values captured from manual pose in Blender
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       -94.0,   1.2,    4.9)
    set_bone_rot(pb["Head"],          1.3,  11.8,  -35.3)
    set_bone_rot(pb["L_UpperArm"],   21.6,  29.1,    8.2)
    set_bone_rot(pb["L_ForeArm"],     7.6, -17.5,    7.2)
    set_bone_rot(pb["R_UpperArm"],   21.2, -25.5,    5.7)
    set_bone_rot(pb["R_ForeArm"],     6.4, -45.5,  -24.6)
    set_bone_rot(pb["L_UpperLeg"],  -80.0,  21.6,    0.0)
    set_bone_rot(pb["R_UpperLeg"],  -88.6, -37.8,    0.0)
    set_bone_loc(pb["Root"], 0, -0.35, 0.30)
    key_all_bones(arm_obj, 30)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Die animation created (frames 1-30, topple backward)")
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
    bpy.ops.object.camera_add(location=(1.5, -2.5, 0.6),
                               rotation=(math.radians(78), 0, math.radians(25)))
    bpy.context.active_object.name = "WarBossCamera"
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
    print("  Orc War Boss — rigged & animated!")
    print("  Massive armored orc warlord with war hammer")
    print("  Actions: Walk (1-25 stomp), Attack (1-24 slam), Die (1-30 collapse)")
    print("")
    print("  Die action is ACTIVE at frame 30.")
    print("  To export for Unity, run export_orc_war_boss.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
