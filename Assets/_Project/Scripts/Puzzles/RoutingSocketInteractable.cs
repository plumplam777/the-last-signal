using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>One socket on a <see cref="RoutingBoard"/>: E plugs/unplugs a cable; shows the cable when plugged.</summary>
    public sealed class RoutingSocketInteractable : Interactable
    {
        [SerializeField] private RoutingBoard board;
        [SerializeField] private string socketId;
        [SerializeField] private GameObject cableVisual;
        [SerializeField] private GameObject liveIndicator;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && board != null && !board.IsSolved;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
            if (board == null)
            {
                Log.Error(nameof(RoutingSocketInteractable), $"'{name}' has no board.", this);
                return;
            }
            board.SocketChanged += (_, _) => Refresh();
            board.Solved += _ => Refresh();
            GameServices.World.Reset += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameServices.IsReady) GameServices.World.Reset -= Refresh;
        }

        public override string GetPromptText()
        {
            var socket = board != null ? board.Find(socketId) : null;
            var label = socket != null ? Loc.Get(socket.labelKey) : socketId;
            return Loc.Get(board != null && board.IsPlugged(socketId) ? "prompt.routing.unplug" : "prompt.routing.plug", label, board != null ? board.FreeCables : 0);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (board.Toggle(socketId, out _)) GameServices.Audio.SfxAt("sfx_cable_plug", transform.position);
        }

        public override bool CanInteract(PlayerInteractor interactor, out string reasonKey)
        {
            if (!base.CanInteract(interactor, out reasonKey)) return false;
            if (!board.IsPlugged(socketId) && board.FreeCables <= 0)
            {
                reasonKey = "puzzle.routing.no_cables";
                return false;
            }
            return true;
        }

        private void Refresh()
        {
            if (board == null) return;
            bool plugged = board.IsPlugged(socketId);
            if (cableVisual != null) cableVisual.SetActive(plugged);
            if (liveIndicator != null) liveIndicator.SetActive(plugged && board.IsSolved);
        }

#if UNITY_EDITOR
        public void EditorSet(RoutingBoard b, string id, GameObject cable, GameObject live)
        {
            board = b; socketId = id; cableVisual = cable; liveIndicator = live;
        }
#endif
    }
}
