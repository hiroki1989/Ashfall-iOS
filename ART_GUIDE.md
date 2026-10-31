# Obsidian Sanctum / art revision

組み込みimagegenを使用して新規生成したオリジナル画像です。プロジェクトにコピーし、UnityのResourcesから実際に読み込んでいます。キャラクター画像は元のアルファ透明度を保持しています。

## 保存先と最終プロンプト仕様

- `Assets/Resources/Art/ObsidianFloor.png`: シームレスな正方形の石床テクスチャ。真上から見た平面のみ。古い黒曜石・玄武岩の不規則な石板、亀裂、欠け、控えめな青緑、青銅の幾何学象嵌、小さな苔。描き込まれた筆致と輪郭線。壁、人物、物体、落ち影、文字を含めない。
- `Assets/Resources/Art/VaultKeyArt.png`: オリジナルの地下黒曜石宮殿の横長キービジュアル。溝彫りの柱、青銅彫刻、エメラルドの結晶、琥珀色の火鉢、霧。右側1/3に黒と青緑の鎧、青銅の長銃、深紅のマント、緑のバイザーを持つ覆面の銃士。左側はメニュー用の暗い余白。鮮明な輪郭と角張った絵画的ハイライト。既存キャラクター、文字、ロゴを含めない。
- `Assets/Resources/Art/RunnerDirections.png`: 透明背景、4列×2行、8方向の同一銃士スプライト。暗い青緑の精緻な鎧、青銅の兜飾り、深紅の短いマント、革のブーツ、緑の目、彫刻入り長銃。全身、同じ縮尺、俯角35°の正投影。上段は画面の下・左下・左・左上、下段は上・右上・右・右下。強い輪郭、精緻なペイントとセル調の陰影。余白あり、背景・床・影・文字・枠を含めない。
- `Assets/Resources/Art/SentinelDirections.png`: 同じ8方向・透明背景・4列×2行の配置。黒曜石の地下宮殿を守る不死の歩哨。黒と暗赤色の重ねた板金鎧、青銅の肩甲、破れたワイン色のタバード、角のある髑髏兜、琥珀色の目、火の銃口を持つ青銅の連射砲。成人の比率、角張った筆致、輪郭線、暖かいハイライト。既存作品の人物を含めない。

## 実装

追加画像 `Assets/Resources/Art/SanctumProps.png` は組み込みimagegenで生成した透明背景の4列×2行の部品アトラスです。最終プロンプトは、斜め35°の正投影、黒曜石・青緑の石と古い青銅、描き込まれた筆致と輪郭、暖かいハイライト、各部品を独立させ余白を確保、床・影・背景・文字を含めない指定です。上段に柱・石棺祭壇・火鉢・折れた柱、下段に翼のある守護像・入口アーチ・水晶群・青銅の壺を指定しました。柱・祭壇・火鉢・守護像・水晶を実際の背景に使用しています。

背景は描画と当たり判定を分離した3Dです。方向別スプライトは透明余白を自動的に切り詰め、足の位置をピボットとして配置します。表示する方向は照準とカメラの関係から決めます。影だけを描く鎧モデルを併置し、祭壇などの奥に入った際の隠れ方には深度判定を使います。

キャラクターは8方向であり、歩行・ダッシュ・被弾の専用フレーム列を作った状態ではありません。現時点の動きは移動による揺れと方向の切り替えです。Hades相当の完成度を目指す次の制作では、各動作のフレームアニメーションと背景部品の作画拡充が必要です。

## 0.2 / 独立した照準銃

組み込みimage_genで元の8方向アトラスから銃を除去し、手と前腕を描き直しました。透明度、鎧・マント、方向、4列×2行の配置を維持しています。元画像は保存し、実際のキャラクター表示は以下の新しい画像を読み込みます。

- Assets/Resources/Art/RunnerUnarmedDirections.png
- Assets/Resources/Art/SentinelUnarmedDirections.png

銃はVaultArt.Weaponで別途構築し、AimedWeaponで照準へ連続的に回転します。銃口が実際の弾の発射位置です。反動・銃口の火花・レア度の発光色を表示します。

両画像の最終プロンプト（編集対象はそれぞれ元の主人公・歩哨アトラス）:

Use case: precise-object-edit. Edit target: the attached game sprite sheet. Remove every firearm and barrel from all eight characters. Keep exactly eight characters, their same armor, red cloaks, helmets, original facing directions and stance, and the existing clean 4 columns by 2 rows sprite-sheet layout. Repaint hands and forearms naturally resting bent near their waist or chest, EMPTY HANDS, no weapon or object. Preserve the detailed painted dark-fantasy art, original proportions, feet positions, equal cell sizes and isolated full body silhouettes. Transparent background with real alpha. No text, no extra characters, no guns anywhere. Production sprite atlas for independently rotating gun models.

### WeaponAtlas.png

組み込みimagegenで生成した透過の縦2セルアトラス。上が主人公の翡翠発光カービン、下が敵の琥珀発光砲。右向き、同じ水平軸、手やキャラクターを含まない。保存先: Assets/Resources/Art/WeaponAtlas.png。

