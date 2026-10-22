"""Retopologize the approved Hunyuan3D-2mini helmet and fit its four-view art.

The original shape remains untouched in AshfallArtValidation. This script
imports that shape, reduces and dimension-fits it, corrects its silhouette,
then projects the approved image views into a single mobile atlas.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path('D:/Unity/AshfallArtValidation/equipment3d-batch/kestrel-field-helmet')
OUT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet/Candidates')
DATA=json.loads((OUT/'silhouette-measurements.json').read_text(encoding='utf-8'))
RINGS=DATA['rings']
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'model-output/kestrel-field-helmet-v2-shape-only.glb'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
obj=max(objects,key=lambda o:len(o.data.polygons))
for o in objects:
    if o!=obj:bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
obj.name='Kestrel Field Helmet Hunyuan3D-2mini silhouette'
dec=obj.modifiers.new('Preserve detail while fitting iOS budget','DECIMATE')
dec.ratio=min(1,32000/max(1,len(obj.data.polygons)))
bpy.ops.object.modifier_apply(modifier=dec.name)
print('V3_DECIMATED',len(obj.data.polygons),flush=True)

# Uniformly place source in head socket space, retaining independent anatomy.
pts=[v.co.copy() for v in obj.data.vertices]
minimum=[min(p[i] for p in pts) for i in range(3)]
maximum=[max(p[i] for p in pts) for i in range(3)]
middle=[(minimum[i]+maximum[i])*.5 for i in range(3)]
dimensions=[maximum[i]-minimum[i] for i in range(3)]
target=[.245,.260,.285]
for v in obj.data.vertices:
    v.co=Vector(tuple((v.co[i]-middle[i])*target[i]/dimensions[i] for i in range(3)))
obj.data.update()

# Hunyuan placed the ear drums near the middle of the side. The illustration's
# disc center is 348/1024 in its side view: 4.7 cm farther toward the nape.
for v in obj.data.vertices:
    p=v.co
    side_weight=max(0,min(1,(abs(p.x)-.028)/.055))
    radial=math.exp(-((p.y/.071)**2+((p.z+.010)/.078)**2)*1.6)
    influence=side_weight*radial
    p.y+=.048*influence
    p.z-=.013*influence
obj.data.update()

# Match measured front and side alpha silhouettes across 29 horizontal bands.
# Keep most of Hunyuan's sculpted relief; silhouette correction fades inward.
def row(z):
    t=max(0,min(28,(.1425-z)/.285*28));i=min(27,int(t));w=t-i
    a,b=RINGS[i],RINGS[i+1]
    return {k:a[k]*(1-w)+b[k]*w for k in ('frontPixels','sidePixels')} if False else (i,w)

def interpolation(values,z):
    t=max(0,min(28,(.1425-z)/.285*28));i=min(27,int(t));w=t-i
    return values[i]*(1-w)+values[i+1]*w

source_min_x=[];source_max_x=[];source_min_y=[];source_max_y=[]
for ring in RINGS:
    z=ring['z'];band=[v.co for v in obj.data.vertices if abs(v.co.z-z)<.009]
    if not band:band=[min((v.co for v in obj.data.vertices),key=lambda p:abs(p.z-z))]
    source_min_x.append(min(p.x for p in band));source_max_x.append(max(p.x for p in band))
    source_min_y.append(min(p.y for p in band));source_max_y.append(max(p.y for p in band))
for v in obj.data.vertices:
    p=v.co;z=p.z
    cur_min_x=interpolation(source_min_x,z);cur_max_x=interpolation(source_max_x,z)
    cur_min_y=interpolation(source_min_y,z);cur_max_y=interpolation(source_max_y,z)
    t=max(0,min(28,(.1425-z)/.285*28));i=min(27,int(t));w=t-i
    def source(k):return RINGS[i][k]*(1-w)+RINGS[i+1][k]*w
    f0=RINGS[i]['frontPixels'];f1=RINGS[i+1]['frontPixels']
    left_px=f0[0]*(1-w)+f1[0]*w;right_px=f0[1]*(1-w)+f1[1]*w
    tx0=(left_px-512)*.245/843;tx1=(right_px-512)*.245/843
    ty0=source('yfront');ty1=source('yback')
    new_x=tx0+(p.x-cur_min_x)*(tx1-tx0)/max(.002,cur_max_x-cur_min_x)
    new_y=ty0+(p.y-cur_min_y)*(ty1-ty0)/max(.002,cur_max_y-cur_min_y)
    # A small correction keeps Hunyuan's integrated visor, ear frame and rear neck.
    p.x=p.x*.45+new_x*.55;p.y=p.y*.45+new_y*.55
obj.data.update()

for layer in list(obj.data.uv_layers):obj.data.uv_layers.remove(layer)
uv=obj.data.uv_layers.new(name='Measured four-view projection')
bvh=BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get(),epsilon=1e-6)
occluded_count=0
for face in obj.data.polygons:
    n=face.normal;c=face.center
    # Assign one view to each face. Use visible surface normal; this prevents
    # front details from projecting across side and rear faces.
    ax=abs(c.x)/.1225;ay=abs(c.y)/.13
    if ax>ay*.93 and abs(c.x)>.028:view='right' if c.x>0 else 'left'
    else:view='front' if c.y<0 else 'back'
    ox,oy={'front':(0,.5),'right':(.5,.5),'back':(0,0),'left':(.5,0)}[view]
    # An orthographic source must paint only the first surface encountered.
    # The original bake projected the same ear drawing onto both the protruding
    # ear and the side shell beneath it, creating two circles in three-quarter.
    normal={'front':Vector((0,-1,0)),'back':Vector((0,1,0)),
            'right':Vector((1,0,0)),'left':Vector((-1,0,0))}[view]
    hit=bvh.ray_cast(c+normal*1.0,-normal,2.0)
    ear_zone=abs(c.x)>.045 and -.092<c.z<.055 and -.055<c.y<.105
    occluded=ear_zone and hit[2] is not None and hit[2]!=face.index
    if occluded:occluded_count+=1
    for li in face.loop_indices:
        if occluded:
            uv.data[li].uv=(.001,.001)
            continue
        p=obj.data.vertices[obj.data.loops[li].vertex_index].co
        if view=='front':px=512+p.x*843/.245;py=61+(.1425-p.z)*901/.285
        elif view=='back':px=512-p.x*899/.245;py=64+(.1425-p.z)*894/.285
        elif view=='right':px=512-p.y*905/.26;py=66+(.1425-p.z)*890/.285
        else:px=512+p.y*905/.26;py=66+(.1425-p.z)*890/.285
        uv.data[li].uv=(ox+max(0,min(1023,px))/2048,oy+.5-max(0,min(1023,py))/2048)
    face.use_smooth=True

obj.data.materials.clear()
mat=bpy.data.materials.new('Kestrel approved painted four-view atlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF')
bs.inputs['Metallic'].default_value=.38;bs.inputs['Roughness'].default_value=.58
image=mat.node_tree.nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(OUT/'kestrel-field-helmet-atlas.png'))
mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.09,.095,.10,1)
mat.node_tree.links.new(image.outputs['Alpha'],mix.inputs[0]);mat.node_tree.links.new(image.outputs['Color'],mix.inputs[2]);mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
obj.data.materials.append(mat)

bpy.ops.export_scene.fbx(filepath=str(OUT/'kestrel-field-helmet-v4.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
print('V4_FBX',len(obj.data.polygons),'occluded_faces',occluded_count,flush=True)
world=bpy.data.worlds.new('Neutral QA');bpy.context.scene.world=world;world.color=(.2,.2,.2)
for pos,power in (((1,-2,3),230),((-2,1,2),190)):
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=power;bpy.context.object.data.size=2
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
for name,pos in {'front':(0,-.8,0),'right':(.8,0,0),'back':(0,.8,0),'three-quarter':(.55,-.6,.12)}.items():
    bpy.ops.object.camera_add(location=pos)
    cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=.315 if name!='three-quarter' else .37;scene.camera=cam
    scene.render.filepath=str(OUT/('qa-v4-'+name+'.png'));bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kestrel-field-helmet-v4.blend'))
print('V4_COMPLETE',flush=True)
