using UnityEngine;

namespace LastSignal.Inventory
{
    public enum ItemType
    {
        Tool,
        Component,
        Keycard,
        Battery,
        Document,
        AccessCode,
    }

    /// <summary>Static description of an item. The <see cref="Id"/> is what saves and the database store.</summary>
    [CreateAssetMenu(menuName = "Last Signal/Item Definition", fileName = "item_new")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private ItemType type = ItemType.Component;
        [Tooltip("Localization key for the display name, e.g. item.fuse.name")]
        [SerializeField] private string nameKey;
        [SerializeField] private string descriptionKey;
        [SerializeField] private Sprite icon;
        [Tooltip("Keycards: clearance level granted (0 = none).")]
        [SerializeField, Min(0)] private int accessLevel;
        [SerializeField] private bool stackable;
        [Tooltip("Optional model shown in the inspection view.")]
        [SerializeField] private GameObject inspectPrefab;

        public string Id => id;
        public ItemType Type => type;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public int AccessLevel => accessLevel;
        public bool Stackable => stackable;
        public GameObject InspectPrefab => inspectPrefab;

#if UNITY_EDITOR
        /// <summary>Editor-only initialiser used by content builder tools.</summary>
        public void EditorInit(string newId, ItemType newType, string newNameKey, string newDescriptionKey, bool isStackable = false, int newAccessLevel = 0)
        {
            id = newId;
            type = newType;
            nameKey = newNameKey;
            descriptionKey = newDescriptionKey;
            stackable = isStackable;
            accessLevel = newAccessLevel;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) Debug.LogWarning($"[ItemDefinition] '{name}' has no id.", this);
        }
#endif
    }
}
