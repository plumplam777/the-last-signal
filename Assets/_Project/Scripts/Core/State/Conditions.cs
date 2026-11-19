using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Core
{
    public enum ConditionType
    {
        FlagTrue,
        FlagFalse,
        IntAtLeast,
        IntBelow,
        StringEquals,
        HasItem,
        SectorPowered,
        PuzzleSolved,
        LogCollected,
        AccessLevelAtLeast,
    }

    /// <summary>
    /// A single serialisable requirement checked against WorldState / Inventory.
    /// Used by doors, interactables, quests, dialogue choices and terminals.
    /// </summary>
    [Serializable]
    public sealed class Condition
    {
        public ConditionType type;
        [Tooltip("Flag/int/string key, sector id, puzzle id or log id depending on type.")]
        public string key;
        public int value;
        public string text;
        public ItemDefinition item;
        [Tooltip("Localization key shown when this condition fails, e.g. prompt.door.no_power")]
        public string failReasonKey;

        public bool Evaluate(WorldState world, InventoryManager inventory) => type switch
        {
            ConditionType.FlagTrue => world.GetBool(key),
            ConditionType.FlagFalse => !world.GetBool(key),
            ConditionType.IntAtLeast => world.GetInt(key) >= value,
            ConditionType.IntBelow => world.GetInt(key) < value,
            ConditionType.StringEquals => world.GetString(key) == text,
            ConditionType.HasItem => item != null && inventory.Has(item, Math.Max(1, value)),
            ConditionType.SectorPowered => world.GetBool(WorldKeys.Power(key)),
            ConditionType.PuzzleSolved => world.GetBool(WorldKeys.PuzzleSolved(key)),
            ConditionType.LogCollected => world.GetBool(WorldKeys.LogCollected(key)),
            ConditionType.AccessLevelAtLeast => inventory.HighestAccessLevel() >= value,
            _ => false,
        };

        public static Condition Flag(string key, string failReasonKey = null)
            => new() { type = ConditionType.FlagTrue, key = key, failReasonKey = failReasonKey };

        public static Condition Powered(string sectorId, string failReasonKey = null)
            => new() { type = ConditionType.SectorPowered, key = sectorId, failReasonKey = failReasonKey };

        public static Condition Item(ItemDefinition item, string failReasonKey = null)
            => new() { type = ConditionType.HasItem, item = item, value = 1, failReasonKey = failReasonKey };
    }

    /// <summary>A list of conditions combined with AND (default) or OR. Empty set = always true.</summary>
    [Serializable]
    public sealed class ConditionSet
    {
        public bool requireAll = true;
        public List<Condition> conditions = new();

        public bool IsEmpty => conditions == null || conditions.Count == 0;

        public bool Evaluate() => Evaluate(out _);

        public bool Evaluate(out string failReasonKey)
            => Evaluate(GameServices.World, GameServices.Inventory, out failReasonKey);

        public bool Evaluate(WorldState world, InventoryManager inventory, out string failReasonKey)
        {
            failReasonKey = null;
            if (IsEmpty) return true;
            string firstFail = null;
            foreach (var c in conditions)
            {
                if (c == null) continue;
                bool ok = c.Evaluate(world, inventory);
                if (requireAll && !ok)
                {
                    failReasonKey = c.failReasonKey;
                    return false;
                }
                if (!requireAll && ok) return true;
                if (!ok && firstFail == null) firstFail = c.failReasonKey;
            }
            if (requireAll) return true;
            failReasonKey = firstFail;
            return false;
        }
    }

    public enum EffectType
    {
        SetFlag,
        ClearFlag,
        AddInt,
        SetInt,
        SetString,
        GiveItem,
        RemoveItem,
        SetSectorPower,
    }

    /// <summary>A single serialisable state change applied when something happens.</summary>
    [Serializable]
    public sealed class Effect
    {
        public EffectType type;
        public string key;
        public int value;
        public string text;
        public ItemDefinition item;

        public void Apply(WorldState world, InventoryManager inventory)
        {
            switch (type)
            {
                case EffectType.SetFlag: world.SetBool(key, true); break;
                case EffectType.ClearFlag: world.SetBool(key, false); break;
                case EffectType.SetInt: world.SetInt(key, value); break;
                case EffectType.SetString: world.SetString(key, text); break;
                case EffectType.AddInt:
                    if (key == WorldKeys.AiTrust)
                        world.AddInt(key, value, WorldKeys.AiTrustMin, WorldKeys.AiTrustMax, WorldKeys.AiTrustDefault);
                    else
                        world.AddInt(key, value);
                    break;
                case EffectType.GiveItem:
                    if (item != null) inventory.Add(item, Math.Max(1, value));
                    break;
                case EffectType.RemoveItem:
                    if (item != null) inventory.Remove(item, Math.Max(1, value));
                    break;
                case EffectType.SetSectorPower: world.SetBool(WorldKeys.Power(key), value != 0); break;
                default:
                    Log.Warn(nameof(Effect), $"Unhandled effect type {type}.");
                    break;
            }
        }

        public static Effect Flag(string key) => new() { type = EffectType.SetFlag, key = key };
        public static Effect Power(string sectorId, bool on = true) => new() { type = EffectType.SetSectorPower, key = sectorId, value = on ? 1 : 0 };
        public static Effect Give(ItemDefinition item) => new() { type = EffectType.GiveItem, item = item, value = 1 };
        public static Effect Take(ItemDefinition item) => new() { type = EffectType.RemoveItem, item = item, value = 1 };
    }

    public static class Effects
    {
        public static void ApplyAll(IEnumerable<Effect> effects)
        {
            if (effects == null) return;
            foreach (var e in effects) e?.Apply(GameServices.World, GameServices.Inventory);
        }
    }
}
