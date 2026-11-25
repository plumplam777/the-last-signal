using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A small locator LED that blinks (short flash, long pause) so an item can be found in the dark, and goes
    /// dark for good once a WorldState flag is set (e.g. the item was taken).
    /// </summary>
    public sealed class LocatorBlink : MonoBehaviour
    {
        [SerializeField] private GameObject lamp;
        [SerializeField] private float period = 1.6f;
        [SerializeField, Range(0.05f, 0.9f)] private float onFraction = 0.22f;
        [Tooltip("Stops blinking (lamp off) once this flag is true. Empty = always blinks.")]
        [SerializeField] private string stopWhenFlag;

        private void Update()
        {
            if (lamp == null) return;
            bool stopped = !string.IsNullOrEmpty(stopWhenFlag) && GameServices.World.GetBool(stopWhenFlag);
            bool on = !stopped && Mathf.Repeat(Time.time, period) < period * onFraction;
            if (lamp.activeSelf != on) lamp.SetActive(on);
        }

#if UNITY_EDITOR
        public void EditorConfigure(GameObject lampObject, string stopFlag)
        {
            lamp = lampObject;
            stopWhenFlag = stopFlag;
        }
#endif
    }
}
