"""Small software raster QA for approved armor mesh + an illustration atlas.

Uses the exact FBX triangles and UVs exported by export_uv.py. It does not
start Unity or Blender, which keeps memory usage low while the Editor is open.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path("D:/Unity/Ashfall-iOS")
VALIDATION = Path("D:/Unity/AshfallArtValidation/shared-armor")
SIZE = 640


def render(vertices, triangles, texture, view, slot):
    frame = np.full((SIZE, SIZE, 3), 112, dtype=np.uint8)
    depth = np.full((SIZE, SIZE), -np.inf, dtype=np.float32)
    if view == "front":
        coords = vertices[:, [0, 2]]
        distance = -vertices[:, 1]
    elif view == "back":
        coords = vertices[:, [0, 2]].copy()
        coords[:, 0] *= -1
        distance = vertices[:, 1]
    else:
        coords = vertices[:, [1, 2]]
        distance = vertices[:, 0]
    xy = np.empty_like(coords)
    xy[:, 0] = (coords[:, 0] + .65) / 1.30 * (SIZE - 1)
    xy[:, 1] = (.95 - coords[:, 1]) / 1.95 * (SIZE - 1)
    for indices, uv in triangles:
        index = np.asarray(indices, dtype=np.int32)
        center = vertices[index].mean(axis=0)
        height = center[2] + 1
        width = abs(center[0])
        # Visual QA crop of the runtime slot split. The exact runtime split
        # also uses bone weights; this is deliberately conservative.
        in_slot = ((slot == 1 and (height >= 1.23 or (height >= .96 and width < .36)))
                   or (slot == 2 and .75 <= height < 1.23 and width >= .30)
                   or (slot == 3 and .43 <= height < 1.25 and width < .36)
                   or (slot == 4 and height < .43))
        if not in_slot:
            continue
        points = xy[index]
        x0 = max(0, int(np.floor(points[:, 0].min())))
        x1 = min(SIZE - 1, int(np.ceil(points[:, 0].max())))
        y0 = max(0, int(np.floor(points[:, 1].min())))
        y1 = min(SIZE - 1, int(np.ceil(points[:, 1].max())))
        if x1 < x0 or y1 < y0:
            continue
        a, b, c = points
        det = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(det) < 1e-7:
            continue
        yy, xx = np.mgrid[y0:y1 + 1, x0:x1 + 1]
        wa = ((b[1] - c[1]) * (xx - c[0]) + (c[0] - b[0]) * (yy - c[1])) / det
        wb = ((c[1] - a[1]) * (xx - c[0]) + (a[0] - c[0]) * (yy - c[1])) / det
        wc = 1 - wa - wb
        z = wa * distance[index[0]] + wb * distance[index[1]] + wc * distance[index[2]]
        inside = (wa >= -1e-4) & (wb >= -1e-4) & (wc >= -1e-4)
        old_depth = depth[y0:y1 + 1, x0:x1 + 1]
        visible = inside & (z > old_depth)
        if not np.any(visible):
            continue
        old_depth[visible] = z[visible]
        uv = np.asarray(uv, dtype=np.float32)
        u = wa * uv[0, 0] + wb * uv[1, 0] + wc * uv[2, 0]
        v = wa * uv[0, 1] + wb * uv[1, 1] + wc * uv[2, 1]
        tx = np.clip(np.rint(u[visible] * (texture.shape[1] - 1)).astype(np.int32), 0, texture.shape[1] - 1)
        ty = np.clip(np.rint((1 - v[visible]) * (texture.shape[0] - 1)).astype(np.int32), 0, texture.shape[0] - 1)
        frame[y0:y1 + 1, x0:x1 + 1][visible] = texture[ty, tx]
    return frame


def main():
    product = sys.argv[1]
    queue = json.loads(Path("D:/Unity/AshfallArtValidation/equipment-generation/queue.json").read_text(encoding="utf-8"))
    jobs = {j["key"]: j for j in queue["jobs"]}
    spec = next(p for p in queue["products"] if p["id"] == product)
    job = jobs[spec["jobs"][0]]
    slot = job.get("slot", job.get("index"))
    mesh = json.loads((VALIDATION / "source-mesh.json").read_text())
    vertices = np.asarray(mesh["vertices"], dtype=np.float32)
    staged = VALIDATION / "illustration-staging" / f"{product}.png"
    source = staged if staged.exists() else ROOT / "Assets/Resources/Art/SharedArmorIllustration" / f"{product}.png"
    texture = np.asarray(Image.open(source).convert("RGB"))
    frames = [render(vertices, mesh["triangles"], texture, view, slot) for view in ("front", "side", "back")]
    output = VALIDATION / "illustration-qa"
    output.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.concatenate(frames, axis=1)).save(output / f"{product}-front-side-back.png")
    print(output / f"{product}-front-side-back.png")


if __name__ == "__main__":
    main()
