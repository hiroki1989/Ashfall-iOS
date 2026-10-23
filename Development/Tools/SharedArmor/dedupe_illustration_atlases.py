"""Keep one Resources PNG per identical illustration and map all catalog IDs."""
import hashlib
import json
import re
import uuid
from pathlib import Path

ROOT = Path("D:/Unity/Ashfall-iOS")
ATLAS = ROOT / "Assets/Resources/Art/SharedArmorIllustration"
STAGE = Path("D:/Unity/AshfallArtValidation/shared-armor/illustration-staging")
QUEUE = Path("D:/Unity/AshfallArtValidation/equipment-generation/queue.json")
CATALOG = ATLAS / "catalog.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    queue = json.loads(QUEUE.read_text(encoding="utf-8"))
    jobs = {j["key"]: j for j in queue["jobs"]}
    ids = sorted(p["id"] for p in queue["products"] if p["kind"] == "armor"
                 and jobs[p["jobs"][0]].get("slot", jobs[p["jobs"][0]].get("index")) in (1, 2, 3, 4))
    if len(ids) != 398:
        raise RuntimeError(f"Expected 398 non-head products, got {len(ids)}")
    grouped = {}
    for product_id in ids:
        name = product_id + ".png"
        current, backup = ATLAS / name, STAGE / name
        if not current.is_file() or not backup.is_file():
            raise FileNotFoundError(name)
        fingerprint = sha(current)
        if fingerprint != sha(backup):
            raise RuntimeError(f"D backup mismatch: {name}")
        if not (ATLAS / (name + ".meta")).is_file():
            raise FileNotFoundError(name + ".meta")
        grouped.setdefault(fingerprint, []).append(product_id)
    if len(grouped) != 168:
        raise RuntimeError(f"Expected 168 unique source atlases, got {len(grouped)}")
    canonical = {product_id: min(group) for group in grouped.values() for product_id in group}
    entries = [{"id": product_id, "atlas": canonical[product_id]} for product_id in ids]
    CATALOG.write_text(json.dumps({"entries": entries}, separators=(",", ":")), encoding="utf-8")
    meta = CATALOG.with_name(CATALOG.name + ".meta")
    if not meta.exists():
        template = (ROOT / "Assets/Resources/Art/GroundedEquipment/kestrel-armor-parts-v1-bounds.json.meta").read_text(encoding="utf-8")
        template = re.sub(r"(?m)^guid: [0-9a-f]{32}$", f"guid: {uuid.uuid4().hex}", template)
        meta.write_text(template, encoding="utf-8")
    duplicates = [product_id for product_id in ids if canonical[product_id] != product_id]
    for product_id in duplicates:
        (ATLAS / (product_id + ".png")).unlink()
        (ATLAS / (product_id + ".png.meta")).unlink()
    remaining = sorted(p.stem for p in ATLAS.glob("*.png"))
    expected = sorted(set(canonical.values()))
    if remaining != expected or len(remaining) != 168:
        raise RuntimeError("Canonical atlas verification failed")
    if len(json.loads(CATALOG.read_text(encoding="utf-8"))["entries"]) != 398:
        raise RuntimeError("Catalog coverage verification failed")
    print(f"products={len(ids)} canonical={len(remaining)} deduped={len(duplicates)} "
          f"stage={len(list(STAGE.glob('*.png')))} "
          f"resource_mb={sum(p.stat().st_size for p in ATLAS.glob('*.png'))/1e6:.2f}")


if __name__ == "__main__":
    main()
