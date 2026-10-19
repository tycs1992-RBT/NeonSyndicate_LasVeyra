using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Classic open-world camera rig: third person (close shoulder, mid, far), first person with a viewmodel, aim zoom,
    /// sniper scope, vehicle chase cam with auto-recentering and speed FOV, hood cam, look-behind, collision,
    /// trauma shake, weapon recoil, death cam, title flyover and scripted cinematic shots.
    /// </summary>
    public sealed partial class PlayerCamera : MonoBehaviour
    {
        public enum ViewMode { Near, Mid, Far, First }

        // Rest tilts in degrees: on foot the street fills the lower frame, in a car the horizon sits near the top third.
        public const float FootPitch = 7.5f;
        public const float CarPitch = 9.2f;

        public Camera Cam;
        public float Yaw;
        public float Pitch = FootPitch;
        public ViewMode FootView = ViewMode.Mid;
        public ViewMode CarView = ViewMode.Near;
        public bool ShoulderLeft;
        public bool IsFirstPerson { get; private set; }
        public bool ScopeActive { get; private set; }
        public float ScopeFov = 13f;
        public float DistanceToPivot { get; private set; }

        private Vector3 _pivot;
        private Vector3 _smoothPos;
        private Vector3 _lastPivot;
        private float _dist = 4.0f;
        private float _bigBlend;
        private float _shoulder = 0.12f;
        private float _pivotHeight = 1.55f;
        private float _fov = 55f;
        private float _trauma;
        private float _recoilPitch;
        private float _recoilYaw;
        private float _noLookTime;
        private bool _cinematic;
        private Vector3 _cinePos;
        private Vector3 _cineLook;
        private float _cineFov = 50f;
        private float _returnYaw04L, _returnPitch04L;
        private float _orbit;
        private bool _wasInVehicle;
        private GameObject _viewmodel;
        private WeaponId _viewmodelId = WeaponId.Count;
        private float _viewKick;
        private int _lastShots;
        private Vehicle _lastVehicle;
        private float _recenter;
        private float _recenterYaw;
        private bool _wasAds;
        private float _snapTime;
        private float _snapYaw, _snapPitch;
        private float _lockBlend;
        private float _footClearance04H = -1f;
        private readonly RaycastHit[] _hits = new RaycastHit[12];

        public Vector3 FlatForward { get { return MathUtil.DirFromYaw(Yaw); } }

        public Ray AimRay
        {
            get
            {
                if (Cam == null) return new Ray(transform.position, transform.forward);
                return Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
        }

        public static PlayerCamera Create()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = Settings.ViewDistance;
            cam.fieldOfView = Settings.FieldOfView;
            cam.cullingMask = ~(1 << Layers.Minimap);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            go.AddComponent<AudioListener>();
            go.AddComponent<NeonPost>();
            PlayerCamera pc = go.AddComponent<PlayerCamera>();
            pc.Cam = cam;
            pc.SetupPhysical(cam);
            return pc;
        }

        // ------------------------------------------------------------------ api used by gameplay

        public void Shake(float amount)
        {
            if (!Settings.CameraShake) return;
            if (Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects) amount *= 0.35f;
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void AddRecoil(float recoil)
        {
            _recoilPitch += recoil * 0.16f;
            _recoilYaw += Random.Range(-0.5f, 0.5f) * recoil * 0.06f;
            _viewKick = Mathf.Min(1f, _viewKick + recoil * 0.08f);
        }

        public void SetCinematic(Vector3 position, Vector3 lookAt, float fov)
        {
            if (!_cinematic) { _returnYaw04L = Yaw; _returnPitch04L = Pitch; CutHistory(); }
            _cinematic = true;
            _cinePos = position;
            _cineLook = lookAt;
            _cineFov = fov;
            _cineBreath = false;
        }

        public void ClearCinematic()
        {
            if (!_cinematic) return;
            _cinematic = false;
            _cineBreath = false;
            _cineBreathT = 0f;
            Yaw = _returnYaw04L; Pitch = _returnPitch04L;
            _smoothPos = Vector3.zero;
            CutHistory();
        }

        public bool InCinematic { get { return _cinematic; } }

        /// <summary>True while the title flyover drives the camera (title and loading screens, or before a player exists).</summary>
        public bool InFlyover { get { return !_cinematic && (Game.State == GameState.Title || Game.State == GameState.Loading || Game.Player == null); } }

        /// <summary>Lock-on pressed with nothing to lock: swing the camera behind Malik.</summary>
        public void RecenterBehindPlayer()
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            _recenter = 0.3f;
            _recenterYaw = p.transform.eulerAngles.y;
        }

        public void SnapBehindPlayer()
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            Yaw = p.InVehicle ? p.CurrentVehicle.transform.eulerAngles.y : p.transform.eulerAngles.y;
            Pitch = p.InVehicle ? CarPitch : FootPitch;
            _smoothPos = Vector3.zero;
            CutHistory();
        }

        /// <summary>A hard cut: frame-to-frame effects (motion blur) must not streak across it.</summary>
        public void CutHistory()
        {
            NeonPost post = GetComponent<NeonPost>();
            if (post != null) post.ResetHistory();
        }

        // ------------------------------------------------------------------ update

        private void LateUpdate()
        {
            // Cinematics use unscaled camera damping, so explicitly hold the frame during Pause.
            if (_cinematic && GameTime.Paused) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (Cam == null) return;
            RestoreBasePose();
            Cam.farClipPlane = Settings.ViewDistance;
            PlayerController p = Game.Player;
            if (_cinematic)
            {
                if (_viewmodel != null) _viewmodel.SetActive(false);
                if (p != null) { p.Weapons.SetFirstPerson(false); p.Rig.SetVisible(true, true); }
                UpdateCinematic(dt);
                ApplyCinematicBreath(dt);
                UpdateOptics();
                return;
            }
            if (Game.State == GameState.Title || Game.State == GameState.Loading || p == null)
            {
                if (_viewmodel != null) _viewmodel.SetActive(false);
                UpdateFlyover(dt);
                ApplyTitleSway();
                UpdateOptics();
                return;
            }
            HandleInput(p, dt);
            if (p.IsDead || (p.IsRagdolled && !p.InVehicle))
            {
                UpdateRagdollCam(p, dt);
            }
            else if (p.InVehicle)
            {
                UpdateVehicleCam(p, p.CurrentVehicle, dt);
            }
            else
            {
                UpdateFootCam(p, dt);
            }
            ApplyShakeAndRecoil(dt);
            p.Weapons.SetFirstPerson(IsFirstPerson && !p.InVehicle);
            UpdateViewmodel(p, dt);
            ApplyPauseDrift(dt);
            UpdateOptics();
        }

        private void HandleInput(PlayerController p, float dt)
        {
            UpdateAimAssist(p, dt);
            Vector2 look = GInput.Look;
            bool blocked = GInput.GameplayBlocked || !Game.IsPlaying;
            if (blocked) look = Vector2.zero;
            // Locked on: the camera frames the fight by itself (the stick and mouse switch targets instead).
            bool locked = p.Combat != null && p.Combat.LockTarget != null && !p.InVehicle && !p.IsDead;
            if (locked) look = Vector2.zero;
            if (_recenter > 0f)
            {
                _recenter -= dt;
                Yaw = Mathf.LerpAngle(Yaw, _recenterYaw, MathUtil.Damp(14f, dt));
                Pitch = Mathf.Lerp(Pitch, FootPitch, MathUtil.Damp(10f, dt));
            }
            float sens = 1f;
            if (ScopeActive) sens = Mathf.Clamp(_fov / 60f, 0.12f, 1f);
            else if (p.AimingDownSights) sens = 0.7f;
            Yaw += look.x * sens;
            Pitch -= look.y * sens;
            Pitch = Mathf.Clamp(Pitch, -70f, 78f);
            if (look.sqrMagnitude > 0.0004f) _noLookTime = 0f;
            else _noLookTime += dt;
            if (!blocked && Act.CameraView)
            {
                if (p.InVehicle) CarView = CarView == ViewMode.Near ? ViewMode.Far : CarView == ViewMode.Far ? ViewMode.First : ViewMode.Near;
                else FootView = (ViewMode)(((int)FootView + 1) % 4);
            }
            if (!blocked && !p.InVehicle && Act.Shoulder(p.AimingDownSights)) ShoulderLeft = !ShoulderLeft;
        }

        /// <summary>
        /// Controller aim assist (guns only): pulling the aim trigger snaps toward the nearest hostile near the
        /// crosshair, the stick slows down over targets, and a gentle pull keeps the reticle on a moving enemy.
        /// </summary>
        private void UpdateAimAssist(PlayerController p, float dt)
        {
            GInput.PadLookScale = 1f;
            bool ads = p.AimingDownSights && !p.InVehicle && p.Weapons.Current.IsGun;
            bool pad = Pad.Connected && Pad.LastUsed && Settings.AimAssist;
            if (!pad || !ads || Cam == null)
            {
                _wasAds = ads;
                _snapTime = 0f;
                return;
            }
            CharacterBase best = null;
            float bestAngle = 30f;
            Vector3 eye = transform.position;
            Vector3 fwd = transform.forward;
            CharacterBase locked = p.Combat != null ? p.Combat.LockTarget : null;
            foreach (NpcController n in NpcController.All)
            {
                if (n.IsDead || (!n.IsHostileToPlayer && n != locked)) continue;
                Vector3 to = n.ChestPosition - eye;
                float dist = to.magnitude;
                if (dist > 60f || dist < 1f) continue;
                float ang = Vector3.Angle(fwd, to);
                if (n == locked) ang *= 0.3f;
                if (ang < bestAngle && !Physics.Linecast(eye, n.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    bestAngle = ang;
                    best = n;
                }
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (!c.Targetable) continue;
                Vector3 to = c.ChestPosition - eye;
                float dist = to.magnitude;
                if (dist > 70f || dist < 1f) continue;
                float ang = Vector3.Angle(fwd, to);
                if (c == locked) ang *= 0.3f;
                if (ang < bestAngle && !Physics.Linecast(eye, c.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    bestAngle = ang;
                    best = c;
                }
            }
            // 04E: an Underheart boss (not a Creature) through its lock point: its open weak point, else its centre.
            UBossLockProxy04E.ConsiderForAimAssist(eye, fwd, locked, ref best, ref bestAngle);
            if (best != null)
            {
                Vector3 to = best.ChestPosition - eye;
                float wantYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float wantPitch = -Mathf.Asin(Mathf.Clamp(to.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (!_wasAds && bestAngle < 26f)
                {
                    _snapTime = 0.14f;
                    _snapYaw = wantYaw;
                    _snapPitch = wantPitch;
                }
                if (_snapTime > 0f)
                {
                    _snapTime -= dt;
                    Yaw = Mathf.LerpAngle(Yaw, _snapYaw, MathUtil.Damp(22f, dt));
                    Pitch = Mathf.Lerp(Pitch, _snapPitch, MathUtil.Damp(22f, dt));
                }
                else if (bestAngle < 7f)
                {
                    // Magnetism: a soft pull that follows a moving target.
                    Yaw = Mathf.LerpAngle(Yaw, wantYaw, MathUtil.Damp(2.5f, dt));
                    Pitch = Mathf.Lerp(Pitch, wantPitch, MathUtil.Damp(2.5f, dt));
                }
                if (bestAngle < 5f) GInput.PadLookScale = 0.45f;
            }
            _wasAds = ads;
        }

        private void UpdateFootCam(PlayerController p, float dt)
        {
            if (_wasInVehicle)
            {
                _wasInVehicle = false;
                _smoothPos = Vector3.zero;
                _footClearance04H = -1f;
                Pitch = FootPitch;
            }
            TravelFollow(p, dt);
            WeaponDef d = p.Weapons.Current;
            bool aim = p.AimingDownSights;
            bool scope = aim && d.ScopeFov > 0f;
            if (scope && !ScopeActive) ScopeFov = d.ScopeFov;
            ScopeActive = scope;
            if (ScopeActive)
            {
                float s = GInput.Scroll;
                if (Mathf.Abs(s) > 0.01f) ScopeFov = Mathf.Clamp(ScopeFov - s * 4f, 4f, 30f);
            }
            bool first = FootView == ViewMode.First || ScopeActive;
            IsFirstPerson = first;
            p.Rig.SetVisible(!first, true);

            // Per-view framing (distance, shoulder offset, pivot height). Mid is the default: character centered
            // about four meters ahead. Near is the tight over-the-shoulder view, Far pulls back.
            float viewDist = FootView == ViewMode.Near ? 2.45f : FootView == ViewMode.Far ? 5.8f : 4.0f;
            float viewShoulder = FootView == ViewMode.Near ? 0.55f : FootView == ViewMode.Far ? 0.1f : 0.12f;
            float viewHeight = FootView == ViewMode.Near ? 1.6f : FootView == ViewMode.Far ? 1.5f : 1.55f;
            // Fights pull the camera back and up so you can read every attacker around you.
            float combat = CombatDirector.CombatFactor;
            CharacterBase lockT = p.Combat != null ? p.Combat.LockTarget : null;
            _lockBlend = Mathf.MoveTowards(_lockBlend, lockT != null ? 1f : 0f, dt * 3f);
            float pull = Mathf.Max(combat, _lockBlend);
            viewDist = Mathf.Lerp(viewDist, Mathf.Max(viewDist, 5.6f), pull);
            viewShoulder = Mathf.Lerp(viewShoulder, 0.05f, pull);
            viewHeight += 0.18f * pull;
            // Great beasts and bosses: pull well back and up so the whole body and its rings stay in frame.
            float big = Game.Creatures != null ? Game.Creatures.FramingSize(p.transform.position, lockT) : 0f;
            _bigBlend = Mathf.MoveTowards(_bigBlend, Mathf.Clamp01((big - 2.2f) / 6f), dt * 0.8f);
            viewDist += _bigBlend * 5f;
            viewHeight += _bigBlend * 0.9f;
            if (lockT != null)
            {
                Vector3 me = p.transform.position + Vector3.up * 1.5f;
                Vector3 tp = lockT.ChestPosition;
                if (lockT.BodyHeight > 2.6f) tp = Vector3.Lerp(lockT.transform.position, tp, 0.55f);
                Vector3 flat = MathUtil.Flat(tp - me);
                float fd = flat.magnitude;
                if (fd > 0.4f)
                {
                    float wantYaw = MathUtil.YawOf(flat);
                    float wantPitch = Mathf.Clamp(13f + Mathf.Atan2(me.y - tp.y, Mathf.Max(1f, fd)) * Mathf.Rad2Deg * 0.6f, -5f, 38f);
                    Yaw = Mathf.LerpAngle(Yaw, wantYaw, MathUtil.Damp(fd < 2.5f ? 3.5f : 7f, dt));
                    Pitch = Mathf.Lerp(Pitch, wantPitch, MathUtil.Damp(5f, dt));
                }
                viewDist += Mathf.Clamp(fd * 0.22f, 0f, 2.4f);
            }
            _pivotHeight = Mathf.Lerp(_pivotHeight, aim ? 1.62f : viewHeight, MathUtil.Damp(10f, dt));
            float height = p.Crouched ? _pivotHeight - 0.47f : _pivotHeight;
            if (p.Climbing) height = 1.4f;
            height += TravelPivotLift(p, dt);
            height += CoverPivotLift(p, dt);
            Vector3 targetPivot = p.transform.position + Vector3.up * height * p.Rig.Scale;
            _pivot = Vector3.Lerp(_pivot == Vector3.zero ? targetPivot : _pivot, targetPivot, MathUtil.Damp(TravelPivotSharpness(p, 25f), dt));
            if ((_pivot - targetPivot).sqrMagnitude > TravelSnapSqr(p, 25f)) _pivot = targetPivot;

            float baseFov = Settings.FieldOfView;
            float wantDist, wantShoulder, wantFov;
            if (first)
            {
                Vector3 eye = p.Rig.Head.position + Vector3.up * 0.07f + FlatForward * 0.12f;
                Quaternion rotFp = Quaternion.Euler(Pitch - _recoilPitch, Yaw + _recoilYaw, 0f);
                wantFov = ScopeActive ? ScopeFov : (aim ? baseFov * 0.78f : baseFov + 4f);
                _fov = Mathf.Lerp(_fov, wantFov, MathUtil.Damp(ScopeActive ? 30f : 14f, dt));
                SetFov(_fov);
                Cam.nearClipPlane = 0.06f;
                transform.SetPositionAndRotation(eye, rotFp);
                DistanceToPivot = 0f;
                return;
            }
            Cam.nearClipPlane = 0.15f;
            // Framing matched to the browser build.
            if (aim)
            {
                wantDist = 2.0f;
                wantShoulder = 0.72f;
                wantFov = baseFov * 0.78f;
            }
            else if (p.Sprinting)
            {
                wantDist = viewDist + 0.4f;
                wantShoulder = viewShoulder;
                wantFov = baseFov + 4f;
            }
            else
            {
                wantDist = viewDist;
                wantShoulder = p.Aiming ? Mathf.Max(0.55f, viewShoulder) : viewShoulder;
                wantFov = baseFov;
            }
            if (!aim) TravelFraming(p, ref wantDist, ref wantShoulder, ref wantFov);
            CoverFraming(p, aim, ref wantDist, ref wantShoulder, ref wantFov);
            // Local framing preserves the selected view, first-person option and all outdoor travel camera settings.
            if (Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.IsOperational && Game.Inhabited.Inside)
            {
                wantDist = Mathf.Min(wantDist, 2.35f);
                wantShoulder = Mathf.Min(wantShoulder, 0.36f);
            }
            // 04E: tight Underheart passages limit the boom like a venue interior (open caverns leave it alone).
            float underLimit = WorldBounds.CameraLimit(Game.PlayerPosition);
            if (underLimit < wantDist)
            {
                wantDist = underLimit;
                wantShoulder = Mathf.Min(wantShoulder, 0.4f);
            }
            if (ShoulderLeft) wantShoulder = -wantShoulder;
            _dist = Mathf.Lerp(_dist, wantDist, MathUtil.Damp(10f, dt));
            _shoulder = Mathf.Lerp(_shoulder, wantShoulder, MathUtil.Damp(10f, dt));
            _fov = Mathf.Lerp(_fov, wantFov, MathUtil.Damp(10f, dt));
            SetFov(_fov);

            Quaternion rot = Quaternion.Euler(Pitch - _recoilPitch, Yaw + _recoilYaw, TravelRoll(p, dt));
            Vector3 right = rot * Vector3.right;
            Vector3 shoulderPivot = _pivot + right * _shoulder;
            // Keep the shoulder offset out of walls.
            RaycastHit sh;
            if (Physics.SphereCast(_pivot, 0.18f, right * Mathf.Sign(_shoulder), out sh, Mathf.Abs(_shoulder), Layers.CameraMask, QueryTriggerInteraction.Ignore))
                shoulderPivot = _pivot + right * Mathf.Sign(_shoulder) * Mathf.Max(0f, sh.distance - 0.05f);
            Vector3 back = rot * Vector3.back;
            float allowed = CollideDistance(shoulderPivot, back, _dist, null);
            if(_footClearance04H<0f || allowed<_footClearance04H) _footClearance04H=allowed;
            else _footClearance04H=Mathf.Min(allowed,Mathf.Lerp(_footClearance04H,allowed,MathUtil.Damp(6f,dt)));
            float dist = _footClearance04H;
            Vector3 pos = shoulderPivot + back * dist;
            DistanceToPivot = dist;
            transform.SetPositionAndRotation(pos, rot);
        }

        private void UpdateVehicleCam(PlayerController p, Vehicle v, float dt)
        {
            ScopeActive = false;
            p.Rig.SetVisible(true, true);
            if (!_wasInVehicle || v != _lastVehicle)
            {
                _wasInVehicle = true;
                _lastVehicle = v;
                Yaw = v.transform.eulerAngles.y;
                Pitch = CarPitch;
                _smoothPos = Vector3.zero;
            }
            float speed = v.Speed;
            float fwdSpeed = v.ForwardSpeed;
            float vehYaw = v.transform.eulerAngles.y;
            if (Mathf.Abs(fwdSpeed) > 3f && v.GroundedWheels > 0)
            {
                float travelYaw = fwdSpeed < -3f ? vehYaw + 180f : vehYaw;
                Vector3 flatVel = MathUtil.Flat(v.Velocity);
                if (flatVel.sqrMagnitude > 9f && fwdSpeed > 0f) travelYaw = Mathf.LerpAngle(vehYaw, MathUtil.YawOf(flatVel), 0.45f);
                if (_noLookTime > 1.1f && !p.AimingDownSights)
                {
                    float k = MathUtil.Damp(Mathf.Lerp(1.2f, 3.2f, Mathf.Clamp01(speed / 30f)), dt);
                    Yaw = Mathf.LerpAngle(Yaw, travelYaw, k);
                    Pitch = Mathf.Lerp(Pitch, CarPitch, k);
                }
            }
            bool lookBack = Act.LookBehind;
            float yaw = Yaw + (lookBack ? 180f : 0f);
            IsFirstPerson = CarView == ViewMode.First;
            float baseFov = Settings.FieldOfView;
            float speedFov = Mathf.Clamp01(speed / Mathf.Max(20f, v.Def.TopSpeed)) * 10f;
            if (IsFirstPerson)
            {
                // First-person driving from the driver's eyes.
                Transform seat = v.SeatTransform(0);
                Vector3 hood = seat.position + v.transform.up * (0.92f * p.Rig.Scale) + v.transform.forward * 0.06f;
                Quaternion rotH = Quaternion.Euler(Pitch * 0.5f + v.transform.eulerAngles.x * 0.3f, v.transform.eulerAngles.y + Mathf.DeltaAngle(vehYaw, yaw) * 0.9f, 0f);
                if (_noLookTime > 1.1f) Yaw = Mathf.LerpAngle(Yaw, vehYaw, MathUtil.Damp(4f, dt));
                Cam.nearClipPlane = 0.08f;
                _fov = Mathf.Lerp(_fov, baseFov + 4f + speedFov, MathUtil.Damp(6f, dt));
                SetFov(_fov);
                transform.SetPositionAndRotation(hood, rotH);
                DistanceToPivot = 0f;
                return;
            }
            Cam.nearClipPlane = 0.2f;
            float size = Mathf.Max(v.Def.Length, v.Def.Height * 2f);
            // Chase camera: about two meters up and six back on a coupe, tilted down about nine degrees, so the
            // horizon sits near the top third and the road fills the lower half under the car.
            float wantDist = (CarView == ViewMode.Far ? 4.2f : 2.0f) + size * 0.92f;
            if (p.AimingDownSights) wantDist *= 0.8f;
            _dist = Mathf.Lerp(_dist, wantDist + speed * 0.02f, MathUtil.Damp(4f, dt));
            _fov = Mathf.Lerp(_fov, baseFov + speedFov, MathUtil.Damp(3f, dt));
            SetFov(_fov);
            // The interpolated transform (not the physics center of mass) keeps the pivot free of jitter.
            Vector3 pivotTarget = v.transform.position + Vector3.up * (v.Def.Height * 0.5f + 0.5f);
            _pivot = pivotTarget;
            Quaternion rot = Quaternion.Euler(Pitch - _recoilPitch, yaw + _recoilYaw, 0f);
            Vector3 back = rot * Vector3.back;
            float dist = CollideDistance(_pivot, back, _dist, v);
            Vector3 desired = _pivot + back * dist;
            // Ride along with the car exactly and smooth only the offset from it: turns still swing the view with
            // some weight, but speed never drags the camera back and shrinks the car on screen.
            if (_smoothPos == Vector3.zero || (_smoothPos - desired).sqrMagnitude > 400f) _smoothPos = desired;
            else _smoothPos += _pivot - _lastPivot;
            _lastPivot = _pivot;
            _smoothPos = Vector3.Lerp(_smoothPos, desired, MathUtil.Damp(12f, dt));
            DistanceToPivot = dist;
            // Look straight at the pivot: the same as looking along the orbit direction, and it stays right while the
            // camera lags behind the car or collision pulls it in.
            Vector3 toPivot = _pivot - _smoothPos;
            if (toPivot.sqrMagnitude < 0.01f) toPivot = rot * Vector3.forward;
            transform.SetPositionAndRotation(_smoothPos, Quaternion.LookRotation(toPivot.normalized, Vector3.up));
        }

        private void UpdateRagdollCam(PlayerController p, float dt)
        {
            ScopeActive = false;
            IsFirstPerson = false;
            p.Rig.SetVisible(true, true);
            Vector3 target = (p.Ragdoll != null && p.Ragdoll.Active ? p.Ragdoll.HipsPosition : p.transform.position) + Vector3.up * 0.6f;
            _pivot = Vector3.Lerp(_pivot, target, MathUtil.Damp(8f, dt));
            if (p.IsDead)
            {
                _orbit += dt * 12f;
                Yaw += dt * 10f;
                Pitch = Mathf.Lerp(Pitch, 35f, MathUtil.Damp(1.5f, dt));
                _dist = Mathf.Lerp(_dist, 6.5f, MathUtil.Damp(0.8f, dt));
            }
            else _dist = Mathf.Lerp(_dist, 4.2f, MathUtil.Damp(4f, dt));
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;
            float dist = CollideDistance(_pivot, back, _dist, null);
            transform.SetPositionAndRotation(_pivot + back * dist, rot);
            SetFov(Mathf.Lerp(Cam.fieldOfView, Settings.FieldOfView, MathUtil.Damp(4f, dt)));
            Cam.nearClipPlane = 0.15f;
            DistanceToPivot = dist;
        }

        private float CollideDistance(Vector3 pivot, Vector3 dir, float maxDist, Vehicle ignore)
        {
            int n = Physics.SphereCastNonAlloc(pivot, 0.24f, dir, _hits, maxDist, Layers.CameraMask, QueryTriggerInteraction.Ignore);
            if(n>=_hits.Length) return .35f;
            float best = maxDist;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null) continue;
                if (ignore != null && h.collider.attachedRigidbody == ignore.Body) continue;
                if (h.distance <= 0f) continue;
                if (h.distance < best) best = h.distance;
            }
            return Mathf.Max(0.35f, best - 0.08f);
        }

        private void ApplyShakeAndRecoil(float dt)
        {
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, MathUtil.Damp(9f, dt));
            _recoilYaw = Mathf.Lerp(_recoilYaw, 0f, MathUtil.Damp(9f, dt));
            if (_trauma > 0.001f)
            {
                float t = Time.unscaledTime * 22f;
                float s = _trauma * _trauma;
                Vector3 e = new Vector3((Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 5f, (Mathf.PerlinNoise(0.3f, t) - 0.5f) * 5f, (Mathf.PerlinNoise(t, t * 0.5f) - 0.5f) * 6f) * s;
                transform.rotation = transform.rotation * Quaternion.Euler(e);
                transform.position += transform.rotation * new Vector3((Mathf.PerlinNoise(t * 0.7f, 2f) - 0.5f), (Mathf.PerlinNoise(3f, t * 0.7f) - 0.5f), 0f) * s * 0.25f;
                _trauma = Mathf.Max(0f, _trauma - dt * 1.4f);
            }
        }

        private void UpdateViewmodel(PlayerController p, float dt)
        {
            bool show = IsFirstPerson && !ScopeActive && !p.InVehicle && !p.IsDead && !p.IsRagdolled;
            WeaponId id = p.Weapons.Current.Id;
            if (!show || id == WeaponId.Fists)
            {
                if (_viewmodel != null) _viewmodel.SetActive(false);
                return;
            }
            if (_viewmodel == null || _viewmodelId != id)
            {
                if (_viewmodel != null) Destroy(_viewmodel);
                Transform muzzle;
                _viewmodel = WeaponModels.Create(id, transform, out muzzle);
                _viewmodelId = id;
                foreach (Renderer r in _viewmodel.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            _viewmodel.SetActive(true);
            if (p.Weapons.ShotsFired != _lastShots)
            {
                _lastShots = p.Weapons.ShotsFired;
                _viewKick = Mathf.Min(1f, _viewKick + 0.35f);
            }
            _viewKick = Mathf.MoveTowards(_viewKick, 0f, dt * 6f);
            bool aim = p.AimingDownSights;
            float bob = p.FlatVelocity.magnitude > 0.5f ? Mathf.Sin(Time.time * 9f) * 0.012f : 0f;
            Vector3 hip = new Vector3(0.22f, -0.2f + bob, 0.42f);
            Vector3 ads = new Vector3(0f, -0.12f, 0.38f);
            Vector3 lp = Vector3.Lerp(hip, ads, aim ? 1f : 0f) + new Vector3(0f, 0f, -_viewKick * 0.07f);
            _viewmodel.transform.localPosition = Vector3.Lerp(_viewmodel.transform.localPosition, lp, MathUtil.Damp(18f, dt));
            _viewmodel.transform.localRotation = Quaternion.Euler(-_viewKick * 8f, 0f, 0f);
            if (p.Weapons.Reloading) _viewmodel.transform.localRotation *= Quaternion.Euler(25f, 0f, -20f);
        }

        // ------------------------------------------------------------------ title flyover and cinematics

        private void UpdateFlyover(float dt)
        {
            ScopeActive = false;
            IsFirstPerson = false;
            _orbit += dt * 2.2f;
            float a = _orbit * Mathf.Deg2Rad;
            Vector3 center = new Vector3(60f, 0f, -40f);
            Vector3 pos = center + new Vector3(Mathf.Sin(a) * 520f, 180f + Mathf.Sin(a * 0.5f) * 40f, Mathf.Cos(a) * 520f);
            Vector3 look = center + new Vector3(Mathf.Sin(a + 0.6f) * 120f, 40f, Mathf.Cos(a + 0.6f) * 120f);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            _flyFocus = (look - pos).magnitude;
            SetFov(55f);
            Cam.nearClipPlane = 0.3f;
        }

        private void UpdateCinematic(float dt)
        {
            ScopeActive = false;
            IsFirstPerson = false;
            if (Game.Player != null) Game.Player.Rig.SetVisible(true, true);
            Vector3 desired = Vector3.Lerp(transform.position, _cinePos, MathUtil.Damp(3f, dt));
            Vector3 offset = desired - _cineLook;
            float distance = offset.magnitude;
            // Check the interpolated camera as well as the destination: a clear target cannot justify moving through a wall.
            if (distance > .01f)
            {
                float clear = CollideDistance(_cineLook, offset / distance, distance, null);
                desired = _cineLook + offset / distance * Mathf.Max(.05f, clear);
            }
            transform.position = desired;
            Quaternion want = Quaternion.LookRotation((_cineLook - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, MathUtil.Damp(4f, dt));
            SetFov(Mathf.Lerp(Cam.fieldOfView, _cineFov, MathUtil.Damp(3f, dt)));
            Cam.nearClipPlane = 0.2f;
        }
    }
}
