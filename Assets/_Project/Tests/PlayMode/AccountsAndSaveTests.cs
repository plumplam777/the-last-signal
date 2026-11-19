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
    /// <summary>Start screen flows (new game / load game / admin in settings), save → continue round trip, admin-only overrides. Uses a temporary DB and save folder.</summary>
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

        private static void Click(string path) => GameObject.Find(path).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        /// <summary>Path-based GameObject.Find also returns inactive children, so check visibility explicitly.</summary>
        private static bool Visible(string path) { var go = GameObject.Find(path); return go != null && go.activeInHierarchy; }
        private static void Type(string path, string text) => GameObject.Find(path).GetComponent<TMPro.TMP_InputField>().text = text;

        private static IEnumerator OpenMainMenu()
        {
            SceneManager.LoadScene(SceneIds.MainMenu);
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<StartScreen>());
        }

        [UnityTest]
        public IEnumerator FirstStart_ShowsPlayerMenu_NotAdminSetup()
        {
            yield return OpenMainMenu();
            Assert.IsTrue(GameServices.Auth.NeedsFirstAdmin);
            foreach (var b in new[] { "Main/NewGame", "Main/LoadGame", "Main/Settings", "Main/Quit" })
                Assert.IsTrue(Visible(b), $"{b} should be on the start screen.");
            Assert.IsFalse(Visible("Admin"), "Administrator setup stays inside Settings.");
            Assert.IsFalse(Visible("Main/Logout"), "No log-out button before anyone signs in.");
        }

        [UnityTest]
        public IEnumerator NewGame_CreatesPlayerAccount_AndStartsOpening()
        {
            yield return OpenMainMenu();
            Click("Main/NewGame");
            Assert.IsTrue(Visible("Register"), "New game asks for an account first.");
            Type("Register/Username", "newpilot");
            Type("Register/Password", "secret1");
            Type("Register/Confirm", "secret1");
            Click("Register/Create");
            Assert.IsTrue(GameServices.Auth.IsLoggedIn);
            Assert.IsFalse(GameServices.Auth.IsAdmin, "Accounts made from New game are players.");
            yield return WaitForScene(SceneIds.CutsceneOpening);
            Assert.AreEqual(SceneIds.CutsceneOpening, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator LoadGame_SignIn_ShowsContinueAndRestart()
        {
            Assert.IsNull(GameServices.Auth.Register("pilot", "secret1", "secret1", UserRole.Player, out _));
            yield return OpenMainMenu();
            Click("Main/LoadGame");
            Type("Login/Username", "pilot");
            Type("Login/Password", "secret1");
            Click("Login/Login");
            Assert.IsTrue(Visible("Load"), "After signing in, the load page is shown.");
            Assert.IsFalse(GameObject.Find("Load/Continue").GetComponent<UnityEngine.UI.Button>().interactable, "No save yet → nothing to continue.");
            Assert.IsTrue(Visible("Load/Restart"));
        }

        [UnityTest]
        public IEnumerator Settings_AdminLogin_RejectsPlayers_AcceptsAdmins()
        {
            var auth = GameServices.Auth;
            Assert.IsNull(auth.Register("boss", "secret1", "secret1", UserRole.Admin, out _));
            Assert.IsNull(auth.Register("pilot", "secret1", "secret1", UserRole.Player, out _));
            yield return OpenMainMenu();
            Click("Main/Settings");
            Click("Settings/Administrator");
            Assert.IsTrue(Visible("Admin/Form"), "Admin login form inside Settings.");
            Assert.IsFalse(Visible("Admin/Form/Confirm"), "No 'repeat password' once an admin exists.");

            Type("Admin/Form/Username", "pilot");
            Type("Admin/Form/Password", "secret1");
            Click("Admin/Form/Submit");
            Assert.IsFalse(auth.IsLoggedIn, "A player account cannot sign in as administrator.");

            Type("Admin/Form/Username", "boss");
            Type("Admin/Form/Password", "secret1");
            Click("Admin/Form/Submit");
            Assert.IsTrue(auth.IsAdmin);
            Assert.IsTrue(Visible("Admin/OpenPanel"));
        }

        [UnityTest]
        public IEnumerator Settings_FirstAdmin_CanBeCreatedThere()
        {
            yield return OpenMainMenu();
            Click("Main/Settings");
            Click("Settings/Administrator");
            Assert.IsTrue(Visible("Admin/Form/Confirm"), "Empty database → create-administrator form.");
            Type("Admin/Form/Username", "boss");
            Type("Admin/Form/Password", "secret1");
            Type("Admin/Form/Confirm", "secret1");
            Click("Admin/Form/Submit");
            Assert.IsTrue(GameServices.Auth.IsAdmin);
            Assert.IsFalse(GameServices.Auth.NeedsFirstAdmin);
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