最終プロンプト:
Use case: stylized-concept. Asset type: transparent weapon sprite atlas for a hand-painted isometric dark fantasy shooter. EXACTLY two isolated weapons in a clean 1-column, 2-row grid, each weapon perfectly horizontal and pointing RIGHT. Top row: ornate long carbine, dark teal-black steel receiver, engraved aged brass body, long black barrel, slender jade-green luminous rail, carved filigree, red leather grip and dark stock. Bottom row: heavier demonic sentinel cannon, dark blackened bronze, layered Gothic engravings, a skull detail in receiver, cylindrical barrel, ember-orange muzzle and red leather. Beautiful fine painted highlights and inked contours, sophisticated game illustration matching a detailed armored dark fantasy gunslinger. Entire weapon only, no characters, no hands, no background, no shadow beneath, real transparent alpha, no text, no borders. Leave clear equal transparent margins in each half. Crisp high detail silhouette, perspective nearly side-on with a slight view of upper surfaces. Same scale and aligned horizontal muzzle axes. Stocks at left, muzzles at right. No upright arrangement.

## 0.3 / 連続回転する立体鎧と手銃リグ

2026-10-11。8方向画像は保存し、ランタイム人物は連続回転する3D鎧、左右の肩・肘・手の幾何学二節リグ、グリップsocket固定の3D銃へ変更。胴鎧は面取りした独自メッシュ、肩は3枚の重ねた金属板、マントは折れと裾を持つ両面メッシュ。防具5部位の色と追加形状を個別に適用する。

Assets/Resources/Art/ArmorSurface.png は組み込みimagegenで生成した黒曜青緑金属・古い青銅の彫金素材。RGBA不透明、Repeat、mipmap有効。人物と銃だけに接続し、環境床素材は維持。面取り胸鎧にもUVを付与。レア度色は控えめな染色として適用し、鏡面反射は使わない。

最終プロンプト:
Use case: stylized-concept. Asset type: square seamless tileable albedo texture for solid 3D armor and engraved guns in NEON VAULT, hand-painted dark fantasy isometric game. Primary request: close flat orthographic material surface, blackened steel and dark obsidian teal metal, finely etched aged brass filigree in sparse thin flowing geometric curls and angular decorative motifs, subtle hammered grain, fine edge scratches, tiny patina, restrained painted brush strokes and inked etching. Composition: entire 1024x1024 image is one uninterrupted planar metal material, seamlessly tileable at all four edges, evenly distributed microdetail; no standalone objects, no bevel frame, no visible panel borders. Palette: mostly neutral charcoal gray with muted blue-green, restrained antique bronze lines, medium subdued brightness so runtime color tint remains usable. Lighting: flat diffuse baked albedo, no directional illumination, no specular hotspots, no shadows or ambient occlusion. Avoid text, letters, words, logos, weapons, characters, gemstones, strong gold expanses, purple, pink, photorealistic shiny chrome. Fine crafted painterly production game texture, seamless repeat.

## 0.4 ItemPortraitAtlas / 2026-10-12
Built-in imagegen（透明背景）。Assets/Resources/Art/ItemPortraitAtlas.png。
プロンプト: stylized-concept / NEON VAULT用の正方形4×4アイテム画像atlas。文字・UI・罫線なし。手描き3D製品画、黒鉄・象牙・彫刻真鍮・翡翠エネルギー。
配置: 1行目はカービン、二連散弾銃、パルスライフル、精密銃。2行目はプリズム反射銃、衛星環銃、重力砲、予備翡翠銃。3行目は兜、胸鎧、腕甲、脚鎧。4行目は靴、差分兜、差分胸鎧、差分靴。
固有の武器効果・防具部位で画像を選び、個体の色・外観差分とレア度枠を重ねる。能力数値はゲーム内テキストで表示する。0.4最終画像はItemPortraitAtlas-v2.png。再生成プロンプトは元16品の見た目と順序を維持し、等間隔4×4・各区画15%透明余白・隣区画へのはみ出し禁止。生成画像の武器／防具の高さ差はItemPortraitsの個別UV範囲とアスペクト維持で吸収する。
## 0.4 Sculpted armor material / 2026-10-12
組み込みimagegenの新規生成: Assets/Resources/Art/ArmorSurface-v2.png。
プロンプト: NEON VAULT用のシームレス正方形・手描きの青いガンメタル材質。鍛造の筋、細い金の幾何彫刻、控えめな擦り傷、落ち着いた中間調。平面の全面スウォッチ、文字・UI・物体・遠近・発光なし。キャラクターの形を邪魔しない広い静かな面と微細な描き込み。
VaultArmor.shaderで彫刻材質の濃度と金属ハイライトを分け、金の装飾は低いPaintMixで素材差を出す。
## 0.5 / キーアート準拠 32方向×4歩行

組み込みimagegen、元のVaultKeyArtをデザイン基準として生成。プレイヤー: Runner32Walk0..3.png、敵: Sentinel32Walk0..3.png、装備: KeyEquipmentAtlas.png。いずれもAssets/Resources/Artに保存、透明背景。旧画像原本は残す。

プレイヤーの全文プロンプトと原本はDevelopment/HeroArtPrompts.md、装備はDevelopment/EquipmentArtPrompts.md。
敵のプロンプト: 精緻な黒鉄と金彫刻の成人の歩哨、角張った広いつば兜・髑髏面・琥珀の目・深紅の破れたマント。両手は銃を握る空の姿勢、武器なし。俯角35度の正投影、8列4行・32個別方向の360度ターンテーブル。4枚は左脚前接地、左支持通過、右脚前接地、右支持通過の歩行姿勢。各セルに全身と透明余白、床・影・文字・UI・罫線なし。初枚から残り3枚を編集生成し、同じ方向・衣装・配列を維持。

