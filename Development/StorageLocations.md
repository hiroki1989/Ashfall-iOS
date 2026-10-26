# iOS版の保存先

このiOSプロジェクトの作業本体は D:/Unity/Ashfall-iOS。今後のコード、アセット、検証画像、ログ、ビルド出力はすべてDへ保存する。
C:/Users/hiroki/OneDrive/ドキュメント/ChatGPT/見下ろし視点ハクスラローグライト は別の旧PC版であり、このiOS版を編集するときの作業ディレクトリには使わない。旧PC版は移動・削除していない。

起動: D:/Unity/Ashfall-iOS/OpenUnity.bat
この起動ファイルはUnityプロセスのTEMP/TMPを D:/Unity/ProcessTemp/Ashfall-iOS、UPM_CACHE_ROOTを D:/Unity/SharedCaches/PackageCache、Editorログを D:/Unity/Ashfall-iOS/Logs/Editor.log に設定する。
Unity Hubから直接起動すると、このプロセス専用TEMP/ログ設定は適用されない。Unity共有キャッシュのCパスはDへのジャンクションにするため、従来の参照でもデータ本体はDへ書かれる。
セーブ・会話・旧PC版・他プロジェクト本体・Windowsの設定・認証情報は移動しない。