using System;
using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>What used up the item in the selected hotbar slot; every category has its own setting.</summary>
    internal enum ReplaceCategory
    {
        Tools,
        Food,
        Consumables,
    }

    /// <summary>
    /// Refills the selected hotbar slot after the game has used up the item in it. Patches of the game actions that
    /// use items take a <see cref="HotbarSnapshot"/> before the action and pass it to <see cref="Complete"/> after it;
    /// when the slot became empty, the same item is moved into it from the main inventory at the end of the frame.
    /// Only the local player's own inventory is changed, the same way as moving an item by hand.
    /// </summary>
    internal static class AutoReplaceService
    {
        private const string HostName = "QoLChanges_AutoReplace";

        private static readonly AccessTools.FieldRef<PlayerInventory, bool> BackpackEquipped =
            AccessTools.FieldRefAccess<PlayerInventory, bool>("backpackEquipped");

        private static AutoReplaceConfig _config;
        private static GameObject _host;
        private static bool _hasPending;
        private static HotbarSnapshot _pending;
        private static ReplaceCategory _pendingCategory;
        private static bool _errorLogged;

        public static void Activate(AutoReplaceConfig config)
        {
            Deactivate();

            _config = config;
            _errorLogged = false;

            _host = new GameObject(HostName);
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _host.AddComponent<AutoReplaceRunner>();
        }

        public static void Deactivate()
        {
            _config = null;
            _hasPending = false;
            _pending = default(HotbarSnapshot);

            if (_host != null)
            {
                UnityEngine.Object.DestroyImmediate(_host);
            }

            _host = null;
        }

        /// <summary>The selected hotbar slot of the local player; invalid when it is empty or there is no player.</summary>
        public static HotbarSnapshot Capture()
        {
            if (_config == null)
            {
                return default(HotbarSnapshot);
            }

            // Runs inside game actions; an exception here would break them.
            try
            {
                PlayerInventory inventory = GetLocalInventory();
                if (inventory == null || inventory.hotbar == null)
                {
                    return default(HotbarSnapshot);
                }

                int index = inventory.hotbar.GetSelectedSlotIndex();
                Slot slot = inventory.GetSlot(index);
                if (slot == null || slot.IsEmpty)
                {
                    return default(HotbarSnapshot);
                }

                return new HotbarSnapshot(slot, index, slot.itemInstance.baseItem);
            }
            catch (Exception e)
            {
                LogError(e);
                return default(HotbarSnapshot);
            }
        }

        /// <summary>Like <see cref="Capture()"/>, but only when <paramref name="slot"/> is the selected hotbar slot.</summary>
        public static HotbarSnapshot CaptureSelected(Slot slot)
        {
            if (slot == null)
            {
                return default(HotbarSnapshot);
            }

            HotbarSnapshot snapshot = Capture();
            return snapshot.IsValid && snapshot.Slot == slot ? snapshot : default(HotbarSnapshot);
        }

        /// <summary>Schedules a refill when the action emptied the slot of <paramref name="snapshot"/>.</summary>
        public static void Complete(HotbarSnapshot snapshot, ReplaceCategory category)
        {
            if (_config == null || !snapshot.IsValid)
            {
                return;
            }

            try
            {
                // A slot that still holds something (the rest of a stack, an empty canteen) is left as it is.
                if (!snapshot.Slot.IsEmpty)
                {
                    return;
                }

                _pending = snapshot;
                _pendingCategory = category;
                _hasPending = true;
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        public static void ProcessPending()
        {
            if (!_hasPending)
            {
                return;
            }

            HotbarSnapshot snapshot = _pending;
            _hasPending = false;
            _pending = default(HotbarSnapshot);

            try
            {
                Refill(snapshot, _pendingCategory);
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private static void Refill(HotbarSnapshot snapshot, ReplaceCategory category)
        {
            PlayerInventory inventory = GetLocalInventory();
            if (inventory == null || inventory.hotbar == null || inventory.hotbar.GetSelectedSlotIndex() != snapshot.Index)
            {
                return;
            }

            // The player may have put something into the slot or switched worlds since the item was used up.
            Slot target = inventory.GetSlot(snapshot.Index);
            if (target == null || target != snapshot.Slot || !target.IsEmpty)
            {
                return;
            }

            Slot source = FindReplacement(inventory, snapshot.Item, category);
            if (source == null)
            {
                return;
            }

            target.SetItem(source.itemInstance);
            source.SetItem(null);
            inventory.hotbar.ReselectCurrentSlot();
        }

        private static Slot FindReplacement(PlayerInventory inventory, Item_Base usedItem, ReplaceCategory category)
        {
            int uniqueIndex = usedItem.UniqueIndex;
            Slot exact = FindBest(inventory, item => item.UniqueIndex == uniqueIndex, category);
            if (exact != null || category != ReplaceCategory.Tools || !_config.useOtherTiers)
            {
                return exact;
            }

            foreach (string tierName in ToolTiers.GetReplacementOrder(usedItem.UniqueName))
            {
                Slot other = FindBest(inventory, item => item.UniqueName == tierName, category);
                if (other != null)
                {
                    return other;
                }
            }

            return null;
        }

        /// <summary>
        /// The slot of the main inventory with a matching item that is used first: the most worn tool, so worn copies
        /// are used up before fresh ones, or the smallest stack, so a slot is freed sooner. Ties keep inventory order.
        /// </summary>
        private static Slot FindBest(PlayerInventory inventory, Func<Item_Base, bool> matches, ReplaceCategory category)
        {
            List<Slot> slots = inventory.allSlots;
            if (slots == null)
            {
                return null;
            }

            bool backpackEquipped = BackpackEquipped(inventory);
            Slot best = null;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                if (!IsMainInventorySlot(inventory, slot, i, backpackEquipped) || slot.IsEmpty)
                {
                    continue;
                }

                ItemInstance instance = slot.itemInstance;
                if (instance.baseItem == null || !matches(instance.baseItem))
                {
                    continue;
                }

                if (best == null || IsUsedBefore(instance, best.itemInstance, category))
                {
                    best = slot;
                }
            }

            return best;
        }

        private static bool IsUsedBefore(ItemInstance candidate, ItemInstance current, ReplaceCategory category)
        {
            return category == ReplaceCategory.Tools ? candidate.Uses < current.Uses : candidate.Amount < current.Amount;
        }

        /// <summary>Inventory and backpack slots; never the hotbar, equipment or slots of an unequipped backpack.</summary>
        private static bool IsMainInventorySlot(PlayerInventory inventory, Slot slot, int index, bool backpackEquipped)
        {
            if (slot == null || index < inventory.hotslotCount || !slot.active || slot.locked || slot is Slot_Equip)
            {
                return false;
            }

            switch (slot.slotType)
            {
                case SlotType.Normal:
                    return true;
                case SlotType.Backpack:
                    return backpackEquipped;
                default:
                    return false;
            }
        }

        private static PlayerInventory GetLocalInventory()
        {
            Network_Player player = ComponentManager<Network_Player>.Value;
            return player != null ? player.Inventory : null;
        }

        private static void LogError(Exception e)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModLog.Error(AutoReplaceFeature.FeatureId + ": failed to replace an item.", e);
            }
        }
    }
}
