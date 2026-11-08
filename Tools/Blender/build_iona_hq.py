# Iona HQ build: MPFB (MakeHuman CC0 base mesh) body + fitted clothing + accessories, face projected from portrait.
# Run with BLENDER_USER_RESOURCES=D:/Unity/AssetIntegration/IonaHQ/blender-user (MPFB installed there)
# blender -b -P Tools/Blender/build_iona_hq.py -- <out.fbx> <preview_dir>
import bpy, bmesh, math, sys, os, random, importlib, addon_utils
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/Iona/Iona.fbx"
PREV = argv[1] if len(argv) > 1 else "D:/Unity/AssetIntegration/IonaHQ"
TEX = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/Iona/Textures/"
PORTRAIT = "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-iona-v12.png"
random.seed(11)

bpy.ops.wm.read_factory_settings(use_empty=True)
mod = next(m.__name__ for m in addon_utils.modules() if m.__name__.endswith("mpfb"))
addon_utils.enable(mod, default_set=True)
HS = importlib.import_module(mod + ".services.humanservice").HumanService
TS = importlib.import_module(mod + ".services.targetservice").TargetService
scene = bpy.context.scene

def sel(o):
    bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o

# ---------------- body ----------------
d = TS.get_default_macro_info_dict()
d.update(gender=0.0, age=0.47, muscle=0.58, weight=0.42, height=0.62, proportions=0.7, cupsize=0.45, firmness=0.6)
human = HS.create_human(macro_detail_dict=d)
TS.bake_targets(human)
rig = HS.add_builtin_rig(human, "game_engine")
for m in list(human.modifiers):
    if m.type == "MASK": human.modifiers.remove(m)
H = human.dimensions.z
print("IONA_HEIGHT", H)

def gidx(o, name): return o.vertex_groups[name].index
def verts_in(o, names, thr=.3):
    ids = {gidx(o, n) for n in names if n in o.vertex_groups}
    return {v.index for v in o.data.vertices if any(g.group in ids and g.weight > thr for g in v.groups)}
def centroid(o, name):
    vs = verts_in(o, [name], .5); return sum((o.data.vertices[i].co for i in vs), Vector()) / max(1, len(vs))

# ---------------- pose arms down, apply as rest ----------------
sel(rig); bpy.ops.object.mode_set(mode="POSE")
def aim(bone, target_dir):
    pb = rig.pose.bones[bone]; bpy.context.view_layer.update()
    head = rig.matrix_world @ pb.head; tail = rig.matrix_world @ pb.tail
    q = (tail - head).normalized().rotation_difference(Vector(target_dir).normalized())
    pb.matrix = Matrix.Translation(pb.head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-pb.head) @ pb.matrix
    bpy.context.view_layer.update()
aim("upperarm_l", (.22, .06, -1)); aim("lowerarm_l", (.05, -.55, -1)); aim("hand_l", (.0, -.35, -1))
aim("upperarm_r", (-.24, .08, -1)); aim("lowerarm_r", (-.1, -.9, -.55)); aim("hand_r", (-.05, -1, -.2))
bpy.ops.object.mode_set(mode="OBJECT")
sel(human)
arm_mod = next(m for m in human.modifiers if m.type == "ARMATURE")
bpy.ops.object.modifier_apply(modifier=arm_mod.name)
sel(rig); bpy.ops.object.mode_set(mode="POSE"); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode="OBJECT")

# ---------------- face projection -> skin texture bake ----------------
body_ids = verts_in(human, ["body"], .5)
eye_ids = verts_in(human, ["helper-l-eye", "helper-r-eye"], .5)
er, el, lips = centroid(human, "helper-r-eye"), centroid(human, "helper-l-eye"), centroid(human, "lips")
emid = (er + el) / 2
img = bpy.data.images.load(PORTRAIT); IW, IH = img.size
px = list(img.pixels[:])
def sample(x, y, r=6):
    acc = [0, 0, 0]; n = 0
    for yy in range(y - r, y + r):
        for xx in range(x - r, x + r):
            i = ((IH - 1 - yy) * IW + xx) * 4; acc = [acc[k] + px[i + k] for k in range(3)]; n += 1
    return [c / n for c in acc]
