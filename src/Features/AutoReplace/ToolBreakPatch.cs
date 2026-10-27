using HarmonyLib;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// Every tool and weapon loses durability through PlayerInventory.RemoveDurabillityFromHotSlot, which calls
    /// RemoveStacksFromSlot for the selected hotbar slot; equipment uses the same method for its own slots.
    /// </summary>
    [HarmonyPatch(typeof(PlayerInventory), "RemoveStacksFromSlot", new[] { typeof(Slot), typeof(int) })]
    internal static class ToolBreakPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Slot slot, out HotbarSnapshot __state)
        {
            __state = AutoReplaceService.CaptureSelected(slot);
        }

        [HarmonyPostfix]
        private static void Postfix(HotbarSnapshot __state)
        {
            AutoReplaceService.Complete(__state, ReplaceCategory.Tools);
        }
    }
}
