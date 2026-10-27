using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// Draws where scarecrows keep seagulls away from crops while the hammer or a scarecrow is selected. Client-side and
    /// purely visual: the zone is the one the seagulls use on the host, the game just never shows it.
    /// </summary>
    internal sealed class ScarecrowRadiusFeature : Feature
    {
        public const string FeatureId = "ScarecrowRadius";

        private readonly ScarecrowRadiusConfig _config;

        public ScarecrowRadiusFeature(ScarecrowRadiusConfig config)
            : base(FeatureId, "Scarecrow radius", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ToggleSetting("With hammer", () => _config.withHammer, value => _config.withHammer = value);
            yield return new ToggleSetting("With scarecrow", () => _config.withScarecrow, value => _config.withScarecrow = value);
            yield return new IntSliderSetting(
                "Line width",
                ScarecrowRadiusConfig.MinLineWidth,
                ScarecrowRadiusConfig.MaxLineWidth,
                () => _config.lineWidth,
                value => _config.lineWidth = value,
                value => value + " cm");
        }

        protected override void OnEnable()
        {
            ScarecrowRings.Activate(_config);
        }

        protected override void OnDisable()
        {
            ScarecrowRings.Deactivate();
        }
    }
}
