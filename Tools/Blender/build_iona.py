# Builds the stylized Iona (Collector / Archivist) hub NPC and exports FBX for Unity.
# Run: blender.exe -b -P Tools/Blender/build_iona.py -- <out.fbx> [preview.png]
import bpy, bmesh, math, sys
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/Iona/Iona.fbx"
PREVIEW = argv[1] if len(argv) > 1 else None

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

MATS = {}
TEXDIR = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/Iona/Textures/"
import os
def mat(name, rgb, rough=.8, metal=0., emit=None, tex=None, tint=1.):
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new("Iona_" + name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1)
        b.inputs["Emission Strength"].default_value = 3.0
    if tex and os.path.exists(TEXDIR + tex):
        n = m.node_tree.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(TEXDIR + tex)
        m.node_tree.links.new(n.outputs["Color"], b.inputs["Base Color"])
        rgb = tint if isinstance(tint, tuple) else (tint, tint, tint); b.inputs["Base Color"].default_value = (*rgb, 1)
    m.diffuse_color = (*rgb, 1)
    MATS[name] = m; return m

SKIN = mat("Skin", (.72, .47, .36), .7, tex="iona_skin.png")
FACE = mat("Face", (.72, .47, .36), .7, tex="iona_face.png")
HAIR = mat("Hair", (.07, .045, .035), .9, tex="iona_hair.png")
JACKET = mat("Jacket", (.25, .24, .13), .95, tex="iona_jacket.png")
JACKET_DK = mat("JacketDark", (.17, .16, .09), .95, tex="iona_jacket_dark.png")
SHIRT = mat("Shirt", (.42, .37, .31), .95, tex="iona_shirt.png")
PANTS = mat("Pants", (.14, .10, .08), .95, tex="iona_pants.png")
LEATHER = mat("Leather", (.20, .11, .06), .7, tex="iona_leather.png")
SCARF = mat("Scarf", (.42, .12, .08), .95, tex="iona_scarf.png")
BRASS = mat("Brass", (.55, .36, .16), .4, .85, tex="iona_brass.png")
LENS = mat("Lens", (.25, .35, .2), .2, .3)
GLOW = mat("Glow", (.1, .8, .9), .3, 0., emit=(.15, .9, 1.0))
PAPER = mat("Paper", (.55, .5, .38), .9, tex="iona_paper.png")

parts = []
def finish(o, m, smooth=True, bevel=0):
    o.data.materials.append(m)
    if smooth:
        for p in o.data.polygons: p.use_smooth = True
    if bevel:
        mod = o.modifiers.new("b", "BEVEL"); mod.width = bevel; mod.segments = 2
    parts.append(o); return o

def cyl(name, m, r1, r2, depth, loc, rot=(0, 0, 0), verts=12, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name; o.scale = scale; return finish(o, m)

def sph(name, m, r, loc, scale=(1, 1, 1), seg=14):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2 + 1, radius=r, location=loc)
    o = bpy.context.object; o.name = name; o.scale = scale; return finish(o, m)

def box(name, m, size, loc, rot=(0, 0, 0), bevel=.01):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.object; o.name = name; o.scale = size; return finish(o, m, False, bevel)

def limb(name, m, a, b, r1, r2, verts=10):
    a, b = Vector(a), Vector(b); d = b - a
    o = cyl(name, m, r1, r2, d.length, (a + b) / 2, verts=verts)
    o.rotation_mode = 'QUATERNION'; o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    return o

# Character faces -Y (Blender front). Height ~1.74.
# Legs & boots
for s in (-1, 1):
    x = .1 * s
    limb("Leg", PANTS, (x, 0, .9), (x * 1.05, 0, .12), .085, .06)
    box("Boot", LEATHER, (.1, .19, .12), (x * 1.05, -.03, .06), bevel=.03)
    cyl("BootCuff", LEATHER, .075, .075, .06, (x * 1.05, 0, .2), verts=10)
    box("KneePad", LEATHER, (.1, .04, .1), (x, -.07, .5), bevel=.015)

