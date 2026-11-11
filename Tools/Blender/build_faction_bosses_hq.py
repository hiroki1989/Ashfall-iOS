# Builds HQ combat-rigged Faction Boss models for Raven (Red Claw) and Vex (Dusk Coyotes)
# using MPFB (MakeHuman CC0 base mesh) + game_engine armature (kept LIVE for combat animation)
# + bespoke portrait-accurate armor/helmets/crests/claws/medallions + portrait bake + normal maps.
# Run:
#   BLENDER_USER_RESOURCES=D:/Unity/AssetIntegration/IonaHQ/blender-user blender -b -P Tools/Blender/build_faction_bosses_hq.py -- <raven|vex>
import bpy, bmesh, math, sys, os, random, shutil, importlib, addon_utils
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
WHO = (argv[0] if argv else "raven").lower()
assert WHO in ("raven", "vex"), f"Unknown boss: {WHO}"

CAP = WHO.capitalize()
OUT = argv[1] if len(argv) > 1 else f"D:/Unity/Ashfall-iOS/Assets/Resources/Characters/{CAP}/{CAP}.fbx"
PREV = argv[2] if len(argv) > 2 else "D:/Unity/AssetIntegration/FactionBossesHQ"
TEX = f"D:/Unity/Ashfall-iOS/Assets/Resources/Characters/{CAP}/Textures/"
BRIGGS_TEX = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/Briggs/Textures/"
os.makedirs(TEX, exist_ok=True)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
os.makedirs(PREV, exist_ok=True)
random.seed(101 if WHO == "raven" else 202)

PORTRAIT = (
    "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-raven-v8.png"
    if WHO == "raven" else
    "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-vex-v8.png"
)

# Copy shared base textures from Briggs/Iona (leather, brass, pants, hair) cleanly
for shared_fn in ("iona_leather.png", "iona_brass.png", "iona_pants.png", "iona_hair.png"):
    src = os.path.join(BRIGGS_TEX, shared_fn)
    dst = os.path.join(TEX, shared_fn)
    if os.path.exists(src):
        shutil.copy2(src, dst)

bpy.ops.wm.read_factory_settings(use_empty=True)
mod = next(m.__name__ for m in addon_utils.modules() if m.__name__.endswith("mpfb"))
addon_utils.enable(mod, default_set=True)
HS = importlib.import_module(mod + ".services.humanservice").HumanService
TS = importlib.import_module(mod + ".services.targetservice").TargetService
scene = bpy.context.scene

def sel(o):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

# ---------------- Athletic boss male body + game_engine rig (A-pose kept intact for Unity animation) ----------------
d = TS.get_default_macro_info_dict()
if WHO == "raven":
    d.update(gender=1.0, age=0.45, muscle=0.82, weight=0.58, height=0.74, proportions=0.64)
else:
    d.update(gender=1.0, age=0.42, muscle=0.72, weight=0.52, height=0.70, proportions=0.60)
human = HS.create_human(macro_detail_dict=d)
TS.bake_targets(human)
rig = HS.add_builtin_rig(human, "game_engine")
for m in list(human.modifiers):
    if m.type == "MASK":
        human.modifiers.remove(m)
H = human.dimensions.z
print(f"{WHO.upper()}_HEIGHT", H)

def gidx(o, name): return o.vertex_groups[name].index
def verts_in(o, names, thr=.3):
    ids = {gidx(o, n) for n in names if n in o.vertex_groups}
    return {v.index for v in o.data.vertices if any(g.group in ids and g.weight > thr for g in v.groups)}
def centroid(o, name):
    vs = verts_in(o, [name], .5)
    return sum((o.data.vertices[i].co for i in vs), Vector()) / max(1, len(vs))

# Apply subtle combat-ready A-pose on arms? No—keep standard MPFB game_engine A-pose directly so
# HumanFactionVisual runtime bone offsets align identically between Raven and Vex!
body_ids = verts_in(human, ["body"], .5)
er, el, lips = centroid(human, "helper-r-eye"), centroid(human, "helper-l-eye"), centroid(human, "lips")
emid = (er + el) / 2

# ---------------- Portrait skin projection bake ----------------
img = bpy.data.images.load(PORTRAIT)
IW, IH = img.size
px = list(img.pixels[:])

def sample(x, y, r=6):
    acc = [0.0, 0.0, 0.0]; n = 0
    for yy in range(max(0, y - r), min(IH, y + r)):
        for xx in range(max(0, x - r), min(IW, x + r)):
            i = ((IH - 1 - yy) * IW + xx) * 4
            if px[i + 3] > 0.2:
                acc = [acc[k] + px[i + k] for k in range(3)]
                n += 1
    return [c / max(1, n) for c in acc]

# Sample warm tanned cheek/skin near temple/forearm
skin_srgb = sample(610, 310) if WHO == "raven" else sample(640, 235)
# Ensure readable human skin brightness even if shadowed in portrait
if sum(skin_srgb) < 0.65:
    skin_srgb = [0.58, 0.42, 0.34]
skin_rgb = [c ** 2.2 for c in skin_srgb]

if WHO == "raven":
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((672, 286)), Vector((813, 331)), Vector((745, 425))
else:
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((690, 245)), Vector((797, 293)), Vector((742, 385))
sx = (EYE_L_PX.x - EYE_R_PX.x) / max(0.001, (el.x - er.x))
sz = (MOUTH_PX.y - (EYE_R_PX.y + EYE_L_PX.y) / 2) / max(0.001, (emid.z - lips.z))
pmid = (EYE_R_PX + EYE_L_PX) / 2

def node_mat(name):
    m = bpy.data.materials.new(name); m.use_nodes = True
    return m, m.node_tree.nodes, m.node_tree.links

me = human.data
proj = me.uv_layers.new(name="proj"); me.uv_layers.active = me.uv_layers["UVMap"]
mask = me.color_attributes.new("facemask", "FLOAT_COLOR", "POINT")
head_ids = verts_in(human, ["head", "neck_01"], .2) & body_ids
FC = pmid + Vector((0, 42)); FR = Vector((110, 138))
for v in me.vertices:
    p = Vector((pmid.x + (v.co.x - emid.x) * sx, pmid.y - (v.co.z - emid.z) * sz))
    e = ((p.x - FC.x) / FR.x) ** 2 + ((p.y - FC.y) / FR.y) ** 2
    w = max(0., min(1., (1.2 - e) / .45)) if v.index in head_ids and v.normal.y < .05 else 0.
    mask.data[v.index].color = (w, w, w, 1)
for loop in me.loops:
    co = me.vertices[loop.vertex_index].co
    proj.data[loop.index].uv = ((pmid.x + (co.x - emid.x) * sx) / IW, 1 - (pmid.y - (co.z - emid.z) * sz) / IH)

sel(human); bm_hide = bmesh.new(); bm_hide.from_mesh(me)
for f in bm_hide.faces: f.hide = not all(v.index in body_ids for v in f.verts)
bm_hide.to_mesh(me); bm_hide.free()

