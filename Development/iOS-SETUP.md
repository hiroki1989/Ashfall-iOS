# ASHFALL iOS 独立プロジェクト / Independent iOS project

- コピー先 / Copy: `D:/Unity/Ashfall-iOS` (Unity 6000.6.0f1).
- 元プロジェクト / Original: `C:/Users/hiroki/OneDrive/ドキュメント/ChatGPT/見下ろし視点ハクスラローグライト`。今回のiOS実装はコピーのみ。Assetsは独立コピーであり元Assetsへの共有リンクではない。 / iOS changes belong only to this copy; Assets are copied, not shared with the original.
- Product `Ashfall iOS`, company `NeonVault`, bundle `com.neonvault.ashfall.ios`。元Windows版とPlayerPrefsの保存先を分離。クラウド接続IDはコピーで空欄。 / Separate product identity isolates Windows PlayerPrefs; copied cloud project identifiers are cleared.

## 設定 / Configuration

`Ashfall > iOS > Configure iOS` を明示実行。起動時の自動設定・プラットフォーム切替なし。既存startup sceneを保持し、旧 `ProjectBuilder.Prepare` は呼ばない。 / Configure explicitly; opening the project never switches platform. Existing startup scene is preserved, without calling the legacy Prepare method.

- iPhone + iPad、横向き左右のみ / both landscape orientations only.
- IL2CPP、ARM64、Metal。Managed stripping Low。レビュー/診断コードでreflectionを使用するため過度なstrippingを避ける。ただしLowだけではreflection経由の全到達性を保証しないので実機で確認する。 / Low stripping reduces risk for reflection; verify on device.
- 最低iOS 15.0：このゲームはiOS 16固有APIを要求していないため既存のUnity設定を維持し、対象端末を不要に狭めない。これは全iOS 15端末での性能保証ではない。 / Keep the existing iOS 15 floor because no iOS 16-only API is required; this is not a device-performance guarantee.
- モバイル目標30fps：現行の高密度人物/敵を考慮した初期予算。実機でGPU/CPU・発熱を計測してから調整。fps制御はruntime側。 / Initial mobile budget is 30 fps; runtime code owns the cap. Profile CPU/GPU and thermals before raising it.
- Apple Team・provisioning profileは未入力。署名情報は推測しない。Configureは後で開発者が入れた署名設定を保持。 / Signing is initially blank; configuration preserves subsequent developer choices.

## Windows preview / Windowsでの確認

`Ashfall > iOS > Build Windows mobile preview` は同じ `Assets/Scenes/NeonVault.unity` を使い、`Builds/MobilePreview/Ashfall-iOS-Preview.exe` を出力する。タッチ配置や日英表示をWindowsで確認するためのpreviewで、iOSアプリではない。実機タッチ、safe area、Metal、IL2CPP、性能、App Store適合性の検証は別途必要。 / The Windows preview checks layout and localization, not iOS installation or device performance.

## Xcode / 実機配布

ローカルEditorの `D:/Unity/Unity Hub/6000.6.0f1/Editor/Data/PlaybackEngines/iOSSupport` にモジュールの存在を確認済み。`Build Xcode project` メニューはUnityがiOS targetをsupportedと判定した場合のみ有効。モジュール不在時はHubで追加する。 / The module directory exists; Unity's target-support check controls export availability.

`Ashfall > iOS > Build Xcode project` は `Builds/iOS/Xcode` にdevelopment用Xcode sourceをexportする。Windowsでのexport成功は端末にインストールできるアプリの完成を意味しない。最終コンパイル・署名・インストールはmacOS + Xcode + Apple開発者のTeam/証明書/profileが必要。 / A source export is not an installable app. Final compilation, signing and installation require macOS, Xcode, and your Apple signing credentials.

Macへ独立コピーを移し、同一UnityバージョンとiOS Build Supportで開く → Configure iOS → Build Xcode project → Xcodeで固有Bundle ID/Team/署名を設定 → 接続したiPhone/iPadでRun。配布用はDevelopment buildを外しArchive。 / Open on Mac with matching Unity/iOS support, export, select your bundle ID and signing team, then run on a device. Use a non-development build and Archive for distribution.

実機確認項目：左右横向き、ノッチ/home indicatorとのsafe area、移動と照準/発射の同時タッチ、日英UI、セーブ再起動、30fps/発熱/大量敵。 / Device checks: both landscape directions, safe areas, simultaneous movement/aim/fire, both languages, save persistence, frame pacing and thermals under enemy load.

## タッチ操作 / Touch controls

- 左の半透明スティックでアナログ移動。右スティックを押してドラッグすると照準・連射、離すと射撃停止。移動と射撃を別の指で同時操作。 / Left analog movement; hold/drag the right stick to aim and fire, release to stop.
- 装填、拾得（チェスト・弾薬箱の開封も兼用）、回避、近接、武器切替、調べる、装備、一時停止。 / Reload, take/open containers, dodge, melee, swap, interact, inventory and pause.
- 装備一覧はドラッグ、都市地図はドラッグ＋2本指ピンチ。商店・返済・スキル・メーカー図鑑・報酬も大きな専用ボタン。 / Drag inventory, pan/pinch city map, large buttons for shops, debt, skills, makers and rewards.
- 画面切替・フォーカス喪失・中断時に保持入力を解除。装備画面などの表示中は戦闘操作を停止。 / Held input clears on transitions, focus loss and suspension; modal menus block combat input.
- `OpenUnity.bat` でこのコピーを開く。`PreviewTouch.bat` でタッチUI付きWindowsプレビューを起動。 / Launchers open this independent copy or its Windows touch preview.

端末用署名情報の保存エラーはゲーム実装とは別途のUnity Cloud設定。証明書・秘密鍵・パスワードをこのドキュメントやチャットに記録しない。 / Unity Cloud credential errors require separate signing configuration; never record private keys or passwords here.

## 最終検証 / Validation (2026-10-17)

Windowsタッチプレビュー: `QA/Mobile24final/mobile-review-result.txt` 221 checks / 0 failures。iPhone/iPad相当各日英、計32枚。独立QAは戦闘HUD・装備・商店・都市地図を目視。疑似ポインター検証であり実機検証ではない。 / Synthetic input and Windows framebuffers only.

Xcode source export: `Builds/iOS/Xcode/Unity-iPhone.xcodeproj`、修正版export成功。人物シェーダーをfull precision化しMetalコンパイルエラーを解消、再export区間 `QA/metal-recheck.log` にShader errorなし。Windows previewの221検証はこの精度修正前の同一UI版。 / Corrected Metal shader compilation passed and source project exported; device rendering and performance remain unverified.

署名済みIPA・実機インストールは未実施。Mac/Xcodeまたは適切な署名情報を設定したビルド環境で実施する。 / No signed IPA or device installation has been produced.
