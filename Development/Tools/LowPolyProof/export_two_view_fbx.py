"""Export inspected two-view meshes as separate FBX candidates.

These are not wired into gameplay. Keep transforms and UVs from the proof so
the skinning and socket QA can use exactly the inspected shapes.
"""
from pathlib import Path
import json

import bpy

ROOT = Path("D:/Unity/Ashfall-iOS")
ART = ROOT / "Assets/ArtSource/LowPolyProof"
OUTPUT = ART / "ExportCandidates"
OUTPUT.mkdir(parents=True, exist_ok=True)

sources = [
    (ART / "BodyTwoView/two-view-proof.blend", "authored silhouette", "plain-body"),
    (ART / "TwoViewCandidate/two-view-proof.blend", "approved Warden sculpt", "warden-armor"),
    (ART / "TwoViewCandidate/two-view-proof.blend", "approved Warden rifle", "warden-rifle"),
]
report = []
for blend, needle, name in sources:
    bpy.ops.wm.open_mainfile(filepath=str(blend))
    mesh = next(o for o in bpy.data.objects if o.type == "MESH" and needle in o.name)
    selected_uv = mesh.data.uv_layers.get("Two view only")
    if selected_uv is None:
        raise RuntimeError(f"Two-view UV is missing from {name}")
    # FBX/Unity reads UV0, not Blender's selected render layer. Remove the
    # superseded four-direction UV so the export cannot silently use it.
    for layer in list(mesh.data.uv_layers):
        if layer != selected_uv:
            mesh.data.uv_layers.remove(layer)
    mesh.data.uv_layers.active = mesh.data.uv_layers[0]
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    out = OUTPUT / (name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=str(out), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=False, bake_space_transform=False,
                             use_mesh_modifiers=False, path_mode="AUTO")
    report.append({"mesh": name, "faces": len(mesh.data.polygons),
                   "uv": mesh.data.uv_layers.active.name, "fbx": str(out)})

(OUTPUT / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("TWO_VIEW_FBX_CANDIDATES", report)
