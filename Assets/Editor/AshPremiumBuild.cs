#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class AshPremiumBuild
{
    [PostProcessBuild]
    public static void AddFrameworks(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        string projectPath = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();
        project.AddFrameworkToProject(frameworkTarget, "StoreKit.framework", false);
        project.AddFrameworkToProject(frameworkTarget, "Security.framework", false);
        project.WriteToFile(projectPath);
    }
}
#endif
