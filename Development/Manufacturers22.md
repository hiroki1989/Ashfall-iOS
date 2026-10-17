# 荒野の6社 / 2026-10-17

独自企業の名称・ロゴ・装備設計。既存作品の社名やマークは使用しない。

|会社|ロゴ|造形・素材|性能の軸と代償|
|---|---|---|---|
|ケストレル精機 / Kestrel Precision|照星を包む二枚の隼の翼|切削レール、狭い放熱孔、青い基準印、象牙色グリップ|精度・装填・機動。単発威力を抑える|
|アイアンウェイク重工 / Ironwake Heavy Industries|金床と航跡|厚い鍛造鋼、鋲、赤褐色革、銅の補強材|単発威力・生命・防護。連射・装填・機動が重い|
|ヴォルトアーク電機 / Voltarc Electric|六角筐体を裂く稲妻|コイル、真鍮導体、セラミック、黄色警告片、紺の筐体|連射・容量・短い待機。単発威力・精度が低い|
|マイアライン生体工学 / Mireline Biotech|二重螺旋の葉脈|オリーブ布、薬液槽、骨白の継手、編組ホース|生命・再生。直接攻撃出力を抑える|
|シンダー再生工廠 / Cinder Reclamation|欠けた歯車と炉の火花|溶接の継ぎ当て、傷んだ橙鋼板、再生木材、不揃いな留め具|散弾・威力。精度・装填・防護に代償|
|イージス兵站局 / Aegis Logistics|二段山形を抱く盾|積層防弾板、青布、白い識別帯、補強バックル|防護・生命・装填の安定。攻撃出力は控えめ|

## 製品数と既存デザイン
導入前の基準は銃18モデル、防具5外観×5部位=25、モジュール4通常+3固有=7。
新カタログは180銃、250防具、70モジュール、50近接武器=550製品。レベル/レア度/ランダム追加特性を別種として数えない。
既存の基礎形状を残し、各基礎形状に10の設計製品を構成。550の全く独立した基礎メッシュを手作業で彫ったという意味ではない。
銃は斥候/要塞/旋風/照準/突破/遠征/急襲/先鋒/試作設計の装弾数・威力・間隔・精度・装填・弾数に差。防具は巡回/防弾/狙撃/突撃/伝令/生存/銃手/先導/衛生/防塁設計。モジュールは70の日英固有名、7系統を保ち出力/待機/追加特性の傾向を差別化。
メーカーのレール/補強/コイル/容器/継ぎ当て/盾板と10配置を装着3Dへ追加。グリップと銃口の接続位置を保持。デザイン0は既存デザインに小さな社章を追加する。

## ロゴと表示
ManufacturerBrandingに6社の独自幾何ロゴを定義。UI縮小と3Dデカールで共用し、会社名は日英UIとして分離。コードで定義できる幾何ロゴのため画像生成モデルは使用しない。QA実行時にPNGを書き出してAssetsにも保存する。
既存イラストは維持。新設計のインベントリ画像は装着時と同じ3D生成器から256pxで撮影し、48画像を上限にキャッシュ。モジュールは回路/端子/コイルのコード描画と社章で区別する。
拠点のメーカー製品図鑑で6社の特徴、全製品の基本性能/評価/外観を閲覧。ドロップ/店は既存生成経路から新製品も抽選。

## 保存と検証
catalogId/manufacturerId/designVariantを既存itemのシリアライズへ追加。メーカー調整は生成時だけ実施。旧保存品は識別情報を補完しても実数値を再計算しない。
ManufacturerCatalogReviewは全500製品/ID一意性/保存往復/数値/全180銃meshとsocket、日英図鑑を実Windowsで検証。ビルド/画像の最終結果はSTATE.mdへ。

近接も5系統×10設計の50製品を追加、総数550。メーカーの威力/間隔/射程/攻撃角のtradeoffと独立3D切替を適用。22c自動8800checks/0fail(550製品/保存/全銃mesh/socket/legacyScatter/個別ReloadSurge)、日英60図鑑撮影。新近接カードも同一3Dfactoryへ変更し元atlasの隣セル断片を除去。

22d最終候補: ビルド成功、8800項目失敗0。近接カードも実装着モデルから撮影、UIボタン被りと銃見切れを実画面で修正確認。独立QAはsourceレビューまで、最終画像はPMO確認。長時間バランスと全件手操作は未実施。

## 2026-10-17 inventory illustrations23
ユーザー指定により銃/防具inventoryはdesignVariantに関係なく既存イラストを表示、manufacturer badgeを別描画。装着3D/実性能/550製品identityは維持。designごと固有の描き下ろし550枚ではなく、基礎デザインのイラストを共有する。moduleは新ModuleIllustrations-v1.pngの6列(メーカー順)×4行(KillShield/DashPulse/ReloadSurge/ShieldConversion)をeffectとmanufacturerで解決、24painted illustrationsを70製品で共有。
組込みimagegen使用、原本はD:/Unity/NeonVaultArtOriginals/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-0114755d-1076-4df1-bff7-f7fe24130b09.png、Assets/Resources/Art/Manufacturersへ保存。C空き不足により本threadの生成原本43filesをDへ移動し保持(削除なし)、旧WorldArt21の素材/都市原本2filesはD:/Unity/NeonVaultArtOriginals直下。ManufacturerフォルダのD移動/ジャンクション化はOneDrive拒否で実施せず、Assetsには通常folderとして全ファイル復元済。
最終prompt:
Create a 6 column by 4 row game inventory illustration atlas, 24 separate full objects, EXACT equally sized cells in a regular grid, each object centered with 14 percent empty margin inside its own cell, objects never touch neighboring cells. Landscape 3:2 image. Transparent background. No frames, no numbers, no text, no letters, no labels. Match a richly hand-illustrated gritty wasteland weapon inventory art style: realistic intricate small machinery, confident black ink contours and fine internal ink lines, painted material shading and warm highlights, worn steel, rivets, cables, ceramic, scratched enamel, leather straps, no 3D-render look. Each is a palm-sized wearable electronic FIELD MODULE, not gun or armor, shown three-quarter top view so faces and thickness readable, consistent scale. ROW1 shield capacitor: round compact armored capacitor block with shield-shaped front plate. ROW2 dash discharge coil: compact copper induction coil with two prongs and insulated leads. ROW3 reload relay: rectangular mechanical relay switch with small amber vacuum tubes and lever. ROW4 shield conversion circuit: angular power converter cartridge with a circular central lens and heat sink fins. SIX columns are the manufacturers: column1 precision machined steel with ivory ceramic and restrained cyan calibration details; column2 heavy forged iron with copper braces and oxblood leather straps; column3 navy enameled electrical housings with brass coils and tiny yellow warning tabs; column4 olive canvas cradle with translucent green medicine reservoirs and bone-white fittings; column5 reclaimed patchwork rusted orange steel with weld seams and mismatched bolts; column6 layered ballistic steel plates with faded blue fabric and pale identification bands. Every cell must show its row's function and column's manufacturer shapes distinctly. Detailed painted illustration, natural proportions, no cartoon faces, no UI lettering, no watermarks.
