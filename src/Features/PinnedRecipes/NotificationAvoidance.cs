using System.Collections.Generic;
using QoLChanges.GameState;
using QoLChanges.UI;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>
    /// Keeps the pinned recipes in the top-right corner and the game's notifications apart: fades the recipes out, moves
    /// them below the notifications or moves the notifications below them. Only the local HUD changes; notifications are
    /// moved by their root, so the game's own animation of them stays as it is. Distances are measured to what is drawn:
    /// the wooden plate of a notification is larger than its rect and reaches about 30 px further down.
    /// </summary>
    internal sealed class NotificationAvoidance
    {
        // Space between the recipes and the notifications, in HUD canvas units.
        private const float Gap = 12f;
        private const float FadeTime = 0.2f;
        private const float MoveTime = 0.25f;

        private readonly Dictionary<RectTransform, Vector2> _vanillaPositions = new Dictionary<RectTransform, Vector2>();
        private readonly Dictionary<Notification, Graphic[]> _graphics = new Dictionary<Notification, Graphic[]>();
        private readonly Vector3[] _corners = new Vector3[4];
        private NotificationManager _manager;
        private float _moveDistance;

        /// <summary>Called every frame after the game has updated its notifications.</summary>
        public void Update(HudSection section, PinnedRecipesConfig config)
        {
            PinnedRecipesNotifications mode = config.position == PinnedRecipesPosition.TopRight
                ? config.notifications
                : PinnedRecipesNotifications.Off;

            NotificationManager manager = GameNotifications.Manager;
            if (manager != _manager)
            {
                // The notifications of the previous world are gone with it.
                _vanillaPositions.Clear();
                _graphics.Clear();
                _manager = manager;
            }

            bool showing = GameNotifications.IsShowing(manager);
            float deltaTime = Time.unscaledDeltaTime;

            float alpha = mode == PinnedRecipesNotifications.HideRecipes && showing ? 0f : 1f;
            section.Alpha = Mathf.MoveTowards(section.Alpha, alpha, deltaTime / FadeTime);

            float gap = 0f;
            if (mode == PinnedRecipesNotifications.MoveRecipesDown && showing)
            {
                gap = DistanceBelowNotifications(manager);
                _moveDistance = gap;
            }

            section.CornerGap = Mathf.MoveTowards(section.CornerGap, gap, Mathf.Max(_moveDistance, 1f) * deltaTime / MoveTime);

            if (mode == PinnedRecipesNotifications.MoveNotificationsDown)
            {
                MoveNotificationsBelow(manager, section);
            }
            else
            {
                Restore();
            }
        }

        /// <summary>Puts every notification moved by the mod back where the game placed it.</summary>
        public void Restore()
        {
            foreach (KeyValuePair<RectTransform, Vector2> entry in _vanillaPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.anchoredPosition = entry.Value;
                }
            }

            _vanillaPositions.Clear();
        }

        /// <summary>
        /// How far the recipes have to move down from their corner for their top to be <see cref="Gap"/> below the lowest
        /// notification; notifications are never moved in this mode, so they are where the game puts them.
        /// </summary>
        private float DistanceBelowNotifications(NotificationManager manager)
        {
            List<Notification> notifications = GameNotifications.All(manager);
            Canvas canvas = manager.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (notifications == null || canvasRect == null)
            {
                return 0f;
            }

            float lowest = float.MaxValue;
            foreach (Notification notification in notifications)
            {
                if (TryGetDrawnBounds(notification, canvasRect, out float bottom, out float _))
                {
                    lowest = Mathf.Min(lowest, bottom);
                }
            }

            if (lowest == float.MaxValue)
            {
                return 0f;
            }

            float belowTop = canvasRect.rect.yMax - lowest;
            return Mathf.Max(0f, belowTop + Gap - HudPanel.CornerMargin);
        }

        /// <summary>
        /// Moves every notification down so that its top is <see cref="Gap"/> below the recipes, or leaves it where the
        /// game places it when it is already lower or the recipes are not shown.
        /// </summary>
        private void MoveNotificationsBelow(NotificationManager manager, HudSection section)
        {
            List<Notification> notifications = GameNotifications.All(manager);
            RectTransform managerRect = manager != null ? manager.transform as RectTransform : null;
            if (notifications == null || managerRect == null || !HudPanel.TryGetWorldCorners(section, _corners))
            {
                Restore();
                return;
            }

            float recipesBottom = managerRect.InverseTransformPoint(_corners[0]).y;
            foreach (Notification notification in notifications)
            {
                RectTransform root = notification != null ? notification.transform as RectTransform : null;
                if (root == null || root.parent != managerRect
                    || !TryGetDrawnBounds(notification, managerRect, out float _, out float top))
                {
                    continue;
                }

                if (!_vanillaPositions.TryGetValue(root, out Vector2 vanilla))
                {
                    vanilla = root.anchoredPosition;
                    _vanillaPositions[root] = vanilla;
                }

                float vanillaTop = top - (root.anchoredPosition.y - vanilla.y);
                float shift = Mathf.Min(0f, recipesBottom - Gap - vanillaTop);
                Vector2 target = new Vector2(vanilla.x, vanilla.y + shift);
                if (root.anchoredPosition != target)
                {
                    root.anchoredPosition = target;
                }
            }
        }

        /// <summary>
        /// Bottom and top of everything a notification draws, in the local space of <paramref name="space"/>. The animator
        /// only slides it sideways, so the heights are the same while it is hidden.
        /// </summary>
        private bool TryGetDrawnBounds(Notification notification, RectTransform space, out float bottom, out float top)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            RectTransform panel = notification != null ? GameNotifications.GetPanel(notification) : null;
            if (panel == null)
            {
                return false;
            }

            if (!_graphics.TryGetValue(notification, out Graphic[] graphics) || graphics == null)
            {
                graphics = panel.GetComponentsInChildren<Graphic>(true);
                _graphics[notification] = graphics;
            }

            foreach (Graphic graphic in graphics)
            {
                if (graphic == null || !graphic.enabled || !graphic.gameObject.activeSelf)
                {
                    continue;
                }

                graphic.rectTransform.GetWorldCorners(_corners);
                foreach (Vector3 corner in _corners)
                {
                    float y = space.InverseTransformPoint(corner).y;
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }

            return bottom <= top;
        }
    }
}
