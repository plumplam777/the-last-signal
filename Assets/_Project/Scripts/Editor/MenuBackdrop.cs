using System.IO;
using LastSignal.Interaction;
using LastSignal.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// The start-screen backdrop: a still, cinematic space shot. Nereth hangs large in the upper right (its night side
    /// with the glowing Murmur cracks, rim-lit), a bright star flares upper-centre with long soft rays, and the icy
    /// limb of Nereth's moon curves across the bottom under a band of haze. The camera only drifts a little.
    /// Everything is unlit/additive quads + 2 spheres + 1 unshadowed directional light: cheap on a laptop.
    /// Textures are generated once into Art/Textures/Menu (delete a PNG to regenerate).
    /// </summary>
    public static class MenuBackdrop
    {
        private const string Folder = "Assets/_Project/Art/Textures/Menu";
        private const string MatFolder = "Assets/_Project/Art/Materials";
        public const string VolumePath = "Assets/_Project/Settings/Volume_Menu.asset";

        // layout (camera at the origin looking +Z, vertical FOV 40)
        private static readonly Vector3 NerethCenter = new(330f, 255f, 1100f);
        private const float NerethRadius = 300f;
        private static readonly Vector3 SunPos = new(60f, 190f, 3000f); // just off Nereth's lower-left limb, clear of the logo
        // a big moon right under the camera (250 m up): its horizon dips ~9 degrees, 3.2 km away
        private const float MoonRadius = 20000f;
        private static readonly Vector3 MoonCenter = new(0f, -MoonRadius - 250f, 0f);

        public static Camera Build(Transform root)
        {
            var b = Group("Backdrop", root).transform;

            var camGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            camGo.transform.SetParent(root, false);
            camGo.transform.localRotation = Quaternion.Euler(-2f, 0f, -2.5f); // a slight roll tilts the horizon
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.02f, 0.035f);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 6000f;
            cam.fieldOfView = 40f;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<MenuCameraDrift>();

            CutsceneBuilder.Stars(b, Vector3.zero, 3200f, 1800);
            Quad("Starfield", b, new Vector3(0, 0, 5200f), new Vector2(7600f, 4300f), SkyArt.Starfield(), 0f);

            // Nereth: the game's planet, slowly turning
            var nereth = Sphere("Nereth", b, NerethCenter, Vector3.one * NerethRadius * 2f, MenuNereth(), false, false);
            nereth.transform.localRotation = Quaternion.Euler(18f, 40f, 12f);
            nereth.AddComponent<FlagSpinner>().EditorSet(null, Vector3.up, 0.6f);
            MakeDynamic(nereth);
            nereth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // thin atmosphere halo behind the planet (rim glow)
            Billboard("NerethHalo", b, NerethCenter * 1.065f, NerethRadius * 2.5f * 1.065f, Additive("Halo", 512, HaloPixel, new Color(0.55f, 0.72f, 0.95f, 0.55f)));

            // the icy moon horizon in the foreground
            var moon = new GameObject("MoonHorizon", typeof(MeshFilter), typeof(MeshRenderer));
            moon.transform.SetParent(b, false);
            moon.transform.localPosition = MoonCenter;
            moon.GetComponent<MeshFilter>().sharedMesh = HorizonCap();
            var moonR = moon.GetComponent<MeshRenderer>();
            moonR.sharedMaterial = MoonIce();
            moonR.shadowCastingMode = ShadowCastingMode.Off;
            var hazeMat = Additive("Haze", 256, HazePixel, new Color(0.75f, 0.85f, 1f, 0.55f));
            Quad("HorizonHaze", b, new Vector3(0f, -470f, 3000f), new Vector2(9000f, 520f), hazeMat, 0f);
            Quad("HorizonHazeNear", b, new Vector3(-400f, -330f, 2000f), new Vector2(5000f, 260f), hazeMat, 0f, 0.5f);

            // the star: core glow, a horizontal streak and long rays falling towards the lower left
            var glow = Additive("Glow", 256, GlowPixel, new Color(1f, 1f, 1f, 1f));
            Billboard("SunGlow", b, SunPos, 520f, glow);
            Billboard("SunCore", b, SunPos + new Vector3(0, 0, -5f), 140f, glow, 2.2f);
            Quad("SunStreak", b, SunPos + new Vector3(0, 0, -8f), new Vector2(1000f, 40f), Additive("Streak", 256, StreakPixel, new Color(0.8f, 0.9f, 1f, 0.7f)), -4f);
            var rays = Additive("Ray", 128, RayPixel, new Color(0.7f, 0.82f, 1f, 0.07f));
            float[] angles = { -72f, -60f, -49f, -37f, -24f, -12f }; // 0 = straight down, negative = towards the lower left
            float[] widths = { 70f, 140f, 55f, 180f, 90f, 120f };
            for (int i = 0; i < angles.Length; i++)
            {
                var ray = Quad($"Ray_{i}", b, SunPos + new Vector3(0, 0, -10f - i), new Vector2(widths[i] * 2f, 4200f), rays, 0f);
                // pivot at the sun: shift the quad half its length along its own down axis, then rotate around Z
                ray.transform.localRotation = Quaternion.Euler(0, 0, angles[i]);
                ray.transform.localPosition = SunPos + new Vector3(0, 0, -10f - i) + ray.transform.localRotation * Vector3.down * 2100f;
            }

            // light: the star, grazing from behind, no shadows
            var sun = new GameObject("StarLight").AddComponent<Light>();
            sun.transform.SetParent(b, false);
            sun.type = LightType.Directional;
            sun.color = new Color(0.86f, 0.92f, 1f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.LookRotation((NerethCenter - SunPos).normalized + Vector3.down * 0.25f);
            // faint cool fill from behind the camera so the planet's face and the ice read (the reference is grey-lit)
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(b, false);
            fill.type = LightType.Directional;
            fill.color = new Color(0.62f, 0.72f, 0.9f);
            fill.intensity = 0.6f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, -18f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.09f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            var volume = Group("GlobalVolume", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = Profile();
            return cam;
        }

        // ------------------------------------------------------------------ objects

        private static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, float roll, float alpha = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 0, roll);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (alpha < 1f)
            {
                var block = new MaterialPropertyBlock();
                var c = mat.GetColor("_BaseColor");
                block.SetColor("_BaseColor", new Color(c.r, c.g, c.b, c.a * alpha));
                r.SetPropertyBlock(block);
            }
            return go;
        }

        /// <summary>A quad facing the camera at the origin.</summary>
        private static GameObject Billboard(string name, Transform parent, Vector3 pos, float size, Material mat, float brightness = 1f)
        {
            var go = Quad(name, parent, pos, new Vector2(size, size), mat, 0f);
            go.transform.localRotation = Quaternion.LookRotation(pos.normalized);
            if (brightness != 1f)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", mat.GetColor("_BaseColor") * brightness);
                go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            }
            return go;
        }

        // ------------------------------------------------------------------ materials

        private static Material Additive(string name, int size, System.Func<float, float, float> alpha, Color tint)
        {
            string path = $"{MatFolder}/M_Menu_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AlphaTexture($"T_Menu_{name}", size, alpha));
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A copy of Nereth's material with the cracks glowing softly (the window version is brighter).</summary>
        private static Material MenuNereth()
        {
            const string path = MatFolder + "/M_Menu_Nereth.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(SkyArt.Nereth());
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.CopyPropertiesFromMaterial(SkyArt.Nereth());
            mat.SetColor("_BaseColor", new Color(1.45f, 1.5f, 1.55f)); // the shared albedo is dark night-side ice: lift it
            mat.SetColor("_EmissionColor", Hex("#2F8FD8") * 0.14f);
            mat.SetFloat("_Smoothness", 0.12f); // no hot specular spot that reads as a second sun
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Smooth spherical cap (the top 11.5 degrees of a sphere of MoonRadius): a Unity sphere is too faceted at this size.</summary>
        private static Mesh HorizonCap()
        {
            const string path = "Assets/_Project/Art/Meshes/Menu_HorizonCap.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            const int rings = 64, segs = 160;
            const float maxTheta = 11.5f * Mathf.Deg2Rad;
            var verts = new Vector3[(rings + 1) * (segs + 1)];
            var normals = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i <= rings; i++)
            {
                float th = maxTheta * Mathf.Pow((float)i / rings, 1.3f);
                for (int j = 0; j <= segs; j++)
                {
                    float ph = (float)j / segs * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    int k = i * (segs + 1) + j;
                    verts[k] = n * MoonRadius;
                    normals[k] = n;
                    uvs[k] = new Vector2(n.x, n.z) * 120f; // planar UVs: no pinching at the pole
                }
            }
            var tris = new int[rings * segs * 6];
            int t = 0;
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int a = i * (segs + 1) + j, b = a + segs + 1;
                    tris[t++] = a; tris[t++] = a + 1; tris[t++] = b;
                    tris[t++] = a + 1; tris[t++] = b + 1; tris[t++] = b;
                }
            mesh = new Mesh { name = "Menu_HorizonCap", vertices = verts, normals = normals, uv = uvs, triangles = tris };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material MoonIce()
        {
            var mat = Lit("M_Menu_MoonIce", new Color(0.92f, 0.95f, 1f), 0.45f, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", IceTexture());
            mat.SetColor("_EmissionColor", new Color(0.3f, 0.34f, 0.4f)); // snow glow: the limb stays bright on the night side
            mat.SetTexture("_BaseMap", IceTexture());
            mat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);
            }
            foreach (var c in profile.components.ToArray())
            {
                profile.Remove(c.GetType());
                Object.DestroyImmediate(c, true);
            }
            var bloom = DockingBayBuilder.AddComponent<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.78f);
            bloom.tint.Override(new Color(0.85f, 0.92f, 1f));
            DockingBayBuilder.AddComponent<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
            var grade = DockingBayBuilder.AddComponent<ColorAdjustments>(profile);
            grade.postExposure.Override(0.35f);
            grade.contrast.Override(14f);
            grade.saturation.Override(-35f); // the cold, nearly monochrome blue of the reference
            grade.colorFilter.Override(new Color(0.88f, 0.94f, 1f));
            var vignette = DockingBayBuilder.AddComponent<Vignette>(profile);
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.01f, 0.02f, 0.04f));
            var grain = DockingBayBuilder.AddComponent<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.15f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ------------------------------------------------------------------ textures

        private static float GlowPixel(float u, float v)
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            float soft = Mathf.Pow(Mathf.Clamp01(1f - r), 3f);
            float core = Mathf.Pow(Mathf.Clamp01(1f - r * 3.5f), 2f);
            return Mathf.Clamp01(soft * 0.8f + core);
        }

        private static float HaloPixel(float u, float v)
        {
            // a ring just outside the planet disc (disc radius = 0.4 of the quad), fading outwards
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            float d = r - 0.8f;
            return d < 0 ? Mathf.Clamp01(1f + d * 25f) * 0.6f : Mathf.Exp(-d * d * 180f);
        }

        private static float StreakPixel(float u, float v)
        {
            float dy = (v - 0.5f) * 2f, dx = Mathf.Abs(u - 0.5f) * 2f;
            return Mathf.Exp(-dy * dy * 30f) * Mathf.Pow(Mathf.Clamp01(1f - dx), 2.5f);
        }

        private static float RayPixel(float u, float v)
        {
            // across: soft gaussian; along (v = 1 at the sun): long fade
            float across = Mathf.Exp(-Mathf.Pow((u - 0.5f) * 2f, 2f) * 4f);
            return across * Mathf.Pow(v, 1.6f);
        }

        private static float HazePixel(float u, float v)
        {
            float dy = (v - 0.45f) * 2f, dx = Mathf.Abs(u - 0.5f) * 2f;
            return Mathf.Exp(-dy * dy * 5f) * Mathf.Clamp01(1.2f - dx * dx);
        }

        private static Texture2D AlphaTexture(string name, int size, System.Func<float, float, float> alpha)
        {
            string path = $"{Folder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(Folder);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / size, (y + 0.5f) / size));
            t.SetPixels(px);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Wind-scoured ice: bright drifts, darker crevasse streaks (tileable Perlin).</summary>
        private static Texture2D IceTexture()
        {
            const string path = Folder + "/T_Menu_Ice.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(Folder);
            const int n = 512;
            var t = new Texture2D(n, n, TextureFormat.RGB24, true);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (float)x / n, v = (float)y / n;
                    float a = Tile(u, v, 4f, 0f), b2 = Tile(u, v, 12f, 3.1f), c = Tile(u, v, 32f, 7.7f);
                    float streak = Mathf.Abs(Tile(u * 0.5f, v * 3f, 6f, 11f) - 0.5f) * 2f;
                    float h = a * 0.55f + b2 * 0.3f + c * 0.15f;
                    float k = Mathf.Lerp(0.55f, 1f, h) * Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(streak * 1.6f));
                    px[y * n + x] = new Color(k * 0.93f, k * 0.97f, k, 1f);
                }
            t.SetPixels(px);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Seamless Perlin noise on the unit square (blends four shifted copies).</summary>
        private static float Tile(float u, float v, float f, float seed)
        {
            float N(float x, float y) => Mathf.PerlinNoise(x * f + seed * 13.1f, y * f + seed * 7.3f);
            float a = N(u, v), b = N(u - 1f, v), c = N(u, v - 1f), d = N(u - 1f, v - 1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }
    }
}
