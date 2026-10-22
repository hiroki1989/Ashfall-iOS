"""Validate partition/UV preservation and render the separable-arm authoring candidate."""
import bpy
from pathlib import Path

root = Path("D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/ArmRigCandidate")
bpy.ops.wm.open_mainfile(filepath=str(root / "plain-body-arm-rig-candidate.blend"))
source = next(o for o in bpy.data.objects if o.type == "MESH" and "authored silhouette" in o.name)
parts = [o for o in bpy.data.objects if o.type == "MESH" and o.name.startswith("plain_")]
for obj in bpy.data.objects:
    if obj.type == "MESH" and obj not in parts:
        obj.hide_render = True
assert sum(len(o.data.polygons) for o in parts) == len(source.data.polygons)
source_faces = set()
for obj in parts:
    assert len(obj.data.uv_layers) == 1
    assert obj.data.uv_layers[0].name == "Two view only"
    assert obj.data.materials and any(n.type == "UVMAP" and n.uv_map == "Two view only" for n in obj.data.materials[0].node_tree.nodes)
    assert all(-.001 <= x <= 1.001 for data in obj.data.uv_layers[0].data for x in data.uv)
    ids = {v.value for v in obj.data.attributes["source_vertex"].data}
    assert ids
    if obj.name != "plain_body":
        assert len(obj.vertex_groups) == 1
        assert all(len(v.groups) == 1 for v in obj.data.vertices)
    adj = {i: set() for i in range(len(obj.data.vertices))}
    for edge in obj.data.edges:
        a, b = edge.vertices
        adj[a].add(b)
        adj[b].add(a)
    pending = set(adj)
    sizes = []
    while pending:
        start_vertex = pending.pop()
        seen = {start_vertex}
        stack = [start_vertex]
        while stack:
            for neighbor in adj[stack.pop()] & pending:
                pending.remove(neighbor)
                seen.add(neighbor)
                stack.append(neighbor)
        xs = [obj.data.vertices[i].co.x for i in seen]
        zs = [obj.data.vertices[i].co.z for i in seen]
        sizes.append((len(seen), (round(min(xs), 3), round(max(xs), 3)), (round(min(zs), 3), round(max(zs), 3))))
    print("PART_COMPONENTS", obj.name, sorted(sizes, reverse=True)[:15])
    assert len(sizes) == 1, (obj.name, sizes)
    source_faces.update(ids)
assert len(source_faces) == len(source.data.vertices)
from mathutils import Vector
bpy.ops.object.camera_add(location=(2.8, -4.5, 2.5))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, .95)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 2.6
bpy.context.scene.camera = camera
bpy.ops.object.light_add(type="AREA", location=(0, -3, 4))
bpy.context.object.data.energy = 850
bpy.context.object.data.shape = "DISK"
bpy.context.object.data.size = 3
bpy.context.scene.render.engine = "BLENDER_EEVEE"
bpy.context.scene.render.resolution_x = 768
bpy.context.scene.render.resolution_y = 768
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.filepath = str(root / "candidate-view.png")
bpy.ops.render.render(write_still=True)
print("ARM_CANDIDATE_VALIDATED", len(parts), len(source_faces), bpy.context.scene.camera.name)
