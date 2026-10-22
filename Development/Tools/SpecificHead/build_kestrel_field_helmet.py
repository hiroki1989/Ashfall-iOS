"""Build a separate, illustration-led Kestrel Field Helmet candidate.

Run with Blender 5.x --background --python this_file. The approved four-view
source atlas is assembled separately by prepare_kestrel_atlas.py.
"""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path('D:/Unity/Ashfall-iOS')
OUT = ROOT / 'Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet/Candidates'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
texture = bpy.data.images.load(str(OUT / 'kestrel-field-helmet-atlas.png'))

def material(name, tint, metallic=0.0, roughness=0.7, painting=0.3):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*tint, 1)
    bs.inputs['Metallic'].default_value = metallic
    bs.inputs['Roughness'].default_value = roughness
    im = m.node_tree.nodes.new('ShaderNodeTexImage')
    im.image = texture
    mix = m.node_tree.nodes.new('ShaderNodeMixRGB')
    mix.blend_type = 'MIX'
    mix.inputs[1].default_value = (*tint, 1)
    strength=m.node_tree.nodes.new('ShaderNodeMath')
    strength.operation='MULTIPLY';strength.inputs[1].default_value=painting
    m.node_tree.links.new(im.outputs['Alpha'],strength.inputs[0])
    m.node_tree.links.new(strength.outputs[0], mix.inputs[0])
    m.node_tree.links.new(im.outputs['Color'], mix.inputs[2])
    m.node_tree.links.new(mix.outputs[0], bs.inputs['Base Color'])
    return m

ivory = material('Painted bone-white steel', (.75,.70,.60), .46, painting=.22)
dark = material('Graphite steel frame', (.095,.105,.105), .68, painting=.12)
visor = material('Black smoke visor', (.025,.045,.052), .2, .17, painting=.8)
teal = material('Turquoise enamel markers', (.05,.35,.36), .42, painting=.15)

def uv_project(obj, panel='front'):
    uv = obj.data.uv_layers.new(name='Approved illustration projection')
    for poly in obj.data.polygons:
        direction=panel
        if panel=='auto':
            normal=poly.normal
            if abs(normal.y)>=abs(normal.x):direction='back' if normal.y>0 else 'front'
            else:direction='right' if normal.x>0 else 'left'
        origin = {'front':(0,.5),'right':(.5,.5),'back':(0,0),'left':(.5,0)}[direction]
        for li in poly.loop_indices:
            p = obj.data.vertices[obj.data.loops[li].vertex_index].co
            u = (p.x/.245 + .5) if direction=='front' else ((-p.x/.245 + .5) if direction=='back' else ((p.y/.26+.5) if direction=='right' else (-p.y/.26+.5)))
            v = p.z/.285 + .5
            uv.data[li].uv = (origin[0] + .035 + max(0,min(1,u))*.43,
                              origin[1] + .035 + max(0,min(1,v))*.43)

def mesh(name, verts, faces, mat, panel='front', bevel=0):
    me=bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob=bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    me.materials.append(mat)
    uv_project(ob,panel)
    if bevel:
        be=ob.modifiers.new('Rounded manufactured edge','BEVEL')
        be.width=bevel;be.segments=2
        ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    return ob

def prism(name, outline, y_front, y_back, mat, panel='front', bevel=.002):
    n=len(outline)
    vs=[(x,y_front,z) for x,z in outline]+[(x,y_back,z) for x,z in outline]
    fs=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,panel,bevel)

def side_prism(name, outline, side, x_inner, x_outer, mat, bevel=.002):
    n=len(outline)
    vs=[(side*x_inner,y,z) for y,z in outline]+[(side*x_outer,y,z) for y,z in outline]
    fs=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,'right' if side>0 else 'left',bevel)

def ring_shell(name, rings, mat, panel='auto'):
    seg=32;vs=[]
    for z,rx,ry,yc in rings:
        for i in range(seg):
            a=i*2*math.pi/seg
            vs.append((rx*math.cos(a),yc+ry*math.sin(a),z))
    fs=[]
    for j in range(len(rings)-1):
        for i in range(seg):
            a=j*seg+i;b=j*seg+(i+1)%seg
            fs.append((a,b,b+seg,a+seg))
    fs.append(tuple(range((len(rings)-1)*seg,len(rings)*seg)))
    ob=mesh(name,vs,fs,mat,panel)
    for p in me_polys(ob):p.use_smooth=True
    return ob

def me_polys(ob):return ob.data.polygons

