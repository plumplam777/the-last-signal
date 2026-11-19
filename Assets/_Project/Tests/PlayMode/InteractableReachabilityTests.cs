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
    /// Art-pass guard: the player's interaction ray must be able to reach every interactable. Most tests call
    /// Interact() directly, so a prop collider swallowing a recorder or switch would go unnoticed. Fails when the
    /// centre of an interactable's collider lies inside a solid collider that belongs to something else.
    /// </summary>
    public class InteractableReachabilityTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        [UnityTest]
        public IEnumerator NoInteractableIsBuriedInsideAnotherCollider([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);
            GameServices.Dialogue.StopAll();

            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            var solids = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.enabled && !c.isTrigger && c.gameObject.layer != ignore && (c is not MeshCollider mc || mc.convex))
                .ToList();
            var problems = new List<string>();
            int checkedCount = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is not IInteractable || !mb.isActiveAndEnabled) continue;
                foreach (var own in mb.GetComponentsInChildren<Collider>(false).Where(c => c.enabled && !c.isTrigger))
                {
                    if (own.GetComponentInParent<IInteractable>() as MonoBehaviour != mb) continue;
                    checkedCount++;
                    Vector3 c = own.bounds.center;
                    foreach (var other in solids)
                    {
                        if (other == own || other.transform.IsChildOf(mb.transform)) continue;
                        if (other.GetComponentInParent<IInteractable>() is MonoBehaviour owner && owner == mb) continue;
                        if ((other.ClosestPoint(c) - c).sqrMagnitude < 1e-8f)
                            problems.Add($"{Path(mb.transform)} is inside {Path(other.transform)}");
                    }
                }
            }
            Assert.Greater(checkedCount, 3, "No interactables found — scene did not load?");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : $"{Path(t.parent)}/{t.name}";
    }
}
