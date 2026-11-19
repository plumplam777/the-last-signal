"""
The Last Signal — hero prop models, built procedurally in Blender (no addon, no paid generation).

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python Tools/blender/build_props.py -- [--preview <dir>]

Writes one FBX per prop to Assets/_Project/Art/Models/Props/. Every part is authored in *Unity* coordinates
(metres, +Y up, front of the prop = Unity -Z unless noted) and converted, so the numbers below match the
C# builders. Material slot names (Brass, Slate, Terracotta, Steel, Ceramic, Rubber, Glass, Cyan) are remapped to the
shared station materials by PropModels.cs, so colours live in one place (Documentation/ArtDirection.md).
With --preview, also renders a small Workbench PNG of each prop for a quick look.
"""
import math
import os
import sys

import bmesh
import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Props")

PREVIEW_COLORS = {
    "Brass": (0.82, 0.58, 0.27, 1), "Slate": (0.12, 0.15, 0.2, 1), "Terracotta": (0.62, 0.34, 0.25, 1),
    "Steel": (0.35, 0.38, 0.42, 1), "Ceramic": (0.86, 0.84, 0.78, 1), "Rubber": (0.05, 0.06, 0.07, 1),
    "Glass": (0.45, 0.8, 0.78, 0.35), "Cyan": (0.37, 0.9, 1.0, 1),
}


# ------------------------------------------------------------------ coordinates

def U(x, y, z):
    """Unity position → Blender position (FBX export -Z forward / Y up, then Unity flips X: B(x,y,z) → U(-x, z, -y))."""
    return (-x, -z, y)


def USize(sx, sy, sz):
    return (sx, sz, sy)


def URot(rx=0.0, ry=0.0, rz=0.0):
    """Unity Euler degrees (applied X then Y here; Z only for parts without X/Y) → Blender Euler radians."""
    return (math.radians(rx), math.radians(rz), math.radians(-ry))


# ------------------------------------------------------------------ scene helpers

def material(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = PREVIEW_COLORS.get(name, (0.5, 0.5, 0.5, 1))
    return m


_parts = []


def _finish(obj, mat, bevel, segments=2):
    obj.data.materials.append(material(mat))
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.limit_method = "ANGLE"
        mod.angle_limit = math.radians(40)
        mod.harden_normals = False
    _parts.append(obj)
    return obj


def box(size, pos, mat, bevel=0.015, rot=(0, 0, 0), segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1, location=U(*pos), rotation=URot(*rot))
    obj = bpy.context.object
    obj.scale = USize(*size)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return _finish(obj, mat, bevel, segments)


def cyl(radius, length, pos, mat, axis="y", verts=32, bevel=0.01, rot=None):
    """Cylinder along a Unity axis ('x', 'y' or 'z')."""
    r = {"y": URot(), "x": URot(rz=90), "z": URot(rx=90)}[axis] if rot is None else URot(*rot)
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=length, location=U(*pos), rotation=r)
    return _finish(bpy.context.object, mat, bevel)


def torus(major, minor, pos, mat, axis="z", major_seg=40, minor_seg=12, half=False):
    r = {"y": URot(), "x": URot(rz=90), "z": URot(rx=90)}[axis]
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=major_seg,
                                     minor_segments=minor_seg, location=(0, 0, 0), rotation=r)
    obj = bpy.context.object
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    if half:
        _cut_below(obj, 0.0)
    obj.location = U(*pos)
    return _finish(obj, mat, 0)


