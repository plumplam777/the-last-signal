using System.Collections;
using System.Linq;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.Puzzles;
using LastSignal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// End-to-end checks of the Docking Bay slice: flashlight → fuse → fuse box → breaker → power → door,
    /// plus objectives, audio logs, the bay terminal and the inventory screen.
    /// </summary>
    public class DockingBaySliceTests
    {
        [UnitySetUp]
        public IEnumerator LoadSector()
        {
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CloseScreens()
        {
            foreach (var s in Object.FindObjectsByType<UiScreen>(FindObjectsSortMode.None)) s.Close();
            GameServices.AudioLogs.Stop();
            yield return null;
        }

        [Test]
        public void Sector_SpawnsPlayerAndEntersPlaying()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Player was not spawned.");
            Assert.AreEqual(GameState.Playing, GameServices.State.Current);
            Assert.Less(Vector3.Distance(player.transform.position, new Vector3(0f, 0.05f, -7.4f)), 0.5f, "Player not at spawn point.");
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.SectorVisited(SectorIds.Docking)));
        }

        [Test]
        public void Flashlight_RequiresPickup()
        {
            var flashlight = Object.FindAnyObjectByType<FlashlightController>();
            Assert.IsFalse(flashlight.IsOwned);
            var pickup = Pickup("item_flashlight");
            pickup.Interact(null);
            Assert.IsTrue(flashlight.IsOwned);
            Assert.IsTrue(GameServices.Inventory.Has("item_flashlight"));
            Assert.IsFalse(pickup.gameObject.activeSelf, "Picked-up object should be hidden.");
            flashlight.SetOn(true);
            Assert.IsTrue(flashlight.IsOn);
        }

        [UnityTest]
        public IEnumerator FuseAndBreaker_RestorePower_AndUnlockDoor()
        {
            var door = Object.FindAnyObjectByType<DoorController>();
            var socket = Object.FindAnyObjectByType<ItemSocketInteractable>();
            var breaker = Object.FindAnyObjectByType<LeverInteractable>();
            var power = Object.FindAnyObjectByType<PoweredObject>();

            Assert.IsFalse(door.CanInteract(null, out var reason));
            Assert.AreEqual("prompt.door.no_power", reason);
            Assert.IsFalse(socket.CanInteract(null, out var socketReason));
            Assert.AreEqual("prompt.socket.missing", socketReason);
            Assert.IsFalse(breaker.CanInteract(null, out var breakerReason));
            Assert.AreEqual("prompt.breaker.no_fuse", breakerReason);

            Pickup("item_fuse_30a").Interact(null);
            socket.Interact(null);
            Assert.IsFalse(GameServices.Inventory.Has("item_fuse_30a"), "Fuse should be consumed.");
            Assert.IsTrue(socket.IsInstalled);
            Assert.IsFalse(power.IsPowered, "Power needs the breaker too.");

            Assert.IsTrue(breaker.CanInteract(null, out _));
            breaker.Interact(null);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Power(SectorIds.Docking)));
            Assert.IsTrue(power.IsPowered);

            Assert.IsTrue(door.CanInteract(null, out _));
            door.Interact(null);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(DoorState.Open, door.State);
        }

        [Test]
        public void Objectives_AdvanceThroughTheBay()
        {
            var quests = GameServices.Quests;
            Assert.AreEqual("obj_01", quests.CurrentObjective?.id);
            Pickup("item_flashlight").Interact(null);
            Assert.AreEqual("obj_02", quests.CurrentObjective?.id);
            Pickup("item_fuse_30a").Interact(null);
            Assert.AreEqual("obj_03", quests.CurrentObjective?.id);
            Object.FindAnyObjectByType<ItemSocketInteractable>().Interact(null);
            Assert.AreEqual("obj_03", quests.CurrentObjective?.id, "Installing the fuse alone does not restore power.");
            Object.FindAnyObjectByType<LeverInteractable>().Interact(null);
            Assert.AreEqual("obj_04", quests.CurrentObjective?.id);
            Object.FindAnyObjectByType<DoorController>().Interact(null);
            Assert.AreEqual("obj_04b", quests.CurrentObjective?.id);
        }

        [UnityTest]
        public IEnumerator AudioLog_CollectsPlaysSubtitles_AndCounts()
        {
            var recorder = Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None).First(r => r.Log.Id == "log_dock_01");
            var logs = GameServices.AudioLogs;
            Assert.IsFalse(logs.IsCollected(recorder.Log));
            recorder.Interact(null);
            Assert.IsTrue(logs.IsCollected(recorder.Log));
            Assert.AreEqual(recorder.Log, logs.Current);
            Assert.AreEqual(1, logs.CollectedCount);
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.AreEqual("log.log_dock_01.line.1", logs.CurrentLineKey, "First subtitle line should be showing.");
            Assert.IsFalse(GameServices.Localization.Get(logs.CurrentLineKey).StartsWith("#"));
            recorder.Interact(null); // interacting again while playing stops it
            Assert.IsFalse(logs.IsPlaying);
            Assert.IsTrue(GameServices.World.GetBool(AudioLogManager.PlayedKey("log_dock_01")));
        }

        [UnityTest]
        public IEnumerator Terminal_OpensWithEntries_AndHidesPoweredEntry()
        {
            var console = Object.FindAnyObjectByType<TerminalInteractable>();
            var screen = Object.FindAnyObjectByType<TerminalScreen>();
            console.Interact(null);
            yield return null;
            Assert.IsTrue(screen.IsOpen);
            Assert.AreEqual(GameState.InMenu, GameServices.State.Current, "Terminal should take input focus.");
            Assert.IsTrue(GameServices.World.GetBool(TerminalDefinition.ReadKey("dock_bay", 0)), "First entry is shown and marked read.");
            var rows = screen.GetComponentsInChildren<UnityEngine.UI.Button>(false).Count(b => b.name.StartsWith("EntryTemplate"));
            Assert.AreEqual(3, rows, "Powered-only entry must be hidden before the breaker is closed.");
            screen.Close();
            Assert.AreEqual(GameState.Playing, GameServices.State.Current);
        }

        [UnityTest]
        public IEnumerator Inventory_ShowsPickedUpItems()
        {
            Pickup("item_fuse_30a").Interact(null);
            var screen = Object.FindAnyObjectByType<InventoryScreen>();
            // open through the same path as the Q key
            var open = typeof(UiScreen).GetMethod("TryOpen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsTrue((bool)open.Invoke(screen, null));
            yield return null;
            Assert.AreEqual(GameServices.Content.FindItem("item_fuse_30a"), GameServices.Inventory.Selected);
            var rows = screen.GetComponentsInChildren<UnityEngine.UI.Button>(false).Count(b => b.name.StartsWith("ItemTemplate"));
            Assert.AreEqual(1, rows);
            screen.Close();
        }

        [Test]
        public void Localization_AllKeysUsedInTheBayExistInBothLanguages()
        {
            var keys = new[] { "prompt.pickup", "prompt.inspect", "prompt.socket.missing", "prompt.socket.install",
                "prompt.door.open", "prompt.door.close", "prompt.door.no_power", "prompt.breaker.close", "prompt.breaker.no_fuse",
                "item.fuse.name", "item.flashlight.name", "obj.fusebox.name", "toast.item_added", "hint.flashlight",
                "ui.pause.resume", "obj.obj_01", "obj.obj_04b", "terminal.dock_bay.e3.body", "log.log_dock_02.line.8" };
            foreach (var lang in new[] { Language.English, Language.Bulgarian })
            {
                GameServices.Localization.SetLanguage(lang, persist: false);
                foreach (var k in keys) Assert.IsFalse(GameServices.Localization.Get(k).StartsWith("#"), $"{k} missing in {lang}");
            }
            GameServices.Localization.SetLanguage(Language.English, persist: false);
        }

        private static PickupInteractable Pickup(string itemId)
            => Object.FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .First(p => p.Item != null && p.Item.Id == itemId);
    }
}
