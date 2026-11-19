using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds Prefabs/Player/Player.prefab: controller, camera, flashlight, HUD, pause menu, event system.</summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("Last Signal/Build Content/Player Prefab")]
        public static GameObject Build()
        {
            var root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;

            var pivot = Group("CameraPivot", root.transform, new Vector3(0f, 1.62f, 0f));
            var camGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Navy * 0.5f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var flash = SpotLight("Flashlight", camGo.transform, new Vector3(0.18f, -0.12f, 0.1f), Quaternion.identity,
                new Color(1f, 0.93f, 0.82f), 6f, 20f, 58f, LightShadows.Soft);
            flash.enabled = false;

            var controller = root.AddComponent<PlayerController>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var flashlight = root.AddComponent<FlashlightController>();
            SetRef(controller, "cameraPivot", pivot.transform);
            SetRef(interactor, "viewCamera", cam);
            SetRef(flashlight, "spotLight", flash);

            BuildHud(root.transform, interactor, flashlight);
            BuildSubtitles(root.transform);
            BuildDialogue(root.transform);
            BuildInventory(root.transform);
            BuildTerminal(root.transform);
            BuildDecision(root.transform);
            BuildPause(root.transform);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(root.transform, false);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PlayerPrefabBuilder] Saved {PrefabPath}");
            return prefab;
        }

        private static void BuildHud(Transform parent, PlayerInteractor interactor, FlashlightController flashlight)
        {
            var canvas = UiCanvas("HUD", parent, 10);
            var rootGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            rootGroup.blocksRaycasts = false;
            var center = new Vector2(0.5f, 0.5f);

            var crosshair = UiImage("Crosshair", canvas.transform, center, Vector2.zero, new Vector2(6, 6), new Color(1, 1, 1, 0.45f));

            var promptRect = UiRect("Prompt", canvas.transform, center, new Vector2(0, -90), new Vector2(900, 90));
            var promptGroup = promptRect.gameObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            var plate = UiImage("Plate", promptRect, center, new Vector2(0, 8), new Vector2(520, 46), new Color(0.05f, 0.07f, 0.11f, 1f));
            var plateOutline = plate.gameObject.AddComponent<Outline>();
            plateOutline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.55f);
            var promptText = UiText("PromptText", promptRect, center, new Vector2(0, 8), new Vector2(900, 46), 26, new Color(0.9f, 0.96f, 1f));
            var reasonText = UiText("ReasonText", promptRect, center, new Vector2(0, -32), new Vector2(900, 36), 21, new Color(1f, 0.62f, 0.55f));
            reasonText.fontStyle = FontStyles.Italic;

            var toastRect = UiRect("Toast", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(900, 60));
            var toastGroup = toastRect.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toastText = UiText("ToastText", toastRect, center, Vector2.zero, new Vector2(900, 60), 28, TealLight);

            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.EditorBind(interactor, rootGroup, crosshair, promptGroup, promptText, reasonText, toastText, toastGroup);
            var click = UiText("ClickToLook", canvas.transform, center, new Vector2(0, 60), new Vector2(900, 50), 30, SignalCyan, "ui.hud.click_to_look");
            click.gameObject.SetActive(false);
            hud.EditorBindClickPrompt(click);

            // objective panel (top-left)
            var obj = UiRect("Objective", canvas.transform, new Vector2(0f, 1f), new Vector2(350, -100), new Vector2(560, 110));
            var objGroup = obj.gameObject.AddComponent<CanvasGroup>();
            UiImage("Accent", obj, new Vector2(0.5f, 0.5f), new Vector2(-272, 0), new Vector2(4, 84), new Color(Brass.r, Brass.g, Brass.b, 0.9f));
            var objHeader = UiText("Header", obj, new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(520, 30), 18, Brass);
            objHeader.alignment = TextAlignmentOptions.Left;
            objHeader.characterSpacing = 10;
            var objText = UiText("Text", obj, new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(520, 60), 24, new Color(0.9f, 0.96f, 1f));
            objText.alignment = TextAlignmentOptions.TopLeft;
            canvas.gameObject.AddComponent<ObjectiveHud>().EditorBind(objGroup, objHeader, objText);

            // flashlight panel (bottom-left): key hint + battery
            var fl = UiRect("Flashlight", canvas.transform, new Vector2(0f, 0f), new Vector2(250, 80), new Vector2(420, 80));
            var flGroup = fl.gameObject.AddComponent<CanvasGroup>();
            flGroup.alpha = 0f;
            UiImage("Back", fl, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 80), new Color(0.05f, 0.07f, 0.11f, 0.75f));
            var flLabel = UiText("Label", fl, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(390, 34), 21, new Color(0.9f, 0.96f, 1f));
            flLabel.alignment = TextAlignmentOptions.Left;
            var cellBack = UiImage("BatteryBack", fl, new Vector2(0.5f, 0.5f), new Vector2(-30, -18), new Vector2(330, 12), new Color(1, 1, 1, 0.12f));
            var cellFill = UiImage("BatteryFill", cellBack.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330, 12), SignalCyan);
            cellFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cellFill.type = Image.Type.Filled;
            cellFill.fillMethod = Image.FillMethod.Horizontal;
            var flPct = UiText("Percent", fl, new Vector2(0.5f, 0.5f), new Vector2(170, -18), new Vector2(70, 30), 18, TealLight);
            canvas.gameObject.AddComponent<FlashlightHud>().EditorBind(flashlight, flGroup, flLabel, cellFill, flPct);
        }

        private static void BuildSubtitles(Transform parent)
        {
            // own canvas so subtitles stay visible while menus (inventory) are open
            var canvas = UiCanvas("Subtitles", parent, 20);
            var bar = UiRect("Bar", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(1300, 140));
            var group = bar.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0f;
            UiImage("Back", bar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 140), new Color(0.03f, 0.05f, 0.08f, 0.62f));
            var speaker = UiText("Speaker", bar, new Vector2(0.5f, 0.5f), new Vector2(0, 44), new Vector2(1240, 34), 22, Brass);
            var line = UiText("Line", bar, new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(1240, 76), 28, new Color(0.93f, 0.96f, 1f));
            var progressBack = UiImage("ProgressBack", bar, new Vector2(0.5f, 0.5f), new Vector2(0, -64), new Vector2(1240, 4), new Color(1, 1, 1, 0.12f));
            var progress = UiImage("Progress", progressBack.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 4), SignalCyan);
            progress.sprite = WhiteSprite();
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillAmount = 0f;
            canvas.gameObject.AddComponent<SubtitleHud>().EditorBind(group, speaker, line, progress);
        }

        private static void BuildDialogue(Transform parent)
        {
            // CAIRN comms panel, above the subtitle bar; own canvas so it shows while choices hold the cursor
            var canvas = UiCanvas("CairnComms", parent, 30);
            var panel = UiRect("Panel", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 470), new Vector2(1300, 150));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var back = UiImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 150), new Color(0.02f, 0.06f, 0.08f, 0.78f));
            back.raycastTarget = false;
            UiImage("Accent", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 74), new Vector2(1300, 2), new Color(SignalCyan.r, SignalCyan.g, SignalCyan.b, 0.6f));
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var ring = UiImage("EyeRing", panel, new Vector2(0.5f, 0.5f), new Vector2(-575, 0), new Vector2(104, 104), SignalCyan);
            ring.sprite = knob;
            var ringHole = UiImage("EyeHole", ring.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86), new Color(0.02f, 0.06f, 0.08f, 1f));
            ringHole.sprite = knob;
            var eye = UiImage("Eye", ring.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34), SignalCyan);
            eye.sprite = knob;
            var speaker = UiText("Speaker", panel, new Vector2(0.5f, 0.5f), new Vector2(60, 48), new Vector2(1120, 34), 22, SignalCyan);
            speaker.alignment = TextAlignmentOptions.Left;
            speaker.characterSpacing = 8;
            var line = UiText("Line", panel, new Vector2(0.5f, 0.5f), new Vector2(60, -10), new Vector2(1120, 84), 27, new Color(0.92f, 0.97f, 1f));
            line.alignment = TextAlignmentOptions.TopLeft;
            var list = VerticalList("Choices", panel, new Vector2(60, -170), new Vector2(1120, 200));
            var template = ListButton("ChoiceTemplate", list, 1120, 52, 23, false);
            canvas.gameObject.AddComponent<DialogueScreen>().EditorBind(group, eye, ring, speaker, line, list, template);
        }

        private static Image Panel(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var img = UiImage(name, parent, new Vector2(0.5f, 0.5f), pos, size, new Color(0.05f, 0.07f, 0.11f, 1f));
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.75f);
            outline.effectDistance = new Vector2(2, -2);
            return img;
        }

        private static Transform VerticalList(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var r = UiRect(name, parent, new Vector2(0.5f, 0.5f), pos, size);
            var layout = r.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            return r;
        }

        private static Button ListButton(string name, Transform parent, float width, float height, float fontSize, bool withIcon)
        {
            var img = UiImage(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), new Color(0.09f, 0.13f, 0.19f, 1f));
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.6f, 0.95f, 1f);
            colors.selectedColor = new Color(0.75f, 0.9f, 1f);
            button.colors = colors;
            if (withIcon)
            {
                var icon = UiImage("Icon", img.transform, new Vector2(0f, 0.5f), new Vector2(height * 0.5f + 6, 0), new Vector2(height - 16, height - 16), Color.white);
                icon.raycastTarget = false;
            }
            var label = UiText("Label", img.transform, new Vector2(0.5f, 0.5f), new Vector2(withIcon ? height * 0.5f : 0, 0), new Vector2(width - (withIcon ? height + 30 : 30), height), fontSize, new Color(0.88f, 0.95f, 1f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return button;
        }

        private static void BuildInventory(Transform parent)
        {
            var canvas = UiCanvas("InventoryScreen", parent, 40);
            var panel = UiImage("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.02f, 0.03f, 0.05f, 0.78f));
            var frame = Panel("Frame", panel.transform, Vector2.zero, new Vector2(1500, 820));
            var title = UiText("Title", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(-560, 370), new Vector2(360, 50), 36, Brass, "ui.inventory.title");
            title.characterSpacing = 12;
            title.alignment = TextAlignmentOptions.Left;
            UiText("CloseHint", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(560, 370), new Vector2(320, 40), 20, new Color(1, 1, 1, 0.45f), "ui.inventory.close").alignment = TextAlignmentOptions.Right;

            var itemsTab = UiButton("Tab_Items", frame.transform, new Vector2(-180, 370), "ui.inventory.items");
            ((RectTransform)itemsTab.transform).sizeDelta = new Vector2(240, 50);
            var logsTab = UiButton("Tab_Logs", frame.transform, new Vector2(80, 370), "ui.inventory.logs");
            ((RectTransform)logsTab.transform).sizeDelta = new Vector2(240, 50);

            // items page
            var itemsPage = UiRect("ItemsPage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1440, 700));
            var itemList = VerticalList("ItemList", itemsPage, new Vector2(-510, 0), new Vector2(400, 660));
            var itemTemplate = ListButton("ItemTemplate", itemList, 400, 64, 24, true);
            var empty = UiText("EmptyText", itemsPage, new Vector2(0.5f, 0.5f), new Vector2(-510, 200), new Vector2(380, 80), 22, new Color(1, 1, 1, 0.5f), "ui.inventory.empty");
            var viewBack = UiImage("InspectView", itemsPage, new Vector2(0.5f, 0.5f), new Vector2(40, 70), new Vector2(500, 500), new Color(0.03f, 0.05f, 0.08f, 1f));
            var raw = new GameObject("Render", typeof(RectTransform), typeof(RawImage));
            raw.transform.SetParent(viewBack.transform, false);
            var rawRect = (RectTransform)raw.transform;
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = rawRect.offsetMax = Vector2.zero;
            var inspector = viewBack.gameObject.AddComponent<ItemInspector>();
            inspector.EditorBind(raw.GetComponent<RawImage>());
            UiText("InspectHint", itemsPage, new Vector2(0.5f, 0.5f), new Vector2(40, -200), new Vector2(500, 30), 18, new Color(1, 1, 1, 0.4f), "ui.inventory.inspect_hint");
            var itemName = UiText("ItemName", itemsPage, new Vector2(0.5f, 0.5f), new Vector2(510, 290), new Vector2(380, 80), 30, Brass);
            itemName.alignment = TextAlignmentOptions.TopLeft;
            var itemDesc = UiText("ItemDescription", itemsPage, new Vector2(0.5f, 0.5f), new Vector2(510, 60), new Vector2(380, 360), 22, new Color(0.85f, 0.9f, 0.95f));
            itemDesc.alignment = TextAlignmentOptions.TopLeft;

            // logs page
            var logsPage = UiRect("LogsPage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1440, 700));
            var counter = UiText("Counter", logsPage, new Vector2(0.5f, 0.5f), new Vector2(0, 310), new Vector2(1300, 40), 24, TealLight);
            counter.alignment = TextAlignmentOptions.Left;
            var logList = VerticalList("LogList", logsPage, new Vector2(0, -40), new Vector2(1300, 620));
            var logTemplate = ListButton("LogTemplate", logList, 1300, 78, 24, false);

            var screen = canvas.gameObject.AddComponent<InventoryScreen>();
            screen.EditorBind(panel.gameObject, itemsTab, logsTab, itemsPage.gameObject, logsPage.gameObject, itemList, itemTemplate,
                empty, itemName, itemDesc, inspector, logList, logTemplate, counter);
        }

        private static void BuildTerminal(Transform parent)
        {
            var canvas = UiCanvas("TerminalScreen", parent, 41);
            var panel = UiImage("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.02f, 0.03f, 0.05f, 0.8f));
            var frame = Panel("Frame", panel.transform, Vector2.zero, new Vector2(1500, 840));
            frame.color = new Color(0.03f, 0.08f, 0.1f, 1f);
            var title = UiText("Title", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(-260, 375), new Vector2(940, 50), 32, SignalCyan);
            title.alignment = TextAlignmentOptions.Left;
            var subtitle = UiText("Subtitle", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(-260, 335), new Vector2(940, 30), 18, Brass);
            subtitle.alignment = TextAlignmentOptions.Left;
            subtitle.characterSpacing = 8;
            UiText("CloseHint", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(560, 375), new Vector2(320, 40), 20, new Color(1, 1, 1, 0.45f), "terminal.close").alignment = TextAlignmentOptions.Right;
            UiImage("Rule", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 310), new Vector2(1420, 2), new Color(Brass.r, Brass.g, Brass.b, 0.6f));

            // lock page
            var lockPage = UiRect("LockPage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1400, 700));
            UiText("LockTitle", lockPage, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(800, 50), 30, SignalCyan, "terminal.pin.title").characterSpacing = 10;
            var pin = UiText("PinDisplay", lockPage, new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(600, 80), 60, new Color(0.9f, 0.97f, 1f));
            var hint = UiText("Hint", lockPage, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 40), 20, new Color(1, 1, 1, 0.5f));
            var feedback = UiText("Feedback", lockPage, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(800, 40), 26, EmergencyRed);
            var pad = UiRect("Keypad", lockPage, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(330, 400));
            var grid = pad.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 84);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Back", "0", "Enter" })
            {
                var b = UiButton($"Key_{key}", pad, Vector2.zero, null);
                b.GetComponentInChildren<TMP_Text>().text = key switch { "Back" => "DEL", "Enter" => "OK", _ => key };
            }

            // entries page
            var entriesPage = UiRect("EntriesPage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1440, 700));
            var list = VerticalList("EntryList", entriesPage, new Vector2(-500, 0), new Vector2(420, 680));
            var template = ListButton("EntryTemplate", list, 420, 58, 22, false);
            var entryTitle = UiText("EntryTitle", entriesPage, new Vector2(0.5f, 0.5f), new Vector2(230, 310), new Vector2(940, 50), 28, Brass);
            entryTitle.alignment = TextAlignmentOptions.Left;
            var entryBody = UiText("EntryBody", entriesPage, new Vector2(0.5f, 0.5f), new Vector2(230, -30), new Vector2(940, 600), 23, new Color(0.75f, 0.97f, 0.95f));
            entryBody.alignment = TextAlignmentOptions.TopLeft;

            var screen = canvas.gameObject.AddComponent<TerminalScreen>();
            screen.EditorBind(panel.gameObject, title, subtitle, lockPage.gameObject, pin, hint, feedback, pad, entriesPage.gameObject, list, template, entryTitle, entryBody);
        }

        private static void BuildDecision(Transform parent)
        {
            var canvas = UiCanvas("DecisionScreen", parent, 45);
            var panel = UiImage("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.01f, 0.02f, 0.03f, 0.9f));
            var frame = Panel("Frame", panel.transform, Vector2.zero, new Vector2(1300, 640));
            var title = UiText("Title", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1200, 60), 38, Brass, "ui.dec.title");
            title.characterSpacing = 10;
            UiText("Subtitle", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 195), new Vector2(1200, 40), 22, new Color(1f, 0.62f, 0.55f), "ui.dec.subtitle");

            var choices = UiRect("ChoicePage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1200, 420));
            Button Option(string name, float y, string key)
            {
                var b = UiButton(name, choices, new Vector2(0, y), key);
                ((RectTransform)b.transform).sizeDelta = new Vector2(980, 80);
                ((RectTransform)b.GetComponentInChildren<TMP_Text>().transform).sizeDelta = new Vector2(940, 76);
                return b;
            }
            var a = Option("Option_A", 110, "ui.dec.option_a");
            var bOpt = Option("Option_B", 0, "ui.dec.option_b");
            var c = Option("Option_C", -110, "ui.dec.option_c");
            c.GetComponent<Image>().color = new Color(0.05f, 0.14f, 0.16f, 1f);

            var confirm = UiRect("ConfirmPage", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1200, 420));
            var chosen = UiText("Chosen", confirm, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1100, 60), 30, new Color(0.92f, 0.97f, 1f));
            UiText("Question", confirm, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1100, 50), 26, EmergencyRed, "ui.dec.confirm_q");
            var ok = UiButton("Confirm", confirm, new Vector2(-170, -80), "ui.dec.confirm");
            var back = UiButton("Back", confirm, new Vector2(170, -80), "ui.dec.back");
            confirm.gameObject.SetActive(false);

            canvas.gameObject.AddComponent<DecisionScreen>().EditorBind(panel.gameObject, choices.gameObject, a, bOpt, c, confirm.gameObject, chosen, ok, back);
        }

        private static Sprite WhiteSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static void BuildPause(Transform parent)
        {
            var canvas = UiCanvas("PauseMenu", parent, 50);
            var panel = UiImage("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.03f, 0.05f, 0.08f, 0.82f));
            var title = UiText("Title", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(800, 90), 64, Brass, "ui.pause.title");
            title.characterSpacing = 14;
            var resume = UiButton("ResumeButton", panel.transform, new Vector2(0, 40), "ui.pause.resume");
            var language = UiButton("LanguageButton", panel.transform, new Vector2(0, -40), "ui.menu.language");
            var menu = UiButton("MainMenuButton", panel.transform, new Vector2(0, -120), "ui.pause.main_menu");

            // settings: mouse sensitivity + invert Y
            UiText("SettingsTitle", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(600, 40), 24, Brass, "ui.pause.settings").characterSpacing = 10;
            UiText("SensitivityLabel", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-170, -275), new Vector2(340, 40), 22, new Color(0.85f, 0.95f, 1f), "ui.pause.sensitivity").alignment = TextAlignmentOptions.Right;
            var resources = new DefaultControls.Resources
            {
                standard = WhiteSprite(),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            };
            var sliderGo = DefaultControls.CreateSlider(resources);
            sliderGo.name = "SensitivitySlider";
            sliderGo.transform.SetParent(panel.transform, false);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.anchoredPosition = new Vector2(120, -275);
            sliderRect.sizeDelta = new Vector2(220, 20);
            var slider = sliderGo.GetComponent<Slider>();
            slider.transform.Find("Fill Area/Fill").GetComponent<Image>().color = SignalCyan;
            var sensValue = UiText("SensitivityValue", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(290, -275), new Vector2(100, 40), 22, TealLight);
            var toggleGo = DefaultControls.CreateToggle(resources);
            toggleGo.name = "InvertYToggle";
            toggleGo.transform.SetParent(panel.transform, false);
            ((RectTransform)toggleGo.transform).anchoredPosition = new Vector2(40, -325);
            ((RectTransform)toggleGo.transform).sizeDelta = new Vector2(300, 30);
            var toggleLabel = toggleGo.GetComponentInChildren<Text>();
            Object.DestroyImmediate(toggleLabel.gameObject);
            UiText("InvertYLabel", toggleGo.transform, new Vector2(0f, 0.5f), new Vector2(160, 0), new Vector2(260, 30), 22, new Color(0.85f, 0.95f, 1f), "ui.pause.invert_y").alignment = TextAlignmentOptions.Left;

            var pause = canvas.gameObject.AddComponent<PauseMenuController>();
            pause.EditorBind(panel.gameObject, resume, language, menu, slider, sensValue, toggleGo.GetComponent<Toggle>());
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
