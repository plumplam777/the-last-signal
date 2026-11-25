using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Playtest guard (§4.5 item 3, "floating props next to desks"): every placed Blender prop (Model_* / Clutter_*)
    /// and every loose item the player can pick up (pickups, audio-log recorders) must rest on something or be
    /// attached to something. A prop is fine when the top of another mesh sits right
    /// under the centre of its footprint (floor, desk, shelf), or when its bounds touch another mesh from the side
    /// (wall-mounted valves, sconces). Axis-aligned bounds are coarse, but a mug hovering 10 cm over a desk fails.
    /// </summary>
    public class PropRestingTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        private const float MaxGap = 0.03f;

        [UnityTest]
        public IEnumerator EveryPropRestsOnSomething([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);
            GameServices.Dialogue.StopAll();

            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.sqrMagnitude > 1e-6f)
                .ToList();
            static bool IsProp(Transform t) => t.name.StartsWith("Model_") || t.name.StartsWith("Clutter_")
                                               || t.GetComponent<PickupInteractable>() != null || t.GetComponent<AudioLogPickup>() != null;
            var props = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.activeInHierarchy && IsProp(t))
                .Where(t => !t.GetComponentsInParent<Transform>().Skip(1).Any(IsProp))
                .ToList();
            Assert.IsNotEmpty(props, $"no props found in {scene}");

            var problems = new List<string>();
            foreach (var prop in props)
            {
                var own = prop.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToList();
                if (own.Count == 0) continue;
                var b = own[0].bounds;
                foreach (var r in own.Skip(1)) b.Encapsulate(r.bounds);
                var others = renderers.Where(r => !r.transform.IsChildOf(prop) && !prop.IsChildOf(r.transform)).ToList();

                // resting: the highest surface under the footprint centre that is not above the prop's bottom
                float bottom = b.min.y, support = float.NegativeInfinity;
                var c = b.center;
                foreach (var r in others)
                {
                    var o = r.bounds;
                    if (c.x < o.min.x || c.x > o.max.x || c.z < o.min.z || c.z > o.max.z) continue;
                    if (o.max.y <= bottom + MaxGap) support = Mathf.Max(support, o.max.y);
                }
                float gap = bottom - support;
                if (gap <= MaxGap) continue;

                // attached: within 1.5 cm of a large mesh (wall, pipe run, column) from the side, or actually
                // interpenetrating a small one (a valve wheel on its valve body), overlapping in height.
                // A mug hovering next to a monitor does not count.
                var near = b;
                near.Expand(0.03f); // Expand grows the size: 1.5 cm per side
                var touching = b;
                touching.Expand(-0.01f);
                bool attached = others.Any(r =>
                {
                    var o = r.bounds;
                    if (o.max.y <= bottom + MaxGap || o.min.y >= b.max.y) return false;
                    return Mathf.Max(o.size.x, o.size.y, o.size.z) > 0.8f ? near.Intersects(o) : touching.Intersects(o);
                });
                if (attached) continue;
                problems.Add($"{Path(prop)} floats {gap:0.00} m above the nearest surface (bottom y {bottom:0.00}, at {c.x:0.00},{c.z:0.00})");
            }
            Assert.IsEmpty(problems, $"{scene}: {problems.Count} floating props:\n" + string.Join("\n", problems));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
