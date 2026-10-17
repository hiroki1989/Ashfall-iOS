# Trail Warden / トレイルウォーデン

2026-10-15。PMOから承認された新規プレイヤー原画制作。原本参照は `Assets/ArtSamples/PoseTarget/GunHoldingTarget-v1.png`。ユーザーが承認した自然な完成ポーズと精緻なインク画を維持する。

組み込みimagegenを使用。旧原画・旧コードは上書きしない。今回はPMO明示承認により銃・両腕・上半身を一体原画に含める。独立銃を重ねず、腕IKで組み直さない。全身原本を保存し、runtime UVだけでベルト境界を裁断する。

## 描画契約

- 正投影、俯角35.264度。原画色と内部線を保持。
- 4列2行、top-left row-major: d0前、d1前右、d2右、d3後右、d4後、d5後左、d6左、d7前左。
- `LowReady8-v1.png` / `Aim8-v1.png` / `CompactAim8-v1.png`。全画像は同じcanvas/cell寸法を基本とする。
- `Poses-v1.json` のpivotのみbottom-left正規化。他の点とbeltYはcell内top-left pixel。heightはalpha縦bboxを2.2 world unitsとして使用。
- 腕/銃/胸/頭は完成上半身を保持。歩行脚の接続はベルト近辺のみ。ポーズ画像のopacity混合はしない。

## 制作記録

生成はすべて組み込みimagegen。逐語promptは同名 `.prompt.txt` に保存。元PNGは以下のCodexディレクトリにも保持する。

`C:/Users/hiroki/.codex/generated_images/01a13f65-306a-71f0-8a85-d4d6362b292e/`

| Asset | Original file | Result |
|---|---|---|
| LowReady8-draft1 | exec-b1fa4516-e215-4e3c-8139-ed7929ff708f.png | 初稿。セル外銃先/方向誤差のため非優先 |
| LowReady8-v1 | exec-c3a74b6d-4030-4096-b773-bf4c47d83f2f.png | 正面改良。d7は単独原画で置換 |
| LowReadyFrontLeft-v1 | exec-20ce73cf-0bc6-43df-8bde-8a650b2e9e79.png | 正しい前左。鼻/銃先/爪先が左向き |
| Aim8-v1 | exec-08cae953-b7cf-4fed-9cc7-3bcf3d43ddcf.png | 背面銃軸誤りのため非優先 |
| Aim8-v2 | exec-369adec2-f351-40e4-8eb4-bc4806f8f3d1.png | 背面3方向を修正。前3方向の銃軸を追加調整中 |
| CompactAim8-v1 | exec-a9c63dea-6ee4-49a3-91c4-b21562f569bd.png | d3銃軸誤りのため非優先 |
| CompactAim8-v2 | exec-1f599f6a-1fdc-433c-848e-38e825cb7a14.png | d3を右上へ修正 |
| Aim8-v3 | exec-b4dfe22a-cb36-4665-a568-45ef00116bc6.png | d0/d1/d7の手前投影を下向きに修正、現在優先 |
| CompactAim8-v3 | exec-3ee77685-ee45-4662-808d-deab82aabf25.png | front3投影修正 |
| CompactAim8-v4 | exec-39ac52cc-ade2-4f05-bcdc-8412baa454d6.png | d5上向き投影修正、現在優先 |
| CompactLowReady8-v1 | exec-dae78097-255a-40aa-b728-d81c2c5489ca.png | 腰前の両手拳銃low、現在優先 |

以降の多数生成はasset名対応のprompt.txtに逐語記録、生成原本は同Codexディレクトリに保持。新規PNGはAssetsへ原寸コピーする。最終32poseに `sourceWidth/sourceHeight` を記録し、Unity NPOT自動拡縮を禁止する（ImporterはPMO担当）。

本物のalphaを保持。例: LowReady初稿1536x1024 RGBA、alpha0約102万pixel、人物alphaは最大254。ビューアに見える茶色は透明pixelのRGBで背景へ描画されない。alpha閾値128の連結成分を使い全身外接矩形を測定。素材の画素加工・resize・切貼りはしていない。

metadataは実画像の任意rectを使う。銃先が数学セルをわずかに越えるため厳密な4x2境界では切らない。接地pivotは両足底中央、身長はalpha縦bbox。握り・支持・銃床・銃口は画面目視で測定。背面の不可視接点は実視認値ではなく投影推定であり `contactVisibility` に明示。`BuildPoseMetadata.py` は画素を書き換えずJSONだけ生成する。

## 追加工程

PMO担当へ `Portrait_*` のみ移管。Artは完成pose24、上半身レイヤー、実8phase走行脚、防具base/5slot overlay、weaponless/handsの順で段階制作。静止全装備画像の完成と、解除/差替え/走行の完成は区別する。全工程完了を現時点で主張しない。

更新: 拳銃lowを追加し4pose×8方向=32。Lowerは8方向×8phase=64個別原画を生成したが、生成誤差で支持脚の半周期交替が弱いviewがある。全身揺れや足回転だけで作ったものではない一方、滑らかな完全周期としてのQA合格とはまだ判断しない。

