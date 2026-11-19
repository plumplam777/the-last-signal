using System.Collections;
using System.Linq;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.Puzzles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Engineering: coupling → generator start order → routing (Array feed temptation) → Medical door; sector transitions.</summary>
    public class EngineeringTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            CompleteDockingBay();
            SceneManager.LoadScene(SceneIds.Engineering);
            yield return null;
            yield return null;
        }

        /// <summary>State of a run that finished the Docking Bay (objectives are sequential).</summary>
        private static void CompleteDockingBay()
        {
            var world = GameServices.World;
            GameServices.Inventory.Add(GameServices.Content.FindItem("item_flashlight"));
            world.SetBool(FlashlightController.OwnedFlag, true);
            world.SetBool(WorldKeys.Story("dock_fuse_installed"), true);
            world.SetBool(WorldKeys.Power(SectorIds.Docking), true);
            world.SetBool(WorldKeys.DoorUnlocked("dock_inner"), true);
        }

        [Test]
        public void Arrival_CompletesCorridorObjective()
        {
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.SectorVisited(SectorIds.Engineering)));
            Assert.IsTrue(GameServices.Quests.IsCompleted("obj_04b"));
        }

        [Test]
        public void FullEngineeringFlow()
        {
            var world = GameServices.World;
            var socket = Object.FindAnyObjectByType<ItemSocketInteractable>();
            var generator = Object.FindAnyObjectByType<SequencePuzzle>();
            var board = Object.FindAnyObjectByType<RoutingBoard>();
            var door = Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None).First(d => d.DoorId == "eng_medical_corridor");
            var switches = Object.FindObjectsByType<SequenceStepInteractable>(FindObjectsSortMode.None);
            SequenceStepInteractable Switch(string step) => switches.First(s => s.InteractableId == $"eng_switch_{step}");

            // switches are dead until the coupling is replaced
            Assert.IsFalse(Switch("coolant").CanInteract(null, out var reason));
            Assert.AreEqual("eng.panel.no_coupling", reason);
            Assert.IsFalse(socket.CanInteract(null, out reason));
            Assert.AreEqual("eng.coupling.missing", reason);

            Object.FindObjectsByType<PickupInteractable>(FindObjectsSortMode.None).First(p => p.Item.Id == "item_power_coupling").Interact(null);
            socket.Interact(null);
            Assert.IsTrue(world.GetBool(WorldKeys.Story("eng_coupling_installed")));
            Assert.AreEqual("obj_06", GameServices.Quests.CurrentObjective?.id);

            // wrong order resets, correct order starts the generator
            Switch("ignition").Interact(null);
            Assert.AreEqual(0, generator.Progress);
            foreach (var step in new[] { "coolant", "fuel", "ignition", "load" }) Switch(step).Interact(null);
            Assert.IsTrue(generator.IsSolved);
            Assert.IsTrue(world.GetBool(WorldKeys.Power(SectorIds.Engineering)));
            Assert.AreEqual("obj_07", GameServices.Quests.CurrentObjective?.id);

            // routing: only 3 cables; the Array feed is a trap that also records the choice
            Assert.IsFalse(door.CanInteract(null, out reason));
            Assert.AreEqual("eng.door.medical_no_power", reason);
            Assert.IsTrue(board.Toggle("array_feed", out _));
            Assert.IsFalse(world.GetBool(WorldKeys.Story("fed_array")), "Feeding the Array is decided in AI_ENG_03, not by the plug.");
            board.Toggle("medical", out _);
            board.Toggle("lighting", out _);
            Assert.IsFalse(board.Toggle("life_support", out var feedback), "Fourth cable must be refused.");
            Assert.AreEqual("puzzle.routing.no_cables", feedback);
            board.Toggle("array_feed", out _);
            board.Toggle("life_support", out _);
            Assert.IsTrue(board.IsSolved);
            Assert.IsTrue(world.GetBool(WorldKeys.Power(SectorIds.Medical)));
            Assert.IsTrue(door.CanInteract(null, out _));
            Assert.AreEqual("obj_07b", GameServices.Quests.CurrentObjective?.id);
        }

        [UnityTest]
        public IEnumerator HatchToDocking_LoadsSectorAtCorridorSpawn()
        {
            var hatch = Object.FindObjectsByType<SectorHatch>(FindObjectsSortMode.None).First(h => h.TargetScene == SceneIds.DockingBay);
            hatch.Interact(null);
            float t = 0f;
            while ((SceneManager.GetActiveScene().name != SceneIds.DockingBay || GameServices.Scenes.IsLoading) && t < 10f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            Assert.AreEqual(SceneIds.DockingBay, SceneManager.GetActiveScene().name);
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.Greater(player.transform.position.z, 10f, "Player should arrive in the corridor next to the Engineering hatch.");
        }

        [Test]
        public void MedicalHatch_LeadsToTheBuiltMedicalBay()
        {
            var hatch = Object.FindObjectsByType<SectorHatch>(FindObjectsSortMode.None).First(h => h.TargetScene == SceneIds.Medical);
            Assert.IsTrue(hatch.CanInteract(null, out _), "The hatch itself has no lock; the corridor door needs routed power.");
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(SceneIds.Medical), "Sector_Medical must be in Build Settings.");
        }
    }
}
