using System.Collections;
using LastSignal.Core;
using LastSignal.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>CAIRN conversations: triggers, choices, trust deltas, once-only, trust-dependent add-on lines.</summary>
    public class DialogueTests
    {
        private static DialogueManager D => GameServices.Dialogue;

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 5f)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator Load(string scene)
        {
            GameServices.StartNewRun();
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PowerOn_PlaysAiDock01_ChoiceRaisesTrust_AndReturnsControl()
        {
            yield return Load(SceneIds.DockingBay);
            GameServices.World.SetBool(WorldKeys.Power(SectorIds.Docking), true);
            yield return WaitFor(() => D.IsRunning);
            Assert.AreEqual("ai_dock_01", D.Current?.Id);
            D.SkipLine();
            yield return WaitFor(() => D.AwaitingChoice);
            Assert.AreEqual(3, D.CurrentChoices.Count);
            Assert.AreEqual(GameState.InMenu, GameServices.State.Current, "Choices take the cursor.");

            D.Choose(1); // "Glad to help. What do you need?" (+5)
            Assert.AreEqual(55, GameServices.World.GetInt(WorldKeys.AiTrust));
            Assert.AreEqual("ai_dock_01_c2", GameServices.World.GetString(WorldKeys.Choice("ai_dock_01")));
            Assert.AreEqual(GameState.Playing, GameServices.State.Current);
            Assert.AreEqual("r2", D.CurrentNode.id);
            D.SkipLine();
            yield return WaitFor(() => !D.IsRunning);
            Assert.IsFalse(D.IsRunning);
            Assert.IsFalse(D.Play(null), "null guard");
        }

        [UnityTest]
        public IEnumerator OnceOnly_GraphDoesNotReplay()
        {
            yield return Load(SceneIds.DockingBay);
            var trigger = Object.FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None);
            DialogueGraph dock01 = null;
            foreach (var t in trigger) if (t.Graph.Id == "ai_dock_01") dock01 = t.Graph;
            Assert.IsNotNull(dock01);
            Assert.IsTrue(D.Play(dock01));
            D.StopAll();
            Assert.IsFalse(D.Play(dock01), "Once-only conversation must not replay.");
        }

        [UnityTest]
        public IEnumerator AiEng02_HighTrust_ShowsHighAddOn()
        {
            yield return RunEng02(70);
            Assert.AreEqual("high", _visited);
        }

        [UnityTest]
        public IEnumerator AiEng02_LowTrust_ShowsLowAddOn()
        {
            yield return RunEng02(20);
            Assert.AreEqual("low", _visited);
        }

        private string _visited;

        private IEnumerator RunEng02(int trust)
        {
            yield return Load(SceneIds.Engineering);
            GameServices.World.SetBool("dlg.ai_eng_01.played", true); // skip AI_ENG_01 (sector start)
            D.StopAll();
            GameServices.World.SetInt(WorldKeys.AiTrust, trust);
            _visited = null;
            void OnLine(DialogueNode n) { if (n.id == "high" || n.id == "low") _visited = n.id; }
            D.LineShown += OnLine;
            GameServices.World.SetBool(LastSignal.Audio.AudioLogManager.PlayedKey("log_eng_03"), true);
            yield return WaitFor(() => D.IsRunning && D.Current.Id == "ai_eng_02");
            Assert.AreEqual("ai_eng_02", D.Current?.Id);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("heard_lockout")));
            for (int i = 0; i < 3 && !D.AwaitingChoice; i++)
            {
                D.SkipLine();
                yield return null;
                yield return null;
            }
            yield return WaitFor(() => D.AwaitingChoice);
            Assert.AreEqual(3, D.CurrentChoices.Count);
            D.Choose(2); // refuse interlocks: -10
            Assert.AreEqual(trust - 10, GameServices.World.GetInt(WorldKeys.AiTrust));
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Story("refused_interlock")));
            D.LineShown -= OnLine;
            D.StopAll();
        }
    }
}
