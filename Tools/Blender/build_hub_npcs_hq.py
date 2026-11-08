# Builds Sera (Salvage Merchant) & Briggs (Debt Collector) HQ models using MPFB (MakeHuman CC0 base mesh)
# + smooth boundary-relaxed clothing + seamless spun Stahlhelm helmet + sculpted respirator/goggles
# + portrait projection bake + procedural normal maps + game_engine rig.
# Run: blender -b -P Tools/Blender/build_hub_npcs_hq.py -- <sera|briggs> <out.fbx> <preview_dir>
import bpy, bmesh, math, sys, os, random, importlib, addon_utils
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
WHO = (argv[0] if argv else "sera").lower()
OUT = argv[1] if len(argv) > 1 else f"D:/Unity/Ashfall-iOS/Assets/Resources/Characters/{WHO.capitalize()}/{WHO.capitalize()}.fbx"
PREV = argv[2] if len(argv) > 2 else "D:/Unity/AssetIntegration/IonaHQ"
TEX = f"D:/Unity/Ashfall-iOS/Assets/Resources/Characters/{WHO.capitalize()}/Textures/"
os.makedirs(TEX, exist_ok=True)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
random.seed(23 if WHO == "sera" else 37)

PORTRAIT = ("D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-sera-v11.png"
            if WHO == "sera" else
            "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-briggs-v9.png")

bpy.ops.wm.read_factory_settings(use_empty=True)
mod = next(m.__name__ for m in addon_utils.modules() if m.__name__.endswith("mpfb"))
addon_utils.enable(mod, default_set=True)
HS = importlib.import_module(mod + ".services.humanservice").HumanService
TS = importlib.import_module(mod + ".services.targetservice").TargetService
scene = bpy.context.scene

def sel(o):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o

# ---------------- male body & rig ----------------
d = TS.get_default_macro_info_dict()
if WHO == "sera":
    d.update(gender=1.0, age=0.55, muscle=0.70, weight=0.58, height=0.68, proportions=0.6)
else:
    d.update(gender=1.0, age=0.52, muscle=0.72, weight=0.52, height=0.72, proportions=0.62)
human = HS.create_human(macro_detail_dict=d)
TS.bake_targets(human)
rig = HS.add_builtin_rig(human, "game_engine")
for m in list(human.modifiers):
    if m.type == "MASK": human.modifiers.remove(m)
H = human.dimensions.z
print(f"{WHO.upper()}_HEIGHT", H)

def gidx(o, name): return o.vertex_groups[name].index
def verts_in(o, names, thr=.3):
    ids = {gidx(o, n) for n in names if n in o.vertex_groups}
    return {v.index for v in o.data.vertices if any(g.group in ids and g.weight > thr for g in v.groups)}
def centroid(o, name):
    vs = verts_in(o, [name], .5); return sum((o.data.vertices[i].co for i in vs), Vector()) / max(1, len(vs))

# ---------------- pose arms matching each portrait ----------------
sel(rig); bpy.ops.object.mode_set(mode="POSE")
def aim(bone, target_dir):
    pb = rig.pose.bones[bone]; bpy.context.view_layer.update()
    head = rig.matrix_world @ pb.head; tail = rig.matrix_world @ pb.tail
    q = (tail - head).normalized().rotation_difference(Vector(target_dir).normalized())
    pb.matrix = Matrix.Translation(pb.head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-pb.head) @ pb.matrix
    bpy.context.view_layer.update()

if WHO == "sera":
    # Right arm welcoming gesture forward, left hand resting near hip/belt
    aim("upperarm_r", (-.28, -.16, -.94)); aim("lowerarm_r", (-.16, -.90, -.32)); aim("hand_r", (-.10, -.95, -.12))
    aim("upperarm_l", (.26, .08, -.94)); aim("lowerarm_l", (-.08, -.58, -.82)); aim("hand_l", (-.14, -.48, -.88))
else:
    # Briggs: tactical pose with left hand raised forward & right hand adjusting glove/strap
    aim("upperarm_l", (.26, -.20, -.92)); aim("lowerarm_l", (.04, -.82, .48)); aim("hand_l", (.02, -.72, .60))
    aim("upperarm_r", (-.26, -.10, -.95)); aim("lowerarm_r", (.12, -.78, -.52)); aim("hand_r", (.16, -.70, -.48))

bpy.ops.object.mode_set(mode="OBJECT")
sel(human)
arm_mod = next(m for m in human.modifiers if m.type == "ARMATURE")
bpy.ops.object.modifier_apply(modifier=arm_mod.name)
sel(rig); bpy.ops.object.mode_set(mode="POSE"); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode="OBJECT")

# ---------------- skin & portrait head projection bake ----------------
body_ids = verts_in(human, ["body"], .5)
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
skin_srgb = sample(605, 455) if WHO == "sera" else sample(635, 360)
skin_rgb = [c ** 2.2 for c in skin_srgb]

if WHO == "sera":
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((660, 226)), Vector((790, 215)), Vector((760, 345))
else:
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((715, 225)), Vector((830, 240)), Vector((785, 360))
sx = (EYE_L_PX.x - EYE_R_PX.x) / (el.x - er.x)
sz = (MOUTH_PX.y - (EYE_R_PX.y + EYE_L_PX.y) / 2) / (emid.z - lips.z)
pmid = (EYE_R_PX + EYE_L_PX) / 2

me = human.data
proj = me.uv_layers.new(name="proj"); me.uv_layers.active = me.uv_layers["UVMap"]
mask = me.color_attributes.new("facemask", "FLOAT_COLOR", "POINT")
head_ids = verts_in(human, ["head", "neck_01"], .2) & body_ids
FC = pmid + Vector((0, 45)); FR = Vector((115, 145))
for v in me.vertices:
    p = Vector((pmid.x + (v.co.x - emid.x) * sx, pmid.y - (v.co.z - emid.z) * sz))
    e = ((p.x - FC.x) / FR.x) ** 2 + ((p.y - FC.y) / FR.y) ** 2
    w = max(0., min(1., (1.2 - e) / .45)) if v.index in head_ids and v.normal.y < .05 else 0.
    mask.data[v.index].color = (w, w, w, 1)
for loop in me.loops:
    co = me.vertices[loop.vertex_index].co
    proj.data[loop.index].uv = ((pmid.x + (co.x - emid.x) * sx) / IW, 1 - (pmid.y - (co.z - emid.z) * sz) / IH)

def node_mat(name):
    m = bpy.data.materials.new(name); m.use_nodes = True; return m, m.node_tree.nodes, m.node_tree.links

sel(human); bm_hide = bmesh.new(); bm_hide.from_mesh(me)
for f in bm_hide.faces: f.hide = not all(v.index in body_ids for v in f.verts)
bm_hide.to_mesh(me); bm_hide.free()

