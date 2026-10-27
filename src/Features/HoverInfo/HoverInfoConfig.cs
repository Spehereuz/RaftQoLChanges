using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QoLChanges.Core;

namespace QoLChanges.Features.HoverInfo
{
    [Serializable]
    public sealed class HoverInfoConfig : FeatureConfig
    {
        /// <summary>Charge of the battery in a battery slot.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public BatteryDisplayMode battery = BatteryDisplayMode.Percent;

        /// <summary>Growth of plants and trees, and a summary on the soil of crop plots.</summary>
        public bool plants = true;

        /// <summary>Purification progress and fresh water of water purifiers.</summary>
        public bool purifiers = true;

        public override void Normalize()
        {
            if (!Enum.IsDefined(typeof(BatteryDisplayMode), battery))
            {
                battery = BatteryDisplayMode.Percent;
            }
        }
    }
}
