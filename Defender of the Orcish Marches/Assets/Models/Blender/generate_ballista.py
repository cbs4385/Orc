"""
Blender 4.x Python script -- Generate a ballista (scorpio) model.
Static mesh (no armature, no animations).

Built at actual game dimensions:
  Base frame ~0.44 wide, ~1.40 long, ~0.06 thick
  Torsion housings and arms extend above and forward
  Total footprint roughly 0.65 wide x 1.50 long x 0.28 tall

In Unity, place at (0, 3.2, 0) on top of the tower with scale (1,1,1).
The Ballista.cs script rotates the entire object around Y-axis to aim.
Add a child "FirePoint" at approximately local (0, 0, 0.75) -- the front.

Blender coords: X=width (Unity X), Y=forward (Unity Z), Z=height (Unity Y).
Forward (firing direction) is +Y in Blender = +Z in Unity.
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

MAT_WOOD = MAT_WOOD_DK = MAT_BOW = MAT_IRON = MAT_STRING = None


def create_materials():
    global MAT_WOOD, MAT_WOOD_DK, MAT_BOW, MAT_IRON, MAT_STRING
    # Warm brown wood (base frame, housings, trough)
    MAT_WOOD    = make_material("BallistaWood",   (0.40, 0.25, 0.12, 1.0), roughness=0.85)
    # Darker wood (rails, beams, winch drum)
    MAT_WOOD_DK = make_material("BallistaWoodDk", (0.28, 0.16, 0.07, 1.0), roughness=0.90)
    # Composite bow limb (reddish dark wood)
    MAT_BOW     = make_material("BallistaBow",    (0.35, 0.15, 0.06, 1.0), roughness=0.75)
    # Dark iron (bands, brackets, mechanism)
    MAT_IRON    = make_material("BallistaIron",   (0.25, 0.23, 0.22, 1.0),
                                 roughness=0.4, metallic=0.7)
    # Tan rope/sinew (bowstring)
    MAT_STRING  = make_material("BallistaString", (0.55, 0.45, 0.30, 1.0), roughness=0.95)


# ──────────────────────────────────────────────
#  Build the ballista
# ──────────────────────────────────────────────

def build_ballista():
    """Build a scorpio / ballista siege weapon.

    Coordinate layout (Blender):
      X = width (left-right)
      Y = length (forward = firing direction = +Y)
      Z = height (up)

    Base frame center at origin.  Forward end at Y ~ +0.70.
    """
    parts = []

    # ── DIMENSIONS ──
    FRAME_W = 0.44    # base platform width (X)
    FRAME_L = 1.40    # base platform length (Y)
    FRAME_H = 0.06    # base platform thickness (Z)
    HW = FRAME_W / 2  # 0.22
    HL = FRAME_L / 2  # 0.70

    # ── BASE PLATFORM DECK ──
    parts.append(add_cube("Deck", (0, 0, FRAME_H / 2),
                          (FRAME_W, FRAME_L, FRAME_H), MAT_WOOD))
    bevel_object(parts[-1], 0.004)

    # ── SIDE RAILS (two heavy beams along length) ──
    rail_w = 0.05
    rail_h = 0.10
    for sx in [-1, 1]:
        rx = sx * (HW - rail_w / 2 + 0.012)
        parts.append(add_cube(f"Rail_{sx}", (rx, 0, rail_h / 2),
                              (rail_w, FRAME_L + 0.02, rail_h), MAT_WOOD_DK))
        bevel_object(parts[-1], 0.003)

    # ── CROSS BEAMS (3 transverse braces) ──
    beam_d = 0.06
    beam_h = 0.07
    for by in [-0.50, -0.05, 0.35]:
        parts.append(add_cube(f"Beam_{by:.2f}", (0, by, beam_h / 2),
                              (FRAME_W + 0.04, beam_d, beam_h), MAT_WOOD_DK))
        bevel_object(parts[-1], 0.003)

    # ── TORSION SPRING HOUSINGS ──
    # Two housings flanking the trough, forward of center
    h_w = 0.14     # housing width
    h_d = 0.18     # housing depth
    h_h = 0.16     # housing height
    h_y = 0.38     # housing center Y (forward of base center)
    h_z = FRAME_H + h_h / 2
    for sx in [-1, 1]:
        hx = sx * 0.18
        parts.append(add_cube(f"Housing_{sx}", (hx, h_y, h_z),
                              (h_w, h_d, h_h), MAT_WOOD))
        bevel_object(parts[-1], 0.005)
        # Iron bands around housing (upper and lower)
        for band_off in [0.03, -0.04]:
            parts.append(add_cube(f"HBand_{sx}_{band_off:.2f}",
                                  (hx, h_y, h_z + band_off),
                                  (h_w + 0.02, h_d + 0.02, 0.025), MAT_IRON))
        # Torsion bundle cap (cylindrical)
        parts.append(add_cylinder(f"TorsionCap_{sx}",
                                   (hx, h_y, h_z + h_h / 2 + 0.01),
                                   (h_w * 0.7, h_w * 0.7, 0.02), MAT_IRON,
                                   vertices=10))

    # ── TORSION ARMS (the "bow" limbs) ──
    arm_l = 0.38    # arm length
    arm_w = 0.055   # arm width
    arm_h = 0.04    # arm height
    arm_angle = 20  # degrees outward
    ang = math.radians(arm_angle)
    arm_start_y = h_y + h_d / 2  # front edge of housing

    for sx in [-1, 1]:
        ax_start = sx * 0.18
        # Arm center = midpoint of arm extent
        arm_cx = ax_start + sx * math.sin(ang) * arm_l / 2
        arm_cy = arm_start_y + math.cos(ang) * arm_l / 2
        parts.append(add_cube(f"Arm_{sx}", (arm_cx, arm_cy, h_z),
                              (arm_w, arm_l, arm_h), MAT_BOW,
                              rotation=(0, 0, sx * ang)))
        bevel_object(parts[-1], 0.003)

        # Iron tip cap at arm end
        tip_x = ax_start + sx * math.sin(ang) * arm_l
        tip_y = arm_start_y + math.cos(ang) * arm_l
        parts.append(add_cube(f"ArmTip_{sx}", (tip_x, tip_y, h_z),
                              (arm_w + 0.02, 0.04, arm_h + 0.015), MAT_IRON))

    # ── BOWSTRING ──
    # Spans between the two arm tips
    ltip_x = -0.18 - math.sin(ang) * arm_l
    rtip_x = 0.18 + math.sin(ang) * arm_l
    string_y = arm_start_y + math.cos(ang) * arm_l
    string_w = rtip_x - ltip_x
    parts.append(add_cube("Bowstring", (0, string_y, h_z),
                          (string_w, 0.015, 0.015), MAT_STRING))

    # String wraps at each arm tip
    for sx in [-1, 1]:
        wx = (ltip_x if sx == -1 else rtip_x)
        parts.append(add_cube(f"StringWrap_{sx}", (wx, string_y, h_z),
                              (0.03, 0.03, 0.025), MAT_STRING))

    # ── TROUGH / SLIDE RAIL ──
    trough_w = 0.05
    trough_l = 1.15
    trough_h = 0.035
    trough_z = FRAME_H + trough_h / 2
    trough_y = -0.08  # slightly rearward of center
    parts.append(add_cube("Trough", (0, trough_y, trough_z),
                          (trough_w, trough_l, trough_h), MAT_WOOD))

    # Raised iron edges on trough
    for sx in [-1, 1]:
        parts.append(add_cube(f"TroughEdge_{sx}",
                              (sx * (trough_w / 2 + 0.008), trough_y,
                               trough_z + trough_h / 2 + 0.008),
                              (0.012, trough_l, 0.016), MAT_IRON))

    # ── BOLT SLIDER / RELEASE MECHANISM ──
    slider_y = 0.22
    slider_z = trough_z + trough_h / 2 + 0.02
    parts.append(add_cube("Slider", (0, slider_y, slider_z),
                          (0.08, 0.10, 0.04), MAT_IRON))
    # Trigger lever
    parts.append(add_cube("Trigger", (0, slider_y - 0.08, slider_z + 0.01),
                          (0.03, 0.05, 0.05), MAT_IRON))

    # ── WINDLASS / CRANK (rear loading mechanism) ──
    crank_y = -0.58
    crank_z = FRAME_H + 0.07

    # Crank support posts
    for sx in [-1, 1]:
        parts.append(add_cube(f"CrankPost_{sx}",
                              (sx * 0.12, crank_y, (crank_z + FRAME_H) / 2),
                              (0.04, 0.04, crank_z - FRAME_H + 0.04), MAT_WOOD))

    # Crossbar axle
    parts.append(add_cube("CrankAxle", (0, crank_y, crank_z + 0.03),
                          (0.30, 0.03, 0.03), MAT_IRON))

    # Crank handle knobs (extending outward along X)
    for sx in [-1, 1]:
        parts.append(add_cylinder(f"CrankHandle_{sx}",
                                   (sx * 0.17, crank_y, crank_z + 0.03),
                                   (0.02, 0.02, 0.06), MAT_WOOD,
                                   vertices=8,
                                   rotation=(0, math.radians(90), 0)))

    # Winch drum (centered on axle)
    parts.append(add_cylinder("WinchDrum", (0, crank_y, crank_z + 0.03),
                               (0.04, 0.04, 0.12), MAT_WOOD_DK,
                               vertices=10,
                               rotation=(0, math.radians(90), 0)))

    # Rope coils on drum (thin ring detail)
    parts.append(add_cylinder("RopeCoil", (0, crank_y, crank_z + 0.03),
                               (0.05, 0.05, 0.08), MAT_STRING,
                               vertices=10,
                               rotation=(0, math.radians(90), 0)))

    # ── IRON REINFORCEMENTS ──
    # Corner L-brackets on base frame
    for sx in [-1, 1]:
        for sy in [-1, 1]:
            bx = sx * (HW - 0.025)
            by = sy * (HL - 0.025)
            parts.append(add_cube(f"LBracket_{sx}_{sy}",
                                  (bx, by, FRAME_H + 0.005),
                                  (0.06, 0.06, 0.01), MAT_IRON))

    # Transverse iron bands on the deck
    for by in [-0.30, 0.10]:
        parts.append(add_cube(f"DeckBand_{by:.2f}",
                              (0, by, FRAME_H + 0.003),
                              (FRAME_W + 0.01, 0.025, 0.012), MAT_IRON))

    # Studs / bolt heads on side rails
    stud_s = 0.02
    for sx in [-1, 1]:
        for sy_off in [-0.50, -0.20, 0.10, 0.40]:
            parts.append(add_cube(f"Stud_{sx}_{sy_off:.2f}",
                                  (sx * (HW + 0.008), sy_off, FRAME_H / 2),
                                  (stud_s, stud_s, stud_s), MAT_IRON))

    # ── REAR SHIELD PLATE (small back plate for operator) ──
    shield_w = 0.30
    shield_h = 0.12
    shield_z = FRAME_H + shield_h / 2
    parts.append(add_cube("RearShield", (0, -HL + 0.02, shield_z),
                          (shield_w, 0.03, shield_h), MAT_IRON))

    # ── APPLY AND JOIN ──
    for p in parts:
        apply_modifiers(p)

    ballista = join_objects(parts, "Ballista")

    # Set origin at geometry center (matches cube-style origin for placement)
    bpy.context.view_layer.objects.active = ballista
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    ballista.location = (0, 0, 0)

    return ballista


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    ballista = build_ballista()

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(2, -2, 3))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-1, 1, 2))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 2.0

    # Camera -- angled top-down view showing the whole weapon
    bpy.ops.object.camera_add(location=(0.6, -1.2, 0.6),
                               rotation=(math.radians(65), 0, math.radians(15)))
    bpy.context.active_object.name = "BallistaCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Ballista (Scorpio) -- torsion siege crossbow")
    print("  Base frame ~0.44 x 1.40, height ~0.28")
    print("  Origin at geometry center (for placement at Y=3.2)")
    print("  Wood frame, torsion housings, angled arms")
    print("  Bowstring, slide trough, windlass crank")
    print("  Iron reinforcements, bolt slider mechanism")
    print("  Static mesh -- no armature or animations")
    print("  Ballista.cs rotates entire object to aim")
    print("")
    print("  To export for Unity, run export_ballista.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
