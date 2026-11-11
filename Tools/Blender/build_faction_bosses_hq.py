# Builds PS3-grade HQ combat-rigged Faction Boss models for Raven (Red Claw) and Vex (Dusk Coyotes)
# using MPFB (MakeHuman CC0 base mesh) + game_engine armature (kept LIVE for combat animation)
# + NumPy 1024x1024 multi-octave weathered PBR albedo/normal textures + portrait projection + bespoke sculpted tactical armor.
# Run:
#   BLENDER_USER_RESOURCES=D:/Unity/AssetIntegration/IonaHQ/blender-user blender -b --factory-startup -P Tools/Blender/build_faction_bosses_hq.py -- <raven|vex>
import bpy, bmesh, math, sys, os, random, shutil, importlib, addon_utils
import numpy as np
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
RNG = np.random.default_rng(101 if WHO == "raven" else 202)

PORTRAIT = (
    "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-raven-v8.png"
    if WHO == "raven" else
    "D:/Unity/Ashfall-iOS/Assets/Resources/Art/Portraits/portrait-vex-v8.png"
)

for shared_fn in ("iona_hair.png",):
    src = os.path.join(BRIGGS_TEX, shared_fn)
    if os.path.exists(src):
        shutil.copy2(src, os.path.join(TEX, shared_fn))

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
    d.update(gender=1.0, age=0.46, muscle=0.84, weight=0.56, height=0.74, proportions=0.64)
else:
    d.update(gender=1.0, age=0.42, muscle=0.74, weight=0.52, height=0.70, proportions=0.60)
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

body_ids = verts_in(human, ["body"], .5)
er, el, lips = centroid(human, "helper-r-eye"), centroid(human, "helper-l-eye"), centroid(human, "lips")
emid = (er + el) / 2

# ---------------- Portrait skin projection bake (2048x2048 with stubble & grit) ----------------
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

skin_srgb = sample(610, 310) if WHO == "raven" else sample(640, 235)
skin_rgb = [c ** 2.2 for c in skin_srgb]
if WHO == "raven":
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((620, 290)), Vector((755, 312)), Vector((668, 435))
else:
    EYE_R_PX, EYE_L_PX, MOUTH_PX = Vector((648, 222)), Vector((785, 250)), Vector((692, 375))
sx = (EYE_L_PX.x - EYE_R_PX.x) / max(.01, el.x - er.x)
sz = (MOUTH_PX.y - (EYE_R_PX.y + EYE_L_PX.y) / 2) / max(.01, emid.z - lips.z)
pmid = (EYE_R_PX + EYE_L_PX) / 2

me = human.data
proj = me.uv_layers.new(name="proj"); me.uv_layers.active = me.uv_layers["UVMap"]
mask = me.color_attributes.new("facemask", "FLOAT_COLOR", "POINT")
head_ids = verts_in(human, ["head", "neck_01"], .2) & body_ids
FC = pmid + Vector((0, 42)); FR = Vector((122, 152))
for v in me.vertices:
    p = Vector((pmid.x + (v.co.x - emid.x) * sx, pmid.y - (v.co.z - emid.z) * sz))
    e = ((p.x - FC.x) / FR.x) ** 2 + ((p.y - FC.y) / FR.y) ** 2
    w = max(0., min(1., (1.2 - e) / .45)) if v.index in head_ids and v.normal.y < .08 else 0.
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
noise = N.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 95
noise.inputs["Detail"].default_value = 8
ramp = N.new("ShaderNodeValToRGB"); L.new(noise.outputs[0], ramp.inputs[0])
ramp.color_ramp.elements[0].color = (*[c * .80 for c in skin_rgb], 1); ramp.color_ramp.elements[1].color = (*[min(1, c * 1.07) for c in skin_rgb], 1)
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

# ---------------- PS3-Grade 1024x1024 NumPy PBR Texture + Normal Generator ----------------
def fbm_2d(H_, W_, octaves=(8, 16, 32, 64, 128), weights=(.42, .26, .16, .10, .06)):
    """Tileable multi-octave bilinear value noise using pure NumPy."""
    acc = np.zeros((H_, W_), dtype=np.float32)
    yy, xx = np.mgrid[0:H_, 0:W_]
    for res, w in zip(octaves, weights):
        grid = RNG.random((res, res), dtype=np.float32)
        fy = (yy / H_) * res; fx = (xx / W_) * res
        y0 = np.floor(fy).astype(np.int32) % res; y1 = (y0 + 1) % res
        x0 = np.floor(fx).astype(np.int32) % res; x1 = (x0 + 1) % res
        ty = fy - np.floor(fy); tx = fx - np.floor(fx)
        ty = ty * ty * (3 - 2 * ty); tx = tx * tx * (3 - 2 * tx)
        v = (grid[y0, x0] * (1 - tx) * (1 - ty) + grid[y0, x1] * tx * (1 - ty) +
             grid[y1, x0] * (1 - tx) * ty + grid[y1, x1] * tx * ty)
        acc += v * w
    acc = (acc - acc.min()) / max(1e-5, acc.max() - acc.min())
    return acc

def save_np_img(name, arr, colorspace="sRGB"):
    h_, w_ = arr.shape[:2]
    rgba = np.ones((h_, w_, 4), dtype=np.float32)
    rgba[..., :3] = np.clip(arr, 0.0, 1.0)
    im = bpy.data.images.new(name, w_, h_)
    im.colorspace_settings.name = colorspace
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = TEX + name + ".png"; im.file_format = "PNG"; im.save()
    bpy.data.images.remove(im)

def save_normal_from_height(name, hmap, strength=5.0):
    dy = np.roll(hmap, -1, axis=0) - np.roll(hmap, 1, axis=0)
    dx = np.roll(hmap, -1, axis=1) - np.roll(hmap, 1, axis=1)
    nx = -dx * strength; ny = -dy * strength; nz = np.ones_like(hmap)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / l * .5 + .5, ny / l * .5 + .5, nz / l * .5 + .5], axis=-1)
    save_np_img(name + "_n", nrm, "Non-Color")

