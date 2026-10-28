using System;
using System.Collections.Generic;
using QoLChanges.Core;
using QoLChanges.GameState;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// Fills the pinned recipes section of the HUD panel: every pinned recipe that is learned in the current world, how
    /// many times it can be crafted and, below it, its ingredients, with the items in storages when they are counted.
    /// Read-only: nothing is changed or sent.
    /// </summary>
    internal sealed class PinnedRecipesHud : MonoBehaviour
    {
        private const string HostName = "QoLChanges_PinnedRecipes";
        private const string SectionName = "PinnedRecipes";

        // Above the raft status, which stays next to the stat bars.
        private const int SectionOrder = 1;
        private const float RefreshInterval = 0.1f;

        private static PinnedRecipesHud _instance;

        private readonly CraftingStock _stock = new CraftingStock();
        private readonly List<IngredientAvailability> _ingredients = new List<IngredientAvailability>();
        private readonly Dictionary<string, Item_Base> _items = new Dictionary<string, Item_Base>();
        private PinnedRecipesConfig _config;
        private HudSection _section;
        private float _nextRefresh;
        private bool _errorLogged;

        public static void Activate(PinnedRecipesConfig config)
        {
            Deactivate();

            GameObject host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<PinnedRecipesHud>();
            _instance._config = config;
            _instance._section = HudPanel.AddSection(SectionName, SectionOrder);
            _instance._section.Scale = config.scale / 100f;
        }

        public static void Deactivate()
        {
            if (_instance != null)
            {
                // Immediate, so that a HUD activated again in the same frame (settings reapplied) is the only one.
                DestroyImmediate(_instance.gameObject);
            }

            _instance = null;
        }

        private void OnDestroy()
        {
            HudPanel.RemoveSection(_section);
            _section = null;
            _stock.Clear();
            _items.Clear();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + RefreshInterval;
            try
            {
                Refresh();
            }
            catch (Exception e)
            {
                _section.Visible = false;
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(PinnedRecipesFeature.FeatureId + ": failed to update the HUD.", e);
                }
            }
        }

        private void Refresh()
        {
            if (_config.recipes.Count == 0 || !_stock.Read(_config.countAllStorages))
            {
                _section.Visible = false;
                return;
            }

            int count = 0;
            foreach (string name in _config.recipes)
            {
                Item_Base item = GetItem(name);

                // Recipes of mods that are not loaded and recipes not learned in this world stay pinned but hidden.
                if (item == null || item.settings_recipe == null || !item.settings_recipe.CanCraft)
                {
                    continue;
                }

                RecipeCrafts crafts = RecipeAvailability.Read(item, _stock, _ingredients);
                HudLine line = _section.GetLine(count++);
                line.Icon = item.settings_Inventory != null ? item.settings_Inventory.Sprite : null;
                line.Text = PinnedRecipesText.Recipe(item, crafts);
                line.ArrowAngle = null;
                line.TrimSegments(0);

                if (_config.showIngredients && _ingredients.Count > 0)
                {
                    HudLine ingredients = _section.GetLine(count++);
                    ingredients.Icon = null;
                    ingredients.Text = null;
                    ingredients.ArrowAngle = null;
                    for (int i = 0; i < _ingredients.Count; i++)
                    {
                        HudSegment segment = ingredients.GetSegment(i);
                        Item_Base ingredient = _ingredients[i].Item;
                        segment.Icon = ingredient.settings_Inventory != null ? ingredient.settings_Inventory.Sprite : null;
                        segment.Text = PinnedRecipesText.Ingredient(_ingredients[i]);
                    }

                    ingredients.TrimSegments(_ingredients.Count);
                }
            }

            _section.TrimLines(count);
            _section.Visible = count > 0;
        }

        private Item_Base GetItem(string uniqueName)
        {
            if (!_items.TryGetValue(uniqueName, out Item_Base item) || item == null)
            {
                item = ItemManager.GetItemByName(uniqueName);
                if (item != null)
                {
                    _items[uniqueName] = item;
                }
            }

            return item;
        }
    }
}
