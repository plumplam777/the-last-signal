using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Shows one set of objects while a condition holds and another while it does not (frosted vs. wiped glass,
    /// closed vs. open cabinet, sealed vs. revealed archive). Pure view: state lives in WorldState, so it survives load.
    /// </summary>
    public sealed class ConditionToggle : MonoBehaviour
    {
        [SerializeField] private ConditionSet condition = new();
        [SerializeField] private GameObject[] whenTrue = new GameObject[0];
        [SerializeField] private GameObject[] whenFalse = new GameObject[0];

        private bool? _shown;

        private void Start() => Refresh(true);

        private void Update() => Refresh(false);

        private void Refresh(bool force)
        {
            bool on = condition.Evaluate();
            if (!force && _shown == on) return;
            _shown = on;
            foreach (var go in whenTrue) if (go != null) go.SetActive(on);
            foreach (var go in whenFalse) if (go != null) go.SetActive(!on);
        }

#if UNITY_EDITOR
        public void EditorSet(ConditionSet c, GameObject[] onTrue, GameObject[] onFalse)
        {
            condition = c;
            whenTrue = onTrue ?? new GameObject[0];
            whenFalse = onFalse ?? new GameObject[0];
        }
#endif
    }
}