bake_m, N, L = node_mat("bake")
N.remove(N["Principled BSDF"]); out = N["Material Output"]
em = N.new("ShaderNodeEmission"); L.new(em.outputs[0], out.inputs[0])
mix = N.new("ShaderNodeMix"); mix.data_type = "RGBA"; L.new(mix.outputs[2], em.inputs[0])
noise = N.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 65
ramp = N.new("ShaderNodeValToRGB"); L.new(noise.outputs[0], ramp.inputs[0])
ramp.color_ramp.elements[0].color = (*[c * .86 for c in skin_rgb], 1)
ramp.color_ramp.elements[1].color = (*[min(1, c * 1.05) for c in skin_rgb], 1)
L.new(ramp.outputs[0], mix.inputs[6])
uvn = N.new("ShaderNodeUVMap"); uvn.uv_map = "proj"
pt = N.new("ShaderNodeTexImage"); pt.image = img; pt.extension = "EXTEND"
L.new(uvn.outputs[0], pt.inputs[0]); L.new(pt.outputs[0], mix.inputs[7])
ca = N.new("ShaderNodeAttribute"); ca.attribute_name = "facemask"; L.new(ca.outputs["Fac"], mix.inputs[0])
body_img = bpy.data.images.new(f"{WHO}_body", 1024, 1024); body_img.colorspace_settings.name = "sRGB"
tgt = N.new("ShaderNodeTexImage"); tgt.image = body_img; N.active = tgt
me.materials.clear(); me.materials.append(bake_m)
scene.render.engine = "CYCLES"; scene.cycles.samples = 4; scene.cycles.device = "CPU"; scene.render.bake.margin = 6
sel(human); bpy.ops.object.bake(type="EMIT")
body_img.filepath_raw = TEX + f"{WHO}_body.png"; body_img.file_format = "PNG"; body_img.save()
me.uv_layers.remove(me.uv_layers["proj"])
bm_unhide = bmesh.new(); bm_unhide.from_mesh(me)
for f in bm_unhide.faces: f.hide = False
bm_unhide.to_mesh(me); bm_unhide.free()

# ---------------- Procedural weathered textures + painted emblems + normal maps ----------------
def save_rgba(name, W, H_, pixels):
    im = bpy.data.images.new(name, W, H_); im.pixels = pixels
    im.filepath_raw = TEX + name + ".png"; im.file_format = "PNG"; im.save()
    return TEX + name + ".png"

def make_fabric_tex(name, base_col, grime_col, weave_freq=90, grit=0.14):
    W = 512; out_p = []
    for y in range(W):
        ny = y / W
        for x in range(W):
            nx = x / W
            w = 0.5 + 0.5 * math.sin(nx * weave_freq * math.pi) * math.sin(ny * weave_freq * math.pi)
            n = 0.5 + 0.3 * math.sin(nx * 23 + math.cos(ny * 17)) + 0.2 * math.cos(nx * 41 - ny * 37)
            stain = max(0., min(1., 0.35 * math.sin(nx * 9 + ny * 7) + 0.35 * math.cos(nx * 19 - ny * 13)))
            c = [
                max(0., min(1., base_col[k] * (0.90 + 0.16 * w + grit * (n - .5)) * (1 - stain * 0.22) + grime_col[k] * stain * 0.22))
                for k in range(3)
            ]
            out_p += [*c, 1.0]
    return save_rgba(name, W, W, out_p)

def make_normal_from_albedo(src_path, dst_path, strength=3.2):
    if not os.path.exists(src_path): return
    im = bpy.data.images.load(src_path)
    im.scale(256, 256)
    p = list(im.pixels[:]); W = 256
    def lum(x, y):
        idx = ((y % W) * W + (x % W)) * 4
        return 0.299 * p[idx] + 0.587 * p[idx + 1] + 0.114 * p[idx + 2]
    npx = []
    for y in range(W):
        for x in range(W):
            dx = (lum(x + 1, y) - lum(x - 1, y)) * strength
            dy = (lum(x, y + 1) - lum(x, y - 1)) * strength
            v = Vector((-dx, -dy, 1.0)).normalized()
            npx += [v.x * .5 + .5, v.y * .5 + .5, v.z * .5 + .5, 1.0]
    nim = bpy.data.images.new("nm", W, W); nim.pixels = npx
    nim.filepath_raw = dst_path; nim.file_format = "PNG"; nim.save()
    bpy.data.images.remove(im); bpy.data.images.remove(nim)

# Emerald Goggle lens sampled from portrait
limg = bpy.data.images.new(f"{WHO}_lens", 256, 256); lpx = []
g_cx, g_cy, g_r = (int(EYE_R_PX.x), int(EYE_R_PX.y), 44)
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        sx_p = int(max(0, min(IW - 1, g_cx + nx * g_r)))
        sy_p = int(max(0, min(IH - 1, g_cy - ny * g_r)))
        pi = ((IH - 1 - sy_p) * IW + sx_p) * 4
        pc = [px[pi], px[pi + 1], px[pi + 2]]
        rim = max(0., min(1., (r - .76) / .22))
        spec = math.exp(-(((nx + .28) / .24) ** 2 + ((ny - .30) / .16) ** 2)) * .55
        c = [max(0., min(1., (pc[k] * .75 + (.09, .30, .12)[k] * .35) * (1 - rim * .65) + spec * (.7, .96, .75)[k])) for k in range(3)]
        lpx += [*c, 1]
limg.pixels = lpx; limg.filepath_raw = TEX + f"{WHO}_lens.png"; limg.file_format = "PNG"; limg.save()

# Canister cap & front respirator grille vent
holes = [(0, 0)]
for ring_r, cnt in ((.28, 6), (.54, 12), (.76, 16)):
    for k in range(cnt):
        a = k * 2 * math.pi / cnt + ring_r * 1.3
        holes.append((math.cos(a) * ring_r, math.sin(a) * ring_r))
cimg = bpy.data.images.new(f"{WHO}_canister_cap", 256, 256); cpx = []
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        d_hole = min(math.hypot(nx - hx, ny - hy) for hx, hy in holes)
        if d_hole < .068 and r < .84: c = (.04, .035, .03)
        elif d_hole < .092 and r < .84: c = (.56, .43, .25) if WHO == "raven" else (.48, .45, .38)
        elif .84 < r < .94: c = (.48, .36, .20) if WHO == "raven" else (.38, .36, .32)
        else:
            v = .88 + .12 * math.sin(nx * 18) * math.cos(ny * 18) - .22 * r
            c = (.40 * v, .30 * v, .17 * v) if WHO == "raven" else (.34 * v, .33 * v, .30 * v)
        cpx += [*c, 1]
cimg.pixels = cpx; cimg.filepath_raw = TEX + f"{WHO}_canister_cap.png"; cimg.file_format = "PNG"; cimg.save()

fimg = bpy.data.images.new(f"{WHO}_vent", 256, 256); fpx = []
for y in range(256):
    for x in range(256):
        nx, ny = (x - 128) / 128., (y - 128) / 128.
        r = math.hypot(nx, ny)
        slot = abs(nx) < .58 and abs(ny) < .68 and (int((ny + .7) * 14) % 2 == 0)
        if slot: c = (.03, .03, .03)
        elif r > .82: c = (.38, .28, .17) if WHO == "raven" else (.35, .34, .32)
        else: c = (.28, .21, .14) if WHO == "raven" else (.25, .25, .24)
        fpx += [*c, 1]
fimg.pixels = fpx; fimg.filepath_raw = TEX + f"{WHO}_vent.png"; fimg.file_format = "PNG"; fimg.save()

