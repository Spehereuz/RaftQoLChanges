using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QoLChanges.Core;

namespace QoLChanges.Features.StorageSort
{
    /// <summary>Order the sort button of a storage puts the items in.</summary>
    public enum StorageSortOrder
    {
        /// <summary>The game's own sort: stacks are merged and keep the order in which the items first appear.</summary>
        Vanilla,

        /// <summary>By English name.</summary>
        Alphabetical,
    }

    [Serializable]
    public sealed class StorageSortConfig : FeatureConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StorageSortOrder order = StorageSortOrder.Alphabetical;

        public override void Normalize()
        {
            if (!Enum.IsDefined(typeof(StorageSortOrder), order))
            {
                order = StorageSortOrder.Alphabetical;
            }
        }
    }
}
