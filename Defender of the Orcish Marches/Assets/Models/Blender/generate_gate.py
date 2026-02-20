"""
Blender 4.x Python script -- Generate a fortress gate model.
Two-door gate with stone posts and iron-banded wooden doors.
Matches wall dimensions (2 wide x 2 tall x 0.5 deep in Unity units).

Doors rigged to LeftDoorPivot and RightDoorPivot bones for runtime rotation.
No animations -- Gate.cs handles door opening/closing at runtime.

Blender coords: X=width, Y=depth (becomes Unity Z), Z=height (becomes Unity Y).
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


def add_wedge(name, location, scale, material, rotation=(0, 0, 0)):
    """Triangular prism (wedge) -- used for spikes and decorative elements."""
    import bmesh
    mesh = bpy.data.meshes.new(name + "_mesh")
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    # Wedge: triangle cross-section extruded along Y
    hw, hd, hh = 0.5, 0.5, 0.5
    verts = [
        bm.verts.new((-hw, -hd, -hh)),
        bm.verts.new(( hw, -hd, -hh)),
        bm.verts.new(( 0,  -hd,  hh)),
        bm.verts.new((-hw,  hd, -hh)),
        bm.verts.new(( hw,  hd, -hh)),
        bm.verts.new(( 0,   hd,  hh)),
    ]
    bm.faces.new([verts[0], verts[1], verts[2]])          # front triangle
    bm.faces.new([verts[5], verts[4], verts[3]])          # back triangle
    bm.faces.new([verts[0], verts[3], verts[4], verts[1]])  # bottom
    bm.faces.new([verts[1], verts[4], verts[5], verts[2]])  # right slope
    bm.faces.new([verts[2], verts[5], verts[3], verts[0]])  # left slope
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


def parent_to_bone(arm_obj, bone_name, mesh):
    """Parent mesh to an armature bone using vertex group weighting."""
    mesh.parent = arm_obj
    mod = mesh.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm_obj
    vg = mesh.vertex_groups.new(name=bone_name)
    vg.add(list(range(len(mesh.data.vertices))), 1.0, 'REPLACE')


# ──────────────────────────────────────────────
#  Materials
# ──────────────────────────────────────────────

MAT_STONE = MAT_STONE_DK = MAT_WOOD = MAT_WOOD_DK = MAT_IRON = None


def create_materials():
    global MAT_STONE, MAT_STONE_DK, MAT_WOOD, MAT_WOOD_DK, MAT_IRON
    # Rough grey stone for posts and frame
    MAT_STONE    = make_material("GateStone",   (0.45, 0.43, 0.40, 1.0), roughness=0.95)
    MAT_STONE_DK = make_material("GateStoneDk", (0.35, 0.33, 0.30, 1.0), roughness=0.95)
    # Heavy dark wood for door panels
    MAT_WOOD     = make_material("GateWood",    (0.30, 0.18, 0.08, 1.0), roughness=0.85)
    MAT_WOOD_DK  = make_material("GateWoodDk",  (0.22, 0.12, 0.05, 1.0), roughness=0.85)
    # Dark iron for bands, hinges, studs, spikes
    MAT_IRON     = make_material("GateIron",    (0.25, 0.23, 0.22, 1.0),
                                  roughness=0.4, metallic=0.7)


# ──────────────────────────────────────────────
#  Gate dimensions (Blender units = Unity units)
# ──────────────────────────────────────────────

GATE_W = 2.0        # total width
GATE_H = 2.0        # total height
GATE_D = 0.5        # total depth (wall thickness)
POST_W = 0.3        # post width
TOP_H  = 0.3        # top beam height
OPEN_W = GATE_W - 2 * POST_W  # 1.4 opening width
OPEN_H = GATE_H - TOP_H       # 1.7 opening height
DOOR_W = OPEN_W / 2           # 0.7 each door panel
DOOR_T = 0.08                 # door thickness
PIVOT_X = OPEN_W / 2          # 0.7 pivot distance from center


# ──────────────────────────────────────────────
#  Build the gate frame (static structure)
# ──────────────────────────────────────────────

def build_frame():
    """Build posts, top beam, and decorative elements. Returns joined mesh."""
    parts = []

    # ── POSTS ──
    # Left post
    parts.append(add_cube("PostL", (-GATE_W/2 + POST_W/2, 0, GATE_H/2),
                          (POST_W, GATE_D, GATE_H), MAT_STONE))
    bevel_object(parts[-1], 0.02)
    # Right post
    parts.append(add_cube("PostR", (GATE_W/2 - POST_W/2, 0, GATE_H/2),
                          (POST_W, GATE_D, GATE_H), MAT_STONE))
    bevel_object(parts[-1], 0.02)

    # ── TOP BEAM (lintel) ──
    parts.append(add_cube("TopBeam", (0, 0, GATE_H - TOP_H/2),
                          (GATE_W, GATE_D, TOP_H), MAT_STONE))
    bevel_object(parts[-1], 0.02)

    # ── POST CAPS (slightly wider, on top of posts) ──
    cap_z = GATE_H + 0.06
    parts.append(add_cube("CapL", (-GATE_W/2 + POST_W/2, 0, cap_z),
                          (POST_W + 0.06, GATE_D + 0.06, 0.08), MAT_STONE_DK))
    bevel_object(parts[-1], 0.015)
    parts.append(add_cube("CapR", (GATE_W/2 - POST_W/2, 0, cap_z),
                          (POST_W + 0.06, GATE_D + 0.06, 0.08), MAT_STONE_DK))
    bevel_object(parts[-1], 0.015)

    # ── IRON SPIKES on post caps ──
    spike_z = cap_z + 0.04
    spike_h = 0.18
    # Left post spike
    parts.append(add_wedge("SpikeL", (-GATE_W/2 + POST_W/2, 0, spike_z + spike_h/2),
                           (0.08, 0.08, spike_h), MAT_IRON))
    # Right post spike
    parts.append(add_wedge("SpikeR", (GATE_W/2 - POST_W/2, 0, spike_z + spike_h/2),
                           (0.08, 0.08, spike_h), MAT_IRON))

    # ── IRON BANDS on posts ──
    band_t = 0.04
    for z in [0.5, 1.2]:
        parts.append(add_cube(f"BandL_{z}", (-GATE_W/2 + POST_W/2, 0, z),
                              (POST_W + 0.02, GATE_D + 0.02, band_t), MAT_IRON))
        parts.append(add_cube(f"BandR_{z}", (GATE_W/2 - POST_W/2, 0, z),
                              (POST_W + 0.02, GATE_D + 0.02, band_t), MAT_IRON))

    # ── KEYSTONE (center of top beam, protruding) ──
    parts.append(add_cube("Keystone", (0, -GATE_D/2 + 0.02, GATE_H - TOP_H/2),
                          (0.25, 0.08, TOP_H - 0.04), MAT_STONE_DK))
    bevel_object(parts[-1], 0.015)

    # ── IRON BRACKET on keystone ──
    parts.append(add_cube("KeyBracket", (0, -GATE_D/2 + 0.05, GATE_H - TOP_H/2),
                          (0.15, 0.02, 0.12), MAT_IRON))

    # ── THRESHOLD (bottom sill between posts) ──
    parts.append(add_cube("Threshold", (0, 0, 0.025),
                          (OPEN_W, GATE_D, 0.05), MAT_STONE_DK))

    # Apply modifiers
    for p in parts:
        apply_modifiers(p)

    return join_objects(parts, "GateFrame")


# ──────────────────────────────────────────────
#  Build a door panel
# ──────────────────────────────────────────────

def build_door(side="left"):
    """Build one door panel with iron bands and studs.
    side: 'left' or 'right'. Left door extends from X=-0.7 to X=0,
    right door from X=0 to X=0.7."""
    sign = -1 if side == "left" else 1
    cx = sign * DOOR_W / 2  # center X of door panel
    parts = []

    # ── MAIN DOOR PANEL ──
    panel_h = OPEN_H - 0.05  # slight clearance from threshold and lintel
    panel_z = panel_h / 2 + 0.05
    parts.append(add_cube(f"Panel_{side}", (cx, 0, panel_z),
                          (DOOR_W - 0.02, DOOR_T, panel_h), MAT_WOOD))
    bevel_object(parts[-1], 0.01)

    # ── VERTICAL PLANKS (darker wood strips for texture) ──
    plank_positions = [cx - 0.2 * sign, cx + 0.2 * sign]
    for i, px in enumerate(plank_positions):
        parts.append(add_cube(f"Plank_{side}_{i}", (px, -DOOR_T/2 - 0.005, panel_z),
                              (0.03, 0.01, panel_h - 0.06), MAT_WOOD_DK))

    # ── HORIZONTAL IRON BANDS ──
    band_zs = [0.25, 0.85, 1.45]
    for z in band_zs:
        parts.append(add_cube(f"DoorBand_{side}_{z}", (cx, -DOOR_T/2 - 0.005, z),
                              (DOOR_W - 0.04, 0.015, 0.06), MAT_IRON))

    # ── IRON STUDS (at band-plank intersections) ──
    stud_size = (0.03, 0.02, 0.03)
    for z in band_zs:
        for px in plank_positions:
            parts.append(add_cube(f"Stud_{side}_{z}_{px:.1f}",
                                  (px, -DOOR_T/2 - 0.01, z),
                                  stud_size, MAT_IRON))

    # ── DOOR RING (handle, center of door) ──
    ring_x = cx + 0.15 * sign  # toward center of gate
    ring_z = 0.85
    parts.append(add_cylinder(f"Ring_{side}", (ring_x, -DOOR_T/2 - 0.02, ring_z),
                              (0.04, 0.04, 0.015), MAT_IRON,
                              rotation=(math.radians(90), 0, 0), vertices=10))
    # Ring mount plate
    parts.append(add_cube(f"RingMount_{side}", (ring_x, -DOOR_T/2 - 0.01, ring_z),
                          (0.06, 0.01, 0.06), MAT_IRON))

    # Apply modifiers
    for p in parts:
        apply_modifiers(p)

    label = "LeftDoor" if side == "left" else "RightDoor"
    return join_objects(parts, label)


# ──────────────────────────────────────────────
#  Armature
# ──────────────────────────────────────────────

def build_armature():
    """Create armature with Root, LeftDoorPivot, and RightDoorPivot bones."""
    arm_data = bpy.data.armatures.new("GateRig")
    arm_obj = bpy.data.objects.new("GateArmature", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj

    bpy.ops.object.mode_set(mode='EDIT')

    # Root bone at center bottom
    root = arm_data.edit_bones.new("Root")
    root.head = (0, 0, 0)
    root.tail = (0, 0, 0.5)

    # LeftDoorPivot -- hinge at inner left post edge, vertical bone
    ldp = arm_data.edit_bones.new("LeftDoorPivot")
    ldp.head = (-PIVOT_X, 0, 0.05)
    ldp.tail = (-PIVOT_X, 0, OPEN_H)
    ldp.parent = root
    ldp.use_connect = False

    # RightDoorPivot -- hinge at inner right post edge, vertical bone
    rdp = arm_data.edit_bones.new("RightDoorPivot")
    rdp.head = (PIVOT_X, 0, 0.05)
    rdp.tail = (PIVOT_X, 0, OPEN_H)
    rdp.parent = root
    rdp.use_connect = False

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj


# ──────────────────────────────────────────────
#  Main
# ──────────────────────────────────────────────

def main():
    clear_scene()
    create_materials()

    # Build meshes
    frame = build_frame()
    left_door = build_door("left")
    right_door = build_door("right")

    # Build armature and parent meshes to bones
    arm_obj = build_armature()
    parent_to_bone(arm_obj, "Root", frame)
    parent_to_bone(arm_obj, "LeftDoorPivot", left_door)
    parent_to_bone(arm_obj, "RightDoorPivot", right_door)

    # Lighting
    bpy.ops.object.light_add(type='SUN', location=(3, -3, 5))
    bpy.context.active_object.name = "KeyLight"
    bpy.context.active_object.data.energy = 3.0

    bpy.ops.object.light_add(type='AREA', location=(-2, 2, 3))
    bpy.context.active_object.name = "FillLight"
    bpy.context.active_object.data.energy = 50.0
    bpy.context.active_object.data.size = 3.0

    # Camera -- front view showing the gate
    bpy.ops.object.camera_add(location=(0, -4, 1.5),
                               rotation=(math.radians(78), 0, 0))
    bpy.context.active_object.name = "GateCamera"
    bpy.context.scene.camera = bpy.context.active_object

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.render.fps = 24

    print("=" * 50)
    print("  Gate Model -- fortress gate with two swinging doors")
    print(f"  Dimensions: {GATE_W} x {GATE_H} x {GATE_D} (W x H x D)")
    print(f"  Opening: {OPEN_W} x {OPEN_H}")
    print("  Bones: Root, LeftDoorPivot, RightDoorPivot")
    print("  No animations (Gate.cs rotates doors at runtime)")
    print("")
    print("  To export for Unity, run export_gate.py")
    print("=" * 50)


if __name__ == "__main__":
    main()