skin_srgb = [(a + b) / 2 for a, b in zip(sample(600, 305), sample(745, 300))]
skin_rgb = [c ** 2.2 for c in skin_srgb]
# paint over hair strands on right forehead/cheek in portrait so face bake is clean skin
for yy in range(185, 315):
    for xx in range(560, 650):
        if yy > 230 and xx > 596 and yy < 275: continue  # preserve right eye & brow
        if yy > 205 and xx > 588 and yy < 232: continue  # preserve right eyebrow
        i = ((IH - 1 - yy) * IW + xx) * 4
        lum = .299 * px[i] + .587 * px[i + 1] + .114 * px[i + 2]
        if lum < .42:
            t = max(0., min(1., (.42 - lum) / .22))
            for k in range(3): px[i + k] = px[i + k] * (1 - t) + skin_srgb[k] * t
EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((625, 252)), Vector((718, 248)), Vector((680, 342))
sx = (EYE_L_PX.x - EYE_R_PX.x) / (el.x - er.x)
sz = (MOUTH_PX.y - (EYE_R_PX.y + EYE_L_PX.y) / 2) / (emid.z - lips.z)
pmid = (EYE_R_PX + EYE_L_PX) / 2
# Mirror left-side clean portrait onto right side around eye-center (x=671) to eliminate hair-strand shadows & eye mismatch
for yy in range(175, 365):
    for xx in range(565, 668):
        mx = int(2 * 671.5 - xx)
        if 0 <= mx < IW:
            i_dst = ((IH - 1 - yy) * IW + xx) * 4
            i_src = ((IH - 1 - max(0, min(IH - 1, yy - int((671.5 - xx) * .04)))) * IW + mx) * 4
            w_m = 1.0 if yy < 305 else max(0., (365 - yy) / 60.)  # keep natural asymmetric smile below nose
            for k in range(3): px[i_dst + k] = px[i_dst + k] * (1 - w_m) + px[i_src + k] * w_m
# Paint smooth skin tone over the open eyeball apertures in portrait so eyelids don't get double-iris baked onto skin
for ex, ey in ((625, 252), (718, 248)):
    for yy in range(ey - 18, ey + 20):
        for xx in range(ex - 30, ex + 30):
            d2 = ((xx - ex) / 26.) ** 2 + ((yy - ey) / 14.) ** 2
            if d2 < 1.0:
                t = (1.0 - d2) ** .7
                i = ((IH - 1 - yy) * IW + xx) * 4
                for k in range(3): px[i + k] = px[i + k] * (1 - t) + (skin_srgb[k] * .88) * t
img.pixels[:] = px
img.filepath_raw = "D:/Unity/AssetIntegration/IonaHQ/portrait_clean_bake.png"
img.file_format = "PNG"; img.save()
img = bpy.data.images.load("D:/Unity/AssetIntegration/IonaHQ/portrait_clean_bake.png")
me = human.data
proj = me.uv_layers.new(name="proj"); me.uv_layers.active = me.uv_layers["UVMap"]
mask = me.color_attributes.new("facemask", "FLOAT_COLOR", "POINT")
head_ids = verts_in(human, ["head"], .2) & body_ids
FC, FR = Vector((671, 288)), Vector((82, 108))
for v in me.vertices:
    p = Vector((pmid.x + (v.co.x - emid.x) * sx, pmid.y - (v.co.z - emid.z) * sz))
    e = ((p.x - FC.x) / FR.x) ** 2 + ((p.y - FC.y) / FR.y) ** 2
    w = max(0., min(1., (1.15 - e) / .45)) if v.index in head_ids and v.normal.y < -.15 and v.co.y < emid.y + .02 else 0.
    mask.data[v.index].color = (w, w, w, 1)
for loop in me.loops:
    co = me.vertices[loop.vertex_index].co
    proj.data[loop.index].uv = ((pmid.x + (co.x - emid.x) * sx) / IW, 1 - (pmid.y - (co.z - emid.z) * sz) / IH)

