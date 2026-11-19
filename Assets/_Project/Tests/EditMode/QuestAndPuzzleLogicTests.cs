using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Inventory;
using LastSignal.Puzzles;
using LastSignal.Quests;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class QuestAndPuzzleLogicTests
    {
        [Test]
        public void SequentialQuest_ActivatesInOrder_AndCascades()
        {
            var world = new WorldState();
            var inv = new InventoryManager();
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.EditorInit("q", "q.title", true, new List<ObjectiveDefinition>
            {
                new() { id = "a", textKey = "a", completeWhen = new ConditionSet { conditions = { Condition.Flag("story.a") } } },
                new() { id = "b", textKey = "b", completeWhen = new ConditionSet { conditions = { Condition.Flag("story.b") } } },
                new() { id = "c", textKey = "c", completeWhen = new ConditionSet { conditions = { Condition.Flag("story.c") } } },
            });
            var qm = new QuestManager(world, inv);
            qm.Register(quest);
            Assert.AreEqual("a", qm.CurrentObjective.id);

            world.SetBool("story.b", true); // b done early but locked behind a
            Assert.AreEqual("a", qm.CurrentObjective.id);
            Assert.IsFalse(qm.IsCompleted("b"));

            world.SetBool("story.a", true); // completes a, then b cascades immediately
            Assert.IsTrue(qm.IsCompleted("a"));
            Assert.IsTrue(qm.IsCompleted("b"));
            Assert.AreEqual("c", qm.CurrentObjective.id);

            world.SetBool("story.c", true);
            Assert.IsNull(qm.CurrentObjective);
            Assert.AreEqual(3, qm.CompletedCount(quest));
            Object.DestroyImmediate(quest);
        }

        [Test]
        public void Quest_CompletionSurvivesWorldRestore()
        {
            var world = new WorldState();
            var inv = new InventoryManager();
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.EditorInit("q", "q.title", true, new List<ObjectiveDefinition>
            {
                new() { id = "a", textKey = "a", completeWhen = new ConditionSet { conditions = { Condition.Flag("story.a") } } },
                new() { id = "b", textKey = "b", completeWhen = new ConditionSet { conditions = { Condition.Flag("story.b") } } },
            });
            var qm = new QuestManager(world, inv);
            qm.Register(quest);
            world.SetBool("story.a", true);
            var snapshot = world.CreateSnapshot();
            world.Clear();
            Assert.AreEqual("a", qm.CurrentObjective.id);
            world.Restore(snapshot);
            Assert.AreEqual("b", qm.CurrentObjective.id);
            Object.DestroyImmediate(quest);
        }

        [Test]
        public void CodeLock_AcceptsOnlyTheCode_AndLimitsLength()
        {
            var lockpad = new CodeLock("0417");
            foreach (var c in "04189") lockpad.Press(c);
            Assert.AreEqual("0418", lockpad.Entry, "Entry is capped at the code length.");
            Assert.IsFalse(lockpad.Submit());
            Assert.AreEqual(1, lockpad.FailedAttempts);
            Assert.AreEqual(string.Empty, lockpad.Entry);
            foreach (var c in "0419") lockpad.Press(c);
            lockpad.Backspace();
            lockpad.Press('7');
            Assert.IsTrue(lockpad.Submit());
        }
    }
}
