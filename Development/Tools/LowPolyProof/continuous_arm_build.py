import bpy,bmesh,json,heapq,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/Unity/Ashfall-iOS');O=Path('D:/Unity/AshfallArtValidation/body-surface-fix/continuous-arm');O.mkdir(exist_ok=True)
profile_bounds=json.loads((O/'profile-bounds.json').read_text())
skin_bounds=json.loads((O/'profile-skin-bounds.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(R/'Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend'))
original=bpy.data.objects['plain_body'];full=next(o for o in bpy.data.objects if o.type=='MESH' and 'authored silhouette' in o.name)
with bpy.data.libraries.load('D:/Unity/AshfallArtValidation/body-surface-fix/surface-fixed-candidate.blend',link=False) as (src,dst):dst.objects=['plain_body']
body=dst.objects[0];bpy.context.collection.objects.link(body);body.name='continuous_body'
bm=bmesh.new();bm.from_mesh(body.data);layer=bm.faces.layers.int.get('shoulder_cap');caps=[f for f in bm.faces if layer and f[layer]==1];bmesh.ops.delete(bm,geom=caps,context='FACES');bm.to_mesh(body.data);bm.free()
def ids(o):return [v.value for v in o.data.attributes['source_vertex'].data]
def facekey(o,f):
 v=ids(o);return tuple(sorted(v[i] for i in f.vertices))
keep={facekey(body,p) for p in body.data.polygons}
origids=ids(original);old_uv=original.data.uv_layers.active
missing=[p for p in original.data.polygons if tuple(sorted(origids[i] for i in p.vertices)) not in keep]
remap={};verts=[];faces=[];uvs=[];sourceids=[]
for f in missing:
 ix=[];uv=[]
 for li in f.loop_indices:
  old=original.data.loops[li].vertex_index
  if old not in remap:remap[old]=len(verts);verts.append(tuple(original.data.vertices[old].co));sourceids.append(origids[old])
  ix.append(remap[old]);uv.append(tuple(old_uv.data[li].uv))
 faces.append(ix);uvs.append(uv)
me=bpy.data.meshes.new('continuous_arm_bridge');me.from_pydata(verts,[],faces);me.update();bridge=bpy.data.objects.new('continuous_arm_bridge',me);bpy.context.collection.objects.link(bridge);bridge.matrix_world=original.matrix_world.copy();attr=me.attributes.new('source_vertex','INT','POINT')
for d,v in zip(attr.data,sourceids):d.value=v
uv=me.uv_layers.new(name='Fixed authored surface')
for f,coords in zip(me.polygons,uvs):
 for li,c in zip(f.loop_indices,coords):uv.data[li].uv=c
arms=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('plain_') and o!=original]+[bridge]
# One source graph provides exactly the same shoulder attachment to every UV copy.
points=[full.matrix_world@v.co for v in full.data.vertices];links=[[] for p in points]
for e in full.data.edges:
 a,b=e.vertices;distance=(points[a]-points[b]).length;links[a].append((b,distance));links[b].append((a,distance))
def distances(arm):
 d=[math.inf]*len(points);q=[]
 for i,p in enumerate(points):
  seed=(.78<p.z<1.33 and abs(p.x)>.335) if arm else (1.02<p.z<1.54 and abs(p.x)<.16)
  if seed:d[i]=0;heapq.heappush(q,(0,i))
 while q:
  dist,i=heapq.heappop(q)
  if dist!=d[i]:continue
  for j,length in links[i]:
   nd=dist+length
   if nd<d[j]:d[j]=nd;heapq.heappush(q,(nd,j))
 return d
ad,td=distances(True),distances(False)
factors=[]
for i,p in enumerate(points):
 if not(.74<p.z<1.53) or (p.z<1.04 and abs(p.x)<.29):factor=0
 elif math.isfinite(ad[i]) and math.isfinite(td[i]):
  t=max(0,min(1,(td[i]-ad[i]+.03)/.12));factor=t*t*(3-2*t)
 else:factor=1 if abs(p.x)>.23 else 0
 factors.append(factor)
# Arm-only authoring membership, extended over its adjacent restored elbow strip.
# The old smooth label crossed an entire bone at 30mm source edges here.
arm_source_ids=set()
for part in arms:
 if part==bridge:continue
 arm_source_ids.update(ids(part))
source_distance=[math.inf]*len(points);queue=[]
for i in arm_source_ids:
 source_distance[i]=0;heapq.heappush(queue,(0,i))
while queue:
 distance,i=heapq.heappop(queue)
 if distance!=source_distance[i] or distance>.16:continue
 for j,length in links[i]:
  candidate=distance+length
  if candidate<source_distance[j]:source_distance[j]=candidate;heapq.heappush(queue,(candidate,j))
corrected=[]
for i,p in enumerate(points):
 if .76<p.z<1.20 and source_distance[i]<.15:
  if factors[i]<1:corrected.append(i)
  factors[i]=1
(O/'corrected-elbow-source-ids.json').write_text(json.dumps(corrected))
for ob in [body]+arms:
 mesh=ob.data;values=ids(ob);col=mesh.color_attributes.new(name='ArmAttachment',type='FLOAT_COLOR',domain='POINT')
 for d,i in zip(col.data,values):d.color=(factors[i],0,0,1)
 # Body keeps its corrected UV and profile pixels verbatim in the left atlas.
 if ob==body:
  for d in mesh.uv_layers.active.data:d.uv.x*=.5
 else:
  layer=mesh.uv_layers.active
  old=[tuple(d.uv) for d in layer.data]
  allpoints=[x.matrix_world@v.co for x in arms for v in x.data.vertices]
  for f in mesh.polygons:
   n=(ob.matrix_world.to_3x3()@f.normal).normalized();side=abs(n.x)>.55 and abs(n.x)>abs(n.y)*.75
   naked=f.center.z<1.14
   if naked:side=True
   low_bridge=ob==bridge and sum(factors[values[i]] for i in f.vertices)/len(f.vertices)<=.5
   for li in f.loop_indices:
    p=ob.matrix_world@mesh.vertices[mesh.loops[li].vertex_index].co
    if not side:u,v=old[li];v=.5+.5*v
    else:
     # Profile source tile retains the hanging arm; align its own depth span
     # per anatomical height, rather than projecting torso photo across it.
     near=[q.y for q in allpoints if abs(q.z-p.z)<.025 and q.x*p.x>0]
     lo,hi=(min(near),max(near)) if near else(-.15,.15);t=max(0,min(1,(p.y-lo)/max(.01,hi-lo)))
     row=max(0,min(1023,int((1-(.055+p.z/1.82*.865))*1024)))
     bounds=(skin_bounds if f.center.z<1.14 else profile_bounds)['left' if n.x<0 else 'right'][row]
     sx=bounds[0]+(bounds[1]-bounds[0])*(1-t if n.x<0 else t)
     u=(0 if n.x<0 else .5)+sx/2048;v=(.055+p.z/1.82*.865)*.5
    if low_bridge:
     # This triangle is retained torso/pants, not an arm. Its paint must
     # sample the repaired armless body and cannot contain a hanging arm.
     right=n.x>=0 if abs(n.x)>.1 else p.x>=0
     tile=.5+(p.y/2.05 if right else -p.y/2.05)
     layer.data[li].uv=((.5 if right else 0)*.5+tile*.25,(.5+(p.z-.91)/2.05)*.5)
    else:layer.data[li].uv=(.5+u*.5,v)
for o in bpy.data.objects:
 if o.type=='MESH':o.hide_render=o not in [body]+arms
# Preserve the complete source face partition; no artificial cap triangles.
expected=len(original.data.polygons)+sum(len(o.data.polygons) for o in arms if o!=bridge)
actual=len(body.data.polygons)+sum(len(o.data.polygons) for o in arms)
assert expected==actual,(expected,actual)
atlas=bpy.data.images.load(str(O/'continuous-arm-atlas.png'));mat=bpy.data.materials.new('Continuous surface paint');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();tex=nodes.new('ShaderNodeTexImage');tex.image=atlas;em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);mat.node_tree.links.new(em.outputs[0],out.inputs[0])
for ob in [body]+arms:ob.data.materials.clear();ob.data.materials.append(mat)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'continuous-arm.blend'));bpy.ops.object.select_all(action='DESELECT')
for ob in [body]+arms:ob.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.fbx(filepath=str(O/'continuous-arm.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=False,bake_space_transform=False,use_mesh_modifiers=False,path_mode='AUTO',colors_type='LINEAR')
report={'removed_caps':len(caps),'restored_bridge_faces':len(missing),'faces_total':actual,'faces_expected':expected,'source_coverage_exact':actual==expected,'vertices':sum(len(o.data.vertices) for o in [body]+arms)};(O/'report.json').write_text(json.dumps(report,indent=2));print(report)