def node_mat(name):
    m = bpy.data.materials.new(name); m.use_nodes = True; return m, m.node_tree.nodes, m.node_tree.links
# temporarily hide non-body helper meshes so overlapping helper UVs don't corrupt the skin bake
sel(human); bm_hide = bmesh.new(); bm_hide.from_mesh(me)
for f in bm_hide.faces:
    f.hide = not all(v.index in body_ids for v in f.verts)
bm_hide.to_mesh(me); bm_hide.free()
bake_m, N, L = node_mat("bake")
N.remove(N["Principled BSDF"]); out = N["Material Output"]
em = N.new("ShaderNodeEmission"); L.new(em.outputs[0], out.inputs[0])
mix = N.new("ShaderNodeMix"); mix.data_type = "RGBA"; L.new(mix.outputs[2], em.inputs[0])
noise = N.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 60
ramp = N.new("ShaderNodeValToRGB"); L.new(noise.outputs[0], ramp.inputs[0])
ramp.color_ramp.elements[0].color = (*[c * .9 for c in skin_rgb], 1); ramp.color_ramp.elements[1].color = (*[min(1, c * 1.06) for c in skin_rgb], 1)
L.new(ramp.outputs[0], mix.inputs[6])
uvn = N.new("ShaderNodeUVMap"); uvn.uv_map = "proj"
pt = N.new("ShaderNodeTexImage"); pt.image = img; pt.extension = "EXTEND"; L.new(uvn.outputs[0], pt.inputs[0]); L.new(pt.outputs[0], mix.inputs[7])
ca = N.new("ShaderNodeAttribute"); ca.attribute_name = "facemask"; L.new(ca.outputs["Fac"], mix.inputs[0])
body_img = bpy.data.images.new("iona_body", 2048, 2048); body_img.colorspace_settings.name = "sRGB"
tgt = N.new("ShaderNodeTexImage"); tgt.image = body_img; N.active = tgt
me.materials.clear(); me.materials.append(bake_m)
scene.render.engine = "CYCLES"; scene.cycles.samples = 4; scene.cycles.device = "CPU"
scene.render.bake.margin = 8
sel(human); bpy.ops.object.bake(type="EMIT")
body_img.filepath_raw = TEX + "iona_body.png"; body_img.file_format = "PNG"; body_img.save()
me.uv_layers.remove(me.uv_layers["proj"])
bm_unhide = bmesh.new(); bm_unhide.from_mesh(me)
for f in bm_unhide.faces: f.hide = False
bm_unhide.to_mesh(me); bm_unhide.free()
print("IONA_BAKED")

# ---------------- materials ----------------
def tex_mat(name, file, rough=.85, metal=0., emit=None, color=None):
    m, N, L = node_mat("Iona_" + name); b = N["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if file:
        t = N.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(TEX + file); L.new(t.outputs[0], b.inputs["Base Color"])
        b.inputs["Base Color"].default_value = (1, 1, 1, 1)
    if color: b.inputs["Base Color"].default_value = (*color, 1)
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = 4
    return m
M = dict(skin=tex_mat("Skin", "iona_body.png", .6), jacket=tex_mat("Jacket", "iona_jacket.png"),
         jacketdk=tex_mat("JacketDark", "iona_jacket_dark.png"), shirt=tex_mat("Shirt", "iona_shirt.png"),
         pants=tex_mat("Pants", "iona_pants.png"), leather=tex_mat("Leather", "iona_leather.png", .6),
         scarf=tex_mat("Scarf", "iona_scarf.png", .95), brass=tex_mat("Brass", "iona_brass.png", .35, .9),
         hair=tex_mat("Hair", "iona_hair.png", .7), paper=tex_mat("Paper", "iona_paper.png"),
         lens=tex_mat("Lens", None, .1, .4, color=(.12, .2, .1)), glow=tex_mat("Glow", None, .3, emit=(.1, .85, 1), color=(.1, .7, .8)))
me.materials.clear(); me.materials.append(M["skin"])

# ---------------- fitted garments (copies keep skin weights) ----------------
parts = []
def extract(name, keep, offset, mat, solid=.004, uvscale=6., noise=0.):
    o = human.copy(); o.data = human.data.copy(); o.name = name; scene.collection.objects.link(o)
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in keep], context="VERTS"); bm.to_mesh(o.data); bm.free()
    o.data.materials.clear(); o.data.materials.append(mat)
    for u in o.data.uv_layers:
        for l in u.data: l.uv = l.uv * uvscale
    sel(o)
    if offset:
        dm = o.modifiers.new("off", "DISPLACE"); dm.strength = offset; dm.mid_level = 0
        if noise:
            tx = bpy.data.textures.new(name + "_n", "CLOUDS"); tx.noise_scale = .06; dm.texture = tx; dm.mid_level = .5 - offset / noise; dm.strength = noise
        bpy.ops.object.modifier_apply(modifier="off")
    if solid:
        s = o.modifiers.new("sol", "SOLIDIFY"); s.thickness = solid; s.offset = 1; bpy.ops.object.modifier_apply(modifier="sol")
    for p in o.data.polygons: p.use_smooth = True
    parts.append(o); return o

