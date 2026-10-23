using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S boss phase beat rules (engine-free, tested offline). When an upgraded boss (the Foreman, the Djinn, the Queen)
    /// opens a phase it now behaves like the regional guardians already did: it roars and holds its own swings for a
    /// moment the player can watch, rings that were scheduled but never shown are dropped (nothing new appears under the
    /// player during the roar; rings already on the floor still burst, they were telegraphed), supports and mirror copies
    /// raised mid-fight wait a moment before their first swing, and a banner names the phase. The Glacier Titan keeps its
    /// original script (its kneel already makes its phase change safe) and only gets the banner.
    /// </summary>
    public static class BossPhaseRulesH8S
    {
        /// <summary>Seconds the phase banner stays on screen.</summary>
        public const float BannerSeconds = 2.4f;
        /// <summary>Seconds the banner takes to appear.</summary>
        public const float BannerFadeIn = 0.18f;
        /// <summary>Seconds the banner takes to fade away.</summary>
        public const float BannerFadeOut = 0.55f;
        /// <summary>The upgraded fights wait this long after a phase opens before their next scripted move (EnterPhase04D).</summary>
        public const float ScriptedPhaseGap = 3f;

        /// <summary>
        /// Seconds an upgraded boss holds its own melee swings while a phase opens: Story 2.4, Balanced 2.0, Veteran 1.6.
        /// Always shorter than the scripted gap, so its next move still comes after it can swing again.
        /// </summary>
        public static float HoldSeconds(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 2.4f : (d == DifficultyLevel.Balanced ? 2f : 1.6f);
        }

        /// <summary>Seconds a support or mirror copy raised mid-fight waits before its first swing: Story 1.2, Balanced 0.9, Veteran 0.7.</summary>
        public static float ArrivalGrace(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 1.2f : (d == DifficultyLevel.Balanced ? 0.9f : 0.7f);
        }

        /// <summary>
        /// True for a scheduled ring that goes when a phase opens: it has not appeared yet (so it was never telegraphed)
        /// and it is not a support arrival (an arrival is the phase's own event).
        /// </summary>
        public static bool DropsAtPhase(bool shown, bool arrival)
        {
            return !shown && !arrival;
        }

        /// <summary>
        /// Removes from pending, in place and keeping the order of the rest, every scheduled ring that DropsAtPhase
        /// (never shown, not an arrival), handing each one to dropped first (to release its ground mark). Returns how many
        /// went. CreatureBoss runs its hazard list through this when a phase opens.
        /// </summary>
        public static int DropUnshown<T>(System.Collections.Generic.List<T> pending, System.Predicate<T> shown, System.Predicate<T> arrival, System.Action<T> dropped)
        {
            if (pending == null || shown == null || arrival == null) return 0;
            int n = 0;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                T h = pending[i];
                if (!DropsAtPhase(shown(h), arrival(h))) continue;
                if (dropped != null) dropped(h);
                pending.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>The banner's opacity at age seconds of a banner that lives life seconds (fades in, holds, fades out).</summary>
        public static float BannerAlpha(float age, float life)
        {
            if (!(age >= 0f) || age >= life) return 0f;
            float a = Mathf.Clamp01(age / BannerFadeIn);
            float left = life - age;
            return Mathf.Min(a, Mathf.Clamp01(left / BannerFadeOut));
        }

        /// <summary>The banner title for a phase index (0 based) of a fight with phaseCount phases, for example "PHASE 2 / 3".</summary>
        public static string PhaseTitle(int phaseIndex, int phaseCount)
        {
            int count = Mathf.Max(1, phaseCount);
            int shown = Mathf.Clamp(phaseIndex, 0, count - 1) + 1;
            return "PHASE " + shown + " / " + count;
        }
    }
}
