"""Review the two-view plain body with one Warden equipment slot at a time."""
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path("D:/Unity/Ashfall-iOS")
SOURCE = ROOT / "Assets/ArtSource/LowPolyProof/BodyTwoView/two-view-proof.blend"
PARTS = ROOT / "Assets/ArtSource/LowPolyProof/SlotProof"
OUT = PARTS / "qa"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
for obj in list(scene.objects):
    if obj.type == "MESH" and "rifle" in obj.name.lower():
        bpy.data.objects.remove(obj, do_unlink=True)

bpy.ops.object.camera_add(location=(3.3, -4.9, 2.7))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 1)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 2.7
scene.camera = camera
scene.render.film_transparent = False
scene.render.image_settings.file_format = "PNG"
scene.render.resolution_x = scene.render.resolution_y = 1024

for name in ("chest", "arms", "legs", "feet"):
    old = set(scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(PARTS / (name + ".glb")))
    added = [obj for obj in scene.objects if obj not in old]
    scene.render.filepath = str(OUT / (name + "-only.png"))
    bpy.ops.render.render(write_still=True)
    for obj in added:
        bpy.data.objects.remove(obj, do_unlink=True)
print("WARDEN_PARTIAL_QA_OK")
