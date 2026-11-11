# Upgrades Raven, Vex & Malcolm ValeProps textures to AAA 4-Map PBR (2048x2048 Albedo, Tangent Normal,
# MetallicGlossMap, and Cycles Raytraced AO) matching portrait-{raven,vex,vale}-v8.png color/material fidelity.
# Preserves 100% of existing meshes, armatures, bone names, skin weights, and ValeProps socket transforms.
#
# Run:
#   blender -b --factory-startup --python-exit-code 1 -P Tools/Blender/upgrade_boss_textures_aaa.py
import bpy, math, os
from mathutils import Vector
import numpy as np

ASSETS = "D:/Unity/Ashfall-iOS/Assets/Resources/"
PORTRAITS = ASSETS + "Art/Portraits/"
HEAD_ATLAS = ASSETS + "Art/GeneratedWarden/head-rebuilt-atlas.png"
RNG = np.random.default_rng(777)

def s2l_c(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

def lin(r, g, b):
    """Converts intuitive sRGB [0..1] color tuple to true Blender Linear float tuple."""
    return (s2l_c(r), s2l_c(g), s2l_c(b))

def sel(o):
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

def fbm_2d(H_, W_, octaves=(8, 16, 32, 64), weights=(.42, .28, .18, .12)):
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

def save_rgba_np(path, rgba, colorspace="Non-Color"):
    h_, w_ = rgba.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w_, h_, alpha=True)
    im.colorspace_settings.name = colorspace
    im.pixels.foreach_set(np.clip(rgba, 0.0, 1.0).ravel())
    im.filepath_raw = path; im.file_format = "PNG"; im.save()
    bpy.data.images.remove(im)

def load_srgb_np(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    arr = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(im)
    return arr

def upgrade_vale_veteran_face():
    """Creates bespoke AAA veteran face + subtle silver-templed dark hair from CommonBody head atlas."""
    out_dir = ASSETS + "Characters/ValeProps/Textures/"
    os.makedirs(out_dir, exist_ok=True)
    arr = load_srgb_np(HEAD_ATLAS)
    h, w = arr.shape[:2]
    rgb = arr[..., :3].copy()
    alpha = arr[..., 3:4].copy()

    # Note: im.pixels returns sRGB-encoded [0..1] values directly!
    lum = 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]
    chroma = np.max(rgb, axis=-1) - np.min(rgb, axis=-1)
    skin_mask = (rgb[..., 0] > rgb[..., 2] * 1.15) & (lum > 0.14) & (chroma > 0.04)
    # Capture ALL hair/eye/eyebrow non-skin pixels (including bright painted highlights!) so zero white blotches remain
    hair_mask = (~skin_mask) & (lum > 0.005) & (alpha[..., 0] > 0.1)

    n1 = fbm_2d(h, w, (16, 40, 96, 192))
    n2 = fbm_2d(h, w, (48, 128, 256))

    # 1) Rich dark espresso hair with subtle silver-ash veteran streaks (clamp out loud white specular hotspots!)
    salt = np.clip((n2 - 0.62) * 3.0, 0.0, 1.0) * 0.12
    hair_col = np.minimum(rgb, 0.24) * 0.72 + salt[..., None] * np.array([0.36, 0.36, 0.38], dtype=np.float32)
    rgb[hair_mask] = np.clip(hair_col[hair_mask], 0.02, 0.26)

    # 2) Weathered veteran olive-tan skin + subtle micro-pore contrast + chin/jaw stubble grit
    pores = (n2 - 0.5) * 0.045
    grit = (n1 - 0.5) * 0.035
    weathered = np.clip(rgb * np.array([0.91, 0.85, 0.79]) + pores[..., None] + grit[..., None], 0.02, 0.96)
    rgb[skin_mask] = weathered[skin_mask]

    save_rgba_np(out_dir + "vale_face.png", np.concatenate([rgb, alpha], axis=-1), "sRGB")

    # Crisp micro-pore + hair strand tangent normal map
    hmap = lum * 0.60 + n2 * 0.40
    dy = np.roll(hmap, -1, axis=0) - np.roll(hmap, 1, axis=0)
    dx = np.roll(hmap, -1, axis=1) - np.roll(hmap, 1, axis=1)
    nx, ny, nz = -dx * 4.0, -dy * 4.0, np.ones_like(hmap)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / l * .5 + .5, ny / l * .5 + .5, nz / l * .5 + .5, np.ones_like(hmap)], axis=-1)
    save_rgba_np(out_dir + "vale_face_n.png", nrm, "Non-Color")
    print("VALE_VETERAN_FACE_OK")

