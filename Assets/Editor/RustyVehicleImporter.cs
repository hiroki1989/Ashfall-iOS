using System;
using UnityEditor;
using UnityEngine;

// Creates two static, batchable variants from the imported low-poly pack.
public static class RustyVehicleImporter
{
    const string Source = "Assets/Rusty Vehicles/Rusty_Vehicles.fbx";
    const string Output = "Assets/Resources/ImportedWorld/RustyVehicles";
    [MenuItem("Tools/Dust & Debt/Import rusty vehicles")]
    public static void Run()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if (!model) throw new InvalidOperationException("Rusty vehicle model is missing: " + Source);
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld", "RustyVehicles");
        for (int variant = 0; variant < 2; variant++)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
            root.name = variant == 0 ? "rusty_sedan" : "rusty_pickup";
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rusty Vehicles/Materials/1K_" + (variant == 0 ? "Vehicle" : "Hillbilly") + "_TXTR.mat");
            if (!material) throw new InvalidOperationException("Vehicle material is missing");
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                string name = filter && filter.sharedMesh ? filter.sharedMesh.name : renderer.name;
                bool pickup = name.IndexOf("Hillbilly", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Billy", StringComparison.OrdinalIgnoreCase) >= 0;
                renderer.enabled = pickup == (variant == 1);
                if (renderer.enabled) { renderer.sharedMaterial = material; count++; }
            }
            if (count < 2) { UnityEngine.Object.DestroyImmediate(root); throw new InvalidOperationException("Vehicle mesh hierarchy unexpected for variant " + variant); }
            PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + root.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RUSTY_VEHICLES_IMPORT_OK");
    }
}
