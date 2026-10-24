"""Make a three-view head-only projection atlas for each enemy."""
from pathlib import Path
from shutil import copy2
import json

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt

ROOT = Path(__file__).resolve().parent
VIEWS = ("body_front", "body_profile", "body_back")
# Pixel rectangles in the individually extracted body PNGs: left, top, right, bottom.
# Each one includes the crown, jaw/helmet rim and neck root, while leaving out shoulders.
BOXES = [
    [(126, 4, 226, 104), (85, 3, 197, 108), (125, 4, 234, 101)],
    [(133, 0, 242, 106), (157, 0, 289, 111), (139, 0, 278, 96)],
    [(117, 18, 225, 125), (108, 14, 241, 123), (117, 16, 226, 114)],
    [(153, 43, 206, 112), (146, 25, 204, 103), (160, 39, 231, 114)],
    [(130, 0, 228, 116), (167, 0, 286, 119), (115, 0, 228, 119)],
    [(67, 5, 170, 139), (111, 5, 217, 138), (67, 5, 177, 141)],
    [(108, 14, 194, 117), (174, 21, 278, 127), (123, 19, 220, 120)],
    [(109, 21, 210, 126), (128, 19, 248, 125), (114, 17, 207, 116)],
    [(113, 6, 204, 135), (114, 4, 201, 137), (112, 5, 202, 133)],
    [(135, 27, 222, 134), (145, 17, 216, 115), (137, 27, 229, 130)],
]

for kind, triplet in enumerate(BOXES):
    folder = ROOT / f"Enemy_{kind}"
    atlas = Image.new("RGBA", (1152, 384))
    entries = {}
    for slot, (view, box) in enumerate(zip(VIEWS, triplet)):
        source = Image.open(folder / f"{view}.png").convert("RGBA")
        if box[2] > source.width or box[3] > source.height:
            raise ValueError((kind, view, box, source.size))
        crop = source.crop(box)
        alpha = crop.getchannel("A")
        bounds = alpha.getbbox()
        if bounds is None:
            raise ValueError((kind, view, "empty"))
        crop = crop.crop(bounds)
        scale = min(340 / crop.width, 340 / crop.height)
        size = tuple(round(v * scale) for v in crop.size)
        crop = crop.resize(size, Image.Resampling.LANCZOS)
        cell = Image.new("RGBA", (384, 384))
        px = (384 - size[0]) // 2
        py = (384 - size[1]) // 2
        cell.alpha_composite(crop, (px, py))
        tile = np.array(cell)
        seeds = tile[:, :, 3] >= 240
        if not seeds.any():
            seeds = tile[:, :, 3] >= 128
        nearest = distance_transform_edt(~seeds, return_distances=False,
                                         return_indices=True)
        tile[:, :, :3] = tile[nearest[0], nearest[1], :3]
        tile[:, :, 3] = 255
        atlas.paste(Image.fromarray(tile, "RGBA"), (slot * 384, 0))
        entries[view] = {"sourceFile": f"{view}.png", "sourceCrop": list(box),
                         "opaqueBoundsInCrop": list(bounds),
                         "atlasCell": [slot, 0],
                         "atlasContentBounds": [slot * 384 + px, py,
                                                slot * 384 + px + size[0], py + size[1]]}
    output = folder / "head-atlas-filled.png"
    atlas.save(output, optimize=True)
    runtime = ROOT.parents[1] / "Resources" / "Art" / "Enemy3DRebuild" / f"Enemy_{kind}"
    runtime.mkdir(parents=True, exist_ok=True)
    copy2(output, runtime / output.name)
    (folder / "head-projection.json").write_text(json.dumps({
        "atlas": "head-atlas-filled.png", "size": [1152, 384],
        "cellSize": [384, 384], "origin": "top-left", "views": entries,
        "note": "Enemy_3 uses its small skull; rear view is partly covered by bone." if kind == 3 else ""
    }, indent=2) + "\n", encoding="utf-8")