def make_weathered_fabric(name, base_rgb, grime_rgb, S=1024, stitch=False, twill=True):
    n1 = fbm_2d(S, S, (6, 12, 24, 48, 96))
    n2 = fbm_2d(S, S, (16, 40, 80, 160))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    # Fine diagonal cotton twill weave + organic creases + oil/dirt mottling
    diag = np.sin((xx * .65 + yy * .65) * 1.45) * .5 + .5 if twill else np.sin(xx * .9) * .5 + .5
    cross = np.sin((xx * .65 - yy * .65) * 1.45) * .5 + .5
    weave = 0.88 + 0.12 * (diag * 0.6 + cross * 0.4)
    folds = np.sin(yy * .032 + n1 * 4.5) * .5 + .5
    dirt = np.clip((n1 - 0.35) * 1.7, 0.0, 1.0) * 0.55 + np.clip((n2 - 0.5) * 1.4, 0.0, 1.0) * 0.25
    # Stitched panel seam tracks along edges & centerlines
    seam_mask = np.zeros((S, S), dtype=np.float32)
    stitch_hi = np.zeros((S, S), dtype=np.float32)
    if stitch:
        for sy in (int(S * .12), int(S * .88)):
            seam_mask[max(0, sy - 4):min(S, sy + 4), :] = 1.0
            dash = ((xx[0] / 12.0).astype(np.int32) % 2) == 0
            stitch_hi[sy - 1:sy + 2, dash] = 1.0
    col = np.zeros((S, S, 3), dtype=np.float32)
    b = np.array(base_rgb, dtype=np.float32)
    g = np.array(grime_rgb, dtype=np.float32)
    for c in range(3):
        col[..., c] = (b[c] * (1.0 - dirt) + g[c] * dirt) * weave * (0.86 + 0.22 * folds)
        col[..., c] *= (1.0 - 0.32 * seam_mask)
        col[..., c] = col[..., c] * (1.0 - 0.65 * stitch_hi) + 0.48 * stitch_hi
    save_np_img(name, col)
    hmap = (weave * 0.25 + folds * 0.35 + n2 * 0.30 - seam_mask * 0.45 + stitch_hi * 0.40)
    save_normal_from_height(name, hmap, strength=5.5)

def make_weathered_leather(name, base_rgb, worn_rgb, S=1024):
    n1 = fbm_2d(S, S, (6, 14, 28, 64))
    n2 = fbm_2d(S, S, (32, 64, 128, 256))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    pebble = np.sin(n2 * 18.0 + n1 * 4.0) * .5 + .5
    scratches = np.clip(np.sin(xx * .05 + n1 * 9.0) * np.sin(yy * .09 - n2 * 7.0), 0.72, 1.0)
    scratches = (scratches - 0.72) / 0.28
    wear = np.clip((n1 - 0.42) * 2.0, 0.0, 1.0) * 0.55 + scratches * 0.35
    b = np.array(base_rgb, dtype=np.float32); w = np.array(worn_rgb, dtype=np.float32)
    col = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3):
        col[..., c] = (b[c] * (1.0 - wear) + w[c] * wear) * (0.85 + 0.25 * pebble)
    # Saddle stitching near edges
    dash = ((xx[0] / 14.0).astype(np.int32) % 2) == 0
    edge_st = np.zeros((S, S), dtype=np.float32)
    for sy in (int(S * .10), int(S * .90)):
        edge_st[sy - 1:sy + 2, dash] = 1.0
    col = col * (1.0 - 0.55 * edge_st[..., None]) + np.array([.58, .48, .32]) * edge_st[..., None] * 0.55
    save_np_img(name, col)
    save_normal_from_height(name, pebble * 0.35 + n2 * 0.40 - scratches * 0.35 + edge_st * 0.3, strength=6.0)

def make_weathered_metal(name, base_rgb, rust_rgb, edge_rgb, S=1024):
    n1 = fbm_2d(S, S, (8, 16, 32, 64))
    n2 = fbm_2d(S, S, (24, 64, 128))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    brush = np.sin(xx * 0.55 + n1 * 8.0) * .5 + .5
    pits = np.clip((0.36 - n2) * 3.5, 0.0, 1.0)
    # Edge wear near border & high-noise speckles
    border = np.maximum(np.maximum(1 - xx / (S * .08), (xx - S * .92) / (S * .08)),
                        np.maximum(1 - yy / (S * .08), (yy - S * .92) / (S * .08)))
    chip = np.clip(border * 0.8 + np.clip((n1 - 0.65) * 3.0, 0.0, 1.0) * 0.6, 0.0, 1.0)
    b = np.array(base_rgb, dtype=np.float32); r = np.array(rust_rgb, dtype=np.float32); e = np.array(edge_rgb, dtype=np.float32)
    col = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3):
        col[..., c] = b[c] * (0.86 + 0.22 * brush) * (1.0 - pits * .65) + r[c] * pits * .65
        col[..., c] = col[..., c] * (1.0 - chip * .55) + e[c] * chip * .55
    save_np_img(name, col)
    save_normal_from_height(name, brush * 0.22 + n2 * 0.35 - pits * 0.65, strength=5.0)

