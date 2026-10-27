using System;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.ScarecrowRadius
{
    [Serializable]
    public sealed class ScarecrowRadiusConfig : FeatureConfig
    {
        public const int MinLineWidth = 2;
        public const int MaxLineWidth = 10;

        /// <summary>Show the zones while the hammer is selected.</summary>
        public bool withHammer = true;

        /// <summary>Show the zones while a scarecrow is selected.</summary>
        public bool withScarecrow = true;

        /// <summary>Also outline the zone two floors above and below the scarecrow.</summary>
        public bool neighbouringFloors;

        /// <summary>Light crop plots green inside a zone and red outside of it.</summary>
        public bool cropPlots = true;

        /// <summary>Width of the lines in centimetres, 2 to 10.</summary>
        public int lineWidth = 4;

        public override void Normalize()
        {
            lineWidth = Mathf.Clamp(lineWidth, MinLineWidth, MaxLineWidth);
        }
    }
}
