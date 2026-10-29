using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// Up to three recipes on the HUD with the amounts of their ingredients and how many times they can be crafted,
    /// pinned with a button under the selected recipe of the crafting menu. Client-side and read-only: the items are
    /// counted the way the crafting menu counts them and the pinned recipes are kept in the config.
    /// </summary>
    internal sealed class PinnedRecipesFeature : Feature
    {
        public const string FeatureId = "PinnedRecipes";

        // Index = PinnedRecipesPosition value.
        private static readonly string[] PositionNames = { "Top right", "Bottom left" };

        // Index = PinnedRecipesNotifications value.
        private static readonly string[] NotificationNames = { "Off", "Hide recipes", "Move recipes down", "Move notifications down" };

        private readonly PinnedRecipesConfig _config;

        public PinnedRecipesFeature(PinnedRecipesConfig config)
            : base(FeatureId, "Pinned recipes", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ChoiceSetting(
                "Position",
                PositionNames,
                () => (int)_config.position,
                index => _config.position = (PinnedRecipesPosition)index)
            {
                RebuildsPage = true,
            };
            yield return new ChoiceSetting(
                "Notifications",
                NotificationNames,
                () => (int)_config.notifications,
                index => _config.notifications = (PinnedRecipesNotifications)index)
            {
                AvailableWhen = () => _config.position == PinnedRecipesPosition.TopRight,
            };
            yield return new ToggleSetting("Count all storages", () => _config.countAllStorages, value => _config.countAllStorages = value);
            yield return new ToggleSetting("Show ingredients", () => _config.showIngredients, value => _config.showIngredients = value);
            yield return new ToggleSetting("Unpin after crafting", () => _config.unpinAfterCrafting, value => _config.unpinAfterCrafting = value);
            yield return new ToggleSetting("Show title", () => _config.showTitle, value => _config.showTitle = value);
            yield return new IntSliderSetting(
                "Background opacity",
                0,
                100 / PinnedRecipesConfig.OpacityStep,
                () => _config.backgroundOpacity / PinnedRecipesConfig.OpacityStep,
                value => _config.backgroundOpacity = value * PinnedRecipesConfig.OpacityStep,
                value => value * PinnedRecipesConfig.OpacityStep + "%");
            yield return new IntSliderSetting(
                "HUD size",
                PinnedRecipesConfig.MinScale / PinnedRecipesConfig.ScaleStep,
                PinnedRecipesConfig.MaxScale / PinnedRecipesConfig.ScaleStep,
                () => _config.scale / PinnedRecipesConfig.ScaleStep,
                value => _config.scale = value * PinnedRecipesConfig.ScaleStep,
                value => value * PinnedRecipesConfig.ScaleStep + "%");
        }

        protected override void OnEnable()
        {
            PinnedRecipesService.Activate(_config, SaveConfig);
            PinnedRecipesHud.Activate(_config);
            PinButton.Activate();
            Patch(typeof(CraftItemPatch));
        }

        protected override void OnDisable()
        {
            PinButton.Deactivate();
            PinnedRecipesHud.Deactivate();
            PinnedRecipesService.Deactivate();
        }
    }
}
