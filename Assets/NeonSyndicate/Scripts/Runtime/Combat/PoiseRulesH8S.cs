using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S poise and stagger readability (engine-free, tested offline). Poise already wears down with every hit and a
    /// broken poise staggers (Creature, NpcController); the HUD now says when one more solid hit will do it (the poise
    /// strip pulses at or under NearBreakShare) and how long a stagger leaves the enemy open (a shrinking window bar).
    /// Numbers only read existing state; poise and stagger rules are untouched.
    /// </summary>
    public static class PoiseRulesH8S
    {
        /// <summary>Share of maximum poise at or under which the strip pulses ("nearly broken").</summary>
        public const float NearBreakShare = 0.25f;

        /// <summary>True when poise is above zero but at or under NearBreakShare of its maximum.</summary>
        public static bool NearBreak(float poise, float maxPoise)
        {
            if (!(maxPoise > 0f) || !(poise > 0f)) return false;
            return poise / maxPoise <= NearBreakShare;
        }

        /// <summary>0 to 1 fill of a poise strip.</summary>
        public static float Fill(float poise, float maxPoise)
        {
            if (!(maxPoise > 0f)) return 0f;
            return Mathf.Clamp01(poise / maxPoise);
        }

        /// <summary>The nearly-broken pulse, 0.55 to 1, on unscaled time.</summary>
        public static float Pulse(float time)
        {
            return 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(time * 9f));
        }

        /// <summary>How much of a stagger window is left, 1 when it starts and 0 when it ends (from StaggerProgress).</summary>
        public static float WindowLeft(float staggerProgress)
        {
            if (float.IsNaN(staggerProgress)) return 0f;
            return Mathf.Clamp01(1f - staggerProgress);
        }
    }
}
