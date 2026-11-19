using System;
using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Puzzles
{
    [Serializable]
    public sealed class TerminalEntry
    {
        public string titleKey;
        public string bodyKey;
        [Tooltip("Entry is listed only when this holds (empty = always).")]
        public ConditionSet visibleWhen = new();
        [Tooltip("Applied the first time the entry is opened (e.g. set a story flag, give an access code).")]
        public List<Effect> onRead = new();
    }

    /// <summary>
    /// Content of a station terminal: header, entries (emails, logs, status pages) and an optional PIN lock.
    /// The lock is a puzzle with id "terminal_&lt;id&gt;", solved state stored in WorldState like any puzzle.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Terminal Definition", fileName = "terminal_new")]
    public sealed class TerminalDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string titleKey;
        [SerializeField] private string subtitleKey;
        [Tooltip("Empty = not locked.")]
        [SerializeField] private string pinCode;
        [SerializeField] private string lockHintKey;
        [SerializeField] private List<Effect> onUnlock = new();
        [SerializeField] private List<TerminalEntry> entries = new();

        public string Id => id;
        public string TitleKey => titleKey;
        public string SubtitleKey => subtitleKey;
        public bool IsLockable => !string.IsNullOrEmpty(pinCode);
        public string PinCode => pinCode;
        public string LockHintKey => lockHintKey;
        public IReadOnlyList<Effect> OnUnlock => onUnlock;
        public IReadOnlyList<TerminalEntry> Entries => entries;
        public string LockPuzzleId => $"terminal_{id}";

        public bool IsUnlocked(WorldState world) => !IsLockable || world.GetBool(WorldKeys.PuzzleSolved(LockPuzzleId));

        public static string ReadKey(string terminalId, int index) => $"terminal.{terminalId}.read.{index}";

#if UNITY_EDITOR
        public void EditorInit(string newId, string title, string subtitle, string pin, string hint, List<TerminalEntry> newEntries)
        {
            id = newId;
            titleKey = title;
            subtitleKey = subtitle;
            pinCode = pin;
            lockHintKey = hint;
            entries = newEntries;
        }
#endif
    }
}
