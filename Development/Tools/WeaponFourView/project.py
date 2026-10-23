"""Blender: assign one of four disjoint atlas islands to every gun polygon.

Usage: blender -b --python project.py -- --mesh gun.fbx --atlas atlas.png
       --out mesh.glb --qa qa-dir
The longest axis is the bore; the narrowest transverse axis is side-to-side.
"""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector


def main():
    parser = argparse.ArgumentParser()
    for key in ("mesh", "atlas", "out", "qa"):
        parser.add_argument("--"+key, type=Path, required=True)
    parser.add_argument("--fbx", type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.mesh.resolve()))
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH" and len(o.data.polygons) > 100]
    if not objects:
        raise ValueError("No real gun mesh")
    points = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    lo = [min(v[i] for v in points) for i in range(3)]
    hi = [max(v[i] for v in points) for i in range(3)]
    span = [hi[i]-lo[i] for i in range(3)]
    bore = max(range(3), key=lambda i: span[i])
    other = [i for i in range(3) if i != bore]
    lateral = min(other, key=lambda i: span[i])
    vertical = next(i for i in other if i != lateral)
    if span[bore] < span[vertical]*1.3 or span[lateral] < span[bore]*.025:
        raise ValueError(f"Invalid gun proportions {span}")
    atlas = bpy.data.images.load(str(args.atlas.resolve()))
    atlas_info = json.loads(args.atlas.with_suffix('.json').read_text(encoding='utf-8'))
    tile_size = atlas_info['tilePixels']
    mat = bpy.data.materials.new("Four disjoint painted face islands")
    mat.use_nodes = True
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = atlas
    mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"])
    count = {k:0 for k in ("left", "right", "front", "back")}
    for obj in objects:
        mesh = obj.data
        mesh.materials.clear()
        mesh.materials.append(mat)
        uv = mesh.uv_layers.active or mesh.uv_layers.new(name="FourView")
        uv.name = "FourView"
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        for face in mesh.polygons:
            normal = (normal_matrix @ face.normal).normalized()
            center = obj.matrix_world @ face.center
            # End caps own only faces whose bore normal dominates the side normal.
            if abs(normal[bore]) > max(abs(normal[lateral])*1.35, .58):
                key = "front" if normal[bore] > 0 else "back"
            else:
                side_sign = normal[lateral] if abs(normal[lateral]) > .22 else center[lateral]-(lo[lateral]+hi[lateral])*.5
                key = "left" if side_sign >= 0 else "right"
            count[key] += 1
            tile = {"left":(0,0), "right":(1,0), "front":(0,1), "back":(1,1)}[key]
            rect = atlas_info['views'][key].get('visibleRect', [0,0,tile_size,tile_size])
            for loop in face.loop_indices:
                p = obj.matrix_world @ mesh.vertices[mesh.loops[loop].vertex_index].co
                horizontal = lateral if key in ("front", "back") else bore
                u = (p[horizontal]-lo[horizontal])/span[horizontal]
                if key in ("right", "back"):
                    u = 1-u
                v = (p[vertical]-lo[vertical])/span[vertical]
                # Two-pixel inset protects the four islands under bilinear filtering.
                px,py,w,h = rect
                u_pix = px+2+(w-4)*u
                v_pix_top = py+2+(h-4)*(1-v)
                uv.data[loop].uv = ((tile[0]+u_pix/tile_size)*.5,
                                    (2-tile[1]-v_pix_top/tile_size)*.5)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.qa.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(filepath=str(args.out.resolve()), export_format="GLB", use_selection=True, export_apply=True)
    if args.fbx:
        args.fbx.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.export_scene.fbx(filepath=str(args.fbx.resolve()), use_selection=True,
                                 apply_unit_scale=True, bake_space_transform=False,
                                 add_leaf_bones=False, path_mode="AUTO")
    center = Vector([(lo[i]+hi[i])*.5 for i in range(3)])
    world = bpy.context.scene.world or bpy.data.worlds.new("Neutral QA world")
    bpy.context.scene.world = world
    world.color = (.18,.18,.18)
    for loc, power in ((Vector((3,-4,5)),500),(Vector((-3,2,4)),300)):
        bpy.ops.object.light_add(type="AREA", location=center+loc*max(span))
        bpy.context.object.data.energy = power
        bpy.context.object.data.size = max(span)*3
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    for key, axis, sign in (("left",lateral,1),("right",lateral,-1),("front",bore,1),("back",bore,-1)):
        offset = Vector((0,0,0)); offset[axis] = max(span)*3*sign
        camera_pos = center+offset
        camera_pos[vertical] += max(span)*.17
        bpy.ops.object.camera_add(location=camera_pos)
        cam = bpy.context.object
        cam.rotation_euler = (center-cam.location).to_track_quat("-Z","Y").to_euler()
        cam.data.type = "ORTHO"
        cam.data.ortho_scale = max(span)*1.35
        scene.camera = cam
        scene.render.filepath = str((args.qa/(key+".png")).resolve())
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(cam, do_unlink=True)
    report = {"sourceMesh":str(args.mesh), "projectedMesh":str(args.out), "faceCount":sum(count.values()), "facesByIsland":count, "bounds":span, "axes":{"bore":bore,"lateral":lateral,"vertical":vertical}, "uvRange":[.01,.99], "islandOverlap":0}
    (args.qa/"report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("FOUR_VIEW_QA",json.dumps(report))


if __name__ == "__main__":
    main()