実際の敵画像は前→右→背→左、プレイヤー画像は前→左→背→右なので描画で順序を補正。各画像は生成物のため方向間隔・歩行姿勢の連続性にはばらつきがある。足元を基準にアルファ範囲を切り出し、画面内の身長を統一。銃はグリップと銃口の投影で位置・角度・前後関係を制御。
## ASHFALL / 0.7 Wasteland 3D
Built-in imagegen, original images retained under Codex generated_images and all final images copied to Assets/Resources/Art.
- WastelandSurface.png: seamless square flat diffuse weathered gunmetal, pitted metal, rust-orange scratches, grey patches. No objects/text/borders/directional lighting. Material on 3D weapons and armor.
- WastelandFloor.png: seamless square weathered industrial concrete albedo, fine aggregate, cracks, ash-grey and sandy tones, tiny rust flecks; no objects/grid/text/shadows. Runtime world floor and concrete props.
- WastelandLeather.png: seamless square worn brown leather grain, rubbed scuffs, muted olive-brown undertones, no seams/objects/text/highlights. Runtime field jacket, trousers, pouches, wooden stocks, gloves, boots.
- WastelandKeyArt.png: original ASHFALL wide cinematic industrial wasteland, left dark negative menu space, right full-body adult six-head survivor with goggles/dual-filter gasmask, olive jacket, leather harness, salvage armor and vacuum-tube rifle. Rusted ruined city, yellow-green radioactive fog and distant infected; no text/logos/UI/franchise character.
- WastelandEquipment.png: transparent 4 columns x 4 rows. Left to right top to bottom: carbine/SMG/sniper/pump shotgun; automatic shotgun/double shotgun/belt machinegun/needle rifle; coil rifle/split-jaw ricochet/isotope drone launcher/vacuum suction; helmet+mask/vest/bracers/trousers+boots. Equal cells, original detailed rusted steel/wood/leather/olive canvas/yellow-green isotope designs, no text/UI/grid.
All prompts used stylized-concept and requested original production game textures/icons. Generated alpha is retained. Equipment has tight silhouette margins; use full cell UVs with aspect preservation. KeyArt/floor/surface/equipment were visually inspected; source filenames are unchanged.
Runtime characters are true 3D smooth 24-segment profile meshes, 2.39-unit height / .38-unit head, with six-head adult proportions, articulated shoulders/elbows/hands and knees. 12 distinct 3D gun assemblies selected by Gun.modelVariant, muzzle endpoint updated to barrel length, grip/support sockets retained. Hostile variants use infected skin, eyes, pustules, salvage armor and isotope growths; no fantasy sprites are attached. Old resources/code remain preserved but unused by VaultArt.

WastelandEquipment-v2.png: built-in precise-object-edit of first equipment atlas, square 1254x1254 RGBA, 4x4 cells, preserved 16 object order/style; requested every silhouette inside its own cell with 15% transparent margins and separation of row3 guns / row4 armor. Original v1 retained. Visual inspection: separation greatly improved, but generated margins are not exactly 15%; top of trousers is near row boundary. Use fitted individual UV rectangles where necessary.

## ASHFALL tailored 3D revision / 2026-10-12
人物: 連続円筒UVへ変更し、面ごとに素材全体を繰り返す問題を修正。襟、折ったラペル、ジッパー歯、衣服の圧縮皺、別体ハーネスとバックル、背嚢、巻いた毛布、分割コート裾、靴のレースを追加。防毒面は呼気弁・ゴーグル反射・頭部ベルト・耳保護。感染者は鼻、顎、眉骨、裂けた耳、頬の傷。12銃の機関部を成形メッシュにし、排莢孔、薬室レバー、照準器、トリガーガード、銃口穴、配線を追加。六頭身寸法と両手socketは維持。
WastelandCloth.png: built-in imagegenで新規生成。1254x1254不透明、Repeat、mipmap有効。原本はCodex generated_images/01a12e25-cb56-7ee0-a9df-adacd7ca4e92/exec-1f8dd248-15ff-477e-b784-36678afdda05.pngに保持。出力は織り目・低コントラスト・文字なしを目視確認。
Final prompt: Use case: stylized-concept. Asset type: seamless square opaque albedo texture for ASHFALL 3D field jacket and trousers. A flat uninterrupted swatch of worn olive-grey heavy cotton canvas twill, fine diagonal weave, dusty rubbed fibers, restrained painted microdetail, low contrast broad midtones. Tileable all four edges. Desaturated neutral grey olive for runtime tint. Flat diffuse illumination. No seams, folds, objects, text, borders, shadows, highlights, leather or metal.
人物/銃の最終画面品質は統合ビルド後のQA目視で確認。コードのみで完成品質と断定しない。
改善2パス: 統合actor-front/side/armored画像から、蛍光レンズと球形肩・膝を修正。レンズ発光を停止し暗い光学ガラスと微小な反射へ変更、肩・膝は薄い連続断面の成形板へ。上衣の胸/腰断面を絞り、側面の布の圧縮皺を追加。再ビルド後の目視が必要。
敵造形の最終パス: 全敵一覧の目視指摘に対応。skinを低明度の灰/褐色/くすみ緑へ。大型肩と背は非発光の異形肉Profileと骨隆起、腹膨張2種は服の中央面を非表示にして裂けた側面布と張り出す腹Profileに置換。大型の膿疱発光を除去、目のみ小さく低強度発光。kind3/7/9は歩行アニメーション配下に固定前傾姿勢を追加し巨大肩背/太い腹/細身前傾脊柱を分離。英雄造形は変更なし。

