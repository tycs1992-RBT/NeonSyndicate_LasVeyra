using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Exploration only reads shipped places and earned powers. It never grants or discovers anything.</summary>
    public static class VisibleRealityRulesV0
    {
        public static WildSite NearestWild(IList<WildSite> sites, Vector3 from)
        {
            WildSite best = null;
            float distance = float.MaxValue;
            if (sites == null || !MathUtil.IsFinite(from)) return null;
            foreach (WildSite site in sites)
            {
                if (site == null || string.IsNullOrEmpty(site.Id) || !MathUtil.IsFinite(site.Center)) continue;
                float d = MathUtil.FlatSqrDistance(from, site.Center);
                if (d >= distance) continue;
                distance = d;
                best = site;
            }
            return best;
        }

        public static string Ability(bool surge, bool leap, bool flight)
        {
            if (flight) return "Flight earned: jump, then double-tap Space / A in the air to fly. Steer toward the wilds; double-tap again to stop.";
            if (leap) return "Super Leap earned: crouch with Left Ctrl / L3, hold Space / A, then release. Hold jump in the air to glide.";
            if (surge) return "Surge earned: sprint, then press Left Ctrl / L3 to run faster along an open walk.";
            return "Travel powers are not ready. Walk and use the normal jump; progression stays with the story.";
        }

        public static void ResetStatics() { }
    }
}