if WHO == "raven":
    make_fabric_tex("raven_coat", (.14, .12, .11), (.06, .04, .04), 85, .16)
    make_fabric_tex("raven_crimson", (.34, .07, .07), (.12, .03, .03), 105, .14)
    make_fabric_tex("raven_armor", (.30, .065, .055), (.11, .04, .03), 42, .18)
    make_fabric_tex("raven_claw", (.58, .58, .60), (.24, .22, .20), 30, .14)
    # 512x512 painted emblem texture for front helmet & right pauldron (3 crisp white diagonal claw slashes)
    W = 512; slp = []
    for y in range(W):
        ny = y / W; dy = ny - .52
        for x in range(W):
            nx = x / W; dx = nx - .50
            n = 0.5 + 0.3 * math.sin(nx * 29 + ny * 17)
            base = [.29 * (.88 + .22 * n), .062 * (.88 + .22 * n), .054 * (.88 + .22 * n)]
            # Three jagged diagonal slash streaks
            hit = False
            for off in (-0.11, 0.0, 0.11):
                u = (dx - off) * 0.84 + dy * 0.54
                v = -(dx - off) * 0.54 + dy * 0.84
                taper = max(0., 1.0 - (v / 0.24) ** 2)
                wiggle = 0.005 * math.sin(v * 65 + off * 40)
                if abs(v) < 0.24 and abs(u + wiggle) < 0.022 * taper:
                    hit = True
            col = (.86, .82, .77) if hit else tuple(base)
            slp += [*col, 1.0]
    save_rgba("raven_slashes", W, W, slp)
    for fn in ("iona_leather", "iona_brass", "iona_pants", "raven_coat", "raven_crimson", "raven_armor", "raven_slashes"):
        make_normal_from_albedo(TEX + fn + ".png", TEX + fn + "_n.png", 3.4)
else:
    make_fabric_tex("vex_coat", (.14, .18, .25), (.08, .08, .08), 88, .15)
    make_fabric_tex("vex_scarf", (.42, .38, .32), (.20, .18, .15), 115, .18)
    make_fabric_tex("vex_metal", (.24, .25, .27), (.12, .10, .08), 36, .16)
    make_fabric_tex("vex_bone", (.76, .70, .58), (.42, .36, .28), 32, .14)
    # 512x512 Slate-blue helmet & medallion texture with white howling coyote silhouette!
    W = 512; hxp = []; medp = []
    def coyote_shape(nx, ny):
        # Stylized leaping/prowling coyote silhouette centered near (0.5, 0.5)
        cx, cy = (nx - .50), (ny - .48)
        torso = (cx / .18) ** 2 + (cy / .07) ** 2 < 1.0
        chest = ((cx - .11) / .09) ** 2 + ((cy - .02) / .075) ** 2 < 1.0
        neck = abs(cx - .17) < .05 and 0.0 < cy < .13
        head = ((cx - .22) / .07) ** 2 + ((cy - .13) / .042) ** 2 < 1.0
        snout = (0.24 < cx < 0.32) and (0.115 < cy < 0.145 - (cx - .24) * .3)
        ear1 = abs(cx - .195) < .014 and 0.15 < cy < 0.21
        ear2 = abs(cx - .218) < .012 and 0.15 < cy < 0.205
        fleg = (0.12 < cx + cy * .45 < 0.17) and (-0.16 < cy < 0.0)
        fleg2 = (0.04 < cx - cy * .35 < 0.09) and (-0.15 < cy < 0.0)
        bleg = (-0.17 < cx - cy * .45 < -0.11) and (-0.16 < cy < 0.02)
        tail = (-0.33 < cx < -0.14) and abs(cy - (0.02 - (cx + .14) * .35)) < .024
        return torso or chest or neck or head or snout or ear1 or ear2 or fleg or fleg2 or bleg or tail
    for y in range(W):
        ny = y / W
        for x in range(W):
            nx = x / W
            n = 0.5 + 0.3 * math.sin(nx * 25 + ny * 19)
            base_h = [.16 * (.88 + .22 * n), .21 * (.88 + .22 * n), .30 * (.88 + .22 * n)]
            emb = coyote_shape(nx, ny)
            hxp += [*( (.84, .82, .78) if emb else tuple(base_h) ), 1.0]
            # Medallion coin
            r = math.hypot(nx - .5, ny - .5)
            rim_m = 0.42 < r < 0.48
            coin_emb = coyote_shape((nx - .5) * 1.15 + .5, (ny - .5) * 1.15 + .5) and r < 0.41
            mc = (.64, .54, .34) if (rim_m or coin_emb) else (.26, .22, .16)
            medp += [*mc, 1.0]
    save_rgba("vex_emblem", W, W, hxp)
    save_rgba("vex_medallion", W, W, medp)
    for fn in ("iona_leather", "iona_brass", "iona_pants", "vex_coat", "vex_scarf", "vex_metal", "vex_emblem"):
        make_normal_from_albedo(TEX + fn + ".png", TEX + fn + "_n.png", 3.4)

