using System;
using HarmonyLib;
using HMLLibrary;
using QoLChanges.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QoLChanges.UI
{
    /// <summary>
    /// Adds the "Mods" button to the in-game pause menu and drives the mods window:
    /// Mods list -> QoL Changes settings. Esc and the close button go one level back.
    /// </summary>
    internal sealed class ModMenu : MonoBehaviour
    {
        private const string ButtonLabel = "Mods";
        private const int PauseMenuSearchInterval = 30;
        private const int SortingOrderAbovePauseMenu = 10;

        private static readonly AccessTools.FieldRef<PauseMenu, bool> IgnoreCancelField =
            AccessTools.FieldRefAccess<PauseMenu, bool>("ignoreCancel");

        private static readonly AccessTools.FieldRef<PauseMenu, InputActionReference> InputCancelField =
            AccessTools.FieldRefAccess<PauseMenu, InputActionReference>("inputCancel");

        private Mod _mod;
        private QoLRuntime _runtime;

        private PauseMenu _pauseMenu;
        private PauseMenu _failedPauseMenu;
        private RaftUiTemplates _templates;
        private PauseMenuButton _button;
        private ModMenuWindow _window;

        private Page _page;
        private bool _rebuildRequested;
        private bool _scrollToTopOnRebuild;
        private int _closedFrame = -1;

        private enum Page
        {
            ModsList,
            QoLSettings,
        }

        public void Initialize(Mod mod, QoLRuntime runtime)
        {
            _mod = mod;
            _runtime = runtime;
            RaftUi.Initialize(transform);
        }

        public void Shutdown()
        {
            if (_window != null && _window.IsOpen)
            {
                CloseWindow(true);
            }

            DetachFromPauseMenu();
            RaftUi.Shutdown();
        }

        private void LateUpdate()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                ModLog.Error("Mods menu failed and was removed from this pause menu.", e);
                _failedPauseMenu = _pauseMenu;
                if (_window != null && _window.IsOpen)
                {
                    CloseWindow(true);
                }

                DetachFromPauseMenu();
            }
        }

        private void Tick()
        {
            if (_pauseMenu == null)
            {
                ResetSceneState();
                if (Time.frameCount % PauseMenuSearchInterval != 0)
                {
                    return;
                }

                _pauseMenu = FindObjectOfType<PauseMenu>();
                if (_pauseMenu == null)
                {
                    return;
                }
            }

            if (_pauseMenu == _failedPauseMenu)
            {
                return;
            }

            if (_button == null && _pauseMenu.buttonHolderPanel != null && _pauseMenu.buttonHolderPanel.activeInHierarchy)
            {
                AttachToPauseMenu();
            }

            if (_window != null && _window.IsOpen)
            {
                if (CanvasHelper.ActiveMenu != MenuType.PauseMenu)
                {
                    CloseWindow(false);
                }
                else if (CancelPressed())
                {
                    Back();
                }

                // Pages are rebuilt here, never inside a UI callback, because rebuilding destroys
                // the element that raised the callback.
                if (_rebuildRequested && _window.IsOpen)
                {
                    BuildPage();
                }
            }

            // PauseMenu resets the flag at the end of every Update, so it is raised every frame the
            // window is open and on the frame it closes, so the same Esc press does not close the pause menu.
            if ((_window != null && _window.IsOpen) || _closedFrame == Time.frameCount)
            {
                IgnoreCancelField(_pauseMenu) = true;
            }
        }

        private void AttachToPauseMenu()
        {
            if (!RaftUiTemplates.TryFind(_pauseMenu, out RaftUiTemplates templates, out string error))
            {
                ModLog.Warning("Mods button was not added, " + error + ".");
                _failedPauseMenu = _pauseMenu;
                return;
            }

            _templates = templates;
            RectTransform holder = (RectTransform)_pauseMenu.buttonHolderPanel.transform;
            _button = PauseMenuButton.Create(_templates, holder, ButtonLabel, OpenWindow);
        }

        private void DetachFromPauseMenu()
        {
            if (_button != null)
            {
                _button.Destroy();
                _button = null;
            }

            if (_window != null)
            {
                _window.Destroy();
                _window = null;
            }

            _templates = null;
        }

        private void ResetSceneState()
        {
            _button = null;
            _window = null;
            _templates = null;
            _rebuildRequested = false;
            _scrollToTopOnRebuild = false;
        }

        private void OpenWindow()
        {
            if (_window == null || !_window.IsAlive)
            {
                int sortingOrder = _templates.SettingsCanvas.sortingOrder + SortingOrderAbovePauseMenu;
                _window = ModMenuWindow.Create(_templates, sortingOrder, Back);
            }

            _pauseMenu.buttonHolderPanel.SetActive(false);
            _window.Show(ModsListPage.Title);
            ShowPage(Page.ModsList);
            BuildPage();
        }

        private void CloseWindow(bool restorePauseMenu)
        {
            if (_window != null)
            {
                _window.Hide();
                _window.ClearContent();
            }

            _rebuildRequested = false;
            _scrollToTopOnRebuild = false;
            _closedFrame = Time.frameCount;

            if (restorePauseMenu && _pauseMenu != null && CanvasHelper.ActiveMenu == MenuType.PauseMenu)
            {
                _pauseMenu.buttonHolderPanel.SetActive(true);
            }
        }

        private void Back()
        {
            if (_page == Page.QoLSettings)
            {
                ShowPage(Page.ModsList);
                return;
            }

            CloseWindow(true);
        }

        private void ShowPage(Page page)
        {
            _page = page;
            _rebuildRequested = true;
            _scrollToTopOnRebuild = true;
        }

        private void RequestRebuild()
        {
            _rebuildRequested = true;
        }

        private void BuildPage()
        {
            bool scrollToTop = _scrollToTopOnRebuild;
            _rebuildRequested = false;
            _scrollToTopOnRebuild = false;
            _window.ClearContent();

            switch (_page)
            {
                case Page.QoLSettings:
                    _window.SetTitle(QoLSettingsPage.Title);
                    QoLSettingsPage.Build(_templates, _window.Content, _runtime, RequestRebuild);
                    break;
                default:
                    _window.SetTitle(ModsListPage.Title);
                    ModsListPage.Build(_templates, _window.Content, _mod, _runtime, () => ShowPage(Page.QoLSettings));
                    break;
            }

            if (scrollToTop)
            {
                _window.ScrollToTop();
            }
        }

        private bool CancelPressed()
        {
            InputActionReference cancel = InputCancelField(_pauseMenu);
            return cancel != null && cancel.action != null && cancel.action.WasPressedThisFrame();
        }
    }
}
