using System;
using QoLChanges.Core;

namespace QoLChanges.Features.AutoReplace
{
    [Serializable]
    public sealed class AutoReplaceConfig : FeatureConfig
    {
        /// <summary>Tools and weapons that break in the selected hotbar slot.</summary>
        public bool tools = true;
    }
}
