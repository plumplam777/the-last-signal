using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A manual door crank: each use turns the handle a step; after <see cref="turnsNeeded"/> turns the completion
    /// effects run (e.g. open the sealed service tunnel). Progress is kept in WorldState ("crank.&lt;id&gt;.turns").
    /// </summary>
    public sealed class CrankInteractable : Interactable
    {
        [SerializeField] private int turnsNeeded = 6;
        [SerializeField] private Transform handle;
        [SerializeField] private List<Effect> onCompleted = new();

        private string TurnsKey => $"crank.{interactableId}.turns";
        private int Turns => GameServices.World.GetInt(TurnsKey);
        public bool IsDone => Turns >= turnsNeeded;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !IsDone;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
            SyncHandle();
        }

        public override string GetPromptText() => Loc.Get("prompt.crank", Turns, turnsNeeded);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            GameServices.World.SetInt(TurnsKey, Turns + 1);
            GameServices.Audio.SfxAt("sfx_crank", transform.position, 1f, 0.7f);
            SyncHandle();
            if (IsDone)
            {
                Effects.ApplyAll(onCompleted);
                GameServices.Audio.SfxAt("sfx_door_open", transform.position, 1f, 0.5f);
            }
        }

        private void SyncHandle()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(0, 0, -60f * Turns);
        }

#if UNITY_EDITOR
        public void EditorSetCrank(int turns, Transform h, List<Effect> completed)
        {
            turnsNeeded = turns;
            handle = h;
            onCompleted = completed ?? new List<Effect>();
        }
#endif
    }
}
