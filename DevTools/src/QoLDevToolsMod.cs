using HMLLibrary;

namespace QoLDevTools
{
    /// <summary>
    /// RML entry point. The console commands live in <see cref="DevCommands"/>; RML finds them by their
    /// ConsoleCommand attribute when the mod is loaded.
    /// </summary>
    public class QoLDevToolsMod : Mod
    {
        public void Start()
        {
            DevLog.Info("Loaded. Commands: qoldev.find, qoldev.give, qoldev.dur, qoldev.dur.all, qoldev.wear, qoldev.fuel.");
        }

        public void OnModUnload()
        {
            CreativeWear.Restore();
            DevLog.Info("Unloaded.");
        }

        public override void Event_ReturnToMainMenu()
        {
            CreativeWear.Restore();
        }
    }
}
