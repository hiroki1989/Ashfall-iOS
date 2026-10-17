# 独立防具2D原画 / Layered 2D armor art

制作: 2026-10-12、組込imagegen。既存 `WastelandEquipment-v2.png` を材質・装備意匠の参照に使用。原本はすべて保持。 / Created using built-in imagegen; existing equipment used as material/design reference. All generated originals retained.

## 保存と方向 / Files and directions

`Assets/Resources/Art/LayeredPlayer/` に以下を保存。1方向につき2セット×8部品、固定4列×4行。全体が同じ方向。 / Each file contains 2 sets × 8 parts in a fixed 4×4 atlas, all parts sharing one direction.

| File | Facing | Original final image | Alpha=0 | Minimum tile margin* |
| --- | --- | --- | --- | --- |
| Armor_Front.png | 画面下 / bottom, 0° | exec-c1127190-6a10-4034-83fe-b725dc2b4724.png | 80.7% | 32 px |
| Armor_FrontRight.png | 画面右下 / bottom-right, 45° | exec-8ea3875d-0efe-4b4e-b24b-603f081b6e8f.png | 90.3% | 59 px |
| Armor_Right.png | 画面右 / right, 90° | exec-60637819-37d9-4a02-9e15-6c93c25ce804.png | 84.5% | 38 px |
| Armor_BackRight.png | 画面右上 / top-right, 135° | exec-f30e28c9-8633-4df2-addb-3a41075c0e60.png | 84.6% | 32 px |
| Armor_Back.png | 画面上 / top, 180° | exec-111a8718-7d93-4a2e-a965-68f9502a5108.png | 88.4% | 38 px |

原本ディレクトリ / Original directory: `C:/Users/hiroki/.codex/generated_images/01a12f78-9757-7ce1-b122-b1b245e334b1/`。
全ファイルの実サイズ1254×1254、PNG RGBA。2048squareを指定したが組込出力の実サイズは1254。引き伸ばし処理なし。 / Native outputs are 1254×1254 RGBA PNG despite requested 2048 square; no artificial upscaling.
*各セルのalpha>32のbboxからセル境界までの最小余白。1254/4のセルに対し全80部品で10%以上。アンチエイリアスの低alpha fringeは別に存在する。 / Minimum margin measured from alpha>32 content to tile boundary; all 80 parts exceed 10%. Low-alpha antialias fringe may extend further.

Left/FrontLeft/BackLeftは対応右画像の左右反転を使用。左右反転時に解剖学的R/Lセルも交換する。 / Mirror right-facing views to create the three left views; swap anatomical R/L cells when mirroring.

## セル契約 / Cell contract

Row-major、左上0。セットAは0–7: rusty steel/brown leather scavenger。セットBは8–15: dark gunmetal/copper reinforced tactical、微小lime isotope glass。 / Row-major from top-left0; A scavenger rust steel/leather, B reinforced tactical darkmetal/copper/isotope.

| Local index | 部品 / Part | A cell | B cell |
| --- | --- | --- | --- |
| 0 | 兜＋ゴーグル＋防毒面 / helmet + goggles + gas mask | 0 | 8 |
| 1 | 腕なし胸鎧・ハーネス・ポーチ / armless armored vest | 1 | 9 |
| 2 | 解剖学的右前腕甲＋手袋 / anatomical right bracer + glove | 2 | 10 |
| 3 | 左前腕甲＋手袋 / left bracer + glove | 3 | 11 |
| 4 | 右膝・脛鎧 / right knee-shin shell | 4 | 12 |
| 5 | 左膝・脛鎧 / left knee-shin shell | 5 | 13 |
| 6 | 右装甲靴 / right armored boot | 6 | 14 |
| 7 | 左装甲靴 / left armored boot | 7 | 15 |

四肢は上が近位関節、下が手/足の縦軸ニュートラル。装甲下の人体・皮膚・全身服を含めない（装備の革巻/裏地は含む）。俯角35°の正投影。背面には後頭部帯・背面ハーネス、脛後ろベルト・四角い踵を使用。 / Limbs are vertical neutral with proximal joint at top; equipment contains no body, skin or complete base garments. Camera pitch35° orthographic. Back uses rear bands/harness, rear shin straps and square heels.

セル余白や描かれた部品のサイズは方向間で完全一致しない。固定セルを切り出した後alpha bboxへtrimし、ゲーム側で部位サイズとjoint位置へ合わせる。画像セル全体を直接同じ骨サイズへ縮尺すると方向間でサイズが変わる。 / Extract fixed cells, then trim alpha bounds and fit to per-part dimensions/joints; do not size whole tiles as the artwork footprint differs across views.
ピボットは兜/胴を中央、前腕と脛の近位側は上端、靴は足首上端を基準に実装側で決定。 / Runtime should choose central head/torso pivots and proximal upper-edge limb/ankle pivots.

## Import settings

Normal Texture / Default、Read/Write enabled、sRGB、mipmaps off、Clamp、Bilinear、NPOT=None、alpha transparency、uncompressed、max2048。`.meta` のGUIDは個別生成。 / Unique GUID per texture, readable and uncompressed to support runtime atlas extraction.

