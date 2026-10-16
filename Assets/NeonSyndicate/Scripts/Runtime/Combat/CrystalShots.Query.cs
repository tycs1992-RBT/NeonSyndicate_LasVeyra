using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The one swept query every crystal shot, beam and pulse uses: SphereCastNonAlloc from the previous to the next
    /// point, triggers ignored, the shooter and the shooter's own vehicle excluded (and the player's own shield face),
    /// hits sorted nearest first, a compound actor reduced to one hit by its damage receiver, corpses passed through,
    /// and the nearest blocker ending the list (nothing behind a wall is ever returned). When the hit buffer comes back
    /// full the nearest true hit might be missing, so a separate single nearest-blocker sweep must prove where the shot
    /// stops; without that proof the query fails closed and the caller quenches the shot.
    /// </summary>
    public sealed partial class CrystalShots
    {
        public const int HitBuffer = 16;

        private static readonly RaycastHit[] _buf = new RaycastHit[HitBuffer];
        private static readonly CrystalCastHit[] _cands = new CrystalCastHit[HitBuffer + 1];
        private static readonly HashSet<int> _dedupe = new HashSet<int>();
        private static int _candCount;

        private static void ResetQueryStatics()
        {
            _dedupe.Clear();
            _candCount = 0;
            for (int i = 0; i < _cands.Length; i++) _cands[i] = new CrystalCastHit();
        }

        /// <summary>The number of results from the last Query.</summary>
        public static int CastCount { get { return _candCount; } }

        /// <summary>Result i of the last Query (nearest first; only the last one can be a blocker).</summary>
        public static CrystalCastHit CastAt(int i)
        {
            return i >= 0 && i < _candCount ? _cands[i] : new CrystalCastHit();
        }

        /// <summary>A stable nonzero id for a damage receiver (its component instance id).</summary>
        public static int ActorId(IDamageable t)
        {
            MonoBehaviour mb = t as MonoBehaviour;
            if (mb != null) return mb.GetInstanceID();
            return t != null ? (t.GetHashCode() | 1) : 0;
        }

        private static bool Excluded(Collider c, Transform ownerRoot, Rigidbody ownerVehicle, bool ownerIsPlayer)
        {
            if (c == null) return true;
            if (c.isTrigger) return true;
            Transform t = c.transform;
            if (ownerRoot != null && (t == ownerRoot || t.IsChildOf(ownerRoot))) return true;
            if (ownerVehicle != null && c.attachedRigidbody == ownerVehicle) return true;
            // The player's own Lattice shield soaks enemy fire only; the player's shots pass it. Other things on that layer
            // (the Queen's mirror focus points) must still take crystal hits, so only the gadget's shield face is skipped.
            if (ownerIsPlayer && c.gameObject.layer == GadgetModels.ShieldLayer && c.GetComponentInParent<GadgetShieldFace>() != null) return true;
            return false;
        }

        private static bool IsStaticWorld(Collider c)
        {
            return c != null && c.gameObject.layer == Layers.World && c.attachedRigidbody == null;
        }

        /// <summary>
        /// Sweeps a sphere of radius from origin along dir (normalized) for dist metres. Returns the number of results
        /// (read them with CastAt), or -1 when the buffer saturated with no proven nearest blocker (quench the shot).
        /// </summary>
        public static int Query(Vector3 origin, float radius, Vector3 dir, float dist, Transform ownerRoot, Rigidbody ownerVehicle, bool ownerIsPlayer)
        {
            _candCount = 0;
            if (!(dist > 1e-5f)) return 0;
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, _buf, dist, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            if (n <= 0) return 0;
            SortHits(n);
            bool saturated = n >= _buf.Length;
            RaycastHit proven = new RaycastHit();
            bool hasProven = false;
            float limit = float.MaxValue;
            if (saturated)
            {
                _saturations++;
                // A single sweep returns the closest hit on its mask: that proves the nearest blocker. The shooter's own
                // vehicle cannot be excluded from a single sweep, so a drive-by proves against static World only.
                int mask = ownerVehicle != null ? Layers.WorldMask : BlockerMask;
                hasProven = Physics.SphereCast(origin, radius, dir, out proven, dist, mask, QueryTriggerInteraction.Ignore);
                if (CrystalShotRules.Saturation(n, _buf.Length, hasProven) == SaturationVerdict.Quench) return -1;
                limit = proven.distance;
            }
            _dedupe.Clear();
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _buf[i];
                Collider c = h.collider;
                if (Excluded(c, ownerRoot, ownerVehicle, ownerIsPlayer)) continue;
                if (saturated && h.distance > limit + 0.001f) break;
                if (Classify(c, h.distance, h.point, h.normal, origin, dir)) return _candCount;
            }
            if (saturated && hasProven && proven.collider != null && !Excluded(proven.collider, ownerRoot, ownerVehicle, ownerIsPlayer))
                Classify(proven.collider, proven.distance, proven.point, proven.normal, origin, dir);
            return _candCount;
        }

        /// <summary>Adds one hit as a receiver or a blocker. Returns true when it was a blocker (the list ends there).</summary>
        private static bool Classify(Collider c, float distance, Vector3 point, Vector3 normal, Vector3 origin, Vector3 dir)
        {
            // A sweep that starts inside a collider reports it at distance zero with no point.
            bool overlap = distance <= 0f && point == Vector3.zero;
            Vector3 p = overlap ? origin : point;
            Vector3 nrm = overlap ? -dir : normal;
            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                if (target.IsAlive)
                {
                    int id = ActorId(target);
                    if (!_dedupe.Add(id)) return false;
                    Add(new CrystalCastHit { Collider = c, Target = target, TargetId = id, Distance = Mathf.Max(0f, distance), Point = p, Normal = nrm });
                    return false;
                }
                // A body that already died is passed through; a destroyed car or prop still blocks.
                if (target is CharacterHealth) return false;
            }
            Add(new CrystalCastHit { Collider = c, Distance = Mathf.Max(0f, distance), Point = p, Normal = nrm, Blocker = true, StaticWorld = IsStaticWorld(c) });
            return true;
        }

        private static void Add(CrystalCastHit h)
        {
            if (_candCount >= _cands.Length) return;
            _cands[_candCount++] = h;
        }

        private static void SortHits(int n)
        {
            for (int i = 1; i < n; i++)
            {
                RaycastHit h = _buf[i];
                int j = i - 1;
                while (j >= 0 && _buf[j].distance > h.distance)
                {
                    _buf[j + 1] = _buf[j];
                    j--;
                }
                _buf[j + 1] = h;
            }
        }
    }
}
