using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One point of the player's breadcrumb trail.</summary>
    public struct TrailCrumb
    {
        public Vector3 P;
        /// <summary>The player was off the ground here (a jump, a glide, flight).</summary>
        public bool Air;
        /// <summary>The point lies in damaging heat (ground companions skip it).</summary>
        public bool Hazard;
        /// <summary>Trail segment: a teleport or fast travel starts a new one, and nothing follows across a break.</summary>
        public int Segment;
        /// <summary>Increasing id, so a follower keeps its place when the ring overwrites old points.</summary>
        public int Id;
    }

    /// <summary>
    /// The player's breadcrumb trail: a bounded ring of points spaced along where the player actually walked, so a
    /// companion that follows it goes through the same doors, stairwells and corridors instead of cutting through
    /// walls. A jump larger than BreakDistance between samples (a teleport, a lift, fast travel) starts a new segment.
    /// Line tests are supplied by the caller, so the selection logic stays engine-free and tested.
    /// </summary>
    public sealed class CompanionTrail
    {
        public const int Capacity = 160;
        public const float Spacing = 0.75f;
        public const float BreakDistance = 10f;

        private readonly TrailCrumb[] _buf = new TrailCrumb[Capacity];
        private int _start;
        private int _count;
        private int _segment;
        private int _nextId = 1;

        public int Count { get { return _count; } }
        public int Segment { get { return _segment; } }

        /// <summary>The crumb at an index (0 is the oldest, Count - 1 the newest).</summary>
        public TrailCrumb this[int i] { get { return _buf[(_start + Mathf.Clamp(i, 0, Mathf.Max(0, _count - 1))) % Capacity]; } }

        public bool HasNewest { get { return _count > 0; } }
        public TrailCrumb Newest { get { return this[_count - 1]; } }

        /// <summary>Forgets every point and starts a new segment.</summary>
        public void Clear()
        {
            _start = 0;
            _count = 0;
            _segment++;
        }

        /// <summary>Adds the player's position when they moved at least Spacing (3D). True when a crumb was added.</summary>
        public bool Record(Vector3 p, bool air, bool hazard)
        {
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) return false;
            if (_count > 0)
            {
                Vector3 last = Newest.P;
                float d = (p - last).magnitude;
                if (d > BreakDistance) Clear();
                else if (d < Spacing) return false;
            }
            if (_count == Capacity)
            {
                _start = (_start + 1) % Capacity;
                _count--;
            }
            _buf[(_start + _count) % Capacity] = new TrailCrumb { P = p, Air = air, Hazard = hazard, Segment = _segment, Id = _nextId++ };
            _count++;
            return true;
        }

        /// <summary>The index of the crumb with an id, or -1 when it has been overwritten or never existed.</summary>
        public int IndexOfId(int id)
        {
            if (_count == 0) return -1;
            int idx = id - this[0].Id;
            return idx >= 0 && idx < _count ? idx : -1;
        }

        /// <summary>Distance with height counted double (a crumb one floor away is far even if it is right above).</summary>
        public static float Weighted(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = (a.y - b.y) * 2f, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>The index of the crumb nearest to p within maxDistance (weighted), or -1.</summary>
        public int NearestIndex(Vector3 p, float maxDistance)
        {
            int best = -1;
            float bd = maxDistance;
            for (int i = 0; i < _count; i++)
            {
                float d = Weighted(this[i].P, p);
                if (d <= bd)
                {
                    bd = d;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>The first index at or after from that is not in heat, looking at most maxLook crumbs ahead (-1 if none).</summary>
        public int NextSafeIndex(int from, int maxLook)
        {
            for (int i = Mathf.Max(0, from); i < _count && i < from + maxLook; i++)
                if (!this[i].Hazard) return i;
            return -1;
        }

        /// <summary>
        /// Bounded repath: the furthest crumb ahead of startIndex that the follower can reach in a straight line from
        /// 'from' (tested newest first, at most maxProbes line tests). -1 when none of the probed crumbs is clear.
        /// </summary>
        public int FurthestClear(Vector3 from, int startIndex, int maxProbes, Func<Vector3, Vector3, bool> clear)
        {
            if (clear == null || _count == 0 || maxProbes <= 0) return -1;
            int lo = Mathf.Clamp(startIndex, 0, _count - 1);
            int span = _count - 1 - lo;
            int probes = Mathf.Min(maxProbes, span + 1);
            for (int k = 0; k < probes; k++)
            {
                int idx = probes <= 1 ? _count - 1 : _count - 1 - Mathf.RoundToInt((float)k * span / (probes - 1));
                if (idx < lo) break;
                TrailCrumb c = this[idx];
                if (c.Hazard) continue;
                if (clear(from, c.P)) return idx;
            }
            return -1;
        }
    }
}