def tex_mat(name, path, rough=.80, metal=0., color=None):
    m, N, L = node_mat(f"{CAP}_{name}"); b = N["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if path and os.path.exists(path):
        t = N.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(path)
        L.new(t.outputs[0], b.inputs["Base Color"]); b.inputs["Base Color"].default_value = (1, 1, 1, 1)
    if color: b.inputs["Base Color"].default_value = (*color, 1)
    return m

if WHO == "raven":
    M = dict(
        skin=tex_mat("Skin", TEX + "raven_body.png", .65),
        leather=tex_mat("Leather", TEX + "iona_leather.png", .50),
        brass=tex_mat("Brass", TEX + "iona_brass.png", .32, .85),
        pants=tex_mat("Pants", TEX + "iona_pants.png", .88),
        hair=tex_mat("Hair", TEX + "iona_hair.png", .82),
        coat=tex_mat("Coat", TEX + "raven_coat.png", .88),
        scarf=tex_mat("Scarf", TEX + "raven_crimson.png", .90),
        armor=tex_mat("Armor", TEX + "raven_armor.png", .44, .52),
        emblem=tex_mat("Emblem", TEX + "raven_slashes.png", .42, .55),
        claw=tex_mat("Claw", TEX + "raven_claw.png", .26, .92),
        lens=tex_mat("Lens", TEX + "raven_lens.png", .08, .35),
        canister=tex_mat("Canister", TEX + "raven_canister_cap.png", .35, .85),
        vent=tex_mat("Vent", TEX + "raven_vent.png", .40, .70),
    )
else:
    M = dict(
        skin=tex_mat("Skin", TEX + "vex_body.png", .65),
        leather=tex_mat("Leather", TEX + "iona_leather.png", .52),
        brass=tex_mat("Brass", TEX + "iona_brass.png", .34, .85),
        pants=tex_mat("Pants", TEX + "iona_pants.png", .88),
        hair=tex_mat("Hair", TEX + "iona_hair.png", .82),
        coat=tex_mat("Coat", TEX + "vex_coat.png", .86),
        scarf=tex_mat("Scarf", TEX + "vex_scarf.png", .92),
        armor=tex_mat("Armor", TEX + "vex_metal.png", .44, .65),
        emblem=tex_mat("Emblem", TEX + "vex_emblem.png", .45, .48),
        medallion=tex_mat("Medallion", TEX + "vex_medallion.png", .32, .82),
        bone=tex_mat("Bone", TEX + "vex_bone.png", .42, .05),
        lens=tex_mat("Lens", TEX + "vex_lens.png", .08, .35),
        canister=tex_mat("Canister", TEX + "vex_canister_cap.png", .35, .82),
        vent=tex_mat("Vent", TEX + "vex_vent.png", .40, .72),
    )
me.materials.clear(); me.materials.append(M["skin"])

# ---------------- Fitted clothes & helpers (adapted cleanly from build_hub_npcs_hq.py) ----------------
parts = []
hi_parts = []
def smooth_boundaries(o, iterations=12, factor=0.65):
    bm = bmesh.new(); bm.from_mesh(o.data)
    for _ in range(iterations):
        bverts = [v for v in bm.verts if v.is_boundary]
        new_co = {}
        for v in bverts:
            nb = [e.other_vert(v).co for e in v.link_edges if e.is_boundary] or [e.other_vert(v).co for e in v.link_edges]
            if nb: new_co[v] = v.co.lerp(sum(nb, Vector()) / len(nb), factor)
        for v, co in new_co.items(): v.co = co
    bm.to_mesh(o.data); bm.free()

def extract(name, keep, offset, mat, solid=.005, uvscale=5., smooth_iter=14, sub=1):
    o = human.copy(); o.data = human.data.copy(); o.name = name; scene.collection.objects.link(o)
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in keep], context="VERTS")
    for _ in range(2):
        spiky = [f for f in bm.faces if sum(1 for e in f.edges if e.is_boundary) >= 2]
        if spiky: bmesh.ops.delete(bm, geom=spiky, context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.to_mesh(o.data); bm.free()
    if smooth_iter: smooth_boundaries(o, iterations=smooth_iter, factor=0.65)
    o.data.materials.clear(); o.data.materials.append(mat)
    for u in o.data.uv_layers:
        for l in u.data: l.uv = l.uv * uvscale
    sel(o)
    if offset:
        dm = o.modifiers.new("off", "DISPLACE"); dm.strength = offset; dm.mid_level = 0; bpy.ops.object.modifier_apply(modifier="off")
    if solid:
        s = o.modifiers.new("sol", "SOLIDIFY"); s.thickness = solid; s.offset = 1; s.use_rim = True; bpy.ops.object.modifier_apply(modifier="sol")
    if sub:
        sb = o.modifiers.new("sub", "SUBSURF"); sb.levels = sub; bpy.ops.object.modifier_apply(modifier="sub")
    for p in o.data.polygons: p.use_smooth = True
    hi_parts.append(o); return o

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
    return (co - el_b).dot(wr_b - el_b) / max(.001, (wr_b - el_b).length_squared)

def surf_y(z, x=0., front=True):
    c = [V[i].co.y for i in body_ids if abs(V[i].co.z - z) < .015 and abs(V[i].co.x - x) < .038]
    return (min(c) if front else max(c)) if c else (-.12 if front else .12)

def bind(o, bone):
    o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))), 1., "REPLACE")
    parts.append(o)

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

def cyl(mat, bone, r, depth, loc, rot=(0, 0, 0), verts=24, scale=(1, 1, 1), r2=None, bevel=0, uv_cube=.25):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2 if r2 is not None else r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone, bevel, uv_cube=uv_cube)

def box(mat, bone, size, loc, rot=(0, 0, 0), bevel=.005, sub=0, uv_cube=.25):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.object; o.scale = size; return finish(o, mat, bone, bevel, sub, uv_cube=uv_cube)

def torus(mat, bone, R, r, loc, rot=(0, 0, 0), scale=(1, 1, 1), segs=32, msegs=12):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=loc, rotation=rot, major_segments=segs, minor_segments=msegs)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone)

def sphere(mat, bone, r, loc, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=8, radius=r, location=loc)
    o = bpy.context.object; o.scale = scale; return finish(o, mat, bone)

def strap(mat, bone, pts, w=.032, t=.005):
    cu = bpy.data.curves.new("s", "CURVE"); cu.dimensions = "3D"; cu.extrude = w / 2; cu.bevel_depth = .0015; cu.bevel_resolution = 2
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts) - 1)
    for p, c in zip(sp.bezier_points, pts): p.co = c; p.handle_left_type = p.handle_right_type = "AUTO"
    o = bpy.data.objects.new("s", cu); scene.collection.objects.link(o); sel(o); bpy.ops.object.convert(target="MESH")
    s_ = o.modifiers.new("sol", "SOLIDIFY"); s_.thickness = t; bpy.ops.object.modifier_apply(modifier="sol")
    return finish(o, mat, bone)

# ---------------- Base Garments (Heavy coat, rolled cuffs, crimson sleeves, fingerless gloves, pants, boots) ----------------
coat_ids = {i for i in torso_grp & body_ids if i not in neck and i not in hand and wz(i) > hip - .04 and (i not in lower_arm or arm_t(i) < .62)}
extract("BossCoat", coat_ids, .022, M["coat"], solid=.007, uvscale=5)

cuff_mat = M["scarf"] if WHO == "raven" else M["coat"]
cuff_ids = {i for i in lower_arm & body_ids if .44 < arm_t(i) < .66}
extract("RolledCuffs", cuff_ids, .028, cuff_mat, solid=.008, uvscale=4)

# Trench coat lower skirt
skirt = extract("CoatSkirt", {i for i in verts_in(human, ["helper-skirt"], .5) if H * .29 < V[i].co.z < waist + .05}, .024, M["coat"], solid=0, uvscale=22, smooth_iter=8, sub=0)
sel(skirt); bm = bmesh.new(); bm.from_mesh(skirt.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < -.02 and abs(f.calc_center_median().x) < .092], context="FACES")
bm.to_mesh(skirt.data); bm.free()
smooth_boundaries(skirt, iterations=18, factor=0.75)
sel(skirt)
s_ = skirt.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .007; s_.offset = 1; bpy.ops.object.modifier_apply(modifier="sol")
sb_ = skirt.modifiers.new("sub", "SUBSURF"); sb_.levels = 1; bpy.ops.object.modifier_apply(modifier="sub")
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.uv.cube_project(cube_size=.30); bpy.ops.object.mode_set(mode="OBJECT")

# Fingerless leather gloves + forearm bracers
finger_tips = verts_in(human, ["index_02_l", "middle_02_l", "ring_02_l", "pinky_02_l", "thumb_02_l",
                               "index_02_r", "middle_02_r", "ring_02_r", "pinky_02_r", "thumb_02_r",
                               "index_03_l", "middle_03_l", "ring_03_l", "pinky_03_l", "thumb_03_l",
                               "index_03_r", "middle_03_r", "ring_03_r", "pinky_03_r", "thumb_03_r"], .4)
extract("Gloves", (hand & body_ids) - finger_tips, .004, M["leather"], solid=.0025, uvscale=4, sub=0)
wrist_ids = {i for i in lower_arm & body_ids if arm_t(i) > .66}
extract("Vambraces", wrist_ids, .016, M["armor"] if WHO == "raven" else M["leather"], solid=.006, uvscale=4)

