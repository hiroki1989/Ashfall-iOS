"""Two-view UV projection onto an existing independent GLB mesh.

blender -b --python project_blender.py -- --mesh source.glb --atlas atlas.png
  --mode armor --out candidate.glb --qa qa-directory

Armor: front/back along Y; weapon: left/right along Y. The source GLB must
contain a real three-dimensional mesh. The source file is never modified.
"""
import argparse
import json
from pathlib import Path

import bpy
from mathutils import Vector


def arguments():
    import sys
    parser = argparse.ArgumentParser()
    parser.add_argument("--mesh", type=Path, required=True)
    parser.add_argument("--atlas", type=Path, required=True)
    parser.add_argument("--mode", choices=("armor", "weapon"), required=True)
    parser.add_argument("--armor-side", choices=("neutral", "front-majority"), default="front-majority")
    parser.add_argument("--max-faces", type=int, default=32000)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--qa", type=Path, required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:])


def main():
    args = arguments()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(args.mesh.resolve()))
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise ValueError("Input has no independent mesh")
    if args.max_faces < 1000:
        raise ValueError("max-faces must be at least 1000")
    original_faces = sum(len(o.data.polygons) for o in meshes)
    if original_faces > args.max_faces:
        # Retopologize before projecting. Existing image UVs cannot survive
        # decimation reliably, and the projection is recomputed afterwards.
        ratio = args.max_faces / original_faces
        for obj in meshes:
            if len(obj.data.polygons) < 100:
                continue
            bpy.ops.object.select_all(action="DESELECT")
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            modifier = obj.modifiers.new("iOS face budget", "DECIMATE")
            modifier.ratio = ratio
            bpy.ops.object.modifier_apply(modifier=modifier.name)
    projected_faces = sum(len(o.data.polygons) for o in meshes)
    if projected_faces > args.max_faces * 1.02:
        raise ValueError(f"Decimation exceeded face budget: {projected_faces}")
    # Do not accept flat cards as stand-ins for item geometry.
    coordinates = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    low = Vector(min(v[i] for v in coordinates) for i in range(3))
    high = Vector(max(v[i] for v in coordinates) for i in range(3))
    size = high - low
    if min(size) < max(size) * 0.025:
        raise ValueError(f"Flat source geometry is not an independent model: {tuple(size)}")
    center = (low + high) / 2
    atlas_meta = json.loads(args.atlas.with_suffix(".json").read_text(encoding="utf-8"))
    tile_size = atlas_meta["height"]
    material = bpy.data.materials.new("Approved two-view atlas")
    material.use_nodes = True
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(str(args.atlas.resolve()))
    texture.interpolation = "Linear"
    texture.extension = "EXTEND"
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    neutral = bpy.data.materials.new("Unpainted side and edge metal")
    neutral.diffuse_color = (.105, .12, .13, 1)
    neutral.use_nodes = True
    neutral.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (.105, .12, .13, 1)
    neutral.node_tree.nodes.get("Principled BSDF").inputs["Metallic"].default_value = .35
    face_counts = [0, 0, 0]
    uv_outside = 0
    for obj in meshes:
        inverse_rotation = obj.matrix_world.to_3x3()
        mesh = obj.data
        mesh.materials.clear()
        mesh.materials.append(material)
        mesh.materials.append(neutral)
        uv = mesh.uv_layers.new(name="Two-view projection")
        for polygon in mesh.polygons:
            normal = (inverse_rotation @ polygon.normal).normalized()
            c = obj.matrix_world @ polygon.center
            # A single view per face avoids the double image produced by
            # interpolating incompatible projections over one triangle.
            selected = 0 if normal.y <= 0 else 1
            side_face = abs(normal.x) > max(abs(normal.y) * 1.25, .40)
            # No approved side painting exists for two-view armor. A neutral
            # physical material is safer than wrapping face details around
            # the ear/side, which creates duplicate parts and false seams.
            if args.mode == "armor" and side_face and args.armor_side == "neutral":
                polygon.material_index = 1
                face_counts[2] += 1
                continue
            if args.mode == "armor" and side_face:
                # Put the only side seam near the rear edge. The front view
                # owns 85% of side depth, so front/back details cannot both
                # appear in the middle of the helmet.
                selected = 0 if c.y < low.y + .85 * size.y else 1
            face_counts[selected] += 1
            for loop_index in polygon.loop_indices:
                p = obj.matrix_world @ mesh.vertices[mesh.loops[loop_index].vertex_index].co
                # Both views share exact physical scale. The rear/right view
                # is mirrored so visible details stay in the proper side.
                u = (p.x - low.x) / max(size.x, 1e-8)
                if selected == 1:
                    u = 1 - u
                v = (p.z - low.z) / max(size.z, 1e-8)
                # Map physical extents to the visible illustration extent.
                # The atlas packer expands transparency to opaque edge colors,
                # leaving room for filtering without revealing empty pixels.
                x0, y0, x1, y1 = atlas_meta["views"][selected]["placed_bbox"]
                u = (x0 + 2 + (x1 - x0 - 4) * u) / tile_size
                v = 1 - (y1 - 2 - (y1 - y0 - 4) * v) / tile_size
                uv.data[loop_index].uv = ((selected + u) / 2, v)
                uv_outside += int(not (0 <= u <= 1 and 0 <= v <= 1))
            polygon.material_index = 0
    if uv_outside:
        raise ValueError(f"UV outside atlas: {uv_outside}")
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.qa.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.export_scene.gltf(filepath=str(args.out.resolve()), export_format="GLB", use_selection=True)
    world = bpy.context.scene.world or bpy.data.worlds.new("QA world")
    bpy.context.scene.world = world
    world.color = (.20, .20, .20)
    for location, energy in (((3, -4, 5), 800), ((-3, 2, 3), 500)):
        bpy.ops.object.light_add(type="AREA", location=center + Vector(location) * max(size))
        bpy.context.object.data.energy = energy
        bpy.context.object.data.size = 4 * max(size)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 800
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    distance = max(size) * 3
    views = {"front": (0, -distance, 0), "side": (distance, 0, 0),
             "back": (0, distance, 0)}
    for name, offset in views.items():
        bpy.ops.object.camera_add(location=center + Vector(offset))
        camera = bpy.context.object
        camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = max(size) * 1.5
        scene.camera = camera
        scene.render.filepath = str((args.qa / f"{name}.png").resolve())
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(camera, do_unlink=True)
    report = {"source": str(args.mesh.resolve()), "mode": args.mode,
              "armor_side": args.armor_side,
              "original_faces": original_faces,
              "projected_faces": projected_faces,
              "max_faces": args.max_faces,
              "bounds": list(size), "mesh_count": len(meshes),
              "faces_by_view": face_counts, "uv_outside": uv_outside,
              "candidate": str(args.out.resolve())}
    (args.qa / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
