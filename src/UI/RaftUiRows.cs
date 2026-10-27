using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>Settings-style rows built from the native "Quick build", "Master volume" and "Language" rows.</summary>
    internal static class RaftUiRows
    {
        private const float IndentWidth = 40f;

        public static Text CreateLabelRow(RaftUiTemplates templates, Transform parent, string label)
        {
            GameObject row = CreateRowBase(templates.ToggleRow, parent, "Row_" + label, out Text text);
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
            bool indented = false)
        {
            GameObject row = CreateRowBase(templates.ToggleRow, parent, "Row_" + label, out Text text);
            RaftUi.SetText(text, label);
            if (indented)
            {
                Indent(text);
            }

            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
            toggle.onValueChanged.AddListener(value => onChanged(value));
            ApplyInteractableTint(text, interactable);
            DimWhenDisabled(toggle.gameObject, interactable);
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
            bool indented,
            Action<int> onChanged)
        {
            GameObject row = CreateRowBase(templates.SliderRow, parent, "Row_" + label, out Text text);
            RaftUi.SetText(text, label);
            if (indented)
            {
                Indent(text);
            }

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
            bool indented,
            Action<int> onChanged)
        {
            GameObject row = CreateRowBase(templates.DropdownRow, parent, "Row_" + label, out Text text);
            RaftUi.SetText(text, label);
            if (indented)
            {
                Indent(text);
            }

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

        private static GameObject CreateRowBase(GameObject template, Transform parent, string name, out Text label)
        {
            GameObject row = RaftUi.Clone(template, name);
            RaftUi.HideChild(row.transform, "Selected");
            label = RaftUi.FindText(row, "Text");
            RaftUi.Place(row, parent);
            return row;
        }

        private static void Indent(Text text)
        {
            RectTransform rect = text.rectTransform;
            rect.anchoredPosition += new Vector2(IndentWidth, 0f);
            rect.sizeDelta -= new Vector2(IndentWidth, 0f);
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
