"""Extend each atlas tile's color independently for opaque mesh projection."""
from pathlib import Path
from shutil import copy2

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt


ROOT = Path(__file__).resolve().parent
CELL = 384
VALID_CELLS = 13

for kind in range(10):
    folder = ROOT / f"Enemy_{kind}"
    source = Image.open(folder / "projection-atlas.png").convert("RGBA")
    if source.size != (CELL * 4, CELL * 4):
        raise ValueError(f"Unexpected atlas size: {folder}: {source.size}")
    filled = np.array(source)
    for slot in range(VALID_CELLS):
        row, col = divmod(slot, 4)
        y0, x0 = row * CELL, col * CELL
        tile = filled[y0:y0 + CELL, x0:x0 + CELL]
        # Opaque interior is a stable seed; antialias colors at the edge can be
        # premultiplied with black, so extend the nearest interior color there too.
        seeds = tile[:, :, 3] >= 240
        if not seeds.any():
            raise ValueError(f"No opaque texels: Enemy_{kind} slot {slot}")
        nearest = distance_transform_edt(~seeds, return_distances=False,
                                         return_indices=True)
        colors = tile[nearest[0], nearest[1], :3].copy()
        tile[:, :, :3] = colors
        tile[:, :, 3] = 255
    output = folder / "projection-atlas-filled.png"
    Image.fromarray(filled, "RGBA").save(output, optimize=True)
    runtime = ROOT.parents[1] / "Resources" / "Art" / "Enemy3DRebuild" / f"Enemy_{kind}"
    runtime.mkdir(parents=True, exist_ok=True)
    copy2(output, runtime / output.name)