# Torso: shirt + long field jacket with flared skirt
cyl("Shirt", SHIRT, .16, .15, .4, (0, 0, 1.2), verts=14, scale=(1, .75, 1))
jk = cyl("JacketBody", JACKET, .175, .2, .46, (0, .005, 1.2), verts=16, scale=(1, .78, 1))
# open front: delete faces facing -Y
bm = bmesh.new(); bm.from_mesh(jk.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.y < -.93 and abs(f.normal.z) < .5], context='FACES')
bm.to_mesh(jk.data); bm.free()
jk.modifiers.new("solid", "SOLIDIFY").thickness = .012
skirt = cyl("JacketSkirt", JACKET, .21, .17, .42, (0, .01, .78), verts=16, scale=(1, .8, 1))
bm = bmesh.new(); bm.from_mesh(skirt.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.y < -.85 and abs(f.normal.z) < .6], context='FACES')
bm.to_mesh(skirt.data); bm.free()
skirt.modifiers.new("solid", "SOLIDIFY").thickness = .012
cyl("Shoulders", JACKET, .21, .18, .1, (0, .005, 1.42), verts=16, scale=(1, .72, 1))
for s in (-1, 1):
    box("ChestPocket", JACKET_DK, (.1, .03, .1), (.1 * s, -.14, 1.27), bevel=.008)
    box("HipPocket", JACKET_DK, (.1, .03, .1), (.15 * s, -.135, .82), rot=(0, 0, -.5 * s), bevel=.008)
    box("Lapel", JACKET_DK, (.05, .02, .22), (.06 * s, -.138, 1.3), rot=(0, .25 * s, 0))
box("Belt", LEATHER, (.32, .26, .05), (0, .005, 1.0), bevel=.01)
box("Buckle", BRASS, (.07, .02, .055), (0, -.13, 1.0))
# pens in pocket
for i, x in enumerate((-.13, -.11, -.09)):
    limb("Pen", BRASS if i != 1 else PAPER, (x, -.15, 1.31), (x, -.15, 1.37), .006, .006, 6)

# Arms: left hand on hip, right arm holding a glowing data ledger
# left
limb("UpperArmL", JACKET, (.23, 0, 1.42), (.33, .02, 1.17), .065, .058)
limb("CuffL", JACKET_DK, (.33, .02, 1.17), (.315, -.03, 1.12), .066, .066)
limb("ForearmL", SKIN, (.32, -.02, 1.13), (.2, -.06, 1.0), .042, .035)
sph("HandL", LEATHER, .045, (.19, -.06, .99), (1, .8, 1.1))
# right
limb("UpperArmR", JACKET, (-.23, 0, 1.42), (-.29, -.03, 1.17), .065, .058)
limb("CuffR", JACKET_DK, (-.29, -.03, 1.17), (-.29, -.07, 1.13), .066, .066)
limb("ForearmR", SKIN, (-.29, -.06, 1.14), (-.22, -.2, 1.07), .042, .035)
sph("HandR", LEATHER, .045, (-.21, -.22, 1.07), (1, 1, .9))
box("LedgerTablet", LEATHER, (.24, .17, .02), (-.17, -.27, 1.12), rot=(math.radians(55), 0, math.radians(-15)))
box("LedgerScreen", GLOW, (.2, .13, .01), (-.17, -.278, 1.128), rot=(math.radians(55), 0, math.radians(-15)), bevel=0)
for s in (-1, 1):
    limb("Bracer", LEATHER, ((.29 if s > 0 else -.27), -.03 if s > 0 else -.12, 1.1), ((.24 if s > 0 else -.24), -.05 if s > 0 else -.17, 1.06), .043, .043)

# Neck, head, face accents
cyl("Neck", SKIN, .045, .05, .1, (0, 0, 1.5))
head = sph("Head", FACE, .1, (0, -.01, 1.62), (.9, .95, 1.12), 18)
for s in (-1, 1):
    sph("Ear", SKIN, .02, (.09 * s, 0, 1.62), (.5, 1, 1.3), 8)
# curly hair: cap + bun of curls + loose ringlets
sph("HairCap", HAIR, .108, (0, .01, 1.665), (.95, 1.0, .95), 18)
import random; random.seed(7)
for i in range(16):
    a = random.uniform(0, 6.283); r = random.uniform(.02, .075)
    sph("Curl", HAIR, random.uniform(.035, .05), (math.cos(a) * r, .03 + math.sin(a) * r, 1.79 + random.uniform(-.02, .04)), seg=10)
for s in (-1, 1):
    for k in range(3):
        sph("Ringlet", HAIR, .028, (.1 * s, .0 + k * .02, 1.6 - k * .045), seg=8)
# goggles on forehead
cyl("GoggleBand", LEATHER, .112, .112, .03, (0, .005, 1.715), verts=20, scale=(.95, 1.02, 1))
for s in (-1, 1):
    cyl("GoggleRim", BRASS, .038, .034, .04, (.045 * s, -.11, 1.725), rot=(math.radians(80), 0, 0), verts=14)
    cyl("GoggleLens", LENS, .03, .03, .042, (.045 * s, -.112, 1.725), rot=(math.radians(80), 0, 0), verts=14)

