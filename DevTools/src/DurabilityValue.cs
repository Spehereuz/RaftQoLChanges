using System.Globalization;
using UnityEngine;

namespace QoLDevTools
{
    /// <summary>Argument of the durability commands: "44" (uses), "37%" (of max uses) or "random".</summary>
    internal struct DurabilityValue
    {
        private const string RandomKeyword = "random";

        private readonly int _number;
        private readonly bool _isPercent;
        private readonly bool _isRandom;

        private DurabilityValue(int number, bool isPercent, bool isRandom)
        {
            _number = number;
            _isPercent = isPercent;
            _isRandom = isRandom;
        }

        public static bool TryParse(string text, bool allowRandom, out DurabilityValue value, out string error)
        {
            value = default(DurabilityValue);
            error = null;
            string trimmed = text == null ? string.Empty : text.Trim();

            if (allowRandom && string.Equals(trimmed, RandomKeyword, System.StringComparison.OrdinalIgnoreCase))
            {
                value = new DurabilityValue(0, false, true);
                return true;
            }

            bool isPercent = trimmed.EndsWith("%");
            string digits = isPercent ? trimmed.Substring(0, trimmed.Length - 1) : trimmed;
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                error = "'" + text + "' is not a number of uses, a percentage" + (allowRandom ? " or 'random'" : string.Empty) + ".";
                return false;
            }

            if (isPercent && number > 100)
            {
                error = "Percentage must be between 0% and 100%.";
                return false;
            }

            value = new DurabilityValue(number, isPercent, false);
            return true;
        }

        /// <summary>Uses for an item with <paramref name="maxUses"/>; random never breaks the item.</summary>
        public int Resolve(int maxUses)
        {
            if (_isRandom)
            {
                return Random.Range(1, maxUses + 1);
            }

            int uses = _isPercent ? Mathf.RoundToInt(maxUses * _number / 100f) : _number;
            return Mathf.Clamp(uses, 0, maxUses);
        }
    }
}