## 最終プロンプト / Prompt set

最初の生成仕様 / Original generation specification:

```text
Use case: stylized-concept. Asset type: precision transparent 2D skeletal-animation equipment texture atlas for a polished wasteland action RPG.
Input image is MATERIAL AND EQUIPMENT DESIGN reference ONLY: rusty riveted steel, worn brown leather, gas-mask goggles, tiny lime isotope glass. Do NOT copy guns or pants.
Primary request: create a 2048x2048 square PNG on actual alpha transparency, 4 columns by 4 rows, 16 completely isolated equipment parts, exactly ONE part per cell, no drawn grid. Every cell same square. Each object fits with at least 10% empty margin on all sides. Center each object. All objects share exact same camera direction and orthographic camera looking down 35 degrees, realistic adult human proportions. High-detail painted game illustration with excellent polished Hades-like art quality, crisp irregular silhouettes, visible craftsmanship, sculptural highlights, fine rivets/seams/scratches, convincing metal/leather, strong soft depth shading.
This is EQUIPMENT ONLY, no body mannequin, no skin, no underlying clothing; bracers may include protective fitted gloves and boots are leather equipment. Each equipment part will fit independently over a separate animated body sprite. Forearms and shins point straight down in neutral extended pose, their proximal joint attachment at top. No arm in the torso cell.
ROW 1 LEFT TO RIGHT (set A): fitted domed scavenger helmet with central metal reinforcing band, attached circular goggles and fitted gas mask with circular filters as ONE head overlay; rusty steel armored vest with chest plates, brown leather harness and pouches, sleeveless NO ARMS; anatomical RIGHT forearm fitted rusty bracer with protective brown glove; anatomical LEFT forearm matching bracer with glove.
ROW 2 (set A): anatomical RIGHT fitted knee/shin armored shell ONLY, no thigh no pants no flesh; anatomical LEFT matching knee/shin shell; anatomical RIGHT armored leather boot with plated toe; anatomical LEFT matching armored leather boot.
ROW 3 (set B): same helmet+mask type with dark gunmetal reinforced tactical construction, copper fittings, small dim lime isotope lens details; darkmetal reinforced tactical vest with chest plates, harness, belt pouches, NO ARMS; anatomical RIGHT forearm fitted reinforced darkmetal bracer and glove; anatomical LEFT matching forearm bracer and glove.
ROW 4 (set B): anatomical RIGHT reinforced tactical knee/shin shell ONLY; anatomical LEFT matching knee/shin shell; anatomical RIGHT reinforced tactical armored boot; anatomical LEFT matching armored boot.
Set A rusty steel/leather scavenger; Set B darkmetal/copper/isotope reinforced tactical. Exactly these sixteen parts and layout, no extra parts.
Avoid any floor, cast shadow, backdrop, typography, UI, letters, numbers, labels, gridlines, mannequin, anatomy, exposed skin, full character, weapon, pants, upper arm. True alpha-transparent background, no checkerboard artwork.
DIRECTION: straight FRONT, toward the bottom of screen; bilaterally symmetric front view with tops visible from 35-degree camera.
```

方向変換共通仕様 / Shared view-conversion specification:

```text
Use case: identity-preserve. Asset type: 2D equipment parts transparent texture atlas for skeletal animation.
Input image is EDIT TARGET AND CANONICAL DESIGN REFERENCE: EXACT SAME 16 independent armor parts in STRICT4 columns4 rows, first8 rusty steel/leather scavenger, next8 dark gunmetal/copper reinforced tactical with tiny lime isotope glass.
Only change the VIEW ANGLE of EACH object. Keep same identity, same shapes/harness/pouches/rivets/filters/materials and sets. No redesign. Keep exact16-cell row-major arrangement: helmet+mask, vest NO ARMS, RIGHT forearm+glove, LEFT forearm+glove; RIGHT knee/shin armor, LEFT knee/shin armor, RIGHT boot, LEFT boot; same8 for setB rows3/4.
Each limb equipment object has straight VERTICAL long axis with proximal attachment at top, hand/foot at bottom. Orthographic camera pitches DOWN35degrees. Fit every object in central60% of its uniform square cell. Cell centers x12.5%,37.5%,62.5%,87.5%; y12.5%,37.5%,62.5%,87.5%. Preserve generous transparent gutters and empty 15% minimum margins.
Beautiful polished hand-painted game art, crisp sculptural detail, rich metal/leather dimensionality, adult fitting equipment. Equipment only, no body, skin, mannequin, upperarms, underlying pants/shirt, or extra parts. No weapons, text, numbers, labels, grids, floor, shadows, checkerboard, background. True transparent alpha PNG. Requested2048square.
```

