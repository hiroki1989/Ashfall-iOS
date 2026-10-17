# Multicolor integration

Originals remain in Assets/Resources/Art/LayeredPlayer and the image generator original directory. New images are byte copies under ColorArt, not Python-edited rasters. The PMO filled the three missing color-only edits after the two Art agents stopped at usage limits.

ArmorV3_Back: target ArmorV2_Back, supporting color reference ArmorV3_Front. Original C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-7c06142e-af12-4b8f-9d97-bba9cd375bbe.png.
Prompt: precise-object-edit, preserve exact16 cells/rear view/geometry/alpha/size. Copy material colors from new front: silver steel and black seams, olive cloth top set/navy and restrained crimson bottom set, brown straps/pouches, copper/brass bolts, small orange/teal top paint, cyan/amber glass. Add original hand-painted cel interior ink/bevel shadows, no new front-face details, no monochrome tint/rainbow/text/grid/background.

WeaponsV3_4: target Weapons_4, style/material reference WeaponsV3_0. Original exec-f7f3f242-43bb-4eff-a9e3-5e7de2f7377a.png in same generator directory. Prompt: color-only strict16 silhouettes, positions, angles, grip/muzzle/stock and alpha. Silver mechanism, graphite barrel/prongs, copper only coil, ivory wrap, walnut/leather grips, black hoses, cyan top/lime bottom glass, small crimson panel detail, interior ink and bevel shading; no sepia/one-color tint/new geometry/text.

WeaponsV3_5: target Weapons_5, material reference WeaponsV3_0. Original exec-df3cf410-aaab-43bc-ae07-b7f0ee980a36.png. Prompt: strict color-only16 view layout/geometry/alpha, silver/graphite mechanism, thin copper pipes/fasteners, ivory existing panels, brown grips, cream wrap and black hoses. Amber-green glass and limited teal top-set panels, cyan-lime glass and tiny crimson lower seams. Preserve neutral metal plus material contrasts, consistent8 views, interior black lines, no one-color tint/rainbow/text/new parts.

Metadata: all80 armor and96 weapon regions remeasured from alpha>96 in semantic prior regions; weapon hull convex from main subject and semantic anchors transformed by slight bbox affine. PNGs preserved; JSON only produced by analysis script D:/Unity/AshfallArtValidation/finish_multicolor.py. Maximum weapon bbox drift7 pixels. Parts/view count and multiple-material palette verified visually; shape preservation is approximate, not pixel-identical.

The Art agents saved4 armor and4 weapon images plus partial notes. Exact per-call prompts/provenance for every agent asset were not fully handed off after their interruption. Common production constraints are recorded in their task and existing MulticolorWeaponArt.md; no fabricated exact missing prompt is claimed.
