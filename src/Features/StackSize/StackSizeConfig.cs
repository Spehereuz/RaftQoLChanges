using System;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.StackSize
{
    [Serializable]
    public sealed class StackSizeConfig : FeatureConfig
    {
        public const int MinMultiplier = 1;
        public const int MaxMultiplier = 100;

        /// <summary>Vanilla stack size of every stackable item is multiplied by this value (20 -> 100 at 5).</summary>
        public int multiplier = 5;

        public override void Normalize()
        {
            multiplier = Mathf.Clamp(multiplier, MinMultiplier, MaxMultiplier);
        }
    }
}
