import json
from pathlib import Path
from PIL import Image

ROOT = Path('D:/Unity/Ashfall-iOS')
OUT = Path('D:/Unity/AshfallArtValidation/equipment3d-batch')
OUT.mkdir(parents=True, exist_ok=True)
classes = ['assault', 'handgun', 'shotgun', 'lmg', 'smg', 'sniper']
sheet = ROOT / 'Assets/Resources/Art/GunSeries/kestrel-series-v1.png'
meta = json.loads((ROOT / 'Assets/Resources/Art/GunSeries/kestrel-series-v1-bounds.json').read_text())
im = Image.open(sheet).convert('RGBA')
report=[]
for row, name in enumerate(classes):
    box=meta['pixelBoundsTopLeft'][row*4]
    crop=im.crop(tuple(box))
    crop.save(OUT / f'kestrel-{name}-series0.png')
    report.append({'class':name,'series':0,'architecture':['patrol rifle','compact pistol','tubular pump','belt box LMG','folding compact SMG','wood stock bolt sniper'][row], 'source':str(sheet),'source_box':box,'crop':f'kestrel-{name}-series0.png'})
(OUT/'representatives.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
