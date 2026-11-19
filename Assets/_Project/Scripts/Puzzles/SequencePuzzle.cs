using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Steps must be triggered in a fixed order (e.g. generator start: coolant → fuel → ignition → load).
    /// A wrong step resets progress and reports failure; no other penalty.
    /// Inputs come from <see cref="SequenceStepInteractable"/>s or from UI.
    /// </summary>
    public sealed class SequencePuzzle : Puzzle
    {
        [SerializeField] private List<string> correctOrder = new();
        [SerializeField] private string wrongStepFeedbackKey = "puzzle.sequence.wrong";

        private int _progress;

        public int Progress => _progress;
        public int Length => correctOrder.Count;
        public IReadOnlyList<string> CorrectOrder => correctOrder;

        /// <summary>(stepId, index accepted)</summary>
        public event Action<string, int> StepAccepted;
        public event Action ProgressReset;

        public bool IsStepDone(string stepId)
        {
            if (IsSolved) return true;
            int i = correctOrder.IndexOf(stepId);
            return i >= 0 && i < _progress;
        }

        /// <summary>Returns true if the step was accepted.</summary>
        public bool Input(string stepId)
        {
            if (IsSolved) return false;
            if (_progress < correctOrder.Count && correctOrder[_progress] == stepId)
            {
                StepAccepted?.Invoke(stepId, _progress);
                _progress++;
                if (_progress >= correctOrder.Count) MarkSolved();
                return true;
            }
            _progress = 0;
            ProgressReset?.Invoke();
            ReportFailure(wrongStepFeedbackKey);
            return false;
        }

        protected override void ApplySolvedState() => _progress = correctOrder.Count;

#if UNITY_EDITOR
        public void EditorSetOrder(List<string> order) => correctOrder = order;
#endif
    }
}
