using System;
using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Quests
{
    [Serializable]
    public sealed class ObjectiveDefinition
    {
        [Tooltip("Stable id, e.g. obj_01. Stored in WorldState as quest.<id>.done")]
        public string id;
        [Tooltip("Localization key of the objective text.")]
        public string textKey;
        [Tooltip("Objective completes (and stays completed) as soon as this is true.")]
        public ConditionSet completeWhen = new();
    }

    /// <summary>
    /// An ordered list of objectives. In a sequential quest the next objective activates when the previous
    /// one completes; an objective whose condition is already true when it activates completes at once.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Quest Definition", fileName = "quest_new")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string titleKey;
        [SerializeField] private bool sequential = true;
        [SerializeField] private List<ObjectiveDefinition> objectives = new();

        public string Id => id;
        public string TitleKey => titleKey;
        public bool Sequential => sequential;
        public IReadOnlyList<ObjectiveDefinition> Objectives => objectives;

#if UNITY_EDITOR
        public void EditorInit(string newId, string newTitleKey, bool isSequential, List<ObjectiveDefinition> newObjectives)
        {
            id = newId;
            titleKey = newTitleKey;
            sequential = isSequential;
            objectives = newObjectives;
        }
#endif
    }
}
