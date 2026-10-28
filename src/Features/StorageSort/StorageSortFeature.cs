using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.StorageSort
{
    /// <summary>
    /// Lets the sort button of a storage put the items in another order. The button and its key are the game's own; the
    /// storage is open and locked to the local player while it is sorted and the game sends its slots to the host when it
    /// is closed, exactly like after the vanilla sort, so other players do not need the mod.
    /// </summary>
    internal sealed class StorageSortFeature : Feature
    {
        public const string FeatureId = "StorageSort";

        // Index = StorageSortOrder value.
        private static readonly string[] OrderNames = { "Vanilla", "Alphabetical" };

        private readonly StorageSortConfig _config;

        public StorageSortFeature(StorageSortConfig config)
            : base(FeatureId, "Storage sort", config)
        {
            _config = config;
        }

        public override IEnumerable<FeatureSetting> GetSettings()
        {
            yield return new ChoiceSetting(
                "Sort order",
                OrderNames,
                () => (int)_config.order,
                index => _config.order = (StorageSortOrder)index);
        }

        protected override void OnEnable()
        {
            StorageSortService.Activate(_config);
            Patch(typeof(InventorySortPatch));
        }

        protected override void OnDisable()
        {
            StorageSortService.Deactivate();
        }
    }
}
