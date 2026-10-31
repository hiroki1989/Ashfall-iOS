using UnityEditor;
namespace NeonVault.Editor
{
    public sealed class VaultArtImporter : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        static void RepairPlayerSurfaces()
        {
            EditorApplication.delayCall+=()=>
            {
                foreach(string name in new[]{"WastelandCloth","WastelandLeather"})
                {
                    string path="Assets/Resources/Art/"+name+".png";
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                }
                UnityEngine.Debug.Log("ASHFALL_PLAYER_SURFACES_READY");
            };
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/Art/"))return;
            var importer=(TextureImporter)assetImporter;
            if(assetPath.StartsWith("Assets/Resources/Art/UI/FieldKit/aerial-"))
            {
                bool source=assetPath.Contains("aerial-surfaces-")||assetPath.Contains("aerial-roofs-");
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=source?1024:4096;
                importer.isReadable=source;importer.mipmapEnabled=false;importer.alphaIsTransparency=false;
                importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.filterMode=UnityEngine.FilterMode.Bilinear;
                importer.textureCompression=source?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;
                var mobile=importer.GetPlatformTextureSettings("iPhone");mobile.name="iPhone";mobile.overridden=true;
                mobile.maxTextureSize=source?1024:4096;mobile.format=TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(mobile);
                return;
            }
            if(assetPath.StartsWith("Assets/Resources/Art/Portraits/"))
            {
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;
                importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=false;
                importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.filterMode=UnityEngine.FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                var ios=importer.GetPlatformTextureSettings("iPhone");ios.name="iPhone";ios.overridden=true;
                ios.maxTextureSize=1024;ios.format=TextureImporterFormat.ASTC_6x6;ios.compressionQuality=100;
                importer.SetPlatformTextureSettings(ios);
                return;
            }
            if(assetPath.StartsWith("Assets/Resources/Art/GeneratedWarden/"))
            {
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=false;importer.mipmapEnabled=true;
                importer.filterMode=UnityEngine.FilterMode.Trilinear;importer.anisoLevel=8;importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
                var ios=importer.GetPlatformTextureSettings("iPhone");ios.name="iPhone";ios.overridden=true;ios.maxTextureSize=4096;ios.format=TextureImporterFormat.ASTC_6x6;ios.compressionQuality=100;importer.SetPlatformTextureSettings(ios);
                return;
            }
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            if(assetPath.Contains("/ApprovedFieldkit/")){importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.filterMode=UnityEngine.FilterMode.Bilinear;}
            importer.isReadable=assetPath.Contains("/LayeredPlayer/")||assetPath.Contains("/LayeredEnemies/")||assetPath.Contains("Directions")||assetPath.Contains("Props")||assetPath.Contains("WeaponAtlas")||assetPath.Contains("32Walk")||assetPath.Contains("ItemPortrait")||assetPath.Contains("KeyEquipment")||assetPath.Contains("Yaw")||assetPath.Contains("Shooting32");
            bool tiled=assetPath.Contains("Floor")||assetPath.Contains("ArmorSurface")||assetPath.Contains("WastelandSurface")||assetPath.Contains("WastelandCloth")||assetPath.Contains("WastelandLeather");
            importer.alphaIsTransparency=true;importer.mipmapEnabled=tiled;
            importer.wrapMode=tiled?UnityEngine.TextureWrapMode.Repeat:UnityEngine.TextureWrapMode.Clamp;
            if(tiled){importer.filterMode=UnityEngine.FilterMode.Trilinear;importer.anisoLevel=8;}
            if(assetPath.Contains("/SurvivorMaterials/")||assetPath.Contains("/Environment/"))
            {importer.mipmapEnabled=true;importer.filterMode=UnityEngine.FilterMode.Trilinear;importer.anisoLevel=8;importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=false;}
            if(assetPath.Contains("/LayeredPlayer/")||assetPath.Contains("/LayeredEnemies/")){importer.mipmapEnabled=true;importer.filterMode=UnityEngine.FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;}
        }
    }
}

