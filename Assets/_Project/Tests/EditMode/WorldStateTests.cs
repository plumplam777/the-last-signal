using LastSignal.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public class WorldStateTests
    {
        [Test]
        public void SetBool_RaisesChangedOnlyOnActualChange()
        {
            var world = new WorldState();
            int raised = 0;
            world.Changed += _ => raised++;

            world.SetBool(WorldKeys.Power(SectorIds.Docking), true);
            world.SetBool(WorldKeys.Power(SectorIds.Docking), true);

            Assert.AreEqual(1, raised);
            Assert.IsTrue(world.GetBool("power.docking"));
        }

        [Test]
        public void AddInt_ClampsTrust()
        {
            var world = new WorldState();
            world.SetInt(WorldKeys.AiTrust, 95);
            int result = world.AddInt(WorldKeys.AiTrust, 20, WorldKeys.AiTrustMin, WorldKeys.AiTrustMax);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void Snapshot_RoundTripsThroughJson()
        {
            var world = new WorldState();
            world.SetBool(WorldKeys.DoorUnlocked("dock_inner"), true);
            world.SetInt(WorldKeys.AiTrust, 63);
            world.SetString(WorldKeys.Choice("final"), "destroy");

            string json = JsonConvert.SerializeObject(world.CreateSnapshot());
            var restored = new WorldState();
            restored.Restore(JsonConvert.DeserializeObject<WorldStateSnapshot>(json));

            Assert.IsTrue(restored.GetBool("door.dock_inner.unlocked"));
            Assert.AreEqual(63, restored.GetInt("ai.trust"));
            Assert.AreEqual("destroy", restored.GetString("choice.final"));
        }
    }
}
