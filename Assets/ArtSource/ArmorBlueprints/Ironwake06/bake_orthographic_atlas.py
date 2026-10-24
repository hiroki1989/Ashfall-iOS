"""Project the Ironwake chest orthographic turnaround onto the fixed Warden UV.

Only the chest slot is consumed by SharedArmorTextures. This script does not
alter meshes, skinning or other inventory art. No oblique portrait is projected.
"""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt, map_coordinates

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
UV = Path("D:/Unity/AshfallArtValidation/shared-armor/uv-position.npz")
BASE = ROOT / "Assets/Resources/Art/GeneratedWarden/armor-rebuilt-atlas.png"
BLUEPRINT = HERE / "ironwake06-orthographic-4view-v1.png"
OUT = HERE / "ironwake06-chest-uv-candidate-v1.png"
MASK = HERE / "ironwake06-chest-uv-mask-v1.png"

# Pixel rectangles are the four non-overlapping orthographic panels in the
# 1774x887 source. Each is centered and padded by transparent pixels.
VIEWS = {
    "front": (0, 115, 621, 750),
    "back": (621, 115, 1216, 750),
    "left": (1216, 115, 1493, 750),
    "right": (1493, 115, 1774, 750),
}

source = np.asarray(Image.open(BLUEPRINT).convert("RGBA"), dtype=np.uint8)
base = np.asarray(Image.open(BASE).convert("RGB"), dtype=np.uint8).copy()
data = np.load(UV)
position = data["position"]
normal = data["normal"]
valid = data["valid"]

def view(name):
    x0, y0, x1, y1 = VIEWS[name]
    tile = source[y0:y1, x0:x1]
    alpha = tile[:, :, 3] > 20
    if not alpha.any():
        raise ValueError("Empty blueprint view: " + name)
    # Each panel uses one matching body frame. Fill transparent margins with
    # the nearest real material pixel to prevent black UV seam triangles.
    yy, xx = np.where(alpha)
    tile = tile[yy.min():yy.max()+1, xx.min():xx.max()+1]
    alpha = tile[:, :, 3] > 20
    near = distance_transform_edt(~alpha, return_distances=False,
                                  return_indices=True)
    return tile[near[0], near[1], :3]

images = {name: view(name) for name in VIEWS}
height = position[:, :, 2] + 1.0
# The approved chest renderer occupies 1.14..1.84 m including sleeves. The
# source collar and hem correspond to these exact mesh heights.
region = valid & (height >= 1.115) & (height <= 1.855)
# The chest slot also contains sleeve joints; arms are farther from the midline.
# Confine the waist below 1.25 m to the garment, not upper-leg UV islands.
region &= (height >= 1.25) | (np.abs(position[:, :, 0]) > .29)
rows, cols = np.where(region)
if len(rows) == 0:
    raise ValueError("No chest UV pixels matched the mesh")

p = position[rows, cols]
n = normal[rows, cols]
h = height[rows, cols]
# Coordinate convention: X = horizontal, Y = body depth (front is -Y), Z = up.
# On the side views, front (-Y) must lie at opposite ends to preserve the
# gauge/canister side rather than mirroring them across the back.
u_front = np.clip((p[:, 0] + .52) / 1.04, 0, 1)
u_back = 1-u_front
u_left = np.clip((p[:, 1] + .29) / .58, 0, 1)
u_right = 1-u_left
v = np.clip((1.85-h) / .74, 0, 1)

def sample(image, u):
    y = v * (image.shape[0]-1)
    x = u * (image.shape[1]-1)
    return np.stack([map_coordinates(image[:, :, c].astype(np.float32),
                [y, x], order=1, mode="nearest") for c in range(3)], axis=1)

front = sample(images["front"], u_front)
back = sample(images["back"], u_back)
left = sample(images["left"], u_left)
right = sample(images["right"], u_right)
front_weight = np.clip(-n[:, 1], 0, 1)
back_weight = np.clip(n[:, 1], 0, 1)
left_weight = np.clip(-n[:, 0], 0, 1)
right_weight = np.clip(n[:, 0], 0, 1)
weights = np.stack((front_weight, back_weight, left_weight, right_weight), axis=1)
weights = weights**3  # dominant orthographic view, soft seams near 45 degrees
weights /= np.maximum(weights.sum(axis=1, keepdims=True), 1e-6)
color = front*weights[:, 0:1]+back*weights[:, 1:2]+left*weights[:, 2:3]+right*weights[:, 3:4]
# Keep a trace of approved high-frequency cloth grain; new design dominates.
color = .91*color + .09*base[rows, cols].astype(np.float32)
base[rows, cols] = np.clip(color, 0, 255).astype(np.uint8)
Image.fromarray(base).save(OUT)
coverage = np.zeros(valid.shape, dtype=np.uint8)
coverage[rows, cols] = 255
Image.fromarray(coverage).save(MASK)
print("painted_pixels", len(rows), "output", OUT)
