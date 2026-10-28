using System.Collections.Generic;
using QoLChanges.GameState;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>One cost of a recipe: the amount needed for one craft and how much of it there is.</summary>
    internal struct IngredientAvailability
    {
        /// <summary>The item shown for the cost: the one there is most of when the cost accepts several items.</summary>
        public Item_Base Item;

        /// <summary>Sum of every item the cost accepts that the crafting menu can use now.</summary>
        public int Have;

        /// <summary>Sum of every item the cost accepts in the other storages.</summary>
        public int Stored;

        public int Need;

        public bool IsEnough => Have >= Need;
    }

    /// <summary>How many times a recipe can be crafted, now and with the items in storages.</summary>
    internal struct RecipeCrafts
    {
        /// <summary>False for a recipe that costs nothing.</summary>
        public bool HasCost;

        /// <summary>From the items the crafting menu can use now.</summary>
        public int Now;

        /// <summary>From those items and the items in the other storages.</summary>
        public int WithStorages;
    }

    /// <summary>How often a recipe can be crafted from the items in <see cref="CraftingStock"/>.</summary>
    internal static class RecipeAvailability
    {
        /// <summary>Fills <paramref name="ingredients"/> with the costs of <paramref name="item"/>.</summary>
        public static RecipeCrafts Read(Item_Base item, CraftingStock stock, List<IngredientAvailability> ingredients)
        {
            ingredients.Clear();
            RecipeCrafts crafts = new RecipeCrafts { Now = int.MaxValue, WithStorages = int.MaxValue };
            CostMultiple[] costs = item.settings_recipe != null ? item.settings_recipe.NewCost : null;
            if (costs == null)
            {
                return default(RecipeCrafts);
            }

            foreach (CostMultiple cost in costs)
            {
                if (cost == null || cost.items == null)
                {
                    continue;
                }

                Item_Base shown = null;
                int shownCount = -1;
                int have = 0;
                int stored = 0;
                foreach (Item_Base option in cost.items)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    int atHand = stock.AtHand(option);
                    int inStorages = stock.Stored(option);
                    have += atHand;
                    stored += inStorages;
                    if (atHand + inStorages > shownCount)
                    {
                        shown = option;
                        shownCount = atHand + inStorages;
                    }
                }

                if (shown == null)
                {
                    continue;
                }

                ingredients.Add(new IngredientAvailability { Item = shown, Have = have, Stored = stored, Need = cost.amount });
                if (cost.amount > 0)
                {
                    crafts.HasCost = true;
                    crafts.Now = System.Math.Min(crafts.Now, have / cost.amount);
                    crafts.WithStorages = System.Math.Min(crafts.WithStorages, (have + stored) / cost.amount);
                }
            }

            return crafts.HasCost ? crafts : default(RecipeCrafts);
        }
    }
}
