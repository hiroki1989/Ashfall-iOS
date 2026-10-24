# 敵10種 3D用投影資料

元絵は `Assets/Resources/Art/LayeredEnemies/Enemy_0.png` 〜 `Enemy_9.png`。原本は変更しない。各画像には上半身の前・斜前・横・斜後・後と、左右脚の前・側・後が別々に描かれている。生成元の方向別PNG、透明atlas、座標JSON、スクリプトは `Assets/ArtSource/Enemy3DRebuild/Enemy_{kind}/` に置き、ランタイムで使う `projection-atlas-filled.png` と `head-atlas-filled.png` のみ `Assets/Resources/Art/Enemy3DRebuild/Enemy_{kind}/` に置く。新規描画はしていない。

## ランタイム向け固定アトラス

`Resources.Load<Texture2D>("Art/Enemy3DRebuild/Enemy_" + kind + "/projection-atlas-filled")`。RGBA、1536×1536 px、384×384 px セル、左上起点の4列×4行。各部位の正確な内容矩形は ArtSource の `projection.json` の `atlasContentBounds` に記録する。末尾3セルは空白。

| 行 | 列0 | 列1 | 列2 | 列3 |
|---|---|---|---|---|
| 0 | body_front | body_front_oblique | body_profile | body_rear_oblique |
| 1 | body_back | leg_front_L | leg_front_R | leg_side_A_L |
| 2 | leg_side_A_R | leg_side_B_L | leg_side_B_R | leg_back_L |
| 3 | leg_back_R | 空 | 空 | 空 |

各画像は最長辺を360px以内へ等比収縮し、セル内へ中央配置。部位輪郭とセル境界の間に最低12pxある。元シートの区画を横切る上半身の腕は拡張範囲から切り出し、隣の脚などの混入は連結成分で除いた。各部位の個別PNGも同フォルダに置く。透明領域のRGBは黒なので、3D投影時はアルファでマスクし、透明ピクセルを色として読まないこと。UVはセル全体ではなく `atlasContentBounds` を参照して余白を避ける。

`Assets/ArtSource/Enemy3DRebuild/generate_projection_bounds.py` は各 `projection.json` から `Assets/Scripts/EnemyProjectionBounds.cs` を再生成する。元画像を切り直した際はアトラスとこのUV表を同時に更新する。脚画像はセル幅の約3〜5割しか使わず、セル全体へUVを広げると補完色の大きな三角模様がモデルに出る。

## 3D形状と投影

正面は `body_front` と `leg_front_L/R`、背面は `body_back` と `leg_back_L/R` を使う。斜めからは斜前/斜後、側面は `body_profile` と `leg_side_A/B_*` を使う。A/Bは元絵の異なる側面表現で、解剖学的な左右ラベルではない。脚の L/R は画面に向かって左/右。反対側の上半身は専用真横絵がないため、必要なら鏡映し、非対称の傷・装備は元画像を確認して補正する。

これは形状を再現する3Dモデルそのものではなく、モデリング/ベイク用の色・ディテール資料である。品質基準は、頭・胴・肩・腕・脚・足の体積を持つメッシュに各視点を焼き付け、正面・両側・背面の継ぎ目と陰面を手直しすること。単一平面への全身投影やビルボード拡大では、プレイヤー素体相当の立体品質にならない。元絵は上半身と脚に分かれ、股関節/腰の接続部と上下面の塗りが存在しないため、そこもモデルに合わせて補筆する。

| kind | 元デザイン | 想定身長（足底〜頭頂） |
|---:|---|---:|
| 0 | 破れた服の感染者 | 1.85 m |
| 1 | 黄衣・発光する感染者 | 1.95 m |
| 2 | 青銅装甲の兵士 | 1.9 m |
| 3 | 骨棘の大型体 | 2.35 m |
| 4 | 細身の変異体 | 1.95 m |
| 5 | 棘付き暗色甲冑 | 2.05 m |
| 6 | 緑の薬剤タンク背負い | 2.1 m |
| 7 | 巨腹の肉体 | 2.2 m |
| 8 | 爪付き重甲冑 | 2.25 m |
| 9 | 紫の菌・脊椎変異 | 2.0 m |

身長はシルエット設計の目安であり、既存ゲームの当たり判定や移動速度から確定した値ではない。最終モデルはその仕様に合わせて縮尺調整する。マテリアルは元画像の色を保持し、強い一律の再着色を避ける。4方向・斜め上の実ゲーム距離で、顔/胸の種別、脚の歩行、装甲/菌/肉の材質差が読めることを確認する。
