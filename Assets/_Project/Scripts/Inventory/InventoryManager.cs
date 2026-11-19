using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;

namespace LastSignal.Inventory
{
    /// <summary>One stack of items in the inventory.</summary>
    public sealed class InventoryItem
    {
        public ItemDefinition Definition { get; }
        public int Quantity { get; internal set; }

        internal InventoryItem(ItemDefinition definition, int quantity)
        {
            Definition = definition;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// The player's collected items. Pure data + events; UI listens to <see cref="Changed"/>.
    /// Save files store <see cref="CreateSnapshot"/> (item id → quantity).
    /// </summary>
    public sealed class InventoryManager
    {
        private readonly List<InventoryItem> _items = new();

        public IReadOnlyList<InventoryItem> Items => _items;
        public ItemDefinition Selected { get; private set; }

        /// <summary>(item, new quantity). Quantity 0 means removed.</summary>
        public event Action<ItemDefinition, int> Changed;
        /// <summary>Raised only when an item is added (for "picked up" toasts).</summary>
        public event Action<ItemDefinition, int> Added;
        public event Action<ItemDefinition> SelectionChanged;
        public event Action Reset;

        public bool Add(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                Log.Warn(nameof(InventoryManager), "Tried to add a null item or non-positive quantity.");
                return false;
            }
            var stack = Find(item.Id);
            if (stack != null && !item.Stackable)
            {
                Log.Warn(nameof(InventoryManager), $"'{item.Id}' is not stackable and is already owned.");
                return false;
            }
            if (stack == null)
            {
                stack = new InventoryItem(item, 0);
                _items.Add(stack);
            }
            stack.Quantity += item.Stackable ? quantity : 1;
            Changed?.Invoke(item, stack.Quantity);
            Added?.Invoke(item, quantity);
            return true;
        }

        public bool Remove(ItemDefinition item, int quantity = 1) => item != null && Remove(item.Id, quantity);

        public bool Remove(string itemId, int quantity = 1)
        {
            var stack = Find(itemId);
            if (stack == null || stack.Quantity < quantity) return false;
            stack.Quantity -= quantity;
            if (stack.Quantity <= 0)
            {
                _items.Remove(stack);
                if (Selected == stack.Definition) Select(null);
            }
            Changed?.Invoke(stack.Definition, Math.Max(0, stack.Quantity));
            return true;
        }

        public bool Has(ItemDefinition item, int quantity = 1) => item != null && Has(item.Id, quantity);

        public bool Has(string itemId, int quantity = 1) => Count(itemId) >= quantity;

        public int Count(string itemId) => Find(itemId)?.Quantity ?? 0;

        /// <summary>Highest keycard clearance the player currently holds.</summary>
        public int HighestAccessLevel()
            => _items.Where(i => i.Definition.Type == ItemType.Keycard)
                     .Select(i => i.Definition.AccessLevel)
                     .DefaultIfEmpty(0)
                     .Max();

        public void Select(ItemDefinition item)
        {
            if (item != null && Find(item.Id) == null) return;
            if (Selected == item) return;
            Selected = item;
            SelectionChanged?.Invoke(item);
        }

        public void Clear()
        {
            _items.Clear();
            Selected = null;
            Reset?.Invoke();
        }

        public Dictionary<string, int> CreateSnapshot()
            => _items.ToDictionary(i => i.Definition.Id, i => i.Quantity);

        /// <summary>Restores from a snapshot; <paramref name="resolve"/> maps ids to definitions (unknown ids are skipped).</summary>
        public void Restore(Dictionary<string, int> snapshot, Func<string, ItemDefinition> resolve)
        {
            _items.Clear();
            Selected = null;
            if (snapshot != null)
            {
                foreach (var kv in snapshot)
                {
                    var def = resolve(kv.Key);
                    if (def == null)
                    {
                        Log.Warn(nameof(InventoryManager), $"Save references unknown item '{kv.Key}', skipped.");
                        continue;
                    }
                    if (kv.Value > 0) _items.Add(new InventoryItem(def, kv.Value));
                }
            }
            Reset?.Invoke();
        }

        private InventoryItem Find(string itemId) => _items.FirstOrDefault(i => i.Definition.Id == itemId);
    }
}
