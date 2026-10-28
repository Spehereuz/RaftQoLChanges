using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>Clones native Raft UI elements and turns them into plain, mod-controlled widgets.</summary>
    internal static class RaftUi
    {
        public static readonly Color DisabledTextTint = new Color(1f, 1f, 1f, 0.45f);

        private static Transform _stash;

        /// <summary>True between <see cref="Initialize"/> and <see cref="Shutdown"/>: <see cref="Clone"/> can be used.</summary>
        public static bool IsInitialized => _stash != null;

        /// <summary>Inactive holder: clones are created inside it so that no game script wakes up before it is stripped.</summary>
        public static void Initialize(Transform owner)
        {
            if (_stash != null)
            {
                return;
            }

            GameObject stash = new GameObject("QoLChanges_UiStash");
            stash.SetActive(false);
            stash.transform.SetParent(owner, false);
            _stash = stash.transform;
        }

        public static void Shutdown()
        {
            if (_stash != null)
            {
                UnityEngine.Object.Destroy(_stash.gameObject);
                _stash = null;
            }
        }

        /// <summary>
        /// Clones <paramref name="template"/>, removes every game script except visual/sound helpers
        /// and drops all serialized UI event listeners. The clone is inactive-parented until placed.
        /// </summary>
        public static GameObject Clone(GameObject template, string name)
        {
            if (_stash == null)
            {
                throw new InvalidOperationException("RaftUi is not initialized.");
            }

            GameObject copy = UnityEngine.Object.Instantiate(template, _stash, false);
            copy.name = name;
            StripGameScripts(copy);
            ResetEvents(copy);
            return copy;
        }

        public static void Place(GameObject element, Transform parent)
        {
            element.transform.SetParent(parent, false);
            element.SetActive(true);
        }

        public static Text FindText(GameObject root, string childName)
        {
            Transform child = root.transform.Find(childName);
            if (child != null)
            {
                Text text = child.GetComponent<Text>();
                if (text != null)
                {
                    return text;
                }
            }

            return root.GetComponentInChildren<Text>(true);
        }

        public static void SetText(Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.supportRichText = true;
            text.text = value;
        }

        public static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        public static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        public static void HideChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void StripGameScripts(GameObject root)
        {
            Type gameScriptBase = typeof(PauseMenu);
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                Type type = behaviour.GetType();
                bool isGameScript = type.Assembly == gameScriptBase.Assembly;
                bool keep = behaviour is ButtonSound || behaviour is TextChangeButton || behaviour is UiMagnet;
                if ((isGameScript && !keep) || behaviour is EventTrigger)
                {
                    UnityEngine.Object.DestroyImmediate(behaviour);
                }
            }
        }

        private static void ResetEvents(GameObject root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                button.onClick = new Button.ButtonClickedEvent();
            }

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                toggle.onValueChanged = new Toggle.ToggleEvent();
            }

            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            {
                slider.onValueChanged = new Slider.SliderEvent();
            }

            foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
            {
                dropdown.onValueChanged = new Dropdown.DropdownEvent();
            }
        }
    }
}
