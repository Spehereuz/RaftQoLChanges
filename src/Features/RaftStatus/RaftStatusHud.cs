using System;
using QoLChanges.Core;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>
    /// Fills the raft status section of the HUD panel while the local player stands on the raft. Everything is read
    /// from the game objects every refresh; nothing is changed or sent over the network.
    /// </summary>
    internal sealed class RaftStatusHud : MonoBehaviour
    {
        private const string HostName = "QoLChanges_RaftStatus";
        private const string SectionName = "RaftStatus";
        private const float RefreshInterval = 0.1f;

        private static RaftStatusHud _instance;

        private readonly RaftAnchors _anchors = new RaftAnchors();
        private RaftStatusConfig _config;
        private HudSection _section;
        private float _nextRefresh;
        private bool _errorLogged;

        public static void Activate(RaftStatusConfig config)
        {
            Deactivate();

            GameObject host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<RaftStatusHud>();
            _instance._config = config;
            _instance._section = HudPanel.AddSection(SectionName, 0);
            _instance._section.Scale = config.scale / 100f;
        }

        public static void Deactivate()
        {
            if (_instance != null)
            {
                // Immediate, so that a HUD activated again in the same frame (settings reapplied) is the only one.
                DestroyImmediate(_instance.gameObject);
            }

            _instance = null;
        }

        private void OnDestroy()
        {
            HudPanel.RemoveSection(_section);
            _section = null;
            _anchors.Clear();
            RaftStatusIcons.Clear();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + RefreshInterval;
            try
            {
                Refresh();
            }
            catch (Exception e)
            {
                _section.Visible = false;
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(RaftStatusFeature.FeatureId + ": failed to update the HUD.", e);
                }
            }
        }

        private void Refresh()
        {
            if (!LocalPlayerIsOnRaft())
            {
                _section.Visible = false;
                _anchors.Clear();
                return;
            }

            int count = 0;
            if (_config.anchor)
            {
                AnchorSummary anchors = _anchors.Read();
                if (anchors.Total > 0)
                {
                    SetLine(count++, RaftStatusIcons.Anchor, RaftStatusText.Anchor(anchors));
                }
            }

            _section.TrimLines(count);
            _section.Visible = count > 0;
        }

        private void SetLine(int index, string iconItem, string text)
        {
            HudLine line = _section.GetLine(index);
            line.Icon = RaftStatusIcons.Get(iconItem);
            line.Text = text;
            line.ArrowAngle = null;
        }

        private static bool LocalPlayerIsOnRaft()
        {
            Network_Player player = ComponentManager<Network_Player>.Value;
            return player != null && player.PersonController != null && player.PersonController.HasRaftAsParent;
        }
    }
}
