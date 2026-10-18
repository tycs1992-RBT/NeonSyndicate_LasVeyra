using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Legal recovery boundaries and audible weapon weight. No animation or invulnerability is skipped.</summary>
    public static class CombatResponse04G
    {
        public static void ResetStatics() { }
        public const float DodgeBuffer = 0.18f;

        public static float DefenseTime(MeleeMove move, MeleeClass style)
        {
            if (move == null) return float.PositiveInfinity;
            float recovery = style == MeleeClass.Maul ? 0.65f : style == MeleeClass.Blade ? 0.12f : 0.32f;
            return move.Windup + move.Active + move.Recovery * recovery;
        }

        public static bool CanDefend(MeleeMove move, float elapsed, MeleeClass style)
        {
            return move != null && elapsed >= DefenseTime(move, style);
        }

        public static float SwingPitch(MeleeClass style)
        {
            return style == MeleeClass.Maul ? 0.65f : style == MeleeClass.Blade ? 1.4f : 1f;
        }
    }
}
