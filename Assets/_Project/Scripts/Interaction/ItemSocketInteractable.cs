using LastSignal.Core;
using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A slot that needs an item inserted (fuse box, battery slot, keycard reader that keeps the card…).
    /// Consumes the item, shows the "installed" visual and applies effects such as restoring sector power.
    /// One-shot: the installed state is restored from the save.
    /// </summary>
    public sealed class ItemSocketInteractable : Interactable
    {
        [SerializeField] private ItemDefinition requiredItem;
        [SerializeField] private bool consumeItem = true;
        [Tooltip("Enabled once the item is installed (e.g. the fuse model inside the box).")]
        [SerializeField] private GameObject installedVisual;
        [Tooltip("Disabled once installed (e.g. a sparking empty socket).")]
        [SerializeField] private GameObject emptyVisual;
        [Tooltip("Localization key shown when the player lacks the item.")]
        [SerializeField] private string missingItemReasonKey = "prompt.socket.missing";

        public bool IsInstalled => WasUsed;

        protected override void Start()
        {
            oneShot = true;
            if (requiredItem == null) Log.Error(nameof(ItemSocketInteractable), $"'{name}' has no required item.", this);
            ApplyVisual(false);
            base.Start();
        }

        public override string GetPromptText()
        {
            if (requiredItem != null && GameServices.Inventory.Has(requiredItem))
                return Loc.Get("prompt.socket.install", Loc.Get(requiredItem.NameKey));
            return base.GetPromptText();
        }

        public override bool CanInteract(PlayerInteractor interactor, out string reasonKey)
        {
            if (requiredItem == null || !GameServices.Inventory.Has(requiredItem))
            {
                reasonKey = missingItemReasonKey;
                return false;
            }
            return base.CanInteract(interactor, out reasonKey);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (consumeItem) GameServices.Inventory.Remove(requiredItem);
            GameServices.Audio.SfxAt("sfx_fuse_insert", transform.position);
            ApplyVisual(true);
        }

        protected override void RestoreUsedState() => ApplyVisual(true);

        private void ApplyVisual(bool installed)
        {
            if (installedVisual != null) installedVisual.SetActive(installed);
            if (emptyVisual != null) emptyVisual.SetActive(!installed);
        }

#if UNITY_EDITOR
        public void EditorSetSocket(ItemDefinition item, GameObject installed, GameObject empty, string missingReasonKey = null)
        {
            if (!string.IsNullOrEmpty(missingReasonKey)) missingItemReasonKey = missingReasonKey;
            requiredItem = item;
            installedVisual = installed;
            emptyVisual = empty;
        }
#endif
    }
}
