using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What to do with a swept query's results.</summary>
    public enum SaturationVerdict
    {
        /// <summary>Every hit was returned, or a separate nearest-blocker query proved where the shot stops.</summary>
        Proceed = 0,
        /// <summary>The buffer was full and no nearest blocker was proven: quench the shot (fail closed).</summary>
        Quench = 1
    }

    /// <summary>What a Gyre Crescent disc may do to an actor it touches.</summary>
    public enum DiscHitVerdict
    {
        /// <summary>No damage (already hit in this phase, or the unique-victim cap is full).</summary>
        None = 0,
        /// <summary>An outbound hit (primary damage).</summary>
        Outbound = 1,
        /// <summary>A return hit (reduced damage).</summary>
        Return = 2
    }

    /// <summary>What a Gyre Crescent disc does when it meets a solid surface.</summary>
    public enum DiscSurfaceVerdict
    {
        /// <summary>Reflect off it and keep flying out (static World geometry with banks left).</summary>
        Bank = 0,
        /// <summary>End the outbound flight here and head back to the hand.</summary>
        BeginReturn = 1,
        /// <summary>The return path is blocked: quench the disc (its presentation returns, no attack).</summary>
        Quench = 2
    }

    /// <summary>
    /// Engine-free rules for the crystal projectiles and pulses (tested offline): query saturation, projectile admission,
    /// the disc's banks and victims, Mirage Choir lock eligibility and the per-volley budget, the Mirrorwake focus and its
    /// no-double-hit redirect, and Sunderheart's pull and rupture limits. The runtime (CrystalShots and the weapon
    /// controller) asks these functions; it never re-decides a rule on its own.
    /// </summary>
    public static class CrystalShotRules
    {
        // ------------------------------------------------------------------ queries and admission

        /// <summary>Largest simulation substep for a projectile, in gameplay seconds.</summary>
        public const float MaxSubstep = 0.05f;
        /// <summary>Most gameplay time one frame may simulate for projectiles (a longer hitch moves them less, never faster).</summary>
        public const float MaxFrameStep = 0.2f;

        /// <summary>
        /// Saturation: when the hit buffer came back full, the nearest true hit may be missing, so a shot may only go on
        /// when a separate nearest-blocker query proved where it stops. Otherwise it is quenched (never fired through).
        /// </summary>
        public static SaturationVerdict Saturation(int hits, int bufferSize, bool provenBlocker)
        {
            if (hits < bufferSize) return SaturationVerdict.Proceed;
            return provenBlocker ? SaturationVerdict.Proceed : SaturationVerdict.Quench;
        }

        /// <summary>Active projectile bodies allowed at once (graphics Low or not).</summary>
        public static int ProjectileCap(bool lowGraphics)
        {
            return lowGraphics ? CrystalFocusDb.MaxProjectilesLow : CrystalFocusDb.MaxProjectiles;
        }

        /// <summary>A whole shot event (one shard, a three-shard fan, a five-shard volley) is admitted entirely or refused entirely.</summary>
        public static bool Admit(int active, int wanted, int cap)
        {
            if (wanted <= 0) return true;
            return active >= 0 && active + wanted <= cap;
        }

        /// <summary>How many equal substeps a frame of dt needs so none is longer than maxStep (0 for no time).</summary>
        public static int Substeps(float dt, float maxStep)
        {
            if (!(dt > 0f) || !(maxStep > 0f)) return 0;
            return Mathf.Clamp(Mathf.CeilToInt(dt / maxStep - 1e-4f), 1, 64);
        }

        /// <summary>The frame time projectiles simulate: the gameplay delta, capped at MaxFrameStep (bad values give 0).</summary>
        public static float FrameStep(float dt)
        {
            if (!(dt > 0f)) return 0f;
            return Mathf.Min(dt, MaxFrameStep);
        }

        /// <summary>Rotates a direction about the world up axis (fans and volleys). Pure maths, no Quaternion.</summary>
        public static Vector3 RotateAboutUp(Vector3 dir, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(dir.x * c + dir.z * s, dir.y, -dir.x * s + dir.z * c);
        }

        /// <summary>The yaw offset of shard i of n in a fan of +-halfSpread degrees (0 for a single shard).</summary>
        public static float FanOffset(int i, int n, float halfSpread)
        {
            if (n <= 1) return 0f;
            return Mathf.Lerp(-halfSpread, halfSpread, i / (float)(n - 1));
        }

        // ------------------------------------------------------------------ Gyre Crescent

        /// <summary>
        /// The bookkeeping of one disc: at most one outbound and one return hit per actor (a compound collider is one
        /// actor, keyed by the damage receiver), at most MaxVictims unique actors, at most MaxBanks banks off static World
        /// geometry, and an outbound travel budget after which the disc must return.
        /// </summary>
        public sealed class DiscBook
        {
            public int MaxVictims = 3;
            public int MaxBanks = 2;
            public float Budget = 32f;

            private readonly HashSet<int> _out = new HashSet<int>();
            private readonly HashSet<int> _ret = new HashSet<int>();
            private readonly HashSet<int> _unique = new HashSet<int>();

            public int Banks { get; private set; }
            public float Travelled { get; private set; }
            public bool Returning { get; private set; }
            public int UniqueVictims { get { return _unique.Count; } }

            /// <summary>Decides one touch of an actor (actorId must be nonzero and stable for the actor's whole body).</summary>
            public DiscHitVerdict TryHit(int actorId)
            {
                if (actorId == 0) return DiscHitVerdict.None;
                HashSet<int> phase = Returning ? _ret : _out;
                if (phase.Contains(actorId)) return DiscHitVerdict.None;
                if (!_unique.Contains(actorId) && _unique.Count >= MaxVictims) return DiscHitVerdict.None;
                phase.Add(actorId);
                _unique.Add(actorId);
                return Returning ? DiscHitVerdict.Return : DiscHitVerdict.Outbound;
            }

            /// <summary>The disc met a solid surface. staticWorld is true only for static World-layer geometry.</summary>
            public DiscSurfaceVerdict OnSurface(bool staticWorld)
            {
                if (Returning) return DiscSurfaceVerdict.Quench;
                if (staticWorld && Banks < MaxBanks)
                {
                    Banks++;
                    return DiscSurfaceVerdict.Bank;
                }
                Returning = true;
                return DiscSurfaceVerdict.BeginReturn;
            }

            /// <summary>Counts outbound travel. Returns true the moment the budget runs out (the return then begins).</summary>
            public bool Spend(float metres)
            {
                if (Returning || !(metres > 0f)) return false;
                Travelled += metres;
                if (Travelled < Budget) return false;
                Returning = true;
                return true;
            }

            /// <summary>An early recall (fire pressed while the disc is out). Returns false when it was already returning.</summary>
            public bool Recall()
            {
                if (Returning) return false;
                Returning = true;
                return true;
            }

            /// <summary>Back to a fresh disc (pool release).</summary>
            public void Reset()
            {
                _out.Clear();
                _ret.Clear();
                _unique.Clear();
                Banks = 0;
                Travelled = 0f;
                Returning = false;
            }
        }

        // ------------------------------------------------------------------ Mirage Choir

        /// <summary>One actor considered for a Mirage Choir lock (or a Mirrorwake redirect).</summary>
        public struct LockCandidate
        {
            public bool Alive;
            /// <summary>Hostile to the player right now (people: their hostility; creatures: engaged or aggressive and not calm).</summary>
            public bool Hostile;
            /// <summary>A civilian, a clerk or venue staff, a companion: never assisted, whatever their state.</summary>
            public bool Protected;
            /// <summary>Calm or peaceful wildlife (habitat residents, an unprovoked territorial creature).</summary>
            public bool Peaceful;
            /// <summary>Not hidden (a stealthed shade or a burrowed wyrm is not visible).</summary>
            public bool Visible;
            /// <summary>Clear line of sight from the shooter to the actor's chest.</summary>
            public bool LineOfSight;
            public float Distance;
            /// <summary>Angle between the aim ray and the direction to the actor, in degrees.</summary>
            public float AngleDeg;
        }

        /// <summary>True when the candidate may be locked: a visible, hostile, unprotected actor in range, in the cone, in sight.</summary>
        public static bool LockEligible(LockCandidate c, float range, float halfAngleDeg)
        {
            if (!c.Alive || !c.Hostile || c.Protected || c.Peaceful || !c.Visible || !c.LineOfSight) return false;
            if (!(c.Distance >= 0f) || c.Distance > range) return false;
            return c.AngleDeg >= 0f && c.AngleDeg <= halfAngleDeg;
        }

        /// <summary>
        /// The locks of one Mirage Choir: aim must dwell DwellSeconds on one candidate to lock it, at most MaxLocks, a
        /// changed candidate restarts the dwell, and a lock whose target fails eligibility is dropped by the runtime.
        /// </summary>
        public sealed class LockBook
        {
            public float DwellSeconds = CrystalFocusDb.LockSeconds;
            public int MaxLocks = CrystalFocusDb.MaxLocks;

            private readonly List<int> _locked = new List<int>();
            private int _candidate;
            private float _dwell;

            public int Count { get { return _locked.Count; } }
            public int Candidate { get { return _candidate; } }
            /// <summary>Progress of the current candidate's dwell, 0 to 1.</summary>
            public float DwellProgress { get { return _candidate == 0 ? 0f : Mathf.Clamp01(_dwell / Mathf.Max(0.01f, DwellSeconds)); } }

            public int LockedAt(int i)
            {
                return i >= 0 && i < _locked.Count ? _locked[i] : 0;
            }

            public bool IsLocked(int id)
            {
                return id != 0 && _locked.Contains(id);
            }

            /// <summary>
            /// One aim update with the best eligible, not yet locked candidate under the reticle (0 for none) and the time
            /// since the previous update. Returns the id that became locked this update, or 0.
            /// </summary>
            public int Update(int candidateId, float dt)
            {
                if (candidateId == 0 || IsLocked(candidateId) || _locked.Count >= MaxLocks)
                {
                    _candidate = 0;
                    _dwell = 0f;
                    return 0;
                }
                if (candidateId != _candidate)
                {
                    // A new candidate starts its dwell now: time spent on another target never counts.
                    _candidate = candidateId;
                    _dwell = 0f;
                    return 0;
                }
                if (dt > 0f) _dwell += dt;
                if (_dwell < DwellSeconds - 1e-4f) return 0;
                _locked.Add(candidateId);
                _candidate = 0;
                _dwell = 0f;
                return candidateId;
            }

            /// <summary>Drops one lock (lost sight, out of range, dead, no longer hostile).</summary>
            public void Drop(int id)
            {
                _locked.Remove(id);
                if (_candidate == id)
                {
                    _candidate = 0;
                    _dwell = 0f;
                }
            }

            public void Clear()
            {
                _locked.Clear();
                _candidate = 0;
                _dwell = 0f;
            }
        }

        /// <summary>Which lock shard i of a volley follows (round robin over the locks), or -1 when it flies straight.</summary>
        public static int ChoirShardTarget(int shardIndex, int lockCount)
        {
            if (lockCount <= 0 || shardIndex < 0) return -1;
            return shardIndex % lockCount;
        }

        /// <summary>
        /// The per-volley damage budget on one actor: the first three shards of a volley to strike the same actor deal
        /// full damage, any further shard from that volley half (hitIndex counts from 0 per actor per volley).
        /// </summary>
        public static float ChoirShardScale(int hitIndexOnActor)
        {
            if (hitIndexOnActor < 0) return 0f;
            return hitIndexOnActor < 3 ? 1f : 0.5f;
        }

        /// <summary>One element proc roll per actor per volley (only the first shard to strike it may roll).</summary>
        public static bool ChoirProcAllowed(int hitIndexOnActor)
        {
            return hitIndexOnActor == 0;
        }

        /// <summary>The most a five-shard volley can deal to one actor, in shards of primary damage (3 full plus 2 halves).</summary>
        public static float ChoirVolleyCapShards(int shards)
        {
            float total = 0f;
            for (int i = 0; i < shards; i++) total += ChoirShardScale(i);
            return total;
        }

        // ------------------------------------------------------------------ Mirrorwake

        /// <summary>Metres of aim tolerance around a planted focus for a redirect.</summary>
        public const float FocusAimTolerance = 1.2f;
        /// <summary>Farthest a pulse may plant the focus.</summary>
        public const float FocusPlantRange = 25f;

        /// <summary>A charged pulse plants the focus when its legal endpoint is static World geometry within range and it hit no hostile.</summary>
        public static bool CanPlantFocus(bool endpointStaticWorld, float distance, bool hostileHit)
        {
            return endpointStaticWorld && !hostileHit && distance >= 0f && distance <= FocusPlantRange;
        }

        /// <summary>
        /// True when the aim line (origin along dir for range metres) passes within tolerance of the focus point, in front
        /// of the origin. dir need not be normalized.
        /// </summary>
        public static bool AimedAtFocus(Vector3 origin, Vector3 dir, float range, Vector3 focus, float tolerance)
        {
            float len = dir.magnitude;
            if (len < 1e-5f) return false;
            Vector3 d = dir / len;
            Vector3 to = focus - origin;
            float along = Vector3.Dot(to, d);
            if (along <= 0f || along > range + tolerance) return false;
            Vector3 closest = origin + d * along;
            return (focus - closest).sqrMagnitude <= tolerance * tolerance;
        }

        /// <summary>The same victim never takes direct and redirected damage from one pulse.</summary>
        public static bool RedirectAllowed(int directVictimId, int candidateId)
        {
            return candidateId != 0 && candidateId != directVictimId;
        }

        // ------------------------------------------------------------------ Sunderheart

        public const int RuptureMaxVictims = 6;
        /// <summary>Seconds of the contained pull before the rupture.</summary>
        public const float PullSeconds = 0.35f;
        /// <summary>Concentrated orb: smaller radius, stronger centre.</summary>
        public const float ConcentratedRadius = 2.5f;
        public const float ConcentratedCentreScale = 1.5f;
        /// <summary>Metres around the pulse centre that count as the centre when the orb struck no actor directly.</summary>
        public const float CentreZone = 1f;
        /// <summary>Most a pull may move a minor hostile, as a speed change in metres per second.</summary>
        public const float MaxNudgeSpeed = 4f;
        /// <summary>Fraction of a boss's poise one orb may take (bosses are never pulled).</summary>
        public const float BossPoiseCapFraction = 0.12f;

        public static float RuptureRadius(bool concentrated, float profileRadius)
        {
            return concentrated ? ConcentratedRadius : Mathf.Max(0.5f, profileRadius);
        }

        /// <summary>Only a living, hostile, minor (not boss-like, not elite) character in sight of the pulse centre is pulled; never civilians, vehicles or scenery.</summary>
        public static bool PullAllowed(bool alive, bool hostile, bool minor, bool protectedActor, bool isCharacter, bool lineOfSight)
        {
            return alive && hostile && minor && !protectedActor && isCharacter && lineOfSight;
        }

        /// <summary>The bounded pull: stronger near the edge of the radius, never more than MaxNudgeSpeed, zero at the centre.</summary>
        public static float NudgeSpeed(float distance, float radius)
        {
            if (!(radius > 0f) || !(distance > 0.25f)) return 0f;
            return MaxNudgeSpeed * Mathf.Clamp01(distance / radius);
        }

        /// <summary>The capped poise a boss loses to one orb.</summary>
        public static float BossPoise(float maxPoise)
        {
            return Mathf.Max(0f, maxPoise) * BossPoiseCapFraction;
        }

        /// <summary>Picks at most max victims nearest first. distances are unsorted; the result lists their indices.</summary>
        public static void NearestFirst(List<float> distances, int max, List<int> result)
        {
            result.Clear();
            if (distances == null) return;
            for (int i = 0; i < distances.Count; i++) result.Add(i);
            result.Sort((a, b) => distances[a].CompareTo(distances[b]));
            if (result.Count > max) result.RemoveRange(max, result.Count - max);
        }
    }
}
