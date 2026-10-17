# ASHFALL iOS / TestFlight

## Ad Hoc is the default / 既定は Ad Hoc

Select `distribution: adhoc` (default) to export a signed IPA artifact only. It requires `IOS_P12_BASE64`, `IOS_P12_PASSWORD`, `IOS_PROFILE_BASE64`, and `APPLE_TEAM_ID`; **no App Store Connect API key or issuer ID is needed**. Supply the existing Ad Hoc profile in `IOS_PROFILE_BASE64`. Validation requires registered devices and `get-task-allow = false`; export uses Xcode's `release-testing` method. The IPA installs only on the devices registered in that profile. Ad Hoc IPAs cannot be uploaded to App Store Connect.

既定の `adhoc` は既存の Ad Hoc プロファイルを使い、署名済み IPA を Actions artifact として保存する。Issuer ID・ASC キーは不要。プロファイルに登録済みの端末だけへインストール可能。App Store Connect にアップロードするときだけ `app-store` を選び、App Store 配布プロファイルと下記 ASC secrets を使用する。

The remaining App Store setup and upload notes apply only to `distribution: app-store`.

日本語: Actions の **iOS TestFlight → Run workflow** を手動実行する。Unity ライセンスは不要。公開リポジトリ `hiroki1989/Ashfall-iOS` の Release `ios-source-20261017` に、Xcode 出力の内容を ZIP 直下に置いた `Ashfall-Xcode.zip` が必要。`Unity-iPhone.xcodeproj` と `Info.plist` は ZIP 直下。

English: Manually run **Actions → iOS TestFlight → Run workflow**. No Unity license is required. The same repository release `ios-source-20261017` must contain `Ashfall-Xcode.zip`, with the exported Xcode contents at the archive root.

Repository Actions secrets / リポジトリの Actions secrets:

| Secret | Value / 値 |
|---|---|
| `IOS_P12_BASE64` | Base64 of an Apple Distribution certificate **with its private key** / 秘密鍵付き配布証明書 |
| `IOS_P12_PASSWORD` | Nonempty P12 export password / P12 パスワード |
| `IOS_PROFILE_BASE64` | Base64 of the App Store distribution `.mobileprovision` for `com.hiroki1989.ashfall` / この Bundle ID の App Store 配布プロファイル |
| `ASC_KEY_P8` | Raw App Store Connect API `.p8` PEM text, including headers / ヘッダー付き PEM 原文 |
| `ASC_KEY_ID` | API key ID |
| `ASC_ISSUER_ID` | API issuer UUID |
| `APPLE_TEAM_ID` | Apple Developer team ID |

The App Store Connect app record must already exist with the bundle ID above. Use an API key authorized to upload builds for that app. Secrets are never committed. Profile checks reject development, ad hoc, enterprise, expired, wrong-team and wrong-bundle profiles, and require a matching imported distribution identity with a private key. Credentials and the temporary keychain are removed in an `always()` step. The signed IPA is retained as an Actions artifact for seven days; it contains the public provisioning profile as expected for signed iOS apps.

App Store Connect に同じ Bundle ID のアプリ登録が必要。API キーにはアップロード権限が必要。配布先テスターの指定・外部ベータ審査・App Store 公開は行わず、ビルドのアップロードまで実施する。Apple 側の処理完了を待たないため、ジョブ成功後も App Store Connect で処理結果を確認する。

The workflow uses `macos-15`, selects the newest installed stable Xcode (26 or newer), archives Release / Unity-iPhone with manual app signing, disables target signing for UnityFramework and other non-app targets, and exports with `app-store-connect`. Build number is `GITHUB_RUN_NUMBER + 100`; rerunning the same run reuses that number, so start a **new dispatch** after an upload. If App Store Connect already contains a higher build number, increase the offset before running. The image must contain an Xcode/iOS SDK version accepted by Apple at the time of upload; this job cannot guarantee future acceptance.

Windows ZIP permissions are restored for Mach-O binaries, shebang scripts, and `.sh` files. The source must include **macOS-compatible IL2CPP tooling**; Windows `.exe` tools cannot run on the Mac. A source export made with Unity Development Build retains its compiled development/debug behavior: selecting Xcode Release does not remove Unity development code. Regenerate a non-development Unity iOS export for production-quality testing. The workflow does not modify Unity gameplay code or rebuild it with Unity.

Windows エクスポート由来の IL2CPP ツールが macOS 用バイナリを含まない場合、Mac ビルドは失敗する。実行権限の復元では Windows 実行ファイルを macOS 用へ変換できない。Unity Development Build のデバッグ動作は Xcode Release だけでは除去されない。本番品質のテストには Development Build を無効にした再エクスポートが必要。

Local checks only / ローカル検証のみ: Python syntax and fixture-based profile validation can run on Windows; Xcode, `security`, signing, archive/export and upload require the macOS Actions run with valid Apple credentials. This change does not perform an upload or access configured secrets.

References: [fastlane TestFlight options](https://docs.fastlane.tools/actions/pilot/), [Apple manual signing](https://help.apple.com/xcode/mac/current/en.lproj/dev1bf96f17e.html), [Apple archive export](https://help.apple.com/xcode/mac/current/en.lproj/deva1f2ab5a2.html).
