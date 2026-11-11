# Two-stage 4-view Orthographic Projection + 4-Map PBR Texture Baker for Raven, Vex & Malcolm Vale (Skin_1).
# Matches the player equipment GeneratedWarden/Ironwake pipeline:
#   Stage 1 (--blender-bake): Runs inside Blender to unwrap head/outfit UVs, export Raven.fbx & Vex.fbx with
#     consolidated materials (Skin, HeadPBR, OutfitPBR), and bake Cycles Pos / WorldNormal / BoneMask / AO / BevelNormal to .npz.
#   Stage 2 (--project): Runs inside system Python (PIL + numpy + scipy) to project the 4-view painted illustrations
#     (Front / Back / Right / Left) with arm-angle rotation, cape/leg de-occlusion, Sobel height-to-normal relief,
#     calibrated MetallicGlossMap, and AO into crisp 2048px PNG textures.
import sys, os, math
import numpy as np

ASSETS = "D:/Unity/Ashfall-iOS/Assets/Resources/"
GUIDES = "D:/Unity/AssetIntegration/FactionBossesHQ/ortho_guides/"
BUFS = "D:/Unity/AssetIntegration/FactionBossesHQ/ortho_guides/uv_bufs/"
SKIN1_FBX = "D:/Unity/Ashfall-iOS/Assets/Croc men/Base mesh/Skin_1.fbx"
SKIN1_TEX = "D:/Unity/Ashfall-iOS/Assets/Croc men/Texture/Skin_1/"