bake_m, N, L = node_mat("bake")
N.remove(N["Principled BSDF"]); out = N["Material Output"]
em = N.new("ShaderNodeEmission"); L.new(em.outputs[0], out.inputs[0])
mix = N.new("ShaderNodeMix"); mix.data_type = "RGBA"; L.new(mix.outputs[2], em.inputs[0])
noise = N.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 65
ramp = N.new("ShaderNodeValToRGB"); L.new(noise.outputs[0], ramp.inputs[0])
ramp.color_ramp.elements[0].color = (*[c * .86 for c in skin_rgb], 1); ramp.color_ramp.elements[1].color = (*[min(1, c * 1.05) for c in skin_rgb], 1)
L.new(ramp.outputs[0], mix.inputs[6])
uvn = N.new("ShaderNodeUVMap"); uvn.uv_map = "proj"
pt = N.new("ShaderNodeTexImage"); pt.image = img; pt.extension = "EXTEND"; L.new(uvn.outputs[0], pt.inputs[0]); L.new(pt.outputs[0], mix.inputs[7])
ca = N.new("ShaderNodeAttribute"); ca.attribute_name = "facemask"; L.new(ca.outputs["Fac"], mix.inputs[0])
body_img = bpy.data.images.new(f"{WHO}_body", 2048, 2048); body_img.colorspace_settings.name = "sRGB"
tgt = N.new("ShaderNodeTexImage"); tgt.image = body_img; N.active = tgt
me.materials.clear(); me.materials.append(bake_m)
scene.render.engine = "CYCLES"; scene.cycles.samples = 4; scene.cycles.device = "CPU"; scene.render.bake.margin = 8
sel(human); bpy.ops.object.bake(type="EMIT")
body_img.filepath_raw = TEX + f"{WHO}_body.png"; body_img.file_format = "PNG"; body_img.save()
me.uv_layers.remove(me.uv_layers["proj"])
bm_unhide = bmesh.new(); bm_unhide.from_mesh(me)
for f in bm_unhide.faces: f.hide = False
bm_unhide.to_mesh(me); bm_unhide.free()
print(f"{WHO.upper()}_BAKED")

# ---------------- generate high-detail procedural textures & normal maps ----------------
def make_normal_from_albedo(src_path, dst_path, strength=3.0):
    if not os.path.exists(src_path): return
    im = bpy.data.images.load(src_path)
    w, h = im.size
    # Downsample to 256x256 for fast normal map generation
    im.scale(256, 256)
    p = list(im.pixels[:])
    W, H_ = 256, 256
    def lum(x, y):
        x = x % W; y = y % H_
        idx = (y * W + x) * 4
        return 0.299 * p[idx] + 0.587 * p[idx + 1] + 0.114 * p[idx + 2]
    npx = []
    for y in range(H_):
        for x in range(W):
            dx = (lum(x + 1, y) - lum(x - 1, y)) * strength
            dy = (lum(x, y + 1) - lum(x, y - 1)) * strength
            v = Vector((-dx, -dy, 1.0)).normalized()
            npx += [v.x * .5 + .5, v.y * .5 + .5, v.z * .5 + .5, 1.0]
    nim = bpy.data.images.new("nm", W, H_)
    nim.pixels = npx; nim.filepath_raw = dst_path; nim.file_format = "PNG"; nim.save()
    bpy.data.images.remove(im); bpy.data.images.remove(nim)

for base_tex in ("iona_leather", "iona_brass", "iona_pants", "npc_helmet_olive",
                 "sera_blue_jacket" if WHO == "sera" else "briggs_coat_brown",
                 "sera_apron_metal" if WHO == "sera" else "briggs_vest_navy"):
    make_normal_from_albedo(TEX + base_tex + ".png", TEX + base_tex + "_n.png", 3.5)

# Goggle lens texture sampled from portrait goggle area + green glass specular highlight & visible eye hint
limg = bpy.data.images.new(f"{WHO}_lens", 256, 256); lpx = []
goggle_cx, goggle_cy, goggle_r = (660, 224, 52) if WHO == "sera" else (722, 225, 48)
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        # Sample portrait goggle lens interior
        sx_p = int(max(0, min(IW - 1, goggle_cx + nx * goggle_r)))
        sy_p = int(max(0, min(IH - 1, goggle_cy - ny * goggle_r)))
        pi = ((IH - 1 - sy_p) * IW + sx_p) * 4
        pc = [px[pi], px[pi + 1], px[pi + 2]]
        # Blend with deep emerald/olive glass & crisp curved window reflection
        rim = max(0., min(1., (r - .78) / .20))
        spec = math.exp(-(((nx + .28) / .26) ** 2 + ((ny - .32) / .16) ** 2)) * .55
        c = [max(0., min(1., (pc[k] * .75 + (.08, .22, .14)[k] * .35) * (1 - rim * .65) + spec * (.7, .95, .8)[k])) for k in range(3)]
        lpx += [*c, 1]
limg.pixels = lpx; limg.filepath_raw = TEX + f"{WHO}_lens.png"; limg.file_format = "PNG"; limg.save()

# Perforated brass canister cap texture (dark recessed holes + brass rim)
cimg = bpy.data.images.new(f"{WHO}_canister_cap", 256, 256); cpx = []
holes = [(0, 0)]
for ring_r, cnt in ((.28, 6), (.54, 12), (.76, 16)):
    for k in range(cnt):
        a = k * 2 * math.pi / cnt + (ring_r * 1.3)
        holes.append((math.cos(a) * ring_r, math.sin(a) * ring_r))
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        d_hole = min(math.hypot(nx - hx, ny - hy) for hx, hy in holes)
        in_hole = d_hole < .068 and r < .84
        bevel_ring = .068 <= d_hole < .092 and r < .84
        outer_rim = .84 < r < .94
        if in_hole:
            c = (.04, .035, .03)
        elif bevel_ring:
            c = (.62, .48, .26)
        elif outer_rim:
            c = (.54, .41, .22)
        else:
            v = .88 + .12 * math.sin(nx * 18) * math.cos(ny * 18) - .22 * r
            c = (.44 * v, .33 * v, .17 * v)
        cpx += [*c, 1]
cimg.pixels = cpx; cimg.filepath_raw = TEX + f"{WHO}_canister_cap.png"; cimg.file_format = "PNG"; cimg.save()

# Front respirator louver/grille texture (dark steel mask with recessed slots/holes)
fimg = bpy.data.images.new(f"{WHO}_vent", 256, 256); fpx = []
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        if WHO == "sera":
            d_hole = min(math.hypot(nx - hx, ny - hy) for hx, hy in holes)
            slot = d_hole < .072 and r < .78
        else:
            slot = abs(nx) < .58 and abs(ny) < .68 and (int((ny + .7) * 14) % 2 == 0)
        if slot:
            c = (.03, .03, .035)
        elif r > .82:
            c = (.26, .24, .22) if WHO == "sera" else (.45, .34, .18)
        else:
            c = (.18, .17, .16) if WHO == "sera" else (.36, .27, .15)
        fpx += [*c, 1]
fimg.pixels = fpx; fimg.filepath_raw = TEX + f"{WHO}_vent.png"; fimg.file_format = "PNG"; fimg.save()

