using System.Collections;
using LastSignal.Core;
using LastSignal.Cutscene;
using LastSignal.Endings;
using LastSignal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Opening cutscene → Docking Bay; decision screen → Ending scene → main menu.</summary>
    public class StoryFlowTests
    {
        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private static IEnumerator WaitForScene(string scene, float realSeconds)
        {
            float t = 0f;
            while ((SceneManager.GetActiveScene().name != scene || GameServices.Scenes.IsLoading) && t < realSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpeningCutscene_PlaysAllShots_ThenLoadsDockingBay()
        {
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.CutsceneOpening);
            yield return null;
            yield return null;
            Assert.AreEqual(GameState.Cutscene, GameServices.State.Current);
            var director = Object.FindAnyObjectByType<CutsceneDirector>();
            Assert.IsNotNull(director);
            Time.timeScale = 40f; // ~81 s of shots
            yield return WaitForScene(SceneIds.DockingBay, 30f);
            Time.timeScale = 1f;
            Assert.AreEqual(SceneIds.DockingBay, SceneManager.GetActiveScene().name);
            Assert.AreEqual(GameState.Playing, GameServices.State.Current);
        }

        [UnityTest]
        public IEnumerator Decision_HiddenOptionLocked_ConfirmLoadsEnding_ThenMainMenu()
        {
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return null;
            yield return null;
            GameServices.World.SetInt(WorldKeys.AiTrust, 80);
            DecisionScreen.RequestOpen();
            yield return null;
            var screen = Object.FindAnyObjectByType<DecisionScreen>();
            Assert.IsTrue(screen.IsOpen);
            screen.Pick(EndingType.Silence); // not available without 14 logs → ignored
            Assert.AreEqual(string.Empty, GameServices.World.GetString(EndingScript.ChoiceKey, string.Empty));
            // B goes straight to the ending scene (A is played as the escape run: see EscapeTests)
            screen.Pick(EndingType.Restore);
            screen.Confirm();
            Assert.AreEqual("restore", GameServices.World.GetString(EndingScript.ChoiceKey));
            yield return WaitForScene(SceneIds.Ending, 10f);
            Assert.AreEqual(SceneIds.Ending, SceneManager.GetActiveScene().name);
            var ending = Object.FindAnyObjectByType<EndingController>();
            Assert.AreEqual(EndingType.Restore, ending.Type);

            Time.timeScale = 60f;
            yield return WaitForScene(SceneIds.MainMenu, 30f);
            Time.timeScale = 1f;
            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name, "Ending should return to the main menu.");
        }
    }
}
