"""
Blender 4.x Python script — Generate a rigged & animated low-poly "Suicide Goblin"
A small, fast kamikaze enemy carrying a bomb barrel strapped to its chest.
Scrawny body, oversized head with crazed eyes, frantic animations.

Usage:
  1. Open Blender 4.2
  2. Switch to the Scripting workspace
  3. Open this file and click "Run Script"
  4. The goblin appears at the origin with 3 animation clips
  5. Run export_suicide_goblin.py to export as FBX for Unity

Animations:  Walk (24 fr frantic run), Attack (20 fr detonation), Die (30 fr collapse)
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


def add_sphere(name, location, scale, material, segments=8, rings=6):
    """Add a UV sphere, apply scale, assign material."""
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
MAT_CLOTH = MAT_WOOD = MAT_TEETH = MAT_FUSE = None
MAT_METAL = MAT_BOMB = None

def create_materials():
    global MAT_SKIN, MAT_SKIN_DK, MAT_MOUTH, MAT_EYES
    global MAT_CLOTH, MAT_WOOD, MAT_TEETH, MAT_FUSE
    global MAT_METAL, MAT_BOMB
    # Yellowish-green skin — distinct from orc (bright green) and troll (olive)
    MAT_SKIN    = make_material("GoblinSkin",     (0.53, 0.67, 0.13, 1.0))
    MAT_SKIN_DK = make_material("GoblinSkinDark", (0.38, 0.48, 0.08, 1.0))
    MAT_MOUTH   = make_material("GoblinMouth",    (0.25, 0.08, 0.05, 1.0))
    MAT_EYES    = make_material("GoblinEyes",     (1.0,  0.9,  0.1,  1.0), emission=5.0)  # bright yellow crazed eyes
    MAT_CLOTH   = make_material("GoblinCloth",    (0.25, 0.15, 0.08, 1.0))  # ragged brown cloth
    MAT_WOOD    = make_material("GoblinWood",     (0.35, 0.20, 0.08, 1.0))
    MAT_TEETH   = make_material("GoblinTeeth",    (0.90, 0.85, 0.60, 1.0))
    MAT_FUSE    = make_material("GoblinFuse",     (1.0,  0.5,  0.0,  1.0), emission=4.0)  # glowing orange fuse
    MAT_METAL   = make_material("GoblinMetal",    (0.30, 0.28, 0.26, 1.0), roughness=0.6)
    MAT_BOMB    = make_material("GoblinBomb",     (0.20, 0.12, 0.06, 1.0))  # dark brown barrel


# ──────────────────────────────────────────────
#  Body-part groups (joined per bone)
# ──────────────────────────────────────────────

# Goblin is scrawny with an oversized head and a bomb barrel on the chest.
# Unity applies 0.7x bodyScale, making it noticeably smaller than orcs.
# We build it at roughly 85% orc scale in Blender for additional size difference.

Z_OFF = 0.09  # raise all parts so feet touch ground

def build_body_parts():
    """Create all mesh parts, grouped by bone assignment.
    Returns dict: bone_name -> single joined mesh object."""

    groups = {}

    def z(val):
        return val + Z_OFF

    # ── SPINE (scrawny torso + bomb barrel + rope bindings) ──
    parts = []
    # Narrow chest — much thinner than orc
    parts.append(add_cube("Torso", (0, 0, z(0.36)),
                          (0.26, 0.18, 0.22), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    # Ragged cloth wrap around waist
    parts.append(add_cube("WaistCloth", (0, 0, z(0.22)),
                          (0.28, 0.20, 0.06), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)
    # Ragged cloth strip hanging
    parts.append(add_cube("Loincloth", (0, -0.08, z(0.14)),
                          (0.14, 0.03, 0.12), MAT_CLOTH))
    bevel_object(parts[-1], 0.01)

    # ── BOMB BARREL — worn as a backpack, long axis vertical (Z) ──
    # Barrel roughly torso-sized, sitting on the goblin's back
    parts.append(add_cylinder("BombBarrel", (0, 0.14, z(0.34)),
                              (0.12, 0.12, 0.24), MAT_BOMB,
                              vertices=8))
    bevel_object(parts[-1], 0.01)
    # Metal bands around barrel (horizontal rings)
    parts.append(add_cylinder("BarrelBand1", (0, 0.14, z(0.28)),
                              (0.13, 0.13, 0.02), MAT_METAL,
                              vertices=8))
    parts.append(add_cylinder("BarrelBand2", (0, 0.14, z(0.40)),
                              (0.13, 0.13, 0.02), MAT_METAL,
                              vertices=8))
    # Rope strap over left shoulder
    parts.append(add_cube("RopeL", (-0.06, 0.04, z(0.40)),
                          (0.04, 0.16, 0.20), MAT_CLOTH,
                          rotation=(0, 0, math.radians(15))))
    bevel_object(parts[-1], 0.005)
    # Rope strap over right shoulder
    parts.append(add_cube("RopeR", (0.06, 0.04, z(0.40)),
                          (0.04, 0.16, 0.20), MAT_CLOTH,
                          rotation=(0, 0, math.radians(-15))))
    bevel_object(parts[-1], 0.005)
    # Fuse sticking out the top of the barrel (glowing!)
    parts.append(add_cylinder("Fuse", (0, 0.14, z(0.50)),
                              (0.015, 0.015, 0.10), MAT_FUSE,
                              vertices=6))
    # Fuse spark tip at the top
    parts.append(add_sphere("FuseSpark", (0, 0.14, z(0.56)),
                            (0.03, 0.03, 0.03), MAT_FUSE, segments=6, rings=4))

    for p in parts:
        apply_modifiers(p)
    groups["Spine"] = join_objects(parts, "Grp_Spine")

    # ── HEAD (oversized goblin head + huge eyes + pointy ears + sharp nose + grin) ──
    parts = []
    # Big round-ish head — proportionally larger than orc
    parts.append(add_cube("Head", (0, 0, z(0.58)),
                          (0.30, 0.26, 0.24), MAT_SKIN))
    bevel_object(parts[-1], 0.04)
    # Prominent brow ridge
    parts.append(add_cube("Brow", (0, -0.12, z(0.64)),
                          (0.28, 0.06, 0.05), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    # Big bulging crazed eyes — larger than orc, yellow glow
    parts.append(add_cube("EyeL", (-0.09, -0.13, z(0.60)),
                          (0.08, 0.06, 0.06), MAT_EYES))
    parts.append(add_cube("EyeR", ( 0.09, -0.13, z(0.60)),
                          (0.08, 0.06, 0.06), MAT_EYES))
    # Long pointy nose
    parts.append(add_wedge("Nose", (0, -0.18, z(0.57)),
                           (0.06, 0.08, 0.08), MAT_SKIN_DK,
                           rotation=(math.radians(-90), 0, 0)))
    # Wide toothy grin
    parts.append(add_cube("Mouth", (0, -0.13, z(0.50)),
                          (0.16, 0.04, 0.04), MAT_MOUTH))
    # Jagged teeth along the grin
    parts.append(add_wedge("ToothL1", (-0.05, -0.15, z(0.52)),
                           (0.03, 0.02, 0.04), MAT_TEETH))
    parts.append(add_wedge("ToothR1", ( 0.05, -0.15, z(0.52)),
                           (0.03, 0.02, 0.04), MAT_TEETH))
    parts.append(add_wedge("ToothC",  ( 0.00, -0.15, z(0.52)),
                           (0.03, 0.02, 0.04), MAT_TEETH))
    # Big pointy ears — larger than orc, sticking out
    parts.append(add_wedge("EarL", (-0.22, 0, z(0.60)),
                           (0.06, 0.14, 0.16), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(-40))))
    parts.append(add_wedge("EarR", ( 0.22, 0, z(0.60)),
                           (0.06, 0.14, 0.16), MAT_SKIN_DK,
                           rotation=(0, 0, math.radians(40))))
    for p in parts:
        apply_modifiers(p)
    groups["Head"] = join_objects(parts, "Grp_Head")

    # ── LEFT UPPER ARM — scrawny ──
    p = add_cube("ArmLUpper", (-0.20, 0, z(0.40)),
                 (0.10, 0.10, 0.16), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["L_UpperArm"] = p

    # ── LEFT FOREARM + HAND ──
    parts = []
    parts.append(add_cube("ArmLLower", (-0.22, -0.02, z(0.26)),
                          (0.09, 0.09, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandL", (-0.22, -0.04, z(0.18)),
                          (0.08, 0.08, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_ForeArm"] = join_objects(parts, "Grp_L_ForeArm")

    # ── RIGHT UPPER ARM — scrawny ──
    p = add_cube("ArmRUpper", (0.20, 0, z(0.40)),
                 (0.10, 0.10, 0.16), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["R_UpperArm"] = p

    # ── RIGHT FOREARM + HAND (no weapon — bomb is on chest) ──
    parts = []
    parts.append(add_cube("ArmRLower", (0.22, -0.02, z(0.26)),
                          (0.09, 0.09, 0.14), MAT_SKIN))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("HandR", (0.22, -0.04, z(0.18)),
                          (0.08, 0.08, 0.06), MAT_SKIN_DK))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["R_ForeArm"] = join_objects(parts, "Grp_R_ForeArm")

    # ── LEFT UPPER LEG — thin ──
    p = add_cube("LegLUpper", (-0.08, 0, z(0.10)),
                 (0.10, 0.12, 0.16), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["L_UpperLeg"] = p

    # ── LEFT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegLLower", (-0.08, 0, z(-0.02)),
                          (0.09, 0.10, 0.12), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootL", (-0.08, -0.04, z(-0.06)),
                          (0.10, 0.16, 0.06), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    for p in parts:
        apply_modifiers(p)
    groups["L_LowerLeg"] = join_objects(parts, "Grp_L_LowerLeg")

    # ── RIGHT UPPER LEG — thin ──
    p = add_cube("LegRUpper", (0.08, 0, z(0.10)),
                 (0.10, 0.12, 0.16), MAT_SKIN)
    bevel_object(p, 0.02); apply_modifiers(p)
    groups["R_UpperLeg"] = p

    # ── RIGHT LOWER LEG + FOOT ──
    parts = []
    parts.append(add_cube("LegRLower", (0.08, 0, z(-0.02)),
                          (0.09, 0.10, 0.12), MAT_CLOTH))
    bevel_object(parts[-1], 0.02)
    parts.append(add_cube("FootR", (0.08, -0.04, z(-0.06)),
                          (0.10, 0.16, 0.06), MAT_CLOTH))
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
    but narrower proportions for the goblin's scrawny build."""

    z = Z_OFF

    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "GoblinArmature"
    arm = arm_obj.data
    arm.name = "GoblinRig"

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

    # Root → Hips (lower than orc — goblin is shorter)
    add_bone("Root",       (0, 0, z+0.18), (0, 0, z+0.22))
    # Spine (shorter torso)
    add_bone("Spine",      (0, 0, z+0.18), (0, 0, z+0.44), "Root", connect=True)
    # Head (higher relative to body — oversized goblin head)
    add_bone("Head",       (0, 0, z+0.44), (0, 0, z+0.70), "Spine", connect=True)

    # Left arm — narrow shoulders
    add_bone("L_UpperArm", (-0.13, 0, z+0.42), (-0.20, 0, z+0.40), "Spine")
    add_bone("L_ForeArm",  (-0.20, 0, z+0.40), (-0.22, 0, z+0.20), "L_UpperArm", connect=True)

    # Right arm — narrow shoulders
    add_bone("R_UpperArm", (0.13, 0, z+0.42),  (0.20, 0, z+0.40),  "Spine")
    add_bone("R_ForeArm",  (0.20, 0, z+0.40),  (0.22, 0, z+0.20),  "R_UpperArm", connect=True)

    # Left leg — narrow stance
    add_bone("L_UpperLeg", (-0.08, 0, z+0.18), (-0.08, 0, z+0.04), "Root")
    add_bone("L_LowerLeg", (-0.08, 0, z+0.04), (-0.08, 0, z-0.06), "L_UpperLeg", connect=True)

    # Right leg — narrow stance
    add_bone("R_UpperLeg", (0.08, 0, z+0.18),  (0.08, 0, z+0.04),  "Root")
    add_bone("R_LowerLeg", (0.08, 0, z+0.04),  (0.08, 0, z-0.06),  "R_UpperLeg", connect=True)

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
    """Frantic running cycle — 24 frames at 24fps = 1 second.
    Faster and more exaggerated than orc walk — this goblin is sprinting
    with a bomb strapped to its chest. Hunched forward, arms pumping."""
    action = bpy.data.actions.new("Walk")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones
    swing = 40   # leg swing angle (bigger than orc's 30 — frantic run)
    arm_sw = 35  # arm counter-swing (bigger — arms pumping wildly)
    bob = 0.03   # more bounce in the run

    # Hunched-forward base posture for all frames
    hunch_spine = 12   # spine tilted forward (running posture)
    hunch_head = -8    # head looking up despite hunched body

    # Frame 1: neutral (start of loop) — hunched
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], hunch_spine, 0, 0)
    set_bone_rot(pb["Head"], hunch_head, 0, 0)
    key_all_bones(arm_obj, 1)

    # Frame 7: left leg forward, right leg back
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], hunch_spine, 0, 5)
    set_bone_rot(pb["Head"], hunch_head, 0, 0)
    set_bone_rot(pb["L_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -swing*0.4, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["R_LowerLeg"],  0, 0, 0)
    set_bone_rot(pb["R_UpperArm"],  arm_sw, 0, 0)
    set_bone_rot(pb["R_ForeArm"],  -arm_sw*0.5, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -arm_sw, 0, 0)
    set_bone_rot(pb["L_ForeArm"],   0, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    key_all_bones(arm_obj, 7)

    # Frame 13: neutral (mid loop)
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], hunch_spine, 0, 0)
    set_bone_rot(pb["Head"], hunch_head, 0, 0)
    key_all_bones(arm_obj, 13)

    # Frame 19: right leg forward, left leg back (mirror of frame 7)
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], hunch_spine, 0, -5)
    set_bone_rot(pb["Head"], hunch_head, 0, 0)
    set_bone_rot(pb["R_UpperLeg"],  swing, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -swing*0.4, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -swing, 0, 0)
    set_bone_rot(pb["L_LowerLeg"],  0, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  arm_sw, 0, 0)
    set_bone_rot(pb["L_ForeArm"],  -arm_sw*0.5, 0, 0)
    set_bone_rot(pb["R_UpperArm"], -arm_sw, 0, 0)
    set_bone_rot(pb["R_ForeArm"],   0, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, bob)
    key_all_bones(arm_obj, 19)

    # Frame 25: same as frame 1 for seamless loop
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], hunch_spine, 0, 0)
    set_bone_rot(pb["Head"], hunch_head, 0, 0)
    key_all_bones(arm_obj, 25)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'

    action.use_fake_user = True
    print("  Walk cycle created (frames 1-25, frantic run loop)")
    return action