# Gauge dial texture for Sera
gimg = bpy.data.images.new(f"{WHO}_dial", 256, 256); gpx = []
for y in range(256):
    for x in range(256):
        dx, dy = (x - 128) / 128, (y - 128) / 128
        r = math.hypot(dx, dy)
        needle = abs(dx * .7 - dy * .7) < .035 and dx > -.1 and dy > -.1 and r < .78
        tick = .72 < r < .88 and (int((math.atan2(dy, dx) + math.pi) * 16 / math.pi) % 2 == 0)
        c = (.10, .07, .05) if (needle or tick or r > .88 or r < .09) else (.82, .77, .66) * (1.0 - .18 * r) if isinstance((.82, .77, .66), float) else tuple(v * (1.0 - .18 * r) for v in (.82, .77, .66))
        gpx += [*c, 1]
gimg.pixels = gpx; gimg.filepath_raw = TEX + f"{WHO}_dial.png"; gimg.file_format = "PNG"; gimg.save()

def tex_mat(name, path, rough=.82, metal=0., color=None):
    m, N, L = node_mat(f"{WHO.capitalize()}_{name}"); b = N["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if path and os.path.exists(path):
        t = N.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(path); L.new(t.outputs[0], b.inputs["Base Color"])
        b.inputs["Base Color"].default_value = (1, 1, 1, 1)
    if color: b.inputs["Base Color"].default_value = (*color, 1)
    return m

M = dict(
    skin=tex_mat("Skin", TEX + f"{WHO}_body.png", .65),
    leather=tex_mat("Leather", TEX + "iona_leather.png", .55),
    brass=tex_mat("Brass", TEX + "iona_brass.png", .32, .88),
    pants=tex_mat("Pants", TEX + "iona_pants.png", .88),
    hair=tex_mat("Hair", TEX + "iona_hair.png", .82),
    helmet=tex_mat("Helmet", TEX + "npc_helmet_olive.png", .48, .45),
    jacket=tex_mat("Jacket", TEX + ("sera_blue_jacket.png" if WHO == "sera" else "briggs_coat_brown.png"), .88),
    armor=tex_mat("Armor", TEX + ("sera_apron_metal.png" if WHO == "sera" else "briggs_vest_navy.png"), .48 if WHO == "sera" else .85, .62 if WHO == "sera" else .08),
    hood=tex_mat("Hood", TEX + ("npc_helmet_olive.png" if WHO == "sera" else "briggs_scarf_dark.png"), .9),
    lens=tex_mat("Lens", TEX + f"{WHO}_lens.png", .08, .35),
    canister=tex_mat("Canister", TEX + f"{WHO}_canister_cap.png", .35, .85),
    vent=tex_mat("Vent", TEX + f"{WHO}_vent.png", .42, .65),
    dial=tex_mat("Dial", TEX + f"{WHO}_dial.png", .25, .1),
    shell_red=tex_mat("ShellRed", None, .45, .1, color=(.52, .10, .07)),
    steel=tex_mat("Steel", TEX + "sera_apron_metal.png", .32, .85),
)
me.materials.clear(); me.materials.append(M["skin"])

# ---------------- fitted garments with boundary smoothing & bevel ----------------
parts = []
def smooth_boundaries(o, iterations=12, factor=0.65):
    bm = bmesh.new(); bm.from_mesh(o.data)
    for _ in range(iterations):
        bverts = [v for v in bm.verts if v.is_boundary]
        new_co = {}
        for v in bverts:
            nb = [e.other_vert(v).co for e in v.link_edges if e.is_boundary]
            if not nb:
                nb = [e.other_vert(v).co for e in v.link_edges]
            if nb:
                avg = sum(nb, Vector()) / len(nb)
                new_co[v] = v.co.lerp(avg, factor)
        for v, co in new_co.items():
            v.co = co
    bm.to_mesh(o.data); bm.free()

def extract(name, keep, offset, mat, solid=.005, uvscale=6., smooth_iter=14, sub=1):
    o = human.copy(); o.data = human.data.copy(); o.name = name; scene.collection.objects.link(o)
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in keep], context="VERTS")
    # Also delete loose vertices/edges and single-triangle spikes on boundaries
    for _ in range(2):
        spiky = [f for f in bm.faces if sum(1 for e in f.edges if e.is_boundary) >= 2]
        if spiky: bmesh.ops.delete(bm, geom=spiky, context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.to_mesh(o.data); bm.free()
    if smooth_iter:
        smooth_boundaries(o, iterations=smooth_iter, factor=0.65)
    o.data.materials.clear(); o.data.materials.append(mat)
    for u in o.data.uv_layers:
        for l in u.data: l.uv = l.uv * uvscale
    sel(o)
    if offset:
        dm = o.modifiers.new("off", "DISPLACE"); dm.strength = offset; dm.mid_level = 0
        bpy.ops.object.modifier_apply(modifier="off")
    if solid:
        s = o.modifiers.new("sol", "SOLIDIFY"); s.thickness = solid; s.offset = 1; s.use_rim = True
        bpy.ops.object.modifier_apply(modifier="sol")
    if sub:
        sb = o.modifiers.new("sub", "SUBSURF"); sb.levels = sub; bpy.ops.object.modifier_apply(modifier="sub")
    for p in o.data.polygons: p.use_smooth = True
    parts.append(o); return o

V = human.data.vertices
def wz(i): return V[i].co.z
shoulder = rig.data.bones["clavicle_l"].head_local.z
hip = rig.data.bones["pelvis"].head_local.z
waist = hip + (shoulder - hip) * .25
lower_arm = verts_in(human, ["lowerarm_l", "lowerarm_r"], .5)
hand = verts_in(human, ["hand_l", "hand_r"], .35)
torso_grp = verts_in(human, ["pelvis", "spine_01", "spine_02", "spine_03", "clavicle_l", "clavicle_r", "upperarm_l", "upperarm_r", "lowerarm_l", "lowerarm_r"], .25)
neck = verts_in(human, ["neck_01", "head"], .25)

def arm_t(i):
    co = V[i].co; s = "l" if co.x > 0 else "r"
    el_b = rig.data.bones["lowerarm_" + s].head_local; wr_b = rig.data.bones["hand_" + s].head_local
    return (co - el_b).dot(wr_b - el_b) / (wr_b - el_b).length_squared

if WHO == "sera":
    # Sera: rolled-up blue work jacket + heavy armored metal chest apron + fingerless leather gloves
    jacket_ids = {i for i in torso_grp & body_ids if i not in neck and i not in hand and wz(i) > hip - .03 and (i not in lower_arm or arm_t(i) < .50)}
    extract("WorkJacket", jacket_ids, .016, M["jacket"], solid=.006, uvscale=5)
    cuff_ids = {i for i in lower_arm & body_ids if .34 < arm_t(i) < .54}
    extract("RolledCuffs", cuff_ids, .026, M["hood"], solid=.008, uvscale=4)
    apron_ids = {i for i in verts_in(human, ["spine_01", "spine_02", "spine_03"], .30) & body_ids if waist - .025 < wz(i) < shoulder - .015 and V[i].co.y < .01 and abs(V[i].co.x) < .155 and i not in neck}
    extract("MetalApron", apron_ids, .024, M["armor"], solid=.009, uvscale=3.5)
    finger_tips = verts_in(human, ["index_02_l", "middle_02_l", "ring_02_l", "pinky_02_l", "thumb_02_l", "index_02_r", "middle_02_r", "ring_02_r", "pinky_02_r", "thumb_02_r", "index_03_l", "middle_03_l", "ring_03_l", "pinky_03_l", "thumb_03_l", "index_03_r", "middle_03_r", "ring_03_r", "pinky_03_r", "thumb_03_r"], .4)
    extract("Gloves", (hand & body_ids) - finger_tips, .004, M["leather"], solid=.0025, uvscale=4, sub=0)
else:
    # Briggs: rugged brown canvas duster coat with open front showing dark navy tactical vest + full leather gloves
    coat_ids = {i for i in torso_grp & body_ids if i not in neck and i not in hand and wz(i) > hip - .04 and (i not in lower_arm or arm_t(i) < .65) and not (V[i].co.y < -.02 and abs(V[i].co.x) < .090 and wz(i) < shoulder - .01)}
    extract("DusterCoat", coat_ids, .024, M["jacket"], solid=.007, uvscale=5)
    cuff_ids = {i for i in lower_arm & body_ids if .48 < arm_t(i) < .68}
    extract("CoatCuffs", cuff_ids, .031, M["jacket"], solid=.008, uvscale=4)
    # Coat skirt overlapping seamlessly with upper DusterCoat up to waist+.05, with clean plane bisect at front opening
    skirt = extract("CoatSkirt", {i for i in verts_in(human, ["helper-skirt"], .5) if H * .27 < V[i].co.z < waist + .05}, .024, M["jacket"], solid=0, uvscale=22, smooth_iter=8, sub=0)
    sel(skirt); bm = bmesh.new(); bm.from_mesh(skirt.data)
    # Delete front-center strip cleanly and relax boundary
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < -.02 and abs(f.calc_center_median().x) < .088], context="FACES")
    bm.to_mesh(skirt.data); bm.free()
    smooth_boundaries(skirt, iterations=18, factor=0.75)
    sel(skirt)
    s_ = skirt.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .007; s_.offset = 1; bpy.ops.object.modifier_apply(modifier="sol")
    sb_ = skirt.modifiers.new("sub", "SUBSURF"); sb_.levels = 1; bpy.ops.object.modifier_apply(modifier="sub")
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.uv.cube_project(cube_size=.30); bpy.ops.object.mode_set(mode="OBJECT")
    vest_ids = {i for i in verts_in(human, ["spine_01", "spine_02", "spine_03", "pelvis"], .30) & body_ids if hip + .02 < wz(i) < shoulder - .01 and abs(V[i].co.x) < .155 and i not in neck}
    extract("TacticalVest", vest_ids, .014, M["armor"], solid=.006, uvscale=4.5)
    extract("FullGloves", hand & body_ids, .0045, M["leather"], solid=.0025, uvscale=4, sub=0)

