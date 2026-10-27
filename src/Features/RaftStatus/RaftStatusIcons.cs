using System.Collections.Generic;
using UnityEngine;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>Inventory icons of game items, used as the icons of the HUD lines.</summary>
    internal static class RaftStatusIcons
    {
        public const string Anchor = "Placeable_Anchor_Stationary";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>The sprite of item <paramref name="uniqueName"/>; null while items are not loaded.</summary>
        public static Sprite Get(string uniqueName)
        {
            if (Cache.TryGetValue(uniqueName, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            Item_Base item = ItemManager.GetItemByName(uniqueName);
            sprite = item != null && item.settings_Inventory != null ? item.settings_Inventory.Sprite : null;
            if (sprite != null)
            {
                Cache[uniqueName] = sprite;
            }

            return sprite;
        }

        public static void Clear()
        {
            Cache.Clear();
        }
    }
}
