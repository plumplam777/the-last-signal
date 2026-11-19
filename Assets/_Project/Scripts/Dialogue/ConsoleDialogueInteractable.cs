using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Dialogue
{
    /// <summary>
    /// A seat/console that starts a CAIRN conversation; optionally opens the final decision screen when the
    /// conversation ends (the captain's console, StoryBible §9.1). Usable again after the conversation played,
    /// then it goes straight to the decision.
    /// </summary>
    public sealed class ConsoleDialogueInteractable : Interactable
    {
        [SerializeField] private DialogueGraph graph;
        [SerializeField] private bool openDecisionAfter;

        private bool _waiting;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !_waiting && !GameServices.Dialogue.IsRunning;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (graph != null && GameServices.Dialogue.Play(graph))
            {
                if (!openDecisionAfter) return;
                _waiting = true;
                GameServices.Dialogue.Ended += OnEnded;
                return;
            }
            if (openDecisionAfter) LastSignal.UI.DecisionScreen.RequestOpen();
        }

        private void OnEnded(DialogueGraph ended)
        {
            if (ended != graph) return;
            GameServices.Dialogue.Ended -= OnEnded;
            _waiting = false;
            LastSignal.UI.DecisionScreen.RequestOpen();
        }

        private void OnDestroy()
        {
            if (_waiting && GameServices.IsReady) GameServices.Dialogue.Ended -= OnEnded;
        }

#if UNITY_EDITOR
        public void EditorSetDialogue(DialogueGraph g, bool decisionAfter)
        {
            graph = g;
            openDecisionAfter = decisionAfter;
        }
#endif
    }
}
