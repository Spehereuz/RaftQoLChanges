using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Water purifiers. Purifiers heated with fuel hold one (simple) or five (advanced) portions, each purified on
    /// its own cooking slot: "Salt water · purifying 45%" / "Fresh water ready" for one portion,
    /// "3 purifying (45%) · 2 fresh" for several, where the percentage is the portion closest to being ready.
    /// The electric purifier shows its fresh water tank and whether it pumps: "Fresh water 12/20 · Pumping".
    /// </summary>
    internal sealed class PurifierHoverInfo : IHoverInfoProvider
    {
        private static readonly AccessTools.FieldRef<CookingSlot, float> CookTimeMultiplierField =
            AccessTools.FieldRefAccess<CookingSlot, float>("cookTimeMultiplier");

        private static readonly AccessTools.FieldRef<Electric_Purifier, int> BatteryDrainPerPumpField =
            AccessTools.FieldRefAccess<Electric_Purifier, int>("batteryDrainPerPump");

        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;

            Block_CookingStand_Purifier purifier = HoverTargets.Find<Block_CookingStand_Purifier>(target);
            if (purifier != null)
            {
                if (Helper.LocalPlayerIsWithinDistance(purifier.transform.position, Player.UseDistance))
                {
                    text = DescribePurifier(purifier);
                }

                return true;
            }

            Electric_Purifier electricPurifier = HoverTargets.Find<Electric_Purifier>(target);
            if (electricPurifier != null)
            {
                if (Helper.LocalPlayerIsWithinDistance(electricPurifier.transform.position, Player.UseDistance))
                {
                    text = DescribeElectricPurifier(electricPurifier);
                }

                return true;
            }

            return false;
        }

        private static string DescribePurifier(Block_CookingStand_Purifier purifier)
        {
            CookingSlot[] slots = purifier.cookingSlots;
            if (slots == null)
            {
                return null;
            }

            int purifying = 0;
            int fresh = 0;
            int bestProgress = 0;

            foreach (CookingSlot slot in slots)
            {
                // A fake slot is occupied by an item cooked on another slot; it has no timer of its own.
                if (slot == null || slot.FakeSlot || slot.CurrentItem == null)
                {
                    continue;
                }

                if (slot.IsComplete)
                {
                    fresh++;
                    continue;
                }

                purifying++;
                int progress = GetProgress(slot);
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                }
            }

            if (purifying + fresh == 0)
            {
                return null;
            }

            if (purifying + fresh == 1)
            {
                return fresh == 1
                    ? HoverText.Colored("Fresh water ready", HoverText.Ready)
                    : "Salt water" + HoverText.Separator + "purifying " + HoverText.Colored(bestProgress + "%", HoverText.Progress);
            }

            string purifyingText = purifying > 0
                ? purifying + " purifying (" + HoverText.Colored(bestProgress + "%", HoverText.Progress) + ")"
                : null;
            string freshText = fresh > 0 ? HoverText.Colored(fresh + " fresh", HoverText.Ready) : null;
            return HoverText.Join(purifyingText, freshText);
        }

        private static int GetProgress(CookingSlot slot)
        {
            ItemInstance_Cookable cookable = slot.CurrentItem.settings_cookable;
            if (cookable == null)
            {
                return 0;
            }

            return Percent.OfProgress(slot.CookTimer, cookable.CookingTime * CookTimeMultiplierField(slot));
        }

        private static string DescribeElectricPurifier(Electric_Purifier purifier)
        {
            Tank tank = purifier.tank;
            if (tank == null)
            {
                return null;
            }

            int amount = Mathf.FloorToInt(tank.CurrentTankAmount);
            int capacity = Mathf.RoundToInt(tank.maxCapacity);
            string water = "Fresh water " + HoverText.Colored(amount + "/" + capacity, HoverText.Water);
            return HoverText.Join(water, DescribeMachineState(purifier, tank));
        }

        /// <summary>Same conditions as Electric_Purifier.Update, which decides whether the pump runs.</summary>
        private static string DescribeMachineState(Electric_Purifier purifier, Tank tank)
        {
            if (tank.IsFull || tank.CurrentTankAmount > tank.maxCapacity)
            {
                return HoverText.Colored("Full", HoverText.Ready);
            }

            Battery battery = purifier.battery;
            bool powered = battery != null
                && battery.CanGiveElectricity
                && battery.BatteryUses >= BatteryDrainPerPumpField(purifier);
            return powered
                ? HoverText.Colored("Pumping", HoverText.Progress)
                : HoverText.Colored("No power", HoverText.Problem);
        }
    }
}
