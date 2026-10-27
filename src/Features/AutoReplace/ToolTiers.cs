using System;
using System.Collections.Generic;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// Vanilla tools that come in several tiers. A broken tool is only ever replaced by a tool of its own group:
    /// a hook by another hook, never by an axe. Items outside these groups (single-tier tools, items of other mods)
    /// are replaced only by the same item.
    /// </summary>
    internal static class ToolTiers
    {
        // Every group from the lowest tier to the highest, by Item_Base.UniqueName.
        private static readonly string[][] Groups =
        {
            new[] { "Hook_Plastic", "Hook_Scrap", "Hook_Titanium" },
            new[] { "Axe_Stone", "Axe", "Axe_Titanium" },
            new[] { "FishingRod", "FishingRod_Metal" },
            new[] { "Spear_Wood", "Spear_Scrap", "Machete", "Sword_Titanium" },
        };

        /// <summary>
        /// Other tiers of the item's group in the order they are tried: better tiers from the nearest one, then worse
        /// tiers from the nearest one. Empty when the item has no other tiers.
        /// </summary>
        public static IEnumerable<string> GetReplacementOrder(string uniqueName)
        {
            foreach (string[] group in Groups)
            {
                int tier = Array.IndexOf(group, uniqueName);
                if (tier < 0)
                {
                    continue;
                }

                for (int i = tier + 1; i < group.Length; i++)
                {
                    yield return group[i];
                }

                for (int i = tier - 1; i >= 0; i--)
                {
                    yield return group[i];
                }

                yield break;
            }
        }
    }
}