def clean_boss_skin_texture(who):
    """Rebuilds {who}_body.png so cheeks/ears have zero muddy mask projection overlap and warm rugged skin."""
    tex_dir = ASSETS + f"Characters/{who.capitalize()}/Textures/"
    src_path = tex_dir + f"{who}_body.png"
    arr = load_srgb_np(src_path)
    h, w = arr.shape[:2]
    rgb = arr[..., :3].copy()

    # Warm rugged wasteland sun-tan base color derived from clean torso swatch (~sRGB 0.54, 0.37, 0.29)
    n_coarse = fbm_2d(h, w, (12, 28, 64))
    n_pores = fbm_2d(h, w, (48, 128, 256))
    fg = (rgb[..., 0] + rgb[..., 1] + rgb[..., 2]) > 0.04

    base_skin = np.array([0.52, 0.36, 0.28], dtype=np.float32) if who == "raven" else np.array([0.55, 0.39, 0.31], dtype=np.float32)
    clean_skin = np.clip(base_skin[None, None, :] + (n_coarse[..., None] - 0.5) * 0.045 + (n_pores[..., None] - 0.5) * 0.030, 0.04, 0.92)
    # Replace ALL skin foreground cleanly (erasing distorted respirator stamp on the cheek island!)
    rgb[fg] = clean_skin[fg]
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :3] = rgb
    save_rgba_np(src_path, rgba, "sRGB")

    # Also bake crisp micro-pore Skin normal map!
    dy = np.roll(n_pores, -1, axis=0) - np.roll(n_pores, 1, axis=0)
    dx = np.roll(n_pores, -1, axis=1) - np.roll(n_pores, 1, axis=1)
    nx, ny, nz = -dx * 2.8, -dy * 2.8, np.ones_like(n_pores)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / l * .5 + .5, ny / l * .5 + .5, nz / l * .5 + .5, np.ones_like(n_pores)], axis=-1)
    save_rgba_np(tex_dir + f"{who}_skin_n.png", nrm, "Non-Color")
    print(f"SKIN_CLEANED_{who.upper()}")

def make_cloth_twill(path, srgb_main, srgb_dark, srgb_thread=None, S=512):
    """Seamless fine herringbone/twill wasteland fabric texture (zero macro polka blotches!)."""
    n1 = fbm_2d(S, S, (6, 12, 24))
    n2 = fbm_2d(S, S, (32, 64, 128))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    diag = np.sin((xx + yy) * 1.15) * 0.5 + 0.5
    cross = np.sin((xx - yy) * 1.15) * 0.5 + 0.5
    weave = 0.88 + 0.12 * (0.65 * diag + 0.35 * cross)
    wear = np.clip((n1 - 0.35) * 1.4, 0.0, 1.0) * 0.45 + np.clip((n2 - 0.50) * 1.1, 0.0, 1.0) * 0.15
    c0 = np.array(srgb_main, np.float32)
    c1 = np.array(srgb_dark, np.float32)
    col = (c0[None, None, :] * (1.0 - wear[..., None]) + c1[None, None, :] * wear[..., None]) * weave[..., None]
    if srgb_thread is not None:
        fray = np.clip((n2 - 0.62) * 3.5, 0.0, 1.0) * 0.25
        col = col * (1.0 - fray[..., None]) + np.array(srgb_thread, np.float32)[None, None, :] * fray[..., None]
    rgba = np.ones((S, S, 4), np.float32)
    rgba[..., :3] = np.clip(col, 0.01, 0.96)
    save_rgba_np(path, rgba, "sRGB")

def make_pebbled_leather(path, srgb_main, srgb_crevice, S=512):
    n1 = fbm_2d(S, S, (8, 16, 32))
    n2 = fbm_2d(S, S, (32, 64, 128))
    pebble = np.sin(n2 * 16.0 + n1 * 4.0) * 0.5 + 0.5
    grain = 0.84 + 0.22 * pebble
    crevice = np.clip((0.55 - n1) * 1.8, 0.0, 1.0) * 0.50
    c0 = np.array(srgb_main, np.float32)
    c1 = np.array(srgb_crevice, np.float32)
    col = (c0[None, None, :] * (1.0 - crevice[..., None]) + c1[None, None, :] * crevice[..., None]) * grain[..., None]
    rgba = np.ones((S, S, 4), np.float32); rgba[..., :3] = np.clip(col, 0.01, 0.96)
    save_rgba_np(path, rgba, "sRGB")

def make_brushed_metal(path, srgb_main, srgb_grime, S=512):
    n1 = fbm_2d(S, S, (8, 20, 48))
    n2 = fbm_2d(S, S, (32, 96, 160))
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    brush = np.sin(xx * 0.45 + n1 * 6.0) * 0.5 + 0.5
    pit = np.clip((0.36 - n2) * 3.0, 0.0, 1.0) * 0.45
    c0 = np.array(srgb_main, np.float32)
    c1 = np.array(srgb_grime, np.float32)
    col = (c0[None, None, :] * (0.88 + 0.18 * brush[..., None])) * (1.0 - pit[..., None]) + c1[None, None, :] * pit[..., None]
    rgba = np.ones((S, S, 4), np.float32); rgba[..., :3] = np.clip(col, 0.01, 0.96)
    save_rgba_np(path, rgba, "sRGB")

