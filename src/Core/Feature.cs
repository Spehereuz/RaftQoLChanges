using System;
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

        protected Feature(string id, FeatureConfig config)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Feature id must not be empty.", nameof(id));
            }

            Id = id;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _harmony = new Harmony(QoLChangesMod.HarmonyId + "." + id);
        }

        public string Id { get; }

        public FeatureConfig Config { get; }

        public bool IsEnabled { get; private set; }

        public bool Enable()
        {
            if (IsEnabled)
            {
                return true;
            }

            try
            {
                OnEnable();
                IsEnabled = true;
                ModLog.Info(Id + ": enabled.");
                return true;
            }
            catch (Exception e)
            {
                ModLog.Error(Id + ": failed to enable, changes were rolled back.", e);
                RevertChanges();
                return false;
            }
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

        protected void Patch(Type patchClass)
        {
            _harmony.CreateClassProcessor(patchClass).Patch();
        }

        /// <summary>Applies the feature. On exception <see cref="OnDisable"/> is called to roll back.</summary>
        protected abstract void OnEnable();

        /// <summary>Reverts everything <see cref="OnEnable"/> changed. Must tolerate a partial enable.</summary>
        protected abstract void OnDisable();

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
