using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Comfortable approach and turning without changing saved companion speed or evolution fields.</summary>
    public static class CompanionMotion04G
    {
        public static void ResetStatics() { }
        public static float Arrival(float distance) { return Mathf.Clamp(distance / 1.4f, 0.15f, 1f); }
        public static float TurnSpeed(float degrees) { return Mathf.Lerp(1f, 0.28f, Mathf.Clamp01((degrees - 45f) / 105f)); }
        public static float SupportRecovery(int rank, int tuning)
        {
            return rank >= BondIds.RankHarmonized ? Mathf.Clamp(1f - 0.04f * Mathf.Clamp(tuning, 0, 3), 0.88f, 1f) : 1f;
        }
    }
}