def build_pbr_node_tree(mat, tex_path, obj_scale=16.0, rough=0.80, metal=0.0, edge_lin=None,
                        edge_rough=0.35, edge_metal=None, dust_str=0.0, bump_str=0.45,
                        preserve_uv_map=False, tint_lin=None, micro_grain=0.14):
    """3D-coordinate-consistent Cycles PBR shader: avoids UV island scale seams and converts edge/dust strictly in linear space."""
    mat.use_nodes = True
    N = mat.node_tree.nodes; L = mat.node_tree.links
    N.clear()
    out = N.new("ShaderNodeOutputMaterial")
    bsdf = N.new("ShaderNodeBsdfPrincipled")
    L.new(bsdf.outputs[0], out.inputs[0])

    geom = N.new("ShaderNodeNewGeometry")
    tcoord = N.new("ShaderNodeTexCoord")

    img_node = N.new("ShaderNodeTexImage")
    if tex_path and os.path.exists(tex_path):
        im = bpy.data.images.load(tex_path, check_existing=True)
        img_node.image = im
    if preserve_uv_map:
        uvn = N.new("ShaderNodeUVMap"); uvn.uv_map = "UVMap"
        L.new(uvn.outputs[0], img_node.inputs[0])
        # Subtle micro noise for decal materials
        noise_d = N.new("ShaderNodeTexNoise"); noise_d.inputs["Scale"].default_value = 48.0; noise_d.inputs["Detail"].default_value = 6.0
        L.new(geom.outputs["Position"], noise_d.inputs[0])
        noise_val = noise_d.outputs["Fac"]
    else:
        # Box-projected object-space sampling! Eliminates stretched UVs & UV scale seams across coats/sleeves/skirts!
        img_node.projection = "BOX"
        img_node.projection_blend = 0.25
        mp = N.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (obj_scale, obj_scale, obj_scale)
        L.new(tcoord.outputs["Object"], mp.inputs[0])
        L.new(mp.outputs[0], img_node.inputs[0])

        noise_d = N.new("ShaderNodeTexNoise"); noise_d.inputs["Scale"].default_value = 42.0; noise_d.inputs["Detail"].default_value = 8.0
        L.new(geom.outputs["Position"], noise_d.inputs[0])
        noise_val = noise_d.outputs["Fac"]

    base_out = img_node.outputs["Color"]

    # Optional linear tint multiplier (e.g. deepening leather/cloth/lens vibrance)
    if tint_lin is not None:
        mul_t = N.new("ShaderNodeMix"); mul_t.data_type = "RGBA"; mul_t.blend_type = "MULTIPLY"
        mul_t.inputs[0].default_value = 1.0
        L.new(base_out, mul_t.inputs[6])
        mul_t.inputs[7].default_value = (*tint_lin, 1.0)
        base_out = mul_t.outputs[2]

    # Subtle micro-value breakup (+/- micro_grain) so large cloth areas feel rich without camo dots
    if micro_grain > 0.0:
        mul_g = N.new("ShaderNodeMix"); mul_g.data_type = "RGBA"; mul_g.blend_type = "OVERLAY"
        mul_g.inputs[0].default_value = micro_grain
        L.new(base_out, mul_g.inputs[6])
        L.new(noise_val, mul_g.inputs[7])
        base_out = mul_g.outputs[2]

    # Crevice grime darkening on folds, strap seams & recessed pockets (Pointiness < 0.494)
    cv_ramp = N.new("ShaderNodeValToRGB")
    cv_ramp.color_ramp.elements[0].position = 0.445; cv_ramp.color_ramp.elements[0].color = (0.48, 0.48, 0.48, 1)
    cv_ramp.color_ramp.elements[1].position = 0.494; cv_ramp.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1)
    L.new(geom.outputs["Pointiness"], cv_ramp.inputs[0])
    mul_cv = N.new("ShaderNodeMix"); mul_cv.data_type = "RGBA"; mul_cv.blend_type = "MULTIPLY"
    mul_cv.inputs[0].default_value = 0.85
    L.new(base_out, mul_cv.inputs[6])
    L.new(cv_ramp.outputs[0], mul_cv.inputs[7])
    base_out = mul_cv.outputs[2]

    # Sharp Pointiness edge-wear ONLY on true raised corners/metal rims (0.572 .. 0.630)
    # Prevents thin leather straps / wrist wraps from turning pale across their whole face
    pt_ramp = N.new("ShaderNodeValToRGB")
    pt_ramp.color_ramp.elements[0].position = 0.572; pt_ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    pt_ramp.color_ramp.elements[1].position = 0.630; pt_ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    L.new(geom.outputs["Pointiness"], pt_ramp.inputs[0])

    if edge_lin is not None:
        mix_e = N.new("ShaderNodeMix"); mix_e.data_type = "RGBA"
        L.new(pt_ramp.outputs[0], mix_e.inputs[0])
        L.new(base_out, mix_e.inputs[6])
        mix_e.inputs[7].default_value = (*edge_lin, 1.0)
        base_out = mix_e.outputs[2]

    # Subtle wasteland lower-hem mud/dust (Z < 0.38m ONLY, deep muted earth in TRUE linear space!)
    if dust_str > 0.0:
        sep = N.new("ShaderNodeSeparateXYZ")
        L.new(geom.outputs["Position"], sep.inputs[0])
        dramp = N.new("ShaderNodeValToRGB")
        dramp.color_ramp.elements[0].position = 0.03; dramp.color_ramp.elements[0].color = (dust_str, dust_str, dust_str, 1)
        dramp.color_ramp.elements[1].position = 0.38; dramp.color_ramp.elements[1].color = (0, 0, 0, 1)
        L.new(sep.outputs["Z"], dramp.inputs[0])
        mix_d = N.new("ShaderNodeMix"); mix_d.data_type = "RGBA"
        L.new(dramp.outputs[0], mix_d.inputs[0])
        L.new(base_out, mix_d.inputs[6])
        # sRGB (0.28, 0.24, 0.19) muted wasteland dust -> Linear ~ (0.063, 0.047, 0.030)
        mix_d.inputs[7].default_value = (*lin(0.28, 0.24, 0.19), 1.0)
        base_out = mix_d.outputs[2]

    # Roughness & Metallic variation
    mix_r = N.new("ShaderNodeMix"); mix_r.data_type = "FLOAT"
    L.new(pt_ramp.outputs[0], mix_r.inputs[0])
    r_math = N.new("ShaderNodeMapRange")
    r_math.inputs["From Min"].default_value = 0.25; r_math.inputs["From Max"].default_value = 0.75
    r_math.inputs["To Min"].default_value = max(0.03, rough - 0.07)
    r_math.inputs["To Max"].default_value = min(0.98, rough + 0.07)
    L.new(noise_val, r_math.inputs[0])
    L.new(r_math.outputs[0], mix_r.inputs[2])
    mix_r.inputs[3].default_value = edge_rough
    rough_out = mix_r.outputs[0]

    if edge_metal is not None and abs(edge_metal - metal) > 0.01:
        mix_m = N.new("ShaderNodeMix"); mix_m.data_type = "FLOAT"
        L.new(pt_ramp.outputs[0], mix_m.inputs[0])
        mix_m.inputs[2].default_value = metal
        mix_m.inputs[3].default_value = edge_metal
        metal_out = mix_m.outputs[0]
    else:
        metal_val = N.new("ShaderNodeValue"); metal_val.outputs[0].default_value = metal
        metal_out = metal_val.outputs[0]

    L.new(base_out, bsdf.inputs["Base Color"])
    L.new(rough_out, bsdf.inputs["Roughness"])
    L.new(metal_out, bsdf.inputs["Metallic"])

    if bump_str > 0.0:
        bev = N.new("ShaderNodeBevel"); bev.samples = 4; bev.inputs["Radius"].default_value = 0.0042
        # Add medium cloth-crease / leather-grain noise so closeups have rich tactile relief
        crease_n = N.new("ShaderNodeTexNoise")
        crease_n.inputs["Scale"].default_value = 14.0; crease_n.inputs["Detail"].default_value = 10.0; crease_n.inputs["Distortion"].default_value = 0.6
        L.new(geom.outputs["Position"], crease_n.inputs[0])

        bmp = N.new("ShaderNodeBump"); bmp.inputs["Strength"].default_value = min(1.0, bump_str * 1.65); bmp.inputs["Distance"].default_value = 0.011
        bw = N.new("ShaderNodeRGBToBW")
        L.new(img_node.outputs["Color"], bw.inputs[0])
        bmix = N.new("ShaderNodeMath"); bmix.operation = "ADD"
        L.new(bw.outputs[0], bmix.inputs[0])
        mul_n = N.new("ShaderNodeMath"); mul_n.operation = "MULTIPLY"; mul_n.inputs[1].default_value = 0.42
        L.new(crease_n.outputs["Fac"], mul_n.inputs[0])
        L.new(mul_n.outputs[0], bmix.inputs[1])
        L.new(bmix.outputs[0], bmp.inputs["Height"])
        L.new(bev.outputs[0], bmp.inputs["Normal"])
        L.new(bmp.outputs[0], bsdf.inputs["Normal"])

