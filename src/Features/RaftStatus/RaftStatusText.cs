using QoLChanges.Core;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>Texts of the HUD lines. Labels are white, states and warnings are coloured.</summary>
    internal static class RaftStatusText
    {
        // Tools have a red-yellow-green durability bar; fuel is coloured the same way.
        private const string GradientItem = "Axe";

        private static Gradient _fuelGradient;
        public static string Anchor(AnchorSummary anchors)
        {
            string state;
            if (anchors.Down > 0)
            {
                state = StatusText.Colored("Anchored", StatusText.Ready);
            }
            else if (anchors.Dropping > 0)
            {
                state = StatusText.Colored("Dropping", StatusText.Progress);
            }
            else if (anchors.Raising > 0)
            {
                state = StatusText.Colored("Raising", StatusText.Progress);
            }
            else
            {
                state = "Free";
            }

            return anchors.Total > 1 ? StatusText.Join(state, anchors.Down + "/" + anchors.Total + " down") : state;
        }

        /// <summary>
        /// "2/3 running", followed by engines without fuel, why the engines do not move the raft and, when
        /// <paramref name="showPower"/> is set, whether they are strong enough for it.
        /// </summary>
        public static string Engines(EngineSummary engines, EngineDrive drive, EnginePower power, int needed, bool showPower)
        {
            string text = engines.Running + "/" + engines.Total + " running";
            if (engines.NoFuel > 0)
            {
                text = StatusText.Join(text, StatusText.Colored(engines.NoFuel + " no fuel", StatusText.Problem));
            }

            switch (drive)
            {
                case EngineDrive.HeldByAnchor:
                    return StatusText.Join(text, StatusText.Colored("Held by anchor", StatusText.Progress));
                case EngineDrive.TooHeavy:
                    return showPower
                        ? StatusText.Join(text, StatusText.Colored(needed + " needed" + StatusText.Separator + "Too heavy", StatusText.Problem))
                        : text;
                case EngineDrive.NoThrust:
                    return StatusText.Join(text, StatusText.Colored("No thrust", StatusText.Progress));
                case EngineDrive.Pushing:
                    return showPower && power == EnginePower.Weak
                        ? StatusText.Join(text, StatusText.Colored("Weak", StatusText.Progress))
                        : text;
                default:
                    return text;
            }
        }

        /// <summary>
        /// The emptiest tank of the switched-on engines with the time it lasts while they burn fuel, "paused" while they
        /// do not; with every engine switched off, the emptiest tank of all of them.
        /// </summary>
        public static string Fuel(EngineSummary engines, EngineDrive drive)
        {
            EngineFuel? switchedOn = engines.LowestSwitchedOn;
            EngineFuel? shown = switchedOn ?? engines.Lowest;
            if (!shown.HasValue)
            {
                return null;
            }

            EngineFuel fuel = shown.Value;
            if (fuel.Amount <= 0f)
            {
                return StatusText.Colored("Empty", StatusText.Problem);
            }

            string percent = StatusText.Colored(Percent.OfRemaining(fuel.Amount, fuel.Capacity) + "%", FuelColor(fuel.Fraction));
            if (!switchedOn.HasValue)
            {
                return percent;
            }

            switch (drive)
            {
                case EngineDrive.Pushing:
                case EngineDrive.NoThrust:
                    return StatusText.Join(percent, Duration(fuel.Amount * fuel.SecondsPerUnit));
                case EngineDrive.HeldByAnchor:
                case EngineDrive.TooHeavy:
                    return StatusText.Join(percent, "paused");
                default:
                    return percent;
            }
        }

        private static string Duration(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            if (minutes < 1)
            {
                return "<1 min";
            }

            return minutes < 60 ? minutes + " min" : minutes / 60 + " h " + minutes % 60 + " min";
        }

        private static Color FuelColor(float fraction)
        {
            if (_fuelGradient == null)
            {
                Item_Base item = ItemManager.GetItemByName(GradientItem);
                _fuelGradient = item != null ? item.BarGradient : null;
            }

            if (_fuelGradient != null)
            {
                return _fuelGradient.Evaluate(Mathf.Clamp01(fraction));
            }

            return fraction < 0.5f
                ? Color.Lerp(StatusText.Problem, StatusText.Progress, fraction * 2f)
                : Color.Lerp(StatusText.Progress, StatusText.Ready, fraction * 2f - 1f);
        }
    }
}
