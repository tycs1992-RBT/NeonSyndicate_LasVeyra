using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a quiet lease is for.</summary>
    public enum QuietKind
    {
        /// <summary>A running encounter owns its area: other automatic aggressive producers stay out until it ends.</summary>
        Admission,
        /// <summary>The quiet aftermath of a real victory: a timer of active play plus the player's departure.</summary>
        Aftermath
    }

    /// <summary>One owner's hold on a local area. Engine-free; the ledger advances it with active play seconds.</summary>
    public sealed class QuietLease
    {
        public string Owner;
        public QuietKind Kind;
        public Vector3 Center;
        public float Radius;
        /// <summary>Active play seconds the aftermath lasts at least (unused for admission leases).</summary>
        public float Duration;
        public float Elapsed;
        /// <summary>The player has to be beyond this distance before an expired aftermath lets the area wake.</summary>
        public float DepartRadius;
        public bool PlayerOutside;

        public float Remaining { get { return Mathf.Max(0f, Duration - Elapsed); } }

        /// <summary>An aftermath lease is over once its time ran out and the player has left; admission leases never expire.</summary>
        public bool Expired { get { return Kind == QuietKind.Aftermath && Elapsed >= Duration && PlayerOutside; } }

        public bool Covers(Vector3 p)
        {
            float x = p.x - Center.x;
            float z = p.z - Center.z;
            return x * x + z * z <= Radius * Radius;
        }
    }

    /// <summary>
    /// Owner-keyed local quiet leases (engine-free). Acquiring an owner that already holds a lease refreshes it instead
    /// of stacking; releasing is idempotent and only ever touches the caller's own key, so nested or overlapping owners
    /// cannot clear each other. The count of live leases is the reference count. Paused time never reaches Tick.
    /// </summary>
    public sealed class QuietLedger
    {
        private readonly List<QuietLease> _leases = new List<QuietLease>();

        public IList<QuietLease> Leases { get { return _leases; } }
        public int Count { get { return _leases.Count; } }

        /// <summary>Takes or refreshes the owner's lease. True when it is new, false when an existing lease was refreshed.</summary>
        public bool Acquire(string owner, QuietKind kind, Vector3 center, float radius, float duration, float departRadius)
        {
            if (string.IsNullOrEmpty(owner)) return false;
            QuietLease l = Find(owner);
            bool fresh = l == null;
            if (fresh)
            {
                l = new QuietLease { Owner = owner };
                _leases.Add(l);
            }
            l.Kind = kind;
            l.Center = new Vector3(center.x, center.y, center.z);
            l.Radius = Mathf.Max(0f, radius);
            l.Duration = Mathf.Max(0f, duration);
            l.Elapsed = 0f;
            l.DepartRadius = Mathf.Max(l.Radius, departRadius);
            l.PlayerOutside = false;
            return fresh;
        }

        /// <summary>Releases the owner's lease. True exactly once; later calls (and unknown owners) return false.</summary>
        public bool Release(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return false;
            for (int i = 0; i < _leases.Count; i++)
            {
                if (_leases[i].Owner != owner) continue;
                _leases.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool Holds(string owner)
        {
            return Find(owner) != null;
        }

        public QuietLease Find(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return null;
            for (int i = 0; i < _leases.Count; i++) if (_leases[i].Owner == owner) return _leases[i];
            return null;
        }

        /// <summary>True when any live lease covers the point (automatic aggressive producers must not spawn there).</summary>
        public bool Blocks(Vector3 p)
        {
            for (int i = 0; i < _leases.Count; i++) if (_leases[i].Covers(p)) return true;
            return false;
        }

        /// <summary>
        /// Advances aftermath leases by active play seconds (callers pass 0 while paused), notes whether the player is
        /// outside each departure radius, and releases the expired ones. Released owners are added to released (may be null).
        /// </summary>
        public int Tick(float activeSeconds, Vector3 player, bool playerKnown, List<string> released)
        {
            float dt = activeSeconds > 0f && !float.IsNaN(activeSeconds) && !float.IsInfinity(activeSeconds) ? activeSeconds : 0f;
            int n = 0;
            for (int i = _leases.Count - 1; i >= 0; i--)
            {
                QuietLease l = _leases[i];
                if (l.Kind != QuietKind.Aftermath) continue;
                l.Elapsed += dt;
                if (playerKnown)
                {
                    float x = player.x - l.Center.x;
                    float z = player.z - l.Center.z;
                    l.PlayerOutside = x * x + z * z > l.DepartRadius * l.DepartRadius;
                }
                if (!l.Expired) continue;
                if (released != null) released.Add(l.Owner);
                _leases.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>Releases every lease of a kind whose owner starts with prefix, except keep (orphan sweep).</summary>
        public int ReleaseKind(QuietKind kind, string prefix, string keep, List<string> released)
        {
            int n = 0;
            for (int i = _leases.Count - 1; i >= 0; i--)
            {
                QuietLease l = _leases[i];
                if (l.Kind != kind || l.Owner == keep) continue;
                if (!string.IsNullOrEmpty(prefix) && !l.Owner.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                if (released != null) released.Add(l.Owner);
                _leases.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>Releases everything (new game, load, title). Completed progression is untouched: only leases go.</summary>
        public int ReleaseAll(List<string> released)
        {
            int n = _leases.Count;
            if (released != null) for (int i = 0; i < _leases.Count; i++) released.Add(_leases[i].Owner);
            _leases.Clear();
            return n;
        }
    }

    /// <summary>
    /// Local quiet zones and admission (owner: encounters). Automatic aggressive producers ask Blocks before spawning;
    /// a lease never removes existing actors, clears a wanted level or makes anyone invulnerable. Engine-free: the
    /// encounter system feeds it active play time and the player's position (paused time excluded).
    /// </summary>
    public static class LocalQuiet
    {
        /// <summary>Radius of a victory's quiet aftermath.</summary>
        public const float QuietRadius = 90f;
        /// <summary>Active play seconds of a victory's quiet aftermath.</summary>
        public const float QuietSeconds = 120f;
        /// <summary>The player must go this far beyond the radius before ambient reactivation.</summary>
        public const float DepartMargin = 30f;

        private static readonly QuietLedger _ledger = new QuietLedger();

        public static void ResetStatics()
        {
            _ledger.ReleaseAll(null);
        }

        /// <summary>True when an automatic aggressive producer must not spawn at this point right now.</summary>
        public static bool Blocks(Vector3 point)
        {
            return _ledger.Blocks(point);
        }

        public static IList<QuietLease> Leases { get { return _ledger.Leases; } }
        /// <summary>The reference count: live leases of every owner.</summary>
        public static int Count { get { return _ledger.Count; } }

        public static bool Holds(string owner) { return _ledger.Holds(owner); }
        public static QuietLease Find(string owner) { return _ledger.Find(owner); }

        /// <summary>Holds an area for a running encounter until its owner releases it.</summary>
        public static bool AcquireAdmission(string owner, Vector3 center, float radius)
        {
            return _ledger.Acquire(owner, QuietKind.Admission, center, radius, 0f, radius);
        }

        /// <summary>Starts (or refreshes) a quiet aftermath: QuietSeconds of active play, then the player has to leave.</summary>
        public static bool AcquireAftermath(string owner, Vector3 center, float radius)
        {
            float r = Mathf.Max(QuietRadius, radius);
            return _ledger.Acquire(owner, QuietKind.Aftermath, center, r, QuietSeconds, r + DepartMargin);
        }

        /// <summary>Releases one owner's lease (true exactly once).</summary>
        public static bool Release(string owner)
        {
            return _ledger.Release(owner);
        }

        public static int Tick(float activeSeconds, Vector3 player, bool playerKnown, List<string> released)
        {
            return _ledger.Tick(activeSeconds, player, playerKnown, released);
        }

        public static int ReleaseKind(QuietKind kind, string prefix, string keep, List<string> released)
        {
            return _ledger.ReleaseKind(kind, prefix, keep, released);
        }

        public static int ReleaseAll(List<string> released)
        {
            return _ledger.ReleaseAll(released);
        }
    }
}