def bake_atlas_group(obj, target_mat_names, uv_layer_name, out_prefix, size=2048):
    sel(obj)
    me = obj.data
    if uv_layer_name not in me.uv_layers:
        me.uv_layers.new(name=uv_layer_name)
    me.uv_layers.active = me.uv_layers[uv_layer_name]
    target_indices = {i for i, m in enumerate(me.materials) if m and m.name in target_mat_names}
    if not target_indices:
        return

    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="FACE")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in me.polygons:
        p.select = (p.material_index in target_indices)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.012, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"; scene.cycles.device = "CPU"
    scene.render.bake.margin = 12
    scene.render.bake.use_selected_to_active = False

    def attach_target_img(img_obj):
        for i, m in enumerate(me.materials):
            if not m or not m.use_nodes: continue
            N = m.node_tree.nodes
            old = N.get("AAABakeTarget")
            if old: N.remove(old)
            if i in target_indices:
                t = N.new("ShaderNodeTexImage"); t.name = "AAABakeTarget"; t.image = img_obj
                N.active = t
            else:
                N.active = None

    def set_emit_source(mode_key):
        for i, m in enumerate(me.materials):
            if i not in target_indices or not m or not m.use_nodes: continue
            N = m.node_tree.nodes; L = m.node_tree.links
            out = next(n for n in N if n.type == "OUTPUT_MATERIAL")
            bsdf = next(n for n in N if n.type == "BSDF_PRINCIPLED")
            old_em = N.get("AAABakeEmit")
            if old_em: N.remove(old_em)
            if mode_key == "BSDF":
                L.new(bsdf.outputs[0], out.inputs[0])
            else:
                em = N.new("ShaderNodeEmission"); em.name = "AAABakeEmit"
                sock = bsdf.inputs[mode_key]
                if sock.is_linked:
                    L.new(sock.links[0].from_socket, em.inputs[0])
                else:
                    v = sock.default_value
                    em.inputs[0].default_value = (v, v, v, 1.0) if isinstance(v, (int, float)) else v
                L.new(em.outputs[0], out.inputs[0])

    im_a = bpy.data.images.new(out_prefix + "_a", size, size); im_a.colorspace_settings.name = "sRGB"
    attach_target_img(im_a); set_emit_source("Base Color")
    scene.cycles.samples = 4
    bpy.ops.object.bake(type="EMIT")
    im_a.filepath_raw = out_prefix + "_a.png"; im_a.file_format = "PNG"; im_a.save()

    im_met = bpy.data.images.new(out_prefix + "_tmp_met", size, size); im_met.colorspace_settings.name = "Non-Color"
    attach_target_img(im_met); set_emit_source("Metallic")
    bpy.ops.object.bake(type="EMIT")
    met_arr = np.array(im_met.pixels[:], dtype=np.float32).reshape(size, size, 4)[..., 0]

    im_rgh = bpy.data.images.new(out_prefix + "_tmp_rgh", size, size); im_rgh.colorspace_settings.name = "Non-Color"
    attach_target_img(im_rgh); set_emit_source("BSDF")
    bpy.ops.object.bake(type="ROUGHNESS")
    rgh_arr = np.array(im_rgh.pixels[:], dtype=np.float32).reshape(size, size, 4)[..., 0]

    mg_rgba = np.zeros((size, size, 4), dtype=np.float32)
    mg_rgba[..., 0] = met_arr; mg_rgba[..., 1] = met_arr; mg_rgba[..., 2] = met_arr
    mg_rgba[..., 3] = np.clip(1.0 - rgh_arr, 0.02, 0.98)
    save_rgba_np(out_prefix + "_m.png", mg_rgba, "Non-Color")

    im_n = bpy.data.images.new(out_prefix + "_n", size, size); im_n.colorspace_settings.name = "Non-Color"
    attach_target_img(im_n); set_emit_source("BSDF")
    scene.cycles.samples = 8
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")
    im_n.filepath_raw = out_prefix + "_n.png"; im_n.file_format = "PNG"; im_n.save()

    im_ao = bpy.data.images.new(out_prefix + "_ao", size, size); im_ao.colorspace_settings.name = "Non-Color"
    attach_target_img(im_ao)
    scene.cycles.samples = 16
    bpy.ops.object.bake(type="AO")
    im_ao.filepath_raw = out_prefix + "_ao.png"; im_ao.file_format = "PNG"; im_ao.save()

    for im in (im_a, im_met, im_rgh, im_n, im_ao):
        bpy.data.images.remove(im)
    print("BAKED_ATLAS_SET", out_prefix)

