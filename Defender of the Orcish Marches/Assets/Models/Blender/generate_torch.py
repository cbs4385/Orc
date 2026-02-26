"""
Blender 4.x Python script — Generate a medieval wall torch.
Static model (no armature, no animations).

Visual: iron wall bracket at the base, wooden handle, cloth wrapping near
the top, and a multi-layered stylized flame with emissive materials.
Sized to ~0.5 units tall. Origin at the bracket base for wall mounting.

The FlameFlicker.cs runtime script handles light/scale animation in Unity.
"""

import bpy
import math

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


def make_material(name, color, emission=0.0, roughness=0.9, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
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


def add_cylinder(name, location, scale, material, rotation=(0, 0, 0), vertices=12):
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


def add_cone(name, location, scale, material, rotation=(0, 0, 0), vertices=10):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices, radius1=0.5, radius2=0.0, depth=1.0,
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


def add_uv_sphere(name, location, scale, material, segments=12, ring_count=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=ring_count, radius=0.5,
        location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj


def bevel_object(obj, width=0.005, segments=1):
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


# ──────────────────────────────────────────────
#  Materials
# ──────────────────────────────────────────────

MAT_IRON = MAT_WOOD = MAT_CLOTH = MAT_FLAME_OUTER = MAT_FLAME_MID = MAT_FLAME_CORE = None

def create_materials():
    global MAT_IRON, MAT_WOOD, MAT_CLOTH
    global MAT_FLAME_OUTER, MAT_FLAME_MID, MAT_FLAME_CORE

    # Dark iron bracket
    MAT_IRON = make_material("TorchIron", (0.25, 0.22, 0.20, 1.0),
                             roughness=0.5, metallic=0.8)
    # Dark wood handle
    MAT_WOOD = make_material("TorchWood", (0.30, 0.18, 0.07, 1.0),
                             roughness=0.85)
    # Cloth wrapping — off-white / tan
    MAT_CLOTH = make_material("TorchCloth", (0.55, 0.45, 0.30, 1.0),
                              roughness=0.95)

    # Flame materials — layered emissive for depth
    # Outer flame: deep orange with moderate glow
    MAT_FLAME_OUTER = make_material("FlameOuter", (1.0, 0.35, 0.05, 1.0),
                                     emission=4.0, roughness=1.0)
    # Mid flame: bright orange-yellow
    MAT_FLAME_MID = make_material("FlameMid", (1.0, 0.55, 0.1, 1.0),
                                   emission=6.0, roughness=1.0)
    # Core flame: hot yellow-white
    MAT_FLAME_CORE = make_material("FlameCore", (1.0, 0.85, 0.4, 1.0),
                                    emission=10.0, roughness=1.0)


# ──────────────────────────────────────────────
#  Build the torch
# ──────────────────────────────────────────────

def build_torch():
    """Build a medieval wall torch.
    Origin at the bottom of the bracket, ~0.5 units tall.
    Y-forward (projects from wall), Z-up."""
    parts = []

    # ── IRON BRACKET (wall mount) ──
    # Back plate (flat against wall)
    parts.append(add_cube("BracketPlate", (0, 0, 0.015),
                          (0.06, 0.015, 0.06), MAT_IRON))
    bevel_object(parts[-1], 0.003)
    # Bracket arm — angled upward to hold the handle
    parts.append(add_cube("BracketArm", (0, 0.025, 0.04),
                          (0.04, 0.035, 0.015), MAT_IRON))
    bevel_object(parts[-1], 0.002)
    # Bracket ring (holds the torch shaft) — a cylinder ring
    parts.append(add_cylinder("BracketRing", (0, 0.04, 0.065),
                              (0.03, 0.03, 0.015), MAT_IRON, vertices=10))
    bevel_object(parts[-1], 0.002)

    # ── WOODEN HANDLE ──
    handle_base_z = 0.05
    handle_height = 0.28
    handle_center_z = handle_base_z + handle_height / 2
    parts.append(add_cylinder("Handle", (0, 0.04, handle_center_z),
                              (0.018, 0.018, handle_height), MAT_WOOD, vertices=8))

    # ── CLOTH WRAPPING (near the top of handle, where it burns) ──
    cloth_z = handle_base_z + handle_height - 0.04
    # Multiple overlapping cloth bands for visual thickness
    parts.append(add_cylinder("Cloth1", (0, 0.04, cloth_z),
                              (0.025, 0.025, 0.05), MAT_CLOTH, vertices=8))
    parts.append(add_cylinder("Cloth2", (0, 0.04, cloth_z + 0.015),
                              (0.028, 0.028, 0.035), MAT_CLOTH, vertices=8))
    # Slightly tilted wrap for organic look
    parts.append(add_cylinder("Cloth3", (0.003, 0.04, cloth_z - 0.01),
                              (0.023, 0.023, 0.03), MAT_CLOTH,
                              rotation=(math.radians(5), 0, 0), vertices=8))

    # ── FLAME (multi-layered for visual depth) ──
    flame_base_z = handle_base_z + handle_height + 0.02

    # Outer flame — wide, tall cone (deep orange)
    parts.append(add_cone("FlameOuter", (0, 0.04, flame_base_z + 0.06),
                          (0.04, 0.04, 0.14), MAT_FLAME_OUTER, vertices=8))

    # Secondary outer flames — slightly offset cones for organic shape
    parts.append(add_cone("FlameOuterL", (-0.008, 0.035, flame_base_z + 0.05),
                          (0.03, 0.03, 0.11), MAT_FLAME_OUTER,
                          rotation=(math.radians(5), math.radians(8), 0), vertices=7))
    parts.append(add_cone("FlameOuterR", (0.008, 0.045, flame_base_z + 0.05),
                          (0.03, 0.03, 0.11), MAT_FLAME_OUTER,
                          rotation=(math.radians(-4), math.radians(-6), 0), vertices=7))
    parts.append(add_cone("FlameOuterB", (0, 0.05, flame_base_z + 0.045),
                          (0.025, 0.025, 0.09), MAT_FLAME_OUTER,
                          rotation=(math.radians(-8), 0, 0), vertices=7))

    # Mid flame — narrower, bright orange-yellow
    parts.append(add_cone("FlameMid", (0, 0.04, flame_base_z + 0.055),
                          (0.025, 0.025, 0.11), MAT_FLAME_MID, vertices=7))
    parts.append(add_cone("FlameMidL", (-0.005, 0.038, flame_base_z + 0.05),
                          (0.02, 0.02, 0.09), MAT_FLAME_MID,
                          rotation=(math.radians(3), math.radians(5), 0), vertices=6))
    parts.append(add_cone("FlameMidR", (0.005, 0.042, flame_base_z + 0.05),
                          (0.02, 0.02, 0.09), MAT_FLAME_MID,
                          rotation=(math.radians(-3), math.radians(-4), 0), vertices=6))

    # Core flame — small, intense yellow-white hot center
    parts.append(add_cone("FlameCore", (0, 0.04, flame_base_z + 0.04),
                          (0.015, 0.015, 0.07), MAT_FLAME_CORE, vertices=6))

    # Flame base glow — a small squashed sphere at the base of the flame
    parts.append(add_uv_sphere("FlameGlow", (0, 0.04, flame_base_z + 0.01),
                               (0.03, 0.03, 0.02), MAT_FLAME_MID,
                               segments=8, ring_count=6))

    # Apply all modifiers
    for p in parts:
        apply_modifiers(p)

    # Join everything into one mesh
    torch = join_objects(parts, "Torch")

    # Set origin to bracket base
    bpy.context.view_layer.objects.active = torch
    cursor_loc_backup = bpy.context.scene.cursor.location.copy()
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = cursor_loc_backup

    return torch


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    torch = build_torch()

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera
    bpy.ops.object.camera_add(location=(0.4, -0.5, 0.3),
                               rotation=(math.radians(70), 0, math.radians(20)))
    bpy.context.active_object.name = "TorchCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Medieval Wall Torch — static model!")
    print("  Iron bracket, wooden handle, cloth wrapping")
    print("  Multi-layered emissive flame")
    print("  ~0.5 units tall, origin at bracket base")
    print("")
    print("  To export for Unity, run export_torch.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
