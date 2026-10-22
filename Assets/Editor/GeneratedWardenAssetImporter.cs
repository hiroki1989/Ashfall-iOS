using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor
{
    public sealed class GeneratedWardenAssetImporter : AssetPostprocessor
    {
        const string Folder="Assets/Resources/Art/GeneratedWarden/";

        public static void ReimportStaticModels()
        {
            foreach(var guid in AssetDatabase.FindAssets("",new[]{Folder.TrimEnd('/')}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                if(path.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase))
                {
                    var model=AssetImporter.GetAtPath(path) as ModelImporter;
                    if(model==null)continue;
                    model.importAnimation=false;
                    model.animationType=ModelImporterAnimationType.None;
                    model.avatarSetup=ModelImporterAvatarSetup.NoAvatar;
                    model.SaveAndReimport();
                }
                else if(path.EndsWith(".png",System.StringComparison.OrdinalIgnoreCase))
                {
                    var texture=AssetImporter.GetAtPath(path) as TextureImporter;
                    if(texture==null)continue;
                    ConfigureTexture(texture);
                    texture.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Generated Warden FBX models reimported as static meshes.");
        }

        public static void ReimportPoseArmor()
        {
            var model=AssetImporter.GetAtPath(Folder+"armor.fbx") as ModelImporter;
            if(model==null)throw new System.InvalidOperationException("Missing generated Warden armor FBX");
            model.isReadable=true;model.SaveAndReimport();AssetDatabase.SaveAssets();
            Debug.Log("Generated Warden armor imported with readable vertices for joint segmentation.");
        }

        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith(Folder))return;
            var importer=(ModelImporter)assetImporter;
            importer.importAnimation=false;
            importer.animationType=ModelImporterAnimationType.None;
            importer.avatarSetup=ModelImporterAvatarSetup.NoAvatar;
            importer.importCameras=false;
            importer.importLights=false;
            importer.meshCompression=ModelImporterMeshCompression.Off;
            if(assetPath.EndsWith("-rig-ready.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("-mobile.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("-rebuilt.fbx",System.StringComparison.OrdinalIgnoreCase))
            {
                // Smooth scan facets so the gameplay depth-normal ink pass does not
                // turn every tiny triangle into a black speckle. Preserve sharp edges.
                importer.importNormals=ModelImporterNormals.Calculate;
                importer.normalCalculationMode=ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                importer.normalSmoothingSource=ModelImporterNormalSmoothingSource.FromAngle;
                importer.normalSmoothingAngle=60f;
            }
            importer.optimizeMeshPolygons=true;
            importer.optimizeMeshVertices=false;
            importer.isReadable=assetPath.EndsWith("/armor.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/body.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/armor-rig-ready.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/body-rig-ready.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/body-mobile.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/armor-mobile.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/head-rebuilt.fbx",System.StringComparison.OrdinalIgnoreCase)
                ||assetPath.EndsWith("/armor-rebuilt.fbx",System.StringComparison.OrdinalIgnoreCase);
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
        }

        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Folder))return;
            ConfigureTexture((TextureImporter)assetImporter);
        }

        static void ConfigureTexture(TextureImporter importer)
        {
            importer.textureType=TextureImporterType.Default;
            var settings=new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.maxTextureSize=4096;
            settings.mipmapEnabled=true;
            settings.filterMode=FilterMode.Trilinear;
            settings.aniso=8;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize=4096;
            importer.mipmapEnabled=true;
            importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=8;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=false;
            var desktop=importer.GetDefaultPlatformTextureSettings();
            desktop.name="DefaultTexturePlatform";desktop.overridden=true;desktop.maxTextureSize=4096;
            desktop.textureCompression=TextureImporterCompression.Uncompressed;
            desktop.format=TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(desktop);
            var ios=importer.GetPlatformTextureSettings("iPhone");
            ios.name="iPhone";ios.overridden=true;ios.maxTextureSize=4096;ios.format=TextureImporterFormat.ASTC_6x6;ios.compressionQuality=100;
            importer.SetPlatformTextureSettings(ios);
        }
    }
}
