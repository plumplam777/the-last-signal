using System.Collections;
using System.Linq;
using LastSignal.Core;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A lift inside one scene (reactor shaft ↔ Command): moves the player to a spawn point in the same scene behind
    /// a short fade. Requirements (e.g. reactor stable) come from the base class.
    /// </summary>
    public sealed class LiftInteractable : Interactable
    {
        [SerializeField] private string targetSpawnId;
        [SerializeField] private string destinationNameKey;
        [SerializeField] private float travelSeconds = 1.6f;

        private bool _moving;

        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !_moving;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
        }

        public override string GetPromptText() => Loc.Get("prompt.lift", Loc.Get(destinationNameKey));

        protected override void OnInteract(PlayerInteractor interactor) => StartCoroutine(Travel());

        private IEnumerator Travel()
        {
            var target = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(s => s.SpawnId == targetSpawnId);
            var player = FindAnyObjectByType<PlayerController>();
            if (target == null || player == null)
            {
                Log.Error(nameof(LiftInteractable), $"Lift '{name}' cannot find spawn '{targetSpawnId}' or the player.", this);
                yield break;
            }
            _moving = true;
            GameServices.Audio.Sfx("sfx_lift", 0.9f);
            FindAnyObjectByType<LastSignal.UI.HudController>()?.ShowToast(Loc.Get("lift.moving", Loc.Get(destinationNameKey)), travelSeconds);
            yield return new WaitForSeconds(travelSeconds);
            player.Teleport(target.transform.position, target.transform.eulerAngles.y);
            _moving = false;
            GameServices.Checkpoint();
        }

#if UNITY_EDITOR
        public void EditorSetLift(string spawnId, string nameKey)
        {
            targetSpawnId = spawnId;
            destinationNameKey = nameKey;
        }
#endif
    }
}
