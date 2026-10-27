using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.Core;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Growth of plants and trees. A plant reads "Growing 63%", "Ready to harvest" or "Needs water"; the soil of a
    /// crop plot sums up its plants: "2 growing (41%) · 1 ready · 1 needs water", where the percentage is the
    /// plant closest to being ready.
    /// </summary>
    /// <remarks>
    /// Plants are not interactables: the game finds them with a raycast of its own against the item layer
    /// (PlantManager.HarvestPlants, tag "Plant"), which is repeated here, so a plant reads the same wherever it
    /// is aimed at. A plant grows only after it has been watered once (PlantationSlot.hasWater, kept until the
    /// plant is removed). The timer runs on every client and the host announces when a plant is ready, so a
    /// client may show a slightly different percentage than the host until then.
    /// </remarks>
    internal sealed class PlantHoverInfo : IHoverInfoProvider
    {
        private const string PlantTag = "Plant";

        private static readonly AccessTools.FieldRef<Plant, float> GrowTimeSecondsField =
            AccessTools.FieldRefAccess<Plant, float>("growTimeSec");

        private enum PlantState
        {
            NeedsWater,
            Growing,
            Ready,
        }

        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;

            Plant plant = FindAimedPlant();
            if (plant != null)
            {
                text = DescribePlant(plant, GetState(plant, FindSlot(plant)));
                return true;
            }

            Cropplot cropplot = HoverTargets.Find<Cropplot>(target);
            if (cropplot == null)
            {
                return false;
            }

            if (Helper.LocalPlayerIsWithinDistance(cropplot.transform.position, Player.UseDistance))
            {
                text = DescribeCropplot(cropplot);
            }

            return true;
        }

        /// <summary>Same raycast as the game's harvest prompt.</summary>
        private static Plant FindAimedPlant()
        {
            if (!Helper.HitAtCursor(out RaycastHit hit, Player.UseDistance, LayerMasks.MASK_Item, QueryTriggerInteraction.UseGlobal))
            {
                return null;
            }

            Transform hitTransform = hit.transform;
            return hitTransform != null && hitTransform.CompareTag(PlantTag) ? hitTransform.GetComponent<Plant>() : null;
        }

        private static string DescribeCropplot(Cropplot cropplot)
        {
            List<PlantationSlot> slots = cropplot.plantationSlots;
            if (slots == null)
            {
                return null;
            }

            int growing = 0;
            int ready = 0;
            int needWater = 0;
            int bestProgress = 0;
            Plant single = null;
            PlantState singleState = PlantState.NeedsWater;

            foreach (PlantationSlot slot in slots)
            {
                if (slot == null || slot.plant == null)
                {
                    continue;
                }

                PlantState state = GetState(slot.plant, slot);
                single = slot.plant;
                singleState = state;
                switch (state)
                {
                    case PlantState.Ready:
                        ready++;
                        break;
                    case PlantState.NeedsWater:
                        needWater++;
                        break;
                    default:
                        growing++;
                        bestProgress = Mathf.Max(bestProgress, GetProgress(slot.plant));
                        break;
                }
            }

            int plants = growing + ready + needWater;
            if (plants == 0)
            {
                return null;
            }

            if (plants == 1)
            {
                return DescribePlant(single, singleState);
            }

            string text = growing > 0
                ? growing + " growing (" + StatusText.Colored(bestProgress + "%", StatusText.Progress) + ")"
                : null;
            if (ready > 0)
            {
                text = StatusText.Join(text, StatusText.Colored(ready + " ready", StatusText.Ready));
            }

            if (needWater > 0)
            {
                text = StatusText.Join(text, StatusText.Colored(needWater + " needs water", StatusText.Problem));
            }

            return text;
        }

        private static string DescribePlant(Plant plant, PlantState state)
        {
            switch (state)
            {
                case PlantState.Ready:
                    return StatusText.Colored("Ready to harvest", StatusText.Ready);
                case PlantState.Growing:
                    return "Growing " + StatusText.Colored(GetProgress(plant) + "%", StatusText.Progress);
                default:
                    return StatusText.Colored("Needs water", StatusText.Problem);
            }
        }

        private static PlantState GetState(Plant plant, PlantationSlot slot)
        {
            if (plant.FullyGrown())
            {
                return PlantState.Ready;
            }

            // A plant outside of a known slot has no water flag; it is shown as growing.
            return slot == null || slot.hasWater ? PlantState.Growing : PlantState.NeedsWater;
        }

        private static int GetProgress(Plant plant)
        {
            return Percent.OfProgress(plant.GetGrowTimer(), GrowTimeSecondsField(plant));
        }

        private static PlantationSlot FindSlot(Plant plant)
        {
            Cropplot cropplot = plant.cropplot;
            if (cropplot == null || cropplot.plantationSlots == null)
            {
                return null;
            }

            foreach (PlantationSlot slot in cropplot.plantationSlots)
            {
                if (slot != null && slot.plant == plant)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
