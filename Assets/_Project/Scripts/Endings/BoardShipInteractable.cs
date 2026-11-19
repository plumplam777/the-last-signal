using LastSignal.Interaction;

namespace LastSignal.Endings
{
    /// <summary>The Tern's airlock during the Ending A escape: boarding ends the run.</summary>
    public sealed class BoardShipInteractable : Interactable
    {
        public override bool IsInteractionEnabled
            => base.IsInteractionEnabled && EscapeSequence.Instance != null && EscapeSequence.Instance.IsRunning;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
        }

        protected override void OnInteract(PlayerInteractor interactor) => EscapeSequence.Instance.Board();
    }
}
