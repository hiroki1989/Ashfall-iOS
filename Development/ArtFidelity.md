# 原画準拠の3D再構築 / 2026-10-16

デザイン変更は禁止。タッチ変更は輪郭・陰影・筆致に限定し、形状・部品・配色・材質区分を変更しない。

実UI原本: ItemPortraits→LayeredPlayer2D.LoadCellsの最終解決先を参照。
- 防具0/1: InkedArt/Armor_Front（olive/blue/teal全顔とnavy/crimson/amber全顔）
- 防具2/3: TacticalArt/ArmorV5_Front（tan amber tactical / navy cyan riot）
- 防具4: ApprovedFieldkit/Portrait_Armor_0..4。
- 武器0..11: ColorArt/WeaponsV3_0..5の各上下8方向。
- 12/13: InkedArt最終weapon6、14..17 HandgunArt、Warden別visualSetはApproved。
- 敵0..9: LayeredEnemies/Enemy_0..9。汎用WastelandModelsは代用不可。

確認は原画とUnity実描画の対比。モデル数・接点PASSは造形一致の保証ではない。
チェック: 全顔被覆/露出範囲、頭身・肩幅・四肢、レンズ色、布色/plate色、輪郭、給弾/stock/銃口、傷・留具・縫い目、原画由来の敵種別特徴、歩行・攻撃・死亡。
今回前候補では照合が不足し、完成報告不可。元イラストへ表示経路を復元。新atlas原本は保存するが設計基準へ勝手に差し替えない。

## 2026-10-16 reference16 / 設計は保持したまま彫り込み
基準原本: Assets/ArtSamples/PoseTarget/Reference-DetailedSurvivor-20261016.png（ユーザー添付を保存、再生成なし）。デフォルト素体の髪は黒茶の無造作な重なる先細り束。ヘルメット着脱で基底髪は保持/表示切替。リアルな比率と既存Wardenの形/配色/木製銃を保持。体の主要曲面2x2の分割で肘膝腰の布折れを彫り込み、髪は156束のBezier断面、顔は頬鼻を連続皮膚面へ統合。装備の金属板に局所打痕、ポーチ/曲面の分割増。最新商用ゲーム/原画と同等とは未判定。単純な三角形数を品質保証にしない。


16d最終候補: 髪310束/2濃淡へ更新。初回hair裏面と毛根埋没は実画像で検出・修正済み。Player1241156triangles、敵350688〜674008。PMO実画像確認済みだが原画同等の品質合格ではない。顔の連続面/毛流れ/材質は未達。QA画像 D:/Unity/NeonVaultTwoHandValidation/QA-hero3d-reference16d-final 。


## shape17 / 分割数ではなく実形状の改訂
ユーザー指摘でhelmet後端を7.5cm詰め(頭local -.105→-.030)、補強板短縮。Wardenポーチは丸いloftから箱型の縫製ボリューム/薄い折返し蓋/側面gussetへ。膝鋼板は5mm実厚と裏面/切断面。素体襟は太い管から3mm二重布面、皺は等間隔から関節圧縮の方向性折れへ。Arsenal装備服は基底服を隠し、単一の色付き曲面と彫り込んだ折れで表示。肩上部の曲面crownと閉じた布端で接合。元デザイン/配色方向は保持、画像原本やアイテム絵は変更なし。17c実画像QA: D:/Unity/NeonVaultTwoHandValidation/QA-hero3d-shape17c 。参照原画の再現度は未達。


## surface19 / 実ゲームの向きと描き込み
ポリゴン数追加を目的にせず、腕/脚 IK の world-space FromToRotation により前面がワールド+Zへ残る不具合を rig heading frame に修正。8方向で肩板/膝板が身体の正面/側面を維持する。布・革・鋼・木の新しい2x2 albedo atlasを組み込みimagegenで生成し、元の素材色へobject-space triplanarで合成。詳細と完全promptはSurvivorSurfaceArt.md。原画・既存装備形状を置換しない。縮小時はmipmap/trilinear/aniso8を適用。髪は不規則なS字束、scarfは布面の縦drape、上着裾は前後ventを持つ重ね布/薄い厚み/摩耗edgeへ。
19a/bの実ゲーム背面画像でhoodの法線打消し/outlineによる黒化を検出し、19cで別lining頂点と外向き巻順へ修正。19c実ゲーム8方向low/aim撮影、PMO前/後/横画像確認、96走行姿勢+全銃接点FinalFailures0。19dは裾追加の最終候補。参考原画同等の形/人体解剖/材質密度にはまだ達していない。独立担当は利用上限で使用できず、画像確認はPMO自身。
