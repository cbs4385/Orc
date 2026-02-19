"""
Blender 4.x Python script — Generate a rigged & animated low-poly "Bow Orc"
Same orc body as BasicOrc but with a bow instead of a club, quiver on back,
and a bow-draw attack animation instead of melee swing.

Usage:
  1. Open Blender 4.2
  2. Switch to the Scripting workspace
  3. Open this file and click "Run Script"
  4. The orc appears at the origin with 3 animation clips
  5. Export as FBX → Assets/Models/BowOrc.fbx  (for Unity)
     - Scale: 1.0, Forward: -Z Forward, Up: Y Up
     - Check "Apply Transform"
     - Under Armature: check "Add Leaf Bones" OFF
     - Under Animation: check "All Actions"

Animations:  Walk (24 fr loop), Attack (20 fr bow draw), Die (30 fr)
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
    # Force-remove ALL orphan data (including fake-user actions)
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.armatures:
        if block.users == 0:
            bpy.data.armatures.remove(block)
    # Remove all actions to prevent .001 suffixes on re-run
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
MAT_LEATHER = MAT_METAL = MAT_WOOD = MAT_TEETH = None
MAT_STRING = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_MOUTH, MAT_EYES
    global MAT_LEATHER, MAT_METAL, MAT_WOOD, MAT_TEETH
    global MAT_STRING
    MAT_SKIN    = make_material("OrcSkin",     (0.45, 0.55, 0.15, 1.0))
    MAT_SKIN_DK = make_material("OrcSkinDark", (0.30, 0.40, 0.10, 1.0))
    MAT_MOUTH   = make_material("OrcMouth",    (0.20, 0.08, 0.05, 1.0))
    MAT_EYES    = make_material("OrcEyes",     (1.0,  0.3,  0.05, 1.0), emission=3.0)
    MAT_LEATHER = make_material("OrcLeather",  (0.35, 0.22, 0.10, 1.0))
    MAT_METAL   = make_material("OrcMetal",    (0.40, 0.40, 0.42, 1.0), roughness=0.5)
    MAT_WOOD    = make_material("OrcWood",     (0.35, 0.22, 0.08, 1.0))
    MAT_TEETH   = make_material("OrcTeeth",    (0.90, 0.88, 0.70, 1.0))
    MAT_STRING  = make_material("OrcString",   (0.80, 0.75, 0.60, 1.0))


# ──────────────────────────────────────────────
#  Body-part groups (joined per bone)
# ──────────────────────────────────────────────

# Z-coordinates reference (origin at feet = 0):
#   Feet bottom:   0.00  (after offset)
#   Feet center:   0.03
#   Lower leg:     0.09
#   Upper leg:     0.23
#   Belt:          0.33
#   Torso center:  0.49
#   Torso top:     0.64
#   Head center:   0.78
#   Head top:      0.92

# We shift everything up by +0.11 so feet bottom ≈ 0
Z_OFF = 0.11  # raise all parts so feet touch ground

def build_body_parts():
    """Create all mesh parts, grouped by bone assignment.
    Returns dict: bone_name → single joined mesh object."""

    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (torso + belly + belt + loincloth + strap + quiver) ──
    parts = []
    parts.append(add_cube("Torso",     (0, 0, z(0.38)),        (0.38, 0.26, 0.30), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("Belly",     (0, -0.12, z(0.32)),    (0.32, 0.10, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    parts.append(add_cube("Belt",      (0, 0, z(0.22)),        (0.40, 0.28, 0.06), MAT_LEATHER))
    bevel_object(parts[-1], 0.01)
    parts.append(add_cube("Loincloth", (0, -0.10, z(0.14)),    (0.18, 0.04, 0.12), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("Strap",     (0.06, -0.02, z(0.40)), (0.08, 0.04, 0.30), MAT_LEATHER,
                           rotation=(0, 0, math.radians(25))))
    bevel_object(parts[-1], 0.01)
    # Quiver on back (diagonal strap across torso)
    parts.append(add_cube("QuiverStrap", (-0.06, 0.02, z(0.40)), (0.06, 0.04, 0.30), MAT_LEATHER,
                           rotation=(0, 0, math.radians(-20))))
    bevel_object(parts[-1], 0.01)
    # Quiver body — cylindrical container on upper back
    parts.append(add_cylinder("Quiver", (0.08, 0.16, z(0.42)), (0.06, 0.06, 0.22), MAT_LEATHER,
                               rotation=(math.radians(10), 0, 0)))
    bevel_object(parts[-1], 0.01)
    # Arrow tips poking out of quiver
    parts.append(add_wedge("QuiverArrow1", (0.06, 0.14, z(0.56)), (0.015, 0.015, 0.04), MAT_METAL,
                            rotation=(math.radians(10), 0, 0)))
    parts.append(add_wedge("QuiverArrow2", (0.10, 0.16, z(0.55)), (0.015, 0.015, 0.04), MAT_METAL,
                            rotation=(math.radians(8), 0, math.radians(5))))
    parts.append(add_wedge("QuiverArrow3", (0.08, 0.18, z(0.57)), (0.015, 0.015, 0.04), MAT_METAL,
                            rotation=(math.radians(12), 0, math.radians(-3))))
    # Arrow shafts visible above quiver
    parts.append(add_cube("QuiverShaft1", (0.06, 0.14, z(0.52)), (0.01, 0.01, 0.06), MAT_WOOD,
                           rotation=(math.radians(10), 0, 0)))
    parts.append(add_cube("QuiverShaft2", (0.10, 0.16, z(0.51)), (0.01, 0.01, 0.06), MAT_WOOD,
                           rotation=(math.radians(8), 0, math.radians(5))))
    parts.append(add_cube("QuiverShaft3", (0.08, 0.18, z(0.53)), (0.01, 0.01, 0.06), MAT_WOOD,
                           rotation=(math.radians(12), 0, math.radians(-3))))
    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (head + brow + eyes + mouth + fangs + ears) ──
    parts = []
    parts.append(add_cube("Head",  (0, 0, z(0.67)),           (0.34, 0.30, 0.28), MAT_SKIN))
    bevel_object(parts[-1], 0.03)
    parts.append(add_cube("Brow",  (0, -0.14, z(0.73)),       (0.30, 0.06, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("EyeL",  (-0.09, -0.15, z(0.69)),   (0.08, 0.04, 0.05), MAT_EYES))
    parts.append(add_cube("EyeR",  ( 0.09, -0.15, z(0.69)),   (0.08, 0.04, 0.05), MAT_EYES))
    parts.append(add_cube("Mouth", (0, -0.15, z(0.59)),       (0.18, 0.04, 0.06), MAT_MOUTH))
    parts.append(add_wedge("FangL", (-0.06, -0.17, z(0.63)),  (0.04, 0.03, 0.06), MAT_TEETH))
    parts.append(add_wedge("FangR", ( 0.06, -0.17, z(0.63)),  (0.04, 0.03, 0.06), MAT_TEETH))
    parts.append(add_wedge("EarL",  (-0.22, 0, z(0.71)),      (0.06, 0.10, 0.12), MAT_SKIN_DK,
                            rotation=(0, 0, math.radians(-30))))
    parts.append(add_wedge("EarR",  ( 0.22, 0, z(0.71)),      (0.06, 0.10, 0.12), MAT_SKIN_DK,
                            rotation=(0, 0, math.radians(30))))
    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM ──
    p = add_cube("ArmLUpper", (-0.28, 0, z(0.48)), (0.14, 0.14, 0.20), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["L_UpperArm"] = p

    # ── LEFT FOREARM + HAND + BOW ──
    parts = []
    parts.append(add_cube("ArmLLower", (-0.30, -0.04, z(0.30)), (0.13, 0.13, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandL",     (-0.30, -0.06, z(0.20)), (0.10, 0.10, 0.08), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Bow — held in left hand, HORIZONTAL along -Y (forward)
    # Orc height ≈ 0.92. Bow length = 3/4 × 0.92 ≈ 0.69.
    # Stepped curve: grip z(0.23) → limbs z(0.20) → tips+string z(0.17)
    # "D" shape from side view. Curve depth = 0.06.
    #
    # Y layout (all rotated 90°X, Z-scale → Y-extent):
    #   farTip[-0.425,-0.305]—farLimb[-0.305,-0.125]—grip[-0.125,-0.035]
    #   —nearLimb[-0.035,0.145]—nearTip[0.145,0.265]
    #   string[-0.425 ————————————————————————— 0.265]
    #
    # Grip (center, where hand holds) — highest point of curve
    parts.append(add_cube("BowGrip", (-0.30, -0.08, z(0.23)), (0.04, 0.04, 0.09), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Far limb (extends forward toward target)
    parts.append(add_cube("BowFarLimb", (-0.30, -0.215, z(0.20)), (0.035, 0.035, 0.18), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Near limb (extends backward toward archer)
    parts.append(add_cube("BowNearLimb", (-0.30, 0.055, z(0.20)), (0.035, 0.035, 0.18), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Far tip (where string attaches)
    parts.append(add_cube("BowFarTip", (-0.30, -0.365, z(0.17)), (0.03, 0.03, 0.12), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Near tip (where string attaches)
    parts.append(add_cube("BowNearTip", (-0.30, 0.205, z(0.17)), (0.03, 0.03, 0.12), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    bevel_object(parts[-1], 0.005)
    # Bowstring — straight line at tip Z level, connecting both tips
    parts.append(add_cube("BowString", (-0.30, -0.08, z(0.17)), (0.008, 0.008, 0.69), MAT_STRING,
                           rotation=(math.radians(90), 0, 0)))
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── RIGHT UPPER ARM ──
    p = add_cube("ArmRUpper", (0.28, 0, z(0.48)), (0.14, 0.14, 0.20), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["R_UpperArm"] = p

    # ── RIGHT FOREARM + HAND + ARROW (nocked) ──
    parts = []
    parts.append(add_cube("ArmRLower", (0.30, -0.04, z(0.30)), (0.13, 0.13, 0.18), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandR",     (0.30, -0.06, z(0.20)), (0.10, 0.10, 0.08), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Arrow held in right hand, pointing forward along -Y (same orientation as club)
    # HandR is at (0.30, -0.06, z(0.20)). Arrow extends forward from hand.
    parts.append(add_cube("ArrowShaft", (0.30, -0.22, z(0.20)), (0.012, 0.012, 0.30), MAT_WOOD,
                           rotation=(math.radians(90), 0, 0)))
    # Arrowhead — small metal tip at the front end of the shaft
    parts.append(add_wedge("ArrowHead", (0.30, -0.38, z(0.20)), (0.025, 0.025, 0.04), MAT_METAL,
                            rotation=(math.radians(90), 0, 0)))
    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── LEFT UPPER LEG ──
    p = add_cube("LegLUpper", (-0.12, 0, z(0.12)), (0.14, 0.16, 0.18), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["L_UpperLeg"] = p

    # ── LEFT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegLLower", (-0.12, 0, z(-0.02)),    (0.12, 0.14, 0.14), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootL",     (-0.12, -0.04, z(-0.08)), (0.12, 0.18, 0.06), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG ──
    p = add_cube("LegRUpper", (0.12, 0, z(0.12)), (0.14, 0.16, 0.18), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegRLower", (0.12, 0, z(-0.02)),    (0.12, 0.14, 0.14), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootR",     (0.12, -0.04, z(-0.08)), (0.12, 0.18, 0.06), MAT_LEATHER))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_LowerLeg"] = join_objects(parts, "Grp_R_LowerLeg")

    return groups


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def create_armature():
    """Build a simple skeleton. Returns the armature object."""

    z = Z_OFF  # ground offset

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "OrcArmature"
    arm = arm_obj.data
    arm.name = "OrcRig"

    # Remove default bone
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

    # Root → Hips
    add_bone("Root",       (0, 0, z+0.22), (0, 0, z+0.26))
    # Spine (torso)
    add_bone("Spine",      (0, 0, z+0.22), (0, 0, z+0.53), "Root", connect=True)
    # Head
    add_bone("Head",       (0, 0, z+0.53), (0, 0, z+0.81), "Spine", connect=True)

    # ── Left arm chain ──
    add_bone("L_UpperArm", (-0.19, 0, z+0.50), (-0.28, 0, z+0.48), "Spine")
    add_bone("L_ForeArm",  (-0.28, 0, z+0.48), (-0.30, 0, z+0.24), "L_UpperArm", connect=True)

    # ── Right arm chain ──
    add_bone("R_UpperArm", (0.19, 0, z+0.50),  (0.28, 0, z+0.48),  "Spine")
    add_bone("R_ForeArm",  (0.28, 0, z+0.48),  (0.30, 0, z+0.24),  "R_UpperArm", connect=True)

    # ── Left leg chain ──
    add_bone("L_UpperLeg", (-0.12, 0, z+0.22), (-0.12, 0, z+0.05), "Root")
    add_bone("L_LowerLeg", (-0.12, 0, z+0.05), (-0.12, 0, z-0.08), "L_UpperLeg", connect=True)

    # ── Right leg chain ──
    add_bone("R_UpperLeg", (0.12, 0, z+0.22),  (0.12, 0, z+0.05),  "Root")
    add_bone("R_LowerLeg", (0.12, 0, z+0.05),  (0.12, 0, z-0.08),  "R_UpperLeg", connect=True)

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
    Identical to BasicOrc walk cycle."""
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
    print("  ✓ Walk cycle created (frames 1–25, loop)")
    return action


