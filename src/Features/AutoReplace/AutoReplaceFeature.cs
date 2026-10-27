using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// When the item in the selected hotbar slot is used up (a tool breaks, the last food is eaten, the last seed is
    /// planted), moves the same item, or another tier
    /// of a broken tool, from the main inventory into the slot and selects it. Client-side: only the local player's inventory changes, the same way
    /// as moving the item by hand; dropping, moving or storing items never triggers it.
    /// </summary>
    internal sealed class AutoReplaceFeature : Feature
    {
        public const string FeatureId = "AutoReplace";

        private readonly AutoReplaceConfig _config;

        public AutoReplaceFeature(AutoReplaceConfig config)
            : base(FeatureId, "Auto-replace tool", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ToggleSetting("Tools and weapons", () => _config.tools, value => _config.tools = value);
            yield return new ToggleSetting("Use other tiers", () => _config.useOtherTiers, value => _config.useOtherTiers = value);
            yield return new ToggleSetting("Food and drinks", () => _config.food, value => _config.food = value);
            yield return new ToggleSetting("Other consumables", () => _config.consumables, value => _config.consumables = value);
        }

        protected override void OnEnable()
        {
            AutoReplaceService.Activate(_config);

            if (_config.tools)
            {
                Patch(typeof(ToolBreakPatch));
            }

            if (_config.food)
            {
                Patch(typeof(FoodConsumePatch));
            }

            if (_config.consumables)
            {
                Patch(typeof(ConsumablePatches));
            }
        }

        protected override void OnDisable()
        {
            AutoReplaceService.Deactivate();
        }
    }
}
