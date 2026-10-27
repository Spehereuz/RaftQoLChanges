using UnityEngine;

namespace QoLChanges.Core
{
    /// <summary>Rounding of values shown as percentages, shared by every feature that writes one.</summary>
    internal static class Percent
    {
        /// <summary>
        /// Share of <paramref name="max"/> that is left, rounded down: a used item never reads 100%, and
        /// anything above zero reads at least 1%.
        /// </summary>
        public static int OfRemaining(int value, int max)
        {
            if (max <= 0 || value <= 0)
            {
                return 0;
            }

            if (value >= max)
            {
                return 100;
            }

            int percent = (int)((long)value * 100 / max);
            return percent == 0 ? 1 : percent;
        }

        /// <summary>Same as <see cref="OfRemaining(int, int)"/> for amounts that are not whole numbers (fuel, water).</summary>
        public static int OfRemaining(float value, float max)
        {
            if (max <= 0f || value <= 0f)
            {
                return 0;
            }

            if (value >= max)
            {
                return 100;
            }

            int percent = Mathf.FloorToInt(value / max * 100f);
            return Mathf.Clamp(percent, 1, 99);
        }

        /// <summary>
        /// Progress of something that is not finished yet, rounded down and capped at 99%, so 100% is never
        /// shown before the game marks it as done.
        /// </summary>
        public static int OfProgress(float elapsed, float total)
        {
            if (total <= 0f || elapsed <= 0f)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.FloorToInt(elapsed / total * 100f), 0, 99);
        }
    }
}
