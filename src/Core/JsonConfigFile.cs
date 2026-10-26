using System;
using System.IO;
using Newtonsoft.Json;

namespace QoLChanges.Core
{
    /// <summary>
    /// Config persistence. Newtonsoft.Json (shipped with Raft) is used instead of UnityEngine.JsonUtility:
    /// RML loads mods from in-memory assemblies that Unity's native serializer does not know, so
    /// JsonUtility silently drops nested config sections.
    /// </summary>
    internal static class JsonConfigFile
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            NullValueHandling = NullValueHandling.Include,
        };

        /// <summary>
        /// Reads the config from <paramref name="path"/>, creating it with defaults when missing.
        /// Values missing from the file keep their defaults, and valid files are rewritten so that
        /// options added in newer versions appear in them. Invalid files are left untouched and
        /// defaults are used for the session.
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
                JsonConvert.PopulateObject(File.ReadAllText(path), config, SerializerSettings);
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
                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(config, SerializerSettings));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(temporaryPath, path);
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
