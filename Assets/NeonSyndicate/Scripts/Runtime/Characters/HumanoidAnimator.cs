using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Drives a HumanoidRig every frame. Gameplay code sets the public fields (velocity, stance, attack timers, aim,
    /// travel and riding poses and so on); HumanoidPose turns them into a full-body pose with IK-planted feet, weapon
    /// holds with both hands, seated and riding poses and travel power poses. This component adds what needs the live
    /// scene: ground probing for the feet, the held model's foregrip and stock, the reload timer, the bike's anchors,
    /// hands up, cowering and phone calls, finger grips (HandPose), the telegraph glow, and blending up from a ragdoll.
    /// Since 04E it also plays the cast gestures (a greeting wave with Greet(), Inspect, Point toward PointTarget) and a
    /// head look toward LookTarget (HeadLook), all eased in and out and skipped while the arms are busy (aiming, attacks,
    /// riding, travel, cover, sitting, the phone). The gesture maths is the pure CastGestures04E.
    /// </summary>
    public sealed partial class HumanoidAnimator : MonoBehaviour
    {
        public HumanoidRig Rig;
        public Vector3 Velocity;
        public bool Grounded = true;
        public float VerticalVelocity;
        public float CrouchWeight;
        public bool Aiming;
        public Vector3 AimDirection = Vector3.forward;
        public HoldType Hold;
        public float SitWeight;
        public float Steer;
        public bool HandsUp;
        public bool Cower;
        public bool Phone;
        public float MeleeT = -1f;
        public bool MeleeLeft;
        public bool BatSwing;
        public float ThrowT = -1f;
        public float Recoil;
        public bool Frozen;
        public float Panic;
        public bool AlwaysAnimate;
        // Combat poses (player combat and telegraphed enemy attacks).
        public float AttackT = -1f;
        public MeleeAnim AttackAnim;
        public bool AttackMirror;
        public float AttackWindup = 0.35f;
        public float AttackActive = 0.15f;
        public float DodgeT = -1f;
        public Vector3 DodgeDir = Vector3.forward;
        public bool Backstep;
        public float Guard;
        public bool EnergyGuard;
        public bool ParcelCarry04G;
        public float CrystalCast04G;
        public float FrontierWard04OG;
        public int FrontierBossKind04OG,FrontierBossPhase04OG;
        public float SynergyCast04N;
        public int RegionalSynergyKind04OF;
        public float RegionalSynergyT04OF;
        public CrystalBehavior CrystalStyle04G;
        public bool TwoHanded;
        /// <summary>A bladed or blunt weapon (not fists) is held for melee.</summary>
        public bool HoldBlade;
        /// <summary>04F paired talons: both hands retain a real handle grip and the free blade counterbalances attacks.</summary>
        public bool PairedBlades;
        public float GuardHit;
        public float StaggerT = -1f;
        public float StaggerSide;
        public float DrinkT = -1f;
        public float ParryT = -1f;
        public float Telegraph;
        public float EliteGlow;
        /// <summary>Fighting stance weight: fists up or weapon forward when a fight is on.</summary>
        public float CombatReady;
        /// <summary>A travel power's body pose (super speed, leap, glide, flight); None for ordinary movement.</summary>
        public TravelPose Travel;
        /// <summary>0..1 how far the travel pose has blended in (the travel power eases it in and out).</summary>
        public float TravelBlend;
        /// <summary>Flight and glide: the body's lean in degrees (x pitch forward, z bank into a turn).</summary>
        public Vector3 TravelLean;
        /// <summary>How the character sits on a vehicle: None in cars, Bike astride a hover bike.</summary>
        public RidePose Ride;
        /// <summary>Riding: the vehicle's lean in degrees (negative leans left), so the rider leans with the bike.</summary>
        public float RideLean;
        /// <summary>Riding: the vehicle model root carrying the RideAnchors children (Seat, HandleL, HandleR, FootL, FootR).</summary>
        public Transform RideRoot;
        /// <summary>Cover 0..1 (facing the surface), head-high cover, the side looked along (-1 left, 1 right) and
        /// blind fire 0..1 (gun over or around the cover toward AimDirection). See HumanoidPose.Cover.</summary>
        public float Cover;
        public bool CoverHigh;
        public float CoverSide = 1f;
        public float CoverBlind;
        // 04E gestures, applied after the solver like HandsUp, Cower and Phone (see CastGestures04E for the maths).
        /// <summary>Both hands forward at chest height and the head lowered: examining something (a receiver, a panel).</summary>
        public bool Inspect;
        /// <summary>While true the right arm points at PointTarget (a world point), kept in front of the body.</summary>
        public bool Point;
        public Vector3 PointTarget;
        /// <summary>While true the head (and a little of the neck) turns toward LookTarget (a world point) within natural
        /// limits; a target behind the person is ignored rather than snapping the head around.</summary>
        public bool HeadLook;
        public Vector3 LookTarget;

        public HumanoidPose Pose;
        private WeaponController _weapons;
        private bool _weaponsLooked;
        private Transform _gunModel, _gunHand;
        private int _gunChildren = -1;
        private HoldType _gunHold;
        private bool _gunFound, _gunForegrip, _gunShoulder;
        private Vector3 _gunGrip, _gunButt;
        private Transform _rideFor, _seatT, _handleLT, _handleRT, _footLT, _footRT;
        private HandGrip _gripL = HandGrip.Relaxed, _gripR = HandGrip.Relaxed;
        private float _getUp;
        private Quaternion[] _getUpFrom;
        private Vector3 _getUpHips;
        private float _handsUpW, _cowerW, _phoneW;
        private float _greetT = -1f, _inspectW, _pointW, _lookW, _lookYaw, _lookPitch;
        private int _lastFrame = -10;
        private float _lastSolve = -1f;
        private float _glowShown = -1f;
        /// <summary>How fast the glow may fall per second: a wind-up flare still lights instantly (the warning cue is
        /// untouched) but fades out over about a tenth of a second instead of switching off in one frame.</summary>
        private const float GlowFallPerSecond = 24f;
        private int _spawnFrames = 4;
        private bool _probeOn;
        private HumanoidPose.GroundProbe _probe;

        private static Quaternion AX(float deg) { return QMath.AX(deg); }
        private static Quaternion AY(float deg) { return QMath.AY(deg); }
        private static Quaternion AZ(float deg) { return QMath.AZ(deg); }

        /// <summary>Called after a ragdoll hands control back: keeps the lying pose and blends up from it.</summary>
        public void SyncAfterRagdoll()
        {
            if (Rig == null || Rig.Bones == null) return;
            if (_getUpFrom == null) _getUpFrom = new Quaternion[Rig.Bones.Length];
            for (int i = 0; i < Rig.Bones.Length; i++) _getUpFrom[i] = Rig.Bones[i].localRotation;
            _getUpHips = Rig.Hips.localPosition;
            _getUp = 1f;
            if (Pose != null) Pose.ResetFeet();
        }

        /// <summary>Planted feet forget their spots (after teleports and vehicle exits).</summary>
        public void ResetFeet()
        {
            if (Pose != null) Pose.ResetFeet();
        }

        /// <summary>Plays one greeting wave with the right arm (about two seconds). A wave already past its middle restarts;
        /// it is skipped while the hands are up, cowering, on the phone, aiming, attacking, seated or riding.</summary>
        public void Greet()
        {
            if (_greetT < 0f || _greetT > CastGestures04E.GreetSeconds * 0.6f) _greetT = 0f;
        }

        /// <summary>True while a greeting wave plays.</summary>
        public bool Greeting { get { return _greetT >= 0f; } }

        private void EnsurePose()
        {
            if (Pose != null && Pose.Layout == Rig.Layout) return;
            Pose = new HumanoidPose(Rig.Layout);
            _probe = ProbeGround;
            Pose.Probe = _probe;
        }

        private bool ProbeGround(Vector3 world, out float groundY)
        {
            groundY = 0f;
            if (!_probeOn) return false;
            RaycastHit hit;
            float baseY = transform.position.y;
            Vector3 o = new Vector3(world.x, baseY + 0.55f * Rig.Scale, world.z);
            if (Physics.Raycast(o, Vector3.down, out hit, 1.1f * Rig.Scale, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
                return true;
            }
            return false;
        }

        private void LateUpdate()
        {
            if (GameTime.Paused || Frozen || Rig == null || Rig.Bones == null || Rig.Layout == null) return;
            // Off screen (renderer visibility is last frame's result) the pose still refreshes every eighth frame,
            // so a character never pops into view in a stale or bind pose; the first frames after spawning always run.
            bool visible = AlwaysAnimate || Rig.Body == null || Rig.Body.isVisible || _spawnFrames > 0;
            if (_spawnFrames > 0) _spawnFrames--;
            if (!visible && ((Time.frameCount + (GetInstanceID() & 7)) & 7) != 0) return;
            EnsurePose();
            // Skipped frames leave planted feet stale.
            if (Time.frameCount - _lastFrame > 2) Pose.ResetFeet();
            _lastFrame = Time.frameCount;
            float dt = _lastSolve < 0f ? Time.deltaTime : Time.time - _lastSolve;
            _lastSolve = Time.time;
            Animate(Mathf.Clamp(dt, 0.0001f, 0.1f));
            if(Game.Player!=null && Game.Player.Anim==this && Game.Player.Combat!=null)Game.Player.Combat.ResolveBladeAfterPose04N();
        }

        private void Animate(float dt)
        {
            // Self-timed bits of state.
            if (ParryT >= 0f)
            {
                ParryT += dt / 0.32f;
                if (ParryT > 1f) ParryT = -1f;
            }
            GuardHit = Mathf.MoveTowards(GuardHit, 0f, dt * 4f);

            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            float camDist = cam != null ? (cam.transform.position - transform.position).sqrMagnitude : 0f;
            bool seated = SitWeight > 0.5f || Ride != RidePose.None;
            _probeOn = Grounded && !seated && camDist < 30f * 30f;

            PoseInput p = PoseInput.Idle();
            p.RootPosition = transform.position;
            Vector3 fwd = transform.forward;
            p.RootYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            p.Velocity = seated ? Vector3.zero : Velocity;
            p.Grounded = Grounded || seated;
            p.VerticalVelocity = VerticalVelocity != 0f ? VerticalVelocity : Velocity.y;
            p.Crouch = Mathf.Max(CrouchWeight, Cower ? 0.8f : 0f) * (1f - SitWeight);
            p.Panic = Panic;
            p.Hold = Hold;
            p.Blade = HoldBlade;
            p.PairedBlades = PairedBlades;
            p.TwoHanded = TwoHanded;
            p.ParcelCarry04G = ParcelCarry04G;
            p.FrontierWard04OG=FrontierWard04OG;
            p.FrontierBossKind04OG=FrontierBossKind04OG;p.FrontierBossPhase04OG=FrontierBossPhase04OG;
            p.CrystalCast04G = Mathf.Max(CrystalCast04G,SynergyCast04N);
            p.RegionalSynergyKind04OF=RegionalSynergyKind04OF;p.RegionalSynergyT04OF=RegionalSynergyT04OF;
            p.CrystalStyle04G = CrystalStyle04G;
            p.Aiming = Aiming;
            p.Ready = CombatReady;
            p.Guard = Guard;
            p.EnergyGuard = EnergyGuard;
            p.GuardHit = GuardHit;
            p.AttackT = AttackT;
            p.AttackAnim = AttackAnim;
            p.AttackMirror = AttackMirror;
            p.AttackWindup = AttackWindup;
            p.AttackActive = AttackActive;
            p.DodgeT = DodgeT;
            p.DodgeDir = DodgeDir;
            p.Backstep = Backstep;
            p.StaggerT = StaggerT;
            p.StaggerSide = StaggerSide;
            p.DrinkT = DrinkT;
            p.ParryT = ParryT;
            p.SwingT = AttackT >= 0f ? -1f : MeleeT;
            p.SwingBat = BatSwing;
            p.SwingLeft = MeleeLeft;
            p.ThrowT = ThrowT;
            // Weapon: aim in root space, recoil, the reload timer and the held model's foregrip and stock.
            Quaternion invRoot = QMath.AY(-p.RootYaw);
            Vector3 aimDir = AimDirection.sqrMagnitude > 0.001f ? AimDirection.normalized : fwd;
            p.AimDir = invRoot * aimDir;
            p.Recoil = Recoil;
            if (!_weaponsLooked)
            {
                _weaponsLooked = true;
                _weapons = GetComponent<WeaponController>();
            }
            p.Reload = _weapons != null && _weapons.Reloading ? Mathf.Clamp01(_weapons.ReloadProgress) : -1f;
            WeaponGeometry();
            p.HasForegrip = _gunForegrip;
            p.Foregrip = _gunGrip;
            p.Butt = _gunButt;
            p.ShoulderHeavy = _gunShoulder;
            // Seats, riding and travel poses.
            p.Sit = SitWeight;
            p.Steer = Steer;
            p.Ride = Ride;
            p.RideLean = RideLean;
            RideAnchorsToRoot(ref p, invRoot);
            p.Travel = Travel;
            p.TravelBlend = TravelBlend;
            p.TravelLean = TravelLean;
            p.Cover = seated ? 0f : Cover;
            p.CoverHigh = CoverHigh;
            p.CoverSide = CoverSide;
            p.CoverBlind = CoverBlind;
#if UNITY_EDITOR
            PrepareFullBodyH8();
#endif
            Pose.Solve(ref p, dt);

            // ---- apply the solved pose (blending up from the ground after a ragdoll)
            Transform[] bones = Rig.Bones;
            if (_getUp > 0f)
            {
                _getUp = Mathf.MoveTowards(_getUp, 0f, dt / 0.9f);
                float k = QMath.Smooth01(1f - _getUp);
                for (int i = 0; i < bones.Length; i++) bones[i].localRotation = QMath.Slerp(_getUpFrom[i], Pose.Local[i], k);
                Rig.Hips.localPosition = Vector3.Lerp(_getUpHips, Pose.HipsPosition, k);
            }
            else
            {
                for (int i = 0; i < bones.Length; i++) bones[i].localRotation = Pose.Local[i];
                Rig.Hips.localPosition = Pose.HipsPosition;
            }

            // ---- arm overrides that are not part of the solver
            _handsUpW = Mathf.MoveTowards(_handsUpW, HandsUp ? 1f : 0f, dt * 5f);
            _cowerW = Mathf.MoveTowards(_cowerW, Cower && !HandsUp ? 1f : 0f, dt * 5f);
            _phoneW = Mathf.MoveTowards(_phoneW, Phone && !HandsUp && !Cower ? 1f : 0f, dt * 4f);
            if (_handsUpW > 0.001f)
            {
                Blend(RigLayout.UpperArmL, AZ(-22f) * AX(-168f), _handsUpW);
                Blend(RigLayout.UpperArmR, AZ(22f) * AX(-168f), _handsUpW);
                Blend(RigLayout.ForearmL, AX(-32f), _handsUpW);
                Blend(RigLayout.ForearmR, AX(-32f), _handsUpW);
            }
            if (_cowerW > 0.001f)
            {
                Blend(RigLayout.UpperArmL, AZ(-38f) * AX(-150f), _cowerW);
                Blend(RigLayout.UpperArmR, AZ(38f) * AX(-150f), _cowerW);
                Blend(RigLayout.ForearmL, AX(-125f), _cowerW);
                Blend(RigLayout.ForearmR, AX(-125f), _cowerW);
                Blend(RigLayout.Head, AX(25f), _cowerW);
            }
            if (_phoneW > 0.001f)
            {
                Blend(RigLayout.UpperArmR, AZ(18f) * AX(-38f), _phoneW);
                Blend(RigLayout.ForearmR, AX(-142f), _phoneW);
            }
            Gestures04E(dt);
            Performance04L(dt);
            Recoil = Mathf.MoveTowards(Recoil, 0f, dt * 70f);
            Fingers(dt);

            // ---- glow: enemy wind-ups flare, elites pulse (rises are instant, falls are eased; visual only)
            float glow = 1f + Telegraph * 2.6f + EliteGlow * (0.5f + 0.35f * Mathf.Sin(Time.time * 5f));
            _glowShown = glow >= _glowShown ? glow : Mathf.MoveTowards(_glowShown, glow, dt * GlowFallPerSecond);
            Rig.SetGlow(_glowShown);
        }

        private void Blend(int bone, Quaternion target, float w)
        {
            Transform b = Rig.Bones[bone];
            b.localRotation = QMath.Slerp(b.localRotation, target, w);
        }

        /// <summary>
        /// 04E gestures after the solver: the greeting wave (right arm), inspecting (both arms forward, head down),
        /// pointing (right arm at PointTarget) and the head look. Hands up, cowering and the phone win; nothing applies
        /// while aiming, attacking, seated, riding or using a travel power, so combat and vehicle poses stay untouched.
        /// </summary>
        private void Gestures04E(float dt)
        {
            bool armsBusy = HandsUp || Cower || Phone || Aiming || AttackT >= 0f || SitWeight > 0.5f || Ride != RidePose.None
                || Travel != TravelPose.None || Cover > 0.2f || DodgeT >= 0f || StaggerT >= 0f;
            if (_greetT >= 0f)
            {
                _greetT += dt;
                if (_greetT > CastGestures04E.GreetSeconds || armsBusy) _greetT = -1f;
            }
            _inspectW = Mathf.MoveTowards(_inspectW, Inspect && !armsBusy ? 1f : 0f, dt * 3.5f);
            _pointW = Mathf.MoveTowards(_pointW, Point && !armsBusy && _greetT < 0f ? 1f : 0f, dt * 4f);
            Vector3 up, fore;
            if (_inspectW > 0.001f)
            {
                CastGestures04E.Inspect(-1, out up, out fore);
                GestureArm(RigLayout.UpperArmL, RigLayout.ForearmL, up, fore, _inspectW);
                if (_greetT < 0f && _pointW < 0.5f)
                {
                    CastGestures04E.Inspect(1, out up, out fore);
                    GestureArm(RigLayout.UpperArmR, RigLayout.ForearmR, up, fore, _inspectW);
                }
                Blend(RigLayout.Head, AX(20f) * Rig.Bones[RigLayout.Head].localRotation, _inspectW);
            }
            if (_greetT >= 0f)
            {
                float w = CastGestures04E.Greet(_greetT, out up, out fore);
                GestureArm(RigLayout.UpperArmR, RigLayout.ForearmR, up, fore, w);
            }
            else if (_pointW > 0.001f && Rig.Chest != null && Rig.UpperArmR != null)
            {
                Vector3 dir = QMath.Inverse(Rig.Chest.rotation) * (PointTarget - Rig.UpperArmR.position);
                CastGestures04E.Point(dir, out up, out fore);
                GestureArm(RigLayout.UpperArmR, RigLayout.ForearmR, up, fore, _pointW);
            }
            // Head look: toward the target in the chest's frame, eased so a new target never snaps the head.
            float yaw = 0f, pitch = 0f;
            bool look = HeadLook && !HandsUp && !Cower && Rig.Chest != null && Rig.Head != null
                && CastGestures04E.Look(QMath.Inverse(Rig.Chest.rotation) * (LookTarget - Rig.Head.position), out yaw, out pitch);
            _lookW = Mathf.MoveTowards(_lookW, look ? 1f : 0f, dt * 2.5f);
            if (look)
            {
                float k = QMath.Damp(6f, dt);
                _lookYaw = Mathf.Lerp(_lookYaw, yaw, k);
                _lookPitch = Mathf.Lerp(_lookPitch, pitch, k);
            }
            if (_lookW > 0.001f)
            {
                Quaternion neck, head;
                CastGestures04E.LookRotations(_lookYaw, _lookPitch, out neck, out head);
                Blend(RigLayout.Neck, neck, _lookW);
                Blend(RigLayout.Head, head, _lookW);
            }
        }

        private void GestureArm(int upperBone, int foreBone, Vector3 upperDir, Vector3 foreDir, float w)
        {
            if (w <= 0.001f) return;
            Blend(upperBone, CastGestures04E.UpperArm(upperDir, foreDir), w);
            Blend(foreBone, CastGestures04E.Forearm(CastGestures04E.Elbow(upperDir, foreDir)), w);
        }

        /// <summary>
        /// Finger grips: the solver's choice per hand (pistol, rifle, support, handle, handlebar, fist, open, relaxed),
        /// overridden by hands up, cowering and the phone, eased so a change of hold never snaps. HandPose turns them
        /// into finger bone rotations once the rig has finger bones.
        /// </summary>
        private void Fingers(float dt)
        {
            HandGrip l = Grip(Pose.GripL), r = Grip(Pose.GripR);
            if (_performanceWeight04L > 0.5f && !HoldBlade && Hold == HoldType.None) r = HandGrip.Open;
            if (_phoneW > 0.5f) r = new HandGrip { Curl = 0.6f, IndexCurl = 0.5f, ThumbCurl = 0.3f, Spread = 0.05f, HandleRadius = 0.035f };
            if (_greetT >= 0f || _inspectW > 0.5f) r = HandGrip.Open;
            if (_pointW > 0.5f && _greetT < 0f) r = new HandGrip { Curl = 0.9f, IndexCurl = 0f, ThumbCurl = 0.7f, Spread = 0.02f, HandleRadius = 0.03f };
            if (_cowerW > 0.5f) l = r = HandGrip.Fist;
            if (_handsUpW > 0.5f) l = r = HandGrip.Open;
            float k = QMath.Damp(14f, dt);
            _gripL = HandGrip.Lerp(_gripL, l, k);
            _gripR = HandGrip.Lerp(_gripR, r, k);
            HandPose.Apply(Rig, _gripL, _gripR);
        }

        private static HandGrip Grip(HandHold h)
        {
            switch (h)
            {
                case HandHold.Fist: return HandGrip.Fist;
                case HandHold.Open: return HandGrip.Open;
                case HandHold.Pistol: return HandGrip.Pistol;
                case HandHold.Rifle: return HandGrip.Rifle;
                case HandHold.Support: return HandGrip.Support;
                case HandHold.Handle: return HandGrip.Handle;
                case HandHold.Handlebar: return HandGrip.Handlebar;
                default: return HandGrip.Relaxed;
            }
        }

        /// <summary>
        /// Finds the weapon model in the right hand (WeaponController parents it there) and reads its support point:
        /// the Foregrip child when it has one, otherwise an estimate from the mesh bounds; plus its stock and whether a
        /// heavy weapon is a shoulder launcher. Rescanned only when the hand's children or the hold change.
        /// </summary>
        private void WeaponGeometry()
        {
            Transform hand = Rig.HandR;
            if (hand == null) return;
            bool stale = _gunHand != hand || hand.childCount != _gunChildren || _gunHold != Hold || (_gunFound && (_gunModel == null || _gunModel.parent != hand));
            if (!stale) return;
            _gunHand = hand;
            _gunChildren = hand.childCount;
            _gunHold = Hold;
            _gunModel = null;
            _gunFound = false;
            _gunForegrip = false;
            _gunShoulder = false;
            _gunButt = Vector3.zero;
            _gunGrip = Vector3.zero;
            MeshFilter mf = null;
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform c = hand.GetChild(i);
                mf = c.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    _gunModel = c;
                    break;
                }
            }
            if (_gunModel == null) return;
            _gunFound = true;
            Bounds bb = mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(new Vector3(0f, 0.02f, 0.15f), new Vector3(0.1f, 0.16f, 0.8f));
            _gunShoulder = WeaponGrip.IsShoulderHeavy(Hold, bb.min);
            _gunButt = WeaponGrip.EstimateButt(bb.min, bb.max);
            Transform fg = WeaponGrip.FindForegrip(_gunModel);
            _gunForegrip = fg != null;
            _gunGrip = fg != null ? fg.localPosition : WeaponGrip.EstimateForegrip(Hold, bb.min, bb.max, _gunShoulder);
        }

        /// <summary>The bike's anchors (RideRoot children) in the solver's root space (root position, yaw only).</summary>
        private void RideAnchorsToRoot(ref PoseInput p, Quaternion invRoot)
        {
            p.RideAnchors = 0;
            if (Ride == RidePose.None || RideRoot == null) return;
            if (_rideFor != RideRoot)
            {
                _rideFor = RideRoot;
                _seatT = FindDeep(RideRoot, RideAnchors.Seat);
                _handleLT = FindDeep(RideRoot, RideAnchors.HandleL);
                _handleRT = FindDeep(RideRoot, RideAnchors.HandleR);
                _footLT = FindDeep(RideRoot, RideAnchors.FootL);
                _footRT = FindDeep(RideRoot, RideAnchors.FootR);
            }
            Vector3 o = transform.position;
            if (_seatT != null) { p.SeatPos = invRoot * (_seatT.position - o); p.RideAnchors |= 1; }
            if (_handleLT != null) { p.HandleL = invRoot * (_handleLT.position - o); p.RideAnchors |= 2; }
            if (_handleRT != null) { p.HandleR = invRoot * (_handleRT.position - o); p.RideAnchors |= 4; }
            if (_footLT != null) { p.PegL = invRoot * (_footLT.position - o); p.RideAnchors |= 8; }
            if (_footRT != null) { p.PegR = invRoot * (_footRT.position - o); p.RideAnchors |= 16; }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            Transform t = root.Find(name);
            if (t != null) return t;
            for (int i = 0; i < root.childCount; i++)
            {
                t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
