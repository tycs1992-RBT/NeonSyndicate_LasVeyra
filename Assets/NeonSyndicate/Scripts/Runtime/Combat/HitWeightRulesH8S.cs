using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How much a kill by the player matters for its confirmation.</summary>
    public enum KillTierH8S
    {
        /// <summary>No kill.</summary>
        None,
        /// <summary>An ordinary enemy or creature.</summary>
        Normal,
        /// <summary>An elite, a champion or a Shard-Touched creature: the heavy confirmation.</summary>
        Elite,
        /// <summary>A boss or great beast: its own death sequence is the confirmation.</summary>
        Boss
    }

    /// <summary>
    /// H8S hit weight rules (engine-free, tested offline): when one of the player's hits earns a short impact freeze
    /// and how a kill is confirmed. Melee keeps its own freeze (PlayerCombat) and a Resonance Burst its own
    /// (ResonanceSystem); this adds weight to the hits that had none: guns, crystal foci, powers and blasts. Only a kill
    /// or a genuinely heavy single hit freezes, at most once every MinStopGap seconds, so a fast gun never stutters, and
    /// reduced effects shrink every freeze to the same 35 percent the melee freeze uses. Kill confirmations in quick
    /// succession climb in pitch (a streak) so several kills read as several.
    /// </summary>
    public static class HitWeightRulesH8S
    {
        /// <summary>Shortest unscaled time between two feedback freezes.</summary>
        public const float MinStopGap = 0.25f;
        /// <summary>Kills closer together than this (unscaled seconds) continue a streak.</summary>
        public const float StreakWindow = 0.6f;
        /// <summary>Most steps a streak climbs (two semitones each).</summary>
        public const int MaxStreakSteps = 4;
        /// <summary>Kills closer together than this share one chime (an explosion's kills ring once, not in a pile).</summary>
        public const float ChimeGap = 0.05f;
        /// <summary>The longest freeze this rule ever asks for.</summary>
        public const float MaxStop = 0.08f;
        /// <summary>Reduced effects keep this share of a freeze (the melee freeze's own reduced-effects scale).</summary>
        public const float ReducedScale = 0.35f;
        /// <summary>Share of the target's health one hit must take to count as heavy.</summary>
        public const float HeavyShare = 0.25f;
        /// <summary>Share of the target's health a critical must take to earn a small freeze.</summary>
        public const float CritShare = 0.12f;

        /// <summary>The kill tier of a hit's outcome.</summary>
        public static KillTierH8S KillTier(bool killed, bool elite, bool bossLike)
        {
            if (!killed) return KillTierH8S.None;
            if (bossLike) return KillTierH8S.Boss;
            return elite ? KillTierH8S.Elite : KillTierH8S.Normal;
        }

        /// <summary>
        /// Seconds of impact freeze for one of the player's hits: none for melee and bursts (they have their own), none
        /// for a boss kill (its death sequence slows the world), 0.06 for an elite kill, 0.03 for a kill, 0.04 for a single
        /// hit that takes a quarter of the target's health, 0.025 for a critical that takes an eighth; none at all within
        /// MinStopGap of the last freeze. share is the hit's damage over the target's maximum health.
        /// </summary>
        public static float HitStop(DamageType type, bool burst, float share, bool crit, KillTierH8S kill, bool reducedEffects, float sinceLastStop)
        {
            if (type == DamageType.Melee || burst) return 0f;
            if (!(sinceLastStop >= MinStopGap)) return 0f;
            float s;
            switch (kill)
            {
                case KillTierH8S.Boss: return 0f;
                case KillTierH8S.Elite: s = 0.06f; break;
                case KillTierH8S.Normal: s = 0.03f; break;
                default:
                    if (share >= HeavyShare) s = 0.04f;
                    else if (crit && share >= CritShare) s = 0.025f;
                    else s = 0f;
                    break;
            }
            if (reducedEffects) s *= ReducedScale;
            return Mathf.Clamp(s, 0f, MaxStop);
        }

        /// <summary>The streak after a kill: one more step when the last kill was within StreakWindow, else back to 0.</summary>
        public static int NextStreak(int streak, float sinceLastKill)
        {
            if (!(sinceLastKill <= StreakWindow)) return 0;
            return Mathf.Clamp(streak + 1, 0, MaxStreakSteps);
        }

        /// <summary>
        /// One kill's confirmation: true when it chimes, with the streak it leaves in nextStreak. A kill within ChimeGap
        /// of the last chime shares that chime and leaves the streak as it was (simultaneous kills never stack into a
        /// chord); a later kill steps the streak when it came within StreakWindow of the last kill (NextStreak), so only
        /// later kills ring higher, and the first kill of a run rings at the base pitch.
        /// </summary>
        public static bool KillChime(int streak, float sinceLastChime, float sinceLastKill, out int nextStreak)
        {
            if (sinceLastChime < ChimeGap)
            {
                nextStreak = streak;
                return false;
            }
            nextStreak = NextStreak(streak, sinceLastKill);
            return true;
        }

        /// <summary>The kill confirmation's pitch for a streak step: 1, then two semitones higher per step.</summary>
        public static float StreakPitch(int streak)
        {
            return Mathf.Pow(2f, Mathf.Clamp(streak, 0, MaxStreakSteps) * 2f / 12f);
        }
    }
}