def make_canister_and_vent(lens_cx, lens_cy, lens_r):
    # Emerald goggle lens with portrait iris detail + subtle grime + curved glass glint
    S = 512
    lpx = []
    for y in range(S):
        ny = (y - S // 2) / (S / 2.0)
        for x in range(S):
            nx = (x - S // 2) / (S / 2.0)
            r = math.hypot(nx, ny)
            sx_p = int(max(0, min(IW - 1, lens_cx + nx * lens_r)))
            sy_p = int(max(0, min(IH - 1, lens_cy - ny * lens_r)))
            pi = ((IH - 1 - sy_p) * IW + sx_p) * 4
            pc = [px[pi], px[pi + 1], px[pi + 2]]
            rim = max(0., min(1., (r - .76) / .22))
            spec = math.exp(-(((nx + .28) / .24) ** 2 + ((ny - .34) / .14) ** 2)) * .62
            tint = (.10, .34, .16) if WHO == "raven" else (.06, .38, .15)
            c = [max(0., min(1., (pc[k] * .62 + tint[k] * .50) * (1 - rim * .72) + spec * (.78, 1., .84)[k])) for k in range(3)]
            lpx += [*c, 1.0]
    lim = bpy.data.images.new(f"{WHO}_lens", S, S); lim.pixels = lpx
    lim.filepath_raw = TEX + f"{WHO}_lens.png"; lim.file_format = "PNG"; lim.save(); bpy.data.images.remove(lim)

    # Perforated canister cap & front grille (512x512 crisp dark recessed holes)
    holes = [(0, 0)]
    for rr, cnt in ((.28, 6), (.54, 12), (.76, 16)):
        for k in range(cnt):
            a = k * 2 * math.pi / cnt + rr * 1.3
            holes.append((math.cos(a) * rr, math.sin(a) * rr))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    nx = (xx - S / 2) / (S / 2); ny = (yy - S / 2) / (S / 2); r = np.hypot(nx, ny)
    d_hole = np.full((S, S), 99.0, dtype=np.float32)
    for hx, hy in holes:
        d_hole = np.minimum(d_hole, np.hypot(nx - hx, ny - hy))
    in_h = (d_hole < .068) & (r < .84)
    bev = (d_hole >= .068) & (d_hole < .094) & (r < .84)
    out_r = (r > .84) & (r < .95)
    n1 = fbm_2d(S, S, (8, 16, 32))
    col = np.zeros((S, S, 3), dtype=np.float32)
    base_m = np.array((.42, .31, .16) if WHO == "raven" else (.23, .24, .26), dtype=np.float32)
    hi_m = np.array((.62, .48, .25) if WHO == "raven" else (.38, .40, .43), dtype=np.float32)
    for c in range(3):
        col[..., c] = base_m[c] * (0.82 + 0.30 * n1) * (1.0 - 0.22 * np.clip(r, 0, 1))
        col[in_h, c] = (.03, .028, .025)[c]
        col[bev, c] = hi_m[c]
        col[out_r, c] = hi_m[c] * 0.90
    save_np_img(f"{WHO}_canister_cap", col)

    # Slatted respirator front intake grille
    slot = (np.abs(nx) < .58) & (np.abs(ny) < .66) & ((np.floor((ny + .7) * 13).astype(np.int32) % 2) == 0)
    vcol = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3):
        vcol[..., c] = np.where(slot, (.025, .025, .03)[c], base_m[c] * (0.84 + 0.25 * n1))
    save_np_img(f"{WHO}_vent", vcol)

make_weathered_leather("iona_leather", (.20, .13, .085), (.34, .24, .16))
make_weathered_metal("iona_brass", (.50, .38, .20), (.24, .16, .08), (.68, .54, .32))
make_weathered_fabric("iona_pants", (.22, .185, .155), (.13, .105, .085))
make_canister_and_vent(*((618, 286, 48) if WHO == "raven" else (654, 224, 48)))

if WHO == "raven":
    make_weathered_fabric("raven_coat", (.13, .125, .12), (.06, .055, .052))
    make_weathered_fabric("raven_crimson", (.31, .072, .072), (.16, .038, .038))
    make_weathered_metal("raven_armor", (.28, .062, .055), (.15, .055, .045), (.44, .40, .38))
    make_weathered_metal("raven_claw", (.56, .57, .59), (.25, .20, .16), (.82, .83, .85))
    # 1024x1024 distressed painted oxblood armor + 3 chipped white claw slashes + steel rim scratches!
    S = 1024
    n1 = fbm_2d(S, S, (8, 16, 32, 64))
    n2 = fbm_2d(S, S, (24, 64, 128))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32) / S
    dx = xx - 0.50; dy = yy - 0.52
    hit = np.zeros((S, S), dtype=bool)
    for off in (-0.115, 0.0, 0.115):
        u = (dx - off) * 0.84 + dy * 0.54
        v = -(dx - off) * 0.54 + dy * 0.84
        taper = np.maximum(0.0, 1.0 - (v / 0.25) ** 2)
        wiggle = 0.0055 * np.sin(v * 68 + off * 40) + (n2 - 0.5) * 0.009
        hit |= (np.abs(v) < 0.25) & (np.abs(u + wiggle) < 0.024 * taper)
    chip_paint = hit & (n2 > 0.28)
    steel_scratch = (n2 > 0.85) & (xx > 0.10) & (xx < 0.90) & (yy > 0.10) & (yy < 0.90)
    slp = np.zeros((S, S, 3), dtype=np.float32)
    base_ox = np.array([.28, .058, .052], dtype=np.float32)
    grime_ox = np.array([.13, .036, .034], dtype=np.float32)
    for c in range(3):
        slp[..., c] = (base_ox[c] * (0.78 + 0.35 * n1) + grime_ox[c] * (1.0 - n1) * 0.45)
        slp[steel_scratch, c] = (.46, .45, .44)[c]
        slp[chip_paint, c] = (.84, .80, .74)[c] * (0.85 + 0.22 * n2[chip_paint])
    save_np_img("raven_slashes", slp)
    save_normal_from_height("raven_slashes", n1 * 0.4 + n2 * 0.3 + chip_paint.astype(np.float32) * 0.25, strength=5.0)
else:
    make_weathered_fabric("vex_coat", (.13, .175, .245), (.065, .085, .12))
    make_weathered_fabric("vex_scarf", (.44, .39, .32), (.24, .21, .17))
    make_weathered_metal("vex_metal", (.22, .24, .26), (.14, .11, .08), (.50, .52, .54))
    make_weathered_leather("vex_bone", (.78, .72, .59), (.48, .41, .30))
    # 1024x1024 Slate-blue weathered steel helmet + distressed white howling coyote decal & medallion
    S = 1024
    n1 = fbm_2d(S, S, (8, 16, 32, 64))
    n2 = fbm_2d(S, S, (24, 64, 128))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32) / S
    def coyote_np(nx, ny):
        cx, cy = (nx - .50), (ny - .48)
        torso = (cx / .18) ** 2 + (cy / .07) ** 2 < 1.0
        chest = ((cx - .11) / .09) ** 2 + ((cy - .02) / .075) ** 2 < 1.0
        neck = (np.abs(cx - .17) < .05) & (cy > 0.0) & (cy < .13)
        head = ((cx - .22) / .07) ** 2 + ((cy - .13) / .042) ** 2 < 1.0
        snout = (cx > 0.24) & (cx < 0.32) & (cy > 0.115) & (cy < 0.145 - (cx - .24) * .3)
        ear1 = (np.abs(cx - .195) < .014) & (cy > 0.15) & (cy < 0.21)
        ear2 = (np.abs(cx - .218) < .012) & (cy > 0.15) & (cy < 0.205)
        fleg = ((cx + cy * .45) > 0.12) & ((cx + cy * .45) < 0.17) & (cy > -0.16) & (cy < 0.0)
        fleg2 = ((cx - cy * .35) > 0.04) & ((cx - cy * .35) < 0.09) & (cy > -0.15) & (cy < 0.0)
        bleg = ((cx - cy * .45) > -0.17) & ((cx - cy * .45) < -0.11) & (cy > -0.16) & (cy < 0.02)
        tail = (cx > -0.33) & (cx < -0.14) & (np.abs(cy - (0.02 - (cx + .14) * .35)) < .024)
        return torso | chest | neck | head | snout | ear1 | ear2 | fleg | fleg2 | bleg | tail
    emb = coyote_np(xx, yy) & (n2 > 0.22)
    steel_chip = (n2 > 0.82)
    hxp = np.zeros((S, S, 3), dtype=np.float32)
    base_sb = np.array([.15, .20, .29], dtype=np.float32)
    for c in range(3):
        hxp[..., c] = base_sb[c] * (0.76 + 0.38 * n1)
        hxp[steel_chip, c] = (.44, .46, .48)[c]
        hxp[emb, c] = (.85, .83, .78)[c] * (0.85 + 0.20 * n2[emb])
    save_np_img("vex_emblem", hxp)
    save_normal_from_height("vex_emblem", n1 * 0.4 + n2 * 0.3 + emb.astype(np.float32) * 0.22, strength=4.8)

    r_c = np.hypot(xx - .5, yy - .5)
    rim_m = (r_c > 0.41) & (r_c < 0.48)
    coin_emb = coyote_np((xx - .5) * 1.15 + .5, (yy - .5) * 1.15 + .5) & (r_c < 0.40)
    medp = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3):
        medp[..., c] = np.where(rim_m | coin_emb, (.66, .54, .30)[c] * (0.85 + .25 * n2), (.22, .18, .12)[c] * (0.80 + .30 * n1))
    save_np_img("vex_medallion", medp)

