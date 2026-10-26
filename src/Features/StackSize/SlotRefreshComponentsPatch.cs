using HarmonyLib;

namespace QoLChanges.Features.StackSize
{
    /// <summary>
    /// Slot.RefreshComponents runs after every SetItem/AddItem, so any item instance that enters a slot
    /// (picked up, loaded from a save, received over the network) gets the current stack size.
    /// </summary>
    [HarmonyPatch(typeof(Slot), nameof(Slot.RefreshComponents))]
    internal static class SlotRefreshComponentsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Slot __instance)
        {
            StackSizeAccess.SyncSlot(__instance);
        }
    }
}
