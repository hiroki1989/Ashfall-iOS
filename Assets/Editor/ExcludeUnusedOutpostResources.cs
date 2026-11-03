using System;
using UnityEditor;
using UnityEngine;

public static class ExcludeUnusedOutpostResources
{
    public static void Run()
    {
        const string source = "Assets/OutPost/Resources";
        const string target = "Assets/OutPost/OutpostSourceAssets";
        if (AssetDatabase.IsValidFolder(target))
        {
            if (AssetDatabase.IsValidFolder(source)) throw new Exception("Both source and destination exist; no move performed.");
            Debug.Log("OUTPOST_RESOURCE_EXCLUSION_ALREADY_APPLIED");
            return;
        }
        if (!AssetDatabase.IsValidFolder(source)) throw new Exception("OutPost Resources folder not found; no move performed.");
        string error = AssetDatabase.MoveAsset(source, target);
        if (!string.IsNullOrEmpty(error)) throw new Exception("OutPost folder move failed: " + error);
        AssetDatabase.SaveAssets();
        Debug.Log("OUTPOST_RESOURCE_EXCLUSION_APPLIED: GUIDs and source assets preserved");
    }
}