V = human.data.vertices
body_ids = verts_in(human, ["body"], .5)
eye_ids = verts_in(human, ["helper-l-eye", "helper-r-eye"], .5)
def wz(i): return V[i].co.z
waist = H * .565; hip = H * .5; knee = H * .29; shoulder = H * .815
lower_arm = verts_in(human, ["lowerarm_l", "lowerarm_r"], .5)
hand = verts_in(human, ["hand_l", "hand_r"], .35)
torso_grp = verts_in(human, ["pelvis", "spine_01", "spine_02", "spine_03", "clavicle_l", "clavicle_r", "upperarm_l", "upperarm_r", "lowerarm_l", "lowerarm_r"], .25)
neck = verts_in(human, ["neck_01", "head"], .15)
# rolled sleeves end mid-forearm: keep forearm verts closer to elbow
def arm_t(i):
    co = V[i].co; s = "l" if co.x > 0 else "r"
    el = rig.data.bones["lowerarm_" + s].head_local; wr = rig.data.bones["hand_" + s].head_local
    return (co - el).dot(wr - el) / (wr - el).length_squared
def elbow_side(i): return arm_t(i) < .45
jacket_ids = {i for i in torso_grp & body_ids if i not in neck and i not in hand and wz(i) > hip - .02 and (i not in lower_arm or elbow_side(i)) and not (V[i].co.y < -.05 and abs(V[i].co.x) < .032 and wz(i) < shoulder - .02)}
extract("Jacket", jacket_ids, .016, M["jacket"])
shirt_ids = {i for i in body_ids if waist - .05 < wz(i) < shoulder and V[i].co.y < 0 and abs(V[i].co.x) < .09 and i not in neck}
extract("Shirt", shirt_ids, .011, M["shirt"], solid=.002)
cuff_ids = {i for i in lower_arm & body_ids if .28 < arm_t(i) < .47}
extract("SleeveCuff", cuff_ids, .028, M["jacketdk"], solid=.006)
skirt = extract("JacketSkirt", {i for i in verts_in(human, ["helper-skirt"], .5) if V[i].co.z > H * .3}, .03, M["jacket"], solid=.006)
# split skirt open at front
sel(skirt); bm = bmesh.new(); bm.from_mesh(skirt.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < -.05 and abs(f.calc_center_median().x) < .055], context="FACES")
bm.to_mesh(skirt.data); bm.free()
legs = verts_in(human, ["thigh_l", "thigh_r", "calf_l", "calf_r", "pelvis"], .3) & body_ids
extract("Pants", {i for i in legs if H * .10 < V[i].co.z < waist + .01 and i not in hand and i not in lower_arm}, .007, M["pants"])
# Smooth leather combat boots (covering toe crevices cleanly)
boot_ids = {i for i in body_ids if wz(i) < H * .18 and i in verts_in(human, ["calf_l", "calf_r", "foot_l", "foot_r", "ball_l", "ball_r"], .2)}
boots = extract("Boots", boot_ids, .0, M["leather"], solid=0, uvscale=4)
sel(boots); sm = boots.modifiers.new("sm", "SMOOTH"); sm.factor = 1; sm.iterations = 40; bpy.ops.object.modifier_apply(modifier="sm")
dm = boots.modifiers.new("d", "DISPLACE"); dm.strength = .018; dm.mid_level = 0; bpy.ops.object.modifier_apply(modifier="d")
s = boots.modifiers.new("s", "SOLIDIFY"); s.thickness = .005; bpy.ops.object.modifier_apply(modifier="s")
glove_ids = (hand & body_ids) - verts_in(human, ["index_02_l", "middle_02_l", "ring_02_l", "pinky_02_l", "thumb_02_l", "index_02_r", "middle_02_r", "ring_02_r", "pinky_02_r", "thumb_02_r", "index_03_l", "middle_03_l", "ring_03_l", "pinky_03_l", "thumb_03_l", "index_03_r", "middle_03_r", "ring_03_r", "pinky_03_r", "thumb_03_r"], .4)
extract("Gloves", glove_ids, .004, M["leather"], solid=.002, uvscale=4)
wrist_ids = {i for i in lower_arm & body_ids if not elbow_side(i) and (V[i].co - rig.data.bones["hand_" + ("l" if V[i].co.x > 0 else "r")].head_local).length < .07}
extract("Bracers", wrist_ids, .012, M["leather"], solid=.004, uvscale=4)
scarf_ids = {i for i in body_ids if (i in neck and wz(i) < H * .875) or (shoulder - .06 < wz(i) < H * .875 and abs(V[i].co.x) < .2 and i not in verts_in(human, ["upperarm_l", "upperarm_r"], .3))}
extract("Scarf", scarf_ids, .03, M["scarf"], solid=.008, uvscale=4, noise=.06)
hair = extract("HairBase", verts_in(human, ["scalp"], .2) & body_ids, .014, M["hair"], solid=.006, uvscale=3, noise=.03)


