using System;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    public enum DoorState
    {
        Closed,
        Opening,
        Open,
        Closing,
    }

    /// <summary>
    /// Sliding door. Locked/unlocked is derived from its <see cref="Interactable"/> requirements
    /// (power, keycard, flags) — the door never contains inventory or quest logic itself.
    /// The first successful unlock is remembered in "door.&lt;id&gt;.unlocked" so it stays unlocked after load.
    /// </summary>
    public sealed class DoorController : Interactable
    {
        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;
        [SerializeField] private float slideDistance = 1.1f;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private bool autoClose;
        [SerializeField] private float autoCloseDelay = 4f;
        [Tooltip("Renderers tinted to show the lock state (status lights).")]
        [SerializeField] private Renderer[] statusLights = new Renderer[0];
        [SerializeField] private Color lockedColor = new(0.88f, 0.27f, 0.23f);
        [SerializeField] private Color unlockedColor = new(0.37f, 0.9f, 1f);

        private Vector3 _leftClosed, _rightClosed;
        private float _openAmount; // 0 closed .. 1 open
        private float _closeTimer;
        private MaterialPropertyBlock _block;
        private bool? _lastShownUnlocked;

        public DoorState State { get; private set; } = DoorState.Closed;
        public string DoorId => interactableId;
        public string UnlockedFlag => WorldKeys.DoorUnlocked(interactableId);

        public event Action<DoorState> StateChanged;

        private void Awake()
        {
            if (leftPanel != null) _leftClosed = leftPanel.localPosition;
            if (rightPanel != null) _rightClosed = rightPanel.localPosition;
            _block = new MaterialPropertyBlock();
        }

        protected override void Start()
        {
            oneShot = false;
            base.Start();
            UpdateStatusLights(true);
        }

        public override string GetPromptText()
            => Loc.Get(State is DoorState.Open or DoorState.Opening ? "prompt.door.close" : "prompt.door.open");

        public bool IsUnlocked => GameServices.World.GetBool(UnlockedFlag) || requirements.Evaluate();

        public override bool CanInteract(PlayerInteractor interactor, out string reasonKey)
        {
            if (GameServices.World.GetBool(UnlockedFlag) || GameServices.Admin.UnlockAllDoors)
            {
                reasonKey = null;
                return true;
            }
            return base.CanInteract(interactor, out reasonKey);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (!string.IsNullOrEmpty(interactableId)) GameServices.World.SetBool(UnlockedFlag, true);
            if (State is DoorState.Open or DoorState.Opening) SetState(DoorState.Closing);
            else SetState(DoorState.Opening);
        }

        /// <summary>Opens regardless of requirements (admin tools, scripted events).</summary>
        public void ForceOpen() => SetState(DoorState.Opening);

        private void Update()
        {
            UpdateStatusLights(false);
            switch (State)
            {
                case DoorState.Opening:
                    _openAmount = Mathf.MoveTowards(_openAmount, 1f, speed * Time.deltaTime);
                    if (_openAmount >= 1f)
                    {
                        SetState(DoorState.Open);
                        _closeTimer = autoCloseDelay;
                    }
                    break;
                case DoorState.Closing:
                    _openAmount = Mathf.MoveTowards(_openAmount, 0f, speed * Time.deltaTime);
                    if (_openAmount <= 0f) SetState(DoorState.Closed);
                    break;
                case DoorState.Open when autoClose:
                    _closeTimer -= Time.deltaTime;
                    if (_closeTimer <= 0f) SetState(DoorState.Closing);
                    break;
            }
            float eased = Mathf.SmoothStep(0f, 1f, _openAmount);
            if (leftPanel != null) leftPanel.localPosition = _leftClosed + Vector3.left * (slideDistance * eased);
            if (rightPanel != null) rightPanel.localPosition = _rightClosed + Vector3.right * (slideDistance * eased);
        }

        private void SetState(DoorState next)
        {
            if (next == State) return;
            State = next;
            if (next == DoorState.Opening) GameServices.Audio.SfxAt("sfx_door_open", transform.position + Vector3.up * 1.5f);
            else if (next == DoorState.Closing) GameServices.Audio.SfxAt("sfx_door_close", transform.position + Vector3.up * 1.5f);
            StateChanged?.Invoke(next);
        }

        private void UpdateStatusLights(bool force)
        {
            if (statusLights.Length == 0) return;
            bool unlocked = IsUnlocked;
            if (!force && _lastShownUnlocked == unlocked) return;
            _lastShownUnlocked = unlocked;
            var c = unlocked ? unlockedColor : lockedColor;
            foreach (var r in statusLights)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor("_BaseColor", c);
                _block.SetColor("_EmissionColor", c * 2.2f);
                r.SetPropertyBlock(_block);
            }
        }

#if UNITY_EDITOR
        public void EditorSetPanels(Transform left, Transform right, Renderer[] lights)
        {
            leftPanel = left;
            rightPanel = right;
            statusLights = lights;
        }
#endif
    }
}
