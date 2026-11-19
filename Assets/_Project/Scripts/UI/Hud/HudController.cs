using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// In-game HUD: crosshair, interaction prompt ("[E] Pick up Fuse"), denied reason and short toasts
    /// (item picked up / used). Lives on the player prefab and only listens to events.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image crosshair;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private Color crosshairIdle = new(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color crosshairActive = new(0.37f, 0.9f, 1f, 1f);
        [SerializeField] private Color crosshairBlocked = new(0.88f, 0.27f, 0.23f, 1f);

        private readonly Queue<string> _toasts = new();
        private float _toastTimer;
        private float _deniedTimer;
        private float _promptTarget;

        private void OnEnable()
        {
            if (interactor != null)
            {
                interactor.FocusChanged += OnFocusChanged;
                interactor.Denied += OnDenied;
            }
            GameServices.Inventory.Added += OnItemAdded;
            GameServices.Inventory.Changed += OnItemChanged;
            GameServices.State.Changed += OnStateChanged;
            GameServices.Localization.LanguageChanged += OnLanguageChanged;
            GameServices.World.Changed += OnWorldChanged;
            OnFocusChanged(null, false, null, null);
            if (toastGroup != null) toastGroup.alpha = 0f;
            OnStateChanged(GameServices.State.Current, GameServices.State.Current);
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.FocusChanged -= OnFocusChanged;
                interactor.Denied -= OnDenied;
            }
            if (!GameServices.IsReady) return;
            GameServices.Inventory.Added -= OnItemAdded;
            GameServices.Inventory.Changed -= OnItemChanged;
            GameServices.State.Changed -= OnStateChanged;
            GameServices.Localization.LanguageChanged -= OnLanguageChanged;
            GameServices.World.Changed -= OnWorldChanged;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (promptGroup != null) promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, _promptTarget, dt * 8f);

            if (_deniedTimer > 0f)
            {
                _deniedTimer -= dt;
                if (_deniedTimer <= 0f && reasonText != null) reasonText.alpha = 0.75f;
            }

            if (toastGroup == null) return;
            if (_toastTimer > 0f)
            {
                _toastTimer -= dt;
                toastGroup.alpha = Mathf.Clamp01(Mathf.Min(_toastTimer, 2.6f - _toastTimer) * 4f);
            }
            else if (_toasts.Count > 0)
            {
                toastText.text = _toasts.Dequeue();
                _toastTimer = 2.6f;
            }
            else
            {
                toastGroup.alpha = 0f;
            }
        }

        public void ShowToast(string message)
        {
            if (!string.IsNullOrEmpty(message)) _toasts.Enqueue(message);
        }

        private void OnFocusChanged(IInteractable target, bool available, string prompt, string reason)
        {
            _promptTarget = target != null ? 1f : 0f;
            if (crosshair != null)
                crosshair.color = target == null ? crosshairIdle : available ? crosshairActive : crosshairBlocked;
            if (target == null) return;
            if (promptText != null)
                promptText.text = available ? $"<color=#5FE6FF>[E]</color>  {prompt}" : prompt;
            if (reasonText != null)
            {
                reasonText.text = available ? string.Empty : reason ?? string.Empty;
                reasonText.alpha = 0.75f;
            }
        }

        private void OnDenied(string reason)
        {
            if (reasonText == null || string.IsNullOrEmpty(reason)) return;
            reasonText.text = reason;
            reasonText.alpha = 1f;
            _deniedTimer = 0.6f;
        }

        private void OnItemAdded(ItemDefinition item, int qty)
            => ShowToast(Loc.Get("toast.item_added", Loc.Get(item.NameKey)));

        private void OnItemChanged(ItemDefinition item, int qty)
        {
            if (qty == 0) ShowToast(Loc.Get("toast.item_used", Loc.Get(item.NameKey)));
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (root != null) root.alpha = next == GameState.Playing ? 1f : 0f;
        }

        private void OnWorldChanged(string key)
        {
            if (key == Player.FlashlightController.OwnedFlag) ShowToast(Loc.Get("hint.flashlight"));
            else if (key.StartsWith("power.") && GameServices.World.GetBool(key)) ShowToast(Loc.Get("toast.power_restored"));
        }

        private void OnLanguageChanged()
        {
            if (interactor != null) interactor.RefreshFocus();
        }

#if UNITY_EDITOR
        public void EditorBind(PlayerInteractor i, CanvasGroup r, Image cross, CanvasGroup pg, TMP_Text pt, TMP_Text rt, TMP_Text tt, CanvasGroup tg)
        {
            interactor = i; root = r; crosshair = cross; promptGroup = pg; promptText = pt; reasonText = rt; toastText = tt; toastGroup = tg;
        }
#endif
    }
}
