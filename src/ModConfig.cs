using System;
using QoLChanges.Core;
using QoLChanges.Features.DurabilityIndicator;
using QoLChanges.Features.StackSize;
using QoLChanges.UI;

namespace QoLChanges
{
    /// <summary>Root of mods\ModData\qol-changes\config.json. One section per feature, plus the menu state.</summary>
    [Serializable]
    public sealed class ModConfig : IConfig
    {
        public const string FileName = "config.json";

        /// <summary>Master switch: when false every feature is off, their own settings are kept.</summary>
        public bool enabled = true;

        public StackSizeConfig stackSize = new StackSizeConfig();

        public DurabilityIndicatorConfig durabilityIndicator = new DurabilityIndicatorConfig();

        public MenuConfig menu = new MenuConfig();

        public void Normalize()
        {
            if (stackSize == null)
            {
                stackSize = new StackSizeConfig();
            }

            if (durabilityIndicator == null)
            {
                durabilityIndicator = new DurabilityIndicatorConfig();
            }

            if (menu == null)
            {
                menu = new MenuConfig();
            }

            stackSize.Normalize();
            durabilityIndicator.Normalize();
            menu.Normalize();
        }
    }
}