# ---------------- rigid accessories bound to a single bone ----------------
def bind(o, bone):
    o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))), 1., "REPLACE"); parts.append(o)
def finish(o, mat, bone, bevel=0, sub=0, uv_cube=.25):
    o.data.materials.append(mat)
    sel(o)
    if bevel: b = o.modifiers.new("b", "BEVEL"); b.width = bevel; b.segments = 2; bpy.ops.object.modifier_apply(modifier="b")
    if sub: s = o.modifiers.new("s", "SUBSURF"); s.levels = sub; bpy.ops.object.modifier_apply(modifier="s")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if uv_cube:
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=uv_cube); bpy.ops.object.mode_set(mode="OBJECT")
    for p in o.data.polygons: p.use_smooth = True
    bind(o, bone); return o
def cyl(mat, bone, r, depth, loc, rot=(0, 0, 0), verts=24, scale=(1, 1, 1), r2=None, bevel=0):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2 if r2 is not None else r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone, bevel)
def box(mat, bone, size, loc, rot=(0, 0, 0), bevel=.006, sub=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot); o = bpy.context.object; o.scale = size; return finish(o, mat, bone, bevel, sub)
def torus(mat, bone, R, r, loc, rot=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=loc, rotation=rot, major_segments=32, minor_segments=10)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone)
def tube_path(mat, bone, pts, r):
    cu = bpy.data.curves.new("c", "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = r; cu.bevel_resolution = 3; cu.use_fill_caps = True
    sp = cu.splines.new("NURBS"); sp.points.add(len(pts) - 1)
    for idx, (p, c) in enumerate(zip(sp.points, pts)):
        p.co = (*c, 1)
        p.radius = (.35 if idx == len(pts) - 1 else (.7 if idx == len(pts) - 2 else 1.0))
    sp.use_endpoint_u = True
    o = bpy.data.objects.new("t", cu); scene.collection.objects.link(o); sel(o); bpy.ops.object.convert(target="MESH"); return finish(o, mat, bone)
def strap(mat, bone, pts, w=.03, t=.004):
    cu = bpy.data.curves.new("s", "CURVE"); cu.dimensions = "3D"; cu.extrude = w / 2; cu.bevel_depth = 0
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts) - 1)
    for p, c in zip(sp.bezier_points, pts): p.co = c; p.handle_left_type = p.handle_right_type = "AUTO"
    o = bpy.data.objects.new("s", cu); scene.collection.objects.link(o); sel(o); bpy.ops.object.convert(target="MESH")
    s = o.modifiers.new("sol", "SOLIDIFY"); s.thickness = t; bpy.ops.object.modifier_apply(modifier="sol"); return finish(o, mat, bone)