上半身だけを生成する試行は10〜30%ほど胴が拡大し、承認pixel完全保持に適合しないため非優先原本として残す。代わりにfullbody元PNGの `upperPolygon` をUV三角形で描画し脚だけ除外、下へ突き出す銃/手を残す。32polygonはmetadata内。画像の再加工はない。

baseはPlain olive jacket/brown trousers/shoesの同人物。Head=マスク/ゴーグル、Chest=ジャケット野戦具/ハーネス/肩板/腰ポーチ/bedroll、Arms=手袋、Legs=腿ポーチ/膝板/脚、Feet=ブーツの5slot。裸体にせず基本衣服を保持する。`LayerMasks.py` はsource pixelのslot境界をpolygon化し、Chestからglove領域を分割除外する。

weaponless試行は銃除去に成功するが一部手の位置まで変わるため、全身をそのまま差替える方法は採用しない。runtimeが元sourceのweaponPolygonだけを抜き、生成weaponlessは穴の中の衣服patchとして使用し、handPolygonsの元pixelを前面へ戻す。完全背面で銃/手が不可視なら `weaponOccluded/handsOccluded` とし架空maskを作らない。旧防具4系統の新pose個別原画は今回範囲外とPMO確定、性能は維持し新baseへfallbackする。

## 走行修正 / stage2e以降

Unity JsonUtilityのnested array非対応に合わせoverlay.polygonsを `[{points:[...]}]` へ修正した。runFront-v2は下段の支持脚を逆脚へ描き直した原画として採用、旧v1は保存。ほか7方向の限定phase修正とd1逆支持単独生成は、明示した左右指定に反して同じ支持脚へ戻る結果となり不採用（draft原本を保存）。成功したと主張しない。PMOは下半身のみmeshによるphase補正を許容したが、脚texture/ポーチ/plateの左右維持・膝/踵の自然さが条件。runtimeが具体可否を検討する。

lower base8方向は同poseの金属板/腿ポーチ/strapを除去し単純なcloth/shoesへ。slot3/4境界はboot領域boxと、その領域を除いたLegs矩形分割でUV描画する。画像原本の加工はしない。ブーツ境界の精度は統合画面で検証が必要。

全8RunBase PNGは1536×1024RGBA/alpha0〜254で保存し目視確認した。全参照36PNGの欠落0。Head-onlyに肩板とharnessが残ったstage3b所見を受け、Head maskを狭め、Chestを肩高さまで上げ、Head/Handsを除外した矩形分割へ変更。aim/compact/compact-lowの真後ろd4は手袋が完全遮蔽なので `occludedSlots:[2]` を明示、lowd4は一部手が見えるため対象外。

## d1独立剛体part / 2026-10-16

完成した走行画像の曲がった脚全体を反対姿勢へmesh変形すると、膝板/布陰影が矩形に歪むため棄却。PMO承認でd1一方向のみ独立pelvis＋左右thigh/shin/bootの7partを組み込みimagegen新規生成。原本RigidD1-draft1は膝板重複/左靴逆向きのため非優先。限定修正版RigidD1-v1.pngは太腿の膝板を除去し、両靴を右向きへ修正。完全膝板はshinに含める。原本exec-ffdd7733-0307-47e2-b81b-b3478454ae1b.png、初稿exec-a8dc8fa2-4e41-427f-9625-e21a8c60d589.png（既記載Codexディレクトリ）。逐語promptは同名prompt.txt。

RigidD1.pyが原寸1536×1024のpart ROI/start/end/pivot/bodyHeightPixelsをPosesへ反映。part独自scaleで画像を縮小加工せず脚長を校正し、膝/靴/ポーチの左右identityを維持。pelvisPartには可視腰上辺startと股end、左右hip点。d1全upperに実clothhemのhemAttach、lowerにwaistAttachを追加。上半身承認pixel不変。完成品質判定はruntime統合画像の接合/走行QAを待つ。

stage3g/stride4脱着は残像と矩形境界が未合格。元sourceに穴を抜いて生成clothingpatchを埋める方式は見逃した装備pixelが残るため、PMO承認で脱着時だけ生成basefull＋正確slot輪郭へ戻す。全5slot装備時は元完成fullsource一枚を維持。完全解決とは現時点で判断しない。

RigidD1-v2は細長い原画へ修正し元PNG保持。exec-ca2e9721-2548-4dcb-91e8-1d4dfa65d448.png。固定校正thigh.49/shin.48/boot.22mはneutral元絵と未一致のため、さらに生成せずNeutralD1-v1.jsonで元静止実pixel測定へ切替。元絵投影股膝R72/L69px、膝足首R77/L63px、布幅thigh65/60・shin48/46px。不可視解剖hip/ankleは4〜8px誤差の推定。runtimeが同カメラneutral並列比較を実施する。Boot sole実踏面中央はrigidPartsへ追加した。

追加生成停止指示より前に進行していた切断rim除去/cloth overlap限定生成は、返却原本をRigidD1-seam-draft3として保存し非採用。原本exec-efca5905-00d4-4dd7-93c7-2e29f283e4f2.png、逐語prompt同名。太腿下端/脛上端の黒い中空ellipseをclothで埋めることはできたがneutral比較前には切替えない。以降の生成は停止し測定/検証へ集中する。
