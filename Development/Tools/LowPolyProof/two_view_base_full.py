"""CPU-only two-view texture reprojection of the approved static sculpt.

Each source is rendered with its existing paint from the two allowed directions.
The captured views are packed with opaque edge bleed, then assigned to the
existing low-poly surface one triangle at a time. This is a visual candidate,
not a replacement for the game's skinned equipment or authored illustrations.
"""
import json
import os
import subprocess
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

ROOT = Path(r"D:/Unity/Ashfall-iOS")
COMPONENT_WRAP = os.environ.get("TWO_VIEW_VARIANT") == "component"
OUT = ROOT / "Assets/ArtSource/LowPolyProof/BodyTwoView"
SOURCE = ROOT / "Assets/ArtSource/LowPolyProof/CleanBase/lowpoly-proof.blend"
PACKER = ROOT / "Development/Tools/TwoView/prepare_atlas.py"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.render.film_transparent = True
scene.view_settings.view_transform = "Standard"

targets = {"body-head": next(o for o in bpy.data.objects if o.type == "MESH" and "authored silhouette" in o.name)}


def bounds(obj):
    points = [obj.matrix_world @ Vector(v) for v in obj.bound_box]
    return Vector(min(v[i] for v in points) for i in range(3)), Vector(max(v[i] for v in points) for i in range(3))


def make_camera(obj, side, weapon):
    lo, hi = bounds(obj)
    center = (lo + hi) / 2
    span = hi - lo
    # The rifle's meaningful silhouette is Y/Z; character's is X/Z.
    width = span.y if weapon else span.x
    scale = max(width, span.z) * 1.08
    offset = Vector((1 if side else -1, 0, 0)) if weapon else Vector((0, 1 if side else -1, 0))
    bpy.ops.object.camera_add(location=center + offset * max(span.length * 3, 2))
    cam = bpy.context.object
    cam.name = "Projection camera"
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = scale
    return cam


def captured_material(obj):
    old = list(obj.data.materials)
    image = None
    for mat in old:
        if mat and mat.use_nodes:
            image = next((n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image), None)
            if image:
                break
    if image is None:
        raise RuntimeError(f"No source texture for {obj.name}")
    mat = bpy.data.materials.new("Unlit source paint / " + obj.name)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = image
    emission = mat.node_tree.nodes.new("ShaderNodeEmission")
    output = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(tex.outputs["Color"], emission.inputs["Color"])
    mat.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return old


def anatomical_region(point):
    """Stable side-wall groups; no additional view images are introduced."""
    x, _depth, height = point
    ax = abs(x)
    if height > 1.56:
        return "head" if ax < .23 else "shoulder"
    if height > 1.10:
        return "arm" if ax > .29 else "torso"
    if height > .79:
        return "glove" if ax > .29 else "waist"
    if height > .39:
        return "thigh" if ax < .28 else "thigh-pouch"
    return "boot"