legs = verts_in(human, ["thigh_l", "thigh_r", "calf_l", "calf_r", "pelvis"], .3) & body_ids
extract("CargoPants", {i for i in legs if H * .14 < V[i].co.z < waist + .02 and i not in hand and i not in lower_arm}, .011, M["pants"], solid=.005, uvscale=5)
# Extract ONLY shin-to-ankle boot uppers (stop above ball/toes so sculpted combat boot toe box + sole NEVER show bare toes!)
ankle_z = max(rig.data.bones["foot_l"].head_local.z, rig.data.bones["foot_r"].head_local.z)
boot_ids = {i for i in body_ids if ankle_z - .015 < wz(i) < H * .21 and i in verts_in(human, ["calf_l", "calf_r", "foot_l", "foot_r"], .2)}
boots = extract("CombatBoots", boot_ids, .012, M["leather"], solid=.006, uvscale=4, smooth_iter=6, sub=0)
# Rugged sculpted combat boot vamp, rounded toe box, thick rubber lug soles & ankle cuff straps
for side_s, bbone in (("l", "foot_l"), ("r", "foot_r")):
    fc = rig.data.bones[f"foot_{side_s}"].head_local
    bc = rig.data.bones[f"ball_{side_s}"].head_local
    toe_z = min(V[i].co.z for i in body_ids if wz(i) < H * .12 and (V[i].co.x > 0 if side_s == "l" else V[i].co.x < 0))
    mid_x = (fc.x + bc.x) * .5
    mid_y = (fc.y + bc.y) * .5 - .016
    box(M["leather"], bbone, (.088, .240, .024), (mid_x, mid_y, toe_z + .011), bevel=.011, sub=1)  # rounded boot lug sole
    box(M["leather"], bbone, (.086, .148, .054), (fc.x, fc.y - .028, toe_z + .044), rot=(math.radians(8), 0, 0), bevel=.018, sub=1)  # enclosed boot vamp
    sphere(M["leather"], bbone, .048, (bc.x, bc.y - .030, toe_z + .040), scale=(.96, 1.42, .74))  # reinforced boot toe cap
    cyl(M["leather"], f"calf_{side_s}", .062, .115, (fc.x, fc.y + .018, toe_z + .165), rot=(math.radians(6), 0, 0), bevel=.006)
    torus(M["armor"] if WHO == "raven" else M["brass"], f"calf_{side_s}", .060, .0085, (fc.x, fc.y + .014, toe_z + .195), rot=(math.radians(6), 0, 0))
# Remove hidden foot/toe/lower-leg skin faces so zero toes ever clip through boots!
body_ids = {i for i in body_ids if wz(i) > H * .16 or i in hand}
extract("HairUnderHelmet", verts_in(human, ["scalp"], .2) & body_ids, .009, M["hair"], solid=.005, uvscale=3, sub=0)

# ---------------- Character-Specific Hair, Helmets, Crests & Emblems ----------------
hc = rig.data.bones["head"].head_local
sh_span = shoulder - waist
z_upper = waist + sh_span * .74
z_mid = waist + sh_span * .50
z_low = waist + sh_span * .26

# Seamless spun Stahlhelm with front planar UV mapping for emblem / claw slash marks!
bm = bmesh.new()
prof = [
    (0.000,  0.145),
    (0.038,  0.142),
    (0.074,  0.130),
    (0.102,  0.108),
    (0.118,  0.078),
    (0.123,  0.046),
    (0.125,  0.024),
    (0.136,  0.008),
    (0.150, -0.010),
    (0.154, -0.016),
    (0.147, -0.019),
]
SEGS = 48
rings = []
pitch_h = math.radians(-11 if WHO == "vex" else 2)  # Vex wears helmet tilted slightly back!
rot_h = Matrix.Rotation(pitch_h, 3, 'X')
pivot_h = Vector((0, hc.y + .014, emid.z + .018))
for r_p, z_p in prof:
    ring = []
    for k in range(SEGS):
        a = k * 2 * math.pi / SEGS
        x = math.sin(a) * r_p * 1.01
        y = -math.cos(a) * r_p * 1.15
        front_lift = 0.014 * max(0., math.cos(a)) * (1.0 if z_p < 0.04 else 0.3)
        back_drop = -0.012 * max(0., -math.cos(a)) * (1.0 if z_p < 0.04 else 0.2)
        lp = Vector((x, y, z_p + front_lift + back_drop))
        v = bm.verts.new(pivot_h + rot_h @ lp)
        ring.append(v)
    rings.append(ring)
for r0, r1 in zip(rings[:-1], rings[1:]):
    for k in range(SEGS):
        bm.faces.new((r0[k], r0[(k + 1) % SEGS], r1[(k + 1) % SEGS], r1[k]))
hmesh = bpy.data.meshes.new("BossHelmet")
bm.to_mesh(hmesh); bm.free()
helm = bpy.data.objects.new("BossHelmet", hmesh); scene.collection.objects.link(helm)
sel(helm)
s_ = helm.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .006; s_.offset = -1; bpy.ops.object.modifier_apply(modifier="sol")
sb = helm.modifiers.new("sub", "SUBSURF"); sb.levels = 1; bpy.ops.object.modifier_apply(modifier="sub")
# Front planar UV projection on the helmet so the 3 claw slashes (Raven) / coyote emblem (Vex) sit cleanly on forehead!
if not helm.data.uv_layers:
    helm.data.uv_layers.new(name="UVMap")
uvl = helm.data.uv_layers.active.data
for p_poly in helm.data.polygons:
    # Project only front-facing dome triangles well inside the decal window; everything else uses solid base margin (0.04, 0.04)
    c = p_poly.center - pivot_h
    in_decal = (c.y < -0.055 and c.z > 0.028 and abs(c.x) < 0.095)
    for li in p_poly.loop_indices:
        if not in_decal:
            uvl[li].uv = (0.04, 0.04)
        else:
            v = helm.data.vertices[helm.data.loops[li].vertex_index]
            p = v.co - pivot_h
            if WHO == "raven":
                u_x = 0.5 + (p.x + 0.036) / 0.145
            else:
                u_x = 0.5 + (p.x + 0.042) / 0.135  # viewer's left / character right temple, clear of center NVG block
            u_y = 0.5 + (p.z - 0.078) / 0.135
            uvl[li].uv = (max(.06, min(.94, u_x)), max(.06, min(.94, u_y)))
helm.data.materials.append(M["emblem"])
for p_poly in helm.data.polygons: p_poly.use_smooth = True
bind(helm, "head")

# Helmet brow strap & chinstrap
torus(M["leather"], "head", .128, .007, (0, hc.y + .012, emid.z + .032), rot=(pitch_h, 0, 0), scale=(1.01, 1.15, .95))
for s_ in (-1, 1):
    strap(M["leather"], "head", [(s_ * .108, hc.y - .015, emid.z + .018), (s_ * .092, hc.y - .022, emid.z - .028), (s_ * .058, lips.y + .012, lips.z - .025)], w=.013, t=.003)

