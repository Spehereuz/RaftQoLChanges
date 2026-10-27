using System;
using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.UI
{
    /// <summary>State of the in-game menu that is kept between game sessions.</summary>
    [Serializable]
    public sealed class MenuConfig : IConfig
    {
        /// <summary>Ids of features whose options are expanded on the QoL Changes page; all collapsed by default.</summary>
        public List<string> expandedFeatures = new List<string>();

        public void Normalize()
        {
            if (expandedFeatures == null)
            {
                expandedFeatures = new List<string>();
                return;
            }

            List<string> unique = new List<string>();
            foreach (string id in expandedFeatures)
            {
                if (!string.IsNullOrEmpty(id) && !unique.Contains(id))
                {
                    unique.Add(id);
                }
            }

            expandedFeatures = unique;
        }
    }
}
