using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Dialogue
{
    [Serializable]
    public sealed class DialogueChoice
    {
        public string id;
        public string textKey;
        [Tooltip("Choice is offered only when this holds.")]
        public ConditionSet showWhen = new();
        public List<Effect> effects = new();
        [Tooltip("Node to continue with (empty = end).")]
        public string next;
    }

    [Serializable]
    public sealed class DialogueNode
    {
        public string id;
        public string speakerKey = "speaker.cairn";
        public string lineKey;
        [Tooltip("If false the node is skipped (used for trust-dependent add-on lines).")]
        public ConditionSet showWhen = new();
        public List<Effect> onEnter = new();
        public List<DialogueChoice> choices = new();
        [Tooltip("Next node when there are no choices (empty = end).")]
        public string next;
    }

    /// <summary>
    /// A conversation (e.g. AI_DOCK_01): nodes with lines, optional choices with trust/flag effects, conditional
    /// add-on nodes. Pure data; <see cref="DialogueManager"/> runs it and the UI only renders events.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Dialogue Graph", fileName = "dlg_new")]
    public sealed class DialogueGraph : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string startNode;
        [Tooltip("Plays only once per run (remembered as dlg.<id>.played).")]
        [SerializeField] private bool once = true;
        [SerializeField] private List<DialogueNode> nodes = new();

        public string Id => id;
        public string StartNode => string.IsNullOrEmpty(startNode) && nodes.Count > 0 ? nodes[0].id : startNode;
        public bool Once => once;
        public IReadOnlyList<DialogueNode> Nodes => nodes;
        public string PlayedKey => $"dlg.{id}.played";

        public DialogueNode Find(string nodeId) => nodes.FirstOrDefault(n => n.id == nodeId);

#if UNITY_EDITOR
        public void EditorInit(string newId, bool playOnce, List<DialogueNode> newNodes)
        {
            id = newId;
            once = playOnce;
            nodes = newNodes;
            startNode = newNodes.Count > 0 ? newNodes[0].id : null;
        }
#endif
    }
}
