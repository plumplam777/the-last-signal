using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using LastSignal.Database;
using LastSignal.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LastSignal.UI.RuntimeUi;

namespace LastSignal.UI
{
    /// <summary>
    /// Administrator panel (main menu): full CRUD for Users, Sectors and Items plus a read-only Progress overview.
    /// Every action re-checks the admin role; delete needs a second click; all SQL goes through the repositories.
    /// </summary>
    public sealed class AdminPanel : MonoBehaviour
    {
        private enum Tab { Users, Sectors, Items, Progress }

        public event Action Closed;
        public bool IsOpen => _root != null && _root.activeSelf;

        private GameObject _root;
        private Tab _tab;
        private RectTransform _list;
        private TMP_Text _feedback, _info, _formTitle;
        private Button _saveButton, _deleteButton;
        private readonly Dictionary<Tab, Button> _tabs = new();
        private readonly List<GameObject> _formRows = new();
        private Transform _form;
        private string _selectedId; // null = create mode
        private bool _deleteArmed;

        // form controls (rebuilt per tab)
        private TMP_InputField _f1, _f2, _f3, _f4, _f5;
        private Toggle _toggle;
        private Button _cycle;
        private int _cycleIndex;
        private UserRole _role;

        private DatabaseManager Db => GameServices.Database;

        public void Open()
        {
            if (!GameServices.Auth.IsAdmin) return;
            if (_root == null) Build();
            _root.SetActive(true);
            SelectTab(Tab.Users);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            Closed?.Invoke();
        }

        private void Build()
        {
            var canvas = Canvas("AdminPanelUI", transform, 20).transform;
            _root = canvas.gameObject;
            Image("Dim", canvas, Vector2.zero, new Vector2(4000, 3000), new Color(0, 0, 0, 0.75f));
            var frame = Panel("Frame", canvas, Vector2.zero, new Vector2(1640, 900)).transform;
            Text("Title", frame, new Vector2(-560, 405), new Vector2(480, 50), 34, Brass, "ui.admin.title", TextAlignmentOptions.Left).characterSpacing = 10;
            Button("Close", frame, new Vector2(730, 405), new Vector2(140, 46), "ui.admin.close", Close, 20);

            float x = -150;
            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                var tab = t;
                _tabs[t] = Button($"Tab_{t}", frame, new Vector2(x, 405), new Vector2(200, 46), $"ui.admin.tab_{t.ToString().ToLowerInvariant()}", () => SelectTab(tab), 21);
                x += 210;
            }

            _list = ScrollList("List", frame, new Vector2(-520, -20), new Vector2(520, 740));
            var formPanel = Image("FormPanel", frame, new Vector2(270, -20), new Vector2(1000, 740), new Color(0.04f, 0.06f, 0.09f, 1f));
            _form = formPanel.transform;
            _formTitle = Text("FormTitle", _form, new Vector2(0, 330), new Vector2(940, 44), 26, Cyan, null, TextAlignmentOptions.Left);
            _info = Text("Info", _form, new Vector2(0, -200), new Vector2(940, 220), 19, TealLight, null, TextAlignmentOptions.TopLeft);
            _feedback = Text("Feedback", frame, new Vector2(270, -420), new Vector2(1000, 40), 21, Red);
            Button("New", _form, new Vector2(-330, -320), new Vector2(240, 56), "ui.admin.new", NewRecord, 22);
            _saveButton = Button("Save", _form, new Vector2(-70, -320), new Vector2(240, 56), "ui.admin.save", SaveRecord, 22);
            _deleteButton = Button("Delete", _form, new Vector2(190, -320), new Vector2(240, 56), "ui.admin.delete", DeleteRecord, 22);
            _deleteButton.GetComponent<Image>().color = new Color(0.25f, 0.08f, 0.08f, 1f);
        }

        private void SelectTab(Tab tab)
        {
            if (!GameServices.Auth.IsAdmin) { Close(); return; }
            _tab = tab;
            foreach (var kv in _tabs) kv.Value.interactable = kv.Key != tab;
            BuildForm();
            RefreshList();
            NewRecord();
        }

        // ------------------------------------------------------------------ form

