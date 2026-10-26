using HarmonyLib;
using UnityEngine;

namespace QoLChanges.Features.StackSize
{
    /// <summary>
    /// The stack size lives in Item_Base.settings_Inventory, and every ItemInstance carries its own
    /// copy of it. The game reads both, so the copy has to follow the base item.
    /// </summary>
    internal static class StackSizeAccess
    {
        private static readonly AccessTools.FieldRef<ItemInstance_Inventory, int> StackSizeField =
            AccessTools.FieldRefAccess<ItemInstance_Inventory, int>("stackSize");

        public static void Set(ItemInstance_Inventory settings, int value)
        {
            StackSizeField(settings) = value;
        }

        public static void SyncSlot(Slot slot)
        {
            if (slot == null)
            {
                return;
            }

            ItemInstance instance = slot.itemInstance;
            if (instance == null || instance.settings_Inventory == null)
            {
                return;
            }

            Item_Base baseItem = instance.baseItem;
            if (baseItem == null || baseItem.settings_Inventory == null)
            {
                return;
            }

            int target = baseItem.settings_Inventory.StackSize;
            if (instance.settings_Inventory.StackSize != target)
            {
                Set(instance.settings_Inventory, target);
            }
        }

        public static void SyncAllSlots()
        {
            foreach (Slot slot in Resources.FindObjectsOfTypeAll<Slot>())
            {
                SyncSlot(slot);
            }
        }
    }
}
