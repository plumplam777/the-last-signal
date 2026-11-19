using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>One key of the decode console: a symbol, or the Clear / Submit key.</summary>
    public sealed class SymbolKeyInteractable : Interactable
    {
        public const string ClearKey = "clear", SubmitKey = "submit";

        [SerializeField] private SymbolDecodePuzzle puzzle;
        [SerializeField] private string symbol;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && puzzle != null && !puzzle.IsSolved;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
        }

        public override string GetPromptText() => symbol switch
        {
            ClearKey => Loc.Get("prompt.decode.clear"),
            SubmitKey => Loc.Get("prompt.decode.submit"),
            _ => Loc.Get("prompt.decode.key", SymbolDecodePuzzle.Glyph(symbol)),
        };

        protected override void OnInteract(PlayerInteractor interactor)
        {
            switch (symbol)
            {
                case ClearKey: puzzle.Clear(); break;
                case SubmitKey: puzzle.Submit(); break;
                default: puzzle.Press(symbol); break;
            }
        }

#if UNITY_EDITOR
        public void EditorSetKey(SymbolDecodePuzzle p, string s)
        {
            puzzle = p;
            symbol = s;
        }
#endif
    }
}
