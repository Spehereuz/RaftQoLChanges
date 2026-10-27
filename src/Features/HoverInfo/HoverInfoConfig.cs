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

        /// <summary>Fuel of engines (motor wheels).</summary>
        public bool engineFuel = true;

        /// <summary>Grills, cooking pots and juicers.</summary>
        public bool cooking = true;

        /// <summary>Smelters and the paint mill.</summary>
        public bool smelters = true;

        /// <summary>Items in collection nets out of how many they hold.</summary>
        public bool collectionNets = true;

        public override void Normalize()
        {
            if (!Enum.IsDefined(typeof(BatteryDisplayMode), battery))
            {
                battery = BatteryDisplayMode.Percent;
            }
        }
    }
}
