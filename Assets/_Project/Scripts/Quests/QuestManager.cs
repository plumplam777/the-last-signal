using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using LastSignal.Inventory;

namespace LastSignal.Quests
{
    public enum ObjectiveState
    {
        Locked,
        Active,
        Completed,
    }

    /// <summary>
    /// Tracks objective states for the registered quests. Completion is stored in WorldState
    /// ("quest.&lt;objectiveId&gt;.done"), so quests save and load with the world and need no state of their own.
    /// Re-evaluates whenever the world or inventory changes.
    /// </summary>
    public sealed class QuestManager
    {
        private readonly List<QuestDefinition> _quests = new();
        private readonly WorldState _world;
        private readonly InventoryManager _inventory;
        private bool _evaluating;

        /// <summary>(quest, objective)</summary>
        public event Action<QuestDefinition, ObjectiveDefinition> ObjectiveActivated;
        public event Action<QuestDefinition, ObjectiveDefinition> ObjectiveCompleted;
        /// <summary>Raised when the objective shown on the HUD changes (null = nothing to do).</summary>
        public event Action<ObjectiveDefinition> CurrentObjectiveChanged;

        public ObjectiveDefinition CurrentObjective { get; private set; }
        public IReadOnlyList<QuestDefinition> Quests => _quests;

        public QuestManager(WorldState world, InventoryManager inventory)
        {
            _world = world;
            _inventory = inventory;
            _world.Changed += _ => Evaluate();
            _world.Reset += Evaluate;
            _inventory.Changed += (_, _) => Evaluate();
            _inventory.Reset += Evaluate;
        }

        public static string DoneKey(string objectiveId) => $"quest.{objectiveId}.done";

        public void Register(QuestDefinition quest)
        {
            if (quest == null || _quests.Contains(quest)) return;
            _quests.Add(quest);
            Evaluate();
        }

        public bool IsCompleted(string objectiveId) => _world.GetBool(DoneKey(objectiveId));

        public ObjectiveState GetState(QuestDefinition quest, ObjectiveDefinition objective)
        {
            if (IsCompleted(objective.id)) return ObjectiveState.Completed;
            if (!quest.Sequential) return ObjectiveState.Active;
            foreach (var o in quest.Objectives)
            {
                if (o == objective) return ObjectiveState.Active;
                if (!IsCompleted(o.id)) return ObjectiveState.Locked;
            }
            return ObjectiveState.Locked;
        }

        public int CompletedCount(QuestDefinition quest) => quest.Objectives.Count(o => IsCompleted(o.id));

        /// <summary>Checks active objectives; completes any whose conditions now hold (cascading).</summary>
        public void Evaluate()
        {
            if (_evaluating) return; // completing an objective writes WorldState, which re-enters here
            _evaluating = true;
            try
            {
                bool progressed;
                do
                {
                    progressed = false;
                    foreach (var quest in _quests)
                    {
                        foreach (var objective in quest.Objectives)
                        {
                            if (GetState(quest, objective) != ObjectiveState.Active) continue;
                            if (!objective.completeWhen.Evaluate(_world, _inventory, out _)) continue;
                            _world.SetBool(DoneKey(objective.id), true);
                            Log.Info(nameof(QuestManager), $"Objective completed: {objective.id}");
                            ObjectiveCompleted?.Invoke(quest, objective);
                            progressed = true;
                        }
                    }
                } while (progressed);
                UpdateCurrent();
            }
            finally
            {
                _evaluating = false;
            }
        }

        private void UpdateCurrent()
        {
            ObjectiveDefinition current = null;
            QuestDefinition owner = null;
            foreach (var quest in _quests)
            {
                current = quest.Objectives.FirstOrDefault(o => GetState(quest, o) == ObjectiveState.Active);
                if (current != null)
                {
                    owner = quest;
                    break;
                }
            }
            if (current == CurrentObjective) return;
            CurrentObjective = current;
            if (current != null) ObjectiveActivated?.Invoke(owner, current);
            CurrentObjectiveChanged?.Invoke(current);
        }
    }
}
