"""Refine the plain-clothes proof using front and back source images only.

This variant avoids the old front-only wrap around almost the entire side.
The source images, shape and render settings remain identical to the earlier
two-view proof, and the result is an isolated candidate.
"""
from pathlib import Path

source = Path(__file__).with_name("two_view_base_full.py").read_text(encoding="utf-8")
source = source.replace('OUT = ROOT / "Assets/ArtSource/LowPolyProof/BodyTwoView"',
                        'OUT = ROOT / "Assets/ArtSource/LowPolyProof/BodyTwoViewRefined"')
source = source.replace('side = int(center.y >= lo.y + (hi.y - lo.y) * .85)',
                        'side = int(center.y >= lo.y + (hi.y - lo.y) * .50)')
source = source.replace('if COMPONENT_WRAP and sidewall:',
                        'if sidewall:')
source = source.replace('near, far = regions[anatomical_region(center)]',
                        'near, far = lo.y, hi.y')
source = source.replace('width = 46 if anatomical_region(center) in ("arm", "thigh", "boot") else 28',
                        'width = 18')
exec(compile(source, str(Path(__file__).with_name("two_view_base_full.py")), "exec"))