wrist_ids = {i for i in lower_arm & body_ids if arm_t(i) > .70}
extract("WristCuffs", wrist_ids, .013, M["leather"], solid=.005, uvscale=4)
legs = verts_in(human, ["thigh_l", "thigh_r", "calf_l", "calf_r", "pelvis"], .3) & body_ids
extract("CargoPants", {i for i in legs if H * .11 < V[i].co.z < waist + .02 and i not in hand and i not in lower_arm}, .010, M["pants"], solid=.005, uvscale=5)
boot_ids = {i for i in body_ids if wz(i) < H * .21 and i in verts_in(human, ["calf_l", "calf_r", "foot_l", "foot_r", "ball_l", "ball_r"], .2)}
boots = extract("CombatBoots", boot_ids, .0, M["leather"], solid=0, uvscale=4, smooth_iter=10, sub=0)
sel(boots); sm = boots.modifiers.new("sm", "SMOOTH"); sm.factor = 1; sm.iterations = 35; bpy.ops.object.modifier_apply(modifier="sm")
dm = boots.modifiers.new("d", "DISPLACE"); dm.strength = .019; dm.mid_level = 0; bpy.ops.object.modifier_apply(modifier="d")
s = boots.modifiers.new("s", "SOLIDIFY"); s.thickness = .006; bpy.ops.object.modifier_apply(modifier="s")
# Extracted boot ankle & shin straps directly from calf mesh so they conform 100% to leg & boot surface
strap_ids = {i for i in verts_in(human, ["calf_l", "calf_r"], .25) & body_ids if any(abs(wz(i) - bz) < .012 for bz in (H * .085, H * .135, H * .185))}
extract("BootStraps", strap_ids, .022, M["leather"], solid=.004, uvscale=4, smooth_iter=6, sub=0)
extract("HairUnderHelmet", verts_in(human, ["scalp"], .2) & body_ids, .009, M["hair"], solid=.005, uvscale=3, sub=0)

# ---------------- rigid & sculpted accessories ----------------
def bind(o, bone):
    o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))), 1., "REPLACE"); parts.append(o)
def finish(o, mat, bone, bevel=0, sub=0, uv_cube=.25):
    o.data.materials.append(mat); sel(o)
    if bevel: b = o.modifiers.new("b", "BEVEL"); b.width = bevel; b.segments = 2; bpy.ops.object.modifier_apply(modifier="b")
    if sub: s_ = o.modifiers.new("s", "SUBSURF"); s_.levels = sub; bpy.ops.object.modifier_apply(modifier="s")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if uv_cube:
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=uv_cube); bpy.ops.object.mode_set(mode="OBJECT")
    for p in o.data.polygons: p.use_smooth = True
    bind(o, bone); return o
def cyl(mat, bone, r, depth, loc, rot=(0, 0, 0), verts=28, scale=(1, 1, 1), r2=None, bevel=0, uv_cube=.25):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2 if r2 is not None else r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone, bevel, uv_cube=uv_cube)
def box(mat, bone, size, loc, rot=(0, 0, 0), bevel=.005, sub=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot); o = bpy.context.object; o.scale = size; return finish(o, mat, bone, bevel, sub)
def torus(mat, bone, R, r, loc, rot=(0, 0, 0), scale=(1, 1, 1), segs=36, msegs=14):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=loc, rotation=rot, major_segments=segs, minor_segments=msegs)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone)
def sphere(mat, bone, r, loc, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=r, location=loc)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone)
def strap(mat, bone, pts, w=.032, t=.005):
    cu = bpy.data.curves.new("s", "CURVE"); cu.dimensions = "3D"; cu.extrude = w / 2; cu.bevel_depth = .0015; cu.bevel_resolution = 2
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts) - 1)
    for p, c in zip(sp.bezier_points, pts): p.co = c; p.handle_left_type = p.handle_right_type = "AUTO"
    o = bpy.data.objects.new("s", cu); scene.collection.objects.link(o); sel(o); bpy.ops.object.convert(target="MESH")
    s_ = o.modifiers.new("sol", "SOLIDIFY"); s_.thickness = t; bpy.ops.object.modifier_apply(modifier="sol"); return finish(o, mat, bone)
def surf_y(z, x=0., front=True):
    c = [V[i].co.y for i in body_ids if abs(V[i].co.z - z) < .015 and abs(V[i].co.x - x) < .038]
    return (min(c) if front else max(c)) if c else (-.12 if front else .12)

