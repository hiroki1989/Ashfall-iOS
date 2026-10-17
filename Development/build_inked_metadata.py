"""Measure native InkedArt atlas alpha; writes JSON only, never image pixels."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image
from scipy.ndimage import label

ROOT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Art/LayeredPlayer/InkedArt'
DIRECTIONS = ('Front', 'FrontRight', 'Right', 'BackRight', 'Back')
# Measured narrow joins between vertically adjacent native armor parts.
# Bounds affect metadata only. FrontRight torso/knee has a short overlapping
# outline at y948; this cut preserves their separate identity.
ARMOR_Y = {
    0: {9: (0, 945), 13: (945, 1254)},
    1: {0: (0, 326), 4: (326, 1254), 5: (0, 626),
        9: (626, 948), 13: (948, 1254), 8: (0, 942),
        12: (942, 1254), 11: (0, 963), 15: (963, 1254)},
    2: {9: (0, 931), 13: (931, 1254)},
    4: {1: (0, 313), 5: (313, 1254), 8: (0, 911),
        12: (911, 1254), 9: (0, 912), 13: (912, 1254)},
}


def measure(kind, cells):
    regions, reports, warnings = [], [], []
    paths = [ROOT / f'{kind}_{name}.png' for name in DIRECTIONS]
    missing = [p.name for p in paths if not p.exists()]
    if missing:
        print(f'{kind}: pending missing atlases: {", ".join(missing)}; no metadata written')
        return
    for direction, path in enumerate(paths):
        before = hashlib.sha256(path.read_bytes()).hexdigest()
        with Image.open(path) as image:
            alpha = np.asarray(image.convert('RGBA'))[:, :, 3]
        height, width = alpha.shape
        opaque = alpha > 96
        labels, count = label(opaque, structure=np.ones((3, 3)))
        component_sizes = np.bincount(labels.ravel())
        selected = {}
        report = dict(file=path.name, width=width, height=height,
                      transparentFraction=round(float(np.mean(alpha == 0)), 6),
                      sha256=before, parts=[])
        for cell in range(cells):
            left, right = round(cell % 4 * width / 4), round((cell % 4 + 1) * width / 4)
            top, bottom = round(cell // 4 * height / 4), round((cell // 4 + 1) * height / 4)
            sizes = np.bincount(labels[top:bottom, left:right].ravel(), minlength=count+1); sizes[0] = 0
            if count == 0 or sizes.max() < 64:
                raise ValueError(f'{path.name} cell {cell}: missing/tiny main component')
            component = int(sizes.argmax())
            body_split = kind == 'Body' and direction == 3 and cell in (6, 10)
            armor_split = ARMOR_Y.get(direction, {}).get(cell) if kind == 'Armor' else None
            known_split = body_split or armor_split is not None
            if component in selected and not known_split:
                warnings.append(f'{path.name} cells {selected[component]}/{cell}: duplicate component {component}; parts may be joined')
            selected[component] = cell
            yy, xx = np.where(labels == component)
            # Native BackRight sleeve and shin touch. Split measurement only;
            # the original atlas bytes and alpha remain untouched.
            if body_split:
                belongs = (xx >= left) & (xx < right)
                belongs &= yy < 628 if cell == 6 else yy >= 628
                yy, xx = yy[belongs], xx[belongs]
            if armor_split:
                belongs = (yy >= armor_split[0]) & (yy < armor_split[1])
                yy, xx = yy[belongs], xx[belongs]
            x0, x1 = int(xx.min()), int(xx.max()) + 1
            y0, y1 = int(yy.min()), int(yy.max()) + 1
            trimmed = 0
            if kind == 'Armor':
                # Rectangular sprites cannot exclude another part whose tip
                # enters the bounding rectangle. Trim the cheapest rectangle
                # edge (fewest own opaque pixels), never alter native alpha.
                original_count = len(xx)
                for _ in range(16):
                    sample = labels[y0:y1, x0:x1]
                    ids = np.unique(sample)
                    foreign = [int(i) for i in ids if i not in (0, component) and component_sizes[i] > 500]
                    if not foreign:
                        break
                    fy, fx = np.where(sample == foreign[0]); fx += x0; fy += y0
                    choices = [(int(fx.max())+1,y0,x1,y1), (x0,y0,int(fx.min()),y1),
                               (x0,int(fy.max())+1,x1,y1), (x0,y0,x1,int(fy.min()))]
                    valid = [(a,b,c,d) for a,b,c,d in choices if a<c and b<d]
                    best = max(valid, key=lambda r: int(np.sum((xx>=r[0])&(yy>=r[1])&(xx<r[2])&(yy<r[3]))))
                    x0,y0,x1,y1=best
                retain=(xx>=x0)&(xx<x1)&(yy>=y0)&(yy<y1)
                yy,xx=yy[retain],xx[retain]
                trimmed=original_count-len(xx)
                remaining = np.unique(labels[y0:y1,x0:x1])
                foreign = [int(i) for i in remaining if i not in (0,component) and component_sizes[i]>500]
                if foreign:
                    raise ValueError(f'{path.name} cell {cell}: neighboring parts remain: {foreign}')
                if trimmed > original_count*.03:
                    warnings.append(f'{path.name} cell {cell}: neighbor exclusion trims {trimmed/original_count:.1%} of own alpha')
            overlap = float(np.mean((xx >= left) & (xx < right) & (yy >= top) & (yy < bottom)))
            if overlap < .75 or not (left <= float(xx.mean()) < right and top <= float(yy.mean()) < bottom):
                warnings.append(f'{path.name} cell {cell}: only {overlap:.1%} of component in target cell; inspect joined/neighbor part')
            regions.append(dict(direction=direction, cell=cell,
                                x=x0/width, y=y0/height,
                                width=(x1-x0)/width, height=(y1-y0)/height))
            report['parts'].append(dict(cell=cell, pixels=len(xx), component=component,
                                        cellOverlap=round(overlap, 6),
                                        metadataSplitY=628 if body_split else armor_split,
                                        neighborExclusionPixels=trimmed,
                                        bbox=[x0, y0, x1-x0, y1-y0]))
        assert hashlib.sha256(path.read_bytes()).hexdigest() == before
        reports.append(report)
        print(f'{path.name}: {width}x{height}, transparent {report["transparentFraction"]:.2%}, {cells} parts')
    result = dict(regions=regions, sourceOrigin='top-left', alphaThreshold=96,
                  componentConnectivity=8, selection='largest-cell-overlap-full-atlas-component',
                  measurements=reports, warnings=warnings)
    (ROOT / f'{kind}Regions.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(f'{kind}: {len(regions)} regions, {len(warnings)} component warnings; source hashes unchanged')
    for warning in warnings:
        print('WARNING:', warning)


if __name__ == '__main__':
    import sys
    if len(sys.argv) == 1 or 'Body' in sys.argv[1:]:
        measure('Body', 15)
    if len(sys.argv) == 1 or 'Armor' in sys.argv[1:]:
        measure('Armor', 16)
