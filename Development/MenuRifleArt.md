# メニュー主人公の長銃をゲーム内へ反映 / 2026-10-15

ユーザーが指定したメニュー画面の主人公が持つ長銃へ、武器モデル12のゲーム内方向アートを更新した。前回のオリーブ樹脂製カービン解釈は誤り。今回の長銃は木製銃床・木製前床、暗色の傷んだ鋼製機関部と長銃身、革スリング、機関部上の琥珀ガラス筒2本、低い照準器を持つディーゼル風サルベージ銃。

アセット: `Assets/Resources/Art/LayeredPlayer/InkedArt/Menu_Weapons_6.png`。既存Weapons_6と同じ1254×1254 RGBAの4×4領域。先頭2行はモデル12の8方向、後半2行は元Weapons_6.pngからピクセル単位でコピーしたショットガン8方向。透明アルファあり。既存アトラスと同じTextureImporter設定で独立GUIDのmetaを作成。

Runtime: `LayeredPlayer2D.LoadCells` が `Menu_Weapons_6` を優先し、なければ以前の素材へフォールバックする。Inventory portraitはLoadCellsを使うので同じ描画源になる。

Imagegen edit prompt: `Use case: precise-object-edit. Asset type: production Unity gameplay sprite atlas, same 4x4 transparent RGBA atlas. Input images: Image 1 is the authoritative actual in-game weapon atlas to edit; Image 2 is the exact reference design from the game's main menu. Edit ONLY the first eight weapon cells in the TOP HALF of Image 1 (model 12, eight distinct directions of one identical weapon). Replace that M4/AR-15 style olive carbine with the specific long battered rifle carried by the masked survivor in Image 2: dark weathered steel receiver and long slim dark steel barrel, warm walnut wooden buttstock and matching lower fore-end, leather sling loop under the receiver, asymmetric mechanical details, two upright amber glass pressure/illumination cylinders on top of the receiver and a compact dark optic. It must read as that unique rifle, visibly wooden rather than olive polymer, and carry the reference's diesel-retrofuture salvage aesthetic. Preserve each existing cell's weapon angle, orientation and precise bounds/scale/position, one unique 8-direction view for each cell, plausible connected 3D construction without mirroring errors; retain strong hand-inked black outline and painterly detailed materials. Keep the lower-half shotgun cells PIXEL-IDENTICAL to input image. Keep all background transparent (true alpha), no text/logos/watermark. This is a rigid production atlas edit; no layout changes, no new weapons, no redraw/reposition of untouched cells.`

検証済み: PNG 1254×1254 RGBA、alpha extrema 0..255。下半分はプログラムで原アトラスから複製し、生成による再描画を防止した。8方向のゲーム内実画面・Unityインポート確認は統合後に実施する。
