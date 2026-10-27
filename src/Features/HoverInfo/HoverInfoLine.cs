using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// A line of text under the crosshair, mirrored from the game's interaction prompt above it
    /// (DisplayTextManager, "DisplayTextMiddle"): same font, size and shadow. It is a separate object, so the game
    /// hiding or replacing its own prompts every frame does not affect it. Text set during a frame stays visible
    /// for that frame only; the line hides itself in LateUpdate when nobody refreshed it.
    /// </summary>
    internal sealed class HoverInfoLine : MonoBehaviour
    {
        private const string LineName = "QoLChanges_HoverInfo";

        private static readonly AccessTools.FieldRef<DisplayTextManager, DisplayText[]> DisplayTextsField =
            AccessTools.FieldRefAccess<DisplayTextManager, DisplayText[]>("displayTexts");

        private static readonly AccessTools.FieldRef<DisplayText, Text> TextComponentField =
            AccessTools.FieldRefAccess<DisplayText, Text>("textComponent");

        private Transform _stash;
        private Text _text;
        private int _shownFrame = -1;

        /// <summary>Shows <paramref name="value"/> (rich text) for the current frame.</summary>
        public void Show(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Text text = GetOrCreateText();
            if (text == null)
            {
                return;
            }

            if (text.text != value)
            {
                text.text = value;
            }

            if (!text.gameObject.activeSelf)
            {
                text.gameObject.SetActive(true);
            }

            _shownFrame = Time.frameCount;
        }

        private void LateUpdate()
        {
            if (_text != null && _shownFrame != Time.frameCount && _text.gameObject.activeSelf)
            {
                _text.gameObject.SetActive(false);
            }
        }

        /// <summary>The line lives in the game's HUD, not under this object, so it is removed explicitly.</summary>
        private void OnDestroy()
        {
            if (_text != null)
            {
                // Immediate, so that a line created again in the same frame (settings reapplied) is the only one.
                DestroyImmediate(_text.gameObject);
                _text = null;
            }
        }

        /// <summary>The line belongs to the world's HUD and is created again after the next world is loaded.</summary>
        private Text GetOrCreateText()
        {
            if (_text != null)
            {
                return _text;
            }

            DisplayTextManager manager = ComponentManager<DisplayTextManager>.Value;
            if (manager == null)
            {
                return null;
            }

            DisplayText[] displayTexts = DisplayTextsField(manager);
            if (displayTexts == null || displayTexts.Length == 0 || displayTexts[0] == null)
            {
                return null;
            }

            Text prompt = TextComponentField(displayTexts[0]);
            if (prompt == null || prompt.transform.parent == null)
            {
                return null;
            }

            _text = CreateText(prompt);
            return _text;
        }

        private Text CreateText(Text prompt)
        {
            if (_stash == null)
            {
                // Clones are made inside an inactive object, so the game scripts on them never wake up.
                GameObject stash = new GameObject("Stash");
                stash.SetActive(false);
                stash.transform.SetParent(transform, false);
                _stash = stash.transform;
            }

            GameObject copy = Instantiate(prompt.gameObject, _stash, false);
            copy.name = LineName;

            // Key hint, gamepad icons and other children of the prompt.
            for (int i = copy.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(copy.transform.GetChild(i).gameObject);
            }

            // Keep Text, Shadow and ContentSizeFitter; drop DisplayText, Localize and anything else from the game.
            Type uiAssembly = typeof(Text);
            foreach (MonoBehaviour behaviour in copy.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Assembly != uiAssembly.Assembly)
                {
                    DestroyImmediate(behaviour);
                }
            }

            copy.SetActive(false);
            Transform promptTransform = prompt.transform;
            copy.transform.SetParent(promptTransform.parent, false);
            copy.transform.SetSiblingIndex(promptTransform.GetSiblingIndex() + 1);

            // The prompt hangs above the crosshair from its bottom edge; the line hangs below it, mirrored.
            RectTransform promptRect = (RectTransform)promptTransform;
            RectTransform rect = (RectTransform)copy.transform;
            rect.pivot = new Vector2(promptRect.pivot.x, 1f - promptRect.pivot.y);
            rect.anchoredPosition = new Vector2(promptRect.anchoredPosition.x, -promptRect.anchoredPosition.y);

            Text text = copy.GetComponent<Text>();
            text.supportRichText = true;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }
    }
}