report = {}
for key, obj in targets.items():
    weapon = key.endswith("rifle")
    old = captured_material(obj)
    hide = {o: o.hide_render for o in bpy.data.objects}
    for other in bpy.data.objects:
        other.hide_render = other is not obj
    views = []
    cameras = []
    for side in range(2):
        cam = make_camera(obj, side, weapon)
        cam.hide_render = False
        scene.camera = cam
        cameras.append(cam)
        output = OUT / f"{key}-source-{side}.png"
        scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True)
        views.append(output)
        cam.hide_render = True
    atlas = OUT / f"{key}-two-view.png"
    subprocess.run(["py", "-3", str(PACKER), "--first", str(views[0]),
                    "--second", str(views[1]), "--out", str(atlas),
                    "--size", "2048", "--occupancy", "0.94"], check=True)
    meta = json.loads(atlas.with_suffix(".json").read_text(encoding="utf-8"))
    obj.data.materials.clear()
    material = bpy.data.materials.new("Two-view reprojection / " + key)
    material.use_nodes = True
    material.node_tree.nodes.clear()
    tex = material.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(atlas))
    bsdf = material.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    out = material.node_tree.nodes.new("ShaderNodeOutputMaterial")
    material.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    material.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    obj.data.materials.append(material)
    uv = obj.data.uv_layers.new(name="Two view only")
    obj.data.uv_layers.active = uv
    uv.active_render = True
    uv_node = material.node_tree.nodes.new("ShaderNodeUVMap")
    uv_node.uv_map = uv.name
    material.node_tree.links.new(uv_node.outputs["UV"], tex.inputs["Vector"])
    counts = [0, 0]
    lo, hi = bounds(obj)
    regions = {}
    if COMPONENT_WRAP and not weapon:
        for vertex in obj.data.vertices:
            p = obj.matrix_world @ vertex.co
            key_region = anatomical_region(p)
            regions.setdefault(key_region, []).append(p.y)
        regions = {k: (min(v), max(v)) for k, v in regions.items()}
    for face in obj.data.polygons:
        normal = obj.matrix_world.to_3x3() @ face.normal
        center = obj.matrix_world @ face.center
        if weapon:
            side = int(normal.x >= 0)
        else:
            side = int(normal.y >= 0)
            # The front image owns most of the profile; one seam near the rear.
            if abs(normal.y) < abs(normal.x) * 1.25:
                side = int(center.y >= lo.y + (hi.y - lo.y) * .85)
        counts[side] += 1
        sidewall = not weapon and abs(normal.x) > abs(normal.y) * 1.25
        camera = cameras[side]
        source_box = meta["views"][side]["source_bbox"]
        placed_box = meta["views"][side]["placed_bbox"]
        for loop_index in face.loop_indices:
            vertex = obj.data.vertices[obj.data.loops[loop_index].vertex_index]
            coord = world_to_camera_view(scene, camera, obj.matrix_world @ vertex.co)
            source_x = coord.x * 1024
            source_y = (1 - coord.y) * 1024
            if COMPONENT_WRAP and sidewall:
                p = obj.matrix_world @ vertex.co
                near, far = regions[anatomical_region(center)]
                # Side depth receives a limited strip from its own limb/body
                # silhouette, rather than collapsing every polygon onto one
                # front-image column. This preserves cloth scale at the edge.
                width = 46 if anatomical_region(center) in ("arm", "thigh", "boot") else 28
                source_x += width * ((p.y - near) / max(far - near, .01) - .5)
            x = placed_box[0] + (source_x - source_box[0]) * (placed_box[2] - placed_box[0]) / max(1, source_box[2] - source_box[0])
            y = placed_box[1] + (source_y - source_box[1]) * (placed_box[3] - placed_box[1]) / max(1, source_box[3] - source_box[1])
            uv.data[loop_index].uv = ((side * 1024 + x) / 2048, 1 - y / 1024)
    for cam in cameras:
        bpy.data.objects.remove(cam, do_unlink=True)
    for other, state in hide.items():
        if other in bpy.data.objects.values():
            other.hide_render = state
    report[key] = {"faces": len(obj.data.polygons), "faces_by_view": counts,
                   "atlas": str(atlas), "source": str(SOURCE)}

scene.render.film_transparent = False
scene.view_settings.view_transform = "AgX"
for name, loc, target, size in [
    ("front-quarter", (3.3, -4.9, 2.7), (.35, 0, .98), 2.7),
    ("side", (-4, -.2, 2.3), (.35, 0, .94), 2.7),
    ("rear-quarter", (3.4, 4.6, 2.6), (.35, 0, .95), 2.7),
    ("gameplay-quarter", (4.2, -5.8, 5.7), (.35, 0, .70), 3.5),
]:
    camera = make_camera(targets["body-head"], 0, False)
    camera.location = loc
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.ortho_scale = size
    scene.camera = camera
    scene.render.filepath = str(OUT / (name + ".png"))
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
scene.render.filepath = ""
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "two-view-proof.blend"))
(OUT / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print("TWO_VIEW_CANDIDATE_OK", json.dumps(report))