def setup_material_textures(who):
    tex_dir = ASSETS + f"Characters/{'ValeProps' if who == 'vale' else who.capitalize()}/Textures/"
    if who == "raven":
        make_cloth_twill(tex_dir + "aaa_coat.png",    (0.16, 0.15, 0.145), (0.09, 0.085, 0.082))
        make_cloth_twill(tex_dir + "aaa_scarf.png",   (0.30, 0.09, 0.09),  (0.16, 0.045, 0.045))
        make_cloth_twill(tex_dir + "aaa_pants.png",   (0.15, 0.14, 0.13),  (0.085, 0.08, 0.075))
        make_pebbled_leather(tex_dir + "aaa_leather.png", (0.28, 0.15, 0.09), (0.14, 0.075, 0.045))
        make_brushed_metal(tex_dir + "aaa_armor.png", (0.34, 0.11, 0.10),  (0.16, 0.05, 0.045))
        make_brushed_metal(tex_dir + "aaa_brass.png", (0.50, 0.37, 0.21),  (0.22, 0.16, 0.09))
        make_brushed_metal(tex_dir + "aaa_claw.png",  (0.64, 0.62, 0.59),  (0.28, 0.27, 0.26))
    elif who == "vex":
        make_cloth_twill(tex_dir + "aaa_coat.png",    (0.16, 0.21, 0.28),  (0.09, 0.12, 0.17))
        make_cloth_twill(tex_dir + "aaa_scarf.png",   (0.24, 0.29, 0.34),  (0.16, 0.18, 0.21), (0.56, 0.49, 0.39))
        make_cloth_twill(tex_dir + "aaa_pants.png",   (0.15, 0.16, 0.18),  (0.09, 0.095, 0.11))
        make_pebbled_leather(tex_dir + "aaa_leather.png", (0.29, 0.16, 0.10), (0.15, 0.08, 0.05))
        make_brushed_metal(tex_dir + "aaa_armor.png", (0.21, 0.27, 0.36),  (0.10, 0.13, 0.18))
        make_brushed_metal(tex_dir + "aaa_brass.png", (0.52, 0.40, 0.22),  (0.24, 0.18, 0.10))
        make_cloth_twill(tex_dir + "aaa_hair.png",    (0.17, 0.13, 0.10),  (0.07, 0.05, 0.04), (0.30, 0.24, 0.18))
    else:
        make_cloth_twill(tex_dir + "aaa_coat.png",    (0.21, 0.16, 0.13),  (0.11, 0.085, 0.068))
        make_cloth_twill(tex_dir + "aaa_scarf.png",   (0.31, 0.095, 0.09), (0.16, 0.048, 0.045))
        make_pebbled_leather(tex_dir + "aaa_leather.png", (0.28, 0.16, 0.10), (0.14, 0.08, 0.05))
        make_brushed_metal(tex_dir + "aaa_bronze.png", (0.52, 0.38, 0.21), (0.24, 0.17, 0.09))

