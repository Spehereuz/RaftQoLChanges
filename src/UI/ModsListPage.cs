using System;
using System.Collections.Generic;
using HMLLibrary;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>
    /// Every mod known to RaftModLoader with its state. Only QoL Changes can be switched off and
    /// configured from here; other mods are listed read-only.
    /// </summary>
    internal static class ModsListPage
    {
        public const string Title = "Mods";

        private const float ToggleSlotWidth = 90f;
        private const float StatusRightOffset = 30f;

        private static readonly Color VersionColor = new Color(0.78f, 0.72f, 0.6f);
        private static readonly Color RunningColor = new Color(0.55f, 0.85f, 0.4f);
        private static readonly Color IdleColor = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color ErrorColor = new Color(0.95f, 0.4f, 0.35f);
        private static readonly Color BusyColor = new Color(0.95f, 0.8f, 0.35f);

        public static void Build(RaftUiTemplates templates, Transform content, Mod ownMod, QoLRuntime runtime, Action openOwnSettings)
        {
            List<ModData> mods = CollectMods(ownMod);
            if (mods.Count == 0)
            {
                RaftUiRows.CreateLabelRow(templates, content, "No mods found.");
                return;
            }

            foreach (ModData mod in mods)
            {
                string label = FormatLabel(mod);
                if (IsOwnMod(mod, ownMod))
                {
                    Toggle toggle = RaftUiRows.CreateToggleRow(templates, content, label, runtime.ModEnabled, true, runtime.SetModEnabled);
                    RaftUiRows.CreateRowButton(templates, toggle.transform.parent, "Settings", ToggleSlotWidth, openOwnSettings);
                }
                else
                {
                    Text labelText = RaftUiRows.CreateLabelRow(templates, content, label);
                    DescribeState(mod, out string state, out Color color);
                    RaftUiRows.CreateRowStatus(labelText.transform.parent, labelText, state, color, StatusRightOffset);
                }
            }
        }

        private static List<ModData> CollectMods(Mod ownMod)
        {
            List<ModData> mods = new List<ModData>();
            if (ModManagerPage.modList != null)
            {
                foreach (ModData mod in ModManagerPage.modList)
                {
                    if (mod != null && mod.jsonmodinfo != null)
                    {
                        mods.Add(mod);
                    }
                }
            }

            mods.Sort((a, b) =>
            {
                bool aOwn = IsOwnMod(a, ownMod);
                bool bOwn = IsOwnMod(b, ownMod);
                if (aOwn != bOwn)
                {
                    return aOwn ? -1 : 1;
                }

                return string.Compare(a.jsonmodinfo.name, b.jsonmodinfo.name, StringComparison.OrdinalIgnoreCase);
            });
            return mods;
        }

        private static bool IsOwnMod(ModData mod, Mod ownMod)
        {
            return mod.modinfo != null && mod.modinfo.mainClass != null && mod.modinfo.mainClass == ownMod;
        }

        private static string FormatLabel(ModData mod)
        {
            string name = string.IsNullOrEmpty(mod.jsonmodinfo.name) ? "Unnamed mod" : mod.jsonmodinfo.name;
            if (string.IsNullOrEmpty(mod.jsonmodinfo.version))
            {
                return name;
            }

            return name + "  <color=#" + ColorUtility.ToHtmlStringRGB(VersionColor) + ">v" + mod.jsonmodinfo.version + "</color>";
        }

        private static void DescribeState(ModData mod, out string state, out Color color)
        {
            if (mod.modinfo == null)
            {
                state = "Not loaded";
                color = IdleColor;
                return;
            }

            switch (mod.modinfo.modState)
            {
                case ModInfo.ModStateEnum.running:
                    state = "Running";
                    color = RunningColor;
                    break;
                case ModInfo.ModStateEnum.errored:
                    state = "Error";
                    color = ErrorColor;
                    break;
                case ModInfo.ModStateEnum.compiling:
                    state = "Compiling";
                    color = BusyColor;
                    break;
                case ModInfo.ModStateEnum.needrestart:
                    state = "Restart required";
                    color = BusyColor;
                    break;
                default:
                    state = "Not loaded";
                    color = IdleColor;
                    break;
            }
        }
    }
}
