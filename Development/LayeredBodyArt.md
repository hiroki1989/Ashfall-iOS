# ASHFALL layered player body art / 人物本体パーツ原画

Built-in imagegen only / 組み込みimagegen使用。生成画像原本は Codex generated_images に保持。画像処理による再描画・alpha除去・リサイズは実施していない。Python/Pillow/SciPyは寸法・透明度・独立部品とROIの測定、メタデータ生成だけに使用。

## 保存ファイル / Deliverables
Assets/Resources/Art/LayeredPlayer/Body_Front.png
Assets/Resources/Art/LayeredPlayer/Body_FrontRight.png
Assets/Resources/Art/LayeredPlayer/Body_Right.png
Assets/Resources/Art/LayeredPlayer/Body_BackRight.png
Assets/Resources/Art/LayeredPlayer/Body_Back.png
各 .png.meta と BodyRegions.json/.json.meta。

全5画像: **1254×1254 RGBA、alpha最小0/最大255**。2048-class指定でも生成ツールの実出力は1254。アップスケールはしていない。4×4、15独立パーツ＋最後透明セル。35度俯角の正投影を指定。同一の短髪成人男性、olive jacket / taupe cargo trousers / brown leather gloves and boots。武器、防具、兜、防毒面を含めない。

## セル契約 / Part order
左上から row-major、人物自身の左右を使用。
0 head / 裸頭・短い首
1 torso / 両腕なしの上着胴
2 pelvis / 太腿なしのパンツ腰
3 rightUpperArm / 右上腕袖
4 rightForearm / 右前腕袖
5 rightHand / 右革手袋
6 leftUpperArm / 左上腕袖
7 leftForearm / 左前腕袖
8 leftHand / 左革手袋
9 rightThigh / 右太腿
10 rightShin / 右下腿
11 rightBoot / 右ブーツ
12 leftThigh / 左太腿
13 leftShin / 左下腿
14 leftBoot / 左ブーツ
15 empty transparent / 完全透明

direction 0 Front（画面下を向く）、1 FrontRight（画面右下）、2 Right（画面右）、3 BackRight（画面右上）、4 Back（画面上）。左3方向はランタイム側で反転する。画面上の光を完全な8方向に描き直した状態ではない。

## 切り出し・骨格接続 / Regions and rig
BodyRegions.json の regions がCodingとの正式契約。
{"regions":[{"direction":0,"cell":0,"x":0.01,"y":0.01,"width":0.24,"height":0.24}, ...]}
direction0..4、cell0..14、x左→右/y上→下、全画像に対するnormalized0..1。75件。
追加directionsフィールドには部品名・整数sourceRect(top-left)/unityRect(bottom-left)と画像寸法を記録。

生成画像の部品は見た目上独立しているが、Front以外には数学的4×4水平境界を越す部分がある。**固定cellUVだけで切り出さずregionsを使う**。Body_Rightの左bootは末尾空セル側へ約5px延びるためその爪先もROIに含めた。
alpha>8連結成分の2000px以上を独立partとして検出、列ごと上から4/4/4/3個でassign。bboxへ3pxの透明余白を付与。全75ROIで大型alpha成分が正確に1個だけ含まれ、別part混入がないことを再検証。
Frontだけはimagegenで75%縮小・セル中央再配置し、314/627/940の水平/垂直境界alpha>16を全0に修正。残4枚は再配置によるidentity変化を避け可変ROIで正確切出し。

各部品は単独セルへ適合して描かれるため、セル中の大きさ同士は人体実寸比ではない。**頭・手・胴・脚の人体比率は骨格joint長と部位widthで決める**。7.3頭身は組立リグで維持する。長軸は原則joint上端→下端。肩袖には自然な斜め輪郭がある。関節開口の楕円影を上位パーツと小さな重なりで隠す。上端中央pivotから接続する。単純な画像の自然寸法連結は不可。

これは多コマアニメーションの入力原画であり、歩行64/待機32/射撃32の手描き完成コマを直接生成したものではない。PMO/Codingが部品を骨格で動かし、方向別フレームへbakeする。独立した武器と5防具レイヤーは別担当が接続する。

TextureImporter: Default、isReadable=1、nPOTScale=0、max2048、Clamp、Bilinear、mipmapなし、DefaultTexturePlatform textureCompression=0、alphaIsTransparency=1。元alpha保持。

