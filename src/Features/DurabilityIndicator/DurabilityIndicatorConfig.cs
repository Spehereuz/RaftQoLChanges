using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QoLChanges.Core;

namespace QoLChanges.Features.DurabilityIndicator
{
    /// <summary>How the durability of an item is written on its slot. Values are the dropdown indices.</summary>
    public enum DurabilityDisplayMode
    {
        Off = 0,
        Percent = 1,
        Remaining = 2,
        RemainingOfMax = 3,
    }

    [Serializable]
    public sealed class DurabilityIndicatorConfig : FeatureConfig
    {
        /// <summary>Tools, weapons, armor and other equipment.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DurabilityDisplayMode tools = DurabilityDisplayMode.Percent;

        /// <summary>Canteens and bottles: sips left.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DurabilityDisplayMode water = DurabilityDisplayMode.Percent;

        /// <summary>Food eaten in several bites (cooked catfish and salmon, watermelon).</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DurabilityDisplayMode food = DurabilityDisplayMode.Percent;

        /// <summary>When false, items that have not been used yet show no indicator.</summary>
        public bool showAtFullDurability;

        public override void Normalize()
        {
            tools = Sanitize(tools);
            water = Sanitize(water);
            food = Sanitize(food);
        }

        private static DurabilityDisplayMode Sanitize(DurabilityDisplayMode mode)
        {
            return Enum.IsDefined(typeof(DurabilityDisplayMode), mode) ? mode : DurabilityDisplayMode.Percent;
        }
    }
}
