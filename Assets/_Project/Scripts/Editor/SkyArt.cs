using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Procedural textures for what the player sees through the windows (Phase 7 lighting pass): Nereth's night side
    /// (dark ice with faint blue fracture lines that carry the Murmur pulse) and a dark starfield. Generated once and
    /// saved as PNG under Art/Textures, so builds do not regenerate them. Deterministic (fixed seeds).
    /// </summary>
    public static class SkyArt
    {
        private const string Folder = "Assets/_Project/Art/Textures/Sky";

        /// <summary>URP Lit material for the planet sphere: dark ice albedo + crack emission map (EmissionPulse tints it).</summary>
        public static Material Nereth()
        {
            var albedo = Texture("T_Nereth_Albedo", 1024, 512, NerethAlbedo, true);
            var cracks = Texture("T_Nereth_Cracks", 1024, 512, NerethCracks, true);
            var mat = BuilderKit.Lit("M_Nereth_Ice", Color.white, 0.62f, 0f);
            mat.SetTexture("_BaseMap", albedo);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", cracks);
            mat.SetColor("_EmissionColor", BuilderKit.Hex("#2F8FD8") * 0.6f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Unlit starfield for the big backdrop quad behind the planet.</summary>
        public static Material Starfield(float tiling = 3f)
        {
            var tex = Texture("T_Starfield", 1024, 1024, Stars, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            const string path = "Assets/_Project/Art/Materials/M_Starfield.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling * 0.6f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ generators

        private static Vector3 Dir(float u, float v)
        {
            float lon = u * Mathf.PI * 2f, lat = (v - 0.5f) * Mathf.PI;
            return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
        }

        /// <summary>Seam-free pseudo-3D noise: Perlin on the three axis-plane projections of the direction.</summary>
        private static float Noise3(Vector3 d, float f, float seed)
        {
            float a = Mathf.PerlinNoise(d.x * f + seed, d.y * f + 11.3f);
            float b = Mathf.PerlinNoise(d.y * f + 37.1f, d.z * f + seed);
            float c = Mathf.PerlinNoise(d.z * f + seed * 0.5f, d.x * f + 71.7f);
            return (a + b + c) / 3f;
        }

        private static float Fbm(Vector3 d, float f, float seed, int octaves = 5)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise3(d, f, seed + i * 13.7f) * amp;
                norm += amp;
                f *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        private static float Ridge(Vector3 d, float f, float seed)
        {
            float n = Noise3(d, f, seed);
            float r = 1f - Mathf.Abs(n * 2f - 1f);
            return Mathf.Pow(Mathf.Clamp01(r), 32f);
        }

        private static Color NerethAlbedo(float u, float v)
        {
            var d = Dir(u, v);
            float n = Fbm(d, 3.2f, 5.1f);
            float bands = Mathf.PerlinNoise(v * 9f, 3.3f) * 0.25f;                      // faint latitude streaks
            var deep = BuilderKit.Hex("#0B1622");
            var ice = BuilderKit.Hex("#3B5568");
            float t = Mathf.SmoothStep(0.25f, 0.75f, n + bands - 0.1f);
            float polar = Mathf.SmoothStep(0.72f, 0.95f, Mathf.Abs(v - 0.5f) * 2f);      // brighter polar caps
            return Color.Lerp(Color.Lerp(deep, ice, t * 0.8f), BuilderKit.Hex("#6F8A9A"), polar * 0.6f);
        }

        private static Color NerethCracks(float u, float v)
        {
            var d = Dir(u, v);
            float cracks = Mathf.Max(Ridge(d, 5.5f, 2.2f), Ridge(d, 13f, 8.8f) * 0.55f);
            float glowPools = Mathf.SmoothStep(0.62f, 0.8f, Fbm(d, 2.1f, 41f, 3)) * 0.22f; // light under thin ice
            float c = Mathf.Clamp01(cracks + glowPools);
            return new Color(c, c, c, 1f);
        }

        private static Color Stars(float u, float v)
        {
            // hash-based stars on a grid (one candidate per cell), faint blue-grey nebula haze underneath
            const int cells = 180;
            float cx = u * cells, cy = v * cells;
            int ix = Mathf.FloorToInt(cx), iy = Mathf.FloorToInt(cy);
            float best = 0f;
            for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            {
                int gx = (ix + ox + cells) % cells, gy = (iy + oy + cells) % cells;
                float h = Hash(gx, gy);
                if (h < 0.55f) continue;                                                    // ~45% of cells have a star
                float sx = ix + ox + Hash(gx + 17, gy) , sy = iy + oy + Hash(gx, gy + 29);
                float dist = Mathf.Sqrt((cx - sx) * (cx - sx) + (cy - sy) * (cy - sy));
                float size = 0.12f + (h - 0.55f) * 0.5f;
                float b = Mathf.Clamp01(1f - dist / size) * (0.25f + Hash(gx + 5, gy + 7) * 0.75f);
                best = Mathf.Max(best, b);
            }
            float haze = Mathf.PerlinNoise(u * 4f + 3f, v * 4f + 9f) * Mathf.PerlinNoise(u * 9f, v * 9f) * 0.05f;
            var bg = BuilderKit.Hex("#03060B");
            return bg + new Color(haze * 0.6f, haze * 0.8f, haze, 0f) + new Color(best * 0.85f, best * 0.9f, best, 0f);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // ------------------------------------------------------------------ asset helper

        private static Texture2D Texture(string name, int w, int h, System.Func<float, float, Color> pixel, bool clampU)
        {
            string path = $"{Folder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;                                          // delete the PNG to regenerate
            Directory.CreateDirectory(Folder);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel((x + 0.5f) / w, (y + 0.5f) / h);
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapModeU = clampU ? TextureWrapMode.Repeat : TextureWrapMode.Repeat;
            importer.wrapModeV = clampU ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
