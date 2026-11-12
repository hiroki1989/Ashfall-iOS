import bpy
import bmesh
import glob
import math
import os
import numpy as np
from mathutils import Vector, Matrix

RES_CHARS = "D:/Unity/Ashfall-iOS/Assets/Resources/Characters"
LEGACY_RAVEN = "D:/Unity/Ashfall-iOS/Assets/ArtSource/LegacyCharactersBackup/Characters/Raven/Raven.fbx"
SCRATCH = "C:/Users/hiroki/.gemini/antigravity/brain/6fbecd1d-d328-48e6-a284-4c4e9bf72c9e/scratch"


def ensure_dir(p):
    os.makedirs(p, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def pack_meshy_pbr(src_dir, prefix_glob, out_dir, out_prefix):
    """Copies albedo & normal, packs linear metallic (R) + smoothness (A=1-roughness) -> _m.png."""
    ensure_dir(out_dir)
    files = glob.glob(os.path.join(src_dir, prefix_glob))
    alb_src = [f for f in files if f.lower().endswith(".png")
               and not f.lower().endswith("_metallic.png")
               and not f.lower().endswith("_normal.png")
               and not f.lower().endswith("_roughness.png")][0]
    met_src = [f for f in files if f.lower().endswith("_metallic.png")][0]
    nrm_src = [f for f in files if f.lower().endswith("_normal.png")][0]
    rgh_src = [f for f in files if f.lower().endswith("_roughness.png")][0]

    alb_img = bpy.data.images.load(alb_src.replace("\\", "/"))
    nrm_img = bpy.data.images.load(nrm_src.replace("\\", "/"))
    met_img = bpy.data.images.load(met_src.replace("\\", "/"))
    rgh_img = bpy.data.images.load(rgh_src.replace("\\", "/"))

    alb_out = os.path.join(out_dir, f"{out_prefix}_a.png").replace("\\", "/")
    nrm_out = os.path.join(out_dir, f"{out_prefix}_n.png").replace("\\", "/")
    met_out = os.path.join(out_dir, f"{out_prefix}_m.png").replace("\\", "/")

    import shutil
    shutil.copy2(alb_src, alb_out)
    shutil.copy2(nrm_src, nrm_out)

    w, h = met_img.size
    met_img.colorspace_settings.name = "Non-Color"
    rgh_img.colorspace_settings.name = "Non-Color"
    m_arr = np.array(met_img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    r_arr = np.array(rgh_img.pixels[:], dtype=np.float32).reshape(h, w, 4)

    out_arr = np.empty((h, w, 4), dtype=np.float32)
    out_arr[:, :, 0] = m_arr[:, :, 0]
    out_arr[:, :, 1] = m_arr[:, :, 0]
    out_arr[:, :, 2] = m_arr[:, :, 0]
    out_arr[:, :, 3] = np.clip(1.0 - r_arr[:, :, 0], 0.0, 1.0)

    packed = bpy.data.images.new(f"{out_prefix}_m", width=w, height=h, alpha=True)
    packed.colorspace_settings.name = "Non-Color"
    packed.pixels.foreach_set(out_arr.ravel())
    packed.filepath_raw = met_out
    packed.file_format = "PNG"
    packed.save()

    # Verify alpha channel survived write
    chk = bpy.data.images.load(met_out)
    chk.colorspace_settings.name = "Non-Color"
    ca = np.array(chk.pixels[:], dtype=np.float32).reshape(h, w, 4)
    print(f"Packed {out_prefix}_m: metallic mean={ca[:,:,0].mean():.3f}, smoothness mean={ca[:,:,3].mean():.3f}")
    return alb_out, nrm_out, met_out


def assign_pbr_material(mesh_obj, mat_name, alb_path, nrm_path, met_path):
    mesh_obj.data.materials.clear()
    mat = bpy.data.materials.new(mat_name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex_a = nt.nodes.new("ShaderNodeTexImage")
    tex_a.image = bpy.data.images.load(alb_path)
    nt.links.new(tex_a.outputs["Color"], bsdf.inputs["Base Color"])

    tex_n = nt.nodes.new("ShaderNodeTexImage")
    img_n = bpy.data.images.load(nrm_path)
    img_n.colorspace_settings.name = "Non-Color"
    tex_n.image = img_n
    nrm_node = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(tex_n.outputs["Color"], nrm_node.inputs["Color"])
    nt.links.new(nrm_node.outputs["Normal"], bsdf.inputs["Normal"])

    mesh_obj.data.materials.append(mat)


def decimate_mesh(obj, ratio=0.36):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    before = len(obj.data.polygons)
    mod = obj.modifiers.new("DecimateMobile", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = ratio
    bpy.ops.object.modifier_apply(modifier="DecimateMobile")
    after = len(obj.data.polygons)
    print(f"Decimated {obj.name}: {before} -> {after} tris (uvs={len(obj.data.uv_layers)})")


def normalize_mesh_vertices(mesh_obj, target_height):
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.select_all(action="DESELECT")
    mesh_obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pts = [v.co for v in mesh_obj.data.vertices]
    xmin = min(p.x for p in pts); xmax = max(p.x for p in pts)
    zmin = min(p.z for p in pts); zmax = max(p.z for p in pts)
    cx = 0.5 * (xmin + xmax)
    h = zmax - zmin
    s = target_height / h
    for v in mesh_obj.data.vertices:
        v.co = Vector(((v.co.x - cx) * s, v.co.y * s, (v.co.z - zmin) * s))
    mesh_obj.data.update()
    pts2 = [v.co for v in mesh_obj.data.vertices]
    print(f"Normalized {mesh_obj.name}: h={max(p.z for p in pts2)-min(p.z for p in pts2):.4f} z=[{min(p.z for p in pts2):.4f},{max(p.z for p in pts2):.4f}]")


def build_static_npc(name, src_fbx, src_tex_dir, target_height, decimate_ratio):
    clear_scene()
    out_dir = os.path.join(RES_CHARS, name)
    tex_dir = os.path.join(out_dir, "Textures")
    out_fbx = os.path.join(out_dir, f"{name}.fbx")
    low = name.lower()
    alb, nrm, met = pack_meshy_pbr(src_tex_dir, "*.png", tex_dir, f"{low}_meshy")

    bpy.ops.import_scene.fbx(filepath=src_fbx)
    mesh = [o for o in bpy.data.objects if o.type == "MESH"][0]
    mesh.name = name
    decimate_mesh(mesh, decimate_ratio)
    normalize_mesh_vertices(mesh, target_height)
    assign_pbr_material(mesh, f"{name}_MeshyPBR", alb, nrm, met)

    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        path_mode="COPY",
        embed_textures=False,
    )
    print(f"Exported static NPC FBX: {out_fbx}")


def centroid_slice(pts, cond):
    sel = [p for p in pts if cond(p)]
    if not sel:
        return None
    n = len(sel)
    return Vector((sum(p.x for p in sel) / n, sum(p.y for p in sel) / n, sum(p.z for p in sel) / n))


def dist_point_to_segment(p, a, b):
    ab = b - a
    denom = ab.dot(ab)
    if denom < 1e-8:
        return (p - a).length
    t = max(0.0, min(1.0, (p - a).dot(ab) / denom))
    proj = a + ab * t
    return (p - proj).length


def build_raven_rigged():
    clear_scene()
    out_dir = os.path.join(RES_CHARS, "Raven")
    tex_dir = os.path.join(out_dir, "Textures")
    out_fbx = os.path.join(out_dir, "Raven.fbx")
    src_tex_dir = "D:/downloads2/Raven/Meshy_AI_Wasteland_Sentinel_20261111_texture_fbx"
    src_fbx = os.path.join(src_tex_dir, "Meshy_AI_Wasteland_Sentinel_20261111_texture.fbx")

    alb, nrm, met = pack_meshy_pbr(src_tex_dir, "*.png", tex_dir, "raven_meshy")

    # 1. Import legacy Raven just to borrow the exact RavenRig skeleton hierarchy & bone roll conventions
    bpy.ops.import_scene.fbx(filepath=LEGACY_RAVEN)
    rig = [o for o in bpy.data.objects if o.type == "ARMATURE"][0]
    rig.name = "RavenRig"
    for o in list(bpy.data.objects):
        if o != rig:
            bpy.data.objects.remove(o, do_unlink=True)

    # Save original bone rolls
    orig_rolls = {}
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for eb in rig.data.edit_bones:
        orig_rolls[eb.name] = eb.roll
    bpy.ops.object.mode_set(mode="OBJECT")

    # 2. Import New Raven mesh, decimate & normalize height to 1.960m (* 1.20 VaultGame scale = 2.352m == Player!)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=src_fbx)
    mesh = [o for o in bpy.data.objects if o not in before and o.type == "MESH"][0]
    mesh.name = "Raven"
    decimate_mesh(mesh, 0.38)
    normalize_mesh_vertices(mesh, 1.960)
    assign_pbr_material(mesh, "Raven_MeshyPBR", alb, nrm, met)

    pts = [v.co.copy() for v in mesh.data.vertices]
    h = 1.960

    def c_or(cond, fallback):
        r = centroid_slice(pts, cond)
        return r if r is not None else Vector(fallback)

    # Measure key anatomical centers dynamically on New_Raven
    pelvis_c = c_or(lambda p: abs(p.x) < 0.22 and 0.94 <= p.z <= 1.04, (0.0, -0.02, 0.99))
    sp1_c    = c_or(lambda p: abs(p.x) < 0.20 and 1.08 <= p.z <= 1.18, (0.0, -0.01, 1.13))
    sp2_c    = c_or(lambda p: abs(p.x) < 0.20 and 1.22 <= p.z <= 1.32, (0.0,  0.00, 1.27))
    sp3_c    = c_or(lambda p: abs(p.x) < 0.22 and 1.38 <= p.z <= 1.48, (0.0, -0.02, 1.43))
    neck_c   = c_or(lambda p: abs(p.x) < 0.14 and 1.54 <= p.z <= 1.62, (0.0, -0.01, 1.58))
    head_c   = c_or(lambda p: abs(p.x) < 0.15 and 1.68 <= p.z <= 1.82, (0.0, -0.03, 1.74))

    side_joints = {}
    for sgn, sfx in [(1.0, "_l"), (-1.0, "_r")]:
        side_cond = (lambda p, sg=sgn: p.x * sg > 0.03)
        foot_c  = c_or(lambda p, sg=sgn: 0.04 <= p.x * sg <= 0.28 and 0.04 <= p.z <= 0.12, (sgn*0.18, -0.02, 0.08))
        knee_c  = c_or(lambda p, sg=sgn: 0.04 <= p.x * sg <= 0.26 and 0.50 <= p.z <= 0.58, (sgn*0.14,  0.00, 0.54))
        thigh_c = c_or(lambda p, sg=sgn: 0.04 <= p.x * sg <= 0.24 and 0.91 <= p.z <= 0.99, (sgn*0.13,  0.01, 0.95))
        knee_c.y = min(knee_c.y, -0.015)  # slight forward anatomical knee hint toward -Y (Unity +Z)

        clav_c  = c_or(lambda p, sg=sgn: 0.05 <= p.x * sg <= 0.14 and p.z >= 1.44, (sgn*0.09, 0.00, 1.51))
        shld_c  = c_or(lambda p, sg=sgn: 0.17 <= p.x * sg <= 0.23 and p.z >= 1.36, (sgn*0.20, 0.03, 1.45))
        elbw_c  = c_or(lambda p, sg=sgn: 0.30 <= p.x * sg <= 0.37 and p.z >= 1.16, (sgn*0.34, 0.03, 1.28))
        elbw_c.y = max(elbw_c.y, 0.035)   # slight backward anatomical elbow hint toward +Y
        wrst_c  = c_or(lambda p, sg=sgn: 0.44 <= p.x * sg <= 0.47 and p.z >= 0.90, (sgn*0.455, -0.04, 1.07))
        palm_c  = c_or(lambda p, sg=sgn: 0.48 <= p.x * sg <= 0.51 and p.z >= 0.85, (sgn*0.495, -0.065, 1.02))
        side_joints[sfx] = (foot_c, knee_c, thigh_c, clav_c, shld_c, elbw_c, wrst_c, palm_c)

    # Position RavenRig edit bones inside New_Raven
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    eb = rig.data.edit_bones

    def set_bone(name, head, tail):
        b = eb.get(name)
        if not b:
            return
        b.use_connect = False
        b.head = Vector(head)
        b.tail = Vector(tail)
        if name in orig_rolls:
            b.roll = orig_rolls[name]

    set_bone("Root", (0, 0, 0), (0, 0, 0.25))
    set_bone("pelvis", (0, pelvis_c.y, pelvis_c.z), (0, sp1_c.y, sp1_c.z))
    set_bone("spine_01", (0, sp1_c.y, sp1_c.z), (0, sp2_c.y, sp2_c.z))
    set_bone("spine_02", (0, sp2_c.y, sp2_c.z), (0, sp3_c.y, sp3_c.z))
    set_bone("spine_03", (0, sp3_c.y, sp3_c.z), (0, neck_c.y, neck_c.z))
    set_bone("neck_01", (0, neck_c.y, neck_c.z), (0, head_c.y, 1.66))
    set_bone("head", (0, head_c.y, 1.66), (0, head_c.y, 1.90))

    for sgn, sfx in [(1.0, "_l"), (-1.0, "_r")]:
        foot_c, knee_c, thigh_c, clav_c, shld_c, elbw_c, wrst_c, palm_c = side_joints[sfx]
        shld_pos = Vector((sgn * 0.195, shld_c.y, 1.46))
        clav_pos = Vector((sgn * 0.04, shld_c.y, 1.49))
        elbw_pos = Vector((sgn * 0.34, elbw_c.y, 1.28))
        wrst_pos = Vector((sgn * 0.455, wrst_c.y, 1.08))
        palm_dir = (palm_c - wrst_pos).normalized()
        hand_tail = wrst_pos + palm_dir * 0.065

        set_bone("clavicle" + sfx, clav_pos, shld_pos)
        set_bone("upperarm" + sfx, shld_pos, elbw_pos)
        set_bone("lowerarm" + sfx, elbw_pos, wrst_pos)
        set_bone("hand" + sfx, wrst_pos, hand_tail)

        # Fingers splayed slightly down/out along glove orientation
        finger_specs = [
            ("index",  -0.024, 0.068, 0.022),
            ("middle", -0.008, 0.072, 0.025),
            ("ring",   +0.010, 0.070, 0.022),
            ("pinky",  +0.026, 0.064, 0.018),
        ]
        for fname, y_off, base_len, seg_len in finger_specs:
            f1_h = wrst_pos + palm_dir * base_len + Vector((0.0, y_off, 0.0))
            fdir = (palm_dir + Vector((0.0, 0.0, -0.35))).normalized()
            f2_h = f1_h + fdir * seg_len
            f3_h = f2_h + fdir * (seg_len * 0.85)
            f3_t = f3_h + fdir * (seg_len * 0.70)
            set_bone(f"{fname}_01{sfx}", f1_h, f2_h)
            set_bone(f"{fname}_02{sfx}", f2_h, f3_h)
            set_bone(f"{fname}_03{sfx}", f3_h, f3_t)

        # Thumb
        t1_h = wrst_pos + palm_dir * 0.028 + Vector((0.0, -0.028, -0.010))
        tdir = Vector((sgn * 0.45, -0.65, -0.60)).normalized()
        t2_h = t1_h + tdir * 0.026
        t3_h = t2_h + tdir * 0.022
        t3_t = t3_h + tdir * 0.018
        set_bone(f"thumb_01{sfx}", t1_h, t2_h)
        set_bone(f"thumb_02{sfx}", t2_h, t3_h)
        set_bone(f"thumb_03{sfx}", t3_h, t3_t)

        # Leg chain
        hip_pos  = Vector((sgn * 0.125, thigh_c.y, 0.96))
        knee_pos = Vector((sgn * 0.145, knee_c.y, 0.54))
        ank_pos  = Vector((sgn * 0.185, foot_c.y + 0.025, 0.085))
        ball_pos = Vector((sgn * 0.185, foot_c.y - 0.075, 0.022))
        toe_pos  = Vector((sgn * 0.185, foot_c.y - 0.145, 0.020))
        set_bone("thigh" + sfx, hip_pos, knee_pos)
        set_bone("calf" + sfx, knee_pos, ank_pos)
        set_bone("foot" + sfx, ank_pos, ball_pos)
        set_bone("ball" + sfx, ball_pos, toe_pos)

    # Capture bone segments in armature world space for geometric weight fallback / cleanup
    bone_segs = {}
    for b in eb:
        bone_segs[b.name] = (b.head.copy(), b.tail.copy())
    bpy.ops.object.mode_set(mode="OBJECT")

    # Parent mesh to rig with Automatic Weights first, then verify & fix any unskinned/cross-leg/head vertices
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    auto_ok = True
    try:
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    except Exception as ex:
        print("ARMATURE_AUTO exception:", ex)
        auto_ok = False
        bpy.ops.object.parent_set(type="ARMATURE_NAME")

    # Ensure every vertex group exists
    for bname in bone_segs:
        if bname not in mesh.vertex_groups:
            mesh.vertex_groups.new(name=bname)

    def set_vertex_weights(v_idx, weights_dict):
        for vg in mesh.vertex_groups:
            vg.remove([v_idx])
        tot = sum(weights_dict.values())
        if tot <= 1e-8:
            mesh.vertex_groups["pelvis"].add([v_idx], 1.0, "REPLACE")
            return
        items = sorted(weights_dict.items(), key=lambda kv: -kv[1])[:4]
        s_w = sum(w for _, w in items)
        for bname, w in items:
            if w / s_w > 0.005:
                mesh.vertex_groups[bname].add([v_idx], w / s_w, "REPLACE")

    def get_v_weights(v):
        out = {}
        for g in v.groups:
            out[mesh.vertex_groups[g.group].name] = g.weight
        return out

    deform_bones = [bn for bn in bone_segs if bn != "Root"]
    unweighted = 0
    for v in mesh.data.vertices:
        p = v.co
        wmap = get_v_weights(v)
        tot = sum(wmap.values())

        # Anatomical cleanup:
        # 1) Head & Helmet & Mask strictly head/neck (no torso/shoulder stretch!)
        if p.z >= 1.65 and abs(p.x) <= 0.165:
            set_vertex_weights(v.index, {"head": 1.0})
            continue
        if p.z >= 1.56 and abs(p.x) <= 0.14:
            t = (p.z - 1.56) / (1.65 - 1.56)
            set_vertex_weights(v.index, {"head": t, "neck_01": 1.0 - t})
            continue

        # 2) Unweighted vertices fallback to nearest inverse-distance-squared bones on same side
        if not auto_ok or tot < 0.05:
            unweighted += 1
            cand = []
            for bn in deform_bones:
                if bn.endswith("_l") and p.x < -0.015:
                    continue
                if bn.endswith("_r") and p.x > 0.015:
                    continue
                a, b = bone_segs[bn]
                d = dist_point_to_segment(p, a, b)
                cand.append((bn, 1.0 / (d * d + 0.0004)))
            cand.sort(key=lambda kv: -kv[1])
            set_vertex_weights(v.index, dict(cand[:3]))
            continue

        # 3) Prevent left/right leg cross-deform below crotch
        if p.z < 0.90:
            cleaned = {}
            for bn, w in wmap.items():
                if p.x > 0.015 and bn.endswith("_r"):
                    continue
                if p.x < -0.015 and bn.endswith("_l"):
                    continue
                cleaned[bn] = w
            if sum(cleaned.values()) > 0.05:
                set_vertex_weights(v.index, cleaned)
            else:
                sfx = "_l" if p.x >= 0 else "_r"
                set_vertex_weights(v.index, {"foot" + sfx: 1.0} if p.z < 0.14 else {"calf" + sfx: 1.0} if p.z < 0.56 else {"thigh" + sfx: 1.0})

    bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    print(f"Raven weighted cleanly! unweighted_fixed={unweighted}")

    # Export Raven FBX
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        path_mode="COPY",
        embed_textures=False,
    )
    print(f"Exported rigged Raven FBX: {out_fbx}")



if __name__ == "__main__":
    build_static_npc("Sera", "D:/downloads2/sera/Meshy_AI_Wasteland_Mechanic_20261111_texture_fbx/Meshy_AI_Wasteland_Mechanic_20261111_texture.fbx", "D:/downloads2/sera/Meshy_AI_Wasteland_Mechanic_20261111_texture_fbx", 2.156, 0.38)
