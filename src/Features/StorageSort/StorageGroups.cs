using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QoLChanges.Features.StorageSort
{
    /// <summary>Groups of the By category order, in the order they appear in a sorted storage.</summary>
    internal enum StorageGroup
    {
        Resources,
        Tools,
        Weapons,
        Equipment,
        Food,
        Drinks,
        Seeds,
        Placeables,
        Other,
    }

    /// <summary>
    /// Puts items into <see cref="StorageGroup"/>s. The game's crafting category alone is not enough: most materials,
    /// food and drinks have none, seeds are in the Creative category. Items are grouped by their crafting category where
    /// it fits and by their properties otherwise, so items of other mods are grouped as well; a few vanilla items whose
    /// properties mislead are listed by name.
    /// </summary>
    internal static class StorageGroups
    {
        private const string CookingRecipesPath = "SO_CookingRecipes";
        private const string RecipeNotePrefix = "Placeable_Recipe_";

        private static readonly Dictionary<string, StorageGroup> VanillaGroups = new Dictionary<string, StorageGroup>
        {
            // Used with the fishing rod.
            { "FishingBait_Simple", StorageGroup.Tools },
            { "FishingBait_Advanced", StorageGroup.Tools },
            { "FishingBait_Expert", StorageGroup.Tools },

            // Bought from traders, so its crafting category is the Creative one.
            { "Compass", StorageGroup.Tools },

            // Fuel, paint and the trash cube are materials; only some of them are crafting ingredients.
            { "BioFuel", StorageGroup.Resources },
            { "Trashcube", StorageGroup.Resources },
            { "Color_Black", StorageGroup.Resources },
            { "Color_Blue", StorageGroup.Resources },
            { "Color_Red", StorageGroup.Resources },
            { "Color_White", StorageGroup.Resources },
            { "Color_Yellow", StorageGroup.Resources },
        };

        private static readonly AccessTools.FieldRef<PlantManager, List<Plant>> AllPlants =
            AccessTools.FieldRefAccess<PlantManager, List<Plant>>("allPlants");

        private static readonly Dictionary<Item_Base, StorageGroup> Groups = new Dictionary<Item_Base, StorageGroup>();
        private static readonly HashSet<Item_Base> CraftingIngredients = new HashSet<Item_Base>();
        private static readonly HashSet<Item_Base> CookingIngredients = new HashSet<Item_Base>();
        private static readonly HashSet<Item_Base> Containers = new HashSet<Item_Base>();
        private static readonly HashSet<Item_Base> Seeds = new HashSet<Item_Base>();

        private static int _itemCount = -1;
        private static PlantManager _plantManager;

        public static void Reset()
        {
            Groups.Clear();
            CraftingIngredients.Clear();
            CookingIngredients.Clear();
            Containers.Clear();
            Seeds.Clear();
            _itemCount = -1;
            _plantManager = null;
        }

        /// <summary>Call before a sort: the groups depend on the loaded items and the world.</summary>
        public static void Prepare()
        {
            Refresh();
        }

        public static StorageGroup Get(Item_Base item)
        {
            if (!Groups.TryGetValue(item, out StorageGroup group))
            {
                group = Classify(item);
                Groups[item] = group;
            }

            return group;
        }

        /// <summary>Rebuilds the lookups when items were added (another mod loaded) or another world was loaded.</summary>
        private static void Refresh()
        {
            List<Item_Base> items = ItemManager.GetAllItems();
            int itemCount = items != null ? items.Count : 0;
            PlantManager plantManager = GetPlantManager();
            if (itemCount == _itemCount && plantManager == _plantManager)
            {
                return;
            }

            Reset();
            _itemCount = itemCount;
            _plantManager = plantManager;

            if (items != null)
            {
                foreach (Item_Base item in items)
                {
                    AddItemLinks(item);
                }
            }

            foreach (SO_CookingTable_Recipe recipe in Resources.LoadAll<SO_CookingTable_Recipe>(CookingRecipesPath))
            {
                if (recipe != null)
                {
                    AddCosts(recipe.RecipeCost, CookingIngredients);
                }
            }

            List<Plant> plants = plantManager != null ? AllPlants(plantManager) : null;
            if (plants != null)
            {
                foreach (Plant plant in plants)
                {
                    if (plant != null && plant.item != null)
                    {
                        Seeds.Add(plant.item);
                    }
                }
            }
        }

        private static void AddItemLinks(Item_Base item)
        {
            if (item == null)
            {
                return;
            }

            if (item.settings_recipe != null)
            {
                AddCosts(item.settings_recipe.NewCost, CraftingIngredients);
            }

            // Empty canteens, cups, bottles, bowls and glasses are what is left of food and drinks.
            ItemInstance_Consumeable consumable = item.settings_consumeable;
            if (consumable != null && consumable.FoodType != FoodType.None && consumable.ItemAfterUse != null && consumable.ItemAfterUse.item != null)
            {
                Containers.Add(consumable.ItemAfterUse.item);
            }
        }

        private static void AddCosts(CostMultiple[] costs, HashSet<Item_Base> target)
        {
            if (costs == null)
            {
                return;
            }

            foreach (CostMultiple cost in costs)
            {
                if (cost == null || cost.items == null)
                {
                    continue;
                }

                foreach (Item_Base ingredient in cost.items)
                {
                    if (ingredient != null)
                    {
                        target.Add(ingredient);
                    }
                }
            }
        }

        private static StorageGroup Classify(Item_Base item)
        {
            string name = item.UniqueName ?? string.Empty;
            if (VanillaGroups.TryGetValue(name, out StorageGroup listed))
            {
                return listed;
            }

            // Recipe notes can be hung on a wall, but they are found and learnt like blueprints.
            if (name.StartsWith(RecipeNotePrefix, StringComparison.Ordinal))
            {
                return StorageGroup.Other;
            }

            ItemInstance_Recipe recipe = item.settings_recipe;
            if (recipe != null && recipe.IsBlueprint)
            {
                return StorageGroup.Other;
            }

            CraftingCategory category = recipe != null ? recipe.CraftingCategory : CraftingCategory.Nothing;
            string subCategory = recipe != null ? recipe.SubCategory : null;

            if ((item.settings_equipment != null && item.settings_equipment.EquipType != EquipSlotType.None) || category == CraftingCategory.Equipment)
            {
                return StorageGroup.Equipment;
            }

            if (category == CraftingCategory.Tools)
            {
                return StorageGroup.Tools;
            }

            if (category == CraftingCategory.Weapons)
            {
                return StorageGroup.Weapons;
            }

            if (Seeds.Contains(item) || (subCategory != null && subCategory.StartsWith("Seed", StringComparison.OrdinalIgnoreCase)))
            {
                return StorageGroup.Seeds;
            }

            ItemInstance_Consumeable consumable = item.settings_consumeable;
            if (consumable != null && consumable.FoodType != FoodType.None)
            {
                return IsDrink(consumable) ? StorageGroup.Drinks : StorageGroup.Food;
            }

            if (Containers.Contains(item) || (category == CraftingCategory.FoodWater && IsContainerSubCategory(subCategory)))
            {
                return StorageGroup.Drinks;
            }

            bool placeable = item.settings_buildable != null && item.settings_buildable.Placeable;

            // Healing salves; cooking pot ingredients that are not eaten on their own (spices, milk).
            if ((category == CraftingCategory.FoodWater && !placeable) || CookingIngredients.Contains(item))
            {
                return StorageGroup.Food;
            }

            if (category == CraftingCategory.Resources)
            {
                return StorageGroup.Resources;
            }

            if (placeable)
            {
                return StorageGroup.Placeables;
            }

            // Materials: crafting ingredients and ores that are smelted.
            if (CraftingIngredients.Contains(item) || (item.settings_cookable != null && item.settings_cookable.CookingResult != null && item.settings_cookable.CookingResult.item != null))
            {
                return StorageGroup.Resources;
            }

            // Single usable items crafted in another category: paddle, paint brush. Cassettes and other things from
            // traders and the Creative category are not tools.
            if (item.settings_usable != null && item.settings_usable.IsUsable() && GetStackSize(item) == 1 && IsCraftedCategory(category))
            {
                return StorageGroup.Tools;
            }

            return StorageGroup.Other;
        }

        /// <summary>Water, and food that quenches thirst more than hunger: smoothies, broth.</summary>
        private static bool IsDrink(ItemInstance_Consumeable consumable)
        {
            if (consumable.FoodType == FoodType.Water || consumable.FoodType == FoodType.SaltWater)
            {
                return true;
            }

            return consumable.FoodForm == FoodForm.Fluid && consumable.ThirstYield > consumable.HungerYield;
        }

        private static bool IsCraftedCategory(CraftingCategory category)
        {
            switch (category)
            {
                case CraftingCategory.Nothing:
                case CraftingCategory.Hidden:
                case CraftingCategory.CreativeMode:
                case CraftingCategory.Skin:
                    return false;
                default:
                    return true;
            }
        }

        private static bool IsContainerSubCategory(string subCategory)
        {
            return string.Equals(subCategory, "Containers", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subCategory, "Bottles", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetStackSize(Item_Base item)
        {
            return item.settings_Inventory != null ? item.settings_Inventory.StackSize : 1;
        }

        private static PlantManager GetPlantManager()
        {
            Network_Player player = ComponentManager<Network_Player>.Value;
            return player != null ? player.PlantManager : null;
        }
    }
}
