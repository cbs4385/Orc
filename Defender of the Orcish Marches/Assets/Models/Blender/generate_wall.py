"""
Blender 4.x Python script -- Generate a fortress wall segment with octagonal tower ends.
Static mesh (no armature, no animations).

Dimensions (Blender coords):
  X: width  ~3.5 total (1.0 wall + two octagonal towers)
  Y: depth  0.5
  Z: height 2.0

Wall body: X -0.5 to 0.5, Y -0.25 to 0.25, Z -1.0 to 1.0
Octagonal towers at each X end, side length = 0.5 (matches wall depth).

Blender coords: X=width (Unity X), Y=depth (Unity Z), Z=height (Unity Y).
"""

import bpy
import bmesh
import math

# ──────────────────────────────────────────────
#  Constants for octagon geometry
# ──────────────────────────────────────────────

OCT_SIDE = 0.5                                    # each octagon face width = wall depth
OCT_R = OCT_SIDE / (2.0 * math.sin(math.pi / 8)) # circumradius  ≈ 0.6533
OCT_APOTHEM = OCT_R * math.cos(math.pi / 8)       # center→flat   ≈ 0.6036

# Wall extents
WALL_HALF_W = 0.5     # wall half-width (X)
WALL_HALF_D = 0.25    # wall half-depth (Y)
WALL_HALF_H = 1.0     # wall half-height (Z)
WALL_DEPTH  = 0.5     # total depth

# Tower centres (one flat face flush with wall end)
TOWER_CX_R =  WALL_HALF_W + OCT_APOTHEM   # right tower
TOWER_CX_L = -WALL_HALF_W - OCT_APOTHEM   # left tower


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


def add_octagonal_prism(name, center, height, material):
    """Create an octagonal prism centred at *center* with side = OCT_SIDE.

    Oriented so one flat face is perpendicular to the X axis and faces
    toward the wall centre (−X for the right tower, +X for the left).
    rotation_offset = π/8 gives a flat at ±X.
    """
    mesh = bpy.data.meshes.new(name + "_mesh")
    obj  = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    half_h = height / 2.0
    rot_off = math.pi / 8.0          # 22.5° so flats align with ±X

    bottom, top = [], []
    for i in range(8):
        a = rot_off + i * (math.pi / 4.0)
        x = center[0] + OCT_R * math.cos(a)
        y = center[1] + OCT_R * math.sin(a)
        bottom.append(bm.verts.new((x, y, center[2] - half_h)))
        top.append(   bm.verts.new((x, y, center[2] + half_h)))

    # faces
    bm.faces.new(bottom[::-1])        # bottom cap
    bm.faces.new(top)                 # top cap
    for i in range(8):
        j = (i + 1) % 8
        bm.faces.new([bottom[i], bottom[j], top[j], top[i]])

    bm.to_mesh(mesh)
    bm.free()

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
    MAT_STONE    = make_material("WallStone",   (0.50, 0.45, 0.38, 1.0), roughness=0.95)
    MAT_STONE_DK = make_material("WallStoneDk", (0.35, 0.30, 0.25, 1.0), roughness=0.95)
    MAT_STONE_LT = make_material("WallStoneLt", (0.58, 0.54, 0.48, 1.0), roughness=0.90)
    MAT_IRON     = make_material("WallIron",    (0.25, 0.23, 0.22, 1.0),
                                  roughness=0.4, metallic=0.7)


# ──────────────────────────────────────────────
#  Build the wall segment
# ──────────────────────────────────────────────

