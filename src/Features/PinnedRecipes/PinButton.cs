using System;
using QoLChanges.Core;
using QoLChanges.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// A button under the box of the recipe selected in the crafting menu that pins or unpins it: "Pin to HUD 1/3",
    /// "Unpin" or, while three recipes are pinned, an inactive "3/3 pinned". It is a copy of the craft button, made again
    /// for the crafting menu of every world.
    /// </summary>
    internal sealed class PinButton : MonoBehaviour
    {
        private const string HostName = "QoLChanges_PinButton";
        private const string ButtonName = "QoLChanges_PinRecipe";

        // Width of the cost list in the box; the box's brown background reaches about 8 px below its rect.
        private const float Width = 224f;
        private const float GapBelowBox = 14f;

        private static PinButton _instance;

        private SelectedRecipeBox _box;
        private Button _button;
        private Text _text;
        private bool _errorLogged;

        public static void Activate()
        {
            Deactivate();

            GameObject host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<PinButton>();
        }

        public static void Deactivate()
        {
            if (_instance != null)
            {
                DestroyImmediate(_instance.gameObject);
            }

            _instance = null;
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                DestroyImmediate(_button.gameObject);
            }

            _button = null;
            _text = null;
            _box = null;
        }

        private void Update()
        {
            try
            {
                if (EnsureButton() && _button.gameObject.activeInHierarchy)
                {
                    Refresh();
                }
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(PinnedRecipesFeature.FeatureId + ": failed to update the pin button.", e);
                }
            }
        }

        /// <summary>Creates the button in the crafting menu of the current world; false while there is none.</summary>
        private bool EnsureButton()
        {
            CraftingMenu menu = ComponentManager<CraftingMenu>.Value;
            SelectedRecipeBox box = menu != null ? menu.selectedRecipeBox : null;
            if (box == null || box.craftButton == null)
            {
                return false;
            }

            if (_button != null && _box == box)
            {
                return true;
            }

            // Clones are made through the mod's UI stash, which exists once the mod menu has started.
            if (!RaftUi.IsInitialized)
            {
                return false;
            }

            if (_button != null)
            {
                DestroyImmediate(_button.gameObject);
            }

            _box = box;
            GameObject copy = RaftUi.Clone(box.craftButton.gameObject, ButtonName);

            // The cursor magnet of the craft button belongs to the gamepad cursor of the crafting menu.
            foreach (UiMagnet magnet in copy.GetComponentsInChildren<UiMagnet>(true))
            {
                DestroyImmediate(magnet);
            }

            RectTransform template = (RectTransform)box.craftButton.transform;
            RectTransform rect = (RectTransform)copy.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -GapBelowBox);
            rect.sizeDelta = new Vector2(Width, template.rect.height);
            rect.localScale = Vector3.one;

            _text = RaftUi.FindText(copy, "Text");
            if (_text != null)
            {
                _text.resizeTextForBestFit = true;
                _text.resizeTextMinSize = 10;
                _text.resizeTextMaxSize = Mathf.Max(10, _text.fontSize);
            }

            _button = copy.GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            RaftUi.Place(copy, box.transform);
            return true;
        }

        private void Refresh()
        {
            Item_Base item = _box.ItemToCraft;
            bool pinned = PinnedRecipesService.IsPinned(item);
            bool interactable = item != null && (pinned || !PinnedRecipesService.IsFull);
            string label;
            if (pinned)
            {
                label = "Unpin";
            }
            else if (PinnedRecipesService.IsFull)
            {
                label = PinnedRecipesConfig.MaxPinned + "/" + PinnedRecipesConfig.MaxPinned + " pinned";
            }
            else
            {
                label = "Pin to HUD " + PinnedRecipesService.Count + "/" + PinnedRecipesConfig.MaxPinned;
            }

            if (_button.interactable != interactable)
            {
                _button.interactable = interactable;
            }

            if (_text != null && _text.text != label)
            {
                RaftUi.SetText(_text, label);
            }
        }

        private void OnClick()
        {
            Item_Base item = _box != null ? _box.ItemToCraft : null;
            if (item == null)
            {
                return;
            }

            if (!PinnedRecipesService.Unpin(item))
            {
                PinnedRecipesService.Pin(item);
            }

            // Otherwise the button stays selected and the submit key would toggle the pin again.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _button.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            Refresh();
        }
    }
}
