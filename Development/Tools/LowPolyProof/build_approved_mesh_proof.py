"""CPU-only visual proof. Does not change gameplay assets or run Hunyuan."""
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(r"D:/Unity/Ashfall-iOS")
OUT = ROOT / "Assets/ArtSource/LowPolyProof/ApprovedSculpt"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.color = (.09,.09,.09)

def material(name, color, rough=1):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color, 1)
    m.use_nodes = True
    m.node_tree.nodes.clear()
    bs = m.node_tree.nodes.get("Principled BSDF") or m.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    bs.name = "Principled BSDF"
    out = m.node_tree.nodes.get("Material Output") or m.node_tree.nodes.new("ShaderNodeOutputMaterial")
    m.node_tree.links.new(bs.outputs["BSDF"], out.inputs["Surface"])
    bs.inputs["Base Color"].default_value = (*color,1)
    bs.inputs["Roughness"].default_value = rough
    return m

iron=material("oxidized iron",(.20,.20,.19)); rust=material("painted rust",(.38,.19,.10))
leather=material("worn leather",(.21,.12,.07)); bronze=material("edge brass",(.45,.27,.10))
black=material("graphite",(.045,.045,.043)); teal=material("oxidized teal",(.04,.26,.25))
earth=material("waste asphalt",(.22,.20,.17))

def import_one(path):
    old=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    mesh=[x for x in bpy.data.objects if x not in old and x.type=="MESH"]
    for x in list(bpy.data.objects):
        if x not in old and x.type=="MESH" and x.name=="Cube": bpy.data.objects.remove(x,do_unlink=True)
    return max([x for x in mesh if x.name in bpy.data.objects],key=lambda x:len(x.data.polygons))

body=import_one(ROOT/"Assets/Resources/Art/GeneratedBase/body.fbx")
body.name="authored silhouette / existing Hunyuan scan, decimated"
body.data.materials.clear()
m=material("painted two-view body",(.7,.7,.7))
nodes=m.node_tree.nodes; links=m.node_tree.links
image=nodes.new("ShaderNodeTexImage")
image.image=bpy.data.images.load(str(ROOT/"Assets/Resources/Art/GeneratedBase/body-atlas.png"))
links.new(image.outputs["Color"],nodes.get("Principled BSDF").inputs["Base Color"])
body.data.materials.append(m)
bpy.context.view_layer.objects.active=body;body.select_set(True)
dec=body.modifiers.new("silhouette first: 12k face budget","DECIMATE")
dec.ratio=min(1,12000/len(body.data.polygons));bpy.ops.object.modifier_apply(modifier=dec.name)
body.select_set(False)

def panel(name, x0,x1,y0,y1,z0,z1,mat,slant=0):
    # Tapered eight-corner solid; bespoke contour, not a sphere/cylinder kitbash.
    v=[(x0,y0,z0),(x1,y0,z0),(x1+slant,y1,z0),(x0+slant,y1,z0),
       (x0+.02,y0,z1),(x1-.02,y0,z1),(x1+slant-.02,y1,z1),(x0+slant+.02,y1,z1)]
    f=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],f);mesh.update()
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    bev=obj.modifiers.new("bent edges","BEVEL");bev.width=.008;bev.segments=1
    obj.modifiers.new("weighted facets","WEIGHTED_NORMAL")
    return obj

def strap(name,a,b,width,depth,mat):
    center=(Vector(a)+Vector(b))/2
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    o=bpy.context.object;o.name=name;o.dimensions=(width,depth,d.length)
    o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    bvl=o.modifiers.new("soft leather edge","BEVEL");bvl.width=.004;bvl.segments=1
    return o

# Reuse the approved Warden sculpt instead of geometric armor primitives.
armor=import_one(ROOT/"Assets/Resources/Art/GeneratedWarden/armor-mobile.fbx")
armor.name="approved Warden sculpt / decimated"
am=material("approved armor atlas",(.7,.7,.7))
an=am.node_tree.nodes;al=am.node_tree.links
ai=an.new("ShaderNodeTexImage");ai.image=bpy.data.images.load(str(ROOT/"Assets/Resources/Art/GeneratedWarden/armor-mobile-atlas.png"))
al.new(ai.outputs["Color"],an.get("Principled BSDF").inputs["Base Color"])
armor.data.materials.clear();armor.data.materials.append(am)
bpy.context.view_layer.objects.active=armor;armor.select_set(True)
adec=armor.modifiers.new("preserve Warden silhouette", "DECIMATE")
adec.ratio=min(1,16000/len(armor.data.polygons));bpy.ops.object.modifier_apply(modifier=adec.name)
armor.select_set(False)
armor.scale=(.9,.9,.9)
armor.location.z=.9
import bmesh
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 1.55], context="VERTS")
bm.to_mesh(body.data);bm.free()
body.location.z=.12
body.hide_render=False
# Existing authored rifle as an independent, inspectable asset. Its grip pose is
# intentionally not claimed as solved by this static low-polygon art study.
rifle=import_one(ROOT/"Assets/Resources/Art/GeneratedWarden/rifle-mobile.fbx")
rifle.name="approved Warden rifle / pose test"
rm=material("approved rifle atlas",(.7,.7,.7))
rn=rm.node_tree.nodes;rl=rm.node_tree.links
ri=rn.new("ShaderNodeTexImage");ri.image=bpy.data.images.load(str(ROOT/"Assets/Resources/Art/GeneratedWarden/rifle-mobile-atlas.png"))
rl.new(ri.outputs["Color"],rn.get("Principled BSDF").inputs["Base Color"])
rifle.data.materials.clear();rifle.data.materials.append(rm)
rifle.scale=(.44,.44,.44);rifle.location=(1.12,0,.98)
bpy.context.view_layer.objects.active=rifle;rifle.select_set(True)
dec=rifle.modifiers.new("compact rifle","DECIMATE");dec.ratio=min(1,4500/len(rifle.data.polygons));bpy.ops.object.modifier_apply(modifier=dec.name)
rifle.select_set(False)

panel("asphalt base",-1.2,2.05,-.65,.60,-.12,-.10,earth)
for x in (-1.03,-.76,.63,1.67):
    strap("fractured paving seam",(x,-.4,-.095),(x+.17,.4,-.095),.008,.008,black)

def camera(name,loc,target,focal):
    bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.name=name
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=focal;scene.camera=cam
    return cam

def area(name,loc,power,size,color):
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name
    o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=color
    o.rotation_euler=(Vector((.4,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()

area("warm key",(-3,-4,6),700,4,(1,.83,.65))
area("cool rim",(2,3,4),520,3,(.58,.72,1))
area("soft fill",(4,-2,3),240,4,(1,1,1))
scene.view_settings.view_transform='AgX'
scene.render.image_settings.color_mode='RGBA'
for name,loc,target,size in [
    ('front-quarter',(3.3,-4.9,2.7),(.35,0,.98),2.7),
    ('side',(-4,-.2,2.3),(.35,0,.94),2.7),
    ('rear-quarter',(3.4,4.6,2.6),(.35,0,.95),2.7),
    ('gameplay-quarter',(4.2,-5.8,5.7),(.35,0,.70),3.5),
]:
    camera(name,loc,target,size)
    scene.render.filepath=str(OUT/(name+'.png'))
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'lowpoly-proof.blend'))
print('LOWPOLY_PROOF_OK body_faces=',len(body.data.polygons),'rifle_faces=',len(rifle.data.polygons))








