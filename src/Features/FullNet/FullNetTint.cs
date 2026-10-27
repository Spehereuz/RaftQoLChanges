using System;
using System.Collections.Generic;
using QoLChanges.Core;
using QoLChanges.GameState;
using UnityEngine;

namespace QoLChanges.Features.FullNet
{
    /// <summary>
    /// Lights full nets with the emission the game uses for block previews (<c>_BuildingEmission</c> of the BlockPaint
    /// shader), set through the renderers' property blocks like the game's paint and removal outline. The block is read
    /// before it is changed, so paint, the outline and the tint never erase each other.
    /// </summary>
    internal static class FullNetTint
    {
        // The game lights a preview that does not fit with (0.86, 0.16, 0.24); the tint is darker and redder so that a
        // full net does not look like a preview.
        private static readonly Color Tint = new Color(0.4f, 0.06f, 0.04f, 1f);

        private static readonly int EmissionId = Shader.PropertyToID("_BuildingEmission");
        private static readonly HashSet<ItemCollector> Tinted = new HashSet<ItemCollector>();

        private static MaterialPropertyBlock _block;
        private static bool _active;
        private static bool _errorLogged;

        public static void Activate()
        {
            _active = true;
            _errorLogged = false;
            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            foreach (ItemCollector collector in UnityEngine.Object.FindObjectsOfType<ItemCollector>())
            {
                Refresh(collector);
            }
        }

        public static void Deactivate()
        {
            _active = false;
            foreach (ItemCollector collector in Tinted)
            {
                if (collector != null)
                {
                    SetTint(collector, false);
                }
            }

            Tinted.Clear();
        }

        /// <summary>Tints the net when it has become full and removes the tint when it is no longer full.</summary>
        public static void Refresh(ItemCollector collector)
        {
            if (!_active || !ItemNets.IsNet(collector))
            {
                return;
            }

            // Called from the game's collecting and networking code; an exception there would break the net.
            try
            {
                bool full = ItemNets.IsFull(collector);
                if (full == Tinted.Contains(collector) || !SetTint(collector, full))
                {
                    return;
                }

                if (full)
                {
                    // Nets of a world that was left are destroyed without being emptied.
                    Tinted.RemoveWhere(tinted => tinted == null);
                    Tinted.Add(collector);
                }
                else
                {
                    Tinted.Remove(collector);
                }
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(FullNetFeature.FeatureId + ": failed to update the net '" + collector.name + "'.", e);
                }
            }
        }

        /// <summary>Only the renderers of the net block itself: the collected items are children of the net.</summary>
        private static bool SetTint(ItemCollector collector, bool tinted)
        {
            Block block = collector.GetComponentInParent<Block>();
            OccupyingComponent occupying = block != null ? block.occupyingComponent : null;
            if (occupying == null || occupying.renderers == null)
            {
                return false;
            }

            foreach (Renderer renderer in occupying.renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, tinted ? Tint : MaterialEmission(renderer));
                renderer.SetPropertyBlock(_block);
            }

            return true;
        }

        private static Color MaterialEmission(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            return material != null && material.HasProperty(EmissionId) ? material.GetColor(EmissionId) : Color.clear;
        }
    }
}