def tex_mat(name, path, rough=.82, metal=0.):
    m, N, L = node_mat(f"{CAP}_{name}"); b = N["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if path and os.path.exists(path):
        t = N.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(path)
        L.new(t.outputs[0], b.inputs["Base Color"]); b.inputs["Base Color"].default_value = (1, 1, 1, 1)
    return m

if WHO == "raven":
    M = dict(
        skin=tex_mat("Skin", TEX + "raven_body.png", .68),
        leather=tex_mat("Leather", TEX + "iona_leather.png", .52),
        brass=tex_mat("Brass", TEX + "iona_brass.png", .34, .85),
        pants=tex_mat("Pants", TEX + "iona_pants.png", .90),
        hair=tex_mat("Hair", TEX + "iona_hair.png", .85),
        coat=tex_mat("Coat", TEX + "raven_coat.png", .92),
        scarf=tex_mat("Scarf", TEX + "raven_crimson.png", .92),
        armor=tex_mat("Armor", TEX + "raven_armor.png", .44, .52),
        emblem=tex_mat("Emblem", TEX + "raven_slashes.png", .42, .55),
        claw=tex_mat("Claw", TEX + "raven_claw.png", .28, .88),
        lens=tex_mat("Lens", TEX + "raven_lens.png", .08, .35),
        canister=tex_mat("Canister", TEX + "raven_canister_cap.png", .35, .85),
        vent=tex_mat("Vent", TEX + "raven_vent.png", .40, .70),
    )
else:
    M = dict(
        skin=tex_mat("Skin", TEX + "vex_body.png", .68),
        leather=tex_mat("Leather", TEX + "iona_leather.png", .54),
        brass=tex_mat("Brass", TEX + "iona_brass.png", .34, .85),
        pants=tex_mat("Pants", TEX + "iona_pants.png", .90),
        hair=tex_mat("Hair", TEX + "iona_hair.png", .85),
        coat=tex_mat("Coat", TEX + "vex_coat.png", .90),
        scarf=tex_mat("Scarf", TEX + "vex_scarf.png", .92),
        armor=tex_mat("Armor", TEX + "vex_metal.png", .44, .65),
        emblem=tex_mat("Emblem", TEX + "vex_emblem.png", .45, .48),
        medallion=tex_mat("Medallion", TEX + "vex_medallion.png", .32, .82),
        bone=tex_mat("Bone", TEX + "vex_bone.png", .44, .05),
        lens=tex_mat("Lens", TEX + "vex_lens.png", .08, .35),
        canister=tex_mat("Canister", TEX + "vex_canister_cap.png", .35, .82),
        vent=tex_mat("Vent", TEX + "vex_vent.png", .40, .72),
    )
me.materials.clear(); me.materials.append(M["skin"])

# ---------------- Geometry & Garment Construction Helpers ----------------
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

def torus(mat, bone, R, r, loc, rot=(0, 0, 0), scale=(1, 1, 1), segs=24, msegs=10):
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

def pouch(mat, bone, center, size, rot=(0, 0, 0)):
    # Two-part sculpted leather utility pouch: rounded body + overhanging stitched lid flap + brass snap stud
    box(mat, bone, size, center, rot=rot, bevel=.008, sub=1)
    box(mat, bone, (size[0] * 1.04, size[1] * 1.06, size[2] * .38),
        (center[0], center[1] - .003, center[2] + size[2] * .34), rot=rot, bevel=.006, sub=1)
    sphere(M["brass"], bone, .005, (center[0], center[1] - size[1] * .55, center[2] + size[2] * .20))

# ---------------- Tailored Coat, Tactical Chest Vest, Gloves, Pants & Briggs-Grade Boots ----------------
coat_ids = {i for i in torso_grp & body_ids if i not in neck and i not in hand and wz(i) > hip - .04 and (i not in lower_arm or arm_t(i) < .62)}
extract("BossCoat", coat_ids, .020, M["coat"], solid=.006, uvscale=5)

# Tailored tactical chest rig / leather jerkin (never plastic pecs) + horizontal Briggs-style webbing ribs
vest_mat = M["leather"]
vest_ids = {i for i in torso_grp & body_ids if waist + .02 < wz(i) < shoulder - .03 and abs(V[i].co.x) < .175}
extract("TacticalVest", vest_ids, .028, vest_mat, solid=.006, uvscale=4, sub=0)
for rz_v in (waist + .07, waist + .13, waist + .19, waist + .25):
    vy_f = surf_y(rz_v, 0) - .044
    box(M["coat"], "spine_02", (.26, .022, .022), (0, vy_f, rz_v), bevel=.004)

# Tailored flared coat skirt (open front duster with clean draped edge)
skirt = extract("CoatSkirt", {i for i in verts_in(human, ["helper-skirt"], .5) if H * .29 < V[i].co.z < waist + .04}, .024, M["coat"], solid=0, uvscale=18, smooth_iter=10, sub=0)
sel(skirt); bm = bmesh.new(); bm.from_mesh(skirt.data)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < -.015 and abs(f.calc_center_median().x) < .105], context="FACES")
bm.to_mesh(skirt.data); bm.free()
smooth_boundaries(skirt, iterations=18, factor=0.75)
sel(skirt)
s_ = skirt.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .007; s_.offset = 1; bpy.ops.object.modifier_apply(modifier="sol")
sb_ = skirt.modifiers.new("sub", "SUBSURF"); sb_.levels = 1; bpy.ops.object.modifier_apply(modifier="sub")
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.uv.cube_project(cube_size=.30); bpy.ops.object.mode_set(mode="OBJECT")

