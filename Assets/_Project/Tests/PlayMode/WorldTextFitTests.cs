using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Playtest bug: BG world text overflowed sticky notes and a corridor sign disappeared into the walls/arch.
    /// For every world-space text in every sector, in English and Bulgarian: the rendered text must fit its rect,
    /// must not be auto-sized below a readable size, and no corner of it may sit inside solid geometry.
    /// </summary>
    public class WorldTextFitTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        private static readonly Language[] Languages = { Language.English, Language.Bulgarian };

        /// <summary>Smallest world font size that is still readable from ~1 m (a fixed floor, plus 45 % of the design size).</summary>
        private const float AbsoluteMinFont = 0.3f;

        [UnityTest]
        public IEnumerator WorldTextFitsAndIsNotBuried(
            [ValueSource(nameof(Sectors))] string scene, [ValueSource(nameof(Languages))] Language language)
        {
            GameServices.StartNewRun();
            GameServices.Localization.SetLanguage(language, persist: false);
            yield return Phase6.Load(scene);
            GameServices.Dialogue.StopAll();

            // texts that only appear with power are inactive at load; switch them on for the check
            var texts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            yield return null;

            var solids = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.enabled && !c.isTrigger && (c is not MeshCollider mc || mc.convex)
                            && c.GetComponentInParent<CharacterController>() == null)
                .ToList();
            var problems = new List<string>();
            int checkedCount = 0;
            foreach (var t in texts)
            {
                if (!t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text)) continue;
                t.ForceMeshUpdate(true);
                checkedCount++;
                string id = $"{Path(t.transform)} \"{t.text}\"";
                var rect = ((RectTransform)t.transform).rect;
                var tb = t.textBounds;

                if (t.isTextOverflowing || tb.size.x > rect.width + 0.01f || tb.size.y > rect.height + 0.01f)
                    problems.Add($"overflows its {rect.width:0.00}x{rect.height:0.00} rect (text {tb.size.x:0.00}x{tb.size.y:0.00}): {id}");

                float design = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
                float minReadable = Mathf.Max(AbsoluteMinFont, design * 0.45f);
                if (t.enableAutoSizing && t.fontSize < minReadable - 0.001f)
                    problems.Add($"shrunk to {t.fontSize:0.00} (design {design:0.00}) — give it more room: {id}");

                // corners of the rendered text, nudged toward the reader (TMP is read looking along +Z). A corner must not
                // be inside solid geometry, and nothing may sit right in front of it (e.g. a hatch ring over a sign).
                // Colliders of the text's own parents (a switch, a key) are interaction volumes, not visible geometry.
                var toReader = -t.transform.forward;
                foreach (var local in Corners(tb))
                {
                    var p = t.transform.TransformPoint(local) + toReader * 0.01f;
                    foreach (var s in solids)
                    {
                        if (t.transform.IsChildOf(s.transform)) continue;
                        if ((s.ClosestPoint(p) - p).sqrMagnitude < 1e-8f)
                        {
                            problems.Add($"corner inside {Path(s.transform)}: {id}");
                            break;
                        }
                    }
                    foreach (var hit in Physics.RaycastAll(p - toReader * 0.008f, toReader, 0.2f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (t.transform.IsChildOf(hit.collider.transform) || hit.collider.GetComponentInParent<CharacterController>() != null) continue;
                        problems.Add($"covered by {Path(hit.collider.transform)} ({hit.distance:0.00} m in front): {id}");
                        break;
                    }
                }
            }
            GameServices.Localization.SetLanguage(Language.English, persist: false);
            Assert.Greater(checkedCount, 0, "No world text found — scene did not load?");
            Assert.IsEmpty(problems, string.Join("\n", problems.Distinct()));
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            yield return new Vector3(b.min.x, b.min.y, 0);
            yield return new Vector3(b.min.x, b.max.y, 0);
            yield return new Vector3(b.max.x, b.min.y, 0);
            yield return new Vector3(b.max.x, b.max.y, 0);
        }

        private static string Path(Transform t) => t.parent == null ? t.name : $"{Path(t.parent)}/{t.name}";
    }
}
