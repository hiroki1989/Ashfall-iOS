"""Build light, genuinely extruded firearm silhouettes from the approved paintings.

One mesh is shared by the four editions of a painted series. The source alpha,
not the old procedural gun, defines the outline and holes. Processing is
streamed one painting at a time so it remains safe on the 16 GB workstation.
"""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageChops

ROOT = Path(__file__).resolve().parents[3]
ATLAS = ROOT / "Assets/StreamingAssets/WeaponFourView/Atlases"
OUTPUT = ROOT / "Assets/Resources/GunSilhouettes"


def source_crop(info):
    source = ROOT / info["sourceFile"]
    bounds = json.loads(source.with_name(source.stem + "-bounds.json").read_text(encoding="utf-8"))
    cell = info["sourceCell"]
    with Image.open(source) as image:
        image = image.convert("RGBA")
        if info["sourceKind"] == "GunSeries":
            rect = tuple(bounds["pixelBoundsTopLeft"][cell])
        else:
            b = bounds["cells"][cell]
            w, h = image.size
            rect = (round(b["x"] * w), round((1 - b["y"] - b["height"]) * h),
                    round((b["x"] + b["width"]) * w), round((1 - b["y"]) * h))
        crop = image.crop(rect)
        if info["sourceKind"] in ("GunSeries", "BespokeLegendaries"):
            b = bounds["cells"][cell]
            left, top, _, _ = rect
            stencil = Image.new("L", crop.size)
            draw = ImageDraw.Draw(stencil)
            points = [(round(p["x"] * image.width) - left,
                       round((1 - p["y"]) * image.height) - top) for p in b.get("hull", [])]
            if len(points) >= 3:
                draw.polygon(points, fill=255)
            else:
                draw.rectangle((0, 0, crop.width, crop.height), fill=255)
            for box in b.get("exclusions", []):
                x0 = round(box["x"] * image.width) - left
                y0 = round((1 - box["y"] - box["height"]) * image.height) - top
                x1 = round((box["x"] + box["width"]) * image.width) - left
                y1 = round((1 - box["y"]) * image.height) - top
                draw.rectangle((x0, y0, x1, y1), fill=0)
            crop.putalpha(ImageChops.multiply(crop.getchannel("A"), stencil))
        return crop


def mesh_text(mask):
    bounds = mask.getbbox()
    if not bounds:
        raise ValueError("painting has no opaque silhouette")
    mask = mask.crop(bounds)
    source_width, source_height = mask.size
    columns = 128
    rows = max(12, min(96, round(columns * source_height / source_width)))
    mask = mask.resize((columns, rows), Image.Resampling.BOX)
    opaque = [[mask.getpixel((x, y)) >= 96 for x in range(columns)] for y in range(rows)]
    visited = set()
    groups = []
    for y in range(rows):
        for x in range(columns):
            if not opaque[y][x] or (x, y) in visited:
                continue
            group = []
            queue = [(x, y)]
            visited.add((x, y))
            while queue:
                px, py = queue.pop()
                group.append((px, py))
                for nx, ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1),
                               (px-1,py-1),(px+1,py-1),(px-1,py+1),(px+1,py+1)):
                    if 0 <= nx < columns and 0 <= ny < rows and opaque[ny][nx] and (nx,ny) not in visited:
                        visited.add((nx,ny))
                        queue.append((nx,ny))
            groups.append(group)
    # The paintings sometimes contain a display shadow or a second decorative
    # gun beneath the actual product.  Those are useful in the inventory icon,
    # but must never become disconnected, floating geometry in the held gun.
    main = max(groups, key=len)
    main_bottom = max(y for _, y in main)
    keep = max(8, round(len(main) * .015))
    for group in groups:
        # A second painted handgun can slightly overlap the main one's
        # bounding Y range while still being a separate illustration.
        low_decoration = (group is not main and
                          min(y for _, y in group) >= main_bottom - 4 and
                          max(x for x, _ in group) - min(x for x, _ in group) > columns * .25)
        detached_below = group is not main and (min(y for _, y in group) > main_bottom or low_decoration)
        if len(group) < keep or detached_below:
            for x, y in group:
                opaque[y][x] = False
    ratio = source_height / source_width
    vertices = []
    faces = []

    def quad(a, b, c, d):
        start = len(vertices) + 1
        vertices.extend((a, b, c, d))
        faces.append((start, start + 1, start + 2, start + 3))

    def top(y):
        return (.5 - y / rows) * ratio

    half_thickness = .037
    for y in range(rows):
        x = 0
        while x < columns:
            if not opaque[y][x]:
                x += 1
                continue
            first = x
            while x < columns and opaque[y][x]:
                x += 1
            start, end = first / columns, x / columns
            upper, lower = top(y), top(y + 1)
            quad((half_thickness, lower, start), (half_thickness, upper, start),
                 (half_thickness, upper, end), (half_thickness, lower, end))
            quad((-half_thickness, lower, end), (-half_thickness, upper, end),
                 (-half_thickness, upper, start), (-half_thickness, lower, start))
        for x in range(columns):
            if not opaque[y][x]:
                continue
            left, right = x / columns, (x + 1) / columns
            upper, lower = top(y), top(y + 1)
            if y == 0 or not opaque[y - 1][x]:
                quad((-half_thickness, upper, left), (-half_thickness, upper, right),
                     (half_thickness, upper, right), (half_thickness, upper, left))
            if y == rows - 1 or not opaque[y + 1][x]:
                quad((-half_thickness, lower, right), (-half_thickness, lower, left),
                     (half_thickness, lower, left), (half_thickness, lower, right))
            if x == 0 or not opaque[y][x - 1]:
                quad((-half_thickness, lower, left), (-half_thickness, upper, left),
                     (half_thickness, upper, left), (half_thickness, lower, left))
            if x == columns - 1 or not opaque[y][x + 1]:
                quad((half_thickness, lower, right), (half_thickness, upper, right),
                     (-half_thickness, upper, right), (-half_thickness, lower, right))
    lines = ["# Extruded from approved firearm illustration alpha", "o Painted_firearm_silhouette"]
    lines.extend(f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in vertices)
    lines.extend("f " + " ".join(str(index) for index in face) for face in faces)
    return "\n".join(lines) + "\n", len(faces)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--prefix", default="", help="Generate just this atlas basename prefix")
    parser.add_argument("--regular-only", action="store_true", help="Keep bespoke legendary models separate")
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    made = 0
    for path in sorted(ATLAS.glob("*.json")):
        if not path.stem.startswith(args.prefix):
            continue
        info = json.loads(path.read_text(encoding="utf-8"))
        if args.regular_only and info["sourceKind"] not in ("GunSeries", "GunSeriesExpanded"):
            continue
        text, _ = mesh_text(source_crop(info).getchannel("A"))
        (OUTPUT / (path.stem + ".obj")).write_text(text, encoding="ascii")
        made += 1
    print(f"generated {made} silhouette meshes in {OUTPUT}")


if __name__ == "__main__":
    main()
