using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>Building blocks of hover info lines: labels stay white, values are coloured with rich text.</summary>
    internal static class HoverText
    {
        public const string Separator = " · ";

        /// <summary>Something is still in progress (growing, purifying).</summary>
        public static readonly Color Progress = new Color(1f, 0.85f, 0.3f);

        /// <summary>Something is done and can be collected.</summary>
        public static readonly Color Ready = new Color(0.55f, 0.9f, 0.35f);

        /// <summary>A machine cannot work.</summary>
        public static readonly Color Problem = new Color(1f, 0.4f, 0.35f);

        /// <summary>Amount of water, close to the cyan of water container bars.</summary>
        public static readonly Color Water = new Color(0.45f, 0.85f, 1f);

        public static string Colored(string value, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + value + "</color>";
        }

        public static string Join(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
            {
                return second;
            }

            return string.IsNullOrEmpty(second) ? first : first + Separator + second;
        }

        /// <summary>
        /// Makes an emissive colour (HDR, used for lights on models) readable as text: the brightest channel is
        /// scaled to 1, black falls back to white.
        /// </summary>
        public static Color FromEmission(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (max <= 0.001f)
            {
                return Color.white;
            }

            return new Color(color.r / max, color.g / max, color.b / max, 1f);
        }
    }
}
