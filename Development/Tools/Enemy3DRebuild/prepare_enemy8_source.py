"""Assemble the preserved enemy painting into a single shape-only input."""
from pathlib import Path
from PIL import Image
import json

repo = Path(__file__).resolve().parents[3]
source = repo / 'Assets/ArtSource/Enemy3DRebuild/Enemy_8'
target = repo / 'Assets/ArtSource/Enemy3DRebuild/NewMesh/Enemy_8'
target.mkdir(parents=True, exist_ok=True)
canvas = Image.new('RGBA', (512, 512))
placements = [
    ('leg_front_L.png', (171, 242, 282, 471)),
    ('leg_front_R.png', (231, 242, 342, 471)),
    ('body_front.png', (105, 22, 407, 329)),
]
for name, box in placements:
    part = Image.open(source / name).convert('RGBA')
    part = part.resize((box[2] - box[0], box[3] - box[1]), Image.Resampling.LANCZOS)
    canvas.alpha_composite(part, (box[0], box[1]))
canvas.save(target / 'source-front.png')
(target / 'source-layout.json').write_text(json.dumps({
    'original': 'Assets/Resources/Art/LayeredEnemies/Enemy_8.png',
    'canvas': [512, 512], 'placements': placements,
    'purpose': 'Hunyuan3D-2mini shape only; no AI texture',
}, indent=2), encoding='utf-8')
