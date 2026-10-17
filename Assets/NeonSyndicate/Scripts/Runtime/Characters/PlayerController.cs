using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Malik Veyr. Third-person on-foot movement (walk, jog, sprint, crouch, jump, climb), free aiming and hip fire,
    /// melee with target snapping, weapon cycling, quick throws, entering/carjacking/exiting/bailing out of vehicles,
    /// driving and drive-bys, interaction, fall damage and modern open-world health regeneration.
    /// </summary>
    public sealed partial class PlayerController : CharacterBase
    {
        public CharacterController Controller;
        public bool ControlsEnabled = true;
        public bool Crouched;
        public bool Sprinting { get; private set; }
        public bool Aiming { get; private set; }
        public bool AimingDownSights { get; private set; }
        public Vector3 AimPoint { get; private set; }
        public CharacterBase AimTarget { get; private set; }
        public Vehicle AimVehicle { get; private set; }
        public float Stamina = 1f;
        public Interactable Focus { get; private set; }
        public Vehicle EnterCandidate { get; private set; }
        public bool Entering { get { return _enter != EnterPhase.None; } }
        public bool Climbing { get { return _climbing; } }
        public float LastDamageFromPlayerAttackTime;
        /// <summary>Dodge, guard, parry, combos, Shard Tonic and lock-on.</summary>
        public PlayerCombat Combat;
        public bool IsGroundedPublic { get { return _grounded; } }
        public float Yaw { get { return _yaw; } }
        /// <summary>MOONJUMP cheat: higher jumps and floaty falls.</summary>
        public static bool MoonJump;

        private enum EnterPhase { None, Approach, Open, Jack, Sit }

        private Vector3 _vel;
        private float _vy;
        private bool _grounded = true;
        private float _yaw;
        private float _hipFire;
        private float _pressBuffer;
        private bool _winded;
        private float _airPeakSpeed;
        private float _footTimer;
        private Vector3 _external;
        private EnterPhase _enter;
        private Vehicle _enterTarget;
        private int _enterSeat;
        private float _enterTimer;
        private bool _climbing;
        private Vector3 _climbFrom;
        private Vector3 _climbTo;
        private float _climbT;
        private float _climbDuration;
        private WeaponId _restoreWeapon = WeaponId.Count;
        private WeaponId _thrownWeapon = WeaponId.Count;
        private float _hornHold;
        private float _exitDoorTimer;
        private Vehicle _exitDoorVehicle;
        private int _exitDoorSeat;
        private float _aimThreatTimer;
        private readonly RaycastHit[] _aimHits = new RaycastHit[16];
        private readonly Collider[] _near = new Collider[24];

        public override Vector3 Velocity
        {
            get
            {
                if (InVehicle) return CurrentVehicle.Velocity;
                if (IsRagdolled && Ragdoll.HipsBody != null) return Ragdoll.HipsBody.linearVelocity;
                return _vel + Vector3.up * _vy;
            }
        }

        public Vector3 FlatVelocity { get { return _vel; } }

        public static PlayerController Spawn(Vector3 pos, float yaw)
        {
            GameObject go = new GameObject("Malik Veyr (Player)");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            PlayerController p = go.AddComponent<PlayerController>();
            p.Init(yaw);
            return p;
        }

        private void Init(float yaw)
        {
            BuildBody(Appearance.Malik(), NeonSyndicate.Team.Player, -1, 2);
            DisplayName = "Malik Veyr";
            _yaw = yaw;
            Controller = gameObject.AddComponent<CharacterController>();
            float s = Rig.Scale;
            Controller.height = 1.78f * s;
            Controller.radius = 0.32f;
            Controller.center = new Vector3(0f, 0.89f * s + 0.02f, 0f);
            Controller.stepOffset = 0.42f;
            Controller.slopeLimit = 52f;
            Controller.skinWidth = 0.04f;
            Controller.minMoveDistance = 0f;
            Weapons.IsPlayer = true;
            Anim.AlwaysAnimate = true;
            Health.MaxHealth = 200f;
            Health.Health = 200f;
            Health.HeadshotMultiplier = 1.6f;
            Combat = new PlayerCombat(this);
            Health.Filter = Combat;
            Health.OnLethal = Combat.OnLethal;
        }

        // ================================================================== damage, death

        protected override void OnDamaged(DamageInfo info)
        {
            TravelOnDamaged(info);
            if (Game.Hud != null) Game.Hud.DamageFlash(info);
            if (Game.Cam != null) Game.Cam.Shake(Mathf.Clamp(info.Amount / 60f, 0.05f, 0.4f));
            if (info.Type == DamageType.Melee && (info.Flags & DamageFlags.Knockdown) != 0 && !InVehicle && !IsDead) Knockdown(info.Direction * 4f + Vector3.up * 1.5f, info.Point, 1.1f);
            if (info.Type == DamageType.Explosion && info.Force > 6f && !InVehicle) Knockdown(info.Direction * info.Force * 0.6f + Vector3.up * info.Force * 0.4f, info.Point, 1.6f);
        }

        protected override void OnDied(DamageInfo info)
        {
            base.OnDied(info);
            Aiming = false;
            if (Combat != null) Combat.ReleaseLock();
            CancelEnter();
            if (Game.Root != null) Game.Root.OnPlayerWasted(info);
        }

        protected override void OnBeforeRagdoll()
        {
            CancelEnter();
            TravelReset();
            _climbing = false;
            if (Controller != null) Controller.enabled = false;
            Aiming = false;
        }

        protected override void OnAfterRagdoll()
        {
            if (Controller != null) Controller.enabled = !InVehicle;
            _vel = Vector3.zero;
            _vy = 0f;
            _yaw = transform.eulerAngles.y;
        }

        /// <summary>Puts the player back on their feet at full health (respawn, mission restart, debug teleport).</summary>
        public void Respawn(Vector3 pos, float yaw)
        {
            if (InVehicle) ExitVehicleImmediate(false);
            if (IsRagdolled) Ragdoll.Deactivate();
            Anim.Frozen = false;
            Rig.ResetPose();
            Health.Revive(Health.MaxHealth);
            Health.Burning = 0f;
            Teleport(pos, yaw);
            Weapons.SetHidden(false);
            Crouched = false;
            if (Combat != null)
            {
                Combat.ResetState();
                Combat.RefillTonics();
            }
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            CancelEnter();
            TravelReset();
            _climbing = false;
            if (InVehicle)
            {
                Vehicle v = CurrentVehicle;
                v.Body.position = pos + Vector3.up * 0.5f;
                v.Body.rotation = Quaternion.Euler(0f, yaw, 0f);
                v.transform.SetPositionAndRotation(pos + Vector3.up * 0.5f, Quaternion.Euler(0f, yaw, 0f));
                v.Body.linearVelocity = Vector3.zero;
                v.Body.angularVelocity = Vector3.zero;
                return;
            }
            bool was = Controller.enabled;
            Controller.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            _yaw = yaw;
            _vel = Vector3.zero;
            _vy = 0f;
            Controller.enabled = was || !IsRagdolled;
            Physics.SyncTransforms();
        }

        public void AddExternalVelocity(Vector3 v)
        {
            _external += v;
        }

        // ================================================================== main loop

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            UpdateRagdollState(dt);
            if (IsDead || IsRagdolled)
            {
                Aiming = false;
                AimingDownSights = false;
                return;
            }
            bool canControl = ControlsEnabled && Game.IsPlaying && !GInput.GameplayBlocked && !GInput.Broken;
            Health.DamageScale = CombatDirector.PlayerDamageTaken * (Game.Gear != null ? Game.Gear.ArmorMultiplier : 1f);
            // Gun damage grows with Might and gear affixes.
            if (Weapons != null) Weapons.DamageScale = (Game.Progress != null ? Game.Progress.GunMultiplier : 1f) * (Game.Gear != null ? Game.Gear.GunMultiplier : 1f);
            UpdatePadLight();
            if (InVehicle && Combat != null && Combat.LockTarget != null) Combat.ReleaseLock();
            if (InVehicle) UpdateInVehicle(dt, canControl);
            else if (_enter != EnterPhase.None) UpdateEntering(dt, canControl);
            else if (_climbing) UpdateClimb(dt);
            else if (!UpdateCover(dt, canControl)) UpdateOnFoot(dt, canControl);
            UpdateTravelPresentation(dt);
            UpdateAim(canControl);
            UpdateWeapons(dt, canControl && !_climbing && _enter == EnterPhase.None);
            UpdateInteraction(canControl);
            UpdateRegen(dt);
            UpdateExitDoor(dt);
        }

        // ================================================================== on foot

        private void UpdateOnFoot(float dt, bool canControl)
        {
            if (!Controller.enabled) Controller.enabled = true;
            // Surge, the air powers and flight move Malik themselves (PlayerController.Travel.cs).
            if (UpdateTravelModes(dt, canControl)) return;
            Vector2 input = canControl ? GInput.Move : Vector2.zero;
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            Vector3 camRight = MathUtil.RightOf(camFwd);
            Vector3 wish = camRight * input.x + camFwd * input.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            Combat.Tick(dt, canControl, wish);
            bool busy = Combat.OverridesMovement;
            bool free = Combat.State == PlayerCombat.Phase.Free;

            bool walk = canControl && Act.WalkHeld;
            bool sprintKey = canControl && Act.SprintHeld;
            // The crouch button while sprinting switches on the Surge (super speed) instead of crouching.
            if (canControl && Act.Crouch && free && !TryStartSurge(sprintKey, wish)) Crouched = !Crouched;
            WeaponDef d = Weapons.Current;
            bool melee = d.Fire == FireKind.Melee;
            // With a melee weapon or fists the right mouse button guards instead of aiming.
            bool aimInput = canControl && !melee && GInput.Aim;
            bool wantsAim = aimInput || (canControl && _hipFire > 0f && (d.IsGun || d.Fire == FireKind.Throw));
            Aiming = wantsAim && free && !Combat.Guarding;
            AimingDownSights = Aiming && aimInput;
            if (sprintKey && Crouched && wish.sqrMagnitude > 0.1f) Crouched = false;
            CharacterBase lockTarget = Combat.LockTarget;
            Sprinting = sprintKey && !Aiming && wish.sqrMagnitude > 0.1f && !Combat.Exhausted && !Crouched && !Combat.Guarding && free;
            UpdateSprintStamina(dt);
            Stamina = Combat.Stamina / Mathf.Max(1f, Combat.MaxStamina);

            float speed;
            if (Aiming) speed = Crouched ? 1.5f : 2.7f;
            else if (Crouched) speed = 1.9f;
            else if (walk) speed = TravelMath.WalkSpeed;
            else if (Sprinting) speed = TravelMath.SprintSpeed * SkillMods.MoveSpeed; // Quick Feet and Gale Runner (SkillMods)
            else if (lockTarget != null) speed = 2.6f;
            else speed = TravelMath.JogSpeed;
            speed *= Combat.SpeedScale * TravelGroundSpeedScale();
            if (Game.Gear != null) speed *= Game.Gear.MoveSpeedMultiplier; // 04E: MoveSpeed affixes and the Void Screen window (BuildStatRules04E)
            if (d.Hold == HoldType.Heavy) speed *= 0.82f;
            Vector3 targetVel = wish * speed;
            if (busy)
            {
                _vel = Combat.Motion;
            }
            else
            {
                float accel = _grounded ? (targetVel.sqrMagnitude > 0.01f ? TravelMath.GroundAccel : TravelMath.GroundDecel) : 3.5f;
                _vel = Vector3.MoveTowards(_vel, targetVel, accel * dt);
            }

            // Facing: rolls and swings choose their own direction; aiming, guarding and lock-on strafe.
            float targetYaw = _yaw;
            float rate = 620f;
            if (busy)
            {
                targetYaw = Combat.Facing;
                rate = 2400f;
            }
            else if (Aiming)
            {
                targetYaw = Game.Cam != null ? Game.Cam.Yaw : _yaw;
                rate = 1100f;
            }
            else if (lockTarget != null && !Sprinting)
            {
                Vector3 to = MathUtil.Flat(lockTarget.transform.position - transform.position);
                if (to.sqrMagnitude > 0.04f) targetYaw = MathUtil.YawOf(to);
                rate = 760f;
            }
            else if (Combat.Guarding && Game.Cam != null)
            {
                targetYaw = Game.Cam.Yaw;
                rate = 600f;
            }
            else if (_vel.sqrMagnitude > 0.04f)
            {
                targetYaw = MathUtil.YawOf(_vel);
                rate = Sprinting ? 420f : 620f;
            }
            _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, rate * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Jump / climb / gravity.
            bool jump = canControl && Act.Jump && Combat.State == PlayerCombat.Phase.Free && !Combat.Guarding;
            if (_grounded)
            {
                if (_vy < 0f) _vy = -2f;
                // Crouched, Jump charges a Super Leap instead (PlayerController.Travel.cs).
                if (!TravelGroundJump(jump, canControl, wish, dt) && jump && !Crouched)
                {
                    if (TryClimb()) return;
                    _vy = MoonJump ? 12f : 5.3f;
                    _grounded = false;
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, transform.position, 0.35f, 0.1f, 15f);
                }
            }
            else
            {
                _vy -= (MoonJump ? 7f : 19.6f) * dt;
                _airPeakSpeed = MoonJump ? 0f : Mathf.Max(_airPeakSpeed, -_vy);
                if (jump && TryClimb()) return;
                TravelAirborne(jump, canControl);
            }

            Vector3 motion = (_vel + Vector3.up * _vy + _external) * dt;
            _external = Vector3.MoveTowards(_external, Vector3.zero, dt * 12f);
            bool wasGrounded = _grounded;
            Controller.Move(motion);
            _grounded = Controller.isGrounded || (_vy <= 0f && GroundProbe());
            // A glide or flight that began this frame lands safely (PlayerController.Travel.cs); ordinary falls can hurt.
            if (_grounded && !wasGrounded)
            {
                if (_travel != TravelMode.None) TravelTouchdown(-_vy, _vel.magnitude);
                else OnLanded(_airPeakSpeed);
            }
            if (_grounded) _airPeakSpeed = 0f;
            if (transform.position.y < WorldBounds.KillY) Respawn(Game.Root != null ? Game.Root.SafeRespawnPoint(transform.position) : Vector3.up * 2f, _yaw);

            // Footsteps.
            float hs = _vel.magnitude;
            if (_grounded && hs > 0.6f)
            {
                _footTimer -= dt * hs;
                if (_footTimer <= 0f)
                {
                    _footTimer = 1.25f;
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Footstep, transform.position, Crouched ? 0.12f : (Sprinting ? 0.4f : 0.25f), 0.15f, 18f);
                }
            }

            Anim.Velocity = _vel;
            Anim.VerticalVelocity = _vy;
            Anim.Grounded = _grounded;
            Anim.CrouchWeight = Crouched ? 1f : 0f;
            Anim.SitWeight = 0f;
            Anim.Hold = d.Hold;
        }

        private bool GroundProbe()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * (Controller.radius + 0.05f);
            return Physics.SphereCast(origin, Controller.radius * 0.9f, Vector3.down, out hit, 0.2f, Layers.GroundMask | (1 << Layers.VehicleShell), QueryTriggerInteraction.Ignore);
        }

        private void OnLanded(float fallSpeed)
        {
            if (fallSpeed > 19f)
            {
                DamageInfo info = DamageInfo.Make((fallSpeed - 12f) * 9f, transform.position, Vector3.down, null, DamageType.Fall);
                Health.TakeDamage(info);
                if (!IsDead) Knockdown(_vel * 0.5f + Vector3.down, transform.position, 1.5f);
            }
            else if (fallSpeed > 12f)
            {
                DamageInfo info = DamageInfo.Make((fallSpeed - 12f) * 7f, transform.position, Vector3.down, null, DamageType.Fall);
                Health.TakeDamage(info);
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Land, transform.position, 0.6f, 0.1f, 20f);
            }
            else if (fallSpeed > 4f)
            {
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Land, transform.position, 0.35f, 0.1f, 15f);
            }
        }

        // ------------------------------------------------------------------ climbing and vaulting

        private bool TryClimb()
        {
            int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell);
            Vector3 fwd = transform.forward;
            Vector3 basePos = transform.position;
            RaycastHit wall;
            bool hitWall = false;
            for (float h = 0.4f; h <= 1.9f; h += 0.5f)
            {
                if (Physics.Raycast(basePos + Vector3.up * h, fwd, out wall, 0.95f, mask, QueryTriggerInteraction.Ignore) && wall.normal.y < 0.5f)
                {
                    hitWall = true;
                    break;
                }
            }
            if (!hitWall) return false;
            RaycastHit top;
            Vector3 probe = basePos + fwd * 0.95f + Vector3.up * 2.6f;
            if (!Physics.Raycast(probe, Vector3.down, out top, 2.6f, mask, QueryTriggerInteraction.Ignore)) return false;
            float climbH = top.point.y - basePos.y;
            if (climbH < 0.45f || climbH > 2.35f || top.normal.y < 0.7f) return false;
            Vector3 dest = top.point + Vector3.up * 0.03f;
            if (Physics.CheckCapsule(dest + Vector3.up * 0.45f, dest + Vector3.up * 1.5f, 0.28f, mask, QueryTriggerInteraction.Ignore))
            {
                // Thin wall or fence: vault over to the far side if there is room and ground.
                Vector3 far = basePos + fwd * 1.9f;
                RaycastHit g;
                if (climbH < 1.4f && !Physics.CheckCapsule(far + Vector3.up * 0.5f, far + Vector3.up * 1.6f, 0.28f, mask, QueryTriggerInteraction.Ignore)
                    && Physics.Raycast(far + Vector3.up * 1.5f, Vector3.down, out g, 4f, mask, QueryTriggerInteraction.Ignore))
                {
                    dest = g.point + Vector3.up * 0.03f;
                }
                else return false;
            }
            _climbFrom = basePos;
            _climbTo = dest;
            _climbTop = top.point.y;
            _climbT = 0f;
            _climbDuration = 0.3f + Mathf.Clamp(climbH, 0.4f, 2.4f) * 0.28f;
            _climbing = true;
            Controller.enabled = false;
            _vel = Vector3.zero;
            _vy = 0f;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Jump, basePos, 0.3f, 0.1f, 12f);
            return true;
        }

        private float _climbTop;

        private void UpdateClimb(float dt)
        {
            _climbT += dt / _climbDuration;
            float t = Mathf.Clamp01(_climbT);
            float riseT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.7f));
            float moveT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.65f));
            // Clear the top of the obstacle (vaults land at ground level on the far side).
            float highest = Mathf.Max(Mathf.Max(_climbFrom.y, _climbTo.y), _climbTop) + 0.15f;
            float y = t < 0.6f ? Mathf.Lerp(_climbFrom.y, highest, riseT) : Mathf.Lerp(highest, _climbTo.y, (t - 0.6f) / 0.4f);
            Vector3 p = Vector3.Lerp(_climbFrom, _climbTo, moveT);
            p.y = y;
            transform.position = p;
            Anim.Velocity = transform.forward * 1.5f;
            Anim.Grounded = false;
            Anim.CrouchWeight = 0.4f;
            if (t >= 1f)
            {
                _climbing = false;
                transform.position = _climbTo;
                Controller.enabled = true;
                Physics.SyncTransforms();
                _grounded = true;
            }
        }

        // ================================================================== aiming and weapons

        private void UpdateAim(bool canControl)
        {
            AimTarget = null;
            AimVehicle = null;
            if (Game.Cam == null) return;
            Ray ray = Game.Cam.AimRay;
            int n = Physics.RaycastNonAlloc(ray, _aimHits, 600f, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            RaycastHit bestHit = new RaycastHit();
            bool found = false;
            float minDist = Game.Cam.DistanceToPivot * 0.9f;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _aimHits[i];
                if (h.collider == null) continue;
                if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform)) continue;
                if (InVehicle && h.collider.attachedRigidbody == CurrentVehicle.Body) continue;
                if (h.distance < minDist && !Game.Cam.IsFirstPerson) continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    bestHit = h;
                    found = true;
                }
            }
            if (found)
            {
                AimPoint = bestHit.point;
                AimTarget = bestHit.collider.GetComponentInParent<CharacterBase>();
                if (AimTarget != null && AimTarget.IsDead) AimTarget = null;
                Rigidbody rb = bestHit.collider.attachedRigidbody;
                if (rb != null) AimVehicle = rb.GetComponent<Vehicle>();
            }
            else AimPoint = ray.origin + ray.direction * 600f;
            if (Aiming && Rig != null) Anim.AimDirection = (AimPoint - Rig.Chest.position).normalized;
            Anim.Aiming = Aiming;

            // Aiming at a car's driver makes them bail out or floor it.
            if (Aiming && Weapons.Current.IsGun && AimTarget == null && AimVehicle != null && AimVehicle != CurrentVehicle && AimVehicle.Driver != null && !AimVehicle.Driver.IsPlayer)
            {
                _aimThreatTimer -= Time.deltaTime;
                if (_aimThreatTimer <= 0f)
                {
                    _aimThreatTimer = 0.5f;
                    Threats.Report(AimVehicle.transform.position, 4f, gameObject, ThreatKind.Aim);
                }
            }
            // Pointing a gun at someone scares them and is a crime in front of police.
            if (Aiming && Weapons.Current.IsGun && AimTarget != null)
            {
                _aimThreatTimer -= Time.deltaTime;
                if (_aimThreatTimer <= 0f)
                {
                    _aimThreatTimer = 0.5f;
                    Threats.Report(AimTarget.transform.position, 3f, gameObject, ThreatKind.Aim);
                    if (AimTarget.Team != null && AimTarget.Team.Team == NeonSyndicate.Team.Police && Game.Wanted != null)
                        Game.Wanted.ReportCrime(CrimeType.AimAtCop, AimTarget.transform.position, AimTarget.gameObject);
                }
            }
        }

        private void UpdateWeapons(float dt, bool canControl)
        {
            if (_hipFire > 0f) _hipFire -= dt;
            if (!canControl) return;
            WeaponInventory inv = Weapons.Inventory;
            bool inCar = InVehicle;
            bool scoped = Game.Cam != null && Game.Cam.ScopeActive;
            if (!scoped && !(Game.Hud != null && Game.Hud.WheelOpen))
            {
                float scroll = GInput.Scroll;
                if (scroll > 0.01f) CycleWeapon(1, inCar);
                else if (scroll < -0.01f) CycleWeapon(-1, inCar);
                if (!inCar && Act.WeaponNext) CycleWeapon(1, false);
                if (!inCar && Act.WeaponPrev) CycleWeapon(-1, false);
            }
            int num = GInput.NumberKeyDown();
            if (num >= 1 && num <= 8)
            {
                WeaponSlot slot = num == 1 ? WeaponSlot.Melee : (WeaponSlot)num;
                if (!inCar || slot == WeaponSlot.Handgun || slot == WeaponSlot.SMG) inv.SelectSlot(slot);
            }
            if (Act.Reload) Weapons.StartReload();
            if (!inCar && Act.Throw && Weapons.CanAct && Combat.State == PlayerCombat.Phase.Free)
            {
                WeaponId prev = inv.Current;
                if (Weapons.QuickThrow(AimPoint) && Weapons.Current.Slot == WeaponSlot.Throwable && prev != inv.Current)
                {
                    _restoreWeapon = prev;
                    _thrownWeapon = inv.Current;
                }
            }
            if (_restoreWeapon != WeaponId.Count && Weapons.CanAct)
            {
                // Go back to the weapon held before the quick throw, unless the player picked something else meanwhile.
                // If the throwable ran out, the auto-switch landed somewhere arbitrary, so go back anyway.
                bool stillHolding = inv.Current == _thrownWeapon;
                bool ranOut = _thrownWeapon != WeaponId.Count && !inv.Usable(_thrownWeapon);
                if ((stillHolding || ranOut) && inv.Usable(_restoreWeapon)) inv.Select(_restoreWeapon);
                _restoreWeapon = WeaponId.Count;
                _thrownWeapon = WeaponId.Count;
            }

            WeaponDef d = Weapons.Current;
            bool held = GInput.Fire;
            bool pressed = GInput.FireDown;
            if (inCar)
            {
                // Drive-by: aim with the right mouse button (or the pad's left bumper) using a one-handed weapon.
                bool driveBy = d.DriveBy && Act.DriveByAim;
                held = Act.DriveByFireHeld;
                pressed = Act.DriveByFirePressed;
                Aiming = driveBy;
                AimingDownSights = driveBy;
                Weapons.SetHidden(!driveBy);
                Anim.Aiming = driveBy;
                Anim.Hold = d.Hold;
                if (driveBy && (held || pressed))
                {
                    Weapons.TryFire(AimPoint, held, pressed, Weapons.CurrentSpread(true, CurrentVehicle.Speed * 0.3f, false) + 1.5f);
                }
                return;
            }
            // Melee weapons and fists belong to PlayerCombat (combos, heavies, ripostes).
            if (d.Fire == FireKind.Melee) return;
            if (Combat.State != PlayerCombat.Phase.Free || Combat.Guarding) return;
            // A click is remembered briefly so semi-auto shots and throws aren't lost while the body turns to aim.
            if (pressed) _pressBuffer = 0.3f;
            else if (_pressBuffer > 0f) _pressBuffer -= dt;
            bool press = pressed || _pressBuffer > 0f;
            if ((_cover || _dashing) && (held || press) && CoverFire(d, held, press)) return;
            if (held || press)
            {
                if (d.IsGun || d.Fire == FireKind.Throw) _hipFire = 1.1f;
                if (d.Fire == FireKind.Spin) Weapons.HoldSpin();
                // Only shoot once the body roughly faces the target so bullets leave the gun, not the back.
                float facing = Mathf.Abs(Mathf.DeltaAngle(_yaw, Game.Cam != null ? Game.Cam.Yaw : _yaw));
                if (facing < 40f)
                {
                    float spread = Weapons.CurrentSpread(GInput.Aim, _vel.magnitude, Crouched);
                    if (Weapons.TryFire(AimPoint, held, press, spread)) _pressBuffer = 0f;
                }
            }
        }

        private void CycleWeapon(int dir, bool inCar)
        {
            WeaponInventory inv = Weapons.Inventory;
            if (!inCar)
            {
                inv.Cycle(dir);
                return;
            }
            // In vehicles only drive-by weapons (and fists) are selectable; find the next one, then switch once.
            List<WeaponId> owned = inv.Owned;
            int start = Mathf.Max(0, owned.IndexOf(inv.Current));
            int step = dir >= 0 ? 1 : -1;
            for (int i = 1; i <= owned.Count; i++)
            {
                WeaponId next = owned[((start + step * i) % owned.Count + owned.Count) % owned.Count];
                WeaponDef nd = NeonSyndicate.Weapons.Get(next);
                if ((nd.DriveBy && inv.Usable(next)) || next == WeaponId.Fists)
                {
                    inv.Select(next);
                    return;
                }
            }
        }

        private void SnapToMeleeTarget()
        {
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, 2.6f, _near, (1 << Layers.Characters) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
            CharacterBase best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _near[i].GetComponentInParent<CharacterBase>();
                if (c == null || c == this || c.IsDead) continue;
                Vector3 to = MathUtil.Flat(c.transform.position - transform.position);
                float angle = Vector3.Angle(camFwd, to);
                if (angle > 70f) continue;
                float score = to.magnitude + angle * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            if (best != null)
            {
                Vector3 to = MathUtil.Flat(best.transform.position - transform.position);
                if (to.sqrMagnitude > 0.01f)
                {
                    _yaw = MathUtil.YawOf(to);
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                }
            }
            else if (Game.Cam != null)
            {
                _yaw = Game.Cam.Yaw;
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
        }

        // ================================================================== interaction

        private void UpdateInteraction(bool canControl)
        {
            Focus = Interactable.Nearest(transform.position, InVehicle);
            EnterCandidate = null;
            if (!InVehicle && _enter == EnterPhase.None)
            {
                EnterCandidate = FindVehicleToEnter();
                bool padUse = canControl && Act.PadUse;
                if (Focus != null && canControl && (Act.InteractKey || padUse))
                {
                    Focus.Activate();
                    padUse = false;
                }
                if (canControl && (Act.VehicleKey || padUse) && EnterCandidate != null && Combat.State == PlayerCombat.Phase.Free) BeginEnter(EnterCandidate);
            }
        }

        private Vehicle FindVehicleToEnter()
        {
            Vehicle best = null;
            float bestScore = float.MaxValue;
            Vector3 p = transform.position;
            Vector3 camFwd = Game.Cam != null ? Game.Cam.FlatForward : transform.forward;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle v = Vehicle.All[i];
                if (v == null || v.IsDestroyed || v.Speed > 5f) continue;
                Vector3 local = v.transform.InverseTransformPoint(p);
                float dx = Mathf.Max(0f, Mathf.Abs(local.x) - v.Def.Width * 0.5f);
                float dz = Mathf.Max(0f, Mathf.Abs(local.z) - v.Def.Length * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > 3.2f) continue;
                if (Mathf.Abs(local.y) > 2.5f) continue;
                Vector3 to = MathUtil.Flat(v.transform.position - p);
                float score = dist + Vector3.Angle(camFwd, to) * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = v;
                }
            }
            return best;
        }

        // ================================================================== entering vehicles

        private void BeginEnter(Vehicle v)
        {
            if (v.Def.IsBike)
            {
                BeginEnterBike(v);
                return;
            }
            _enterTarget = v;
            _enterSeat = 0;
            // Use the passenger door if it is much closer and the driver seat would need a long walk.
            if (v.Def.Seats > 1)
            {
                float dDriver = MathUtil.FlatDistance(transform.position, v.DoorPoint(0));
                float dPass = MathUtil.FlatDistance(transform.position, v.DoorPoint(1));
                if (dPass + 2.5f < dDriver && v.Occupants[1] == null && v.Driver == null) _enterSeat = 1;
            }
            _enter = EnterPhase.Approach;
            _enterTimer = 0f;
            Aiming = false;
            Crouched = false;
        }

        private void CancelEnter()
        {
            if (_enter == EnterPhase.None) return;
            if (_enterTarget != null) _enterTarget.OpenDoor(_enterSeat, false);
            _enter = EnterPhase.None;
            _enterTarget = null;
            if (Controller != null && !InVehicle && !IsRagdolled) Controller.enabled = true;
        }

        private void UpdateEntering(float dt, bool canControl)
        {
            Vehicle v = _enterTarget;
            if (v == null || v.IsDestroyed || (v.Speed > 6f && _enter != EnterPhase.Sit))
            {
                CancelEnter();
                return;
            }
            _enterTimer += dt;
            if (v.Def.IsBike)
            {
                UpdateEnteringBike(v, dt, canControl);
                return;
            }
            Vector3 door = v.DoorPoint(_enterSeat);
            switch (_enter)
            {
                case EnterPhase.Approach:
                {
                    if (canControl && (Act.VehicleKey || Act.PadUse || GInput.Move.sqrMagnitude > 0.5f && _enterTimer > 0.35f))
                    {
                        CancelEnter();
                        return;
                    }
                    Vector3 to = MathUtil.Flat(door - transform.position);
                    float dist = to.magnitude;
                    if (dist < 0.45f || _enterTimer > 2.6f)
                    {
                        if (_enterTimer > 2.6f)
                        {
                            // Teleport() cancels entering, so put the entry back afterwards.
                            int keepSeat = _enterSeat;
                            Teleport(door, MathUtil.YawOf(MathUtil.Flat(v.transform.forward)));
                            _enterTarget = v;
                            _enterSeat = keepSeat;
                            _enter = EnterPhase.Approach;
                        }
                        _enter = EnterPhase.Open;
                        _enterTimer = 0f;
                        bool locked = v.Locked || (v.Parked && v.Driver == null && v.OccupantCount() == 0 && Random.value < 0.25f);
                        _enterLockedSmash = locked;
                        if (locked)
                        {
                            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.GlassBreak, door + Vector3.up, 0.7f);
                            v.TriggerAlarm(18f);
                            if (Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.StealVehicle, v.transform.position, v.gameObject);
                        }
                        return;
                    }
                    Vector3 step = to.normalized * Mathf.Min(dist, 4.2f * dt);
                    _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(to), 720f * dt);
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                    _vy = Controller.isGrounded ? -2f : _vy - 19.6f * dt;
                    Controller.Move(step + Vector3.up * _vy * dt);
                    _vel = to.normalized * 4.2f;
                    Anim.Velocity = _vel;
                    Anim.Grounded = true;
                    Anim.Aiming = false;
                    break;
                }
                case EnterPhase.Open:
                {
                    Vector3 face = -v.transform.right * v.SeatSide(_enterSeat);
                    _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(face), 720f * dt);
                    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                    Anim.Velocity = Vector3.zero;
                    float openTime = _enterLockedSmash ? 0.75f : 0.32f;
                    if (_enterTimer > openTime * 0.5f) v.OpenDoor(_enterSeat, true);
                    if (_enterTimer >= openTime)
                    {
                        CharacterBase occupant = v.Occupants[_enterSeat];
                        _enter = occupant != null ? EnterPhase.Jack : EnterPhase.Sit;
                        _enterTimer = 0f;
                    }
                    break;
                }
                case EnterPhase.Jack:
                {
                    CharacterBase occupant = v.Occupants[_enterSeat];
                    if (occupant != null && _enterTimer > 0.25f)
                    {
                        occupant.ExitVehicleImmediate(false);
                        Vector3 away = MathUtil.Flat(occupant.transform.position - v.transform.position).normalized;
                        occupant.Knockdown(away * 2.5f + Vector3.up * 1.2f, occupant.ChestPosition, 1.4f);
                        NpcController npc = occupant as NpcController;
                        if (npc != null) npc.OnCarjacked(this);
                        if (Game.Wanted != null) Game.Wanted.ReportCrime(occupant.Team != null && occupant.Team.Team == NeonSyndicate.Team.Police ? CrimeType.AttackCop : CrimeType.Carjack, v.transform.position, occupant.gameObject);
                        if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Punch, door + Vector3.up, 0.6f, 0.1f, 25f);
                    }
                    if (_enterTimer > 0.6f)
                    {
                        _enter = EnterPhase.Sit;
                        _enterTimer = 0f;
                    }
                    break;
                }
                case EnterPhase.Sit:
                {
                    // Nobody is driving, so slide over and take the wheel even when entering from the passenger door.
                    int seat = _enterSeat != 0 && v.Driver == null ? 0 : _enterSeat;
                    if (v.Occupants[seat] != null && v.Occupants[seat] != this)
                    {
                        CancelEnter();
                        return;
                    }
                    // Getting back into your own car never throws out the people riding with you.
                    bool foreignCar = !v.StolenByPlayer;
                    if (seat == 0 && foreignCar)
                    {
                        // Passengers of a jacked car get out too: cops and crews fight, civilians run.
                        // Companions and mission passengers stay put.
                        for (int i = 1; i < v.Occupants.Length; i++)
                        {
                            NpcController other = v.Occupants[i] as NpcController;
                            if (other == null || other.Role == NpcRole.Companion) continue;
                            bool hostile = other.Role == NpcRole.Police || other.Role == NpcRole.Swat || other.Role == NpcRole.Gang || other.Role == NpcRole.Enemy;
                            if (!hostile && other.MissionTag != null) continue;
                            other.ExitVehicleImmediate(false);
                            if (hostile) other.Attack(this);
                            else other.Flee(transform.position, 12f);
                        }
                    }
                    Controller.enabled = false;
                    int doorSeat = _enterSeat;
                    EnterVehicleImmediate(v, seat);
                    _enter = EnterPhase.None;
                    _enterTarget = null;
                    // Close the door we actually came in through (the passenger door when sliding over).
                    _exitDoorVehicle = v;
                    _exitDoorSeat = doorSeat;
                    _exitDoorTimer = 0.55f;
                    break;
                }
            }
        }

        private bool _enterLockedSmash;

        protected override void OnEnteredVehicle()
        {
            Vehicle v = CurrentVehicle;
            if (Controller != null) Controller.enabled = false;
            Aiming = false;
            _vel = Vector3.zero;
            if (Seat == 0)
            {
                v.Handbrake = false;
                // Only the first time counts: climbing back into a car you already took is not a new theft.
                bool firstTime = !v.StolenByPlayer;
                if (firstTime && !v.MissionCritical && Game.Stats != null) Game.Stats.Add(StatId.VehiclesStolen, 1);
                v.StolenByPlayer = true;
                v.Parked = false;
                if (firstTime && v.IsPolice && Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.StealCopCar, v.transform.position, v.gameObject);
            }
            TrafficDriver ai = v.GetComponent<TrafficDriver>();
            if (ai != null) ai.enabled = false;
            // The bike you ride becomes "your bike" (the phone's Bring my bike returns it).
            if (Seat == 0 && v.Def.IsBike && Game.Traffic != null) Game.Traffic.NoteRiddenBike(v);
            if (Game.Radio != null) Game.Radio.OnEnterVehicle(v.IsPolice);
            if (Game.Hud != null) Game.Hud.ShowVehicleName(v.Def.Name);
        }

        protected override void OnExitedVehicle(Vehicle v)
        {
            if (Controller != null) Controller.enabled = true;
            Physics.SyncTransforms();
            _yaw = transform.eulerAngles.y;
            _vel = Vector3.zero;
            _vy = 0f;
            v.HornOn = false;
            v.BoostHeld = false;
            if (Game.Radio != null) Game.Radio.OnExitVehicle();
            Aiming = false;
        }

        private void UpdateExitDoor(float dt)
        {
            if (_exitDoorVehicle == null) return;
            _exitDoorTimer -= dt;
            if (_exitDoorTimer <= 0f)
            {
                _exitDoorVehicle.OpenDoor(_exitDoorSeat, false);
                _exitDoorVehicle = null;
            }
        }

        // ================================================================== driving

        private void UpdateInVehicle(float dt, bool canControl)
        {
            Vehicle v = CurrentVehicle;
            Anim.Velocity = Vector3.zero;
            Anim.Grounded = true;
            Anim.SitWeight = 1f;
            Anim.CrouchWeight = 0f;
            if (v == null) return;
            if (v.IsDestroyed)
            {
                ExitVehicleImmediate(false);
                return;
            }
            if (canControl && Act.ExitVehicle)
            {
                float speed = v.Speed;
                if (speed > 7f)
                {
                    v.Throttle = 0f;
                    ExitVehicleImmediate(true);
                    if (Game.Stats != null) Game.Stats.Add(StatId.Bailouts, 1);
                }
                else if (v.Def.IsBike)
                {
                    // Off a bike in one step, no door.
                    v.Throttle = 0f;
                    v.Handbrake = true;
                    ExitVehicleImmediate(false);
                }
                else
                {
                    int seat = Seat;
                    v.OpenDoor(seat, true);
                    v.Throttle = 0f;
                    v.Handbrake = true;
                    ExitVehicleImmediate(false);
                    _exitDoorVehicle = v;
                    _exitDoorSeat = seat;
                    _exitDoorTimer = 0.7f;
                }
                return;
            }
            if (Seat != 0)
            {
                Anim.Steer = 0f;
                return;
            }
            if (!canControl)
            {
                v.Throttle = 0f;
                v.SteerInput = 0f;
                v.Handbrake = !ControlsEnabled;
                v.HornOn = false;
                return;
            }
            float throttle = Act.Throttle;
            float steer = Act.Steer;
            v.Throttle = throttle;
            v.SteerInput = steer;
            v.DirectSteer = Act.SteerIsAnalog;
            v.Handbrake = Act.Handbrake;
            v.BoostHeld = Act.Boost;
            Anim.Steer = steer;
            if (Mathf.Abs(throttle) > 0.2f && v.GroundedWheels > 0) Pad.Rumble(0.04f + Mathf.Abs(throttle) * 0.05f, 0f, 0.05f);

            // Horn, or siren toggle on emergency vehicles (tap E = siren, hold E = horn).
            if (v.Def.Siren)
            {
                if (Act.HornHeld) _hornHold += dt;
                v.HornOn = _hornHold > 0.35f;
                if (Act.HornReleased)
                {
                    if (_hornHold <= 0.35f) v.SirenOn = !v.SirenOn;
                    _hornHold = 0f;
                }
            }
            else v.HornOn = Act.HornHeld;
            if (Act.Headlights) v.LightsOn = !v.LightsOn;
            if (Game.Radio != null)
            {
                if (Act.RadioNext) Game.Radio.Next();
                if (Act.RadioPrev) Game.Radio.Previous();
                if (Act.RadioOff) Game.Radio.Tune(-1);
            }
            if (Act.JobMode && Game.Activities != null) Game.Activities.ToggleVehicleJob(v);
        }

        // ================================================================== health regeneration

        private void UpdateRegen(float dt)
        {
            if (IsDead) return;
            // Out of combat health climbs back to the difficulty's cap (all the way on Story).
            float cap = Health.MaxHealth * CombatDirector.RegenCap;
            if (Health.Health < cap && Time.time - Health.LastDamageTime > 5f && !CombatDirector.InCombat)
                Health.Heal(Mathf.Min(dt * Health.MaxHealth * 0.06f, cap - Health.Health));
        }

        private float _lightTimer;

        /// <summary>DualSense / DualShock light bar: health colour, cyan in the Third Eye, red and blue when wanted.</summary>
        private void UpdatePadLight()
        {
            if (!Pad.Connected) return;
            _lightTimer += Time.unscaledDeltaTime;
            Color c;
            int stars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            if (IsDead) c = new Color(0.3f, 0f, 0f);
            else if (Game.Abilities != null && Game.Abilities.ThirdEyeActive) c = new Color(0.1f, 0.9f, 1f);
            else if (stars > 0 && Mathf.Repeat(_lightTimer * 2f, 1f) < 0.5f) c = Mathf.Repeat(_lightTimer, 1f) < 0.5f ? new Color(1f, 0f, 0f) : new Color(0f, 0.1f, 1f);
            else
            {
                float hp = Health.Health / Mathf.Max(1f, Health.MaxHealth);
                c = Color.Lerp(new Color(1f, 0.05f, 0.02f), new Color(0.75f, 0.2f, 1f), Mathf.Clamp01((hp - 0.15f) / 0.6f));
                if (hp < 0.25f) c *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_lightTimer * 6f));
            }
            Pad.SetLight(c);
        }
    }
}
