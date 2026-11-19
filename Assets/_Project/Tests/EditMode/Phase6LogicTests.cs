using System.Linq;
using LastSignal.Puzzles;
using NUnit.Framework;

namespace LastSignal.Tests
{
    /// <summary>Pure logic of the Research decode (mirror rule) and the reactor balance (StoryBible §6.4–6.5).</summary>
    public class Phase6LogicTests
    {
        private static readonly string[] Trial7Sent = { "dia", "sqr", "cir", "tri", "cir", "sqr" };

        [Test]
        public void Mirror_ReversesTheSentRow_LikeTheBibleTrials()
        {
            CollectionAssert.AreEqual(new[] { "sqr", "cir", "tri" }, SymbolSequenceLock.Mirror(new[] { "tri", "cir", "sqr" }));
            CollectionAssert.AreEqual(new[] { "tri", "dia", "cir", "cir" }, SymbolSequenceLock.Mirror(new[] { "cir", "cir", "dia", "tri" }));
            CollectionAssert.AreEqual(new[] { "sqr", "cir", "tri", "cir", "sqr", "dia" }, SymbolSequenceLock.Mirror(Trial7Sent));
        }

        [Test]
        public void SymbolLock_AcceptsOnlyTheFullCorrectRow()
        {
            var answer = SymbolSequenceLock.Mirror(Trial7Sent);
            var l = new SymbolSequenceLock(answer);
            foreach (var s in Trial7Sent) l.Press(s); // the sent row, not the answer
            Assert.IsFalse(l.Submit());
            Assert.AreEqual(1, l.FailedAttempts);
            l.Press("sqr");
            Assert.IsFalse(l.Submit(), "Incomplete rows are not checked.");
            Assert.AreEqual(1, l.FailedAttempts, "Incomplete rows do not count as failures.");
            l.Clear();
            foreach (var s in answer) l.Press(s);
            l.Press("tri"); // ignored, already full
            Assert.AreEqual(6, l.Entry.Count);
            Assert.IsTrue(l.Submit());
            Assert.AreEqual(0, l.Entry.Count);
        }

        [Test]
        public void Reactor_IsStable_ExactlyWhenEveryValveIsDifferent()
        {
            var r = new ReactorBalance();
            for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
            for (int c = 0; c < 3; c++)
            {
                r.Set(0, a);
                r.Set(1, b);
                r.Set(2, c);
                bool distinct = new[] { a, b, c }.Distinct().Count() == 3;
                Assert.AreEqual(distinct, r.IsStable, $"L/M/H = {a}{b}{c}: output {r.Output}, temp {r.Temperature}");
                if (r.IsStable) Assert.IsFalse(r.IsOvershoot);
            }
        }

        [Test]
        public void Reactor_StartsSafe_CyclesAndOvershoots()
        {
            var r = new ReactorBalance();
            Assert.IsFalse(r.IsStable);
            Assert.IsFalse(r.IsOvershoot, "Mid/Mid/Mid start must not trip the alarm.");
            r.Set(0, ReactorBalance.High);
            r.Set(1, ReactorBalance.High);
            r.Set(2, ReactorBalance.High);
            Assert.IsTrue(r.IsOvershoot);
            r.Cycle(0);
            Assert.AreEqual(ReactorBalance.Low, r.Get(0), "High wraps to Low.");
            r.Reset();
            Assert.AreEqual(ReactorBalance.Mid, r.Get(2));
        }
    }
}
