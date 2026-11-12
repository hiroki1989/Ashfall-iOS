#if UNITY_EDITOR
using UnityEditor;
sealed class KnoxImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if(assetPath=="Assets/Resources/Characters/Knox/Knox.fbx")
        {
            var importer=(ModelImporter)assetImporter;
            importer.isReadable=true;
            importer.importAnimation=false;
            importer.optimizeGameObjects=false;
        }
    }
    void OnPreprocessTexture()
    {
        if(assetPath=="Assets/Resources/Characters/Knox/knox_n.png")
            ((TextureImporter)assetImporter).textureType=TextureImporterType.NormalMap;
    }
}
#endif
