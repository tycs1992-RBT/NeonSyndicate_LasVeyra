using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Hover bike behaviour for Vehicle: arcade hover physics that can never tip over (the physics body stays upright
    /// and only the model banks), quick throttle response, grip-limited nimble steering, the visual lean and hover bob,
    /// handlebars that turn, riders seated in view with the animator's ride fields kept up to date, and riders thrown
    /// off in a hard crash or a blast.
    /// </summary>
    public sealed partial class Vehicle
    {
        /// <summary>Speed change in one impact (m/s) above which a bike throws its riders.</summary>
        public const float BikeThrowDv = 8.5f;

        /// <summary>Takes no damage at all (story bikes that must survive a scripted fight).</summary>
        public bool Invulnerable;

        /// <summary>Hover bikes: the lean the model shows right now, in degrees (negative leans left).</summary>
        public float Lean { get { return _lean; } }

        /// <summary>
        /// 04E: handling the personal garage (a tune) and the fusion system (a fused form) give this bike: multipliers,
        /// the glide, the air brake and the chassis damage scale. FusedHandling.None (all 1) for every other bike.
        /// </summary>
        public FusedHandling FusionHandling = FusedHandling.None;

        private bool _fusionImpulseQueued;
        private float _fusionForwardDv, _fusionUpDv, _fusionCap;

        /// <summary>
        /// 04E: a one-shot velocity change from a fused form's ability, applied on the next physics step: forward along
        /// the heading (never above top speed times capFactor) and up. The chassis' colliders are unchanged, so the
        /// world still stops it.
        /// </summary>
        public void QueueFusionImpulse(float forwardDeltaV, float upDeltaV, float capFactor)
        {
            _fusionImpulseQueued = true;
            _fusionForwardDv = Mathf.Max(0f, forwardDeltaV);
            _fusionUpDv = Mathf.Max(0f, upDeltaV);
            _fusionCap = Mathf.Max(0f, capFactor);
        }

        private float _lean;
        private float _leanTarget;
        private BoxCollider _riderBox;
        private BoxCollider _riderShellBox;
        private Vector3[] _podBase;

        /// <summary>Ground point beside a bike seat where a rider steps on or off (side -1 left, +1 right).</summary>
        public Vector3 MountPoint(int seat, int side)
        {
            Transform s = SeatTransform(seat);
            Vector3 p = s.position + transform.right * (side < 0 ? -1f : 1f) * (Def.Width * 0.5f + 0.45f);
            p.y = transform.position.y;
            return p;
        }

        // ================================================================== physics

        private void BikeStep(float dt)
        {
            bool hasDriver = Driver != null && !IsDestroyed;
            float throttle = hasDriver && EngineRunning ? Mathf.Clamp(Throttle, -1f, 1f) : 0f;
            bool handbrake = !hasDriver || Handbrake;
            // 04E fusion and tune handling (all 1 on an ordinary bike).
            FusedHandling fh = FusionHandling.Drive > 0f ? FusionHandling : FusedHandling.None;
            if (_fusionImpulseQueued)
            {
                _fusionImpulseQueued = false;
                if (hasDriver)
                {
                    float topNow = Def.TopSpeed * (1f + EngineUpgrade * 0.06f) * fh.TopSpeed;
                    Vector3 boosted = FusionRules.CapImpulse(Body.linearVelocity, transform.forward, _fusionForwardDv, topNow, _fusionCap);
                    boosted.y += _fusionUpDv;
                    Body.linearVelocity = boosted;
                }
            }
            Vector3 vel = Body.linearVelocity;
            float speed = vel.magnitude;
            float fwdSpeed = Vector3.Dot(vel, transform.forward);

            // Steering: snappier than a car's; the AI's angle is used directly.
            float maxSteer = MaxSteerAngleAt(fwdSpeed) * fh.Steer;
            float target = hasDriver ? Mathf.Clamp(SteerInput, -1f, 1f) * maxSteer : 0f;
            _steer = PlayerDriving ? BikeMath.StepPlayerSteer(_steer, target, fwdSpeed, true, DirectSteer, dt) :
                (DirectSteer ? Mathf.MoveTowards(_steer, target, 260f * dt) : Mathf.MoveTowards(_steer, target, (Mathf.Abs(target) < Mathf.Abs(_steer) ? 210f : 150f) * dt));

            float top = Def.TopSpeed * (1f + EngineUpgrade * 0.06f) * fh.TopSpeed;
            float maxForce = Def.DriveForce * (1f + EngineUpgrade * 0.12f) * BoostForce * fh.Drive;
            if (BoostTimer > 0f)
            {
                BoostTimer -= dt;
                if (BoostTimer <= 0f) { BoostGrip = 1f; BoostForce = 1f; BoostArmor = 1f; }
            }
            float drive = 0f;
            bool braking = false;
            float brakeAmount = 0f;
            if (throttle > 0.01f)
            {
                if (fwdSpeed < -1f) { braking = true; brakeAmount = throttle; }
                else drive = BikeMath.DriveForce(throttle, fwdSpeed, maxForce, top);
            }
            else if (throttle < -0.01f)
            {
                if (fwdSpeed > 1f) { braking = true; brakeAmount = -throttle; }
                else drive = throttle * maxForce * 0.3f * (1f - Mathf.InverseLerp(5f, 8f, -fwdSpeed));
            }
            UpdateGearbox(fwdSpeed, top, dt, ref drive);
            if (BoostHeld && NitroCharges > 0 && _nitroTimer <= 0f && hasDriver)
            {
                NitroCharges--;
                _nitroTimer = 3f;
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, transform.position, 0.9f, 0.7f, 50f);
            }
            if (_nitroTimer > 0f)
            {
                _nitroTimer -= dt;
                drive += maxForce * 0.7f * (PlayerDriving ? SkillMods.VehicleBoost : 1f);
                if (Game.Fx != null) Game.Fx.FirePuff(transform.TransformPoint(Parts.ExhaustLocal), 0.18f);
            }
            IsBraking = braking || (handbrake && hasDriver && Mathf.Abs(fwdSpeed) > 0.5f);

            // Hover suspension: four short rays, sprung like a car's corners.
            Vector3 up = transform.up;
            int grounded = 0;
            Vector3 normalSum = Vector3.zero;
            float r = Def.WheelRadius;
            for (int i = 0; i < 4; i++)
            {
                Vector3 attach = transform.TransformPoint(_w[i].Attach);
                RaycastHit hit;
                if (Physics.Raycast(attach, -up, out hit, _restLength + r, Layers.WheelMask, QueryTriggerInteraction.Ignore))
                {
                    float length = Mathf.Max(0f, hit.distance - r);
                    float compression = _restLength - length;
                    float compVel = (compression - _w[i].LastCompression) / dt;
                    _w[i].LastCompression = compression;
                    _w[i].Compression = compression;
                    _w[i].Length = length;
                    _w[i].Grounded = true;
                    _w[i].Contact = hit.point;
                    _w[i].Normal = hit.normal;
                    _w[i].Surface = hit.collider;
                    float springForce = _spring * compression + _damper * compVel;
                    if (length < 0.03f) springForce += _spring * 4f * (0.03f - length);
                    springForce = Mathf.Clamp(springForce, 0f, Def.Mass * 9.81f * 3f);
                    Body.AddForceAtPosition(up * springForce, attach);
                    normalSum += hit.normal;
                    grounded++;
                }
                else
                {
                    _w[i].Grounded = false;
                    _w[i].Compression = 0f;
                    _w[i].LastCompression = 0f;
                    _w[i].Length = _restLength;
                    _w[i].Slip = 0f;
                }
            }
            GroundedWheels = grounded;

            float drag = BikeMath.DragCoefficient(Def.Width, Def.Height);
            float yawNow = Vector3.Dot(Body.angularVelocity, up);
            float slip = 0f;
            float leanGrip = 0f;
            if (grounded > 0)
            {
                AirTime = 0f;
                Vector3 n = (normalSum / grounded).normalized;
                Vector3 f = transform.forward - n * Vector3.Dot(transform.forward, n);
                f = f.sqrMagnitude > 1e-4f ? f.normalized : transform.forward;
                Vector3 side = Vector3.Cross(n, f);
                float vLong = Vector3.Dot(vel, f);
                float vLat = Vector3.Dot(vel, side);
                float surfaceGrip = SurfaceGrip() * (PlayerDriving ? SkillMods.VehicleGrip : 1f);
                float maxLat = BikeMath.MaxLateral(Def.Grip * BoostGrip * fh.Grip, surfaceGrip) * (grounded / 4f);
                leanGrip = maxLat;

                // Along the bike: thrust, brakes, engine braking, rolling resistance, and a firm stop when parked.
                float lon = drive / Def.Mass;
                float stop = Mathf.Abs(vLong) / dt;
                if (braking) lon -= Mathf.Sign(vLong) * Mathf.Min(stop, 11f * brakeAmount * fh.Brake);
                if (handbrake && hasDriver) lon -= Mathf.Sign(vLong) * Mathf.Min(stop, 4f);
                if (!hasDriver || (Mathf.Abs(throttle) < 0.05f && !braking))
                {
                    float hold = Mathf.Abs(vLong) < 1.5f ? stop * 0.25f : (hasDriver ? 1.1f : 6f);
                    lon -= Mathf.Sign(vLong) * Mathf.Min(stop, hold);
                }
                lon -= Mathf.Sign(vLong) * Mathf.Min(stop, BikeMath.RollingDecel);

                // Across the bike: grip pulls any slide straight, up to the grip limit. The handbrake at speed lets
                // the tail step out for a drift.
                bool drifting = handbrake && hasDriver && Mathf.Abs(vLong) > 6f;
                yawNow = Vector3.Dot(Body.angularVelocity, n);
                float lat = PlayerDriving && !drifting && grounded >= 2 ?
                    BikeMath.PlayerLateralAcceleration(vLong, vLat, yawNow, maxLat, dt) :
                    Mathf.Clamp(-vLat * (drifting ? 2.2f : 9f), -maxLat, maxLat);
                // Applied at the centre of mass: cornering never rolls the body (the lean is only the model's).
                Body.AddForce((f * lon + side * lat) * Def.Mass);
                slip = Mathf.Clamp01((Mathf.Abs(vLat) - 1.2f) / 5f);
                if (drifting) slip = Mathf.Max(slip, 0.35f);

                // Yaw toward the turn the steering asks for, capped by grip.
                float want = hasDriver ? (PlayerDriving && !drifting ?
                    BikeMath.PlayerYawRate(vLong, _steer, Def.Wheelbase, maxLat, true) :
                    BikeMath.YawRate(vLong, _steer, Def.Wheelbase, maxLat * (drifting ? 1.5f : 1f))) : 0f;
                float yawAcceleration = PlayerDriving && !drifting ?
                    BikeMath.PlayerYawAcceleration(want, yawNow, vLong, grounded, true, dt) : (want - yawNow) * 9f;
                Body.AddTorque(n * yawAcceleration * Body.inertiaTensor.y);

                // Air drag and a little downforce over crests.
                Body.AddForce(-vel * speed * drag);
                Body.AddForce(-n * speed * speed * 0.3f);
            }
            else
            {
                AirTime += dt;
                Body.AddForce(-vel * speed * drag);
                // In the air the nose drifts back toward level; throttle pitches and steering yaws a little.
                float control = hasDriver && AirTime > 0.15f ? 1f : 0f;
                float pitchRate = Vector3.Dot(Body.angularVelocity, transform.right);
                Body.AddTorque(transform.right * (transform.forward.y * 4f - pitchRate * 2.5f + control * Throttle * 2.2f) * Body.inertiaTensor.x);
                Body.AddTorque(Vector3.up * (control * SteerInput * 1.5f * fh.AirSteer - Vector3.Dot(Body.angularVelocity, Vector3.up) * 0.8f) * Body.inertiaTensor.y);
                // 04E Ventwing glide: part of gravity is carried by the wings and the sink speed is capped.
                if (hasDriver && fh.GlideGravity < 0.999f)
                {
                    Body.AddForce(-Physics.gravity * (1f - Mathf.Clamp01(fh.GlideGravity)), ForceMode.Acceleration);
                    float sink = Body.linearVelocity.y;
                    if (sink < -fh.GlideMaxSink) Body.AddForce(Vector3.up * Mathf.Min((-fh.GlideMaxSink - sink) / dt * 0.5f, 30f), ForceMode.Acceleration);
                }
                // 04E Vent Brake profile: braking in the air bleeds flat speed through the vents.
                if (hasDriver && fh.AirBrake > 0f && throttle < -0.1f)
                {
                    Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
                    float flatSpeed = flatVel.magnitude;
                    if (flatSpeed > 0.5f) Body.AddForce(-flatVel / flatSpeed * Mathf.Min(fh.AirBrake * -throttle, flatSpeed / dt), ForceMode.Acceleration);
                }
            }
            for (int i = 0; i < 4; i++) _w[i].Slip = _w[i].Grounded ? slip : 0f;
            SlipAmount = grounded > 0 ? slip : 0f;

            StabilizeBike(dt);
            _leanTarget = grounded > 0 && !IsDestroyed ? (PlayerDriving ?
                BikeMath.PlayerLeanForTurn(fwdSpeed, yawNow, leanGrip) : BikeMath.LeanForTurn(fwdSpeed, yawNow)) : 0f;

            // Upside-down recovery for the player (a bike only gets there nose-first off a big jump).
            if (IsUpsideDown && speed < 2.5f)
            {
                _upsideDownTimer += dt;
                if (_upsideDownTimer > 3f && HasPlayer && !IsDestroyed)
                {
                    _upsideDownTimer = 0f;
                    Vector3 flatFwd = MathUtil.Flat(transform.forward);
                    if (flatFwd.sqrMagnitude < 0.01f) flatFwd = Vector3.forward;
                    Body.position += Vector3.up * 1.2f;
                    Body.rotation = Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
                    Body.angularVelocity = Vector3.zero;
                    Body.linearVelocity = Vector3.zero;
                }
            }
            else _upsideDownTimer = 0f;

            _hitCheckTimer -= dt;
            if (_hitCheckTimer <= 0f && speed > 2f)
            {
                _hitCheckTimer = 0.04f;
                CheckPedestrianHits(vel, speed);
            }
        }

        /// <summary>Roll recovery. Player correction is bounded and reserves direct rotation for extreme impacts.</summary>
        private void StabilizeBike(float dt)
        {
            if (IsDestroyed) return; // a wreck falls where it falls
            Vector3 fwd = transform.forward;
            float rollSin = Vector3.Dot(transform.right, Vector3.up);
            float rollRate = Vector3.Dot(Body.angularVelocity, fwd);
            float acceleration = PlayerDriving ? BikeMath.PlayerRollAcceleration(rollSin, rollRate) : -rollSin * 90f - rollRate * 18f;
            Body.AddTorque(fwd * acceleration * Body.inertiaTensor.z);
            if (Mathf.Abs(rollSin) > (PlayerDriving ? 0.766f : 0.5f) && Mathf.Abs(fwd.y) < 0.95f)
            {
                float recovery = PlayerDriving ? 1f - Mathf.Exp(-7f * dt) : 0.35f;
                Body.rotation = Quaternion.Slerp(Body.rotation, Quaternion.LookRotation(fwd, Vector3.up), recovery);
                Body.angularVelocity -= fwd * rollRate;
            }
        }

        // ================================================================== visuals and riders

        private void UpdateBikeVisuals(float dt)
        {
            if (Parts.LeanRoot == null) return;
            _lean = PlayerDriving ? BikeMath.StepPlayerLean(_lean, IsDestroyed ? 0f : _leanTarget, dt) :
                BikeMath.StepLean(_lean, IsDestroyed ? 0f : _leanTarget, dt);
            Parts.LeanRoot.localRotation = Quaternion.Euler(0f, 0f, -_lean);

            // Pods ride the suspension; the front one vectors with the steering and both tip with speed.
            if (_podBase == null)
            {
                _podBase = new Vector3[2];
                for (int k = 0; k < 2; k++) if (Parts.Wheels[k * 2] != null) _podBase[k] = Parts.Wheels[k * 2].localPosition;
            }
            float tip = Mathf.Min(7f, Speed * 0.25f);
            for (int k = 0; k < 2; k++)
            {
                Transform pod = Parts.Wheels[k * 2];
                if (pod == null) continue;
                float len = (_w[k * 2].Length + _w[k * 2 + 1].Length) * 0.5f;
                float lift = Mathf.Clamp(_attachDrop - len, -0.1f, 0.15f) * 0.6f;
                pod.localPosition = Vector3.Lerp(pod.localPosition, _podBase[k] + Vector3.up * lift, MathUtil.Damp(25f, dt));
                pod.localRotation = Quaternion.Euler(tip, k == 0 ? _steer * 0.5f : 0f, 0f);
            }

            // Handlebars turn with the steering, and the hand anchors stay on the grips.
            if (Parts.Bars != null)
            {
                Parts.Bars.localRotation = Quaternion.Euler(-22f, 0f, 0f) * Quaternion.Euler(0f, _steer * 0.9f, 0f);
                if (Parts.HandleL != null) Parts.HandleL.SetPositionAndRotation(Parts.Bars.TransformPoint(Parts.GripL), Parts.Bars.rotation);
                if (Parts.HandleR != null) Parts.HandleR.SetPositionAndRotation(Parts.Bars.TransformPoint(Parts.GripR), Parts.Bars.rotation);
            }

            // Seats sit on the Seat anchors but stay upright (no roll): the animator leans the body by RideLean.
            if (Parts.Seats != null && Parts.SeatAnchors != null)
                for (int i = 0; i < Parts.Seats.Length && i < Parts.SeatAnchors.Length; i++)
                    if (Parts.Seats[i] != null && Parts.SeatAnchors[i] != null) Parts.Seats[i].SetPositionAndRotation(Parts.SeatAnchors[i].position, transform.rotation);

            for (int i = 0; i < Occupants.Length; i++) if (Occupants[i] != null) ApplyRidePose(Occupants[i], i);
        }

        /// <summary>Puts a character into the bike riding pose: HumanoidAnimator's Ride, RideRoot, RideLean, SitWeight and (for AI riders) Steer.</summary>
        private void ApplyRidePose(CharacterBase c, int seat)
        {
            if (c == null || c.Anim == null) return;
            c.Anim.Ride = RidePose.Bike;
            c.Anim.RideRoot = Parts.RideRoots != null && seat >= 0 && seat < Parts.RideRoots.Length ? Parts.RideRoots[seat] : Parts.LeanRoot;
            c.Anim.RideLean = _lean;
            c.Anim.SitWeight = 1f;
            // The player's steering pose comes from the controls (PlayerController); AI riders mirror the bars.
            if (!c.IsPlayer) c.Anim.Steer = seat == 0 ? Mathf.Clamp(_steer / Mathf.Max(1f, Def.SteerAngle * 0.5f), -1f, 1f) : 0f;
        }

        private static void ClearRidePose(CharacterBase c)
        {
            if (c == null || c.Anim == null) return;
            c.Anim.Ride = RidePose.None;
            c.Anim.RideRoot = null;
            c.Anim.RideLean = 0f;
        }

        private void OnBikeOccupantChanged(int seat, CharacterBase previous, CharacterBase now)
        {
            if (previous != null && previous != now) ClearRidePose(previous);
            if (now != null) ApplyRidePose(now, seat);
            bool anyone = OccupantCount() > 0;
            if (_riderBox != null) _riderBox.enabled = anyone;
            if (_riderShellBox != null) _riderShellBox.enabled = anyone;
        }

        /// <summary>
        /// Throws everyone off the bike: they land beside it, take crash damage (hurt), and ragdoll with the bike's
        /// momentum plus a push. They get up by themselves and can climb back on.
        /// </summary>
        public void ThrowRiders(Vector3 inherited, Vector3 push, float hurt)
        {
            GameObject source = LastAttacker != null && Time.time - LastDamageTime < 0.5f ? LastAttacker : gameObject;
            bool any = false;
            for (int i = 0; i < Occupants.Length; i++)
            {
                CharacterBase c = Occupants[i];
                if (c == null) continue;
                any = true;
                c.ExitVehicleImmediate(false);
                if (c.IsDead) continue;
                if (hurt > 0f && c.Health != null)
                {
                    DamageInfo info = DamageInfo.Make(hurt, c.ChestPosition, inherited, source, DamageType.Vehicle);
                    info.Force = inherited.magnitude;
                    c.Health.TakeDamage(info);
                }
                if (!c.IsDead) c.KnockdownWithVelocity(inherited, push + Vector3.up * 2f, c.ChestPosition, 1.5f);
            }
            if (any && Game.Audio != null) Game.Audio.PlayVar(SfxId.BodyFall, transform.position, 0.7f, 0.1f, 40f);
        }
    }
}
