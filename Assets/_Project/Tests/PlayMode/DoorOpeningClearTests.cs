using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using LastSignal.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Art-pass guard (playtest bug: a wall band ran straight across the open Docking → Engineering door).
    /// Nothing except the door's own panels may have a renderer or solid collider inside a sliding door's opening.
    /// The opening is the closed panels' bounds, shrunk a little so frames/pillars touching the edge are fine.
    /// </summary>
    public class DoorOpeningClearTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        [UnityTest]
        public IEnumerator NothingBlocksADoorOpening([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);
            GameServices.Dialogue.StopAll();

            var doors = Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            var problems = new List<string>();
            foreach (var door in doors)
            {
                var panelRenderers = door.GetComponentsInChildren<Renderer>()
                    .Where(r => r.transform.parent != null && r.transform.parent.name.StartsWith("Panel_")).ToList();
                if (panelRenderers.Count == 0) continue;
                var b = panelRenderers[0].bounds;
                foreach (var r in panelRenderers) b.Encapsulate(r.bounds);

                // inner 80 % of the width, 0.3 m clear of floor and lintel, 0.5 m in front of and behind the door
                bool alongX = b.size.x >= b.size.z;
                var size = new Vector3(
                    alongX ? b.size.x * 0.8f : 1f,
                    b.size.y - 0.6f,
                    alongX ? 1f : b.size.z * 0.8f);
                var opening = new Bounds(b.center, size);

                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(door.transform)) continue;
                    if (r is ParticleSystemRenderer || r.GetComponentInParent<CharacterController>() != null) continue;
                    if (r.bounds.Intersects(opening)) problems.Add($"{door.name}: renderer {Path(r.transform)}");
                }
                foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    if (!c.enabled || c.isTrigger || c.transform.IsChildOf(door.transform)) continue;
                    if (c is CharacterController || c.GetComponentInParent<CharacterController>() != null) continue;
                    if (c.bounds.Intersects(opening)) problems.Add($"{door.name}: collider {Path(c.transform)}");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems.Distinct()));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : $"{Path(t.parent)}/{t.name}";
    }
}
