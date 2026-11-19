using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.Core;
using LastSignal.Database;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Start screen first-run flow, save → continue round trip, admin-only overrides. Uses a temporary DB and save folder.</summary>
    public class AccountsAndSaveTests
    {
        private string _dir;

        [UnitySetUp]
        public IEnumerator UseTemporaryData()
        {
            _dir = Path.Combine(Path.GetTempPath(), $"lastsignal_play_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_dir);
            GameServices.Auth.Logout();
            Assert.IsTrue(GameServices.Database.Open(Path.Combine(_dir, "test.db")));
            GameServices.Database.SeedCatalog(GameServices.Content, k => k);
            GameServices.Saves.SetRoot(Path.Combine(_dir, "Saves"));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreRealData()
        {
            GameServices.Auth.Logout();
            GameServices.Database.Open(Path.Combine(Application.persistentDataPath, "lastsignal.db"));
            GameServices.Saves.SetRoot(Path.Combine(Application.persistentDataPath, "Saves"));
            yield return null;
            try { Directory.Delete(_dir, true); } catch { /* temp folder */ }
        }

        private static IEnumerator WaitForScene(string scene, float seconds = 10f)
        {
            float t = 0f;
            while ((SceneManager.GetActiveScene().name != scene || GameServices.Scenes.IsLoading) && t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstStart_AsksForAdministrator()
        {
            SceneManager.LoadScene(SceneIds.MainMenu);
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<StartScreen>());
            Assert.IsTrue(GameServices.Auth.NeedsFirstAdmin);
            var page = GameObject.Find("FirstAdmin");
            Assert.IsNotNull(page, "First-run administrator page should be shown on an empty database.");
            Assert.IsNull(GameObject.Find("Login"), "Login page hidden until an admin exists.");
        }

        [UnityTest]
        public IEnumerator Save_ThenContinue_RestoresSceneInventoryAndPose()
        {
            var auth = GameServices.Auth;
            Assert.IsNull(auth.Register("tester", "secret1", "secret1", UserRole.Player, out var uid));
            Assert.IsNull(auth.Login("tester", "secret1"));
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return WaitForScene(SceneIds.DockingBay);
            Assert.IsTrue(File.Exists(GameServices.Saves.PathFor(uid)), "Entering a sector writes a checkpoint.");

            Object.FindObjectsByType<PickupInteractable>(FindObjectsSortMode.None).First(p => p.Item.Id == "item_flashlight").Interact(null);
            var player = Object.FindAnyObjectByType<PlayerController>();
            player.Teleport(new Vector3(2f, 0.05f, -1f), 90f);
            GameServices.World.SetInt(WorldKeys.AiTrust, 61);
            GameServices.Checkpoint();
            var summary = GameServices.Database.Progress.Find(uid);
            Assert.AreEqual(61, summary.AiTrust, "Database summary follows the JSON save.");

            // wipe the run, then continue
            GameServices.StartNewRun();
            Assert.IsFalse(GameServices.Inventory.Has("item_flashlight"));
            SceneManager.LoadScene(SceneIds.MainMenu);
            yield return WaitForScene(SceneIds.MainMenu);
            Assert.IsTrue(GameServices.Saves.Continue(), GameServices.Saves.LastError);
            yield return WaitForScene(SceneIds.DockingBay);
            Assert.IsTrue(GameServices.Inventory.Has("item_flashlight"));
            Assert.AreEqual(61, GameServices.World.GetInt(WorldKeys.AiTrust));
            player = Object.FindAnyObjectByType<PlayerController>();
            Assert.Less(Vector3.Distance(player.transform.position, new Vector3(2f, 0.05f, -1f)), 0.3f, "Saved position restored.");
            Assert.AreEqual(90f, player.Yaw, 1f);
        }

        [UnityTest]
        public IEnumerator CorruptSave_FallsBackToBackup()
        {
            var auth = GameServices.Auth;
            auth.Register("tester2", "secret1", "secret1", UserRole.Player, out var uid);
            auth.Login("tester2", "secret1");
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return WaitForScene(SceneIds.DockingBay);
            GameServices.Checkpoint(); // second write creates the .bak
            File.WriteAllText(GameServices.Saves.PathFor(uid), "{ this is not json");
            var data = GameServices.Saves.Read(uid);
            Assert.IsNotNull(data, "Backup should be used when the main save is corrupt.");
            Assert.AreEqual(SceneIds.DockingBay, data.scene);
        }

        [UnityTest]
        public IEnumerator AdminOverrides_OnlyForAdmins_AndNotSaved()
        {
            var auth = GameServices.Auth;
            auth.Register("boss", "secret1", "secret1", UserRole.Admin, out _);
            auth.Register("crew", "secret1", "secret1", UserRole.Player, out _);
            auth.Login("crew", "secret1");
            Assert.IsFalse(GameServices.Admin.SetUnlockAllDoors(true), "Players cannot use admin tools.");
            Assert.IsFalse(GameServices.Admin.UnlockAllDoors);
            auth.Login("boss", "secret1");
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return WaitForScene(SceneIds.DockingBay);
            var door = Object.FindAnyObjectByType<DoorController>();
            Assert.IsFalse(door.CanInteract(null, out _));
            Assert.IsTrue(GameServices.Admin.SetUnlockAllDoors(true));
            Assert.IsTrue(door.CanInteract(null, out _), "Admin unlock-all opens doors without power.");
            Assert.IsFalse(GameServices.World.GetBool(WorldKeys.Power(SectorIds.Docking)), "Override must not change world progress.");
            auth.Logout();
            Assert.IsFalse(GameServices.Admin.UnlockAllDoors, "Override ends with the session.");
        }
    }
}
