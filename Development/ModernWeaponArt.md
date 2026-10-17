# Modern weapon art / 現代銃2種

## Result / 結果
Built-in imagegen, stylized-concept, one generation. New image only; no programmatic PNG editing or CLI. Models12 (contemporary assault rifle) and13 (pump-action shotgun) extend the existing12 weapons.

Files:
- Assets/Resources/Art/LayeredPlayer/TacticalArt/WeaponsModern_6.png
- Assets/Resources/Art/LayeredPlayer/TacticalArt/ModernWeaponRegions.json
- Assets/Resources/Art/LayeredPlayer/TacticalArt/ModernWeaponAnchors.json

Source original: C:/Users/hiroki/.codex/generated_images/01a132de-862b-70d2-8c8a-1b0aa4eb7af0/exec-fa4f9da4-dcfe-4c86-8303-d08bcfd7041a.png. Kept unchanged. Output is1254×1254 RGBA, native generated transparency. The old Weapons_0..5 layout was visually inspected as format guidance; no old image was passed as an edit target.

## Contract / 契約
4 columns×4 rows. Rows1–2 model12, rows3–4 model13. Each model: d0 down/front, d1 front-right, d2 right, d3 back-right, d4 back/up, d5 back-left, d6 left, d7 front-left. d4 AR remains slightly diagonal in the generated image; runtime should use actual grip/muzzle residual alignment. No image resizing or rearrangement performed.

Regions use original normalized global image coordinates, x rightwards/y downwards. Convex hulls are global normalized and padded1.25 pixels for outline filtering. Each ROI encloses measured main alpha>24 component with2 pixel padding. JSON has16 records, model12..13 anddirection0..7, matching the existing schema. Some gun ends cross nominal cell borders; use measured ROI rather than fixed cell crop.

Anchors are original nominal-cell normalized, top-left origin; global image pixel=(column+x,row+y)×313.5. Points were visually measured directly on generated image: grip at pistol grip/shotgun stock wrist, muzzle at primary bore center, support at fore-end/pump hold, stock at butt/shoulder contact. Front/back concealed trigger/grip/support points are inferred, not exact exposed-trigger measurements. Approximate precision3–5 pixels on exposed side details and8–15 pixels on occluded front/back. Do not clamp out-of-cell points.

Model12 materials: charcoal steel receivers/barrel, cold steel edges/bolt, olive polymer stock and handguard, tan magazine tape, tiny turquoise optic lens. Model13: charcoal steel receiver/tubes, silver bolt/edges, warm wood stock/pump, ivory stock wrap, small red marker. Consistent material identity across views, strong black contours and inner seams. No UI, labels, logos, characters or cast shadows.

## Self-check / 自己確認
view_image checked whole atlas and material separation. Pillow/NumPy/SciPy read-only PNG analysis found exactly16 main alpha components;16 independent ROI/hull records and16 anchor records. No other weapon's opaque pixels fall inside any hull. Max40 hull vertices below64 runtime limit. 1,158,246 pixels have alpha0. Full muzzle/stock silhouettes are preserved inside canvas. Both filenames and JSON schemas validated. No Unity build or runtime QA performed by Art; integration belongs to PMO. Direction transitions and semantic anchors retain generation/measurement uncertainty stated above.

## Final prompt / 全文
Use case: stylized-concept. Asset type: production transparent quarter-view weapon sprite atlas for NEON VAULT. Create exactly 16 isolated complete gun sprites arranged evenly in a 4 columns by 4 rows grid, square 1254x1254 or higher square resolution. Rows 1-2 show model12, the SAME contemporary modern assault rifle in eight rotation angles. Rows3-4 show model13, the SAME contemporary pump action shotgun in eight rotation angles. Orthographic camera elevation35 degrees, crisp hand-painted original cel-shaded game art, bold black outer contours and inner mechanical seams, controlled broad shadow planes, detailed real receiver mechanism, trigger guard, grip, bolt, sights, barrel openings and stocks. No giant science fiction parts, no fantasy coils, no excessive glow. Gun12 is compact contemporary AR-type: black steel barrel and upper/lower receiver, cold silver bolt/edge highlights, small olive polymer handguard and adjustable stock furniture, black pistol grip, curved dark magazine with scratched tan tape strip, compact realistic red-dot optic with tiny dark turquoise lens. Gun13 is contemporary pump shotgun: charcoal black steel receiver and barrel/magazine tube, polished cold steel bolt and fasteners, warm brown walnut shoulder stock and grooved pump foregrip, small ivory wrap around rear stock and small red existing painted marker on receiver. Natural metal grey/black is dominant; clearly separate materials on each part. All eight views of each gun have identical physical design and material assignments. Direction sequence EXACT: cell0 front muzzle facing viewer/down screen; cell1 muzzle toward front-right/lower-right; cell2 muzzle right, stock left; cell3 muzzle back-right/upper-right; cell4 stock toward viewer/down, muzzle away/up; cell5 muzzle back-left/upper-left; cell6 muzzle left, stock right; cell7 muzzle front-left/lower-left. Repeat this exact eight-angle cycle for shotgun in cells8..15. Every gun centered in its cell with at least 12% transparent margins, full silhouette including muzzle/stock, equal physical scale across that gun's views. Fully transparent background actual alpha, no people/hands, no floor, no cast shadow, no text, UI, logos, labels, gridlines, watermarks.

