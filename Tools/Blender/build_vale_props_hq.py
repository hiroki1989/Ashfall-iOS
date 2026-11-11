# Builds Malcolm Vale's PS3-grade weathered trenchcoat lapels, popped collar, tattered shoulder capelets,
# crimson scarf & waist sash, right-chest D-ring, leather belt/straps, and bronze dual-canister half-respirator
# cleanly in Unity bone-local space so they attach onto Vale's existing live DetailedHero3D + CommonBody skeleton.
# Run:
#   blender -b --factory-startup --python-exit-code 1 -P Tools/Blender/build_vale_props_hq.py
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector, Matrix

OUT = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/ValeProps/ValeProps.fbx"
TEX = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters/ValeProps/Textures/"
PREV = "D:/Unity/AssetIntegration/FactionBossesHQ"
os.makedirs(TEX, exist_ok=True); os.makedirs(os.path.dirname(OUT), exist_ok=True); os.makedirs(PREV, exist_ok=True)
RNG = np.random.default_rng(303)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

def sel(o):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True); bpy.context.view_layer.objects.active = o

# Convert Unity bone-local meters (+X right, +Y up, +Z forward) to Blender coords
# matching FBX export (axis_forward='-Z', axis_up='Y'): Blender = (-X_u, -Z_u, +Y_u)
def u2b(x, y, z): return (-x, -z, y)

# ---------------- 1024x1024 NumPy Weathered PBR Albedo & Normal Generator ----------------
def fbm_2d(H_, W_, octaves=(8, 16, 32, 64, 128), weights=(.42, .26, .16, .10, .06)):
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
    return (acc - acc.min()) / max(1e-5, acc.max() - acc.min())

def save_np_img(name, arr, colorspace="sRGB"):
    h_, w_ = arr.shape[:2]
    rgba = np.ones((h_, w_, 4), dtype=np.float32)
    rgba[..., :3] = np.clip(arr, 0.0, 1.0)
    im = bpy.data.images.new(name, w_, h_)
    im.colorspace_settings.name = colorspace
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = TEX + name + ".png"; im.file_format = "PNG"; im.save()
    bpy.data.images.remove(im)

def save_normal(name, hmap, strength=5.5):
    dy = np.roll(hmap, -1, axis=0) - np.roll(hmap, 1, axis=0)
    dx = np.roll(hmap, -1, axis=1) - np.roll(hmap, 1, axis=1)
    nx = -dx * strength; ny = -dy * strength; nz = np.ones_like(hmap)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    save_np_img(name + "_n", np.stack([nx / l * .5 + .5, ny / l * .5 + .5, nz / l * .5 + .5], axis=-1), "Non-Color")

def make_fabric(name, base, grime, S=1024):
    n1 = fbm_2d(S, S, (6, 12, 24, 48, 96)); n2 = fbm_2d(S, S, (20, 48, 96, 192))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    weave = .88 + .12 * (.6 * (np.sin((xx + yy) * .9) * .5 + .5) + .4 * (np.sin((xx - yy) * .9) * .5 + .5))
    folds = np.sin(yy * .032 + n1 * 4.5) * .5 + .5
    dirt = np.clip((n1 - .34) * 1.7, 0, 1) * .58 + np.clip((n2 - .48) * 1.3, 0, 1) * .25
    col = np.zeros((S, S, 3), dtype=np.float32)
    b, g = np.array(base, np.float32), np.array(grime, np.float32)
    for c in range(3):
        col[..., c] = (b[c] * (1 - dirt) + g[c] * dirt) * weave * (.85 + .22 * folds)
    save_np_img(name, col)
    save_normal(name, weave * .25 + folds * .35 + n2 * .32)

def make_leather(name, base, worn, S=1024):
    n1 = fbm_2d(S, S, (6, 14, 28, 64)); n2 = fbm_2d(S, S, (32, 64, 128, 256))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    pebble = np.sin(n2 * 18. + n1 * 4.) * .5 + .5
    wear = np.clip((n1 - .42) * 2., 0, 1) * .55
    b, w = np.array(base, np.float32), np.array(worn, np.float32)
    col = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3): col[..., c] = (b[c] * (1 - wear) + w[c] * wear) * (.85 + .25 * pebble)
    save_np_img(name, col)
    save_normal(name, pebble * .38 + n2 * .40, strength=6.0)

