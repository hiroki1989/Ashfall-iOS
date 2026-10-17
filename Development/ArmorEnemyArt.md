# Armor V2 / Enemy 0–4 painted art — 2026-10-13
## 変更 / Changes
Built-in imagegen with imagegen skill. New native PNG originals copied unchanged into Assets; earlier Armor_* and Body_* remain intact. No Python image edits, artificial resizing, painted UI text or baked guns.

- Assets/Resources/Art/LayeredPlayer/ArmorV2_{Front,FrontRight,Right,BackRight,Back}.png (+unique .meta): 5 direction sheets, 2 sets × 8 overlays, 80 parts. All 1254×1254 RGBA.
- Assets/Resources/Art/LayeredPlayer/ArmorV2Regions.json: 80 normalized top-left texture ROI records {direction,cell,x,y,width,height}.
- Assets/Resources/Art/LayeredEnemies/Enemy_{0,1,2,3,4}.png (+unique .meta): 15 parts each; 75 parts.
- Assets/Resources/Art/LayeredEnemies/EnemyRegions_A.json: 75 normalized ROI records {kind,cell,x,y,width,height}; PMO merges with B. Enemy0–2 1254×1254, Enemy3 1269×1239, Enemy4 1225×1284.

## デザイン / Design
Armor A: olive technical fabric, steel-blue faceted pauldrons, tailored waist/belt, copper hardware, vivid teal optical glass. Armor B: crimson/navy, sharper steel plates, diagonal red sash, amber glass. WastelandKeyArt is quality/mood reference; new designs retain worn metal, tactical layering and craft detail. Avoid former gold bubble-shaped vest. No body flesh/mannequins.

Enemy0: ragged thin civilian zombie. Enemy1: asymmetric acid throat sacs/mustard rags. Enemy2: teal armored mutant soldier, empty hands bent inward around belt/lower chest for independent gun. Enemy3: huge bone-shouldered colossus/tiny skull/thick limbs. Enemy4: wiry pouncer with long claws and spiny hunched back.

## 契約 / Contract
Armor direction indices 0 Front,1 FrontRight,2 Right,3 BackRight,4 Back. Left facings derive by mirror plus anatomical R/L exchange.
Row-major cells0–7 = helmetmask, chest/pauldrons/belt, right bracerglove, left bracerglove, right kneeshin, left kneeshin, right boot,left boot. Cells8–15 repeat set B.
Chest contains shoulder caps and waist, no full arms. Longitudinal limb axes have proximal joint at top.

Enemy cell3*d = head+torso+pelvis+whole arms/hands (no legs or gun); cell3*d+1 = left complete leg/foot; cell3*d+2 = right complete leg/foot; five directions same as Armor. Cell15 empty, no ROI. Runtime deforms upperbody and animates legs; these are 5 direction source paintings, NOT individually hand-painted many-frame animations.

Use ROI before alpha trim; mathematically even tiles cut shoulder artwork or include neighboring parts. ROI measured from opened alpha>96 connected components plus 2 px pad, all 16/15 semantic components inspected in correct order. Metadata: Default readable sRGB, no mipmaps, Clamp, uncompressed, max2048, unique GUIDs.

## 検証 / Verification
Viewed every final atlas. Count: Armor5×16=80; Enemy5×15=75. True alpha-zero area 55–67% except ArmorFR56% (all have real alpha). JSON counts and native texture bounds checked; no resampling performed. Enemy0 initial missing back right leg and 3×5 layout corrected in final 4×4 generation. Enemy2 initial hanging arms corrected to bent waist hands. ArmorRight/BR re-generated for side/rear chest silhouette and right-facing footwear.
Runtime fit/shoulder overlap/weapon grip/deformed walk still require PMO integration and finished-build QA.

## 限界 / Limits
ArmorRight kneeshin and one forearm retain somewhat three-quarter surfaces; BR helmet still exposes a sliver of side optic and can be closer to side than exact 135°. Back limbs retain similar front-like plate shapes despite rear chest/head being distinct. Clothing collar/boot tops still have dark interior hints, although not separate black caps. AI sheet cell gutters are imperfect, hence mandatory ROI. Small seam/limb fit must be assessed after integration. No claim of complete frame-by-frame art or commercial benchmark parity.

## 原本 / Unmodified source files
Default generation directory: C:/Users/hiroki/.codex/generated_images/01a13066-30a1-7e12-b89d-3333532e3b48/
Armor Front exec-79ad5f37-1257-49ef-8c0a-cb4f3cadb624.png
Armor FR exec-9be5006f-9bcb-41df-9f2c-63ce6254091d.png
Armor Right final exec-0f8a30ca-e7b4-47a8-a9fb-513340255a33.png
Armor BR final exec-3b35d7e3-6e9f-4837-b6a9-80aaf65fae0a.png
Armor Back exec-c1d1116e-0425-4689-af70-c2a281e2cb13.png
Enemy0 final exec-e6bdd870-73c9-40cb-8f0e-46bb817a4e71.png
Enemy1 final exec-6c91e141-3e6f-4cae-a60a-6046e2e61433.png
Enemy2 final exec-b8e1df20-94f0-4e23-b27f-9f793dd201bb.png
Enemy3 exec-e9791c43-5036-473f-919a-89c47a7d138f.png
Enemy4 exec-ffbbfeef-bb07-4598-9195-4113d4411a45.png
Selected Assets PNGs are byte-for-byte copies of these final originals. Earlier generated attempts remain in default original directory.

## 最終プロンプト / Final prompt specification
Armor initial: stylized-concept, square transparent 4x4 isolated overlays, fixed 16part order above, realistic painterly tactical survivor details, olive steelblue teal vs crimson navy amber, tailored waist and sculpted shoulder caps, no mannequin/gun/text/grid/shadow, exterior overlays without black hollow cuts.
Armor FR/Back: precise-object-edit using Front as costume reference, same parts/colors/design, requested orthographic view, vertical part axes, real alpha, wide gutters.
ArmorRight correction: strict90degree right profile, only one goggle, narrow side chest, toe points RIGHT, forearms edge-on; same 16parts/colors, camera35deg, no text/shadow.
ArmorBR correction: rear135degree right3quarter, rear helmet and rear chest panel/straps instead of front sash/pouches; same part order/colors,35deg, alpha.
Enemy0–2 final: precise-object-edit from corresponding draft, EXACT4columns4rows; ROW1 frontupper/frontleftleg/frontrightleg/FRupper; ROW2 FRleftleg/FRrightleg/Rightupper/Rightleftleg; ROW3 Rightrightleg/BRupper/BRleftleg/BRrightleg; ROW4 Backupper/Backleftleg/Backrightleg/empty. Same creature identity, upper includes head/torso/pelvis/arms but no legs/gun. One complete leg per cell. Orthographic35deg. Enemy2 both forearms inward around belt, empty hands ready to cradle independent rifle; others spread menacing claws. Real alpha, no grid lines, no text/floor.
Enemy3–4: stylized-concept, production15part4x4 atlas, same explicit cell0..14 directions/parts, distinctive creature silhouette described above, clawed neutral combat pose,35deg, full isolated parts/clear margins, real alpha/no guns/no text/no floor/no hollow cut ends.

