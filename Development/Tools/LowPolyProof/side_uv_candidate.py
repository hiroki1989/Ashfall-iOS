"""Four disjoint planar UV projections on a copy of the arm rig authoring candidate.

Run: blender -b -t 4 --python side_uv_candidate.py
This writes only ArmRigSide artifacts; it never changes a runtime Resource.
"""
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path('D:/Unity/Ashfall-iOS')
OUT = ROOT / 'Assets/ArtSource/LowPolyProof/ArmRigSide'
SOURCE = ROOT / 'Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
objects = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith('plain_')]
if len(objects) != 7:
    raise RuntimeError(f'Expected seven separated candidate meshes, got {len(objects)}')

atlas = bpy.data.images.load(str(OUT / 'plain-body-four-view-atlas.png'))
mat = bpy.data.materials.new('Four distinct view paint')
mat.use_nodes = True
nodes = mat.node_tree.nodes
nodes.clear()
uvmap = nodes.new('ShaderNodeUVMap')
uvmap.uv_map = 'Four distinct views'
tex = nodes.new('ShaderNodeTexImage')
tex.image = atlas
tex.interpolation = 'Linear'
emission = nodes.new('ShaderNodeEmission')
output = nodes.new('ShaderNodeOutputMaterial')
mat.node_tree.links.new(uvmap.outputs['UV'], tex.inputs['Vector'])
mat.node_tree.links.new(tex.outputs['Color'], emission.inputs['Color'])
mat.node_tree.links.new(emission.outputs['Emission'], output.inputs['Surface'])

counts = dict(front=0, back=0, left=0, right=0)
for obj in objects:
    mesh = obj.data
    old = mesh.uv_layers.get('Two view only')
    if old is None:
        raise RuntimeError(f'Missing original UV on {obj.name}')
    old_coords = [tuple(loop.uv) for loop in old.data]
    layer = mesh.uv_layers.new(name='Four distinct views')
    mesh.uv_layers.active = layer
    layer.active_render = True
    for poly in mesh.polygons:
        normal = (obj.matrix_world.to_3x3() @ poly.normal).normalized()
        # Only use a profile image on genuinely side-facing polygons. Oblique
        # shoulder, hand and boot-edge faces keep the front/back UV; assigning
        # the profile photo there painted a second arm/leg onto the silhouette.
        if abs(normal.x) > 0.48 and abs(normal.x) > abs(normal.y) * 0.72:
            view = 'left' if normal.x < 0 else 'right'
        else:
            view = 'front' if normal.y < 0 else 'back'
        counts[view] += 1
        for li in poly.loop_indices:
            if view in ('front', 'back'):
                old_u, old_v = old_coords[li]
                # The original 2048 x 1024 front/back atlas occupies the top row.
                u, v = old_u, 0.5 + old_v * 0.5
            else:
                point = obj.matrix_world @ mesh.vertices[mesh.loops[li].vertex_index].co
                # From -X, the front points image-right; from +X it points left.
                depth = max(-0.26, min(0.26, point.y))
                # The profile photo occupies roughly one third of its isolated
                # tile. Map the full scanned body depth into that silhouette;
                # the earlier .36 scale collapsed it into the centre stripe and
                # made the arm and leg motifs appear twice on the side.
                tile_x = 0.485 + (-depth * 0.40 if view == 'left' else depth * 0.40)
                tile_y = max(0.055, min(0.92, point.z / 1.82 * 0.865 + 0.055))
                u = (0 if view == 'left' else 0.5) + tile_x * 0.5
                v = tile_y * 0.5
            if not (0.0 <= u <= 1.0 and 0.0 <= v <= 1.0):
                raise RuntimeError((obj.name, view, u, v))
            layer.data[li].uv = (u, v)
    mesh.materials.clear()
    mesh.materials.append(mat)
    # Export only the new mapping so Unity cannot silently choose an old UV layer.
    mesh.uv_layers.remove(old)

scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world.color = (0.13, 0.13, 0.13)
for o in bpy.data.objects:
    if o.type == 'MESH' and o not in objects:
        o.hide_render = True
for obj in objects:
    obj.hide_render = False
target = Vector((0, 0, 0.91))
for view, location in [('front', (0, -4.0, 0.91)), ('right', (4.0, 0, 0.91)),
                       ('back', (0, 4.0, 0.91)), ('left', (-4.0, 0, 0.91))]:
    camera_data = bpy.data.cameras.new(f'QA {view}')
    camera = bpy.data.objects.new(f'QA {view}', camera_data)
    scene.collection.objects.link(camera)
    camera.location = location
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = 2.05
    scene.camera = camera
    scene.render.filepath = str(OUT / f'qa-{view}.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'plain-body-four-view-candidate.blend'))
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.export_scene.fbx(filepath=str(OUT / 'plain-body-four-view-candidate.fbx'),
                         use_selection=True, object_types={'MESH'},
                         apply_unit_scale=False, bake_space_transform=False,
                         use_mesh_modifiers=False, path_mode='AUTO')
(OUT / 'uv-report.json').write_text(json.dumps({'face_counts': counts, 'faces_total': sum(counts.values()),
    'tiles': {'front': [0, 0, 1024, 1024], 'back': [1024, 0, 2048, 1024],
              'left': [0, 1024, 1024, 2048], 'right': [1024, 1024, 2048, 2048]},
    'status': 'visual candidate; rig pose and seam QA outstanding'}, indent=2), encoding='utf-8')
print('SIDE_UV_OK', counts)
