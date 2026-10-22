"""Inventory exact authored meshes; procedural permutations do not count as art.

Run with the project's bundled Python or `py -3 audit_equipment_3d.py`.
The report lives outside Assets so it cannot be mistaken for a shippable item.
"""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RESOURCE = ROOT / "Assets/Resources/Art/GeneratedEquipment"
REPORT = Path("D:/Unity/AshfallArtValidation/equipment-3d-coverage.json")


def exact_product(kind: str, number: int) -> dict:
    catalog_id = f"distinct.{kind}.{number:03d}"
    directory = RESOURCE / kind / catalog_id
    models = [p.name for p in directory.glob("mesh.*") if p.suffix.lower() in {".fbx", ".obj", ".glb"}]
    atlas = (directory / "atlas.png").is_file()
    return {
        "id": catalog_id,
        "model": models[0] if len(models) == 1 else None,
        "atlas": atlas,
        "ready": len(models) == 1 and atlas,
    }


def main() -> None:
    products = {
        "gun": [exact_product("gun", i) for i in range(606)],
        "armor": [exact_product("armor", i) for i in range(500)],
    }
    summary = {
        kind: {"total": len(rows), "authored_ready": sum(row["ready"] for row in rows)}
        for kind, rows in products.items()
    }
    shared_root = ROOT / "Assets/Resources/Art/GeneratedWarden"
    shared = {
        name: {
            "model": (shared_root / f"{name}-mobile.fbx").is_file(),
            "atlas": (shared_root / f"{name}-mobile-atlas.png").is_file(),
        }
        for name in ("body", "armor", "mask", "rifle", "sidearm")
    }
    output = {
        "scope": "product-specific Resources mesh + atlas; Warden shared assets are listed separately and procedural meshes do not count",
        "summary": summary,
        "approved_shared_warden": shared,
        "products": products,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))
    print(REPORT)


if __name__ == "__main__":
    main()
