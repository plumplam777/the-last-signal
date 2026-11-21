using System.IO;
using LastSignal.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Shared helpers for code-built greybox content: palette materials (Documentation/ArtDirection.md),
    /// primitives, rings/arches, lights and UI elements.
    /// </summary>
    public static class BuilderKit
    {
        public const string MaterialsFolder = "Assets/_Project/Art/Materials";

        // Palette — keep in sync with Documentation/ArtDirection.md
        public static readonly Color Navy = Hex("#141B26");
        public static readonly Color Slate = Hex("#2A313B");
        public static readonly Color MetalGrey = Hex("#8A8F93");
        public static readonly Color Brass = Hex("#C9A14A");
        public static readonly Color Teal = Hex("#3FB8B0");
        public static readonly Color TealLight = Hex("#7FE3D8");
        public static readonly Color Terracotta = Hex("#B8664A");
        public static readonly Color SignalCyan = Hex("#5FE6FF");
        public static readonly Color Amber = Hex("#FFB36B");
        public static readonly Color EmergencyRed = Hex("#E0453A");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ---------- Materials ----------

        public static Material Lit(string name, Color color, float smoothness = 0.25f, float metallic = 0f)
        {
            var mat = LoadOrCreate(name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Emissive(string name, Color color, float intensity, Color? baseColor = null)
        {
            var mat = LoadOrCreate(name);
            mat.SetColor("_BaseColor", baseColor ?? color * 0.3f);
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Glass(string name, Color color, float alpha, float emission)
        {
            var mat = LoadOrCreate(name);
            // URP Lit transparent setup
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            var c = color;
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Smoothness", 0.92f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreate(string name)
        {
            Directory.CreateDirectory(MaterialsFolder);
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------- Geometry ----------

        public static GameObject Group(string name, Transform parent, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
            bool collider = true, Quaternion? rot = null, bool isStatic = true)
            => Prim(PrimitiveType.Cube, name, parent, pos, size, mat, collider, rot, isStatic);

        /// <summary>Open-fronted box (front = local -Z): back, sides, top and bottom panels, so things inside are visible and reachable.</summary>
        public static GameObject HollowCase(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float wall = 0.05f)
        {
            var g = Group(name, parent, center).transform;
            Vector3 h = size * 0.5f;
            Box("Back", g, new Vector3(0, 0, h.z - wall * 0.5f), new Vector3(size.x, size.y, wall), mat);
            Box("Side_L", g, new Vector3(-h.x + wall * 0.5f, 0, 0), new Vector3(wall, size.y, size.z), mat);
            Box("Side_R", g, new Vector3(h.x - wall * 0.5f, 0, 0), new Vector3(wall, size.y, size.z), mat);
            Box("Top", g, new Vector3(0, h.y - wall * 0.5f, 0), new Vector3(size.x, wall, size.z), mat);
            Box("Bottom", g, new Vector3(0, -h.y + wall * 0.5f, 0), new Vector3(size.x, wall, size.z), mat);
            return g.gameObject;
        }

        /// <summary>Removes the renderers under <paramref name="go"/> (keeps colliders) when a model replaces the greybox look.</summary>
        public static GameObject HideRenderers(GameObject go, bool hide = true)
        {
            if (!hide) return go;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                Object.DestroyImmediate(r);
                if (mf != null) Object.DestroyImmediate(mf);
            }
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
            bool collider = true, Quaternion? rot = null, bool isStatic = true)
            => Prim(PrimitiveType.Cylinder, name, parent, pos, size, mat, collider, rot, isStatic);

        public static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
            bool collider = false, bool isStatic = true)
            => Prim(PrimitiveType.Sphere, name, parent, pos, size, mat, collider, null, isStatic);

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size,
            Material mat, bool collider, Quaternion? rot, bool isStatic)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (type == PrimitiveType.Cube) go.GetComponent<MeshFilter>().sharedMesh = MetricBox(size);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        private const string BoxMeshFolder = "Assets/_Project/Art/Meshes/MetricBoxes";
        private static readonly System.Collections.Generic.Dictionary<string, Mesh> BoxMeshes = new();

        /// <summary>
        /// Unit cube (same as Unity's, so localScale = size still works) whose UVs are in metres of the scaled box:
        /// a 16 m wall repeats its texture 16 x per metre-tiling instead of stretching one copy. Cached per size (mm).
        /// Side faces: u = horizontal, v = up; top/bottom: u = x, v = z. UVs start at 0 on the box's min edge.
        /// </summary>
        public static Mesh MetricBox(Vector3 size)
        {
            int sx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.x) * 1000f));
            int sy = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.y) * 1000f));
            int sz = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.z) * 1000f));
            string key = $"Box_{sx}x{sy}x{sz}";
            if (BoxMeshes.TryGetValue(key, out var cached) && cached != null) return cached;
            string path = $"{BoxMeshFolder}/{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                Directory.CreateDirectory(BoxMeshFolder);
                mesh = BuildMetricBox(new Vector3(sx, sy, sz) / 1000f);
                mesh.name = key;
                AssetDatabase.CreateAsset(mesh, path);
            }
            BoxMeshes[key] = mesh;
            return mesh;
        }

        private static Mesh BuildMetricBox(Vector3 s)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var norms = new System.Collections.Generic.List<Vector3>();
            var tans = new System.Collections.Generic.List<Vector4>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            // (normal, u axis, v axis)
            var faces = new (Vector3 n, Vector3 u, Vector3 v)[]
            {
                (Vector3.forward, Vector3.left, Vector3.up), (Vector3.back, Vector3.right, Vector3.up),
                (Vector3.right, Vector3.forward, Vector3.up), (Vector3.left, Vector3.back, Vector3.up),
                (Vector3.up, Vector3.right, Vector3.forward), (Vector3.down, Vector3.right, Vector3.back),
            };
            foreach (var (n, ua, va) in faces)
            {
                int start = verts.Count;
                float su = Mathf.Abs(Vector3.Dot(ua, s)), sv = Mathf.Abs(Vector3.Dot(va, s));
                for (int i = 0; i < 4; i++)
                {
                    float a = (i == 1 || i == 2) ? 0.5f : -0.5f, b = i >= 2 ? 0.5f : -0.5f;
                    verts.Add(n * 0.5f + ua * a + va * b);
                    norms.Add(n);
                    tans.Add(new Vector4(ua.x, ua.y, ua.z, -1f)); // cross(n, u) = -v for these faces
                    uvs.Add(new Vector2((a + 0.5f) * su, (b + 0.5f) * sv));
                }
                tris.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTangents(tans);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            // a second UV set for lightmaps/probes is not needed (realtime lighting only)
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Ring (or arc) of box segments in the local XY plane, facing Z. Angles in degrees, 0 = +X, CCW.</summary>
        public static GameObject Ring(string name, Transform parent, Vector3 center, float radius, float thickness,
            float depth, int segments, Material mat, float startDeg = 0f, float endDeg = 360f, bool collider = false)
        {
            var root = Group(name, parent, center);
            float span = endDeg - startDeg;
            float step = span / segments;
            float segLen = 2f * Mathf.PI * radius * (step / 360f) * 1.08f;
            for (int i = 0; i < segments; i++)
            {
                float a = (startDeg + step * (i + 0.5f)) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                var rot = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                Box($"Seg{i:00}", root.transform, pos, new Vector3(segLen, thickness, depth), mat, collider, rot);
            }
            return root;
        }

        public static Light PointLight(string name, Transform parent, Vector3 pos, Color color, float intensity,
            float range, LightShadows shadows = LightShadows.None)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows;
            return l;
        }

        public static Light SpotLight(string name, Transform parent, Vector3 pos, Quaternion rot, Color color,
            float intensity, float range, float angle, LightShadows shadows = LightShadows.Soft)
        {
            var l = PointLight(name, parent, pos, color, intensity, range, shadows);
            l.type = LightType.Spot;
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.55f;
            l.transform.localRotation = rot;
            return l;
        }

        public static TextMeshPro WorldText(string name, Transform parent, Vector3 pos, Quaternion rot, string key,
            float size, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var t = go.GetComponent<TextMeshPro>();
            t.text = key;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.characterSpacing = 8f;
            // auto-size: the design size is the maximum; longer translations (BG) shrink to fit their rect instead
            // of spilling over notes, plates and arches. Callers set sizeDelta to the space actually available.
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.35f;
            t.textWrappingMode = TextWrappingModes.Normal;
            ((RectTransform)go.transform).sizeDelta = new Vector2(6f, 1.2f);
            SetLocalizedKey(go.AddComponent<LocalizedText>(), key);
            return t;
        }

        /// <summary>World text that is not localized (numbers, symbol rows).</summary>
        public static TextMeshPro RawText(string name, Transform parent, Vector3 pos, Quaternion rot, string text, float size, Color color)
        {
            var t = WorldText(name, parent, pos, rot, text, size, color);
            Object.DestroyImmediate(t.GetComponent<LocalizedText>());
            t.text = text;
            return t;
        }

        // ---------- UI ----------

        public static void SetLocalizedKey(LocalizedText lt, string key)
        {
            var so = new SerializedObject(lt);
            so.FindProperty("key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static RectTransform UiRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static TextMeshProUGUI UiText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            float fontSize, Color color, string localizationKey = null)
        {
            var r = UiRect(name, parent, anchor, pos, size);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.text = localizationKey ?? string.Empty;
            if (localizationKey != null) SetLocalizedKey(r.gameObject.AddComponent<LocalizedText>(), localizationKey);
            return t;
        }

        public static Image UiImage(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var r = UiRect(name, parent, anchor, pos, size);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Button UiButton(string name, Transform parent, Vector2 pos, string key)
        {
            var img = UiImage(name, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(380, 64), new Color(0.08f, 0.11f, 0.16f, 1f));
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.6f, 0.95f, 1f);
            colors.selectedColor = new Color(0.6f, 0.95f, 1f);
            button.colors = colors;
            var label = UiText("Label", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 60), 28, new Color(0.85f, 0.95f, 1f), key);
            label.raycastTarget = false;
            return button;
        }

        public static Canvas UiCanvas(string name, Transform parent, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }
}
