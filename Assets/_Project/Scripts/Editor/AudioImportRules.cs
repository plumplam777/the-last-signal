using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Import settings for every clip under Audio/Resources, by folder (keeps build size and memory low):
    /// voices — mono Vorbis 45 %, compressed in memory, loaded in the background (TTS speech loses nothing audible);
    /// ambience — stereo Vorbis 55 %, streamed (long loops, never all in memory);
    /// SFX — mono Vorbis 60 %, decompressed on load (short, played often, no decode cost at play time).
    /// Reimport everything with Last Signal/Setup/Reimport Audio.
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.preloadAudioData = false;
            if (assetPath.Contains("/Voice/"))
            {
                importer.forceToMono = true;
                importer.loadInBackground = true;
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.quality = 0.45f;
            }
            else if (assetPath.Contains("/Ambience/"))
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                s.loadType = AudioClipLoadType.Streaming;
                s.quality = 0.55f;
            }
            else
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.quality = 0.6f;
            }
            importer.defaultSampleSettings = s;
        }

        [MenuItem("Last Signal/Setup/Reimport Audio")]
        public static void ReimportAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log("[AudioImportRules] Reimported all audio clips.");
        }
    }
}