if WHO == "raven":
    # Dark shaggy hair strands under back & side brim (sleek tapered strands, no puffballs)
    for k in range(12):
        ang = math.radians(108 + k * 13.5)
        hx = math.sin(ang) * .114; hy = hc.y + .018 + math.cos(ang) * .116
        cyl(M["hair"], "head", .015, .072, (hx, hy, emid.z - .008), r2=.004, rot=(math.radians(14), 0, -ang))
    # Riveted front brow armor plate flush on helmet dome (matches portrait-raven-v8!)
    box(M["leather"], "head", (.095, .020, .024), (0, hc.y - .102, emid.z + .064), rot=(math.radians(-18), 0, 0), bevel=.003)
    box(M["brass"], "head", (.016, .024, .016), (0, hc.y - .106, emid.z + .064), bevel=.002)
    # Prominent notched Mohawk crest fins along helmet crown (6 bold blades matching portrait!)
    for fz, fh, rot_x in ((.118, .054, -26), (.078, .064, -14), (.034, .068, -2), (-.012, .064, 12), (-.056, .056, 24), (-.096, .044, 36)):
        box(M["armor"], "head", (.018, .038, fh), (0, hc.y + .012 - fz * .78, emid.z + .166 - abs(fz) * .28), rot=(math.radians(rot_x), 0, 0), bevel=.003)
    # Continuous arched base spine strap under crest
    box(M["leather"], "head", (.028, .225, .018), (0, hc.y + .014, emid.z + .156), bevel=.004)
    # Circular perforated bronze ear cups on helmet sides
    for s_ in (-1, 1):
        cyl(M["canister"], "head", .036, .022, (s_ * .132, hc.y + .005, emid.z + .018), rot=(0, math.radians(85 * s_), 0), bevel=.004, uv_cube=0)
        torus(M["brass"], "head", .035, .0045, (s_ * .138, hc.y + .005, emid.z + .018), rot=(0, math.radians(85 * s_), 0))
else:
    # Vex: Thick wild curly tousled hair tufts jutting organically around temples, fringe, ears, and nape!
    for s_ in (-1, 1):
        for dx, dy, dz, rad in (
            (.065, -.105, .026, .022), (.098, -.080, .022, .028), (.124, -.042, .014, .032),
            (.134, -.002, .006, .031), (.126, .042, -.004, .030), (.098, .082, -.010, .030),
            (.110, -.018, -.026, .026), (.052, .112, -.014, .028),
        ):
            sphere(M["hair"], "head", rad, (s_ * dx, hc.y + dy, emid.z + dz), scale=(1.15, .92, 1.32))
    # Front rectangular NVG shroud mount seated flush on helmet forehead right of coyote decal
    box(M["armor"], "head", (.046, .020, .046), (.022, hc.y - .104, emid.z + .086), rot=(math.radians(-25), math.radians(-10), 0), bevel=.004)
    box(M["brass"], "head", (.028, .022, .030), (.022, hc.y - .108, emid.z + .086), rot=(math.radians(-25), math.radians(-10), 0), bevel=.003)
    # Right helmet side tactical radio box + antenna + 2 bone fang/tusk charms + dangling blue cloth ribbons
    rx = -.134
    box(M["armor"], "head", (.028, .055, .072), (rx, hc.y + .012, emid.z + .035), rot=(0, math.radians(-10), 0), bevel=.004)
    cyl(M["brass"], "head", .005, .085, (rx - .004, hc.y + .022, emid.z + .108), bevel=.001)  # helmet whip antenna
    # Two curved pale coyote fang/tusk charms dangling beside right temple
    for tx_f, ty_f, rz_f in ((-.128, -.026, 22), (-.118, -.014, 12)):
        cyl(M["bone"], "head", .008, .048, (tx_f, hc.y + ty_f, emid.z - .015), r2=.001, rot=(math.radians(15), math.radians(rz_f), math.radians(-18)), bevel=.001)
    # Dangling blue cloth helmet ribbons trailing behind right ear
    strap(M["coat"], "head", [(-.132, hc.y + .04, emid.z + .015), (-.155, hc.y + .11, emid.z - .025), (-.172, hc.y + .17, emid.z - .065)], w=.024, t=.004)
    strap(M["scarf"], "head", [(-.128, hc.y + .045, emid.z + .022), (-.148, hc.y + .12, emid.z + .005), (-.165, hc.y + .18, emid.z - .020)], w=.018, t=.003)

# ---------------- Emerald Goggles + Heavy Dual-Canister Respirator + Corrugated Hose ----------------
torus(M["leather"], "head", .112, .009, (0, hc.y + .012, emid.z + .004), scale=(.99, 1.10, .82))
rim_mat = M["brass"] if WHO == "raven" else M["armor"]
for s_ in (-1, 1):
    gx, gy, gz = s_ * .038, emid.y - .018, emid.z + .003
    cyl(M["leather"], "head", .034, .026, (gx, gy + .004, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)), bevel=.004)
    cyl(rim_mat, "head", .031, .024, (gx, gy - .002, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)), bevel=.003)
    torus(rim_mat, "head", .029, .0045, (gx, gy - .013, gz), rot=(math.radians(90), 0, math.radians(-10 * s_)))
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.026, depth=.008, location=(gx, gy - .011, gz), rotation=(math.radians(90), 0, math.radians(-10 * s_)))
    lo = bpy.context.object; sel(lo); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for l in lo.data.loops:
        co = lo.data.vertices[l.vertex_index].co
        lo.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .052, .5 + co.z / .052)
    finish(lo, M["lens"], "head", uv_cube=0)
box(rim_mat, "head", (.024, .014, .010), (0, emid.y - .026, emid.z + .005), bevel=.003)

my, mz = lips.y - .020, (emid.z + lips.z) / 2 - .010
mask_mat = M["brass"] if WHO == "raven" else M["armor"]
bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=14, radius=.058, location=(0, my + .006, mz))
fplate = bpy.context.object; fplate.scale = (.92, .95, 1.05)
sel(fplate); bm = bmesh.new(); bm.from_mesh(fplate.data)
for v in bm.verts:
    if v.co.z > 0: v.co.x *= max(.32, 1.0 - v.co.z * 9.5); v.co.y -= v.co.z * .25
    if v.co.y > .015: v.co.y = .015
bm.to_mesh(fplate.data); bm.free()
finish(fplate, mask_mat, "head", sub=1)
torus(M["leather"], "head", .052, .007, (0, my + .012, mz + .002), rot=(math.radians(78), 0, 0), scale=(.92, 1.08, .9))

cyl(mask_mat, "head", .028, .022, (0, my - .042, mz - .012), rot=(math.radians(96), 0, 0), bevel=.004)
bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.024, depth=.006, location=(0, my - .051, mz - .013), rotation=(math.radians(96), 0, 0))
vo = bpy.context.object; sel(vo); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
for l in vo.data.loops:
    co = vo.data.vertices[l.vertex_index].co
    vo.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .048, .5 + co.z / .048)
finish(vo, M["vent"], "head", uv_cube=0)

for s_ in (-1, 1):
    cx, cy, cz = s_ * .060, my - .006, mz - .014
    rot_c = (math.radians(76), 0, math.radians(-46 * s_))
    cyl(mask_mat, "head", .034, .036, (cx, cy, cz), rot=rot_c, bevel=.004)
    torus(mask_mat, "head", .034, .004, (cx + s_ * .011, cy - .011, cz - .003), rot=rot_c)
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.032, depth=.006, location=(cx + s_ * .013, cy - .013, cz - .004), rotation=rot_c)
    co_obj = bpy.context.object; sel(co_obj); bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for l in co_obj.data.loops:
        co = co_obj.data.vertices[l.vertex_index].co
        co_obj.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .064, .5 + co.y / .064)
    finish(co_obj, M["canister"], "head", uv_cube=0)

# Corrugated breather hose curving from mask down into neck scarf
for k in range(10):
    t = k / 9.
    torus(M["leather"], "head", .014, .0042, (math.sin(t * 1.2) * .008, my - .036 + (t ** 1.3) * .042, mz - .032 - t * .072), rot=(math.radians(28 + t * 25), 0, 0))

