"""Deterministic, bounded-memory four-face atlas from approved source paintings.

The two end tiles are neutral metal sampled from the supplied side paintings
when dedicated front/back paintings do not exist. No source image is changed.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image
import numpy as np
from scipy.ndimage import distance_transform_edt


def tile_image(source, source_label, side, end=False):
    source.thumbnail((2048, 2048), Image.Resampling.LANCZOS)
    rgba = np.asarray(source.convert("RGBA"))
    mask = rgba[:, :, 3] > 24
    if not mask.any():
        raise ValueError(f"No opaque pixels in {source_label}")
    ys, xs = np.where(mask)
    crop = rgba[ys.min():ys.max()+1, xs.min():xs.max()+1]
    if end:
        # The end face has no painted design; use the median metal/stock color.
        inside = crop[crop[:, :, 3] > 24, :3]
        rgb = np.median(inside, axis=0).astype(np.uint8)
        result = np.empty((side, side, 4), np.uint8)
        result[:, :, :3] = rgb
        result[:, :, 3] = 255
        return Image.fromarray(result), {"source": source_label, "method": "neutral-median"}
    h, w = crop.shape[:2]
    scale = min((side-24)/w, (side-24)/h)
    im = Image.fromarray(crop).resize((max(1, round(w*scale)), max(1, round(h*scale))), Image.Resampling.LANCZOS)
    data = np.asarray(im)
    visible = data[:, :, 3] > 24
    _, nearest = distance_transform_edt(~visible, return_indices=True)
    filled = data[nearest[0], nearest[1], :3]
    base = np.empty((side, side, 4), np.uint8)
    base[:, :, :3] = np.median(filled[visible], axis=0).astype(np.uint8)
    base[:, :, 3] = 255
    px, py = (side-im.width)//2, (side-im.height)//2
    base[py:py+im.height, px:px+im.width, :3] = filled
    return Image.fromarray(base), {"source": source_label, "method": "silhouette-crop-edge-fill", "visibleRect": [px, py, im.width, im.height]}


def tile(path, side, end=False):
    with Image.open(path) as source:
        im, info = tile_image(source.copy(), str(path), side, end=end)
    info["sha256"] = hashlib.sha256(Path(path).read_bytes()).hexdigest()
    return im, info


def main():
    p = argparse.ArgumentParser()
    for name in ("left", "right", "front", "back"):
        p.add_argument("--"+name, type=Path)
    p.add_argument("--out", type=Path, required=True)
    p.add_argument("--tile", type=int, default=512)
    a = p.parse_args()
    if not a.left or not a.right:
        p.error("left and right painted views are required")
    if a.tile < 64 or a.tile > 1024 or a.tile & (a.tile-1):
        p.error("tile must be power of two from 64 to 1024")
    paths = [a.left, a.right, a.front or a.left, a.back or a.right]
    atlas = Image.new("RGBA", (a.tile*2, a.tile*2))
    views = {}
    for i, (name, path) in enumerate(zip(("left", "right", "front", "back"), paths)):
        im, info = tile(path, a.tile, end=(i >= 2 and getattr(a, name) is None))
        atlas.paste(im, ((i%2)*a.tile, (i//2)*a.tile))
        views[name] = info
    a.out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(a.out, optimize=True)
    manifest = {"schema": 1, "atlas": a.out.name, "tilePixels": a.tile, "layoutTopLeft": [["left", "right"], ["front", "back"]], "views": views, "quality": "candidate" if not a.front or not a.back else "source-complete", "alphaMin": 255}
    a.out.with_suffix(".json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"atlas": str(a.out), "quality": manifest["quality"]}))


if __name__ == "__main__":
    main()
