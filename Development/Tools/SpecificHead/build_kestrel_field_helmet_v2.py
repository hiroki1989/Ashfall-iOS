"""Reconstruct Kestrel Field Helmet from measured front/side/back silhouettes.

Blender 5.x --background --python build_kestrel_field_helmet_v2.py
"""
import bpy,json,math
print('V2_SCRIPT_ENTERED',flush=True)
from pathlib import Path
from mathutils import Vector

OUT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet/Candidates')
DATA=json.loads((OUT/'silhouette-measurements.json').read_text(encoding='utf-8'))
print('V2_DATA_LOADED',flush=True)
RINGS=DATA['rings']
bpy.ops.wm.read_factory_settings(use_empty=True)
atlas=bpy.data.images.load(str(OUT/'kestrel-field-helmet-atlas.png'))
print('V2_ATLAS_LOADED',flush=True)

def mat(name,rgba,metal,rough,paint=1):
    m=bpy.data.materials.new(name);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    image=m.node_tree.nodes.new('ShaderNodeTexImage');image.image=atlas
    mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=rgba
    multiply=m.node_tree.nodes.new('ShaderNodeMath');multiply.operation='MULTIPLY';multiply.inputs[1].default_value=paint
    m.node_tree.links.new(image.outputs['Alpha'],multiply.inputs[0])
    m.node_tree.links.new(multiply.outputs[0],mix.inputs[0]);m.node_tree.links.new(image.outputs['Color'],mix.inputs[2])
    m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
    return m

shellmat=mat('Four-view illustrated steel shell',(.10,.105,.11,1),.5,.52,1)
cream=mat('Illustrated curved cream ceramic armor',(.73,.68,.60,1),.45,.57,1)
black=mat('Dark anodized edge',(.08,.09,.10,1),.6,.54,.25)
glass=mat('Dark optical glass',(.025,.04,.05,1),.15,.19,1)

def sample_ring(z):
    f=(.1425-z)/.285*28
    i=max(0,min(27,int(f)));t=max(0,min(1,f-i))
    a,b=RINGS[i],RINGS[i+1]
    return {k:a[k]*(1-t)+b[k]*t for k in ('rx','yfront','yback')}

def shell_y(x,z,front=True):
    r=sample_ring(z);yc=(r['yfront']+r['yback'])*.5;ry=(r['yback']-r['yfront'])*.5
    frac=min(.985,(x/max(r['rx'],.001))**2)
    return yc+(-1 if front else 1)*ry*math.sqrt(1-frac)

def shell_x(y,z,side):
    r=sample_ring(z);yc=(r['yfront']+r['yback'])*.5;ry=(r['yback']-r['yfront'])*.5
    frac=min(.985,((y-yc)/max(ry,.001))**2)
    return side*r['rx']*math.sqrt(1-frac)

def front_point(px,py,offset=.003):
    x=(px-512)*.245/843;z=.1425-(py-61)*.285/901
    return (x,shell_y(x,z)-offset,z)

def side_point(px,py,side,offset=.003):
    y=(512-px)*.26/905;z=.1425-(py-66)*.285/890
    return (shell_x(y,z,side)+side*offset,y,z)

def uv(o,view='auto'):
    layer=o.data.uv_layers.new(name='Measured four-view UV')
    for face in o.data.polygons:
        v=view
        if v=='auto':
            n=o.matrix_world.to_3x3()@face.normal
            v=('right' if n.x>0 else 'left') if abs(n.x)>abs(n.y) else ('back' if n.y>0 else 'front')
        ox,oy={'front':(0,.5),'right':(.5,.5),'back':(0,0),'left':(.5,0)}[v]
        for li in face.loop_indices:
            p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            if v=='front':px=512+p.x*843/.245;py=61+(.1425-p.z)*901/.285
            elif v=='back':px=512-p.x*899/.245;py=64+(.1425-p.z)*894/.285
            elif v=='right':px=512-p.y*905/.26;py=66+(.1425-p.z)*890/.285
            else:px=512+p.y*905/.26;py=66+(.1425-p.z)*890/.285
            layer.data[li].uv=(ox+max(0,min(1023,px))/2048,oy+1-max(0,min(1023,py))/2048)

def new_mesh(name,verts,faces,material,view='auto',bevel=0):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.materials.append(material);uv(o,view)
    if bevel:
        mod=o.modifiers.new('Manufactured rolled edge','BEVEL');mod.width=bevel;mod.segments=2
        o.modifiers.new('Corner normals','WEIGHTED_NORMAL')
    return o

# Outer volume follows the front width and independent side front/rear extents at 29 heights.
segments=48;vertices=[];faces=[]
for r in RINGS:
    rx=r['rx'];yf=r['yfront'];yb=r['yback'];yc=(yf+yb)*.5;ry=(yb-yf)*.5
    for j in range(segments):
        a=2*math.pi*j/segments
        vertices.append((rx*math.cos(a),yc+ry*math.sin(a),r['z']))
