using HarmonyLib;

namespace QoLChanges.Features.StorageSort
{
    // Inventory.Sort is called only by the sort button of a storage and the gamepad action for it. The original runs
    // when the order is Vanilla, the inventory is not a storage or the mod's own sort fails.

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Sort))]
    internal static class InventorySortPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Inventory __instance)
        {
            return !StorageSortService.TrySort(__instance);
        }
    }

    // The button and the key that store the items of the inventory which the storage already holds.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.SortPlayerInventoryIntoStorage))]
    internal static class SortPlayerInventoryIntoStoragePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Inventory __instance)
        {
            StorageSortService.SortAfterStoring(__instance);
        }
    }
}
