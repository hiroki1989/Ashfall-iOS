import bpy, math
from pathlib import Path
from mathutils import Vector

OUT=Path('D:/Unity/AshfallArtValidation/equipment3d-batch')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

img=bpy.data.images.load(str(OUT/'kestrel-assault-series0.png'))
paint=bpy.data.materials.new('Kestrel original painted projection'); paint.use_nodes=True
nodes=paint.node_tree.nodes; tex=nodes.new('ShaderNodeTexImage'); tex.image=img
paint.node_tree.links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
metal=bpy.data.materials.new('Forged steel edges'); metal.diffuse_color=(.095,.105,.11,1)
wood=bpy.data.materials.new('Walnut end grain'); wood.diffuse_color=(.26,.12,.055,1)

# All parts occupy physical volume; image coordinates span the whole side silhouette.
def projected(obj, material=paint):
    obj.data.materials.append(material)
    if obj.type!='MESH': return obj
    uv=obj.data.uv_layers.new(name='Original painting side projection')
    for poly in obj.data.polygons:
        for li in poly.loop_indices:
            v=obj.data.vertices[obj.data.loops[li].vertex_index].co
            uv.data[li].uv=((v.x+2.0)/4.0, (v.z+0.77)/1.54)
    return obj

def prism(name, points, depth, mat=paint):
    n=len(points); verts=[(x,-depth/2,z) for x,z in points]+[(x,depth/2,z) for x,z in points]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj); projected(obj,mat)
    bevel=obj.modifiers.new('Machined rounded edges','BEVEL'); bevel.width=.035; bevel.segments=3
    obj.modifiers.new('Weighted corners','WEIGHTED_NORMAL')
    return obj

def cylinder(name,a,b,r,material=metal,vertices=16):
    mid=(Vector(a)+Vector(b))/2; d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=mid)
    o=bpy.context.object; o.name=name; o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    o.data.materials.append(material); o.modifiers.new('Weighted metal normals','WEIGHTED_NORMAL')
    return o

# Coordinate scheme follows the original side profile: stock left, muzzle right.
prism('Walnut full shoulder stock',[(-1.97,.02),(-1.03,.03),(-.75,.12),(-.73,-.14),(-1.04,-.23),(-1.91,-.35)],.17)
prism('Forged receiver upper', [(-.90,.18),(.24,.20),(.37,.01),(.11,-.18),(-.91,-.17)],.25)
prism('Receiver dust cover', [(-.76,.21),(.31,.21),(.28,.28),(-.63,.28)],.27)
prism('Walnut handguard',[(.29,.18),(1.09,.16),(1.08,-.02),(.31,-.04)],.20)
prism('Pistol grip',[(-.66,-.12),(-.37,-.13),(-.42,-.68),(-.65,-.70)],.14)
prism('Curved detachable magazine',[(-.02,-.15),(.40,-.18),(.30,-.53),(.02,-.82),(-.17,-.90),(-.29,-.81),(-.08,-.58)],.16)
prism('Trigger guard',[(-.42,-.17),(-.05,-.16),(-.13,-.32),(-.37,-.32)],.035,metal)
cylinder('Long barrel',(.95,0,.06),(1.87,0,.06),.055)
cylinder('Gas tube',(.68,0,.17),(1.64,0,.17),.045)
cylinder('Muzzle brake',(1.78,0,.06),(1.99,0,.06),.078)
cylinder('Front sight post',(1.72,0,.07),(1.72,0,.35),.023)
cylinder('Rear sight post',(-.48,0,.22),(-.48,0,.35),.022)
cylinder('Stock butt plate',(-1.96,0,-.17),(-1.96,0,.05),.047)
for x in [-.70,-.33,.26,.92]:
    cylinder('Metal pin or band', (x,-.15,.10),(x,.15,.10),.018)

world=bpy.context.scene.world; world.color=(.22,.22,.22)
for pos,power,size in [((0,-4,5),900,4),((-4,2,2),500,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos); bpy.context.object.data.energy=power; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=size
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE_NEXT'; scene.render.resolution_x=960; scene.render.resolution_y=600; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
scene.camera=None
views={'side':(0,-6,1.6),'front':(6,0,1.3),'three-quarter':(3,-5,2.6),'rear':(-6,1,1.5)}
for name,pos in views.items():
    bpy.ops.object.camera_add(location=pos); cam=bpy.context.object; cam.rotation_euler=(Vector((0,0,-.14))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=5.0; scene.camera=cam
    scene.render.filepath=str(OUT/f'kestrel-assault-3d-{name}.png'); bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kestrel-assault-prototype.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'kestrel-assault-prototype.glb'),export_format='GLB')
