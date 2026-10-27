using QoLChanges.GameState;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// State of the scarecrow under the crosshair: "Parts 2/3" for a basic scarecrow, which loses a part every time a
    /// seagull pecks it, and "Indestructible" for an advanced one; "· seagull" while a seagull sits on it. The game shows
    /// the parts only as the missing head and arms.
    /// </summary>
    /// <remarks>
    /// Scarecrows are not interactables, so they are found with a raycast of their own against blocks and items: the
    /// scarecrow has to be the first thing under the crosshair, within interaction distance.
    /// </remarks>
    internal sealed class ScarecrowHoverInfo : IHoverInfoProvider
    {
        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;
            Scarecrow scarecrow = FindAimedScarecrow();
            if (scarecrow == null)
            {
                return false;
            }

            text = Describe(scarecrow);
            return true;
        }

        private static Scarecrow FindAimedScarecrow()
        {
            LayerMask mask = LayerMasks.MASK_Block | LayerMasks.MASK_Item;
            if (!Helper.HitAtCursor(out RaycastHit hit, Player.UseDistance, mask, QueryTriggerInteraction.Ignore) || hit.collider == null)
            {
                return null;
            }

            Scarecrow scarecrow = hit.collider.GetComponentInParent<Scarecrow>();
            Block block = scarecrow != null ? scarecrow.GetComponent<Block>() : null;
            return block != null && block.hasBeenPlaced ? scarecrow : null;
        }

        private static string Describe(Scarecrow scarecrow)
        {
            if (scarecrow.invurnerable)
            {
                return StatusText.Colored("Indestructible", StatusText.Ready);
            }

            int parts = Scarecrows.Parts(scarecrow);
            int max = Scarecrows.MaxParts(scarecrow);
            string text = "Parts " + StatusText.Colored(
                parts + "/" + max,
                ToolDurabilityColor.Evaluate(max > 0 ? (float)parts / max : 0f));
            return Scarecrows.HasSeagull(scarecrow)
                ? StatusText.Join(text, StatusText.Colored("seagull", StatusText.Problem))
                : text;
        }
    }
}
