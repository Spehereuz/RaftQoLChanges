using System.IO;
using QoLChanges.Core;
using QoLChanges.Features.AutoReplace;
using QoLChanges.Features.DurabilityIndicator;
using QoLChanges.Features.FullNet;
using QoLChanges.Features.HoverInfo;
using QoLChanges.Features.PinnedRecipes;
using QoLChanges.Features.RaftStatus;
using QoLChanges.Features.ScarecrowRadius;
using QoLChanges.Features.StackSize;
using QoLChanges.Features.StorageSort;

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
            Register(new StackSizeFeature(Config.stackSize));
            Register(new DurabilityIndicatorFeature(Config.durabilityIndicator));
            Register(new HoverInfoFeature(Config.hoverInfo));
            Register(new AutoReplaceFeature(Config.autoReplace));
            Register(new RaftStatusFeature(Config.raftStatus));
            Register(new FullNetFeature(Config.fullNet));
            Register(new ScarecrowRadiusFeature(Config.scarecrowRadius));
            Register(new StorageSortFeature(Config.storageSort));
            Register(new PinnedRecipesFeature(Config.pinnedRecipes));
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

        public bool IsFeatureExpanded(Feature feature)
        {
            return Config.menu.expandedFeatures.Contains(feature.Id);
        }

        /// <summary>Remembers whether the feature's options are shown on the settings page.</summary>
        public void SetFeatureExpanded(Feature feature, bool expanded)
        {
            if (IsFeatureExpanded(feature) == expanded)
            {
                return;
            }

            if (expanded)
            {
                Config.menu.expandedFeatures.Add(feature.Id);
            }
            else
            {
                Config.menu.expandedFeatures.Remove(feature.Id);
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

        private void Register(Feature feature)
        {
            feature.SetConfigSaver(Save);
            Features.Register(feature);
        }

        private void Save()
        {
            JsonConfigFile.TrySave(_configPath, Config);
        }
    }
}
