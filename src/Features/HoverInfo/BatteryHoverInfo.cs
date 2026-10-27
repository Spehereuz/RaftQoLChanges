using System;
using HarmonyLib;
using QoLChanges.Core;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>How the charge of a battery is written. Values are the dropdown indices.</summary>
    public enum BatteryDisplayMode
    {
        Off = 0,
        Percent = 1,
        Remaining = 2,
        RemainingOfMax = 3,
    }

    /// <summary>
    /// Charge of the battery in a battery slot: "Battery 68%". Every machine slot (purifier, grill, smelter,
    /// juicer, receiver, sprinkler, recycler, battery charger) is a <see cref="Battery"/>, so all of them are covered.
    /// Only the slot itself is described, not the machine around it; an empty slot shows nothing, the game
    /// already offers to place a battery there.
    /// </summary>
    internal sealed class BatteryHoverInfo : IHoverInfoProvider
    {
        private static readonly AccessTools.FieldRef<Battery, ItemInstance> BatteryInstanceField =
            AccessTools.FieldRefAccess<Battery, ItemInstance>("batteryInstance");

        private readonly Func<BatteryDisplayMode> _mode;

        public BatteryHoverInfo(Func<BatteryDisplayMode> mode)
        {
            _mode = mode ?? throw new ArgumentNullException(nameof(mode));
        }

        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;
            Battery battery = HoverTargets.Find<Battery>(target);
            if (battery == null)
            {
                return false;
            }

            if (battery.BatterySlotIsEmpty || !Helper.LocalPlayerIsWithinDistance(battery.transform.position, Player.UseDistance))
            {
                return true;
            }

            ItemInstance instance = BatteryInstanceField(battery);
            int max = instance.BaseItemMaxUses;
            int uses = Mathf.Clamp(instance.Uses, 0, max);
            text = "Battery " + StatusText.Colored(Format(uses, max), ChargeColor(instance.baseItem, uses, max));
            return true;
        }

        private string Format(int uses, int max)
        {
            switch (_mode())
            {
                case BatteryDisplayMode.Remaining:
                    return uses.ToString();
                case BatteryDisplayMode.RemainingOfMax:
                    return uses + "/" + max;
                default:
                    return Percent.OfRemaining(uses, max) + "%";
            }
        }

        /// <summary>Same colour as the charge bar and the durability text of the battery in the inventory.</summary>
        private static Color ChargeColor(Item_Base item, int uses, int max)
        {
            Gradient gradient = item != null ? item.BarGradient : null;
            return gradient != null && max > 0 ? gradient.Evaluate((float)uses / max) : Color.white;
        }
    }
}
