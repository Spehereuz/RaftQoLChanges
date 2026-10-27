using System;
using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.DurabilityIndicator
{
    /// <summary>
    /// Shows the durability of tools, water containers and food portions, and the charge of batteries, as text
    /// on their slots, next to the vanilla durability bar. Purely visual and client-side: the same value is
    /// already shown by the bar.
    /// </summary>
    internal sealed class DurabilityIndicatorFeature : Feature
    {
        public const string FeatureId = "DurabilityIndicator";

        // Index = DurabilityDisplayMode value.
        private static readonly string[] ModeNames = { "Off", "Percent", "Remaining", "Remaining / max" };

        private readonly DurabilityIndicatorConfig _config;

        public DurabilityIndicatorFeature(DurabilityIndicatorConfig config)
            : base(FeatureId, "Durability indicator", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return ModeSetting("Tools and equipment", () => _config.tools, value => _config.tools = value);
            yield return ModeSetting("Water containers", () => _config.water, value => _config.water = value);
            yield return ModeSetting("Food portions", () => _config.food, value => _config.food = value);
            yield return ModeSetting("Batteries", () => _config.batteries, value => _config.batteries = value);
            yield return new ToggleSetting(
                "Show at full durability",
                () => _config.showAtFullDurability,
                value => _config.showAtFullDurability = value);
        }

        protected override void OnEnable()
        {
            Patch(typeof(SlotRefreshComponentsPatch));
            DurabilityLabels.Activate(_config);
        }

        protected override void OnDisable()
        {
            DurabilityLabels.Deactivate();
        }

        private static ChoiceSetting ModeSetting(string label, Func<DurabilityDisplayMode> getter, Action<DurabilityDisplayMode> setter)
        {
            return new ChoiceSetting(label, ModeNames, () => (int)getter(), index => setter((DurabilityDisplayMode)index));
        }
    }
}
