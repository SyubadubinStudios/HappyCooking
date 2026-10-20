"""
Happy Cooking - asset pipeline (Blender 5.x)

Turns the raw Rodin GLB exports in assets/raw/models into game-ready GLBs in
src/HappyCooking.App/Assets/Models:

  * normalises scale (characters 1.0 m tall, food/props to a target size),
    origin at the bottom centre, standing on the ground;
  * decimates to a mobile-friendly triangle budget;
  * downsizes textures (characters 1024 px, food 512 px) and re-encodes them as JPEG;
  * characters get a three-bone rig (Root > Body > Head), height-based skin
    weights and the clips Idle / Happy / Talk, exported as glTF animations.

Run headless (from the repo root):
  blender -b --factory-startup -P tools/blender/process_assets.py

The same file can be pasted into Blender MCP's execute_blender_code; it never
resets the session, only removes the objects it imported.
"""
import bpy
import math
import os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(bpy.data.filepath or __file__), "..", ".."))
if not os.path.isdir(os.path.join(ROOT, "assets")):
    ROOT = os.getcwd()
RAW = os.path.join(ROOT, "assets", "raw", "models")
OUT = os.path.join(ROOT, "src", "HappyCooking.App", "Assets", "Models")

# name: (kind, target size in metres, max faces, texture px, neck height ratio)
ASSETS = {
    "chef_happy":      ("character", 1.00, 14000, 1024, 0.52),
    "coco_panda":      ("character", 0.95, 14000, 1024, 0.50),
    "jenggo_chicken":  ("character", 0.95, 14000, 1024, 0.55),
    "mochi_cat":       ("character", 0.90, 14000, 1024, 0.55),
    "pepper_bot":      ("character", 0.85, 12000, 1024, 0.55),
    "burnt_onion":     ("character", 0.90, 12000, 1024, 0.62),
    "cooking_pot":     ("prop",      0.70, 9000,  512,  0),
    "rendang":         ("food",      0.42, 8000,  512,  0),
    "sushi":           ("food",      0.42, 8000,  512,  0),
    "pizza":           ("food",      0.45, 8000,  512,  0),
    "rainbow_cupcake": ("food",      0.36, 8000,  512,  0),
    "dim_sum":         ("food",      0.40, 8000,  512,  0),
    "galaxy_donut":    ("food",      0.38, 8000,  512,  0),
    "taco":            ("food",      0.42, 8000,  512,  0),
}

FPS = 24


def clear_imported():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    # Clips carry a fake user, so drop them explicitly or the next character's
    # clips would be renamed Idle.001, Idle.002, ...
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.armatures, bpy.data.actions):
        for block in list(coll):
            if block.users == 0:
                coll.remove(block)


def import_mesh(path):
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for m in meshes:
        m.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    # Bake parent transforms into the mesh so we start from identity.
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    for o in [o for o in bpy.data.objects if o.type == "EMPTY"]:
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj


def normalise(obj, kind, size):
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    mx = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    dims = mx - mn
    # Characters and props scale by height, food by its largest extent.
    ref = dims.z if kind in ("character", "prop") else max(dims)
    s = size / ref
    centre_bottom = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    me = obj.data
    for v in me.vertices:
        v.co = (v.co - centre_bottom) * s
    me.update()
    obj.location = (0, 0, 0)


def decimate(obj, max_faces):
    faces = len(obj.data.polygons)
    if faces <= max_faces:
        return
    mod = obj.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = max_faces / faces
    mod.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)


def shrink_textures(px):
    for im in bpy.data.images:
        if im.size[0] > px:
            im.scale(px, px)
        im.file_format = "JPEG"


