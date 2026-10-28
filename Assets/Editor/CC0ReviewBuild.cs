using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace NeonVault.Editor {
 public static class CC0ReviewBuild {
  public static void Run() {
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes=new[]{"Assets/Scenes/NeonVault.unity"},
    locationPathName="D:/Unity/CC0AssetStaging/ReviewBuild/NeonVault.exe",
    target=BuildTarget.StandaloneWindows64,
    options=BuildOptions.Development
   });
   if(result.summary.result!=BuildResult.Succeeded)throw new Exception("CC0 review build failed: "+result.summary.result);
   UnityEngine.Debug.Log("CC0_REVIEW_BUILD_OK");
  }
 }
}
