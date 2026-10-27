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

        // Line whose arrow follows the camera every frame, -1 when there is none.
        private int _thrustLine = -1;
        private Vector3 _thrust;

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
            try
            {
                if (Time.unscaledTime >= _nextRefresh)
                {
                    _nextRefresh = Time.unscaledTime + RefreshInterval;
                    Refresh();
                }

                UpdateThrustArrow();
            }
            catch (Exception e)
            {
                _section.Visible = false;
                _thrustLine = -1;
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
            _thrustLine = -1;

            // Engines need the anchors as well: an anchor that holds the raft stops them and their fuel.
            bool showEngines = _config.engines || _config.fuel;
            AnchorSummary anchors = _config.anchor || showEngines ? _anchors.Read() : default(AnchorSummary);
            if (_config.anchor && anchors.Total > 0)
            {
                SetLine(count++, RaftStatusIcons.Anchor, RaftStatusText.Anchor(anchors));
            }

            EngineSummary engines = showEngines ? RaftEngines.Read() : default(EngineSummary);
            if (engines.Total > 0)
            {
                // Until the raft is loaded nothing is rated as too weak.
                int weight = RaftEngines.ReadWeight() ?? 0;
                EnginePower power = RaftEngines.GetPower(engines, weight);
                EngineDrive drive = RaftEngines.GetDrive(engines, anchors.HoldsRaft, power);

                if (_config.engines)
                {
                    if (drive == EngineDrive.Pushing && _config.thrustDirection)
                    {
                        _thrustLine = count;
                        _thrust = engines.Thrust;
                    }

                    int needed = RaftEngines.EnginesNeeded(engines, weight);
                    string text = RaftStatusText.Engines(engines, drive, power, needed, _config.enginePower);
                    SetLine(count++, RaftStatusIcons.Engine, text);
                }

                if (_config.fuel)
                {
                    SetLine(count++, RaftStatusIcons.Fuel, RaftStatusText.Fuel(engines, drive));
                }
            }

            _section.TrimLines(count);
            _section.Visible = count > 0;
        }

        /// <summary>The arrow shows where the engines push the raft as seen from the camera, so it turns every frame.</summary>
        private void UpdateThrustArrow()
        {
            if (_thrustLine < 0 || _thrustLine >= _section.Lines.Count)
            {
                return;
            }

            Network_Player player = ComponentManager<Network_Player>.Value;
            Transform view = player != null && player.CameraTransform != null ? player.CameraTransform : null;
            if (view == null)
            {
                _section.Lines[_thrustLine].ArrowAngle = null;
                return;
            }

            Vector3 forward = view.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                // Looking straight up or down: the top of the screen points where the camera is turned.
                forward = view.up;
                forward.y = 0f;
            }

            _section.Lines[_thrustLine].ArrowAngle = Vector3.SignedAngle(forward, _thrust, Vector3.up);
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
