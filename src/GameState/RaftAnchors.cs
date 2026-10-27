using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QoLChanges.GameState
{
    /// <summary>State of all anchors on the raft at one moment.</summary>
    internal struct AnchorSummary
    {
        public int Total;
        public int Down;
        public int Dropping;
        public int Raising;

        /// <summary>
        /// The raft is held: a stationary anchor is dropping or at the bottom, or a thrown anchor has reached the bottom.
        /// This is what the host counts in <c>Raft.AnchorCount</c>, which is not kept up to date on other players' games.
        /// </summary>
        public bool HoldsRaft;
    }

    /// <summary>
    /// Finds the placed stationary anchors (manual and advanced) and throwable anchor stands. The game keeps no list of
    /// them, so they are searched again every second; their state is read on every refresh.
    /// </summary>
    internal sealed class RaftAnchors
    {
        private const float SearchInterval = 1f;

        private static readonly AccessTools.FieldRef<Anchor_Stationary, bool> WeighingField =
            AccessTools.FieldRefAccess<Anchor_Stationary, bool>("weighingAnchor");

        private readonly List<Anchor_Stationary> _stationary = new List<Anchor_Stationary>();
        private readonly List<Anchor_Throwable_Stand> _stands = new List<Anchor_Throwable_Stand>();
        private float _nextSearch;

        public AnchorSummary Read()
        {
            if (Time.unscaledTime >= _nextSearch)
            {
                _nextSearch = Time.unscaledTime + SearchInterval;
                Search();
            }

            AnchorSummary summary = new AnchorSummary();
            foreach (Anchor_Stationary anchor in _stationary)
            {
                if (anchor == null)
                {
                    continue;
                }

                summary.Total++;
                if (anchor.AtBottom)
                {
                    summary.Down++;
                }
                else if (anchor.DroppingAnchor)
                {
                    summary.Dropping++;
                }
                else if (WeighingField(anchor))
                {
                    summary.Raising++;
                }

                summary.HoldsRaft |= anchor.AtBottom || anchor.DroppingAnchor;
            }

            foreach (Anchor_Throwable_Stand stand in _stands)
            {
                if (stand == null)
                {
                    continue;
                }

                summary.Total++;
                if (stand.FullyAnchored)
                {
                    summary.Down++;
                    summary.HoldsRaft = true;
                }
                else if (stand.HasThrownAnchor)
                {
                    summary.Dropping++;
                }
            }

            return summary;
        }

        public void Clear()
        {
            _stationary.Clear();
            _stands.Clear();
            _nextSearch = 0f;
        }

        private void Search()
        {
            _stationary.Clear();
            foreach (Anchor_Stationary anchor in UnityEngine.Object.FindObjectsOfType<Anchor_Stationary>())
            {
                if (IsPlaced(anchor))
                {
                    _stationary.Add(anchor);
                }
            }

            _stands.Clear();
            foreach (Anchor_Throwable_Stand stand in UnityEngine.Object.FindObjectsOfType<Anchor_Throwable_Stand>())
            {
                if (IsPlaced(stand))
                {
                    _stands.Add(stand);
                }
            }
        }

        /// <summary>Excludes the preview of a block that is being placed.</summary>
        private static bool IsPlaced(Component component)
        {
            Block block = component.GetComponentInParent<Block>();
            return block != null && block.hasBeenPlaced;
        }
    }
}
