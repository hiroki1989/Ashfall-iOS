"""Register the six reviewed transparent chest/boot illustration sheets.

The artwork itself is copied byte-for-byte. This script only measures opaque
cell bounds for Unity's portrait compositor; it never paints or recolors art.
"""

import json
import shutil
from pathlib import Path

from PIL import Image


SOURCE = Path(r"C:\Users\hiroki\.codex\generated_images\01a16864-aeda-7303-a4ff-89e624f0d62f")
TARGET = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/DistinctEquipment"
SHEETS = {
    "kestrel": "exec-ac5570e5-98ee-4092-a3f9-c42f54d24c0c.png",
    "ironwake": "exec-73afe5f4-7150-4008-a0cf-c8d2a3d9d5bb.png",
    "voltarc": "exec-b78d52e8-0a57-40d8-9f58-703f4d07a08c.png",
    "mireline": "exec-635700ef-fbe3-45e4-aa9e-d6ece5dc4230.png",
    "cinder": "exec-d27c3f09-a8d9-4a99-b067-babddcedbd94.png",
    "aegis": "exec-227d9d37-a8aa-405c-b51d-cfcf9f47d943.png",
}


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    for maker, filename in SHEETS.items():
        source = SOURCE / filename
        if not source.exists():
            raise FileNotFoundError(source)
        image = Image.open(source).convert("RGBA")
        width, height = image.size
        cells = []
        for row in range(2):
            for col in range(6):
                x0, x1 = col * width // 6, (col + 1) * width // 6
                y0, y1 = row * height // 2, (row + 1) * height // 2
                alpha = image.crop((x0, y0, x1, y1)).getchannel("A")
                bounds = alpha.point(lambda value: 255 if value > 24 else 0).getbbox()
                if bounds is None:
                    raise ValueError(f"Empty illustration: {maker} row={row} col={col}")
                left, top, right, bottom = bounds
                left += x0
                right += x0
                top += y0
                bottom += y0
                cells.append({
                    "x": left / width,
                    "y": 1 - bottom / height,
                    "width": (right - left) / width,
                    "height": (bottom - top) / height,
                })
        base = TARGET / f"{maker}-armor-chest-boots-v2"
        shutil.copy2(source, base.with_suffix(".png"))
        base.with_name(base.name + "-bounds.json").write_text(
            json.dumps({"cells": cells}, ensure_ascii=False, separators=(",", ":")),
            encoding="utf-8",
        )
        print(maker, image.size, "cells", len(cells))


if __name__ == "__main__":
    main()
