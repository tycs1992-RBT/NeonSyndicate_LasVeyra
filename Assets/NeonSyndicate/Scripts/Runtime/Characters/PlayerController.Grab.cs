using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Super-strength grab and throw (numbers in HeroPowerRules): the use button on a person in reach who has noticed
    /// Malik (an unnoticed one gets the takedown instead) lifts them overhead; Malik walks slowly with them, and the use
    /// button or the light attack throws them along the camera, the thrown body knocking down whoever it hits. Crouched
    /// beside a light, empty vehicle (a hover bike, a small car) the use button lifts the vehicle instead and throws it
    /// the same way; the vehicle keeps its own physics (its body is steered to the carry point every physics step and
    /// let go with the throw), so its collisions and damage stay the vehicle system's. Opens with the Super Leap.
    /// </summary>
    public sealed partial class PlayerController
    {
        /// <summary>The person being carried (null for none).</summary>
        public NpcController HeldPerson { get { return _fr == FreeRunState.Grab ? _heldNpc : null; } }
        /// <summary>The vehicle being carried (null for none).</summary>
        public Vehicle HeldVehicle { get { return _fr == FreeRunState.Grab ? _heldVehicle : null; } }
        /// <summary>0..1 through the lift, for the pose.</summary>
        public float GrabLiftPhase { get { return Mathf.Clamp01(_frTime / HeroPowerRules.LiftSeconds); } }
        /// <summary>0..1 through the throw, for the pose (0 while carrying).</summary>
        public float GrabThrowPhase { get { return _throwing ? Mathf.Clamp01(_throwT / 0.3f) : 0f; } }

        private NpcController _heldNpc;
        private Vehicle _heldVehicle;
        private Vector3 _liftFrom;
        private bool _throwing;
        private float _throwT;
        private CharacterBase _thrownBody;
        private float _thrownUntil;
        private readonly System.Collections.Generic.List<CharacterBase> _thrownHit = new System.Collections.Generic.List<CharacterBase>(8);

        // ================================================================== starting

        /// <summary>The use button on a person who has noticed Malik, or crouched beside a light empty vehicle.</summary>
        private bool TryStartGrab()
        {
            if (!LeapOpen || Focus != null || IsDead) return false;
            if (!Crouched && EnterCandidate != null) return false;
            if (Crouched)
            {
                Vehicle v = FindLiftableVehicle();
                if (v != null)
                {
                    StartVehicleGrab(v);
                    return true;
                }
            }
            if (EnterCandidate != null) return false;
            NpcController n = FindGrabTarget();
            if (n == null) return false;
            StartPersonGrab(n);
            return true;
        }

        private NpcController FindGrabTarget()
        {
            Vector3 pos = transform.position;
            Vector3 aim = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            NpcController best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || n.IsRagdolled || n.InVehicle || n.IsCreature || n.IsChampion) continue;
                if (n.Role == NpcRole.Companion || n.StealthHeldH8S) continue;
                Vector3 to = MathUtil.Flat(n.transform.position - pos);
                float dist = to.magnitude;
                if (dist > HeroPowerRules.GrabReach || Mathf.Abs(n.transform.position.y - pos.y) > HeroPowerRules.GrabHeightTolerance) continue;
                float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(aim, to) : 0f;
                if (angle > HeroPowerRules.GrabFrontAngle) continue;
                float score = dist + angle * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }
            return best;
        }

        private Vehicle FindLiftableVehicle()
        {
            Vector3 p = transform.position;
            Vehicle best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle v = Vehicle.All[i];
                if (v == null || v.Body == null || v.Def == null || v == CurrentVehicle) continue;
                if (!HeroPowerRules.VehicleLiftable(v.Def.Mass, v.OccupantCount(), v.IsDestroyed, v.MissionCritical, v.Speed)) continue;
                float d = MathUtil.FlatDistance(p, v.transform.position) - Mathf.Max(v.Def.Width, v.Def.Length) * 0.5f;
                if (d > HeroPowerRules.VehicleReach || Mathf.Abs(v.transform.position.y - p.y) > 2.5f) continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = v;
                }
            }
            return best;
        }

        private void BeginGrabState()
        {
            _travel = TravelMode.None;
            SetSurgeStepping(false);
            _fr = FreeRunState.Grab;
            _frTime = 0f;
            _throwing = false;
            _throwT = 0f;
            Crouched = false;
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Combat.ReleaseLock();
            SetHandsBusy(true);
        }

        private void StartPersonGrab(NpcController n)
        {
            BeginGrabState();
            _heldNpc = n;
            _heldVehicle = null;
            _liftFrom = n.transform.position;
            n.BeginCarryH8S();
            _yaw = MathUtil.YawOf(MathUtil.Flat(n.transform.position - transform.position));
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Swing, ChestPosition, 0.5f, 0.1f, 15f);
            Pad.Rumble(0.4f, 0.2f, 0.15f);
            Threats.Report(transform.position, 12f, gameObject, ThreatKind.Violence);
        }

        private void StartVehicleGrab(Vehicle v)
        {
            BeginGrabState();
            _heldNpc = null;
            _heldVehicle = v;
            _liftFrom = v.Body.position;
            v.Handbrake = true;
            v.Body.WakeUp();
            _yaw = MathUtil.YawOf(MathUtil.Flat(v.transform.position - transform.position));
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            TravelSound(TravelSounds.Clip.LandHeavy, 0.6f, 0.8f);
            TravelShake(0.15f, 0.6f, 0.2f, 0.3f);
            if (Game.Wanted != null && v.IsPolice) Game.Wanted.ReportCrime(CrimeType.StealCopCar, v.transform.position, v.gameObject);
        }

        // ================================================================== carrying

        private void UpdateGrab(float dt, bool canControl)
        {
            _frTime += dt;
            float stick;
            Vector3 wish = TravelWish(canControl, out stick);
            Combat.Tick(dt, false, wish);
            Aiming = false;
            AimingDownSights = false;
            Sprinting = false;
            Crouched = false;
            bool person = _heldNpc != null;
            if ((person && (_heldNpc.IsDead || _heldNpc.IsRagdolled)) || (!person && (_heldVehicle == null || _heldVehicle.IsDestroyed)))
            {
                ReleaseHeld();
                FreeRunEnd();
                SetFreeRunGroundAnim();
                return;
            }
            if (_throwing)
            {
                _throwT += dt;
                if (_throwT >= 0.3f)
                {
                    FreeRunEnd();
                    SetFreeRunGroundAnim();
                    return;
                }
            }
            else if (_frTime >= HeroPowerRules.LiftSeconds && canControl)
            {
                if (Act.Hands || Act.StrikePressed)
                {
                    ThrowHeld();
                }
                else if (Act.Crouch || _frTime > HeroPowerRules.HoldMaxSeconds + HeroPowerRules.LiftSeconds)
                {
                    DropHeld();
                    FreeRunEnd();
                    SetFreeRunGroundAnim();
                    return;
                }
            }
            // Walk slowly, turning with the stick (or the camera while throwing).
            float speed = _throwing ? 0f : HeroPowerRules.GrabWalkSpeed;
            _vel = Vector3.MoveTowards(_vel, wish * speed, 10f * dt);
            if (_throwing && Game.Cam != null) _yaw = Mathf.MoveTowardsAngle(_yaw, Game.Cam.Yaw, 900f * dt);
            else if (_vel.sqrMagnitude > 0.04f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(_vel), 360f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _vy = _grounded ? -2f : _vy - TravelMath.FallGravity * dt;
            Controller.Move((_vel + Vector3.up * _vy + _external) * dt);
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            // The person rides at the carry point (lifted there over the first third of a second).
            if (person && !_throwing)
            {
                Vector3 carry = HeroPowerRules.CarryPoint(transform.position, transform.forward, HeroPowerRules.CarryHeight);
                float u = GrabLiftPhase;
                Vector3 at = Vector3.Lerp(_liftFrom, carry, u * u * (3f - 2f * u));
                _heldNpc.CarryToH8S(at, _yaw);
            }
            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = HoldType.None;
            Anim.Aiming = false;
        }

        /// <summary>Physics steps while a vehicle is carried: its body is driven to the carry point (the vehicle keeps its own colliders and damage).</summary>
        private void FixedUpdate()
        {
            if (_fr != FreeRunState.Grab || _heldVehicle == null || _throwing || _heldVehicle.Body == null) return;
            Rigidbody rb = _heldVehicle.Body;
            float height = HeroPowerRules.VehicleCarryHeight(_heldVehicle.Def != null ? _heldVehicle.Def.Height : 1.4f);
            Vector3 carry = HeroPowerRules.CarryPoint(transform.position, transform.forward, height);
            float u = GrabLiftPhase;
            Vector3 want = Vector3.Lerp(_liftFrom, carry, u * u * (3f - 2f * u));
            Vector3 toWant = want - rb.position;
            if (toWant.magnitude > 5f)
            {
                // It got caught on something: let it go rather than drag it through the world.
                DropHeld();
                FreeRunEnd();
                return;
            }
            Vector3 v = toWant * 12f;
            if (v.magnitude > 25f) v = v.normalized * 25f;
            rb.linearVelocity = v;
            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(0f, _yaw + 90f, 0f), 0.15f));
        }

        // ================================================================== letting go

        /// <summary>Throws what is held along the camera's aim.</summary>
        private void ThrowHeld()
        {
            Vector3 aimDir = Game.Cam != null ? Game.Cam.AimRay.direction : transform.forward;
            if (_heldNpc != null)
            {
                NpcController n = _heldNpc;
                Vector3 v = HeroPowerRules.ThrowVelocity(aimDir, HeroPowerRules.ThrowSpeed);
                n.EndCarryH8S(v, 2.4f);
                if (n.IsHostileTo())
                {
                    DamageInfo info = DamageInfo.Make(HeroPowerRules.ThrowDamage * Abilities.PowerScale, n.ChestPosition, v, gameObject, DamageType.Melee);
                    info.Force = 6f;
                    info.Flags = DamageFlags.Knockdown | DamageFlags.Heavy;
                    info.Poise = 200f;
                    n.Health.TakeDamage(info);
                    CombatDirector.MarkCombat();
                }
                bool cop = n.Role == NpcRole.Police || n.Role == NpcRole.Swat;
                if (Game.Wanted != null) Game.Wanted.ReportCrime(cop ? CrimeType.AttackCop : CrimeType.Assault, n.transform.position, n.gameObject);
                _thrownBody = n;
                _thrownUntil = Time.time + HeroPowerRules.ThrownTrackSeconds;
                _thrownHit.Clear();
                _heldNpc = null;
            }
            else if (_heldVehicle != null)
            {
                Vehicle v = _heldVehicle;
                Vector3 vel = HeroPowerRules.ThrowVelocity(aimDir, HeroPowerRules.VehicleThrowSpeed);
                if (v.Body != null)
                {
                    v.Body.linearVelocity = vel;
                    v.Body.angularVelocity = transform.right * 2f;
                }
                v.Handbrake = false;
                _heldVehicle = null;
                TravelShake(0.25f, 0.7f, 0.3f, 0.3f);
            }
            _throwing = true;
            _throwT = 0f;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Swing, ChestPosition, 0.8f, 0.1f, 25f);
            if (Game.Cam != null) Game.Cam.Shake(0.18f);
            Pad.Rumble(0.6f, 0.4f, 0.2f);
            Threats.Report(transform.position, 20f, gameObject, ThreatKind.Violence);
        }

        /// <summary>Sets what is held down gently (crouch, or the hold ran out).</summary>
        private void DropHeld()
        {
            if (_heldNpc != null)
            {
                _heldNpc.EndCarryH8S(transform.forward * HeroPowerRules.DropSpeed + Vector3.up * 1f, 1.2f);
                _heldNpc = null;
            }
            if (_heldVehicle != null)
            {
                if (_heldVehicle.Body != null) _heldVehicle.Body.linearVelocity = transform.forward * HeroPowerRules.DropSpeed;
                _heldVehicle.Handbrake = false;
                _heldVehicle = null;
            }
        }

        /// <summary>Lets go of anything held without a throw (teleports, ragdolls, death). Called from FreeRunReset.</summary>
        private void ReleaseHeld()
        {
            if (_heldNpc != null)
            {
                _heldNpc.EndCarryH8S(Vector3.up * 0.5f, 1f);
                _heldNpc = null;
            }
            if (_heldVehicle != null)
            {
                _heldVehicle.Handbrake = false;
                _heldVehicle = null;
            }
            _throwing = false;
        }

        /// <summary>A thrown body knocks down whoever it flies into (runs every frame from the stealth frame).</summary>
        private void TrackThrownBody()
        {
            if (_thrownBody == null) return;
            if (Time.time > _thrownUntil || _thrownBody.IsDead && !_thrownBody.IsRagdolled)
            {
                _thrownBody = null;
                return;
            }
            Vector3 at = _thrownBody.IsRagdolled && _thrownBody.Ragdoll != null ? _thrownBody.Ragdoll.HipsPosition : _thrownBody.ChestPosition;
            float scale = Abilities.PowerScale;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n == _thrownBody || n.IsDead || n.IsRagdolled || n.InVehicle || _thrownHit.Contains(n)) continue;
                if ((n.transform.position + Vector3.up - at).sqrMagnitude > HeroPowerRules.ThrownImpactRadius * HeroPowerRules.ThrownImpactRadius) continue;
                if (WeaponController.CrystalProtected(n)) continue;
                _thrownHit.Add(n);
                Vector3 away = MathUtil.Flat(n.transform.position - at);
                away = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                if (n.IsHostileTo())
                {
                    DamageInfo info = DamageInfo.Make(HeroPowerRules.ThrownImpactDamage * scale, n.ChestPosition, away, gameObject, DamageType.Melee);
                    info.Force = 5f;
                    info.Flags = DamageFlags.Knockdown;
                    n.Health.TakeDamage(info);
                }
                n.Knockdown(away * 5f + Vector3.up * 3f, n.ChestPosition, 1.4f);
                CombatDirector.HitStop(0.03f);
                if (Game.Fx != null) Game.Fx.Sparks(n.ChestPosition, away, 2);
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.BodyFall, n.ChestPosition, 0.7f, 0.1f, 25f);
                if (Game.Hud != null) Game.Hud.HitMarker(!n.Health.IsAlive);
            }
        }
    }
}