# ---------------- Draped Tattered Scarf / Cowl & Iconic Boss Gear ----------------
for k, (rz, rr, mr, tilt) in enumerate(((.005, .122, .032, 14), (.028, .112, .028, 6), (.054, .100, .024, 10), (.074, .090, .020, 6))):
    o = torus(M["scarf"], "neck_01", rr, mr, (0, .006, shoulder + rz), rot=(math.radians(tilt), math.radians(3 * (k - 1)), 0), scale=(1.12, 1.18, .98))
    sel(o); dm = o.modifiers.new("f", "DISPLACE"); tx = bpy.data.textures.new(f"sc_{k}", "CLOUDS"); tx.noise_scale = .045
    dm.texture = tx; dm.strength = .010; bpy.ops.object.modifier_apply(modifier="f")

# Waist belt + buckle
belt_ring = [i for i in body_ids if abs(V[i].co.z - waist) < .012 and abs(V[i].co.x) < .22 and i not in hand and i not in lower_arm]
bw = max(V[i].co.x for i in belt_ring); bf = min(V[i].co.y for i in belt_ring); bb = max(V[i].co.y for i in belt_ring)
torus(M["leather"], "pelvis", 1, .025, (0, (bf + bb) / 2, waist), scale=(bw + .036, (bb - bf) / 2 + .036, .95))
box(M["brass"], "pelvis", (.068, .016, .054), (0, bf - .038, waist), bevel=.005)
box(M["leather"], "pelvis", (.044, .018, .034), (0, bf - .040, waist), bevel=.003)

# Cross-chest leather harness straps
for sx_d in (-1, 1):
    pts = [(sx_d * .110, .05, shoulder - .01), (sx_d * .110, -.02, shoulder + .025),
           (sx_d * .094, surf_y(z_upper, sx_d * .094) - .038, z_upper),
           (sx_d * .086, surf_y(waist + .03, sx_d * .086) - .038, waist + .03)]
    strap(M["leather"], "spine_02", pts, w=.042, t=.006)
    box(M["brass"], "spine_02", (.046, .012, .032), (sx_d * .094, surf_y(z_upper, sx_d * .094) - .042, z_upper), bevel=.004)

# Actual MPFB upper-arm deltoid anchors (sits cleanly outside extracted BossCoat sleeve!)
ua_r = rig.data.bones["upperarm_r"].head_local
ua_l = rig.data.bones["upperarm_l"].head_local

if WHO == "raven":
    # Diagonal chest bandolier (right shoulder to left hip) + brass O-ring + 4 brass cartridges
    b_pts = [(ua_r.x + .035, -.01, shoulder + .025)] + [(x, surf_y(z, x) - .042, z) for x, z in ((-.06, z_upper), (0, z_mid), (.06, z_low), (.12, waist + .01))]
    strap(M["leather"], "spine_02", b_pts, w=.044, t=.0065)
    torus(M["brass"], "spine_02", .024, .0055, (-.082, surf_y(z_upper, -.082) - .046, z_upper), rot=(math.radians(80), 0, 0))
    for t_s in (.32, .42, .52, .62):
        sx_s = -.10 + t_s * .20; sz_s = (shoulder - .02) - t_s * (sh_span - .04)
        sy_s = surf_y(sz_s, sx_s) - .048
        cyl(M["brass"], "spine_02", .0095, .044, (sx_s, sy_s, sz_s), rot=(0, math.radians(40), 0), verts=14, bevel=.002)
        cyl(M["leather"], "spine_02", .0108, .015, (sx_s, sy_s + .001, sz_s), rot=(0, math.radians(40), 0), verts=14)

    # Raven's iconic layered crimson & leather shoulder pauldrons sitting proudly outside BossCoat!
    # Right shoulder (-X): tiered dark brown leather underplates + big oxblood dome with 3 crisp white claw slashes + 3 hanging silver metal claws!
    rx_p = ua_r.x - .054; ry_p = ua_r.y - .005; rz_p = ua_r.z + .038
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=18, radius=.118, location=(rx_p, ry_p, rz_p))
    pd = bpy.context.object; pd.scale = (.94, 1.08, .80); pd.rotation_euler = (0, math.radians(-26), 0)
    sel(pd); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bm = bmesh.new(); bm.from_mesh(pd.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().z < -.032 or f.calc_center_median().x > .055], context="FACES")
    bm.to_mesh(pd.data); bm.free()
    if not pd.data.uv_layers: pd.data.uv_layers.new(name="UVMap")
    for p_poly in pd.data.polygons:
        c = p_poly.center
        on_face = (c.z > -.018 and abs(c.y) < .082 and c.x < .020)
        for li in p_poly.loop_indices:
            if not on_face:
                pd.data.uv_layers.active.data[li].uv = (0.04, 0.04)
            else:
                co = pd.data.vertices[pd.data.loops[li].vertex_index].co
                u_x = .5 + (-co.y - .010) / .135
                u_y = .5 + (co.z - .020) / .125
                pd.data.uv_layers.active.data[li].uv = (max(.06, min(.94, u_x)), max(.06, min(.94, u_y)))
    finish(pd, M["emblem"], "upperarm_r", bevel=0, sub=1, uv_cube=0)
    # Two layered oxblood/leather sub-plates cascading down outer right upper arm
    box(M["leather"], "upperarm_r", (.112, .154, .016), (rx_p - .022, ry_p, rz_p - .052), rot=(0, math.radians(-48), 0), bevel=.005, sub=1)
    box(M["armor"], "upperarm_r", (.104, .140, .015), (rx_p - .038, ry_p, rz_p - .088), rot=(0, math.radians(-58), 0), bevel=.005, sub=1)
    # THREE large iconic curved silver metal claws dangling clearly outside right pauldron rim!
    for cy_off, cz_off, rot_z in ((-.064, -.004, 18), (-.008, -.010, 4), (.046, -.004, -10)):
        ring_p = (rx_p - .088, ry_p + cy_off, rz_p - .046 + cz_off)
        torus(M["brass"], "upperarm_r", .016, .0040, ring_p, rot=(0, math.radians(76), math.radians(rot_z)))
        claw_p = (ring_p[0] - .012, ring_p[1], ring_p[2] - .052)
        cyl(M["claw"], "upperarm_r", .020, .104, claw_p, r2=.0015, rot=(math.radians(174), math.radians(-14), math.radians(rot_z)), bevel=.001)

    # Left shoulder (+X): heavy multi-tiered dark brown leather armor plates sitting cleanly on top of coat sleeve
    lx_p = ua_l.x + .054; ly_p = ua_l.y; lz_p = ua_l.z + .034
    for lk, (dx_l, dz_l, ang_l) in enumerate(((0.0, 0.018, 28), (.026, -.020, 44), (.042, -.058, 56))):
        box(M["leather"], "upperarm_l", (.122, .158, .018), (lx_p + dx_l, ly_p, lz_p + dz_l), rot=(0, math.radians(ang_l), 0), bevel=.005, sub=1)

    # Flowing tattered crimson back cape hanging from shoulders down mid-back/thighs
    by_b = max(V[i].co.y for i in body_ids if abs(V[i].co.z - shoulder) < .03) + .046
    cape_verts = [
        (-.23, by_b - .02, shoulder + .035), (0, by_b + .025, shoulder + .055), (.22, by_b - .02, shoulder + .035),
        (-.26, by_b + .05, z_mid), (0, by_b + .07, z_mid), (.24, by_b + .05, z_mid),
        (-.30, by_b + .09, H * .30), (-.11, by_b + .10, H * .25), (0, by_b + .11, H * .31), (.13, by_b + .10, H * .26), (.28, by_b + .09, H * .30),
    ]
    bm_c = bmesh.new()
    vs_c = [bm_c.verts.new(v) for v in cape_verts]
    bm_c.faces.new((vs_c[0], vs_c[1], vs_c[4], vs_c[3]))
    bm_c.faces.new((vs_c[1], vs_c[2], vs_c[5], vs_c[4]))
    bm_c.faces.new((vs_c[3], vs_c[4], vs_c[8], vs_c[7], vs_c[6]))
    bm_c.faces.new((vs_c[4], vs_c[5], vs_c[10], vs_c[9], vs_c[8]))
    cm = bpy.data.meshes.new("CrimsonCape"); bm_c.to_mesh(cm); bm_c.free()
    cape = bpy.data.objects.new("CrimsonCape", cm); scene.collection.objects.link(cape)
    sel(cape)
    s_cp = cape.modifiers.new("sol", "SOLIDIFY"); s_cp.thickness = .008; bpy.ops.object.modifier_apply(modifier="sol")
    finish(cape, M["scarf"], "spine_02", sub=1, uv_cube=.35)

