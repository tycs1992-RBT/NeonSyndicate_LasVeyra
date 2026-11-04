using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the creature attack timing as it is played, with the tell rules on top of the original Timing: the wind-up
    /// never drops below its class minimum for the difficulty (CombatTellsH8S.MinWindup), and a blow that covers an area
    /// (a slam, an eruption, an all-round sweep) always leaves time to get out of it with one dodge and a few steps.
    /// Active and recovery are untouched, and so is damage. Pure maths (tested offline).
    /// </summary>
    public static partial class CreatureAttackDb
    {
        /// <summary>
        /// Timing(a, windupScale, size) with the H8S floors for difficulty d. bodyRadius is the creature's radius and
        /// meleeScale its broken-part melee scale (1 when whole), which set how deep the player can stand in a sweep or
        /// a ring around the body.
        /// </summary>
        public static AttackTiming TimingH8S(CreatureAttack a, float windupScale, float size, DifficultyLevel d, float bodyRadius, float meleeScale)
        {
            if (a == null) return new AttackTiming { Windup = MinWindup, Active = 0.05f, Recovery = 0.2f };
            AttackTiming t = Timing(a, windupScale, size);
            t.Windup = Mathf.Max(t.Windup, CombatTellsH8S.CreatureWindupFloor(a, d, bodyRadius, meleeScale));
            return t;
        }
    }
}
