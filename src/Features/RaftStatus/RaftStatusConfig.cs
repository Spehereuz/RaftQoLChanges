using System;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.RaftStatus
{
    [Serializable]
    public sealed class RaftStatusConfig : FeatureConfig
    {
        public const int MinScale = 75;
        public const int MaxScale = 150;
        public const int ScaleStep = 25;

        /// <summary>State of the raft's anchors: anchored, dropping, raising or free.</summary>
        public bool anchor = true;

        /// <summary>Running engines, engines without fuel and whether the anchor holds the raft.</summary>
        public bool engines = true;

        /// <summary>Arrow of the direction the running engines push the raft, relative to the camera.</summary>
        public bool thrustDirection = true;

        /// <summary>Warnings when the running engines are too weak for the size of the raft.</summary>
        public bool enginePower = true;

        /// <summary>Size of the HUD lines in percent: 75, 100, 125 or 150.</summary>
        public int scale = 100;

        public override void Normalize()
        {
            int steps = Mathf.RoundToInt((float)Mathf.Clamp(scale, MinScale, MaxScale) / ScaleStep);
            scale = steps * ScaleStep;
        }
    }
}
