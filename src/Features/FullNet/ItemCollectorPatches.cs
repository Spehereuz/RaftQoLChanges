using HarmonyLib;

namespace QoLChanges.Features.FullNet
{
    // A net changes only when an item is collected or the net is emptied. Both run on every player's game: on the
    // host when it collects, on the others when the host tells them, and when a world or a joined game is loaded.

    [HarmonyPatch(typeof(ItemCollector), nameof(ItemCollector.CollectItem))]
    internal static class ItemCollectorCollectPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ItemCollector __instance, bool __result)
        {
            if (__result)
            {
                FullNetTint.Refresh(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ItemCollector), "ClearCollectedItems")]
    internal static class ItemCollectorClearPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ItemCollector __instance)
        {
            FullNetTint.Refresh(__instance);
        }
    }
}
