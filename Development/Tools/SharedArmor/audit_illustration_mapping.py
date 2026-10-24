"""Read-only check of non-head product, illustration and runtime atlas mapping."""
import hashlib
import json
from collections import defaultdict
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
QUEUE = Path('D:/Unity/AshfallArtValidation/equipment-generation/queue.json')
CATALOG = ROOT / 'Assets/Resources/Art/SharedArmorIllustration/catalog.json'
ATLAS = ROOT / 'Assets/StreamingAssets/SharedArmorIllustration'


def source_crop(job):
    image = Image.open(ROOT / job['source']).convert('RGBA')
    width, height = image.size
    c = job['crop_uv']
    bounds = (round(c['x'] * width), round((1 - c['y'] - c['height']) * height),
              round((c['x'] + c['width']) * width), round((1 - c['y']) * height))
    crop = image.crop(bounds)
    alpha = crop.getchannel('A')
    return hashlib.sha256(crop.tobytes()).hexdigest(), alpha.getbbox()


def main():
    queue = json.loads(QUEUE.read_text(encoding='utf-8'))
    jobs = {job['key']: job for job in queue['jobs']}
    products = {p['id']: p for p in queue['products'] if p['kind'] == 'armor'
                and jobs[p['jobs'][0]].get('slot', jobs[p['jobs'][0]].get('index')) in (1, 2, 3, 4)}
    entries = json.loads(CATALOG.read_text(encoding='utf-8'))['entries']
    mapping = {entry['id']: entry['atlas'] for entry in entries}
    problems = []
    if len(entries) != len(mapping):
        problems.append('duplicate catalog IDs')
    if set(mapping) != set(products):
        problems.append(f'ID coverage missing={sorted(set(products)-set(mapping))} extra={sorted(set(mapping)-set(products))}')
    groups = defaultdict(list)
    for product_id, product in products.items():
        job = jobs[product['jobs'][0]]
        crop_hash, alpha_box = source_crop(job)
        if alpha_box is None:
            problems.append(f'empty source crop: {product_id}')
        groups[mapping.get(product_id)].append((product_id, crop_hash, job['source']))
    for atlas_name, group in groups.items():
        path = ATLAS / f'{atlas_name}.png'
        if not path.exists():
            problems.append(f'missing atlas: {atlas_name}')
            continue
        with Image.open(path) as image:
            expected_size = (2048, 2048) if atlas_name == 'distinct.armor.006' else (1024, 1024)
            if image.size != expected_size:
                problems.append(f'wrong atlas dimensions: {atlas_name} {image.size}')
        if len({row[1] for row in group}) != 1:
            problems.append(f'different source crops share {atlas_name}: {group}')
    actual = {path.stem for path in ATLAS.glob('*.png')}
    if actual != set(groups):
        problems.append(f'atlas file mismatch missing={sorted(set(groups)-actual)} extra={sorted(actual-set(groups))}')
    print(f'products={len(products)} entries={len(entries)} atlases={len(groups)} files={len(actual)} problems={len(problems)}')
    for problem in problems:
        print(problem)
    return bool(problems)


if __name__ == '__main__':
    raise SystemExit(main())