# ---------------- Sculpted Smooth Neck Collar / Scarf & Shoulder Armor ----------------
# Smooth multi-fold neck cloth wrap covering neck base down to chest collar seamlessly
for k, (rz, rr, mr, tilt) in enumerate(((.005, .112, .028, 12), (.028, .104, .025, 4), (.052, .095, .022, 8), (.072, .088, .019, 6))):
    o = torus(M["hood"], "neck_01", rr, mr, (0, .006, shoulder + rz), rot=(math.radians(tilt), math.radians(3 * (k - 1)), 0), scale=(1.08, 1.15, .95))
    # Add subtle organic cloth folds via displacement
    sel(o); dm = o.modifiers.new("f", "DISPLACE"); tx = bpy.data.textures.new(f"sc_{k}", "CLOUDS"); tx.noise_scale = .045
    dm.texture = tx; dm.strength = .008; bpy.ops.object.modifier_apply(modifier="f")

# Sculpted layered shoulder armor / pauldrons + upper-sleeve cargo pockets
for s_ in (-1, 1):
    ub = "upperarm_l" if s_ > 0 else "upperarm_r"
    cb = "clavicle_l" if s_ > 0 else "clavicle_r"
    if WHO == "sera":
        # Sera: thick contoured leather shoulder pad + riveted steel plate sitting cleanly on top of shoulder
        box(M["leather"], cb, (.092, .125, .018), (s_ * .142, -.005, shoulder + .032), rot=(math.radians(8), math.radians(22 * s_), 0), bevel=.007, sub=1)
        box(M["armor"], cb, (.078, .102, .014), (s_ * .142, -.006, shoulder + .042), rot=(math.radians(8), math.radians(22 * s_), 0), bevel=.005, sub=1)
        for rx in (-1, 1):
            for ry in (-1, 1):
                sphere(M["brass"], cb, .005, (s_ * .142 + rx * .025, -.006 + ry * .032, shoulder + .049))
        # Upper-sleeve pleated flap pocket on outer deltoid
        ua_head = rig.data.bones[ub].head_local
        ua_tail = rig.data.bones[ub].tail_local
        ua_mid = ua_head.lerp(ua_tail, .42)
        px_a = ua_mid.x + s_ * .048; py_a = ua_mid.y - .010; pz_a = ua_mid.z
        box(M["jacket"], ub, (.028, .076, .088), (px_a, py_a, pz_a), rot=(0, math.radians(-16 * s_), 0), bevel=.006)
        box(M["jacket"], ub, (.032, .080, .030), (px_a + s_ * .004, py_a, pz_a + .032), rot=(0, math.radians(-16 * s_), 0), bevel=.004)
        sphere(M["brass"], ub, .005, (px_a + s_ * .018, py_a, pz_a + .024))
    else:
        # Briggs: standing coat collar + 2-layer weathered canvas shoulder capelet/mantle with rivets over shoulders
        torus(M["jacket"], "neck_01", .132, .022, (0, .012, shoulder + .042), rot=(math.radians(14), 0, 0), scale=(1.12, 1.18, 1.55))
        for lk, (dz_m, sc_m) in enumerate(((.042, 1.0), (.012, 1.15))):
            box(M["jacket"], cb, (.115 * sc_m, .148 * sc_m, .014), (s_ * (.148 + lk * .020), -.004, shoulder + dz_m),
                rot=(math.radians(6), math.radians((26 + lk * 8) * s_), 0), bevel=.006, sub=1)
        for ry in (-.038, 0, .038):
            sphere(M["brass"], cb, .005, (s_ * .148, ry, shoulder + .052))

# ---------------- Boot Toe Caps, Laces, Straps & Knee Pads ----------------
for s_ in (-1, 1):
    b_bone = "foot_l" if s_ > 0 else "foot_r"
    c_bone = "calf_l" if s_ > 0 else "calf_r"
    fverts = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and V[i].co.z < H * .09]
    fx = sum(v.x for v in fverts) / len(fverts)
    fy_min = min(v.y for v in fverts); fy_max = max(v.y for v in fverts)
    cyl(M["leather"], b_bone, .048, fy_max - fy_min + .024, (fx, (fy_min + fy_max) / 2 - .004, .040), rot=(math.radians(90), 0, 0), scale=(1.04, .80, 1), bevel=.012)
    box(M["leather"], b_bone, (.100, fy_max - fy_min + .030, .022), (fx, (fy_min + fy_max) / 2 - .004, .011), bevel=.004)
    # Boot side buckles
    for bz in (H * .085, H * .135, H * .185):
        cv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(V[i].co.z - bz) < .015]
        if cv:
            cx_b = (max(v.x for v in cv) if s_ > 0 else min(v.x for v in cv)) + s_ * .018
            cy_b = sum(v.y for v in cv) / len(cv)
            box(M["brass"], c_bone, (.014, .022, .018), (cx_b, cy_b, bz), bevel=.003)
    # Sculpted leather knee pad + thigh cargo pocket
    kz = H * .285
    kv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(V[i].co.z - kz) < .018]
    if kv:
        kx = sum(v.x for v in kv) / len(kv); ky = min(v.y for v in kv)
        box(M["leather"], c_bone, (.082, .024, .095), (kx, ky - .010, kz), bevel=.010, sub=1)
        torus(M["leather"], c_bone, .068, .006, (kx, ky + .055, kz), scale=(1.05, 1.12, .8))
    tz = H * .41
    tv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(V[i].co.z - tz) < .018]
    if tv:
        tx_p = (max(v.x for v in tv) if s_ > 0 else min(v.x for v in tv)) + s_ * .012
        ty_p = sum(v.y for v in tv) / len(tv)
        box(M["pants"], "thigh_l" if s_ > 0 else "thigh_r", (.028, .095, .115), (tx_p, ty_p, tz), bevel=.006)
        box(M["pants"], "thigh_l" if s_ > 0 else "thigh_r", (.032, .098, .032), (tx_p + s_ * .002, ty_p, tz + .045), bevel=.004)
        sphere(M["brass"], "thigh_l" if s_ > 0 else "thigh_r", .0045, (tx_p + s_ * .016, ty_p, tz + .035))

# ---------------- Seamless One-Piece Spun Stahlhelm Combat Helmet ----------------
hc = rig.data.bones["head"].head_local
# Create continuous Stahlhelm profile via bmesh lathe/spin so there is zero gap between dome and brim
bm = bmesh.new()
# Profile points (radius, z) from crown top down to flared rim with rolled lip
prof = [
    (0.000,  0.145),
    (0.038,  0.142),
    (0.074,  0.130),
    (0.102,  0.108),
    (0.118,  0.078),
    (0.123,  0.046),
    (0.125,  0.024),  # brow transition
    (0.136,  0.008),  # flare start
    (0.150, -0.010),  # flared skirt
    (0.154, -0.016),  # rolled outer rim
    (0.147, -0.019),  # rolled under-lip
]
SEGS = 48
rings = []
for r_p, z_p in prof:
    ring = []
    for k in range(SEGS):
        a = k * 2 * math.pi / SEGS
        x = math.sin(a) * r_p * 1.01
        y = -math.cos(a) * r_p * 1.15
        # Slight upward curve at front visor & lower drop at back neck guard
        front_lift = 0.014 * max(0., math.cos(a)) * (1.0 if z_p < 0.04 else 0.3)
        back_drop = -0.012 * max(0., -math.cos(a)) * (1.0 if z_p < 0.04 else 0.2)
        v = bm.verts.new((x, hc.y + .014 + y, emid.z + .018 + z_p + front_lift + back_drop))
        ring.append(v)
    rings.append(ring)
