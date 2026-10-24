"""Build a NEW articulated enemy mesh from the 2D orthographic design.

This is a lightweight visual-hull reconstruction, not a reskin or subdivision
of FaithfulEnemy3D's old primitives. It is intentionally kept separate from
the memory-heavy Hunyuan attempt and produces inspectable OBJ parts.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, map_coordinates, distance_transform_edt
from skimage.measure import marching_cubes

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets/Resources/Art/Enemy3DRebuild/NewMesh/Enemy_8"
SOURCE = ROOT / "Assets/ArtSource/Enemy3DRebuild/NewMesh/Enemy_8/orthographic-4view-v1.png"
OUT.mkdir(parents=True, exist_ok=True)

sheet = np.asarray(Image.open(SOURCE).convert("RGBA"))
panels = ((0, 540), (540, 1080), (1090, 1425), (1465, 1774))
views = []
for left, right in panels:
    tile = sheet[:, left:right]
    ys, xs = np.where(tile[:, :, 3] > 32)
    if len(xs) == 0:
        raise RuntimeError("Empty orthographic panel")
    # All four drawings use the same floor and crown line. Crop only width.
    tile = tile[:, xs.min():xs.max() + 1]
    mask = tile[:, :, 3] > 32
    iy, ix = distance_transform_edt(~mask, return_distances=False, return_indices=True)
    rgb = tile[iy, ix, :3]
    views.append((tile, rgb))

S = 1024
GUTTER = 12
atlas = Image.new("RGB", (S * 2, S * 2))
for index, (_, rgb) in enumerate(views):
    # Duplicate edge pixels into a gutter so bilinear/mip sampling never
    # reaches a different view at a tile boundary.
    tile = np.asarray(Image.fromarray(rgb).resize(
        (S - 2 * GUTTER, S - 2 * GUTTER), Image.Resampling.LANCZOS))
    padded = np.pad(tile, ((GUTTER, GUTTER), (GUTTER, GUTTER), (0, 0)), mode="edge")
    atlas.paste(Image.fromarray(padded), ((index % 2) * S, (index // 2) * S))
atlas.save(OUT / "texture-atlas.png")
body_atlas = np.asarray(atlas).copy()
for tile_index in (2, 3):
    left = (tile_index % 2) * S
    top = (tile_index // 2) * S
    tile = body_atlas[top:top + S, left:left + S].copy()
    # The side drawing puts the red hand in front of the waist. Remove that
    # occluder only from the torso's material; arm meshes retain the original.
    red = (tile[:, :, 0].astype(np.int16) > tile[:, :, 1].astype(np.int16) * 1.35) & \
          (tile[:, :, 0].astype(np.int16) > tile[:, :, 2].astype(np.int16) * 1.28) & \
          (tile[:, :, 0] > 60)
    _, indices = distance_transform_edt(red, return_indices=True)
    tile[red] = tile[indices[0][red], indices[1][red]]
    body_atlas[top:top + S, left:left + S] = tile
Image.fromarray(body_atlas).save(OUT / "texture-atlas-body.png")
boot_atlas = Image.new("RGB", (S * 2, S * 2))
for index, (_, rgb) in enumerate(views):
    # The boot occupies ~230 source pixels, whereas projecting the whole
    # character mapped only the very bottom sole and produced vertical bands.
    crop = rgb[600:831]
    tile = np.asarray(Image.fromarray(crop).resize(
        (S - 2 * GUTTER, S - 2 * GUTTER), Image.Resampling.LANCZOS))
    padded = np.pad(tile, ((GUTTER, GUTTER), (GUTTER, GUTTER), (0, 0)), mode="edge")
    boot_atlas.paste(Image.fromarray(padded), ((index % 2) * S, (index // 2) * S))
boot_atlas.save(OUT / "texture-boots.png")

# Coordinate frame: X left/right, Y Unity up, Z front. The source art spans
# crown at pixel 19 and sole at pixel 831, mapped to a 2.12 m humanoid rig.
NX, NY, NZ = 36, 58, 26
xs = np.linspace(-.75, .75, NX, dtype=np.float32)
ys = np.linspace(0, 2.12, NY, dtype=np.float32)
zs = np.linspace(-.48, .48, NZ, dtype=np.float32)
x, y, z = np.meshgrid(xs, ys, zs, indexing="ij")
row = (1 - y / 2.12) * (831 - 19) + 19

def silhouette(index, horizontal):
    tile = views[index][0]
    # Alpha remains the true silhouette; RGB edge filling is texture only.
    col = np.clip(horizontal * (tile.shape[1] - 1), 0, tile.shape[1] - 1)
    return map_coordinates(tile[:, :, 3].astype(np.float32),
                           [row, col], order=1, mode="constant") / 255

front = silhouette(0, (x + .75) / 1.50)
back = silhouette(1, (.75 - x) / 1.50)
side_l = silhouette(2, (.48 - z) / .96)
side_r = silhouette(3, (z + .48) / .96)
# Front/back and left/right are both used for the volume. The average makes
# small asymmetries possible without letting a transparent drawing cut a limb.
volume = (np.maximum(front, back) > .38) & (np.maximum(side_l, side_r) > .38)
volume = gaussian_filter(volume.astype(np.float32), .75) > .36

parts = {
    "Head": ((y >= 1.62) & (np.abs(x) < .38), (0, 1.78, 0)),
    "Chest": ((y >= 1.10) & (y < 1.80) & (np.abs(x) < .36), (0, 1.34, 0)),
    "Pelvis": ((y >= .86) & (y < 1.27) & (np.abs(x) < .40), (0, .96, 0)),
    "UpperArm_L": ((x < -.31) & (y >= 1.39) & (y < 1.70), (-.305, 1.64, 0)),
    "UpperArm_R": ((x > .31) & (y >= 1.39) & (y < 1.70), (.305, 1.64, 0)),
    "Forearm_L": ((x < -.30) & (y >= .80) & (y < 1.43), (-.305, 1.34, 0)),
    "Forearm_R": ((x > .30) & (y >= .80) & (y < 1.43), (.305, 1.34, 0)),
    "Thigh_L": ((x < 0) & (y >= .50) & (y < .99), (-.151, .935, 0)),
    "Thigh_R": ((x >= 0) & (y >= .50) & (y < .99), (.151, .935, 0)),
    "Shin_L": ((x < 0) & (y >= .18) & (y < .53), (-.151, .505, 0)),
    "Shin_R": ((x >= 0) & (y >= .18) & (y < .53), (.151, .505, 0)),
    "Foot_L": ((x < 0) & (y < .21), (-.151, .075, 0)),
    "Foot_R": ((x >= 0) & (y < .21), (.151, .075, 0)),
}

def swept_section(low, high, cx_low, cx_high, rx_low, rx_high,
                  z_low, z_high, rz_low, rz_high):
    """A separate tapered limb volume; the 2D silhouette mixed hands into torso."""
    t = np.clip((y - low) / (high - low), 0, 1)
    cx = cx_low + (cx_high - cx_low) * t
    cz = z_low + (z_high - z_low) * t
    rx = rx_low + (rx_high - rx_low) * t
    rz = rz_low + (rz_high - rz_low) * t
    return (y >= low) & (y <= high) & (((x - cx) / rx) ** 2 + ((z - cz) / rz) ** 2 <= 1)

def articulated_volume(name, region):
    if name == "Chest":
        # The source back drawing leaves transparent openings exactly where
        # independently animated head/chest/pelvis parts meet. Two rounded
        # back-armor overlaps close those openings without flat cap plates.
        collar = ((x/.30)**2 + ((y-1.69)/.13)**2 + ((z+.27)/.15)**2 <= 1)
        belt = ((x/.32)**2 + ((y-1.11)/.115)**2 + ((z+.27)/.15)**2 <= 1)
        return (volume & region) | collar | belt
    if name in ("Head", "Pelvis"):
        return volume & region
    side = -1 if name.endswith("_L") else 1
    if name.startswith("UpperArm"):
        return swept_section(1.37, 1.71, side*.51, side*.41,
                             .125, .155, .12, 0, .14, .17)
    if name.startswith("Forearm"):
        forearm = swept_section(.84, 1.43, side*.57, side*.50,
                                .13, .135, .24, .12, .14, .16)
        # Three independent claw tips replace the previous broad red hand slab.
        for claw in range(3):
            finger_x = side*(.50 + claw*.055)
            forearm |= swept_section(.72, .90, finger_x+side*.025, finger_x,
                                     .016, .038, .32, .23, .024, .045)
        return forearm
    if name.startswith("Thigh"):
        return swept_section(.49, 1.00, side*.20, side*.20,
                             .155, .17, .015, -.015, .19, .21)
    if name.startswith("Shin"):
        return swept_section(.17, .56, side*.18, side*.20,
                             .14, .155, .015, .01, .17, .18)
    # Boot heel, ankle and projecting toe are one continuous foot part.
    ankle = swept_section(.07, .23, side*.18, side*.18,
                           .14, .135, .015, .01, .18, .16)
    sole = (((x-side*.18)/.155)**2 + ((y-.067)/.067)**2 +
            ((z-.10)/.285)**2 <= 1)
    return ankle | sole

def uv_for(point, view, foot=False):
    px, py, pz = point
    # Pixel centers stay inside the real image, away from the tile gutter.
    lo = (GUTTER + .5) / (2 * S)
    scale = (S - 2 * GUTTER - 1) / (2 * S)
    height = np.clip(py / (.25 if foot else 2.12), 0, 1)
    if view == 0:
        return lo + scale * np.clip((px + .75) / 1.5, 0, 1), .5 + lo + scale * height
    if view == 1:
        return .5 + lo + scale * np.clip((.75 - px) / 1.5, 0, 1), .5 + lo + scale * height
    if view == 2:
        return lo + scale * np.clip((.48 - pz) / .96, 0, 1), lo + scale * height
    return .5 + lo + scale * np.clip((pz + .48) / .96, 0, 1), lo + scale * height

report = {}
for name, (region, joint) in parts.items():
    # A small overlap at cut planes prevents seams opening in animated poses.
    field = gaussian_filter(articulated_volume(name,region).astype(np.float32), .58)
    if field.max() < .35:
        raise RuntimeError("Missing segmented part: " + name)
    verts, faces, normals, _ = marching_cubes(field, level=.33,
        spacing=(float(xs[1]-xs[0]), float(ys[1]-ys[0]), float(zs[1]-zs[0])))
    verts += np.array([xs[0], ys[0], zs[0]], dtype=np.float32)
    # The marching-cubes vertex normals point into the solid. Their averaged
    # direction is stabler than the normal of one tiny voxel triangle.
    outward = -normals
    path = OUT / (name + ".obj")
    with path.open("w", encoding="ascii") as f:
        f.write("# New visual hull from Enemy_8 orthographic concept; local joint coordinates\n")
        for vx, vy, vz in verts:
            f.write(f"v {vx-joint[0]:.6f} {vy-joint[1]:.6f} {vz-joint[2]:.6f}\n")
        uv_index = 0
        for raw_tri in faces:
            # skimage's default gradient orientation points into a filled
            # alpha volume. Reverse winding so Unity's front face and the
            # inverted-hull ink pass do not turn the entire model black.
            tri = raw_tri[::-1]
            a, b, c = verts[tri]
            normal = np.cross(b-a, c-a)
            if np.linalg.norm(normal) < 1e-8:
                continue
            # Choose the drawing facing the surface. Front/back-only mapping
            # stretched a single painted motif across the entire side and
            # carried the red claws onto unrelated armor surfaces.
            direction = outward[tri].mean(axis=0)
            if abs(direction[0]) > abs(direction[2]) * 1.12:
                view = 2 if direction[0] < 0 else 3
            else:
                # Unity's OBJ import reverses the source Z facing. Its +Z
                # camera sees the drawing's back after import.
                view = 1 if direction[2] >= 0 else 0
            uv = [uv_for(verts[idx], view, name.startswith("Foot")) for idx in tri]
            for u, v in uv:
                f.write(f"vt {u:.6f} {v:.6f}\n")
            f.write("f " + " ".join(f"{int(idx)+1}/{uv_index+j+1}" for j, idx in enumerate(tri)) + "\n")
            uv_index += 3
    report[name] = {"vertices": len(verts), "faces": len(faces), "joint": joint}

(OUT / "shape-report.json").write_text(json.dumps({
    "method": "orthographic 2D visual hull / new mesh, no legacy geometry",
    "reference": str(SOURCE.relative_to(ROOT)), "parts": report,
    "ai_shape_attempt": "Hunyuan3D-2mini aborted at memory watchdog; no AI mesh used"
}, indent=2), encoding="utf-8")
print("NEW_VISUAL_HULL_PARTS", len(report), "FACES", sum(v["faces"] for v in report.values()))