各変換の方向指定 / Direction-specific instructions:
- FrontRight: yaw45°, wearer screen bottom-right; front + anatomical left side; mask/toes lower-right.
- Right: pure90° side profile; one goggle/cheek filter, nose/toes right; torso side armhole and chest edge.
- BackRight: yaw135°, screen top-right; rear helmet/back plate/harness; rear/left surfaces and heel foreground.
- Back: yaw180°, screen top; rear dome/mask straps, crossed harness, rear shin leather straps with no frontal kneecap plate, squared heels.

目視確認で正面yaw、前腕の傾斜、セル跨ぎを修正。最後のlayout-only editは「same16objects/identity/view/order; shrink another20–30%; strict4x4 centered tile; wide transparent gutters; true alpha; no labels/grid/body」。各最終出力はtool生成のままコピーし、Pythonでの画像編集なし。 / Targeted imagegen edits corrected yaw, forearm tilt, spacing and back surfaces. Final layout edits requested20–30% reduction with strict cells and preserved identities; image files were copied unchanged, never edited with Python.

## 検証と制約 / Verification and limitations

5方向すべて目視確認、16非空セル、真alpha、セル余白をPillowで読み取り検査。文字/床/武器/人体/不透明背景なし。 / Inspected all five views and verified16 nonempty cells, true alpha and margins using read-only Pillow checks.
画風・セット差・細部は維持するが生成画像の別視点再投影のため、全リベットの厳密な立体対応や正確な同一照明は保証しない。 / Designs remain recognizable, but imagegen view synthesis does not guarantee exact rivet correspondence or identical lighting.
Unity骨格への統合、bake64歩行/32待機/32射撃とゲーム画面QAはPMO/実装担当の工程。原画完成だけでゲーム内動作検証完了とはしない。 / Unity integration, baking and final in-game QA belong to implementation/PMO.

## 初回Unity合成のfit提案 / First Unity composition fit review

確認画像 / Reviewed images: `D:/Unity/AshfallArtValidation/Baked-first/portrait-front-right.png` and `portrait-side-aim.png`。初回合成のArt自己確認であり、修正後QAではない。 / This is an initial art fit review, not final QA.

胴の襟からhelmet下面への重なりは成立し、FrontRight胸鎧の幅はbody torsoより約13%広く自然。巨大銃で肩と頭が隠れる点、body腕の太さとmotionは実装修正中のため今回の原画欠陥には含めない。 / Collar overlap is plausible and FrontRight vest width is about13% wider than the torso. Weapon and body-arm issues are under implementation repair.

頭のtop-centerをbodyheadと共用し高さ1.03倍にすると、短首を含む細長いbodyheadに対し、兜+マスクは短く広いので幅が過大化する。alpha>24bboxの形から、同じ高さfitのhelmet/bodyhead幅比はFront1.38、FrontRight1.50、Right1.43、BackRight1.59、Back1.68。 / Equal-height fitting excessively widens the helmet because the body head includes a short neck, while helmet/mask art is broader.

頭はbodyhead幅の1.12–1.25を初期値にuniform scaleし、兜下面/マスク下面を首と襟に合わせる。方向間でhelmetの幅を急に変えず、首接続点から上へ配置する。フルhelmet着用時、裸顔が輪郭外へ出るならbodyheadを置換/非表示し、首/襟はtorso側の接続として残す。高さ一致を守るための非等方stretchは推奨しない。 / Start with helmet width1.12–1.25 times the body head, uniform scale and align its lower mask/neck edge to collar. Hide/replace the base head if bare face protrudes; retain neck/collar connection. Do not distort artwork to force equal height.

胸鎧はtorsoheight1.0–1.03、bodytorso幅1.05–1.15を初期範囲に、FrontRight/Rightの腕穴へ肩袖が自然に入るか確認。BackRightのみ元のbbox比が広いため1.2までを監視。肩のjointを胸鎧全体bboxの左右端へ置かず、描かれたarmhole上端へ合わせる。 / Vest starts at torso height1.0–1.03 and width1.05–1.15, checking sleeve insertion into armholes; BackRight may need a1.2 cap. Shoulder joint corresponds to armhole, not full vest bbox edge.

前腕甲は手袋まで含むので、骨のelbow→wristだけでなくelbow→fingertipを長さ基準にする。bodyforearmheight1.24倍の一律uniformfitでは横幅も1.24倍になり、FrontRight左はbodyforearmの約1.55倍幅となる。装甲の手首位置をgripに合わせ、glove部分の末端はbodyhandtipと一致させる。装甲側gloveと独立bodyhandを二重に描かない（bodyhandを非表示または完全下層へ）。 / Bracer includes glove; fit elbow to fingertip and anchor authored wrist to grip. A1.24 forearm-height multiplier also widens the art, especially FrontRight left. Avoid double-rendering separate body hand over armor glove.

sourceの対称なtop-centerは必ずしも実際のjoint中心ではない。兜はmask下端/neck、胸は襟/armhole、前腕はelbowcutとwrist、靴はankleopeningをsemantic anchorとして使う。元のpainted identityを維持したfit/anchor修正で対応でき、今回source画像の修正は不要と判断。 / Use semantic anchors rather than bbox top-center alone; existing painted identity can be retained through fit and anchor changes.

