using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Mirage Choir (the Djinn's signature): holding aim designates up to three visible hostiles inside an 18 degree cone
    /// within 48 m, 0.35 s of steady aim each, with line of sight from the muzzle; civilians, staff, companions and
    /// peaceful wildlife are never locked. A press fires five guided shards (0.45 B each, 42 m/s, 180 degrees a second)
    /// spread over the locks; unlocked shards fly as a straight fan. Every shard collides with the world, a lock that
    /// drops out of sight is dropped for that shard (it flies straight and never switches target), and one volley has
    /// one critical roll, one proc per actor and a damage budget per actor.
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float ChoirShardRadius = 0.07f;
        public const float ChoirFanHalfSpread = 8f;
        /// <summary>Half of the 18 degree lock cone.</summary>
        public const float ChoirLockHalfAngle = 9f;
        /// <summary>A held lock survives inside this wider half-angle (looking well away drops it).</summary>
        public const float ChoirKeepHalfAngle = 35f;
        /// <summary>Seconds between lock scans (bounded target selection).</summary>
        public const float ChoirScanInterval = 0.1f;

        private CrystalShotRules.LockBook _crLocks;
        private readonly CharacterBase[] _crLockBodies = new CharacterBase[CrystalFocusDb.MaxLocks];
        private CharacterBase _crLockCandidate;
        private float _crLockScanAt;
        private float _crLockScanLast;

        /// <summary>The bodies currently locked (HUD); entries may be null.</summary>
        internal CharacterBase[] CrystalLockBodies { get { return _crLockBodies; } }
        internal CharacterBase CrystalLockCandidate { get { return _crLockCandidate; } }
        internal float CrystalLockDwell { get { return _crLocks != null ? _crLocks.DwellProgress : 0f; } }
        internal int CrystalLockCount { get { return _crLocks != null ? _crLocks.Count : 0; } }

        private bool ChoirTrigger(WeaponDef d, Vector3 target, bool pressed)
        {
            if (!pressed || !CanAct) return false;
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.MirageChoir);
            if (p == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                _cooldown = 0.12f;
                return false;
            }
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, ChoirShardRadius))
            {
                CrystalDeny("Blocked");
                _cooldown = 0.15f;
                return false;
            }
            int count = Mathf.Max(1, p.Count);
            if (!CrystalShots.CanAdmit(count))
            {
                CrystalShots.CountRefused();
                CrystalRefused();
                _cooldown = 0.15f;
                return false;
            }
            Vector3 aim = CrystalAimDir(muzzle, target);
            if (!CrystalChargeReady(CrystalBehavior.MirageChoir, false, target)) return false;
            CrystalShotSpec spec = CrystalShots.NewSpec(this, CrystalBehavior.MirageChoir, d);
            if (spec == null) return false;
            spec.Crit = CrystalCritRoll(d);
            int locks = CompactLocks();
            int launched = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = CrystalShotRules.RotateAboutUp(aim, CrystalShotRules.FanOffset(i, count, ChoirFanHalfSpread));
                // Guided shards leave a little upward so their turns read as a fan, not one bolt.
                if (locks > 0) dir = (dir + Vector3.up * 0.08f * (1 + (i & 1))).normalized;
                int li = CrystalShotRules.ChoirShardTarget(i, locks);
                CharacterBase lockBody = li >= 0 ? _crLockBodies[li] : null;
                if (CrystalShots.Launch(spec, CrystalShotKind.ChoirShard, muzzle, dir.normalized * p.Speed, ChoirShardRadius, 0f, p.Range, p.Range / p.Speed + 0.6f, lockBody)) launched++;
            }
            if (launched == 0)
            {
                CrystalShots.DiscardSpec(spec);
                CrystalRefused();
                return false;
            }
            _cooldown = d.Interval;
            CrystalAfterShot(d, CrystalBehavior.MirageChoir, muzzle, aim, p.Heat, target, 60f);
            return true;
        }

        /// <summary>Moves live locks to the front of the array and returns how many there are.</summary>
        private int CompactLocks()
        {
            int n = 0;
            for (int i = 0; i < _crLockBodies.Length; i++)
            {
                CharacterBase c = _crLockBodies[i];
                if (c == null || c.IsDead)
                {
                    // A destroyed or dead body also leaves the lock book, so it never holds a slot.
                    if (!ReferenceEquals(c, null) && _crLocks != null) _crLocks.Drop(c.GetInstanceID());
                    continue;
                }
                _crLockBodies[n++] = c;
            }
            for (int i = n; i < _crLockBodies.Length; i++) _crLockBodies[i] = null;
            return n;
        }

        private void ChoirUpdate(WeaponDef d, float dt)
        {
            if (_crLocks == null) _crLocks = new CrystalShotRules.LockBook();
            PlayerController p = Game.Player;
            bool aiming = p != null && p.AimingDownSights && CrystalBlockReason() == null;
            if (!aiming)
            {
                if (_crLocks.Count > 0 || _crLockCandidate != null) ClearCrystalLocks();
                return;
            }
            float now = CrystalNow;
            if (now < _crLockScanAt) return;
            // The first scan after aim begins credits no time; later scans credit the gameplay time since the last one.
            float scanDt = _crLockScanLast > 0f ? Mathf.Min(0.25f, now - _crLockScanLast) : 0f;
            _crLockScanLast = now;
            _crLockScanAt = now + ChoirScanInterval;
            CrystalProfile prof = CrystalFocusDb.Get(CrystalBehavior.MirageChoir);
            float range = prof != null ? prof.Range : 48f;
            Vector3 eye = MuzzlePosition;
            Ray ray = Game.Cam != null ? Game.Cam.AimRay : new Ray(eye, transform.forward);
            // Revalidate held locks: dead, out of sight, out of range, no longer hostile or looked well away drops them.
            for (int i = 0; i < _crLockBodies.Length; i++)
            {
                CharacterBase c = _crLockBodies[i];
                if (ReferenceEquals(c, null)) continue;
                if (c == null || !LockCandidateOk(c, eye, ray, range, ChoirKeepHalfAngle))
                {
                    _crLocks.Drop(c.GetInstanceID());
                    _crLockBodies[i] = null;
                }
            }
            CharacterBase best = BestLockCandidate(eye, ray, range);
            int lockedId = _crLocks.Update(best != null ? best.GetInstanceID() : 0, scanDt);
            _crLockCandidate = _crLocks.Candidate != 0 ? best : null;
            if (lockedId != 0 && best != null)
            {
                for (int i = 0; i < _crLockBodies.Length; i++)
                {
                    if (_crLockBodies[i] != null) continue;
                    _crLockBodies[i] = best;
                    break;
                }
                CrystalSounds.PlayChargeReady(CrystalBehavior.MirageChoir, eye);
            }
        }

        private void ClearCrystalLocks()
        {
            if (_crLocks != null) _crLocks.Clear();
            for (int i = 0; i < _crLockBodies.Length; i++) _crLockBodies[i] = null;
            _crLockCandidate = null;
            _crLockScanLast = 0f;
            _crLockScanAt = 0f;
        }

        /// <summary>The eligible, not yet locked actor nearest the reticle inside the lock cone, or null.</summary>
        private CharacterBase BestLockCandidate(Vector3 eye, Ray ray, float range)
        {
            if (_crLocks.Count >= _crLocks.MaxLocks) return null;
            CharacterBase best = null;
            float bestAngle = float.MaxValue;
            ConsiderLocks(NpcController.All, eye, ray, range, ref best, ref bestAngle);
            ConsiderLocks(Creature.All, eye, ray, range, ref best, ref bestAngle);
            return best;
        }

        private void ConsiderLocks<T>(List<T> all, Vector3 eye, Ray ray, float range, ref CharacterBase best, ref float bestAngle) where T : CharacterBase
        {
            float r2 = range * range;
            for (int i = 0; i < all.Count; i++)
            {
                CharacterBase c = all[i];
                if (c == null || c.IsDead) continue;
                if ((c.ChestPosition - eye).sqrMagnitude > r2) continue;
                if (_crLocks.IsLocked(c.GetInstanceID())) continue;
                Vector3 to = c.ChestPosition - ray.origin;
                float angle = Vector3.Angle(ray.direction, to);
                if (angle > ChoirLockHalfAngle || angle >= bestAngle) continue;
                if (!LockCandidateOk(c, eye, ray, range, ChoirLockHalfAngle)) continue;
                best = c;
                bestAngle = angle;
            }
        }

        /// <summary>Builds the pure lock candidate for an actor and asks CrystalShotRules.LockEligible.</summary>
        private bool LockCandidateOk(CharacterBase c, Vector3 eye, Ray ray, float range, float halfAngle)
        {
            if (c == null) return false;
            Vector3 chest = c.ChestPosition;
            CrystalShotRules.LockCandidate k = CrystalCandidate(c);
            k.Distance = (chest - eye).magnitude;
            k.AngleDeg = Vector3.Angle(ray.direction, chest - ray.origin);
            k.LineOfSight = k.Distance <= range && !Physics.Linecast(eye, chest, Layers.SightMask, QueryTriggerInteraction.Ignore);
            return CrystalShotRules.LockEligible(k, range, halfAngle);
        }

        /// <summary>
        /// The hostility and protection facts of an actor for assistance (locks and redirects). People: their hostility
        /// to the player, never civilians, staff or companions. Creatures: hostile and engaged (or aggressive by nature),
        /// never calm habitat residents or an unprovoked territorial beast.
        /// </summary>
        internal static CrystalShotRules.LockCandidate CrystalCandidate(CharacterBase c)
        {
            CrystalShotRules.LockCandidate k = new CrystalShotRules.LockCandidate();
            if (c == null) return k;
            k.Alive = !c.IsDead;
            k.Protected = CrystalProtected(c);
            k.Visible = true;
            NpcController n = c as NpcController;
            Creature cr = c as Creature;
            if (n != null)
            {
                k.Hostile = n.IsHostileToPlayer;
                k.Peaceful = n.IsCreature && !n.IsHostileToPlayer;
            }
            else if (cr != null)
            {
                Temperament t = cr.Species != null ? cr.Species.Temperament : Temperament.Aggressive;
                bool engagedOrWild = cr.Provoked || cr.IsEngaged || t == Temperament.Aggressive || t == Temperament.Swarm || t == Temperament.Apex || t == Temperament.Stalker;
                k.Hostile = cr.IsHostileToPlayer && engagedOrWild;
                k.Peaceful = cr.CalmHabitatResident || (t == Temperament.Territorial && !cr.Provoked && !cr.IsEngaged);
                k.Visible = cr.Targetable;
            }
            return k;
        }
    }
}
