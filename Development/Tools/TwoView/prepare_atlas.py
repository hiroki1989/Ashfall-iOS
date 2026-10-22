"""Pack two approved views into a padded, fully opaque projection atlas.

Run with the ordinary Python 3 environment (Pillow, numpy, scipy). The source
files are only read. The nearest visible pixel fills transparent illustration
padding so UVs at silhouette edges cannot sample an empty texel.
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt


def filled_view(path: Path, size: int, occupancy: float) -> tuple[Image.Image, dict]:
    image = Image.open(path).convert("RGBA")
    rgba = np.asarray(image)
    opaque = rgba[:, :, 3] > 16
    if not opaque.any():
        raise ValueError(f"No visible pixels: {path}")
    yy, xx = np.where(opaque)
    bbox = (int(xx.min()), int(yy.min()), int(xx.max() + 1), int(yy.max() + 1))
    cropped = image.crop(bbox)
    span = round(size * occupancy)
    scale = min(span / cropped.width, span / cropped.height)
    shape = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    resized = cropped.resize(shape, Image.Resampling.LANCZOS)
    pixels = np.asarray(resized)
    mask = pixels[:, :, 3] > 16
    if not mask.any():
        raise ValueError(f"Empty resized image: {path}")
    _, indices = distance_transform_edt(~mask, return_indices=True)
    rgb = pixels[indices[0], indices[1], :3]
    field = np.empty((size, size, 4), dtype=np.uint8)
    field[:, :, :3] = np.median(rgb[mask], axis=0).astype(np.uint8)
    field[:, :, 3] = 255
    x0 = (size - shape[0]) // 2
    y0 = (size - shape[1]) // 2
    field[y0:y0 + shape[1], x0:x0 + shape[0], :3] = rgb
    return Image.fromarray(field, "RGBA"), {
        "source": str(path.resolve()), "source_bbox": bbox,
        "placed_bbox": [x0, y0, x0 + shape[0], y0 + shape[1]],
        "visible_fraction": round(float(mask.mean()), 6),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--first", type=Path, required=True)
    parser.add_argument("--second", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--size", type=int, default=2048)
    parser.add_argument("--occupancy", type=float, default=0.9)
    args = parser.parse_args()
    if not 0.4 <= args.occupancy <= 0.96:
        raise ValueError("occupancy must be between 0.4 and 0.96")
    half = args.size // 2
    views = [filled_view(p, half, args.occupancy) for p in (args.first, args.second)]
    atlas = Image.new("RGBA", (args.size, half), (0, 0, 0, 255))
    for i, (image, _) in enumerate(views):
        atlas.paste(image, (i * half, 0))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(args.out)
    report = {"atlas": str(args.out.resolve()), "width": args.size,
              "height": half, "views": [meta for _, meta in views],
              "alpha_min": int(np.asarray(atlas)[:, :, 3].min())}
    args.out.with_suffix(".json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
