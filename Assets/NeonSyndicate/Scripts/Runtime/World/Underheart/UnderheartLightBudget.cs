using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// How many Underheart practical lights stay on near the view (pure, tested offline by UnderheartLightBudget04ETests).
    /// The region keeps its nearest 8 lights on (5 on the Low preset), all shadowless. While an Underheart boss fight is
    /// armed or running and its arena is near the view, the region keeps one fewer (7, or 4 on Low), so together with the
    /// fight's single key light (BossLightBudget04E) the total stays at the region's own budget. The full budget returns
    /// as soon as the fight ends, fails or resets, or the view leaves the arena.
    /// </summary>
    public static class UnderheartLightBudget
    {
        /// <summary>Practical lights on near the view on every preset but Low.</summary>
        public const int Region = 8;
        /// <summary>Practical lights on near the view on the Low preset.</summary>
        public const int RegionLow = 5;
        /// <summary>Lights a nearby boss fight takes from the region (its own key light takes their place).</summary>
        public const int BossShare = 1;
        /// <summary>The arena counts as near within its radius plus this margin (flat metres).</summary>
        public const float ArenaMargin = 20f;

        /// <summary>The number of nearest practical lights to keep on (never below one).</summary>
        public static int Budget(bool lowPreset, bool bossArenaNear)
        {
            int b = lowPreset ? RegionLow : Region;
            return bossArenaNear ? Mathf.Max(1, b - BossShare) : b;
        }

        /// <summary>
        /// True while a boss fight is active and the view is within its arena radius plus ArenaMargin (flat) and within
        /// WorldBounds.VerticalBand of the arena floor. Bad numbers never count as near.
        /// </summary>
        public static bool ArenaNear(bool fightActive, Vector3 view, Vector3 arenaCenter, float arenaRadius)
        {
            if (!fightActive || !MathUtil.IsFinite(view) || !MathUtil.IsFinite(arenaCenter) || float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius)) return false;
            float reach = Mathf.Max(0f, arenaRadius) + ArenaMargin;
            float dx = view.x - arenaCenter.x, dz = view.z - arenaCenter.z;
            if (dx * dx + dz * dz > reach * reach) return false;
            return Mathf.Abs(view.y - arenaCenter.y) <= WorldBounds.VerticalBand;
        }
    }
}
