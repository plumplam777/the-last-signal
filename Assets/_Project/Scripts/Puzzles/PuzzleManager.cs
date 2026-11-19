using System;
using System.Collections.Generic;
using LastSignal.Core;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Registry of the puzzles in the loaded sector plus global solved/failed events (for audio, CAIRN reactions,
    /// analytics). Solved state itself lives in WorldState.
    /// </summary>
    public sealed class PuzzleManager
    {
        private readonly Dictionary<string, Puzzle> _active = new();
        private readonly WorldState _world;

        public event Action<Puzzle> PuzzleSolved;
        public event Action<Puzzle, string> PuzzleFailed;

        public PuzzleManager(WorldState world) => _world = world;

        public IReadOnlyCollection<Puzzle> ActivePuzzles => _active.Values;

        public bool IsSolved(string puzzleId) => _world.GetBool(WorldKeys.PuzzleSolved(puzzleId));

        public Puzzle Find(string puzzleId) => _active.TryGetValue(puzzleId, out var p) ? p : null;

        internal void Register(Puzzle puzzle)
        {
            if (string.IsNullOrEmpty(puzzle.PuzzleId)) return;
            if (_active.TryGetValue(puzzle.PuzzleId, out var existing) && existing != puzzle)
                Log.Warn(nameof(PuzzleManager), $"Duplicate puzzle id '{puzzle.PuzzleId}' ({existing.name} / {puzzle.name}).", puzzle);
            _active[puzzle.PuzzleId] = puzzle;
        }

        internal void Unregister(Puzzle puzzle)
        {
            if (puzzle.PuzzleId != null && _active.TryGetValue(puzzle.PuzzleId, out var p) && p == puzzle)
                _active.Remove(puzzle.PuzzleId);
        }

        internal void NotifySolved(Puzzle puzzle)
        {
            Log.Info(nameof(PuzzleManager), $"Puzzle solved: {puzzle.PuzzleId}");
            PuzzleSolved?.Invoke(puzzle);
        }

        internal void NotifyFailed(Puzzle puzzle, string feedbackKey) => PuzzleFailed?.Invoke(puzzle, feedbackKey);
    }
}
