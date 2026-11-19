using System.Collections;
using System.Linq;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>End-to-end check of the Phase 2 slice: flashlight → fuse → fuse box → power → door.</summary>
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
        public IEnumerator Fuse_RestoresPower_AndUnlocksDoor()
        {
            var door = Object.FindAnyObjectByType<DoorController>();
            var socket = Object.FindAnyObjectByType<ItemSocketInteractable>();
            var power = Object.FindAnyObjectByType<PoweredObject>();

            Assert.IsFalse(door.CanInteract(null, out var reason));
            Assert.AreEqual("prompt.door.no_power", reason);
            Assert.IsFalse(socket.CanInteract(null, out var socketReason));
            Assert.AreEqual("prompt.socket.missing", socketReason);
            Assert.IsFalse(power.IsPowered);

            Pickup("item_fuse").Interact(null);
            Assert.IsTrue(GameServices.Inventory.Has("item_fuse"));

            Assert.IsTrue(socket.CanInteract(null, out _));
            socket.Interact(null);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Power(SectorIds.Docking)));
            Assert.IsFalse(GameServices.Inventory.Has("item_fuse"), "Fuse should be consumed.");
            Assert.IsTrue(socket.IsInstalled);
            Assert.IsTrue(power.IsPowered);

            Assert.IsTrue(door.CanInteract(null, out _));
            door.Interact(null);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(DoorState.Open, door.State);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.DoorUnlocked("dock_inner")));
        }

        [Test]
        public void Localization_AllPromptKeysExistInBothLanguages()
        {
            var keys = new[] { "prompt.pickup", "prompt.inspect", "prompt.socket.missing", "prompt.socket.install",
                "prompt.door.open", "prompt.door.close", "prompt.door.no_power", "item.fuse.name", "item.flashlight.name",
                "obj.fusebox.name", "toast.item_added", "hint.flashlight", "ui.pause.resume" };
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
