using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>
    /// The red-yellow-green colour of a tool's durability bar in the inventory, for values that run out like a tool:
    /// engine fuel, parts of a scarecrow.
    /// </summary>
    internal static class ToolDurabilityColor
    {
        private const string GradientItem = "Axe";

        private static Gradient _gradient;

        /// <param name="fraction">What is left, 0 to 1.</param>
        public static Color Evaluate(float fraction)
        {
            if (_gradient == null)
            {
                Item_Base item = ItemManager.GetItemByName(GradientItem);
                _gradient = item != null ? item.BarGradient : null;
            }

            if (_gradient != null)
            {
                return _gradient.Evaluate(Mathf.Clamp01(fraction));
            }

            return fraction < 0.5f
                ? Color.Lerp(StatusText.Problem, StatusText.Progress, fraction * 2f)
                : Color.Lerp(StatusText.Progress, StatusText.Ready, fraction * 2f - 1f);
        }
    }
}
