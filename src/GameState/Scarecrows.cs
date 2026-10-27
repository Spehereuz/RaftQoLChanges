using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QoLChanges.GameState
{
    /// <summary>
    /// Where scarecrows keep seagulls away from crops, the way <c>Seagull.FindCropplotAndNest</c> decides it on the host:
    /// a crop plot is safe while a sphere of <c>searchScarecrowRadius</c> (6 m) around the centre of its main collider
    /// touches a collider tagged "Scarecrow" of a scarecrow that is not destroyed. The seagull then pecks a basic
    /// scarecrow instead of the crops (one part per attack) and leaves crops near an advanced one alone. Every player
    /// has the same scarecrows, so the zones are the same on every player's game.
    /// </summary>
    internal static class Scarecrows
    {
        public const float DefaultRadius = 6f;

        private const float SeagullSearchInterval = 5f;
        private const float SearchInterval = 1f;

        private static readonly AccessTools.FieldRef<Seagull, float> RadiusField =
            AccessTools.FieldRefAccess<Seagull, float>("searchScarecrowRadius");

        private static readonly Dictionary<Scarecrow, ScarecrowZone> Zones = new Dictionary<Scarecrow, ScarecrowZone>();
        private static readonly List<Scarecrow> PlacedScarecrows = new List<Scarecrow>();
        private static readonly List<Scarecrow> StaleZones = new List<Scarecrow>();

        private static float _radius = DefaultRadius;
        private static bool _radiusRead;
        private static float _nextSeagullSearch;
        private static float _nextSearch;
        private static int _placedBlockCount = -1;

        /// <summary>
        /// Radius the seagulls search for scarecrows with. It is read from a seagull once one is around, so a radius
        /// changed by another mod is shown as well; until then the radius of the seagull prefab is used.
        /// </summary>
        public static float Radius
        {
            get
            {
                if (!_radiusRead && Time.unscaledTime >= _nextSeagullSearch)
                {
                    _nextSeagullSearch = Time.unscaledTime + SeagullSearchInterval;
                    Seagull seagull = UnityEngine.Object.FindObjectOfType<Seagull>();
                    if (seagull != null)
                    {
                        _radius = RadiusField(seagull);
                        _radiusRead = true;
                    }
                }

                return _radius;
            }
        }

        /// <summary>
        /// Placed scarecrows, destroyed ones included. The game keeps no list of them, so they are searched again every
        /// second and as soon as a block is placed or removed.
        /// </summary>
        public static List<Scarecrow> Placed()
        {
            List<Block> blocks = BlockCreator.GetPlacedBlocks();
            int blockCount = blocks != null ? blocks.Count : 0;
            if (Time.unscaledTime >= _nextSearch || blockCount != _placedBlockCount)
            {
                _nextSearch = Time.unscaledTime + SearchInterval;
                _placedBlockCount = blockCount;
                Search();
            }

            return PlacedScarecrows;
        }

        /// <summary>The shape of the scarecrow's colliders; it never changes, so it is measured once per scarecrow.</summary>
        public static ScarecrowZone GetZone(Scarecrow scarecrow)
        {
            if (!Zones.TryGetValue(scarecrow, out ScarecrowZone zone))
            {
                zone = ScarecrowZone.Measure(scarecrow);
                Zones[scarecrow] = zone;
            }

            return zone;
        }

        public static void Clear()
        {
            PlacedScarecrows.Clear();
            Zones.Clear();
            _radius = DefaultRadius;
            _radiusRead = false;
            _nextSeagullSearch = 0f;
            _nextSearch = 0f;
            _placedBlockCount = -1;
        }

        private static void Search()
        {
            PlacedScarecrows.Clear();
            foreach (Scarecrow scarecrow in UnityEngine.Object.FindObjectsOfType<Scarecrow>())
            {
                // The preview of a scarecrow that is being placed has the same components.
                Block block = scarecrow.GetComponent<Block>();
                if (block != null && block.hasBeenPlaced)
                {
                    PlacedScarecrows.Add(scarecrow);
                }
            }

            StaleZones.Clear();
            foreach (Scarecrow scarecrow in Zones.Keys)
            {
                if (scarecrow == null)
                {
                    StaleZones.Add(scarecrow);
                }
            }

            foreach (Scarecrow scarecrow in StaleZones)
            {
                Zones.Remove(scarecrow);
            }
        }
    }

    /// <summary>
    /// The colliders the seagulls look for, as one box in the scarecrow's own space: a pole of 0.25 x 1.4 x 0.6 m for the
    /// basic scarecrow and the whole body for the advanced one. A point is inside the zone when it is at most the radius
    /// away from the box, which is what an overlap sphere around the point finds.
    /// </summary>
    internal sealed class ScarecrowZone
    {
        private const string ZoneTag = "Scarecrow";

        private ScarecrowZone(Bounds box)
        {
            Box = box;
        }

        /// <summary>Box of the tagged colliders relative to the scarecrow's transform.</summary>
        public Bounds Box { get; }

        public static ScarecrowZone Measure(Scarecrow scarecrow)
        {
            Transform root = scarecrow.transform;
            bool found = false;
            Bounds box = new Bounds(Vector3.zero, Vector3.zero);
            foreach (BoxCollider collider in scarecrow.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!collider.CompareTag(ZoneTag))
                {
                    continue;
                }

                Vector3 extents = collider.size * 0.5f;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = collider.center + new Vector3(
                        (corner & 1) == 0 ? -extents.x : extents.x,
                        (corner & 2) == 0 ? -extents.y : extents.y,
                        (corner & 4) == 0 ? -extents.z : extents.z);
                    Vector3 point = root.InverseTransformPoint(collider.transform.TransformPoint(local));
                    if (found)
                    {
                        box.Encapsulate(point);
                    }
                    else
                    {
                        box = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }

            return new ScarecrowZone(box);
        }

        /// <summary>
        /// Radius of the zone in a horizontal plane at <paramref name="height"/> above the scarecrow's base: the full
        /// radius beside the box, less above and below it; 0 where the zone does not reach.
        /// </summary>
        public float HorizontalRadius(float height, float radius)
        {
            float vertical = Mathf.Max(0f, Mathf.Max(Box.min.y - height, height - Box.max.y));
            return vertical >= radius ? 0f : Mathf.Sqrt(radius * radius - vertical * vertical);
        }
    }
}
