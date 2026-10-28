using System;
using System.Collections.Generic;

namespace QoLChanges.Features.StorageSort
{
    /// <summary>
    /// Rearranges the slots of an open storage. Like the vanilla sort, stacks of unused items are merged and filled up to
    /// the stack size; used items (worn tools, eaten food, partly drunk containers) and stacks larger than the stack size
    /// are kept as they are. Unlike it, nothing is recreated: every item keeps its own data, and the slots are written in
    /// one pass from a finished plan, so a failure leaves the storage as it was.
    /// </summary>
    internal static class StorageSorter
    {
        /// <summary>False when the storage has no slots to sort.</summary>
        public static bool Sort(Inventory inventory, StorageSortOrder order)
        {
            List<Slot> slots = GetSortableSlots(inventory);
            if (slots.Count == 0)
            {
                return false;
            }

            if (order == StorageSortOrder.ByCategory)
            {
                StorageGroups.Prepare();
            }

            List<Entry> entries = CollectEntries(slots);
            entries.Sort(new EntryComparer(order));
            Write(slots, entries);

            if (entries.Count > 0)
            {
                // The vanilla sort plays the sound once per stack it adds, all in the same frame.
                SoundManager sound = ComponentManager<SoundManager>.Value;
                if (sound != null)
                {
                    sound.PlayUI_MoveItem();
                }
            }

            return true;
        }

        /// <summary>Slots that can hold items. Locked or hidden slots keep what they hold.</summary>
        private static List<Slot> GetSortableSlots(Inventory inventory)
        {
            List<Slot> slots = new List<Slot>();
            if (inventory.allSlots == null)
            {
                return slots;
            }

            foreach (Slot slot in inventory.allSlots)
            {
                if (slot != null && slot.active && !slot.locked)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static List<Entry> CollectEntries(List<Slot> slots)
        {
            List<Entry> entries = new List<Entry>();
            Dictionary<MergeKey, MergedStack> merged = new Dictionary<MergeKey, MergedStack>();
            List<MergedStack> mergedOrder = new List<MergedStack>();

            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                if (!slot.HasValidItemInstance())
                {
                    continue;
                }

                ItemInstance instance = slot.itemInstance;
                Item_Base item = instance.baseItem;
                int stackSize = GetStackSize(item);

                // Same rule as the vanilla sort: only unused items within the stack size are merged.
                if (!instance.HasMaxUses || instance.Amount > stackSize)
                {
                    entries.Add(new Entry(instance.Clone(), item, i));
                    continue;
                }

                MergeKey key = new MergeKey(item.UniqueIndex, instance.exclusiveString);
                if (!merged.TryGetValue(key, out MergedStack stack))
                {
                    stack = new MergedStack(instance, i);
                    merged.Add(key, stack);
                    mergedOrder.Add(stack);
                }

                stack.Amount += instance.Amount;
            }

            foreach (MergedStack stack in mergedOrder)
            {
                Item_Base item = stack.Template.baseItem;
                int stackSize = GetStackSize(item);
                for (int remaining = stack.Amount; remaining > 0; remaining -= stackSize)
                {
                    ItemInstance part = stack.Template.Clone();
                    part.Amount = Math.Min(remaining, stackSize);
                    entries.Add(new Entry(part, item, stack.FirstSlot));
                }
            }

            return entries;
        }

        /// <summary>
        /// Merging never needs more slots than the stacks it merges, so the plan always fits. If a slot cannot be
        /// written, every slot gets its original item back.
        /// </summary>
        private static void Write(List<Slot> slots, List<Entry> entries)
        {
            ItemInstance[] original = new ItemInstance[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                original[i] = slots[i].itemInstance;
            }

            try
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].SetItem(i < entries.Count ? entries[i].Instance : null);
                }
            }
            catch
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].SetItem(original[i]);
                }

                throw;
            }
        }

        private static int GetStackSize(Item_Base item)
        {
            ItemInstance_Inventory settings = item.settings_Inventory;
            return settings != null ? Math.Max(1, settings.StackSize) : 1;
        }

        private static string GetName(Item_Base item)
        {
            string name = item.settings_Inventory != null ? item.settings_Inventory.DisplayName : null;
            return string.IsNullOrEmpty(name) ? item.UniqueName : name.Trim();
        }

        private struct MergeKey : IEquatable<MergeKey>
        {
            private readonly int _uniqueIndex;
            private readonly string _exclusiveString;

            public MergeKey(int uniqueIndex, string exclusiveString)
            {
                _uniqueIndex = uniqueIndex;
                _exclusiveString = exclusiveString ?? string.Empty;
            }

            public bool Equals(MergeKey other)
            {
                return _uniqueIndex == other._uniqueIndex && string.Equals(_exclusiveString, other._exclusiveString, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MergeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (_uniqueIndex * 397) ^ StringComparer.Ordinal.GetHashCode(_exclusiveString);
            }
        }

        private sealed class MergedStack
        {
            public MergedStack(ItemInstance template, int firstSlot)
            {
                Template = template;
                FirstSlot = firstSlot;
            }

            public ItemInstance Template { get; }

            public int FirstSlot { get; }

            public int Amount { get; set; }
        }

        private sealed class Entry
        {
            public Entry(ItemInstance instance, Item_Base item, int slotIndex)
            {
                Instance = instance;
                Item = item;
                Name = GetName(item);
                SlotIndex = slotIndex;
            }

            public ItemInstance Instance { get; }

            public Item_Base Item { get; }

            public string Name { get; }

            /// <summary>Where the item was before sorting; keeps equal items in their old order.</summary>
            public int SlotIndex { get; }
        }

        /// <summary>
        /// Items in the chosen order; copies of one item stay together: whole stacks first, the largest first, then used
        /// items, the least used first.
        /// </summary>
        private sealed class EntryComparer : IComparer<Entry>
        {
            private readonly StorageSortOrder _order;

            public EntryComparer(StorageSortOrder order)
            {
                _order = order;
            }

            public int Compare(Entry x, Entry y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                int result = CompareItems(x, y);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(x.Instance.exclusiveString ?? string.Empty, y.Instance.exclusiveString ?? string.Empty);
                if (result != 0)
                {
                    return result;
                }

                return CompareCopies(x.Instance, y.Instance, x.SlotIndex, y.SlotIndex);
            }

            private int CompareItems(Entry x, Entry y)
            {
                int result = _order == StorageSortOrder.ByCategory ? ((int)StorageGroups.Get(x.Item)).CompareTo((int)StorageGroups.Get(y.Item)) : 0;
                if (result != 0)
                {
                    return result;
                }

                result = string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                if (result != 0)
                {
                    return result;
                }

                return x.Item.UniqueIndex.CompareTo(y.Item.UniqueIndex);
            }

            private static int CompareCopies(ItemInstance x, ItemInstance y, int xSlot, int ySlot)
            {
                bool xUsed = !x.HasMaxUses;
                bool yUsed = !y.HasMaxUses;
                if (xUsed != yUsed)
                {
                    return xUsed ? 1 : -1;
                }

                int result = xUsed ? y.Uses.CompareTo(x.Uses) : 0;
                if (result != 0)
                {
                    return result;
                }

                result = y.Amount.CompareTo(x.Amount);
                return result != 0 ? result : xSlot.CompareTo(ySlot);
            }
        }
    }
}
