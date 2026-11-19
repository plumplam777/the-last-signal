using LastSignal.Core;
using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Adds an item to the inventory and hides the world object. Always one-shot, so a picked-up
    /// item stays gone after save/load. Optional extra effects (e.g. "has_flashlight") come from the base class.
    /// </summary>
    public sealed class PickupInteractable : Interactable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemDefinition Item => item;

        private void Reset()
        {
            promptKey = "prompt.pickup";
            oneShot = true;
        }

        protected override void Start()
        {
            oneShot = true;
            if (item == null) Log.Error(nameof(PickupInteractable), $"'{name}' has no item assigned.", this);
            if (string.IsNullOrEmpty(objectNameKey) && item != null) objectNameKey = item.NameKey;
            base.Start();
        }

        public override bool CanInteract(PlayerInteractor interactor, out string reasonKey)
        {
            if (item == null)
            {
                reasonKey = null;
                return false;
            }
            if (!item.Stackable && GameServices.Inventory.Has(item))
            {
                reasonKey = "prompt.already_have";
                return false;
            }
            return base.CanInteract(interactor, out reasonKey);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            GameServices.Inventory.Add(item, quantity);
            gameObject.SetActive(false);
        }

        protected override void RestoreUsedState() => gameObject.SetActive(false);

#if UNITY_EDITOR
        public void EditorSetItem(ItemDefinition newItem, int qty = 1)
        {
            item = newItem;
            quantity = qty;
            promptKey = "prompt.pickup";
            objectNameKey = newItem != null ? newItem.NameKey : null;
            oneShot = true;
        }
#endif
    }
}
