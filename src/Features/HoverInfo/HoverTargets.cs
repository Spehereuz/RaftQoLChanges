using System.Collections.Generic;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    internal static class HoverTargets
    {
        /// <summary>
        /// Finds the game object of type <typeparamref name="T"/> that the aimed interactable belongs to: one of
        /// the raycastables it forwards to (RaycastInteractable_Extension adds those of other objects), or a
        /// component on it or its parents (a plant on a crop plot, a part of a machine).
        /// </summary>
        public static T Find<T>(RaycastInteractable target) where T : Component
        {
            if (target == null)
            {
                return null;
            }

            List<IRaycastable> raycastables = target.RaycastableObjects;
            if (raycastables != null)
            {
                foreach (IRaycastable raycastable in raycastables)
                {
                    T component = raycastable as T;
                    if (component != null)
                    {
                        return component;
                    }
                }
            }

            return target.GetComponentInParent<T>();
        }
    }
}