## 検証 / Checks
5枚をview_imageで目視。Front/FrontRightの顔identity、Rightの鼻/胸profileと爪先右向き、BackRightの背中panel/後面パンツ、Backの後頭部/背中/踵を確認。
Back初枚で第3行の左手欠落と後続partの左詰めが発生したためimagegenで第3行だけ修復。最終版は75部品すべて存在。
全5枚1254²RGBA、alpha0..255。完全透明比率: Front79.54%、FrontRight61.52%、Right61.65%、BackRight62.30%、Back63.27%。75 ROI isolation checks PASS。
Unity統合・ゲーム縮小表示・アニメーション関節接続の最終QAはPMO/QA担当が完成版で実施する。

## 原本 / Sources
Codex generated_images/01a12f77-f59c-7e82-bb8c-c0439ed61f5d/
Front初枚: exec-1015b346-8059-48a6-8eaa-2eaa17fe7b81.png
Front採用（余白修正）: exec-f0a8e790-5940-431a-af85-ad4c76e1070e.png
FrontRight採用: exec-8bd7c503-4d59-4d87-a8ff-1f0c762ff830.png
Right採用: exec-6b7691ff-3110-4f7a-acc2-c2b27eb34fef.png
BackRight採用: exec-57b1b9b4-b7f5-4daf-aafe-58c7a65a38b6.png
Back初枚（不採用）: exec-4d1518e0-d49b-4957-a816-93ffda460b11.png
Back採用（部品順修正）: exec-1b4cef8b-1079-4fdb-a77d-8fea8240dec3.png
全原本を保持、base64ログなし。

## 実使用プロンプト / Exact prompts

### Front initial

```text
Use case: stylized-concept. Asset type: production 2D skeletal paper-doll PARTS ATLAS for ASHFALL, a premium hand-painted quarter-view wasteland action game. Generate a square 2048-class image with true transparent alpha. Exactly 4 equal columns and 4 equal rows, no visible grid, each isolated component centered in its own cell with 10 percent clear margins. All parts belong to ONE realistic 7.3-head-tall adult male survivor with short dark hair, weathered handsome face, olive canvas field jacket with collar, zipper, stitched pockets, worn taupe trousers and brown leather gloves/boots. Rich precise hand-painted game illustration, natural cloth folds, fine seams, nuanced skin, crisp readable silhouette, painterly realistic detail, never 3D render. Camera: orthographic FRONT view of the survivor, elevated camera 35 degrees looking down, the survivor faces camera/screen bottom. These are SEPARATE body PARTS, NOT complete people. Each limb is straight and vertical, upper joint at top, lower joint at bottom. Natural limb widths from one adult body, do not enlarge small parts. Allow small hidden overlap padding at joint ends. Exact row-major cell order: row1 column1 = bare head and short neck with no hat or mask; row1 column2 = olive jacket torso ONLY, shoulders to hem, NO arms; row1 column3 = trouser pelvis/belt ONLY, hips to crotch, NO thighs; row1 column4 = character RIGHT upper sleeve ONLY shoulder to elbow. Row2 column1 = RIGHT forearm sleeve elbow to wrist; row2 column2 = RIGHT gloved hand wrist at top fingers down; row2 column3 = LEFT upper sleeve shoulder to elbow; row2 column4 = LEFT forearm sleeve elbow to wrist. Row3 column1 = LEFT gloved hand wrist at top fingers down; row3 column2 = RIGHT trouser thigh hip to knee; row3 column3 = RIGHT trouser shin knee to ankle; row3 column4 = RIGHT leather boot ankle above toe, toe facing viewer. Row4 column1 = LEFT trouser thigh hip to knee; row4 column2 = LEFT trouser shin knee to ankle; row4 column3 = LEFT leather boot ankle above toe, toe facing viewer; row4 column4 = completely EMPTY transparent. Keep cells perfectly separate with generous blank gaps, no part crosses a cell border. Same scale, no floor, no background, no shadows outside parts, no labels, no text, no UI, no border. No weapons, no armor, no helmet, no gas mask, no backpack. Every part is isolated and open joint cuts are neat and overlap-ready. Warm soft light from upper left with cool reflected highlights.
```

### FrontRight; input Front initial

