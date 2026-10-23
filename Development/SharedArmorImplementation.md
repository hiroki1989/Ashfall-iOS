# 現行の防具共通化
最新ユーザー指示により、個別3D生成の計画は保留。頭部を除く398防具は承認済Wardenの部位別形状/骨/UVを共用する。胴に袖を含み、手はカフ以降。腰1.04m、足境界.2882m。素体の装備下非表示も維持。

製品別の材質は `StreamingAssets/SharedArmorIllustration/<代表ID>.png` に置き、`Resources/Art/SharedArmorIllustration/catalog.json` で398製品から168種の原画由来atlasへ対応付ける。同じ原画のvariantはatlasを共有する。承認済みの部位メッシュ、UV、骨は変えず、各原画から前面・背面へ投影した1024x1024のatlasを装備時に読み込んで `_MainTex` として差し替える。使わないatlasは解放する。側面は両方向から連続的につなぎ、UV境界には色を延長する。初期装備は承認済みの元atlasを使い続ける。

旧 `SharedArmorLUT` 方式は原画の色だけを移し、初期装備のしわ・縫目を全品に残すため、現在の実行経路では使わない。生成は `Tools/SharedArmor/bake_illustration.py`、前横後のメッシュ確認は `render_illustration.py`。頭部・モジュール・武器は対象外。既存原画のcropは398製品に対して168種類なので、同じ絵を使う製品は同じ意匠となる。独立した背面原画はなく、背面は各原画の布・革領域から推定する。

旧LUT版の検証記録: shared-armor-lut-build2.log、shared-armor-final-runtime2.log、shared-armor-final/result.txt、shared-armor-motion-runtime.log。新atlas版のUnity取込・ゲーム表示・iOS実機は別途検証する。
