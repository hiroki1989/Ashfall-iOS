"""Extract articulated plain-body arm candidates from the approved two-view scan.

Run in background Blender. No generated texture or runtime asset is modified.
The face partition follows the outer arm silhouette and keeps each bone's
triangles independent. Open cut boundaries require subsequent seam treatment.
"""
import json
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path("D:/Unity/Ashfall-iOS")
SOURCE = ROOT / "Assets/ArtSource/LowPolyProof/BodyTwoView/two-view-proof.blend"
OUT = ROOT / "Assets/ArtSource/LowPolyProof/ArmRigCandidate"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
source = next(o for o in bpy.data.objects if o.type == "MESH" and "authored silhouette" in o.name)
uv = source.data.uv_layers.get("Two view only")
if uv is None:
    raise RuntimeError("Source two-view projection is missing")


def arm_side(point):
    x, _depth, z = point
    ax = abs(x)
    # Deliberately exclude hip and upper chest faces: these are scanned close
    # to the relaxed hands/shoulders but must stay with the trunk.
    threshold = .235 if z >= 1.34 else .275 if z >= 1.14 else .305
    if .76 <= z <= 1.57 and ax >= threshold:
        return "left" if x < 0 else "right"
    return None


def part_name(point):
    side = arm_side(point)
    if side is None:
        return "body"
    z = point.z
    segment = "upper" if z >= 1.14 else "forearm" if z >= .91 else "hand"
    return side + "_" + segment


faces = {n: [] for n in ("body", "left_upper", "left_forearm", "left_hand", "right_upper", "right_forearm", "right_hand")}
for poly in source.data.polygons:
    center = source.matrix_world @ poly.center
    faces[part_name(center)].append(poly.index)

# Per-face copy keeps the original shape and exact loop UVs. Source-index
# attributes allow the rig author to map each candidate vertex back to scan.
objects = {}
vertex_map = {}
for name, indices in faces.items():
    old_to_new = {}
    vertex_ids = []
    polygons = []
    uv_per_face = []
    for face_index in indices:
        poly = source.data.polygons[face_index]
        polygon = []
        face_uv = []
        for loop_index in poly.loop_indices:
            old_id = source.data.loops[loop_index].vertex_index
            if old_id not in old_to_new:
                old_to_new[old_id] = len(vertex_ids)
                vertex_ids.append(old_id)
            polygon.append(old_to_new[old_id])
            face_uv.append(tuple(uv.data[loop_index].uv))
        polygons.append(polygon)
        uv_per_face.append(face_uv)
    mesh = bpy.data.meshes.new("plain_" + name)
    mesh.from_pydata([tuple(source.data.vertices[i].co) for i in vertex_ids], [], polygons)
    mesh.update()
    # Keep the exact name because the source material's UV Map node explicitly
    # references it. Renaming it made Blender sample the wrong coordinates.
    layer = mesh.uv_layers.new(name="Two view only")
    for poly, face_uv in zip(mesh.polygons, uv_per_face):
        for li, coord in zip(poly.loop_indices, face_uv):
            layer.data[li].uv = coord
    obj = bpy.data.objects.new("plain_" + name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.matrix_world = source.matrix_world.copy()
    attr = mesh.attributes.new("source_vertex", "INT", "POINT")
    for new_id, old_id in enumerate(vertex_ids):
        attr.data[new_id].value = old_id
    if name != "body":
        group = obj.vertex_groups.new(name=name)
        group.add(list(range(len(vertex_ids))), 1.0, "REPLACE")
    if source.data.materials:
        mesh.materials.append(source.data.materials[0])
    objects[name] = obj
    vertex_map[name] = vertex_ids

source.hide_render = True
source.hide_set(True)
landmarks = {
    "coordinate_system": "Blender Z-up metres; Unity=(x,z,y) after the established FBX import transform",
    "source_blend": str(SOURCE),
    "texture": "BodyTwoView/body-head-two-view.png",
    "uv_layer": "Two view only",
    "joint_centres_blender": {
        "left_shoulder": [-.245, .055, 1.427], "left_elbow": [-.345, .055, 1.142], "left_wrist": [-.37, .035, .877],
        "right_shoulder": [.245, .055, 1.427], "right_elbow": [.345, .055, 1.142], "right_wrist": [.37, .035, .877],
    },
    "target_rig_unity": {
        "left_shoulder": [-.18, 1.427, 0], "left_elbow": [-.18, 1.142, 0], "left_wrist": [-.18, .877, 0],
        "right_shoulder": [.18, 1.427, 0], "right_elbow": [.18, 1.142, 0], "right_wrist": [.18, .877, 0],
    },
    "source_vertex_attribute": "source_vertex",
    "parts": {name: {"vertices": len(obj.data.vertices), "faces": len(obj.data.polygons)} for name, obj in objects.items()},
    "status": "authoring candidate, not approved for runtime; pose and seam QA required",
}
for name, xyz in landmarks["joint_centres_blender"].items():
    empty = bpy.data.objects.new("pivot_" + name, None)
    bpy.context.collection.objects.link(empty)
    empty.location = xyz
    empty.empty_display_size = .035

(OUT / "landmarks.json").write_text(json.dumps(landmarks, indent=2), encoding="utf-8")
(OUT / "vertex-map.json").write_text(json.dumps({
    "description": "Candidate local vertex index to source scan vertex index; each arm part has rigid weight 1 to its named bone",
    "parts": vertex_map,
}, separators=(",", ":")), encoding="utf-8")
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "plain-body-arm-rig-candidate.blend"))
bpy.ops.object.select_all(action="DESELECT")
for obj in objects.values():
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects["body"]
bpy.ops.export_scene.fbx(filepath=str(OUT / "plain-body-arm-rig-candidate.fbx"), use_selection=True,
                         object_types={"MESH"}, apply_unit_scale=False, bake_space_transform=False,
                         use_mesh_modifiers=False, path_mode="AUTO")
print("ARM_CANDIDATE", landmarks["parts"])
