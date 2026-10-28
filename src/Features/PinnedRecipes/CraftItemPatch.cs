using HarmonyLib;

namespace QoLChanges.Features.PinnedRecipes
{
    // The craft button of the crafting menu. The item is read before crafting, as the game reads it.
    [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftItem))]
    internal static class CraftItemPatch
    {
        [HarmonyPrefix]
        private static void Prefix(CraftingMenu __instance, out Item_Base __state)
        {
            __state = __instance.selectedRecipeBox != null ? __instance.selectedRecipeBox.ItemToCraft : null;
        }

        [HarmonyPostfix]
        private static void Postfix(Item_Base __state)
        {
            PinnedRecipesService.OnCrafted(__state);
        }
    }
}