def upgrade_character_fbx(who):
    cap = who.capitalize()
    fbx_path = ASSETS + f"Characters/{cap}/{cap}.fbx"
    tex_dir = ASSETS + f"Characters/{cap}/Textures/"
    clean_boss_skin_texture(who)
    setup_material_textures(who)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    boss = next(o for o in bpy.data.objects if o.type == "MESH")
    sel(boss)

    head_mats = {f"{cap}_{s}" for s in ("Emblem", "Lens", "Canister", "Vent", "Hair")
                 if f"{cap}_{s}" in [m.name for m in boss.data.materials if m]}
    outfit_mats = {m.name for m in boss.data.materials if m and m.name not in head_mats and not m.name.endswith("_Skin")}

    for m in boss.data.materials:
        if not m: continue
        suf = m.name.split("_", 1)[1] if "_" in m.name else m.name
        if suf == "Skin":
            build_pbr_node_tree(m, tex_dir + f"{who}_body.png", obj_scale=1.0, rough=0.72, metal=0.0, bump_str=0.20, preserve_uv_map=True, micro_grain=0.06)
        elif suf == "Coat":
            edge = lin(0.25, 0.23, 0.22) if who == "raven" else lin(0.24, 0.30, 0.38)
            build_pbr_node_tree(m, tex_dir + "aaa_coat.png", obj_scale=14.0, rough=0.90, metal=0.0, edge_lin=edge, edge_rough=0.78, dust_str=0.35, bump_str=0.55, micro_grain=0.16)
        elif suf == "Scarf":
            edge = lin(0.42, 0.14, 0.14) if who == "raven" else lin(0.55, 0.49, 0.39)
            build_pbr_node_tree(m, tex_dir + "aaa_scarf.png", obj_scale=15.0, rough=0.92, metal=0.0, edge_lin=edge, edge_rough=0.80, bump_str=0.55, micro_grain=0.16)
        elif suf == "Pants":
            build_pbr_node_tree(m, tex_dir + "aaa_pants.png", obj_scale=14.0, rough=0.88, metal=0.0, edge_lin=lin(0.22, 0.20, 0.19), edge_rough=0.76, dust_str=0.45, bump_str=0.50, micro_grain=0.14)
        elif suf == "Leather":
            build_pbr_node_tree(m, tex_dir + "aaa_leather.png", obj_scale=14.0, rough=0.54, metal=0.04, edge_lin=lin(0.44, 0.28, 0.17), edge_rough=0.36, dust_str=0.22, bump_str=0.55, micro_grain=0.14)
        elif suf == "Brass":
            build_pbr_node_tree(m, tex_dir + "aaa_brass.png", obj_scale=12.0, rough=0.34, metal=0.88, edge_lin=lin(0.74, 0.60, 0.38), edge_rough=0.18, edge_metal=0.95, bump_str=0.42, micro_grain=0.10)
        elif suf == "Armor":
            edge = lin(0.58, 0.56, 0.53)
            build_pbr_node_tree(m, tex_dir + "aaa_armor.png", obj_scale=12.0, rough=0.42, metal=0.60, edge_lin=edge, edge_rough=0.22, edge_metal=0.92, bump_str=0.48, micro_grain=0.12)
        elif suf == "Claw":
            build_pbr_node_tree(m, tex_dir + "aaa_claw.png", obj_scale=10.0, rough=0.25, metal=0.92, edge_lin=lin(0.86, 0.87, 0.88), edge_rough=0.14, edge_metal=0.98, bump_str=0.34, micro_grain=0.08)
        elif suf == "Bone":
            build_pbr_node_tree(m, tex_dir + "vex_bone.png", obj_scale=10.0, rough=0.46, metal=0.02, edge_lin=lin(0.85, 0.80, 0.66), edge_rough=0.32, bump_str=0.35, micro_grain=0.10)
        elif suf == "Emblem":
            emb_img = f"{who}_slashes.png" if who == "raven" else "vex_emblem.png"
            build_pbr_node_tree(m, tex_dir + emb_img, obj_scale=1.0, rough=0.40, metal=0.55, edge_lin=lin(0.60, 0.58, 0.56), edge_rough=0.20, edge_metal=0.92, bump_str=0.42, preserve_uv_map=True, micro_grain=0.10)
        elif suf == "Lens":
            # Crisp vibrant emerald goggle tint visible even under dark helmet brim!
            build_pbr_node_tree(m, tex_dir + f"{who}_lens.png", obj_scale=1.0, rough=0.05, metal=0.15, bump_str=0.05, preserve_uv_map=True, tint_lin=lin(1.15, 1.55, 0.90), micro_grain=0.0)
        elif suf == "Canister":
            build_pbr_node_tree(m, tex_dir + f"{who}_canister_cap.png", obj_scale=1.0, rough=0.36, metal=0.84, edge_lin=lin(0.66, 0.54, 0.34), edge_rough=0.22, edge_metal=0.94, bump_str=0.42, preserve_uv_map=True, micro_grain=0.08)
        elif suf == "Vent":
            build_pbr_node_tree(m, tex_dir + f"{who}_vent.png", obj_scale=1.0, rough=0.40, metal=0.75, bump_str=0.42, preserve_uv_map=True, micro_grain=0.08)
        elif suf == "Medallion":
            build_pbr_node_tree(m, tex_dir + "vex_medallion.png", obj_scale=1.0, rough=0.32, metal=0.86, edge_lin=lin(0.76, 0.65, 0.42), edge_rough=0.18, edge_metal=0.95, bump_str=0.40, preserve_uv_map=True, micro_grain=0.08)
        elif suf == "Hair":
            build_pbr_node_tree(m, tex_dir + ("aaa_hair.png" if who == "vex" else "iona_hair.png"), obj_scale=8.0, rough=0.86, metal=0.0, bump_str=0.48, micro_grain=0.14)

    bake_atlas_group(boss, head_mats, "UV_Head", tex_dir + f"{who}_head_pbr", size=2048)
    bake_atlas_group(boss, outfit_mats, "UV_Outfit", tex_dir + f"{who}_outfit_pbr", size=2048)

    # Skin AO on UVMap
    skin_im = bpy.data.images.new(f"{who}_skin_ao", 1024, 1024); skin_im.colorspace_settings.name = "Non-Color"
    boss.data.uv_layers.active = boss.data.uv_layers["UVMap"]
    for m in boss.data.materials:
        if not m or not m.use_nodes: continue
        N = m.node_tree.nodes
        old = N.get("AAABakeTarget")
        if old: N.remove(old)
        if m.name.endswith("_Skin"):
            t = N.new("ShaderNodeTexImage"); t.name = "AAABakeTarget"; t.image = skin_im; N.active = t
        else:
            N.active = None
    bpy.context.scene.cycles.samples = 12
    bpy.ops.object.bake(type="AO")
    skin_im.filepath_raw = tex_dir + f"{who}_skin_ao.png"; skin_im.file_format = "PNG"; skin_im.save()
    bpy.data.images.remove(skin_im)

    # Consolidate UVs into UVMap and assign {Cap}_Skin, {Cap}_HeadPBR, {Cap}_OutfitPBR
    me = boss.data
    uv_main = me.uv_layers["UVMap"]
    uv_head = me.uv_layers["UV_Head"]
    uv_out = me.uv_layers["UV_Outfit"]
    head_idx_set = {i for i, m in enumerate(me.materials) if m and m.name in head_mats}
    outfit_idx_set = {i for i, m in enumerate(me.materials) if m and m.name in outfit_mats}

    for p in me.polygons:
        if p.material_index in head_idx_set:
            for li in p.loop_indices: uv_main.data[li].uv = uv_head.data[li].uv
        elif p.material_index in outfit_idx_set:
            for li in p.loop_indices: uv_main.data[li].uv = uv_out.data[li].uv
    me.uv_layers.remove(uv_head); me.uv_layers.remove(uv_out)
    me.uv_layers.active = uv_main

    def simple_tex_mat(mname, a_path):
        m = bpy.data.materials.new(mname); m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        t = m.node_tree.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(a_path)
        m.node_tree.links.new(t.outputs[0], b.inputs["Base Color"])
        return m

    m_skin = simple_tex_mat(f"{cap}_Skin", tex_dir + f"{who}_body.png")
    m_head = simple_tex_mat(f"{cap}_HeadPBR", tex_dir + f"{who}_head_pbr_a.png")
    m_outfit = simple_tex_mat(f"{cap}_OutfitPBR", tex_dir + f"{who}_outfit_pbr_a.png")

    poly_new_mat = [1 if p.material_index in head_idx_set else (2 if p.material_index in outfit_idx_set else 0) for p in me.polygons]
    me.materials.clear()
    me.materials.append(m_skin); me.materials.append(m_head); me.materials.append(m_outfit)
    for p, nm in zip(me.polygons, poly_new_mat): p.material_index = nm

    bpy.ops.object.select_all(action="DESELECT"); boss.select_set(True); rig.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE",
        use_armature_deform_only=True
    )
    print(f"AAA_UPGRADE_{who.upper()}_DONE")

