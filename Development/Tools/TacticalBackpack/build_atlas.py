"""Pack the four hand-authored direction sources into a Unity texture atlas."""

from pathlib import Path
from shutil import copyfile

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt


ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / "Assets/Resources/Art/TacticalBackpack"
CELL = 1024
MARGIN = 30
BLEED = 8
ORDER = (
    "pack-front.png",  # wearer-facing harness
    "pack-back.png",   # outward-facing storage face
    "pack-left.png",
    "pack-right.png",  # empty muzzle-up rifle retention rail
)


def pack_cell(path: Path) -> Image.Image:
    source = Image.open(path).convert("RGBA")
    alpha = np.asarray(source.getchannel("A"))
    ys, xs = np.where(alpha > 16)
    if not len(xs):
        raise ValueError(f"No visible pixels in {path}")
    source = source.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    scale = min((CELL - 2 * MARGIN) / source.width,
                (CELL - 2 * MARGIN) / source.height)
    size = (round(source.width * scale), round(source.height * scale))
    source = source.resize(size, Image.Resampling.LANCZOS)
    cell = Image.new("RGBA", (CELL, CELL))
    cell.alpha_composite(source, ((CELL - size[0]) // 2, (CELL - size[1]) // 2))

    # Color padding beneath transparent pixels prevents dark seams at UV edges.
    rgba = np.array(cell)
    opaque = rgba[:, :, 3] > 8
    distance, nearest = distance_transform_edt(~opaque, return_indices=True)
    # Fill off-silhouette texels with the canvas median. Only the immediate
    # eight-pixel fringe borrows its nearest neighbor; extending every edge
    # indefinitely produces distracting radial streaks on the 3D side walls.
    cloth = np.median(rgba[opaque, :3], axis=0).astype(np.uint8)
    missing = ~opaque
    rgba[missing, :3] = cloth
    fringe = missing & (distance <= BLEED)
    rgba[fringe, :3] = rgba[nearest[0][fringe], nearest[1][fringe], :3]
    rgba[:, :, 3] = 255
    return Image.fromarray(rgba, "RGBA")


def main() -> None:
    atlas = Image.new("RGBA", (CELL * 2, CELL * 2))
    for index, name in enumerate(ORDER):
        atlas.paste(pack_cell(ART / name), ((index % 2) * CELL,
                                            (index // 2) * CELL))
    atlas.save(ART / "pack-atlas.png", optimize=True)
    # Runtime Resources.Load uses the established flat Art directory.
    copyfile(ART / "pack-atlas.png", ART.parent / "TacticalBackpackAtlas.png")


if __name__ == "__main__":
    main()
