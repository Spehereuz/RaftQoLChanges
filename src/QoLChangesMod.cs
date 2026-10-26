using HarmonyLib;
using HMLLibrary;
using System.Reflection;

namespace QoLChanges
{
    public class QoLChangesMod : Mod
    {
        public const string HarmonyId = "com.vladyslavhardetskyi.qolchanges";

        private Harmony _harmony;

        public void Start()
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log("Loaded.");
        }

        public void OnModUnload()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(HarmonyId);
                _harmony = null;
            }

            Log("Unloaded.");
        }
    }
}
