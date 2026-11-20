using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Pink means a missing/broken shader. Every renderer (props, world text, particles) must have materials whose
    /// shader exists and is supported, so nothing shows up magenta in a build.
    /// </summary>
    public class MaterialSanityTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        [UnityTest]
        public IEnumerator NoRendererIsPink([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);
            GameServices.Dialogue.StopAll();

            var problems = new List<string>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { problems.Add($"{Path(r.transform)}: missing material"); continue; }
                    if (m.shader == null || !m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader")
                        problems.Add($"{Path(r.transform)}: material '{m.name}' has broken shader '{m.shader?.name}'");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems.Distinct()));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : $"{Path(t.parent)}/{t.name}";
    }
}
