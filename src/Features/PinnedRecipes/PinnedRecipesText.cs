using QoLChanges.UI;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>Texts of the pinned recipe lines. Names and needed amounts are white, what is missing is red.</summary>
    internal static class PinnedRecipesText
    {
        /// <summary>"Metal axe  ×2": the item name and how many times it can be crafted, red at ×0.</summary>
        public static string Recipe(Item_Base item, int? crafts)
        {
            string name = item.settings_Inventory != null ? item.settings_Inventory.DisplayName : item.UniqueName;
            if (!crafts.HasValue)
            {
                return name;
            }

            string times = "×" + crafts.Value;
            return name + "  " + StatusText.Colored(times, crafts.Value > 0 ? StatusText.Ready : StatusText.Problem);
        }

        /// <summary>"12/4": the amount there is, red when it is not enough for one craft, and the amount one craft needs.</summary>
        public static string Ingredient(IngredientAvailability ingredient)
        {
            string have = ingredient.Have.ToString();
            if (!ingredient.IsEnough)
            {
                have = StatusText.Colored(have, StatusText.Problem);
            }

            return have + "/" + ingredient.Need;
        }
    }
}
