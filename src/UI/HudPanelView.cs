using System;
using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>
    /// Draws the <see cref="HudPanel"/> sections inside the game HUD. The panel is a child of the stat bars' parent,
    /// so it is hidden together with the rest of the HUD; it is created again after a world is loaded. Text uses the
    /// font and shadow of the game's interaction prompt, arrows the sprite of the settings dropdowns.
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

        private static readonly AccessTools.FieldRef<DisplayTextManager, DisplayText[]> DisplayTextsField =
            AccessTools.FieldRefAccess<DisplayTextManager, DisplayText[]>("displayTexts");

        private static readonly AccessTools.FieldRef<DisplayText, Text> TextComponentField =
            AccessTools.FieldRefAccess<DisplayText, Text>("textComponent");

        private static readonly AccessTools.FieldRef<GeneralSettingsBox, Dropdown> LanguageDropdownField =
            AccessTools.FieldRefAccess<GeneralSettingsBox, Dropdown>("dropdown_language");

        private readonly Dictionary<HudSection, SectionView> _views = new Dictionary<HudSection, SectionView>();

        private CanvasHelper _canvas;
        private UISlider_Stat[] _statBars;
        private RectTransform _panel;
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
                Sync();
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

        /// <summary>The panel is removed with the HUD it lives in; this object only owns it while it exists.</summary>
        private void OnDestroy()
        {
            if (_panel != null)
            {
                DestroyImmediate(_panel.gameObject);
            }

            _panel = null;
            _views.Clear();
        }

        private void Sync()
        {
            IReadOnlyList<HudSection> sections = HudPanel.ActiveSections;
            HudSection bottom = null;
            foreach (HudSection section in sections)
            {
                if (IsShown(section))
                {
                    bottom = section;
                    break;
                }
            }

            if (bottom == null)
            {
                if (_panel != null && _panel.gameObject.activeSelf)
                {
                    _panel.gameObject.SetActive(false);
                }

                return;
            }

            if (!EnsurePanel())
            {
                return;
            }

            if (!_panel.gameObject.activeSelf)
            {
                _panel.gameObject.SetActive(true);
            }

            for (int i = 0; i < sections.Count; i++)
            {
                HudSection section = sections[i];
                SectionView view = GetView(section);
                bool shown = IsShown(section);
                view.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                // The layout runs top to bottom; the first section (lowest order) is the last child.
                int siblingIndex = sections.Count - 1 - i;
                if (view.Root.GetSiblingIndex() != siblingIndex)
                {
                    view.Root.SetSiblingIndex(siblingIndex);
                }

                view.Apply(section, _arrowSprite);
            }

            UpdatePosition(bottom.Scale);
        }

        private static bool IsShown(HudSection section)
        {
            return section.Visible && section.Lines.Count > 0;
        }

        private SectionView GetView(HudSection section)
        {
            if (!_views.TryGetValue(section, out SectionView view))
            {
                view = new SectionView(_panel, _textStyle, section.Name);
                _views[section] = view;
            }

            return view;
        }

        /// <summary>Creates the panel in the HUD of the current world; false while there is no world.</summary>
        private bool EnsurePanel()
        {
            CanvasHelper canvas = ComponentManager<CanvasHelper>.Value;
            if (canvas == null || canvas.StatSliderParent == null)
            {
                return false;
            }

            if (_panel != null && _canvas == canvas)
            {
                return true;
            }

            if (_panel != null)
            {
                DestroyImmediate(_panel.gameObject);
            }

            // Views of the previous world were destroyed together with its HUD.
            _views.Clear();
            _panel = null;
            _canvas = canvas;
            _statBars = new[] { canvas.healthSlider, canvas.hungerSlider, canvas.thirstSlider };

            if (!TryGetTextStyle(canvas, out _textStyle))
            {
                return false;
            }

            if (!_arrowSearched)
            {
                _arrowSearched = true;
                _arrowSprite = FindArrowSprite();
            }

            Transform parent = canvas.StatSliderParent.transform;
            GameObject panelObject = new GameObject(PanelName, typeof(RectTransform));
            panelObject.layer = parent.gameObject.layer;
            _panel = (RectTransform)panelObject.transform;
            _panel.SetParent(parent, false);
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;

            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            ConfigureStack(layout, SectionSpacing);

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return true;
        }

        /// <summary>Above the highest stat bar, icons centred on the stat icons.</summary>
        private void UpdatePosition(float scale)
        {
            Transform parent = _panel.parent;
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

            Vector3 current = _panel.localPosition;
            if (!Mathf.Approximately(current.x, target.x) || !Mathf.Approximately(current.y, target.y))
            {
                _panel.localPosition = new Vector3(target.x, target.y, 0f);
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
