using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Mirrorwake (the Queen's signature): hold fire to charge for 0.6 s; a real release fires an instant precision pulse
    /// (3.0 B, 0.32 heat, one per 1.1 s) along the legal muzzle path to the first receiver or blocker. A charged pulse
    /// whose endpoint is static World geometry within 25 m with no receiver hit plants the single reflective focus for
    /// 8 s (visual only: no collider, never a platform). A later charged pulse aimed within 1.2 m of the focus travels
    /// muzzle to focus (that leg blocked on its own), then bends to the nearest visible hostile within 30 m of the focus
    /// (the second leg blocked on its own) for the redirect share; the same victim never takes direct and redirected
    /// damage from one pulse, and the focus is consumed. A release before the charge is full fires nothing.
    /// </summary>
    public sealed partial class WeaponController
    {
        public const float PulseRadius = 0.06f;
        public const float PulseVisualSeconds = 0.14f;

        private bool _crFocusActive;
        private Vector3 _crFocusPos;
        private Vector3 _crFocusNormal = Vector3.up;
        private float _crFocusUntil;
        private GameObject _crFocusMarker;
        private float _crPulseHideAt;
        private bool _crAimOnFocus;
        private static readonly List<CharacterBase> _crRedirectWork = new List<CharacterBase>();

        /// <summary>True while a Mirrorwake focus is planted (HUD and rig).</summary>
        public bool CrystalFocusPlanted { get { return _crFocusActive; } }
        internal Vector3 CrystalFocusPosition { get { return _crFocusPos; } }
        internal float CrystalFocusLeft { get { return _crFocusActive ? Mathf.Max(0f, _crFocusUntil - CrystalNow) : 0f; } }
        internal bool CrystalAimOnFocus { get { return _crAimOnFocus; } }

        private bool MirrorTrigger(WeaponDef d, bool held)
        {
            if (!held) return false;
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Mirrorwake);
            if (p == null) return false;
            if (!_crCharging)
            {
                if (!CanAct) return false;
                if (!_crHeat.CanFire(CrystalKey(d)))
                {
                    CrystalDeny("Overheated");
                    return false;
                }
                _crCharging = true;
                _crCharge = 0f;
                _crChargeReady = false;
            }
            AdvanceCrystalCharge(CrystalBehavior.Mirrorwake, p.ChargeSeconds);
            return false;
        }

        private void MirrorUpdate(WeaponDef d)
        {
            _crAimOnFocus = false;
            if (!_crFocusActive || Game.Player == null) return;
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Mirrorwake);
            Vector3 muzzle = MuzzlePosition;
            Vector3 dir = CrystalAimDir(muzzle, Game.Player.AimPoint);
            _crAimOnFocus = CrystalShotRules.AimedAtFocus(muzzle, dir, p != null ? p.Range : 60f, _crFocusPos, CrystalShotRules.FocusAimTolerance);
        }

        /// <summary>The charged pulse: a redirect through the focus when aimed at it, otherwise a direct pulse (which may plant the focus).</summary>
        private bool MirrorPulse(WeaponDef d, Vector3 target)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Mirrorwake);
            if (p == null || _crHeat == null) return false;
            int key = CrystalKey(d);
            if (!_crHeat.CanFire(key))
            {
                CrystalDeny("Overheated");
                return false;
            }
            if (_cooldown > 0.05f) return false;
            Vector3 muzzle = MuzzlePosition;
            if (!CrystalLegalOrigin(muzzle, PulseRadius))
            {
                CrystalDeny("Blocked");
                return false;
            }
            if (!CrystalChargeReady(CrystalBehavior.Mirrorwake, false, target) || CrystalBeamFor(CrystalBehavior.Mirrorwake) == null) return false;
            Vector3 dir = CrystalAimDir(muzzle, target);
            CrystalShotSpec spec = CrystalLocalSpec(d, CrystalBehavior.Mirrorwake, CrystalCritRoll(d));
            spec.Secondary = CrystalSecondary(d, p);
            Vector3 end;
            if (_crFocusActive && CrystalShotRules.AimedAtFocus(muzzle, dir, p.Range, _crFocusPos, CrystalShotRules.FocusAimTolerance))
                end = RedirectPulse(spec, muzzle, p);
            else
                end = DirectPulse(spec, muzzle, dir, p);
            _cooldown = d.Interval;
            CrystalAfterShot(d, CrystalBehavior.Mirrorwake, muzzle, dir, p.Heat, end, 70f);
            if (Game.Fx != null && CrystalShots.FxBudget()) Game.Fx.Flash(muzzle, new Color(0.75f, 0.85f, 1f), 2f, 4f, 0.08f);
            return true;
        }

        private Rigidbody CrystalOwnVehicleBody { get { return IgnoreVehicle != null ? IgnoreVehicle.Body : null; } }

        private Vector3 DirectPulse(CrystalShotSpec spec, Vector3 muzzle, Vector3 dir, CrystalProfile p)
        {
            Vector3 end = muzzle + dir * p.Range;
            int n = CrystalShots.Query(muzzle, PulseRadius, dir, p.Range, transform, CrystalOwnVehicleBody, IsPlayer);
            if (n < 0)
            {
                end = muzzle + dir * 0.3f;
                CrystalDeny("Blocked");
            }
            else if (n > 0)
            {
                CrystalCastHit c = CrystalShots.CastAt(0);
                end = c.Point;
                if (c.Blocker)
                {
                    CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                    if (CrystalShotRules.CanPlantFocus(c.StaticWorld, c.Distance, false)) PlantFocus(c.Point, c.Normal);
                }
                else
                {
                    bool killed;
                    CrystalStrike(spec, c.Target, c.Collider, c.Point, c.Normal, dir, spec.Damage, DamageType.Bullet, 4f, spec.TakeProc(), true, out killed);
                }
            }
            CrystalBeamVisual v = CrystalBeamFor(CrystalBehavior.Mirrorwake);
            if (v != null) v.Show(muzzle, end, 0.07f, 1f);
            _crPulseHideAt = Time.time + PulseVisualSeconds;
            return end;
        }

        /// <summary>Muzzle to focus, then focus to the nearest visible hostile; each leg is blocked on its own.</summary>
        private Vector3 RedirectPulse(CrystalShotSpec spec, Vector3 muzzle, CrystalProfile p)
        {
            Vector3 focus = _crFocusPos;
            Vector3 leg1 = focus - muzzle;
            float len1 = leg1.magnitude;
            Vector3 dir1 = len1 > 1e-3f ? leg1 / len1 : transform.forward;
            CrystalBeamVisual v = CrystalBeamFor(CrystalBehavior.Mirrorwake);
            _crPulseHideAt = Time.time + PulseVisualSeconds;
            int directVictim = 0;
            // Leg one stops short of the focus's own surface so the wall it sits on is not counted as a blocker.
            int n = CrystalShots.Query(muzzle, PulseRadius, dir1, Mathf.Max(0f, len1 - 0.15f), transform, CrystalOwnVehicleBody, IsPlayer);
            if (n < 0)
            {
                CrystalDeny("Blocked");
                if (v != null) v.Show(muzzle, muzzle + dir1 * 0.3f, 0.07f, 1f);
                return muzzle + dir1 * 0.3f;
            }
            if (n > 0)
            {
                CrystalCastHit c = CrystalShots.CastAt(0);
                if (c.Blocker)
                {
                    // The focus is out of reach: the pulse ends on the blocker and the focus stays planted.
                    CrystalBlockerImpact(c.Collider, c.Point, c.Normal);
                    CrystalFlash("Focus blocked", new Color(1f, 0.6f, 0.4f));
                    if (v != null) v.Show(muzzle, c.Point, 0.07f, 1f);
                    return c.Point;
                }
                // Someone stands on the first leg: they take the direct pulse and the pulse stops there.
                directVictim = c.TargetId;
                bool killedDirect;
                CrystalStrike(spec, c.Target, c.Collider, c.Point, c.Normal, dir1, spec.Damage, DamageType.Bullet, 4f, spec.TakeProc(), true, out killedDirect);
                if (v != null) v.Show(muzzle, c.Point, 0.07f, 1f);
                return c.Point;
            }
            // The focus is reached and consumed: bend toward the nearest visible hostile within range of it.
            Vector3 start = focus + _crFocusNormal * 0.08f;
            RemoveFocus();
            CharacterBase victim = NearestRedirectTarget(start, directVictim);
            Vector3 end2;
            if (victim == null)
            {
                Vector3 bounce = Vector3.Reflect(dir1, _crFocusNormal);
                end2 = start + bounce * 6f;
                CrystalFlash("No target in sight", new Color(0.8f, 0.9f, 1f));
            }
            else
            {
                Vector3 chest = victim.ChestPosition;
                Vector3 leg2 = chest - start;
                float len2 = leg2.magnitude;
                Vector3 dir2 = len2 > 1e-3f ? leg2 / len2 : dir1;
                end2 = chest;
                int m = CrystalShots.Query(start, PulseRadius, dir2, len2 + 0.5f, transform, CrystalOwnVehicleBody, IsPlayer);
                if (m < 0) end2 = start + dir2 * 0.3f;
                else if (m > 0)
                {
                    CrystalCastHit c2 = CrystalShots.CastAt(0);
                    end2 = c2.Point;
                    if (c2.Blocker) CrystalBlockerImpact(c2.Collider, c2.Point, c2.Normal);
                    else if (CrystalShotRules.RedirectAllowed(directVictim, c2.TargetId))
                    {
                        bool killed;
                        CrystalStrike(spec, c2.Target, c2.Collider, c2.Point, c2.Normal, dir2, spec.Secondary, DamageType.Bullet, 4f, spec.TakeProc(), true, out killed);
                    }
                }
            }
            if (v != null) v.ShowTwo(muzzle, focus, end2, 0.07f, 1f);
            if (Game.Fx != null && CrystalShots.FxBudget()) Game.Fx.GlassBurst(focus, new Color(0.75f, 0.85f, 1f), 14);
            return end2;
        }

        /// <summary>The nearest hostile within RedirectRange of the focus that the focus can see (never protected actors, never the direct victim).</summary>
        private CharacterBase NearestRedirectTarget(Vector3 from, int directVictimId)
        {
            float range = CrystalFocusDb.RedirectRange;
            CharacterBase best = null;
            float bestD = float.MaxValue;
            _crRedirectWork.Clear();
            for (int i = 0; i < NpcController.All.Count; i++) _crRedirectWork.Add(NpcController.All[i]);
            for (int i = 0; i < Creature.All.Count; i++) _crRedirectWork.Add(Creature.All[i]);
            for (int i = 0; i < _crRedirectWork.Count; i++)
            {
                CharacterBase c = _crRedirectWork[i];
                if (c == null || c.IsDead) continue;
                Vector3 chest = c.ChestPosition;
                float d = (chest - from).magnitude;
                if (d > range || d >= bestD) continue;
                int id = c.Health != null ? CrystalShots.ActorId(c.Health) : 0;
                if (!CrystalShotRules.RedirectAllowed(directVictimId, id)) continue;
                CrystalShotRules.LockCandidate k = CrystalCandidate(c);
                k.Distance = d;
                k.AngleDeg = 0f;
                k.LineOfSight = !Physics.Linecast(from, chest, Layers.SightMask, QueryTriggerInteraction.Ignore);
                if (!CrystalShotRules.LockEligible(k, range, 180f)) continue;
                best = c;
                bestD = d;
            }
            _crRedirectWork.Clear();
            return best;
        }

        private void PlantFocus(Vector3 point, Vector3 normal)
        {
            _crFocusActive = true;
            _crFocusPos = point + normal * 0.02f;
            _crFocusNormal = normal.sqrMagnitude > 1e-4f ? normal.normalized : Vector3.up;
            _crFocusUntil = CrystalNow + CrystalFocusDb.FocusLifetime;
            if (_crFocusMarker == null)
            {
                Transform parent = CrystalShots.Instance != null ? CrystalShots.Instance.transform : null;
                try { _crFocusMarker = CrystalFocusModels.CreateFocusMarker(parent); }
                catch (System.Exception e) { Log.Exception("Mirrorwake focus marker", e); }
                if (_crFocusMarker != null) Layers.SetRecursive(_crFocusMarker, Layers.IgnoreRaycast);
            }
            if (_crFocusMarker != null)
            {
                _crFocusMarker.transform.position = _crFocusPos;
                _crFocusMarker.transform.rotation = Quaternion.LookRotation(_crFocusNormal, Mathf.Abs(_crFocusNormal.y) > 0.95f ? Vector3.forward : Vector3.up);
                _crFocusMarker.SetActive(true);
            }
            CrystalFlash("Focus planted", new Color(0.7f, 0.9f, 1f));
            CrystalSounds.PlayChargeReady(CrystalBehavior.Mirrorwake, _crFocusPos);
        }

        private void RemoveFocus()
        {
            _crFocusActive = false;
            _crAimOnFocus = false;
            if (_crFocusMarker != null) _crFocusMarker.SetActive(false);
        }

        /// <summary>The focus expires after FocusLifetime seconds of gameplay (paused time excluded).</summary>
        private void UpdateFocusMarker()
        {
            if (!_crFocusActive) return;
            // Gone after its lifetime, and with the player's death (no stale focus waits at the respawn).
            if (CrystalNow >= _crFocusUntil || (IsPlayer && Game.Player != null && Game.Player.Weapons == this && Game.Player.IsDead)) RemoveFocus();
        }

        /// <summary>Hides a fired pulse's beam after its short flash.</summary>
        private void UpdatePulseVisual()
        {
            if (_crPulseHideAt <= 0f || Time.time < _crPulseHideAt) return;
            _crPulseHideAt = 0f;
            CrystalBeamVisual v = _crBeams[(int)CrystalBehavior.Mirrorwake];
            if (v != null) v.Hide();
        }
    }
}
