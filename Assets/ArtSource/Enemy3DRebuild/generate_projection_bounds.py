"""Bake each enemy projection tile's authored image bounds into runtime C#.

Run after build_projection_sources.py whenever the source sheets change.
The filled margins are for seam safety, not for mapping to visible anatomy.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT = ROOT.parents[1] / "Scripts" / "EnemyProjectionBounds.cs"

rows = []
for kind in range(10):
    data = json.loads((ROOT / f"Enemy_{kind}" / "projection.json").read_text(encoding="utf-8"))
    cells = [(0.03, 0.03, 0.97, 0.97)] * 16
    for tile in data["tiles"].values():
        col, row = tile["atlasCell"]
        x0, y0, x1, y1 = tile["atlasContentBounds"]
        cells[row * 4 + col] = (
            (x0 - col * 384) / 384,
            1 - (y1 - row * 384) / 384,
            (x1 - col * 384) / 384,
            1 - (y0 - row * 384) / 384,
        )
    rows.append(cells)

lines = [
    "// Generated from Assets/ArtSource/Enemy3DRebuild/Enemy_*/projection.json.",
    "// Values are local tile UV minX, minY, maxX, maxY (Unity bottom-left origin).",
    "using UnityEngine;",
    "namespace NeonVault",
    "{",
    "    internal static class EnemyProjectionBounds",
    "    {",
    "        internal static readonly Vector4[,] Tiles = new Vector4[10,16]",
    "        {",
]
for kind, cells in enumerate(rows):
    lines.append(f"            {{ // Enemy {kind}")
    for col, (x0, y0, x1, y1) in enumerate(cells):
        comma = "," if col != 15 else ""
        lines.append(f"                new Vector4({x0:.6f}f,{y0:.6f}f,{x1:.6f}f,{y1:.6f}f){comma}")
    lines.append("            }," if kind != 9 else "            }")
lines += ["        };", "    }", "}"]
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(OUT)
