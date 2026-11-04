using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free rules for children of the city (owner request 2026-10-24: a boy, a girl and an old woman as NPCs):
    /// how many pedestrians are families or elders, where a child walks beside the grown-up, how fast it goes to keep
    /// up, and when it gives up and goes home. Pure numbers, tested offline (FamilyRulesTests).
    /// </summary>
    public static class FamilyRules
    {
        /// <summary>Of new pedestrians: the share that is a grown-up walking with a child, and the share that is an elder.</summary>
        public const double FamilyShare = 0.12, ElderShare = 0.14;
        /// <summary>The child walks this far to the side of the grown-up and this far behind.</summary>
        public const float SideOffset = 0.75f, BackOffset = 0.25f;
        /// <summary>Within this distance of its spot the child stands still; beyond Run it runs to catch up.</summary>
        public const float Settle = 0.35f, Run = 3.5f;
        /// <summary>Farther than this from the grown-up and out of sight, the child is put back beside them.</summary>
        public const float Rejoin = 20f;

        /// <summary>The spot beside a grown-up (position, flat forward, flat right) where the child walks, on the right.</summary>
        public static Vector3 Spot(Vector3 guardian, Vector3 forward, Vector3 right)
        {
            return guardian + right * SideOffset - forward * BackOffset;
        }

        /// <summary>
        /// The child's speed toward its spot: zero when it is there, the grown-up's own speed (a little more, so it closes
        /// the gap) when near, its run speed when it fell behind.
        /// </summary>
        public static float Speed(float distanceToSpot, float guardianSpeed, float walkSpeed, float runSpeed)
        {
            if (float.IsNaN(distanceToSpot) || distanceToSpot <= Settle) return 0f;
            if (distanceToSpot > Run) return runSpeed;
            return Mathf.Clamp(Mathf.Max(guardianSpeed * 1.15f, walkSpeed * 0.6f), 0f, runSpeed);
        }

        /// <summary>What a new pedestrian is from one random number in [0, 1): a family (a grown-up with a child), an elder or neither.</summary>
        public static AgeClass Pick(double roll, out bool family)
        {
            family = roll < FamilyShare;
            if (family) return AgeClass.Adult;
            return roll < FamilyShare + ElderShare ? AgeClass.Elder : AgeClass.Adult;
        }
    }
}
