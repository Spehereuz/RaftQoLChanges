using System.Collections.Generic;

namespace QoLChanges.GameState
{
    /// <summary>
    /// Amounts of the items the local player crafts with. At hand are the inventories the game's crafting menu takes
    /// items from: the hotbar, the inventory and the backpack, plus the inventory open next to them (a storage). Stored
    /// are, on request, the items in every other storage of the world. Unlike the game, which counts unlimited resources
    /// in Creative, the real amounts are counted.
    /// </summary>
    internal sealed class CraftingStock
    {
        private readonly Dictionary<int, int> _atHand = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _stored = new Dictionary<int, int>();

        /// <summary>
        /// Counts the items again, those in the other storages only when <paramref name="includeStorages"/> is set; false
        /// when there is no local player.
        /// </summary>
        public bool Read(bool includeStorages)
        {
            Clear();

            Network_Player player = ComponentManager<Network_Player>.Value;
            PlayerInventory inventory = player != null ? player.Inventory : null;
            if (inventory == null)
            {
                return false;
            }

            Inventory open = inventory.secondInventory;
            Add(_atHand, inventory);
            if (open != null)
            {
                Add(_atHand, open);
            }

            if (includeStorages)
            {
                AddStorages(open);
            }

            return true;
        }

        /// <summary>Items the crafting menu can use right now.</summary>
        public int AtHand(Item_Base item)
        {
            return Get(_atHand, item);
        }

        /// <summary>Items in storages that are not open; zero unless storages were read.</summary>
        public int Stored(Item_Base item)
        {
            return Get(_stored, item);
        }

        public void Clear()
        {
            _atHand.Clear();
            _stored.Clear();
        }

        /// <summary>
        /// Every placed storage except the trash can and the one already counted as open. Other players' games keep the
        /// contents of every storage: the game sends them on joining and after a storage is closed.
        /// </summary>
        private void AddStorages(Inventory open)
        {
            foreach (Storage_Small storage in StorageManager.allStorages)
            {
                Inventory inventory = storage != null ? storage.GetInventoryReference() : null;
                if (inventory == null || inventory == open || inventory is Inventory_Trashcan)
                {
                    continue;
                }

                Add(_stored, inventory);
            }
        }

        private static int Get(Dictionary<int, int> counts, Item_Base item)
        {
            return item != null && counts.TryGetValue(item.UniqueIndex, out int count) ? count : 0;
        }

        private static void Add(Dictionary<int, int> counts, Inventory inventory)
        {
            if (inventory.allSlots == null)
            {
                return;
            }

            foreach (Slot slot in inventory.allSlots)
            {
                if (slot == null || slot.IsEmpty || slot.itemInstance.baseItem == null)
                {
                    continue;
                }

                int index = slot.itemInstance.baseItem.UniqueIndex;
                counts.TryGetValue(index, out int count);
                counts[index] = count + slot.itemInstance.Amount;
            }
        }
    }
}
