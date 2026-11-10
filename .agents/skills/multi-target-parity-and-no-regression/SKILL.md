---
name: multi-target-parity-and-no-regression
description: >-
  Use whenever creating, editing, debugging, or syncing C# scripts, UI, gameplay systems,
  save-slot logic, or assets across Ashfall-iOS, DustDebt-Steam, and DustDebt-Steam-Demo.
  Enforces zero regressions, strict exclusion of iOS-only features (ads, daily chest, IAP,
  Game Center stub) from Steam/Demo, strict guarding of desktop-only code (SteamController)
  from iOS builds, and full gameplay/UI/save parity across all three targets.
---

# マルチプラットフォーム平仄一致 ＆ ゼロデグレ開発スキル

`Ashfall-iOS`・`DustDebt-Steam`・`DustDebt-Steam-Demo` の3プロジェクトを並行保守する際、**既存機能のデグレ（先祖返り・コンパイル破壊・挙動不整合）を絶対に起こさず、プラットフォーム固有要素を厳密に分離した上で共通要素の平仄を完全に合わせる**ための標準手順です。

---

## 1. 絶対遵守ルール

### A. ゼロデグレ（Zero Regression）原則
1. **全体コンパイル検証を必須化**:
   - スクリプト変更後は、1つのプロジェクトだけでなく **3プロジェクト全6構成**（iOS runtime `-define:UNITY_IOS`, iOS Editor `-define:UNITY_IOS`, Steam runtime/Editor, Demo runtime/Editor）すべてをコンパイル検証し、エラー0件を確認する。
2. **プラットフォーム境界のプリプロセッサ保護**:
   - 共有ファイル（`VaultGame.cs`, `VaultEquipmentScreen.cs`, `VaultInventoryUI.cs`, `VaultUI.cs`, `ActionSkillUI.cs` 等）で Steam/Desktop専用API（例：`SteamController`）を使う場合は、**必ず `#if !UNITY_IOS && !UNITY_ANDROID` ～ `#endif` で囲む**。
   - iOS専用API（例：`VaultDailyChest`, `VaultRewardedAds`, `IosPremiumAccess`）を共有ファイルから呼ぶ場合は、**必ず `#if UNITY_IOS || UNITY_ANDROID` ～ `#endif` で囲む**。
3. **`using` 追加による名前衝突の防止**:
   - `using System;` 等を追加する場合は `UnityEngine.Object` と `System.Object` の衝突（`CS0104`）が起きないか確認し、衝突する箇所では `System.Array` / `UnityEngine.Object` と完全修飾する。
4. **機能削除・巻き戻し（先祖返り）の禁止**:
   - ファイルを別プロジェクトへコピー・上書きする前に必ず差分を確認し、各プロジェクトのコントローラー対応・デモ制限文言・既存修正を上書き消去しない。

### B. iOS版特有の要素（Steam版・Demo版へ絶対に反映しないもの）
以下の要素は **iOS版専用** であり、Steam版・Demo版へ混入させてはなりません：
- **動画リワード広告・GDPR/ATTプライバシー設定**: `VaultRewardedAds.cs`、`VaultRewardedAds.*` 呼び出し、`FirstStepsTutorial.cs` 内の広告視聴に関する説明文、`VaultMobileUI.cs` の `AD PRIVACY` ボタン
- **広告連動デイリーチェスト**: `VaultDailyChest.cs`、`BuildDailyChest` / `UpdateDailyChestMarker` / `DrawDailyChestAdConfirm` / `DrawDailyChestReward` 等
- **App Store アプリ内課金（IAP）およびiOS課金解除UI**: `IosPremiumAccess` による購入・復元ダイアログ（ただし Demo版は Steam Demo固有の「体験版レベル3上限」ロジックと文言を維持すること）
- **Game Center 同期のiOS固有スタブ**: `GameCenterSyncManager.cs`（Steam/Demoの実装と競合させない）
- **タッチ操作専用UI**: `MobileTouchControls.cs` のレイアウト設定（※ただしアイコン描画・機能改善等の共通バグ修正は確認のうえ反映）