## ASHFALL adult player sculpt / 2026-10-12
プレイヤーのみPlayerSculpt.csの専用meshへ移行。成人skullは顎1.398〜頭頂1.719（.321）、兜頂1.73 / 靴底約-.661、約7.45頭身。肩socketは±.32 / 1.16で右肩構えIKと互換。敵の旧造形は維持。
WastelandEquipment-v2.png下段を形状参照。48周断面×区間5補間の連続曲面、前後別輪郭、superellipse断面、局所表面変位で縫製した上着/パンツ/袖を形成。16×20の厚み付き曲面板で胸/膝/脛/ポーチ蓋、長手方向の踵・甲・爪先断面で靴を形成。球/円柱への飾り付けではなく主輪郭から再構築。
兜の張り出した縁・頭頂補強帯・耳保護・リベット、防毒面の顔に沿うゴム、金属リング・丸い左右フィルター・格子、ベスト/胸金属板/肩カップ/前面ポーチ/バックル、腕を一周する金属ブレーサーと締め革、紐靴/厚い靴底/爪先・踵防具を実mesh化。5防具部位の有無/色差分とGeneratedMeshLifetimeは維持。文字を画像へ追加せず既存原本を使用、新規画像生成なし。
最終画面品質はPMOの統合とQAの接写/実画面確認で判断。成人頭身・材質分類・IKの参照mesh名もQAへ連絡する。

## ASHFALL layered painted player / 2026-10-12

ユーザー指定によりプレイヤーの可視3D人物・銃を撤回。Assets/Resources/Art/LayeredPlayerへ、本体15部位×5原画方向、防具2セット×8部品×5原画方向、武器12種×8方向の透明原画を新規生成。左右3方向は本体・防具の鏡像と左右部品の交換で作る。旧3Dソース/原画/アーカイブは保持。

制作はimagegenスキルと組み込みimagegen。Body/Armorは4×4セルの1254×1254 RGBA、武器は同じサイズの6枚（各2種×8方向）。原画の細部と透明余白を確認し、数学的セルからはみ出した部品はROI JSONで隔離。BodyRegions/WeaponRegionsは全Texture基準の上から下の座標、WeaponAnchorsは元セル基準の実握り/支持/銃床/銃口座標。

記録: Development/LayeredBodyArt.md、Development/LayeredArmorArt.md、Development/LayeredWeaponArt.md、Development/LayeredWeaponAnchors.md。原本は各文書に記載し削除しない。文字・UI・罫線は原画へ焼き込まない。

アニメーションは独立SpriteRendererと関節姿勢の補間。歩行64・待機32・射撃32のサンプルを8方向で合成し、画像のopacity混合はしない。1024種類の個別描き直し原画ではなく、緻密な部位原画を動かした実合成コマ。通常は低構え、発射でサイト姿勢へ移行。体右側のグリップと支持手、武器の実銃口から発射位置を同期する。

最初の実合成では巨大銃・腕の伸び・直線的な膝を確認。武器は握り〜銃口ではなく銃床〜銃口の全長基準、部位は関節間の距離/天然軸/人体幅で合わせて修正。待機の重心と屈膝、走行の前傾・肩腰の逆振り、足運びは動画と8方向接写で評価する。参考はSupergiantのHades公式画面/動作で、素材や人物デザインは独自。
接写最終パス: 頭部rigを-.028y、襟を+.020yで首の隙間を短縮（skull自体.321維持、約7.36頭身）。前腕の背面板脇に実径約.018の小型黄緑ガラス管/上下金属socket/2本保護railを追加。ゴーグルに細い黄緑反射弧、専用ガラス名はPlayer isotope glass lens / Player isotope glass bracer tube / Player goggle glass reflection。足の爪先金属殻が革meshに埋まらないよう外面寸法を調整。大きい発光形状は追加しない。

## ポップな2D描画 / 2026-10-13

現行はArmorV2の5方向・2セット、敵Enemy_0..9の5方向・各上半身と左右脚、計150領域を使用。原画の制作記録はDevelopment/ArmorEnemyArt.mdとEnemyArtB.md。武器は12種×8方向の原画を維持し、実握り位置に別レイヤーで接続する。

ユーザーの最新指定は腰だめ射撃。旧サイト姿勢指定を置き換えた。歩行64・走行64・待機32・射撃32×8方向=1536合成姿勢を補間する。接地半周期は移動の逆方向へ足を送る支持、残り半周期は持ち上げて振り出す遊脚とし、走行の半歩幅.60・前傾.34を使う。全コマの個別描き直しではない。

LayeredPlayerSprite/EnemySpriteは原画の色を実行時に強調し、5段階の色面と画面上約1.45px以上の黒輪郭を加える。敵の暗い原画は中間色を先に持ち上げ、黒を輪郭に残す。PopItemPreviewは同じ原画を余白付きで表示し、武器輪郭のROIを適用、外周の黒線と鮮やかな色を加える。12種の武器配色は戦闘とアイテムで共有。元PNGは加工・置換しない。この処理は新しいトゥーン原画への全面描き直しではなく、既存の細密原画を使った描画表現の変更。

防具は鋼鉄青/オリーブ/銅/青緑ガラスと、深紅/紺/琥珀ガラスの2系統。全所持品をそれぞれ固有原画にしたものではない。左右鏡像の差、背面腕、敵の胴脚継ぎ目と射撃支持手は追加改善余地がある。判定は実描画とモーション出力で行う。

