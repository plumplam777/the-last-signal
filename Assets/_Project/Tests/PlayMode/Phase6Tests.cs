using System.Collections;
using System.Linq;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Dialogue;
using LastSignal.Endings;
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
    /// <summary>Shared helpers for the Phase 6 sectors.</summary>
    internal static class Phase6
    {
        public static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
        }

        public static IEnumerator WaitForScene(string scene, float seconds = 15f)
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

        public static T Find<T>(System.Func<T, bool> where) where T : Object
            => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(where);

        public static SectorHatch HatchTo(string scene) => Find<SectorHatch>(h => h.TargetScene == scene);
    }

    /// <summary>Medical Bay (StoryBible §6.3): frost → PIN 0417 → record → release cabinet → research keycard.</summary>
    public class MedicalTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            GameServices.World.SetBool(WorldKeys.Power(SectorIds.Medical), true);
            yield return Phase6.Load(SceneIds.Medical);
        }

        [UnityTest]
        public IEnumerator FullMedicalFlow()
        {
            var world = GameServices.World;
            var wipe = Phase6.Find<Interactable>(i => i.InteractableId == "med_pod_wipe");
            var pickup = Phase6.Find<PickupInteractable>(p => p.InteractableId == "med_pickup_keycard_research");
            var hatch = Phase6.HatchTo(SceneIds.Research);

            Assert.IsFalse(pickup.CanInteract(null, out var reason));
            Assert.AreEqual("med.cabinet.locked", reason);
            Assert.IsFalse(hatch.CanInteract(null, out reason));
            Assert.AreEqual("med.door.research_keycard", reason);
            Assert.IsFalse(Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None).Any(l => l.Log.Id == "log_med_03"),
                "Okafor's day -9 note is hidden until the record is open.");

            wipe.Interact(null);
            Assert.IsTrue(world.GetBool(WorldKeys.Story("med_pod_wiped")));
            Assert.IsFalse(wipe.IsInteractionEnabled, "Wiping is one-shot.");

            var console = Phase6.Find<TerminalInteractable>(t => t.Terminal != null && t.Terminal.Id == "med_patient");
            console.Interact(null);
            var screen = Object.FindAnyObjectByType<TerminalScreen>();
            Assert.IsTrue(screen.IsOpen);
            foreach (var k in new[] { "1", "2", "3", "4", "Enter" }) screen.PressKey(k);
            Assert.IsFalse(world.GetBool(WorldKeys.PuzzleSolved("terminal_med_patient")), "Wrong PIN is rejected.");
            foreach (var k in new[] { "0", "4", "1", "7", "Enter" }) screen.PressKey(k);
            Assert.IsTrue(world.GetBool(WorldKeys.PuzzleSolved("terminal_med_patient")));
            Assert.IsFalse(world.GetBool(WorldKeys.Story("med_cabinet_released")), "The cabinet opens only from its record entry.");
            screen.ShowEntry(1);
            Assert.IsTrue(world.GetBool(WorldKeys.Story("med_cabinet_released")));
            screen.Close();
            yield return null;

            Assert.IsTrue(Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None).Any(l => l.Log.Id == "log_med_03"));
            Assert.IsTrue(pickup.CanInteract(null, out _));
            pickup.Interact(null);
            Assert.IsTrue(GameServices.Inventory.Has(GameServices.Content.FindItem("item_keycard_research")));
            Assert.IsTrue(hatch.CanInteract(null, out _));
        }

        [UnityTest]
        public IEnumerator OkaforsNote_TriggersCairnsConfession()
        {
            var def = GameServices.AudioLogs.AllLogs.First(l => l.Id == "log_med_03");
            GameServices.AudioLogs.Collect(def, playNow: false);
            GameServices.World.SetBool(AudioLogManager.PlayedKey("log_med_03"), true);
            float t = 0f;
            while (GameServices.Dialogue.Current?.Id != "ai_med_02" && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual("ai_med_02", GameServices.Dialogue.Current?.Id);
            yield return null;
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("knows_jonah_death")));
            GameServices.Dialogue.StopAll();
        }
    }

    /// <summary>Research Lab (StoryBible §6.4): Trial 7 = the sent row reversed; hint after two misses; safe → core card.</summary>
    public class ResearchTests
    {
        private static readonly string[] Answer = { "sqr", "cir", "tri", "cir", "sqr", "dia" };

        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            GameServices.Inventory.Add(GameServices.Content.FindItem("item_keycard_research"));
            GameServices.World.SetBool("dlg.ai_res_01.played", true); // keep the opening line out of the way
            yield return Phase6.Load(SceneIds.Research);
        }

        [TearDown]
        public void StopDialogue() => GameServices.Dialogue.StopAll();

        private static void Enter(SymbolDecodePuzzle p, string[] row)
        {
            foreach (var s in row) p.Press(s);
            p.Submit();
        }

        [UnityTest]
        public IEnumerator Decode_WrongTwice_GivesHint_ThenSolvingOpensSafe()
        {
            var world = GameServices.World;
            var puzzle = Object.FindAnyObjectByType<SymbolDecodePuzzle>();
            var safe = Phase6.Find<PickupInteractable>(p => p.InteractableId == "res_pickup_keycard_core");
            var hatch = Phase6.HatchTo(SceneIds.ReactorCommand);
            Assert.IsFalse(safe.CanInteract(null, out var reason));
            Assert.AreEqual("res.safe.locked", reason);
            Assert.IsFalse(hatch.CanInteract(null, out reason));
            Assert.AreEqual("res.door.core_keycard", reason);

            Enter(puzzle, new[] { "dia", "sqr", "cir", "tri", "cir", "sqr" }); // the sent row itself
            Assert.IsFalse(world.GetBool(WorldKeys.Story("res_decode_hint")));
            Enter(puzzle, new[] { "tri", "tri", "tri", "tri", "tri", "tri" });
            Assert.IsTrue(world.GetBool(WorldKeys.Story("res_decode_hint")), "Two misses → CAIRN's hint.");
            float t = 0f;
            while (GameServices.Dialogue.Current?.Id != "ai_res_hint" && t < 3f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual("ai_res_hint", GameServices.Dialogue.Current?.Id);
            GameServices.Dialogue.StopAll();

            Enter(puzzle, Answer);
            Assert.IsTrue(puzzle.IsSolved);
            Assert.IsFalse(world.GetBool(WorldKeys.Story("noticed_mirror")), "Neutral hint after misses does not count as noticing the mirror.");
            yield return null;
            Assert.IsTrue(Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None).Any(l => l.Log.Id == "log_res_02"), "Protected files open.");
            Assert.IsTrue(safe.CanInteract(null, out _));
            safe.Interact(null);
            Assert.IsTrue(hatch.CanInteract(null, out _));
        }

        [Test]
        public void Decode_FirstTry_NoticesTheMirror()
        {
            var puzzle = Object.FindAnyObjectByType<SymbolDecodePuzzle>();
            Enter(puzzle, Answer);
            Assert.IsTrue(puzzle.IsSolved);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("noticed_mirror")));
        }
    }

    /// <summary>Reactor Core + Command (StoryBible §6.5, §9): valves → power → lift → comms log → captain's console.</summary>
    public class ReactorCommandTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            GameServices.Inventory.Add(GameServices.Content.FindItem("item_keycard_core"));
            GameServices.World.SetBool("dlg.ai_core_01.played", true);
            yield return Phase6.Load(SceneIds.ReactorCommand);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            GameServices.Dialogue.StopAll();
        }

        private static ValveInteractable Valve(int n) => Phase6.Find<ValveInteractable>(v => v.InteractableId == $"core_valve_{n}");

        [UnityTest]
        public IEnumerator Balancing_PowersTheLift_ToCommand()
        {
            var lift = Phase6.Find<LiftInteractable>(l => l.InteractableId == "core_lift_up");
            Assert.IsFalse(lift.CanInteract(null, out var reason));
            Assert.AreEqual("reactor.lift.no_power", reason);

            Valve(1).Interact(null);   // Mid → High
            Valve(3).Interact(null);   // Mid → High
            Valve(3).Interact(null);   // High → Low   ⇒ High / Mid / Low
            var puzzle = Object.FindAnyObjectByType<ReactorBalancePuzzle>();
            Assert.IsTrue(puzzle.IsSolved, $"Output {puzzle.Balance.Output}, temp {puzzle.Balance.Temperature}");
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Power(SectorIds.Reactor)));
            Assert.IsFalse(Valve(2).IsInteractionEnabled, "Valves lock once stable.");

            Assert.IsTrue(lift.CanInteract(null, out _));
            lift.Interact(null);
            float t = 0f;
            var player = Object.FindAnyObjectByType<PlayerController>();
            while (player.transform.position.y < 15f && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.Greater(player.transform.position.y, 15f, "The lift moves the player up to Command.");
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("command_reached")));

            var console = Object.FindAnyObjectByType<ConsoleDialogueInteractable>();
            Assert.IsFalse(console.CanInteract(null, out reason));
            Assert.AreEqual("command.console.first_comms", reason);
            GameServices.AudioLogs.Collect(GameServices.AudioLogs.AllLogs.First(l => l.Id == "log_core_02"), playNow: false);
            Assert.IsTrue(console.CanInteract(null, out _));
        }

        [UnityTest]
        public IEnumerator ValveGauges_React_AndTheConsoleNamesResonatingLoops()
        {
            var puzzle = Object.FindAnyObjectByType<ReactorBalancePuzzle>();
            for (int i = 0; i < 3; i++)
            {
                var gauge = puzzle.LoopGauge(i);
                Assert.IsNotNull(gauge, $"Loop {i} has a pressure gauge.");
                Assert.Less(Vector3.Distance(gauge.transform.position, Valve(i + 1).transform.position), 1.2f, "The gauge sits by its own valve.");
                StringAssert.Contains("1.6", gauge.Readout);
                Assert.AreEqual(PressureGauge.Resonant, gauge.ReadoutColour, "Mid/Mid/Mid resonates.");
            }
            StringAssert.Contains("A = B = C", puzzle.StatusLine);

            float before = puzzle.LoopGauge(0).TargetAngle;
            Valve(1).Interact(null); // H M M
            Assert.AreNotEqual(before, puzzle.LoopGauge(0).TargetAngle, "Turning a valve swings its gauge.");
            Assert.IsTrue(puzzle.LoopGauge(0).enabled, "The needle animates towards the new pressure.");
            StringAssert.Contains("2.4", puzzle.LoopGauge(0).Readout);
            Assert.AreEqual(PressureGauge.Calm, puzzle.LoopGauge(0).ReadoutColour);
            StringAssert.Contains("B = C", puzzle.StatusLine);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(puzzle.LoopGauge(0).enabled, "The gauge stops updating once the needle settles.");

            Assert.IsNotNull(GameObject.Find("DiagramPlaque"), "A diagram plaque explains the loops.");
            var note = GameObject.Find("Uv_Kaye");
            Assert.IsNotNull(note, "Kaye's hint is painted on the shaft wall.");
            Assert.IsNotNull(note.GetComponent<BeamRevealText>(), "…and only shows in the flashlight beam.");
        }

        [UnityTest]
        public IEnumerator Overshoot_RaisesAlarm_AndResetsToMid()
        {
            var puzzle = Object.FindAnyObjectByType<ReactorBalancePuzzle>();
            Valve(1).Interact(null); // H M M : 55
            Valve(2).Interact(null); // H H M : 78 → overshoot
            Assert.IsTrue(puzzle.Balance.IsOvershoot);
            Assert.IsTrue(puzzle.IsResetting);
            Assert.IsFalse(Valve(3).IsInteractionEnabled, "Valves do not respond during the reset.");
            Time.timeScale = 4f;
            float t = 0f;
            while (puzzle.IsResetting && t < 3f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsFalse(puzzle.IsResetting);
            Assert.AreEqual(ReactorBalance.Mid, puzzle.Balance.Get(0));
            Assert.IsFalse(puzzle.IsSolved);
        }

        [Test]
        public void PrivateArchive_StaysSealed_UntilCairnsPrivateLogWasHeard()
        {
            Assert.IsFalse(Object.FindObjectsByType<AudioLogPickup>(FindObjectsSortMode.None).Any(l => l.Log.Id == "log_core_03"));
        }
    }

    /// <summary>Ending A (StoryBible §9.3): countdown, sealed tunnel + crank at low trust, boarding the Tern, running out of time.</summary>
    public class EscapeTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            var world = GameServices.World;
            world.SetBool("dlg.ai_core_01.played", true);
            world.SetBool(WorldKeys.Power(SectorIds.Reactor), true);
            yield return Phase6.Load(SceneIds.ReactorCommand);
        }

        [TearDown]
        public void Stop()
        {
            EscapeSequence.Instance?.Cancel();
            GameServices.StartNewRun();
        }

        private static SectorHatch Tunnel => Phase6.Find<SectorHatch>(h => h.InteractableId == "hatch_servicetunnel");

        private static void Decide(int trust)
        {
            GameServices.World.SetInt(WorldKeys.AiTrust, trust);
            var decision = Object.FindAnyObjectByType<DecisionScreen>();
            decision.Open();
            decision.Pick(EndingType.Destroy);
            decision.Confirm();
        }

        [Test]
        public void Tunnel_IsSealed_OutsideTheEscape()
        {
            Assert.IsFalse(Tunnel.CanInteract(null, out var reason));
            Assert.AreEqual("reactor.tunnel.sealed", reason);
        }

        [UnityTest]
        public IEnumerator LowTrust_CairnSealsTheTunnel_CrankOpensIt()
        {
            Decide(30);
            Assert.AreEqual(SceneIds.ReactorCommand, SceneManager.GetActiveScene().name, "Destroy starts the escape instead of the ending scene.");
            Assert.IsTrue(EscapeSequence.Instance.IsRunning);
            yield return null;
            Assert.IsFalse(Tunnel.CanInteract(null, out var reason));
            Assert.AreEqual("reactor.tunnel.cairn_sealed", reason);
            var crank = Object.FindAnyObjectByType<CrankInteractable>();
            Assert.IsNotNull(crank, "The crank is shown during a low-trust escape.");
            for (int i = 0; i < 6; i++) crank.Interact(null);
            Assert.IsTrue(crank.IsDone);
            Assert.IsTrue(Tunnel.CanInteract(null, out _));
        }

        [UnityTest]
        public IEnumerator HighTrust_CairnOpensTheTunnel_AndBoardingEndsTheRun()
        {
            Decide(80);
            yield return null;
            Assert.IsTrue(Tunnel.CanInteract(null, out _), "At high trust CAIRN opens every door.");
            Assert.IsNull(Object.FindAnyObjectByType<CrankInteractable>(), "No crank needed.");

            Tunnel.Interact(null);
            yield return Phase6.WaitForScene(SceneIds.DockingBay);
            Assert.IsTrue(EscapeSequence.Instance.IsRunning, "The countdown survives the scene change.");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.Less(player.transform.position.z, -5f, "The tunnel comes out next to the airlock.");
            var board = Object.FindAnyObjectByType<BoardShipInteractable>();
            Assert.IsNotNull(board);
            board.Interact(null);
            yield return Phase6.WaitForScene(SceneIds.Ending, 20f);
            Assert.AreEqual(SceneIds.Ending, SceneManager.GetActiveScene().name);
            Assert.IsTrue(GameServices.World.GetBool(EscapeSequence.EscapedFlag));
            Assert.IsFalse(EscapeSequence.Instance.IsRunning);
            var lines = EndingScript.Lines(EndingType.Destroy, 80, escapedLive: true);
            Assert.IsFalse(lines.Any(l => l.Key == "end.a.01"), "Lines already played live are not repeated.");
        }

        [UnityTest]
        public IEnumerator RunningOutOfTime_RestartsTheEscape()
        {
            Decide(30);
            EscapeSequence.Instance.SetRemaining(0.2f);
            float t = 0f;
            yield return null;
            while ((SceneManager.GetActiveScene().name != SceneIds.ReactorCommand || GameServices.Scenes.IsLoading || t < 1f) && t < 12f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // fail screen (1.5 s) + message (4 s) + reload
            while (t < 9f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return Phase6.WaitForScene(SceneIds.ReactorCommand);
            Assert.IsTrue(EscapeSequence.Instance.IsRunning, "The escape re-arms after the reload.");
            Assert.Greater(EscapeSequence.Instance.Remaining, 100f, "A fresh countdown.");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.Greater(player.transform.position.y, 15f, "Restarts at Command.");
        }
    }
}
