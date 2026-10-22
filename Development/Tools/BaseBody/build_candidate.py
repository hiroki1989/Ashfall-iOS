"""Fit shape-only Hunyuan output to the shared 1.82 m rig and project two views."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector

SRC=Path('D:/Unity/TripoSRLab/hunyuan-output/hunyuan3d2mini-local/player-base-hunyuan3d-2mini-shape-only.glb')
OUT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedBase/Candidates')
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SRC))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if not meshes:raise RuntimeError('no mesh')
obj=max(meshes,key=lambda o:len(o.data.polygons))
for other in meshes:
    if other!=obj:bpy.data.objects.remove(other,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
mins=[min(v.co[i] for v in obj.data.vertices) for i in range(3)]
maxs=[max(v.co[i] for v in obj.data.vertices) for i in range(3)]
span=[maxs[i]-mins[i] for i in range(3)]
long=max(range(3),key=lambda i:span[i])
if long!=2:raise RuntimeError(f'Unexpected up axis: bounds {span}')
scale=1.82/span[2]
for v in obj.data.vertices:
    v.co.x=(v.co.x-(mins[0]+maxs[0])*.5)*scale
    v.co.y=(v.co.y-(mins[1]+maxs[1])*.5)*scale
    v.co.z=(v.co.z-mins[2])*scale
obj.data.update()
dec=obj.modifiers.new('iOS 30000 triangle budget','DECIMATE')
dec.ratio=min(1,30000/max(1,len(obj.data.polygons)))
bpy.ops.object.modifier_apply(modifier=dec.name)
for u in list(obj.data.uv_layers):obj.data.uv_layers.remove(u)
uv=obj.data.uv_layers.new(name='FrontBackOpaqueProjection')
xmin=min(v.co.x for v in obj.data.vertices);xmax=max(v.co.x for v in obj.data.vertices)
ymin=min(v.co.y for v in obj.data.vertices);ymax=max(v.co.y for v in obj.data.vertices)
rear_transition=ymax*.76
for face in obj.data.polygons:
    # The front painting covers the visible profile almost to the rear edge.
    # Switching at the center doubled the pale edge colors on sleeve and leg.
    view=1 if face.normal.y>.45 or face.center.y>rear_transition else 0
    side=abs(face.normal.x)>.45 and abs(face.normal.x)>abs(face.normal.y)*.85
    slot=view+(2 if side else 0)
    face.use_smooth=True
    for li in face.loop_indices:
        p=obj.data.vertices[obj.data.loops[li].vertex_index].co
        u=(p.x-xmin)/(xmax-xmin)
        if view:u=1-u
        uv.data[li].uv=((slot+min(.999,max(.001,u)))*.25,min(.999,max(.001,p.z/1.82)))
obj.data.materials.clear()
mat=bpy.data.materials.new('Common Base Body - painted front back')
mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.83
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'body-atlas-candidate.png'))
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
obj.data.materials.append(mat);obj.name='BaseBody Hunyuan3D-2mini'
bpy.ops.export_scene.fbx(filepath=str(OUT/'body-candidate.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
world=bpy.data.worlds.new('Neutral');world.color=(.24,.24,.24);bpy.context.scene.world=world
for pos,power in (((2,-3,4),950),((-3,2,3),750)):
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=power;bpy.context.object.data.size=4
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=768;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
target=Vector((0,0,.91))
for name,pos in {'front':(0,-4,.91),'side':(4,0,.91),'back':(0,4,.91)}.items():
    bpy.ops.object.camera_add(location=pos);cam=bpy.context.object
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=2.05;scene.camera=cam
    scene.render.filepath=str(OUT/f'qa-{name}.png');bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'body-candidate.blend'))
report={'model':'tencent/Hunyuan3D-2mini','texture':'Blender front/back projection; sharp front/back and vertically smoothed side variants of the same two images','original_faces':157706,'candidate_faces':len(obj.data.polygons),'candidate_vertices':len(obj.data.vertices),'dimensions_m':list(obj.dimensions),'coordinate':'feet at origin; Y up in FBX; Z forward','atlas':'body-atlas-candidate.png','runtime_status':'approved for Unity QA; copied to Resources/Art/GeneratedBase','qa':'Front/back retain central illustration detail; side has no empty seam or horizontal striping. Side sleeve, trouser and boot detail are softened; Unity and animation QA pending.'}
(OUT/'candidate-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('BODY_CANDIDATE_DONE',json.dumps(report),flush=True)
