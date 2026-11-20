using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Builds the three quality levels (Low / Medium / High) from the project's PC URP asset, each with its own copy:
    /// render scale, shadow resolution/distance/cascades, point-light shadows, soft-shadow quality and SSAO differ.
    /// Medium is the default (the M4 laptop runs warm on the old "everything high" settings).
    /// Re-run after changing the PC asset: menu Last Signal/Setup/Build Quality Presets.
    /// </summary>
    public static class QualityPresets
    {
        private const string SourceAsset = "Assets/Settings/PC_RPAsset.asset";
        private const string SourceRenderer = "Assets/Settings/PC_Renderer.asset";
        private const string Folder = "Assets/Settings/Quality";
        private const string RendererNoAo = Folder + "/Renderer_NoSSAO.asset";

        private struct Preset
        {
            public string Name;
            public float RenderScale;
            public int MainShadowRes, AdditionalAtlas, TierLow, TierMed, TierHigh, Cascades, SoftQuality;
            public float ShadowDistance;
            public bool AdditionalShadows, SoftShadows, Ssao, DepthTexture;
        }

        public static readonly string[] LevelNames = { "Low", "Medium", "High" };
        public const int DefaultLevel = 1;

        private static readonly Preset[] Presets =
        {
            new() { Name = "Low", RenderScale = 0.7f, MainShadowRes = 1024, AdditionalShadows = false, AdditionalAtlas = 512,
                TierLow = 128, TierMed = 128, TierHigh = 256, Cascades = 1, ShadowDistance = 18f, SoftShadows = false, SoftQuality = 1,
                Ssao = false, DepthTexture = false },
            new() { Name = "Medium", RenderScale = 0.85f, MainShadowRes = 1024, AdditionalShadows = true, AdditionalAtlas = 1024,
                TierLow = 128, TierMed = 256, TierHigh = 512, Cascades = 2, ShadowDistance = 25f, SoftShadows = true, SoftQuality = 1,
                Ssao = false, DepthTexture = false },
            new() { Name = "High", RenderScale = 1f, MainShadowRes = 2048, AdditionalShadows = true, AdditionalAtlas = 2048,
                TierLow = 256, TierMed = 512, TierHigh = 1024, Cascades = 2, ShadowDistance = 35f, SoftShadows = true, SoftQuality = 2,
                Ssao = true, DepthTexture = true },
        };

        [MenuItem("Last Signal/Setup/Build Quality Presets")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Settings", "Quality");

            // renderer copy without SSAO (Low/Medium); High keeps the original renderer with SSAO
            AssetDatabase.DeleteAsset(RendererNoAo);
            AssetDatabase.CopyAsset(SourceRenderer, RendererNoAo);
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(RendererNoAo))
                if (sub != null && sub.name == "ScreenSpaceAmbientOcclusion")
                {
                    var so = new SerializedObject(sub);
                    so.FindProperty("m_Active").boolValue = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            var aoRenderer = AssetDatabase.LoadMainAssetAtPath(SourceRenderer);
            var noAoRenderer = AssetDatabase.LoadMainAssetAtPath(RendererNoAo);

            var pipelines = new RenderPipelineAsset[Presets.Length];
            for (int i = 0; i < Presets.Length; i++)
            {
                var p = Presets[i];
                string path = $"{Folder}/URP_{p.Name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CopyAsset(SourceAsset, path);
                var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
                var so = new SerializedObject(asset);
                so.FindProperty("m_RenderScale").floatValue = p.RenderScale;
                so.FindProperty("m_MainLightShadowmapResolution").intValue = p.MainShadowRes;
                so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = p.AdditionalShadows;
                so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = p.AdditionalAtlas;
                so.FindProperty("m_AdditionalLightsShadowResolutionTierLow").intValue = p.TierLow;
                so.FindProperty("m_AdditionalLightsShadowResolutionTierMedium").intValue = p.TierMed;
                so.FindProperty("m_AdditionalLightsShadowResolutionTierHigh").intValue = p.TierHigh;
                so.FindProperty("m_ShadowCascadeCount").intValue = p.Cascades;
                so.FindProperty("m_ShadowDistance").floatValue = p.ShadowDistance;
                so.FindProperty("m_SoftShadowsSupported").boolValue = p.SoftShadows;
                so.FindProperty("m_SoftShadowQuality").intValue = p.SoftQuality;
                so.FindProperty("m_RequireDepthTexture").boolValue = p.DepthTexture;
                so.FindProperty("m_RequireOpaqueTexture").boolValue = false; // no shader samples scene colour
                var list = so.FindProperty("m_RendererDataList");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = p.Ssao ? aoRenderer : noAoRenderer;
                so.FindProperty("m_DefaultRendererIndex").intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                pipelines[i] = asset;
            }

            // quality levels: exactly Low / Medium / High, each with its pipeline; vSync off (FrameRateGovernor caps fps)
            var qs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = qs.FindProperty("m_QualitySettings");
            int template = levels.arraySize - 1;
            while (levels.arraySize < Presets.Length) levels.InsertArrayElementAtIndex(template);
            while (levels.arraySize > Presets.Length) levels.DeleteArrayElementAtIndex(0);
            for (int i = 0; i < Presets.Length; i++)
            {
                var l = levels.GetArrayElementAtIndex(i);
                l.FindPropertyRelative("name").stringValue = Presets[i].Name;
                l.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipelines[i];
                l.FindPropertyRelative("vSyncCount").intValue = 0;
                var excluded = l.FindPropertyRelative("excludedTargetPlatforms"); // copied entries kept "Standalone" excluded
                if (excluded != null) excluded.ClearArray();
                l.FindPropertyRelative("antiAliasing").intValue = 0;
                l.FindPropertyRelative("realtimeReflectionProbes").boolValue = false;
                l.FindPropertyRelative("anisotropicTextures").intValue = i == 0 ? 0 : 1;
                l.FindPropertyRelative("globalTextureMipmapLimit").intValue = i == 0 ? 1 : 0;
            }
            qs.FindProperty("m_CurrentQuality").intValue = DefaultLevel;
            var perPlatform = qs.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < perPlatform.arraySize; i++)
                perPlatform.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue = DefaultLevel;
            qs.ApplyModifiedPropertiesWithoutUndo();

            // the graphics-settings default pipeline is used when a level has none; point it at Medium
            GraphicsSettings.defaultRenderPipeline = pipelines[DefaultLevel];
            AssetDatabase.SaveAssets();
            Debug.Log("[QualityPresets] Built Low/Medium/High quality levels (default Medium).");
        }
    }
}
