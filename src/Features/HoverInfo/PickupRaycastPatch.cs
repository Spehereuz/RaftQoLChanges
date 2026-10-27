using HarmonyLib;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Pickup.Update of the local player raycasts once per frame for the interactable under the crosshair and
    /// lets it show its prompt; the hover info line uses the same result, so it follows exactly what the game
    /// considers aimed at.
    /// </summary>
    [HarmonyPatch(typeof(Pickup), "RaycastForRayInteractables")]
    internal static class PickupRaycastPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RaycastInteractable __result)
        {
            HoverInfoService.OnInteractableRaycast(__result);
        }
    }
}
