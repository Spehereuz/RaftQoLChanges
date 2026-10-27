using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace QoLDevTools
{
    /// <summary>Fills the fuel tanks of the raft's engines the way the host does when fuel changes.</summary>
    internal static class EngineFuel
    {
        public const string Syntax = "qoldev.fuel <units|percent%>";

        public static string Set(string argument)
        {
            if (!TryParse(argument, out float value, out bool isPercent, out string error))
            {
                return error;
            }

            if (!Raft_Network.IsHost)
            {
                return "Only the host can change engine fuel.";
            }

            List<MotorWheel> motors = RaftVelocityManager.Motors;
            int changed = 0;
            string result = null;
            if (motors != null)
            {
                foreach (MotorWheel motor in motors)
                {
                    Tank tank = motor != null ? motor.FuelTank : null;
                    if (tank == null)
                    {
                        continue;
                    }

                    float amount = isPercent ? tank.maxCapacity * value / 100f : value;
                    tank.SetTankAmount(amount);
                    tank.SendTankSetValueMessage(null);
                    changed++;
                    result = tank.CurrentTankAmount.ToString("0.##", CultureInfo.InvariantCulture) + "/" +
                             tank.maxCapacity.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }

            return changed == 0
                ? "There are no engines on the raft."
                : "Set the fuel of " + changed + " engine(s) to " + result + ".";
        }

        private static bool TryParse(string text, out float value, out bool isPercent, out string error)
        {
            value = 0f;
            error = null;
            string trimmed = text == null ? string.Empty : text.Trim();
            isPercent = trimmed.EndsWith("%");
            string digits = isPercent ? trimmed.Substring(0, trimmed.Length - 1) : trimmed;
            if (!float.TryParse(digits, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
            {
                error = "'" + text + "' is not an amount of fuel or a percentage. Syntax: " + Syntax;
                return false;
            }

            if (isPercent && value > 100f)
            {
                error = "Percentage must be between 0% and 100%.";
                return false;
            }

            value = Mathf.Max(0f, value);
            return true;
        }
    }
}
