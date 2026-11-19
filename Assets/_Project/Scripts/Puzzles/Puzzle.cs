using System;
using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Base for every puzzle. Solved state is the WorldState flag "puzzle.&lt;id&gt;.solved", so it saves with the
    /// world and other systems (doors, quests) can react through conditions without referencing the puzzle.
    /// Subclasses implement the rules and call <see cref="MarkSolved"/> / <see cref="ReportFailure"/>.
    /// </summary>
    public abstract class Puzzle : MonoBehaviour
    {
        [SerializeField] protected string puzzleId;
        [SerializeField] protected string sectorId;
        [Tooltip("Applied once when the puzzle is solved (e.g. restore power, unlock a door).")]
        [SerializeField] protected List<Effect> onSolvedEffects = new();
        [SerializeField] protected UnityEvent onSolved = new();
        [Tooltip("Sound played at the puzzle when solved (Resources/Sfx name, optional).")]
        [SerializeField] protected string solvedSfx;

        public string PuzzleId => puzzleId;
        public string SectorId => sectorId;
        public bool IsSolved => !string.IsNullOrEmpty(puzzleId) && GameServices.World.GetBool(WorldKeys.PuzzleSolved(puzzleId));

        public event Action<Puzzle> Solved;
        /// <summary>Wrong input; argument is a localization key describing the feedback.</summary>
        public event Action<Puzzle, string> Failed;

        protected virtual void OnEnable() => GameServices.Puzzles.Register(this);

        protected virtual void OnDisable()
        {
            if (GameServices.IsReady) GameServices.Puzzles.Unregister(this);
        }

        protected virtual void Start()
        {
            if (string.IsNullOrEmpty(puzzleId)) Log.Error(nameof(Puzzle), $"Puzzle '{name}' has no id.", this);
            if (IsSolved) ApplySolvedState();
        }

        protected void MarkSolved()
        {
            if (IsSolved) return;
            GameServices.World.SetBool(WorldKeys.PuzzleSolved(puzzleId), true);
            Effects.ApplyAll(onSolvedEffects);
            ApplySolvedState();
            if (!string.IsNullOrEmpty(solvedSfx)) GameServices.Audio.SfxAt(solvedSfx, transform.position, 1f, 0f);
            onSolved.Invoke();
            Solved?.Invoke(this);
            GameServices.Puzzles.NotifySolved(this);
        }

        protected void ReportFailure(string feedbackKey)
        {
            Failed?.Invoke(this, feedbackKey);
            GameServices.Puzzles.NotifyFailed(this, feedbackKey);
        }

        /// <summary>Put the world into the solved look (also used when loading a save where it was solved).</summary>
        protected virtual void ApplySolvedState() { }

#if UNITY_EDITOR
        public void EditorSetSolvedSfx(string sfx) => solvedSfx = sfx;

        public void EditorConfigurePuzzle(string id, string sector, List<Effect> effects)
        {
            puzzleId = id;
            sectorId = sector;
            onSolvedEffects = effects ?? new List<Effect>();
        }
#endif
    }
}
