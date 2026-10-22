import bpy,json,math
from pathlib import Path
from mathutils import Vector
O=Path('D:/Unity/AshfallArtValidation/hidden-surface-art')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/Unity/Ashfall-iOS/Assets/Resources/Art/GeneratedWarden/armor-rebuilt.fbx')
a=max([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:len(o.data.polygons));bpy.context.view_layer.objects.active=a;a.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
m=bpy.data.materials.new('clay');m.diffuse_color=(.5,.5,.5,1);a.data.materials.clear();a.data.materials.append(m)
for p in a.data.polygons:p.material_index=0
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.render.film_transparent=True;s.render.resolution_x=512;s.render.resolution_y=1024;s.render.resolution_percentage=100
cd=bpy.data.cameras.new('camera');cam=bpy.data.objects.new('camera',cd);s.collection.objects.link(cam);s.camera=cam;cd.type='ORTHO'
original=a.data.copy();records=[]
for name,kind,sign in [('torso-positive-x','torso',1),('torso-negative-x','torso',-1),('leg-negative-x-inner','leg',-1),('leg-positive-x-inner','leg',1)]:
 vs=original.vertices;faces=[]
 for p in original.polygons:
  c=p.center
  if kind=='torso':
   cut=.26 if c.z>.65 else .32 if c.z>.30 else .37
   keep=c.z>=-.18 and abs(c.x)<cut
  else:keep=c.z<-.18 and c.x*sign>0
  if keep:faces.append(list(p.vertices))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([v.co for v in vs],[],faces);mesh.update();a.data=mesh;a.data.materials.append(m)
 used=[vs[i].co for f in faces for i in f];lo=Vector([min(v[i] for v in used) for i in range(3)]);hi=Vector([max(v[i] for v in used) for i in range(3)]);center=(lo+hi)/2;cd.ortho_scale=max((hi.z-lo.z)*1.10,(hi.y-lo.y)*2.20);d=Vector((sign if kind=='torso' else -sign,0,0));cam.location=center+d*5;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(O/(name+'-clay.png'));bpy.ops.render.render(write_still=True)
 records.append(dict(name=name,part=kind,camera_location=list(cam.location),camera_rotation_euler=list(cam.rotation_euler),orthographic_scale=cd.ortho_scale,resolution=[512,1024],bounds_min=list(lo),bounds_max=list(hi),center=list(center),direction=list(d),faces=len(faces)))
(O/'camera-bounds.json').write_text(json.dumps(records,indent=2))
