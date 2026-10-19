using UnityEngine;

namespace NeonSyndicate
{
    public static class VanguardRules04K
    {
        public const string Quest = "uh05_04k_01_fractured_bell";
        public const string Arena = "Under:workshops.convoy_obstacle";
        public const string Victory = "uh05_04k_bell_defeated";
        public const string Reward = "uh05_04k_recovered_caster";
        public static bool Authorized(string quest,string at) { return quest==Quest && at==Arena; }
        public static bool Enraged(float fraction) { return !float.IsNaN(fraction) && fraction<=.45f; }
        public static AttackTiming Timing(AttackTiming original,float fraction)
        {
            // Faster second movement still has a long readable tell and a guaranteed punish window.
            original.Windup=Mathf.Max(Enraged(fraction)?.85f:1.05f,original.Windup);
            original.Recovery=Mathf.Max(Enraged(fraction)?1.05f:1.3f,original.Recovery);
            return original;
        }
        public static float ExposedMultiplier(int phase) { return phase==2?1.35f:1f; }
    }
}
