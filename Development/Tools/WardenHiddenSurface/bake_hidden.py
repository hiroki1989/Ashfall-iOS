import bpy,json,math
from pathlib import Path
from mathutils import Vector,Euler
from mathutils.bvhtree import BVHTree
O=Path('D:/Unity/AshfallArtValidation/hidden-surface-art');A=Path('D:/Unity/Ashfall-iOS/Assets/Resources/Art/GeneratedWarden')
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(A/'armor-rebuilt.fbx'));obj=max([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:len(o.data.polygons));bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
vs=obj.data.vertices;old=obj.data.uv_layers[0].name;uv=obj.data.uv_layers.new(name='HiddenSource');records=json.loads((O/'camera-bounds.json').read_text());obj.data.materials.clear();bvh=BVHTree.FromPolygons([v.co for v in vs],[list(p.vertices) for p in obj.data.polygons],all_triangles=True)
for k in range(5):
 m=bpy.data.materials.new('base' if k==0 else records[k-1]['name']);m.use_nodes=True;n=m.node_tree.nodes;n.clear();l=m.node_tree.links;t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(O/'armor-before-hidden-source.png') if k==0 else str(O/(records[k-1]['name']+'-registered.png')));t.extension='EXTEND';u=n.new('ShaderNodeUVMap');u.uv_map=old if k==0 else uv.name;l.new(u.outputs[0],t.inputs[0]);e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');l.new(t.outputs[0],e.inputs[0]);l.new(e.outputs[0],out.inputs[0]);obj.data.materials.append(m)
count=[0]*5
for p in obj.data.polygons:
 c=p.center;sign=1 if c.x>0 else -1;d=Vector((sign,0,0));hit,_,_,_=bvh.ray_cast(c+d*3,-d,6);hidden=hit is not None and (hit-c).length>.012
 cut=.26 if c.z>.65 else .32 if c.z>.30 else .37;torso=c.z>=-.18 and abs(c.x)<cut
 leg=c.z<-.18
 # Only previously occluded side-facing surfaces. Retain all exposed armour/front/back art.
 centerx=sign*.235 if leg else 0;side=abs(c.x-centerx)/(.13 if leg else .285)>abs(c.y-.015)/(.12 if leg else .19)*1.1
 patch=side and ((hidden and torso and c.z<.59) or (leg and (hidden or (c.x-centerx)*sign<0)))
 k=(1 if sign>0 else 2) if torso else (4 if sign>0 else 3)
 if not patch:k=0
 p.material_index=k;count[k]+=1
 if k:
  r=records[k-1];rot=Euler(r['camera_rotation_euler']).to_matrix();right=rot@Vector((1,0,0));up=rot@Vector((0,1,0));center=Vector(r['center']);scale=r['orthographic_scale'];aspect=r['resolution'][0]/r['resolution'][1]
  for li in p.loop_indices:
   v=vs[obj.data.loops[li].vertex_index].co-center;uv.data[li].uv=(.5+v.dot(right)/(scale*aspect),.5+v.dot(up)/scale)
obj.data.uv_layers.active_index=0;obj.data.uv_layers[old].active_render=True;s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=1;s.render.bake.margin=8;s.view_settings.view_transform='Standard';atlas=bpy.data.images.new('hidden patched atlas',width=2048,height=2048,alpha=False)
for m in obj.data.materials:t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=atlas;m.node_tree.nodes.active=t
bpy.ops.object.bake(type='EMIT');atlas.filepath_raw=str(O/'armor-hidden-patched-atlas.png');atlas.file_format='PNG';atlas.save();(O/'patch-report.json').write_text(json.dumps({'faceCounts':count,'faces':len(obj.data.polygons),'vertices':len(vs)},indent=2))
mask=bpy.data.images.new('hidden mask',width=2048,height=2048,alpha=False)
for i,m in enumerate(obj.data.materials):
 n=m.node_tree.nodes;l=m.node_tree.links;e=next(q for q in n if q.type=='EMISSION')
 for link in list(l):
  if link.to_node==e:l.remove(link)
 e.inputs[0].default_value=(1,1,1,1) if i else (0,0,0,1)
 t=n.new('ShaderNodeTexImage');t.image=mask;n.active=t
bpy.ops.object.bake(type='EMIT');mask.filepath_raw=str(O/'hidden-mask.png');mask.file_format='PNG';mask.save()