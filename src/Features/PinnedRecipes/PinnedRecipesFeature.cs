using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// Up to three recipes on the HUD with the amounts of their ingredients and how many times they can be crafted.
    /// Client-side and read-only: the items are counted the way the crafting menu counts them.
    /// </summary>
    internal sealed class PinnedRecipesFeature : Feature
    {
        public const string FeatureId = "PinnedRecipes";

        private readonly PinnedRecipesConfig _config;

        public PinnedRecipesFeature(PinnedRecipesConfig config)
            : base(FeatureId, "Pinned recipes", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ToggleSetting("Show ingredients", () => _config.showIngredients, value => _config.showIngredients = value);
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
            PinnedRecipesHud.Activate(_config);
        }

        protected override void OnDisable()
        {
            PinnedRecipesHud.Deactivate();
        }
    }
}
