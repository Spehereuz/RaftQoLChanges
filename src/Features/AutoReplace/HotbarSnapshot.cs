namespace QoLChanges.Features.AutoReplace
{
    /// <summary>The selected hotbar slot of the local player and its item, taken before the game uses the item.</summary>
    internal struct HotbarSnapshot
    {
        public HotbarSnapshot(Slot slot, int index, Item_Base item)
        {
            Slot = slot;
            Index = index;
            Item = item;
        }

        public Slot Slot { get; }

        public int Index { get; }

        public Item_Base Item { get; }

        public bool IsValid => Slot != null && Item != null;
    }
}
