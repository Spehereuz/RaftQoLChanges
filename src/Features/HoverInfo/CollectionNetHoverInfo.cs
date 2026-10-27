using QoLChanges.GameState;
using QoLChanges.UI;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Items in the collection net under the crosshair out of how many it holds: "Items 7/10", "Items 0/10" for an empty
    /// net and "Full 10/10" (red) once it collects nothing more. A net without a limit shows only the count. The game's
    /// own prompt above the crosshair offers to collect the items, but only shows how many there are.
    /// </summary>
    internal sealed class CollectionNetHoverInfo : IHoverInfoProvider
    {
        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;
            ItemNet net = HoverTargets.Find<ItemNet>(target);
            if (net == null)
            {
                return false;
            }

            ItemCollector collector = net.itemCollector;
            if (collector != null && Helper.LocalPlayerIsWithinDistance(net.transform.position, Player.UseDistance))
            {
                text = Describe(collector);
            }

            return true;
        }

        private static string Describe(ItemCollector collector)
        {
            int count = ItemNets.Count(collector);
            int capacity = ItemNets.Capacity(collector);
            if (capacity <= 0)
            {
                return "Items " + count;
            }

            string amount = count + "/" + capacity;
            return ItemNets.IsFull(collector)
                ? StatusText.Colored("Full " + amount, StatusText.Problem)
                : "Items " + amount;
        }
    }
}
