using System.Linq;
using LastSignal.Core;
using LastSignal.Endings;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public class EndingScriptTests
    {
        [Test]
        public void TrustChangesLinesButNotTheEnding()
        {
            var high = EndingScript.Lines(EndingType.Destroy, 80).Select(l => l.Key).ToList();
            var low = EndingScript.Lines(EndingType.Destroy, 10).Select(l => l.Key).ToList();
            Assert.Contains("end.a.02_high", high);
            Assert.Contains("end.a.02_low", low);
            Assert.AreEqual("epi.a_high", high.Last());
            Assert.AreEqual("epi.a_low", low.Last());
            Assert.Contains("end.a.04", high);
            Assert.Contains("end.a.04", low);
        }

        [Test]
        public void EveryEndingHasAnEventAndAnEpilogue()
        {
            foreach (var type in new[] { EndingType.Destroy, EndingType.Restore, EndingType.Silence })
            {
                var lines = EndingScript.Lines(type, 50);
                Assert.AreEqual(1, lines.Count(l => l.Beat == EndingBeat.Event), type.ToString());
                Assert.AreEqual(EndingBeat.Epilogue, lines.Last().Beat, type.ToString());
            }
        }

        [Test]
        public void HiddenEndingNeedsAllFourteenLogs()
        {
            Assert.IsFalse(EndingScript.HiddenEndingAvailable(13));
            Assert.IsTrue(EndingScript.HiddenEndingAvailable(14));
        }

        [Test]
        public void CommitStoresChoice()
        {
            var world = new WorldState();
            EndingScript.Commit(world, EndingType.Silence);
            Assert.AreEqual("silence", world.GetString("choice.final"));
            Assert.AreEqual(EndingType.Silence, EndingScript.FromId(world.GetString("choice.final")));
            Assert.IsTrue(world.GetBool("story.ending_reached"));
        }
    }
}
