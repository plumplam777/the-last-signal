using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Hand-painted-style surface detail for the station materials (Phase 7 art pass, ArtDirection.md "Materials"):
    /// soft mottled paint, panel seams with rivets, worn floor plates, brushed brass with patina, chipped enamel.
    /// Each texture is a near-white detail map (the material's _BaseColor keeps the palette hue) plus a normal map
    /// derived from a height field. Seamlessly tiling, deterministic, generated once into Art/Textures/Surfaces
    /// (delete a PNG to regenerate). Boxes get UVs in metres (BuilderKit.Box), so the material tiling below is
    /// "texture repeats per metre".
    /// </summary>
    public static class StationTextures
    {
        private const string Folder = "Assets/_Project/Art/Textures/Surfaces";
        private const int Size = 512;

        /// <summary>Generated map pair; Tiling = repeats per metre.</summary>
        public readonly struct Maps
        {
            public readonly Texture2D Albedo, Normal;
            public readonly float Tiling;
            public Maps(Texture2D a, Texture2D n, float tiling) { Albedo = a; Normal = n; Tiling = tiling; }
        }

        public static Maps PaintedMetal() => Get("PaintedMetal", PaintedMetalPixel, 2.2f, 0.5f);   // 2 m tile, 1 m x 2 m panels
        public static Maps FloorPlate() => Get("FloorPlate", FloorPixel, 1.6f, 1f);                 // 1 m plates
        public static Maps CeilingPanel() => Get("CeilingPanel", CeilingPixel, 1.4f, 0.5f);        // 2 m tile, 1 m panels
        public static Maps Brass() => Get("Brass", BrassPixel, 0.6f, 1f);                           // 1 m tile
        public static Maps Enamel() => Get("Enamel", EnamelPixel, 0.8f, 1f);                        // 1 m tile

        /// <summary>Trim band height the rondel motif is made for: one square motif per <see cref="TrimHeight"/> metres.</summary>
        public const float TrimHeight = 0.22f;
        /// <summary>Full-colour ornamental band (reference A): brass rondels in square brass frames on terracotta enamel.</summary>
        public static Maps RondelTrim() => Get("RondelTrim", RondelPixel, 2.2f, 1f / TrimHeight);

        /// <summary>Puts the maps on a URP Lit material.</summary>
        public static void Apply(Material mat, Maps maps, float normalStrength = 1f)
        {
            mat.SetTexture("_BaseMap", maps.Albedo);
            mat.SetTextureScale("_BaseMap", Vector2.one * maps.Tiling);
            mat.SetTexture("_BumpMap", maps.Normal);
            mat.SetFloat("_BumpScale", normalStrength);
            mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
        }

        // ------------------------------------------------------------------ pixel functions
        // (u, v) in [0,1) over one tile; return (albedo multiplier, height 0..1)

        private struct Px
        {
            public Color C;
            public float H;
            public Px(Color c, float h) { C = c; H = h; }
        }

        private static Px PaintedMetalPixel(float u, float v)
        {
            // panels: 2 across (1 m) x 1 high (2 m) per tile
            float pu = Frac(u * 2f), pv = v;
            float du = Mathf.Min(pu, 1f - pu) * 256f, dv = Mathf.Min(pv, 1f - pv) * 512f;       // px to nearest seam
            float d = Mathf.Min(du, dv);

            float paint = 0.88f + (Fbm(u, v, 4, 4, 3.1f) - 0.5f) * 0.22f;                       // soft mottling
            paint += (Fbm(u * 1f, v * 1f, 32, 2, 7.7f) - 0.5f) * 0.05f;                          // brush grain
            paint += (pv - 0.5f) * 0.08f;                                                         // lighter at the top
            float grime = Smooth(0.22f, 0f, pv) * (0.6f + Fbm(u, v, 16, 3, 9.2f) * 0.6f);
            paint -= grime * 0.16f;

            float h = 0.6f;
            // seam groove + raised bevel lip
            float groove = Smooth(3.5f, 1.2f, d);
            float lip = Smooth(3f, 6f, d) * Smooth(12f, 7f, d);
            h += lip * 0.12f - groove * 0.5f;
            paint *= 1f - groove * 0.55f;
            paint += lip * 0.05f;

            // rivet rows along the vertical seams every 0.25 m
            float ry = Frac(v * 8f) - 0.5f;                                                      // 8 per 2 m
            foreach (float side in new[] { 11f, -11f })
            {
                float rx = (pu < 0.5f ? pu * 256f : (pu - 1f) * 256f) - side;
                float r = Mathf.Sqrt(rx * rx + ry * 64f * ry * 64f);
                float dome = Mathf.Clamp01(1f - r / 4.5f);
                h += Mathf.Sqrt(dome) * 0.35f;
                paint += dome * 0.12f;
            }
            // small chips of paint at panel edges
            float chip = Smooth(0.74f, 0.8f, Fbm(u, v, 24, 3, 2.4f)) * Smooth(30f, 6f, d);
            paint = Mathf.Lerp(paint, 0.62f, chip);
            h -= chip * 0.08f;

            return new Px(Tint(paint, 1f, 1.0f, 1.03f), h);
        }

        private static Px FloorPixel(float u, float v)
        {
            float du = Mathf.Min(u, 1f - u) * Size, dv = Mathf.Min(v, 1f - v) * Size;
            float d = Mathf.Min(du, dv);
            float baseC = 0.86f + (Fbm(u, v, 4, 4, 1.3f) - 0.5f) * 0.18f;
            // centre worn lighter by footsteps, edges dirtier
            float cx = u - 0.5f, cy = v - 0.5f;
            float wear = Smooth(0.42f, 0.05f, Mathf.Sqrt(cx * cx + cy * cy)) * Fbm(u, v, 8, 3, 4.4f);
            baseC += wear * 0.14f;
            baseC -= Smooth(40f, 4f, d) * 0.1f * Fbm(u, v, 16, 2, 8.8f);

            float h = 0.6f;
            // diamond tread every 1/16 m
            float tu = Frac(u * 16f) - 0.5f, tv = Frac(v * 16f) - 0.5f;
            bool alt = (Mathf.FloorToInt(u * 16f) + Mathf.FloorToInt(v * 16f)) % 2 == 0;
            float ax = alt ? tu + tv : tu - tv, ay = alt ? tu - tv : tu + tv;
            float tread = Mathf.Clamp01(1f - Mathf.Abs(ax) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(ay) * 2.6f);
            h += Smooth(0f, 0.5f, tread) * 0.07f * Smooth(10f, 22f, d);
            baseC += tread * 0.025f * (1f - wear);

            // plate seam + bevel
            float groove = Smooth(4f, 1.5f, d);
            float bevel = Smooth(4f, 12f, d);
            h = Mathf.Lerp(h * 0.4f, h, bevel) - groove * 0.4f;
            baseC *= 1f - groove * 0.6f;

            // corner bolts
            float bolt = Mathf.Clamp01(1f - Mathf.Sqrt((du - 22f) * (du - 22f) + (dv - 22f) * (dv - 22f)) / 7f);
            h += Mathf.Sqrt(bolt) * 0.3f;
            baseC += bolt * 0.15f;
            return new Px(Tint(baseC, 1f, 1f, 1.02f), h);
        }

        private static Px CeilingPixel(float u, float v)
        {
            float pu = Frac(u * 2f), pv = Frac(v * 2f);
            float du = Mathf.Min(pu, 1f - pu) * 256f, dv = Mathf.Min(pv, 1f - pv) * 256f;
            float d = Mathf.Min(du, dv);
            float c = 0.9f + (Fbm(u, v, 4, 3, 5.5f) - 0.5f) * 0.14f;
            float h = 0.6f;
            float groove = Smooth(4f, 1.5f, d);
            float inset = Smooth(26f, 30f, d);                                          // recessed centre panel
            h += -groove * 0.45f - inset * 0.12f;
            c *= 1f - groove * 0.5f;
            c -= inset * 0.06f;
            // vent slots in alternate panels
            bool vent = (Mathf.FloorToInt(u * 2f) + Mathf.FloorToInt(v * 2f)) % 2 == 0;
            if (vent && du > 70f && du < 186f && dv > 70f)
            {
                float slot = Frac(pv * 18f);
                float s = Smooth(0.25f, 0.35f, slot) * Smooth(0.75f, 0.65f, slot);
                h -= s * 0.25f;
                c *= 1f - s * 0.55f;
            }
            return new Px(Tint(c, 1f, 1f, 1.03f), h);
        }

        private static Px BrassPixel(float u, float v)
        {
            // polished, brushed along u; patina only as faint, rare spots (it should read as cared-for brass)
            float brushed = (Fbm(u * 0.25f, v, 48, 2, 6.6f) - 0.5f) * 0.08f + (Fbm(u, v, 2, 3, 3.3f) - 0.5f) * 0.1f;
            float c = 0.97f + brushed;
            float patina = Smooth(0.68f, 0.8f, Fbm(u, v, 3, 4, 12.1f)) * 0.6f;
            c -= patina * 0.12f;
            var col = Color.Lerp(Tint(c, 1f, 1f, 1f), new Color(0.7f, 0.78f, 0.68f), patina * 0.2f);
            float h = 0.5f + brushed * 0.4f;
            return new Px(col, h);
        }

        private static Px EnamelPixel(float u, float v)
        {
            float c = 0.92f + (Fbm(u, v, 4, 4, 21.3f) - 0.5f) * 0.14f;
            c += (Fbm(u, v, 48, 2, 3.9f) - 0.5f) * 0.04f;
            float chip = Smooth(0.73f, 0.77f, Fbm(u, v, 12, 4, 17.7f));
            c = Mathf.Lerp(c, 0.72f, chip * 0.8f);                                  // soft chips (read as dirt on pale enamel if too dark)
            float h = 0.6f - chip * 0.12f + (Fbm(u, v, 24, 2, 8.1f) - 0.5f) * 0.04f;
            return new Px(Tint(c, 1f, 1f, 1f), h);
        }

        private static Px RondelPixel(float u, float v)
        {
            var terracotta = Color.Lerp(BuilderKit.Terracotta, BuilderKit.Navy, 0.22f);
            var brass = BuilderKit.Hex("#D3A24E");
            float x = u - 0.5f, y = v - 0.5f;
            float enamel = 0.92f + (Fbm(u, v, 4, 3, 31.7f) - 0.5f) * 0.12f;
            var col = terracotta * enamel;
            float h = 0.45f;
            float m = 0f;                                                        // brass coverage 0..1
            // edge lines top and bottom
            float edge = Mathf.Min(v, 1f - v);
            m = Mathf.Max(m, Smooth(0.075f, 0.06f, edge));
            // square frame
            float sq = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            m = Mathf.Max(m, Smooth(0.025f, 0.012f, Mathf.Abs(sq - 0.33f)));
            // rondel ring + boss
            float r = Mathf.Sqrt(x * x + y * y);
            m = Mathf.Max(m, Smooth(0.03f, 0.016f, Mathf.Abs(r - 0.2f)));
            float boss = Smooth(0.075f, 0.055f, r);
            m = Mathf.Max(m, boss);
            // brass: soft painted highlight toward the top-left, darker toward the bottom-right
            float shade = 0.85f + (0.5f - v) * -0.2f + (0.5f - u) * 0.1f;
            col = Color.Lerp(col, brass * shade, m);
            h += m * 0.4f + boss * 0.15f * (1f - r / 0.075f);
            col.a = 1f;
            return new Px(col, h);
        }

        private static Color Tint(float v, float r, float g, float b)
            => new(Mathf.Clamp01(v * r), Mathf.Clamp01(v * g), Mathf.Clamp01(v * b), 1f);

        // ------------------------------------------------------------------ tileable noise

        private static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>GLSL-style smoothstep (Unity's Mathf.SmoothStep is an interpolation, not an edge function).
        /// Works with edge0 &gt; edge1 too (falls instead of rises).</summary>
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Value noise on a lattice of <paramref name="period"/> cells per tile, so it wraps seamlessly.</summary>
        private static float ValueNoise(float u, float v, int period, float seed)
        {
            float x = u * period, y = v * period;
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float H(int a, int b) => Hash(((a % period) + period) % period, ((b % period) + period) % period, seed);
            float a0 = Mathf.Lerp(H(ix, iy), H(ix + 1, iy), fx);
            float a1 = Mathf.Lerp(H(ix, iy + 1), H(ix + 1, iy + 1), fx);
            return Mathf.Lerp(a0, a1, fy);
        }

        private static float Fbm(float u, float v, int period, int octaves, float seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u, v, period, seed + i * 7.31f) * amp;
                norm += amp;
                period *= 2;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        private static float Hash(int x, int y, float seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + (int)(seed * 1000f) * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // ------------------------------------------------------------------ asset helper

        private static Maps Get(string name, Func<float, float, Px> pixel, float normalStrength, float tiling)
        {
            string albedoPath = $"{Folder}/T_{name}_Albedo.png", normalPath = $"{Folder}/T_{name}_Normal.png";
            if (!File.Exists(albedoPath) || !File.Exists(normalPath))
            {
                Directory.CreateDirectory(Folder);
                var col = new Color[Size * Size];
                var hgt = new float[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        var p = pixel((x + 0.5f) / Size, (y + 0.5f) / Size);
                        col[y * Size + x] = p.C;
                        hgt[y * Size + x] = p.H;
                    }
                var nrm = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float hl = hgt[y * Size + (x + Size - 1) % Size], hr = hgt[y * Size + (x + 1) % Size];
                        float hd = hgt[((y + Size - 1) % Size) * Size + x], hu = hgt[((y + 1) % Size) * Size + x];
                        var n = new Vector3((hl - hr) * normalStrength * 4f, (hd - hu) * normalStrength * 4f, 1f).normalized;
                        nrm[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    }
                Write(albedoPath, col);
                Write(normalPath, nrm);
            }
            Configure(albedoPath, false);
            Configure(normalPath, true);
            return new Maps(AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath), AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath), tiling);
        }

        private static void Write(string path, Color[] px)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        private static void Configure(string path, bool normal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && importer.wrapMode == TextureWrapMode.Repeat && importer.maxTextureSize == Size) return;
            importer.textureType = type;
            importer.sRGBTexture = !normal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }
}
