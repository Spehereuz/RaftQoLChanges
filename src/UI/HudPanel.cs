using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>One line of a HUD section: an item icon, rich text and an optional direction arrow.</summary>
    internal sealed class HudLine
    {
        public Sprite Icon { get; set; }

        public string Text { get; set; }

        /// <summary>Clockwise angle of the arrow from screen up, in degrees; null hides the arrow.</summary>
        public float? ArrowAngle { get; set; }
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

        internal HudSection(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }

        /// <summary>Sections with a lower order are closer to the stat bars.</summary>
        public int Order { get; }

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
    /// Shared panel in the game HUD, stacked above the health, hunger and thirst bars in the bottom-left corner.
    /// Features add their own sections; the panel exists while at least one section does and follows the game HUD
    /// from world to world.
    /// </summary>
    internal static class HudPanel
    {
        private const string HostName = "QoLChanges_Hud";

        private static readonly List<HudSection> Sections = new List<HudSection>();
        private static HudPanelView _view;

        /// <summary>Sections ordered from the stat bars upwards.</summary>
        public static IReadOnlyList<HudSection> ActiveSections => Sections;

        public static HudSection AddSection(string name, int order)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Section name must not be empty.", nameof(name));
            }

            HudSection section = new HudSection(name, order);
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