def upgrade_vale_props_fbx():
    fbx_path = ASSETS + "Characters/ValeProps/ValeProps.fbx"
    tex_dir = ASSETS + "Characters/ValeProps/Textures/"
    setup_material_textures("vale")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    chest = bpy.data.objects.get("Vale_ChestProps")
    if chest:
        delta = chest.matrix_world.inverted().to_3x3() @ Vector((0.0, -0.014, 0.0))
        for v in chest.data.vertices:
            v.co += delta  # Bake +1.4cm forward clearance directly into mesh vertices since AttachSocket sets localPosition=0

    for o in meshes:
        for m in o.data.materials:
            if not m: continue
            suf = m.name.split("_", 1)[1] if "_" in m.name else m.name
            if suf == "Coat":
                build_pbr_node_tree(m, tex_dir + "aaa_coat.png", obj_scale=14.0, rough=0.88, metal=0.0, edge_lin=lin(0.34, 0.25, 0.19), edge_rough=0.72, bump_str=0.55, micro_grain=0.16)
            elif suf == "Scarf":
                build_pbr_node_tree(m, tex_dir + "aaa_scarf.png", obj_scale=14.0, rough=0.90, metal=0.0, edge_lin=lin(0.42, 0.14, 0.14), edge_rough=0.76, bump_str=0.55, micro_grain=0.16)
            elif suf == "Leather":
                build_pbr_node_tree(m, tex_dir + "aaa_leather.png", obj_scale=14.0, rough=0.50, metal=0.04, edge_lin=lin(0.44, 0.29, 0.18), edge_rough=0.34, bump_str=0.55, micro_grain=0.14)
            elif suf == "Bronze":
                build_pbr_node_tree(m, tex_dir + "aaa_bronze.png", obj_scale=12.0, rough=0.32, metal=0.88, edge_lin=lin(0.76, 0.62, 0.38), edge_rough=0.18, edge_metal=0.95, bump_str=0.45, micro_grain=0.10)
            elif suf == "Scar":
                build_pbr_node_tree(m, tex_dir + "vale_scar.png", obj_scale=1.0, rough=0.65, metal=0.0, bump_str=0.32, preserve_uv_map=True, micro_grain=0.06)
            elif suf == "Canister":
                build_pbr_node_tree(m, tex_dir + "vale_canister.png", obj_scale=1.0, rough=0.35, metal=0.84, edge_lin=lin(0.68, 0.54, 0.32), edge_rough=0.20, edge_metal=0.94, bump_str=0.42, preserve_uv_map=True, micro_grain=0.08)
            elif suf == "Vent":
                build_pbr_node_tree(m, tex_dir + "vale_vent.png", obj_scale=1.0, rough=0.40, metal=0.76, bump_str=0.42, preserve_uv_map=True, micro_grain=0.08)

    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    for o in meshes:
        me = o.data
        if "UV_Atlas" not in me.uv_layers: me.uv_layers.new(name="UV_Atlas")
        me.uv_layers.active = me.uv_layers["UV_Atlas"]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.015, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"; scene.cycles.device = "CPU"; scene.render.bake.margin = 12
    all_mats = {m for o in meshes for m in o.data.materials if m and m.use_nodes}
    prefix = tex_dir + "vale_props_pbr"

    def attach_all(im):
        for m in all_mats:
            N = m.node_tree.nodes
            old = N.get("AAABakeTarget")
            if old: N.remove(old)
            t = N.new("ShaderNodeTexImage"); t.name = "AAABakeTarget"; t.image = im; N.active = t

    def emit_all(mode_key):
        for m in all_mats:
            N = m.node_tree.nodes; L = m.node_tree.links
            out = next(n for n in N if n.type == "OUTPUT_MATERIAL")
            bsdf = next(n for n in N if n.type == "BSDF_PRINCIPLED")
            old_em = N.get("AAABakeEmit")
            if old_em: N.remove(old_em)
            if mode_key == "BSDF":
                L.new(bsdf.outputs[0], out.inputs[0])
            else:
                em = N.new("ShaderNodeEmission"); em.name = "AAABakeEmit"
                sock = bsdf.inputs[mode_key]
                if sock.is_linked: L.new(sock.links[0].from_socket, em.inputs[0])
                else:
                    v = sock.default_value
                    em.inputs[0].default_value = (v, v, v, 1.0) if isinstance(v, (int, float)) else v
                L.new(em.outputs[0], out.inputs[0])

    sz = 2048
    im_a = bpy.data.images.new("vp_a", sz, sz); im_a.colorspace_settings.name = "sRGB"
    attach_all(im_a); emit_all("Base Color"); scene.cycles.samples = 4
    bpy.ops.object.bake(type="EMIT"); im_a.filepath_raw = prefix + "_a.png"; im_a.file_format = "PNG"; im_a.save()

    im_m = bpy.data.images.new("vp_m", sz, sz); im_m.colorspace_settings.name = "Non-Color"
    attach_all(im_m); emit_all("Metallic"); bpy.ops.object.bake(type="EMIT")
    met = np.array(im_m.pixels[:], np.float32).reshape(sz, sz, 4)[..., 0]

    im_r = bpy.data.images.new("vp_r", sz, sz); im_r.colorspace_settings.name = "Non-Color"
    attach_all(im_r); emit_all("BSDF"); bpy.ops.object.bake(type="ROUGHNESS")
    rgh = np.array(im_r.pixels[:], np.float32).reshape(sz, sz, 4)[..., 0]
    mg = np.zeros((sz, sz, 4), np.float32)
    mg[..., 0] = met; mg[..., 1] = met; mg[..., 2] = met; mg[..., 3] = np.clip(1.0 - rgh, 0.02, 0.98)
    save_rgba_np(prefix + "_m.png", mg, "Non-Color")

    im_n = bpy.data.images.new("vp_n", sz, sz); im_n.colorspace_settings.name = "Non-Color"
    attach_all(im_n); emit_all("BSDF"); scene.cycles.samples = 8
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT"); im_n.filepath_raw = prefix + "_n.png"; im_n.file_format = "PNG"; im_n.save()

    im_ao = bpy.data.images.new("vp_ao", sz, sz); im_ao.colorspace_settings.name = "Non-Color"
    attach_all(im_ao); scene.cycles.samples = 16
    bpy.ops.object.bake(type="AO"); im_ao.filepath_raw = prefix + "_ao.png"; im_ao.file_format = "PNG"; im_ao.save()

    for im in (im_a, im_m, im_r, im_n, im_ao): bpy.data.images.remove(im)

    shared_mat = bpy.data.materials.new("ValeProps_AtlasPBR"); shared_mat.use_nodes = True
    t = shared_mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(prefix + "_a.png")
    shared_mat.node_tree.links.new(t.outputs[0], shared_mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])

    for o in meshes:
        me = o.data
        uv_m = me.uv_layers["UVMap"]; uv_at = me.uv_layers["UV_Atlas"]
        for i in range(len(uv_m.data)): uv_m.data[i].uv = uv_at.data[i].uv
        me.uv_layers.remove(uv_at); me.uv_layers.active = uv_m
        me.materials.clear(); me.materials.append(shared_mat)
        for p in me.polygons: p.material_index = 0

    # CRITICAL: Export with bake_space_transform=False so imported (1.571, 0, 0) Unity socket orientation is NOT double-rotated!
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False, path_mode="RELATIVE", mesh_smooth_type="FACE"
    )
    print("AAA_UPGRADE_VALEPROPS_DONE")

upgrade_vale_veteran_face()
upgrade_character_fbx("raven")
upgrade_character_fbx("vex")
upgrade_vale_props_fbx()

# Remove intermediate aaa_*.png generator inputs from Assets/Resources so iOS bundles stay lean
for sub in ("Raven", "Vex", "ValeProps"):
    td = ASSETS + f"Characters/{sub}/Textures/"
    for fn in os.listdir(td):
        if fn.startswith("aaa_"):
            try: os.remove(td + fn)
            except OSError: pass

print("ALL_AAA_TEXTURES_UPGRADED")
