using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Hero powers built on the travel powers and the crystal bar (numbers in HeroPowerRules): the dive slam (a second
    /// press of crouch in a dive arms it; the landing throws a shockwave), the Shard Gaze beam (the twelfth selectable
    /// power: cast and hold the power button; a focused crystal beam from the hands that ramps up on one target), and
    /// the Surge rush strike (the light attack at speed in a Surge: a blitz through a line of enemies). Energy is spent
    /// through Abilities, damage goes through the ordinary damage path, and the feel comes from the existing hit-stop,
    /// camera shake, rumble, damage numbers and effects. The grab and throw are in PlayerController.Grab.cs.
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>The Shard Gaze is burning (for the HUD and the pose).</summary>
        public bool BeamActive { get { return _fr == FreeRunState.Beam; } }
        /// <summary>Where the beam ends this frame (a target, a wall, or its full range).</summary>
        public Vector3 BeamEnd { get { return _beamEnd; } }
        /// <summary>Seconds left before the rush strike may be used again (0 when ready).</summary>
        public float RushCooldownLeft { get { return Mathf.Max(0f, _rushReadyAt - Time.time); } }

        private readonly Collider[] _heroBuf = new Collider[64];
        private readonly List<CharacterBase> _heroHits = new List<CharacterBase>(16);
        private Vector3 _rushDir = Vector3.forward;
        private Vector3 _rushFrom;
        private float _rushSpeedBefore;
        private float _rushAlong;
        private float _rushReadyAt;
        private WeaponId _rushWeapon = WeaponId.Count;
        private Vector3 _beamEnd;
        private float _beamTick;
        private float _beamOnTarget;
        private float _beamPrime;
        private CharacterBase _beamTarget;
        private float _beamSound;

        // ================================================================== dive slam

        /// <summary>A second press of crouch during a dive: arm the slam (Super Leap unlocked, 20 crystal energy).</summary>
        private bool TryArmSlam()
        {
            if (_slamArmed || !LeapOpen || Game.Abilities == null) return false;
            if (!Game.Abilities.SpendHeroEnergyH8S(HeroPowerRules.SlamEnergy)) return false;
            _slamArmed = true;
            TravelSound(TravelSounds.Clip.Whoosh, 0.55f, 1.45f);
            if (Game.Fx != null) Game.Fx.Flash(ChestPosition, Abilities.ShardGazeColor, 3f, 7f, 0.25f);
            Pad.Rumble(0.35f, 0.2f, 0.12f);
            return true;
        }

        /// <summary>The slam lands: everyone in the shockwave is thrown, hostiles are hurt, cars are shoved, the ground rings.</summary>
        private void DiveSlamImpact(Vector3 p, float fallSpeed)
        {
            float radius = HeroPowerRules.SlamRadius(fallSpeed);
            float scale = Abilities.PowerScale;
            int n = Physics.OverlapSphereNonAlloc(p, radius + 2f, _heroBuf, (1 << Layers.Characters) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
            _heroHits.Clear();
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _heroBuf[i].GetComponentInParent<CharacterBase>();
                if (c == null || c == this || c.IsDead || c.InVehicle || _heroHits.Contains(c)) continue;
                if (WeaponController.CrystalProtected(c)) continue;
                Vector3 to = c.transform.position - p;
                float dist = MathUtil.Flat(to).magnitude;
                if (dist > radius + c.BodyRadius) continue;
                _heroHits.Add(c);
                Vector3 push = HeroPowerRules.SlamPush(to, dist, radius, fallSpeed);
                if (c.IsHostileTo())
                {
                    DamageInfo info = DamageInfo.Make(HeroPowerRules.SlamDamage(fallSpeed, dist, radius) * scale, c.ChestPosition, push, gameObject, DamageType.Power);
                    info.Force = push.magnitude * 0.5f;
                    info.Flags = DamageFlags.Knockdown | DamageFlags.Unblockable | DamageFlags.Heavy;
                    info.Poise = 999f;
                    c.Health.TakeDamage(info);
                    ResonanceSystem.PrimeSpell(c, "SHARD_GAZE");
                }
                c.Knockdown(push, c.ChestPosition, 1.6f);
                NpcController npc = c as NpcController;
                if (npc != null) npc.StartleH8S(p);
            }
            // Cars and debris are shoved outward (never the player's own car).
            int m = Physics.OverlapSphereNonAlloc(p, radius, _heroBuf, (1 << Layers.Vehicles) | (1 << Layers.Debris), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < m; i++)
            {
                Rigidbody rb = _heroBuf[i].attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;
                Vector3 to = rb.worldCenterOfMass - p;
                float dist = to.magnitude;
                Vector3 dir = dist > 0.05f ? to / dist : Vector3.up;
                float k = 1f - Mathf.Clamp01(dist / radius);
                rb.AddForce((dir * 4f + Vector3.up * 2.5f) * k * Mathf.Lerp(0.6f, 1.4f, HeroPowerRules.SlamWeight(fallSpeed)), ForceMode.VelocityChange);
            }
            float w = HeroPowerRules.SlamWeight(fallSpeed);
            if (Game.Fx != null)
            {
                Game.Fx.TravelLanding(p, 1.6f + 1.2f * w);
                Game.Fx.TravelBoom(p + Vector3.up * 0.25f, transform.forward);
                Game.Fx.Flash(p + Vector3.up * 0.5f, Abilities.ShardGazeColor, 4f + 4f * w, 10f + 6f * w, 0.35f);
                Game.Fx.Dust(p, Vector3.up * 3f, 1.5f + w);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, p, 0.7f + 0.3f * w, 0.08f, 60f);
            CombatDirector.HitStop(0.08f);
            TravelShake(0.45f + 0.35f * w, 1f, 0.6f, 0.4f);
            CombatDirector.MarkCombat();
            Threats.Report(p, 30f, gameObject, ThreatKind.Power);
            if (Game.Hud != null && _heroHits.Count > 0) Game.Hud.HitMarker(false);
        }

        // ================================================================== Surge rush strike

        /// <summary>The light attack at speed in a Surge: the rush strike (15 crystal energy, a short cooldown).</summary>
        private bool TryRushStrike(float speed, Vector3 dir)
        {
            if (Game.Abilities == null) return false;
            if (!HeroPowerRules.RushAllowed(speed, Time.time - _rushReadyAt + HeroPowerRules.RushCooldown, Game.Abilities.Energy))
            {
                if (speed >= HeroPowerRules.RushMinSpeed && Time.time >= _rushReadyAt && Game.Hud != null) Game.Hud.Notify("Not enough crystal energy.", 1.2f);
                return false;
            }
            if (!Game.Abilities.SpendHeroEnergyH8S(HeroPowerRules.RushEnergy)) return false;
            _rushReadyAt = Time.time + HeroPowerRules.RushCooldown;
            _fr = FreeRunState.RushStrike;
            _frTime = 0f;
            _rushDir = dir.sqrMagnitude > 0.01f ? MathUtil.Flat(dir).normalized : transform.forward;
            _rushFrom = transform.position;
            _rushSpeedBefore = speed;
            _rushAlong = 0f;
            _heroHits.Clear();
            // Fists for the strike, so the light attack never also fires a held gun; the weapon comes back after.
            _rushWeapon = WeaponId.Count;
            WeaponInventory inv = Weapons.Inventory;
            if (Weapons.Current.Fire != FireKind.Melee && inv.Usable(WeaponId.Fists))
            {
                _rushWeapon = inv.Current;
                inv.Select(WeaponId.Fists);
            }
            _yaw = MathUtil.YawOf(_rushDir);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            Combat.ReleaseLock();
            TravelSound(TravelSounds.Clip.Boom, 0.6f, 1.3f);
            if (Game.Fx != null) Game.Fx.TravelBoom(transform.position + Vector3.up * 0.9f, _rushDir);
            TravelShake(0.2f, 0.6f, 0.3f, 0.2f);
            Threats.Report(transform.position, 25f, gameObject, ThreatKind.Power);
            return true;
        }

        private void UpdateRushStrike(float dt, bool canControl)
        {
            _frTime += dt;
            Combat.Tick(dt, false, Vector3.zero);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            float phase = _frTime / HeroPowerRules.RushSeconds;
            float along = HeroPowerRules.RushProgress(phase);
            Vector3 want = _rushFrom + _rushDir * along;
            Vector3 before = transform.position;
            want.y = before.y;
            CollisionFlags flags = Controller.Move(want - before + Vector3.down * (2f * dt));
            Vector3 moved = transform.position - before;
            float gained = Vector3.Dot(moved, _rushDir);
            RushHits(_rushAlong, _rushAlong + gained + 0.5f);
            _rushAlong += Mathf.Max(0f, gained);
            if (Game.Fx != null && Rig != null) Game.Fx.TravelAfterimage(Rig.Body, 0.35f);
            bool blocked = (flags & CollisionFlags.Sides) != 0 && gained < (along - _rushAlong) * 0.2f && _frTime > 0.05f;
            if (phase >= 1f || blocked)
            {
                EndRushStrike(blocked);
                return;
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            Anim.Velocity = _rushDir * _rushSpeedBefore;
            Anim.VerticalVelocity = 0f;
            // Airborne for the pose solver: the blitz is a horizontal lunge, nothing is planted until it ends.
            Anim.Grounded = false;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }

        /// <summary>Everyone in the stretch of the line swept this frame is struck once: hostiles hurt, all thrown aside.</summary>
        private void RushHits(float fromAlong, float toAlong)
        {
            Vector3 centre = _rushFrom + _rushDir * ((fromAlong + toAlong) * 0.5f);
            float half = (toAlong - fromAlong) * 0.5f + HeroPowerRules.RushHalfWidth + 2f;
            int n = Physics.OverlapSphereNonAlloc(centre + Vector3.up, half, _heroBuf, (1 << Layers.Characters) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
            float scale = Abilities.PowerScale;
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _heroBuf[i].GetComponentInParent<CharacterBase>();
                if (c == null || c == this || c.IsDead || c.InVehicle || _heroHits.Contains(c)) continue;
                if (WeaponController.CrystalProtected(c)) continue;
                Vector3 rel = c.transform.position - _rushFrom;
                if (!HeroPowerRules.InRushCorridor(rel, _rushDir, fromAlong, toAlong, c.BodyRadius)) continue;
                _heroHits.Add(c);
                Vector3 side = rel - _rushDir * Vector3.Dot(rel, _rushDir);
                float sideSign = Vector3.Dot(side, MathUtil.RightOf(_rushDir)) >= 0f ? 1f : -1f;
                Vector3 push = HeroPowerRules.RushPush(_rushDir, sideSign);
                bool hostile = c.IsHostileTo();
                if (hostile)
                {
                    DamageInfo info = DamageInfo.Make(HeroPowerRules.RushDamage * scale, c.ChestPosition, _rushDir, gameObject, DamageType.Melee);
                    info.Force = 6f;
                    info.Flags = DamageFlags.Knockdown | DamageFlags.Heavy;
                    info.Poise = 200f;
                    c.Health.TakeDamage(info);
                }
                c.Knockdown(push, c.ChestPosition, 1.3f);
                NpcController npc = c as NpcController;
                if (npc != null)
                {
                    bool cop = npc.Role == NpcRole.Police || npc.Role == NpcRole.Swat;
                    if (cop && Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.AttackCop, npc.transform.position, npc.gameObject);
                }
                CombatDirector.HitStop(HeroPowerRules.RushHitStop);
                if (Game.Fx != null) Game.Fx.Sparks(c.ChestPosition, -_rushDir, 4);
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Punch, c.ChestPosition, 0.8f, 0.1f, 30f);
                if (Game.Cam != null) Game.Cam.Shake(0.12f);
                Pad.Rumble(0.6f, 0.4f, 0.1f);
                if (hostile) CombatDirector.MarkCombat();
                if (Game.Hud != null) Game.Hud.HitMarker(!c.Health.IsAlive);
            }
        }

        private void EndRushStrike(bool blocked)
        {
            if (_rushWeapon != WeaponId.Count && Weapons != null && Weapons.Inventory.Usable(_rushWeapon)) Weapons.Inventory.Select(_rushWeapon);
            _rushWeapon = WeaponId.Count;
            FreeRunEnd();
            if (blocked)
            {
                EndSurge();
                _vel = -_rushDir * 2f;
                _vy = 1.5f;
                _grounded = false;
                StartLanding(0.45f);
                TravelSound(TravelSounds.Clip.LandHeavy, 0.6f, 1.1f);
                return;
            }
            // Back into the Surge at speed (the Surge code brakes it if the stick is centred).
            _vel = _rushDir * Mathf.Max(_rushSpeedBefore * 0.9f, TravelMath.SurgeEndSpeed + 2f);
            _grounded = Controller.isGrounded || GroundProbe();
            _vy = _grounded ? -2f : 0f;
        }

        // ================================================================== Shard Gaze (the beam)

        /// <summary>Cast from Abilities: starts the beam if Malik is on his feet or hovering. False refuses the cast.</summary>
        public bool BeginBeamH8S()
        {
            if (IsDead || IsRagdolled || InVehicle || _climbing || _enter != EnterPhase.None) return false;
            if (_fr != FreeRunState.None) return false;
            if (_travel == TravelMode.Surge || _travel == TravelMode.Air) return false;
            if (Combat.State != PlayerCombat.Phase.Free) return false;
            _fr = FreeRunState.Beam;
            _frTime = 0f;
            _beamTick = 0f;
            _beamOnTarget = 0f;
            _beamPrime = 0f;
            _beamTarget = null;
            _beamSound = 0f;
            Crouched = false;
            Combat.ReleaseLock();
            if (Game.Cam != null) _yaw = Game.Cam.Yaw;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            SetHandsBusy(true);
            return true;
        }

        private void UpdateBeam(float dt, bool canControl)
        {
            _frTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            bool held = canControl && Act.CastPowerHeld;
            float energy = Game.Abilities != null ? Game.Abilities.Energy : 0f;
            if (!HeroPowerRules.BeamContinues(held, energy, _frTime) || IsRagdolled)
            {
                EndBeam();
                return;
            }
            if (Game.Abilities != null) Game.Abilities.AddEnergy(-HeroPowerRules.BeamEnergyPerSecond * dt);
            bool flying = _travel == TravelMode.Flight;
            // Face the camera; a slow strafe on foot, a hover in flight.
            if (Game.Cam != null) _yaw = Mathf.MoveTowardsAngle(_yaw, Game.Cam.Yaw, 1100f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (flying)
            {
                _vel = Vector3.MoveTowards(_vel, wish * 2f, 8f * dt);
                _vy = Mathf.MoveTowards(_vy, Mathf.Cos(_frTime * 2f) * 0.3f, 10f * dt);
                Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
            }
            else
            {
                _vel = Vector3.MoveTowards(_vel, wish * HeroPowerRules.BeamMoveSpeed, 14f * dt);
                _vy = _grounded ? -2f : _vy - TravelMath.FallGravity * dt;
                Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
                _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            }
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            BeamFire(dt);
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = flying ? 0f : _vy;
            Anim.Grounded = !flying && _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }

        /// <summary>The beam itself: a thin sweep from the hands toward the aim point; damage in ticks that ramp on one target.</summary>
        private void BeamFire(float dt)
        {
            Vector3 origin = ChestPosition + transform.forward * 0.45f + Vector3.up * 0.05f;
            Vector3 dir = (AimPoint - origin);
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            RaycastHit hit;
            CharacterBase target = null;
            Vector3 end = origin + dir * HeroPowerRules.BeamRange;
            if (Physics.SphereCast(origin, HeroPowerRules.BeamRadius, dir, out hit, HeroPowerRules.BeamRange, Layers.ShootMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform))
                {
                    end = hit.point;
                    target = hit.collider.GetComponentInParent<CharacterBase>();
                    if (target != null && (target.IsDead || target == this || WeaponController.CrystalProtected(target))) target = null;
                }
            }
            _beamEnd = end;
            if (target != null && target == _beamTarget) _beamOnTarget += dt;
            else _beamOnTarget = 0f;
            _beamTarget = target;
            _beamTick += dt;
            _beamPrime += dt;
            if (_beamTick >= HeroPowerRules.BeamTick)
            {
                _beamTick -= HeroPowerRules.BeamTick;
                if (target != null)
                {
                    DamageInfo info = DamageInfo.Make(HeroPowerRules.BeamTickDamage(_beamOnTarget, HeroPowerRules.BeamTick) * Abilities.PowerScale, end, dir, gameObject, DamageType.Power);
                    info.Poise = 6f;
                    target.Health.TakeDamage(info);
                    if (_beamPrime >= HeroPowerRules.BeamPrimeEvery)
                    {
                        _beamPrime = 0f;
                        ResonanceSystem.PrimeSpell(target, "SHARD_GAZE");
                    }
                    if (target.IsHostileTo()) CombatDirector.MarkCombat();
                    if (Game.Hud != null) Game.Hud.HitMarker(!target.Health.IsAlive);
                }
                if (Game.Fx != null)
                {
                    Game.Fx.Sparks(end, -dir, 2);
                    Game.Fx.Flash(end, Abilities.ShardGazeColor, 1.5f, 4f, 0.12f);
                }
            }
            if (Game.Abilities != null) Game.Abilities.BeamVisualH8S(origin, end, Abilities.ShardGazeColor);
            _beamSound -= dt;
            if (_beamSound <= 0f)
            {
                _beamSound = 0.28f;
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.22f);
                Pad.Rumble(0.12f, 0.25f, 0.3f);
            }
            if (Game.Cam != null) Game.Cam.Shake(0.02f);
        }

        private void EndBeam()
        {
            FreeRunEnd();
            _beamTarget = null;
            if (_travel != TravelMode.Flight)
            {
                _grounded = Controller.isGrounded || GroundProbe();
                if (_grounded) _vy = -2f;
            }
            SetFreeRunGroundAnim();
        }

        // ================================================================== presentation (from FreeRunPresentation)

        /// <summary>The hero poses: the beam's hands-forward stance with the aim pitch, the rush strike's fists-first lunge, the grab.</summary>
        private void HeroPresentation(ref TravelPose pose, ref Vector3 lean, ref float speed)
        {
            switch (_fr)
            {
                case FreeRunState.Beam:
                {
                    pose = TravelPose.Beam;
                    Vector3 d = _beamEnd - ChestPosition;
                    float pitch = d.sqrMagnitude > 0.01f ? -Mathf.Asin(Mathf.Clamp(d.normalized.y, -1f, 1f)) * Mathf.Rad2Deg : 0f;
                    lean = new Vector3(Mathf.Clamp01(_frTime / 0.3f), pitch, 0f);
                    break;
                }
                case FreeRunState.RushStrike:
                    pose = TravelPose.RushStrike;
                    speed = _rushSpeedBefore;
                    lean = new Vector3(Mathf.Clamp01(_frTime / HeroPowerRules.RushSeconds), 0f, 0f);
                    break;
                case FreeRunState.Grab:
                    pose = TravelPose.Grab;
                    lean = new Vector3(GrabLiftPhase, GrabThrowPhase, 0f);
                    break;
            }
        }
    }
}
