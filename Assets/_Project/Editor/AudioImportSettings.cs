#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets import settings on everything under Resources/Audio automatically: short effects are
/// decompressed into memory (instant playback, no decode hitch), music streams from disk.
/// Runs on import, so regenerating clips with tools/make_audio.py needs no manual steps.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    private const string Root = "Assets/_Project/Resources/Audio/";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/Music/");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = 0.7f;
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = music;
    }

    [MenuItem("Exterminator/Reimport Audio")]
    public static void ReimportAll()
    {
        AssetDatabase.ImportAsset("Assets/_Project/Resources/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
    }
}
#endif
