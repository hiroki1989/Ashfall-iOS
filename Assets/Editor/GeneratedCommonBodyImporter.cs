using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor
{
    // The common body is rebound to the existing gameplay rig at runtime, so its
    // imported vertices must remain readable. It is independent of every armor slot.
    public sealed class GeneratedCommonBodyImporter : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Art/GeneratedBase/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".fbx")) return;
            var model = (ModelImporter)assetImporter;
            model.importAnimation = false;
            model.animationType = ModelImporterAnimationType.None;
            model.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            model.importCameras = false;
            model.importLights = false;
            model.materialImportMode = ModelImporterMaterialImportMode.None;
            model.optimizeMeshVertices = false;
            model.isReadable = true;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png")) return;
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Default;
            texture.alphaIsTransparency = false;
            texture.mipmapEnabled = true;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.maxTextureSize = 4096;
            var ios = texture.GetPlatformTextureSettings("iPhone");
            ios.name = "iPhone";
            ios.overridden = true;
            ios.maxTextureSize = 4096;
            ios.format = TextureImporterFormat.ASTC_6x6;
            texture.SetPlatformTextureSettings(ios);
        }
    }
}
