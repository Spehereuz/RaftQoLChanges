using System;
using System.IO;
using UnityEngine;

namespace QoLChanges.Core
{
    internal static class JsonConfigFile
    {
        /// <summary>
        /// Reads the config from <paramref name="path"/>, creating it with defaults when missing.
        /// Valid files are rewritten so that options added in newer versions appear in them.
        /// Invalid files are left untouched and defaults are used for the session.
        /// </summary>
        public static T LoadOrCreate<T>(string path) where T : class, IConfig, new()
        {
            T config = new T();

            if (!File.Exists(path))
            {
                config.Normalize();
                if (TrySave(path, config))
                {
                    ModLog.Info("Created default config: " + path);
                }

                return config;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
            }
            catch (Exception e)
            {
                ModLog.Error("Config file is invalid and was not changed, defaults are used: " + path, e);
                T defaults = new T();
                defaults.Normalize();
                return defaults;
            }

            config.Normalize();
            TrySave(path, config);
            return config;
        }

        public static bool TrySave<T>(string path, T config) where T : class, IConfig
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(config, true));
                return true;
            }
            catch (Exception e)
            {
                ModLog.Error("Failed to write config: " + path, e);
                return false;
            }
        }
    }
}