for i in range(len(RINGS)-1):
    for j in range(segments):
        a=i*segments+j;b=i*segments+(j+1)%segments
        faces.append((a,a+segments,b+segments,b))
faces.append(tuple(reversed(range(segments))))
faces.append(tuple((len(RINGS)-1)*segments+j for j in range(segments)))
main=new_mesh('Measured closed helmet shell',vertices,faces,shellmat)
print('V2_SHELL_BUILT',flush=True)
for p in main.data.polygons:p.use_smooth=True

def curved_panel(name,pixels,project,material,view,thickness=.002):
    outer=[project(*p) for p in pixels]
    if view=='front':inner=[(x,y+thickness,z) for x,y,z in outer]
    elif view in ('right','left'):
        side=1 if view=='right' else -1
        inner=[(x-side*thickness,y,z) for x,y,z in outer]
    else:inner=[(x,y-thickness,z) for x,y,z in outer]
    n=len(outer);fs=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return new_mesh(name,outer+inner,fs,material,view,.001)

# Trace visual boundaries on the approved front illustration, then bend over the shell.
visor=[(239,462),(370,482),(512,510),(654,482),(785,462),(776,563),(665,588),(606,681),(551,791),(512,753),(473,791),(418,681),(359,588),(248,563)]
curved_panel('Continuous sculpted optical visor',visor,lambda x,y:front_point(x,y,.005),glass,'front')
cheek=[(824,552),(784,560),(677,622),(632,708),(570,834),(580,880),(608,923),(767,840),(824,740)]
curved_panel('Right curved ceramic cheek and chin armor',cheek,lambda x,y:front_point(x,y,.010),cream,'front')
curved_panel('Left curved ceramic cheek and chin armor',[(1024-x,y) for x,y in reversed(cheek)],lambda x,y:front_point(x,y,.010),cream,'front')

# Side plate crest bends with the side silhouette; the ear circle is centered
# on the measured drawing at approximately (348,580) pixels.
crown=[(198,179),(320,123),(483,96),(647,135),(796,235),(869,357),(891,467),(815,487),(689,447),(588,414),(501,360),(334,331),(196,339),(166,301)]
for side,label in ((1,'Right'),(-1,'Left')):
    curved_panel(label+' curved upper ceramic shell',crown,lambda x,y,s=side:side_point(x,y,s,.004),cream,'right' if side>0 else 'left')
    # Low flank plate follows the jaw direction and clears the actual visor opening.
    low=[(536,592),(678,607),(780,653),(858,781),(887,895),(835,928),(684,843),(607,755),(538,675)]
    curved_panel(label+' swept lower flank',low,lambda x,y,s=side:side_point(x,y,s,.006),cream,'right' if side>0 else 'left')
    ey=(512-348)*.26/905;ez=.1425-(580-66)*.285/890
    for radius,depth,material,detail in ((.051,.014,black,'dark ear seat'),(.045,.014,cream,'ivory ear rim'),(.036,.015,black,'dark ear diaphragm'),(.012,.016,black,'ear hub')):
        bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,
            location=(side*(.114+depth*.25),ey,ez),rotation=(0,math.pi/2,0))
        ob=bpy.context.object;ob.name=label+' '+detail;ob.data.materials.append(material)
        uv(ob,'right' if side>0 else 'left')
print('V2_PANELS_BUILT',flush=True)

# Export only authored geometry; render all three orthographic comparisons.
models=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in models:
    bpy.context.view_layer.objects.active=o
    for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    o.select_set(True)
bpy.context.view_layer.objects.active=models[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'kestrel-field-helmet-v2.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
print('V2_FBX_EXPORTED',flush=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'kestrel-field-helmet-v2.glb'),export_format='GLB',use_selection=True)
for o in models:o.select_set(False)

world=bpy.data.worlds.new('Neutral QA');bpy.context.scene.world=world;world.color=(.2,.2,.2)
for pos,power in (((1,-2,3),230),((-2,1,2),190)):
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=power;bpy.context.object.data.size=2
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
for label,position in {'front':(0,-.8,0),'right':(.8,0,0),'back':(0,.8,0),'three-quarter':(.55,-.6,.12)}.items():
    bpy.ops.object.camera_add(location=position)
    cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=.315 if label!='three-quarter' else .37;scene.camera=cam
    scene.render.filepath=str(OUT/('qa-v2-'+label+'.png'));bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kestrel-field-helmet-v2.blend'))
print('SPECIFIC_HEAD_V2_OK',len(models),sum(len(o.data.polygons) for o in models))
