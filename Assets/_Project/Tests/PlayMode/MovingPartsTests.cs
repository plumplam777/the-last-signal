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
    /// Static batching freezes static-flagged objects in a build, so everything that moves at runtime (spinners such as
    /// the lab armillary and reactor gyro rings, sliding door panels) must not be static. In the editor it would still
    /// appear to move, so only this test catches it.
    /// </summary>
    public class MovingPartsTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        [UnityTest]
        public IEnumerator MovingPartsAreNotStatic([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);

            var moving = new List<Transform>();
            moving.AddRange(Object.FindObjectsByType<FlagSpinner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(s => s.transform));
            foreach (var door in Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                moving.AddRange(door.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Panel_")));

            var problems = moving.SelectMany(m => m.GetComponentsInChildren<Transform>(true))
                .Where(t => t.gameObject.isStatic)
                .Select(t => Path(t)).Distinct().ToList();
            Assert.IsEmpty(problems, "static objects under moving parts:\n" + string.Join("\n", problems.Take(20)));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : $"{Path(t.parent)}/{t.name}";
    }
}
