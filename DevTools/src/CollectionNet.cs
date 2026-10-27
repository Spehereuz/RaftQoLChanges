using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QoLDevTools
{
    /// <summary>
    /// Fills or empties the collection net under the crosshair the way the host does: floating items are spawned by the
    /// game's debris spawner and collected by the net, and emptying removes them; every step is sent to other players.
    /// </summary>
    internal static class CollectionNet
    {
        public const string Syntax = "qoldev.net <count|full|clear>";

        // Spawned items a net cannot collect (barrels have to be carried by hand) are removed again.
        private const int MaxSpawnAttempts = 5;

        private static readonly AccessTools.FieldRef<ItemCollector, int> CapacityField =
            AccessTools.FieldRefAccess<ItemCollector, int>("maxNumberOfItems");

        private static readonly MethodInfo ClearMethod = AccessTools.Method(typeof(ItemCollector), "ClearCollectedItems");

        public static string Set(string argument)
        {
            string text = argument == null ? string.Empty : argument.Trim().ToLowerInvariant();
            bool full = text == "full";
            int count = 0;
            if (!full && text != "clear" && (!int.TryParse(text, out count) || count < 0))
            {
                return "'" + argument + "' is not a number of items, 'full' or 'clear'. Syntax: " + Syntax;
            }

            if (!Raft_Network.IsHost)
            {
                return "Only the host can change collection nets.";
            }

            ItemCollector collector = FindAimedNet();
            if (collector == null)
            {
                return "Aim at a collection net.";
            }

            int capacity = CapacityField(collector);
            if (full)
            {
                if (capacity <= 0)
                {
                    return "This net has no limit.";
                }

                count = capacity;
            }

            if (capacity > 0 && count > capacity)
            {
                return "This net holds at most " + capacity + " items.";
            }

            Raft_Network network = ComponentManager<Raft_Network>.Value;
            Clear(network, collector);
            int added = Fill(network, collector, count);

            string result = "The net holds " + collector.collectedItems.Count + (capacity > 0 ? "/" + capacity : string.Empty) + " items.";
            return added < count ? "Could only add " + added + " of " + count + " items. " + result : result;
        }

        private static ItemCollector FindAimedNet()
        {
            RaycastInteractable target = Helper.FindInteractable(Player.UseDistance * 2f);
            ItemNet net = target != null ? target.GetComponentInParent<ItemNet>() : null;
            return net != null ? net.itemCollector : null;
        }

        /// <summary>Same as collecting the items by hand, but nothing goes into anyone's inventory.</summary>
        private static void Clear(Raft_Network network, ItemCollector collector)
        {
            if (collector.collectedItems.Count == 0)
            {
                return;
            }

            // Without a valid player every game removes the items instead of giving them to that player.
            Message_NetworkBehaviour_ID_SteamID message = new Message_NetworkBehaviour_ID_SteamID(
                Messages.ItemCollectorClear, network.NetworkIDManager, collector.ObjectIndex, default(Network_UserId));
            network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            ClearMethod.Invoke(collector, new object[] { null });
        }

        /// <summary>Same as the net catching debris on the host (ItemCollector.OnTriggerEnter).</summary>
        private static int Fill(Raft_Network network, ItemCollector collector, int count)
        {
            // The spawner of ordinary debris; items a net cannot collect are removed below.
            ObjectSpawnerManager spawners = ComponentManager<ObjectSpawnerManager>.Value;
            ObjectSpawner spawner = null;
            if (spawners != null)
            {
                spawner = spawners.plankSpawner != null ? spawners.plankSpawner : spawners.itemSpawner;
            }

            if (spawner == null)
            {
                return 0;
            }

            int added = 0;
            int attempts = 0;
            while (added < count && attempts < count * MaxSpawnAttempts)
            {
                attempts++;
                PickupItem_Networked item = spawner.SpawnItemNetwork(collector.transform.position);
                if (item == null)
                {
                    continue;
                }

                WaterFloatSemih2 floating = item.GetComponent<WaterFloatSemih2>();
                if (item.PickupItem == null || item.PickupItem.requiresHands || floating == null || !floating.enabled)
                {
                    PickupObjectManager.RemovePickupItemNetwork(item);
                    continue;
                }

                Message_ItemCollectorCollect message = new Message_ItemCollectorCollect(
                    Messages.ItemCollectorCollect, network.NetworkIDManager, item, collector.ObjectIndex);
                network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                if (collector.CollectItem(item, added == 0))
                {
                    added++;
                }
            }

            return added;
        }
    }
}