def create_attack_anim(arm_obj):
    """Detonation animation — goblin hunches over bomb, then spreads arms
    wide as it explodes. 20 frames."""
    action = bpy.data.actions.new("Attack")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: rest (hunched running posture)
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], 12, 0, 0)
    set_bone_rot(pb["Head"], -8, 0, 0)
    key_all_bones(arm_obj, 1)

    # Frame 4: hunch over the bomb — curling inward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       25, 0, 0)    # lean far forward over bomb
    set_bone_rot(pb["Head"],       -15, 0, 0)    # head tucked
    set_bone_rot(pb["R_UpperArm"],  20, 0, -30)  # arms wrapping around bomb
    set_bone_rot(pb["R_ForeArm"],  -40, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  20, 0, 30)
    set_bone_rot(pb["L_ForeArm"],  -40, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, -0.03)        # crouch down
    key_all_bones(arm_obj, 4)

    # Frame 7: maximum curl — about to detonate
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       30, 0, 0)    # maximum hunch
    set_bone_rot(pb["Head"],       -20, 0, 0)    # head down
    set_bone_rot(pb["R_UpperArm"],  30, 0, -40)  # arms tight around bomb
    set_bone_rot(pb["R_ForeArm"],  -50, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  30, 0, 40)
    set_bone_rot(pb["L_ForeArm"],  -50, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, -0.05)        # deep crouch
    key_all_bones(arm_obj, 7)

    # Frame 10: BOOM — arms flung wide, torso snaps upright
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],      -15, 0, 0)    # torso snaps backward
    set_bone_rot(pb["Head"],        20, 0, 0)    # head thrown back
    set_bone_rot(pb["R_UpperArm"],   0, 0, -80)  # arms flung up and out
    set_bone_rot(pb["R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"],   0, 0, 80)   # mirror
    set_bone_rot(pb["L_ForeArm"],  -20, 0, 0)
    set_bone_loc(pb["Root"], 0, 0, 0.04)         # launched upward slightly
    key_all_bones(arm_obj, 10)

    # Frame 14: explosion hold — spread eagle
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],      -10, 0, 0)
    set_bone_rot(pb["Head"],        15, 0, 0)
    set_bone_rot(pb["R_UpperArm"],   0, 0, -90)  # arms fully out
    set_bone_rot(pb["R_ForeArm"],    0, 0, 0)
    set_bone_rot(pb["L_UpperArm"],   0, 0, 90)
    set_bone_rot(pb["L_ForeArm"],    0, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], -20, 0, -15)  # legs spread
    set_bone_rot(pb["R_UpperLeg"], -20, 0, 15)
    set_bone_loc(pb["Root"], 0, 0, 0.02)
    key_all_bones(arm_obj, 14)

    # Frame 20: slump — post-explosion
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       40, 0, 0)    # collapse forward
    set_bone_rot(pb["Head"],       -30, 0, 10)   # head hanging
    set_bone_rot(pb["R_UpperArm"],  15, 0, 20)   # arms limp
    set_bone_rot(pb["R_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["L_UpperArm"],  15, 0, -20)
    set_bone_rot(pb["L_ForeArm"],  -30, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.10, -0.05)    # dropped down
    key_all_bones(arm_obj, 20)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Attack animation created (frames 1-20, detonation)")
    return action


def create_die_anim(arm_obj):
    """Collapse forward — 30 frames. Since the goblin usually dies by
    self-detonation, this is for when it gets killed before reaching
    its target. Quick crumple forward."""
    action = bpy.data.actions.new("Die")
    arm_obj.animation_data.action = action

    pb = arm_obj.pose.bones

    # Frame 1: alive (hunched running posture)
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"], 12, 0, 0)
    set_bone_rot(pb["Head"], -8, 0, 0)
    key_all_bones(arm_obj, 1)

    # Frame 6: hit stagger — stumble forward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    25, 0, 0)
    set_bone_rot(pb["Head"],     15, 0, 5)
    set_bone_rot(pb["R_UpperArm"], 10, 0, 20)
    set_bone_rot(pb["L_UpperArm"], 10, 0, -20)
    set_bone_loc(pb["Root"], 0, -0.02, 0)
    key_all_bones(arm_obj, 6)

    # Frame 12: knees buckling — dropping forward
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    40, 0, 3)
    set_bone_rot(pb["Head"],    -10, 0, -5)
    set_bone_rot(pb["R_UpperArm"], -10, 0, 30)
    set_bone_rot(pb["R_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -10, 0, -30)
    set_bone_rot(pb["L_ForeArm"],  -20, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], 30, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -40, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], 30, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -40, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.10, -0.05)
    key_all_bones(arm_obj, 12)

    # Frame 20: falling face-first
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],    60, 0, 5)
    set_bone_rot(pb["Head"],    -20, 0, -10)
    set_bone_rot(pb["R_UpperArm"], -30, 0, 45)
    set_bone_rot(pb["R_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["L_UpperArm"], -30, 0, -45)
    set_bone_rot(pb["L_ForeArm"],  -30, 0, 0)
    set_bone_rot(pb["L_UpperLeg"], 50, 0, 0)
    set_bone_rot(pb["L_LowerLeg"], -60, 0, 0)
    set_bone_rot(pb["R_UpperLeg"], 50, 0, 0)
    set_bone_rot(pb["R_LowerLeg"], -60, 0, 0)
    set_bone_loc(pb["Root"], 0, -0.20, -0.10)
    key_all_bones(arm_obj, 20)

    # Frame 30: face-down on the ground — crumpled heap
    # Values captured from manual pose in Blender
    reset_pose(arm_obj)
    set_bone_rot(pb["Spine"],       80.0,    0.0,    5.0)
    set_bone_rot(pb["Head"],         2.8,    6.9,  -10.0)
    set_bone_rot(pb["R_UpperArm"],  25.8,  -37.9,  -50.8)
    set_bone_rot(pb["R_ForeArm"],   23.6,   -4.5,  -55.3)
    set_bone_rot(pb["L_UpperArm"],  40.5,   25.2,   34.6)
    set_bone_rot(pb["L_ForeArm"],   37.2,    7.3,   54.8)
    set_bone_rot(pb["L_UpperLeg"],  89.5,  -29.0,   -9.8)
    set_bone_rot(pb["L_LowerLeg"],  -8.9,   70.0,   91.2)
    set_bone_rot(pb["R_UpperLeg"],  98.0,   37.7,   18.0)
    set_bone_rot(pb["R_LowerLeg"], -44.3,  -65.5,  -53.1)
    set_bone_loc(pb["Root"], 0, -0.30, -0.15)
    key_all_bones(arm_obj, 30)

    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'

    action.use_fake_user = True
    print("  Die animation created (frames 1-30, face-down collapse)")
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

    # Leave Die action active so user can preview/tweak
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

    # Camera
    bpy.ops.object.camera_add(location=(3, -5, 0.5),
                               rotation=(math.radians(82), 0, math.radians(35)))
    bpy.context.active_object.name = "GoblinCamera"
    bpy.context.scene.camera = bpy.context.active_object

    # Scene settings
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 30
    bpy.context.scene.render.fps = 24

    # Re-select armature and enter Pose Mode
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    bpy.context.scene.frame_set(30)

    print("=" * 50)
    print("  Suicide Goblin — rigged & animated!")
    print("  Actions: Walk (1-25 frantic run), Attack (1-20 detonation), Die (1-30 collapse)")
    print("")
    print("  Die action is ACTIVE at frame 30 — you can adjust the death pose.")
    print("  Scrub the timeline to preview animations.")
    print("")
    print("  To export for Unity, run export_suicide_goblin.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
