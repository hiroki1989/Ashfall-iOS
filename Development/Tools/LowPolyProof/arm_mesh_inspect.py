"""Inspect the existing plain-body candidate for an arm topology pass."""
import bpy
from mathutils import Vector

bpy.ops.wm.open_mainfile(filepath="D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/BodyTwoView/two-view-proof.blend")
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    if "authored silhouette" not in obj.name:
        continue
    mesh = obj.data
    coords = [obj.matrix_world @ v.co for v in mesh.vertices]
    print("ARM_INSPECT", obj.name, "vertices", len(mesh.vertices), "faces", len(mesh.polygons))
    for z0, z1 in [(1.35, 1.6), (1.1, 1.35), (.85, 1.1)]:
        for x0, x1 in [(.2, .3), (.3, .45), (.45, .7)]:
            p = [c for c in coords if z0 <= c.z < z1 and x0 <= c.x < x1]
            if p:
                print("BIN", z0, z1, x0, x1, len(p), "x", min(c.x for c in p), max(c.x for c in p), "y", min(c.y for c in p), max(c.y for c in p))
    print("UV", [(u.name, len(u.data)) for u in mesh.uv_layers])
    print("MATERIALS", [(m.name if m else None, [(n.type, n.image.filepath if n.type == 'TEX_IMAGE' and n.image else None) for n in m.node_tree.nodes] if m and m.use_nodes else []) for m in mesh.materials])
    for m in mesh.materials:
        if m and m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == "UVMAP": print("UVNODE", n.uv_map)
            for l in m.node_tree.links: print("MAT_LINK",l.from_node.type,l.from_socket.name,"->",l.to_node.type,l.to_socket.name)
    for layer in mesh.uv_layers:
        coords = [tuple(layer.data[i].uv) for i in range(len(layer.data))]
        print("UV_RANGE", layer.name, min(u for u,v in coords), max(u for u,v in coords), min(v for u,v in coords), max(v for u,v in coords))
