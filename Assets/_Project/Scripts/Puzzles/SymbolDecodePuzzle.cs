using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using TMPro;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Trial 7 decode console in the Research Lab. Symbol keys (<see cref="SymbolKeyInteractable"/>) feed a
    /// <see cref="SymbolSequenceLock"/>; the entry shows on a world-space display. Solving on the first try sets
    /// the "noticed mirror" flag; the second wrong attempt sets the hint flag that triggers CAIRN's hint.
    /// </summary>
    public sealed class SymbolDecodePuzzle : Puzzle
    {
        public const string Triangle = "tri", Circle = "cir", Square = "sqr", Diamond = "dia";

        [SerializeField] private List<string> answer = new();
        [SerializeField] private TMP_Text display;
        [SerializeField] private string noticedMirrorFlag;
        [SerializeField] private string hintFlag;
        [SerializeField] private int hintAfterFailures = 2;

        private SymbolSequenceLock _lock;

        public int FailedAttempts => _lock?.FailedAttempts ?? 0;
        public IReadOnlyList<string> Entry => _lock?.Entry;

        private void Awake() => _lock = new SymbolSequenceLock(answer);

        protected override void Start()
        {
            base.Start();
            Refresh();
        }

        /// <summary>Glyph for a symbol id (▲ ● ■ and a rotated ■ as the diamond, which the font lacks).</summary>
        public static string Glyph(string id) => id switch
        {
            Triangle => "▲",
            Circle => "●",
            Square => "■",
            Diamond => "<rotate=45><size=80%>■</size></rotate>",
            _ => "?",
        };

        public static string Row(IEnumerable<string> ids) => string.Join("  ", ids.Select(Glyph));

        public void Press(string symbol)
        {
            if (IsSolved) return;
            _lock.Press(symbol);
            GameServices.Audio.SfxAt("sfx_terminal_key", transform.position, 0.7f, 0.5f);
            Refresh();
        }

        public void Clear()
        {
            if (IsSolved) return;
            _lock.Clear();
            Refresh();
        }

        public void Submit()
        {
            if (IsSolved) return;
            if (!_lock.IsFull)
            {
                ReportFailure("res.decode.incomplete");
                return;
            }
            if (_lock.Submit())
            {
                if (_lock.FailedAttempts == 0 && !string.IsNullOrEmpty(noticedMirrorFlag)) GameServices.World.SetBool(noticedMirrorFlag, true);
                GameServices.Audio.SfxAt("sfx_terminal_ok", transform.position, 1f, 0.5f);
                MarkSolved();
            }
            else
            {
                GameServices.Audio.SfxAt("sfx_terminal_error", transform.position, 1f, 0.5f);
                if (_lock.FailedAttempts >= hintAfterFailures && !string.IsNullOrEmpty(hintFlag)) GameServices.World.SetBool(hintFlag, true);
                ReportFailure("res.decode.wrong");
            }
            Refresh();
        }

        protected override void ApplySolvedState() => Refresh();

        private void Refresh()
        {
            if (display == null || _lock == null) return;
            if (IsSolved)
            {
                display.text = Row(answer);
                display.color = new Color(0.5f, 1f, 0.85f);
                return;
            }
            var slots = new List<string>();
            for (int i = 0; i < _lock.Length; i++) slots.Add(i < _lock.Entry.Count ? Glyph(_lock.Entry[i]) : "_");
            display.text = string.Join("  ", slots);
        }

#if UNITY_EDITOR
        public void EditorSetDecode(List<string> newAnswer, TMP_Text text, string mirrorFlag, string hint)
        {
            answer = newAnswer;
            display = text;
            noticedMirrorFlag = mirrorFlag;
            hintFlag = hint;
        }
#endif
    }
}
