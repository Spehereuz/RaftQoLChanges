using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLDevTools
{
    /// <summary>
    /// Shows the game's "researched" notification for random items one second after the command, so that the console can
    /// be closed first. The notifications go through the game's own queue like real ones; nothing is learned, given or
    /// sent to other players.
    /// </summary>
    internal static class TestNotifications
    {
        public const string Syntax = "qoldev.notify [count]";
        public const int MaxCount = 10;

        private const float Delay = 1f;
        private const string ResearchNotification = "Research";

        public static string Schedule(Network_Player player, string[] args)
        {
            int count = 1;
            if (args != null && args.Length > 1)
            {
                return "Syntax: " + Syntax;
            }

            if (args != null && args.Length == 1
                && (!int.TryParse(args[0].Trim(), out count) || count < 1 || count > MaxCount))
            {
                return "Count must be between 1 and " + MaxCount + ". Syntax: " + Syntax;
            }

            if (ComponentManager<NotificationManager>.Value == null)
            {
                return "The game has no notifications here.";
            }

            // Created in the world's scene, so it is gone together with the world.
            GameObject host = new GameObject("QoLDevTools_TestNotifications");
            DelayedNotifications delayed = host.AddComponent<DelayedNotifications>();
            delayed.Initialize(player, count, Time.unscaledTime + Delay);
            return "Showing " + count + " research notification" + (count == 1 ? string.Empty : "s") + " in 1 second.";
        }

        /// <summary>Queues <paramref name="count"/> research notifications of different random items.</summary>
        internal static void Show(Network_Player player, int count)
        {
            NotificationManager manager = ComponentManager<NotificationManager>.Value;
            if (manager == null || player == null)
            {
                return;
            }

            List<Item_Base> items = new List<Item_Base>();
            foreach (Item_Base item in ItemManager.GetAllItems())
            {
                if (item != null && item.settings_Inventory != null && item.settings_Inventory.Sprite != null
                    && !string.IsNullOrEmpty(item.settings_Inventory.DisplayName))
                {
                    items.Add(item);
                }
            }

            for (int i = 0; i < count && items.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, items.Count);
                Item_Base item = items[index];
                items.RemoveAt(index);

                Notification_Research notification = manager.ShowNotification(ResearchNotification) as Notification_Research;
                if (notification == null)
                {
                    return;
                }

                notification.researchInfoQue.Enqueue(
                    new Notification_Research_Info(item.settings_Inventory.DisplayName, player.steamID, item.settings_Inventory.Sprite));
            }

            DevLog.Info("Queued " + count + " research notification(s).");
        }
    }

    /// <summary>Waits on unscaled time, so it also counts while the game is paused, then shows the notifications once.</summary>
    internal sealed class DelayedNotifications : MonoBehaviour
    {
        private Network_Player _player;
        private int _count;
        private float _showAt;

        public void Initialize(Network_Player player, int count, float showAt)
        {
            _player = player;
            _count = count;
            _showAt = showAt;
        }

        private void Update()
        {
            if (Time.unscaledTime < _showAt)
            {
                return;
            }

            try
            {
                TestNotifications.Show(_player, _count);
            }
            catch (Exception e)
            {
                DevLog.Error("Failed to show notifications.", e);
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }
}
