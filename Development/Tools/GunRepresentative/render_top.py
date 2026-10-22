"""Render top and angled QA views of the two-side gun candidate."""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

candidate = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
out = candidate.parent / "qa-v2"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(candidate))
objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
points = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
lo = Vector(min(p[i] for p in points) for i in range(3))
hi = Vector(max(p[i] for p in points) for i in range(3))
center = (lo + hi) / 2
span = max(hi - lo)
world = bpy.data.worlds.new("QA charcoal")
bpy.context.scene.world = world
world.color = (.2, .2, .2)
for offset, energy in (((3,-4,5),800),((-3,2,3),500)):
    bpy.ops.object.light_add(type="AREA", location=center+Vector(offset)*span)
    bpy.context.object.data.energy = energy
    bpy.context.object.data.size = 4*span
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = scene.render.resolution_y = 800
scene.render.image_settings.file_format = "PNG"
for name, offset in (("top", (0,0,3)),("bottom", (0,0,-3)),("three-quarter", (2,-3,2))):
    bpy.ops.object.camera_add(location=center+Vector(offset)*span)
    camera = bpy.context.object
    camera.rotation_euler = (center-camera.location).to_track_quat("-Z","Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = span*1.5
    scene.camera = camera
    scene.render.filepath = str(out / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera,do_unlink=True)
