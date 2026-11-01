using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ActionSkillBuildReview
{
    public static void Run()
    {
        AssetDatabase.DesiredWorkerCount=1;
        EditorUtility.UnloadUnusedAssetsImmediate();
        System.GC.Collect();System.GC.WaitForPendingFinalizers();System.GC.Collect();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/NeonVault.unity"},
            locationPathName="D:/Unity/BuildTemp/ActionSkillBuildReview/NeonVault.exe",
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.Development
        });
        Debug.Log("ACTION_SKILL_BUILD "+report.summary.result);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Skill review build failed: "+report.summary.result);
    }
}