def make_metal(name, base, rust, edge, S=1024):
    n1 = fbm_2d(S, S, (8, 16, 32, 64)); n2 = fbm_2d(S, S, (24, 64, 128))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    brush = np.sin(xx * .55 + n1 * 8.) * .5 + .5
    pits = np.clip((.36 - n2) * 3.5, 0, 1)
    chip = np.clip((n1 - .66) * 3.2, 0, 1) * .55
    b, r, e = np.array(base, np.float32), np.array(rust, np.float32), np.array(edge, np.float32)
    col = np.zeros((S, S, 3), dtype=np.float32)
    for c in range(3):
        col[..., c] = (b[c] * (.86 + .22 * brush) * (1 - pits * .6) + r[c] * pits * .6) * (1 - chip * .55) + e[c] * chip * .55
    save_np_img(name, col)
    save_normal(name, brush * .22 + n2 * .35 - pits * .65, strength=5.0)

def make_caps():
    S = 512; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    nx = (xx - S / 2) / (S / 2); ny = (yy - S / 2) / (S / 2); r = np.hypot(nx, ny)
    holes = [(0, 0)] + [(math.cos(k * 2 * math.pi / cnt + rr * 1.3) * rr, math.sin(k * 2 * math.pi / cnt + rr * 1.3) * rr) for rr, cnt in ((.28, 6), (.54, 12), (.76, 16)) for k in range(cnt)]
    d = np.full((S, S), 99., np.float32)
    for hx, hy in holes: d = np.minimum(d, np.hypot(nx - hx, ny - hy))
    n1 = fbm_2d(S, S, (8, 16, 32))
    col = np.zeros((S, S, 3), np.float32)
    for c in range(3):
        col[..., c] = (.46 * (.82 + .28 * n1), .34 * (.82 + .28 * n1), .18 * (.82 + .28 * n1))[c]
        col[d < .068, c] = (.03, .028, .025)[c]
        col[(d >= .068) & (d < .094) & (r < .84), c] = (.62, .48, .25)[c]
    save_np_img("vale_canister", col)
    slot = (np.abs(nx) < .58) & (np.abs(ny) < .66) & ((np.floor((ny + .7) * 13).astype(np.int32) % 2) == 0)
    vc = np.zeros((S, S, 3), np.float32)
    for c in range(3): vc[..., c] = np.where(slot, (.025, .025, .03)[c], (.38, .29, .16)[c] * (.85 + .25 * n1))
    save_np_img("vale_vent", vc)

make_fabric("vale_coat", (.20, .145, .112), (.09, .065, .05))
make_fabric("vale_scarf", (.34, .095, .090), (.16, .045, .042))
make_leather("vale_leather", (.21, .14, .09), (.35, .25, .17))
make_metal("vale_bronze", (.46, .35, .19), (.20, .14, .07), (.65, .52, .30))
make_fabric("vale_scar", (.46, .16, .14), (.32, .10, .09), S=256)
make_caps()

def tex_mat(name, path, rough=.85, metal=0.):
    m = bpy.data.materials.new(f"Vale_{name}"); m.use_nodes = True
    N, L = m.node_tree.nodes, m.node_tree.links; b = N["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if path and os.path.exists(path):
        t = N.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(path)
        L.new(t.outputs[0], b.inputs["Base Color"])
    return m

M = dict(
    coat=tex_mat("Coat", TEX + "vale_coat.png", .90),
    scarf=tex_mat("Scarf", TEX + "vale_scarf.png", .92),
    leather=tex_mat("Leather", TEX + "vale_leather.png", .50),
    bronze=tex_mat("Bronze", TEX + "vale_bronze.png", .34, .82),
    scar=tex_mat("Scar", TEX + "vale_scar.png", .65),
    canister=tex_mat("Canister", TEX + "vale_canister.png", .35, .82),
    vent=tex_mat("Vent", TEX + "vale_vent.png", .40, .70),
)

# Group builder: collects clean primitives & UV-projects them, then joins into target Unity bone-socket object
groups = {k: [] for k in ("Vale_HeadProps", "Vale_ChestProps", "Vale_ShoulderL", "Vale_ShoulderR", "Vale_PelvisProps")}

def prep(o, mat, grp, bevel=0, sub=0, uv_cube=.22):
    o.data.materials.append(mat); sel(o)
    if bevel: b = o.modifiers.new("b", "BEVEL"); b.width = bevel; b.segments = 2; bpy.ops.object.modifier_apply(modifier="b")
    if sub: s_ = o.modifiers.new("s", "SUBSURF"); s_.levels = sub; bpy.ops.object.modifier_apply(modifier="s")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if uv_cube:
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=uv_cube); bpy.ops.object.mode_set(mode="OBJECT")
    for p in o.data.polygons: p.use_smooth = True
    groups[grp].append(o); return o

