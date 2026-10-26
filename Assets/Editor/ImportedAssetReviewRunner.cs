using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;
namespace NeonVault.Editor {
 [InitializeOnLoad]public static class ImportedAssetReviewRunner {
  static ImportedAssetReviewRunner(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--asset-review")>=0)EditorApplication.update+=Tick;}
  public static void Run(){SessionState.SetFloat("AssetReviewStart",(float)EditorApplication.timeSinceStartup);EditorSceneManager.OpenScene("Assets/Scenes/NeonVault.unity");EditorApplication.isPlaying=true;}
  static void Tick(){if(File.Exists("D:/Unity/AssetIntegration/Review/result.txt")){EditorApplication.Exit(0);}else if(EditorApplication.timeSinceStartup-SessionState.GetFloat("AssetReviewStart",(float)EditorApplication.timeSinceStartup)>240){UnityEngine.Debug.LogError("ASSET_REVIEW_TIMEOUT");EditorApplication.Exit(2);}}
 }
}
