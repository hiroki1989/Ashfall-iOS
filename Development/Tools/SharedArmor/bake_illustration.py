"""Bake each armor illustration onto the approved Warden UV without changing geometry.

CPU only. Run with a bounded list of product IDs; no AI texture generation.
The source art is a front illustration. Its painted lines are projected to the
front and mirrored onto the rear, with edge sampling on side walls. Rear anatomy
is an interpretation and needs game-camera review before acceptance.
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt

ROOT = Path("D:/Unity/Ashfall-iOS")
QUEUE = Path("D:/Unity/AshfallArtValidation/equipment-generation/queue.json")
UV = Path("D:/Unity/AshfallArtValidation/shared-armor/uv-position.npz")
OUT = ROOT / "Assets/Resources/Art/SharedArmorIllustration"
SIZE = 1024

# Geometry coordinates are from the approved armor-rebuilt FBX projection cache.
# Heights are CharacterRigStandard metres; x is left/right across the body.
REGIONS = {
    1: (.99, 1.84, .52),  # chest, sleeves, and waist pouches
    2: (.77, 1.18, .51),  # gauntlets
    3: (.28, 1.07, .34),  # two legs, excluding boots
    4: (.08, .48, .31),  # two boots
}


def illustration(job):
    image = Image.open(ROOT / job["source"]).convert("RGBA")
    width, height = image.size
    c = job["crop_uv"]
    bounds = (round(c["x"] * width), round((1 - c["y"] - c["height"]) * height),
              round((c["x"] + c["width"]) * width), round((1 - c["y"]) * height))
    rgba = np.asarray(image.crop(bounds))
    alpha = rgba[:, :, 3] > 64
    if not np.any(alpha):
        raise ValueError(f"Source has no opaque paint: {job['key']}")
    ys, xs = np.where(alpha)
    rgba = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    alpha = rgba[:, :, 3] > 64
    nearest = distance_transform_edt(~alpha, return_distances=False, return_indices=True)
    return rgba[nearest[0], nearest[1], :3], alpha


def chest_rear_paint(source, alpha):
    """Choose a quiet sleeve/coat material patch, omitting front badges/pouches."""
    h, w = alpha.shape
    candidates = ((.04, .34, .34, .69), (.66, .96, .34, .69),
                  (.34, .66, .58, .88), (.30, .70, .10, .34))
    best = None
    for left, right, top, bottom in candidates:
        x0, x1 = int(w * left), int(w * right)
        y0, y1 = int(h * top), int(h * bottom)
        patch = source[y0:y1, x0:x1]
        opaque = alpha[y0:y1, x0:x1]
        if patch.size == 0 or opaque.mean() < .80:
            continue
        gray = patch.astype(np.float32) @ np.array([.2126, .7152, .0722])
        gradient = np.mean(np.abs(np.diff(gray, axis=0))) + np.mean(np.abs(np.diff(gray, axis=1)))
        variance = gray.std()
        score = gradient + max(0, 8 - variance) * 2
        if best is None or score < best[0]:
            best = (score, patch)
    if best is None:
        patch = source[int(h * .35):int(h * .65), int(w * .1):int(w * .4)]
    else:
        patch = best[1]
    return np.asarray(Image.fromarray(patch).resize((w, h), Image.Resampling.BILINEAR))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="*", help="Product IDs; defaults to all non-head armor")
    parser.add_argument("--start", type=int, default=0, help="Start offset for bounded batches")
    parser.add_argument("--limit", type=int, default=0, help="Bound this invocation")
    parser.add_argument("--staging", action="store_true", help="Write outside Assets while Unity imports")
    args = parser.parse_args()
    queue = json.loads(QUEUE.read_text(encoding="utf-8"))
    jobs = {job["key"]: job for job in queue["jobs"]}
    ids = set(args.ids)
    products = [p for p in queue["products"] if p["kind"] == "armor"
                and jobs[p["jobs"][0]].get("slot", jobs[p["jobs"][0]].get("index")) in REGIONS
                and (not ids or p["id"] in ids)]
    products = products[args.start:args.start + args.limit] if args.limit else products[args.start:]
    cache = np.load(UV)
    # Cache uses 2048px matching the original approved atlas. Sample pixel centres.
    pos = cache["position"][1::2, 1::2]
    norm = cache["normal"][1::2, 1::2]
    valid = cache["valid"][1::2, 1::2]
    world_height = pos[:, :, 2] + 1
    output = Path("D:/Unity/AshfallArtValidation/shared-armor/illustration-staging") if args.staging else OUT
    output.mkdir(parents=True, exist_ok=True)
    records = []
    for product in products:
        job = jobs[product["jobs"][0]]
        slot = job.get("slot", job.get("index"))
        low, high, halfwidth = REGIONS[slot]
        source, alpha = illustration(job)
        u = np.clip((pos[:, :, 0] / halfwidth + 1) / 2, 0, 1)
        v = np.clip((high - world_height) / (high - low), 0, 1)
        sx = np.rint(u * (source.shape[1] - 1)).astype(np.int32)
        sy = np.rint(v * (source.shape[0] - 1)).astype(np.int32)
        front = source[sy, sx]
        rear_source = chest_rear_paint(source, alpha) if slot == 1 else source
        rear = rear_source[sy, source.shape[1] - sx - 1]
        # Signed normal selects the front or back source. Side faces use a
        # symmetric blend to avoid a hard seam at the silhouette.
        back_weight = np.clip((norm[:, :, 1] + .22) / .44, 0, 1).astype(np.float32)
        projected = (front * (1 - back_weight[:, :, None]) + rear * back_weight[:, :, None])
        # Orthogonal side sampling gives the sleeve, glove, trouser and boot
        # depth its own painted pixels. Without it, front/back projection
        # clamps the side silhouette to one image column and creates stripes.
        depth_u = np.clip((pos[:, :, 1] + .35) / .70, 0, 1)
        if slot in (2, 3, 4):
            depth_u = np.where(pos[:, :, 0] < 0, depth_u * .5, .5 + depth_u * .5)
        side_x = np.rint(depth_u * (source.shape[1] - 1)).astype(np.int32)
        side = source[sy, side_x]
        side_weight = np.clip((np.abs(norm[:, :, 0]) - .20) / .48, 0, .86).astype(np.float32)
        projected = projected * (1 - side_weight[:, :, None]) + side * side_weight[:, :, None]
        # Every occupied UV texel belongs to this illustration. Runtime slot
        # classification also considers skin weights, so a height-only mask can
        # leave starter art visible on a sleeve, pouch or belt. Paint the whole
        # mesh UV footprint and let the unchanged slot mesh select its islands.
        result = np.empty((SIZE, SIZE, 3), dtype=np.uint8)
        result[:] = np.median(source.reshape(-1, 3), axis=0).astype(np.uint8)
        result[valid] = np.clip(projected[valid], 0, 255).astype(np.uint8)
        # Eight-pixel UV edge bleed avoids black mip seams. No Warden atlas is
        # sampled, including on side/rear faces.
        distance, nearest = distance_transform_edt(~valid, return_indices=True)
        bleed = (~valid) & (distance <= 8)
        result[bleed] = result[nearest[0][bleed], nearest[1][bleed]]
        target = output / f"{product['id']}.png"
        Image.fromarray(result).save(target, optimize=True)
        records.append({"id": product["id"], "slot": slot, "source": job["source"],
                        "front_back": ("front illustration; rear source-material patch" if slot == 1
                                       else "front illustration; rear horizontally mirrored"),
                        "texture": (OUT / target.name).relative_to(ROOT).as_posix(),
                        "painted_pixels": int(np.count_nonzero(valid)),
                        "starter_pixels": 0})
        print(product["id"], slot, target.stat().st_size, flush=True)
    validation = Path("D:/Unity/AshfallArtValidation/shared-armor/illustration-batches")
    validation.mkdir(parents=True, exist_ok=True)
    (validation / f"batch-{args.start:03}.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
    print("COUNT", len(records))


if __name__ == "__main__":
    main()
