using System.Collections.Generic;

namespace QoLChanges.GameState
{
    /// <summary>
    /// Amounts of the items the local player crafts with, read from the same inventories as the game's crafting menu:
    /// the hotbar, the inventory and the backpack, plus the inventory the game crafts from while it is open next to them
    /// (a storage). Unlike the game, which counts unlimited resources in Creative, the real amounts are counted.
    /// </summary>
    internal sealed class CraftingStock
    {
        private readonly Dictionary<int, int> _counts = new Dictionary<int, int>();

        /// <summary>Counts the items again; false when there is no local player.</summary>
        public bool Read()
        {
            _counts.Clear();

            Network_Player player = ComponentManager<Network_Player>.Value;
            PlayerInventory inventory = player != null ? player.Inventory : null;
            if (inventory == null)
            {
                return false;
            }

            Add(inventory);
            if (inventory.secondInventory != null)
            {
                Add(inventory.secondInventory);
            }

            return true;
        }

        public int Count(Item_Base item)
        {
            return item != null && _counts.TryGetValue(item.UniqueIndex, out int count) ? count : 0;
        }

        public void Clear()
        {
            _counts.Clear();
        }

        private void Add(Inventory inventory)
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
                _counts.TryGetValue(index, out int count);
                _counts[index] = count + slot.itemInstance.Amount;
            }
        }
    }
}
