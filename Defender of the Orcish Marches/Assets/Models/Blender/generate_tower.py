"""
Blender 4.x Python script -- Generate a fortress tower (keep) model.
Static mesh (no armature, no animations).

Built at actual game dimensions (footprint ~1.6x1.6, height ~3.3 units).
Origin at base center so Unity placement at (0,0,0) puts base on ground.

The tower is the central keep of the orcish fortress:
  - Square stone keep with corner buttresses
  - Arrow slits, iron reinforcement bands, mortar course lines
  - Overhanging corbel ledge with crenellated parapet
  - Raised corner turret posts
  - Entrance doorway on -Y face (south in Blender = -Z in Unity)
In-game, the ScorpioBase (ballista) sits at Y=3.2 on top of this tower.
Courtyard radius ~3.5, so the ~1.6 footprint leaves ample room.

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


def add_cylinder(name, location, scale, material, rotation=(0, 0, 0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=0.5, depth=1,
                                         location=location)
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

MAT_STONE = MAT_STONE_DK = MAT_STONE_LT = MAT_IRON = MAT_WOOD = None


def create_materials():
    global MAT_STONE, MAT_STONE_DK, MAT_STONE_LT, MAT_IRON, MAT_WOOD
    # Warm grey stone (main body) -- matches wall palette
    MAT_STONE    = make_material("TowerStone",   (0.50, 0.45, 0.38, 1.0), roughness=0.95)
    # Darker stone for foundation, buttresses, recessed areas
    MAT_STONE_DK = make_material("TowerStoneDk", (0.35, 0.30, 0.25, 1.0), roughness=0.95)
    # Lighter stone for corbels, caps, lintels
    MAT_STONE_LT = make_material("TowerStoneLt", (0.58, 0.54, 0.48, 1.0), roughness=0.90)
    # Dark iron for bands and studs
    MAT_IRON     = make_material("TowerIron",    (0.25, 0.23, 0.22, 1.0),
                                  roughness=0.4, metallic=0.7)
    # Dark wood for entrance door
    MAT_WOOD     = make_material("TowerWood",    (0.30, 0.18, 0.08, 1.0), roughness=0.85)


# ──────────────────────────────────────────────
#  Build the tower
# ──────────────────────────────────────────────

def build_tower():
    """Build a fortress keep tower.

    Height breakdown (Blender Z = Unity Y):
      Z = 0.00 - 0.25  Foundation (1.8 x 1.8, dark stone)
      Z = 0.25 - 2.50  Main body (1.6 x 1.6, stone)
      Z = 2.50 - 2.65  Corbel ledge (2.0 x 2.0, overhangs)
      Z = 2.65 - 3.10  Parapet walls (2.0 x 2.0 ring, 0.15 thick)
      Z = 3.10 - 3.30  Crenellations (merlons + corner turrets)

    Origin at base center (0, 0, 0).
    """
    parts = []

    # ── DIMENSIONS ──
    BODY_S = 1.6     # main body side length
    FOUND_S = 1.8    # foundation side length
    CORBEL_S = 2.0   # corbel / parapet outer side length
    WALL_T = 0.15    # parapet wall thickness

    HB = BODY_S / 2    # 0.8
    HF = FOUND_S / 2   # 0.9
    HC = CORBEL_S / 2   # 1.0

    # Height stations
    FOUND_TOP = 0.25
    BODY_TOP = 2.50
    CORBEL_TOP = 2.65
    PARAPET_TOP = 3.10
    MERLON_TOP = 3.30

    # ── FOUNDATION ──
    parts.append(add_cube("Foundation", (0, 0, FOUND_TOP / 2),
                          (FOUND_S, FOUND_S, FOUND_TOP), MAT_STONE_DK))
    bevel_object(parts[-1], 0.012)

    # ── MAIN BODY ──
    body_h = BODY_TOP - FOUND_TOP
    parts.append(add_cube("Body", (0, 0, FOUND_TOP + body_h / 2),
                          (BODY_S, BODY_S, body_h), MAT_STONE))
    bevel_object(parts[-1], 0.008)

    # ── CORNER BUTTRESSES ──
    # Thicker pilasters at 4 corners for a sturdy look
    butt_w = 0.20
    butt_h = body_h * 0.65
    for sx in [-1, 1]:
        for sy in [-1, 1]:
            bx = sx * (HB - butt_w / 2 + 0.025)
            by = sy * (HB - butt_w / 2 + 0.025)
            parts.append(add_cube(f"Buttress_{sx}_{sy}",
                                  (bx, by, FOUND_TOP + butt_h / 2),
                                  (butt_w, butt_w, butt_h), MAT_STONE_DK))
            bevel_object(parts[-1], 0.006)

    # ── HORIZONTAL MORTAR COURSE LINES (all 4 faces) ──
    mortar_fracs = [0.25, 0.50, 0.75]
    for i, frac in enumerate(mortar_fracs):
        mz = FOUND_TOP + body_h * frac
        # +/- X faces
        for sx in [-1, 1]:
            parts.append(add_cube(f"MortarX_{i}_{sx}",
                                  (sx * (HB + 0.003), 0, mz),
                                  (0.005, BODY_S - 0.06, 0.008), MAT_STONE_DK))
        # +/- Y faces
        for sy in [-1, 1]:
            parts.append(add_cube(f"MortarY_{i}_{sy}",
                                  (0, sy * (HB + 0.003), mz),
                                  (BODY_S - 0.06, 0.005, 0.008), MAT_STONE_DK))

    # ── ARROW SLITS (lower row: 1 per face, mid-height) ──
    slit_z = FOUND_TOP + body_h * 0.40
    for sx in [-1, 1]:
        parts.append(add_cube(f"SlitX_{sx}",
                              (sx * (HB + 0.005), 0, slit_z),
                              (0.06, 0.04, 0.25), MAT_STONE_DK))
    for sy in [-1, 1]:
        parts.append(add_cube(f"SlitY_{sy}",
                              (0, sy * (HB + 0.005), slit_z),
                              (0.04, 0.06, 0.25), MAT_STONE_DK))

    # ── ARROW SLITS (upper row: 2 per face, flanking) ──
    slit_z2 = FOUND_TOP + body_h * 0.72
    for sx in [-1, 1]:
        for off in [-0.30, 0.30]:
            parts.append(add_cube(f"Slit2X_{sx}_{off:.0f}",
                                  (sx * (HB + 0.005), off, slit_z2),
                                  (0.06, 0.035, 0.20), MAT_STONE_DK))
    for sy in [-1, 1]:
        for off in [-0.30, 0.30]:
            parts.append(add_cube(f"Slit2Y_{sy}_{off:.0f}",
                                  (off, sy * (HB + 0.005), slit_z2),
                                  (0.035, 0.06, 0.20), MAT_STONE_DK))

    # ── IRON REINFORCEMENT BANDS (one ring at ~55% height) ──
    iron_z = FOUND_TOP + body_h * 0.55
    for sx in [-1, 1]:
        parts.append(add_cube(f"IronX_{sx}",
                              (sx * (HB + 0.006), 0, iron_z),
                              (0.015, BODY_S - 0.12, 0.03), MAT_IRON))
    for sy in [-1, 1]:
        parts.append(add_cube(f"IronY_{sy}",
                              (0, sy * (HB + 0.006), iron_z),
                              (BODY_S - 0.12, 0.015, 0.03), MAT_IRON))

    # Iron corner studs at band height
    for sx in [-1, 1]:
        for sy in [-1, 1]:
            parts.append(add_cube(f"IronStud_{sx}_{sy}",
                                  (sx * (HB + 0.012), sy * (HB + 0.012), iron_z),
                                  (0.04, 0.04, 0.04), MAT_IRON))

    # ── SECOND IRON BAND (higher, near parapet) ──
    iron_z2 = BODY_TOP - 0.15
    for sx in [-1, 1]:
        parts.append(add_cube(f"Iron2X_{sx}",
                              (sx * (HB + 0.006), 0, iron_z2),
                              (0.015, BODY_S - 0.12, 0.025), MAT_IRON))
    for sy in [-1, 1]:
        parts.append(add_cube(f"Iron2Y_{sy}",
                              (0, sy * (HB + 0.006), iron_z2),
                              (BODY_S - 0.12, 0.015, 0.025), MAT_IRON))

    # ── CORBEL LEDGE (overhanging platform support) ──
    corbel_h = CORBEL_TOP - BODY_TOP
    parts.append(add_cube("Corbel", (0, 0, BODY_TOP + corbel_h / 2),
                          (CORBEL_S, CORBEL_S, corbel_h), MAT_STONE_LT))
    bevel_object(parts[-1], 0.008)

    # ── PARAPET WALLS (4 walls forming a ring) ──
    pw_h = PARAPET_TOP - CORBEL_TOP
    pw_z = CORBEL_TOP + pw_h / 2
    # -Y (south)
    parts.append(add_cube("ParapetS", (0, -HC + WALL_T / 2, pw_z),
                          (CORBEL_S, WALL_T, pw_h), MAT_STONE))
    # +Y (north)
    parts.append(add_cube("ParapetN", (0, HC - WALL_T / 2, pw_z),
                          (CORBEL_S, WALL_T, pw_h), MAT_STONE))
    # -X (west)
    parts.append(add_cube("ParapetW", (-HC + WALL_T / 2, 0, pw_z),
                          (WALL_T, CORBEL_S, pw_h), MAT_STONE))
    # +X (east)
    parts.append(add_cube("ParapetE", (HC - WALL_T / 2, 0, pw_z),
                          (WALL_T, CORBEL_S, pw_h), MAT_STONE))

    # ── CRENELLATIONS (3 merlons per side) ──
    merlon_h = MERLON_TOP - PARAPET_TOP
    merlon_w = 0.30
    merlon_xs = [-0.55, 0, 0.55]

    # Helper to add a merlon + cap on a given face
    def add_merlons_on_face(label, fixed_axis, fixed_val, vary_axis, positions):
        for pos in positions:
            if fixed_axis == 'y':
                loc = (pos, fixed_val, PARAPET_TOP + merlon_h / 2)
                sz = (merlon_w, WALL_T + 0.02, merlon_h)
                cap_sz = (merlon_w + 0.03, WALL_T + 0.04, 0.02)
            else:
                loc = (fixed_val, pos, PARAPET_TOP + merlon_h / 2)
                sz = (WALL_T + 0.02, merlon_w, merlon_h)
                cap_sz = (WALL_T + 0.04, merlon_w + 0.03, 0.02)

            cap_loc = (loc[0], loc[1], MERLON_TOP + 0.01)

            parts.append(add_cube(f"M{label}_{pos:.2f}", loc, sz, MAT_STONE))
            bevel_object(parts[-1], 0.004)
            parts.append(add_cube(f"MC{label}_{pos:.2f}", cap_loc, cap_sz, MAT_STONE_LT))

    add_merlons_on_face("S", 'y', -HC + WALL_T / 2, 'x', merlon_xs)
    add_merlons_on_face("N", 'y',  HC - WALL_T / 2, 'x', merlon_xs)
    add_merlons_on_face("W", 'x', -HC + WALL_T / 2, 'y', merlon_xs)
    add_merlons_on_face("E", 'x',  HC - WALL_T / 2, 'y', merlon_xs)

    # ── CORNER TURRET POSTS (raised at 4 parapet corners) ──
    turret_s = 0.22
    turret_h = merlon_h + 0.10
    for sx in [-1, 1]:
        for sy in [-1, 1]:
            cx = sx * (HC - turret_s / 2)
            cy = sy * (HC - turret_s / 2)
            parts.append(add_cube(f"Turret_{sx}_{sy}",
                                  (cx, cy, PARAPET_TOP + turret_h / 2),
                                  (turret_s, turret_s, turret_h), MAT_STONE_DK))
            bevel_object(parts[-1], 0.005)
            # Turret cap slab
            parts.append(add_cube(f"TurretCap_{sx}_{sy}",
                                  (cx, cy, PARAPET_TOP + turret_h + 0.012),
                                  (turret_s + 0.04, turret_s + 0.04, 0.025),
                                  MAT_STONE_LT))

    # ── ENTRANCE DOORWAY (-Y face / south) ──
    arch_w = 0.45
    arch_h = 0.65
    arch_z = FOUND_TOP + arch_h / 2
    # Dark recess behind the door
    parts.append(add_cube("DoorRecess", (0, -HB - 0.002, arch_z),
                          (arch_w, 0.08, arch_h), MAT_STONE_DK))
    # Wooden door planks
    parts.append(add_cube("Door", (0, -HB - 0.015, arch_z),
                          (arch_w - 0.06, 0.06, arch_h - 0.05), MAT_WOOD))
    # Stone lintel above door
    parts.append(add_cube("Lintel", (0, -HB - 0.01, FOUND_TOP + arch_h + 0.04),
                          (arch_w + 0.10, 0.10, 0.07), MAT_STONE_LT))
    bevel_object(parts[-1], 0.005)
    # Iron door bands
    for dz in [0.18, 0.40]:
        parts.append(add_cube(f"DoorBand_{dz:.2f}",
                              (0, -HB - 0.028, FOUND_TOP + dz),
                              (arch_w - 0.08, 0.02, 0.03), MAT_IRON))
    # Iron door ring handle
    parts.append(add_cube("DoorRing", (0, -HB - 0.035, FOUND_TOP + 0.30),
                          (0.06, 0.02, 0.06), MAT_IRON))

    # ── APPLY AND JOIN ──
    for p in parts:
        apply_modifiers(p)

    tower = join_objects(parts, "Tower")

    # Set origin at base center (ground level)
    bpy.context.view_layer.objects.active = tower
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    tower.location = (0, 0, 0)

    return tower


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    tower = build_tower()

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 4))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 4.0

    # Camera -- angled view showing front face and top
    bpy.ops.object.camera_add(location=(2.5, -3.5, 2.5),
                               rotation=(math.radians(60), 0, math.radians(30)))
    bpy.context.active_object.name = "TowerCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Tower Keep -- fortress central tower")
    print("  Footprint ~1.6x1.6, height ~3.3 units")
    print("  Origin at base center (ground level)")
    print("  Stone body, corner buttresses, arrow slits")
    print("  Overhanging corbel, crenellated parapet")
    print("  Entrance door, iron bands")
    print("  Static mesh -- no armature or animations")
    print("")
    print("  To export for Unity, run export_tower.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
