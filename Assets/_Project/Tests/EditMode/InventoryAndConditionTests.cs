using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class InventoryAndConditionTests
    {
        private ItemDefinition _fuse, _battery, _keycard;

        [SetUp]
        public void SetUp()
        {
            _fuse = Make("item_fuse", ItemType.Component, false);
            _battery = Make("item_battery", ItemType.Battery, true);
            _keycard = Make("item_keycard_l2", ItemType.Keycard, false, 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fuse);
            Object.DestroyImmediate(_battery);
            Object.DestroyImmediate(_keycard);
        }

        [Test]
        public void NonStackable_CanOnlyBeOwnedOnce()
        {
            var inv = new InventoryManager();
            Assert.IsTrue(inv.Add(_fuse));
            Assert.IsFalse(inv.Add(_fuse));
            Assert.AreEqual(1, inv.Count("item_fuse"));
        }

        [Test]
        public void Stackable_AccumulatesAndRemoves()
        {
            var inv = new InventoryManager();
            inv.Add(_battery, 2);
            inv.Add(_battery, 3);
            Assert.AreEqual(5, inv.Count("item_battery"));
            Assert.IsTrue(inv.Remove(_battery, 5));
            Assert.IsFalse(inv.Has(_battery));
            Assert.IsFalse(inv.Remove(_battery));
        }

        [Test]
        public void Snapshot_RestoresThroughResolver()
        {
            var inv = new InventoryManager();
            inv.Add(_fuse);
            inv.Add(_battery, 4);
            var snap = inv.CreateSnapshot();
            var map = new Dictionary<string, ItemDefinition> { ["item_fuse"] = _fuse, ["item_battery"] = _battery };
            var restored = new InventoryManager();
            restored.Restore(snap, id => map.TryGetValue(id, out var d) ? d : null);
            Assert.AreEqual(1, restored.Count("item_fuse"));
            Assert.AreEqual(4, restored.Count("item_battery"));
        }

        [Test]
        public void ConditionSet_AndReportsFirstFailingReason()
        {
            var world = new WorldState();
            var inv = new InventoryManager();
            var set = new ConditionSet
            {
                conditions = new List<Condition>
                {
                    Condition.Powered(SectorIds.Docking, "reason.power"),
                    Condition.Item(_keycard, "reason.keycard"),
                },
            };
            Assert.IsFalse(set.Evaluate(world, inv, out var reason));
            Assert.AreEqual("reason.power", reason);
            world.SetBool(WorldKeys.Power(SectorIds.Docking), true);
            Assert.IsFalse(set.Evaluate(world, inv, out reason));
            Assert.AreEqual("reason.keycard", reason);
            inv.Add(_keycard);
            Assert.IsTrue(set.Evaluate(world, inv, out reason));
            Assert.IsNull(reason);
        }

        [Test]
        public void ConditionSet_OrPassesWhenAnyPasses()
        {
            var world = new WorldState();
            var inv = new InventoryManager();
            var set = new ConditionSet
            {
                requireAll = false,
                conditions = new List<Condition>
                {
                    Condition.Flag("story.a", "reason.a"),
                    new() { type = ConditionType.AccessLevelAtLeast, value = 2, failReasonKey = "reason.level" },
                },
            };
            Assert.IsFalse(set.Evaluate(world, inv, out var reason));
            Assert.AreEqual("reason.a", reason);
            inv.Add(_keycard);
            Assert.IsTrue(set.Evaluate(world, inv, out _));
        }

        [Test]
        public void Effects_ClampTrustAndGiveItems()
        {
            var world = new WorldState();
            var inv = new InventoryManager();
            world.SetInt(WorldKeys.AiTrust, 95);
            new Effect { type = EffectType.AddInt, key = WorldKeys.AiTrust, value = 20 }.Apply(world, inv);
            Effect.Give(_fuse).Apply(world, inv);
            Effect.Power(SectorIds.Engineering).Apply(world, inv);
            Assert.AreEqual(100, world.GetInt(WorldKeys.AiTrust));
            Assert.IsTrue(inv.Has(_fuse));
            Assert.IsTrue(world.GetBool("power.engineering"));
        }

        private static ItemDefinition Make(string id, ItemType type, bool stackable, int access = 0)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.EditorInit(id, type, $"item.{id}.name", $"item.{id}.desc", stackable, access);
            return def;
        }
    }
}
