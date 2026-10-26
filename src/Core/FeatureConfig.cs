using System;

namespace QoLChanges.Core
{
    [Serializable]
    public class FeatureConfig : IConfig
    {
        public bool enabled = true;

        public virtual void Normalize()
        {
        }
    }
}