## 強いトゥーンとカラーパネル / 2026-10-13
背面（3/4/5方向）は低構えでも上腕・前腕・肩つなぎ・手袋を胴体の奥に置く。肩の輪郭から外に出る部分だけ自然に見える。腰の原画segmentは高さ.41、正面幅目標.49、左右脚に重ねる。銃床の右offsetを.14へ、両肘を下げて軽く内側へ寄せる。戦闘カメラのorthographicSizeは9.8→7.5、見かけの人物サイズは約1.31倍。

3shaderはsourcealphaとclipを保持し、近傍の色・明度差を使って微細ノイズを軽く平滑化。色面は5明度帯、黒輪郭は最低1.8画面px/2.8texel。暖色優勢を30%中和し、AccentColor/AccentStrengthで輝度を維持した塗装パネルを差し込む。服紺/青.35〜.48、防具各slot/variant別青・シアン・橙・紫・深紅.78、銃12種vivid palette.82。金属の明るい反射と暗い内部線を残す。アイテムBlitにも同じAccentを設定しcachekeyへ装備種別を含める。画面全体の暖色post tintも撤回。原画は変更せず、画像再生成ではなく描画の変更。

## 素材ごとの多色原画と洗練パス / 2026-10-13
装備全体に単色を乗せるAccent処理は撤回。ArmorV3_5方向、WeaponsV3_6枚（12種8方向）は元PNGから組み込みimagegenのcolor-only editで再制作。金属/革/木/布/塗装/レンズを個別に塗り分け、ボルト/パネル継ぎ目/機関部/皺へ内部黒線とベベルの陰影を残す。SourceHueを保持し、表示側は彩度1.08と明度整理35%のみ。元V2/Weapons原画は保存、新ColorArtがPaletteReady markerで一括使用される。

元画像と新画像の透明形状は完全一致ではない（最大weapon bbox差7pixel）。PMOが全11画像をalpha解析、80armorROI/96weaponROI/96semantic anchorsを再測定。PNGの切り貼り/resizeはしていない。原画の制作共通specはDevelopment/MulticolorWeaponArt.md、主担当補完分と原本はDevelopment/MulticolorIntegration.mdへ。停止した担当の細部promptに未保存分があるので正確な全call記録と断定しない。

歩行では脚・脚防具の幅を一定にし、視点奥行きの足運びを抑え、脚の伸長を制限。左支持腕を銃の手前へ。正面/背面の短縮投影でstock/muzzleのy順が反転しても銃全体を180度回さず、左右をmirrorしてroll上限70度、正面後面22度。全96武器姿勢を実撮影してroll検査、表示BBoxにも全長上限を設ける。射撃は以前より.09だけ高い低構え。低構え検証は斜め方向の肩投影差を考慮して肩中心より下を確認する。

敵は上半身の解剖腰と脚上端のalpha重心をpivotにして共有腰へ接続。死亡は方向freezeと被弾方向よろめき→膝折れ→側方沈下（.85秒）、最後30%で消え、判定/lootは即時。ステージはdepth-normalによる面境界の墨線、床textureの高周波抑制、壁capの二重縁とサービスベイmarkings、寒暖を分けた照明。UIはnavy ink/高い文字contrast/選択色/カード影。構造・装備能力・日英・セーブは保持。

## 現代の傷ついた装備・右肩支持 / 2026-10-13
最新版はTacticalArt/ReadyによりArmorV4_5view/80partsとWeaponsModern_6の16viewsへ接続。現代的な野戦服・革ストラップ/ポーチ・傷錆の小肩板/丸膝板・防毒面/ゴーグル・少量の琥珀瓶を基調とし、以前の翼肩/宇宙装甲を撤回。布/鋼/革/ガラスの色は原画通り、多色のV3武器12種類は維持。現代カービンFIELD CARBINE/フィールドカービンとポンプ散弾BREACH PUMP/ブリーチポンプを追加、14種類×8方向。新原画は組み込みimagegenで生成、Assetsへ保存。全prompt・生成原本・ROI精度はDevelopment/TacticalArmorArt.md、Development/ModernWeaponArt.md。

太腿.31/すね.25/boot.265の方向投影幅を一律に維持し、動作中の急な拡大を防ぐ。肩半幅.265/肩つなぎ.185×.175へ、背面膝/足の左右間隔を維持。銃床の実画像semantic stock点を解剖学的右肩へ置き、actualgrip/supportへ両手を接続、反動は肩支点の小回転。射撃時はsourceUVの銃床側領域と胴体の投影capsuleとの重なりだけを隠し、銃身を同時に隠さない。装備画像は同じ実原画。個別1536枚の描き直しではなく関節合成の制約は維持。

参考調査: RUINERの開発者インタビュー https://www.unrealengine.com/blog/unmasking-the-motives-and-mayhem-in-ruiner-copy 、SYNTHETIK公式 https://www.synthetikgame.com/ とweaponfeatures https://www.synthetikgame.com/weapons 。公式画像/動画取得は一部制限、両作品の全歩行コマを解析したとは主張しない。主な姿勢基準はユーザー添付の現代装備と右肩支持指定。

