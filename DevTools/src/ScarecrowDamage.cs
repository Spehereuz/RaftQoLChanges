using System;
using UnityEngine;

namespace QoLDevTools
{
    /// <summary>
    /// Damages the scarecrow under the crosshair the way a pecking seagull does on the host (Scarecrow.DamageScarecrow):
    /// every hit removes one part and is sent to other players; after the last part the game removes the scarecrow.
    /// </summary>
    internal static class ScarecrowDamage
    {
        public const string Syntax = "qoldev.scarecrow [parts]";

        public static string Damage(string[] args)
        {
            int count = 1;
            if (args != null && args.Length > 1)
            {
                return "Syntax: " + Syntax;
            }

            if (args != null && args.Length == 1 && (!int.TryParse(args[0].Trim(), out count) || count < 1))
            {
                return "'" + args[0] + "' is not a number of parts. Syntax: " + Syntax;
            }

            if (!Raft_Network.IsHost)
            {
                return "Only the host can damage scarecrows.";
            }

            Scarecrow scarecrow = FindAimedScarecrow();
            if (scarecrow == null)
            {
                return "Aim at a scarecrow.";
            }

            if (scarecrow.invurnerable)
            {
                return "This scarecrow is indestructible.";
            }

            int before = scarecrow.Health;
            if (before <= 0)
            {
                return "This scarecrow is already destroyed.";
            }

            int hits = Math.Min(count, before);
            for (int i = 0; i < hits; i++)
            {
                scarecrow.DamageScarecrow();
            }

            int left = scarecrow.Health;
            string removed = "Removed " + hits + " part" + (hits == 1 ? string.Empty : "s") + ", ";
            return left > 0
                ? removed + left + " left."
                : removed + "the scarecrow is destroyed and disappears in 3 seconds.";
        }

        private static Scarecrow FindAimedScarecrow()
        {
            LayerMask mask = LayerMasks.MASK_Block | LayerMasks.MASK_Item;
            if (!Helper.HitAtCursor(out RaycastHit hit, Player.UseDistance * 2f, mask, QueryTriggerInteraction.Ignore) || hit.collider == null)
            {
                return null;
            }

            Scarecrow scarecrow = hit.collider.GetComponentInParent<Scarecrow>();
            Block block = scarecrow != null ? scarecrow.GetComponent<Block>() : null;
            return block != null && block.hasBeenPlaced ? scarecrow : null;
        }
    }
}
