using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the live impact cadence for the player (owner: the combat director's area; reset from
    /// CombatDirector.ResetStatics). Creatures plan their landing moment when a blow starts and give it back when the blow
    /// is broken; people and humanoid creatures are read live from their wind-ups (NpcController.MeleeImpactInH8S), so a
    /// staggered swing frees its moment at once. A blow that would land within the difficulty's gap of another waits a
    /// beat (RetrySeconds) while its attacker keeps circling. No allocation: one fixed book and one fixed scratch array.
    /// </summary>
    public static class CombatCadenceH8S
    {
        /// <summary>Seconds an attacker waits before trying again when its blow would land on top of another.</summary>
        public const float RetrySeconds = 0.15f;

        private static readonly ImpactCadenceH8S _book = new ImpactCadenceH8S();
        private static readonly float[] _people = new float[16];
        private static int _waits;
        private static int _plans;

        /// <summary>Chained from CombatDirector.ResetStatics.</summary>
        public static void ResetStatics()
        {
            _book.Clear();
            _waits = 0;
            _plans = 0;
            System.Array.Clear(_people, 0, _people.Length);
        }

        /// <summary>The gap between two blows on the player at the current difficulty.</summary>
        public static float Gap { get { return ImpactCadenceH8S.MinGap(Settings.Difficulty); } }

        /// <summary>Blows that had to wait a beat since the session started (debug page).</summary>
        public static int Waits { get { return _waits; } }

        /// <summary>Blows planned since the session started (debug page).</summary>
        public static int Plans { get { return _plans; } }

        /// <summary>Planned or just-landed creature blows in the book now (debug page).</summary>
        public static int Booked { get { return _book.Count; } }

        /// <summary>
        /// True when owner's blow, landing impactIn seconds from now, keeps the gap to every planned creature blow and to
        /// every person's or humanoid creature's swing winding up on the player. Counts a wait when false.
        /// </summary>
        public static bool Fits(Object owner, float impactIn)
        {
            if (owner == null) return true;
            float now = Time.time;
            float at = now + Mathf.Max(0f, impactIn);
            float gap = Gap;
            _book.Prune(now);
            bool ok = _book.Fits(owner.GetInstanceID(), at, gap) && ImpactCadenceH8S.FitsAmong(_people, ScanSwings(owner, now), at, gap);
            if (!ok) _waits++;
            return ok;
        }

        /// <summary>Plans owner's blow to land impactIn seconds from now (call after Fits, when the blow starts).</summary>
        public static void Reserve(Object owner, float impactIn)
        {
            if (owner == null) return;
            float now = Time.time;
            _book.Reserve(owner.GetInstanceID(), now, now + Mathf.Max(0f, impactIn));
            _plans++;
        }

        /// <summary>Gives owner's planned moment back (its blow was broken before it landed).</summary>
        public static void Release(Object owner)
        {
            if (owner != null) _book.Release(owner.GetInstanceID());
        }

        /// <summary>Landing times of the melee swings winding up on the player right now, people and humanoid creatures alike.</summary>
        private static int ScanSwings(Object except, float now)
        {
            int n = 0;
            for (int i = 0; i < NpcController.All.Count && n < _people.Length; i++)
            {
                NpcController c = NpcController.All[i];
                if (c == null || c == except) continue;
                float s;
                if (!c.MeleeImpactInH8S(out s)) continue;
                _people[n++] = now + s;
            }
            return n;
        }
    }
}