## 4系統防具・敵背面脚・走行方向 / latest
TacticalArt/ArmorV5_5方向はArmorVariantsReadyでvisualVariant2/3の新kitへ。元V4を保持して4系統、runtime/imageicon同source。新砂漠偵察/市街突入はhelmet/visor/platecarrier/molle/pouches独自形状、2色置換ではない。imagegen原本全5保存/native1254RGBA、元window最大alpha成分80ROI。プロンプト全記録Development/ArmorVariantsV5.md。EnemyBackLegsV2は5×4の20parts、全10種真背面の踵/膝裏。左右は原画実toe方向に基づくview/kind例外を適用、元素材も保持。Source画像nativealphaは変更せず、QA足下stray片はROI成分boundsを再測定で除去する。
走行runheelpeak約.41・walk約.23、boots折上げ/次接地軸、正背膝screen横曲げ制限。d7右肩銃床維持・rightelbowflank・distalforearmだけgun手前のclipbridgeで孤立handを接続。QuarterviewworldbasisW右上/S左下/D右下/A左上、射撃時はmouse独立aim。影/素材の連続性は既存NPC3Dと2D部位合成で差があり同等の完成度とは断言しない。

## 片手銃と銃軸 / 2026-10-14
全18銃は機関部後端/銃床→銃口の原画実アンカーを銃軸に用い、握り→銃口を軸にした方向別の誤回転を修正。両手銃は右肩stockmount、片手=model1/5/14..17は右gripmountで自由左腕。低構えの銃傾き8°。新HandgunArt2PNG/32方向は組み込みimagegen原画、32領域・32アンカー・Readyで実銃とアイコンへ接続。制作仕様/原本パス/生成方向の補正はDevelopment/HandgunArt.md。nativePNG加工なし、alpha主成分からセルを越える外形を採取する。モーションは関節合成の64/64/32/32サンプル。各コマ個別原画ではない。

## 承認インク画の実描画 / 2026-10-14
InkedArt/ReadyでBody5方向・Armor0/1の2kit5方向・現代銃12/13を新nativePNGへ。GameとInventoryが同sprite、Layered/Item shaderは原画色と内部線を直接保持。制作prompt原本対応表Development/InkedRuntimeArt.md、alpha非加工測定scriptとROI分離記録InkedArt JSON。QA最終は肩突出/片手free-arm/両手支持肘/足幅を目視で確認、InkedApproved448paired＋Item38/Smoke9072。5方向mirror/関節合成制約と非対称左右反転可能性は維持し、完全8方向立体設計や全コマ独立描画とはしない。

2026-10-16 WardenEquipmentAtlas-v3.png: imagegen edit WastelandEquipment-v2 + ApprovedFieldkit/Portrait_Weapon_12 reference。既存4x4/全16designを保持、Wardenの細いink/写実比率/セル陰影へ統一。透明alpha、文字なし。旧原本保存。銃0..11のinventoryへ接続、その他個別絵は未統一。

2026-10-17 SurvivorMaterials/SurvivorSurfaceAtlas-v1.png: 組み込みimagegen生成のcanvas/leather/steel/walnut 2x2素材。既存装備デザイン/イラストは保持。DetailedHeroInk3D object-space triplanar/mipmap合成。最終promptと原本はDevelopment/SurvivorSurfaceArt.md。表面画像は造形一致の証明ではなく、実ゲーム8方向で別途確認。

2026-10-17 CreatureSurfaceAtlas-v1.png: 組み込みimagegenで皮膚/暗灰hide/ivorybone/plummembraneの4素材生成。既存Enemy_0..9原本を設計基準として保持。完全promptと照合表はDevelopment/EnemyReferenceRebuild.md。形状再現は未完、単なるtexture更新を造形完成とはしない。

## 2026-10-17 顔と廃墟都市
素体に連続顔面の鼻翼/眉骨/頬/眼窩、閉口、目、短い無精髭を追加。新Environment/RuinSurfaceAtlas-v1はconcrete/steel/brick/asphalt、AshfallCityMap-v1はUI文字なしの都市全域俯瞰図。imagegen原本と完全promptはDevelopment/WorldArt21.md。都市地点/日英/UIは画像へ焼き込まず別描画。背景は既存solidセル内の廃墟detailと低い瓦礫、専用triplanar素材。実画面検証前に完成と判断しない。

## 2026-10-17 六社メーカー
6社の独自幾何ロゴはManufacturerBrandingへコード定義しUI/装着3Dデカールで共有。新設計製品は既存の基礎造形を保持して社別の金属/革/薬液/コイル等を増設、全体の単色染めをしない。新設計のカード画像は装着モデルの同一factoryから描画。概要と正確な製品数/基礎形状再利用の範囲はDevelopment/Manufacturers22.md。

## 2026-10-17 inventory illustration
銃/防具cardsを3D撮影から既存イラストへ戻す。modulecardsは組込みimagegen新24点(6社×4系統)painted atlasを使用、maker/effectで切り出し。社章は別UI。製品ごとの固有絵を全550枚生成したものではない。装着3Dは保持。原本/promptはDevelopment/Manufacturers22末尾。

## 2026-10-21 Warden hidden surface source repair
Built-in imagegen skill used for arm-free torso ±X and isolated inner leg texture projection sources, saved Assets/Resources/Art/GeneratedWarden/HiddenSurfaceSource. Existing equipment design is authority. The clay templates are orthographic extracts of armor-rebuilt.fbx. Torso sources omit the occluding arms/hands; leg v2 has trouser upper57%, tall leather boot lower43%, no inner kneeplate/pouch or duplicate laces. Positive inner leg uses mirrored v2 registered to its own template. Rejected v1/source attempts retained. Prompt/registration/camera details in source README/prompts.json and D:/Unity/AshfallArtValidation/hidden-surface-art. Atlas bake repairs hidden side surfaces, retains exposed original art; geometry/rig unchanged. This replaces false cuffs/hands and foreign-leg details baked into occluded surfaces. Fine seam/color matching is not guaranteed perfect.

