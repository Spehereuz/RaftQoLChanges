using HarmonyLib;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// The preview of a scarecrow the local player is placing (<c>BlockCreator.selectedBlock</c>) and whether it can be
    /// placed where it is. The game decides that every frame in <c>BlockCreator.Update</c> and only shows it by giving the
    /// preview the green or the red ghost material, so the material it sets is remembered.
    /// </summary>
    internal static class ScarecrowGhost
    {
        private static OccupyingComponent _component;
        private static bool _placeable;

        /// <summary>The visible preview of a scarecrow, or null when the player is not placing one.</summary>
        public static Scarecrow Find(Network_Player player)
        {
            BlockCreator creator = player.BlockCreator;
            Block block = creator != null ? creator.selectedBlock : null;
            if (block == null || block.hasBeenPlaced || !block.gameObject.activeInHierarchy)
            {
                return null;
            }

            return block.GetComponent<Scarecrow>();
        }

        /// <summary>Whether the game shows the preview green, so it would be placed where it is.</summary>
        public static bool CanBePlaced(Scarecrow ghost)
        {
            Block block = ghost.GetComponent<Block>();
            return block != null && block.occupyingComponent != null && block.occupyingComponent == _component && _placeable;
        }

        public static void Clear()
        {
            _component = null;
            _placeable = false;
        }

        internal static void OnMaterialSet(OccupyingComponent component, UnityEngine.Material material)
        {
            GameManager manager = SingletonGeneric<GameManager>.Singleton;
            if (manager == null || material == null)
            {
                return;
            }

            if (material == manager.ghostMaterialGreen)
            {
                _component = component;
                _placeable = true;
            }
            else if (material == manager.ghostMaterialRed)
            {
                _component = component;
                _placeable = false;
            }
        }
    }

    // Only the local player's BlockCreator colours its preview, every frame while it is shown.
    [HarmonyPatch(typeof(OccupyingComponent), nameof(OccupyingComponent.SetNewMaterial))]
    internal static class OccupyingComponentSetMaterialPatch
    {
        [HarmonyPostfix]
        private static void Postfix(OccupyingComponent __instance, UnityEngine.Material material)
        {
            ScarecrowGhost.OnMaterialSet(__instance, material);
        }
    }
}
