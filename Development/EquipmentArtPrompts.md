# KeyEquipmentAtlas / キーアート準拠装備

2026-10-12. Built-in imagegen / 組み込みimagegen。Reference / 参照: `Assets/Resources/Art/VaultKeyArt.png` (style only).

Output / 保存先: `Assets/Resources/Art/KeyEquipmentAtlas.png`. Transparent 4 columns × 4 rows, top to bottom / 透過4列×4行、上から順。

Validation / 確認: actual output 1254×1254, 32-bit RGBA, corner alpha 0. All sixteen ordered items visually confirmed; no text, floor or shadow. The spacing edit restored canvas edges, but 15% cell margins remain unmet (weapons nearly fill cell width). Consumers should crop each cell's alpha silhouette and fit it inside a 70% display area. 実出力1254×1254、実アルファ透過。16品と順序を目視確認。余白修正後も15%セル余白には未達のため、表示側ではセル内アルファ範囲を切り出し、表示領域70%に収める。

Layout / 配置:
- Row 0: antique carbine, double barrel shotgun, pulse carbine, precision rifle / 長銃、二連散弾銃、パルス銃、精密銃。
- Row 1: prism ricochet, satellite orbital, gravity cannon, alternate jade gun / 反射銃、衛星銃、重力砲、予備翡翠銃。
- Row 2: wide brim masked helm, breastplate, paired gauntlets, armored pants / つば付き覆面兜、胸鎧、腕甲組、脚鎧。
- Row 3: paired boots, crested helm, alternate breastplate, alternate paired boots / 靴組、差分兜、差分胸鎧、差分靴組。

Final prompt / 最終プロンプト:
```text
undefined
```

Spacing edit / 余白修正プロンプト（初回生成はセル端に近かったため）:
```text
undefined
```
