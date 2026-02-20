"""
Blender 4.x Python script — Generate a small treasure chest with gold coins.
Static model (no armature, no animations) used as a spinning loot pickup.

Visual: small wooden chest, open lid tilted back, gold coins peeking out.
Metal bands and clasp on the chest body. Sized to ~0.3 units total.

The game script already rotates the pickup on Y, so no animation needed.
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

MAT_WOOD = MAT_WOOD_DK = MAT_GOLD = MAT_METAL = MAT_LINING = None

def create_materials():
    global MAT_WOOD, MAT_WOOD_DK, MAT_GOLD, MAT_METAL, MAT_LINING
    # Rich dark wood
    MAT_WOOD    = make_material("TreasureWood",   (0.35, 0.20, 0.08, 1.0))
    MAT_WOOD_DK = make_material("TreasureWoodDk", (0.25, 0.14, 0.05, 1.0))
    # Gold coins — warm yellow, slight metallic sheen + subtle glow
    MAT_GOLD    = make_material("TreasureGold",   (1.0, 0.82, 0.2, 1.0),
                                emission=1.5, roughness=0.3, metallic=0.8)
    # Iron bands/clasp
    MAT_METAL   = make_material("TreasureMetal",  (0.45, 0.42, 0.40, 1.0),
                                roughness=0.4, metallic=0.7)
    # Red velvet lining (inside of chest)
    MAT_LINING  = make_material("TreasureLining", (0.50, 0.10, 0.08, 1.0))


# ──────────────────────────────────────────────
#  Build the treasure chest
# ──────────────────────────────────────────────

def build_treasure():
    """Build a small open treasure chest with gold coins.
    Model centered at origin, roughly 0.3 units wide/deep, 0.25 tall.
    Origin at base so it sits on the ground plane."""
    parts = []

    # Scale factor — the whole model should be about 0.3 units across
    # We'll build at a comfortable size then it gets imported at correct scale
    # Building at roughly 0.15 half-width

    # ── CHEST BODY (box shape, open top) ──
    # Bottom
    parts.append(add_cube("ChestBottom", (0, 0, 0.02),
                          (0.14, 0.10, 0.02), MAT_WOOD))
    bevel_object(parts[-1], 0.003)
    # Front wall
    parts.append(add_cube("ChestFront", (0, -0.045, 0.06),
                          (0.14, 0.015, 0.06), MAT_WOOD))
    bevel_object(parts[-1], 0.003)
    # Back wall
    parts.append(add_cube("ChestBack", (0, 0.045, 0.06),
                          (0.14, 0.015, 0.06), MAT_WOOD))
    bevel_object(parts[-1], 0.003)
    # Left wall
    parts.append(add_cube("ChestLeft", (-0.065, 0, 0.06),
                          (0.015, 0.10, 0.06), MAT_WOOD))
    bevel_object(parts[-1], 0.003)
    # Right wall
    parts.append(add_cube("ChestRight", (0.065, 0, 0.06),
                          (0.015, 0.10, 0.06), MAT_WOOD))
    bevel_object(parts[-1], 0.003)

    # ── LINING (inside walls — red velvet visible from top) ──
    parts.append(add_cube("Lining", (0, 0, 0.06),
                          (0.11, 0.07, 0.055), MAT_LINING))

    # ── METAL BANDS (iron straps around chest body) ──
    # Front band
    parts.append(add_cube("BandFront", (0, -0.048, 0.06),
                          (0.15, 0.005, 0.065), MAT_METAL))
    # Back band
    parts.append(add_cube("BandBack", (0, 0.048, 0.06),
                          (0.15, 0.005, 0.065), MAT_METAL))
    # Side band left
    parts.append(add_cube("BandLeft", (-0.068, 0, 0.06),
                          (0.005, 0.105, 0.065), MAT_METAL))
    # Side band right
    parts.append(add_cube("BandRight", (0.068, 0, 0.06),
                          (0.005, 0.105, 0.065), MAT_METAL))
    # Bottom corner reinforcements
    parts.append(add_cube("CornerFL", (-0.065, -0.045, 0.015),
                          (0.02, 0.02, 0.02), MAT_METAL))
    parts.append(add_cube("CornerFR", (0.065, -0.045, 0.015),
                          (0.02, 0.02, 0.02), MAT_METAL))
    parts.append(add_cube("CornerBL", (-0.065, 0.045, 0.015),
                          (0.02, 0.02, 0.02), MAT_METAL))
    parts.append(add_cube("CornerBR", (0.065, 0.045, 0.015),
                          (0.02, 0.02, 0.02), MAT_METAL))

    # ── CLASP (front lock piece) ──
    parts.append(add_cube("Clasp", (0, -0.055, 0.06),
                          (0.03, 0.01, 0.03), MAT_METAL))
    bevel_object(parts[-1], 0.002)
    # Keyhole
    parts.append(add_cube("Keyhole", (0, -0.062, 0.06),
                          (0.008, 0.005, 0.012), MAT_WOOD_DK))

    # ── LID (open, tilted back ~60°) ──
    # The lid pivots from the back edge of the chest
    # Lid at ~60° open angle from vertical
    lid_pivot_y = 0.045   # back edge
    lid_pivot_z = 0.09    # top of chest walls
    lid_angle = math.radians(-50)  # tilted back

    # Lid panel
    parts.append(add_cube("LidPanel", (0, lid_pivot_y + 0.04, lid_pivot_z + 0.04),
                          (0.14, 0.015, 0.10), MAT_WOOD,
                          rotation=(lid_angle, 0, 0)))
    bevel_object(parts[-1], 0.003)
    # Lid top (slightly arched / thicker top surface)
    parts.append(add_cube("LidTop", (0, lid_pivot_y + 0.045, lid_pivot_z + 0.05),
                          (0.13, 0.02, 0.08), MAT_WOOD_DK,
                          rotation=(lid_angle, 0, 0)))
    bevel_object(parts[-1], 0.004)
    # Metal band on lid
    parts.append(add_cube("LidBand", (0, lid_pivot_y + 0.042, lid_pivot_z + 0.04),
                          (0.15, 0.005, 0.105), MAT_METAL,
                          rotation=(lid_angle, 0, 0)))
    # Hinge left
    parts.append(add_cube("HingeL", (-0.05, 0.050, lid_pivot_z),
                          (0.015, 0.015, 0.01), MAT_METAL))
    # Hinge right
    parts.append(add_cube("HingeR", (0.05, 0.050, lid_pivot_z),
                          (0.015, 0.015, 0.01), MAT_METAL))

    # ── GOLD COINS (piled inside chest, visible from above) ──
    # Bottom layer of coins (flat cylinders)
    coin_z = 0.045
    coin_positions = [
        (0, 0, coin_z),
        (-0.03, -0.01, coin_z),
        (0.03, -0.01, coin_z),
        (-0.02, 0.02, coin_z),
        (0.02, 0.02, coin_z),
    ]
    for i, pos in enumerate(coin_positions):
        parts.append(add_cylinder(f"CoinBase{i}", pos,
                                  (0.025, 0.025, 0.008), MAT_GOLD,
                                  vertices=10))

    # Middle layer — slightly offset, stacked
    coin_z2 = 0.055
    coin_positions2 = [
        (-0.015, -0.005, coin_z2),
        (0.015, 0.005, coin_z2),
        (0.0, 0.015, coin_z2),
        (-0.03, 0.01, coin_z2),
    ]
    for i, pos in enumerate(coin_positions2):
        parts.append(add_cylinder(f"CoinMid{i}", pos,
                                  (0.025, 0.025, 0.008), MAT_GOLD,
                                  vertices=10))

    # Top coins — some tilted at angles for visual interest
    parts.append(add_cylinder("CoinTop0", (0.01, -0.01, 0.065),
                              (0.025, 0.025, 0.008), MAT_GOLD,
                              rotation=(math.radians(15), 0, math.radians(10)),
                              vertices=10))
    parts.append(add_cylinder("CoinTop1", (-0.02, 0.005, 0.065),
                              (0.025, 0.025, 0.008), MAT_GOLD,
                              rotation=(math.radians(-10), math.radians(20), 0),
                              vertices=10))
    # A few coins leaning against the edge (partially visible)
    parts.append(add_cylinder("CoinLean0", (0.04, 0, 0.06),
                              (0.025, 0.025, 0.008), MAT_GOLD,
                              rotation=(0, math.radians(60), 0),
                              vertices=10))
    parts.append(add_cylinder("CoinLean1", (-0.04, -0.01, 0.055),
                              (0.025, 0.025, 0.008), MAT_GOLD,
                              rotation=(0, math.radians(-55), math.radians(10)),
                              vertices=10))

    # Apply all modifiers
    for p in parts:
        apply_modifiers(p)

    # Join everything into one mesh
    chest = join_objects(parts, "TreasureChest")

    # Set origin to the base center
    bpy.context.view_layer.objects.active = chest
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    # Move origin to bottom center
    chest.location = (0, 0, 0)

    return chest


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    chest = build_treasure()

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera — closer in since the model is small
    bpy.ops.object.camera_add(location=(0.3, -0.4, 0.2),
                               rotation=(math.radians(70), 0, math.radians(20)))
    bpy.context.active_object.name = "TreasureCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Treasure Chest — static model!")
    print("  Small open wooden chest with gold coins inside")
    print("  Metal bands, clasp, red velvet lining")
    print("  No armature or animations (game script rotates it)")
    print("")
    print("  To export for Unity, run export_treasure.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
