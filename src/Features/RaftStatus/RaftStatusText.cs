using QoLChanges.GameState;
using QoLChanges.UI;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>Texts of the HUD lines. Labels are white, states and warnings are coloured.</summary>
    internal static class RaftStatusText
    {
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
            if (engines.LowestSwitchedOn.HasValue)
            {
                return FuelText.Format(engines.LowestSwitchedOn.Value, RaftEngines.GetFuelUse(drive));
            }

            return engines.Lowest.HasValue ? FuelText.Format(engines.Lowest.Value, FuelUse.Off) : null;
        }
    }
}
