using System;
using QoLChanges.Core;
using QoLChanges.Features.StackSize;

namespace QoLChanges
{
    /// <summary>Root of mods\ModData\qol-changes\config.json. One section per feature.</summary>
    [Serializable]
    public sealed class ModConfig : IConfig
    {
        public const string FileName = "config.json";

        public StackSizeConfig stackSize = new StackSizeConfig();

        public void Normalize()
        {
            if (stackSize == null)
            {
                stackSize = new StackSizeConfig();
            }

            stackSize.Normalize();
        }
    }
}