# ==============================================================================
# STAGE 1: BLENDER BUFFER BAKER & FBX CONSOLIDATOR
# ==============================================================================
def run_blender_bake():
    import bpy
    from mathutils import Vector
    os.makedirs(BUFS, exist_ok=True)

    def sel(o):
        if bpy.context.object and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True); bpy.context.view_layer.objects.active = o

    def build_geom_nodes(mat, pos_lo, pos_span):
        mat.use_nodes = True
        N = mat.node_tree.nodes; L = mat.node_tree.links
        N.clear()
        out = N.new("ShaderNodeOutputMaterial")
        em = N.new("ShaderNodeEmission"); em.name = "Em"
        L.new(em.outputs[0], out.inputs[0])

        geom = N.new("ShaderNodeNewGeometry")
        vsub = N.new("ShaderNodeVectorMath"); vsub.operation = "SUBTRACT"; vsub.inputs[1].default_value = pos_lo
        vdiv = N.new("ShaderNodeVectorMath"); vdiv.operation = "SCALE"; vdiv.inputs["Scale"].default_value = 1.0 / pos_span
        L.new(geom.outputs["Position"], vsub.inputs[0]); L.new(vsub.outputs["Vector"], vdiv.inputs[0])

        nmul = N.new("ShaderNodeVectorMath"); nmul.operation = "MULTIPLY_ADD"
        nmul.inputs[1].default_value = (0.5, 0.5, 0.5); nmul.inputs[2].default_value = (0.5, 0.5, 0.5)
        L.new(geom.outputs["Normal"], nmul.inputs[0])

        attr = N.new("ShaderNodeAttribute"); attr.attribute_name = "ProjMask"
        L.new(vdiv.outputs["Vector"], em.inputs[0])
        return dict(em=em, pos=vdiv.outputs["Vector"], nrm=nmul.outputs["Vector"], mask=attr.outputs["Color"], L=L)

    def tag_proj_mask(obj, cape_mats=(), pants_mats=()):
        me = obj.data
        col = me.color_attributes.get("ProjMask")
        if col: me.color_attributes.remove(col)
        col = me.color_attributes.new(name="ProjMask", type="BYTE_COLOR", domain="CORNER")

        vg_names = [g.name.lower() for g in obj.vertex_groups]
        arm_l_ids = {i for i, n in enumerate(vg_names) if any(t in n for t in ("arm","hand","thumb","index","middle","ring","pinky")) and ("_l" in n or "left" in n)}
        arm_r_ids = {i for i, n in enumerate(vg_names) if any(t in n for t in ("arm","hand","thumb","index","middle","ring","pinky")) and ("_r" in n or "right" in n)}

        v_al = np.zeros(len(me.vertices), dtype=np.float32)
        v_ar = np.zeros(len(me.vertices), dtype=np.float32)
        mw = obj.matrix_world
        for vi, v in enumerate(me.vertices):
            wl = sum(g.weight for g in v.groups if g.group in arm_l_ids)
            wr = sum(g.weight for g in v.groups if g.group in arm_r_ids)
            wco = mw @ v.co
            outboard = float(np.clip((abs(wco.x) - 0.19) / 0.08, 0.0, 1.0))
            v_al[vi] = min(1.0, wl * outboard)
            v_ar[vi] = min(1.0, wr * outboard)

        cape_set = {i for i, m in enumerate(me.materials) if m and m.name in cape_mats}
        pants_set = {i for i, m in enumerate(me.materials) if m and m.name in pants_mats}
        for p in me.polygons:
            center_w = mw @ p.center
            is_cape = 1.0 if (p.material_index in cape_set and center_w.y > 0.05 and center_w.z < 1.55) else (0.5 if p.material_index in pants_set else 0.0)
            for li in p.loop_indices:
                vi = me.loops[li].vertex_index
                col.data[li].color = (float(v_al[vi]), float(v_ar[vi]), is_cape, 1.0)

    def bake_group_to_npz(obj, mat_names, uv_name, out_npz, size, pos_lo, pos_span, extra_meta):
        sel(obj)
        me = obj.data
        target_idx = {i for i, m in enumerate(me.materials) if m and m.name in mat_names}
        if uv_name != "KEEP":
            if uv_name not in me.uv_layers: me.uv_layers.new(name=uv_name)
            me.uv_layers.active = me.uv_layers[uv_name]
            bpy.ops.object.mode_set(mode="EDIT")
            bpy.ops.mesh.select_mode(type="FACE"); bpy.ops.mesh.select_all(action="DESELECT")
            bpy.ops.object.mode_set(mode="OBJECT")
            for p in me.polygons: p.select = (p.material_index in target_idx)
            bpy.ops.object.mode_set(mode="EDIT")
            bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.012, scale_to_bounds=True)
            bpy.ops.object.mode_set(mode="OBJECT")

        # Clear non-target material trees cleanly so Cycles never complains about deleted legacy texture paths!
        for i, m in enumerate(me.materials):
            if m and i not in target_idx:
                m.use_nodes = True; m.node_tree.nodes.clear()
                o_n = m.node_tree.nodes.new("ShaderNodeOutputMaterial")
                b_n = m.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
                m.node_tree.links.new(b_n.outputs[0], o_n.inputs[0])
        info = {i: build_geom_nodes(m, pos_lo, pos_span) for i, m in enumerate(me.materials) if i in target_idx and m}
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"; scene.cycles.device = "CPU"
        scene.render.bake.margin = 16; scene.render.bake.use_selected_to_active = False

        def bake_pass(key, is_ao=False):
            im = bpy.data.images.new(f"b_{key}", size, size, float_buffer=True, alpha=True)
            im.colorspace_settings.name = "Non-Color"
            for i, m in enumerate(me.materials):
                if not m or not m.use_nodes: continue
                N = m.node_tree.nodes
                old = N.get("BTgt");
                if old: N.remove(old)
                if i in target_idx:
                    t = N.new("ShaderNodeTexImage"); t.name = "BTgt"; t.image = im; N.active = t
                    if not is_ao: info[i]["L"].new(info[i][key], info[i]["em"].inputs[0])
                else:
                    N.active = None
            scene.cycles.samples = 16 if is_ao else 1
            bpy.ops.object.bake(type="AO" if is_ao else "EMIT")
            arr = np.array(im.pixels[:], dtype=np.float32).reshape(size, size, 4)
            bpy.data.images.remove(im)
            return arr

        buf_pos = bake_pass("pos")
        buf_nrm = bake_pass("nrm")
        buf_msk = bake_pass("mask")
        buf_ao  = bake_pass("ao", is_ao=True)[..., 0]

        im_tn = bpy.data.images.new("b_tn", size, size, alpha=True); im_tn.colorspace_settings.name = "Non-Color"
        for i, m in enumerate(me.materials):
            if not m or not m.use_nodes: continue
            N = m.node_tree.nodes; L = m.node_tree.links
            if i in target_idx:
                out = next(n for n in N if n.type == "OUTPUT_MATERIAL")
                bsdf = N.new("ShaderNodeBsdfPrincipled")
                bev = N.new("ShaderNodeBevel"); bev.samples = 4; bev.inputs["Radius"].default_value = 0.0035
                L.new(bev.outputs[0], bsdf.inputs["Normal"]); L.new(bsdf.outputs[0], out.inputs[0])
                t = N.get("BTgt"); t.image = im_tn; N.active = t
            else:
                N.active = None
        scene.cycles.samples = 4
        bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")
        buf_tn = np.array(im_tn.pixels[:], dtype=np.float32).reshape(size, size, 4)[..., :3]
        bpy.data.images.remove(im_tn)

        np.savez_compressed(out_npz, pos=buf_pos[...,:3], nrm=buf_nrm[...,:3], msk=buf_msk, ao=buf_ao, tn=buf_tn, **extra_meta)
        print("BAKED_NPZ", out_npz)

    for who in ("raven", "vex"):
        cap = who.capitalize()
        fbx_path = ASSETS + f"Characters/{cap}/{cap}.fbx"
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
        boss = next(o for o in bpy.data.objects if o.type == "MESH")
        sel(boss); bpy.context.view_layer.update()

        mw = boss.matrix_world
        pts = [mw @ v.co for v in boss.data.vertices]
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        pos_lo = (lo.x - 0.1, lo.y - 0.1, lo.z - 0.1)
        pos_span = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) + 0.2

        sh_b = rig.pose.bones["upperarm_l"]; ha_b = rig.pose.bones["hand_l"]
        p_sh = rig.matrix_world @ sh_b.head; p_ha = rig.matrix_world @ ha_b.head
        mesh_arm_deg = math.degrees(math.atan2(-(p_ha.z - p_sh.z), p_ha.x - p_sh.x))
        meta = dict(z_lo=np.float32(lo.z), z_hi=np.float32(hi.z), pos_lo=np.array(pos_lo, np.float32),
                    pos_span=np.float32(pos_span), sh_x=np.float32(p_sh.x), sh_z=np.float32(p_sh.z),
                    mesh_arm_deg=np.float32(mesh_arm_deg))

        tag_proj_mask(boss, cape_mats={f"{cap}_Coat", f"{cap}_Scarf"}, pants_mats={f"{cap}_Pants"})
        head_mats = {f"{cap}_{s}" for s in ("Emblem", "Lens", "Canister", "Vent", "Hair") if f"{cap}_{s}" in [m.name for m in boss.data.materials if m]}
        outfit_mats = {m.name for m in boss.data.materials if m and m.name not in head_mats and not m.name.endswith("_Skin")}
        skin_mats = {f"{cap}_Skin"}

        bake_group_to_npz(boss, head_mats,   "UV_Head",   BUFS + f"{who}_head.npz",   2048, pos_lo, pos_span, meta)
        bake_group_to_npz(boss, outfit_mats, "UV_Outfit", BUFS + f"{who}_outfit.npz", 2048, pos_lo, pos_span, meta)
        bake_group_to_npz(boss, skin_mats,   "KEEP",      BUFS + f"{who}_skin.npz",   1024, pos_lo, pos_span, meta)

        # Consolidate UVs into UVMap and assign {Cap}_Skin, {Cap}_HeadPBR, {Cap}_OutfitPBR cleanly!
        me = boss.data
        uv_main = me.uv_layers["UVMap"]; uv_head = me.uv_layers["UV_Head"]; uv_out = me.uv_layers["UV_Outfit"]
        head_idx_set = {i for i, m in enumerate(me.materials) if m and m.name in head_mats}
        outfit_idx_set = {i for i, m in enumerate(me.materials) if m and m.name in outfit_mats}
        for p in me.polygons:
            if p.material_index in head_idx_set:
                for li in p.loop_indices: uv_main.data[li].uv = uv_head.data[li].uv
            elif p.material_index in outfit_idx_set:
                for li in p.loop_indices: uv_main.data[li].uv = uv_out.data[li].uv
        me.uv_layers.remove(uv_head); me.uv_layers.remove(uv_out); me.uv_layers.active = uv_main

        tex_dir = ASSETS + f"Characters/{cap}/Textures/"
        def simple_mat(mname, tex_path):
            old = bpy.data.materials.get(mname)
            if old: bpy.data.materials.remove(old)
            m = bpy.data.materials.new(mname); m.use_nodes = True
            if os.path.exists(tex_path):
                t = m.node_tree.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(tex_path)
                m.node_tree.links.new(t.outputs[0], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
            return m
        poly_new = [1 if p.material_index in head_idx_set else (2 if p.material_index in outfit_idx_set else 0) for p in me.polygons]
        me.materials.clear()
        for m in list(bpy.data.materials):
            if m.users == 0: bpy.data.materials.remove(m)
        m_skin = simple_mat(f"{cap}_Skin", tex_dir + f"{who}_body.png")
        m_head = simple_mat(f"{cap}_HeadPBR", tex_dir + f"{who}_head_pbr_a.png")
        m_outfit = simple_mat(f"{cap}_OutfitPBR", tex_dir + f"{who}_outfit_pbr_a.png")
        me.materials.append(m_skin); me.materials.append(m_head); me.materials.append(m_outfit)
        for p, nm in zip(me.polygons, poly_new): p.material_index = nm

        bpy.ops.object.select_all(action="DESELECT"); boss.select_set(True); rig.select_set(True)
        bpy.ops.export_scene.fbx(
            filepath=fbx_path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z", axis_up="Y", bake_space_transform=False,
            add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE",
            use_armature_deform_only=True
        )
        print("CONSOLIDATED_FBX", fbx_path)

    # Vale Skin_1 (RaiderBossNPC / Enemy_11): bake geometry buffers directly off original untouched UVs!
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SKIN1_FBX)
    rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    # Remove sunglasses mesh BEFORE Cycles AO bake so it never casts pitch-black raccoon shadows across Vale's eyes!
    for glass_obj in [o for o in bpy.data.objects if o.type == "MESH" and ("45" in o.name or any(m and "glass" in m.name.lower() for m in o.data.materials))]:
        bpy.data.objects.remove(glass_obj, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object; bpy.context.view_layer.update()

    mw = joined.matrix_world
    pts = [mw @ v.co for v in joined.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    pos_lo = (lo.x - 0.1, lo.y - 0.1, lo.z - 0.1)
    pos_span = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) + 0.2
    sh_b = rig.pose.bones["upperarm_l"]; ha_b = rig.pose.bones["hand_l"]
    p_sh = rig.matrix_world @ sh_b.head; p_ha = rig.matrix_world @ ha_b.head
    mesh_arm_deg = math.degrees(math.atan2(-(p_ha.z - p_sh.z), p_ha.x - p_sh.x))
    meta = dict(z_lo=np.float32(lo.z), z_hi=np.float32(hi.z), pos_lo=np.array(pos_lo, np.float32),
                pos_span=np.float32(pos_span), sh_x=np.float32(p_sh.x), sh_z=np.float32(p_sh.z),
                mesh_arm_deg=np.float32(mesh_arm_deg))
    tag_proj_mask(joined)

    bake_group_to_npz(joined, {"Mat_croc_men_Body"},      "KEEP", BUFS + "vale_body.npz",      2048, pos_lo, pos_span, meta)
    bake_group_to_npz(joined, {"Mat_croc_men_waistcoat"}, "KEEP", BUFS + "vale_waistcoat.npz", 2048, pos_lo, pos_span, meta)
    bake_group_to_npz(joined, {"Mat_croc_men_pants"},     "KEEP", BUFS + "vale_pants.npz",     2048, pos_lo, pos_span, meta)
    print("BLENDER_BAKE_STAGE_DONE")

# ==============================================================================
# STAGE 2: PYTHON MULTI-VIEW PROJECTION + SOBEL NORMAL + METALLIC/GLOSS GENERATOR
# ==============================================================================
def run_projection():
    from PIL import Image
    from scipy.ndimage import distance_transform_edt, map_coordinates, binary_fill_holes, binary_erosion, gaussian_filter

    def load_panels(sheet_path):
        im = np.asarray(Image.open(sheet_path).convert("RGB"), dtype=np.uint8)
        H, W = im.shape[:2]
        fg_all = np.max(255 - im, axis=2) > 22
        col_has = np.sum(fg_all, axis=0) > 6
        spans = []; in_s = False; s0 = 0
        for x, v in enumerate(col_has):
            if v and not in_s: in_s = True; s0 = x
            elif not v and in_s:
                if x - s0 > 28: spans.append((s0, x))
                in_s = False
        if in_s and W - s0 > 28: spans.append((s0, W))
        assert len(spans) == 4, f"Expected 4 views in {sheet_path}, got {spans}"

        yt = min(int(np.where(fg_all[:, x0:x1])[0].min()) for x0, x1 in spans)
        yb = max(int(np.where(fg_all[:, x0:x1])[0].max()) for x0, x1 in spans)

        panels = {}
        for name, (x0, x1) in zip(("front", "back", "right", "left"), spans):
            crop = im[yt:yb+1, x0:x1].copy()
            alpha = binary_fill_holes(fg_all[yt:yb+1, x0:x1])
            # Erode 2px inward to completely strip white-bg antialiasing halos from silhouettes!
            alpha = binary_erosion(alpha, iterations=2)
            hs = crop.shape[0]
            ys_l, xs_l = np.where(alpha)
            core = xs_l[(ys_l < int(0.14 * hs)) | ((ys_l > int(0.46 * hs)) & (ys_l < int(0.56 * hs)))]
            cx = float(np.median(core)) if len(core) > 0 else crop.shape[1] * 0.5
            near = distance_transform_edt(~alpha, return_distances=False, return_indices=True)
            padded = crop[near[0], near[1]].astype(np.float32) / 255.0
            panels[name] = dict(rgb=padded, h=crop.shape[0], w=crop.shape[1], cx=cx)
        return panels

    def sample_panel(pan, u_norm, v_norm):
        rgb = pan["rgb"]; ph = pan["h"]; cx = pan["cx"]
        px = cx + u_norm * float(ph)
        py = v_norm * float(ph - 1)
        return np.stack([map_coordinates(rgb[..., c], [py, px], order=1, mode="nearest") for c in range(3)], axis=-1)

    def project_npz(npz_path, panels, img_arm_deg, y_center, mirror_rear_pants=False, crimson_inner_cape=False, min_v_clamp=0.0):
        d = np.load(npz_path)
        # Flip vertical axis at load time so row 0 = UV V=1 (top-down PNG orientation)
        pos_s = np.flipud(d["pos"]); nrm_s = np.flipud(d["nrm"])
        msk_s = np.flipud(d["msk"]); ao_s  = np.flipud(d["ao"]); tn_s = np.flipud(d["tn"])
        z_lo = float(d["z_lo"]); z_hi = float(d["z_hi"])
        pos_lo = d["pos_lo"]; pos_span = float(d["pos_span"])
        sh_x = float(d["sh_x"]); sh_z = float(d["sh_z"])
        mesh_arm_deg = float(d["mesh_arm_deg"])

        pos_w = pos_s * pos_span + pos_lo
        nrm_w = nrm_s * 2.0 - 1.0
        nlen = np.sqrt(np.sum(nrm_w * nrm_w, axis=-1, keepdims=True))
        valid = (nlen[..., 0] > 0.25) & (msk_s[..., 3] > 0.1)
        nrm_w /= np.maximum(nlen, 1e-5)

        H, W = valid.shape
        rows, cols = np.where(valid)
        p = pos_w[rows, cols].copy()
        n = nrm_w[rows, cols].copy()
        arm_l = msk_s[rows, cols, 0]; arm_r = msk_s[rows, cols, 1]; reg_code = msk_s[rows, cols, 2]
        z_span = max(0.5, z_hi - z_lo)

        # Rotate arms cleanly onto painted arm axis in Front/Back
        p_rot = p.copy()
        d_deg = img_arm_deg - mesh_arm_deg
        if abs(d_deg) > 1.0:
            rad = math.radians(d_deg); c_r, s_r = math.cos(rad), math.sin(rad)
            al_m = arm_l > 0.02
            if np.any(al_m):
                dx = p[al_m, 0] - sh_x; dz = p[al_m, 2] - sh_z
                rx = dx * c_r + dz * s_r; rz = -dx * s_r + dz * c_r
                p_rot[al_m, 0] = p[al_m, 0] * (1.0 - arm_l[al_m]) + (sh_x + rx) * arm_l[al_m]
                p_rot[al_m, 2] = p[al_m, 2] * (1.0 - arm_l[al_m]) + (sh_z + rz) * arm_l[al_m]
            ar_m = arm_r > 0.02
            if np.any(ar_m):
                dx = p[ar_m, 0] + sh_x; dz = p[ar_m, 2] - sh_z
                rx = dx * c_r - dz * s_r; rz = dx * s_r + dz * c_r
                p_rot[ar_m, 0] = p[ar_m, 0] * (1.0 - arm_r[ar_m]) + (-sh_x + rx) * arm_r[ar_m]
                p_rot[ar_m, 2] = p[ar_m, 2] * (1.0 - arm_r[ar_m]) + (sh_z + rz) * arm_r[ar_m]

        v_fb = np.clip((z_hi - p_rot[:, 2]) / z_span, min_v_clamp, 1.0)
        v_lr = np.clip((z_hi - p[:, 2]) / z_span, min_v_clamp, 1.0)
        u_f =  p_rot[:, 0] / z_span
        u_b = -p_rot[:, 0] / z_span
        u_r = -(p[:, 1] - y_center) / z_span
        u_l =  (p[:, 1] - y_center) / z_span

        cf = sample_panel(panels["front"], u_f, v_fb)
        cb = sample_panel(panels["back"],  u_b, v_fb)
        cr = sample_panel(panels["right"], u_r, v_lr)
        cl = sample_panel(panels["left"],  u_l, v_lr)

        if mirror_rear_pants:
            is_p = (reg_code > 0.25) & (reg_code < 0.75)
            cb[is_p] = cf[is_p]
        if crimson_inner_cape:
            is_ci = (reg_code >= 0.75) & (n[:, 1] < -0.05)
            if np.any(is_ci):
                vcl = np.clip(v_fb[is_ci], 0.56, 0.88)
                ucl = np.where(p[is_ci, 0] >= 0, 0.135, -0.135)
                cf[is_ci] = sample_panel(panels["front"], ucl, vcl)

        wf = np.clip(-n[:, 1], 0, 1) ** 3; wb = np.clip(n[:, 1], 0, 1) ** 3
        wr = np.clip(-n[:, 0], 0, 1) ** 3; wl = np.clip(n[:, 0], 0, 1) ** 3
        ws = np.maximum(wf + wb + wr + wl, 1e-6)
        blended = (cf*wf[:,None] + cb*wb[:,None] + cr*wr[:,None] + cl*wl[:,None]) / ws[:,None]

        img = np.zeros((H, W, 3), dtype=np.float32)
        img[rows, cols] = np.clip(blended, 0.0, 1.0)
        near_uv = distance_transform_edt(~valid, return_distances=False, return_indices=True)
        albedo = img[near_uv[0], near_uv[1]]

        # Sobel Height->Tangent Normal + Bevel Normal
        lum = 0.2126*albedo[...,0] + 0.7152*albedo[...,1] + 0.0722*albedo[...,2]
        lum_s = gaussian_filter(lum, sigma=1.6)
        dy = (np.roll(lum, -1, 0) - np.roll(lum, 1, 0)) * 2.2 + (np.roll(lum_s, -1, 0) - np.roll(lum_s, 1, 0)) * 3.4
        dx = (np.roll(lum, -1, 1) - np.roll(lum, 1, 1)) * 2.2 + (np.roll(lum_s, -1, 1) - np.roll(lum_s, 1, 1)) * 3.4
        bn = tn_s * 2.0 - 1.0
        # Notice bn in Blender OpenGL space already matches Unity after flipud cleanly!
        nx = bn[..., 0] - dx * 0.75; ny = bn[..., 1] + dy * 0.75; nz = np.maximum(0.18, bn[..., 2])
        nl = np.sqrt(nx*nx + ny*ny + nz*nz)
        nrm = np.ones((H, W, 4), np.float32)
        nrm[..., 0] = nx/nl*.5+.5; nrm[..., 1] = ny/nl*.5+.5; nrm[..., 2] = nz/nl*.5+.5
        return albedo, nrm, ao_s, pos_w

    def write_rgb_l_rgba(path, arr, mode="RGB"):
        u8 = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        tmp = path + ".tmp.png"
        if mode == "RGB": Image.fromarray(u8[..., :3]).save(tmp, format="PNG")
        elif mode == "L": Image.fromarray(u8).save(tmp, format="PNG")
        else: Image.fromarray(u8).save(tmp, format="PNG")
        os.replace(tmp, path)

    # ---- Project Raven & Vex ----
    for who, img_arm in (("raven", 49.0), ("vex", 49.0)):
        cap = who.capitalize()
        tex_dir = ASSETS + f"Characters/{cap}/Textures/"
        panels = load_panels(GUIDES + f"{who}_4view_painted.jpg")

        # 1) HeadPBR (matte-satin painted helmet steel, glossy green goggle lenses!)
        a, nrm, ao, _ = project_npz(BUFS + f"{who}_head.npz", panels, img_arm, y_center=-0.03)
        is_green = (a[...,1] > a[...,0]*1.32) & (a[...,1] > a[...,2]*1.32) & (a[...,1] > 0.16)
        a[is_green] = np.clip(a[is_green] * 1.45, 0, 1)
        mg = np.zeros((*a.shape[:2], 4), np.float32)
        mg[...,0] = np.where(is_green, 0.15, 0.28); mg[...,1] = mg[...,0]; mg[...,2] = mg[...,0]
        mg[...,3] = np.where(is_green, 0.92, np.clip(0.24 + 0.18*ao, 0.18, 0.42))
        write_rgb_l_rgba(tex_dir + f"{who}_head_pbr_a.png", a, "RGB")
        write_rgb_l_rgba(tex_dir + f"{who}_head_pbr_n.png", nrm, "RGBA")
        write_rgb_l_rgba(tex_dir + f"{who}_head_pbr_m.png", mg, "RGBA")
        write_rgb_l_rgba(tex_dir + f"{who}_head_pbr_ao.png", ao, "L")

        # 2) OutfitPBR
        a, nrm, ao, _ = project_npz(BUFS + f"{who}_outfit.npz", panels, img_arm, y_center=-0.03,
                                    mirror_rear_pants=(who=="raven"), crimson_inner_cape=(who=="raven"))
        lum = 0.2126*a[...,0] + 0.7152*a[...,1] + 0.0722*a[...,2]
        warm = a[...,0] - a[...,2]
        is_metal = lum > 0.44
        is_lea = (warm > 0.065) & (lum < 0.44)
        mg = np.zeros((*a.shape[:2], 4), np.float32)
        mg[...,0] = np.where(is_metal, 0.78, np.where(is_lea, 0.12, 0.0))
        mg[...,1] = mg[...,0]; mg[...,2] = mg[...,0]
        mg[...,3] = np.where(is_metal, 0.68, np.where(is_lea, 0.40, np.clip(0.14 + 0.12*lum, 0.08, 0.26)))
        write_rgb_l_rgba(tex_dir + f"{who}_outfit_pbr_a.png", a, "RGB")
        write_rgb_l_rgba(tex_dir + f"{who}_outfit_pbr_n.png", nrm, "RGBA")
        write_rgb_l_rgba(tex_dir + f"{who}_outfit_pbr_m.png", mg, "RGBA")
        write_rgb_l_rgba(tex_dir + f"{who}_outfit_pbr_ao.png", ao, "L")

        # 3) Skin
        a, nrm, ao, _ = project_npz(BUFS + f"{who}_skin.npz", panels, img_arm, y_center=-0.03)
        ao = np.maximum(0.55, ao)
        write_rgb_l_rgba(tex_dir + f"{who}_body.png", a, "RGB")
        write_rgb_l_rgba(tex_dir + f"{who}_skin_n.png", nrm, "RGBA")
        write_rgb_l_rgba(tex_dir + f"{who}_skin_ao.png", ao, "L")
        print("PROJECTED_BOSS_TEXTURES", who)

    # ---- Project Malcolm Vale (Skin_1 / RaiderBossNPC / Enemy_11) ----
    vale_dir = ASSETS + "Characters/ValeBoss/Textures/"
    v_panels = load_panels(GUIDES + "vale_4view_painted.jpg")
    for prefix, orig_n, orig_r in [
        ("vale_body",      "Body1_Normal_OpenGL.tga",     "Body1_Roughness.tga"),
        ("vale_waistcoat", "waistcoat_Normal_OpenGL.tga", "waistcoat_Roughness.tga"),
        ("vale_pants",     "pants1_Normal_OpenGL.tga",    "pants1_Roughness.tga"),
    ]:
        sz = 2048
        # Clamp waistcoat v >= 0.165 so collar upper lining never samples beard/chin from Head height!
        vc = 0.165 if prefix == "vale_waistcoat" else 0.0
        a, nrm, ao, pos_w = project_npz(BUFS + f"{prefix}.npz", v_panels, img_arm_deg=62.0, y_center=0.01, min_v_clamp=vc)
        # Blend original Skin_1 2K Substance Painter normal & roughness so native skin pores & vest grain shine!
        on = np.asarray(Image.open(SKIN1_TEX + orig_n).convert("RGB").resize((sz, sz), Image.BILINEAR), np.float32) / 255.0
        nxy = (nrm[..., :2] - 0.5) + (on[..., :2] - 0.5) * 0.65
        nz = np.maximum(0.22, nrm[..., 2] * 0.65 + on[..., 2] * 0.35)
        nvec = np.stack([nxy[...,0]*2.0, nxy[...,1]*2.0, nz], axis=-1)
        nl = np.sqrt(np.sum(nvec*nvec, axis=-1, keepdims=True))
        nrm[..., :3] = (nvec / np.maximum(nl, 1e-5)) * 0.5 + 0.5

        orr = np.asarray(Image.open(SKIN1_TEX + orig_r).convert("L").resize((sz, sz), Image.BILINEAR), np.float32) / 255.0
        lum = 0.2126*a[...,0] + 0.7152*a[...,1] + 0.0722*a[...,2]
        mg = np.zeros((sz, sz, 4), np.float32)
        if prefix == "vale_body":
            # Prevent shadowed AO & dark eye sockets on face (Z > 1.54m); gently warm & brighten face skin/eyes!
            is_head = pos_w[..., 2] > 1.54
            ao = np.where(is_head, np.maximum(0.68, ao), ao)
            a[is_head] = np.clip(a[is_head] * 1.18 + 0.04, 0.0, 1.0)
            mg[..., 3] = np.where(is_head, 0.20, np.clip(0.18 + (1.0 - orr)*0.15, 0.10, 0.36))
        else:
            is_brass = (a[...,0] - a[...,2] > 0.14) & (lum > 0.32)
            mg[...,0] = np.where(is_brass, 0.85, 0.04); mg[...,1] = mg[...,0]; mg[...,2] = mg[...,0]
            mg[...,3] = np.where(is_brass, 0.68, np.clip((1.0 - orr)*0.30 + 0.12, 0.12, 0.40))

        write_rgb_l_rgba(vale_dir + f"{prefix}_a.png", a, "RGB")
        write_rgb_l_rgba(vale_dir + f"{prefix}_n.png", nrm, "RGBA")
        write_rgb_l_rgba(vale_dir + f"{prefix}_m.png", mg, "RGBA")
        write_rgb_l_rgba(vale_dir + f"{prefix}_ao.png", ao, "L")
        print("PROJECTED_VALE_SKIN1", prefix)

if "--blender-bake" in sys.argv:
    run_blender_bake()
elif "--project" in sys.argv:
    run_projection()
