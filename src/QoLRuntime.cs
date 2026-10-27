using System.IO;
using QoLChanges.Core;
using QoLChanges.Features.DurabilityIndicator;
using QoLChanges.Features.StackSize;

namespace QoLChanges
{
    /// <summary>Owns the config and the features; every change made from the menu goes through here and is saved.</summary>
    internal sealed class QoLRuntime
    {
        private readonly string _configPath;

        public QoLRuntime(string dataFolder)
        {
            _configPath = Path.Combine(dataFolder, ModConfig.FileName);
            Config = JsonConfigFile.LoadOrCreate<ModConfig>(_configPath);

            Features = new FeatureManager();
            Features.Register(new StackSizeFeature(Config.stackSize));
            Features.Register(new DurabilityIndicatorFeature(Config.durabilityIndicator));
        }

        public ModConfig Config { get; }

        public FeatureManager Features { get; }

        public bool ModEnabled => Config.enabled;

        public void Start()
        {
            Features.ApplyConfig(Config.enabled);
        }

        public void Shutdown()
        {
            Features.DisableAll();
        }

        public void SetModEnabled(bool enabled)
        {
            if (Config.enabled == enabled)
            {
                return;
            }

            Config.enabled = enabled;
            Features.ApplyConfig(enabled);
            Save();
        }

        public void SetFeatureEnabled(Feature feature, bool enabled)
        {
            if (feature.Config.enabled == enabled)
            {
                return;
            }

            feature.Config.enabled = enabled;
            if (Config.enabled)
            {
                if (enabled)
                {
                    feature.Enable();
                }
                else
                {
                    feature.Disable();
                }
            }

            Save();
        }

        /// <summary>Call after a feature option has been changed.</summary>
        public void ApplyFeatureSettings(Feature feature)
        {
            feature.Config.Normalize();
            feature.Reapply();
            Save();
        }

        private void Save()
        {
            JsonConfigFile.TrySave(_configPath, Config);
        }
    }
}
