using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>
    /// Native UI pieces of the current game scene used as templates, so that everything the mod shows
    /// looks exactly like Raft's own menus.
    /// </summary>
    internal sealed class RaftUiTemplates
    {
        private static readonly AccessTools.FieldRef<PauseMenu, Settings> PauseSettingsField =
            AccessTools.FieldRefAccess<PauseMenu, Settings>("settings");

        private static readonly AccessTools.FieldRef<PauseMenu, UiMagnet> PauseSettingsMagnetField =
            AccessTools.FieldRefAccess<PauseMenu, UiMagnet>("settingsMagnet");

        private static readonly AccessTools.FieldRef<Settings, GameObject> OptionsCanvasField =
            AccessTools.FieldRefAccess<Settings, GameObject>("optionsCanvas");

        private static readonly AccessTools.FieldRef<GeneralSettingsBox, Toggle> QuickBuildToggleField =
            AccessTools.FieldRefAccess<GeneralSettingsBox, Toggle>("toggle_QuickBuild");

        private static readonly AccessTools.FieldRef<GeneralSettingsBox, Dropdown> LanguageDropdownField =
            AccessTools.FieldRefAccess<GeneralSettingsBox, Dropdown>("dropdown_language");

        private RaftUiTemplates()
        {
        }

        /// <summary>"Settings" button of the pause menu.</summary>
        public GameObject PauseButton { get; private set; }

        /// <summary>Canvas of the settings window; its Canvas/CanvasScaler setup is copied.</summary>
        public Canvas SettingsCanvas { get; private set; }

        /// <summary>Settings window panel: background, title, close button, divider and tab content.</summary>
        public GameObject SettingsPanel { get; private set; }

        /// <summary>Scroll view of the "General" tab, the direct child of the tab content holder.</summary>
        public GameObject SettingsScrollView { get; private set; }

        /// <summary>"Quick build" row: label + toggle.</summary>
        public GameObject ToggleRow { get; private set; }

        /// <summary>"Master volume" row: label + slider + value text.</summary>
        public GameObject SliderRow { get; private set; }

        /// <summary>"Language" row: label + dropdown.</summary>
        public GameObject DropdownRow { get; private set; }

        public static bool TryFind(PauseMenu pauseMenu, out RaftUiTemplates templates, out string error)
        {
            templates = null;
            error = null;

            UiMagnet settingsMagnet = PauseSettingsMagnetField(pauseMenu);
            if (settingsMagnet == null)
            {
                error = "pause menu Settings button not found";
                return false;
            }

            Settings settings = PauseSettingsField(pauseMenu);
            if (settings == null)
            {
                error = "Settings component not found";
                return false;
            }

            GameObject optionsCanvas = OptionsCanvasField(settings);
            Canvas canvas = optionsCanvas != null ? optionsCanvas.GetComponent<Canvas>() : null;
            if (canvas == null)
            {
                error = "settings canvas not found";
                return false;
            }

            GeneralSettingsBox general = optionsCanvas.GetComponentInChildren<GeneralSettingsBox>(true);
            Toggle quickBuild = general != null ? QuickBuildToggleField(general) : null;
            if (quickBuild == null || general.transform.parent == null || general.transform.parent.parent == null)
            {
                error = "General settings tab not found";
                return false;
            }

            UISlider slider = settings.audioBox != null ? settings.audioBox.GetComponentInChildren<UISlider>(true) : null;
            if (slider == null)
            {
                error = "audio slider row not found";
                return false;
            }

            Dropdown language = LanguageDropdownField(general);
            if (language == null || language.transform.parent == null)
            {
                error = "language dropdown row not found";
                return false;
            }

            templates = new RaftUiTemplates
            {
                PauseButton = settingsMagnet.gameObject,
                SettingsCanvas = canvas,
                SettingsPanel = general.transform.parent.parent.gameObject,
                SettingsScrollView = general.gameObject,
                ToggleRow = quickBuild.transform.parent.gameObject,
                SliderRow = slider.gameObject,
                DropdownRow = language.transform.parent.gameObject,
            };
            return true;
        }
    }
}
