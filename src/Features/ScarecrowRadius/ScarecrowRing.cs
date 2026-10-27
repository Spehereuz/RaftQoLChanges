using System.Collections.Generic;
using QoLChanges.GameState;
using UnityEngine;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// The outline of one scarecrow's zone: a line around its box at the height of the crop plots, which is a rectangle
    /// with rounded corners (the box grown by the radius). It is a separate object that follows the scarecrow, so nothing
    /// is added to the game's blocks.
    /// </summary>
    internal sealed class ScarecrowRing
    {
        // Centres of the crop plots' main colliders are 0.1-0.45 m above the floor they stand on.
        private const float LineHeight = 0.3f;
        private const int ArcSegments = 12;

        private readonly GameObject _root;
        private readonly LineRenderer _line;
        private readonly List<Vector3> _points = new List<Vector3>();
        private ScarecrowZone _builtZone;
        private float _builtRadius = -1f;

        public ScarecrowRing(Transform parent, Material material, float width)
        {
            _root = new GameObject("ScarecrowRing");
            _root.transform.SetParent(parent, false);

            _line = _root.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.widthMultiplier = width;
            _line.numCornerVertices = 2;
            _line.alignment = LineAlignment.View;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.allowOcclusionWhenDynamic = false;
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

            _line.startColor = colour;
            _line.endColor = colour;
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

        private void Build(ScarecrowZone zone, float radius)
        {
            float reach = zone.HorizontalRadius(LineHeight, radius);
            _points.Clear();
            if (reach > 0f)
            {
                AddOutline(_points, zone.Box, reach, LineHeight);
            }

            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
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
