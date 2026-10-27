using System;
using HarmonyLib;
using QoLChanges.Core;
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

        private static readonly AccessTools.FieldRef<Battery, float> GreenLimitField =
            AccessTools.FieldRefAccess<Battery, float>("greenLimit");

        private static readonly AccessTools.FieldRef<Battery, float> YellowLimitField =
            AccessTools.FieldRefAccess<Battery, float>("yellowLimit");

        private static readonly AccessTools.FieldRef<Battery, float> OrangeLimitField =
            AccessTools.FieldRefAccess<Battery, float>("orangeLimit");

        private static readonly AccessTools.FieldRef<Battery, Color> GreenField =
            AccessTools.FieldRefAccess<Battery, Color>("green");

        private static readonly AccessTools.FieldRef<Battery, Color> YellowField =
            AccessTools.FieldRefAccess<Battery, Color>("yellow");

        private static readonly AccessTools.FieldRef<Battery, Color> OrangeField =
            AccessTools.FieldRefAccess<Battery, Color>("orange");

        private static readonly AccessTools.FieldRef<Battery, Color> RedField =
            AccessTools.FieldRefAccess<Battery, Color>("red");

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
            text = "Battery " + HoverText.Colored(Format(uses, max), ChargeColor(battery, instance.NormalizedUses));
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

        /// <summary>Same thresholds and colours as the lights of the battery slot (Battery.UpdateBatteryHolderLights).</summary>
        private static Color ChargeColor(Battery battery, float charge)
        {
            Color color = RedField(battery);
            if (charge > GreenLimitField(battery))
            {
                color = GreenField(battery);
            }
            else if (charge > YellowLimitField(battery))
            {
                color = YellowField(battery);
            }
            else if (charge > OrangeLimitField(battery))
            {
                color = OrangeField(battery);
            }

            return HoverText.FromEmission(color);
        }
    }
}