## 2026-10-22 素体の固定UV修復
素体はカメラ方向で全身写真を切り替えない。側面sourceは腕で隠れた胴・腰をimagegenで補完し、最終メッシュを正投影した図の寸法へ合わせて固定UVへ投影する。新source/生成記録はAssets/ArtSource/LowPolyProof/SideSurfaceRepair。側面を下垂腕付き写真で上書きすると腕の絵が胴に残るため禁止。手順は承認済みHunyuan元形状をCPUのBlenderで修復して再利用し、Hunyuan/AIテクスチャモデルのローカルGPU生成は行わない。頭は承認済みhead-rebuiltを素体/装備で共用し、マスクは別装備として扱う。頭部専用bake候補画像は原本として保存するが採用モデルの頭を置換しない。

## 2026-10-26 FieldKit UI refresh
Built-in imagegen: field-kit-v1/v2.png (4x4 atlas, rifle/reload/hand/knife/backpack/map/camp/tower/chest/skulls/med/cog, two button plates, stick pad, card frame); map-paper-v1.png subtle graphite cartographic paper. No localized text baked in. Originals retained. Runtime UV crop via FieldUIArt; alpha preserved. Source prompts in current task; imagery uses graphite, ivory, amber/teal, restrained wasteland ink. Assets/Resources/Art/UI/FieldKit.

2026-10-26: built-in imagegen field-kit-bright-v1.png, same 4x4 16 asset contract, bright ivory enamel/coral/mint/slate, no text, transparent alpha preserved. Original C generated image copied Assets/Resources/Art/UI/FieldKit.

2026-10-26 imagegen wasteland-materials-v1.png: 2x2 asphalt/concrete/dirt/moss basecolor atlas, orthographic flat, no objects/text, copied Assets/Resources/Art/Environment/Ground. Runtime DistrictGround quadrant samples + noise blend. Original retained.

## 2026-10-27 / Premium field-terminal UI assets
Built-in imagegen. Sources retained in Codex generated_images. New resources: Assets/Resources/Art/UI/FieldKit/premium-panel-v1.png, premium-icons-v1.png, premium-map-surface-v1.png.
Panel prompt: one edge-to-edge matte charcoal field-terminal panel, quiet center, restrained copper perimeter and tiny chamfered corners, no text/icons; authored border kept by nine-slice rendering.
Icon prompt: transparent 4x4 equal-cell warm ivory atlas: fire cartridges/reload/hand/knife; backpack/map/camp/dungeon; health/radio/cache/clue; settings/armor/rifle/module. No text or surrounding tiles. Preserve alpha.
Map prompt: flat dark charcoal archival paper with fine linen fibers, quiet 5% tonal variation, no roads/labels/geography. Actual geography is rendered from OverworldGeography; new texture supplies surface only.
Implementation: shared FieldUIArt now uses dedicated nine-slice panels and aspect-preserved icons. Equipment screen uses charcoal, offwhite and copper, illustrated cards and live hero portrait. Map uses staged actual building footprint layer and higher-resolution local cartography. Source generation tool: built-in image_gen (not CLI).

Premium panel v2 (2026-10-27): imagegen production 9-slice square charcoal matte panel. Near-uniform graphite, extremely thin warm gray perimeter, subtle grain, no ornament/glow/bevel/text/icons. Border confined to outer 24px. Source exec-296e946f-932d-4171-9d88-4db591239a3e.png. Runtime FieldUIArt uses v2; v1 retained.

## Premium panel v3 / 2026-10-27
imagegen generated normal and selected assets: premium-panel-v3.png (exec-b1929ffc-12ca-429b-a19c-7505de582961) and premium-panel-selected-v3.png (exec-38a9afb5-14da-4507-8af0-1e3787a29a28). Prompt: single full-canvas orthographic square production UI, charcoal diagonal gradient with upper-left reflected illumination fading to near black lower right, fine matte texture, extremely thin 6px-inset rounded perimeter line, subtle inner shadow. No text/icons/ornaments/bevel/neon/scifi corners. Normal graphite blue-gray, selected warm rusty copper border and broad restrained amber inner gradient. Border details confined to outer20px for nine-slice. Runtime uses 2.2% source corners, 8-unit destination border, 62% normal / 68% selected tint to restrain brightness. No UI text baked in; scores rendered as live bold text over item artwork.

