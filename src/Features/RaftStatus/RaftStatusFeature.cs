using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>
    /// Lines above the stat bars with the state of the raft while the player is on it: anchors, engines and their fuel.
    /// Client-side and read-only: every value is one the game shows on the anchors and engines themselves.
    /// </summary>
    internal sealed class RaftStatusFeature : Feature
    {
        public const string FeatureId = "RaftStatus";

        private readonly RaftStatusConfig _config;

        public RaftStatusFeature(RaftStatusConfig config)
            : base(FeatureId, "Raft status HUD", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ToggleSetting("Anchor", () => _config.anchor, value => _config.anchor = value);
            yield return new ToggleSetting("Engines", () => _config.engines, value => _config.engines = value);
            yield return new ToggleSetting("Thrust direction", () => _config.thrustDirection, value => _config.thrustDirection = value);
            yield return new ToggleSetting("Engine power", () => _config.enginePower, value => _config.enginePower = value);
            yield return new ToggleSetting("Fuel", () => _config.fuel, value => _config.fuel = value);
            yield return new IntSliderSetting(
                "HUD size",
                RaftStatusConfig.MinScale / RaftStatusConfig.ScaleStep,
                RaftStatusConfig.MaxScale / RaftStatusConfig.ScaleStep,
                () => _config.scale / RaftStatusConfig.ScaleStep,
                value => _config.scale = value * RaftStatusConfig.ScaleStep,
                value => value * RaftStatusConfig.ScaleStep + "%");
        }

        protected override void OnEnable()
        {
            RaftStatusHud.Activate(_config);
        }

        protected override void OnDisable()
        {
            RaftStatusHud.Deactivate();
        }
    }
}