def half_shell(radius, length, pos, mat, thickness=0.012, verts=48):
    """Upper half of a tube along Unity Z (a canopy)."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=length, location=(0, 0, 0), rotation=URot(rx=90),
                                        end_fill_type="NOTHING")
    obj = bpy.context.object
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    _cut_below(obj, -0.001)
    mod = obj.modifiers.new("Solidify", "SOLIDIFY")
    mod.thickness = thickness
    obj.location = U(*pos)
    _parts.append(obj)
    obj.data.materials.append(material(mat))
    return obj


def _cut_below(obj, z):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < z - 1e-4], context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _parts.clear()


def export(name, preview_dir=None):
    for o in _parts:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        for m in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in _parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = _parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    # pivot = Unity origin of the authored coordinates (floor centre / hinge / hub)
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    obj.name = name
    obj.data.name = name
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))
    # merge doubles from joined pieces' touching faces is not needed; keep pieces separate (cleaner bevels)
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, use_mesh_modifiers=True,
                             mesh_smooth_type="OFF", add_leaf_bones=False, object_types={"MESH"})
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"[build_props] {name}: {tris} tris → {path}")
    if preview_dir:
        render_preview(obj, os.path.join(preview_dir, name + ".png"))


def render_preview(obj, path):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = 640, 480
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("W")
    scene.world = world
    dims = obj.dimensions
    size = max(dims) or 1
    center = sum((obj.matrix_world @ __import__("mathutils").Vector(c) for c in obj.bound_box), __import__("mathutils").Vector()) / 8
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    # three-quarter view from the prop's front (Unity -Z = Blender +Y), slightly above
    d = size * 1.9
    cam.location = (center.x - d * 0.55, center.y + d * 0.8, center.z + d * 0.45)
    direction = center - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# ------------------------------------------------------------------ props

def cryo_pod_base():
    """Medical stasis pod body. Length along Z (2.4 m), aisle side = +X, bed top at y 0.66."""
    box((1.2, 0.46, 2.4), (0, 0.27, 0), "Slate", 0.04)
    box((1.1, 0.08, 2.3), (0, 0.04, 0), "Rubber", 0.01)                      # kick recess
    box((1.27, 0.05, 2.47), (0, 0.52, 0), "Brass", 0.012)                    # trim band
    for z in (-0.72, 0.0, 0.72):                                             # service panels, aisle side
        box((0.02, 0.26, 0.56), (0.605, 0.28, z), "Steel", 0.006)
        box((0.025, 0.03, 0.12), (0.61, 0.36, z + 0.2), "Brass", 0.004)      # handle
    box((0.95, 0.1, 2.0), (0, 0.6, 0), "Ceramic", 0.04, segments=3)          # mattress
    box((0.3, 0.06, 0.32), (0, 0.67, -0.82), "Ceramic", 0.025, segments=3)   # pillow (head end = -Z)
    for z in (-1.12, 1.12):                                                  # end caps
        box((1.2, 0.5, 0.18), (0, 0.78, z), "Slate", 0.05, segments=3)
        cyl(0.42, 0.05, (0, 0.82, z + (0.095 if z > 0 else -0.095)), "Brass", axis="z", verts=40, bevel=0.01)
        cyl(0.33, 0.06, (0, 0.82, z + (0.1 if z > 0 else -0.1)), "Steel", axis="z", verts=40, bevel=0.008)
    # controls on the foot end, aisle side
    box((0.26, 0.2, 0.3), (0.5, 0.66, 1.0), "Steel", 0.02)
    box((0.02, 0.06, 0.18), (0.635, 0.7, 1.0), "Cyan", 0.0)
    # coolant hoses down the wall side
    for z in (-1.0, 1.0):
        cyl(0.045, 0.6, (-0.52, 0.55, z), "Brass", axis="y", verts=16, bevel=0.0)
        torus(0.08, 0.045, (-0.52, 0.85, z - 0.08 if z > 0 else z + 0.08), "Brass", axis="x", major_seg=24, minor_seg=8)


def cryo_pod_canopy():
    """Glass canopy; pivot = hinge on the wall-side (-X) edge at the bed line. Closed: no rotation; open: rotate Z."""
    half_shell(0.5, 2.06, (0.5, 0, 0), "Glass")
    for z in (-1.03, -0.35, 0.35, 1.03):
        torus(0.5, 0.025, (0.5, 0, z), "Brass", axis="z", major_seg=40, minor_seg=8, half=True)
    for x in (0.0, 1.0):                                                     # rails along the lips
        box((0.05, 0.04, 2.1), (x, 0.0, 0), "Brass", 0.01)


def console_station():
    """Bridge workstation. Operator stands at -Z. Screen glow plane (added by the builder) sits at (0, 1.3, 0.175), tilted -8°."""
    box((1.4, 0.82, 0.56), (0, 0.45, 0.05), "Slate", 0.035)
    box((1.3, 0.1, 0.5), (0, 0.05, 0.08), "Rubber", 0.01)                    # kick recess
    box((1.18, 0.46, 0.02), (0, 0.46, -0.235), "Steel", 0.006)               # front fascia
    for x in (-0.3, 0.3):
        box((0.02, 0.4, 0.025), (x, 0.46, -0.245), "Brass", 0.004)           # fascia seams
    for x in (-0.72, 0.72):
        box((0.05, 0.92, 0.62), (x, 0.46, 0.05), "Brass", 0.015)             # side fins
    box((1.5, 0.04, 0.72), (0, 0.925, -0.05), "Brass", 0.012, rot=(-12, 0, 0))   # desk edge
    box((1.44, 0.05, 0.66), (0, 0.955, -0.05), "Slate", 0.015, rot=(-12, 0, 0))  # desk top
    box((0.86, 0.015, 0.22), (0, 0.99, -0.17), "Steel", 0.004, rot=(-12, 0, 0))  # keyboard insert
    for i in range(6):
        box((0.1, 0.012, 0.05), (-0.3 + i * 0.12, 0.995, -0.2), "Rubber", 0.003, rot=(-12, 0, 0))
    box((0.14, 0.32, 0.08), (0, 1.07, 0.26), "Steel", 0.01)                  # monitor neck
    box((1.24, 0.6, 0.06), (0, 1.3, 0.215), "Slate", 0.015, rot=(-8, 0, 0))   # bezel
    box((1.29, 0.65, 0.035), (0, 1.3, 0.245), "Brass", 0.01, rot=(-8, 0, 0))  # bezel frame
    box((0.3, 0.04, 0.03), (0, 1.58, 0.215), "Brass", 0.006, rot=(-8, 0, 0))  # top lip


def captain_chair():
    """Captain's chair. Faces +Z (towards the captain's console). Seat top at y 0.62."""
    cyl(0.32, 0.06, (0, 0.03, 0), "Brass", bevel=0.015, verts=40)
    for a in range(5):                                                       # star base
        ang = math.radians(a * 72)
        box((0.07, 0.05, 0.36), (math.sin(ang) * 0.2, 0.06, math.cos(ang) * 0.2), "Steel", 0.012, rot=(0, a * 72, 0))
    cyl(0.06, 0.36, (0, 0.27, 0), "Steel", bevel=0.005)
    box((0.74, 0.06, 0.69), (0, 0.46, 0), "Brass", 0.012)                    # seat frame
    box((0.7, 0.14, 0.65), (0, 0.55, 0.0), "Terracotta", 0.05, segments=3)    # cushion
    box((0.72, 0.9, 0.07), (0, 1.02, -0.37), "Slate", 0.012, rot=(-8, 0, 0))  # back frame
    box((0.66, 0.84, 0.14), (0, 1.03, -0.31), "Terracotta", 0.055, rot=(-8, 0, 0), segments=3)
    for y in (0.85, 1.05, 1.25):                                             # tufting seams
        box((0.6, 0.02, 0.02), (0, y, -0.235 - (y - 1.03) * 0.14), "Slate", 0.0, rot=(-8, 0, 0))
    box((0.4, 0.17, 0.12), (0, 1.58, -0.4), "Terracotta", 0.045, rot=(-8, 0, 0), segments=3)
    for x in (-0.42, 0.42):
        box((0.09, 0.06, 0.56), (x, 0.79, 0.02), "Brass", 0.015)            # armrests
        box((0.06, 0.24, 0.06), (x, 0.65, 0.18), "Steel", 0.008)
        box((0.06, 0.24, 0.06), (x, 0.65, -0.2), "Steel", 0.008)


def valve_wheel():
    """Coolant valve hand-wheel in the XY plane, facing -Z, hub at the origin (the builder spins this object)."""
    torus(0.3, 0.035, (0, 0, 0), "Terracotta", axis="z", major_seg=48, minor_seg=12)
    for a in (0, 60, 120):
        box((0.6, 0.035, 0.035), (0, 0, 0), "Terracotta", 0.008, rot=(0, 0, a))
    cyl(0.075, 0.09, (0, 0, 0), "Brass", axis="z", verts=24, bevel=0.012)
    cyl(0.03, 0.03, (0, 0, -0.055), "Steel", axis="z", verts=16, bevel=0.005)
    for a in range(0, 360, 30):                                              # grip knurls
        ang = math.radians(a)
        cyl(0.045, 0.02, (math.cos(ang) * 0.3, math.sin(ang) * 0.3, 0), "Terracotta", axis="z", verts=12, bevel=0.0)


def valve_body():
    """Valve body between the pipes; wheel spindle along -Z. Origin = wheel hub position (wheel sits at z -0.05)."""
    cyl(0.17, 0.26, (0, 0, 0.22), "Brass", axis="z", verts=32, bevel=0.02)
    for z in (0.1, 0.34):
        cyl(0.21, 0.04, (0, 0, z), "Steel", axis="z", verts=32, bevel=0.006)
        for a in range(0, 360, 45):                                          # bolts
            ang = math.radians(a)
            cyl(0.018, 0.05, (math.cos(ang) * 0.185, math.sin(ang) * 0.185, z), "Brass", axis="z", verts=8, bevel=0.0)
    cyl(0.04, 0.12, (0, 0, 0.04), "Steel", axis="z", verts=16, bevel=0.005)  # spindle
    cyl(0.13, 0.42, (0, 0, 0.3), "Brass", axis="y", verts=24, bevel=0.01)    # body bulge (vertical)


PROPS = [
    ("prop_cryo_pod_base", cryo_pod_base),
    ("prop_cryo_pod_canopy", cryo_pod_canopy),
    ("prop_console_station", console_station),
    ("prop_captain_chair", captain_chair),
    ("prop_valve_wheel", valve_wheel),
    ("prop_valve_body", valve_body),
]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    preview = argv[argv.index("--preview") + 1] if "--preview" in argv else None
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    os.makedirs(OUT, exist_ok=True)
    if preview:
        os.makedirs(preview, exist_ok=True)
    for name, build in PROPS:
        if only and name not in only:
            continue
        reset()
        build()
        export(name, preview)


main()
