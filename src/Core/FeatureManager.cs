using System;
using System.Collections.Generic;

namespace QoLChanges.Core
{
    internal sealed class FeatureManager
    {
        private readonly List<Feature> _features = new List<Feature>();

        public IReadOnlyList<Feature> Features => _features;

        public void Register(Feature feature)
        {
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (_features.Exists(f => f.Id == feature.Id))
            {
                throw new InvalidOperationException("Feature '" + feature.Id + "' is already registered.");
            }

            _features.Add(feature);
        }

        /// <summary>Enables or disables every feature according to its config.</summary>
        public void ApplyConfig()
        {
            foreach (Feature feature in _features)
            {
                if (feature.Config.enabled)
                {
                    feature.Enable();
                }
                else
                {
                    feature.Disable();
                }
            }
        }

        public void DisableAll()
        {
            for (int i = _features.Count - 1; i >= 0; i--)
            {
                _features[i].Disable();
            }
        }
    }
}
