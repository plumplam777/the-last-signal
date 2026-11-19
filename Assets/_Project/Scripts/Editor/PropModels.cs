using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using M = LastSignal.EditorTools.StationMaterials;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Hero prop models made in Blender by <c>Tools/blender/build_props.py</c> (FBX in Art/Models/Props).
    /// Builders call <see cref="Place"/> to put a model where a greybox primitive used to be; the model's Blender
    /// material slots (Brass, Slate, Terracotta, …) are swapped for the shared station materials so the palette stays
    /// in one place. If a model is missing, <see cref="Place"/> returns null and the builder keeps its greybox.
    /// </summary>
    public static class PropModels
    {
        public const string Folder = "Assets/_Project/Art/Models/Props";

        public static string PathOf(string id) => $"{Folder}/{id}.fbx";

        public static bool Exists(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(id)) != null;

        /// <summary>Instantiates the model under <paramref name="parent"/> (pivot = floor centre, front = local −Z unless noted).</summary>
        public static GameObject Place(string id, Transform parent, Vector3 localPos, Quaternion? localRot = null,
            IDictionary<string, Material> overrides = null, string name = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(id));
            if (asset == null)
            {
                Debug.LogWarning($"[PropModels] {PathOf(id)} missing — keeping greybox. Run Tools/blender/build_props.py.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name ?? $"Model_{id}";
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => Remap(m, overrides)).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic);
            }
            return go;
        }

        private static Material _steel;

        private static Material Remap(Material m, IDictionary<string, Material> overrides)
        {
            if (m == null) return M.SlateDark;
            string key = m.name.Split('.', ' ')[0];
            if (overrides != null && overrides.TryGetValue(key, out var o)) return o;
            switch (key)
            {
                case "Brass": return M.Brass;
                case "Slate": return M.SlateDark;
                case "Wall": return M.Wall;
                case "Terracotta": return M.Terracotta;
                case "Ceramic": return M.Ceramic;
                case "Rubber": return M.Rubber;
                case "Glass": return M.GlassTeal;
                case "Cyan": return M.CyanStrip;
                case "Amber": return M.AmberLamp;
                case "Red": return M.RedLamp;
                case "Steel":
                    if (_steel == null) _steel = BuilderKit.Lit("M_Steel_Brushed", Color.Lerp(BuilderKit.MetalGrey, BuilderKit.Navy, 0.35f), 0.5f, 0.8f);
                    return _steel;
                default:
                    Debug.LogWarning($"[PropModels] Unknown material slot '{m.name}' — using slate.");
                    return M.SlateDark;
            }
        }

        [MenuItem("Last Signal/Diagnostics/Report Prop Models")]
        public static void Report()
        {
            var sb = new StringBuilder("[PropModels] ");
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var b = new Bounds();
                bool first = true;
                int tris = 0;
                var slots = new HashSet<string>();
                Vector3 maxX = Vector3.zero, minZ = Vector3.zero;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = mf.sharedMesh;
                    tris += mesh.triangles.Length / 3;
                    foreach (var v in mesh.vertices)
                    {
                        var w = mf.transform.TransformPoint(v); // asset space (root at origin)
                        if (first) { b = new Bounds(w, Vector3.zero); maxX = minZ = w; first = false; }
                        b.Encapsulate(w);
                        if (w.x > maxX.x) maxX = w;
                        if (w.z < minZ.z) minZ = w;
                    }
                    foreach (var m in mf.GetComponent<MeshRenderer>().sharedMaterials) slots.Add(m != null ? m.name : "null");
                }
                sb.Append($"\n{System.IO.Path.GetFileNameWithoutExtension(path)}: center {b.center:F2} size {b.size:F2} tris {tris} " +
                          $"maxX@{maxX:F2} minZ@{minZ:F2} slots [{string.Join(",", slots)}]");
            }
            Debug.Log(sb.ToString());
        }
    }

    /// <summary>Import settings for the Blender props: metres, no cameras/lights/animation, readable for colliders.</summary>
    public sealed class PropModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(PropModels.Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importNormals = ModelImporterNormals.Import;
        }
    }
}
