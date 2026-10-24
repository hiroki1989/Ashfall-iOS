# 新規敵3Dの組込規格

元デザインは `Assets/Resources/Art/LayeredEnemies/Enemy_0.png`〜`Enemy_9.png`。旧 `FaithfulEnemy3D` の丸・板・棘を組み合わせた形状はモデリング参考に使わない。元絵の前・斜・横・後と左右脚を同一設計図へ配置して、敵種ごとに独立した体積を作る。

Unityの配置先は `Assets/Resources/Art/Enemy3DRebuild/NewMesh/Enemy_{kind}/`。`Head`, `Chest`, `Pelvis`, `UpperArm_L/R`, `Forearm_L/R`, `Thigh_L/R`, `Shin_L/R`, `Foot_L/R` の13個のFBXと、全パーツ共通UV0の `texture-atlas.png` が必須。各FBXの原点を対応ジョイント位置へ合わせる。標準寸法は `FaithfulEnemy3D.cs` の `head/chest/pelvis/arms/forearms/thighs/shins/feet` と一致させる。Unityでは全アセットを事前ロードし、欠けがない時だけ新モデルを表示する。旧メッシュとの重ね描きは禁止。

テクスチャは元絵の装甲・布・肌・錆の色と配置を守り、画風はBorderlandsを参考に輪郭、段階陰影、擦れや面の描き分けを強める。UIや文字を画像へ焼き込まない。正面だけでなく側面と背面のつなぎ目を確かめる。iOSで使うテクスチャ解像度と描画負荷も確認する。

合格条件: 13パーツを独立可動させ、元絵の正面・側面・背面シルエットを再現する。待機、歩行、攻撃、被弾、死亡で接合部が外れず、ゲーム内距離で敵種を識別できる。最初の1体をWindows実画面で確認してから残りへ展開し、最終的にiOS/Metalで検証する。
