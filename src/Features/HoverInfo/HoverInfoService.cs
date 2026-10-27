using System;
using System.Collections.Generic;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.HoverInfo
{
    /// <summary>
    /// Connects the game's interaction raycast with the providers and the line under the crosshair. Called once
    /// per frame for the object the local player aims at; when the game skips its raycast (menus open, player
    /// busy) nothing refreshes the line and it hides itself.
    /// </summary>
    internal static class HoverInfoService
    {
        private const string HostName = "QoLChanges_HoverInfo";

        private static IHoverInfoProvider[] _providers;
        private static HoverInfoLine _line;
        private static bool _errorLogged;

        public static void Activate(IList<IHoverInfoProvider> providers)
        {
            Deactivate();

            _providers = new List<IHoverInfoProvider>(providers).ToArray();
            _errorLogged = false;

            GameObject host = new GameObject(HostName);
            UnityEngine.Object.DontDestroyOnLoad(host);
            _line = host.AddComponent<HoverInfoLine>();
        }

        public static void Deactivate()
        {
            _providers = null;
            if (_line != null)
            {
                UnityEngine.Object.DestroyImmediate(_line.gameObject);
            }

            _line = null;
        }

        /// <param name="target">Interactable under the crosshair, null when there is none.</param>
        public static void OnInteractableRaycast(RaycastInteractable target)
        {
            if (_line == null || _providers == null)
            {
                return;
            }

            // Runs inside the game's per-frame pickup handling; an exception here would break interaction.
            try
            {
                string text = Describe(target);
                if (!string.IsNullOrEmpty(text))
                {
                    _line.Show(text);
                }
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    string name = target != null ? target.name : "nothing";
                    ModLog.Error(HoverInfoFeature.FeatureId + ": failed to describe '" + name + "'.", e);
                }
            }
        }

        private static string Describe(RaycastInteractable target)
        {
            foreach (IHoverInfoProvider provider in _providers)
            {
                if (provider.TryDescribe(target, out string text))
                {
                    return text;
                }
            }

            return null;
        }
    }
}
