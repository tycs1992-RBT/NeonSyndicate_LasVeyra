using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The fused forms in motion: handling applied to the chassis every frame (with the Ventwing glide and the hot-vent
    /// penalty, the Kilnback brace's harder shell), the U ability of each form, its hits on hostiles only (credited once
    /// to the player as fused-vehicle damage), the rider's bounded protection and the pose the form view draws.
    /// </summary>
    public sealed partial class FusionSystem
    {
        private const float CoyoteTime = 0.4f;
        private readonly Collider[] _hits = new Collider[24];
        private readonly HashSet<CharacterBase> _braceHit = new HashSet<CharacterBase>();
        private float _dischargeAt = -1f;

        private void UpdateFused(float dt)
        {
            if (_vehicle == null) return;
            string form = _machine.Form;
            FormMeters m = _machine.Meters(form);
            FormAbilitySpec spec = FusionRules.Ability(form);
            // Ventwing glide: hold U in the air while the vents have heat to spend.
            bool airborne = _vehicle.GroundedWheels == 0;
            bool wantGlide = form == FusionIds.Ventwing && airborne && Act.CompanionAssistHeld && !m.Overheated && m.Heat < 1f;
            if (wantGlide && !_gliding)
            {
                Underheart04ESounds.Play(UhSfx.VentGlide, _vehicle.transform.position, 0.9f, 1f);
                AbilityUsedOnce(form, false);
            }
            if (!wantGlide && _gliding && m.Overheated) Toast("Vents overheated. The wings fold until they cool.", true);
            _gliding = wantGlide;
            ApplyHandling(dt);
            if (form == FusionIds.Kilnback)
            {
                bool bracing = m.Active > 0f;
                if (bracing) BraceHits(spec);
                else _braceHit.Clear();
                if (_view != null) _view.SetHot(bracing);
            }
            if (_dischargeAt > 0f && Time.time >= _dischargeAt)
            {
                _dischargeAt = -1f;
                Discharge(spec);
            }
            if (_view != null)
            {
                float speed = _vehicle.Speed;
                FormPoseInput pose = _view.Pose;
                pose.Spread = Mathf.Max(FormPose.IdleSpread, Mathf.Clamp01((speed - 4f) / 18f) * 0.85f);
                pose.Glide = _gliding ? 1f : 0f;
                pose.Brace = form == FusionIds.Kilnback && m.Active > 0f ? 1f : 0f;
                pose.Steer = Mathf.Clamp(_vehicle.SteerInput, -1f, 1f);
                pose.Flare = form == FusionIds.Coilrunner && m.Charges > 0 && m.Cooldown <= 0f ? 1f : 0.2f;
                pose.Vane = Mathf.Clamp(_vehicle.Throttle * 0.6f + (_gliding ? 0.4f : 0f), -1f, 1f);
                pose.Pulse = Mathf.Clamp01(0.35f + m.Heat * 0.5f + Mathf.Sin(Time.time * (2.5f + m.Heat * 8f)) * 0.25f);
                pose.Unfold = 1f;
                _view.Pose = pose;
                _view.SetTrails(_gliding);
            }
        }

        /// <summary>Writes this frame's handling to the chassis (form profile, glide, hot vents, brace).</summary>
        private void ApplyHandling(float dt)
        {
            if (_vehicle == null) return;
            string form = _machine.Form;
            PersonalGarage g = Game.Garage;
            ChassisRecord r = g != null ? g.RecordOf(_vehicle) : null;
            string profile = g != null ? g.ActiveProfile : FusionRules.ProfileUpdraft;
            FusedHandling h = FusionRules.Handling(form, profile, r != null ? r.Tune : "");
            h = FusionRules.WithGlide(h, form, profile, _gliding);
            FormMeters m = _machine.Meters(form);
            // Hot vents choke the thrust: the cost of leaning on bursts and glides.
            if (form == FusionIds.Ventwing && m.Heat > 0.75f) h.Drive *= 0.85f;
            h.DamageScale = FusionRules.ChassisDamageScale(form, form == FusionIds.Kilnback && m.Active > 0f);
            _vehicle.FusionHandling = h;
        }

        /// <summary>The U ability of the active form (refused with a reason when the meters or the space say no).</summary>
        private void UseAbility()
        {
            if (_vehicle == null) return;
            string form = _machine.Form;
            FormAbilitySpec spec = FusionRules.Ability(form);
            FormMeters m = _machine.Meters(form);
            // In the air the Ventwing's U is the glide (held), not a burst: no refusal noise while it engages.
            if (form == FusionIds.Ventwing && _vehicle.GroundedWheels < 2 && _airTime > CoyoteTime)
            {
                if (m.Overheated) Toast("Vents overheated. Let them cool before gliding.", true);
                return;
            }
            string why = FusionRules.WhyNotUse(m, spec);
            if (why.Length == 0 && form == FusionIds.Ventwing && !HeadroomFor(spec.NeedsHeadroom)) why = "No room overhead for a vent burst.";
            if (why.Length > 0)
            {
                Toast(why, true);
                return;
            }
            string reason;
            if (!_machine.TryUseAbility(out reason))
            {
                Toast(reason, true);
                return;
            }
            Vector3 pos = _vehicle.transform.position;
            Vector3 fwd = _vehicle.transform.forward;
            switch (form)
            {
                case FusionIds.Ventwing:
                    _vehicle.QueueFusionImpulse(spec.ForwardDeltaV, spec.UpDeltaV, spec.SpeedCapFactor);
                    Underheart04ESounds.Play(UhSfx.VentBurst, pos, 1f, 1f);
                    if (Game.Fx != null)
                    {
                        for (int i = -1; i <= 1; i += 2) Game.Fx.FirePuff(_vehicle.transform.TransformPoint(new Vector3(i * 0.22f, 0.45f, -0.6f)), 0.35f);
                        Game.Fx.Dust(pos, Vector3.up * 2f, 1.2f);
                    }
                    Scorch(spec, pos - fwd * 1.2f);
                    if (Game.Cam != null) Game.Cam.Shake(0.15f);
                    break;
                case FusionIds.Kilnback:
                    _braceHit.Clear();
                    _vehicle.QueueFusionImpulse(spec.ForwardDeltaV, 0f, spec.SpeedCapFactor);
                    Underheart04ESounds.Play(UhSfx.BraceRam, pos, 1f, 1f);
                    if (Game.Cam != null) Game.Cam.Shake(0.18f);
                    break;
                case FusionIds.Coilrunner:
                    _vehicle.QueueFusionImpulse(spec.ForwardDeltaV, 0f, spec.SpeedCapFactor);
                    Underheart04ESounds.Play(UhSfx.ArcDash, pos, 1f, 1f);
                    _dischargeAt = Time.time + 0.25f;
                    break;
            }
            AbilityUsedOnce(form, true);
        }

        private void AbilityUsedOnce(string form, bool counted)
        {
            if (!counted) return;
            RaiseAbilityUsed(form);
        }

        private bool HeadroomFor(float need)
        {
            if (need <= 0f || _vehicle == null) return true;
            Vector3 p = _vehicle.transform.position;
            if (Game.Underheart != null && WorldBounds.IsUnderground(p))
            {
                float headroom;
                if (!Game.Underheart.VehicleSpaceAt(p, out headroom)) return false;
                if (headroom > 0.01f && headroom < need) return false;
            }
            return !Physics.Raycast(p + Vector3.up * 0.5f, Vector3.up, need, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== hits (hostiles only, one attribution)

        /// <summary>Every hostile within reach: hostile NPCs and targetable creatures (never civilians, police while not
        /// hunting the player, companions or the player).</summary>
        private static void Hostiles(Vector3 center, float radius, List<CharacterBase> output)
        {
            output.Clear();
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || !n.IsHostileToPlayer || n.InVehicle) continue;
                if ((n.transform.position - center).sqrMagnitude > radius * radius) continue;
                output.Add(n);
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Targetable) continue;
                float r = radius + c.Radius;
                if ((c.transform.position - center).sqrMagnitude > r * r) continue;
                output.Add(c);
            }
        }

        private static readonly List<CharacterBase> _found = new List<CharacterBase>();

        /// <summary>Clears the shared hit list (FusionStatics.ResetStatics, so no stale character survives a play-mode restart).</summary>
        internal static void ResetStaticLists() { _found.Clear(); }

        private void Hit(CharacterBase c, float amount, DamageType type, Vector3 dir, float stagger, float bossStagger, bool knock)
        {
            if (c == null || c.Health == null || Game.Player == null) return;
            DamageInfo info = DamageInfo.Make(amount, c.ChestPosition, dir, Game.Player.gameObject, type);
            info.Origin = DamageOrigin.FusedVehicle;
            info.Force = 6f;
            info.Poise = stagger > 0f ? 60f : 0f;
            c.Health.TakeDamage(info);
            Creature cr = c as Creature;
            if (cr != null && stagger > 0f && !c.IsDead) cr.Stagger(cr.IsBossLike ? bossStagger : stagger);
            if (knock && cr == null && !c.IsDead) c.Knockdown(dir.normalized * 5f + Vector3.up * 2f, c.ChestPosition, 1.1f);
        }

        /// <summary>Ventwing: the vent burst scorches hostiles right behind and under the chassis.</summary>
        private void Scorch(FormAbilitySpec spec, Vector3 center)
        {
            Hostiles(center, spec.Radius, _found);
            foreach (CharacterBase c in _found) Hit(c, spec.Damage, DamageType.Fire, c.transform.position - center, 0f, 0f, false);
        }

        /// <summary>Kilnback: while braced, hostiles in the box ahead are struck once each (shoved, creatures staggered;
        /// bosses only briefly). The shove follows physics: nothing passes through world geometry.</summary>
        private void BraceHits(FormAbilitySpec spec)
        {
            if (_vehicle == null) return;
            Transform t = _vehicle.transform;
            float len = spec.Radius;
            Vector3 center = t.TransformPoint(new Vector3(0f, 1.1f, _vehicle.Def.Length * 0.5f + len * 0.5f - 0.2f));
            int n = Physics.OverlapBoxNonAlloc(center, new Vector3(1.3f, 1.1f, len * 0.5f), _hits, t.rotation, 1 << Layers.Characters, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _hits[i] != null ? _hits[i].GetComponentInParent<CharacterBase>() : null;
                if (c == null || c.IsPlayer || c.IsDead || _braceHit.Contains(c)) continue;
                NpcController npc = c as NpcController;
                Creature cr = c as Creature;
                bool hostile = (npc != null && npc.IsHostileToPlayer) || (cr != null && cr.Targetable);
                if (!hostile) continue;
                _braceHit.Add(c);
                Hit(c, spec.Damage, DamageType.Vehicle, t.forward, spec.Stagger, spec.BossStagger, true);
                if (Game.Fx != null) Game.Fx.Sparks(c.ChestPosition, -t.forward * 3f, 14);
            }
        }

        /// <summary>Coilrunner: the discharge after the dash strikes hostiles in line of sight around the chassis.</summary>
        private void Discharge(FormAbilitySpec spec)
        {
            if (_vehicle == null) return;
            Vector3 from = _vehicle.transform.TransformPoint(new Vector3(0f, 1f, 0f));
            Hostiles(_vehicle.transform.position, spec.Radius, _found);
            List<Vector3> struck = new List<Vector3>();
            foreach (CharacterBase c in _found)
            {
                if (struck.Count >= 6) break;
                Vector3 to = c.ChestPosition;
                if (Physics.Linecast(from, to, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                Hit(c, spec.Damage, DamageType.Power, to - from, spec.Stagger, spec.BossStagger, false);
                struck.Add(to);
                if (Game.Fx != null) Game.Fx.Sparks(to, Vector3.up * 2f, 10);
            }
            if (_view != null) _view.Discharge(struck.ToArray());
            if (struck.Count > 0 && Game.Fx != null) Game.Fx.Flash(from, new Color(0.7f, 0.65f, 1f), 2.5f, 9f, 0.15f);
        }

        /// <summary>The rider's bounded protection while fused (the Kilnback shell; harder from the front while braced).
        /// Never cancels a hit: no form makes the rider invulnerable.</summary>
        private bool RiderDamage(ref DamageInfo info)
        {
            if (!IsFused || _vehicle == null || Game.Player == null || Game.Player.CurrentVehicle != _vehicle) return true;
            string form = _machine.Form;
            bool bracing = form == FusionIds.Kilnback && _machine.Meters(form).Active > 0f;
            Vector3 incoming = info.Direction.sqrMagnitude > 1e-6f ? -info.Direction.normalized : Vector3.zero;
            bool frontal = incoming != Vector3.zero && Vector3.Dot(MathUtil.Flat(incoming).normalized, MathUtil.Flat(_vehicle.transform.forward).normalized) > Mathf.Cos(70f * Mathf.Deg2Rad);
            info.Amount *= FusionRules.RiderDamageScale(form, bracing, frontal);
            return true;
        }
    }
}
