# Enemy reference rebuild / 2026-10-17

設計原本は Assets/Resources/Art/LayeredEnemies/Enemy_0.png ～ Enemy_9.png。全10画像を直接目視。2D原本は変更しない。

| Kind | 形状・素材の照合基準 |
|---|---|
| 0 | 痩せた感染者、後退した灰髪、裂けた茶色ジャケット、露出した胸骨と肋骨、裂けたズボン、編上げ靴 |
| 1 | 黄土色の裂けたコート、非対称の緑色頬嚢と胸の嚢胞、革の防具 |
| 2 | 露出頭部と赤レンズ防毒面、青緑の制服、鋼の肩・膝、革ハーネスとポーチ |
| 3 | 小さな頭蓋と太い首、連続した大胸筋・腹斜筋、巨大な腕、欠けと穴のある骨肩・前腕、裸足と革腰布 |
| 4 | 痩せた赤みの皮膚、前傾した胴、長い爪と独立した親指、紺の裂けた服、背骨の突起、裸足 |
| 5 | 紺フードと防毒面、骨の肩突起、包帯・革・布の層。通常の感染者の顔を露出させない |
| 6 | 筋肉質の胴・腕、胸の緑の薬液容器、背中のケージ付きタンクと管。緑の肋骨へ置換しない |
| 7 | 首・上腕まで厚い肥満体、垂れた非対称の腹、淡い皮膚の紫斑、赤い裂けたベスト、茶色の下半身 |
| 8 | 頭蓋・冠状の骨突起、重なった鋼板の全身鎧、赤褐色の爪。布・骨・鋼を別素材にする |
| 9 | 細い閉じた口の顔、細い腕と爪、紫と緑の裂けた布、肩・背中の紫色腫瘍と緑の血管 |

## 実装

FaithfulEnemySculpt3D.cs: 一体の頭蓋・頬・鼻・眼窩、実際に開いた口、内側の歯列、閉口種の分離、連続した胸背部の筋肉起伏、4指と独立した親指、関節・先細りの指、裂けたズボン。

FaithfulEnemy3D.cs: 胸郭を肩まで延長し、分離した球形の大胸筋/腹筋と肩球を廃止。低姿勢種の前傾を胴へ分け、休止中の膝を上げすぎる姿勢を変更。膨張腹・嚢胞の形を完全な球体から局所的な軟部組織形状へ。Kind6の発光肋骨をケージ付き容器へ。

高密度化だけで品質を評価しない。元絵と実ゲームの前・横・後・攻撃・死亡を比較し、形状と色の不一致を残件として記録する。原画と同等の解剖学的精密さを達成したとは自動判定しない。

## CreatureSurfaceAtlas-v1.png

保存先: Assets/Resources/Art/SurvivorMaterials/CreatureSurfaceAtlas-v1.png

組み込み image_gen で生成。原本: C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-6916797f-298d-43fc-a0bd-83df3d3b4f28.png

2×2: 左上 pale skin / 右上 dark hide / 左下 ivory bone / 右下 plum membrane。既存の種別色に素材の粒度を合成。プレイヤーには感染者用の皮膚を適用しない。材質の高さはalbedoの輝度から推定した微細なsurface gradientであり、別撮影したnormal mapではない。布/革/木/金属も同じ仕組みで微細な光の応答と反射のばらつきを持つ。

### Final prompt

Production seamless surface albedo texture atlas for an existing realistic horror wasteland monster 3D game. A square divided into exactly four equal square material swatches, no gutters, no borders, no text, no objects, no body parts, no face. Top left: pale grey-beige unhealthy human-like skin surface, irregular fine pores, subtle bluish capillary networks, old shallow scars, tiny muted pink-purple mottling; very realistic restrained skin, flat illumination. Top right: dark charcoal grey thick muscular creature skin, fine interlocking pores and stretched skin grain, natural muted grey-blue marbling, subtle faded healed fissures; NOT rock, not plates. Bottom left: aged warm ivory bone surface with layered porous bone grain, subtle brown weather staining and fine irregular old fractures; not metal. Bottom right: dull plum-purple diseased membrane surface, fine thin branching olive capillaries, muted translucency coloration and skin mottling, realistic biological texture; no round balls or organs. Flat neutral diffuse albedo lighting, no shadows or reflections, no painted directional lighting. Fine dense realism with restrained illustrated ink grain matching realistic-proportioned dark action RPG creatures. Surface swatches fill the entire quadrants. No gore splashes or bright red blood. High resolution.

輪郭の追加修正: 指定した太さと高さを同じ補間率で動かすと円錐台の連結になるため、SculptProfile3Dで実高さに対する半径を形状保持cubicで補間する。プレイヤーと敵の胴・四肢に適用し、髪の根元基準となるプレイヤーscalpは維持。
