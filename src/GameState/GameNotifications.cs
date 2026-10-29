using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QoLChanges.GameState
{
    /// <summary>
    /// The game's notifications in the top-right corner: research, blueprints, notebook notes, quest items and characters.
    /// <see cref="NotificationManager"/> shows them one at a time from a queue. Every notification has an AnimatorParent
    /// child that its animator slides in from the right edge, keeps on screen and slides out again in 0.42 s, switching it
    /// off at the end; the next notification starts as soon as the previous one starts sliding out.
    /// </summary>
    internal static class GameNotifications
    {
        private const string PanelName = "AnimatorParent";

        private static readonly AccessTools.FieldRef<NotificationManager, List<Notification>> NotificationsField =
            AccessTools.FieldRefAccess<NotificationManager, List<Notification>>("notifications");

        private static readonly AccessTools.FieldRef<NotificationManager, Queue<Notification>> QueueField =
            AccessTools.FieldRefAccess<NotificationManager, Queue<Notification>>("notificationQue");

        /// <summary>The notification manager of the current world; null without one.</summary>
        public static NotificationManager Manager => ComponentManager<NotificationManager>.Value;

        /// <summary>Every notification the manager can show, shown or not.</summary>
        public static List<Notification> All(NotificationManager manager)
        {
            return manager != null ? NotificationsField(manager) : null;
        }

        /// <summary>
        /// True from the moment a notification is queued until the last one has slid out completely.
        /// </summary>
        public static bool IsShowing(NotificationManager manager)
        {
            if (manager == null)
            {
                return false;
            }

            Queue<Notification> queue = QueueField(manager);
            if (queue != null && queue.Count > 0)
            {
                return true;
            }

            List<Notification> notifications = NotificationsField(manager);
            if (notifications == null)
            {
                return false;
            }

            foreach (Notification notification in notifications)
            {
                if (notification == null)
                {
                    continue;
                }

                RectTransform panel = GetPanel(notification);
                if (notification.active || (panel != null && panel.gameObject.activeSelf))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The part of a notification that is animated and visible; its root keeps the place on screen.</summary>
        public static RectTransform GetPanel(Notification notification)
        {
            Transform panel = notification.transform.Find(PanelName);
            if (panel == null && notification.transform.childCount > 0)
            {
                panel = notification.transform.GetChild(0);
            }

            return panel as RectTransform;
        }
    }
}
