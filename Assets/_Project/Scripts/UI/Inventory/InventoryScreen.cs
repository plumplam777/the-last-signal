using System.Collections.Generic;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Q opens the inventory: an Items tab (select an item to read its description and inspect it in 3D)
    /// and a Logs tab (collected audio logs, replay, x/14 counter). Rows are cloned from templates at runtime;
    /// the screen never stores items itself — it reads InventoryManager / AudioLogManager.
    /// </summary>
    public sealed class InventoryScreen : UiScreen
    {
        [Header("Tabs")]
        [SerializeField] private Button itemsTab;
        [SerializeField] private Button logsTab;
        [SerializeField] private GameObject itemsPage;
        [SerializeField] private GameObject logsPage;

        [Header("Items")]
        [SerializeField] private Transform itemList;
        [SerializeField] private Button itemTemplate;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemDescription;
        [SerializeField] private ItemInspector inspector;

        [Header("Logs")]
        [SerializeField] private Transform logList;
        [SerializeField] private Button logTemplate;
        [SerializeField] private TMP_Text logCounter;

        private readonly List<GameObject> _rows = new();
        private bool _showingLogs;

        protected override void Awake()
        {
            base.Awake();
            itemTemplate.gameObject.SetActive(false);
            logTemplate.gameObject.SetActive(false);
            itemsTab.onClick.AddListener(() => ShowTab(false));
            logsTab.onClick.AddListener(() => ShowTab(true));
        }

        protected override void Update()
        {
            var controls = GameServices.Input.Controls;
            if (!IsOpen && GameServices.State.IsGameplayActive && controls.Interaction.Inventory.WasPressedThisFrame())
            {
                TryOpen();
                return;
            }
            if (IsOpen && controls.UI.ToggleInventory.WasPressedThisFrame())
            {
                Close();
                return;
            }
            base.Update();
        }

        protected override void OnOpened()
        {
            GameServices.Inventory.Changed += OnInventoryChanged;
            GameServices.AudioLogs.Started += OnLogChanged;
            GameServices.AudioLogs.Finished += OnLogChanged;
            GameServices.Localization.LanguageChanged += Rebuild;
            ShowTab(_showingLogs);
        }

        protected override void OnClosing()
        {
            GameServices.Inventory.Changed -= OnInventoryChanged;
            GameServices.AudioLogs.Started -= OnLogChanged;
            GameServices.AudioLogs.Finished -= OnLogChanged;
            GameServices.Localization.LanguageChanged -= Rebuild;
            inspector.Clear();
        }

        private void OnInventoryChanged(ItemDefinition _, int __) => Rebuild();
        private void OnLogChanged(AudioLogDefinition _) => Rebuild();

        private void ShowTab(bool logs)
        {
            _showingLogs = logs;
            itemsPage.SetActive(!logs);
            logsPage.SetActive(logs);
            itemsTab.interactable = logs;
            logsTab.interactable = !logs;
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();
            if (_showingLogs) BuildLogs();
            else BuildItems();
        }

        private void BuildItems()
        {
            var items = GameServices.Inventory.Items;
            emptyText.gameObject.SetActive(items.Count == 0);
            foreach (var stack in items)
            {
                var def = stack.Definition;
                var row = Instantiate(itemTemplate, itemList);
                row.gameObject.SetActive(true);
                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = stack.Quantity > 1 ? $"{Loc.Get(def.NameKey)}  ×{stack.Quantity}" : Loc.Get(def.NameKey);
                var icon = row.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = def.Icon;
                    icon.color = def.Icon != null ? Color.white : TypeColor(def.Type);
                }
                row.onClick.AddListener(() => Select(def));
                _rows.Add(row.gameObject);
            }
            var selected = GameServices.Inventory.Selected;
            if (selected == null && items.Count > 0) selected = items[0].Definition;
            Select(selected);
        }

        private void Select(ItemDefinition def)
        {
            GameServices.Inventory.Select(def);
            itemName.text = def != null ? Loc.Get(def.NameKey) : string.Empty;
            itemDescription.text = def != null ? Loc.Get(def.DescriptionKey) : string.Empty;
            inspector.Show(def != null ? def.InspectPrefab : null);
        }

        private void BuildLogs()
        {
            var logs = GameServices.AudioLogs;
            logCounter.text = Loc.Get("ui.inventory.log_counter", logs.CollectedCount, Mathf.Max(logs.HiddenEndingTotal, 14));
            foreach (var log in logs.CollectedLogs)
            {
                var row = Instantiate(logTemplate, logList);
                row.gameObject.SetActive(true);
                bool playing = logs.Current == log;
                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = $"{(playing ? "■" : "»")}  {Loc.Get(log.TitleKey)}\n<size=70%><color=#C9A14A>{Loc.Get(log.SpeakerKey)}</color>  ·  {Loc.Get(log.DateKey)}</size>";
                var captured = log;
                row.onClick.AddListener(() =>
                {
                    if (GameServices.AudioLogs.Current == captured) GameServices.AudioLogs.Stop();
                    else GameServices.AudioLogs.Play(captured);
                });
                _rows.Add(row.gameObject);
            }
        }

        private static Color TypeColor(ItemType type) => type switch
        {
            ItemType.Keycard => new Color(0.37f, 0.9f, 1f),
            ItemType.Tool => new Color(0.79f, 0.63f, 0.29f),
            ItemType.Component => new Color(0.72f, 0.4f, 0.29f),
            _ => new Color(0.6f, 0.65f, 0.7f),
        };

#if UNITY_EDITOR
        public void EditorBind(GameObject p, Button iTab, Button lTab, GameObject iPage, GameObject lPage, Transform iList,
            Button iTemplate, TMP_Text empty, TMP_Text name, TMP_Text desc, ItemInspector insp, Transform lList, Button lTemplate, TMP_Text counter)
        {
            panel = p; itemsTab = iTab; logsTab = lTab; itemsPage = iPage; logsPage = lPage; itemList = iList; itemTemplate = iTemplate;
            emptyText = empty; itemName = name; itemDescription = desc; inspector = insp; logList = lList; logTemplate = lTemplate; logCounter = counter;
        }
#endif
    }
}
