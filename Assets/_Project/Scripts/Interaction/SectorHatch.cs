using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A hatch/door that moves the player to another sector scene at a named spawn point.
    /// Requirements (e.g. power) come from the base class; the base effects run before the load.
    /// </summary>
    public sealed class SectorHatch : Interactable
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId = "default";
        [Tooltip("Localization key of the destination name shown in the prompt.")]
        [SerializeField] private string destinationNameKey;

        public string TargetScene => targetScene;

        protected override void Start()
        {
            oneShot = false;
            base.Start();
        }

        public override string GetPromptText() => Loc.Get("prompt.hatch.go", Loc.Get(destinationNameKey));

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Log.Error(nameof(SectorHatch), $"'{name}' has no target scene.", this);
                return;
            }
            GameServices.Scenes.Load(targetScene, targetSpawnId);
        }

#if UNITY_EDITOR
        public void EditorSetTarget(string scene, string spawnId, string nameKey)
        {
            targetScene = scene;
            targetSpawnId = spawnId;
            destinationNameKey = nameKey;
        }
#endif
    }
}
