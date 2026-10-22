"""Split the two-view Warden proof into inspectable equipment meshes.

The source remains intact. This is a static geometry QA artifact; Unity's
GeneratedWardenRig supplies the actual skinned, animated slot renderers.
"""
from pathlib import Path
import json

import bpy
import bmesh
from mathutils import Vector

ROOT = Path("D:/Unity/Ashfall-iOS")
OUT = ROOT / "Assets/ArtSource/LowPolyProof/SlotProof"
SOURCE = ROOT / "Assets/ArtSource/LowPolyProof/TwoViewCandidate/two-view-proof.blend"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
armor = next(o for o in bpy.data.objects if o.type == "MESH" and "approved Warden sculpt" in o.name)


def slot(center):
    x, _, height = center
    if height < .40:
        return "feet"
    if height < .91:
        return "legs"
    # The hands reach below the belt. Put outer lower arms in the arm slot,
    # while pouches close to the body's waist stay with the chest.
    if abs(x) > .32 and height > .91:
        return "arms"
    return "chest"


membership = {}
for polygon in armor.data.polygons:
    center = armor.matrix_world @ polygon.center
    membership[polygon.index] = slot(center)

counts = {}
for name in ("chest", "arms", "legs", "feet"):
    obj = armor.copy()
    obj.data = armor.data.copy()
    obj.name = "Warden / " + name + " / two-view proof"
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    doomed = [face for face in bm.faces if membership[face.index] != name]
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    counts[name] = len(bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(
        filepath=str(OUT / (name + ".glb")),
        export_format="GLB", use_selection=True,
    )
    obj.hide_render = True

(OUT / "report.json").write_text(json.dumps({"source": str(SOURCE), "faces": counts,
    "note": "Static part separation only; animation remains in GeneratedWardenRig."}, indent=2), encoding="utf-8")
print("WARDEN_SLOT_PROOF", counts, "total", sum(counts.values()))
