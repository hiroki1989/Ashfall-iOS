using UnityEditor;
using UnityEngine;

// Import settings belong to these two new models only.
public static class FactionSoldierImport
{
    public static void Prepare()
    {
        foreach (var root in new[] { "Assets/Resources/Characters/Redcrow", "Assets/Resources/Characters/Coyote" })
            foreach (var guid in AssetDatabase.FindAssets("", new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx"))
                {
                    var model = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (!model) continue;
                    model.animationType = ModelImporterAnimationType.Generic;
                    model.optimizeBones = false; model.isReadable = true;
                    model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    model.SaveAndReimport();
                }
                else if (path.Contains("/Textures/") && path.EndsWith(".png"))
                {
                    var texture = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!texture) continue;
                    texture.maxTextureSize = 1024;
                    texture.textureCompression = TextureImporterCompression.CompressedHQ;
                    texture.mipmapEnabled = true;
                    if (path.EndsWith("_n.png")) texture.textureType = TextureImporterType.NormalMap;
                    if (path.EndsWith("_m.png")) texture.sRGBTexture = false;
                    texture.SaveAndReimport();
                }
            }
    }
}
