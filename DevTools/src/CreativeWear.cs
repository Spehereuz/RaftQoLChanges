namespace QoLDevTools
{
    /// <summary>
    /// Creative makes tools indestructible through the game mode asset (areToolsIndestructible = true,
    /// toolDurabilityLossMultiplier = 0). The asset is shared by every world of the session, so the original
    /// values are restored on "off", when the world is left and when the mod is unloaded.
    /// </summary>
    internal static class CreativeWear
    {
        private const float NormalLossMultiplier = 1f;

        private static SO_GameModeValue _modifiedMode;
        private static bool _originalIndestructible;
        private static float _originalLossMultiplier;

        public static bool IsOn => _modifiedMode != null;

        public static string TurnOn()
        {
            SO_GameModeValue mode = GameModeValueManager.GetCurrentGameModeValue();
            if (mode == null || mode.toolVariables == null)
            {
                return "Game mode values are not available.";
            }

            if (mode == _modifiedMode)
            {
                return "Tool wear is already on.";
            }

            Restore();
            ToolVariables tools = mode.toolVariables;
            if (!tools.areToolsIndestructible && tools.toolDurabilityLossMultiplier > 0f)
            {
                return "Tools already wear out in this game mode, nothing changed.";
            }

            _modifiedMode = mode;
            _originalIndestructible = tools.areToolsIndestructible;
            _originalLossMultiplier = tools.toolDurabilityLossMultiplier;
            tools.areToolsIndestructible = false;
            tools.toolDurabilityLossMultiplier = NormalLossMultiplier;

            DevLog.Info("Tool wear on (" + mode.name + ").");
            return "Tool wear on: tools lose durability as in Normal mode until 'qoldev.wear off' or leaving the world.";
        }

        public static string TurnOff()
        {
            if (!IsOn)
            {
                return "Tool wear is already off.";
            }

            Restore();
            return "Tool wear off: game mode values restored.";
        }

        public static void Restore()
        {
            if (_modifiedMode == null)
            {
                return;
            }

            if (_modifiedMode.toolVariables != null)
            {
                _modifiedMode.toolVariables.areToolsIndestructible = _originalIndestructible;
                _modifiedMode.toolVariables.toolDurabilityLossMultiplier = _originalLossMultiplier;
            }

            DevLog.Info("Tool wear off (" + _modifiedMode.name + " restored).");
            _modifiedMode = null;
        }
    }
}