cuff_mat = M["scarf"] if WHO == "raven" else M["coat"]
cuff_ids = {i for i in lower_arm & body_ids if .44 < arm_t(i) < .66}
extract("RolledCuffs", cuff_ids, .028, cuff_mat, solid=.007, uvscale=4)

finger_tips = verts_in(human, ["index_02_l", "middle_02_l", "ring_02_l", "pinky_02_l", "thumb_02_l",
                               "index_02_r", "middle_02_r", "ring_02_r", "pinky_02_r", "thumb_02_r",
                               "index_03_l", "middle_03_l", "ring_03_l", "pinky_03_l", "thumb_03_l",
                               "index_03_r", "middle_03_r", "ring_03_r", "pinky_03_r", "thumb_03_r"], .4)
extract("Gloves", (hand & body_ids) - finger_tips, .004, M["leather"], solid=.0025, uvscale=4, sub=0)
wrist_ids = {i for i in lower_arm & body_ids if arm_t(i) > .66}
extract("Bracers", wrist_ids, .016, M["armor"] if WHO == "raven" else M["leather"], solid=.006, uvscale=4)

legs = verts_in(human, ["thigh_l", "thigh_r", "calf_l", "calf_r", "pelvis"], .3) & body_ids
extract("CargoPants", {i for i in legs if H * .14 < V[i].co.z < waist + .02 and i not in hand and i not in lower_arm}, .011, M["pants"], solid=.005, uvscale=5)

# Sleek Briggs-grade laced combat boots (seamless ankle join + contoured toe cap + knee pads + thigh cargo pockets)
fz_floor = min(wz(i) for i in body_ids if wz(i) < H * .10)
boot_ids = {i for i in body_ids if fz_floor + .028 < wz(i) < H * .21 and i in verts_in(human, ["calf_l", "calf_r", "foot_l", "foot_r"], .2)}
extract("CombatBoots", boot_ids, .013, M["leather"], solid=.006, uvscale=4, smooth_iter=6, sub=0)

for s_ in (-1, 1):
    b_bone = "foot_l" if s_ > 0 else "foot_r"
    c_bone = "calf_l" if s_ > 0 else "calf_r"
    t_bone = "thigh_l" if s_ > 0 else "thigh_r"
    fverts = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and wz(i) < H * .09]
    fx = sum(v.x for v in fverts) / max(1, len(fverts))
    fy_min = min(v.y for v in fverts); fy_max = max(v.y for v in fverts)
    fz_min = min(v.z for v in fverts)
    flen = fy_max - fy_min + .018
    # Contoured cylindrical boot vamp + curved front toe cap & slim rubber sole + seamless ankle shaft!
    cyl(M["leather"], b_bone, .046, flen, (fx, (fy_min + fy_max) / 2 - .004, fz_min + .040),
        rot=(math.radians(90), 0, 0), scale=(1.02, .80, 1), bevel=.012)
    cyl(M["leather"], c_bone, .054, .090, (fx, (fy_min + fy_max) / 2 + .022, fz_min + .085), bevel=.006)
    box(M["leather"], b_bone, (.092, flen + .010, .020), (fx, (fy_min + fy_max) / 2 - .004, fz_min + .011), bevel=.006, sub=1)
    # 3 brass ankle strap buckles
    for bz in (H * .085, H * .135, H * .185):
        cv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(wz(i) - bz) < .015]
        if cv:
            cx_b = (max(v.x for v in cv) if s_ > 0 else min(v.x for v in cv)) + s_ * .018
            cy_b = sum(v.y for v in cv) / len(cv)
            box(M["brass"], c_bone, (.014, .022, .018), (cx_b, cy_b, bz), bevel=.003)
    # Sculpted leather knee pad + thigh cargo pocket
    kz = H * .285
    kv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(wz(i) - kz) < .018]
    if kv:
        kx = sum(v.x for v in kv) / len(kv); ky = min(v.y for v in kv)
        box(M["leather"], c_bone, (.084, .024, .096), (kx, ky - .012, kz), bevel=.010, sub=1)
        torus(M["leather"], c_bone, .068, .006, (kx, ky + .055, kz), scale=(1.05, 1.12, .8))
    tz = H * .41
    tv = [V[i].co for i in body_ids if (V[i].co.x * s_ > 0) and abs(wz(i) - tz) < .018]
    if tv:
        tx_p = (max(v.x for v in tv) if s_ > 0 else min(v.x for v in tv)) + s_ * .012
        ty_p = sum(v.y for v in tv) / len(tv)
        box(M["pants"], t_bone, (.028, .096, .118), (tx_p, ty_p, tz), bevel=.006)
        box(M["pants"], t_bone, (.032, .100, .032), (tx_p + s_ * .002, ty_p, tz + .046), bevel=.004)
        sphere(M["brass"], t_bone, .0045, (tx_p + s_ * .016, ty_p, tz + .035))

body_ids = {i for i in body_ids if wz(i) > H * .16 or i in hand}
extract("HairUnderHelmet", verts_in(human, ["scalp"], .2) & body_ids, .009, M["hair"], solid=.005, uvscale=3, sub=0)

# ---------------- Character-Specific Helmets, Hair, Goggles & Respirators ----------------
hc = rig.data.bones["head"].head_local
sh_span = shoulder - waist
z_upper = waist + sh_span * .74
z_mid = waist + sh_span * .50
z_low = waist + sh_span * .26

bm = bmesh.new()
prof = [
    (0.000,  0.145), (0.038,  0.142), (0.074,  0.130), (0.102,  0.108),
    (0.118,  0.078), (0.123,  0.046), (0.125,  0.024), (0.136,  0.008),
    (0.150, -0.010), (0.154, -0.016), (0.147, -0.019),
]
SEGS = 48; rings = []
pitch_h = math.radians(-11 if WHO == "vex" else 2)
rot_h = Matrix.Rotation(pitch_h, 3, 'X')
pivot_h = Vector((0, hc.y + .014, emid.z + .018))
for r_p, z_p in prof:
    ring = []
    for k in range(SEGS):
        a = k * 2 * math.pi / SEGS
        x = math.sin(a) * r_p * 1.01; y = -math.cos(a) * r_p * 1.15
        front_lift = 0.014 * max(0., math.cos(a)) * (1.0 if z_p < 0.04 else 0.3)
        back_drop = -0.012 * max(0., -math.cos(a)) * (1.0 if z_p < 0.04 else 0.2)
        ring.append(bm.verts.new(pivot_h + rot_h @ Vector((x, y, z_p + front_lift + back_drop))))
    rings.append(ring)