def create_attack_anim(arm_obj):
    """Bow draw and release — 24 frames.

    The ENTIRE BODY rotates via the Root bone (legs rotate too).

    Sequence:
      Frame  1: Rest, facing -Y
      Frame  3: Body turning (Root Y=45°), arms start positioning
      Frame  6: Full turn (Root Y=90°), L arm T-pose with bow, R hand at bowstring
      Frame  9: Mid-draw — R arm pulling string back
      Frame 12: Full draw — R hand at cheek, arrow at midline
      Frame 14: Release — R arm snaps forward
      Frame 18: Body returns (Root Y→0), L arm lowers, R hand reaches to quiver
      Frame 21: Nearly at rest
      Frame 24: Rest

    Bone rotation reference (Euler XYZ):
      Root (vertical, bone-local Y = world Z): Y=yaw/turn (+Y = CCW from above)
      L_UpperArm (points -X): Z=raise (+raise)
      L_ForeArm (hangs -Z): Z=extend outward in frontal plane (+Z for T-pose ~75°), X=bend fwd/back
      R_UpperArm (points +X): X=fwd swing, Z=raise (-Z raises)
      R_ForeArm (hangs -Z): X=extend/bend
      Head (vertical, bone-local Y = world Z): Y=yaw (needs -90° to counter Root +90°)
    """
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # ── Frame 1: rest — facing -Y, arms at sides ──
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 1)

    # ── Frame 3: body turning halfway, arms start positioning ──
    # R_UpperArm X is MIRRORED: negative X = forward swing (toward bow/front)
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 45, 0)     # entire body turns 45° toward +X
    set_bone_rot(pb["Head"],         0, -45, 0)    # counter-rotate to keep looking -Y
    set_bone_rot(pb["L_UpperArm"],   0, 0, 30)     # bow arm raising
    set_bone_rot(pb["L_ForeArm"],    0, 45, 35)    # Y=roll hand 45° (halfway), Z=extend outward
    set_bone_rot(pb["R_UpperArm"], -25, 0, -10)    # R arm reaching forward toward bow
    set_bone_rot(pb["R_ForeArm"],  -15, 45, 0)     # Y=roll 45° (arrow reorienting)
    key_all_bones(arm_obj, 3)

    # ── Frame 6: full turn, L arm T-pose, R hand at bowstring center ──
    # Root Y=90° → entire body faces +X (including legs).
    # R_UpperArm X=-50° swings R arm forward (toward orc's front/bowstring).
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 90, 0)     # entire body faces +X
    set_bone_rot(pb["Head"],         0, -90, 0)    # counter-rotate to look -Y
    set_bone_rot(pb["L_UpperArm"],   0, 0, 45)     # arm raised for bow stance
    set_bone_rot(pb["L_ForeArm"],    0, 90, 75)    # Y=90° roll bow to front, Z=75° T-pose extend
    set_bone_rot(pb["R_UpperArm"], -50, 0, -30)    # forward toward bowstring (neg X = forward for R arm)
    set_bone_rot(pb["R_ForeArm"],  -20, 90, 0)     # Y=90° roll → arrow parallel to Y world
    key_all_bones(arm_obj, 6)

    # ── Frame 9: mid-draw — R arm pulling string back ──
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 90, 0)     # body still facing +X
    set_bone_rot(pb["Head"],         0, -90, 0)    # still looking at target
    set_bone_rot(pb["L_UpperArm"],   0, 0, 45)     # bow arm steady
    set_bone_rot(pb["L_ForeArm"],    0, 90, 75)    # T-pose held, bow facing front
    set_bone_rot(pb["R_UpperArm"],   5, 0, -50)    # pulling back (pos X = backward for R arm)
    set_bone_rot(pb["R_ForeArm"],  -35, 90, 0)     # bending + arrow stays along Y
    key_all_bones(arm_obj, 9)

    # ── Frame 12: FULL DRAW — string at midline, hand anchored at cheek ──
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],        -3, 90, 0)     # slight lean back under draw tension
    set_bone_rot(pb["Head"],         3, -90, 0)    # aiming, chin slightly up
    set_bone_rot(pb["L_UpperArm"],   0, 0, 45)     # bow arm steady
    set_bone_rot(pb["L_ForeArm"],    0, 90, 75)    # T-pose held, bow facing front
    set_bone_rot(pb["R_UpperArm"],  25, 0, -65)    # drawn way back (pos X = backward)
    set_bone_rot(pb["R_ForeArm"],  -75, 90, 0)     # sharply bent, arrow along Y
    key_all_bones(arm_obj, 12)

    # ── Frame 14: release — R arm snaps forward, arrow flies ──
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 90, 0)     # still facing +X
    set_bone_rot(pb["Head"],         0, -90, 0)    # watching arrow fly
    set_bone_rot(pb["L_UpperArm"],   0, 0, 43)     # bow arm holds
    set_bone_rot(pb["L_ForeArm"],    0, 90, 73)    # still extended, bow facing front
    set_bone_rot(pb["R_UpperArm"],  -5, 0, -25)    # R arm snaps forward from release
    set_bone_rot(pb["R_ForeArm"],   -5, 45, 0)     # forearm extends, roll easing back
    key_all_bones(arm_obj, 14)

    # ── Frame 18: body returns, R hand reaches to shoulder/quiver ──
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 15, 0)     # mostly returned to facing -Y
    set_bone_rot(pb["Head"],         0, -12, 0)    # counter-rotate
    set_bone_rot(pb["L_UpperArm"],   0, 0, 10)     # bow arm lowering
    set_bone_rot(pb["L_ForeArm"],    0, 15, 10)    # relaxing, roll easing back
    set_bone_rot(pb["R_UpperArm"],  25, 0, -50)    # reaching back to shoulder (pos X = backward)
    set_bone_rot(pb["R_ForeArm"],  -55, 0, 0)      # bent, hand at quiver, roll back to 0
    key_all_bones(arm_obj, 18)

    # ── Frame 21: nearly at rest, R arm coming back down ──
    reset_pose(arm_obj)
    set_bone_rot(pb["Root"],         0, 3, 0)      # almost back
    set_bone_rot(pb["R_UpperArm"],   8, 0, -15)    # R arm coming down (slight backward)
    set_bone_rot(pb["R_ForeArm"],  -10, 0, 0)
    key_all_bones(arm_obj, 21)

    # ── Frame 24: rest — back to idle ──
    reset_pose(arm_obj)
    key_all_bones(arm_obj, 24)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  ✓ Attack animation created (frames 1–24, bow draw)")
    return action