else:
    # Vex: Layered armored metal/leather guard on right shoulder (clearly outside coat!) + torn blue cloth arm band
    rx_v = ua_r.x - .054; ry_v = ua_r.y - .005; rz_v = ua_r.z + .038
    box(M["armor"], "upperarm_r", (.124, .158, .018), (rx_v, ry_v, rz_v), rot=(0, math.radians(-30), 0), bevel=.005, sub=1)
    box(M["leather"], "upperarm_r", (.114, .144, .016), (rx_v - .022, ry_v, rz_v - .036), rot=(0, math.radians(-42), 0), bevel=.005, sub=1)
    box(M["armor"], "upperarm_r", (.102, .130, .014), (rx_v - .038, ry_v, rz_v - .068), rot=(0, math.radians(-52), 0), bevel=.004, sub=1)
    torus(M["coat"], "upperarm_r", .066, .015, (ua_r.x - .055, ua_r.y, ua_r.z - .095), rot=(0, math.radians(-26), 0))

    # Frayed wide tan cowl drape (sculpted V-shaped front cowl wrap rather than a boxy slab!) + Coyote Medallion necklace!
    cy_ch = surf_y(z_upper + .02, 0) - .046
    for cx_w, cz_w, rz_w, ang_w in ((-.065, z_upper + .055, .078, -22), (.0, z_upper + .040, .092, 0), (.065, z_upper + .055, .078, 22)):
        cyl(M["scarf"], "spine_03", .052, .095, (cx_w, cy_ch + .018, cz_w), r2=.022, rot=(math.radians(16), math.radians(ang_w), 0), bevel=.006)
    # Cord necklace + sharp round Coyote Medallion (UV in local space around center (0,0,0)!)
    strap(M["leather"], "spine_02", [(-.065, surf_y(shoulder - .01, -.065) - .030, shoulder - .01), (0, surf_y(z_upper - .02, 0) - .052, z_upper - .035), (.065, surf_y(shoulder - .01, .065) - .030, shoulder - .01)], w=.007, t=.004)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=.028, depth=.009, location=(.006, surf_y(z_upper - .03, .006) - .058, z_upper - .038), rotation=(math.radians(82), 0, 0))
    med = bpy.context.object; sel(med); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for l in med.data.loops:
        co = med.data.vertices[l.vertex_index].co
        med.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .056, .5 + co.z / .056)
    finish(med, M["medallion"], "spine_02", uv_cube=0)

    # Clip-on chest walkie-talkie radio with antenna on right chest strap + Twin Brass Binoculars on left chest!
    rx_c = -.092; rz_c = z_upper + .005; ry_c = surf_y(rz_c, rx_c) - .052
    box(M["armor"], "spine_02", (.038, .026, .062), (rx_c, ry_c, rz_c), bevel=.004)
    cyl(M["brass"], "spine_02", .004, .068, (rx_c - .010, ry_c, rz_c + .055), bevel=.001)
    # Twin Brass Binoculars on left chest (+X)
    for bx_o in (.076, .106):
        cyl(M["brass"], "spine_02", .015, .078, (bx_o, surf_y(z_upper - .03, bx_o) - .048, z_upper - .035), rot=(math.radians(14), math.radians(-10), 0), bevel=.003)
    # Waist utility pouches
    for mx_p in (-.048, .052):
        my_p = surf_y(waist + .01, mx_p) - .046
        box(M["leather"], "pelvis", (.065, .042, .082), (mx_p, my_p, waist), bevel=.007)

# ---------------- Join, decimate ONLY high-poly MPFB skin/clothes (keep sculpted gear crisp!), keep LIVE ARMATURE, export FBX ----------------
sel(human); bm = bmesh.new(); bm.from_mesh(human.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in body_ids], context="VERTS")
bm.to_mesh(human.data); bm.free()
sel(human)
for o in hi_parts: o.select_set(True)
bpy.ops.object.join()
hi_mesh = bpy.context.object
for m in list(hi_mesh.modifiers): hi_mesh.modifiers.remove(m)
sel(hi_mesh)
dec = hi_mesh.modifiers.new("dec", "DECIMATE"); dec.ratio = 0.25
bpy.ops.object.modifier_apply(modifier="dec")

sel(hi_mesh)
for o in parts: o.select_set(True)
bpy.ops.object.join()
boss = bpy.context.object; boss.name = CAP
for m in list(boss.modifiers): boss.modifiers.remove(m)
am = boss.modifiers.new("Armature", "ARMATURE"); am.object = rig; boss.parent = rig
rig.name = f"{CAP}Rig"
tris = sum(len(p.vertices) - 2 for p in boss.data.polygons)
print(f"{WHO.upper()}_TRIS", tris)

bpy.ops.object.select_all(action="DESELECT"); boss.select_set(True); rig.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=OUT, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", bake_space_transform=False,
    add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE",
    use_armature_deform_only=True
)
print(f"{WHO.upper()}_EXPORTED", OUT)

# ---------------- Blender EEVEE Renders for Visual Verification ----------------
scene.render.engine = "BLENDER_EEVEE"; scene.render.resolution_x = 900; scene.render.resolution_y = 1200
w = bpy.data.worlds.new("w"); scene.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (.06, .065, .07, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.25
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), 0, math.radians(35))); bpy.context.object.data.energy = 3.6
bpy.ops.object.light_add(type="AREA", location=(-1.5, -1.8, 2.2)); bpy.context.object.data.energy = 220; bpy.context.object.data.color = (.85, .9, 1)
bpy.ops.object.camera_add(); cam = bpy.context.object; scene.camera = cam; cam.data.type = "ORTHO"
for nm, loc, look, os_ in (
    ("front", (0, -4, H * .56), (0, 0, H * .56), H * 1.32),
    ("three_quarter", (2.6, -3, H * .88), (0, 0, H * .56), H * 1.32),
    ("face", (.35, -3, emid.z + .03), (0, 0, emid.z + .02), .48),
):
    cam.location = loc; cam.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat("-Z", "Y").to_euler(); cam.data.ortho_scale = os_
    scene.render.filepath = f"{PREV}/{WHO}_hq_{nm}.png"; bpy.ops.render.render(write_still=True)
print(f"{WHO.upper()}_PREVIEWS_DONE")
