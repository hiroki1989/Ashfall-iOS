# Kestrel Harrier Patrol / Field — 商品別立体候補

対象商品は `distinct.gun.030`。既存インベントリ原画 `Assets/Resources/Art/GunSeries/kestrel-series-v1.png` の先頭セルを形状・塗装基準とする。ウォーデンや他の銃の立体を流用しない。

手順: `extract_kestrel.py` で原画を余白付き1024角に抽出 → `generate_shape.py` でローカル Hunyuan3D-2mini 形状のみ生成（AIテクスチャ生成なし）→ `TwoView/prepare_atlas.py` と `project_blender.py` で左右2面から投影し、正面・側面・背面・上面を評価する。元原画・既存実行時コードは変更しない。

現時点では右側専用の承認済み原画がない。imagegenによる反対側生成は入力段階の自動審査で拒否されたため、候補の右側は同じ原画を鏡像として投影する。その限界をQAで明示し、正式な商品モデルとして `Resources` に登録しない。平面板、部位の欠落、曲がった弾倉・木製銃床の崩れ、テクスチャの空白継ぎ目や二重模様があれば不採用。