for r0, r1 in zip(rings[:-1], rings[1:]):
    for k in range(SEGS):
        bm.faces.new((r0[k], r0[(k + 1) % SEGS], r1[(k + 1) % SEGS], r1[k]))
hmesh = bpy.data.meshes.new("Stahlhelm")
bm.to_mesh(hmesh); bm.free()
helm = bpy.data.objects.new("Stahlhelm", hmesh); scene.collection.objects.link(helm)
sel(helm)
s_ = helm.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .006; s_.offset = -1; bpy.ops.object.modifier_apply(modifier="sol")
finish(helm, M["helmet"], "head", sub=1, uv_cube=.35)

# Leather helmet strap resting neatly along the brow ridge + dome ventilation grommets/rivets
torus(M["leather"], "head", .127, .0075, (0, hc.y + .012, emid.z + .034), rot=(math.radians(6), 0, 0), scale=(1.01, 1.15, .95))
for s_ in (-1, 1):
    # Side vent lug + dome rivets
    torus(M["brass"], "head", .008, .0025, (s_ * .102, hc.y - .035, emid.z + .088), rot=(0, math.radians(72 * s_), math.radians(-20 * s_)))
    sphere(M["brass"], "head", .0045, (s_ * .092, hc.y - .065, emid.z + .064))
    sphere(M["brass"], "head", .0045, (s_ * .115, hc.y + .025, emid.z + .055))
    # Chinstrap descending from helmet rim along jawline under respirator
    strap(M["leather"], "head", [(s_ * .108, hc.y - .015, emid.z + .018), (s_ * .092, hc.y - .022, emid.z - .028), (s_ * .058, lips.y + .012, lips.z - .025)], w=.013, t=.003)

if WHO == "briggs":
    # Briggs's front NVG shroud plate with recessed frame + side rails + perforated ear-pro pods
    box(M["helmet"], "head", (.050, .014, .048), (0, emid.y - .102, emid.z + .086), rot=(math.radians(-22), 0, 0), bevel=.004)
    box(M["brass"], "head", (.034, .016, .032), (0, emid.y - .105, emid.z + .086), rot=(math.radians(-22), 0, 0), bevel=.003)
    for s_ in (-1, 1):
        cyl(M["canister"], "head", .034, .020, (s_ * .130, hc.y + .008, emid.z + .028), rot=(0, math.radians(85 * s_), 0), bevel=.004, uv_cube=0)
        torus(M["brass"], "head", .034, .004, (s_ * .136, hc.y + .008, emid.z + .028), rot=(0, math.radians(85 * s_), 0))

# ---------------- Sculpted Goggles & Contoured Respirator Mask ----------------
# Leather goggle head-band + contoured brass/leather frame around both eye cups
torus(M["leather"], "head", .112, .009, (0, hc.y + .012, emid.z + .004), scale=(.99, 1.10, .82))
for s_ in (-1, 1):
    gx, gy, gz = s_ * .038, emid.y - .018, emid.z + .003
    # Outer leather cushion cup
    cyl(M["leather"], "head", .034, .026, (gx, gy + .004, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)), bevel=.004)
    # Brass barrel + knurled front ring
    cyl(M["brass"], "head", .031, .024, (gx, gy - .002, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)), bevel=.003)
    torus(M["brass"], "head", .029, .0045, (gx, gy - .013, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)))
    # Side hinge bracket
    box(M["brass"], "head", (.016, .018, .016), (s_ * .072, gy + .008, gz), rot=(0, 0, math.radians(-15 * s_)), bevel=.002)
    # Planar-mapped green glass lens with portrait eye reflection
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.026, depth=.008, location=(gx, gy - .011, gz), rotation=(math.radians(90), 0, math.radians(-10 * s_)))
    lo = bpy.context.object; sel(lo); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for l in lo.data.loops:
        co = lo.data.vertices[l.vertex_index].co
        lo.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .052, .5 + co.z / .052)
    finish(lo, M["lens"], "head", uv_cube=0)
# Nose bridge bar between goggle cups
box(M["brass"], "head", (.024, .014, .010), (0, emid.y - .026, emid.z + .005), bevel=.003)

# Contoured Faceplate Shell fitting smoothly over nose bridge, cheeks, and jaw
my, mz = lips.y - .020, (emid.z + lips.z) / 2 - .010
mask_mat = M["steel"] if WHO == "sera" else M["brass"]
# Main sculpted faceplate (subdivided & tapered prism hugging the face)
bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=.058, location=(0, my + .006, mz))
fplate = bpy.context.object; fplate.scale = (.92, .95, 1.05)
sel(fplate); bm = bmesh.new(); bm.from_mesh(fplate.data)
for v in bm.verts:
    # Taper top into nose bridge between goggles and widen cheeks
    if v.co.z > 0:
        v.co.x *= max(.32, 1.0 - v.co.z * 9.5)
        v.co.y -= v.co.z * .25
    if v.co.y > .015:
        v.co.y = .015
bm.to_mesh(fplate.data); bm.free()
finish(fplate, mask_mat, "head", sub=1)
# Rubber/leather face seal rim against cheeks
torus(M["leather"], "head", .052, .007, (0, my + .012, mz + .002), rot=(math.radians(78), 0, 0), scale=(.92, 1.08, .9))

# Front central breather valve housing with perforated/louvered cap UV
cyl(M["brass"], "head", .028, .022, (0, my - .042, mz - .012), rot=(math.radians(96), 0, 0), bevel=.004)
torus(M["brass"], "head", .027, .0035, (0, my - .051, mz - .013), rot=(math.radians(96), 0, 0))
bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.024, depth=.006, location=(0, my - .051, mz - .013), rotation=(math.radians(96), 0, 0))
vo = bpy.context.object; sel(vo); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
for l in vo.data.loops:
    co = vo.data.vertices[l.vertex_index].co
    vo.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .048, .5 + co.z / .048)
finish(vo, M["vent"], "head", uv_cube=0)

# Twin angled brass filter canisters with perforated face caps
for s_ in (-1, 1):
    cx, cy, cz = s_ * .060, my - .006, mz - .014
    rot_c = (math.radians(76), 0, math.radians(-46 * s_))
    cyl(M["brass"], "head", .034, .036, (cx, cy, cz), rot=rot_c, bevel=.004)
    torus(M["brass"], "head", .034, .004, (cx + s_ * .011, cy - .011, cz - .003), rot=rot_c)
    torus(M["brass"], "head", .034, .0035, (cx - s_ * .008, cy + .008, cz + .002), rot=rot_c)
    # Perforated front cap disk
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.032, depth=.006, location=(cx + s_ * .013, cy - .013, cz - .004), rotation=rot_c)
    co_obj = bpy.context.object; sel(co_obj); bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for l in co_obj.data.loops:
        co = co_obj.data.vertices[l.vertex_index].co
        co_obj.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .064, .5 + co.y / .064)
    finish(co_obj, M["canister"], "head", uv_cube=0)

