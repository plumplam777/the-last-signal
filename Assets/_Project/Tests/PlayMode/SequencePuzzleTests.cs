using System.Collections;
using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Puzzles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public class SequencePuzzleTests
    {
        private GameObject _go;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameServices.StartNewRun();
            _go = new GameObject("GeneratorStart");
            _go.SetActive(false);
            var puzzle = _go.AddComponent<SequencePuzzle>();
            puzzle.EditorConfigurePuzzle("test_generator_start", SectorIds.Engineering,
                new List<Effect> { Effect.Power(SectorIds.Engineering) });
            puzzle.EditorSetOrder(new List<string> { "coolant", "fuel", "ignition", "load" });
            _go.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_go);
            yield return null;
        }

        [Test]
        public void WrongStep_ResetsAndReportsFailure()
        {
            var puzzle = _go.GetComponent<SequencePuzzle>();
            string feedback = null;
            puzzle.Failed += (_, key) => feedback = key;
            Assert.IsTrue(puzzle.Input("coolant"));
            Assert.IsFalse(puzzle.Input("ignition"));
            Assert.AreEqual(0, puzzle.Progress);
            Assert.AreEqual("puzzle.sequence.wrong", feedback);
            Assert.IsFalse(puzzle.IsSolved);
        }

        [Test]
        public void CorrectOrder_SolvesAndAppliesEffects()
        {
            var puzzle = _go.GetComponent<SequencePuzzle>();
            Puzzle solved = null;
            GameServices.Puzzles.PuzzleSolved += p => solved = p;
            foreach (var step in new[] { "coolant", "fuel", "ignition", "load" }) puzzle.Input(step);
            Assert.IsTrue(puzzle.IsSolved);
            Assert.AreEqual(puzzle, solved);
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.PuzzleSolved("test_generator_start")));
            Assert.IsTrue(GameServices.World.GetBool(WorldKeys.Power(SectorIds.Engineering)));
            Assert.IsFalse(puzzle.Input("coolant"), "Solved puzzles ignore input.");
        }
    }
}
