# 生成防具の共通骨格契約（量産前調査）

2026-10-23、D:/Unity/Ashfall-iOS現行コードを参照。新独立防具の納品座標と旧Warden scan専用変換を区別する。コード・既存モデル変更なし。

## 共通rest座標

Unity character-local、単位m、Y上、X左右、Z前、足底Y=0、腕下げ姿勢。root position=0/rotation=identity/scale=1。BlenderはZ上からFBX変換し、import後の実座標・符号・倍率を測定する。

CharacterRigStandard.RestPositionsが唯一の関節基準。以下は全身local座標であり、親骨に対するlocalPositionではない。

|index|骨|rest(X,Y,Z)m|
|---|---|---|
|0|chest|(0,1.25,0)|
|1|pelvis|(0,.98,0)|
|2|head|(0,1.55,0)|
|3/6|left/right upper arm|(−.18/+.18,1.427,0)|
|4/7|left/right forearm|(−.18/+.18,1.142,0)|
|5/8|left/right hand|(−.18/+.18,.877,0)|
|9/12|left/right thigh|(−.105/+.105,.955,0)|
|10/13|left/right shin|(−.105/+.105,.517,0)|
|11/14|left/right foot|(−.105/+.105,.101,0)|

頂点bone indexはこの15骨順、weight合計1。全階層を共通restへ配置して `bindpose = bone.worldToLocalMatrix * renderer.localToWorldMatrix` を計算し、元の生姿勢へ復元する。アニメ中の姿勢から直接bindを作らない。SkinnedMeshRenderer.bonesは同順、rootBone=chest。関節原点だけでなく、メッシュ断面中心・掌向きも合わせる。

## slot・境界・coverage

|slot|形状と骨|境界/表示|
|---|---|---|
|Head|頭骨に追従するヘルメット/マスク付属物|共通顔・頭を保持して上から装着。HeadMeshHeight=.285、mask高さ.153/中心head-local(0,.048,.025)は既存品の値で、任意ヘルメットの一律fit値ではない。|
|Chest|胴・肩・袖一体。chest/pelvis/upperArm/forearm|belt Y1.04。袖はChest。ポーチは途中で切らず一体で所属。|
|Hands（enum Arms）|左右手袋＋カフ。forearm/hand|カフ上端は現Warden約Y.955、手首中心.877。装備slotとboneの所属を混同しない。|
|Legs|骨盤〜ズボン/脚鎧。pelvis/thigh/shin|上端belt1.04、下端Feet境界.2882。膝中心.517。|
|Feet|左右ブーツ。shin/foot|上端.2882、足首中心.101、足底0付近。片側sourceか左右一体sourceかを明記。|

頭以外の装備部位は対応素体を隠す。全身linerを重ねて穴を隠さない。境界三角形は補間clipし、同じ境界loopを共有する。UV/slot seamで複製した頂点もsource_vertex単位でfit/weightを一致させる。人工capを動く腕と別骨へ固定しない。

CharacterArmFitは肩/肘/手首を共通骨から取得し、肘±.055m・手首±.045mで連続weight遷移。CharacterLowerBodyFitの位置profileは現素体scan実測専用であり、新商品へ無条件に適用しない。

## 旧Warden source変換（新独立防具へ流用しない）

CreateCharacterPartの旧armorはscale90/Y+1を経由し、GeneratedWardenRigがsource座標を共通骨空間へfitする。UV channel2にsource XYZ/vertex IDを保存しslot分類へ使う。

- source肩Y1.62/肘1.30/手首1.10 → target1.427/1.142/.877。横軸は肩約.265/肘.37/手首.425。source cuff1.14に最大25mm局所延長し、上端target約.955。
- source belt1.25→target1.04、肩1.62→1.427、上端1.84→1.515。
- source脚1.00→.89、膝.65→.517、足首.25→.101、足元.10→.03。boot rim .43→.2882。
- sourceY<1の脚は当該scan実測により最大65mm内寄せ。一般化しない。
- belt source1.25で補間clip。腰付属物はsourceY.95..1.40、absX>.235またはZ>.13またはZ<−.12をChestに分類する専用処理。

新メッシュが既に共通restメーター座標なら、このscan変換を二重適用しない。「rest-fit済」の独立import種別が必要。

## 現factoryと不足情報

GeneratedEquipmentAssetFactoryはsocketへroot identityでInstantiateするrigid方式。左右slotは同prefabを左右socketへ複製する。15骨へのrebind、左右一体mesh分離、袖・脚のskinning接続は実装されていない。胸袖一体を置くだけでは関節追従しない。

GeneratedEquipmentAssetCatalog.Entryはmodel/atlas/socket/slot等だけを持ち、次が未契約。

1. rest-fit済/旧source-fit要、単位・軸・root変換、socket-local/character-localの区別。
2. bone名→15index、bindposes、skin済/未済、rebind責任。
3. 左右片側/両側source、反転規則、境界loopとsource_vertex対応。
4. 肩/袖口/カフ/ベルト/足rimのsemantic label、ポーチのまとまり。
5. 掌・指・握りlandmarks、脚靴の断面中心、頭防具の顔/髪干渉範囲。
6. 商品と原画/UV/atlas対応、固定UV、slot別coverage。

Hunyuan shape-only出力だけではこれらが揃わない。原画輪郭から骨・掌向き・付属物所属を安全に確定できず、landmark/label付与と実装側skinned importの追加なしには全自動化できない。

## 納品と採用条件

mesh.fbx、atlas.png、manifest（商品/原画ID、単位/軸、bind種別、bone map、左右、境界loop/landmarks、source来歴）。独立形状を保持し、旧primitiveの絵貼替は完成品としない。固定UV0、32k面程度以下を基準に面数/UV範囲/共有頂点を検査。単独slot・混成・一式、前横後、rifle/pistolの待機/射撃/走行で穴・二重表示・扇状変形・手足接続を目視し採用する。

参照: Assets/Scripts/CharacterRigStandard.cs、CharacterArmFit.cs、CharacterLowerBodyFit.cs、GeneratedWardenRig.cs、GeneratedCommonBodyRig.cs、GeneratedEquipmentAssetFactory.cs、GeneratedEquipmentAssetCatalog.cs。

## 明示skinned納品名（実装用）
root子に `NV_SHARED_REST_V1` markerを置く。bone名は15順で `chest,pelvis,head,leftUpperArm,leftForearm,leftHand,rightUpperArm,rightForearm,rightHand,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot`。すべて共通restでbind済、meshとrendererはcharacter-local identity。追加の90倍source変換を禁止。左右一体は一度だけinstantiateする。不正marker付きassetをrigid装備として表示しない。marker無しの従来装備は従来経路を保持する。
