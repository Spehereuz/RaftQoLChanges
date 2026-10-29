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
    /// dropdowns; panel sections are drawn with <see cref="HudSprites"/>.
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

        // Panel style, at scale 1.
        private const float PanelPaddingX = 14f;
        private const float PanelPaddingTitle = 6f;
        private const float PanelPaddingBottom = 12f;
        private const int TitleFontSize = 15;
        private const float TitleHeight = 22f;
        private const float DividerHeight = 12f;
        private const float DividerThickness = 2f;
        private const float IconInset = 3f;
        private const float SegmentIconInset = 2f;
        private const int PanelSegmentFontSize = 18;
        private const float ShadowOpacity = 0.35f;
        private const float ShadowSpread = 6f;
        private static readonly Vector2 ShadowOffset = new Vector2(3f, -4f);
        private static readonly Color TitleColor = new Color(0.94f, 0.88f, 0.76f);

        // Health, hunger and thirst bars start with a 43 px wide icon background; HUD icons are centred on it.
        private const float StatIconCenterX = 21.5f;
        private static readonly Vector2 FallbackPosition = new Vector2(16f, 190f);

        // As far from the top edge as from the right one; the game's notifications slide in over it for a few seconds.
        private static readonly Vector2 TopRightOffset = new Vector2(-HudPanel.CornerMargin, -HudPanel.CornerMargin);

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

        /// <summary>World corners of a section on screen (as <see cref="RectTransform.GetWorldCorners"/>); false while it is not shown.</summary>
        public bool TryGetWorldCorners(HudSection section, Vector3[] corners)
        {
            if (!_views.TryGetValue(section, out SectionView view) || view.Root == null || !view.Root.gameObject.activeInHierarchy)
            {
                return false;
            }

            ((RectTransform)view.Root).GetWorldCorners(corners);
            return true;
        }

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
            HudSprites.Release();
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
            float cornerGap = 0f;
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

                cornerGap = Mathf.Max(cornerGap, section.CornerGap);

                // The layout runs top to bottom: at the bottom the nearest section is the last child, at the top the first.
                int siblingIndex = corner == HudCorner.BottomLeft ? _cornerSections.Count - 1 - i : i;
                if (view.Root.GetSiblingIndex() != siblingIndex)
                {
                    view.Root.SetSiblingIndex(siblingIndex);
                }

                view.Apply(section, _arrowSprite);
            }

            SetCornerGap(panel, corner, cornerGap);
            if (corner == HudCorner.BottomLeft)
            {
                PlaceAboveBars(panel, nearest.Scale);
            }
        }

        /// <summary>Padding of the panel on the side of its corner pushes every section of it away from the corner.</summary>
        private static void SetCornerGap(RectTransform panel, HudCorner corner, float gap)
        {
            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            int pixels = Mathf.RoundToInt(gap);
            RectOffset padding = layout.padding;
            int current = corner == HudCorner.TopRight ? padding.top : padding.bottom;
            if (current == pixels)
            {
                return;
            }

            layout.padding = corner == HudCorner.TopRight
                ? new RectOffset(padding.left, padding.right, pixels, padding.bottom)
                : new RectOffset(padding.left, padding.right, padding.top, pixels);
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

        /// <summary>
        /// An item icon on an optional slot background. The holder takes part in the layout and shows the slot; the icon
        /// inside it is inset while the slot is shown.
        /// </summary>
        private sealed class SlotIcon
        {
            private readonly Image _slot;
            private readonly Image _icon;
            private readonly LayoutElement _layout;
            private bool _slotShown;
            private float _size = -1f;
            private float _inset = -1f;
            private float _opacity = -1f;

            public SlotIcon(Transform parent)
            {
                GameObject holder = CreateChild(parent, "Icon");
                _slot = holder.AddComponent<Image>();
                _slot.raycastTarget = false;
                _slot.enabled = false;
                _layout = holder.AddComponent<LayoutElement>();

                GameObject icon = CreateChild(holder.transform, "Image");
                RectTransform rect = (RectTransform)icon.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _icon = icon.AddComponent<Image>();
                _icon.preserveAspect = true;
                _icon.raycastTarget = false;
            }

            /// <summary><paramref name="opacity"/> above 0 shows the slot behind the icon.</summary>
            public void SetStyle(float size, float inset, float opacity)
            {
                _slotShown = opacity > 0f && HudSprites.Slot != null;
                if (!Mathf.Approximately(_size, size))
                {
                    _size = size;
                    SetSize(_layout, size);
                }

                float currentInset = _slotShown ? inset : 0f;
                if (!Mathf.Approximately(_inset, currentInset))
                {
                    _inset = currentInset;
                    _icon.rectTransform.offsetMin = new Vector2(currentInset, currentInset);
                    _icon.rectTransform.offsetMax = new Vector2(-currentInset, -currentInset);
                }

                if (_slotShown && !Mathf.Approximately(_opacity, opacity))
                {
                    _opacity = opacity;
                    _slot.sprite = HudSprites.Slot;
                    _slot.color = new Color(1f, 1f, 1f, opacity);
                }
            }

            public void Apply(Sprite sprite)
            {
                SetIcon(_icon, sprite);

                // A line without an icon keeps the space but shows no empty slot.
                bool showSlot = _slotShown && sprite != null;
                if (_slot.enabled != showSlot)
                {
                    _slot.enabled = showSlot;
                }
            }
        }

        private sealed class SectionView
        {
            private readonly List<RowView> _rows = new List<RowView>();
            private readonly TextStyle _style;
            private readonly CanvasGroup _group;
            private readonly Image _shadow;
            private readonly Image _background;
            private readonly Transform _body;
            private readonly VerticalLayoutGroup _bodyLayout;
            private readonly Text _title;
            private readonly LayoutElement _titleLayout;
            private float _scale = -1f;
            private float _opacity = -1f;
            private bool _hasTitle;

            public SectionView(Transform panel, TextStyle style, string name)
            {
                _style = style;
                GameObject root = CreateChild(panel, "Section_" + name);
                ConfigureStack(root.AddComponent<VerticalLayoutGroup>(), 0f);
                Root = root.transform;
                _group = root.AddComponent<CanvasGroup>();
                _group.interactable = false;
                _group.blocksRaycasts = false;

                // Behind the body, stretched over the section and shifted down to the right; not part of the layout.
                GameObject shadow = CreateChild(Root, "Shadow");
                shadow.AddComponent<LayoutElement>().ignoreLayout = true;
                RectTransform shadowRect = (RectTransform)shadow.transform;
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                _shadow = shadow.AddComponent<Image>();
                _shadow.raycastTarget = false;
                _shadow.type = Image.Type.Sliced;
                _shadow.enabled = false;

                GameObject body = CreateChild(Root, "Body");
                _background = body.AddComponent<Image>();
                _background.raycastTarget = false;
                _background.type = Image.Type.Sliced;
                _background.enabled = false;
                _bodyLayout = body.AddComponent<VerticalLayoutGroup>();
                ConfigureStack(_bodyLayout, 0f);
                _body = body.transform;

                _title = CreateText(_body, style);
                _title.gameObject.name = "Title";
                _title.color = TitleColor;
                _titleLayout = _title.gameObject.AddComponent<LayoutElement>();
                _title.gameObject.SetActive(false);
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
                bool restyle = false;
                while (_rows.Count < lines.Count)
                {
                    _rows.Add(new RowView(_body, _style, _rows.Count));
                    restyle = true;
                }

                while (_rows.Count > lines.Count)
                {
                    RowView last = _rows[_rows.Count - 1];
                    _rows.RemoveAt(_rows.Count - 1);
                    last.Destroy();
                }

                // A panel without background is drawn like plain text, apart from its title.
                bool isPanel = section.Style == HudSectionStyle.Panel;
                float opacity = isPanel ? section.BackgroundOpacity : 0f;
                string title = isPanel ? section.Title : null;
                bool hasTitle = !string.IsNullOrEmpty(title);
                if (restyle || hasTitle != _hasTitle || !Mathf.Approximately(_scale, section.Scale) || !Mathf.Approximately(_opacity, opacity))
                {
                    _scale = section.Scale;
                    _opacity = opacity;
                    _hasTitle = hasTitle;
                    SetStyle();
                }

                if (!Mathf.Approximately(_group.alpha, section.Alpha))
                {
                    _group.alpha = section.Alpha;
                }

                SetText(_title, title);
                for (int i = 0; i < lines.Count; i++)
                {
                    HudLine line = lines[i];
                    _rows[i].Apply(line, arrowSprite, opacity > 0f && line.StartsGroup && i > 0);
                }
            }

            public void Destroy()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root.gameObject);
                }
            }

            private void SetStyle()
            {
                float scale = _scale;
                bool background = _opacity > 0f;
                HudPanelView.SetActive(_title.gameObject, _hasTitle);
                _title.fontSize = Mathf.Max(1, Mathf.RoundToInt(TitleFontSize * scale));
                _titleLayout.minHeight = TitleHeight * scale;

                _background.enabled = background;
                _shadow.enabled = background;
                if (background)
                {
                    // Borders of the sliced sprites grow with the HUD size.
                    _background.sprite = HudSprites.Panel;
                    _background.pixelsPerUnitMultiplier = 1f / scale;
                    _background.color = new Color(1f, 1f, 1f, _opacity);
                    _shadow.sprite = HudSprites.Shadow;
                    _shadow.pixelsPerUnitMultiplier = 1f / scale;
                    _shadow.color = new Color(1f, 1f, 1f, _opacity * ShadowOpacity);
                    RectTransform shadowRect = _shadow.rectTransform;
                    shadowRect.offsetMin = (ShadowOffset - new Vector2(ShadowSpread, ShadowSpread)) * scale;
                    shadowRect.offsetMax = (ShadowOffset + new Vector2(ShadowSpread, ShadowSpread)) * scale;
                }

                int sides = background ? Mathf.RoundToInt(PanelPaddingX * scale) : 0;
                int top = background ? Mathf.RoundToInt((_hasTitle ? PanelPaddingTitle : PanelPaddingBottom) * scale) : 0;
                int bottom = background ? Mathf.RoundToInt(PanelPaddingBottom * scale) : 0;
                RectOffset padding = _bodyLayout.padding;
                if (padding.left != sides || padding.right != sides || padding.top != top || padding.bottom != bottom)
                {
                    _bodyLayout.padding = new RectOffset(sides, sides, top, bottom);
                }

                foreach (RowView row in _rows)
                {
                    row.SetStyle(scale, _opacity);
                }
            }
        }

        private sealed class RowView
        {
            private readonly TextStyle _style;
            private readonly GameObject _divider;
            private readonly LayoutElement _dividerLayout;
            private readonly Image _dividerLine;
            private readonly LayoutElement _rowLayout;
            private readonly HorizontalLayoutGroup _layout;
            private readonly SlotIcon _icon;
            private readonly Text _text;
            private readonly GameObject _segmentHolder;
            private readonly HorizontalLayoutGroup _segmentLayout;
            private readonly List<SegmentView> _segments = new List<SegmentView>();
            private readonly GameObject _arrowHolder;
            private readonly LayoutElement _arrowLayout;
            private readonly Image _arrow;
            private float _scale = 1f;
            private float _opacity;

            public RowView(Transform section, TextStyle style, int index)
            {
                _style = style;

                // Above the line, across the whole section.
                _divider = CreateChild(section, "Divider_" + index);
                _dividerLayout = _divider.AddComponent<LayoutElement>();
                _dividerLayout.flexibleWidth = 1f;
                GameObject line = CreateChild(_divider.transform, "Line");
                RectTransform lineRect = (RectTransform)line.transform;
                lineRect.anchorMin = new Vector2(0f, 0.5f);
                lineRect.anchorMax = new Vector2(1f, 0.5f);
                _dividerLine = line.AddComponent<Image>();
                _dividerLine.raycastTarget = false;
                _divider.SetActive(false);

                Root = CreateChild(section, "Line_" + index);
                _layout = Root.AddComponent<HorizontalLayoutGroup>();
                ConfigureStack(_layout, BaseSpacing);
                _rowLayout = Root.AddComponent<LayoutElement>();

                _icon = new SlotIcon(Root.transform);
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

            /// <summary><paramref name="opacity"/> above 0 draws slots behind the icons and allows dividers.</summary>
            public void SetStyle(float scale, float opacity)
            {
                _scale = scale;
                _opacity = opacity;
                _rowLayout.minHeight = BaseRowHeight * scale;
                _layout.spacing = BaseSpacing * scale;
                _segmentLayout.spacing = BaseSegmentSpacing * scale;

                _icon.SetStyle(BaseIconSize * scale, IconInset * scale, opacity);
                SetSize(_arrowLayout, BaseArrowSize * scale);
                _text.fontSize = FontSize(BaseFontSize, scale);

                _dividerLayout.minHeight = DividerHeight * scale;
                _dividerLayout.preferredHeight = DividerHeight * scale;
                _dividerLine.rectTransform.sizeDelta = new Vector2(0f, DividerThickness * scale);
                Sprite dividerSprite = opacity > 0f ? HudSprites.Divider : null;
                _dividerLine.sprite = dividerSprite;
                _dividerLine.type = Image.Type.Sliced;
                _dividerLine.color = dividerSprite != null
                    ? new Color(1f, 1f, 1f, opacity)
                    : new Color(TitleColor.r, TitleColor.g, TitleColor.b, 0.5f * opacity);

                foreach (SegmentView segment in _segments)
                {
                    segment.SetStyle(scale, opacity);
                }
            }

            public void Apply(HudLine line, Sprite arrowSprite, bool showDivider)
            {
                SetActive(_divider, showDivider);
                _icon.Apply(line.Icon);

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

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(_divider);
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private void ApplySegments(List<HudSegment> segments)
            {
                while (_segments.Count < segments.Count)
                {
                    SegmentView view = new SegmentView(_segmentHolder.transform, _style, _segments.Count);
                    view.SetStyle(_scale, _opacity);
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
            private readonly SlotIcon _icon;
            private readonly Text _text;

            public SegmentView(Transform holder, TextStyle style, int index)
            {
                Root = CreateChild(holder, "Segment_" + index);
                _layout = Root.AddComponent<HorizontalLayoutGroup>();
                ConfigureStack(_layout, BaseSegmentInnerSpacing);
                _icon = new SlotIcon(Root.transform);
                _text = CreateText(Root.transform, style);
            }

            public GameObject Root { get; }

            /// <summary>On a panel the text is smaller and the icon sits on a slot.</summary>
            public void SetStyle(float scale, float opacity)
            {
                _layout.spacing = BaseSegmentInnerSpacing * scale;
                _icon.SetStyle(BaseSegmentIconSize * scale, SegmentIconInset * scale, opacity);
                _text.fontSize = FontSize(opacity > 0f ? PanelSegmentFontSize : BaseFontSize, scale);
            }

            public void Apply(HudSegment segment)
            {
                _icon.Apply(segment.Icon);
                SetText(_text, segment.Text);
            }
        }

        private static int FontSize(int size, float scale)
        {
            return Mathf.Max(1, Mathf.RoundToInt(size * scale));
        }
    }
}
