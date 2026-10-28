using System;
using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>
    /// Draws the <see cref="HudPanel"/> sections inside the game HUD, one panel per <see cref="HudCorner"/>. The panels
    /// live in the HUD canvas, so they are hidden together with the rest of the HUD; they are created again after a world
    /// is loaded. Text uses the font and shadow of the game's interaction prompt, arrows the sprite of the settings
    /// dropdowns.
    /// </summary>
    internal sealed class HudPanelView : MonoBehaviour
    {
        private const string PanelName = "QoLChanges_HudPanel";

        // Sizes at scale 1, in HUD canvas units.
        private const float BaseIconSize = 32f;
        private const float BaseRowHeight = 34f;
        private const int BaseFontSize = 22;
        private const float BaseArrowSize = 22f;
        private const float BaseSpacing = 6f;
        private const float BaseSegmentIconSize = 28f;
        private const float BaseSegmentSpacing = 14f;
        private const float BaseSegmentInnerSpacing = 3f;
        private const float SectionSpacing = 6f;
        private const float GapAboveBars = 12f;

        // Health, hunger and thirst bars start with a 43 px wide icon background; HUD icons are centred on it.
        private const float StatIconCenterX = 21.5f;
        private static readonly Vector2 FallbackPosition = new Vector2(16f, 190f);

        // As far from the top edge as from the right one; the game's notifications slide in over it for a few seconds.
        private static readonly Vector2 TopRightOffset = new Vector2(-16f, -16f);

        private static readonly AccessTools.FieldRef<DisplayTextManager, DisplayText[]> DisplayTextsField =
            AccessTools.FieldRefAccess<DisplayTextManager, DisplayText[]>("displayTexts");

        private static readonly AccessTools.FieldRef<DisplayText, Text> TextComponentField =
            AccessTools.FieldRefAccess<DisplayText, Text>("textComponent");

        private static readonly AccessTools.FieldRef<GeneralSettingsBox, Dropdown> LanguageDropdownField =
            AccessTools.FieldRefAccess<GeneralSettingsBox, Dropdown>("dropdown_language");

        private static readonly HudCorner[] Corners = { HudCorner.BottomLeft, HudCorner.TopRight };

        private readonly Dictionary<HudSection, SectionView> _views = new Dictionary<HudSection, SectionView>();
        private readonly Dictionary<HudCorner, RectTransform> _panels = new Dictionary<HudCorner, RectTransform>();
        private readonly List<HudSection> _cornerSections = new List<HudSection>();

        private CanvasHelper _canvas;
        private UISlider_Stat[] _statBars;
        private TextStyle _textStyle;
        private Sprite _arrowSprite;
        private bool _arrowSearched;
        private bool _errorLogged;

        public void RemoveSectionView(HudSection section)
        {
            if (_views.TryGetValue(section, out SectionView view))
            {
                view.Destroy();
                _views.Remove(section);
            }
        }

        private void LateUpdate()
        {
            try
            {
                foreach (HudCorner corner in Corners)
                {
                    Sync(corner);
                }
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error("HUD panel: failed to update.", e);
                }
            }
        }

        /// <summary>The panels are removed with the HUD they live in; this object only owns them while they exist.</summary>
        private void OnDestroy()
        {
            DestroyPanels();
            _views.Clear();
        }

        /// <summary>Shows the sections of one corner; a lower order is closer to the corner.</summary>
        private void Sync(HudCorner corner)
        {
            _cornerSections.Clear();
            bool anyShown = false;
            foreach (HudSection section in HudPanel.ActiveSections)
            {
                if (section.Corner == corner)
                {
                    _cornerSections.Add(section);
                    anyShown |= IsShown(section);
                }
            }

            _panels.TryGetValue(corner, out RectTransform panel);
            if (!anyShown)
            {
                if (panel != null && panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                }

                return;
            }

            panel = EnsurePanel(corner);
            if (panel == null)
            {
                return;
            }

            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            HudSection nearest = null;
            for (int i = 0; i < _cornerSections.Count; i++)
            {
                HudSection section = _cornerSections[i];
                SectionView view = GetView(section, panel);
                bool shown = IsShown(section);
                view.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                if (nearest == null)
                {
                    nearest = section;
                }

                // The layout runs top to bottom: at the bottom the nearest section is the last child, at the top the first.
                int siblingIndex = corner == HudCorner.BottomLeft ? _cornerSections.Count - 1 - i : i;
                if (view.Root.GetSiblingIndex() != siblingIndex)
                {
                    view.Root.SetSiblingIndex(siblingIndex);
                }

                view.Apply(section, _arrowSprite);
            }

            if (corner == HudCorner.BottomLeft)
            {
                PlaceAboveBars(panel, nearest.Scale);
            }
        }

        private static bool IsShown(HudSection section)
        {
            return section.Visible && section.Lines.Count > 0;
        }

        private SectionView GetView(HudSection section, RectTransform panel)
        {
            if (!_views.TryGetValue(section, out SectionView view))
            {
                view = new SectionView(panel, _textStyle, section.Name);
                _views[section] = view;
            }

            return view;
        }

        /// <summary>The panel of a corner in the HUD of the current world; null while there is no world.</summary>
        private RectTransform EnsurePanel(HudCorner corner)
        {
            CanvasHelper canvas = ComponentManager<CanvasHelper>.Value;
            if (canvas == null || canvas.StatSliderParent == null)
            {
                return null;
            }

            if (_canvas != canvas)
            {
                // Panels and views of the previous world were destroyed together with its HUD.
                DestroyPanels();
                _views.Clear();
                _canvas = canvas;
                _statBars = new[] { canvas.healthSlider, canvas.hungerSlider, canvas.thirstSlider };
                _textStyle = null;
            }

            if (_textStyle == null && !TryGetTextStyle(canvas, out _textStyle))
            {
                return null;
            }

            if (!_arrowSearched)
            {
                _arrowSearched = true;
                _arrowSprite = FindArrowSprite();
            }

            if (_panels.TryGetValue(corner, out RectTransform panel) && panel != null)
            {
                return panel;
            }

            panel = corner == HudCorner.TopRight ? CreateTopRightPanel(canvas) : CreateBottomLeftPanel(canvas);
            _panels[corner] = panel;
            return panel;
        }

        /// <summary>Inside the stat bars' parent, moved above the highest bar every frame.</summary>
        private static RectTransform CreateBottomLeftPanel(CanvasHelper canvas)
        {
            RectTransform panel = CreatePanel(canvas.StatSliderParent.transform, PanelName, TextAnchor.LowerLeft);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            return panel;
        }

        /// <summary>
        /// In the HUD canvas right after the stat bars, so the inventory, storages and other windows of the game are drawn
        /// over it.
        /// </summary>
        private static RectTransform CreateTopRightPanel(CanvasHelper canvas)
        {
            Transform statBars = canvas.StatSliderParent.transform;
            RectTransform panel = CreatePanel(statBars.parent, PanelName + "_TopRight", TextAnchor.UpperRight);
            panel.SetSiblingIndex(statBars.GetSiblingIndex() + 1);
            panel.anchorMin = Vector2.one;
            panel.anchorMax = Vector2.one;
            panel.pivot = Vector2.one;
            panel.anchoredPosition = TopRightOffset;
            return panel;
        }

        private static RectTransform CreatePanel(Transform parent, string name, TextAnchor alignment)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.layer = parent.gameObject.layer;
            RectTransform panel = (RectTransform)panelObject.transform;
            panel.SetParent(parent, false);

            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            ConfigureStack(layout, SectionSpacing);
            layout.childAlignment = alignment;

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        private void DestroyPanels()
        {
            foreach (RectTransform panel in _panels.Values)
            {
                if (panel != null)
                {
                    DestroyImmediate(panel.gameObject);
                }
            }

            _panels.Clear();
        }

        /// <summary>Above the highest stat bar, icons centred on the stat icons.</summary>
        private void PlaceAboveBars(RectTransform panel, float scale)
        {
            Transform parent = panel.parent;
            bool found = false;
            float top = float.MinValue;
            float left = float.MaxValue;
            foreach (UISlider_Stat bar in _statBars)
            {
                RectTransform rect = bar != null ? bar.transform as RectTransform : null;
                if (rect == null || rect.parent != parent || !rect.gameObject.activeSelf)
                {
                    continue;
                }

                Rect bounds = rect.rect;
                Vector3 position = rect.localPosition;
                top = Mathf.Max(top, position.y + bounds.yMax * rect.localScale.y);
                left = Mathf.Min(left, position.x + bounds.xMin * rect.localScale.x);
                found = true;
            }

            Vector2 target = found
                ? new Vector2(left + StatIconCenterX - BaseIconSize * scale * 0.5f, top + GapAboveBars)
                : FallbackPosition;

            Vector3 current = panel.localPosition;
            if (!Mathf.Approximately(current.x, target.x) || !Mathf.Approximately(current.y, target.y))
            {
                panel.localPosition = new Vector3(target.x, target.y, 0f);
            }
        }

        private static bool TryGetTextStyle(CanvasHelper canvas, out TextStyle style)
        {
            style = null;
            DisplayTextManager manager = canvas.displayTextManager != null
                ? canvas.displayTextManager
                : ComponentManager<DisplayTextManager>.Value;
            if (manager == null)
            {
                return false;
            }

            DisplayText[] displayTexts = DisplayTextsField(manager);
            Text prompt = displayTexts != null && displayTexts.Length > 0 && displayTexts[0] != null
                ? TextComponentField(displayTexts[0])
                : null;
            if (prompt == null || prompt.font == null)
            {
                return false;
            }

            Shadow shadow = prompt.GetComponent<Shadow>();
            style = new TextStyle
            {
                Font = prompt.font,
                FontStyle = prompt.fontStyle,
                HasShadow = shadow != null,
                ShadowColor = shadow != null ? shadow.effectColor : Color.black,
                ShadowDistance = shadow != null ? shadow.effectDistance : Vector2.zero,
            };
            return true;
        }

        /// <summary>
        /// The arrow of the language dropdown in the settings (points down). Only the sprite is used, so any instance,
        /// including the prefab, will do.
        /// </summary>
        private static Sprite FindArrowSprite()
        {
            foreach (GeneralSettingsBox box in Resources.FindObjectsOfTypeAll<GeneralSettingsBox>())
            {
                Dropdown dropdown = box != null ? LanguageDropdownField(box) : null;
                Transform arrow = dropdown != null ? dropdown.transform.Find("Arrow") : null;
                Image image = arrow != null ? arrow.GetComponent<Image>() : null;
                if (image != null && image.sprite != null)
                {
                    return image.sprite;
                }
            }

            ModLog.Warning("HUD panel: dropdown arrow sprite not found, direction arrows are not shown.");
            return null;
        }

        private static void ConfigureStack(HorizontalOrVerticalLayoutGroup layout, float spacing)
        {
            layout.childAlignment = layout is VerticalLayoutGroup ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Image CreateIcon(Transform parent, out LayoutElement layout)
        {
            GameObject icon = CreateChild(parent, "Icon");
            Image image = icon.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            layout = icon.AddComponent<LayoutElement>();
            return image;
        }

        private static Text CreateText(Transform parent, TextStyle style)
        {
            GameObject textObject = CreateChild(parent, "Text");
            Text text = textObject.AddComponent<Text>();
            text.font = style.Font;
            text.fontStyle = style.FontStyle;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            if (style.HasShadow)
            {
                Shadow shadow = textObject.AddComponent<Shadow>();
                shadow.effectColor = style.ShadowColor;
                shadow.effectDistance = style.ShadowDistance;
            }

            return text;
        }

        private static void SetIcon(Image image, Sprite sprite)
        {
            if (image.sprite != sprite)
            {
                image.sprite = sprite;
            }

            bool hasIcon = sprite != null;
            if (image.enabled != hasIcon)
            {
                image.enabled = hasIcon;
            }
        }

        private static void SetText(Text text, string value)
        {
            value = value ?? string.Empty;
            if (text.text != value)
            {
                text.text = value;
            }
        }

        private static void SetSize(LayoutElement element, float size)
        {
            element.minWidth = size;
            element.minHeight = size;
            element.preferredWidth = size;
            element.preferredHeight = size;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        private static void SetActive(GameObject gameObject, bool active)
        {
            if (gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }

        private sealed class TextStyle
        {
            public Font Font;
            public FontStyle FontStyle;
            public bool HasShadow;
            public Color ShadowColor;
            public Vector2 ShadowDistance;
        }

        private sealed class SectionView
        {
            private readonly List<RowView> _rows = new List<RowView>();
            private readonly TextStyle _style;
            private float _scale = -1f;

            public SectionView(Transform panel, TextStyle style, string name)
            {
                _style = style;
                GameObject root = CreateChild(panel, "Section_" + name);
                ConfigureStack(root.AddComponent<VerticalLayoutGroup>(), 0f);
                Root = root.transform;
            }

            public Transform Root { get; }

            public void SetActive(bool active)
            {
                if (Root.gameObject.activeSelf != active)
                {
                    Root.gameObject.SetActive(active);
                }
            }

            public void Apply(HudSection section, Sprite arrowSprite)
            {
                List<HudLine> lines = section.Lines;
                while (_rows.Count < lines.Count)
                {
                    _rows.Add(new RowView(Root, _style, _rows.Count));
                    _scale = -1f;
                }

                while (_rows.Count > lines.Count)
                {
                    RowView last = _rows[_rows.Count - 1];
                    _rows.RemoveAt(_rows.Count - 1);
                    UnityEngine.Object.DestroyImmediate(last.Root);
                }

                if (!Mathf.Approximately(_scale, section.Scale))
                {
                    _scale = section.Scale;
                    foreach (RowView row in _rows)
                    {
                        row.SetScale(_scale);
                    }
                }

                for (int i = 0; i < lines.Count; i++)
                {
                    _rows[i].Apply(lines[i], arrowSprite);
                }
            }

            public void Destroy()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root.gameObject);
                }
            }
        }

        private sealed class RowView
        {
            private readonly TextStyle _style;
            private readonly LayoutElement _rowLayout;
            private readonly HorizontalLayoutGroup _layout;
            private readonly Image _icon;
            private readonly LayoutElement _iconLayout;
            private readonly Text _text;
            private readonly GameObject _segmentHolder;
            private readonly HorizontalLayoutGroup _segmentLayout;
            private readonly List<SegmentView> _segments = new List<SegmentView>();
            private readonly GameObject _arrowHolder;
            private readonly LayoutElement _arrowLayout;
            private readonly Image _arrow;
            private float _scale = 1f;

            public RowView(Transform section, TextStyle style, int index)
            {
                _style = style;
                Root = CreateChild(section, "Line_" + index);
                _layout = Root.AddComponent<HorizontalLayoutGroup>();
                ConfigureStack(_layout, BaseSpacing);
                _rowLayout = Root.AddComponent<LayoutElement>();

                _icon = CreateIcon(Root.transform, out _iconLayout);
                _text = CreateText(Root.transform, style);

                _segmentHolder = CreateChild(Root.transform, "Segments");
                _segmentLayout = _segmentHolder.AddComponent<HorizontalLayoutGroup>();
                ConfigureStack(_segmentLayout, BaseSegmentSpacing);
                _segmentHolder.SetActive(false);

                // The holder takes part in the layout; the arrow inside it is free to rotate.
                _arrowHolder = CreateChild(Root.transform, "ArrowHolder");
                _arrowLayout = _arrowHolder.AddComponent<LayoutElement>();
                GameObject arrow = CreateChild(_arrowHolder.transform, "Arrow");
                RectTransform arrowRect = (RectTransform)arrow.transform;
                arrowRect.anchorMin = Vector2.zero;
                arrowRect.anchorMax = Vector2.one;
                arrowRect.offsetMin = Vector2.zero;
                arrowRect.offsetMax = Vector2.zero;
                _arrow = arrow.AddComponent<Image>();
                _arrow.preserveAspect = true;
                _arrow.raycastTarget = false;
                _arrowHolder.SetActive(false);
            }

            public GameObject Root { get; }

            public void SetScale(float scale)
            {
                _scale = scale;
                float rowHeight = BaseRowHeight * scale;
                _rowLayout.minHeight = rowHeight;
                _layout.spacing = BaseSpacing * scale;
                _segmentLayout.spacing = BaseSegmentSpacing * scale;

                float iconSize = BaseIconSize * scale;
                SetSize(_iconLayout, iconSize);
                SetSize(_arrowLayout, BaseArrowSize * scale);
                _text.fontSize = FontSize(scale);
                foreach (SegmentView segment in _segments)
                {
                    segment.SetScale(scale);
                }
            }

            public void Apply(HudLine line, Sprite arrowSprite)
            {
                // A line without an icon keeps the space, so the texts stay aligned.
                SetIcon(_icon, line.Icon);

                // An empty text is left out of the layout, so segments start where the text would.
                string text = line.Text ?? string.Empty;
                SetText(_text, text);
                SetActive(_text.gameObject, text.Length > 0);

                ApplySegments(line.Segments);

                bool showArrow = line.ArrowAngle.HasValue && arrowSprite != null;
                SetActive(_arrowHolder, showArrow);
                if (showArrow)
                {
                    if (_arrow.sprite != arrowSprite)
                    {
                        _arrow.sprite = arrowSprite;
                    }

                    // The sprite points down; rotating it counter-clockwise by (180 - angle) points it at the angle.
                    _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f - line.ArrowAngle.Value);
                }
            }

            private void ApplySegments(List<HudSegment> segments)
            {
                while (_segments.Count < segments.Count)
                {
                    SegmentView view = new SegmentView(_segmentHolder.transform, _style, _segments.Count);
                    view.SetScale(_scale);
                    _segments.Add(view);
                }

                while (_segments.Count > segments.Count)
                {
                    SegmentView last = _segments[_segments.Count - 1];
                    _segments.RemoveAt(_segments.Count - 1);
                    UnityEngine.Object.DestroyImmediate(last.Root);
                }

                for (int i = 0; i < segments.Count; i++)
                {
                    _segments[i].Apply(segments[i]);
                }

                SetActive(_segmentHolder, segments.Count > 0);
            }
        }

        private sealed class SegmentView
        {
            private readonly HorizontalLayoutGroup _layout;
            private readonly Image _icon;
            private readonly LayoutElement _iconLayout;
            private readonly Text _text;

            public SegmentView(Transform holder, TextStyle style, int index)
            {
                Root = CreateChild(holder, "Segment_" + index);
                _layout = Root.AddComponent<HorizontalLayoutGroup>();
                ConfigureStack(_layout, BaseSegmentInnerSpacing);
                _icon = CreateIcon(Root.transform, out _iconLayout);
                _text = CreateText(Root.transform, style);
            }

            public GameObject Root { get; }

            public void SetScale(float scale)
            {
                _layout.spacing = BaseSegmentInnerSpacing * scale;
                SetSize(_iconLayout, BaseSegmentIconSize * scale);
                _text.fontSize = FontSize(scale);
            }

            public void Apply(HudSegment segment)
            {
                SetIcon(_icon, segment.Icon);
                SetText(_text, segment.Text);
            }
        }

        private static int FontSize(float scale)
        {
            return Mathf.Max(1, Mathf.RoundToInt(BaseFontSize * scale));
        }
    }
}
