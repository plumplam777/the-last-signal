using System;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// First-person movement and mouse look on a CharacterController. Reads only the Player input map,
    /// which InputService disables outside the Playing state, so menus never move the player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float sprintSpeed = 5.4f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private bool jumpEnabled = true;
        [SerializeField] private float jumpHeight = 0.9f;

        [Header("Look")]
        [Tooltip("Multiplier on the player's sensitivity setting (Settings → Mouse sensitivity).")]
        [SerializeField] private float lookScale = 1f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Footsteps")]
        [SerializeField] private float stepDistance = 2.1f;

        private CharacterController _controller;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _pitch;
        private float _stepAccumulator;

        /// <summary>Raised each time the player has walked one step length on the ground (for footstep audio).</summary>
        public event Action<bool> Footstep; // bool = sprinting

        public Transform CameraPivot => cameraPivot;
        public float Yaw => transform.eulerAngles.y;
        public float Pitch => _pitch;
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraPivot == null) Log.Error(nameof(PlayerController), "Camera pivot is not assigned.", this);
        }

        private void Update()
        {
            if (!GameServices.State.IsGameplayActive) return;
            var player = GameServices.Input.Controls.Player;
            if (GameServices.State.MouseLookReady) Look(player.Look.ReadValue<Vector2>());
            Move(player.Move.ReadValue<Vector2>(), player.Sprint.IsPressed(), player.Jump.WasPressedThisFrame());
        }

        private void Look(Vector2 delta)
        {
            var settings = GameServices.Settings;
            float sensitivity = settings.MouseSensitivity * lookScale;
            float dy = settings.InvertY ? -delta.y : delta.y;
            transform.Rotate(0f, delta.x * sensitivity, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - dy * sensitivity, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move(Vector2 input, bool sprint, bool jump)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            IsSprinting = sprint && input.y > 0.1f;
            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 target = (transform.right * input.x + transform.forward * input.y) * speed;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, acceleration * speed * Time.deltaTime);

            bool grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity < 0f) _verticalVelocity = -2f; // keep snapped to the floor
            if (grounded && jump && jumpEnabled) _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = (_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
            _controller.Move(motion);

            if (grounded)
            {
                _stepAccumulator += new Vector3(motion.x, 0f, motion.z).magnitude;
                if (_stepAccumulator >= stepDistance * (IsSprinting ? 1.3f : 1f))
                {
                    _stepAccumulator = 0f;
                    Footstep?.Invoke(IsSprinting);
                }
            }
        }

        /// <summary>Moves the player instantly (spawn, load, admin teleport).</summary>
        public void Teleport(Vector3 position, float yaw, float pitch = 0f)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;
            _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }
    }
}