def u_box(grp, mat, size_u, pos_u, rot_deg=(0, 0, 0), bevel=.004, sub=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=u2b(*pos_u), rotation=(math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])))
    o = bpy.context.object; o.scale = (size_u[0], size_u[2], size_u[1])
    return prep(o, mat, grp, bevel, sub)

def u_cyl(grp, mat, r, depth, pos_u, rot_deg=(0, 0, 0), r2=None, verts=24, bevel=.003, uv_cube=.20):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2 if r2 is not None else r, depth=depth,
        location=u2b(*pos_u), rotation=(math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])))
    return prep(bpy.context.object, mat, grp, bevel, uv_cube=uv_cube)

def u_torus(grp, mat, R, r, pos_u, rot_deg=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=u2b(*pos_u),
        rotation=(math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])), major_segments=28, minor_segments=10)
    o = bpy.context.object; o.scale = scale; return prep(o, mat, grp)

def u_strap(grp, mat, pts_u, w=.016, t=.004):
    cu = bpy.data.curves.new("s", "CURVE"); cu.dimensions = "3D"; cu.extrude = w / 2; cu.bevel_depth = .0012; cu.bevel_resolution = 2
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts_u) - 1)
    for bp, pu in zip(sp.bezier_points, pts_u):
        bp.co = u2b(*pu); bp.handle_left_type = bp.handle_right_type = "AUTO"
    o = bpy.data.objects.new("s", cu); scene.collection.objects.link(o); sel(o)
    bpy.ops.object.convert(target="MESH")
    s_ = o.modifiers.new("sol", "SOLIDIFY"); s_.thickness = t; bpy.ops.object.modifier_apply(modifier="sol")
    return prep(o, mat, grp)

# ---------------- 1. Vale_HeadProps (Half-respirator + Straps + Forehead Scar) ----------------
u_strap("Vale_HeadProps", M["scar"], [(0.022, 0.068, 0.102), (0.034, 0.084, 0.100), (0.045, 0.098, 0.093)], w=.0055, t=.0025)
bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=14, radius=.044, location=u2b(0, 0.018, 0.096))
fp = bpy.context.object; fp.scale = (.88, .86, .95); sel(fp)
bm = bmesh.new(); bm.from_mesh(fp.data)
for v in bm.verts:
    if v.co.z > 0: v.co.x *= max(.30, 1.0 - v.co.z * 11.); v.co.y -= v.co.z * .25
    if v.co.y > .012: v.co.y = .012
bm.to_mesh(fp.data); bm.free(); prep(fp, M["bronze"], "Vale_HeadProps", sub=1)
u_torus("Vale_HeadProps", M["leather"], .040, .005, (0, 0.016, 0.090), rot_deg=(80, 0, 0), scale=(.92, 1.04, .88))
u_cyl("Vale_HeadProps", M["bronze"], .021, .016, (0, 0.008, 0.128), rot_deg=(96, 0, 0))
bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.018, depth=.005, location=u2b(0, 0.007, 0.135), rotation=(math.radians(96), 0, 0))
vo = bpy.context.object; sel(vo); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
for l in vo.data.loops:
    co = vo.data.vertices[l.vertex_index].co
    vo.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .036, .5 + co.z / .036)
prep(vo, M["vent"], "Vale_HeadProps", uv_cube=0)
for s_ in (-1, 1):
    cx, cy, cz = s_ * .044, 0.010, 0.098
    rc = (math.radians(78), 0, math.radians(-46 * s_))
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=.025, radius2=.025, depth=.026, location=u2b(cx, cy, cz), rotation=rc)
    prep(bpy.context.object, M["bronze"], "Vale_HeadProps", bevel=.003)
    u_torus("Vale_HeadProps", M["bronze"], .025, .0032, (cx + s_ * .009, cy - .002, cz + .010), rot_deg=(78, 0, -46 * s_))
    bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=.023, depth=.005, location=u2b(cx + s_ * .011, cy - .003, cz + .012), rotation=rc)
    co_o = bpy.context.object; sel(co_o); bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for l in co_o.data.loops:
        co = co_o.data.vertices[l.vertex_index].co
        co_o.data.uv_layers.active.data[l.index].uv = (.5 + co.x / .046, .5 + co.y / .046)
    prep(co_o, M["canister"], "Vale_HeadProps", uv_cube=0)
    u_strap("Vale_HeadProps", M["leather"], [(s_ * .036, .046, .096), (s_ * .086, .075, .022), (s_ * .068, .082, -.068)], w=.010, t=.003)
    u_strap("Vale_HeadProps", M["leather"], [(s_ * .044, .008, .082), (s_ * .082, .016, .005), (s_ * .062, .024, -.072)], w=.010, t=.003)

