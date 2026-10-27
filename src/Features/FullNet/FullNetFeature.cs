using QoLChanges.Core;

namespace QoLChanges.Features.FullNet
{
    /// <summary>
    /// Tints collection nets red while they are full and collect nothing more. Client-side and purely visual: the
    /// game already shows the collected items inside the net, the tint only saves counting them.
    /// </summary>
    internal sealed class FullNetFeature : Feature
    {
        public const string FeatureId = "FullNet";

        public FullNetFeature(FullNetConfig config)
            : base(FeatureId, "Full net indicator", config)
        {
        }

        protected override void OnEnable()
        {
            FullNetTint.Activate();
            Patch(typeof(ItemCollectorCollectPatch));
            Patch(typeof(ItemCollectorClearPatch));
        }

        protected override void OnDisable()
        {
            FullNetTint.Deactivate();
        }
    }
}
