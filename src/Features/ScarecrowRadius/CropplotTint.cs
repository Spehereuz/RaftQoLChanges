using System.Collections.Generic;
using QoLChanges.GameState;
using UnityEngine;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// Lights crop plots that seagulls may attack: green inside a scarecrow's zone, red outside of it. Plots that are not
    /// peckable (the large ones) are never attacked and never lit; empty plots are lit as well, for planning. The light is
    /// the emission the game uses for block previews (<c>_BuildingEmission</c> of the BlockPaint shader), set through the
    /// renderers' property blocks after reading them, like Full net indicator, so paint and the hammer outline stay.
    /// </summary>
    internal sealed class CropplotTint
    {
        // Darker than the game's previews (0.54, 0.75, 0.27) and (0.86, 0.16, 0.24), like a full collection net.
        private static readonly Color ProtectedTint = new Color(0.1f, 0.3f, 0.05f, 1f);
        private static readonly Color UnprotectedTint = new Color(0.4f, 0.06f, 0.04f, 1f);

        private static readonly int EmissionId = Shader.PropertyToID("_BuildingEmission");

        private readonly Dictionary<Cropplot, bool> _tinted = new Dictionary<Cropplot, bool>();
        private readonly List<Cropplot> _removed = new List<Cropplot>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        /// <param name="protectors">Scarecrows whose zones protect crops, with the preview of one that can be placed.</param>
        public void Refresh(List<Scarecrow> protectors, float radius)
        {
            _removed.Clear();
            foreach (Cropplot cropplot in _tinted.Keys)
            {
                if (cropplot == null)
                {
                    _removed.Add(cropplot);
                }
            }

            foreach (Cropplot cropplot in _removed)
            {
                _tinted.Remove(cropplot);
            }

            List<Cropplot> cropplots = PlantManager.allCropplots;
            if (cropplots == null)
            {
                return;
            }

            foreach (Cropplot cropplot in cropplots)
            {
                if (cropplot == null || !cropplot.peckable)
                {
                    continue;
                }

                bool isProtected = IsProtected(cropplot, protectors, radius);
                if (_tinted.TryGetValue(cropplot, out bool tintedProtected) && tintedProtected == isProtected)
                {
                    continue;
                }

                if (SetTint(cropplot, isProtected ? ProtectedTint : UnprotectedTint))
                {
                    _tinted[cropplot] = isProtected;
                }
            }
        }

        /// <summary>Gives every lit crop plot its own emission back.</summary>
        public void Clear()
        {
            foreach (Cropplot cropplot in _tinted.Keys)
            {
                if (cropplot != null)
                {
                    SetTint(cropplot, null);
                }
            }

            _tinted.Clear();
        }

        private static bool IsProtected(Cropplot cropplot, List<Scarecrow> protectors, float radius)
        {
            Vector3 centre = Scarecrows.CropplotCentre(cropplot);
            foreach (Scarecrow scarecrow in protectors)
            {
                if (Scarecrows.GetZone(scarecrow).Contains(scarecrow.transform, centre, radius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Only the renderers of the crop plot block, found before anything was planted in it.</summary>
        private bool SetTint(Cropplot cropplot, Color? tint)
        {
            OccupyingComponent occupying = cropplot.occupyingComponent;
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
                _block.SetColor(EmissionId, tint ?? MaterialEmission(renderer));
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
