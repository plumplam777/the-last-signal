using System;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Pure logic of a numeric keypad lock (terminal PINs, door keypads). No Unity dependencies so it is unit-testable.
    /// </summary>
    public sealed class CodeLock
    {
        private readonly string _code;

        public int MaxLength { get; }
        public string Entry { get; private set; } = string.Empty;
        public int FailedAttempts { get; private set; }

        public CodeLock(string code)
        {
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("Code must not be empty.", nameof(code));
            _code = code;
            MaxLength = code.Length;
        }

        public void Press(char digit)
        {
            if (!char.IsDigit(digit) || Entry.Length >= MaxLength) return;
            Entry += digit;
        }

        public void Backspace()
        {
            if (Entry.Length > 0) Entry = Entry.Substring(0, Entry.Length - 1);
        }

        public void Clear() => Entry = string.Empty;

        /// <summary>Checks the entry; clears it either way. Returns true when correct.</summary>
        public bool Submit()
        {
            bool ok = Entry == _code;
            if (!ok) FailedAttempts++;
            Entry = string.Empty;
            return ok;
        }
    }
}
