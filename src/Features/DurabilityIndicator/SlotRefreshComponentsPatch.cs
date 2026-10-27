using HarmonyLib;

namespace QoLChanges.Features.DurabilityIndicator
{
    /// <summary>
    /// Slot.RefreshComponents redraws the item icon, amount text and durability bar after every change of the
    /// slot, including uses spent by IncrementUses, so the indicator is updated right after it.
    /// </summary>
    [HarmonyPatch(typeof(Slot), nameof(Slot.RefreshComponents))]
    internal static class SlotRefreshComponentsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Slot __instance)
        {
            DurabilityLabels.Refresh(__instance);
        }
    }
}
