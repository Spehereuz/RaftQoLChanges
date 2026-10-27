using HarmonyLib;

namespace QoLChanges.GameState
{
    /// <summary>
    /// Fill state of collection nets. A net is an <see cref="ItemCollector"/> that is not player controlled (the hook
    /// is); it stops collecting once it holds <c>maxNumberOfItems</c> items: 10 for the basic net, 15 for the advanced
    /// one, 0 means no limit. The host collects and tells every other player, so the collected items are known on
    /// every player's game.
    /// </summary>
    internal static class ItemNets
    {
        private static readonly AccessTools.FieldRef<ItemCollector, int> CapacityField =
            AccessTools.FieldRefAccess<ItemCollector, int>("maxNumberOfItems");

        public static bool IsNet(ItemCollector collector)
        {
            return collector != null && !collector.playerControlled;
        }

        /// <summary>Items the net holds at most; 0 when it has no limit.</summary>
        public static int Capacity(ItemCollector collector)
        {
            return CapacityField(collector);
        }

        public static int Count(ItemCollector collector)
        {
            return collector.collectedItems != null ? collector.collectedItems.Count : 0;
        }

        /// <summary>Whether the net collects nothing more until it is emptied.</summary>
        public static bool IsFull(ItemCollector collector)
        {
            int capacity = Capacity(collector);
            return capacity > 0 && Count(collector) >= capacity;
        }
    }
}