def build_wall():
    """Build a fortress wall with octagonal tower ends.

    Wall body coordinate ranges (Blender):
      X: -0.5  to  0.5   (width 1.0)
      Y: -0.25 to  0.25  (depth 0.5)
      Z: -1.0  to  1.0   (height 2.0)

    Octagonal towers extend beyond ±0.5 on X.
    Each octagon side = 0.5 (matches wall depth).
    """
    parts = []

    # ────────────────────────────────────────────
    #  MAIN WALL BODY
    # ────────────────────────────────────────────
    # Body fills from Z = -1.0 up to body_top, leaving room for crenellations
    body_top = 0.60                     # Z coord of wall-walk level
    body_h   = body_top + WALL_HALF_H   # total body height = 1.60
    body_cz  = -WALL_HALF_H + body_h / 2.0   # centre Z of body

    parts.append(add_cube("WallBody", (0, 0, body_cz),
                          (1.0, WALL_DEPTH, body_h), MAT_STONE))
    bevel_object(parts[-1], 0.012)

    # ── FOUNDATION COURSE ──
    found_h = 0.14
    parts.append(add_cube("Foundation", (0, 0, -WALL_HALF_H + found_h / 2.0),
                          (1.04, WALL_DEPTH + 0.04, found_h), MAT_STONE_DK))
    bevel_object(parts[-1], 0.008)

    # ── WALKWAY LEDGE ──
    ledge_z = body_top + 0.025
    parts.append(add_cube("Ledge", (0, 0, ledge_z),
                          (1.02, WALL_DEPTH + 0.04, 0.05), MAT_STONE_LT))

    # ── CRENELLATIONS (merlons) ──
    merlon_h = 0.28
    merlon_z = ledge_z + 0.025 + merlon_h / 2.0
    merlon_w = 0.22
    merlon_d = WALL_DEPTH - 0.04

    for mx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"Merlon_{mx:.2f}", (mx, 0, merlon_z),
                              (merlon_w, merlon_d, merlon_h), MAT_STONE))
        bevel_object(parts[-1], 0.008)
        # cap
        cap_z = merlon_z + merlon_h / 2.0 + 0.012
        parts.append(add_cube(f"MerlonCap_{mx:.2f}", (mx, 0, cap_z),
                              (merlon_w + 0.03, merlon_d + 0.03, 0.022), MAT_STONE_LT))

    # ── STONE COURSE LINES (horizontal mortar, front & back) ──
    mortar_t  = 0.012
    mortar_zs = [-0.60, -0.20, 0.20]
    fy = -WALL_HALF_D - 0.005
    by =  WALL_HALF_D + 0.005
    for i, mz in enumerate(mortar_zs):
        parts.append(add_cube(f"MortarF_{i}", (0, fy, mz),
                              (0.98, 0.006, mortar_t), MAT_STONE_DK))
        parts.append(add_cube(f"MortarB_{i}", (0, by, mz),
                              (0.98, 0.006, mortar_t), MAT_STONE_DK))

    # ── VERTICAL MORTAR (staggered, front face only) ──
    vm_w = 0.008
    # Course 1 (below -0.60)
    for vx in [-0.17, 0.17]:
        parts.append(add_cube(f"VMortarF_c1_{vx:.2f}", (vx, fy, -0.80),
                              (vm_w, 0.006, 0.38), MAT_STONE_DK))
    # Course 2 (-0.60 to -0.20)
    for vx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"VMortarF_c2_{vx:.2f}", (vx, fy, -0.40),
                              (vm_w, 0.006, 0.38), MAT_STONE_DK))
    # Course 3 (-0.20 to 0.20)
    for vx in [-0.17, 0.17]:
        parts.append(add_cube(f"VMortarF_c3_{vx:.2f}", (vx, fy, 0.00),
                              (vm_w, 0.006, 0.38), MAT_STONE_DK))
    # Course 4 (0.20 to body_top)
    for vx in [-0.33, 0.0, 0.33]:
        parts.append(add_cube(f"VMortarF_c4_{vx:.2f}", (vx, fy, 0.40),
                              (vm_w, 0.006, 0.38), MAT_STONE_DK))

    # ── IRON REINFORCEMENT BANDS ──
    iron_h  = 0.04
    iron_zs = [-0.35, 0.40]
    for i, iz in enumerate(iron_zs):
        parts.append(add_cube(f"IronF_{i}", (0, fy - 0.004, iz),
                              (0.96, 0.018, iron_h), MAT_IRON))
        parts.append(add_cube(f"IronB_{i}", (0, by + 0.004, iz),
                              (0.96, 0.018, iron_h), MAT_IRON))

    # ── IRON STUDS ──
    stud = (0.035, 0.025, 0.035)
    for iz in iron_zs:
        for sx in [-0.44, 0.44]:
            parts.append(add_cube(f"StudF_{iz}_{sx}", (sx, fy - 0.010, iz),
                                  stud, MAT_IRON))
            parts.append(add_cube(f"StudB_{iz}_{sx}", (sx, by + 0.010, iz),
                                  stud, MAT_IRON))

    # ── CORNER PILASTERS ──
    pil_w = 0.05
    pil_h = body_h - found_h
    for sx in [-WALL_HALF_W + pil_w / 2, WALL_HALF_W - pil_w / 2]:
        parts.append(add_cube(f"Pilaster_{sx:.2f}",
                              (sx, 0, -WALL_HALF_H + found_h + pil_h / 2),
                              (pil_w, WALL_DEPTH + 0.01, pil_h), MAT_STONE_DK))

    # ────────────────────────────────────────────
    #  OCTAGONAL TOWER ENDS
    # ────────────────────────────────────────────
    tower_body_h = body_h                      # same height as wall body
    tower_body_cz = body_cz                    # same centre Z

    for side, tcx in [("R", TOWER_CX_R), ("L", TOWER_CX_L)]:
        # Main tower body (octagonal prism)
        t = add_octagonal_prism(f"Tower_{side}",
                                (tcx, 0, tower_body_cz),
                                tower_body_h, MAT_STONE)
        bevel_object(t, 0.012)
        parts.append(t)

        # Tower foundation ring (slightly larger octagon at base)
        tf = add_octagonal_prism(f"TowerFound_{side}",
                                 (tcx, 0, -WALL_HALF_H + found_h / 2),
                                 found_h, MAT_STONE_DK)
        # Scale it slightly larger in XY
        tf.scale = (1.04, 1.04, 1.0)
        bpy.context.view_layer.objects.active = tf
        tf.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        tf.select_set(False)
        bevel_object(tf, 0.008)
        parts.append(tf)

        # Tower ledge / walkway ring at top of body
        tl = add_octagonal_prism(f"TowerLedge_{side}",
                                 (tcx, 0, ledge_z),
                                 0.05, MAT_STONE_LT)
        tl.scale = (1.04, 1.04, 1.0)
        bpy.context.view_layer.objects.active = tl
        tl.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        tl.select_set(False)
        parts.append(tl)

        # Tower crenellations -- 8 small merlons around the top
        # Place a merlon at the centre of each octagon face, wide side tangent
        rot_off = math.pi / 8.0
        tm_h = merlon_h * 0.85
        tm_w = OCT_SIDE * 0.50        # tangential width (along face)
        tm_d = 0.08                    # radial depth (thin, outward)
        for fi in range(8):
            face_angle = rot_off + fi * (math.pi / 4.0) + (math.pi / 8.0)
            # Place at the apothem (outer face centre)
            mx = tcx + OCT_APOTHEM * math.cos(face_angle)
            my =       OCT_APOTHEM * math.sin(face_angle)
            # Wide dimension (X) becomes tangential after rotation by face_angle + π/2
            # Thin dimension (Y) becomes radial (pointing outward)
            tm = add_cube(f"TMerlon_{side}_{fi}",
                          (mx, my, merlon_z),
                          (tm_w, tm_d, tm_h),
                          MAT_STONE,
                          rotation=(0, 0, face_angle + math.pi / 2.0))
            bevel_object(tm, 0.006)
            parts.append(tm)

            # merlon cap
            tc = add_cube(f"TMerlonCap_{side}_{fi}",
                          (mx, my, merlon_z + tm_h / 2.0 + 0.012),
                          (tm_w + 0.03, tm_d + 0.02, 0.022),
                          MAT_STONE_LT,
                          rotation=(0, 0, face_angle + math.pi / 2.0))
            parts.append(tc)

        # Tower iron band (decorative ring at mid-height, front-visible faces)
        # We'll add a thin octagonal ring
        for iz in iron_zs:
            tr = add_octagonal_prism(f"TowerIron_{side}_{iz:.2f}",
                                     (tcx, 0, iz),
                                     iron_h, MAT_IRON)
            tr.scale = (1.02, 1.02, 1.0)
            bpy.context.view_layer.objects.active = tr
            tr.select_set(True)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            tr.select_set(False)
            parts.append(tr)

    # ────────────────────────────────────────────
    #  Finalise
    # ────────────────────────────────────────────
    for p in parts:
        apply_modifiers(p)

    wall = join_objects(parts, "WallSegment")

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
    bpy.ops.object.light_add(type='SUN', location=(4, -4, 6))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-3, 3, 4))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 4.0

    # Camera -- pulled back to show full wall + towers
    bpy.ops.object.camera_add(location=(2.5, -3.0, 1.5),
                               rotation=(math.radians(68), 0, math.radians(22)))
    bpy.context.active_object.name = "WallCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 60)
    print("  Wall Segment with Octagonal Tower Ends")
    print(f"  Wall body:  1.0 W × 0.5 D × 2.0 H")
    print(f"  Octagon side length: {OCT_SIDE}")
    print(f"  Octagon circumradius: {OCT_R:.4f}")
    print(f"  Octagon apothem:      {OCT_APOTHEM:.4f}")
    print(f"  Tower centres at X = ±{TOWER_CX_R:.4f}")
    print("  Static mesh -- no armature or animations")
    print("")
    print("  To export for Unity, run export_wall.py")
    print("=" * 60)


if __name__ == "__main__":
    main()