### C. Steam版・Demo版特有の要素（iOS版へ誤混入させないもの）
- `SteamController.cs`, `SteamControllerLiveReview.cs`（XInput/DirectInput/十字キー操作/メニューカーソル）
- Steam版・Demo版間の差分は **体験版Lv3上限および体験版案内文言（`VaultDebtSkills.cs`, `VaultRunSave.cs`, `SteamWindowsBuild.cs` 等）のみ** に限定する。

### D. それ以外の全要素（3プロジェクト間で徹底的に平仄を合わせる対象）
上記 B・C を除く以下のシステム・UI・バランス・バグ修正は、**常に3プロジェクト（iOS / Steam / Demo）間で最新仕様と一致させる**：
1. **セーブスロット完全分離**:
   - ゲーム進行・借金・NPC会話・チュートリアル・ペット・倉庫・中断セーブ等を扱う全ファイルで必ず `using PlayerPrefs = NeonVault.SaveSlotPrefs;` を使用する（素の `UnityEngine.PlayerPrefs` を誤用してスロット間でデータが混同するバグを防ぐ）。
   - 新しいセーブキーを追加した際は、必ず [`SaveSlotPrefs.DeleteSlot`](../../../Assets/Scripts/SaveSlotPrefs.cs) と [`ResetRuntimeStateForSlot`](../../../Assets/Scripts/SaveSlotMenu.cs) の両方にも追加する。
2. **アクションスキル ＆ スキルツリー（`ActionSkill*.cs`, `PetSkill*.cs`）**:
   - ツリー構成、階層・横並びブランチ差異化、キーストーン重複禁止、数値バランス、アイコン表示（モバイル対戦HUDのアイコン描画含む）。
3. **装備・戦闘・図鑑・拠点・オーバーワールド仕様**:
   - `GunClassCorrections.cs`, `rangeRetention`, `SharedArmorTextures.cs`, `VaultLegendaryEffects.cs`, `VaultCompletedRewardUI.cs`, `PetIonaExchange.cs`, `VaultManufacturerUI.cs` / `VaultMobileAuxUI.cs`（図鑑仕様）, `FirstStepsTutorial.cs`（UI非重複位置）, `VaultProgression.cs` / `VaultGame.cs`（死亡帰還後の拠点状態リセット）, `VaultMobileInputBridge.cs`（画面基準移動・照準）等。

---

## 2. 変更時の実行チェックリスト

1. **変更前**: 対象ファイルが3プロジェクト間で既にどう異なっているか `diff` を確認する（片方の独自修正を潰さない）。
2. **編集・同期**: 共通変更は `Ashfall-iOS`, `DustDebt-Steam`, `DustDebt-Steam-Demo` すべてに適用する。
3. **検証スクリプト実行**: 以下の統合チェックスクリプトを実行し、全項目 PASS を確認する：
   ```powershell
   python .agents/skills/multi-target-parity-and-no-regression/scripts/verify_parity.py
   ```
   このスクリプトは以下を自動検証します：
   - **全6アセンブリのRoslynコンパイルチェック**（iOS target / iOS editor with `UNITY_IOS`, Steam runtime/editor, Demo runtime/editor）
   - **未保護の `SteamController` 参照スキャン**（`Ashfall-iOS` 内）
   - **セーブデータ混同防止スキャン**（3プロジェクトでゲームセーブ系ファイルがすべて `SaveSlotPrefs` エイリアスを使用しているか）
   - **iOS専用ファイル・Steam専用ファイルの越境チェック**
   - **共有スクリプトの予期しない差分レポート**
4. **バージョン確認**: iOS版をビルド・提出する変更を行った場合は、`ProjectSettings/ProjectSettings.asset` の `bundleVersion` と `buildNumber.iPhone` の更新要否を確認する。
