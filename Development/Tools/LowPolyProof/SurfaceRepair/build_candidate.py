import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Unity/Ashfall-iOS'); O=Path('D:/Unity/AshfallArtValidation/body-surface-fix')
bpy.ops.wm.open_mainfile(filepath=str(R/'Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend'))
obj=bpy.data.objects['plain_body']; m=obj.data
bm=bmesh.new();bm.from_mesh(m)
# The authoring arm partition left narrow medial strips. Remove those source
# faces once, then close its exact topological boundary. No runtime spatial cut.
remove=[]
for f in bm.faces:
 c=f.calc_center_median(); x,y,z=obj.matrix_world@c
 boundary=.235 if z>=1.32 else .215
 if .76<z<1.55 and abs(x)>boundary:remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES')
boundary=[e for e in bm.edges if e.is_boundary]
cap=bm.faces.layers.int.new('shoulder_cap')
fill=bmesh.ops.holes_fill(bm,edges=boundary,sides=0).get('faces',[])
for f in fill:f[cap]=1
# Repair any residual branched cut loop with an edge fan.
remaining={e for e in bm.edges if e.is_boundary}
while remaining:
 component={remaining.pop()};verts=set(component.copy().pop().verts)
 changed=True
 while changed:
  connected={e for e in remaining if any(v in verts for v in e.verts)};changed=bool(connected)
  for e in connected:verts.update(e.verts)
  component.update(connected);remaining.difference_update(connected)
 centre=bm.verts.new(sum((v.co for v in verts),Vector())/len(verts))
 for e in component:
  a,b=e.verts
  if e.link_loops and e.link_loops[0].vert==a:a,b=b,a
  f=bm.faces.new((a,b,centre));f[cap]=1

bmesh.ops.triangulate(bm,faces=list(fill)); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
bm.to_mesh(m);bm.free();m.update()
# Each surface triangle receives one fixed projection chosen by its rest normal.
# Keep front/back unchanged; profiles occupy the lower atlas row.
old=m.uv_layers.active; coords=[tuple(l.uv) for l in old.data]
layer=m.uv_layers.new(name='Surface fixed four views');m.uv_layers.active=layer;layer.active_render=True
profile_rows=json.loads((O/'profile-rows.json').read_text())
mesh_fit=(O/'mesh-fit-ready.txt').exists()
# Approved shared head replaces this source head at runtime.
head_front=False
world_points=[obj.matrix_world@v.co for v in m.vertices]
def depth_bounds(height):
 points=[p.y for p in world_points if abs(p.z-height)<.025]
 return (min(points),max(points)) if points else (-.19,.19)
row_depth=[depth_bounds(i/500*1.82) for i in range(501)]
counts={}
for f in m.polygons:
 n=(obj.matrix_world.to_3x3()@f.normal).normalized()
 is_cap=m.attributes['shoulder_cap'].data[f.index].value==1
 view=('left' if f.center.x<0 else 'right') if is_cap else (('left' if n.x<0 else 'right') if abs(n.x)>.60 and abs(n.x)>abs(n.y)*1.1 else 'original')
 if f.center.z>1.50 and abs(n.x)<.75: view='original'
 if mesh_fit: view=('left' if n.x<0 else 'right') if abs(n.x)>.18 and abs(n.x)>abs(n.y)*.40 else 'original'
 if mesh_fit and is_cap:view='left' if f.center.x<0 else 'right'
 counts[view]=counts.get(view,0)+1
 for li in f.loop_indices:
  p=obj.matrix_world@m.vertices[m.loops[li].vertex_index].co
  if view=='original':u,v=coords[li];v=.5+.5*v
  else:
   height=max(.080,min(.927,p.z/1.82*.847+.080))
   row=max(0,min(1023,int((1-height)*1024)));sx0,sx1=profile_rows[row]
   near,far=row_depth[max(0,min(500,int(p.z/1.82*500)))];t=max(0,min(1,(p.y-near)/max(.01,far-near)))
   sx=sx0+(sx1-sx0)*(1-t)
   if view=='right':sx=682-sx
   tile=(170+sx)/1024
   u=(0 if view=='left' else .5)+tile*.5;v=height*.5
  if mesh_fit and view!='original':
   tile=.5+(p.y/2.05 if view=='right' else -p.y/2.05)
   u=(0 if view=='left' else .5)+tile*.5
   v=(.5+(p.z-.91)/2.05)*.5
  if head_front:
   if f.center.z>1.49 and n.y<-.80 and abs(f.center.x)<.075:
    u=.5+(.5+p.x/2.05)*.25
    v=.5+(.5+(p.z-.91)/2.05)*.5
   else:u*=.5
  layer.data[li].uv=(u,v)
m.uv_layers.remove(old)
for o in bpy.data.objects:
 if o.type=='MESH':o.hide_render=o!=obj
atlas=bpy.data.images.load(str(O/'surface-fixed-candidate-atlas.png'),check_existing=False)
mat=bpy.data.materials.new('Fixed surface atlas');mat.use_nodes=True;nd=mat.node_tree.nodes;nd.clear()
t=nd.new('ShaderNodeTexImage');t.image=atlas;e=nd.new('ShaderNodeEmission');out=nd.new('ShaderNodeOutputMaterial')
mat.node_tree.links.new(t.outputs['Color'],e.inputs['Color']);mat.node_tree.links.new(e.outputs[0],out.inputs[0]);m.materials.clear();m.materials.append(mat)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(O/'surface-fixed-candidate.blend'))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.fbx(filepath=str(O/'surface-fixed-candidate.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=False,bake_space_transform=False,use_mesh_modifiers=False,path_mode='AUTO')
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
for name,loc in [('front',(0,-4,.91)),('right',(4,0,.91)),('back',(0,4,.91)),('left',(-4,0,.91))]:
 data=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,data);scene.collection.objects.link(cam);cam.location=loc;cam.rotation_euler=(Vector((0,0,.91))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=2.05;scene.camera=cam;scene.render.filepath=str(O/(name+'.png'));bpy.ops.render.render(write_still=True)
bm=bmesh.new();bm.from_mesh(m)
r={'deleted_residual_faces':len(remove),'initial_cut_boundary_edges':len(boundary),'filled_faces':len(fill),'remaining_boundary_edges':sum(e.is_boundary for e in bm.edges),'fixed_projection_face_counts':counts,'status':'Fixed UV candidate; Unity pose and garment seam QA required.'};bm.free();(O/'report.json').write_text(json.dumps(r,indent=2));print(r)