# Complete under-shell and four broad manufactured crown panels.
ring_shell('Continuous fitted graphite helmet shell', [(-.112,.093,.100,.013),(-.075,.112,.119,.010),(-.025,.119,.127,.005),(.035,.117,.124,.004),(.086,.096,.103,.008),(.125,.051,.061,.010),(.142,.012,.016,.010)], dark)
ring_shell('Cream ceramic ballistic crown', [(.005,.114,.122,.004),(.052,.106,.113,.006),(.100,.078,.085,.009),(.132,.027,.033,.010)], ivory)

# Face glass has its own solid volume and a deliberately angular lower nose.
prism('One-piece recessed dark visor', [(-.105,.008),(-.099,-.041),(-.059,-.069),(-.023,-.096),(.023,-.096),(.059,-.069),(.099,-.041),(.105,.008),(.088,.025),(-.088,.025)], -.132, -.114, visor, bevel=.003)
prism('Upper visor brow rail', [(-.110,.030),(-.087,.012),(0,.002),(.087,.012),(.110,.030),(.102,.043),(0,.034),(-.102,.043)], -.141,-.121,dark,bevel=.002)
for side in (-1,1):
    pts=[(.018,-.128),(.110,-.053),(.113,-.120),(.045,-.136)] if side>0 else [(-x,z) for x,z in [(.018,-.128),(.110,-.053),(.113,-.120),(.045,-.136)]]
    prism(('Right' if side>0 else 'Left')+' independent cream mandible',pts,-.140,-.110,ivory,bevel=.004)
    # Ear discs are concentric, axial, and separate from the crown; rear cheek support is deep.
    for radius,depth,mat,name in [(.055,.018,dark,'ear outer frame'),(.043,.021,ivory,'ear cream annulus'),(.030,.024,dark,'ear black hub'),(.013,.026,dark,'ear center')]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=depth,
            location=(side*(.111+depth*.28),.011,-.045), rotation=(0,math.pi/2,0))
        ob=bpy.context.object;ob.name=('Right' if side>0 else 'Left')+' '+name
        ob.data.materials.append(mat)
        uv_project(ob,'right' if side>0 else 'left')
    sx=side
    # Profile-built shells close the side and carry the illustrated cream field.
    side_prism(('Right' if side>0 else 'Left')+' cream crown side plate',
        [(-.074,.032),(-.044,.078),(.025,.102),(.085,.067),(.102,.002),(.080,-.019),(-.042,-.014)],
        side,.085,.101,ivory,.003)
    side_prism(('Right' if side>0 else 'Left')+' graphite jaw socket',
        [(-.096,-.074),(-.021,-.093),(.073,-.113),(.120,-.081),(.119,-.024),(.014,-.014)],
        side,.113,.125,dark,.002)
    side_prism(('Right' if side>0 else 'Left')+' cream cheek flank',
        [(-.082,-.070),(-.015,-.066),(.078,-.120),(.096,-.113),(.065,-.048),(-.059,-.030)],
        side,.124,.133,ivory,.003)

side_prism('Central raised crown spline',[(0,.141),(-.082,.120),(-.117,.061),(-.119,.037),(.117,.039),(.110,.102),(.058,.135)],1,0,.011,dark,.001)
prism('Central turquoise forehead mark',[(-.014,.070),(.014,.070),(.014,.047),(-.014,.047)],-.126,-.112,teal,bevel=.001)
prism('Chin mechanical latch',[(-.020,-.131),(.020,-.131),(.019,-.105),(-.019,-.105)],-.149,-.128,dark,bevel=.002)
prism('Back nape armored panel',[(-.060,-.112),(.060,-.112),(.076,-.065),(-.076,-.065)],.125,.137,ivory,'back',.003)

# Apply manufactured bevels before exporting; the candidate is independent of body mesh.
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':continue
    bpy.context.view_layer.objects.active=ob
    for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)

models=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in models:o.select_set(True)
bpy.context.view_layer.objects.active=models[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'kestrel-field-helmet.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'kestrel-field-helmet.glb'),export_format='GLB',use_selection=True)
for o in models:o.select_set(False)

world=bpy.context.scene.world or bpy.data.worlds.new('QA neutral world')
bpy.context.scene.world=world
world.color=(.17,.17,.17)
for pos,power in [((1,-2,3),220),((-2,1,1),140)]:
    bpy.ops.object.light_add(type='AREA',location=pos)
    bpy.context.object.data.energy=power;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=2
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
for label,position in {'front':(0,-.75,.10),'right':(.75,0,.10),'back':(0,.75,.10),'three-quarter':(.53,-.60,.16)}.items():
    bpy.ops.object.camera_add(location=position)
    cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.39;scene.camera=cam
    scene.render.filepath=str(OUT/('qa-'+label+'.png'));bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kestrel-field-helmet.blend'))
print('SPECIFIC_HEAD_OK',len(models),sum(len(o.data.polygons) for o in models))
