"""Extract projection references from the preserved LayeredEnemies sheets."""
from pathlib import Path
from PIL import Image
import numpy as np
from scipy import ndimage
import json

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parents[1] / "Resources" / "Art" / "LayeredEnemies"
TILES = {
    "body_front": (0, 0),
    "body_front_oblique": (3, 0),
    "body_profile": (2, 1),
    "body_rear_oblique": (1, 2),
    "body_back": (0, 3),
    "leg_front_L": (1, 0),
    "leg_front_R": (2, 0),
    "leg_side_A_L": (0, 1),
    "leg_side_A_R": (3, 1),
    "leg_side_B_L": (0, 2),
    "leg_side_B_R": (3, 2),
    "leg_back_L": (1, 3),
    "leg_back_R": (2, 3),
}
ATLAS_ORDER = list(TILES)
CELL = 384

for index in range(10):
    source_path = SOURCE / f"Enemy_{index}.png"
    sheet = Image.open(source_path).convert("RGBA")
    out = ROOT / f"Enemy_{index}"
    out.mkdir(parents=True, exist_ok=True)
    records = {}
    for name, (col, row) in TILES.items():
        # Rounding cumulative boundaries keeps non-square sheets and all pixels intact.
        x0 = round(col * sheet.width / 4)
        x1 = round((col + 1) * sheet.width / 4)
        y0 = round(row * sheet.height / 4)
        y1 = round((row + 1) * sheet.height / 4)
        # Torso arms extend across nominal cell lines on the original sheet.
        if name.startswith("body_"):
            x0 = max(0, x0 - 92)
            x1 = min(sheet.width, x1 + 92)
        box = (x0, y0, x1, y1)
        tile = sheet.crop(box)
        pixels = np.array(tile)
        labels, count = ndimage.label(pixels[:, :, 3] > 24,
                                       structure=np.ones((3, 3), dtype=np.uint8))
        if count:
            sizes = np.bincount(labels.ravel())
            sizes[0] = 0
            main = labels == sizes.argmax()
            # Keep anti-aliased fringe, but discard pieces from adjacent cells.
            mask = ndimage.binary_dilation(main, iterations=2)
            pixels[~mask] = 0
            tile = Image.fromarray(pixels, "RGBA")
        alpha = tile.getchannel("A")
        bbox = alpha.getbbox()
        if bbox is None:
            raise ValueError(f"Empty tile {source_path}: {name}")
        # Remove transparent black and keep 8 transparent pixels for sampling safety.
        detail = tile.crop(bbox)
        canvas = Image.new("RGBA", (detail.width + 16, detail.height + 16))
        canvas.alpha_composite(detail, (8, 8))
        target = out / f"{name}.png"
        canvas.save(target, optimize=True)
        records[name] = {"file": target.name, "sourceCell": [col, row],
                         "sourceBounds": list(box), "opaqueBoundsInCell": list(bbox),
                         "size": list(canvas.size)}
    atlas = Image.new("RGBA", (CELL * 4, CELL * 4))
    for slot, name in enumerate(ATLAS_ORDER):
        tile = Image.open(out / records[name]["file"])
        # Preserve original aspect ratio and leave room for bilinear sampling.
        scale = min((CELL - 24) / tile.width, (CELL - 24) / tile.height)
        size = (round(tile.width * scale), round(tile.height * scale))
        tile = tile.resize(size, Image.Resampling.LANCZOS)
        x = (slot % 4) * CELL + (CELL - size[0]) // 2
        y = (slot // 4) * CELL + (CELL - size[1]) // 2
        atlas.alpha_composite(tile, (x, y))
        records[name]["atlasCell"] = [slot % 4, slot // 4]
        records[name]["atlasContentBounds"] = [x, y, x + size[0], y + size[1]]
    atlas.save(out / "projection-atlas.png", optimize=True)
    (out / "projection.json").write_text(json.dumps({
        "source": str(source_path.relative_to(ROOT.parents[1])).replace("\\", "/"),
        "layout": "4 columns x 4 rows; pixels top-left origin",
        "atlas": {"file": "projection-atlas.png", "size": [CELL * 4, CELL * 4],
                  "cellSize": [CELL, CELL], "origin": "top-left"},
        "tiles": records,
    }, indent=2) + "\n", encoding="utf-8")
