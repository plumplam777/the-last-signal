using System.Collections;
using System.Linq;
using LastSignal.Core;
using LastSignal.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// The Docking Bay opens in near-darkness: the flashlight is found right by the spawn, the crew's fluorescent
    /// messages show only in its beam, and restoring power brings the room's light back.
    /// </summary>
    public class DarkDockingTests
    {
        [UnityTest]
        public IEnumerator FlashlightWaitsNextToTheSpawn()
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(SceneIds.DockingBay);
            var spawn = GameObject.Find("Spawn_Default");
            var pickup = GameObject.Find("Pickup_Flashlight");
            Assert.IsNotNull(spawn, "Spawn_Default missing");
            Assert.IsNotNull(pickup, "Pickup_Flashlight missing");
            float d = Vector3.Distance(Flat(spawn.transform.position), Flat(pickup.transform.position));
            Assert.Less(d, 2.5f, $"the flashlight is {d:0.0} m from the spawn; it must be within reach in the dark");
        }

        [UnityTest]
        public IEnumerator UnpoweredRoomIsDark_PowerBringsTheLightBack()
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(SceneIds.DockingBay);
            yield return null;
            float dark = RenderSettings.ambientSkyColor.maxColorComponent;

            GameServices.World.SetBool(WorldKeys.Power(SectorIds.Docking), true);
            yield return new WaitForSeconds(2.2f);
            float lit = RenderSettings.ambientSkyColor.maxColorComponent;
            Assert.Greater(lit, dark * 4f, $"ambient should rise a lot with power (dark {dark:0.000}, lit {lit:0.000})");
        }

        [UnityTest]
        public IEnumerator HiddenMessagesShowOnlyInTheBeam()
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(SceneIds.DockingBay);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var flashlight = FlashlightController.Current;
            Assert.IsNotNull(player);
            Assert.IsNotNull(flashlight);
            GameServices.World.SetBool(FlashlightController.OwnedFlag, true);

            var messages = Object.FindObjectsByType<BeamRevealText>(FindObjectsSortMode.InstanceID);
            Assert.GreaterOrEqual(messages.Length, 2, "expected the crew's hidden messages in the Docking Bay");
            foreach (var m in messages)
            {
                var t = m.transform;
                var stand = t.position - t.forward * 2.2f;
                stand.y = 0.05f;
                var look = t.position - new Vector3(stand.x, stand.y + 1.62f, stand.z); // eye height above the floor
                float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg;
                player.Teleport(stand, yaw, pitch);

                flashlight.SetOn(false);
                yield return new WaitForSeconds(0.5f);
                Assert.Less(m.Visibility, 0.05f, $"{t.name} is visible without the flashlight");

                flashlight.SetOn(true);
                yield return new WaitForSeconds(1.2f);
                Assert.Greater(m.Visibility, 0.6f, $"{t.name} does not appear in the beam");

                player.Teleport(stand, yaw + 120f, 0f); // beam away
                yield return new WaitForSeconds(1.8f);
                Assert.Less(m.Visibility, 0.05f, $"{t.name} stays visible after the beam moved away");
            }
            flashlight.SetOn(false);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
