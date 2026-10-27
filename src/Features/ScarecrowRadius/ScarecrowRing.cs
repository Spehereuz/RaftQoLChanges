using System.Collections.Generic;
using QoLChanges.GameState;
using UnityEngine;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// The outline of one scarecrow's zone: a line around its box at the height of the crop plots, which is a rectangle
    /// with rounded corners (the box grown by the radius). The zone is a sphere around the box, so it also reaches the
    /// floors above and below, with a smaller radius; those lines are drawn fainter. It is a separate object that follows
    /// the scarecrow, so nothing is added to the game's blocks.
    /// </summary>
    internal sealed class ScarecrowRing
    {
        // Centres of the crop plots' main colliders are 0.1-0.45 m above the floor they stand on.
        private const float LineHeight = 0.3f;
        private const int ArcSegments = 12;
        private const float NeighbourFloorAlpha = 0.5f;

        private readonly GameObject _root;
        private readonly LineRenderer[] _lines;
        private readonly int _floorRange;
        private readonly List<Vector3> _points = new List<Vector3>();
        private ScarecrowZone _builtZone;
        private float _builtRadius = -1f;

        /// <param name="floorRange">How many floors above and below the scarecrow get a line as well.</param>
        public ScarecrowRing(Transform parent, Material material, float width, int floorRange)
        {
            _root = new GameObject("ScarecrowRing");
            _root.transform.SetParent(parent, false);

            _floorRange = floorRange;
            _lines = new LineRenderer[floorRange * 2 + 1];
            for (int i = 0; i < _lines.Length; i++)
            {
                GameObject floor = new GameObject("Floor " + (i - floorRange));
                floor.transform.SetParent(_root.transform, false);
                _lines[i] = CreateLine(floor, material, width);
            }
        }

        /// <summary>Set every frame while the ring is shown; rings that were not used in a frame are hidden.</summary>
        public bool Used { get; set; }

        public void Show(Transform target, ScarecrowZone zone, float radius, Color colour)
        {
            Used = true;
            _root.transform.SetPositionAndRotation(target.position, target.rotation);
            // The ring of the preview is used for basic and advanced scarecrows alike.
            if (zone != _builtZone || !Mathf.Approximately(_builtRadius, radius))
            {
                _builtZone = zone;
                _builtRadius = radius;
                Build(zone, radius);
            }

            Color neighbour = colour;
            neighbour.a *= NeighbourFloorAlpha;
            for (int i = 0; i < _lines.Length; i++)
            {
                Color lineColour = i == _floorRange ? colour : neighbour;
                _lines[i].startColor = lineColour;
                _lines[i].endColor = lineColour;
            }

            if (!_root.activeSelf)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            Used = false;
            if (_root.activeSelf)
            {
                _root.SetActive(false);
            }
        }

        public void Destroy()
        {
            Object.Destroy(_root);
        }

        private static LineRenderer CreateLine(GameObject owner, Material material, float width)
        {
            LineRenderer line = owner.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.allowOcclusionWhenDynamic = false;
            return line;
        }

        private void Build(ScarecrowZone zone, float radius)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                float height = (i - _floorRange) * BlockCreator.FullFloorHeight + LineHeight;
                float reach = zone.HorizontalRadius(height, radius);
                _points.Clear();
                if (reach > 0f)
                {
                    AddOutline(_points, zone.Box, reach, height);
                }

                _lines[i].positionCount = _points.Count;
                _lines[i].SetPositions(_points.ToArray());
            }
        }

        /// <summary>The box seen from above, grown by <paramref name="reach"/>: four straight sides and four quarter circles.</summary>
        private static void AddOutline(List<Vector3> points, Bounds box, float reach, float height)
        {
            Vector3 min = box.min;
            Vector3 max = box.max;
            AddArc(points, max.x, max.z, 0f, reach, height);
            AddArc(points, min.x, max.z, 90f, reach, height);
            AddArc(points, min.x, min.z, 180f, reach, height);
            AddArc(points, max.x, min.z, 270f, reach, height);
        }

        private static void AddArc(List<Vector3> points, float centreX, float centreZ, float startAngle, float reach, float height)
        {
            for (int i = 0; i <= ArcSegments; i++)
            {
                float angle = (startAngle + 90f * i / ArcSegments) * Mathf.Deg2Rad;
                points.Add(new Vector3(centreX + reach * Mathf.Cos(angle), height, centreZ + reach * Mathf.Sin(angle)));
            }
        }
    }
}
