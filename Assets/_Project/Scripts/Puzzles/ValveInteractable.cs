using LastSignal.Core;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>A coolant loop valve on the reactor catwalk: each use turns it one step (Low → Mid → High → Low).</summary>
    public sealed class ValveInteractable : Interactable
    {
        [SerializeField] private ReactorBalancePuzzle puzzle;
        [SerializeField] private int valveIndex;
        [SerializeField] private Transform wheel;
        [SerializeField] private string labelKey;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && puzzle != null && !puzzle.IsSolved && !puzzle.IsResetting;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
            if (puzzle != null) puzzle.Changed += SyncWheel;
            SyncWheel();
        }

        private void OnDestroy()
        {
            if (puzzle != null) puzzle.Changed -= SyncWheel;
        }

        public override string GetPromptText()
            => Loc.Get("prompt.valve", Loc.Get(labelKey), Loc.Get(SettingKey(puzzle != null ? puzzle.Balance.Get(valveIndex) : 1)));

        public static string SettingKey(int s) => s switch
        {
            ReactorBalance.Low => "reactor.setting.low",
            ReactorBalance.High => "reactor.setting.high",
            _ => "reactor.setting.mid",
        };

        protected override void OnInteract(PlayerInteractor interactor)
        {
            GameServices.Audio.SfxAt("sfx_valve", transform.position, 0.9f, 0.7f);
            puzzle.CycleValve(valveIndex);
        }

        private void SyncWheel()
        {
            if (wheel != null && puzzle != null) wheel.localRotation = Quaternion.Euler(0, 0, -120f * puzzle.Balance.Get(valveIndex));
        }

#if UNITY_EDITOR
        public void EditorSetValve(ReactorBalancePuzzle p, int index, Transform w, string label)
        {
            puzzle = p; valveIndex = index; wheel = w; labelKey = label;
        }
#endif
    }
}
