"""Extract the existing Kestrel Harrier Patrol illustration for shape input.

This is a lossless source crop/scale preparation, not a new item design.
"""
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path(r"D:\Unity\Ashfall-iOS")
SOURCE = ROOT / "Assets/Resources/Art/GunSeries/kestrel-series-v1.png"
OUT = ROOT / "Assets/ArtSource/GeneratedEquipment/KestrelHarrierPatrol"
OUT.mkdir(parents=True, exist_ok=True)
image = Image.open(SOURCE).convert("RGBA")
# First row, first series: documented pixel bounds in kestrel-series-v1-bounds.json.
crop = image.crop((18, 29, 433, 147))
canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
scale = min(944 / crop.width, 944 / crop.height)
crop = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.Resampling.LANCZOS)
canvas.alpha_composite(crop, ((1024 - crop.width) // 2, (1024 - crop.height) // 2))
canvas.save(OUT / "kestrel-harrier-left-source.png")
ImageOps.mirror(canvas).save(OUT / "kestrel-harrier-right-mirror-for-qa.png")
print(OUT / "kestrel-harrier-left-source.png")
