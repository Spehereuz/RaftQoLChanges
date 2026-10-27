using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Shows the state of the object under the crosshair in a line below it: battery charge, plant growth, water
    /// purifiers, cooking, smelting. Client-side and read-only: every value is one the game already shows in a
    /// less precise way (battery slot lights, plant size, water levels, cooking models), nothing is changed or sent
    /// over the network.
    /// </summary>
    internal sealed class HoverInfoFeature : Feature
    {
        public const string FeatureId = "HoverInfo";

        // Index = BatteryDisplayMode value.
        private static readonly string[] BatteryModeNames = { "Off", "Percent", "Remaining", "Remaining / max" };

        private readonly HoverInfoConfig _config;

        public HoverInfoFeature(HoverInfoConfig config)
            : base(FeatureId, "Hover info", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ChoiceSetting(
                "Batteries",
                BatteryModeNames,
                () => (int)_config.battery,
                index => _config.battery = (BatteryDisplayMode)index);
            yield return new ToggleSetting("Plants", () => _config.plants, value => _config.plants = value);
            yield return new ToggleSetting("Water purifiers", () => _config.purifiers, value => _config.purifiers = value);
            yield return new ToggleSetting("Cooking", () => _config.cooking, value => _config.cooking = value);
            yield return new ToggleSetting("Smelters and paint mill", () => _config.smelters, value => _config.smelters = value);
        }

        protected override void OnEnable()
        {
            // Order matters: a battery slot is a part of a machine, a plant is a part of its crop plot.
            List<IHoverInfoProvider> providers = new List<IHoverInfoProvider>();
            if (_config.battery != BatteryDisplayMode.Off)
            {
                providers.Add(new BatteryHoverInfo(() => _config.battery));
            }

            if (_config.plants)
            {
                providers.Add(new PlantHoverInfo());
            }

            // A purifier is a cooking stand; it has its own texts and goes first.
            if (_config.purifiers)
            {
                providers.Add(new PurifierHoverInfo());
            }

            if (_config.cooking || _config.smelters)
            {
                providers.Add(new CookingHoverInfo(_config.cooking, _config.smelters));
            }

            if (_config.purifiers || _config.cooking || _config.smelters)
            {
                CookingTimers.Activate();
                Patch(typeof(CookingStandUpdatePatch));
                Patch(typeof(CookingTableUpdatePatch));
            }

            HoverInfoService.Activate(providers);
            Patch(typeof(PickupRaycastPatch));
        }

        protected override void OnDisable()
        {
            HoverInfoService.Deactivate();
            CookingTimers.Deactivate();
        }
    }
}