```text
Use case: identity-preserve. Edit target: reference ASHFALL body-parts atlas. Create the SAME 15 separated paper-doll body components in the SAME exact 4x4 cell positions, change only VIEW DIRECTION to FRONT-RIGHT three-quarter: adult survivor faces screen bottom-right, turned 45 degrees from front toward screen right. Orthographic camera elevated 35 degrees looking down. Preserve the same face identity, short hair, olive jacket with stitches/zipper/pockets, taupe cargo trousers/belt, leather gloves and laced boots, painterly premium realistic game art, material density, warm upper-left light, sharp contours. Square 2048-class transparent alpha atlas. NOT whole people. Every limb remains vertical shoulder/elbow/wrist/hip/knee/ankle upper at top and lower at bottom, no bent pose. Show each separated part as seen from this single front-right camera direction, including head in 3/4 view, torso and pelvis in 3/4 view, rotated sleeve surface, trouser-leg surface, and boots with toes pointing toward screen bottom-right. Respect distinct anatomical RIGHT and LEFT parts. Exactly preserve row-major content: 0 head short neck; 1 torso no arms; 2 pelvis no thighs; 3 right upper sleeve; 4 right forearm sleeve; 5 right gloved hand; 6 left upper sleeve; 7 left forearm sleeve; 8 left gloved hand; 9 right thigh; 10 right shin; 11 right boot; 12 left thigh; 13 left shin; 14 left boot; 15 EMPTY TRANSPARENT. Each object entirely within its cell with 10 percent margins. Clean transparent gaps. Anatomical cut surfaces overlap-ready. No weapons/armor/helmet/mask/backpack. No additional objects. No text/grid/floor/shadows/background. Do not add complete limbs or duplicate joints; strictly one piece per cell.
```

### Right; input FrontRight

```text
Use case: identity-preserve. Edit target: ASHFALL paper-doll parts atlas reference. Change ONLY view to a TRUE RIGHT SIDE PROFILE, survivor faces SCREEN RIGHT, 90 degrees from frontal. Camera remains orthographic elevated 35 degrees looking down. Repaint all fifteen parts in side view, while preserving same adult male face identity, short dark hair, olive field jacket, fine stitching/pockets/zipper, taupe cargo trousers/belt, brown leather gloves/boots, premium detailed hand-painted realistic game aesthetic, warm upper left light and transparent alpha. Head MUST show one eye and clear side nose silhouette. Torso MUST be narrow profile showing chest side at screen right and back at screen left. Pelvis MUST be side profile with buckle at screen right and buttock at screen left. Boots MUST be full SIDE silhouette with heel left and toe extending right, NEVER forward-facing. Arms and legs are separated, their straight long axis remains vertical (joint above and lower joint below), distinct right/left side surfaces. Exactly 4 equal columns by 4 equal rows, identical cell content/order to reference: 0 head/short neck, 1 torso NO ARMS, 2 pelvis NO THIGHS, 3 right upper sleeve, 4 right forearm sleeve, 5 right gloved hand, 6 left upper sleeve, 7 left forearm sleeve, 8 left gloved hand, 9 right thigh, 10 right shin, 11 right boot, 12 left thigh, 13 left shin, 14 left boot, 15 EMPTY TRANSPARENT. Each isolated component centered fully inside cell, ten percent transparent margins, no touching adjacent cells. 2048-class square. No complete person, no weapons/armor/helmet/gas mask/backpack. No text/UI/grid/border/floor/cast shadow/background. Preserve realistic human proportions and cloth texture. Joint cuts only, ready for overlapping skeleton assembly.
```

### BackRight; input Right

```text
Use case: identity-preserve. Edit target: ASHFALL paper-doll parts atlas reference. CHANGE ONLY VIEW to BACK-RIGHT three-quarter: survivor faces SCREEN TOP-RIGHT, turned 135 degrees away from frontal, show mostly BACK of head, BACK of jacket, BACK of pelvis and BACKS/SIDES of legs, 45 degrees from rear toward screen right. Camera orthographic elevated 35 degrees looking down. Preserve exactly same adult male identity, short dark hair, olive field jacket fabric and collar/seams, taupe cargo trousers/belt/pockets, brown leather gloves and boots, fine weathering, realistic premium hand-painted game illustration, warm upper-left lighting, transparent alpha. Head show back of short hair, one ear, at most a sliver of cheek; NO frontal eyes. Jacket torso shows broad plain stitched BACK panel, side panel at screen right, NO front zipper or front chest pockets facing viewer. Pelvis shows back trouser seat and back belt, NO front buckle facing viewer. Boots toes recede toward top-right, visible heel and outer side. Separate straight vertically oriented parts, upper joint at top lower joint below, no bent poses or complete people. EXACT original 4 columns x 4 rows, row-major parts: 0 bare head with short neck, 1 torso no arms, 2 pelvis no thighs, 3 right upper sleeve, 4 right forearm sleeve, 5 right gloved hand, 6 left upper sleeve, 7 left forearm sleeve, 8 left gloved hand, 9 right thigh, 10 right shin, 11 right boot, 12 left thigh, 13 left shin, 14 left boot, 15 EMPTY transparent. Each isolated part centered and fully inside own cell with ten percent transparent clear margins. 2048-class square. No grid, borders, labels, text, UI, floor, cast shadow, backdrop, weapons, armor, helmet, gas mask, backpack. Maintain detailed matching clothing and human anatomy. Joint ends overlap-ready.
```

