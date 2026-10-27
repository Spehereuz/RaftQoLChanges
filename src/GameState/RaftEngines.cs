using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QoLChanges.GameState
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

        /// <summary>The raft is too heavy for the running engines: they strain and burn no fuel.</summary>
        TooHeavy,

        /// <summary>The engines push in opposite directions and cancel each other out.</summary>
        NoThrust,
    }

    /// <summary>Strength of the running engines compared with the weight of the raft, as the game rates it.</summary>
    internal enum EnginePower
    {
        Enough,

        /// <summary>Slightly too heavy: the engines move the raft at half speed.</summary>
        Weak,

        /// <summary>The engines do not move the raft.</summary>
        TooHeavy,
    }

    /// <summary>Whether the fuel of an engine is being burnt.</summary>
    internal enum FuelUse
    {
        /// <summary>The engine is switched off.</summary>
        Off,

        /// <summary>The engine runs and burns fuel.</summary>
        Burning,

        /// <summary>The engine runs, but an anchor holds the raft or it is too heavy: no fuel is burnt.</summary>
        Paused,
    }

    /// <summary>Fuel in the tank of one engine.</summary>
    internal struct EngineFuel
    {
        public float Amount;
        public float Capacity;

        /// <summary>Seconds one unit of fuel lasts while the engine burns fuel.</summary>
        public float SecondsPerUnit;

        public float Fraction => Capacity > 0f ? Amount / Capacity : 0f;
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

        /// <summary>Foundations the running engines move at full speed.</summary>
        public int Strength;

        /// <summary>Foundations above <see cref="Strength"/> the running engines still move at half speed.</summary>
        public int ExtraStrength;

        /// <summary>Foundations one engine moves at full speed (the strongest engine on the raft).</summary>
        public int EngineStrength;

        /// <summary>The emptiest tank of the engines that are switched on; null when none is.</summary>
        public EngineFuel? LowestSwitchedOn;

        /// <summary>The emptiest tank of all engines.</summary>
        public EngineFuel? Lowest;
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

        private static readonly AccessTools.FieldRef<MotorWheel, int> StrengthField =
            AccessTools.FieldRefAccess<MotorWheel, int>("_motorStrenght");

        private static readonly AccessTools.FieldRef<MotorWheel, int> ExtraStrengthField =
            AccessTools.FieldRefAccess<MotorWheel, int>("_extraMotorStrength");

        private static readonly AccessTools.FieldRef<MotorWheel, float> TimePerFuelField =
            AccessTools.FieldRefAccess<MotorWheel, float>("timePerFuel");

        private static readonly AccessTools.FieldRef<RaftBounds> RaftBoundsField =
            AccessTools.StaticFieldRefAccess<RaftBounds>(AccessTools.Field(typeof(RaftWeightManager), "raftBounds"));

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
                summary.EngineStrength = Mathf.Max(summary.EngineStrength, StrengthField(motor));

                EngineFuel fuel = ReadFuel(motor);
                summary.Lowest = Emptier(summary.Lowest, fuel);
                if (!motor.engineSwitchOn)
                {
                    continue;
                }

                summary.LowestSwitchedOn = Emptier(summary.LowestSwitchedOn, fuel);

                if (motor.FuelTank.CurrentTankAmount <= 0f)
                {
                    summary.NoFuel++;
                    continue;
                }

                summary.Running++;
                summary.Thrust += PushDirection(motor);
                summary.Strength += StrengthField(motor);
                summary.ExtraStrength += ExtraStrengthField(motor);
            }

            return summary;
        }

        /// <summary>Weight of the raft the engines have to move, as the game counts it; null while the raft is not loaded.</summary>
        public static int? ReadWeight()
        {
            return RaftBoundsField() != null ? RaftWeightManager.FoundationWeight : (int?)null;
        }

        /// <summary>Same rating as RaftVelocityManager.MotorWheelWeightStrength, without the anchor.</summary>
        public static EnginePower GetPower(EngineSummary engines, int weight)
        {
            if (weight <= engines.Strength)
            {
                return EnginePower.Enough;
            }

            return weight <= engines.Strength + engines.ExtraStrength ? EnginePower.Weak : EnginePower.TooHeavy;
        }

        /// <summary>Engines that have to run to move the raft at full speed.</summary>
        public static int EnginesNeeded(EngineSummary engines, int weight)
        {
            return engines.EngineStrength > 0 ? Mathf.CeilToInt((float)weight / engines.EngineStrength) : 0;
        }

        public static EngineDrive GetDrive(EngineSummary engines, bool anchored, EnginePower power)
        {
            if (engines.Running == 0)
            {
                return EngineDrive.Stopped;
            }

            if (anchored)
            {
                return EngineDrive.HeldByAnchor;
            }

            if (power == EnginePower.TooHeavy)
            {
                return EngineDrive.TooHeavy;
            }

            return engines.Thrust.sqrMagnitude < MinThrust * MinThrust ? EngineDrive.NoThrust : EngineDrive.Pushing;
        }

        public static EngineFuel ReadFuel(MotorWheel motor)
        {
            return new EngineFuel
            {
                Amount = Mathf.Max(0f, motor.FuelTank.CurrentTankAmount),
                Capacity = motor.FuelTank.maxCapacity,
                SecondsPerUnit = TimePerFuelField(motor),
            };
        }

        private static EngineFuel Emptier(EngineFuel? current, EngineFuel candidate)
        {
            return current.HasValue && current.Value.Fraction <= candidate.Fraction ? current.Value : candidate;
        }

        /// <summary>Whether the engines that are switched on burn fuel while they do <paramref name="drive"/>.</summary>
        public static FuelUse GetFuelUse(EngineDrive drive)
        {
            return drive == EngineDrive.HeldByAnchor || drive == EngineDrive.TooHeavy ? FuelUse.Paused : FuelUse.Burning;
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
