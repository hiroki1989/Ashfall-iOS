"""Pack alpha-matted front/back and profile photos into four disjoint atlas tiles."""
from pathlib import Path
from PIL import Image
import numpy as np

ROOT = Path('D:/Unity/Ashfall-iOS')
OUT = ROOT / 'Assets/ArtSource/LowPolyProof/ArmRigSide'
BODY = ROOT / 'Assets/ArtSource/LowPolyProof/BodyTwoView'
OUT.mkdir(parents=True, exist_ok=True)
atlas = Image.new('RGBA', (2048, 2048), (0, 0, 0, 0))
front = Image.open(BODY / 'body-head-source-0.png').convert('RGBA').resize((1024, 1024), Image.Resampling.LANCZOS)
back = Image.open(BODY / 'body-head-source-1.png').convert('RGBA').resize((1024, 1024), Image.Resampling.LANCZOS)
atlas.alpha_composite(front, (0, 0))
atlas.alpha_composite(back, (1024, 0))

for name, source_name, x in [('left', 'right', 0), ('right', 'left', 1024)]:
    src = Image.open(OUT / f'{source_name}-source.png').convert('RGB').resize((683, 1024), Image.Resampling.LANCZOS)
    pixels = np.asarray(src).copy()
    # Estimate the studio backdrop per scanline, then create a soft alpha edge.
    edge = np.concatenate((pixels[:, :24], pixels[:, -24:]), axis=1)
    background = np.median(edge, axis=1)
    distance = np.sqrt(np.sum((pixels.astype(np.float32) - background[:, None, :]) ** 2, axis=2))
    alpha = np.clip((distance - 17.0) * 8.0, 0, 255).astype(np.uint8)
    side = Image.fromarray(np.dstack((pixels, alpha)), 'RGBA')
    atlas.alpha_composite(side, (x + 170, 1024))

atlas.save(OUT / 'plain-body-four-view-atlas.png', optimize=True)
print('SIDE_ATLAS_ALPHA_OK', OUT / 'plain-body-four-view-atlas.png')