def surf_y(z, x=0., front=True):
    """front-most body surface y at height z near x"""
    c = [V[i].co.y for i in body_ids if abs(V[i].co.z - z) < .012 and abs(V[i].co.x - x) < .03]
    return (min(c) if front else max(c)) if c else (-.1 if front else .1)

hc = rig.data.bones["head"].head_local
top = max(V[i].co.z for i in body_ids) + .01
# Smooth boot toe caps & soles so toes are fully enclosed in combat boots
for s_ in (-1, 1):
    b_bone = "foot_l" if s_ > 0 else "foot_r"
    fverts = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and V[i].co.z < H * .09]
    fx = sum(v.x for v in fverts) / len(fverts)
    fy_min = min(v.y for v in fverts); fy_max = max(v.y for v in fverts)
    cyl(M["leather"], b_bone, .044, fy_max - fy_min + .02, (fx, (fy_min + fy_max) / 2 - .004, .038), rot=(math.radians(90), 0, 0), scale=(1.0, .78, 1), bevel=.012)
    box(M["leather"], b_bone, (.092, fy_max - fy_min + .025, .018), (fx, (fy_min + fy_max) / 2 - .004, .010), bevel=.004)
# eyeballs with painted iris
eimg = bpy.data.images.new("iona_eye", 256, 256); pxl = []
for y in range(256):
    for x in range(256):
        r = math.hypot(x - 128, y - 128) / 128
        hi = math.hypot(x - 146, y - 148) / 128 < .09
        c = (.98, .98, 1.0) if hi else ((.02, .015, .01) if r < .19 else ((.20, .25, .11) if r < .38 else ((.06, .07, .03) if r < .43 else (.84, .79, .75))))
        pxl += [*c, 1]
eimg.pixels = pxl; eimg.filepath_raw = TEX + "iona_eye.png"; eimg.file_format = "PNG"; eimg.save()
M["eye"] = tex_mat("Eye", "iona_eye.png", .1)
for c_ in (er, el):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=.0128, location=c_)
    o = bpy.context.object
    for l in o.data.loops:
        co = o.data.vertices[l.vertex_index].co
        o.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .0128 * .48, .5 + (co.z + .0018) / .0128 * .48) if co.y < 0 else (.02, .02)
    finish(o, M["eye"], "head", uv_cube=0)
# curly bun + loose curls
for k in range(26):
    a = random.uniform(0, 6.283); r = random.uniform(.0, .055)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=random.uniform(.028, .04), location=(math.cos(a) * r, .035 + math.sin(a) * r, top + random.uniform(-.005, .045)))
    o = bpy.context.object; o.scale = (1, 1, random.uniform(.8, 1.2))
    dm = o.modifiers.new("d", "DISPLACE"); tx = bpy.data.textures.new("curl", "CLOUDS"); tx.noise_scale = .02; dm.texture = tx; dm.strength = .012
    finish(o, M["hair"], "head", sub=1)
def ringlet(base, length, turns, rad, thick, bone="head"):
    pts = [(base[0] + math.cos(t / 20 * turns * 6.283) * rad, base[1] + math.sin(t / 20 * turns * 6.283) * rad, base[2] - length * t / 20) for t in range(21)]
    tube_path(M["hair"], bone, pts, thick)
gz = hc.z + .115; gy = min(V[i].co.y for i in verts_in(human, ["scalp", "head"], .3) & body_ids if abs(V[i].co.z - gz) < .015) - .02
for s in (-1, 1):
    for k in range(4):
        ringlet((s * (.075 + k * .008), hc.y - .02 + k * .025, hc.z + .09 - k * .01), .13 + k * .02, 3, .011, .0085)
    # Soft temple framing curls tucked outside the eyes
    for k in range(2):
        ringlet((s * (.058 + k * .012), gy + .012 + k * .008, gz - .008), .055 + k * .015, 1.4, .006, .007)
