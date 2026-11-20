using LastSignal.Core;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public class FrameRateGovernorTests
    {
        [Test]
        public void GameplayAndMenuRunAt60_PauseAt30()
        {
            Assert.AreEqual(60, FrameRateGovernor.TargetFor(GameState.Playing, true));
            Assert.AreEqual(60, FrameRateGovernor.TargetFor(GameState.Cutscene, true));
            Assert.AreEqual(60, FrameRateGovernor.TargetFor(GameState.MainMenu, true));
            Assert.AreEqual(30, FrameRateGovernor.TargetFor(GameState.Paused, true));
            Assert.AreEqual(60, FrameRateGovernor.TargetFor(GameState.InMenu, true));
        }
    }
}
