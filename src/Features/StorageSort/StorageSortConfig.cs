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

        /// <summary>By <see cref="StorageGroup"/>, then by English name.</summary>
        ByCategory,

        /// <summary>By English name.</summary>
        Alphabetical,
    }

    [Serializable]
    public sealed class StorageSortConfig : FeatureConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StorageSortOrder order = StorageSortOrder.ByCategory;

        /// <summary>Sorts the storage after the button that stores the matching items of the inventory in it.</summary>
        public bool sortAfterStoring;

        public override void Normalize()
        {
            if (!Enum.IsDefined(typeof(StorageSortOrder), order))
            {
                order = StorageSortOrder.ByCategory;
            }
        }
    }
}