        private void BuildForm()
        {
            foreach (var r in _formRows) Destroy(r);
            _formRows.Clear();
            _f1 = _f2 = _f3 = _f4 = _f5 = null;
            _toggle = null;
            _cycle = null;
            bool readOnly = _tab == Tab.Progress;
            _saveButton.gameObject.SetActive(!readOnly);
            _deleteButton.gameObject.SetActive(!readOnly);
            _form.Find("New").gameObject.SetActive(!readOnly);
            switch (_tab)
            {
                case Tab.Users:
                    _f1 = Field("ui.admin.f_username", 250);
                    _f2 = Field("ui.admin.f_new_password", 170, password: true);
                    _cycle = CycleField("ui.admin.f_role", 90, CycleRole);
                    break;
                case Tab.Sectors:
                    _f1 = Field("ui.admin.f_id", 250);
                    _f2 = Field("ui.admin.f_name", 190);
                    _f3 = Field("ui.admin.f_description", 130);
                    _f4 = Field("ui.admin.f_scene", 70);
                    _f5 = Field("ui.admin.f_order", 10);
                    _toggle = AddToggle("ui.admin.f_unlocked", -50);
                    break;
                case Tab.Items:
                    _f1 = Field("ui.admin.f_id", 250);
                    _f2 = Field("ui.admin.f_name", 190);
                    _f3 = Field("ui.admin.f_description", 130);
                    _cycle = CycleField("ui.admin.f_type", 70, CycleType);
                    _f5 = Field("ui.admin.f_access", 10);
                    _f5.contentType = TMP_InputField.ContentType.IntegerNumber;
                    break;
            }
        }

        private TMP_InputField Field(string labelKey, float y, bool password = false)
        {
            var label = Text("Label", _form, new Vector2(-330, y), new Vector2(280, 40), 21, TextColor, labelKey, TextAlignmentOptions.Right);
            var input = Input("Input", _form, new Vector2(150, y), new Vector2(620, 50), "ui.admin.placeholder", password);
            _formRows.Add(label.gameObject);
            _formRows.Add(input.gameObject);
            return input;
        }

        private Button CycleField(string labelKey, float y, Action onClick)
        {
            var label = Text("Label", _form, new Vector2(-330, y), new Vector2(280, 40), 21, TextColor, labelKey, TextAlignmentOptions.Right);
            var b = Button("Cycle", _form, new Vector2(150, y), new Vector2(620, 50), null, () => onClick(), 22);
            _formRows.Add(label.gameObject);
            _formRows.Add(b.gameObject);
            return b;
        }

        private Toggle AddToggle(string key, float y)
        {
            var t = Toggle("Toggle", _form, new Vector2(150, y), key, false, null);
            _formRows.Add(t.gameObject);
            return t;
        }

        private void CycleRole()
        {
            _role = _role == UserRole.Player ? UserRole.Admin : UserRole.Player;
            SetButtonText(_cycle, _role == UserRole.Admin ? Loc.Get("ui.admin.role_admin") : Loc.Get("ui.admin.role_player"));
        }

        private static readonly string[] ItemTypes = Enum.GetNames(typeof(ItemType));

        private void CycleType()
        {
            _cycleIndex = (_cycleIndex + 1) % ItemTypes.Length;
            SetButtonText(_cycle, ItemTypes[_cycleIndex]);
        }

        // ------------------------------------------------------------------ list + selection (Read)

        private void RefreshList()
        {
            Clear(_list);
            if (!Db.IsAvailable) { _feedback.text = Loc.Get("auth.err.no_database"); return; }
            switch (_tab)
            {
                case Tab.Users:
                case Tab.Progress:
                    foreach (var u in Db.Users.All())
                    {
                        var id = u.UserId.ToString();
                        var badge = u.Role == UserRole.Admin ? "<color=#C9A14A>ADMIN</color>  " : string.Empty;
                        ListRow(_list, $"{badge}{u.Username}", () => Select(id));
                    }
                    break;
                case Tab.Sectors:
                    foreach (var s in Db.Sectors.All())
                    {
                        var id = s.SectorId;
                        var flags = (s.IsUnlocked ? " <color=#5FE6FF>●</color>" : string.Empty) + (s.IsBuiltIn ? string.Empty : "  <color=#8A8F93>(custom)</color>");
                        ListRow(_list, $"{s.SortOrder}. {s.SectorName}{flags}", () => Select(id));
                    }
                    break;
                case Tab.Items:
                    foreach (var i in Db.Items.All())
                    {
                        var id = i.ItemId;
                        ListRow(_list, $"{i.ItemName}  <color=#8A8F93>{i.ItemType} · L{i.AccessLevel}{(i.IsBuiltIn ? string.Empty : " · custom")}</color>", () => Select(id));
                    }
                    break;
            }
        }

