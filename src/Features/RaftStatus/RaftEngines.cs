using System.Collections.Generic;
using UnityEngine;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>What the running engines do with the raft.</summary>
    internal enum EngineDrive
    {
        /// <summary>No engine is running.</summary>
        Stopped,

        /// <summary>The engines push the raft in <see cref="EngineSummary.Thrust"/>.</summary>
        Pushing,

        /// <summary>An anchor holds the raft: the engines strain and burn no fuel.</summary>
        HeldByAnchor,

        /// <summary>The engines push in opposite directions and cancel each other out.</summary>
        NoThrust,
    }

    /// <summary>State of all engines (motor wheels) on the raft at one moment.</summary>
    internal struct EngineSummary
    {
        public int Total;

        /// <summary>Switched on with fuel in the tank: the engine pushes the raft.</summary>
        public int Running;

        /// <summary>Switched on, but the tank is empty.</summary>
        public int NoFuel;

        /// <summary>Sum of the push directions of the running engines, horizontal, in world space.</summary>
        public Vector3 Thrust;
    }

    /// <summary>
    /// Reads the engines the game keeps in <see cref="RaftVelocityManager.Motors"/>. Their state is read from the
    /// fields behind <c>MotorWheel.MotorState</c> and <c>PushDirection</c>: the getter of MotorState updates the engine
    /// and notifies other code when it changes, so it is not called from here.
    /// </summary>
    internal static class RaftEngines
    {
        // One running engine pushes with a length of 1; less than this is no push at all.
        private const float MinThrust = 0.1f;

        public static EngineSummary Read()
        {
            EngineSummary summary = new EngineSummary();
            List<MotorWheel> motors = RaftVelocityManager.Motors;
            if (motors == null)
            {
                return summary;
            }

            foreach (MotorWheel motor in motors)
            {
                if (motor == null || motor.FuelTank == null)
                {
                    continue;
                }

                summary.Total++;
                if (!motor.engineSwitchOn)
                {
                    continue;
                }

                if (motor.FuelTank.CurrentTankAmount <= 0f)
                {
                    summary.NoFuel++;
                    continue;
                }

                summary.Running++;
                summary.Thrust += PushDirection(motor);
            }

            return summary;
        }

        public static EngineDrive GetDrive(EngineSummary engines, bool anchored)
        {
            if (engines.Running == 0)
            {
                return EngineDrive.Stopped;
            }

            if (anchored)
            {
                return EngineDrive.HeldByAnchor;
            }

            return engines.Thrust.sqrMagnitude < MinThrust * MinThrust ? EngineDrive.NoThrust : EngineDrive.Pushing;
        }

        /// <summary>Same as MotorWheel.PushDirection of a running engine.</summary>
        private static Vector3 PushDirection(MotorWheel motor)
        {
            Vector3 forward = motor.transform.forward;
            forward.y = 0f;
            return motor.rotatesForward ? -forward : forward;
        }
    }
}
