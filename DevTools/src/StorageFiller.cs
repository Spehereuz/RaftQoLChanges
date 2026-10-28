using System;
using System.Collections.Generic;

namespace QoLDevTools
{
    /// <summary>
    /// Empties the open storage and puts random items into random slots, so that sorting has every case to deal with:
    /// items of every group of Storage sort, several part stacks of one item, used tools and drinks, partly eaten food
    /// and a stack above the stack size. The storage sends its slots to the host when it is closed, as usual.
    /// </summary>
    internal static class StorageFiller
    {
        public const string Syntax = "qoldev.chest [slots]";

        // A share of the slots stays empty by default, so the sort has gaps to close.
        private const float DefaultFill = 0.75f;

        private static readonly string[] Pool =
        {
            // Resources
            "Plank", "Plastic", "Scrap", "Rope", "Nail", "MetalOre", "MetalIngot", "Clay", "Glass", "BioFuel", "Color_Red", "Battery",
            // Tools, weapons, equipment
            "Axe", "Hook_Scrap", "FishingRod", "Shovel", "Paddle", "FishingBait_Simple", "Spear_Plank", "Arrow_Stone", "Flipper", "HeadLight",
            // Food and drinks
            "Raw_Salmon", "Cooked_Catfish", "Coconut", "ClayPlate_Sushi", "HealingSalve", "Chili", "Canteen_Water", "PlasticCup_Water",
            "DrinkingGlass_SimpleSmoothie", "Canteen_Empty",
            // Seeds, placeables, other
            "Seed_Strawberry", "Raw_Potato", "Seed_Palm", "Placeable_Storage_Small", "Placeable_Cropplot_Small", "Placeable_Recipe_Sushi",
            "TrophyFish_T1_SharpCarp", "TradeToken",
        };

        private static readonly Random Random = new Random();

        public static string Fill(Network_Player player, string[] args)
        {
            Storage_Small storage = player.StorageManager != null ? player.StorageManager.currentStorage : null;
            Inventory inventory = storage != null ? storage.GetInventoryReference() : null;
            if (inventory == null || inventory.allSlots == null)
            {
                return "Open a storage first.";
            }

            List<Slot> slots = new List<Slot>();
            foreach (Slot slot in inventory.allSlots)
            {
                if (slot != null && slot.active && !slot.locked)
                {
                    slots.Add(slot);
                }
            }

            int count = Math.Max(1, (int)(slots.Count * DefaultFill));
            if (args != null && args.Length > 0)
            {
                if (args.Length > 1 || !int.TryParse(args[0], out count) || count < 1 || count > slots.Count)
                {
                    return "Slots must be a number from 1 to " + slots.Count + ". Syntax: " + Syntax;
                }
            }

            List<Item_Base> pool = ResolvePool();
            if (pool.Count == 0)
            {
                return "None of the test items exist.";
            }

            foreach (Slot slot in slots)
            {
                slot.SetItem(null);
            }

            Shuffle(slots);
            int used = 0;
            int oversized = 0;
            for (int i = 0; i < count; i++)
            {
                // Every fourth slot repeats an item that is already in the storage, so stacks of it can be merged.
                Item_Base item = i >= 4 && i % 4 == 0 ? slots[Random.Next(i)].itemInstance.baseItem : pool[Random.Next(pool.Count)];
                ItemInstance instance = CreateInstance(item, oversized == 0, out bool isUsed, out bool isOversized);
                slots[i].SetItem(instance);
                used += isUsed ? 1 : 0;
                oversized += isOversized ? 1 : 0;
            }

            return "Filled " + count + " of " + slots.Count + " slots: " + used + " used, " + oversized + " above the stack size.";
        }

        private static ItemInstance CreateInstance(Item_Base item, bool allowOversized, out bool isUsed, out bool isOversized)
        {
            int stackSize = Math.Max(1, item.settings_Inventory.StackSize);
            int maxUses = Math.Max(1, item.MaxUses);

            int amount = stackSize > 1 ? Random.Next(1, stackSize + 1) : 1;
            isOversized = allowOversized && stackSize > 1 && Random.Next(6) == 0;
            if (isOversized)
            {
                amount = stackSize + Random.Next(1, stackSize + 1);
            }

            // Half of the items with a durability bar are used.
            int uses = maxUses > 1 && Random.Next(2) == 0 ? Random.Next(1, maxUses) : maxUses;
            isUsed = uses < maxUses;
            return new ItemInstance(item, amount, uses);
        }

        private static List<Item_Base> ResolvePool()
        {
            List<Item_Base> items = new List<Item_Base>();
            foreach (string name in Pool)
            {
                Item_Base item = ItemManager.GetItemByName(name);
                if (item != null && item.settings_Inventory != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static void Shuffle(List<Slot> slots)
        {
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = Random.Next(i + 1);
                Slot swap = slots[i];
                slots[i] = slots[j];
                slots[j] = swap;
            }
        }
    }
}
