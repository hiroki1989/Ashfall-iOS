import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet');O=R/'Candidates/TwoViewOnly';source=Path('D:/Unity/AshfallArtValidation/equipment3d-batch/kestrel-field-helmet/model-output/kestrel-field-helmet-v2-shape-only.glb')
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source));objects=[o for o in bpy.data.objects if o.type=='MESH'];original=sum(len(o.data.polygons) for o in objects)
for obj in objects:
 bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 dec=obj.modifiers.new('CPU mobile simplification','DECIMATE');dec.ratio=min(1,31990/original);bpy.ops.object.modifier_apply(modifier=dec.name);obj.select_set(False)
pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices];lo=Vector(min(p[k] for p in pts) for k in range(3));hi=Vector(max(p[k] for p in pts) for k in range(3));size=hi-lo;centre=(hi+lo)/2;target=Vector((.245,.260,.285))
for obj in objects:
 for v in obj.data.vertices:
  p=obj.matrix_world@v.co;v.co=Vector((p[k]-centre[k])*target[k]/size[k] for k in range(3))
 obj.matrix_world.identity();obj.data.update()
metadata=json.loads((O/'atlas-manifest.json').read_text());counts=[0,0];uv_out=0
for obj in objects:
 m=obj.data
 for uv in list(m.uv_layers):m.uv_layers.remove(uv)
 layer=m.uv_layers.new(name='Front back fixed exclusive UV')
 for f in m.polygons:
  # One paint owner per face, with one seam near the rear of side walls.
  side=int(f.normal.y>=0)
  if abs(f.normal.y)<abs(f.normal.x)*1.25:side=int(f.center.y>=-.13+.26*.85)
  counts[side]+=1
  box=metadata['views'][side]['placed_bbox']
  for li in f.loop_indices:
   p=m.vertices[m.loops[li].vertex_index].co;u=(p.x+.1225)/.245
   if side:u=1-u
   v=(p.z+.1425)/.285
   x=box[0]+2+(box[2]-box[0]-4)*u;y=box[3]-2-(box[3]-box[1]-4)*v
   coord=((side*1024+x)/2048,1-y/1024);uv_out+=int(not(0<=coord[0]<=1 and 0<=coord[1]<=1));layer.data[li].uv=coord
  f.use_smooth=True
atlas=bpy.data.images.load(str(O/'helmet-front-back-atlas.png'));mat=bpy.data.materials.new('Two approved views only');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=atlas;e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(tex.outputs['Color'],e.inputs[0]);mat.node_tree.links.new(e.outputs[0],out.inputs[0])
for o in objects:o.data.materials.clear();o.data.materials.append(mat)
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world=bpy.data.worlds.new('QA');scene.world.color=(.15,.15,.15)
for name,loc in [('front',(0,-1,0)),('back',(0,1,0)),('right',(1,0,0)),('quarter',(.7,-1,.35)),('top',(0,0,1))]:
 d=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,d);scene.collection.objects.link(cam);cam.location=loc;cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.36;scene.camera=cam;scene.render.filepath=str(O/('qa-'+name+'.png'));bpy.ops.render.render(write_still=True)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'helmet-front-back-candidate.blend'));bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.export_scene.fbx(filepath=str(O/'helmet-front-back-candidate.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=False,bake_space_transform=False,use_mesh_modifiers=False,path_mode='AUTO')
faces=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in objects);assert faces<=32000;assert uv_out==0
report={'source':str(source),'source_triangles':original,'triangles':faces,'dimensions_blender_m':list(target),'fixed_view_faces':counts,'uv_outside':uv_out,'side_images_used':False,'occupancy':.945,'shape_operations':'Decimation and size fit only; no silhouette edits','status':'Candidate; visual QA required, not Resources'};(O/'projection-report.json').write_text(json.dumps(report,indent=2));print(report)
