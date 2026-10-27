using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>
    /// Lines above the stat bars with the state of the raft while the player is on it: anchors. Client-side and
    /// read-only: every value is one the game shows on the anchors themselves.
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
