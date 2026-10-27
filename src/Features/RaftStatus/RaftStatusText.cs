using QoLChanges.UI;

namespace QoLChanges.Features.RaftStatus
{
    /// <summary>Texts of the HUD lines. Labels are white, states and warnings are coloured.</summary>
    internal static class RaftStatusText
    {
        public static string Anchor(AnchorSummary anchors)
        {
            string state;
            if (anchors.Down > 0)
            {
                state = StatusText.Colored("Anchored", StatusText.Ready);
            }
            else if (anchors.Dropping > 0)
            {
                state = StatusText.Colored("Dropping", StatusText.Progress);
            }
            else if (anchors.Raising > 0)
            {
                state = StatusText.Colored("Raising", StatusText.Progress);
            }
            else
            {
                state = "Free";
            }

            return anchors.Total > 1 ? StatusText.Join(state, anchors.Down + "/" + anchors.Total + " down") : state;
        }

        /// <summary>"2/3 running", followed by engines without fuel and why the engines do not move the raft.</summary>
        public static string Engines(EngineSummary engines, EngineDrive drive)
        {
            string text = engines.Running + "/" + engines.Total + " running";
            if (engines.NoFuel > 0)
            {
                text = StatusText.Join(text, StatusText.Colored(engines.NoFuel + " no fuel", StatusText.Problem));
            }

            switch (drive)
            {
                case EngineDrive.HeldByAnchor:
                    return StatusText.Join(text, StatusText.Colored("Held by anchor", StatusText.Progress));
                case EngineDrive.NoThrust:
                    return StatusText.Join(text, StatusText.Colored("No thrust", StatusText.Progress));
                default:
                    return text;
            }
        }
    }
}
