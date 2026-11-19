using LastSignal.Core;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Dialogue
{
    public enum DialogueTriggerMode
    {
        /// <summary>Shortly after the sector loads.</summary>
        OnSectorStart,
        /// <summary>When a WorldState bool becomes true (checked on change and on start).</summary>
        OnFlag,
        /// <summary>When the player enters this object's trigger collider.</summary>
        OnEnterVolume,
    }

    /// <summary>
    /// Starts a <see cref="DialogueGraph"/> when its condition is met. Extra requirements (e.g. "only after
    /// AI_DOCK_01") are expressed with a ConditionSet. Once-only graphs remember themselves in WorldState.
    /// </summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueGraph graph;
        [SerializeField] private DialogueTriggerMode mode = DialogueTriggerMode.OnFlag;
        [Tooltip("OnFlag: the WorldState key to watch.")]
        [SerializeField] private string flag;
        [SerializeField] private ConditionSet requirements = new();
        [SerializeField] private float delaySeconds = 0.8f;

        private float _pendingAt = -1f;

        public DialogueGraph Graph => graph;

        private void OnEnable()
        {
            if (mode == DialogueTriggerMode.OnFlag) GameServices.World.Changed += OnWorldChanged;
        }

        private void OnDisable()
        {
            if (mode == DialogueTriggerMode.OnFlag && GameServices.IsReady) GameServices.World.Changed -= OnWorldChanged;
        }

        private void Start()
        {
            if (graph == null)
            {
                Log.Error(nameof(DialogueTrigger), $"'{name}' has no dialogue graph.", this);
                enabled = false;
                return;
            }
            if (mode == DialogueTriggerMode.OnSectorStart) Arm();
            if (mode == DialogueTriggerMode.OnFlag && GameServices.World.GetBool(flag)) Arm();
        }

        private void OnWorldChanged(string key)
        {
            if (key == flag && GameServices.World.GetBool(flag)) Arm();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (mode == DialogueTriggerMode.OnEnterVolume && other.GetComponentInParent<PlayerController>() != null) Arm();
        }

        private void Arm()
        {
            if (GameServices.Dialogue.HasPlayed(graph) && graph.Once) return;
            _pendingAt = Time.time + delaySeconds;
        }

        private void Update()
        {
            if (_pendingAt < 0f || Time.time < _pendingAt) return;
            if (!GameServices.State.IsGameplayActive && GameServices.State.Current != GameState.InMenu) return;
            if (!requirements.Evaluate()) return; // stays armed until requirements hold
            _pendingAt = -1f;
            GameServices.Dialogue.Play(graph);
        }

#if UNITY_EDITOR
        public void EditorSet(DialogueGraph g, DialogueTriggerMode m, string watchFlag, ConditionSet reqs = null, float delay = 0.8f)
        {
            graph = g; mode = m; flag = watchFlag; requirements = reqs ?? new ConditionSet(); delaySeconds = delay;
        }
#endif
    }
}