for r0, r1 in zip(rings[:-1], rings[1:]):
    for k in range(SEGS):
        bm.faces.new((r0[k], r0[(k + 1) % SEGS], r1[(k + 1) % SEGS], r1[k]))
hmesh = bpy.data.meshes.new("BossHelmet"); bm.to_mesh(hmesh); bm.free()
helm = bpy.data.objects.new("BossHelmet", hmesh); scene.collection.objects.link(helm)
sel(helm)
s_ = helm.modifiers.new("sol", "SOLIDIFY"); s_.thickness = .006; s_.offset = -1; bpy.ops.object.modifier_apply(modifier="sol")
sb = helm.modifiers.new("sub", "SUBSURF"); sb.levels = 1; bpy.ops.object.modifier_apply(modifier="sub")
if not helm.data.uv_layers: helm.data.uv_layers.new(name="UVMap")
uvl = helm.data.uv_layers.active.data
for p_poly in helm.data.polygons:
    c = p_poly.center - pivot_h
    in_decal = (c.y < -0.055 and c.z > 0.028 and abs(c.x) < 0.095)
    for li in p_poly.loop_indices:
        if not in_decal:
            uvl[li].uv = (0.04, 0.04)
        else:
            p = helm.data.vertices[helm.data.loops[li].vertex_index].co - pivot_h
            u_x = 0.5 + (p.x + (0.036 if WHO == "raven" else 0.042)) / (0.145 if WHO == "raven" else 0.135)
            u_y = 0.5 + (p.z - 0.078) / 0.135
            uvl[li].uv = (max(.06, min(.94, u_x)), max(.06, min(.94, u_y)))
helm.data.materials.append(M["emblem"])
for p_poly in helm.data.polygons: p_poly.use_smooth = True
bind(helm, "head")

torus(M["leather"], "head", .128, .007, (0, hc.y + .012, emid.z + .032), rot=(pitch_h, 0, 0), scale=(1.01, 1.15, .95))
for s_ in (-1, 1):
    strap(M["leather"], "head", [(s_ * .108, hc.y - .015, emid.z + .018), (s_ * .092, hc.y - .022, emid.z - .028), (s_ * .058, lips.y + .012, lips.z - .025)], w=.013, t=.003)

if WHO == "raven":
    for k in range(12):
        ang = math.radians(108 + k * 13.5)
        hx = math.sin(ang) * .114; hy = hc.y + .018 + math.cos(ang) * .116
        cyl(M["hair"], "head", .015, .072, (hx, hy, emid.z - .008), r2=.004, rot=(math.radians(14), 0, -ang))
    box(M["leather"], "head", (.095, .020, .024), (0, hc.y - .102, emid.z + .064), rot=(math.radians(-18), 0, 0), bevel=.003)
    box(M["brass"], "head", (.016, .024, .016), (0, hc.y - .106, emid.z + .064), bevel=.002)
    for fz, fh, rot_x in ((.118, .054, -26), (.078, .064, -14), (.034, .068, -2), (-.012, .064, 12), (-.056, .056, 24), (-.096, .044, 36)):
        box(M["armor"], "head", (.018, .038, fh), (0, hc.y + .012 - fz * .78, emid.z + .166 - abs(fz) * .28), rot=(math.radians(rot_x), 0, 0), bevel=.003)
    box(M["leather"], "head", (.028, .225, .018), (0, hc.y + .014, emid.z + .156), bevel=.004)
    for s_ in (-1, 1):
        cyl(M["canister"], "head", .036, .022, (s_ * .132, hc.y + .005, emid.z + .018), rot=(0, math.radians(85 * s_), 0), bevel=.004, uv_cube=0)
        torus(M["brass"], "head", .035, .0045, (s_ * .138, hc.y + .005, emid.z + .018), rot=(0, math.radians(85 * s_), 0))
else:
    for s_ in (-1, 1):
        for dx, dy, dz, rad in (
            (.065, -.105, .026, .022), (.098, -.080, .022, .028), (.124, -.042, .014, .032),
            (.134, -.002, .006, .031), (.126, .042, -.004, .030), (.098, .082, -.010, .030),
            (.110, -.018, -.026, .026), (.052, .112, -.014, .028),
        ):
            sphere(M["hair"], "head", rad, (s_ * dx, hc.y + dy, emid.z + dz), scale=(1.15, .92, 1.32))
    box(M["armor"], "head", (.046, .020, .046), (.022, hc.y - .104, emid.z + .086), rot=(math.radians(-25), math.radians(-10), 0), bevel=.004)
    box(M["brass"], "head", (.028, .022, .030), (.022, hc.y - .108, emid.z + .086), rot=(math.radians(-25), math.radians(-10), 0), bevel=.003)
    rx = -.134
    box(M["armor"], "head", (.028, .055, .072), (rx, hc.y + .012, emid.z + .035), rot=(0, math.radians(-10), 0), bevel=.004)
    cyl(M["brass"], "head", .005, .085, (rx - .004, hc.y + .022, emid.z + .108), bevel=.001)
    for tx_f, ty_f, rz_f in ((-.128, -.026, 22), (-.118, -.014, 12)):
        cyl(M["bone"], "head", .008, .048, (tx_f, hc.y + ty_f, emid.z - .015), r2=.001, rot=(math.radians(15), math.radians(rz_f), math.radians(-18)), bevel=.001)
    strap(M["coat"], "head", [(-.132, hc.y + .04, emid.z + .015), (-.155, hc.y + .11, emid.z - .025), (-.172, hc.y + .17, emid.z - .065)], w=.024, t=.004)
    strap(M["scarf"], "head", [(-.128, hc.y + .045, emid.z + .022), (-.148, hc.y + .12, emid.z + .005), (-.165, hc.y + .18, emid.z - .020)], w=.018, t=.003)

# Goggles + Contoured Dual-Canister Respirator + Corrugated Breather Hose
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

for k in range(10):
    t = k / 9.
    torus(M["leather"], "head", .014, .0042, (math.sin(t * 1.2) * .008, my - .036 + (t ** 1.3) * .042, mz - .032 - t * .072), rot=(math.radians(28 + t * 25), 0, 0), segs=16, msegs=6)

