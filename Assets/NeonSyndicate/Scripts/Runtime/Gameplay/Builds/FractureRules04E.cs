using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Fracture Lattice focus module (the Vaultbreaker's reward): a crystal focus fitted with it splits secondary
    /// fracture shards off its primary hits toward other foes it can see near the impact. It changes how the focus fires
    /// (one shot now reaches a cluster) rather than adding a flat percentage. Limits: one fracture burst per Interval per
    /// focus, shards never fracture again (no chain), only direct hits fracture (never the shards, blasts or burns), each
    /// shard needs a clear line from the impact point. Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class FractureRules04E
    {
        /// <summary>Seconds between fracture bursts of one focus (the beam's 0.1 s ticks fracture about three times a second).</summary>
        public const float Interval = 0.35f;
        /// <summary>Shards reach foes within this many metres of the impact.</summary>
        public const float Range = 7f;
        /// <summary>Each shard deals this share of the damage the primary hit actually dealt.</summary>
        public const float Share = 0.4f;
        /// <summary>With no other foe in reach, one shard re-strikes the same target for this share, less often.</summary>
        public const float SelfShare = 0.25f;
        public const float SelfInterval = 0.7f;
        /// <summary>A hit smaller than this never fractures (a glancing tick).</summary>
        public const float MinDealt = 1f;

        /// <summary>Shards per burst: two, three from Legendary up.</summary>
        public static int ShardCount(Rarity r)
        {
            return r >= Rarity.Legendary ? 3 : 2;
        }

        /// <summary>True when this hit may start a fracture burst.</summary>
        public static bool Triggers(bool fitted, bool reentrant, DamageType type, float dealt, bool hostileLivingTarget, float now, float lastBurst)
        {
            if (!fitted || reentrant || !hostileLivingTarget) return false;
            if (type != DamageType.Bullet) return false;
            if (!(dealt >= MinDealt)) return false;
            return now - lastBurst >= Interval;
        }

        /// <summary>Damage of one shard from what the primary hit dealt.</summary>
        public static float ShardDamage(float dealt, bool self)
        {
            return Mathf.Max(0f, dealt) * (self ? SelfShare : Share);
        }

        /// <summary>
        /// The foes the shards go to: indices into candidates, nearest first, only visible ones within Range of the impact,
        /// never the primary target (exclude), at most max. Ties keep candidate order.
        /// </summary>
        public static List<int> PickTargets(Vector3 impact, IList<Vector3> candidates, IList<bool> visible, int exclude, int max, List<int> into)
        {
            List<int> r = into ?? new List<int>();
            r.Clear();
            if (candidates == null || max <= 0) return r;
            float range2 = Range * Range;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i == exclude) continue;
                if (visible != null && (i >= visible.Count || !visible[i])) continue;
                float d = (candidates[i] - impact).sqrMagnitude;
                if (d > range2) continue;
                int at = r.Count;
                while (at > 0 && (candidates[r[at - 1]] - impact).sqrMagnitude > d) at--;
                r.Insert(at, i);
                if (r.Count > max) r.RemoveAt(r.Count - 1);
            }
            return r;
        }

        /// <summary>One line for the item card and the BUILD page.</summary>
        public static string Line(Rarity r)
        {
            return "Fracture Lattice: direct hits split " + ShardCount(r) + " shards (" + Mathf.RoundToInt(Share * 100f) + "% each) into other foes you can see within " +
                   Mathf.RoundToInt(Range) + " m, at most every " + Interval.ToString("0.##") + " s. A lone target takes one " + Mathf.RoundToInt(SelfShare * 100f) + "% re-strike instead.";
        }
    }
}
