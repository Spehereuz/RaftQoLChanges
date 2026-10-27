using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Cooking stands (grills, smelters, paint mill) and cooking tables (cooking pot, juicer).
    /// <list type="bullet">
    /// <item>One item, or the item under the crosshair: "Raw salmon · cooking 45%", "Cooked salmon ready".</item>
    /// <item>Several items: "2 cooking (45%) · 1 ready", the percentage is the item closest to being ready.</item>
    /// <item>Cooking pot and juicer: "Fish stew · cooking 45%", then "Fish stew · 3 portions".</item>
    /// <item>"· no fuel" / "· no power" is added while something waits because the fire is out or there is no power.</item>
    /// </list>
    /// Water purifiers are cooking stands too, they are described by <see cref="PurifierHoverInfo"/>.
    /// </summary>
    internal sealed class CookingHoverInfo : IHoverInfoProvider
    {
        private const string NoFuelText = "no fuel";
        private const string NoPowerText = "no power";

        private static readonly AccessTools.FieldRef<CookingSlot, List<CookItemConnection>> ItemConnectionsField =
            AccessTools.FieldRefAccess<CookingSlot, List<CookItemConnection>>("itemConnections");

        private static readonly List<Renderer> RendererBuffer = new List<Renderer>();

        private readonly bool _cooking;
        private readonly bool _smelters;

        /// <param name="cooking">Grills, cooking pots and juicers.</param>
        /// <param name="smelters">Smelters and the paint mill.</param>
        public CookingHoverInfo(bool cooking, bool smelters)
        {
            _cooking = cooking;
            _smelters = smelters;
        }

        private enum StandKind
        {
            Food,
            Smelter,
            PaintMill,
        }

        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;

            Block_CookingStand stand = FindStand(target, out CookingSlot aimedSlot);
            if (stand != null)
            {
                return TryDescribeStand(stand, aimedSlot, out text);
            }

            CookingTable table = HoverTargets.Find<CookingTable>(target);
            if (table != null && _cooking)
            {
                if (Helper.LocalPlayerIsWithinDistance(table.transform.position, Player.UseDistance))
                {
                    text = DescribeTable(table);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// A finished item has its colliders enabled for collecting and is found by the game's item raycast
        /// (Block_CookingStand.OnIsRayed); everything else is a part of the stand the interactable belongs to.
        /// </summary>
        private static Block_CookingStand FindStand(RaycastInteractable target, out CookingSlot itemSlot)
        {
            itemSlot = null;
            if (Helper.HitAtCursor(out RaycastHit hit, Player.UseDistance, LayerMasks.MASK_Item, QueryTriggerInteraction.UseGlobal)
                && hit.transform != null)
            {
                CookingSlot slot = hit.transform.GetComponent<CookingSlot>();
                Block_CookingStand owner = slot != null ? slot.GetComponentInParent<Block_CookingStand>() : null;
                if (owner != null)
                {
                    itemSlot = slot;
                    return owner;
                }
            }

            return HoverTargets.Find<Block_CookingStand>(target);
        }

        private bool TryDescribeStand(Block_CookingStand stand, CookingSlot itemSlot, out string text)
        {
            text = null;
            if (stand is Block_CookingStand_Purifier)
            {
                return false;
            }

            StandKind kind = GetKind(stand);
            if (kind == StandKind.Food ? !_cooking : !_smelters)
            {
                return false;
            }

            if (!Helper.LocalPlayerIsWithinDistance(stand.transform.position, Player.UseDistance) || stand.cookingSlots == null)
            {
                return true;
            }

            CookingSlot aimed = Master(itemSlot);
            if (aimed == null)
            {
                aimed = FindAimedSlot(stand);
            }

            text = aimed != null && IsOccupied(aimed) ? DescribeSlot(stand, kind, aimed) : DescribeStand(stand, kind);
            return true;
        }

        private static string DescribeStand(Block_CookingStand stand, StandKind kind)
        {
            int cooking = 0;
            int ready = 0;
            int bestProgress = 0;
            CookingSlot single = null;

            foreach (CookingSlot slot in stand.cookingSlots)
            {
                if (!IsOccupied(slot))
                {
                    continue;
                }

                single = slot;
                if (slot.IsComplete)
                {
                    ready++;
                }
                else
                {
                    cooking++;
                    bestProgress = Mathf.Max(bestProgress, CookingTimers.GetProgress(slot));
                }
            }

            if (cooking + ready == 0)
            {
                return null;
            }

            if (cooking + ready == 1)
            {
                return DescribeSlot(stand, kind, single);
            }

            string text = cooking > 0
                ? cooking + " " + Verb(kind) + " (" + StatusText.Colored(bestProgress + "%", StatusText.Progress) + ")"
                : null;
            if (ready > 0)
            {
                text = StatusText.Join(text, StatusText.Colored(ready + " ready", StatusText.Ready));
            }

            return cooking > 0 ? StatusText.Join(text, StandProblem(stand)) : text;
        }

        private static string DescribeSlot(Block_CookingStand stand, StandKind kind, CookingSlot slot)
        {
            Item_Base item = slot.CurrentItem;
            if (slot.IsComplete)
            {
                return StatusText.Colored(ResultName(item) + " ready", StatusText.Ready);
            }

            string text = Name(item) + StatusText.Separator + Verb(kind) + " "
                + StatusText.Colored(CookingTimers.GetProgress(slot) + "%", StatusText.Progress);
            return StatusText.Join(text, StandProblem(stand));
        }

        private static string DescribeTable(CookingTable table)
        {
            SO_CookingTable_Recipe recipe = table.CurrentRecipe;
            if (recipe == null)
            {
                return null;
            }

            string name = Name(recipe.Result);
            uint portions = table.Portions;
            if (portions > 0)
            {
                string amount = portions + (portions == 1 ? " portion" : " portions");
                return name + StatusText.Separator + StatusText.Colored(amount, StatusText.Ready);
            }

            bool juicer = table is CookingTable_Juicer;
            string text = name + StatusText.Separator + (juicer ? "blending " : "cooking ")
                + StatusText.Colored(CookingTimers.GetProgress(table) + "%", StatusText.Progress);
            if (!CookingTimers.IsRunning(table))
            {
                text = StatusText.Join(text, StatusText.Colored(juicer ? NoPowerText : NoFuelText, StatusText.Problem));
            }

            return text;
        }

        /// <summary>"no fuel" / "no power" while items wait for the stand's energy source; null when it runs.</summary>
        private static string StandProblem(Block_CookingStand stand)
        {
            if (CookingTimers.IsRunning(stand))
            {
                return null;
            }

            return StatusText.Colored(stand.fuel != null ? NoFuelText : NoPowerText, StatusText.Problem);
        }

        /// <summary>
        /// The item under the crosshair while it is cooking: raw items have no colliders, so the ray is tested
        /// against the bounds of the item models shown on the slots, and the nearest one wins.
        /// </summary>
        private static CookingSlot FindAimedSlot(Block_CookingStand stand)
        {
            Camera camera = Helper.MainCamera;
            if (camera == null)
            {
                return null;
            }

            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            float maxDistance = Player.UseDistance * 1.1f;
            CookingSlot nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (CookingSlot slot in stand.cookingSlots)
            {
                if (!IsOccupied(slot) || !TryGetModelBounds(slot, out Bounds bounds))
                {
                    continue;
                }

                if (bounds.IntersectRay(ray, out float distance) && distance <= maxDistance && distance < nearestDistance)
                {
                    nearest = slot;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>Bounds of the raw or cooked model the slot currently shows (CookItemConnection).</summary>
        private static bool TryGetModelBounds(CookingSlot slot, out Bounds bounds)
        {
            bounds = default(Bounds);
            List<CookItemConnection> connections = ItemConnectionsField(slot);
            if (connections == null)
            {
                return false;
            }

            bool found = false;
            foreach (CookItemConnection connection in connections)
            {
                if (connection == null || connection.cookableItem != slot.CurrentItem)
                {
                    continue;
                }

                found |= Encapsulate(connection.rawItem, ref bounds, found);
                found |= Encapsulate(connection.cookedItem, ref bounds, found);
            }

            return found;
        }

        private static bool Encapsulate(GameObject model, ref Bounds bounds, bool hasBounds)
        {
            if (model == null || !model.activeInHierarchy)
            {
                return false;
            }

            bool found = false;
            model.GetComponentsInChildren(false, RendererBuffer);
            foreach (Renderer renderer in RendererBuffer)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (hasBounds || found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                }

                found = true;
            }

            RendererBuffer.Clear();
            return found;
        }

        /// <summary>A slot with an item of its own; a fake slot is occupied by an item cooked on another slot.</summary>
        private static bool IsOccupied(CookingSlot slot)
        {
            return slot != null && !slot.FakeSlot && slot.CurrentItem != null;
        }

        private static CookingSlot Master(CookingSlot slot)
        {
            if (slot == null)
            {
                return null;
            }

            return slot.FakeSlot ? slot.connectedSlot : slot;
        }

        /// <summary>By the placeable item of the stand, so electric variants and stands of other mods are sorted too.</summary>
        private static StandKind GetKind(Block_CookingStand stand)
        {
            Item_Base item = stand.buildableItem;
            string name = item != null ? item.UniqueName : null;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.IndexOf("Smelter", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return StandKind.Smelter;
                }

                if (name.IndexOf("PaintMill", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return StandKind.PaintMill;
                }
            }

            return StandKind.Food;
        }

        private static string Verb(StandKind kind)
        {
            switch (kind)
            {
                case StandKind.Smelter:
                    return "smelting";
                case StandKind.PaintMill:
                    return "grinding";
                default:
                    return "cooking";
            }
        }

        private static string ResultName(Item_Base item)
        {
            ItemInstance_Cookable cookable = item != null ? item.settings_cookable : null;
            Cost result = cookable != null ? cookable.CookingResult : null;
            return result != null && result.item != null ? Name(result.item) : Name(item);
        }

        private static string Name(Item_Base item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            ItemInstance_Inventory inventory = item.settings_Inventory;
            string name = inventory != null ? inventory.DisplayName : null;
            return string.IsNullOrEmpty(name) ? item.UniqueName : name;
        }
    }
}
