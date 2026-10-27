using System;
using System.Collections.Generic;
using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Cooking progress as the local player should see it. Only the host advances the timers of cooking slots
    /// (Block_CookingStand.Update) and of cooking pots and juicers (CookingTable.Update); other players keep the
    /// value they received when joining or when cooking started and only learn when it is done. On those
    /// players the timers are advanced here with the same rules as on the host, in the mod's own state: the game
    /// objects are never changed. The host simply reads the game's timers.
    /// </summary>
    internal static class CookingTimers
    {
        // Above this many tracked objects destroyed ones are dropped (a world was left, stands were removed).
        private const int PruneThreshold = 256;

        private static readonly AccessTools.FieldRef<CookingSlot, float> CookTimeMultiplierField =
            AccessTools.FieldRefAccess<CookingSlot, float>("cookTimeMultiplier");

        private static readonly Func<Block_CookingStand, bool> IsUsingEnergySource =
            AccessTools.MethodDelegate<Func<Block_CookingStand, bool>>(
                AccessTools.Method(typeof(Block_CookingStand), "IsUsingEnergySource"));

        private static readonly Func<CookingTable, bool> IsCooking =
            AccessTools.MethodDelegate<Func<CookingTable, bool>>(AccessTools.Method(typeof(CookingTable), "IsCooking"));

        private static readonly Dictionary<CookingSlot, Timer<Item_Base>> SlotTimers =
            new Dictionary<CookingSlot, Timer<Item_Base>>();

        private static readonly Dictionary<CookingTable, Timer<SO_CookingTable_Recipe>> TableTimers =
            new Dictionary<CookingTable, Timer<SO_CookingTable_Recipe>>();

        private static bool _active;
        private static bool _errorLogged;

        public static void Activate()
        {
            Clear();
            _errorLogged = false;
            _active = true;
        }

        public static void Deactivate()
        {
            _active = false;
            Clear();
        }

        /// <summary>Progress of the item on a busy, unfinished slot, 0-99%.</summary>
        public static int GetProgress(CookingSlot slot)
        {
            ItemInstance_Cookable cookable = slot.CurrentItem != null ? slot.CurrentItem.settings_cookable : null;
            if (cookable == null)
            {
                return 0;
            }

            // Same limit as CookingSlot.UpdateSlot.
            return Percent.OfProgress(GetSeconds(slot), cookable.CookingTime * CookTimeMultiplierField(slot));
        }

        /// <summary>Progress of the recipe of a cooking pot or juicer, 0-99%.</summary>
        public static int GetProgress(CookingTable table)
        {
            SO_CookingTable_Recipe recipe = table.CurrentRecipe;
            return recipe != null ? Percent.OfProgress(GetSeconds(table), recipe.CookTime) : 0;
        }

        /// <summary>Seconds a slot has been cooking its current item.</summary>
        public static float GetSeconds(CookingSlot slot)
        {
            if (_active && !Raft_Network.IsHost && SlotTimers.TryGetValue(slot, out Timer<Item_Base> timer)
                && timer.Subject == slot.CurrentItem)
            {
                return Mathf.Max(timer.Seconds, slot.CookTimer);
            }

            return slot.CookTimer;
        }

        /// <summary>Seconds a cooking pot or juicer has been cooking its current recipe.</summary>
        public static float GetSeconds(CookingTable table)
        {
            if (_active && !Raft_Network.IsHost && TableTimers.TryGetValue(table, out Timer<SO_CookingTable_Recipe> timer)
                && timer.Subject == table.CurrentRecipe)
            {
                return Mathf.Max(timer.Seconds, table.CookTimer);
            }

            return table.CookTimer;
        }

        /// <summary>Whether the stand burns fuel or draws power, which is when its slots cook.</summary>
        public static bool IsRunning(Block_CookingStand stand)
        {
            return IsUsingEnergySource(stand);
        }

        /// <summary>Whether a pot or juicer with a recipe is cooking: fuel burns or the battery gives power.</summary>
        public static bool IsRunning(CookingTable table)
        {
            return IsCooking(table);
        }

        /// <summary>Same condition as the host: busy, unfinished slots cook while the stand uses its energy source.</summary>
        public static void OnStandUpdated(Block_CookingStand stand)
        {
            if (!_active || Raft_Network.IsHost || stand.cookingSlots == null)
            {
                return;
            }

            // Runs after the game's update of every stand, every frame; it must never break it.
            try
            {
                AdvanceSlots(stand);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        /// <summary>Same condition as the host: a recipe without portions yet cooks while IsCooking().</summary>
        public static void OnTableUpdated(CookingTable table)
        {
            if (!_active || Raft_Network.IsHost)
            {
                return;
            }

            try
            {
                AdvanceTable(table);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        private static void AdvanceSlots(Block_CookingStand stand)
        {
            bool running = IsUsingEnergySource(stand);
            foreach (CookingSlot slot in stand.cookingSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                // Fake slots belong to an item cooked on another slot and have no timer of their own.
                if (!slot.IsBusy || slot.IsComplete || slot.FakeSlot || slot.CurrentItem == null)
                {
                    SlotTimers.Remove(slot);
                    continue;
                }

                Timer<Item_Base> timer = GetOrStart(SlotTimers, slot, slot.CurrentItem, slot.CookTimer);
                if (running)
                {
                    timer.Seconds += Time.deltaTime;
                }
            }
        }

        private static void AdvanceTable(CookingTable table)
        {
            SO_CookingTable_Recipe recipe = table.CurrentRecipe;
            if (recipe == null || table.Portions > 0)
            {
                TableTimers.Remove(table);
                return;
            }

            Timer<SO_CookingTable_Recipe> timer = GetOrStart(TableTimers, table, recipe, table.CookTimer);
            if (IsCooking(table))
            {
                timer.Seconds += Time.deltaTime;
            }
        }

        private static Timer<TSubject> GetOrStart<TKey, TSubject>(
            Dictionary<TKey, Timer<TSubject>> timers, TKey key, TSubject subject, float seconds)
            where TKey : UnityEngine.Object
            where TSubject : UnityEngine.Object
        {
            if (timers.TryGetValue(key, out Timer<TSubject> timer) && timer.Subject == subject)
            {
                return timer;
            }

            if (timers.Count >= PruneThreshold)
            {
                Prune(timers);
            }

            timer = new Timer<TSubject>(subject, seconds);
            timers[key] = timer;
            return timer;
        }

        private static void Prune<TKey, TValue>(Dictionary<TKey, TValue> timers) where TKey : UnityEngine.Object
        {
            List<TKey> destroyed = new List<TKey>();
            foreach (TKey key in timers.Keys)
            {
                if (key == null)
                {
                    destroyed.Add(key);
                }
            }

            foreach (TKey key in destroyed)
            {
                timers.Remove(key);
            }
        }

        private static void LogOnce(Exception e)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModLog.Error(HoverInfoFeature.FeatureId + ": failed to advance a cooking timer.", e);
            }
        }

        private static void Clear()
        {
            SlotTimers.Clear();
            TableTimers.Clear();
        }

        private sealed class Timer<TSubject>
        {
            public Timer(TSubject subject, float seconds)
            {
                Subject = subject;
                Seconds = seconds;
            }

            /// <summary>Item or recipe being cooked; a different one starts the timer again.</summary>
            public TSubject Subject { get; }

            public float Seconds { get; set; }
        }
    }
}
