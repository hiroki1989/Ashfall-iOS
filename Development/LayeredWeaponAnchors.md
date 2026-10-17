# ASHFALL 2D weapon anchors / 武器接続点

担当範囲: WeaponAnchors.json/.meta とこの文書。武器原本画像を編集・trim・再配置していない。

## 契約 / Contract

Weapons_N.png は1254x1254 RGBA、4列x4行。2行が1モデル。model=2N が上2行、model=2N+1 が下2行。direction0 down,1 downright,2 right,3 upright,4 upaway,5 upleft,6 left,7 downleft。

JSON: {"anchors":[{"model":0,"direction":0,"grip":{"x":0.5,"y":0.5},"muzzle":{"x":0.5,"y":0.8},"support":{"x":0.5,"y":0.6},"stock":{"x":0.5,"y":0.2}}, ...]}

座標はalpha trim前の名目セル全体を基準にしたnormalized値。x左→右、y上→下。セルの一辺は1254/4=313.5px、col=direction%4、row=(model%2)*2+floor(direction/4)。Global pixel=(col+x,row+y)*313.5。測定点は実際の描画を優先する。

重要: 生成画像の一部は数学的セル境界を越える。そこで実画像にある銃口/銃床についてx<0/x>1、少数のy<0を保持した。これらをclampすると接続点が実形状からずれる。grip/supportはセル内。PMOへ範囲外許容と可変ROIの必要を報告済み。固定セル切り出しでは銃端の欠けが起き得る。

## 測定方法 / Measurement

全sheetをview_imageで確認し、画像上の銃口開口中心、ピストルグリップ握り中心、自然なfore-end支持位置、銃床後端の肩接点を手測定。JSONは小数6桁だが画像読取の精度を意味しない。細いノズル・輪郭は約3px、重なる前後方向の推定点は約8px程度の不確かさを持つ。

muzzle: 開口またはノズル先端の直接測定。複数銃身は全開口の視覚的中心、needle gunは針先を採用。
stock: 銃床後端面の中心の直接測定。短い銃床はその後端を採用。
grip: 横/斜め方向は見えるピストルグリップから測定。direction0/4ではtriggerが隠れ、機関部・銃床の位置関係から自然な握り位置を推定した。推定を実形状の完全なトリガー測定と扱わない。
support: 描かれたvertical foregripがあればその握り中心、なければbarrel shroud/木製fore-end下面の支持点を選択。direction0/4で下面が隠れる場合は対応する機関部下の位置を推定。

モデル1 direction3は予定uprightに対し実絵が銃床右上・銃口左下。方向名を変えずactual muzzle/grip軸で接続。見えない握りを正確なtrigger座標と断定しない。

## 状態 / Status

model0..11の96records（12武器×8方向）を保存済み。WeaponRegions.jsonは全96枚の実alpha主成分を測定したROIと凸外形を保存し、cell境界を越える銃床/針先を維持する。矩形ROI内に隣の武器の針先が入る場合もあるため、専用Sprite shaderで測定凸外形の外をクリップする。全96外形内部に他の主武器の不透明画素がないことをSciPy測定で確認。最大51頂点。Unity6000.6のruntime SpriteはOverrideGeometryを拒否するため採用しなかった。元画像を編集・切断しない。
64records・256点の全点が実画像の不透明形状上または3px近傍内であることをPillow alpha測定で確認。セル境界越え21点は意図したactualpointsであり、虚構のclampをしていない。
JSON書き込みは同ディレクトリのpendingファイルを作成し、Windows File.Replaceでatomicに置き換える。読取側へ途中JSONを渡さない。
Unity組立での両手位置、武器反動、発射位置・銃床肩接触の最終QAはCoding/PMOの担当。
