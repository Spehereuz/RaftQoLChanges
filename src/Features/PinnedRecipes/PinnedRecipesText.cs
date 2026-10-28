using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// Texts of the pinned recipe lines. Names and needed amounts are white, what is missing now is red and what lies in
    /// the other storages is blue.
    /// </summary>
    internal static class PinnedRecipesText
    {
        /// <summary>Items in storages that are not open; the cyan of water container bars, like water amounts.</summary>
        public static readonly Color Stored = StatusText.Water;

        /// <summary>
        /// "Metal axe  ×2": the item name and how many times it can be crafted now, red at ×0, followed by "(×5)" when the
        /// storages allow more.
        /// </summary>
        public static string Recipe(Item_Base item, RecipeCrafts crafts)
        {
            string name = item.settings_Inventory != null ? item.settings_Inventory.DisplayName : item.UniqueName;
            if (!crafts.HasCost)
            {
                return name;
            }

            string text = name + "  " + StatusText.Colored(Times(crafts.Now), crafts.Now > 0 ? StatusText.Ready : StatusText.Problem);
            if (crafts.WithStorages > crafts.Now)
            {
                text += " " + StatusText.Colored("(" + Times(crafts.WithStorages) + ")", Stored);
            }

            return text;
        }

        /// <summary>
        /// "12/4": the amount there is now, red when it is not enough for one craft, and the amount one craft needs,
        /// followed by "+40" in the other storages.
        /// </summary>
        public static string Ingredient(IngredientAvailability ingredient)
        {
            string have = ingredient.Have.ToString();
            if (!ingredient.IsEnough)
            {
                have = StatusText.Colored(have, StatusText.Problem);
            }

            string text = have + "/" + ingredient.Need;
            if (ingredient.Stored > 0)
            {
                text += " " + StatusText.Colored("+" + ingredient.Stored, Stored);
            }

            return text;
        }

        private static string Times(int crafts)
        {
            return "×" + crafts;
        }
    }
}
