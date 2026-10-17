# 顔・廃墟・都市作戦図 / 2026-10-17

## 実装
顔は連続した顔面に眉骨、頬骨、鼻翼、顎、眼窩の起伏を与え、細い眼裂、虹彩、閉じた口、無精髭を独立した微細メッシュで追加。素体に適用し、頭装備の着脱を維持する。
環境は既存solidセル内の崩れた窓、露出鉄筋、配管、変圧設備、閉鎖店舗、崩落床と低い瓦礫。装飾には衝突を追加せず、小部品は材質ごとに結合する。床と既存のコンクリート/鋼材にも新素材を適用。
都市図は画像と操作UIを分離。100地点は既存ID/レベル/報酬/セーブを維持し、37との互いに素な置換で都市全体へ分散する。日英、拡縮、移動、レベル制限を維持。

## 画像原本
組み込みimagegenを使用。ゲーム内スクリーンショットではなく、素材として生成した画像。
- RuinSurfaceAtlas-v1.png: C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-3434f101-7dad-4715-890b-d2bb7efb3692.png
- AshfallCityMap-v1.png: C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/exec-b273701f-22d9-49b5-a2cf-8a9d9d6dd5fd.png
両方Assets/Resources/Art/Environmentへ保存。UI文字の焼き込みなし。atlasは左上concrete、右上steel、左下brick、右下asphalt。

## 最終生成プロンプト
### 素材
Create a production game material albedo texture atlas, square, exact 2 by 2 equal tiles with no gutters no labels no text no perspective, evenly diffuse lit without cast shadows, intricate realistic surfaces with restrained ink-like fine crevice definition for gritty post-apocalyptic abandoned city. Upper left: weathered grey beige concrete, spiderweb cracks, spalled patches exposing small aggregate, soot stains and sparse ochre water streaks, no large objects. Upper right: old teal industrial painted steel, extensively peeling teal paint revealing dark iron and layered reddish rust, scratches bolts not needed, flat material. Lower left: damaged old burnt sienna brick masonry with recessed crumbling charcoal mortar, broken irregular edges and pale efflorescence, straight horizontal brick courses. Lower right: dark charcoal cracked asphalt, scattered tiny gravel, oil stains, dry dust and worn faded fragments of ochre road paint, asphalt predominant. Each quadrant detailed flat photoreal material, physically believable muted colors with subtle stylized dark crevices, consistent scale, no rendered spheres, no objects, no UI, no watermark. Tiles fill whole image edge to edge.
### 都市
High detail overhead orthographic illustrated city map for a premium post-apocalyptic action RPG stage selection screen. Landscape 3:2 composition, an entire large ruined metropolis filling frame edge to edge, NO TEXT NO LETTERS NO NUMBERS NO ICONS NO UI NO BORDER. Beautiful gritty hand-painted realistic cartography with subtle ink outlines and realistic building proportions. Many irregular neighborhoods with dense individually drawn collapsed roofs, bombed civic blocks, rusted industrial works and cooling towers southwest, freight railway yards northeast, overgrown suburbs east, a circular partially collapsed stadium southwest bottom, fortified civic citadel northwest, botanical glasshouses southeast. A dirty narrow teal canal runs vertically near center from top to bottom, precisely three ruined road bridges across. A broken elevated ring highway loosely encircles town. Tangled arterial roads and small lanes with organic branching and varied block sizes, no perfectly repeated grid. Desaturated moss, charcoal, rusty orange, dusty beige palette, strong readable roads contrasting roofs, abandoned polluted atmosphere, exquisite small detail of rubble and skeleton buildings. Totally top down like a richly painted city operations map, not isometric, no mountains no labels. Flat broad ambient lighting, crisp at game UI size.

## 判定
ビルド/実ゲーム画像の確認結果はSTATE.mdへ記録する。画像生成だけを実装完了の根拠にしない。長時間負荷検証と全100ステージの目視は別途必要。

21d検証: 実Unity Windowsで顔/廃墟/都市図を撮影。QA-world21d-world。旧目6パーツ撤去、首上端顎内接続、床黄色の反復抑制、district原点高さと瓦礫接地補正。目標品質の全面達成ではなく、顔の自然さと都市建築の大きなシルエットには追加改善が必要。
