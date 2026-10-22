import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelHarrierPatrol');O=R/'ExportCandidate';O.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(R/'kestrel-harrier-two-side-candidate-v2.glb'));objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world@v.co for o in objs for v in o.data.vertices];lo=Vector(min(p[k] for p in pts) for k in range(3));hi=Vector(max(p[k] for p in pts) for k in range(3));print('SOURCE_BOUNDS',list(lo),list(hi));faces=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in objs);assert faces<=32000
atlas=bpy.data.images.load(str(R/'kestrel-harrier-two-side-atlas-v2.png'),check_existing=False);mat=bpy.data.materials.new('Kestrel Harrier authored two-side paint');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=atlas;e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(t.outputs['Color'],e.inputs[0]);mat.node_tree.links.new(e.outputs[0],out.inputs[0])
# Preserve UV and independent Hunyuan geometry; set physical overall length 1.0m.
scale=1/(hi.x-lo.x);centre=(hi+lo)/2
for o in objs:
 for v in o.data.vertices:v.co=((o.matrix_world@v.co)-centre)*scale
 o.matrix_world.identity();o.data.materials.clear();o.data.materials.append(mat)
landmarks={'trigger':(-.110,0,-.018),'support':(.170,0,.015),'muzzle':(.500,0,.078),'stock':(-.480,0,-.015)}
for name,coordinate in landmarks.items():
 empty=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(empty);empty.location=coordinate;empty.empty_display_type='SPHERE';empty.empty_display_size=.012
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1000;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world=bpy.data.worlds.new('QA');scene.world.color=(.15,.15,.15)
for name,loc in [('left',(0,-3,0)),('right',(0,3,0)),('quarter',(1.3,-3,1.1))]:
 d=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,d);scene.collection.objects.link(cam);cam.location=loc;cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=1.15;scene.camera=cam;scene.render.filepath=str(O/(name+'.png'));bpy.ops.render.render(write_still=True)
# Unity FBX import mirrors Blender X. Rotate source180deg around Z so
# the final imported weapon points +X, applying the same frame to markers.
for obj in objs:
 for vertex in obj.data.vertices:vertex.co.x=-vertex.co.x;vertex.co.y=-vertex.co.y
landmarks={key:(-value[0],-value[1],value[2]) for key,value in landmarks.items()}
for name,coordinate in landmarks.items():bpy.data.objects[name].location=coordinate
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'kestrel-harrier.blend'));bpy.ops.object.select_all(action='DESELECT')
for o in objs:o.select_set(True)
for name in landmarks:bpy.data.objects[name].select_set(True)
bpy.context.view_layer.objects.active=objs[0];bpy.ops.export_scene.fbx(filepath=str(O/'mesh.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=False,bake_space_transform=False,use_mesh_modifiers=False,path_mode='AUTO',axis_forward='-Z',axis_up='Y')
r={'product':'distinct.gun.030','family':'Kestrel Harrier Patrol','shape_source':str(R/'kestrel-harrier-shape-only.glb'),'textured_source':str(R/'kestrel-harrier-two-side-candidate-v2.glb'),'atlas':str(R/'kestrel-harrier-two-side-atlas-v2.png'),'triangles':faces,'source_bounds':[list(lo),list(hi)],'scale_to_meters':scale,'dimensions_blender_m':list((hi-lo)*scale),'axis':'Final FBX source Blender -X barrel/Z up, Unity import +X barrel/Y up; source rotated180deg before export.','uv':'Unchanged from two-side-v2 GLB','opposite_side':'Mirrored existing illustration, no independently approved reverse illustration','landmarks_blender_m':landmarks,'landmarks_unity_m_assuming_axis_conversion':{k:[-v[0],v[2],v[1]] for k,v in landmarks.items()},'status':'Exported candidate; Unity grip QA and visual acceptance required'};(O/'manifest.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))
