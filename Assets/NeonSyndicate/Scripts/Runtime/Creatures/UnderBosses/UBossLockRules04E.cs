using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: engine-free rules for locking on to an Underheart boss. The lock point is the part kind that takes the most
    /// damage right now by the boss's own part table (UnderBossProfiles.PartMultiplier with the fight's live state and
    /// any open punish window), so it moves to the exposed weak point when a window opens and back to the body (or to a
    /// standing weak point such as the Valve Guardian's wheel) afterwards. Among several colliders of that kind the one
    /// nearest the camera's aim is chosen, and the current one is kept unless another is clearly nearer the aim, so the
    /// reticle does not jitter between two seams.
    /// </summary>
    public static class UBossLockRules04E
    {
        /// <summary>A part kind must beat the current best by this much to win (ties keep the plain body).</summary>
        public const float MultiplierMargin = 0.01f;
        /// <summary>The current collider stays chosen while it is within this many degrees of the best one.</summary>
        public const float KeepAngle = 10f;
        /// <summary>Seconds between re-choosing the lock point.</summary>
        public const float Interval = 0.2f;
        /// <summary>Score bonus of a boss in lock-on scoring (lower wins; the same as an engaged creature).</summary>
        public const float LockBonus = 28f;
        /// <summary>The pad aim assist and the lock-on reach this far for a boss lock point (metres, like a creature).</summary>
        public const float AimAssistRange = 70f;

        /// <summary>
        /// The best part kind to strike among those present (enabled hit colliders). Body wins ties so the lock does not
        /// wander to a part that is no better than the hull.
        /// </summary>
        public static UBossPart BestKind(string bossId, IList<UBossPart> present, UBossDamageState state)
        {
            UBossPart best = UBossPart.Body;
            float bestMul = UnderBossProfiles.PartMultiplier(bossId, UBossPart.Body, state);
            if (present == null) return best;
            for (int i = 0; i < present.Count; i++)
            {
                UBossPart p = present[i];
                if (p == UBossPart.Body) continue;
                float m = UnderBossProfiles.PartMultiplier(bossId, p, state);
                if (m > bestMul + MultiplierMargin)
                {
                    bestMul = m;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>True when the current collider (at currentAngle from the aim) should stay chosen over the best one.</summary>
        public static bool KeepCurrent(float currentAngle, float bestAngle)
        {
            return currentAngle <= bestAngle + KeepAngle;
        }

        /// <summary>The lock radius of a body from the farthest flat reach of its hit volumes (metres), kept sensible.</summary>
        public static float Radius(float farthestFlatReach)
        {
            if (float.IsNaN(farthestFlatReach)) return 1f;
            return Mathf.Clamp(farthestFlatReach, 0.8f, 8f);
        }

        /// <summary>The body height for camera framing from the top of its hit volumes above its feet (metres).</summary>
        public static float Height(float topAboveFeet)
        {
            if (float.IsNaN(topAboveFeet)) return 2f;
            return Mathf.Clamp(topAboveFeet, 1f, 14f);
        }

        /// <summary>
        /// Lock-on score of a boss lock point (the same formula as a creature: angle from the view, distance to the body's
        /// edge, a bonus for a fight). Lower wins.
        /// </summary>
        public static float Score(float angleDegrees, float distanceToEdge)
        {
            return angleDegrees * 0.6f + distanceToEdge * 1.2f - LockBonus;
        }
    }
}
