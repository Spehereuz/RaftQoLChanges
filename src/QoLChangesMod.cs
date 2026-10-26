using System.IO;
using HMLLibrary;
using QoLChanges.Core;
using QoLChanges.Features.StackSize;

namespace QoLChanges
{
    public class QoLChangesMod : Mod
    {
        public const string HarmonyId = "com.vladyslavhardetskyi.qolchanges";

        private FeatureManager _features;

        public void Start()
        {
            ModConfig config = JsonConfigFile.LoadOrCreate<ModConfig>(Path.Combine(DataFolder, ModConfig.FileName));

            _features = new FeatureManager();
            _features.Register(new StackSizeFeature(config.stackSize));
            _features.ApplyConfig();

            ModLog.Info("Loaded.");
        }

        public void OnModUnload()
        {
            if (_features != null)
            {
                _features.DisableAll();
                _features = null;
            }

            ModLog.Info("Unloaded.");
        }
    }
}