# ---------------- Sculpted Folded Scarf + Belt + Tactical Waist Pouches + Chest Harness ----------------
for k, (rz, rr, mr, tilt) in enumerate(((.005, .122, .030, 14), (.028, .112, .026, 6), (.054, .100, .022, 10), (.074, .090, .019, 6))):
    o = torus(M["scarf"], "neck_01", rr, mr, (0, .006, shoulder + rz), rot=(math.radians(tilt), math.radians(3 * (k - 1)), 0), scale=(1.12, 1.18, .96), segs=24, msegs=10)
    sel(o); dm = o.modifiers.new("f", "DISPLACE"); tx = bpy.data.textures.new(f"sc_{k}", "CLOUDS"); tx.noise_scale = .045
    dm.texture = tx; dm.strength = .012; bpy.ops.object.modifier_apply(modifier="f")

belt_ring = [i for i in body_ids if abs(wz(i) - waist) < .012 and abs(V[i].co.x) < .22 and i not in hand and i not in lower_arm]
bw = max(V[i].co.x for i in belt_ring); bf = min(V[i].co.y for i in belt_ring); bb = max(V[i].co.y for i in belt_ring)
torus(M["leather"], "pelvis", 1, .025, (0, (bf + bb) / 2, waist), scale=(bw + .040, (bb - bf) / 2 + .040, .95))
box(M["brass"], "pelvis", (.072, .018, .056), (0, bf - .044, waist), bevel=.005)
box(M["leather"], "pelvis", (.046, .020, .034), (0, bf - .046, waist), bevel=.003)

# Sculpted waist leather utility pouches around belt (gives Briggs-grade belt bulk!)
for px_p, ang_p in ((-.155, -22), (-.085, -8), (.085, 8), (.155, 22)):
    py_p = surf_y(waist, px_p) - .052
    pouch(M["leather"], "pelvis", (px_p, py_p, waist - .005), (.062, .042, .084), rot=(0, 0, math.radians(ang_p)))

for sx_d in (-1, 1):
    pts = [(sx_d * .110, .05, shoulder - .01), (sx_d * .110, -.02, shoulder + .025),
           (sx_d * .094, surf_y(z_upper, sx_d * .094) - .044, z_upper),
           (sx_d * .086, surf_y(waist + .03, sx_d * .086) - .044, waist + .03)]
    strap(M["leather"], "spine_02", pts, w=.042, t=.0065)
    box(M["brass"], "spine_02", (.046, .014, .032), (sx_d * .094, surf_y(z_upper, sx_d * .094) - .048, z_upper), bevel=.004)

ua_r = rig.data.bones["upperarm_r"].head_local
ua_l = rig.data.bones["upperarm_l"].head_local

if WHO == "raven":
    b_pts = [(ua_r.x + .035, -.01, shoulder + .025)] + [(x, surf_y(z, x) - .048, z) for x, z in ((-.06, z_upper), (0, z_mid), (.06, z_low), (.12, waist + .01))]
    strap(M["leather"], "spine_02", b_pts, w=.046, t=.007)
    torus(M["brass"], "spine_02", .024, .0055, (-.082, surf_y(z_upper, -.082) - .052, z_upper), rot=(math.radians(80), 0, 0))
    for t_s in (.32, .42, .52, .62):
        sx_s = -.10 + t_s * .20; sz_s = (shoulder - .02) - t_s * (sh_span - .04)
        sy_s = surf_y(sz_s, sx_s) - .054
        cyl(M["brass"], "spine_02", .010, .044, (sx_s, sy_s, sz_s), rot=(0, math.radians(40), 0), verts=14, bevel=.002)
        cyl(M["leather"], "spine_02", .0112, .016, (sx_s, sy_s + .001, sz_s), rot=(0, math.radians(40), 0), verts=14)

    # Raven Right Pauldron: Multi-plate oxblood & leather shoulder guard + 3 CURVED swept razor blade claws!
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
                pd.data.uv_layers.active.data[li].uv = (max(.06, min(.94, .5 + (-co.y - .010) / .135)), max(.06, min(.94, .5 + (co.z - .020) / .125)))
    finish(pd, M["emblem"], "upperarm_r", bevel=0, sub=1, uv_cube=0)
    # Corner brass bolts on right pauldron
    for bx_r, by_r in ((-.025, -.068), (-.025, .068)):
        sphere(M["brass"], "upperarm_r", .0065, (rx_p + bx_r, ry_p + by_r, rz_p + .018))
    box(M["leather"], "upperarm_r", (.112, .154, .016), (rx_p - .022, ry_p, rz_p - .052), rot=(0, math.radians(-48), 0), bevel=.005, sub=1)
    box(M["armor"], "upperarm_r", (.104, .140, .015), (rx_p - .038, ry_p, rz_p - .088), rot=(0, math.radians(-58), 0), bevel=.005, sub=1)

    # THREE truly curved swept blade claws via segmented tapered Bezier arc!
    for cy_off, cz_off, rot_z in ((-.064, -.004, 18), (-.008, -.010, 4), (.046, -.004, -10)):
        ring_p = (rx_p - .088, ry_p + cy_off, rz_p - .046 + cz_off)
        torus(M["brass"], "upperarm_r", .015, .0038, ring_p, rot=(0, math.radians(76), math.radians(rot_z)))
        cu_c = bpy.data.curves.new("claw", "CURVE"); cu_c.dimensions = "3D"; cu_c.bevel_depth = .012; cu_c.bevel_resolution = 3
        sp_c = cu_c.splines.new("BEZIER"); sp_c.bezier_points.add(2)
        c_pts = [
            (ring_p[0] - .006, ring_p[1], ring_p[2] - .005, 1.0),
            (ring_p[0] - .024, ring_p[1] - .004, ring_p[2] - .052, 0.72),
            (ring_p[0] - .008, ring_p[1] - .008, ring_p[2] - .102, 0.06),
        ]
        for pt_b, (px_c, py_c, pz_c, rad_c) in zip(sp_c.bezier_points, c_pts):
            pt_b.co = (px_c, py_c, pz_c); pt_b.radius = rad_c
            pt_b.handle_left_type = pt_b.handle_right_type = "AUTO"
        claw_o = bpy.data.objects.new("CurvedClaw", cu_c); scene.collection.objects.link(claw_o); sel(claw_o)
        bpy.ops.object.convert(target="MESH")
        finish(claw_o, M["claw"], "upperarm_r")

    # Left shoulder (+X): heavy multi-tiered dark brown leather armor plates + rivets
    lx_p = ua_l.x + .054; ly_p = ua_l.y; lz_p = ua_l.z + .034
    for dx_l, dz_l, ang_l in ((0.0, 0.018, 28), (.026, -.020, 44), (.042, -.058, 56)):
        box(M["leather"], "upperarm_l", (.122, .158, .018), (lx_p + dx_l, ly_p, lz_p + dz_l), rot=(0, math.radians(ang_l), 0), bevel=.005, sub=1)
        sphere(M["brass"], "upperarm_l", .006, (lx_p + dx_l + .018, ly_p - .055, lz_p + dz_l + .005))

    # Tattered flowing crimson back cape
    by_b = max(V[i].co.y for i in body_ids if abs(wz(i) - shoulder) < .03) + .046
    cape_verts = [
        (-.23, by_b - .02, shoulder + .035), (0, by_b + .025, shoulder + .055), (.22, by_b - .02, shoulder + .035),
        (-.26, by_b + .05, z_mid), (0, by_b + .07, z_mid), (.24, by_b + .05, z_mid),
        (-.30, by_b + .09, H * .30), (-.11, by_b + .10, H * .25), (0, by_b + .11, H * .31), (.13, by_b + .10, H * .26), (.28, by_b + .09, H * .30),
    ]
    bm_c = bmesh.new(); vs_c = [bm_c.verts.new(v) for v in cape_verts]
    bm_c.faces.new((vs_c[0], vs_c[1], vs_c[4], vs_c[3])); bm_c.faces.new((vs_c[1], vs_c[2], vs_c[5], vs_c[4]))
    bm_c.faces.new((vs_c[3], vs_c[4], vs_c[8], vs_c[7], vs_c[6])); bm_c.faces.new((vs_c[4], vs_c[5], vs_c[10], vs_c[9], vs_c[8]))
    cm = bpy.data.meshes.new("CrimsonCape"); bm_c.to_mesh(cm); bm_c.free()
    cape = bpy.data.objects.new("CrimsonCape", cm); scene.collection.objects.link(cape)
    sel(cape); s_cp = cape.modifiers.new("sol", "SOLIDIFY"); s_cp.thickness = .008; bpy.ops.object.modifier_apply(modifier="sol")
    finish(cape, M["scarf"], "spine_02", sub=1, uv_cube=.35)
