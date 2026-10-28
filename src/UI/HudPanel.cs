using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>
    /// One line of a HUD section: an item icon, rich text, segments of smaller item icons with their own text and an
    /// optional direction arrow.
    /// </summary>
    internal sealed class HudLine
    {
        public Sprite Icon { get; set; }

        public string Text { get; set; }

        /// <summary>Clockwise angle of the arrow from screen up, in degrees; null hides the arrow.</summary>
        public float? ArrowAngle { get; set; }

        /// <summary>Drawn after the text, left to right.</summary>
        public List<HudSegment> Segments { get; } = new List<HudSegment>();

        /// <summary>Segment <paramref name="index"/>, created with the segments before it when missing.</summary>
        public HudSegment GetSegment(int index)
        {
            while (Segments.Count <= index)
            {
                Segments.Add(new HudSegment());
            }

            return Segments[index];
        }

        /// <summary>Removes the segments after the first <paramref name="count"/>.</summary>
        public void TrimSegments(int count)
        {
            if (Segments.Count > count)
            {
                Segments.RemoveRange(count, Segments.Count - count);
            }
        }
    }

    /// <summary>A small item icon followed by rich text, such as one ingredient of a recipe.</summary>
    internal sealed class HudSegment
    {
        public Sprite Icon { get; set; }

        public string Text { get; set; }
    }

    /// <summary>Screen corner a HUD section is shown in.</summary>
    internal enum HudCorner
    {
        /// <summary>Above the health, hunger and thirst bars.</summary>
        BottomLeft,

        /// <summary>The game's notifications, the inventory and other windows of the game cover it.</summary>
        TopRight,
    }

    /// <summary>
    /// A block of lines owned by one feature. The owner changes the lines and the panel shows them on the next frame;
    /// lines are listed top to bottom.
    /// </summary>
    internal sealed class HudSection
    {
        public const float MinScale = 0.5f;
        public const float MaxScale = 2f;

        private float _scale = 1f;

        internal HudSection(string name, int order, HudCorner corner)
        {
            Name = name;
            Order = order;
            Corner = corner;
        }

        public string Name { get; }

        /// <summary>Sections with a lower order are closer to their corner.</summary>
        public int Order { get; }

        public HudCorner Corner { get; }

        public bool Visible { get; set; }

        /// <summary>Size of icons and text relative to the default size.</summary>
        public float Scale
        {
            get { return _scale; }
            set { _scale = Mathf.Clamp(value, MinScale, MaxScale); }
        }

        public List<HudLine> Lines { get; } = new List<HudLine>();

        /// <summary>Line <paramref name="index"/>, created with the lines before it when missing.</summary>
        public HudLine GetLine(int index)
        {
            while (Lines.Count <= index)
            {
                Lines.Add(new HudLine());
            }

            return Lines[index];
        }

        /// <summary>Removes the lines after the first <paramref name="count"/>.</summary>
        public void TrimLines(int count)
        {
            if (Lines.Count > count)
            {
                Lines.RemoveRange(count, Lines.Count - count);
            }
        }
    }

    /// <summary>
    /// Shared panels in the game HUD: above the health, hunger and thirst bars in the bottom-left corner and in the
    /// top-right corner. Features add their own sections; the panels exist while at least one section does and follow the
    /// game HUD from world to world.
    /// </summary>
    internal static class HudPanel
    {
        private const string HostName = "QoLChanges_Hud";

        private static readonly List<HudSection> Sections = new List<HudSection>();
        private static HudPanelView _view;

        /// <summary>Sections ordered by <see cref="HudSection.Order"/>, nearest to their corner first.</summary>
        public static IReadOnlyList<HudSection> ActiveSections => Sections;

        public static HudSection AddSection(string name, int order, HudCorner corner = HudCorner.BottomLeft)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Section name must not be empty.", nameof(name));
            }

            HudSection section = new HudSection(name, order, corner);
            Sections.Add(section);
            Sections.Sort((a, b) => a.Order.CompareTo(b.Order));

            if (_view == null)
            {
                GameObject host = new GameObject(HostName);
                UnityEngine.Object.DontDestroyOnLoad(host);
                _view = host.AddComponent<HudPanelView>();
            }

            return section;
        }

        public static void RemoveSection(HudSection section)
        {
            if (section == null || !Sections.Remove(section))
            {
                return;
            }

            if (_view != null)
            {
                _view.RemoveSectionView(section);
                if (Sections.Count == 0)
                {
                    // Immediate, so that a section added again in the same frame (settings reapplied) gets a new panel.
                    UnityEngine.Object.DestroyImmediate(_view.gameObject);
                    _view = null;
                }
            }
        }
    }
}