        private void NewRecord()
        {
            _selectedId = null;
            _deleteArmed = false;
            _feedback.text = string.Empty;
            _info.text = string.Empty;
            _formTitle.text = Loc.Get(_tab == Tab.Progress ? "ui.admin.progress_hint" : "ui.admin.creating");
            foreach (var f in new[] { _f1, _f2, _f3, _f4, _f5 }) if (f != null) { f.text = string.Empty; f.interactable = true; }
            if (_toggle != null) _toggle.isOn = false;
            if (_tab == Tab.Users) { _role = UserRole.Admin; CycleRole(); }
            if (_tab == Tab.Items) { _cycleIndex = ItemTypes.Length - 1; CycleType(); }
            if (_tab == Tab.Sectors && _f5 != null) _f5.text = "10";
            if (_tab == Tab.Items && _f5 != null) _f5.text = "0";
            _deleteButton.interactable = false;
            SetButtonText(_deleteButton, Loc.Get("ui.admin.delete"));
        }

        private void Select(string id)
        {
            NewRecord();
            _selectedId = id;
            _deleteButton.interactable = _tab != Tab.Progress;
            switch (_tab)
            {
                case Tab.Users:
                {
                    var u = Db.Users.FindById(long.Parse(id));
                    if (u == null) { RefreshList(); return; }
                    _formTitle.text = Loc.Get("ui.admin.editing", u.Username);
                    _f1.text = u.Username;
                    _role = u.Role == UserRole.Admin ? UserRole.Player : UserRole.Admin;
                    CycleRole();
                    _info.text = Loc.Get("ui.admin.user_info", u.UserId, u.CreatedAt?.Substring(0, 10), u.LastLoginAt?.Substring(0, 16).Replace('T', ' ') ?? "—", u.PreferredLanguage);
                    break;
                }
                case Tab.Sectors:
                {
                    var s = Db.Sectors.Find(id);
                    if (s == null) { RefreshList(); return; }
                    _formTitle.text = Loc.Get("ui.admin.editing", s.SectorName);
                    _f1.text = s.SectorId;
                    _f1.interactable = false; // primary key
                    _f2.text = s.SectorName;
                    _f3.text = s.Description;
                    _f4.text = s.SceneName;
                    _f5.text = s.SortOrder.ToString();
                    _toggle.isOn = s.IsUnlocked;
                    _deleteButton.interactable = !s.IsBuiltIn;
                    _info.text = Loc.Get(s.IsBuiltIn ? "ui.admin.builtin_sector" : "ui.admin.custom_sector");
                    break;
                }
                case Tab.Items:
                {
                    var i = Db.Items.Find(id);
                    if (i == null) { RefreshList(); return; }
                    _formTitle.text = Loc.Get("ui.admin.editing", i.ItemName);
                    _f1.text = i.ItemId;
                    _f1.interactable = false;
                    _f2.text = i.ItemName;
                    _f3.text = i.Description;
                    _cycleIndex = Math.Max(0, Array.IndexOf(ItemTypes, i.ItemType)) - 1;
                    CycleType();
                    _f5.text = i.AccessLevel.ToString();
                    _info.text = Loc.Get(i.IsBuiltIn ? "ui.admin.builtin_item" : "ui.admin.custom_item");
                    break;
                }
                case Tab.Progress:
                {
                    var u = Db.Users.FindById(long.Parse(id));
                    var p = Db.Progress.Find(u.UserId);
                    _formTitle.text = u.Username;
                    if (p == null) { _info.text = Loc.Get("ui.admin.no_progress"); break; }
                    var choices = string.Join("\n", Db.Progress.Choices(u.UserId).Select(c => $"  {c.name} = {c.value}  <color=#8A8F93>{c.at?.Substring(0, 16).Replace('T', ' ')}</color>"));
                    _info.text = Loc.Get("ui.admin.progress_info", p.CurrentSector, p.CurrentObjective ?? "—", p.LogsCollected, p.PlaytimeSeconds / 60, p.AiTrust,
                        p.EndingReached ?? "—", p.LastSavedAt?.Substring(0, 16).Replace('T', ' ')) + (choices.Length > 0 ? "\n\n" + choices : string.Empty);
                    _info.rectTransform.anchoredPosition = new Vector2(0, 60);
                    _info.rectTransform.sizeDelta = new Vector2(940, 500);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ Create / Update / Delete

        private void SaveRecord()
        {
            if (!GameServices.Auth.IsAdmin) { Close(); return; }
            string err = null;
            try
            {
                switch (_tab)
                {
                    case Tab.Users: err = SaveUser(); break;
                    case Tab.Sectors: err = SaveSector(); break;
                    case Tab.Items: err = SaveItem(); break;
                }
            }
            catch (SqliteException e)
            {
                Log.Error(nameof(AdminPanel), e.Message);
                err = "ui.admin.err_db";
            }
            Feedback(err);
        }

        private string SaveUser()
        {
            var auth = GameServices.Auth;
            if (_selectedId == null)
            {
                var e = auth.Register(_f1.text, _f2.text, _f2.text, _role, out var newId);
                if (e != null) return e;
                RefreshList();
                Select(newId.ToString());
                return null;
            }
            var user = Db.Users.FindById(long.Parse(_selectedId));
            if (user == null) return "auth.err.not_found";
            var name = _f1.text.Trim();
            if (!string.Equals(name, user.Username, StringComparison.Ordinal))
            {
                var e = AuthService.ValidateUsername(name);
                if (e != null) return e;
                var other = Db.Users.FindByName(name);
                if (other != null && other.UserId != user.UserId) return "auth.err.username_taken";
                Db.Users.UpdateUsername(user.UserId, name);
            }
            if (_role != user.Role)
            {
                var e = auth.CanChangeRole(user, _role);
                if (e != null) return e;
                Db.Users.UpdateRole(user.UserId, _role);
            }
            if (!string.IsNullOrEmpty(_f2.text))
            {
                var e = auth.ChangePassword(user.UserId, _f2.text);
                if (e != null) return e;
            }
            RefreshList();
            Select(_selectedId);
            return null;
        }

        private string SaveSector()
        {
            var id = _f1.text.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_]{2,24}$")) return "ui.admin.err_id";
            if (string.IsNullOrWhiteSpace(_f2.text)) return "ui.admin.err_name";
            if (!int.TryParse(_f5.text, out var order)) order = 0;
            var record = new SectorRecord
            {
                SectorId = id, SectorName = _f2.text.Trim(), Description = _f3.text.Trim(), SceneName = _f4.text.Trim(),
                SortOrder = order, IsUnlocked = _toggle.isOn,
            };
            if (_selectedId == null)
            {
                if (Db.Sectors.Find(id) != null) return "ui.admin.err_exists";
                Db.Sectors.Create(record);
            }
            else Db.Sectors.Update(record);
            RefreshList();
            Select(id);
            return null;
        }

        private string SaveItem()
        {
            var id = _f1.text.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_]{2,32}$")) return "ui.admin.err_id";
            if (string.IsNullOrWhiteSpace(_f2.text)) return "ui.admin.err_name";
            if (!int.TryParse(_f5.text, out var access) || access < 0 || access > 9) return "ui.admin.err_access";
            var record = new ItemRecord { ItemId = id, ItemName = _f2.text.Trim(), Description = _f3.text.Trim(), ItemType = ItemTypes[_cycleIndex], AccessLevel = access };
            if (_selectedId == null)
            {
                if (Db.Items.Find(id) != null) return "ui.admin.err_exists";
                Db.Items.Create(record);
            }
            else Db.Items.Update(record);
            RefreshList();
            Select(id);
            return null;
        }

        private void DeleteRecord()
        {
            if (!GameServices.Auth.IsAdmin || _selectedId == null) return;
            if (!_deleteArmed)
            {
                _deleteArmed = true;
                SetButtonText(_deleteButton, Loc.Get("ui.admin.delete_confirm"));
                return;
            }
            string err = null;
            switch (_tab)
            {
                case Tab.Users:
                {
                    var u = Db.Users.FindById(long.Parse(_selectedId));
                    err = u == null ? "auth.err.not_found" : GameServices.Auth.CanDelete(u);
                    if (err == null)
                    {
                        GameServices.Saves.DeleteSave(u.UserId);
                        Db.Users.Delete(u.UserId);
                    }
                    break;
                }
                case Tab.Sectors:
                    err = Db.Sectors.Delete(_selectedId) ? null : "ui.admin.err_builtin";
                    break;
                case Tab.Items:
                    err = Db.Items.Delete(_selectedId) ? null : "auth.err.not_found";
                    break;
            }
            if (err == null)
            {
                RefreshList();
                NewRecord();
                Feedback(null, "ui.admin.deleted");
            }
            else
            {
                _deleteArmed = false;
                SetButtonText(_deleteButton, Loc.Get("ui.admin.delete"));
                Feedback(err);
            }
        }

        private void Feedback(string errorKey, string okKey = "ui.admin.saved")
        {
            _feedback.color = errorKey == null ? Cyan : Red;
            _feedback.text = Loc.Get(errorKey ?? okKey);
            GameServices.Audio.Sfx(errorKey == null ? "sfx_terminal_ok" : "sfx_terminal_error", 0.7f);
        }

        private void Update()
        {
            if (IsOpen && GameServices.Input.Controls.UI.Cancel.WasPressedThisFrame()) Close();
        }
    }
}
