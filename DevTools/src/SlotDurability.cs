using System.Collections.Generic;

namespace QoLDevTools
{
    /// <summary>
    /// Changes item durability through Slot.SetUses, the same path the game uses: the slot is redrawn,
    /// 0 uses breaks a tool, empties a canteen or eats one piece of a food stack.
    /// </summary>
    internal static class SlotDurability
    {
        public static bool HasDurability(Slot slot)
        {
            return slot != null && !slot.IsEmpty && slot.itemInstance.baseItem != null && slot.itemInstance.baseItem.MaxUses > 1;
        }

        /// <summary>Returns a line describing the change, e.g. "Metal axe: 120 -> 44/120".</summary>
        public static string Apply(Slot slot, DurabilityValue value)
        {
            ItemInstance instance = slot.itemInstance;
            Item_Base item = instance.baseItem;
            string name = item.settings_Inventory != null ? item.settings_Inventory.DisplayName : item.UniqueName;

            int max = item.MaxUses;
            int before = instance.Uses;
            int target = value.Resolve(max);
            slot.SetUses(target);

            string result = name + ": " + before + " -> " + target + "/" + max;
            return target == 0 ? result + " (used up)" : result;
        }

        /// <summary>Hotbar, inventory and equipment of the local player, plus the storage they have open.</summary>
        public static List<Slot> CollectPlayerSlots(Network_Player player, out bool includesStorage)
        {
            HashSet<Slot> unique = new HashSet<Slot>();
            List<Slot> slots = new List<Slot>();
            PlayerInventory inventory = player.Inventory;

            AddRange(inventory.allSlots, unique, slots);
            if (inventory.equipSlots != null)
            {
                foreach (Slot_Equip slot in inventory.equipSlots)
                {
                    Add(slot, unique, slots);
                }
            }

            includesStorage = false;
            StorageManager storageManager = player.StorageManager;
            Storage_Small storage = storageManager != null ? storageManager.currentStorage : null;
            Inventory storageInventory = storage != null ? storage.GetInventoryReference() : null;
            if (storageInventory != null)
            {
                includesStorage = true;
                AddRange(storageInventory.allSlots, unique, slots);
            }

            return slots;
        }

        private static void AddRange(IEnumerable<Slot> source, HashSet<Slot> unique, List<Slot> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (Slot slot in source)
            {
                Add(slot, unique, target);
            }
        }

        private static void Add(Slot slot, HashSet<Slot> unique, List<Slot> target)
        {
            if (slot != null && unique.Add(slot))
            {
                target.Add(slot);
            }
        }
    }
}