# Red scarf around shoulders with a draped point
bpy.ops.mesh.primitive_torus_add(major_radius=.12, minor_radius=.05, location=(0, -.005, 1.47), major_segments=18, minor_segments=8)
t = bpy.context.object; t.name = "Scarf"; t.scale = (1.1, .9, .65); finish(t, SCARF)
bm = bmesh.new()
v = [bm.verts.new(p) for p in ((-.16, -.12, 1.46), (.2, -.11, 1.46), (.12, -.15, 1.22), (.04, -.15, 1.32))]
bm.faces.new(v); me = bpy.data.meshes.new("ScarfDrape"); bm.to_mesh(me); bm.free()
o = bpy.data.objects.new("ScarfDrape", me); scene.collection.objects.link(o)
o.modifiers.new("solid", "SOLIDIFY").thickness = .02; finish(o, SCARF, False)

# Gas mask hanging on chest with straps
box("MaskBody", BRASS, (.09, .05, .07), (0, -.16, 1.4), bevel=.02)
box("MaskGrille", LEATHER, (.04, .01, .035), (0, -.188, 1.395))
for s in (-1, 1):
    cyl("MaskFilter", BRASS, .032, .032, .04, (.065 * s, -.16, 1.39), rot=(0, math.radians(90), 0), verts=12)
    limb("MaskStrap", LEATHER, (.05 * s, -.15, 1.43), (.08 * s, -.08, 1.52), .008, .008, 6)

# Bandolier strap + scroll tube & satchel on the back
limb("Bandolier", LEATHER, (.17, -.12, 1.43), (-.17, -.16, .95), .02, .02, 6)
for z in (1.3, 1.12):
    box("StrapBuckle", BRASS, (.04, .015, .03), (.17 - (1.43 - z) * .71, -.155 + (1.43 - z) * -.07, z))
tube = cyl("ScrollTube", PAPER, .055, .055, .7, (-.12, .2, 1.25), rot=(0, math.radians(-18), 0), verts=14)
cyl("TubeCap", BRASS, .06, .06, .06, (-.23, .2, 1.58), rot=(0, math.radians(-18), 0), verts=14)
cyl("TubeBand", LEATHER, .058, .058, .03, (-.13, .2, 1.3), rot=(0, math.radians(-18), 0), verts=14)
box("Satchel", LEATHER, (.18, .08, .15), (.2, .03, .86), bevel=.02)
box("SatchelFlap", JACKET_DK, (.185, .085, .06), (.2, .03, .92), bevel=.01)
sph("SatchelGlow", GLOW, .015, (.2, -.015, .9), seg=8)

# UVs: world-scaled cube projection for tiling materials, front planar for the face
for o in parts:
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if o is head:
        uv = o.data.uv_layers.active.data
        for poly in o.data.polygons:
            for li in poly.loop_indices:
                co = o.data.vertices[o.data.loops[li].vertex_index].co
                uv[li].uv = (.5 + co.x / .2, .5 + (co.z - 1.62) / .24) if poly.normal.y < .15 else (.04, .04)
    else:
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=.35 if o.data.materials[0] in (BRASS, LEATHER) else .6)
        bpy.ops.object.mode_set(mode="OBJECT")
# Join into one mesh, origin at feet
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
for o in parts:
    bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
bpy.context.view_layer.objects.active = head
bpy.ops.object.join()
iona = bpy.context.object; iona.name = "Iona"
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
scene.cursor.location = (0, 0, 0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
print("IONA_TRIS", sum(len(p.vertices) - 2 for p in iona.data.polygons))

bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, path_mode='RELATIVE', mesh_smooth_type='FACE')
print("IONA_EXPORTED", OUT)

if PREVIEW:
    bpy.ops.object.camera_add(location=(1.6, -2.6, 2.1)); cam = bpy.context.object
    cam.rotation_euler = (math.radians(68), 0, math.radians(32)); scene.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.1
    bpy.ops.object.light_add(type='SUN', rotation=(math.radians(45), 0, math.radians(30))); bpy.context.object.data.energy = 3.5
    w = bpy.data.worlds.new("w"); scene.world = w; w.color = (.25, .25, .27)
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    scene.render.resolution_x = 700; scene.render.resolution_y = 900
    scene.render.filepath = PREVIEW; bpy.ops.render.render(write_still=True)




