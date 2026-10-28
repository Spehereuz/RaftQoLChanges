using System.Collections.Generic;
using QoLChanges.GameState;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>One cost of a recipe: the amount needed for one craft and how much of it there is.</summary>
    internal struct IngredientAvailability
    {
        /// <summary>The item shown for the cost: the one there is most of when the cost accepts several items.</summary>
        public Item_Base Item;

        /// <summary>Sum of every item the cost accepts.</summary>
        public int Have;

        public int Need;

        public bool IsEnough => Have >= Need;
    }

    /// <summary>How often a recipe can be crafted from the items in <see cref="CraftingStock"/>.</summary>
    internal static class RecipeAvailability
    {
        /// <summary>
        /// Fills <paramref name="ingredients"/> with the costs of <paramref name="item"/> and returns how many times it can
        /// be crafted, or null when it costs nothing.
        /// </summary>
        public static int? Read(Item_Base item, CraftingStock stock, List<IngredientAvailability> ingredients)
        {
            ingredients.Clear();
            CostMultiple[] costs = item.settings_recipe != null ? item.settings_recipe.NewCost : null;
            if (costs == null)
            {
                return null;
            }

            int? crafts = null;
            foreach (CostMultiple cost in costs)
            {
                if (cost == null || cost.items == null)
                {
                    continue;
                }

                Item_Base shown = null;
                int shownCount = -1;
                int have = 0;
                foreach (Item_Base option in cost.items)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    int count = stock.Count(option);
                    have += count;
                    if (count > shownCount)
                    {
                        shown = option;
                        shownCount = count;
                    }
                }

                if (shown == null)
                {
                    continue;
                }

                ingredients.Add(new IngredientAvailability { Item = shown, Have = have, Need = cost.amount });
                if (cost.amount > 0)
                {
                    int times = have / cost.amount;
                    crafts = crafts.HasValue ? System.Math.Min(crafts.Value, times) : times;
                }
            }

            return crafts;
        }
    }
}
