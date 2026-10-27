using QoLChanges.Core;
using QoLChanges.GameState;
using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>Fuel of an engine as shown by the HUD and hover info: "72% · 36 min", "72% · paused", "Empty".</summary>
    internal static class FuelText
    {
        /// <summary>
        /// The percentage, followed by how long the fuel lasts while it is burnt, or "paused" while the engine runs
        /// without burning it. A switched-off engine shows only the percentage.
        /// </summary>
        public static string Format(EngineFuel fuel, FuelUse use)
        {
            if (fuel.Amount <= 0f)
            {
                return StatusText.Colored("Empty", StatusText.Problem);
            }

            string percent = StatusText.Colored(Percent.OfRemaining(fuel.Amount, fuel.Capacity) + "%", ToolDurabilityColor.Evaluate(fuel.Fraction));
            switch (use)
            {
                case FuelUse.Burning:
                    return StatusText.Join(percent, Duration(fuel.Amount * fuel.SecondsPerUnit));
                case FuelUse.Paused:
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
    }
}
