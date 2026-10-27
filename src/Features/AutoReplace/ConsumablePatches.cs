using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// Game actions that use up the item in the player's hand, other than tools and food. Each of them removes the
    /// item from the selected hotbar slot or, by name, from the first slot that holds it, which can be the selected
    /// one. Crafting, trading, storing and moving items are not listed, so they never refill the hotbar.
    /// </summary>
    [HarmonyPatch]
    internal static class ConsumablePatches
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase[] methods =
            {
                // Placing an item from the hotbar: storages, grills, decorations and other placeables.
                Find(typeof(BlockCreator), "CreateBlock", typeof(Item_Base), typeof(Vector3), typeof(Vector3), typeof(DPS), typeof(int), typeof(bool), typeof(uint), typeof(uint), typeof(uint)),

                // Seeds and saplings.
                Find(typeof(PlantManager), "PlantSeed", typeof(Cropplot), typeof(Plant), typeof(uint), typeof(bool), typeof(bool)),

                // Batteries put into a machine.
                Find(typeof(Battery), "Insert", typeof(Network_Player), typeof(int), typeof(int)),

                // Fuel, raw food and ores put into grills, smelters, the paint mill, fuel tanks, the cooking pot and the juicer.
                Find(typeof(Block_CookingStand), "IncrementFuel", typeof(int), typeof(Network_Player)),
                Find(typeof(Block_CookingStand), "InsertItem", typeof(Item_Base), typeof(CookingSlot[]), typeof(Network_Player)),
                Find(typeof(FuelNetwork), "AddFuel", typeof(Network_Player), typeof(int)),
                Find(typeof(CookingTable_Slot), "InsertItem", typeof(Network_Player), typeof(ItemInstance)),

                // Healing salves, buff items and mystery packages.
                Find(typeof(Item_HealingSalve), "HandleChannelDelay"),
                Find(typeof(Item_HealingSalve), "TriggerHeal"),
                Find(typeof(Item_ApplyBuff), "HandleChannelDelay"),
                Find(typeof(Item_ApplyBuff), "TriggerEffect"),
                Find(typeof(Item_MysteryPackage), "ChannelFinished"),

                // Thrown items, shark bait and fireworks.
                Find(typeof(ThrowableComponent), "Throw", typeof(uint), typeof(uint), typeof(Vector3), typeof(Vector3), typeof(Throwable_Object)),
                Find(typeof(SharkBaitThrower), "OnHitWater"),
                Find(typeof(Firework_Hand), "LaunchFirework", typeof(Vector3), typeof(Vector3), typeof(float), typeof(bool), typeof(int), typeof(uint)),

                // Trophies and cassettes.
                Find(typeof(TrophyHolder), "PlaceTrophy", typeof(Network_Player), typeof(Item_Base)),
                Find(typeof(RadioPlayer), "InsertCassette", typeof(Network_Player), typeof(int)),
            };

            foreach (MethodBase method in methods)
            {
                if (method != null)
                {
                    yield return method;
                }
            }
        }

        [HarmonyPrefix]
        private static void Prefix(out HotbarSnapshot __state)
        {
            __state = AutoReplaceService.Capture();
        }

        [HarmonyPostfix]
        private static void Postfix(HotbarSnapshot __state)
        {
            AutoReplaceService.Complete(__state, ReplaceCategory.Consumables);
        }

        /// <summary>A missing method (changed by a game update) only disables refilling after that action.</summary>
        private static MethodBase Find(Type type, string name, params Type[] parameters)
        {
            MethodInfo method = AccessTools.Method(type, name, parameters);
            if (method == null)
            {
                ModLog.Warning(AutoReplaceFeature.FeatureId + ": " + type.Name + "." + name + " not found, it will not refill the hotbar.");
            }

            return method;
        }
    }
}
