using System;
using System.Collections.Generic;
using System.Linq;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Pure logic of the Research decode console (StoryBible §6.4): the player enters a fixed-length row of symbols
    /// and submits it. No Unity dependencies so it is unit-testable.
    /// </summary>
    public sealed class SymbolSequenceLock
    {
        private readonly string[] _answer;
        private readonly List<string> _entry = new();

        public int Length => _answer.Length;
        public IReadOnlyList<string> Entry => _entry;
        public int FailedAttempts { get; private set; }
        public bool IsFull => _entry.Count >= _answer.Length;

        public SymbolSequenceLock(IEnumerable<string> answer)
        {
            _answer = answer?.ToArray() ?? Array.Empty<string>();
            if (_answer.Length == 0) throw new ArgumentException("Answer must not be empty.", nameof(answer));
        }

        public void Press(string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || IsFull) return;
            _entry.Add(symbol);
        }

        public void Backspace()
        {
            if (_entry.Count > 0) _entry.RemoveAt(_entry.Count - 1);
        }

        public void Clear() => _entry.Clear();

        /// <summary>Checks a full entry; clears it either way. Incomplete entries are ignored (returns false, no failure).</summary>
        public bool Submit()
        {
            if (!IsFull) return false;
            bool ok = _entry.SequenceEqual(_answer);
            if (!ok) FailedAttempts++;
            _entry.Clear();
            return ok;
        }

        /// <summary>The Murmur's rule: it answers with the sent row reversed.</summary>
        public static string[] Mirror(IEnumerable<string> sent) => sent.Reverse().ToArray();
    }
}