## Aerial city cartography / 2026-10-27
New imagegen aerial-surfaces-v1.png, source exec-df4b1d78-d797-4899-8f7f-477945a2e420. Prompt: exact2x2 square material swatches, top-left weathered roof/concrete, top-right asphalt, bottom-left muted natural canopy/grass, bottom-right desaturated river ripples. Perfectly overhead photographic material microtexture, no labels/text/icons/roads silhouettes/large objects, no baked map positions. CPU readable512px source grain sampled by AerialCityMapBaker. The actual map is baked from OverworldGeography shared roads/river/building footprints/cars/trees, with parapets/ridges/service boxes/canopy shading and shadows at4096 resolution. UI text/markers are live overlays; navigation geography is never inferred from generated artwork. Bake through Editor, no GPU scene capture/model generation. Runtime uses compressed imported PNG for low cost zoom/pan.
Aerial roof atlas v1: imagegen exec-bd9968d0-a080-40e0-bef5-6af5d1b1e84d, exact4x4 straight-down roof materials: weathered industrial HVAC/skylights, tan apartment parapets/staircases, residential corrugated/slate, damaged office roofs/exposed rooms. No text/roads/trees/labels/perspective. Applied only within exact generated building rectangles. Bake upgraded8192 RGB24 CPU encode with no full GPU upload; CPU arrays released before PNG encode, runtime iPhone ASTC6x6. Terrain first then shared-world roof/trees shadows prevents chunk-edge erasure.
Final pipeline:4 independent4096x4096 tiles covering10km square (effective8192²), baked sequentially in headless Editor. Sources CPU-readable1024; tiles non-readable/compressed, iOS ASTC6x6. Aerial import branch in VaultArtImporter prevents generic2048 downsize/uncompressed override; ProjectBuilder preserves this profile. Roof styles are illustrative materials inside exact footprints, not a literal full-world GPU photograph. High-detail tiles rendered over coarse overview with UV intersection to prevent seams. Fullmap and minimap share the tiles. World road graph, river centre/variable-width, buildings and trees drive the bake; labels/icons stay live UI.

## 2026-10-27 gun-class-icons-v1
imagegen: transparent warm ivory side-profile handgun/pump shotgun/assault rifle/belt-fed heavy machine gun/scoped sniper rifle atlas. No text or UI labels. Source exec-155ffa4d-aec1-4aa7-bbdb-dec42b6ee2ee.png preserved. Nonuniform authored silhouettes sampled with dedicated UV crops and aspect fit; combat/inventory/merchant shared FieldUIArt.GunClassIcon.

## 2026-10-27 manufacturer insignia
imagegen six original engraved industrial marks without text; sheet exec-c1796232-8554-4cbd-b898-edf6f783c748.png. Six fixed cells exported as maker-emblem-v1-0 through 5.

## Manufacturer wordmarks v2
User explicitly requested trademark-style textual logos. Imagegen source exec-68fb755c-de33-4808-8d26-3192c8bdfade.png; six manufacturer emblems with letterforms and palettes saved as maker-wordmark-v2-0..5. This text is part of each brand logo; interface labels remain separate localized text.

## Action skill illustrations v1
201: Imagegen source exec-11cdcb53-d4c2-4e1e-b2b2-b7396f61f0b2.png: three military field devices (sentry, grenade, healing emitter), aged copper and charcoal with no text. `Assets/Resources/Art/action-skills-v1.png` uses three equal horizontal cells for the inventory skill selector. Labels and values are rendered separately in the UI.

## 2026-11-01 / Dialogue standing portraits (MGS brush-pen ink & color wash)
Built-in imagegen. Originals retained in `D:/Unity/PortraitsOriginal/` and artifacts directory; 1024x1024 PNGs saved in `Assets/Resources/Art/Portraits/`:
- `portrait-knox.png`: Protagonist Knox (left-facing 3/4 bust for right side of dialogue screen), based on `WastelandKeyArt.png` (messy brown hair above dual-filter brass gasmask/teal goggles, tattered scarf over explosive collar, olive jacket, rusted riveted pauldron, two glowing amber vacuum-tube vials).
- `portrait-vale.png`: Malcolm Vale (right-facing 3/4 bust for left side of dialogue screen), based on `RaiderBossNPC` 3D model + dual-filter gasmask/teal goggles, crocodile-leather studded vest, gold hexagonal subdermal cybernetics.
- `portrait-raven.png`: Red Claw boss Raven (right-facing 3/4 bust), based on Red Claw 3D model + dual-filter gasmask/teal goggles, crimson-rust dome helmet with side rails, riveted rust-red pauldron, bandoliers.
- `portrait-vex.png`: Coyote boss Vex (right-facing 3/4 bust), based on Coyote 3D model + dual-filter gasmask/teal goggles, slate-blue helmet, navy/slate jacket with ochre-gold harness straps and circular rust-orange shoulder shield.
- `portrait-sera.png`: Merchant Sera (right-facing 3/4 bust), based on Warden NPC variant 0 3D model + dual-filter gasmask/teal goggles; middle-aged, stocky, broad-shouldered veteran gunsmith matron with graying hair under olive Warden helmet, heavy leather/steel gunsmith apron over steel-blue/olive field jacket, pipe wrench, brass calipers, pressure gauge, and shotgun shell belt.
- `portrait-briggs.png`: Debt Collector Briggs (right-facing 3/4 bust), based on Warden NPC variant 0 3D model + dual-filter gasmask/teal goggles, olive helmet, high-collar slate/olive coat, brass tally tokens and ledger slate.
- `portrait-gideon.png` (and `portrait-ione.png`): Collector Dr. Gideon (right-facing 3/4 bust), based on Warden NPC variant 1 3D model + dual-filter gasmask/teal goggles; eccentric older male Western ballistics professor & military-ordnance geek with unruly white/silver hair under olive helmet, articulated multi-lens brass optical magnifier loupe, white/steel-blue Aegis armored lab coat with micrometer, ballistic dividers, glass specimen vials, blank brass catalog tags, and inspection tools.
No UI text baked into any portrait. Connected via `VaultDialoguePortraits.cs` across all boss and NPC conversation screens.