if WHO == "briggs":
    # Curved ribbed breathing hose from lower snout into dark scarf
    for k in range(10):
        t = k / 9.
        hx = math.sin(t * 1.2) * .008
        hy = my - .036 + (t ** 1.3) * .042
        hz = mz - .032 - t * .072
        torus(M["leather"], "head", .014, .0042, (hx, hy, hz), rot=(math.radians(28 + t * 25), 0, 0))

# ---------------- High-Detail Torso, Chest & Belt Equipment ----------------
sh_span = shoulder - waist
belt_ring = [i for i in body_ids if abs(V[i].co.z - waist) < .012 and abs(V[i].co.x) < .22 and i not in hand and i not in lower_arm and i not in verts_in(human, ["upperarm_l", "upperarm_r"], .2)]
bw = max(V[i].co.x for i in belt_ring); bf = min(V[i].co.y for i in belt_ring); bb = max(V[i].co.y for i in belt_ring)
torus(M["leather"], "pelvis", 1, .024, (0, (bf + bb) / 2, waist), scale=(bw + .035, (bb - bf) / 2 + .035, .95))
# Heavy brass belt buckle frame + tongue
box(M["brass"], "pelvis", (.066, .016, .054), (0, bf - .038, waist), bevel=.005)
box(M["leather"], "pelvis", (.042, .018, .034), (0, bf - .040, waist), bevel=.003)
box(M["brass"], "pelvis", (.026, .020, .007), (.012, bf - .042, waist), bevel=.002)

z_upper = waist + sh_span * .74
z_mid = waist + sh_span * .50
z_low = waist + sh_span * .26
for s_ in (-1, 1):
    pts = [(s_ * .108, .05, shoulder - .01), (s_ * .108, -.02, shoulder + .025), (s_ * .092, surf_y(z_upper, s_ * .092) - .036, z_upper), (s_ * .085, surf_y(waist + .03, s_ * .085) - .036, waist + .03)]
    strap(M["leather"], "spine_02", pts, w=.042, t=.006)
    box(M["brass"], "spine_02", (.046, .012, .032), (s_ * .092, surf_y(z_upper, s_ * .092) - .041, z_upper), bevel=.004)
    torus(M["brass"], "spine_02", .022, .0045, (s_ * .090, surf_y(z_upper - .035, s_ * .090) - .040, z_upper - .035), rot=(math.radians(82), 0, 0), scale=(1.1, .8, 1))

if WHO == "sera":
    # Sera's upper chest bib plate + riveted apron border + side leather straps
    zy_bib = surf_y(z_upper + .02, 0) - .034
    box(M["armor"], "spine_03", (.115, .014, .058), (0, zy_bib, z_upper + .025), bevel=.005)
    for rx in (-.042, 0, .042):
        sphere(M["brass"], "spine_03", .0045, (rx, zy_bib - .007, z_upper + .042))
    # Lower apron pocket seam + corner rivets
    zy_low = surf_y(z_low, 0) - .034
    box(M["armor"], "spine_01", (.185, .012, .095), (0, zy_low, z_low - .01), bevel=.006)
    for rx in (-.078, -.028, .028, .078):
        sphere(M["brass"], "spine_01", .0045, (rx, zy_low - .006, waist + .025))
        sphere(M["brass"], "spine_02", .0045, (rx, surf_y(z_upper - .02, rx) - .034, z_upper - .02))
    # Diagonal leather bandolier (left shoulder to right hip)
    b_pts = [(.12, -.01, shoulder + .02)] + [(x, surf_y(z, x) - .040, z) for x, z in ((.07, z_upper), (.01, z_mid), (-.05, z_low), (-.12, waist + .01))]
    strap(M["leather"], "spine_02", b_pts, w=.044, t=.007)
    # Pressure gauge high on left shoulder strap with brass bezel & dial
    gx, gy, gz = .098, surf_y(z_upper + .035, .098) - .044, z_upper + .035
    cyl(M["brass"], "spine_03", .032, .020, (gx, gy, gz), rot=(math.radians(85), 0, math.radians(-12)), bevel=.004)
    torus(M["brass"], "spine_03", .031, .004, (gx, gy - .009, gz), rot=(math.radians(85), 0, math.radians(-12)))
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.026, depth=.008, location=(gx, gy - .008, gz), rotation=(math.radians(85), 0, math.radians(-12)))
    go = bpy.context.object; sel(go); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for l in go.data.loops:
        co = go.data.vertices[l.vertex_index].co
        go.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .052, .5 + co.z / .052)
    finish(go, M["dial"], "spine_03", uv_cube=0)
    # 8 shotgun shells (brass base + red hull + leather retention loop) along bandolier
    for k, t_s in enumerate((.16, .25, .34, .43, .62, .71, .80, .89)):
        sx_s = .09 - t_s * .19; sz_s = (shoulder - .03) - t_s * (sh_span - .05)
        sy_s = surf_y(sz_s, sx_s) - .048
        rot_sh = (0, math.radians(-42), 0)
        cyl(M["brass"], "spine_02", .009, .022, (sx_s + .005, sy_s, sz_s + .005), rot=rot_sh, verts=16, bevel=.001)
        cyl(M["shell_red"], "spine_02", .0086, .024, (sx_s - .009, sy_s, sz_s - .009), rot=rot_sh, verts=16, bevel=.001)
        cyl(M["leather"], "spine_02", .0102, .012, (sx_s - .002, sy_s + .001, sz_s - .002), rot=rot_sh, verts=16)
    # Detailed Heavy Pipe Wrench (I-beam handle, hooked upper jaw, lower jaw, knurled adjustment nut, hanging hole)
    wy = surf_y(z_mid, 0) - .044
    w_rot = (0, math.radians(36), 0)
    box(M["steel"], "spine_02", (.022, .012, .22), (.005, wy, z_mid - .015), rot=w_rot, bevel=.003)
    box(M["steel"], "spine_02", (.056, .016, .032), (-.062, wy - .002, z_mid + .062), rot=w_rot, bevel=.004)
    box(M["steel"], "spine_02", (.048, .015, .022), (-.044, wy - .002, z_mid + .035), rot=w_rot, bevel=.003)
    cyl(M["brass"], "spine_02", .014, .022, (-.028, wy - .002, z_mid + .028), rot=w_rot, verts=16, bevel=.002)
    torus(M["steel"], "spine_02", .012, .004, (.065, wy, z_mid - .095), rot=(math.radians(90), math.radians(36), 0))
    # Detailed Brass Vernier Caliper (graduated main beam + fixed & sliding jaws)
    c_rot = (0, math.radians(-48), 0)
    box(M["brass"], "spine_02", (.018, .007, .19), (.042, wy - .006, z_mid + .018), rot=c_rot, bevel=.002)
    box(M["brass"], "spine_02", (.054, .008, .018), (-.012, wy - .007, z_mid + .062), rot=c_rot, bevel=.002)
    box(M["brass"], "spine_02", (.048, .009, .022), (.018, wy - .007, z_mid + .036), rot=c_rot, bevel=.002)
    # Right hip leather salvage pouch + flap + buckle + 2 tool cylinders
    box(M["leather"], "pelvis", (.115, .068, .125), (-.145, bf - .012, waist - .035), rot=(0, 0, math.radians(16)), bevel=.012)
    box(M["leather"], "pelvis", (.120, .072, .045), (-.145, bf - .015, waist + .012), rot=(0, 0, math.radians(16)), bevel=.008)
    box(M["brass"], "pelvis", (.028, .012, .032), (-.145, bf - .048, waist - .015), rot=(0, 0, math.radians(16)), bevel=.003)
    for tx_c in (-.075, -.052):
        cyl(M["leather"], "pelvis", .013, .085, (tx_c, bf - .032, waist - .035), rot=(0, math.radians(8), 0), bevel=.003)
        cyl(M["brass"], "pelvis", .011, .035, (tx_c, bf - .032, waist + .012), rot=(0, math.radians(8), 0), bevel=.002)
