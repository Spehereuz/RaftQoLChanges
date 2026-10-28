using System;
using System.Collections.Generic;
using HarmonyLib;

namespace QoLChanges.Core
{
    /// <summary>
    /// Self-contained toggleable feature. Each feature owns a Harmony instance, so disabling it
    /// removes exactly its own patches and nothing else.
    /// </summary>
    internal abstract class Feature
    {
        private readonly Harmony _harmony;
        private Action _saveConfig;

        protected Feature(string id, string displayName, FeatureConfig config)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Feature id must not be empty.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _harmony = new Harmony(QoLChangesMod.HarmonyId + "." + id);
        }

        public string Id { get; }

        public string DisplayName { get; }

        public FeatureConfig Config { get; }

        public bool IsEnabled { get; private set; }

        public bool Enable()
        {
            if (IsEnabled)
            {
                return true;
            }

            if (!TryApply())
            {
                return false;
            }

            IsEnabled = true;
            ModLog.Info(Id + ": enabled.");
            return true;
        }

        public void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            RevertChanges();
            IsEnabled = false;
            ModLog.Info(Id + ": disabled.");
        }

        /// <summary>Re-applies an enabled feature so that changed settings take effect.</summary>
        public void Reapply()
        {
            if (!IsEnabled)
            {
                return;
            }

            RevertChanges();
            if (!TryApply())
            {
                IsEnabled = false;
                return;
            }

            ModLog.Info(Id + ": settings applied.");
        }

        /// <summary>Options shown in the settings menu, besides the enabled toggle.</summary>
        public virtual IEnumerable<FeatureSetting> GetSettings()
        {
            yield break;
        }

        /// <summary>Set by the owner of the config; <see cref="SaveConfig"/> calls it.</summary>
        public void SetConfigSaver(Action saveConfig)
        {
            _saveConfig = saveConfig;
        }

        /// <summary>Saves a change the feature made to its own config outside the settings menu.</summary>
        protected void SaveConfig()
        {
            Config.Normalize();
            if (_saveConfig != null)
            {
                _saveConfig();
            }
        }

        protected void Patch(Type patchClass)
        {
            _harmony.CreateClassProcessor(patchClass).Patch();
        }

        /// <summary>Applies the feature. On exception <see cref="OnDisable"/> is called to roll back.</summary>
        protected abstract void OnEnable();

        /// <summary>Reverts everything <see cref="OnEnable"/> changed. Must tolerate a partial enable.</summary>
        protected abstract void OnDisable();

        private bool TryApply()
        {
            try
            {
                OnEnable();
                return true;
            }
            catch (Exception e)
            {
                ModLog.Error(Id + ": failed to enable, changes were rolled back.", e);
                RevertChanges();
                return false;
            }
        }

        private void RevertChanges()
        {
            try
            {
                OnDisable();
            }
            catch (Exception e)
            {
                ModLog.Error(Id + ": error while disabling.", e);
            }
            finally
            {
                _harmony.UnpatchAll(_harmony.Id);
            }
        }
    }
}
