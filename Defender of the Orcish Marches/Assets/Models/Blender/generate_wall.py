"""
Blender 4.x Python script -- Generate a fortress wall segment model.
Static mesh (no armature, no animations).

Built as a unit cube (-0.5 to 0.5 on each axis) so the Unity prefab
can keep its existing scale (2, 2, 0.5) without breaking WallCorners.cs.

After non-uniform stretch in Unity (2x wide, 2x tall, 0.5x deep):
  - Crenellations become ~0.6 wide, 0.3 tall merlons
  - Iron bands become thin horizontal strips
  - Stone details maintain correct proportions for a wall

Blender coords: X=width (Unity X), Y=depth (Unity Z), Z=height (Unity Y).
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


def add_wedge(name, location, scale, material, rotation=(0, 0, 0)):
    """Triangular prism (wedge) for spike details."""
    import bmesh
    mesh = bpy.data.meshes.new(name + "_mesh")
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    hw, hd, hh = 0.5, 0.5, 0.5
    verts = [
        bm.verts.new((-hw, -hd, -hh)),
        bm.verts.new(( hw, -hd, -hh)),
        bm.verts.new(( 0,  -hd,  hh)),
        bm.verts.new((-hw,  hd, -hh)),
        bm.verts.new(( hw,  hd, -hh)),
        bm.verts.new(( 0,   hd,  hh)),
    ]
    bm.faces.new([verts[0], verts[1], verts[2]])
    bm.faces.new([verts[5], verts[4], verts[3]])
    bm.faces.new([verts[0], verts[3], verts[4], verts[1]])
    bm.faces.new([verts[1], verts[4], verts[5], verts[2]])
    bm.faces.new([verts[2], verts[5], verts[3], verts[0]])
    bm.to_mesh(mesh)
    bm.free()

    obj.location = location
    obj.scale = scale
    obj.rotation_euler = rotation
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)
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
    if not objects:
        return None
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

MAT_STONE = MAT_STONE_DK = MAT_STONE_LT = MAT_IRON = None


def create_materials():
    global MAT_STONE, MAT_STONE_DK, MAT_STONE_LT, MAT_IRON
    # Warm grey stone (main wall body)
    MAT_STONE    = make_material("WallStone",   (0.50, 0.45, 0.38, 1.0), roughness=0.95)
    # Darker stone for foundation, mortar lines, recessed areas
    MAT_STONE_DK = make_material("WallStoneDk", (0.35, 0.30, 0.25, 1.0), roughness=0.95)
    # Lighter stone for crenellation caps and highlights
    MAT_STONE_LT = make_material("WallStoneLt", (0.58, 0.54, 0.48, 1.0), roughness=0.90)
    # Dark iron for reinforcement bands and studs
    MAT_IRON     = make_material("WallIron",    (0.25, 0.23, 0.22, 1.0),
                                  roughness=0.4, metallic=0.7)


# ──────────────────────────────────────────────
#  Build the wall segment
# ──────────────────────────────────────────────

def build_wall():
    """Build a fortress wall segment as a unit cube with stone detail.

    Coordinate ranges (Blender):
      X: -0.5 to 0.5 (width, Unity X)
      Y: -0.5 to 0.5 (depth, Unity Z)
      Z: -0.5 to 0.5 (height, Unity Y)

    After Unity scale (2, 2, 0.5):
      Width = 2, Height = 2, Depth = 0.5
    """
    parts = []

    # ── MAIN WALL BODY ──
    # Solid block filling most of the unit cube, leaving room for crenellations at top
    body_top = 0.36
    parts.append(add_cube("WallBody", (0, 0, (body_top - 0.5) / 2),
                          (1.0, 1.0, body_top + 0.5), MAT_STONE))
    bevel_object(parts[-1], 0.008)

    # ── FOUNDATION COURSE ──
    # Slightly wider/deeper at the base, darker stone
    found_h = 0.08
    parts.append(add_cube("Foundation", (0, 0, -0.5 + found_h / 2),
                          (1.04, 1.04, found_h), MAT_STONE_DK))
    bevel_object(parts[-1], 0.006)

    # ── WALKWAY LEDGE ──
    # Thin slab at the top of the main body, slightly protruding
    # Defenders would stand on this (visually)
    ledge_z = body_top + 0.015
    parts.append(add_cube("Ledge", (0, 0, ledge_z),
                          (1.02, 1.04, 0.03), MAT_STONE_LT))

    # ── CRENELLATIONS (merlons) ──
    # 3 merlons with gaps (crenels) between them
    merlon_h = 0.13
    merlon_z = ledge_z + 0.015 + merlon_h / 2
    merlon_w = 0.22
    merlon_d = 0.92  # slightly narrower than wall depth so edges are visible

    # Merlon positions along X (evenly spaced)
    for mx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"Merlon_{mx:.2f}", (mx, 0, merlon_z),
                              (merlon_w, merlon_d, merlon_h), MAT_STONE))
        bevel_object(parts[-1], 0.006)

        # Merlon cap (tiny lighter slab on top)
        cap_z = merlon_z + merlon_h / 2 + 0.008
        parts.append(add_cube(f"MerlonCap_{mx:.2f}", (mx, 0, cap_z),
                              (merlon_w + 0.02, merlon_d + 0.02, 0.015), MAT_STONE_LT))

    # ── STONE COURSE LINES (horizontal mortar grooves) ──
    # Thin dark strips on front and back faces to suggest stone courses
    mortar_t = 0.008  # mortar line height
    mortar_zs = [-0.30, -0.08, 0.14]  # 3 horizontal courses
    for i, mz in enumerate(mortar_zs):
        # Front face mortar
        parts.append(add_cube(f"MortarF_{i}", (0, -0.505, mz),
                              (0.98, 0.005, mortar_t), MAT_STONE_DK))
        # Back face mortar
        parts.append(add_cube(f"MortarB_{i}", (0, 0.505, mz),
                              (0.98, 0.005, mortar_t), MAT_STONE_DK))

    # ── VERTICAL MORTAR (staggered between courses, front only) ──
    # Course 1 (below mortar line at -0.30): 2 vertical joints
    v_mortar_w = 0.006
    for vx in [-0.17, 0.17]:
        parts.append(add_cube(f"VMortarF_c1_{vx:.2f}", (vx, -0.505, -0.40),
                              (v_mortar_w, 0.005, 0.19), MAT_STONE_DK))
    # Course 2 (between -0.30 and -0.08): offset joints
    for vx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"VMortarF_c2_{vx:.2f}", (vx, -0.505, -0.19),
                              (v_mortar_w, 0.005, 0.21), MAT_STONE_DK))
    # Course 3 (between -0.08 and 0.14): offset back
    for vx in [-0.17, 0.17]:
        parts.append(add_cube(f"VMortarF_c3_{vx:.2f}", (vx, -0.505, 0.03),
                              (v_mortar_w, 0.005, 0.21), MAT_STONE_DK))
    # Course 4 (between 0.14 and body_top): offset
    for vx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"VMortarF_c4_{vx:.2f}", (vx, -0.505, 0.25),
                              (v_mortar_w, 0.005, 0.21), MAT_STONE_DK))

    # ── IRON REINFORCEMENT BANDS (front and back faces) ──
    iron_h = 0.025
    iron_zs = [-0.15, 0.22]
    for i, iz in enumerate(iron_zs):
        # Front iron band
        parts.append(add_cube(f"IronF_{i}", (0, -0.508, iz),
                              (0.96, 0.015, iron_h), MAT_IRON))
        # Back iron band
        parts.append(add_cube(f"IronB_{i}", (0, 0.508, iz),
                              (0.96, 0.015, iron_h), MAT_IRON))

    # ── IRON STUDS at band ends ──
    stud_size = (0.025, 0.02, 0.025)
    for iz in iron_zs:
        for sx in [-0.45, 0.45]:
            parts.append(add_cube(f"StudF_{iz}_{sx}", (sx, -0.515, iz),
                                  stud_size, MAT_IRON))
            parts.append(add_cube(f"StudB_{iz}_{sx}", (sx, 0.515, iz),
                                  stud_size, MAT_IRON))

    # ── CORNER PILASTERS (subtle vertical strips at wall ends) ──
    # These help define the wall edges and look good when walls connect
    pilaster_w = 0.04
    pilaster_h = body_top + 0.5 - found_h
    for side_x in [-0.5 + pilaster_w / 2, 0.5 - pilaster_w / 2]:
        parts.append(add_cube(f"Pilaster_{side_x:.2f}",
                              (side_x, 0, -0.5 + found_h + pilaster_h / 2),
                              (pilaster_w, 1.01, pilaster_h), MAT_STONE_DK))

    # Apply all modifiers
    for p in parts:
        apply_modifiers(p)

    # Join into one mesh
    wall = join_objects(parts, "WallSegment")

    # Set origin to geometric center (should be near 0,0,0)
    bpy.context.view_layer.objects.active = wall
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    wall.location = (0, 0, 0)

    return wall


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    wall = build_wall()

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera -- angled view showing front face and crenellations
    bpy.ops.object.camera_add(location=(1.2, -1.8, 0.8),
                               rotation=(math.radians(70), 0, math.radians(25)))
    bpy.context.active_object.name = "WallCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Wall Segment -- fortress wall with crenellations")
    print("  Unit cube model (use with prefab scale 2, 2, 0.5)")
    print("  Stone body, iron bands, mortar course lines")
    print("  3 merlons on top for classic battlement look")
    print("  Static mesh -- no armature or animations")
    print("")
    print("  To export for Unity, run export_wall.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
