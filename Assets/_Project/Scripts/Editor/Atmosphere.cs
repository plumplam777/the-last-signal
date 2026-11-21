using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Cheap painterly atmosphere (ArtDirection.md "floating dust"): a slow field of soft additive motes per room.
    /// ~60 particles, no lighting, no collisions, no noise module — negligible cost even on Low quality.
    /// </summary>
    public static class Atmosphere
    {
        private const string TexturePath = "Assets/_Project/Art/Textures/FX/T_SoftDot.png";
        private const string MaterialPath = "Assets/_Project/Art/Materials/M_Dust_Additive.mat";

        public static ParticleSystem Dust(Transform parent, Vector3 center, Vector3 size, Color color, int maxParticles = 60)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.005f, 0.03f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.04f);
            main.startColor = color;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = maxParticles / 11f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;

            var vel = ps.velocityOverLifetime;                                  // slow drift, slightly upward
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.005f, 0.015f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var fade = ps.colorOverLifetime;                                    // fade in and out
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Material();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 0.02f;
            return ps;
        }

        private static Material Material()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.SetTexture("_BaseMap", Texture());
            mat.SetColor("_BaseColor", Color.white);
            // transparent, additive
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D Texture()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex != null) return tex;
            Directory.CreateDirectory(Path.GetDirectoryName(TexturePath)!);
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            File.WriteAllBytes(TexturePath, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(TexturePath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
    }
}
