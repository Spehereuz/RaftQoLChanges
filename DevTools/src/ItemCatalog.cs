using System;
using System.Collections.Generic;

namespace QoLDevTools
{
    /// <summary>Looks items up by their unique name (Cooked_Catfish) or English display name (Cooked catfish).</summary>
    internal static class ItemCatalog
    {
        public static string DisplayName(Item_Base item)
        {
            string name = item.settings_Inventory != null ? item.settings_Inventory.DisplayName : null;
            return string.IsNullOrEmpty(name) ? item.UniqueName : name;
        }

        public static string Describe(Item_Base item)
        {
            return item.UniqueName + " - " + DisplayName(item);
        }

        /// <summary>Items whose unique or display name contains <paramref name="text"/>, ignoring case.</summary>
        public static List<Item_Base> Search(string text)
        {
            List<Item_Base> matches = new List<Item_Base>();
            foreach (Item_Base item in AllItems())
            {
                if (Contains(item.UniqueName, text) || Contains(DisplayName(item), text))
                {
                    matches.Add(item);
                }
            }

            matches.Sort((a, b) => string.Compare(a.UniqueName, b.UniqueName, StringComparison.OrdinalIgnoreCase));
            return matches;
        }

        /// <summary>
        /// Exact unique name first, then exact display name. Several items can share a display name;
        /// then all of them are returned and the caller asks for the unique name.
        /// </summary>
        public static List<Item_Base> FindExact(string name)
        {
            List<Item_Base> byDisplayName = new List<Item_Base>();
            foreach (Item_Base item in AllItems())
            {
                if (string.Equals(item.UniqueName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return new List<Item_Base> { item };
                }

                if (string.Equals(DisplayName(item), name, StringComparison.OrdinalIgnoreCase))
                {
                    byDisplayName.Add(item);
                }
            }

            return byDisplayName;
        }

        private static IEnumerable<Item_Base> AllItems()
        {
            List<Item_Base> items = ItemManager.GetAllItems();
            if (items == null)
            {
                yield break;
            }

            foreach (Item_Base item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.UniqueName))
                {
                    yield return item;
                }
            }
        }

        private static bool Contains(string value, string text)
        {
            return value != null && value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
