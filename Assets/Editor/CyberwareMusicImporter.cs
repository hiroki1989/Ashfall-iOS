using UnityEditor;
using UnityEngine;
public sealed class CyberwareMusicImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/Resources/CyberwareBGM/")) return;
        var importer=(AudioImporter)assetImporter;
        importer.forceToMono=false;
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.Streaming;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;
        settings.quality=.75f;
        importer.defaultSampleSettings=settings;
    }
}
