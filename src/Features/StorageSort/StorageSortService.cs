using System;
using QoLChanges.Core;

namespace QoLChanges.Features.StorageSort
{
    /// <summary>Decides whether an inventory is sorted by the mod and runs <see cref="StorageSorter"/> on it.</summary>
    internal static class StorageSortService
    {
        private static StorageSortConfig _config;
        private static bool _errorLogged;

        public static void Activate(StorageSortConfig config)
        {
            _config = config;
            _errorLogged = false;
            StorageGroups.Reset();
        }

        public static void Deactivate()
        {
            _config = null;
            StorageGroups.Reset();
        }

        /// <summary>Sorts a storage in the configured order. False lets the game sort it itself.</summary>
        public static bool TrySort(Inventory inventory)
        {
            if (_config == null || _config.order == StorageSortOrder.Vanilla)
            {
                return false;
            }

            try
            {
                return IsStorage(inventory) && StorageSorter.Sort(inventory, _config.order);
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(StorageSortFeature.FeatureId + ": failed to sort a storage, the game sorted it instead.", e);
                }

                return false;
            }
        }

        /// <summary>
        /// The inventory of a placed storage. The player inventory, the trash can and inventories of other blocks are
        /// left to the game.
        /// </summary>
        private static bool IsStorage(Inventory inventory)
        {
            if (inventory == null || inventory is PlayerInventory || inventory is Inventory_Trashcan)
            {
                return false;
            }

            foreach (Storage_Small storage in StorageManager.allStorages)
            {
                if (storage != null && storage.GetInventoryReference() == inventory)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
