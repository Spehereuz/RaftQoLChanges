using HMLLibrary;
using QoLChanges.Core;
using QoLChanges.UI;

namespace QoLChanges
{
    public class QoLChangesMod : Mod
    {
        public const string HarmonyId = "com.vladyslavhardetskyi.qolchanges";

        private QoLRuntime _runtime;
        private ModMenu _menu;

        public void Start()
        {
            _runtime = new QoLRuntime(DataFolder);
            _runtime.Start();

            _menu = gameObject.AddComponent<ModMenu>();
            _menu.Initialize(this, _runtime);

            ModLog.Info("Loaded.");
        }

        public void OnModUnload()
        {
            if (_menu != null)
            {
                _menu.Shutdown();
                Destroy(_menu);
                _menu = null;
            }

            if (_runtime != null)
            {
                _runtime.Shutdown();
                _runtime = null;
            }

            ModLog.Info("Unloaded.");
        }
    }
}
