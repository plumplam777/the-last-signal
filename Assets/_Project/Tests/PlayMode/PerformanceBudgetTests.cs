using System.Collections;
using System.Linq;
using LastSignal.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Performance guards (the M4 laptop ran hot): few shadow-casting point lights per sector, an fps cap that is
    /// actually applied, and the three quality levels the Settings button cycles through.
    /// </summary>
    public class PerformanceBudgetTests
    {
        private static readonly string[] Sectors =
        {
            SceneIds.DockingBay, SceneIds.Engineering, SceneIds.Medical, SceneIds.Research, SceneIds.ReactorCommand,
        };

        /// <summary>Each point-light shadow is 6 cube faces in the shadow atlas; 2 per scene keeps it at 12.</summary>
        private const int MaxShadowedPointLights = 2;

        [UnityTest]
        public IEnumerator SectorStaysWithinShadowBudget([ValueSource(nameof(Sectors))] string scene)
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(scene);
            int shadowed = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(l => l.type == LightType.Point && l.shadows != LightShadows.None);
            Assert.LessOrEqual(shadowed, MaxShadowedPointLights, $"{scene} has {shadowed} shadow-casting point lights");
        }

        [UnityTest]
        public IEnumerator FrameRateIsCappedWhilePlaying()
        {
            GameServices.StartNewRun();
            yield return Phase6.Load(SceneIds.DockingBay);
            GameServices.State.Set(GameState.Playing);
            yield return null;
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(FrameRateGovernor.PlayingFps, Application.targetFrameRate);
            GameServices.State.Set(GameState.Paused);
            yield return null;
            Assert.AreEqual(FrameRateGovernor.MenuFps, Application.targetFrameRate);
            GameServices.State.Set(GameState.Playing);
        }

        [Test]
        public void QualityLevelsAreLowMediumHigh()
        {
            CollectionAssert.AreEqual(new[] { "Low", "Medium", "High" }, QualitySettings.names);
            int before = GameServices.Settings.QualityLevel;
            GameServices.Settings.SetQualityLevel(0);
            Assert.AreEqual(0, QualitySettings.GetQualityLevel());
            Assert.IsNotNull(QualitySettings.renderPipeline, "Low has no URP asset");
            GameServices.Settings.SetQualityLevel(99);
            Assert.AreEqual(2, GameServices.Settings.QualityLevel, "level is clamped");
            GameServices.Settings.SetQualityLevel(before);
        }
    }
}
