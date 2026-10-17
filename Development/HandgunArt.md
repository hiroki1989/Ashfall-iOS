# 片手銃原画と接続仕様

組み込み imagegen スキルで Art 担当が制作した native RGBA 原画。PNG の画像加工は行わず、透過 alpha の解析から32領域と32組のアンカーをJSONへ保存する。

## 保存先と原本

- `Assets/Resources/Art/LayeredPlayer/HandgunArt/Handguns_7.png` — model14 リボルバー、model15 ブローバック式。各8方向。
- `Assets/Resources/Art/LayeredPlayer/HandgunArt/Handguns_8.png` — model16 ハンドキャノン、model17 消音付きコンパクト。各8方向。
- 原本7: `C:/Users/hiroki/.codex/generated_images/01a132de-862b-70d2-8c8a-1b0aa4eb7af0/exec-37ed0d81-f7f8-4ec4-89c8-a462adf2c80a.png`
- 原本8: `C:/Users/hiroki/.codex/generated_images/01a132de-862b-70d2-8c8a-1b0aa4eb7af0/exec-d6fd7ed5-8e5c-471a-9946-1a593e5387b4.png`

## 制作指示の要約

以下は担当から継承した制作仕様の要約で、送信プロンプトの逐語記録ではない。担当の利用制限でPMOが未完了メタデータと統合を引き継いだ。

Transparent 4×4 atlas, sixteen isolated finely detailed toon firearm views, no hands, characters, lettering, grid or UI. First eight cells depict one firearm, next eight depict the other. Eight facings in order: front/down, front-right, right, back-right, back/up, back-left, left, front-left. Strong black outlines, internal mechanical contours, layered metallic highlights and material-specific colors, quarter-view compatible. Sheet7: a six-shot revolver with walnut grip, dark steel frame and silver cylinder; a blowback service pistol with steel slide and olive polymer grip. Sheet8: a heavy hand cannon with steel slide, walnut/ivory grip and a small red control; a compact suppressed black pistol with a turquoise optic and dark rubber grip.

## メタデータと実行時契約

原画は1254×1254。`HandgunWeaponRegions.json`は元テクスチャ左上基準の領域と凸包、`HandgunWeaponAnchors.json`は元4×4セル内の左上基準の grip/muzzle/support/stock。一部の銃がセル外へ延びるため、領域はセルで切らず alpha の主連結成分全体から採取する。アンカーのセル内正規化値はこの場合0..1の外にも出る。

ピストルのstockは握り底ではなく機関部後端を表す。機関部後端→銃口を銃軸とし、右手gripで位置を合わせる。銃床を肩へ接触させる処理・胴体による銃床遮蔽は両手銃に限定する。左手は銃を支えず腰側で走行のバランスを取る。

model15の側面右向き原画が左向きに生成されたため、銃軸と左右を照合して実行時に鏡像補正する。原PNGは変更せず保持。前後原画は見下ろしの短縮表現を含み、純粋な真上・真下の平面図ではない。これらは方向別の武器原画であり、モーションは64歩行/64走行/32待機/32射撃の関節合成と補間で作る。

再採取スクリプト: `Development/build_handgun_metadata.py`。PNGは読み取りのみ、32ROI/32anchors/Readyを出力する。