### Back initial; input BackRight

```text
Use case: identity-preserve. Edit target: ASHFALL 15-component paper-doll body atlas. Change ONLY VIEW DIRECTION to TRUE STRAIGHT BACK view. Survivor faces SCREEN TOP, rotated 180 degrees from front, back squarely facing viewer. Orthographic camera elevated 35 degrees looking down. EXACT 4x4 separated part cell layout from reference. Preserve original male survivor, short dark hair, olive canvas jacket with realistic cloth folds and sewn back yoke, taupe cargo trousers/belt/pockets, brown leather gloves and laced boots, rich fine painterly realistic premium 2D game illustration, same warm upper-left lighting, transparent alpha. Head MUST show back of hair and nape, symmetric ears, NO face/eyes/nose. Jacket torso shows symmetric wide BACK PANEL and back collar, no zipper/chest pockets, NO arms. Pelvis symmetric trouser BACK SEAT with rear patch pockets and rear belt, no front buckle, NO thighs. Boots face away directly up-screen: visible back heel, heel seam and sole, toe receding behind ankle; NO front laces facing viewer. All straight separate body segments vertical with upper joint at top and lower joint below, right and left maintained. Same row-major cells: 0 bare head/short neck, 1 arm-free jacket torso, 2 pelvis, 3 right upper sleeve, 4 right forearm sleeve, 5 right gloved hand, 6 left upper sleeve, 7 left forearm sleeve, 8 left gloved hand, 9 right trouser thigh, 10 right trouser shin, 11 right leather boot, 12 left trouser thigh, 13 left trouser shin, 14 left leather boot, 15 EMPTY TRANSPARENT. Each isolated one component centered entirely inside its own cell with ten percent transparent margin; no border crossing, no touching. Square 2048-class. No complete human figure, no extra objects, weapons, armor, helmet, gas mask, backpack, text, UI, grid, border, floor, backdrop, cast shadows. Keep rich delicate seams and weathering, not plastic/3D/chibi. Joint cuts clean overlap-ready.
```

### Back repair; input Back initial

```text
Use case: precise-object-edit. Edit target: attached back-view ASHFALL parts atlas. Correct ONLY the THIRD ROW of the 4x4 atlas, which is currently missing the left hand and has misplaced parts. Keep rows one, two and four completely unchanged, keep head/jacket/pelvis and boots in straight rear view. The third row MUST contain FOUR different isolated objects in this EXACT order: third row FIRST column = LEFT BROWN LEATHER GLOVED HAND (wrist at top, fingers hanging down); third row SECOND column = RIGHT TROUSER THIGH from hip to knee; third row THIRD column = RIGHT TROUSER SHIN from knee to ankle; third row FOURTH column = RIGHT BROWN LEATHER BOOT viewed from its BACK, heel seam visible, toe points away/up-screen. Repaint and move those components to correct cells. Do NOT place thigh in first column. Do NOT leave fourth column of third row empty. Keep FOURTH row as left thigh, left shin, left rear-view boot, final empty transparent cell. All 15 expected body parts must exist, exactly one per specified cell. Same high-detail olive canvas/taupe trouser/brown leather art and alpha. 4 equal columns x 4 equal rows, all isolated with ten percent margins, no grid/text/extra objects/background. Camera 35 degree elevated rear orthographic. Keep rest unchanged.
```

### Front spacing; input Front initial

```text
Use case: precise-object-edit. Edit target: attached ASHFALL FRONT paper-doll 4x4 body components atlas. CORRECT SPACING ONLY. Preserve all fifteen original painted parts exactly as-is (face, cloth folds, jacket, colors, anatomy and FRONT direction), but SHRINK EACH PART TO 75 PERCENT OF ITS CURRENT SIZE and independently recenter each in its own exact mathematically equal grid cell. 1254x1254 square, FOUR columns and FOUR rows. Each cell approximately 313x313. All opaque content in any cell MUST stay within a centered 230x230 box; keep at least 40 pixel transparent margin at top, bottom, left and right. Do not enlarge a short component. Resize uniformly so original shape isn't stretched. Keep the exact 0..14 row-major order: head, torso, pelvis, right upper sleeve; right forearm, right glove, left upper sleeve, left forearm; left glove, right thigh, right shin, right boot; left thigh, left shin, left boot, EMPTY transparent. Each part appears ONLY in its own cell, not touching any cell border. NOTHING within 40 px of imaginary row or column dividers. This is production UV slicing spacing correction, not a new character design. No changes to painterly details or identity. No text, labels, grid, extra objects, weapons, armor, whole body, backdrop or shadows. Genuine transparent background.
```

