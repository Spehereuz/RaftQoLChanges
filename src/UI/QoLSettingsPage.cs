using System;
using System.Collections.Generic;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>
    /// One header per feature (expand arrow, name, toggle) followed by its options when expanded.
    /// Changes, including the expanded state, apply and save immediately.
    /// </summary>
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
                List<FeatureSetting> settings = new List<FeatureSetting>(current.GetSettings());
                bool expandable = settings.Count > 0;
                bool expanded = expandable && runtime.IsFeatureExpanded(current);

                RaftUiRows.CreateFeatureHeaderRow(
                    templates,
                    content,
                    current.DisplayName,
                    current.Config.enabled,
                    modEnabled,
                    value =>
                    {
                        runtime.SetFeatureEnabled(current, value);
                        refresh();
                    },
                    expandable,
                    expanded,
                    () =>
                    {
                        runtime.SetFeatureExpanded(current, !expanded);
                        refresh();
                    });

                if (!expanded)
                {
                    continue;
                }

                // Options of a disabled feature stay visible but cannot be changed.
                bool optionsInteractable = modEnabled && current.Config.enabled;
                foreach (FeatureSetting setting in settings)
                {
                    AddSetting(templates, content, runtime, current, setting, optionsInteractable && setting.IsAvailable, refresh);
                }
            }
        }

        private static void AddSetting(
            RaftUiTemplates templates,
            Transform content,
            QoLRuntime runtime,
            Feature feature,
            FeatureSetting setting,
            bool interactable,
            Action refresh)
        {
            // The page is rebuilt on the next frame, never inside the callback of the row being changed.
            Action apply = () =>
            {
                runtime.ApplyFeatureSettings(feature);
                if (setting.RebuildsPage)
                {
                    refresh();
                }
            };

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
                        apply();
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
                        apply();
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
                        apply();
                    });
                return;
            }

            ModLog.Warning("Unsupported setting type " + setting.GetType().Name + " in " + feature.Id + ".");
        }
    }
}
