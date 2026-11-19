using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>A world switch/button that feeds one step into a <see cref="SequencePuzzle"/>.</summary>
    public sealed class SequenceStepInteractable : Interactable
    {
        [SerializeField] private SequencePuzzle puzzle;
        [SerializeField] private string stepId;
        [Tooltip("Lit while this step is accepted in the current attempt.")]
        [SerializeField] private GameObject activeIndicator;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && puzzle != null && !puzzle.IsSolved;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
            if (puzzle == null)
            {
                Log.Error(nameof(SequenceStepInteractable), $"'{name}' has no puzzle.", this);
                return;
            }
            puzzle.StepAccepted += (_, _) => Refresh();
            puzzle.ProgressReset += Refresh;
            puzzle.Solved += _ => Refresh();
            Refresh();
        }

        public override bool CanInteract(PlayerInteractor interactor, out string reasonKey)
        {
            if (puzzle.IsStepDone(stepId))
            {
                reasonKey = "puzzle.sequence.already";
                return false;
            }
            return base.CanInteract(interactor, out reasonKey);
        }

        protected override void OnInteract(PlayerInteractor interactor) => puzzle.Input(stepId);

#if UNITY_EDITOR
        public void EditorSetStep(SequencePuzzle p, string id, GameObject indicator)
        {
            puzzle = p; stepId = id; activeIndicator = indicator;
        }
#endif

        private void Refresh()
        {
            if (activeIndicator != null && puzzle != null) activeIndicator.SetActive(puzzle.IsStepDone(stepId));
        }
    }
}