# ---------------- 2. Vale_ChestProps (Tailored Burgundy Scarf + Open-Front Popped Coat Collar + Flat Angled Lapels + D-ring + Belt) ----------------
# Slim crimson neck scarf below chin so jaw & respirator breathe cleanly
u_torus("Vale_ChestProps", M["scarf"], .086, .015, (0, .188, 0.008), rot_deg=(12, 0, 0), scale=(1.04, 1.10, .92))
u_strap("Vale_ChestProps", M["scarf"], [(-.052, .185, .082), (0, .125, .136), (.052, .185, .082)], w=.052, t=.006)
# Tailored open-front popped trenchcoat collar wrapping sides & back of neck ONLY (no giant donut ring in front!)
u_strap("Vale_ChestProps", M["coat"], [(-.095, .185, .062), (-.105, .212, -.025), (0, .218, -.088), (.105, .212, -.025), (.095, .185, .062)], w=.058, t=.008)
# Flat diagonal notched lapels hugging upper chest contour cleanly
for s_ in (-1, 1):
    u_strap("Vale_ChestProps", M["coat"], [(s_ * .096, .182, .082), (s_ * .076, .132, .140), (s_ * .025, .048, .154)], w=.052, t=.007)
    u_cyl("Vale_ChestProps", M["bronze"], .010, .006, (s_ * .078, .148, .148))
    u_cyl("Vale_ChestProps", M["bronze"], .010, .006, (s_ * .048, .032, .158))
# Right chest (+X_u) webbing loop + brass D-ring
u_strap("Vale_ChestProps", M["leather"], [(0.080, 0.140, 0.148), (0.080, 0.095, 0.152)], w=.018, t=.004)
u_torus("Vale_ChestProps", M["bronze"], .014, .0035, (0.080, 0.082, 0.156), rot_deg=(0, 0, 0))
# Diagonal cross-chest leather belt & brass slider buckle
u_strap("Vale_ChestProps", M["leather"], [(-.086, .185, .118), (0, .100, .152), (.112, -.045, .138)], w=.030, t=.0055)
u_box("Vale_ChestProps", M["bronze"], (.040, .032, .011), (-.018, .115, .154), rot_deg=(0, 0, -40), bevel=.003)

# ---------------- 3. Vale_ShoulderL & R (Low-profile layered leather capelets hugging top of upper arm) ----------------
for grp_s, side_u in (("Vale_ShoulderL", -1), ("Vale_ShoulderR", 1)):
    for lk, (dy_m, sc_m) in enumerate(((-.015, 1.0), (-.045, 1.06))):
        u_box(grp_s, M["coat"], (.018, .078 * sc_m, .138 * sc_m), (side_u * (.028 + lk * .010), dy_m, 0),
              rot_deg=(0, 0, side_u * -(28 + lk * 12)), bevel=.007, sub=1)

# ---------------- 4. Vale_PelvisProps (Leather waist belt + brass buckle + hanging crimson sash tie) ----------------
u_torus("Vale_PelvisProps", M["leather"], 1, .024, (0, .008, .002), scale=(.178, .162, .95))
u_box("Vale_PelvisProps", M["bronze"], (.062, .050, .018), (0, .008, .162), bevel=.004)
u_strap("Vale_PelvisProps", M["scarf"], [(-.055, .012, .155), (-.062, -.075, .162), (-.070, -.165, .158)], w=.036, t=.007)
u_strap("Vale_PelvisProps", M["scarf"], [(-.042, .010, .155), (-.044, -.060, .165), (-.048, -.130, .162)], w=.026, t=.006)

# Join each socket group cleanly & export ValeProps.fbx
for grp_name, objs in groups.items():
    sel(objs[0])
    for o in objs[1:]: o.select_set(True)
    bpy.ops.object.join()
    joined = bpy.context.object; joined.name = grp_name
    for p in joined.data.polygons: p.use_smooth = True

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=OUT, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", bake_space_transform=True, path_mode="RELATIVE", mesh_smooth_type="FACE"
)
print("VALE_PROPS_EXPORTED", OUT)