else:
    rx_v = ua_r.x - .054; ry_v = ua_r.y - .005; rz_v = ua_r.z + .038
    box(M["armor"], "upperarm_r", (.124, .158, .018), (rx_v, ry_v, rz_v), rot=(0, math.radians(-30), 0), bevel=.005, sub=1)
    box(M["leather"], "upperarm_r", (.114, .144, .016), (rx_v - .022, ry_v, rz_v - .036), rot=(0, math.radians(-42), 0), bevel=.005, sub=1)
    box(M["armor"], "upperarm_r", (.102, .130, .014), (rx_v - .038, ry_v, rz_v - .068), rot=(0, math.radians(-52), 0), bevel=.004, sub=1)
    torus(M["coat"], "upperarm_r", .066, .015, (ua_r.x - .055, ua_r.y, ua_r.z - .095), rot=(0, math.radians(-26), 0))

    # Sculpted jagged-edge V-cowl cloth drape (replaces boxy blobs with folded torn cloth!)
    cy_ch = surf_y(z_upper + .02, 0) - .050
    cowl_v = [
        (-.135, cy_ch + .035, shoulder - .005), (0, cy_ch + .005, shoulder - .015), (.135, cy_ch + .035, shoulder - .005),
        (-.105, cy_ch + .012, z_upper + .045), (0, cy_ch - .004, z_upper + .028), (.105, cy_ch + .012, z_upper + .045),
        (-.085, cy_ch + .006, z_upper - .010), (-.030, cy_ch - .006, z_upper + .006), (.020, cy_ch - .008, z_upper - .025), (.085, cy_ch + .008, z_upper + .004),
    ]
    bm_cw = bmesh.new(); vcw = [bm_cw.verts.new(v) for v in cowl_v]
    bm_cw.faces.new((vcw[0], vcw[1], vcw[4], vcw[3])); bm_cw.faces.new((vcw[1], vcw[2], vcw[5], vcw[4]))
    bm_cw.faces.new((vcw[3], vcw[4], vcw[7], vcw[6])); bm_cw.faces.new((vcw[4], vcw[5], vcw[9], vcw[8], vcw[7]))
    cwm = bpy.data.meshes.new("VexCowlFold"); bm_cw.to_mesh(cwm); bm_cw.free()
    cwo = bpy.data.objects.new("VexCowlFold", cwm); scene.collection.objects.link(cwo)
    sel(cwo); s_cw = cwo.modifiers.new("sol", "SOLIDIFY"); s_cw.thickness = .009; bpy.ops.object.modifier_apply(modifier="sol")
    finish(cwo, M["scarf"], "spine_03", sub=1, uv_cube=.22)

    strap(M["leather"], "spine_02", [(-.065, surf_y(shoulder - .01, -.065) - .034, shoulder - .01), (0, surf_y(z_upper - .02, 0) - .056, z_upper - .038), (.065, surf_y(shoulder - .01, .065) - .034, shoulder - .01)], w=.007, t=.004)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=.028, depth=.009, location=(.006, surf_y(z_upper - .03, .006) - .062, z_upper - .040), rotation=(math.radians(82), 0, 0))
    med = bpy.context.object; sel(med); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for l in med.data.loops:
        co = med.data.vertices[l.vertex_index].co
        med.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .056, .5 + co.z / .056)
    finish(med, M["medallion"], "spine_02", uv_cube=0)

    rx_c = -.092; rz_c = z_upper + .005; ry_c = surf_y(rz_c, rx_c) - .056
    box(M["armor"], "spine_02", (.040, .028, .064), (rx_c, ry_c, rz_c), bevel=.004)
    box(M["lens"], "spine_02", (.024, .008, .026), (rx_c, ry_c - .014, rz_c + .008), bevel=.001)
    cyl(M["brass"], "spine_02", .004, .072, (rx_c - .011, ry_c, rz_c + .056), bevel=.001)
    for bx_o in (.074, .106):
        cyl(M["brass"], "spine_02", .015, .078, (bx_o, surf_y(z_upper - .03, bx_o) - .054, z_upper - .038), r2=.012, rot=(math.radians(14), math.radians(-10), 0), bevel=.003)
    box(M["armor"], "spine_02", (.034, .016, .018), (.090, surf_y(z_upper - .02, .090) - .056, z_upper - .032), bevel=.003)

# ---------------- Join, decimate high-poly MPFB skin/clothes (keep sculpted gear crisp!), keep LIVE ARMATURE, export FBX ----------------
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

# ---------------- Blender EEVEE Renders ----------------
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
