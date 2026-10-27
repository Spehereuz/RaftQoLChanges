using System;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>One toggle per feature followed by its options. Changes apply and save immediately.</summary>
    internal static class QoLSettingsPage
    {
        public const string Title = "QoL Changes";

        public static void Build(RaftUiTemplates templates, Transform content, QoLRuntime runtime, Action refresh)
        {
            bool modEnabled = runtime.ModEnabled;
            if (!modEnabled)
            {
                RaftUiRows.CreateLabelRow(templates, content, "<i>The mod is switched off in the Mods list. All features are inactive.</i>");
            }

            foreach (Feature feature in runtime.Features.Features)
            {
                Feature current = feature;
                RaftUiRows.CreateToggleRow(
                    templates,
                    content,
                    current.DisplayName,
                    current.Config.enabled,
                    modEnabled,
                    value =>
                    {
                        runtime.SetFeatureEnabled(current, value);
                        refresh();
                    });

                bool optionsInteractable = modEnabled && current.Config.enabled;
                foreach (FeatureSetting setting in current.GetSettings())
                {
                    AddSetting(templates, content, runtime, current, setting, optionsInteractable);
                }
            }
        }

        private static void AddSetting(
            RaftUiTemplates templates, Transform content, QoLRuntime runtime, Feature feature, FeatureSetting setting, bool interactable)
        {
            IntSliderSetting slider = setting as IntSliderSetting;
            if (slider != null)
            {
                RaftUiRows.CreateIntSliderRow(
                    templates,
                    content,
                    slider.Label,
                    slider.Min,
                    slider.Max,
                    slider.Value,
                    slider.Format,
                    interactable,
                    RowStyle.Option,
                    value =>
                    {
                        slider.Value = value;
                        runtime.ApplyFeatureSettings(feature);
                    });
                return;
            }

            ToggleSetting toggle = setting as ToggleSetting;
            if (toggle != null)
            {
                RaftUiRows.CreateToggleRow(
                    templates,
                    content,
                    toggle.Label,
                    toggle.Value,
                    interactable,
                    value =>
                    {
                        toggle.Value = value;
                        runtime.ApplyFeatureSettings(feature);
                    },
                    RowStyle.Option);
                return;
            }

            ChoiceSetting choice = setting as ChoiceSetting;
            if (choice != null)
            {
                RaftUiRows.CreateDropdownRow(
                    templates,
                    content,
                    choice.Label,
                    choice.Options,
                    choice.Value,
                    interactable,
                    RowStyle.Option,
                    value =>
                    {
                        choice.Value = value;
                        runtime.ApplyFeatureSettings(feature);
                    });
                return;
            }

            ModLog.Warning("Unsupported setting type " + setting.GetType().Name + " in " + feature.Id + ".");
        }
    }
}
