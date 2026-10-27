using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    internal enum RowStyle
    {
        /// <summary>Dark background, label at the left edge: a feature or a mod.</summary>
        Header,

        /// <summary>No background, indented label: an option under a header.</summary>
        Option,
    }

    /// <summary>Settings-style rows built from the native "Quick build", "Master volume" and "Language" rows.</summary>
    internal static class RaftUiRows
    {
        // Header labels leave room for the expand arrow, options are indented below the header name.
        private const float ArrowSpace = 30f;
        private const float IndentWidth = 60f;

        // Right part of a header row that belongs to its toggle; the rest of the row expands/collapses.
        private const float ToggleSpace = 80f;

        public static Text CreateLabelRow(RaftUiTemplates templates, Transform parent, string label)
        {
            GameObject row = CreateRowBase(templates.ToggleRow, parent, "Row_" + label, RowStyle.Header, out Text text);
            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle != null)
            {
                UnityEngine.Object.DestroyImmediate(toggle.gameObject);
            }

            RaftUi.SetText(text, label);
            return text;
        }

        public static Toggle CreateToggleRow(
            RaftUiTemplates templates,
            Transform parent,
            string label,
            bool isOn,
            bool interactable,
            Action<bool> onChanged,
            RowStyle style = RowStyle.Header)
        {
            GameObject row = CreateRowBase(templates.ToggleRow, parent, "Row_" + label, style, out Text text);
            RaftUi.SetText(text, label);

            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
            toggle.onValueChanged.AddListener(value => onChanged(value));
            ApplyInteractableTint(text, interactable);
            DimWhenDisabled(toggle.gameObject, interactable);
            return toggle;
        }

        /// <summary>
        /// Feature row: expand arrow, name and enable toggle. Clicking the arrow or the name calls
        /// <paramref name="onExpandClicked"/>; features without options get no arrow but keep the alignment.
        /// </summary>
        public static Toggle CreateFeatureHeaderRow(
            RaftUiTemplates templates,
            Transform parent,
            string label,
            bool isOn,
            bool interactable,
            Action<bool> onChanged,
            bool expandable,
            bool expanded,
            Action onExpandClicked)
        {
            Toggle toggle = CreateToggleRow(templates, parent, label, isOn, interactable, onChanged, RowStyle.Header);
            Transform row = toggle.transform.parent;
            Text text = RaftUi.FindText(row.gameObject, "Text");

            RectTransform textRect = text.rectTransform;
            float textLeft = textRect.anchoredPosition.x - textRect.sizeDelta.x * textRect.pivot.x;
            Shift(text, ArrowSpace);
            if (!expandable)
            {
                return toggle;
            }

            text.raycastTarget = false;
            Button area = CreateClickArea(row, onExpandClicked);

            GameObject arrow = RaftUi.Clone(templates.DropdownArrow, "ExpandArrow");
            RectTransform arrowRect = (RectTransform)arrow.transform;
            arrowRect.anchorMin = new Vector2(0f, 0.5f);
            arrowRect.anchorMax = new Vector2(0f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(textLeft + ArrowSpace * 0.5f, 0f);

            // The dropdown arrow points down (expanded); turned a quarter counter-clockwise it points right.
            arrowRect.localRotation = expanded ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f);

            Image arrowImage = arrow.GetComponent<Image>();
            arrowImage.raycastTarget = false;
            area.targetGraphic = arrowImage;

            RaftUi.Place(arrow, row);
            arrow.transform.SetSiblingIndex(area.transform.GetSiblingIndex() + 1);
            return toggle;
        }

        public static Slider CreateIntSliderRow(
            RaftUiTemplates templates,
            Transform parent,
            string label,
            int min,
            int max,
            int value,
            Func<int, string> format,
            bool interactable,
            RowStyle style,
            Action<int> onChanged)
        {
            GameObject row = CreateRowBase(templates.SliderRow, parent, "Row_" + label, style, out Text text);
            RaftUi.SetText(text, label);

            Slider slider = row.GetComponentInChildren<Slider>(true);
            Text valueText = RaftUi.FindText(row, "Slider value text");

            slider.wholeNumbers = true;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.interactable = interactable;
            RaftUi.SetText(valueText, format(value));

            int lastValue = value;
            slider.onValueChanged.AddListener(raw =>
            {
                int current = Mathf.RoundToInt(raw);
                RaftUi.SetText(valueText, format(current));
                if (current != lastValue)
                {
                    lastValue = current;
                    onChanged(current);
                }
            });

            ApplyInteractableTint(text, interactable);
            ApplyInteractableTint(valueText, interactable);
            DimWhenDisabled(slider.gameObject, interactable);
            return slider;
        }

        /// <summary>Row with the native dropdown of the "Language" setting; the value is the option index.</summary>
        public static Dropdown CreateDropdownRow(
            RaftUiTemplates templates,
            Transform parent,
            string label,
            IEnumerable<string> options,
            int value,
            bool interactable,
            RowStyle style,
            Action<int> onChanged)
        {
            GameObject row = CreateRowBase(templates.DropdownRow, parent, "Row_" + label, style, out Text text);
            RaftUi.SetText(text, label);

            Dropdown dropdown = row.GetComponentInChildren<Dropdown>(true);
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(value);
            dropdown.RefreshShownValue();
            dropdown.interactable = interactable;

            int lastValue = value;
            dropdown.onValueChanged.AddListener(current =>
            {
                if (current != lastValue)
                {
                    lastValue = current;
                    onChanged(current);
                }
            });

            ApplyInteractableTint(text, interactable);
            DimWhenDisabled(dropdown.gameObject, interactable);
            return dropdown;
        }

        /// <summary>Button styled like the pause menu buttons, anchored to the right edge of a row.</summary>
        public static Button CreateRowButton(RaftUiTemplates templates, Transform row, string label, float rightOffset, Action onClick)
        {
            GameObject buttonObject = RaftUi.Clone(templates.PauseButton, "Button_" + label);
            foreach (LayoutElement layoutElement in buttonObject.GetComponents<LayoutElement>())
            {
                UnityEngine.Object.DestroyImmediate(layoutElement);
            }

            foreach (UiMagnet magnet in buttonObject.GetComponents<UiMagnet>())
            {
                UnityEngine.Object.DestroyImmediate(magnet);
            }

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightOffset, 0f);
            rect.sizeDelta = new Vector2(170f, 42f);

            Text text = RaftUi.FindText(buttonObject, "Text");
            if (text != null)
            {
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 26;
            }

            RaftUi.SetText(text, label);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            RaftUi.Place(buttonObject, row);
            return button;
        }

        /// <summary>Right-aligned secondary text inside a row (e.g. a mod state).</summary>
        public static Text CreateRowStatus(Transform row, Text labelTemplate, string value, Color color, float rightOffset)
        {
            GameObject statusObject = UnityEngine.Object.Instantiate(labelTemplate.gameObject, row, false);
            statusObject.name = "Status";

            RectTransform rect = (RectTransform)statusObject.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightOffset, 0f);
            rect.sizeDelta = new Vector2(260f, rect.sizeDelta.y);

            Text status = statusObject.GetComponent<Text>();
            status.alignment = TextAnchor.MiddleRight;
            status.color = color;
            RaftUi.SetText(status, value);
            return status;
        }

        private static GameObject CreateRowBase(GameObject template, Transform parent, string name, RowStyle style, out Text label)
        {
            GameObject row = RaftUi.Clone(template, name);
            RaftUi.HideChild(row.transform, "Selected");
            label = RaftUi.FindText(row, "Text");
            ApplyStyle(row, label, style);
            RaftUi.Place(row, parent);
            return row;
        }

        /// <summary>
        /// Raft's settings rows carry their own background image, enabled on some rows and disabled on
        /// others (the "MasterVolume" slider row has none), so it is set explicitly for every row.
        /// </summary>
        private static void ApplyStyle(GameObject row, Text label, RowStyle style)
        {
            Image background = row.GetComponent<Image>();
            if (background != null)
            {
                background.enabled = style == RowStyle.Header;
            }

            if (style == RowStyle.Option && label != null)
            {
                Indent(label);
            }
        }

        private static void Indent(Text text)
        {
            Shift(text, IndentWidth);
        }

        /// <summary>Moves the left edge of a left-anchored label, keeping its right edge.</summary>
        private static void Shift(Text text, float width)
        {
            RectTransform rect = text.rectTransform;
            rect.anchoredPosition += new Vector2(width * (1f - rect.pivot.x), 0f);
            rect.sizeDelta -= new Vector2(width, 0f);
        }

        /// <summary>Transparent button over the row, except the toggle at its right end.</summary>
        private static Button CreateClickArea(Transform row, Action onClick)
        {
            GameObject areaObject = new GameObject("ExpandArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            areaObject.layer = row.gameObject.layer;

            RectTransform rect = (RectTransform)areaObject.transform;
            rect.SetParent(row, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(-ToggleSpace, 0f);

            // Fully transparent images still receive raycasts.
            areaObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            Button button = areaObject.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// Selectable's colour tint only affects its target graphic (the handle, the checkbox background),
        /// so the track, fill and checkmark stay bright on a disabled control. A canvas group dims the whole
        /// control like the labels.
        /// </summary>
        private static void DimWhenDisabled(GameObject control, bool interactable)
        {
            if (interactable)
            {
                return;
            }

            CanvasGroup group = control.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = control.AddComponent<CanvasGroup>();
            }

            group.alpha = RaftUi.DisabledTextTint.a;
        }

        private static void ApplyInteractableTint(Text text, bool interactable)
        {
            if (text != null && !interactable)
            {
                text.color *= RaftUi.DisabledTextTint;
            }
        }
    }
}
