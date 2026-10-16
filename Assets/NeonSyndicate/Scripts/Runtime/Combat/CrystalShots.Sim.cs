using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One substep of every crystal body. Shards (primary, fan, choir) stop at the first receiver or blocker. The Gyre
    /// Crescent disc passes through bodies within its victim rules, banks off static World geometry at most twice, turns
    /// back when its outbound budget runs out (or when recalled) and is quenched when its way back to the hand is blocked.
    /// The Sunderheart orb stops at its first contact, pulls minor hostiles a bounded distance for 0.35 s (only those the
    /// pulse can see), then ruptures with line-of-sight-gated explosion damage on at most six victims.
    /// </summary>
    public sealed partial class CrystalShots
    {
        private static readonly Collider[] _area = new Collider[48];
        private static readonly List<IDamageable> _areaTargets = new List<IDamageable>();
        private static readonly List<Collider> _areaColliders = new List<Collider>();
        private static readonly List<float> _areaDistances = new List<float>();
        private static readonly List<int> _areaOrder = new List<int>();
        private static readonly HashSet<int> _areaSeen = new HashSet<int>();

        private void Step(CrystalShot s, float h)
        {
            WeaponController owner = s.Spec != null ? s.Spec.Owner : null;
            if (owner == null)
            {
                End(s, "owner gone");
                return;
            }
            s.Age += h;
            if (s.Pulsing)
            {
                s.PulseT += h;
                if (s.PulseT >= CrystalShotRules.PullSeconds) Rupture(s, owner);
                return;
            }
            if (s.Age >= s.Lifetime)
            {
                if (s.Kind == CrystalShotKind.Orb) StartPulse(s, owner, s.Pos, Vector3.up, null, null);
                else End(s, "lifetime");
                return;
            }
            switch (s.Kind)
            {
                case CrystalShotKind.ChoirShard:
                    Steer(s, h);
                    break;
                case CrystalShotKind.Disc:
                    if (s.Disc.Returning && !SteerHome(s, owner)) return;
                    break;
                case CrystalShotKind.Orb:
                    s.Vel += Vector3.down * (s.Gravity * h);
                    break;
            }
            Vector3 step = s.Vel * h;
            float dist = step.magnitude;
            if (dist < 1e-5f) return;
            Vector3 dir = step / dist;
            int n = Query(s.Pos, s.Radius, dir, dist, s.OwnerRoot, s.OwnerVehicleBody, s.OwnerIsPlayer);
            if (n < 0)
            {
                // Saturated with no proven blocker: fail closed, never fly through.
                End(s, "saturated");
                return;
            }
            switch (s.Kind)
            {
                case CrystalShotKind.Disc:
                    StepDisc(s, owner, dir, dist, n);
                    break;
                case CrystalShotKind.Orb:
                    StepOrb(s, owner, dir, dist, n);
                    break;
                default:
                    StepShard(s, owner, dir, dist, n);
                    break;
            }
        }

        // ------------------------------------------------------------------ shards

        private void StepShard(CrystalShot s, WeaponController owner, Vector3 dir, float dist, int n)
        {
            for (int i = 0; i < n; i++)
            {
                CrystalCastHit c = _cands[i];
                if (c.Blocker)
                {
                    s.Pos = c.Point;
                    owner.CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                    End(s, "blocked");
                    return;
                }
                if (s.Victims.Contains(c.TargetId)) continue;
                s.Victims.Add(c.TargetId);
                s.Pos = c.Point;
                float amount;
                bool proc;
                int k = s.Spec.CountHit(c.TargetId);
                if (s.Kind == CrystalShotKind.ChoirShard)
                {
                    // The volley budget: later shards on one actor deal less, and only a first strike may proc.
                    amount = s.Spec.Damage * CrystalShotRules.ChoirShardScale(k);
                    proc = CrystalShotRules.ChoirProcAllowed(k) && s.Spec.TakeProc();
                }
                else
                {
                    // A fan is one shot event: one proc roll however many shards land.
                    amount = s.Kind == CrystalShotKind.FanShard ? s.Spec.Secondary : s.Spec.Damage;
                    proc = s.Spec.TakeProc();
                }
                bool killed;
                owner.CrystalStrike(s.Spec, c.Target, c.Collider, c.Point, c.Normal, dir, amount, DamageType.Bullet, 1.6f, proc, true, out killed);
                End(s, "hit");
                return;
            }
            Advance(s, dir, dist);
        }

        private void Advance(CrystalShot s, Vector3 dir, float dist)
        {
            s.Pos += dir * dist;
            s.Travelled += dist;
            if (s.Kind != CrystalShotKind.Disc && s.Travelled >= s.MaxTravel)
            {
                if (s.Kind == CrystalShotKind.Orb)
                {
                    WeaponController owner = s.Spec != null ? s.Spec.Owner : null;
                    if (owner != null) StartPulse(s, owner, s.Pos, Vector3.up, null, null);
                    else End(s, "owner gone");
                }
                else
                {
                    // Ran out in the open: a faint air fade, no decal.
                    if (s.Spec != null && s.Spec.Owner != null) s.Spec.Owner.CrystalAirImpact(s.Pos);
                    End(s, "range");
                }
            }
        }

        /// <summary>Mirage Choir: turn toward the lock at the fixed turn rate; a lock out of sight is dropped for good (it flies straight).</summary>
        private static void Steer(CrystalShot s, float h)
        {
            CharacterBase t = s.Lock;
            if (t == null) return;
            if (t.IsDead || !t.isActiveAndEnabled)
            {
                s.Lock = null;
                return;
            }
            Vector3 chest = t.ChestPosition;
            if (s.Age >= s.LockCheckAt)
            {
                s.LockCheckAt = s.Age + 0.1f;
                if (Physics.Linecast(s.Pos, chest, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    // Lost behind cover: no knowledge through walls, and it never switches to another target.
                    s.Lock = null;
                    return;
                }
            }
            Vector3 want = chest - s.Pos;
            if (want.sqrMagnitude < 1e-4f) return;
            Vector3 cur = s.Vel.sqrMagnitude > 1e-4f ? s.Vel.normalized : want.normalized;
            Vector3 dir = Vector3.RotateTowards(cur, want.normalized, s.TurnRate * Mathf.Deg2Rad * h, 0f);
            s.Vel = dir.normalized * s.Speed;
        }

        // ------------------------------------------------------------------ the disc

        /// <summary>Points a returning disc at the owner's hand. Returns false when it was caught (ended).</summary>
        private bool SteerHome(CrystalShot s, WeaponController owner)
        {
            Vector3 hand = owner.MuzzlePosition;
            Vector3 to = hand - s.Pos;
            float d = to.magnitude;
            if (d <= 0.9f)
            {
                End(s, "caught");
                return false;
            }
            s.Vel = to / d * s.Speed;
            return true;
        }

        private static bool HardTarget(IDamageable t)
        {
            return t is Vehicle || t is Helicopter || t is BreakableProp || t is TacticalTurret || t is GadgetShieldFace;
        }

        private void StepDisc(CrystalShot s, WeaponController owner, Vector3 dir, float dist, int n)
        {
            for (int i = 0; i < n; i++)
            {
                CrystalCastHit c = _cands[i];
                bool solid = c.Blocker || HardTarget(c.Target);
                if (!c.Blocker)
                {
                    DiscHitVerdict v = s.Disc.TryHit(c.TargetId);
                    if (v != DiscHitVerdict.None)
                    {
                        float amount = v == DiscHitVerdict.Return ? s.Spec.Secondary : s.Spec.Damage;
                        s.Spec.CountHit(c.TargetId);
                        bool killed;
                        owner.CrystalStrike(s.Spec, c.Target, c.Collider, c.Point, c.Normal, dir, amount, DamageType.Bullet, 2.5f, s.Spec.TakeProc(), true, out killed);
                    }
                    if (!solid) continue;
                }
                // A surface: bank, turn back or (already returning) quench.
                DiscSurfaceVerdict sv = s.Disc.OnSurface(c.StaticWorld);
                Vector3 off = c.Point + c.Normal * (s.Radius + 0.03f);
                switch (sv)
                {
                    case DiscSurfaceVerdict.Bank:
                        s.Pos = off;
                        s.Vel = Vector3.Reflect(s.Vel, c.Normal);
                        owner.CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                        return;
                    case DiscSurfaceVerdict.BeginReturn:
                        s.Pos = off;
                        if (c.Blocker) owner.CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                        return;
                    default:
                        // The way home is blocked: quench (the cradle shows the disc back after its recovery; no attack).
                        if (c.Blocker) owner.CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                        End(s, "return blocked");
                        return;
                }
            }
            s.Pos += dir * dist;
            s.Travelled += dist;
            s.Disc.Spend(dist);
            if (s.Travelled > s.MaxTravel * 2.5f) End(s, "range");
        }

        // ------------------------------------------------------------------ the orb

        private void StepOrb(CrystalShot s, WeaponController owner, Vector3 dir, float dist, int n)
        {
            if (n > 0)
            {
                CrystalCastHit c = _cands[0];
                Vector3 center = c.Blocker ? c.Point + c.Normal * 0.3f : c.Point;
                StartPulse(s, owner, center, c.Normal, c.Blocker ? null : c.Target, c.Collider);
                if (c.Blocker) owner.CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                return;
            }
            Advance(s, dir, dist);
        }

        private void StartPulse(CrystalShot s, WeaponController owner, Vector3 center, Vector3 normal, IDamageable direct, Collider directCollider)
        {
            s.Pulsing = true;
            s.PulseT = 0f;
            s.PulseCenter = center;
            s.Pos = center;
            s.Vel = Vector3.zero;
            s.Direct = direct;
            s.DirectCollider = directCollider;
            s.DirectId = direct != null ? ActorId(direct) : 0;
            if (Game.Fx != null && FxBudget()) Game.Fx.Flash(center, new Color(0.95f, 0.75f, 0.4f), 2.5f, s.Spec.Radius * 2f, 0.35f);
            try { CrystalSounds.PlayImpact(CrystalImpact.Stone, center); }
            catch (System.Exception e) { Log.Exception("Crystal impact sound", e); }
            Pull(s, owner);
        }

        /// <summary>
        /// The contained pull: one bounded nudge of every minor hostile the pulse can see (through their own knockback,
        /// so collision always applies). Bosses are never moved; they take a capped poise drain instead.
        /// </summary>
        private static void Pull(CrystalShot s, WeaponController owner)
        {
            float radius = s.Spec.Radius;
            Vector3 center = s.PulseCenter;
            int mask = (1 << Layers.Characters) | (1 << Layers.Ragdoll);
            int n = Physics.OverlapSphereNonAlloc(center, radius, _area, mask, QueryTriggerInteraction.Ignore);
            _areaSeen.Clear();
            for (int i = 0; i < n; i++)
            {
                Collider col = _area[i];
                if (col == null) continue;
                CharacterBase c = col.GetComponentInParent<CharacterBase>();
                if (c == null || c.IsPlayer || !_areaSeen.Add(c.GetInstanceID())) continue;
                if (s.OwnerRoot != null && c.transform == s.OwnerRoot) continue;
                Vector3 chest = c.ChestPosition;
                bool los = !Physics.Linecast(center, chest, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                bool hostile = c.IsHostileTo();
                bool boss = ResonanceSystem.IsBossLike(c);
                if (boss)
                {
                    if (los && hostile) DrainBossPoise(c);
                    continue;
                }
                bool minor = !IsElite(c);
                if (!CrystalShotRules.PullAllowed(!c.IsDead, hostile, minor, WeaponController.CrystalProtected(c), true, los)) continue;
                Vector3 to = center - chest;
                to.y = 0f;
                float d = to.magnitude;
                float speed = CrystalShotRules.NudgeSpeed(d, radius);
                if (speed <= 0f || d < 1e-3f) continue;
                c.Knockdown(to / d * speed + Vector3.up * 1.2f, chest, 0.5f);
            }
        }

        private static bool IsElite(CharacterBase c)
        {
            NpcController n = c as NpcController;
            if (n != null) return n.Elite || n.IsChampion;
            Creature cr = c as Creature;
            return cr != null && (cr.Elite || cr.Size > 2.5f);
        }

        private static void DrainBossPoise(CharacterBase c)
        {
            Creature cr = c as Creature;
            if (cr != null)
            {
                cr.DrainPoise(CrystalShotRules.BossPoise(cr.MaxPoise));
                return;
            }
            NpcController n = c as NpcController;
            if (n != null) n.Poise -= CrystalShotRules.BossPoise(n.MaxPoise);
        }

        /// <summary>
        /// The rupture: explosion damage on at most six receivers the pulse centre can see, nearest first. The receiver the
        /// orb struck (or the nearest within the centre zone) takes the centre hit, the rest the splash. Splash only touches
        /// foes and practice-style receivers, never civilians, cars or scenery.
        /// </summary>
        private void Rupture(CrystalShot s, WeaponController owner)
        {
            CrystalShotSpec spec = s.Spec;
            Vector3 center = s.PulseCenter;
            float radius = spec.Radius;
            GatherArea(s, owner, center, radius);
            CrystalShotRules.NearestFirst(_areaDistances, CrystalShotRules.RuptureMaxVictims, _areaOrder);
            int centreIndex = -1;
            for (int i = 0; i < _areaTargets.Count; i++) if (s.DirectId != 0 && ActorId(_areaTargets[i]) == s.DirectId) centreIndex = i;
            if (centreIndex < 0 && _areaOrder.Count > 0 && _areaDistances[_areaOrder[0]] <= CrystalShotRules.CentreZone) centreIndex = _areaOrder[0];
            // The struck receiver always counts, even if six others were nearer.
            if (centreIndex >= 0 && !_areaOrder.Contains(centreIndex))
            {
                if (_areaOrder.Count >= CrystalShotRules.RuptureMaxVictims) _areaOrder.RemoveAt(_areaOrder.Count - 1);
                _areaOrder.Insert(0, centreIndex);
            }
            for (int k = 0; k < _areaOrder.Count; k++)
            {
                int i = _areaOrder[k];
                IDamageable t = _areaTargets[i];
                if (t == null || !t.IsAlive) continue;
                Collider col = _areaColliders[i];
                Vector3 at = TargetPoint(t, col);
                Vector3 dir = at - center;
                bool centre = i == centreIndex;
                float amount = centre ? spec.Damage : spec.Secondary;
                bool killed;
                owner.CrystalStrike(spec, t, col, at, -dir.normalized, dir, amount, DamageType.Explosion, centre ? 5f : 3.5f, centre && spec.TakeProc(), false, out killed);
            }
            if (Game.Fx != null)
            {
                Color col = new Color(0.95f, 0.72f, 0.38f);
                Game.Fx.Flash(center, col, 5f, radius * 3f, 0.3f);
                Game.Fx.RockBurst(center, col, spec.Concentrated ? 30 : 22, radius * 0.6f);
                Game.Fx.CrystalBurst(center, col, 18, radius * 0.5f);
                Game.Fx.GroundDust(center, radius * 0.6f, 10);
            }
            try { CrystalSounds.PlayImpact(CrystalImpact.Crystal, center); }
            catch (System.Exception e) { Log.Exception("Crystal impact sound", e); }
            if (Game.Cam != null && Game.Player != null && (Game.Player.transform.position - center).sqrMagnitude < 400f) Game.Cam.Shake(0.12f);
            Threats.Report(center, 40f, owner.gameObject, ThreatKind.Gunshot);
            End(s, "rupture");
        }

        private static Vector3 TargetPoint(IDamageable t, Collider col)
        {
            CharacterHealth ch = t as CharacterHealth;
            if (ch != null && ch.Owner != null) return ch.Owner.ChestPosition;
            return col != null ? col.bounds.center : Vector3.zero;
        }

        /// <summary>Every receiver inside radius that the centre can see, deduplicated per actor, with its distance.</summary>
        private static void GatherArea(CrystalShot s, WeaponController owner, Vector3 center, float radius)
        {
            _areaTargets.Clear();
            _areaColliders.Clear();
            _areaDistances.Clear();
            _areaSeen.Clear();
            int n = Physics.OverlapSphereNonAlloc(center, radius, _area, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = _area[i];
                if (Excluded(col, s.OwnerRoot, s.OwnerVehicleBody, s.OwnerIsPlayer)) continue;
                IDamageable t = col.GetComponentInParent<IDamageable>();
                if (t == null || !t.IsAlive) continue;
                int id = ActorId(t);
                if (!_areaSeen.Add(id)) continue;
                bool direct = id == s.DirectId;
                CharacterHealth ch = t as CharacterHealth;
                if (!direct)
                {
                    // Splash: foes only among people and creatures; cars, props and shields are never splashed.
                    if (ch != null)
                    {
                        CharacterBase body = ch.Owner;
                        if (body == null || !LootCombat.IsFoe(body, owner.gameObject)) continue;
                    }
                    else if (HardTarget(t)) continue;
                }
                Vector3 at = TargetPoint(t, col);
                if (!direct && Physics.Linecast(center, at, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                _areaTargets.Add(t);
                _areaColliders.Add(col);
                float d = (at - center).magnitude;
                if (ch != null && ch.Owner != null) d = Mathf.Max(0f, d - ch.Owner.BodyRadius);
                _areaDistances.Add(d);
            }
            // A direct hit on a receiver the overlap missed (a big body whose collider centre is outside) still counts.
            if (s.Direct != null && s.Direct.IsAlive && !_areaSeen.Contains(s.DirectId))
            {
                _areaTargets.Add(s.Direct);
                _areaColliders.Add(s.DirectCollider);
                _areaDistances.Add(0f);
            }
        }
    }
}
