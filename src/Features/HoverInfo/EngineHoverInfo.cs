using QoLChanges.GameState;
using QoLChanges.UI;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Fuel of the engine (motor wheel) under the crosshair, wherever it is aimed at (body, tank, levers):
    /// "Fuel 72% · 36 min" while it burns fuel, "Fuel 72% · paused" while it runs but an anchor holds the raft or the
    /// raft is too heavy, "Fuel 72%" when it is switched off, "Fuel Empty". The same text as the fuel line of the
    /// Raft status HUD, for one engine instead of the emptiest one.
    /// </summary>
    internal sealed class EngineHoverInfo : IHoverInfoProvider
    {
        private readonly RaftAnchors _anchors = new RaftAnchors();

        public bool TryDescribe(RaycastInteractable target, out string text)
        {
            text = null;
            MotorWheel motor = HoverTargets.Find<MotorWheel>(target);
            if (motor == null)
            {
                return false;
            }

            if (motor.FuelTank != null)
            {
                text = "Fuel " + FuelText.Format(RaftEngines.ReadFuel(motor), GetFuelUse(motor));
            }

            return true;
        }

        /// <summary>Whether the engines burn fuel depends on the whole raft: its anchors, weight and running engines.</summary>
        private FuelUse GetFuelUse(MotorWheel motor)
        {
            if (!motor.engineSwitchOn)
            {
                return FuelUse.Off;
            }

            EngineSummary engines = RaftEngines.Read();
            EnginePower power = RaftEngines.GetPower(engines, RaftEngines.ReadWeight() ?? 0);
            EngineDrive drive = RaftEngines.GetDrive(engines, _anchors.Read().HoldsRaft, power);
            return RaftEngines.GetFuelUse(drive);
        }
    }
}
