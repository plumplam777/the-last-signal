using UnityEngine;

namespace LastSignal.Cutscene
{
    /// <summary>Switches its children on one by one when enabled (docking lights along the bay mouth).</summary>
    public sealed class SequentialActivator : MonoBehaviour
    {
        [SerializeField] private float interval = 0.35f;

        private float _timer;
        private int _next;

        private void OnEnable()
        {
            _timer = 0f;
            _next = 0;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_next >= transform.childCount) return;
            _timer += Time.deltaTime;
            while (_timer >= interval && _next < transform.childCount)
            {
                _timer -= interval;
                transform.GetChild(_next++).gameObject.SetActive(true);
            }
        }
    }
}
