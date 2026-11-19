using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Switches lights, emissive renderers and objects between the powered and unpowered state of a sector.
    /// Purely reactive: it listens to WorldState "power.&lt;sector&gt;".
    /// </summary>
    public sealed class PoweredObject : MonoBehaviour
    {
        [SerializeField] private string sectorId = SectorIds.Docking;
        [Tooltip("Active only while powered.")]
        [SerializeField] private GameObject[] poweredOn = new GameObject[0];
        [Tooltip("Active only while unpowered (emergency lights, sparks…).")]
        [SerializeField] private GameObject[] poweredOff = new GameObject[0];
        [Tooltip("Optional: power-up flicker before settling on.")]
        [SerializeField] private bool flickerOnPowerUp = true;

        private string _key;
        private float _flickerUntil;
        private bool _powered;

        public bool IsPowered => _powered;

        private void Awake() => _key = WorldKeys.Power(sectorId);

        private void OnEnable()
        {
            GameServices.World.Changed += OnWorldChanged;
            GameServices.World.Reset += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (!GameServices.IsReady) return;
            GameServices.World.Changed -= OnWorldChanged;
            GameServices.World.Reset -= Refresh;
        }

        private void OnWorldChanged(string key)
        {
            if (key != _key) return;
            bool wasPowered = _powered;
            Refresh();
            if (!wasPowered && _powered && flickerOnPowerUp) _flickerUntil = Time.time + 0.9f;
        }

        private void Refresh()
        {
            _powered = GameServices.World.GetBool(_key);
            Apply(_powered);
        }

        private void Update()
        {
            if (_flickerUntil <= 0f) return;
            if (Time.time >= _flickerUntil)
            {
                _flickerUntil = 0f;
                Apply(_powered);
                return;
            }
            // Short irregular stutter, like old fluorescent tubes catching.
            Apply(Mathf.PerlinNoise(Time.time * 18f, 0.3f) > 0.45f);
        }

        private void Apply(bool on)
        {
            foreach (var go in poweredOn) if (go != null) go.SetActive(on);
            foreach (var go in poweredOff) if (go != null) go.SetActive(!on);
        }

#if UNITY_EDITOR
        public void EditorConfigure(string sector, GameObject[] on, GameObject[] off)
        {
            sectorId = sector;
            poweredOn = on;
            poweredOff = off;
        }
#endif
    }
}
