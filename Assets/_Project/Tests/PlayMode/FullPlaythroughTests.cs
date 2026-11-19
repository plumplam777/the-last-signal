using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LastSignal.Core;
using LastSignal.Database;
using LastSignal.Dialogue;
using LastSignal.Endings;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.Puzzles;
using LastSignal.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Phase 7: one continuous run per ending, start → finish, with no state set by hand. The player moves between
    /// sectors only through the real hatches, answers every CAIRN conversation (always the most trusting or the most
    /// hostile answer), picks up every visible audio log, and at each sector saves → wipes the run → Load game.
    /// Every visible text is checked for missing translation keys (#key#). Unexpected error logs fail the test.
    /// </summary>
    public class FullPlaythroughTests
    {
        private enum Attitude { Trusting, Hostile }

        private string _dir;
        private long _userId;
        private Attitude _attitude;

        [UnitySetUp]
        public IEnumerator UseTemporaryAccount()
        {
            _dir = Path.Combine(Path.GetTempPath(), $"lastsignal_run_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_dir);
            GameServices.Auth.Logout();
            Assert.IsTrue(GameServices.Database.Open(Path.Combine(_dir, "test.db")));
            GameServices.Database.SeedCatalog(GameServices.Content, k => k);
            GameServices.Saves.SetRoot(Path.Combine(_dir, "Saves"));
            Assert.IsNull(GameServices.Auth.Register("runner", "secret1", "secret1", UserRole.Player, out _userId));
            Assert.IsNull(GameServices.Auth.Login("runner", "secret1"));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreRealData()
        {
            Time.timeScale = 1f;
            EscapeSequence.Instance?.Cancel();
            GameServices.Dialogue.StopAll();
            GameServices.AudioLogs.Stop();
            GameServices.Localization.SetLanguage(Language.English, persist: false);
            GameServices.Auth.Logout();
            GameServices.StartNewRun();
            GameServices.Database.Open(Path.Combine(Application.persistentDataPath, "lastsignal.db"));
            GameServices.Saves.SetRoot(Path.Combine(Application.persistentDataPath, "Saves"));
            yield return null;
            try { Directory.Delete(_dir, true); } catch { /* temp folder */ }
        }

        // ================================================================== the four endings

        [UnityTest, Timeout(600000)]
        public IEnumerator EndingA_HighTrust_EscapeThroughOpenTunnel()
        {
            yield return PlayToDecision(Attitude.Trusting);
            Decide(EndingType.Destroy);
            yield return null;
            Assert.IsTrue(EscapeSequence.Instance.IsRunning);
            Assert.IsTrue(Tunnel.CanInteract(null, out _), "High trust: CAIRN opens the tunnel.");
            yield return EscapeToTern();
            yield return FinishEnding(EndingType.Destroy);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EndingA_LowTrust_LoadGameMidEscape_CrankAndBoard()
        {
            yield return PlayToDecision(Attitude.Hostile);
            Decide(EndingType.Destroy);
            yield return null;
            Assert.IsFalse(Tunnel.CanInteract(null, out var reason));
            Assert.AreEqual("reactor.tunnel.cairn_sealed", reason);

            // quit mid-escape, then Load game: the countdown must come back, fresh
            yield return SaveWipeAndLoad();
            Assert.IsTrue(EscapeSequence.Instance.IsRunning, "Escape re-arms after Load game.");
            Assert.Greater(EscapeSequence.Instance.Remaining, 100f);

            var crank = Object.FindAnyObjectByType<CrankInteractable>();
            Assert.IsNotNull(crank, "Crank is available after loading a low-trust escape.");
            while (!crank.IsDone) crank.Interact(null);
            Assert.IsTrue(Tunnel.CanInteract(null, out _));
            yield return EscapeToTern();
            yield return FinishEnding(EndingType.Destroy);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EndingB_HighTrust_InBulgarian()
        {
            GameServices.Localization.SetLanguage(Language.Bulgarian, persist: false);
            yield return PlayToDecision(Attitude.Trusting);
            Decide(EndingType.Restore);
            yield return FinishEnding(EndingType.Restore);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EndingC_AllFourteenLogs_LowTrust()
        {
            yield return PlayToDecision(Attitude.Hostile);
            var logs = GameServices.AudioLogs;
            Assert.AreEqual(logs.HiddenEndingTotal, logs.HiddenEndingCollected,
                "A complete run must be able to find every log: missing " +
                string.Join(", ", logs.AllLogs.Where(l => l.CountsForHiddenEnding && !logs.IsCollected(l)).Select(l => l.Id)));
            Decide(EndingType.Silence);
            yield return FinishEnding(EndingType.Silence);
        }

        /// <summary>Regression: New game after a finished run started with obj_01 already done (leftover flashlight).</summary>
        [Test]
        public void NewRun_AfterAnotherRun_StartsAtTheFirstObjective()
        {
            GameServices.Inventory.Add(GameServices.Content.FindItem("item_flashlight"));
            Assert.IsTrue(GameServices.Quests.IsCompleted("obj_01"));
            GameServices.StartNewRun();
            Assert.IsFalse(GameServices.Quests.IsCompleted("obj_01"));
            Assert.AreEqual("obj_01", GameServices.Quests.CurrentObjective?.id);
        }

        // ================================================================== the run

        private IEnumerator PlayToDecision(Attitude attitude)
        {
            _attitude = attitude;
            GameServices.StartNewRun();
            GameServices.Scenes.Load(SceneIds.DockingBay);
            yield return Arrive(SceneIds.DockingBay);
            yield return DockingBay();
            yield return Through(SceneIds.Engineering);
            yield return Engineering();
            yield return Through(SceneIds.Medical);
            yield return Medical();
            yield return Through(SceneIds.Research);
            yield return Research();
            yield return Through(SceneIds.ReactorCommand);
            yield return ReactorAndCommand();

            int trust = GameServices.World.GetInt(WorldKeys.AiTrust);
            if (attitude == Attitude.Trusting) Assert.GreaterOrEqual(trust, 50, "Always trusting CAIRN must reach high trust.");
            else Assert.Less(trust, 50, "Always distrusting CAIRN must end in low trust.");
        }

        private IEnumerator DockingBay()
        {
            ExpectObjective("obj_01");
            Pickup("item_flashlight").Interact(null);
            Pickup("item_fuse_30a").Interact(null);
            Object.FindAnyObjectByType<ItemSocketInteractable>().Interact(null);
            Object.FindAnyObjectByType<LeverInteractable>().Interact(null);
            Object.FindAnyObjectByType<DoorController>().Interact(null);
            yield return Settle();
            yield return CollectVisibleLogs();
            ExpectObjective("obj_04b");
            yield return SaveWipeAndLoad();
        }

        private IEnumerator Engineering()
        {
            Assert.IsTrue(GameServices.Quests.IsCompleted("obj_04b"));
            ExpectObjective("obj_05");
            var switches = Object.FindObjectsByType<SequenceStepInteractable>(FindObjectsSortMode.None);
            Object.FindObjectsByType<PickupInteractable>(FindObjectsSortMode.None).First(p => p.Item != null && p.Item.Id == "item_power_coupling").Interact(null);
            Object.FindAnyObjectByType<ItemSocketInteractable>().Interact(null);
            yield return Settle();
            foreach (var step in new[] { "coolant", "fuel", "ignition", "load" })
                switches.First(s => s.InteractableId == $"eng_switch_{step}").Interact(null);
            yield return Settle();
            var board = Object.FindAnyObjectByType<RoutingBoard>();
            board.Toggle("array_feed", out _); // the temptation (AI_ENG_03)
            yield return Settle();
            board.Toggle("array_feed", out _);
            foreach (var cable in new[] { "medical", "life_support", "lighting" }) board.Toggle(cable, out _);
            Assert.IsTrue(board.IsSolved);
            yield return Settle();
            yield return CollectVisibleLogs();
            ExpectObjective("obj_07b");
            yield return SaveWipeAndLoad();
        }

        private IEnumerator Medical()
        {
            ExpectObjective("obj_08");
            var player = Object.FindAnyObjectByType<PlayerController>();
            var podZone = Object.FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None).FirstOrDefault(t => t.GetComponent<Collider>() != null && t.GetComponent<Collider>().isTrigger);
            if (podZone != null)
            {
                player.Teleport(podZone.transform.position, player.Yaw); // walk up to the pods (AI_MED_01)
                yield return new WaitForSeconds(0.3f);
                yield return Settle();
            }
            Phase6.Find<Interactable>(i => i.InteractableId == "med_pod_wipe").Interact(null);
            var console = Phase6.Find<TerminalInteractable>(t => t.Terminal != null && t.Terminal.Id == "med_patient");
            console.Interact(null);
            var screen = Object.FindAnyObjectByType<TerminalScreen>();
            foreach (var k in new[] { "0", "4", "1", "7", "Enter" }) screen.PressKey(k);
            screen.ShowEntry(1);
            AssertNoMissingText();
            screen.Close();
            yield return null;
            yield return CollectVisibleLogs(); // includes Okafor's day -9 note → AI_MED_02
            Phase6.Find<PickupInteractable>(p => p.InteractableId == "med_pickup_keycard_research").Interact(null);
            yield return Settle();
            ExpectObjective("obj_10");
            yield return SaveWipeAndLoad();
        }

        private IEnumerator Research()
        {
            ExpectObjective("obj_11");
            var puzzle = Object.FindAnyObjectByType<SymbolDecodePuzzle>();
            foreach (var s in new[] { "sqr", "cir", "tri", "cir", "sqr", "dia" }) puzzle.Press(s);
            puzzle.Submit();
            Assert.IsTrue(puzzle.IsSolved);
            yield return Settle();
            yield return CollectVisibleLogs();
            Phase6.Find<PickupInteractable>(p => p.InteractableId == "res_pickup_keycard_core").Interact(null);
            yield return Settle();
            ExpectObjective("obj_13");
            yield return SaveWipeAndLoad();
        }

        private IEnumerator ReactorAndCommand()
        {
            ExpectObjective("obj_14");
            ValveInteractable Valve(int n) => Phase6.Find<ValveInteractable>(v => v.InteractableId == $"core_valve_{n}");
            Valve(1).Interact(null);
            Valve(3).Interact(null);
            Valve(3).Interact(null);
            Assert.IsTrue(Object.FindAnyObjectByType<ReactorBalancePuzzle>().IsSolved);
            Phase6.Find<LiftInteractable>(l => l.InteractableId == "core_lift_up").Interact(null);
            var player = Object.FindAnyObjectByType<PlayerController>();
            float t = 0f;
            while (player.transform.position.y < 15f && t < 8f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("command_reached")), "Lift reaches Command.");
            yield return Settle();
            ExpectObjective("obj_15");
            yield return SaveWipeAndLoad();
            Assert.Greater(Object.FindAnyObjectByType<PlayerController>().transform.position.y, 15f, "Load game resumes at Command, not in the core.");

            yield return CollectVisibleLogs(); // chair, comms (critical → AI_CORE_02), private archive (→ needs LOG_RES_03)
            ExpectObjective("obj_16");

            var console = Object.FindAnyObjectByType<ConsoleDialogueInteractable>();
            Assert.IsTrue(console.CanInteract(null, out var reason), reason);
            console.Interact(null);
            var decision = Object.FindAnyObjectByType<DecisionScreen>();
            yield return Settle(until: () => decision.IsOpen);
            Assert.IsTrue(decision.IsOpen, "AI_CORE_03 ends in the decision screen.");
            AssertNoMissingText();
        }

        // ================================================================== endings

        private static SectorHatch Tunnel => Phase6.Find<SectorHatch>(h => h.InteractableId == "hatch_servicetunnel");

        private static void Decide(EndingType type)
        {
            var decision = Object.FindAnyObjectByType<DecisionScreen>();
            if (!decision.IsOpen) decision.Open();
            decision.Pick(type);
            Assert.AreEqual(EndingScript.Id(type) != null, true);
            decision.Confirm();
            Assert.AreEqual(EndingScript.Id(type), GameServices.World.GetString(EndingScript.ChoiceKey), $"{type} was not accepted.");
        }

        private IEnumerator EscapeToTern()
        {
            Tunnel.Interact(null);
            yield return Phase6.WaitForScene(SceneIds.DockingBay);
            Assert.IsTrue(EscapeSequence.Instance.IsRunning);
            AssertNoMissingText();
            Object.FindAnyObjectByType<BoardShipInteractable>().Interact(null);
        }

        private IEnumerator FinishEnding(EndingType type)
        {
            yield return Phase6.WaitForScene(SceneIds.Ending, 20f);
            Assert.AreEqual(SceneIds.Ending, SceneManager.GetActiveScene().name);
            Assert.AreEqual(type, Object.FindAnyObjectByType<EndingController>().Type);
            Assert.IsTrue(GameServices.Quests.IsCompleted("obj_16"), "Reaching an ending completes the last main objective.");
            if (type == EndingType.Destroy) Assert.IsTrue(GameServices.Quests.IsCompleted("obj_17a"));

            var summary = GameServices.Database.Progress.Find(_userId);
            Assert.IsNotNull(summary, "The database keeps the run summary.");

            // watch the ending in fast-forward, checking every subtitle for missing keys
            Time.timeScale = 30f;
            float t = 0f;
            while ((SceneManager.GetActiveScene().name != SceneIds.MainMenu || GameServices.Scenes.IsLoading) && t < 60f)
            {
                if (SceneManager.GetActiveScene().name == SceneIds.Ending) AssertNoMissingText();
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name, "Credits return to the main menu.");
        }

        // ================================================================== helpers

        private IEnumerator Through(string scene)
        {
            var hatch = Phase6.HatchTo(scene);
            Assert.IsTrue(hatch.CanInteract(null, out var reason), $"Hatch to {scene} still locked: {reason}");
            hatch.Interact(null);
            yield return Arrive(scene);
        }

        private IEnumerator Arrive(string scene)
        {
            yield return Phase6.WaitForScene(scene);
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(GameState.Playing, GameServices.State.Current, $"{scene}: not in Playing state on arrival.");
            yield return Settle(minSeconds: 3.5f); // sector-start CAIRN lines have a short delay
            AssertNoMissingText();
        }

        /// <summary>Plays through any running dialogue / audio log, answering with the run's attitude.</summary>
        private IEnumerator Settle(float minSeconds = 0.4f, System.Func<bool> until = null)
        {
            float t = 0f, quiet = 0f;
            while (t < 40f)
            {
                var d = GameServices.Dialogue;
                bool busy = d.IsRunning || GameServices.AudioLogs.IsPlaying || GameServices.Scenes.IsLoading;
                if (d.IsRunning)
                {
                    AssertNoMissingText();
                    if (d.AwaitingChoice) d.Choose(PickChoice(d));
                    else d.SkipLine();
                }
                if (GameServices.AudioLogs.IsPlaying)
                {
                    AssertNoMissingText();
                    GameServices.AudioLogs.Stop();
                }
                quiet = busy ? 0f : quiet + Time.unscaledDeltaTime;
                t += Time.unscaledDeltaTime;
                if (until != null ? until() && !busy : quiet >= 0.4f && t >= minSeconds) break;
                yield return null;
            }
            Assert.Less(t, 40f, "Dialogue/logs never settled.");
        }

        private int PickChoice(DialogueManager d)
        {
            int best = 0, bestDelta = int.MinValue;
            for (int i = 0; i < d.CurrentChoices.Count; i++)
            {
                int delta = d.CurrentChoices[i].effects.Where(e => e.type == EffectType.AddInt && e.key == WorldKeys.AiTrust).Sum(e => e.value);
                if (_attitude == Attitude.Hostile) delta = -delta;
                if (delta > bestDelta)
                {
                    best = i;
                    bestDelta = delta;
                }
            }
            return best;
        }

        /// <summary>Picks up every audio log the player can currently reach, including ones revealed by earlier logs.</summary>
        private IEnumerator CollectVisibleLogs()
        {
            for (int pass = 0; pass < 6; pass++)
            {
                var pending = Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None)
                    .Where(l => l.isActiveAndEnabled && !GameServices.AudioLogs.IsCollected(l.Log) && l.CanInteract(null, out _))
                    .ToList();
                if (pending.Count == 0) yield break;
                foreach (var log in pending)
                {
                    log.Interact(null);
                    yield return Settle();
                }
            }
        }

        /// <summary>Checkpoint → quit to menu → new empty run → Load game; the run must come back identical.</summary>
        private IEnumerator SaveWipeAndLoad()
        {
            yield return Settle();
            string scene = SceneManager.GetActiveScene().name;
            string objective = GameServices.Quests.CurrentObjective?.id;
            int trust = GameServices.World.GetInt(WorldKeys.AiTrust);
            int logs = GameServices.AudioLogs.CollectedCount;
            int items = GameServices.Inventory.Items.Count;
            GameServices.Checkpoint();

            GameServices.StartNewRun();
            EscapeSequence.Instance?.Cancel();
            GameServices.Scenes.Load(SceneIds.MainMenu);
            yield return Phase6.WaitForScene(SceneIds.MainMenu);
            Assert.IsTrue(GameServices.Saves.Continue(), GameServices.Saves.LastError);
            yield return Phase6.WaitForScene(scene);
            yield return null;

            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "Load game returns to the saved sector.");
            Assert.AreEqual(objective, GameServices.Quests.CurrentObjective?.id, $"{scene}: objective after Load game.");
            Assert.AreEqual(trust, GameServices.World.GetInt(WorldKeys.AiTrust), $"{scene}: trust after Load game.");
            Assert.AreEqual(logs, GameServices.AudioLogs.CollectedCount, $"{scene}: logs after Load game.");
            Assert.AreEqual(items, GameServices.Inventory.Items.Count, $"{scene}: inventory after Load game.");
            yield return Settle(minSeconds: 3.5f);
        }

        private static void ExpectObjective(string id)
            => Assert.AreEqual(id, GameServices.Quests.CurrentObjective?.id, $"Objective out of order in {SceneManager.GetActiveScene().name}.");

        private static PickupInteractable Pickup(string itemId)
            => Object.FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .First(p => p.Item != null && p.Item.Id == itemId);

        private static readonly Regex MissingKey = new(@"#[a-z0-9_]+(\.[a-z0-9_]+)+#");

        /// <summary>Fails on any visible text that is an untranslated key (Loc.Get returns #key# for unknown keys).</summary>
        private static void AssertNoMissingText()
        {
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text)) continue;
                var m = MissingKey.Match(text.text);
                Assert.IsFalse(m.Success, $"Missing translation '{m.Value}' on '{text.name}' in {SceneManager.GetActiveScene().name} ({GameServices.Localization.Current}).");
            }
        }
    }
}
