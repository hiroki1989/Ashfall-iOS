# Survivor surface atlas / 2026-10-17

組み込み image_gen を使用。新規 bitmap 素材。装備デザインや既存イラストを置換しない。

- Asset: Assets/Resources/Art/SurvivorMaterials/SurvivorSurfaceAtlas-v1.png
- Source: C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-c5f65062-324b-4aef-b828-1287b312a422.png
- 2×2: 左上 canvas / 右上 leather / 左下 steel / 右下 walnut。文字なし、全域不透明。
- DetailedHeroInk3D の object-space triplanar sampler で各区画内を反復。区画境界の色混入を避ける余白と連続した mip 導関数を指定。既存素材の色を基準に表面の明暗・色むらを乗せる。
- 単一 albedo atlas。normal/roughness map は未作成。画像だけで彫刻形状が再現できたとはしない。実ゲーム QA の見た目を別途確認する。

## Final prompt

Create a square 2x2 material texture atlas for an existing realistic-proportion ink-painted wasteland survivor 3D game. This is a production BASE COLOR MATERIAL atlas, NOT a concept illustration. Four equal square quadrants, no gutters, no labels, no border, no objects, no text, no perspective. Top left: muted olive field-jacket cotton canvas, fine worn diagonal weave, subtle irregular faded threads, small dirt trapped in weave, natural hand-painted pigment variation; no large folds or seams. Top right: brown aged leather, fine pores, dark natural grain fissures, tiny pale abrasion scratches, warm worn pigment, no stitched straps or objects. Bottom left: cool grey forged steel with localized irregular reddish brown oxidized islands, small bright exposed steel scratches and pits, old scratched brushed finish; no bolts or objects. Bottom right: warm walnut rifle-stock wood, gently flowing linear grain, fine tiny black ink grain marks, worn amber finish, small scratches. Flat neutral diffuse light, no directional highlights, no cast shadows. Surface textures should have believable fine scale, restrained high-end hand-painted ink texture with realistic materials and crisp micro details. All four must fill their quadrant completely and have no edges or frames. Colors should remain mid-value and usable as surface albedo. High resolution.
