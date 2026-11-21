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


def sphere(radius, pos, mat, segments=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=U(*pos))
    return _finish(bpy.context.object, mat, 0)


def cable(points, radius, mat, resolution=2):
    """A tube through Unity-space points (sagging cables, hoses)."""
    curve = bpy.data.curves.new("cable", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = resolution
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for i, p in enumerate(points):
        spline.points[i].co = (*U(*p), 1.0)
    obj = bpy.data.objects.new("cable", curve)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    return _finish(bpy.context.object, mat, 0)


def sag(x0, x1, y, z, depth, n=10):
    return [(x0 + (x1 - x0) * i / n, y - depth * (1 - (2 * i / n - 1) ** 2), z) for i in range(n + 1)]


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


def captain_console():
    """Captain's console: low wide desk, top tilted +12° towards the chair (operator at -Z). Key glow added by the builder."""
    box((1.6, 0.86, 0.5), (0, 0.45, 0), "Slate", 0.035)
    box((1.5, 0.1, 0.44), (0, 0.05, 0.03), "Rubber", 0.01)
    box((1.4, 0.5, 0.02), (0, 0.45, -0.255), "Steel", 0.006)
    for x in (-0.47, 0.0, 0.47):
        box((0.02, 0.44, 0.025), (x, 0.45, -0.265), "Brass", 0.004)
    for x in (-0.82, 0.82):
        box((0.05, 0.95, 0.56), (x, 0.47, 0), "Brass", 0.015)
    box((1.72, 0.04, 0.62), (0, 0.925, 0), "Brass", 0.012, rot=(12, 0, 0))
    box((1.66, 0.05, 0.56), (0, 0.955, 0), "Slate", 0.015, rot=(12, 0, 0))
    box((0.36, 0.06, 0.08), (0, 1.0, 0.27), "Steel", 0.01)                   # comms grille at the back
    for i in range(5):
        box((0.3, 0.008, 0.008), (0, 1.0 + (i - 2) * 0.012, 0.225), "Rubber", 0.0)


def terminal_kiosk():
    """Patient terminal: cabinet with a front-mounted, tilted screen (glow plane at (0, 1.38, -0.18) by the builder)."""
    box((1.3, 0.86, 0.55), (0, 0.45, 0), "Slate", 0.035)
    box((1.2, 0.1, 0.5), (0, 0.05, 0.02), "Rubber", 0.01)
    box((1.4, 0.05, 0.7), (0, 0.93, 0.05), "Brass", 0.015)
    box((1.1, 0.42, 0.02), (0, 0.45, -0.28), "Ceramic", 0.008)               # clinical white front panel
    box((0.5, 0.06, 0.03), (0, 0.6, -0.295), "Steel", 0.006)                 # handle
    box((0.12, 0.3, 0.1), (0, 1.1, -0.05), "Steel", 0.01)                    # screen arm
    box((1.02, 0.66, 0.06), (0, 1.38, -0.13), "Slate", 0.015, rot=(-10, 0, 0))
    box((1.06, 0.7, 0.03), (0, 1.38, -0.1), "Brass", 0.008, rot=(-10, 0, 0))
    box((0.06, 0.06, 0.02), (0.45, 1.06, -0.165), "Cyan", 0.0, rot=(-10, 0, 0))  # status LED


def decode_console():
    """Research decode console: pedestal + desk tilted -15° (the six key interactables sit on it) + symbol display housing."""
    box((1.8, 0.96, 0.5), (0, 0.5, 0), "Slate", 0.035)
    box((1.7, 0.1, 0.44), (0, 0.05, 0.03), "Rubber", 0.01)
    for x in (-0.6, 0.0, 0.6):
        box((0.5, 0.6, 0.02), (x, 0.5, -0.255), "Steel", 0.006)
    for x in (-0.92, 0.92):
        box((0.05, 1.08, 0.58), (x, 0.52, -0.02), "Brass", 0.015)
    box((1.92, 0.06, 0.62), (0, 1.03, -0.05), "Brass", 0.015, rot=(-15, 0, 0))
    box((1.86, 0.04, 0.56), (0, 1.06, -0.05), "Slate", 0.01, rot=(-15, 0, 0))
    box((1.9, 0.42, 0.1), (0, 1.36, 0.27), "Slate", 0.02)                    # display housing (text at z 0.2)
    box((1.94, 0.46, 0.04), (0, 1.36, 0.31), "Brass", 0.01)


def desk():
    """Desk along Z (2.0 m), top at y 0.8, wall side = +X, drawer pedestal at +Z. Scale Z for longer desks."""
    box((1.1, 0.05, 2.0), (0, 0.8, 0), "Slate", 0.015)
    box((1.14, 0.03, 2.04), (0, 0.765, 0), "Brass", 0.008)
    box((0.85, 0.74, 0.62), (0.1, 0.38, 0.62), "Steel", 0.02)                # drawer pedestal
    for i in range(3):
        y = 0.16 + i * 0.22
        box((0.02, 0.18, 0.54), (-0.33, y + 0.02, 0.62), "Slate", 0.006)
        box((0.025, 0.025, 0.16), (-0.35, y + 0.07, 0.62), "Brass", 0.004)
    box((0.05, 0.74, 0.06), (-0.45, 0.38, -0.92), "Slate", 0.01)             # legs at the open end
    box((0.05, 0.74, 0.06), (0.45, 0.38, -0.92), "Slate", 0.01)
    box((0.04, 0.5, 1.7), (0.5, 0.45, 0.0), "Slate", 0.01)                   # modesty panel (wall side)


def office_chair():
    """Small swivel chair, faces +Z. Seat at y 0.48."""
    cyl(0.24, 0.04, (0, 0.02, 0), "Steel", bevel=0.01, verts=24)
    for a in range(5):
        ang = math.radians(a * 72)
        box((0.05, 0.04, 0.26), (math.sin(ang) * 0.14, 0.05, math.cos(ang) * 0.14), "Steel", 0.01, rot=(0, a * 72, 0))
    cyl(0.04, 0.36, (0, 0.24, 0), "Steel", bevel=0.004)
    box((0.48, 0.09, 0.46), (0, 0.46, 0), "Rubber", 0.035, segments=3)
    box((0.44, 0.48, 0.07), (0, 0.8, -0.23), "Rubber", 0.03, rot=(-8, 0, 0), segments=3)
    box((0.04, 0.3, 0.04), (0, 0.6, -0.24), "Brass", 0.006)


def cabinet_case():
    """Secure cabinet shell 1.2 x 2.0 x 0.5, open front (-Z), origin at the floor centre. Doors are separate (builder)."""
    w, h, d, t = 1.2, 2.0, 0.5, 0.05
    box((w, h, t), (0, h / 2, d / 2 - t / 2), "Slate", 0.01)
    for x in (-1, 1):
        box((t, h, d), (x * (w / 2 - t / 2), h / 2, 0), "Slate", 0.01)
        box((0.06, h + 0.02, 0.06), (x * (w / 2 - 0.02), h / 2, -d / 2 + 0.02), "Brass", 0.012)   # corner posts
    box((w, t, d), (0, h - t / 2, 0), "Slate", 0.01)
    box((w, t, d), (0, t / 2, 0), "Slate", 0.01)
    box((w + 0.06, 0.06, d + 0.06), (0, h + 0.02, 0), "Brass", 0.012)
    box((w + 0.04, 0.1, d + 0.04), (0, 0.05, 0), "Rubber", 0.01)
    box((0.28, 0.12, 0.02), (0, h - 0.18, -d / 2 + 0.005), "Steel", 0.005)   # label plate


def safe_case():
    """Specimen safe shell 1.1 x 1.5 x 0.7, thick walls, open front (-Z). Door separate (builder)."""
    w, h, d, t = 1.1, 1.5, 0.7, 0.08
    box((w, h, t), (0, h / 2, d / 2 - t / 2), "Steel", 0.015)
    for x in (-1, 1):
        box((t, h, d), (x * (w / 2 - t / 2), h / 2, 0), "Steel", 0.015)
    box((w, t, d), (0, h - t / 2, 0), "Steel", 0.015)
    box((w, t, d), (0, t / 2, 0), "Steel", 0.015)
    box((w + 0.04, 0.08, d + 0.04), (0, -0.0 + 0.04, 0), "Slate", 0.012)
    for x in (-1, 1):                                                        # front frame rails with rivets
        box((0.05, h, 0.04), (x * (w / 2 - 0.025), h / 2, -d / 2 + 0.01), "Brass", 0.01)
        for i in range(6):
            cyl(0.012, 0.02, (x * (w / 2 - 0.025), 0.15 + i * 0.24, -d / 2 - 0.012), "Steel", axis="z", verts=8, bevel=0.0)
    for y in (0.35, 1.15):                                                   # hinge barrels (left)
        cyl(0.03, 0.18, (-w / 2 - 0.02, y, -d / 2), "Brass", axis="y", verts=12, bevel=0.004)


def pipe_run():
    """2 m pipe segment along X, mounted on a wall at z = 0 and sticking out towards -Z. Tile it along walls."""
    for x in (-0.75, 0.25):                                                  # wall brackets
        box((0.08, 0.36, 0.05), (x, 0.0, -0.025), "Slate", 0.01)
        box((0.05, 0.05, 0.26), (x, 0.11, -0.13), "Steel", 0.008)
        box((0.05, 0.05, 0.2), (x, -0.11, -0.1), "Steel", 0.008)
        torus(0.085, 0.015, (x, 0.11, -0.22), "Brass", axis="x", major_seg=20, minor_seg=6)
        torus(0.06, 0.012, (x, -0.11, -0.16), "Brass", axis="x", major_seg=20, minor_seg=6)
    cyl(0.07, 2.0, (0, 0.11, -0.22), "Brass", axis="x", verts=20, bevel=0.0)  # coolant line
    cyl(0.045, 2.0, (0, -0.11, -0.16), "Steel", axis="x", verts=16, bevel=0.0)  # cable conduit
    cyl(0.085, 0.06, (0.98, 0.11, -0.22), "Steel", axis="x", verts=20, bevel=0.005)  # flange at the joint


def crate():
    """Cargo crate, 1 m cube centred on the origin (scaled per crate by the builder). Label faces -Z."""
    box((0.94, 0.94, 0.94), (0, 0, 0), "Slate", 0.03)
    for y in (-0.44, 0.44):                                                  # brass edge rims
        for z in (-0.47, 0.47):
            box((1.0, 0.08, 0.08), (0, y, z), "Brass", 0.015)
        for x in (-0.47, 0.47):
            box((0.08, 0.08, 1.0), (x, y, 0), "Brass", 0.015)
    for x in (-1, 1):                                                        # corner posts
        for z in (-1, 1):
            box((0.09, 0.82, 0.09), (x * 0.47, 0, z * 0.47), "Steel", 0.015)
    for z in (-1, 1):                                                        # side ribs
        box((0.86, 0.06, 0.03), (0, 0.0, z * 0.48), "Steel", 0.008)
    box((0.42, 0.26, 0.02), (0, 0.18, -0.485), "Terracotta", 0.006)          # label plate
    for x in (-0.15, 0.15):
        box((0.12, 0.05, 0.02), (x, -0.2, -0.485), "Rubber", 0.006)          # grab slots


def workbench():
    """Workbench along Z (2.4 m), top at y 0.88, user side = +X. Lower shelf; brass legs."""
    box((1.1, 0.07, 2.4), (0, 0.885, 0), "Slate", 0.015)
    box((1.14, 0.03, 2.44), (0, 0.84, 0), "Brass", 0.008)
    for x in (-0.47, 0.47):
        for z in (-1.12, 1.12):
            box((0.07, 0.84, 0.07), (x, 0.42, z), "Brass", 0.012)
    box((1.0, 0.04, 2.3), (0, 0.2, 0), "Steel", 0.008)                       # lower shelf
    box((0.04, 0.5, 2.3), (-0.5, 0.5, 0), "Slate", 0.008)                    # back panel (wall side)
    box((0.06, 0.12, 2.3), (0.52, 0.8, 0), "Steel", 0.008)                   # front apron
    box((0.5, 0.26, 0.4), (-0.15, 0.36, -0.7), "Terracotta", 0.02)           # parts bin on the shelf
    box((0.4, 0.2, 0.5), (-0.1, 0.32, 0.6), "Rubber", 0.02)


def fuse_box():
    """Wall fuse box, front = +Z (the builder turns it to face the hall). Socket recess at the centre."""
    box((0.62, 0.92, 0.2), (0, 0, 0), "Slate", 0.02)
    box((0.7, 0.07, 0.1), (0, 0.47, 0.06), "Brass", 0.015)
    box((0.7, 0.07, 0.1), (0, -0.47, 0.06), "Brass", 0.015)
    box((0.07, 0.98, 0.1), (-0.33, 0, 0.06), "Brass", 0.015)
    box((0.07, 0.98, 0.1), (0.33, 0, 0.06), "Brass", 0.015)
    box((0.42, 0.24, 0.03), (0, 0, 0.1), "Rubber", 0.006)                    # socket recess surround
    box((0.4, 0.1, 0.012), (0, 0.33, 0.105), "Terracotta", 0.003)            # plaque
    cyl(0.07, 0.03, (0.18, -0.28, 0.11), "Brass", axis="z", verts=24, bevel=0.006)
    cyl(0.055, 0.01, (0.18, -0.28, 0.13), "Ceramic", axis="z", verts=24, bevel=0.0)
    for x in (-0.33, 0.33):                                                  # bolts
        for y in (-0.47, 0.47):
            cyl(0.02, 0.03, (x, y, 0.12), "Steel", axis="z", verts=8, bevel=0.0)


def map_table():
    """Command map table: slate top (1.6 x 1.0, surface y 0.88) in a brass rim, terracotta apron, turned brass pedestal."""
    box((1.6, 0.06, 1.0), (0, 0.85, 0), "Slate", 0.02)
    for (sx, sz, x, z) in ((1.66, 0.04, 0, 0.52), (1.66, 0.04, 0, -0.52), (0.04, 1.06, 0.82, 0), (0.04, 1.06, -0.82, 0)):
        box((sx, 0.06, sz), (x, 0.87, z), "Brass", 0.012)
    box((1.48, 0.1, 0.88), (0, 0.77, 0), "Terracotta", 0.02)
    cyl(0.1, 0.68, (0, 0.38, 0), "Brass", axis="y", verts=24)
    for y in (0.12, 0.7):
        torus(0.13, 0.03, (0, y, 0), "Brass", axis="y", major_seg=24, minor_seg=8)
    cyl(0.16, 0.06, (0, 0.06, 0), "Brass", axis="y", verts=24)
    box((1.1, 0.06, 0.14), (0, 0.04, 0), "Slate", 0.015)
    box((0.14, 0.06, 0.8), (0, 0.04, 0), "Slate", 0.015)
    for (x, z) in ((0.55, 0), (-0.55, 0), (0, 0.4), (0, -0.4)):
        cyl(0.07, 0.05, (x, 0.05, z), "Brass", axis="y", verts=16)


def listening_booth():
    """Research listening booth, open to +X: terracotta shell with brass edges, an arch crown over the opening,
    quilted acoustic panels high on the inside walls (kept clear of the tally marks, terminal and headphones)."""
    box((0.1, 2.6, 2.6), (-1.6, 1.3, 0), "Terracotta", 0.02)
    for z in (-1.3, 1.3):
        box((2.4, 2.6, 0.1), (-0.4, 1.3, z), "Terracotta", 0.02)
        box((0.14, 2.7, 0.16), (0.82, 1.33, z), "Brass", 0.02)                   # front edge posts
        box((2.5, 0.12, 0.14), (-0.4, 0.06, z), "Slate", 0.015)                   # plinth
        box((2.5, 0.06, 0.14), (-0.4, 1.0, z), "Brass", 0.01)                     # rail
    box((0.14, 0.12, 2.7), (-1.6, 0.06, 0), "Slate", 0.015)
    box((2.6, 0.08, 2.8), (-0.4, 2.64, 0), "Slate", 0.02)                         # roof
    box((2.66, 0.08, 2.86), (-0.4, 2.72, 0), "Brass", 0.015)                      # cornice
    torus(1.22, 0.06, (0.84, 1.55, 0), "Brass", axis="x", major_seg=36, minor_seg=10, half=True)
    box((0.06, 0.06, 2.6), (0.84, 1.55, 0), "Brass", 0.01)                        # arch spring line
    cyl(0.14, 0.06, (0.84, 2.78, 0), "Cyan", axis="x", verts=20, bevel=0.0)       # keystone light
    # quilted acoustic panels (upper band only)
    for iy in range(3):
        y = 1.9 + iy * 0.22
        for iz in range(11):
            box((0.04, 0.19, 0.19), (-1.53, y, -1.0 + iz * 0.2), "Slate", 0.02, segments=3)
        for ix in range(10):
            box((0.19, 0.19, 0.04), (-1.35 + ix * 0.2, y + 0.05, 1.23), "Slate", 0.02, segments=3)
            if y + 0.05 > 2.0:
                box((0.19, 0.19, 0.04), (-1.35 + ix * 0.2, y + 0.05, -1.23), "Slate", 0.02, segments=3)
    cyl(0.18, 0.08, (-0.6, 2.56, 0), "Brass", axis="y", verts=20)                 # lamp housing
    cyl(0.14, 0.02, (-0.6, 2.51, 0), "Ceramic", axis="y", verts=20, bevel=0.0)


def lift_cage():
    """Brass cage lift on a 1.8 m platform, front (call panel side) = -Z open: fluted corner posts, scissor lattice
    on the other three sides, cornice and a pulley crown with cables."""
    box((1.8, 0.1, 1.8), (0, 0.05, 0), "Brass", 0.02)
    for i in range(7):
        box((1.7, 0.012, 0.05), (0, 0.105, -0.75 + i * 0.25), "Slate", 0.0)          # floor grate lines
    for (x, z) in ((-0.85, -0.85), (0.85, -0.85), (-0.85, 0.85), (0.85, 0.85)):
        cyl(0.06, 3.0, (x, 1.5, z), "Brass", axis="y", verts=16)
        for y in (0.2, 1.0, 2.0, 2.9):
            torus(0.075, 0.018, (x, y, z), "Brass", axis="y", major_seg=16, minor_seg=6)
    # rails + scissor lattice on +X, -X and +Z
    sides = [((0.85, 0), "z"), ((-0.85, 0), "z"), ((0, 0.85), "x")]
    for (cx, cz), along in sides:
        for y in (1.0, 2.0):
            if along == "z":
                cyl(0.025, 1.7, (cx, y, cz), "Brass", axis="z", verts=10)
            else:
                cyl(0.025, 1.7, (cx, y, cz), "Brass", axis="x", verts=10)
        for k in range(4):
            off = -0.64 + k * 0.425
            for sgn in (-1, 1):
                if along == "z":
                    box((0.02, 0.62, 0.03), (cx, 1.5, cz + off), "Brass", 0.0, rot=(sgn * 35, 0, 0))
                else:
                    box((0.03, 0.62, 0.02), (cx + off, 1.5, cz), "Brass", 0.0, rot=(0, 0, sgn * 35))
    box((1.9, 0.08, 1.9), (0, 3.02, 0), "Slate", 0.02)
    box((2.0, 0.1, 2.0), (0, 3.1, 0), "Brass", 0.02)
    box((1.5, 0.12, 1.5), (0, 3.2, 0), "Terracotta", 0.02)
    torus(0.26, 0.04, (0, 3.6, 0), "Brass", axis="x", major_seg=28, minor_seg=8)
    cyl(0.08, 0.14, (0, 3.6, 0), "Steel", axis="x", verts=16)
    box((0.08, 0.4, 0.08), (0, 3.4, 0), "Brass", 0.01)
    for z in (-0.24, 0.24):
        cyl(0.012, 1.6, (0, 4.4, z), "Steel", axis="y", verts=6, bevel=0.0)


# ------------------------------------------------------------------ art pass 4.4: leftovers

def lander_cradle():
    """Empty survey-lander cradle, length along Z (3.0 m), floor pivot. Hazard-edged deck plate, brass rails,
    four empty landing-gear rams and a rear umbilical gantry (+Z) with cut, hanging lines."""
    box((2.2, 0.04, 3.2), (0, 0.02, 0), "Slate", 0.01)
    for x in (-1.06, 1.06):                                                   # hazard edges (terracotta + slate chevrons)
        box((0.1, 0.012, 3.2), (x, 0.045, 0), "Terracotta", 0.0)
        for i in range(12):
            box((0.035, 0.01, 0.16), (x, 0.052, -1.45 + i * 0.265), "Slate", 0.0, rot=(0, 45, 0))
    for z in (-1.56, 1.56):
        box((2.2, 0.012, 0.1), (0, 0.045, z), "Terracotta", 0.0)
    for x in (-0.75, 0.75):                                                   # rails with bolts
        box((0.14, 0.14, 3.0), (x, 0.11, 0), "Brass", 0.02)
        for i in range(6):
            cyl(0.022, 0.03, (x, 0.19, -1.25 + i * 0.5), "Steel", axis="y", verts=8, bevel=0.0)
    for z in (-1.2, -0.4, 0.4, 1.2):                                          # cross ties
        box((1.36, 0.07, 0.14), (0, 0.07, z), "Slate", 0.01)
    for x in (-0.75, 0.75):                                                   # landing-gear rams (empty cups)
        for z in (-0.95, 0.95):
            box((0.32, 0.22, 0.32), (x, 0.3, z), "Steel", 0.02)
            cyl(0.075, 0.5, (x, 0.65, z), "Brass", axis="y", verts=16)
            torus(0.085, 0.02, (x, 0.45, z), "Brass", axis="y", major_seg=16, minor_seg=6)
            cyl(0.04, 0.35, (x, 1.05, z), "Steel", axis="y", verts=12, bevel=0.0)
            cyl(0.19, 0.06, (x, 1.24, z), "Terracotta", axis="y", verts=20)
            cyl(0.13, 0.065, (x, 1.25, z), "Rubber", axis="y", verts=16, bevel=0.0)
    for x in (-1.0, 1.0):                                                     # rear gantry
        box((0.16, 2.7, 0.16), (x, 1.35, 1.65), "Brass", 0.02)
        box((0.34, 0.12, 0.34), (x, 0.06, 1.65), "Slate", 0.015)
    box((2.3, 0.18, 0.2), (0, 2.7, 1.65), "Brass", 0.02)
    box((2.0, 0.08, 0.12), (0, 2.56, 1.65), "Terracotta", 0.01)
    for (x, length) in ((-0.45, 0.9), (0.05, 1.25), (0.5, 0.7)):             # cut umbilicals with plug ends
        cyl(0.045, length, (x, 2.6 - length / 2, 1.65), "Rubber", axis="y", verts=10, bevel=0.0)
        cyl(0.06, 0.1, (x, 2.6 - length - 0.03, 1.65), "Terracotta", axis="y", verts=10, bevel=0.0)


def pod_cradle():
    """Escape-pod launch collar, ring axis = Z, front (hall) = -Z, floor pivot. Open clamp jaws (none at the top:
    the ESCAPE POD 1 label sits above), launch-tube mouth behind, side pylons with rams, pedestal."""
    torus(1.05, 0.12, (0, 1.35, 0), "Brass", axis="z", major_seg=40, minor_seg=10)
    torus(0.9, 0.045, (0, 1.35, -0.06), "Steel", axis="z", major_seg=36, minor_seg=6)
    cyl(1.08, 0.1, (0, 1.35, 0.22), "Slate", axis="z", verts=40)              # tube mouth
    cyl(0.86, 0.04, (0, 1.35, 0.16), "Rubber", axis="z", verts=36, bevel=0.0)
    for k in range(4):                                                         # guide rails inside the tube
        a = math.radians(45 + k * 90)
        box((0.06, 0.06, 0.12), (math.cos(a) * 0.8, 1.35 + math.sin(a) * 0.8, 0.12), "Brass", 0.0)
    for deg in (0, 40, 140, 180, 220, 320):                                    # open clamp jaws
        a = math.radians(deg)
        box((0.26, 0.14, 0.2), (math.cos(a) * 1.22, 1.35 + math.sin(a) * 1.22, -0.02), "Terracotta", 0.015, rot=(0, 0, deg + 18))
        cyl(0.03, 0.24, (math.cos(a) * 1.13, 1.35 + math.sin(a) * 1.13, -0.02), "Steel", axis="z", verts=8, bevel=0.0)
    box((1.3, 0.3, 0.8), (0, 0.15, 0.05), "Slate", 0.02)                       # pedestal + saddle
    box((1.36, 0.06, 0.86), (0, 0.31, 0.05), "Brass", 0.01)
    box((0.7, 0.12, 0.5), (0, 0.38, -0.02), "Terracotta", 0.02)
    for x in (-1.36, 1.36):                                                    # pylons with rams
        box((0.18, 2.1, 0.34), (x, 1.05, 0.08), "Slate", 0.02)
        box((0.24, 0.08, 0.4), (x, 2.12, 0.08), "Brass", 0.01)
        box((0.26, 0.1, 0.42), (x, 0.05, 0.08), "Brass", 0.01)
        cyl(0.055, 0.2, (x * 0.92, 1.35, 0.0), "Steel", axis="x", verts=12, bevel=0.0)
        cyl(0.08, 0.06, (x * 0.97, 1.35, 0.0), "Brass", axis="x", verts=12)


def hatch_rivets():
    """Twelve low-poly rivet heads on a 0.92 m circle in the XY plane, front = -Z (replaces Unity spheres)."""
    for i in range(12):
        a = math.radians(i * 30)
        x, y = math.cos(a) * 0.92, math.sin(a) * 0.92
        cyl(0.035, 0.022, (x, y, -0.04), "Brass", axis="z", verts=8, bevel=0.0)            # stepped head, no bevel
        cyl(0.022, 0.02, (x, y, -0.06), "Brass", axis="z", verts=8, bevel=0.0)


def mug():
    cyl(0.042, 0.1, (0, 0.05, 0), "Ceramic", axis="y", verts=16, bevel=0.004)
    cyl(0.036, 0.004, (0, 0.098, 0), "Rubber", axis="y", verts=16, bevel=0.0)   # coffee
    torus(0.028, 0.008, (0.05, 0.055, 0), "Ceramic", axis="z", major_seg=12, minor_seg=6)


def chess_set():
    """Travel chess board mid-game: 0.3 m board (8x8), a handful of pieces."""
    box((0.32, 0.025, 0.32), (0, 0.0125, 0), "Brass", 0.004)
    box((0.3, 0.004, 0.3), (0, 0.027, 0), "Slate", 0.0)
    for i in range(8):
        for j in range(8):
            if (i + j) % 2 == 0:
                box((0.0375, 0.002, 0.0375), (-0.13125 + i * 0.0375, 0.03, -0.13125 + j * 0.0375), "Ceramic", 0.0)
    pieces = [(-1, 2, "Ceramic", 0.05), (1, 1, "Ceramic", 0.04), (3, -2, "Ceramic", 0.065), (-2, -1, "Rubber", 0.04),
              (0, -3, "Rubber", 0.06), (2, 3, "Rubber", 0.045), (-3, 0, "Rubber", 0.07)]
    for (i, j, m, h) in pieces:
        x, z = -0.01875 + i * 0.0375, -0.01875 + j * 0.0375
        cyl(0.012, 0.008, (x, 0.036, z), m, axis="y", verts=10, bevel=0.0)
        cyl(0.007, h, (x, 0.032 + h / 2, z), m, axis="y", verts=8, bevel=0.0)
        sphere(0.01, (x, 0.034 + h, z), m, segments=8, rings=5)


def photo_frame():
    """Standing photo frame, front = -Z, tilted back."""
    box((0.15, 0.19, 0.012), (0, 0.1, 0), "Brass", 0.004, rot=(-12, 0, 0))
    box((0.12, 0.155, 0.004), (0, 0.1, -0.008), "Ceramic", 0.0, rot=(-12, 0, 0))
    box((0.07, 0.09, 0.003), (-0.015, 0.09, -0.011), "Terracotta", 0.0, rot=(-12, 0, 0))   # faded figures
    box((0.012, 0.16, 0.012), (0, 0.075, 0.05), "Brass", 0.0, rot=(25, 0, 0))               # stand


def file_stack():
    """Three patient folders and loose sheets."""
    for k, (m, yaw) in enumerate((("Terracotta", 4), ("Ceramic", -7), ("Slate", 11))):
        box((0.23, 0.014, 0.31), (0.005 * k, 0.008 + k * 0.016, 0.004 * k), m, 0.002, rot=(0, yaw, 0))
    box((0.21, 0.002, 0.297), (0.02, 0.056, -0.01), "Ceramic", 0.0, rot=(0, -15, 0))


def paper_spread():
    """Spectrogram printouts scattered over ~0.6 x 0.5 m (Rhee's desk)."""
    sheets = [(-0.15, 0.0, -12), (0.06, -0.12, 9), (0.16, 0.1, 24), (-0.05, 0.15, -3), (0.2, -0.17, -30)]
    for k, (x, z, yaw) in enumerate(sheets):
        box((0.21, 0.002, 0.297), (x, 0.001 + k * 0.0022, z), "Ceramic", 0.0, rot=(0, yaw, 0))
        for line in range(4):                                                  # trace lines
            box((0.17, 0.0008, 0.006), (x, 0.0025 + k * 0.0022, z - 0.09 + line * 0.06), "Slate", 0.0, rot=(0, yaw, 0))


def sample_rack():
    """Brass rack with five glass vials of teal ice samples."""
    box((0.3, 0.03, 0.08), (0, 0.015, 0), "Brass", 0.004)
    box((0.3, 0.02, 0.08), (0, 0.09, 0), "Brass", 0.004)
    for x in (-0.14, 0.14):
        box((0.02, 0.1, 0.08), (x, 0.05, 0), "Brass", 0.0)
    for i in range(5):
        x = -0.1 + i * 0.05
        cyl(0.017, 0.13, (x, 0.08, 0), "Glass", axis="y", verts=12, bevel=0.0)
        if i != 3:                                                             # one vial is empty
            cyl(0.011, 0.05, (x, 0.045, 0), "Cyan", axis="y", verts=8, bevel=0.0)
        cyl(0.019, 0.015, (x, 0.15, 0), "Rubber", axis="y", verts=12, bevel=0.0)


def nereth_globe():
    """Desk globe of Nereth on a brass stand."""
    cyl(0.06, 0.015, (0, 0.0075, 0), "Brass", axis="y", verts=20)
    cyl(0.008, 0.09, (0, 0.05, 0), "Brass", axis="y", verts=8, bevel=0.0)
    sphere(0.08, (0, 0.18, 0), "Ceramic", segments=20, rings=12)
    torus(0.09, 0.006, (0, 0.18, 0), "Brass", axis="x", major_seg=28, minor_seg=6)
    torus(0.082, 0.004, (0, 0.18, 0), "Cyan", axis="y", major_seg=24, minor_seg=4)        # the crack belt


def keyboard():
    box((0.42, 0.025, 0.15), (0, 0.0125, 0), "Slate", 0.006)
    for r in range(4):
        box((0.38, 0.008, 0.026), (0, 0.028, -0.05 + r * 0.033), "Steel", 0.002)


def cable_run():
    """Three sagging cables between brackets, 3 m along X, wall at +Z (front = -Z). Hang at y 0 = bracket height."""
    for x in (-1.5, 0.0, 1.5):
        box((0.06, 0.16, 0.14), (x, -0.04, -0.07), "Brass", 0.01)
        cyl(0.018, 0.03, (x, 0.02, -0.14), "Steel", axis="z", verts=8, bevel=0.0)
    for (z, depth, r, m) in ((-0.05, 0.16, 0.022, "Rubber"), (-0.1, 0.24, 0.018, "Rubber"), (-0.15, 0.12, 0.016, "Terracotta")):
        for (a, b) in ((-1.5, 0.0), (0.0, 1.5)):
            cable(sag(a, b, -0.06, z, depth), r, m, resolution=1)


def wall_sconce():
    """Brass wall lamp, back plate at z 0 (wall = +Z), front = -Z; glass shade centre at (0, 0.12, -0.2)."""
    box((0.16, 0.3, 0.03), (0, 0, -0.015), "Brass", 0.008)
    cyl(0.05, 0.03, (0, 0, -0.04), "Terracotta", axis="z", verts=16)
    box((0.03, 0.03, 0.18), (0, 0.03, -0.12), "Brass", 0.004, rot=(-20, 0, 0))
    torus(0.07, 0.012, (0, 0.06, -0.2), "Brass", axis="y", major_seg=16, minor_seg=6)
    cyl(0.03, 0.04, (0, 0.04, -0.2), "Brass", axis="y", verts=12)


def hazard_plate():
    """Floor hazard plate 1.8 x 0.3 m (along X): terracotta with slate chevrons, brass edge."""
    box((1.8, 0.01, 0.3), (0, 0.005, 0), "Terracotta", 0.0)
    for i in range(9):
        box((0.06, 0.004, 0.32), (-0.8 + i * 0.2, 0.011, 0), "Slate", 0.0, rot=(0, 35, 0))
    for z in (-0.16, 0.16):
        box((1.84, 0.014, 0.025), (0, 0.007, z), "Brass", 0.0)


PROPS = [
    ("prop_cryo_pod_base", cryo_pod_base),
    ("prop_cryo_pod_canopy", cryo_pod_canopy),
    ("prop_console_station", console_station),
    ("prop_captain_chair", captain_chair),
    ("prop_valve_wheel", valve_wheel),
    ("prop_valve_body", valve_body),
    ("prop_captain_console", captain_console),
    ("prop_terminal_kiosk", terminal_kiosk),
    ("prop_decode_console", decode_console),
    ("prop_desk", desk),
    ("prop_office_chair", office_chair),
    ("prop_cabinet_case", cabinet_case),
    ("prop_safe_case", safe_case),
    ("prop_pipe_run", pipe_run),
    ("prop_crate", crate),
    ("prop_workbench", workbench),
    ("prop_fuse_box", fuse_box),
    ("prop_map_table", map_table),
    ("prop_listening_booth", listening_booth),
    ("prop_lift_cage", lift_cage),
    ("prop_lander_cradle", lander_cradle),
    ("prop_pod_cradle", pod_cradle),
    ("prop_hatch_rivets", hatch_rivets),
    ("prop_mug", mug),
    ("prop_chess_set", chess_set),
    ("prop_photo_frame", photo_frame),
    ("prop_file_stack", file_stack),
    ("prop_paper_spread", paper_spread),
    ("prop_sample_rack", sample_rack),
    ("prop_nereth_globe", nereth_globe),
    ("prop_keyboard", keyboard),
    ("prop_cable_run", cable_run),
    ("prop_wall_sconce", wall_sconce),
    ("prop_hazard_plate", hazard_plate),
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
