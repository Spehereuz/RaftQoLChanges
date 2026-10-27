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
    }
}
