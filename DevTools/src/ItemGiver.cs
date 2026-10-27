namespace QoLDevTools
{
    /// <summary>
    /// Puts items into the player inventory without exceeding stack sizes. Vanilla Inventory.AddItem(string, int)
    /// puts the whole amount into one empty slot and drops everything on the ground when no slot is free,
    /// so slots are filled here one by one with Inventory.FindSuitableSlot.
    /// </summary>
    internal static class ItemGiver
    {
        /// <summary>Returns how many items were added.</summary>
        public static int Give(PlayerInventory inventory, Item_Base item, int amount)
        {
            Slot selected = inventory.GetSelectedHotbarSlot();
            bool selectedWasEmpty = selected != null && selected.IsEmpty;

            int remaining = amount;
            while (remaining > 0)
            {
                Slot slot = inventory.FindSuitableSlot(item);
                if (slot == null)
                {
                    break;
                }

                int added;
                if (slot.IsEmpty)
                {
                    added = System.Math.Min(remaining, System.Math.Max(1, item.settings_Inventory.StackSize));
                    slot.SetItem(item, added);
                }
                else
                {
                    int free = slot.itemInstance.settings_Inventory.StackSize - slot.itemInstance.Amount;
                    if (free <= 0)
                    {
                        break;
                    }

                    added = System.Math.Min(remaining, free);
                    slot.AddItem(item, added);
                }

                remaining -= added;
            }

            // Same as the vanilla pickup: an item that lands in the selected empty hotbar slot is taken in hand.
            if (selectedWasEmpty && !selected.IsEmpty)
            {
                inventory.hotbar.ReselectCurrentSlot();
            }

            return amount - remaining;
        }
    }
}