def rig_character(obj, neck_ratio):
    h = obj.dimensions.z
    neck = h * neck_ratio
    arm_data = bpy.data.armatures.new("Rig")
    arm = bpy.data.objects.new("Rig", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    root = arm_data.edit_bones.new("Root")
    root.head, root.tail = (0, 0, 0), (0, 0, h * 0.08)
    body = arm_data.edit_bones.new("Body")
    body.head, body.tail = (0, 0, h * 0.08), (0, 0, neck)
    body.parent = root
    head = arm_data.edit_bones.new("Head")
    head.head, head.tail = (0, 0, neck), (0, 0, h)
    head.parent = body
    head.use_connect = True
    bpy.ops.object.mode_set(mode="OBJECT")

    # Height based weights with a soft blend at the neck: Rodin meshes are
    # rarely manifold, so automatic weights are unreliable on them.
    g_body = obj.vertex_groups.new(name="Body")
    g_head = obj.vertex_groups.new(name="Head")
    band = h * 0.07
    for v in obj.data.vertices:
        t = (v.co.z - (neck - band)) / (2 * band)
        t = max(0.0, min(1.0, t))
        t = t * t * (3 - 2 * t)
        if t < 1:
            g_body.add([v.index], 1 - t, "REPLACE")
        if t > 0:
            g_head.add([v.index], t, "REPLACE")

    obj.parent = arm
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    make_clips(arm, h)
    return arm


def key(pb, frame, loc=None, rot=None, scale=None):
    if loc is not None:
        pb.location = loc
        pb.keyframe_insert("location", frame=frame)
    if rot is not None:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = [math.radians(a) for a in rot]
        pb.keyframe_insert("rotation_euler", frame=frame)
    if scale is not None:
        pb.scale = scale
        pb.keyframe_insert("scale", frame=frame)


def new_action(arm, name):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = action
    return action


def push_to_nla(arm, action):
    track = arm.animation_data.nla_tracks.new()
    track.name = action.name
    track.strips.new(action.name, 1, action)
    arm.animation_data.action = None


def make_clips(arm, h):
    pb = arm.pose.bones
    bpy.context.scene.render.fps = FPS

    # Idle: breathing squash and a lazy head sway (2 s loop).
    a = new_action(arm, "Idle")
    for f, sy, tilt in ((1, 1.0, -3), (13, 1.035, 0), (25, 1.0, 3), (37, 1.035, 0), (49, 1.0, -3)):
        key(pb["Body"], f, scale=(1 / math.sqrt(sy), sy, 1 / math.sqrt(sy)))
        key(pb["Head"], f, rot=(0, 0, tilt))
    push_to_nla(arm, a)

    # Happy: crouch, hop with a full spin, squash on landing (1.25 s).
    a = new_action(arm, "Happy")
    j = h * 0.35
    frames = ((1, 0, 0, 1.0), (5, 0, 0, 0.88), (12, j, 180, 1.08), (19, 0, 360, 0.86), (25, 0, 360, 1.04), (31, 0, 360, 1.0))
    for f, z, spin, sy in frames:
        key(pb["Root"], f, loc=(0, z, 0), rot=(0, spin, 0))
        key(pb["Body"], f, scale=(1 / math.sqrt(sy), sy, 1 / math.sqrt(sy)))
    push_to_nla(arm, a)

    # Talk: quick nods and a head waggle (1 s loop).
    a = new_action(arm, "Talk")
    for f, nod, roll in ((1, 0, 0), (5, 8, 4), (9, -2, -3), (13, 7, 3), (17, -2, -4), (21, 6, 2), (25, 0, 0)):
        key(pb["Head"], f, rot=(nod, 0, roll))
    push_to_nla(arm, a)


def export(name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format="GLB",
        export_image_format="JPEG",
        export_jpeg_quality=85,
        export_animations=True,
        export_animation_mode="NLA_TRACKS",
        export_apply=True,
        export_yup=True,
    )
    return path


def process(name, kind, size, max_faces, tex, neck):
    clear_imported()
    obj = import_mesh(os.path.join(RAW, name + ".glb"))
    normalise(obj, kind, size)
    decimate(obj, max_faces)
    shrink_textures(tex)
    obj.name = name
    if kind == "character":
        rig_character(obj, neck)
    path = export(name)
    print(f"[asset] {name}: {len(obj.data.polygons)} faces, {os.path.getsize(path) // 1024} KiB")


for asset_name, spec in ASSETS.items():
    process(asset_name, *spec)
print("[asset] done")
