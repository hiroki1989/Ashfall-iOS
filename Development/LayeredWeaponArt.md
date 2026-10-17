# Layered player weapon art / 2026-10-12

Built-in imagegen skill. Original files are preserved. Output is native alpha PNG, 1254 × 1254 as returned by the service (2048 requested). Six 4 × 4 atlases, two model families per atlas, eight painted views per model. Files live under Assets/Resources/Art/LayeredPlayer/Weapons_0..5.png.

Rows 1–2 contain the even model; rows 3–4 the odd model. Read order: down, down-right, right, up-right, up, up-left, left, down-left. Orthographic camera elevation35°. Measured WeaponAnchors.json specifies real trigger/support/stock/muzzle locations; WeaponRegions.json extends isolation beyond mathematical cell edges when needed. Artwork should never be resized from a assumed 2048.

Models: 0 Dust Runner / 1 Rivet SMG; 2 Long Watch / 3 Scrap Hammer; 4 Trench Sweeper / 5 Sawed Thunder; 6 Iron Belt / 7 Needle Driver; 8 Coil Lance / 9 Lead Splitter; 10 Isotope Swarm / 11 Vacuum Engine.

## Generation specification

Reference: Assets/Resources/Art/WastelandEquipment-v2.png; later generations also use Weapons_0.png for visual/grid consistency. Every prompt asks for isolated detailed complete guns in a uniform4×4 transparent grid, padded margins, no hands/people/ground/shadows/text/labels/UI/flashes; realistic hand-painted rusty steel, scratched machining, warm/cool material highlights, worn walnut/dusty canvas, accurate orthographic directional foreshortening and consistent designs. All weapon components in the equipment illustration are used as family references. Imagegen may exceed a requested margin or invert a view; actual anchors and isolated ROI correct the rendered result, rather than pretending the mathematical grid is exact.

## Recorded generation conditions / 生成条件の要約

All six jobs used the following grid/direction/material conditions. This record summarizes the issued prompts rather than claiming to reproduce every prompt character.

A new exact four-column by four-row native-alpha transparent atlas: sixteen isolated complete guns. Each gun occupies its own cell with fifteen percent transparent margin. First two rows contain weapon A; last two rows weapon B. Eight views per weapon: muzzle toward viewer/down, down-right, right, up-right, away/up with stock foreground, up-left, left, down-left. Orthographic elevation35°, weapons level in world, foreshortened front/back. Rich realistic hand-painted action-game illustration with defined edges, finely scratched rust steel, worn walnut, dusty canvas, machined screws, warm highlights and cool reflected light, consistent design in all eight views. No people, hands, floor, background, cast shadows, text, labels, grid lines, UI or muzzle flash. Square2048 requested.

Weapon A/B families, respectively: carbine/SMG; scoped long rifle/pump shotgun; drum automatic shotgun/short shotgun; belt machine gun/glass needle gun; copper electromagnetic coil lance/forked split-jaw rifle; circular isotope drone launcher/vacuum trumpet cannon. The last two atlases follow the visible copper coils, glass isotope cells, rusty mechanical housings and canvas-wrapped stocks in the reference illustration, with Weapons_0 as palette/grid reference.

## Original image files

All originals are in C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/:

- Weapons_0: exec-fbd38073-8aa9-4c1e-ab66-7cc6784dcc78.png
- Weapons_1: exec-ded7c168-452e-4ff0-91e6-d945cc16e8e0.png
- Weapons_2: exec-c8779ffa-e6ca-4599-8375-d531287dcdbd.png
- Weapons_3: exec-85b7b3a1-a1a7-4a96-8241-bd73b62f15e6.png
- Weapons_4: exec-d4325f79-c27c-42ad-be4d-c3aa030f94f3.png
- Weapons_5: exec-8f0f1256-77da-496e-b65e-d2384eb937d4.png

Animation uses independent painted source sprites and cached articulated poses, with matching item sprites on each joint; 1024 poses are not 1024 independent hand/AI-drawn animation images. Runtime validation and motion evidence are recorded by PMO in STATE.md.

