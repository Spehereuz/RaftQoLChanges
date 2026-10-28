using System;
using QoLChanges.Core;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>Pins and unpins recipes; every change is saved to the config at once.</summary>
    internal static class PinnedRecipesService
    {
        private static PinnedRecipesConfig _config;
        private static Action _save;

        public static int Count => _config != null ? _config.recipes.Count : 0;

        public static bool IsFull => Count >= PinnedRecipesConfig.MaxPinned;

        public static void Activate(PinnedRecipesConfig config, Action save)
        {
            _config = config;
            _save = save;
        }

        public static void Deactivate()
        {
            _config = null;
            _save = null;
        }

        public static bool IsPinned(Item_Base item)
        {
            return _config != null && item != null && _config.recipes.Contains(item.UniqueName);
        }

        /// <summary>Pins <paramref name="item"/> after the recipes pinned before it; false when it cannot be pinned.</summary>
        public static bool Pin(Item_Base item)
        {
            if (_config == null || item == null || IsPinned(item) || IsFull)
            {
                return false;
            }

            _config.recipes.Add(item.UniqueName);
            Save();
            return true;
        }

        public static bool Unpin(Item_Base item)
        {
            if (_config == null || item == null || !_config.recipes.Remove(item.UniqueName))
            {
                return false;
            }

            Save();
            return true;
        }

        /// <summary>
        /// Unpins an item crafted with the craft button when <see cref="PinnedRecipesConfig.unpinAfterCrafting"/> is on.
        /// Only the pinned variant counts: crafting another skin of the same recipe keeps it pinned.
        /// </summary>
        public static void OnCrafted(Item_Base item)
        {
            if (_config == null || !_config.unpinAfterCrafting)
            {
                return;
            }

            // Runs after the game has crafted the item; an exception here must not reach the game.
            try
            {
                Unpin(item);
            }
            catch (Exception e)
            {
                ModLog.Error(PinnedRecipesFeature.FeatureId + ": failed to unpin a crafted recipe.", e);
            }
        }

        private static void Save()
        {
            if (_save != null)
            {
                _save();
            }
        }
    }
}
