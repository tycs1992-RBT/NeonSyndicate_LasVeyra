using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S impact cadence (engine-free, tested offline): the planned landing moments of enemy blows on the player, so
    /// that two blows never land closer together than the difficulty's gap. The attack tokens already limit how many
    /// enemies may swing at once; this spaces out when their blows arrive, so each can be read and answered on its own
    /// (a parry, a guard, a dodge) instead of two landing in the same instant behind one "!". Fixed capacity, no
    /// allocation after construction. Owners are plain ids (the runtime uses instance ids).
    /// </summary>
    public sealed class ImpactCadenceH8S
    {
        /// <summary>Most planned blows remembered at once (the earliest is forgotten when a new one arrives on a full book).</summary>
        public const int Capacity = 24;
        /// <summary>Seconds a landed blow is still remembered (longer than the widest gap).</summary>
        public const float Memory = 1f;

        private readonly int[] _owner = new int[Capacity];
        private readonly float[] _at = new float[Capacity];
        private int _count;

        /// <summary>Planned or recently landed blows in the book.</summary>
        public int Count { get { return _count; } }

        /// <summary>The shortest time between two blows landing on the player: Story 0.55 s, Balanced 0.35 s, Veteran 0.22 s.</summary>
        public static float MinGap(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 0.55f : (d == DifficultyLevel.Balanced ? 0.35f : 0.22f);
        }

        /// <summary>Forgets blows that landed more than Memory seconds before now.</summary>
        public void Prune(float now)
        {
            for (int i = _count - 1; i >= 0; i--)
                if (_at[i] < now - Memory) RemoveAt(i);
        }

        /// <summary>True when a blow by owner landing at impactAt keeps the gap to every other planned blow (owner's own plan is ignored).</summary>
        public bool Fits(int owner, float impactAt, float gap)
        {
            if (float.IsNaN(impactAt) || float.IsInfinity(impactAt)) return false;
            for (int i = 0; i < _count; i++)
            {
                if (_owner[i] == owner) continue;
                if (Mathf.Abs(_at[i] - impactAt) < gap) return false;
            }
            return true;
        }

        /// <summary>True when impactAt keeps the gap to the first count landing times in times (blows seen live, not planned here).</summary>
        public static bool FitsAmong(float[] times, int count, float impactAt, float gap)
        {
            if (float.IsNaN(impactAt) || float.IsInfinity(impactAt)) return false;
            if (times == null) return true;
            int n = Mathf.Min(count, times.Length);
            for (int i = 0; i < n; i++)
                if (Mathf.Abs(times[i] - impactAt) < gap) return false;
            return true;
        }

        /// <summary>
        /// Plans owner's blow at impactAt (replacing an earlier plan of the same owner). The caller asks Fits first; a
        /// full book forgets its earliest blow. Broken times are ignored.
        /// </summary>
        public void Reserve(int owner, float now, float impactAt)
        {
            if (float.IsNaN(impactAt) || float.IsInfinity(impactAt)) return;
            Prune(now);
            for (int i = 0; i < _count; i++)
            {
                if (_owner[i] != owner) continue;
                _at[i] = impactAt;
                return;
            }
            if (_count >= Capacity) RemoveAt(IndexOfEarliest());
            _owner[_count] = owner;
            _at[_count] = impactAt;
            _count++;
        }

        /// <summary>Fits, then Reserve. False (and nothing changes) when the blow would land too close to another.</summary>
        public bool TryReserve(int owner, float now, float impactAt, float gap)
        {
            Prune(now);
            if (!Fits(owner, impactAt, gap)) return false;
            Reserve(owner, now, impactAt);
            return true;
        }

        /// <summary>Forgets owner's plan (its blow was broken before it landed).</summary>
        public void Release(int owner)
        {
            for (int i = _count - 1; i >= 0; i--)
                if (_owner[i] == owner) RemoveAt(i);
        }

        /// <summary>
        /// The earliest landing at or after impactAt that keeps the gap to every planned blow except owner's (a hair past
        /// the gap, so float rounding can never leave it a whisker inside).
        /// </summary>
        public float NextFit(int owner, float impactAt, float gap)
        {
            float t = impactAt;
            for (int pass = 0; pass <= Capacity; pass++)
            {
                bool moved = false;
                for (int i = 0; i < _count; i++)
                {
                    if (_owner[i] == owner) continue;
                    if (Mathf.Abs(_at[i] - t) < gap)
                    {
                        t = _at[i] + gap + 1e-4f;
                        moved = true;
                    }
                }
                if (!moved) return t;
            }
            return t;
        }

        /// <summary>The planned landing time of owner's blow, or false when it has none.</summary>
        public bool PlanOf(int owner, out float impactAt)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_owner[i] != owner) continue;
                impactAt = _at[i];
                return true;
            }
            impactAt = 0f;
            return false;
        }

        /// <summary>Forgets everything (new game, load).</summary>
        public void Clear()
        {
            _count = 0;
        }

        private int IndexOfEarliest()
        {
            int best = 0;
            for (int i = 1; i < _count; i++) if (_at[i] < _at[best]) best = i;
            return best;
        }

        private void RemoveAt(int i)
        {
            _count--;
            _owner[i] = _owner[_count];
            _at[i] = _at[_count];
        }
    }
}
