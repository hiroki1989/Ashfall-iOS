using System;
using System.IO;
using UnityEditor;
namespace NeonVault.Editor {
 [InitializeOnLoad] static class MobileBuildRequest {
  static MobileBuildRequest() { EditorApplication.delayCall += Run; }
  static void Run() {
   const string request="Development/mobile-preview.request";
   if(!File.Exists(request)) return;
   File.Move(request,request+"."+DateTime.UtcNow.Ticks+".started");
   try { IOSProjectBuilder.BuildMobilePreview(); File.WriteAllText("Development/ios-export-support.txt",BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS).ToString()); File.WriteAllText("Development/mobile-preview.result","PASS "+DateTime.UtcNow.ToString("O")); }
   catch(Exception e) { File.WriteAllText("Development/mobile-preview.result",e.ToString()); UnityEngine.Debug.LogException(e); }
  }
 }
}
