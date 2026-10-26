using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>
    /// "Mods" button inserted right below "Settings" in the pause menu. The button holder has a fixed
    /// height, so it is grown by one button and the free-floating decorations are kept in place.
    /// </summary>
    internal sealed class PauseMenuButton
    {
        private const string MainBackgroundName = "BrownBackground";

        private readonly GameObject _button;
        private readonly RectTransform _holder;
        private readonly Vector2 _originalHolderSize;
        private readonly List<FloatingElement> _floatingElements = new List<FloatingElement>();

        private PauseMenuButton(GameObject button, RectTransform holder, float extraHeight)
        {
            _button = button;
            _holder = holder;
            _originalHolderSize = holder.sizeDelta;

            for (int i = 0; i < holder.childCount; i++)
            {
                RectTransform child = holder.GetChild(i) as RectTransform;
                LayoutElement layoutElement = child != null ? child.GetComponent<LayoutElement>() : null;
                if (layoutElement == null || !layoutElement.ignoreLayout || child.name == MainBackgroundName)
                {
                    continue;
                }

                _floatingElements.Add(new FloatingElement(child));
                KeepInPlace(child, extraHeight);
            }

            holder.sizeDelta = _originalHolderSize + new Vector2(0f, extraHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(holder);
        }

        public static PauseMenuButton Create(RaftUiTemplates templates, RectTransform holder, string label, Action onClick)
        {
            RectTransform settingsButton = (RectTransform)templates.PauseButton.transform;
            if (settingsButton.parent != holder)
            {
                throw new InvalidOperationException("Settings button is not a child of the pause button holder.");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(holder);
            VerticalLayoutGroup layout = holder.GetComponent<VerticalLayoutGroup>();
            float extraHeight = settingsButton.rect.height + (layout != null ? layout.spacing : 0f);

            GameObject button = RaftUi.Clone(templates.PauseButton, "QoLChanges_ModsButton");
            RaftUi.SetText(RaftUi.FindText(button, "Text"), label);
            button.GetComponent<Button>().onClick.AddListener(() => onClick());

            RaftUi.Place(button, holder);
            button.transform.SetSiblingIndex(settingsButton.GetSiblingIndex() + 1);

            return new PauseMenuButton(button, holder, extraHeight);
        }

        public void Destroy()
        {
            if (_button != null)
            {
                UnityEngine.Object.DestroyImmediate(_button);
            }

            if (_holder == null)
            {
                return;
            }

            _holder.sizeDelta = _originalHolderSize;
            foreach (FloatingElement element in _floatingElements)
            {
                element.Restore();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_holder);
        }

        /// <summary>The holder grows symmetrically; keep decorations at the same distance from their edge.</summary>
        private static void KeepInPlace(RectTransform element, float extraHeight)
        {
            bool stretchedVertically = Mathf.Approximately(element.anchorMin.y, 0f) && Mathf.Approximately(element.anchorMax.y, 1f);
            if (stretchedVertically)
            {
                element.offsetMin += new Vector2(0f, extraHeight);
                return;
            }

            float halfShift = extraHeight * 0.5f;
            float direction = element.anchoredPosition.y < 0f ? -1f : 1f;
            element.anchoredPosition += new Vector2(0f, direction * halfShift);
        }

        private sealed class FloatingElement
        {
            private readonly RectTransform _rect;
            private readonly Vector2 _offsetMin;
            private readonly Vector2 _offsetMax;

            public FloatingElement(RectTransform rect)
            {
                _rect = rect;
                _offsetMin = rect.offsetMin;
                _offsetMax = rect.offsetMax;
            }

            public void Restore()
            {
                if (_rect == null)
                {
                    return;
                }

                _rect.offsetMin = _offsetMin;
                _rect.offsetMax = _offsetMax;
            }
        }
    }
}
