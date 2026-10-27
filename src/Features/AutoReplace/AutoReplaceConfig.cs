using System;
using QoLChanges.Core;

namespace QoLChanges.Features.AutoReplace
{
    [Serializable]
    public sealed class AutoReplaceConfig : FeatureConfig
    {
        /// <summary>Tools and weapons that break in the selected hotbar slot.</summary>
        public bool tools = true;

        /// <summary>
        /// When no copy of a broken tool is left, use another tier of the same tool: a better one first, then a worse one.
        /// </summary>
        public bool useOtherTiers = true;

        /// <summary>Food and drinks of which the last portion was eaten from the selected hotbar slot.</summary>
        public bool food = true;
    }
}