for k in range(5):
    ringlet((random.uniform(-.06, .06), hc.y + .07, hc.z + .06), .12, 3, .013, .01)
# goggles pushed up on forehead
torus(M["leather"], "head", .088, .009, (0, hc.y + .01, gz), rot=(math.radians(-12), 0, 0), scale=(1, 1.12, .9))
for s in (-1, 1):
    cyl(M["brass"], "head", .026, .022, (s * .033, gy - .006, gz), rot=(math.radians(78), 0, 0), bevel=.003)
    cyl(M["lens"], "head", .02, .024, (s * .033, gy - .008, gz), rot=(math.radians(78), 0, 0))
    torus(M["brass"], "head", .022, .004, (s * .033, gy - .019, gz + .002), rot=(math.radians(78), 0, 0))
box(M["brass"], "head", (.018, .012, .01), (0, gy - .004, gz))
# gas mask hanging on chest
mz = H * .79; my = surf_y(mz) - .035
mk = box(M["brass"], "spine_03", (.075, .05, .06), (0, my, mz), bevel=.012, sub=1)
box(M["leather"], "spine_03", (.035, .01, .03), (0, my - .027, mz - .005), bevel=.003)
for s in (-1, 1):
    cyl(M["brass"], "spine_03", .03, .035, (s * .055, my + .005, mz - .01), rot=(0, math.radians(90), math.radians(-20 * s)), bevel=.004)
    strap(M["leather"], "spine_03", [(s * .035, my, mz + .02), (s * .06, surf_y(mz + .07, s * .06) - .03, mz + .07), (s * .07, -.0, H * .865)], w=.014)
# belt + buckle
belt_ring = [i for i in body_ids if abs(V[i].co.z - waist) < .01 and abs(V[i].co.x) < .2 and i not in hand and i not in lower_arm and i not in verts_in(human, ["upperarm_l", "upperarm_r"], .2)]
bw = max(V[i].co.x for i in belt_ring); bf = min(V[i].co.y for i in belt_ring); bb = max(V[i].co.y for i in belt_ring)
torus(M["leather"], "pelvis", 1, .022, (0, (bf + bb) / 2, waist), scale=(bw + .03, (bb - bf) / 2 + .03, .9))
box(M["brass"], "pelvis", (.06, .012, .045), (0, bf - .035, waist), bevel=.004)
box(M["leather"], "pelvis", (.03, .013, .025), (0, bf - .037, waist), bevel=.002)
# bandolier diagonal from left shoulder to right hip
chest = [(x, surf_y(z, x) - .035, z) for x, z in ((.13, shoulder - .01), (.06, H * .74), (-.03, H * .66), (-.12, waist - .02))]
strap(M["leather"], "spine_02", [(.14, .02, shoulder + .02)] + chest, w=.04)
for (x, y, z) in chest[1:3]: box(M["brass"], "spine_02", (.03, .008, .022), (x, y - .006, z), bevel=.002)
# chest pockets w/ pens, hip pockets
for s in (-1, 1):
    pz = H * .735; x = s * .085
    box(M["jacketdk"], "spine_02", (.075, .014, .075), (x, surf_y(pz, x) - .028, pz), bevel=.006)
    box(M["brass"], "spine_02", (.008, .004, .008), (x, surf_y(pz, x) - .037, pz + .02), bevel=0)
    hz = H * .47; hx = s * .14
    box(M["jacketdk"], "pelvis", (.09, .02, .09), (hx, surf_y(hz, hx) - .045, hz), rot=(0, 0, -.35 * s), bevel=.006)
for k, x in enumerate((-.1, -.088, -.076)):
    pz = H * .735; y = surf_y(pz, x) - .028
    cyl(M["brass"] if k != 1 else M["paper"], "spine_02", .004, .05, (x, y, pz + .04), verts=8)
