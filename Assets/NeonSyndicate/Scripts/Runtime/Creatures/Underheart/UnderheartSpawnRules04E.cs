using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Where a creature's ground probe starts and how far it reaches (engine-free). On the surface the old long probes
    /// stay as they were. Underground (below WorldBounds.LayerSplitY, or inside an Underheart cell) a probe starts a short
    /// way above the anchor height, always below the lowest Underheart ceiling (walkable headroom is at least 2.6 m), and
    /// reaches only a few metres down, so it can never land on the street above, on the ceiling or on a floor of a
    /// deeper region below. CreatureDirector.GroundAt and PlaceOnGround use it.
    /// </summary>
    public static class UnderheartSpawnRules04E
    {
        /// <summary>An underground probe starts this far above the anchor (below any ceiling: headroom is at least 2.6 m).</summary>
        public const float ProbeUp = 1.2f;
        /// <summary>...and reaches this far below the anchor (Underheart floors stacked over each other are 6 m or more apart).</summary>
        public const float ProbeDown = 3.5f;
        /// <summary>The walkable headroom every Underheart cell guarantees (the UNDERWORLD layout tests check it).</summary>
        public const float MinHeadroom = 2.6f;
        /// <summary>A surface probe's extra reach below its start (unchanged from the original GroundAt).</summary>
        public const float SurfaceReach = 60f;

        /// <summary>True when a point belongs to the underground layer by height alone (the probe may add Underheart cells).</summary>
        public static bool UndergroundByHeight(Vector3 p)
        {
            return p.y < WorldBounds.LayerSplitY;
        }

        /// <summary>
        /// The probe for a point: start (a point above it) and distance (straight down). requestedUp is what the caller
        /// asked for (3 m for a spawn, 20 m for a wander goal, 80 m for the old fallback); underground it is capped.
        /// </summary>
        public static void Probe(Vector3 p, float requestedUp, bool underground, out Vector3 start, out float distance)
        {
            float up = Mathf.Max(0f, requestedUp);
            if (underground)
            {
                up = Mathf.Min(up, ProbeUp);
                start = new Vector3(p.x, p.y + up, p.z);
                distance = up + ProbeDown;
                return;
            }
            start = new Vector3(p.x, p.y + up, p.z);
            distance = up + SurfaceReach;
        }

        /// <summary>
        /// Simulates a straight-down probe through a column of horizontal surfaces (their heights): the first surface at
        /// or below start within distance, the way Physics.Raycast reports the first hit. Used by the offline tests to prove
        /// that underground probes land on the anchor's own floor; false when nothing is reached.
        /// </summary>
        public static bool FirstHit(IList<float> surfaces, Vector3 start, float distance, out float y)
        {
            y = start.y;
            float best = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < surfaces.Count; i++)
            {
                float s = surfaces[i];
                if (s > start.y || s < start.y - distance) continue;
                if (s > best)
                {
                    best = s;
                    found = true;
                }
            }
            if (found) y = best;
            return found;
        }

        /// <summary>Whether a spawn may fall back to the long 80 m probe when the short one finds nothing (never underground).</summary>
        public static bool AllowsLongFallback(bool underground)
        {
            return !underground;
        }
    }
}
