using HarmonyLib;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>Advances the client-side timers of cooking slots after the game's own update of the stand.</summary>
    [HarmonyPatch(typeof(Block_CookingStand), "Update")]
    internal static class CookingStandUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Block_CookingStand __instance)
        {
            CookingTimers.OnStandUpdated(__instance);
        }
    }

    /// <summary>Advances the client-side timers of cooking pots and juicers after the game's own update.</summary>
    [HarmonyPatch(typeof(CookingTable), "Update")]
    internal static class CookingTableUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CookingTable __instance)
        {
            CookingTimers.OnTableUpdated(__instance);
        }
    }
}