# scroll tube on back + satchel
by = max(V[i].co.y for i in body_ids if abs(V[i].co.z - H * .72) < .01) + .06
cyl(M["paper"], "spine_02", .05, .62, (-.07, by, H * .7), rot=(0, math.radians(-20), 0), bevel=.003)
for dz in (-.28, .28, .1):
    cyl(M["brass"] if dz != .1 else M["leather"], "spine_02", .054, .045 if dz != .1 else .025, (-.07 + math.sin(math.radians(-20)) * dz, by, H * .7 + math.cos(math.radians(20)) * dz), rot=(0, math.radians(-20), 0), bevel=.003)
sx_ = .19; sz_ = H * .47
box(M["leather"], "pelvis", (.16, .07, .14), (sx_, .03, sz_), bevel=.015)
box(M["jacketdk"], "pelvis", (.165, .075, .06), (sx_, .03, sz_ + .05), bevel=.008)
cyl(M["glow"], "pelvis", .01, .01, (sx_, -.009, sz_ + .04), rot=(math.radians(90), 0, 0), verts=12)
# glowing data ledger in right hand (positioned under palm/fingers so hand holds it without clipping)
hr = rig.data.bones["hand_r"]; hp = hr.head_local + (hr.tail_local - hr.head_local) * 1.2
box(M["leather"], "hand_r", (.2, .14, .014), (hp.x - .07, hp.y - .075, hp.z - .082), rot=(math.radians(25), 0, math.radians(-12)), bevel=.004)
box(M["glow"], "hand_r", (.17, .11, .004), (hp.x - .07, hp.y - .079, hp.z - .075), rot=(math.radians(25), 0, math.radians(-12)), bevel=0)

# ---------------- join, strip helper geometry, rig, export ----------------
sel(human); bm = bmesh.new(); bm.from_mesh(human.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in body_ids], context="VERTS")
bm.to_mesh(human.data); bm.free()
sel(human)
for o in parts: o.select_set(True)
bpy.ops.object.join()
iona = bpy.context.object; iona.name = "Iona"
for m in list(iona.modifiers): iona.modifiers.remove(m)
# Decimate to game-ready polycount (~38k tris) while preserving UVs and skinning weights
sel(iona)
dec = iona.modifiers.new("dec", "DECIMATE"); dec.ratio = 0.24
bpy.ops.object.modifier_apply(modifier="dec")
am = iona.modifiers.new("Armature", "ARMATURE"); am.object = rig; iona.parent = rig
rig.name = "IonaRig"
tris = sum(len(p.vertices) - 2 for p in iona.data.polygons); print("IONA_TRIS", tris)
bpy.ops.object.select_all(action="DESELECT"); iona.select_set(True); rig.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=False, add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE", use_armature_deform_only=True)
print("IONA_EXPORTED", OUT)
bpy.ops.wm.save_as_mainfile(filepath=PREV + "/iona_hq.blend")

# ---------------- previews ----------------
scene.render.engine = "BLENDER_EEVEE_NEXT"; scene.render.resolution_x = 900; scene.render.resolution_y = 1200
w = bpy.data.worlds.new("w"); scene.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (.06, .065, .07, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.2
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), 0, math.radians(35))); bpy.context.object.data.energy = 3.5
bpy.ops.object.light_add(type="AREA", location=(-1.5, 1.5, 2.2)); bpy.context.object.data.energy = 250; bpy.context.object.data.color = (.5, .85, 1)
bpy.ops.object.camera_add(); cam = bpy.context.object; scene.camera = cam; cam.data.type = "ORTHO"
for nm, loc, look, os_ in (("front", (0, -4, H * .55), (0, 0, H * .52), H * 1.1), ("three_quarter", (2.6, -3, H * .9), (0, 0, H * .52), H * 1.1),
                          ("back", (-1.5, 3.5, H * .7), (0, 0, H * .52), H * 1.1), ("face", (.5, -3, hc.z + .06), (0, 0, hc.z + .04), .38)):
    cam.location = loc; cam.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat("-Z", "Y").to_euler(); cam.data.ortho_scale = os_
    scene.render.filepath = f"{PREV}/iona_hq_{nm}.png"; bpy.ops.render.render(write_still=True)
print("IONA_PREVIEWS_DONE")