else:
    # Briggs: horizontal MOLLE webbing rows across navy tactical vest
    for kz_m in (z_upper - .02, z_mid + .04, z_mid - .05, z_low - .02):
        pts_m = [(x_m, surf_y(kz_m, x_m) - .028, kz_m) for x_m in (-.085, -.03, .03, .085)]
        strap(M["armor"], "spine_02", pts_m, w=.018, t=.004)
    # Diagonal strap (right shoulder to left hip) with brass rounds & buckle
    b_pts = [(-.12, -.01, shoulder + .02)] + [(x, surf_y(z, x) - .040, z) for x, z in ((-.06, z_upper), (0, z_mid), (.06, z_low), (.12, waist + .01))]
    strap(M["leather"], "spine_02", b_pts, w=.042, t=.006)
    box(M["brass"], "spine_02", (.044, .012, .032), (.01, surf_y(z_mid + .02, .01) - .044, z_mid + .02), rot=(0, math.radians(42), 0), bevel=.003)
    for t_s in (.16, .24, .32, .40):
        sx_s = -.08 + t_s * .16; sz_s = (shoulder - .03) - t_s * (sh_span - .05)
        sy_s = surf_y(sz_s, sx_s) - .046
        cyl(M["brass"], "spine_02", .0085, .038, (sx_s, sy_s, sz_s), rot=(0, math.radians(42), 0), verts=14, bevel=.002)
        cyl(M["leather"], "spine_02", .0098, .014, (sx_s, sy_s + .001, sz_s), rot=(0, math.radians(42), 0), verts=14)
    # 3 contoured vertical navy MOLLE mag pouches across chest with flaps, pull tabs & brass snaps
    for mx_p in (-.068, -.004, .060):
        mz_p = z_mid - .015; my_p = surf_y(mz_p, mx_p) - .038
        box(M["armor"], "spine_02", (.052, .034, .105), (mx_p, my_p, mz_p), bevel=.007)
        box(M["armor"], "spine_02", (.055, .038, .038), (mx_p, my_p - .003, mz_p + .036), bevel=.005)
        box(M["armor"], "spine_02", (.016, .008, .042), (mx_p, my_p - .020, mz_p + .005), bevel=.002)
        sphere(M["brass"], "spine_02", .005, (mx_p, my_p - .022, mz_p + .024))
    # Belt leather pouches + flaps + brass snaps
    for bx_p in (-.115, -.052, .135):
        box(M["leather"], "pelvis", (.060, .046, .085), (bx_p, bf - .026, waist - .012), bevel=.008)
        box(M["leather"], "pelvis", (.064, .050, .030), (bx_p, bf - .028, waist + .018), bevel=.005)
        sphere(M["brass"], "pelvis", .005, (bx_p, bf - .052, waist + .008))
    # Forearm leather bracers with dual straps & brass rivets
    for s_ in (-1, 1):
        lb = "lowerarm_l" if s_ > 0 else "lowerarm_r"
        wr_p = rig.data.bones["hand_l" if s_ > 0 else "hand_r"].head_local
        el_p = rig.data.bones[lb].head_local
        mid_f = el_p.lerp(wr_p, .76)
        torus(M["leather"], lb, .042, .007, mid_f, rot=(math.radians(65), 0, math.radians(15 * s_)))
    # Backpack & rolled canvas bedroll with spiral end-seam on back
    by_b = max(V[i].co.y for i in body_ids if abs(V[i].co.z - z_mid) < .015) + .055
    box(M["jacket"], "spine_02", (.25, .12, .29), (0, by_b + .035, z_mid), bevel=.022)
    cyl(M["jacket"], "spine_03", .065, .36, (0, by_b + .025, shoulder + .022), rot=(0, math.radians(90), 0), bevel=.010)
    for sx_b in (-.10, .10):
        torus(M["leather"], "spine_03", .068, .009, (sx_b, by_b + .025, shoulder + .022), rot=(0, math.radians(90), 0))
        box(M["brass"], "spine_03", (.018, .016, .024), (sx_b, by_b + .085, shoulder + .022), bevel=.003)

# ---------------- join, strip helper geometry, decimate, rig, export ----------------
sel(human); bm = bmesh.new(); bm.from_mesh(human.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in body_ids], context="VERTS")
bm.to_mesh(human.data); bm.free()
sel(human)
for o in parts: o.select_set(True)
bpy.ops.object.join()
npc = bpy.context.object; npc.name = WHO.capitalize()
for m in list(npc.modifiers): npc.modifiers.remove(m)
sel(npc)
dec = npc.modifiers.new("dec", "DECIMATE"); dec.ratio = 0.34
bpy.ops.object.modifier_apply(modifier="dec")
am = npc.modifiers.new("Armature", "ARMATURE"); am.object = rig; npc.parent = rig
rig.name = f"{WHO.capitalize()}Rig"
tris = sum(len(p.vertices) - 2 for p in npc.data.polygons); print(f"{WHO.upper()}_TRIS", tris)
bpy.ops.object.select_all(action="DESELECT"); npc.select_set(True); rig.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=False, add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE", use_armature_deform_only=True)
print(f"{WHO.upper()}_EXPORTED", OUT)

# ---------------- previews ----------------
scene.render.engine = "BLENDER_EEVEE_NEXT"; scene.render.resolution_x = 900; scene.render.resolution_y = 1200
w = bpy.data.worlds.new("w"); scene.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (.06, .065, .07, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.25
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), 0, math.radians(35))); bpy.context.object.data.energy = 3.6
bpy.ops.object.light_add(type="AREA", location=(-1.5, -1.8, 2.2)); bpy.context.object.data.energy = 220; bpy.context.object.data.color = (.85, .9, 1)
bpy.ops.object.camera_add(); cam = bpy.context.object; scene.camera = cam; cam.data.type = "ORTHO"
for nm, loc, look, os_ in (("front", (0, -4, H * .53), (0, 0, H * .53), H * 1.18),
                          ("three_quarter", (2.6, -3, H * .90), (0, 0, H * .53), H * 1.18),
                          ("face", (.35, -3, emid.z + .01), (0, 0, emid.z), .38)):
    cam.location = loc; cam.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat("-Z", "Y").to_euler(); cam.data.ortho_scale = os_
    scene.render.filepath = f"{PREV}/{WHO}_hq_{nm}.png"; bpy.ops.render.render(write_still=True)
print(f"{WHO.upper()}_PREVIEWS_DONE")
