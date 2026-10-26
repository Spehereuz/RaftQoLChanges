using System;
using System.Collections.Generic;
using QoLChanges.Core;

namespace QoLChanges.Features.StackSize
{
    /// <summary>
    /// Multiplies the stack size of every stackable item. Items with a vanilla stack size of 1
    /// (tools, equipment, containers with water, anything with durability) are left untouched.
    /// </summary>
    /// <remarks>
    /// Saves keep the real amount per slot and the game never clamps it on load, so oversized stacks
    /// survive disabling the mod: they can be taken from but not added to until they shrink.
    /// </remarks>
    internal sealed class StackSizeFeature : Feature
    {
        public const string FeatureId = "StackSize";

        // RGD_Slot stores item amounts as Int16.
        private const int MaxPersistableStack = short.MaxValue;

        private readonly StackSizeConfig _config;
        private readonly Dictionary<Item_Base, int> _originalStackSizes = new Dictionary<Item_Base, int>();

        public StackSizeFeature(StackSizeConfig config)
            : base(FeatureId, config)
        {
            _config = config;
        }

        protected override void OnEnable()
        {
            foreach (Item_Base item in ItemManager.GetAllItems())
            {
                if (item == null || item.settings_Inventory == null)
                {
                    continue;
                }

                int original = item.settings_Inventory.StackSize;
                if (original <= 1)
                {
                    continue;
                }

                int scaled = (int)Math.Min((long)original * _config.multiplier, MaxPersistableStack);
                if (scaled == original)
                {
                    continue;
                }

                _originalStackSizes[item] = original;
                StackSizeAccess.Set(item.settings_Inventory, scaled);
            }

            Patch(typeof(SlotRefreshComponentsPatch));
            StackSizeAccess.SyncAllSlots();

            ModLog.Info(Id + ": x" + _config.multiplier + " applied to " + _originalStackSizes.Count + " items.");
        }

        protected override void OnDisable()
        {
            foreach (KeyValuePair<Item_Base, int> entry in _originalStackSizes)
            {
                if (entry.Key != null && entry.Key.settings_Inventory != null)
                {
                    StackSizeAccess.Set(entry.Key.settings_Inventory, entry.Value);
                }
            }

            _originalStackSizes.Clear();
            StackSizeAccess.SyncAllSlots();
        }
    }
}