def create_die_anim(arm_obj):
    """Stagger and topple backward — 30 frames.
    Identical to BasicOrc die animation."""
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
    # Legs tilt backward with spine (negative X = backward for these bones)
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
    # Legs match spine tilt for rigid timber fall
    set_bone_rot(pb["L_UpperLeg"], -50, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -50, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.20, 0.15)
    key_all_bones(arm_obj, 20)

    # Frame 30: on the ground — stiff body, legs in line with torso
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    -80, 0, 5)
    set_bone_rot(pb["Head"],     -40, 0, -15)
    set_bone_rot(pb["R_UpperArm"], -30, 0, 60)
    set_bone_rot(pb["R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -30, 0, -60)
    set_bone_rot(pb["L_ForeArm"],  -20, 0, 0)
    # Legs match spine — full rigid fall
    set_bone_rot(pb["L_UpperLeg"], -80, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -80, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.35, 0.30)
    key_all_bones(arm_obj, 30)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  ✓ Die animation created (frames 1–30)")
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

    # Push each action onto its own NLA track so all are visible
    # and export correctly via FBX "All Actions"
    anim_data = arm_obj.animation_data

    # Clear the active action first
    anim_data.action = None

    # Create NLA tracks for each clip — all muted by default
    # to prevent blending. Solo a track (star icon) to preview it.
    for action in [walk_action, attack_action, die_action]:
        track = anim_data.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, int(action.frame_range[0]), action)
        strip.name = action.name
        track.mute = True  # prevent all tracks from blending together

    bpy.ops.object.mode_set(mode='OBJECT')

    # Lighting for preview
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera
    bpy.ops.object.camera_add(location=(4, -6, 0.6),
                               rotation=(math.radians(80), 0, math.radians(45)))
    bpy.context.active_object.name = "OrcCamera"
    bpy.context.scene.camera = bpy.context.active_object

    # Scene settings
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 30
    bpy.context.scene.render.fps = 24

    # Select armature
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    print("=" * 50)
    print("  Bow Orc — rigged & animated!")
    print("  Actions: Walk (1-25 loop), Attack (1-24 bow draw), Die (1-30)")
    print("")
    print("  To export for Unity:")
    print("    File → Export → FBX (.fbx)")
    print("    - Scale: 1.0, Forward: -Z, Up: Y")
    print("    - Apply Transform: checked")
    print("    - Armature → Add Leaf Bones: OFF")
    print("    - Bake Animation: checked")
    print("    - All Actions: checked")
    print("=" * 50)


if __name__ == "__main__":
    main()
