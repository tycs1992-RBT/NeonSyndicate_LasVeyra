using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Everything the pose solver needs for one frame (filled by HumanoidAnimator, or by the offline tests).</summary>
    public struct PoseInput
    {
        /// <summary>World position of the character root (at the feet) and its heading in degrees.</summary>
        public Vector3 RootPosition;
        public float RootYaw;
        /// <summary>World velocity; only the horizontal part drives the gait.</summary>
        public Vector3 Velocity;
        public bool Grounded;
        public float VerticalVelocity;
        public float Crouch;
        public float Panic;
        public HoldType Hold;
        /// <summary>A melee weapon (not fists or gauntlets) is in the right hand.</summary>
        public bool Blade;
        public bool PairedBlades;
        public bool ParcelCarry04G;
        public float CrystalCast04G;
        public float FrontierWard04OG;
        public int FrontierBossKind04OG,FrontierBossPhase04OG;
        public int RegionalSynergyKind04OF;
        public float RegionalSynergyT04OF;
        public CrystalBehavior CrystalStyle04G;
        public bool TwoHanded;
        /// <summary>Gun aiming: the arms are posed afterwards by HumanoidAnimator in world space.</summary>
        public bool Aiming;
        /// <summary>Fighting stance weight (0..1): fists up or blade forward.</summary>
        public float Ready;
        public float Guard;
        public bool EnergyGuard;
        public float GuardHit;
        public float AttackT;
        public MeleeAnim AttackAnim;
        public bool AttackMirror;
        public float AttackWindup, AttackActive;
        public float DodgeT;
        /// <summary>Roll direction in root space.</summary>
        public Vector3 DodgeDir;
        public bool Backstep;
        public float StaggerT, StaggerSide;
        public float DrinkT;
        public float ParryT;
        /// <summary>Older weapon-controller swings (punch or bat) and grenade throws, 0..1 or negative when idle.</summary>
        public float SwingT;
        public bool SwingBat, SwingLeft;
        public float ThrowT;
        /// <summary>Aim direction in root space (unit), read while Aiming.</summary>
        public Vector3 AimDir;
        /// <summary>Gun kick in degrees (it decays in the animator).</summary>
        public float Recoil;
        /// <summary>Magazine change progress 0..1, or negative when not reloading.</summary>
        public float Reload;
        /// <summary>The held model's support point (where the left hand grips it), model space; HasForegrip when the model
        /// carries a Foregrip child, otherwise an estimate from its bounds (or a cupped pistol hand).</summary>
        public Vector3 Foregrip;
        public bool HasForegrip;
        /// <summary>The held model's stock butt, model space (zero for a typical carbine stock).</summary>
        public Vector3 Butt;
        /// <summary>A heavy weapon fired from the shoulder (a launcher tube) rather than braced at the hip.</summary>
        public bool ShoulderHeavy;
        /// <summary>Car seat weight 0..1 and steering -1..1.</summary>
        public float Sit, Steer;
        /// <summary>Riding pose (a hover bike), its lean in degrees (negative leans left) and its anchors in root space.
        /// RideAnchors bits: 1 Seat, 2 HandleL, 4 HandleR, 8 FootL, 16 FootR (missing anchors fall back to a typical layout).</summary>
        public RidePose Ride;
        public float RideLean;
        public int RideAnchors;
        public Vector3 SeatPos, HandleL, HandleR, PegL, PegR;
        /// <summary>Travel power pose, its blend 0..1 and the body lean in degrees (x pitch forward, z bank right).</summary>
        public TravelPose Travel;
        public float TravelBlend;
        public Vector3 TravelLean;
        /// <summary>
        /// Cover weight 0..1: the character faces a surface it is hiding behind. CoverHigh is head-high cover (standing,
        /// turned along the surface); low cover crouches through Crouch. CoverSide is the way the character looks along
        /// the surface (-1 left, 1 right). CoverBlind 0..1 holds the gun over (low) or around (high) the cover toward
        /// AimDir while the head stays down. All zero for anyone not in cover (HumanoidPose.Cover.cs).
        /// </summary>
        public float Cover;
        public bool CoverHigh;
        public float CoverSide;
        public float CoverBlind;

        /// <summary>A standing, grounded character with every action timer idle (-1).</summary>
        public static PoseInput Idle()
        {
            PoseInput p = new PoseInput();
            p.Grounded = true;
            p.AttackT = p.DodgeT = p.StaggerT = p.DrinkT = p.ParryT = p.SwingT = p.ThrowT = -1f;
            p.AttackWindup = 0.35f;
            p.AttackActive = 0.15f;
            p.DodgeDir = Vector3.forward;
            p.AimDir = Vector3.forward;
            p.Reload = -1f;
            return p;
        }
    }

    /// <summary>
    /// Procedural humanoid motion as plain math (no engine calls), so the same code animates the game and the offline
    /// preview. Feet are placed with two-bone IK: a real gait cycle with heel strike, roll over the ball and toe-off,
    /// stance and swing phases, pelvis rotation, tilt and bob, counter-rotating shoulders and arm swing that blend from
    /// walk to run to sprint, crossover steps when strafing and a backpedal. When standing, fighting or turning on the
    /// spot the feet stay planted in the world and step when they drift. Combat moves are authored key poses (wind-up,
    /// strike, follow-through) for every MeleeAnim, plus rolls, backsteps, guards, parries, staggers and drinking.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        public delegate bool GroundProbe(Vector3 world, out float groundY);

        private struct Arm
        {
            public Vector3 Hand;      // wrist target (root space)
            public Vector3 Elbow;     // a point the elbow should lean toward (root space)
            public Quaternion Wrist;  // hand rotation relative to the forearm when HandW = 0
            public Vector3 Blade;     // where the held weapon (the hand's -Y) points, root space, when HandW = 1
            public Vector3 Edge;      // where the weapon's edge (the hand's +Z) faces
            public float HandW;
        }

        private struct Leg
        {
            public Vector3 Ankle;     // ankle target (root space)
            public Vector3 KneeDir;   // where the kneecap points (root space)
            public Quaternion Foot;   // foot rotation (root space)
            public float Lift;        // extra height even for a planted foot (a push-off lifts it where it stands)
        }

        private struct Body
        {
            public Vector3 Pelvis;
            public Quaternion Hips, Spine, Chest, Neck, Head;
            public Arm AL, AR;
            public Leg LL, LR;
            /// <summary>1: feet stay planted in the world and step toward the pose's feet; 0: feet follow the pose.</summary>
            public float Plant;
            /// <summary>How quickly planted feet step: 0 relaxed, 1 combat footwork.</summary>
            public float Urgency;
        }

        private struct Key
        {
            public float Yaw, Twist, Pitch, Drop, Step, Back;
            public Vector3 R, RPole, RBlade, REdge, L, LPole;
            /// <summary>Optional pass-through halfway from the wind-up to this strike key: hand position and blade
            /// direction. Big swings use it so the hand travels on an arc and the blade sweeps over the top or across
            /// the front instead of cutting along the chord (which runs through the body).</summary>
            public Vector3 RMid, RBladeMid;
            public bool HasL, HasMid;
        }

        public readonly RigLayout Layout;
        /// <summary>Solved local rotations for every bone (HumanoidRig bone order).</summary>
        public readonly Quaternion[] Local = new Quaternion[RigLayout.BoneCount];
        /// <summary>Solved hips position in root space (the Hips bone's localPosition).</summary>
        public Vector3 HipsPosition;
        /// <summary>Optional ground query for foot placement on slopes and steps.</summary>
        public GroundProbe Probe;
        /// <summary>Where the feet were asked to go this frame (root space), for diagnostics and tests.</summary>
        public Vector3 TargetAnkleL, TargetAnkleR;
        /// <summary>Gait phase 0..1 (left heel strike at 0), for diagnostics and tests.</summary>
        public float Phase { get { return _phase; } }

        private readonly float _s, _h0, _l1, _l2, _lu, _lf, _ankleH, _heel, _ball;
        private float _phase, _idleT, _crouch, _ready, _guard, _yawRate, _prevYaw, _land, _airT, _lean, _fwdAcc;
        private float _gaitYaw, _pelvisY, _reach = 9f, _lowestGround;
        /// <summary>Whole-body turn added after smoothing this frame (a spin attack's full rotation).</summary>
        private float _spinYaw;
        private bool _back, _hasPrevYaw, _wasGrounded = true, _init;
        private Vector3 _vel, _prevVel, _prevRoot, _dir = Vector3.forward;
        private Body _out;
        private bool _hasOut;
        // Planted feet (world space).
        private readonly Vector3[] _lock = new Vector3[2];
        private readonly float[] _lockYaw = new float[2];
        private readonly float[] _stepT = { -1f, -1f };
        private readonly Vector3[] _stepFrom = new Vector3[2];
        private readonly float[] _stepFromYaw = new float[2];
        private readonly float[] _sinceStep = new float[2];
        private bool _locksValid;
        private readonly Leg[] _legs = new Leg[2];
        private readonly float[] _groundOff = new float[2];
        // Gait stance anchoring (world space, horizontal): while a foot is in stance its heel stays where it landed, so
        // speeding up, slowing down, turning or a lagging velocity estimate never slides it. The offset from the
        // parametric gait fades out over the next swing so every step still lands where the gait wants it.
        private readonly Vector3[] _gaitCorr = new Vector3[2];
        private readonly Vector3[] _gaitAnchor = new Vector3[2];
        private readonly Vector3[] _gaitCorr0 = new Vector3[2];
        private readonly float[] _gaitSwingU0 = new float[2];
        private readonly int[] _gaitMode = new int[2];
        private readonly Vector3[] _gaitW = new Vector3[2];
        private readonly Vector3[] _finalW = new Vector3[2];
        // The same for the foot's heading: a planted foot does not swivel when the walking direction changes.
        private readonly float[] _gaitYawAnchor = new float[2];
        private readonly float[] _gaitYawCorr = new float[2];
        private readonly float[] _gaitYawCorr0 = new float[2];
        private readonly float[] _finalYaw = new float[2];
        private readonly Quaternion[] _finalRot = { QMath.Identity, QMath.Identity };
        private bool _gaitActive, _gaitHasFinal;
        private float _lastLockW = 1f, _plantG = 1f, _gaitT = 1f, _gaitSf = 0.6f;
        private bool _cadenceInit;
        // Swing leg forward kinematics (per frame): hip joint height, step size, weight and the gait blends it reads.
        private float _hipY, _fkAmp, _fkSwing, _run01, _sprint01, _edge = 0.1f;
        private bool _landing;
        /// <summary>Super speed weight (TravelPose.Surge times its blend), smoothed.</summary>
        private float _surge;
        // Humanoid creatures (RigLayout.Creature): all of these are inert for people, whose rigs keep every code path below
        // exactly as it was. Big bodies stretch their gait and steps in time (TimeScale), stalkers and golems hunch,
        // heavy arms swing wider, and floating bodies hover with their legs trailing as a tail.
        private readonly bool _creature, _float;
        private readonly float _tScale = 1f, _hunch, _armSpread;

        public HumanoidPose(RigLayout layout)
        {
            Layout = layout;
            _s = layout.Scale;
            _h0 = layout.Local[RigLayout.Hips].y;
            _l1 = layout.ThighLength;
            _l2 = layout.ShinLength;
            _lu = layout.UpperArmLength;
            _lf = layout.ForearmLength;
            _ankleH = layout.World[RigLayout.FootL].y;
            _heel = 0.075f * _s;
            _ball = 0.125f * _s;
            _pelvisY = _h0;
            for (int i = 0; i < Local.Length; i++) Local[i] = QMath.Identity;
            HipsPosition = layout.Local[RigLayout.Hips];
            if (layout.Creature != HumanoidCreature.None)
            {
                _creature = true;
                _float = layout.Floating;
                _tScale = Mathf.Clamp(layout.TimeScale, 0.5f, 3f);
                _hunch = layout.Hunch;
                _armSpread = layout.ArmSpread;
            }
        }

        /// <summary>True for a floating body (the Mirage Djinn): no foot is planted and the legs trail as a tail.</summary>
        public bool Floating { get { return _float; } }

        /// <summary>Forget planted feet (after a teleport, ragdoll or vehicle exit).</summary>
        public void ResetFeet()
        {
            _sideAcc04L = 0f;
            _locksValid = false;
            _stepT[0] = _stepT[1] = -1f;
            _hasOut = false;
            _gaitActive = _gaitHasFinal = false;
            _gaitCorr[0] = _gaitCorr[1] = Vector3.zero;
        }

        // ================================================================== helpers

        private static float E3(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }
        private static float EOut(float u) { u = 1f - Mathf.Clamp01(u); return 1f - u * u * u; }
        private static float SS(float a, float b, float x) { return E3((x - a) / (b - a)); }
        private static float Frac(float x) { return x - Mathf.Floor(x); }

        private static Vector3 Nrm(Vector3 v)
        {
            float m = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 1e-9f ? v / m : Vector3.zero;
        }

        /// <summary>Rotation whose local +Y is yAxis and whose +Z leans toward zHint.</summary>
        private static Quaternion Basis(Vector3 yAxis, Vector3 zHint)
        {
            Vector3 y = Nrm(yAxis);
            if (y.sqrMagnitude < 0.5f) return QMath.Identity;
            Vector3 z = zHint - y * Vector3.Dot(zHint, y);
            if (z.sqrMagnitude < 1e-8f)
            {
                z = Vector3.Cross(Vector3.right, y);
                if (z.sqrMagnitude < 1e-8f) z = Vector3.Cross(Vector3.up, y);
            }
            z = Nrm(z);
            Vector3 x = Vector3.Cross(y, z);
            return QMath.FromBasis(x, y, z);
        }

        /// <summary>Hand rotation that points a held weapon (the hand's -Y) along blade, with its edge (+Z) toward edge.</summary>
        private static Quaternion HandFrom(Vector3 blade, Vector3 edge)
        {
            return Basis(-blade, edge);
        }

        /// <summary>Two-bone IK: the middle joint position for a chain root -> joint -> end reaching for target.</summary>
        private static Vector3 SolveJoint(Vector3 root, Vector3 target, float l1, float l2, Vector3 pole, out Vector3 end)
        {
            Vector3 d = target - root;
            float dist = d.magnitude;
            Vector3 dir = dist > 1e-6f ? d / dist : Vector3.down;
            float reach = Mathf.Clamp(dist, Mathf.Abs(l1 - l2) + 0.01f, (l1 + l2) * 0.9995f);
            end = root + dir * reach;
            float cosA = Mathf.Clamp((l1 * l1 + reach * reach - l2 * l2) / (2f * l1 * reach), -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));
            Vector3 p = pole - dir * Vector3.Dot(pole, dir);
            if (p.sqrMagnitude < 1e-8f) p = Vector3.Cross(dir, Vector3.right);
            if (p.sqrMagnitude < 1e-8f) p = Vector3.Cross(dir, Vector3.forward);
            p = Nrm(p);
            return root + (dir * cosA + p * sinA) * l1;
        }

        private static Quaternion Mirror(Quaternion q) { return new Quaternion(q.x, -q.y, -q.z, q.w); }
        private static Vector3 Mirror(Vector3 v) { return new Vector3(-v.x, v.y, v.z); }

        private static Arm MirrorArm(Arm a)
        {
            a.Hand = Mirror(a.Hand);
            a.Elbow = Mirror(a.Elbow);
            a.Wrist = Mirror(a.Wrist);
            a.Blade = Mirror(a.Blade);
            a.Edge = Mirror(a.Edge);
            return a;
        }

        private static Leg MirrorLeg(Leg l)
        {
            l.Ankle = Mirror(l.Ankle);
            l.KneeDir = Mirror(l.KneeDir);
            l.Foot = Mirror(l.Foot);
            return l;
        }

        private static void MirrorBody(ref Body b)
        {
            b.Pelvis = Mirror(b.Pelvis);
            b.Hips = Mirror(b.Hips);
            b.Spine = Mirror(b.Spine);
            b.Chest = Mirror(b.Chest);
            b.Neck = Mirror(b.Neck);
            b.Head = Mirror(b.Head);
            Arm al = MirrorArm(b.AR), ar = MirrorArm(b.AL);
            b.AL = al;
            b.AR = ar;
            Leg ll = MirrorLeg(b.LR), lr = MirrorLeg(b.LL);
            b.LL = ll;
            b.LR = lr;
        }

        private static Arm Lerp(Arm a, Arm b, float t)
        {
            Arm r;
            r.Hand = Vector3.Lerp(a.Hand, b.Hand, t);
            r.Elbow = Vector3.Lerp(a.Elbow, b.Elbow, t);
            r.Wrist = QMath.Slerp(a.Wrist, b.Wrist, t);
            if (a.HandW < 0.001f) { r.Blade = b.Blade; r.Edge = b.Edge; }
            else if (b.HandW < 0.001f) { r.Blade = a.Blade; r.Edge = a.Edge; }
            else
            {
                // Directions interpolate along the chord, so a sword swings through the outside, not across the face.
                r.Blade = Nrm(Vector3.Lerp(a.Blade, b.Blade, t));
                if (r.Blade.sqrMagnitude < 0.5f) r.Blade = t < 0.5f ? a.Blade : b.Blade;
                r.Edge = Vector3.Lerp(a.Edge, b.Edge, t);
            }
            r.HandW = Mathf.Lerp(a.HandW, b.HandW, t);
            return r;
        }

        private static Leg Lerp(Leg a, Leg b, float t)
        {
            Leg r;
            r.Ankle = Vector3.Lerp(a.Ankle, b.Ankle, t);
            r.KneeDir = Vector3.Lerp(a.KneeDir, b.KneeDir, t);
            r.Foot = QMath.Slerp(a.Foot, b.Foot, t);
            r.Lift = Mathf.Lerp(a.Lift, b.Lift, t);
            return r;
        }

        private static void Blend(ref Body a, ref Body b, float t)
        {
            if (t <= 0f) return;
            if (t >= 1f) { a = b; return; }
            a.Pelvis = Vector3.Lerp(a.Pelvis, b.Pelvis, t);
            a.Hips = QMath.Slerp(a.Hips, b.Hips, t);
            a.Spine = QMath.Slerp(a.Spine, b.Spine, t);
            a.Chest = QMath.Slerp(a.Chest, b.Chest, t);
            a.Neck = QMath.Slerp(a.Neck, b.Neck, t);
            a.Head = QMath.Slerp(a.Head, b.Head, t);
            a.AL = Lerp(a.AL, b.AL, t);
            a.AR = Lerp(a.AR, b.AR, t);
            a.LL = Lerp(a.LL, b.LL, t);
            a.LR = Lerp(a.LR, b.LR, t);
            a.Plant = Mathf.Lerp(a.Plant, b.Plant, t);
            a.Urgency = Mathf.Lerp(a.Urgency, b.Urgency, t);
        }

        /// <summary>Blends only the spine, chest, neck and head.</summary>
        private static void BlendTorso(ref Body a, ref Body b, float t)
        {
            if (t <= 0f) return;
            a.Spine = QMath.Slerp(a.Spine, b.Spine, t);
            a.Chest = QMath.Slerp(a.Chest, b.Chest, t);
            a.Neck = QMath.Slerp(a.Neck, b.Neck, t);
            a.Head = QMath.Slerp(a.Head, b.Head, t);
        }

        private static void BlendArms(ref Body a, ref Body b, float t)
        {
            if (t <= 0f) return;
            a.AL = Lerp(a.AL, b.AL, Mathf.Min(1f, t));
            a.AR = Lerp(a.AR, b.AR, Mathf.Min(1f, t));
        }

        /// <summary>Shoulder position (root space) for a body pose.</summary>
        private Vector3 Shoulder(ref Body b, int side)
        {
            Quaternion rS = b.Hips * b.Spine;
            Vector3 pS = b.Pelvis + b.Hips * Layout.Local[RigLayout.Spine];
            Quaternion rC = rS * b.Chest;
            Vector3 pC = pS + rS * Layout.Local[RigLayout.Chest];
            return pC + rC * Layout.Local[side < 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR];
        }

        private Quaternion ChestRot(ref Body b) { return b.Hips * b.Spine * b.Chest; }

        private Vector3 ChestPos(ref Body b)
        {
            Quaternion rS = b.Hips * b.Spine;
            return b.Pelvis + b.Hips * Layout.Local[RigLayout.Spine] + rS * Layout.Local[RigLayout.Chest];
        }

        /// <summary>Root-space point attached to the head (offset in head space).</summary>
        private Vector3 HeadPoint(ref Body b, Vector3 offset)
        {
            Quaternion rC = ChestRot(ref b);
            Vector3 pN = ChestPos(ref b) + rC * Layout.Local[RigLayout.Neck];
            Quaternion rN = rC * b.Neck;
            Vector3 pH = pN + rN * Layout.Local[RigLayout.Head];
            return pH + rN * b.Head * offset;
        }

        /// <summary>Arm posed forward-kinematically (relative to the chest), expressed as an IK target.</summary>
        private Arm FkArm(ref Body b, int side, float swing, float abduct, float elbow, float twist, Quaternion wrist)
        {
            Quaternion rC = ChestRot(ref b);
            Vector3 sh = Shoulder(ref b, side);
            Quaternion up = rC * QMath.AZ(side * abduct) * QMath.AY(side * twist) * QMath.AX(-swing);
            Vector3 e = sh + up * new Vector3(0f, -_lu, 0f);
            Quaternion fore = up * QMath.AX(-elbow);
            Arm a;
            a.Hand = e + fore * new Vector3(0f, -_lf, 0f);
            a.Elbow = e;
            a.Wrist = wrist;
            a.Blade = rC * fore * wrist * Vector3.down;
            a.Edge = rC * fore * wrist * Vector3.forward;
            a.HandW = 0f;
            return a;
        }

        /// <summary>Arm reaching for a root-space wrist point, elbow leaning along pole (root space direction).</summary>
        private Arm IkArm(ref Body b, int side, Vector3 hand, Vector3 pole)
        {
            Vector3 sh = Shoulder(ref b, side);
            Arm a;
            a.Hand = hand;
            a.Elbow = (sh + hand) * 0.5f + Nrm(pole) * 0.3f * _s;
            a.Wrist = QMath.AX(-8f);
            a.Blade = Vector3.down;
            a.Edge = Vector3.forward;
            a.HandW = 0f;
            return a;
        }

        private Arm IkArm(ref Body b, int side, Vector3 hand, Vector3 pole, Vector3 blade, Vector3 edge, float handW)
        {
            Arm a = IkArm(ref b, side, hand, pole);
            if (handW > 0f && blade.sqrMagnitude > 1e-6f)
            {
                a.Blade = Nrm(blade);
                a.Edge = edge.sqrMagnitude > 1e-6f ? edge : Vector3.up;
                a.HandW = handW;
            }
            return a;
        }

        private Vector3 S(float x, float y, float z) { return new Vector3(x * _s, y * _s, z * _s); }

        // ================================================================== solve

        public void Solve(ref PoseInput p, float dt)
        {
            dt = Mathf.Clamp(dt, 0.0001f, 0.1f);
            float s = _s;
            if (_creature) _idleT += dt / _tScale;
            else _idleT += dt;
            if (!_init)
            {
                _init = true;
                _prevYaw = p.RootYaw;
                _prevRoot = p.RootPosition;
            }
            // A teleport (respawn, fast travel, leaving a vehicle) must not drag planted feet across the map.
            if ((p.RootPosition - _prevRoot).sqrMagnitude > 4f) ResetFeet();
            _prevRoot = p.RootPosition;
            // Root-space velocity, acceleration and turn rate.
            Quaternion rootRot = QMath.AY(p.RootYaw);
            Quaternion invRoot = QMath.AY(-p.RootYaw);
            Vector3 vWorld = new Vector3(p.Velocity.x, 0f, p.Velocity.z);
            Vector3 vRoot = invRoot * vWorld;
            _prevVel = _vel;
            _vel = Vector3.Lerp(_vel, vRoot, QMath.Damp(20f, dt));
            float yawDelta = Mathf.DeltaAngle(_prevYaw, p.RootYaw);
            _prevYaw = p.RootYaw;
            // Velocity is in root space, so turning rotates it; undo that before measuring acceleration.
            Vector3 accel = (_vel - QMath.AY(-yawDelta) * _prevVel) / dt;
            _sideAcc04L = Mathf.Lerp(_sideAcc04L, Mathf.Clamp(accel.x, -12f, 12f), QMath.Damp(5f, dt));
            _fwdAcc = Mathf.Lerp(_fwdAcc, Mathf.Clamp(accel.z, -12f, 12f), QMath.Damp(5f, dt));
            _yawRate = Mathf.Lerp(_yawRate, Mathf.Clamp(yawDelta / dt, -400f, 400f), QMath.Damp(6f, dt));
            _crouch = Mathf.MoveTowards(_crouch, p.Crouch, dt * 3.5f);
            _ready = Mathf.MoveTowards(_ready, Mathf.Clamp01(p.Ready), dt * 5f);
            _guard = Mathf.MoveTowards(_guard, Mathf.Clamp01(p.Guard), dt * 9f);

            // Landing dip and air time.
            if (!p.Grounded) _airT += dt;
            if (p.Grounded && !_wasGrounded && _airT > 0.15f) _land = Mathf.Clamp01(_airT * 1.6f);
            if (p.Grounded) _airT = 0f;
            _wasGrounded = p.Grounded;
            _land = Mathf.MoveTowards(_land, 0f, dt * 3.2f);

            Body b = new Body();
            _spinYaw = 0f;
            TravelWeights(ref p, dt);
            SeatWeights(ref p, dt);
            Locomotion(ref p, dt, ref b);
            WeightBalance04L(ref p, ref b);
            Travel(ref p, ref b);
            Seat(ref p, ref b);
            CoverWeights(ref p, dt);
            CoverBody(ref p, ref b);
            Carry(ref p, ref b);
            Anticipation04H(ref p, ref b);
            GroundedRecovery04OB(ref p,ref b);
            CoverArms(ref p, ref b);
            if (_ready > 0.001f) Stance(ref p, ref b, _ready, false);
            if (_guard > 0.001f) Stance(ref p, ref b, _guard, true);
            if (p.GuardHit > 0.001f) GuardRecoil(ref b, Mathf.Clamp01(p.GuardHit));
            if (p.AttackT >= 0f) Attack(ref p, ref b, p.AttackAnim, p.AttackMirror, p.AttackT, Mathf.Clamp(p.AttackWindup, 0.05f, 0.9f), Mathf.Clamp(p.AttackActive, 0.02f, 0.5f));
            else if (p.SwingT >= 0f) Attack(ref p, ref b, p.SwingBat ? MeleeAnim.HomeRun : MeleeAnim.Jab, p.SwingLeft, p.SwingT, 0.32f, 0.25f);
            Motion04K(ref p, ref b, dt);
            PairedBladeMotion04F(ref p, ref b);
            AscensionWeight04OH(ref p,ref b);
            Casting04G(ref p, ref b);
            DefenderWard04OG(ref p,ref b);
            FrontierBossStance04OG(ref p,ref b);
            RegionalSynergy04OF(ref p,ref b,dt);
            if (p.ThrowT >= 0f) Throw(ref b, p.ThrowT);
            if (p.ParryT >= 0f) Parry(ref p, ref b, p.ParryT);
            if (p.DrinkT >= 0f) Drink(ref b, p.DrinkT);
            if (p.StaggerT >= 0f) Stagger(ref b, p.StaggerT, p.StaggerSide);
            if (p.DodgeT >= 0f)
            {
                if (p.Backstep) Backstep(ref b, p.DodgeT);
                else Roll(ref b, p.DodgeT, p.DodgeDir);
            }
            if (_float) Hover(ref b);
            AuthoredBodyH8(ref p,ref b,dt);
            // An action that ends with the feet planted hands them back planted, instead of to a gait that still
            // thinks the body is moving from the dodge or lunge a moment ago.
            if (p.AttackT >= 0f || p.DodgeT >= 0f || p.StaggerT >= 0f || p.SwingT >= 0f) _plantG = Mathf.Max(_plantG, b.Plant);
            Feet(ref p, ref b, dt, rootRot, invRoot);
            // Planted feet can sit where the gait's own feet are not (stopping mid-stride, stepping into the fighting
            // stance): the pelvis must stay low enough for them too, or they would be dragged.
            float reachFeet = float.MaxValue;
            if (p.Grounded && _lastLockW > 0.001f && !_float)
            {
                Vector3 flat = new Vector3(b.Pelvis.x, 0f, b.Pelvis.z);
                reachFeet = Mathf.Min(PelvisLimit(ref b.LL, _lastLockW, b.Hips, -1f, flat, 4f), PelvisLimit(ref b.LR, _lastLockW, b.Hips, 1f, flat, 4f));
            }
            Smooth(ref b, dt, p.AttackT >= 0f || p.DodgeT >= 0f || p.StaggerT >= 0f);
            if (b.Pelvis.y > reachFeet)
            {
                b.Pelvis.y = reachFeet;
                _out.Pelvis.y = b.Pelvis.y;
            }
            // Smoothing must never lift the pelvis past what the planted legs can reach (that would make feet skate).
            if (p.Grounded && b.Pelvis.y > _reach + _lowestGround)
            {
                b.Pelvis.y = _reach + _lowestGround;
                _out.Pelvis.y = b.Pelvis.y;
            }
            if (_spinYaw != 0f) SpinBody(ref b, _spinYaw);
            ChooseGrips(ref p);
            SeatHands(ref p, ref b);
            AimPost(ref p, ref b, dt, _seatW > 0.5f);
            SupportWeight(ref p, dt);
            Apply(ref b, ref p);
        }

        /// <summary>Turns the whole posed body about the vertical axis through the root.</summary>
        private static void SpinBody(ref Body b, float yaw)
        {
            Quaternion r = QMath.AY(yaw);
            b.Pelvis = r * b.Pelvis;
            b.Hips = r * b.Hips;
            b.AL = ArmToRoot(b.AL, r, Vector3.zero);
            b.AR = ArmToRoot(b.AR, r, Vector3.zero);
            b.LL.Ankle = r * b.LL.Ankle;
            b.LR.Ankle = r * b.LR.Ankle;
            b.LL.KneeDir = r * b.LL.KneeDir;
            b.LR.KneeDir = r * b.LR.KneeDir;
            b.LL.Foot = r * b.LL.Foot;
            b.LR.Foot = r * b.LR.Foot;
        }

        // ================================================================== locomotion

        private void Locomotion(ref PoseInput p, float dt, ref Body b)
        {
            float s = _s;
            float speed = _vel.magnitude;
            // People normalise speed by their size; creatures use the square-root (Froude) rule, so a giant at a person's
            // pace by that measure strides long and slow instead of scurrying (see RigLayout.TimeScale).
            float sp = _creature ? speed * _tScale / s : speed / s;
            if (speed > 0.08f) _dir = _vel / speed;
            float crouch = _crouch;
            float surge = _surge;
            float fem = Layout.Female ? 1f : 0f;
            float walk01 = SS(0.04f, 0.8f, sp);
            // Sidesteps (lock-on strafing) stay a quick walk to higher speeds: a sideways leg cannot reach a running stride.
            float sideways = Mathf.Abs(_dir.x) * walk01;
            float run01 = SS(1.8f + 0.6f * sideways, 2.6f + 0.9f * sideways, sp);
            float sprint01 = SS(5.2f, 7.6f, sp);
            // Cadence from published stride-frequency data: walking about 0.55 + 0.3 v strides a second, running 1.3 rising
            // to 2.1 at 10 m/s, with speed measured in body sizes so children and tall people step at their own rate.
            float rs = Mathf.Max(0f, sp - 2f);
            float freq = Mathf.Lerp(0.55f + 0.3f * sp, Mathf.Min(1.28f + 0.035f * rs + 0.0085f * rs * rs, 2.3f), run01) * (1f + 0.03f * fem);
            // Super speed: a stylised fast cadence that still reads at 30 m/s (real legs would blur past 4 strides a second).
            freq = Mathf.Lerp(freq, Mathf.Lerp(2.7f, 3.8f, SS(10f, 30f, sp)), surge);
            float T = (1f / Mathf.Max(0.3f, freq)) * (1f + 0.18f * crouch) * (1f - 0.22f * sideways);
            if (_creature) T *= _tScale;
            // Stance share: about 62 percent of the cycle walking; running, a contact distance of roughly one leg length
            // (faster running means shorter contact and a longer float).
            float legLen = _l1 + _l2;
            float sfWalk = Mathf.Clamp(0.7f - 0.06f * sp, 0.57f, 0.68f);
            float dRun = (0.89f + 0.12f * SS(3f, 8f, sp)) * legLen * (1f - 0.3f * sideways);
            float sfRun = Mathf.Clamp(dRun / Mathf.Max(0.05f, speed * T), 0.1f, 0.45f);
            float sf = Mathf.Lerp(sfWalk, sfRun, run01) + 0.06f * crouch;
            // Legs only reach so far: past that the cadence rises instead of the stride growing.
            float maxTravel = Mathf.Lerp(1f, 1.03f, run01) * legLen * (1f + 0.1f * surge) * (1f - 0.25f * sideways);
            if (speed * T * sf > maxTravel) T = maxTravel / Mathf.Max(0.01f, speed * sf);
            // Braking keeps the quicker cadence for a moment (short, fast steps) instead of dropping straight into long
            // walking strides, which would leave the planted foot down far too long and drag it.
            if (_cadenceInit)
            {
                T = Mathf.Min(T, _gaitT + dt * 0.8f);
                sf = Mathf.Min(sf, _gaitSf + dt * 0.6f);
            }
            _gaitT = T;
            _gaitSf = sf;
            _cadenceInit = true;
            // A planted foot the body has already moved past (starting off from a standstill, pushing out of a turn)
            // ends its contact sooner: a quicker step instead of a lunge to keep reaching it. A foot that is ahead of
            // where the gait wants it (braking) is simply left planted.
            float behind = 0f;
            if (_gaitActive)
            {
                Quaternion inv = QMath.AY(-p.RootYaw);
                for (int f = 0; f < 2; f++)
                    if (_gaitMode[f] != 2) behind = Mathf.Max(behind, -Vector3.Dot(inv * _gaitCorr[f], _dir));
            }
            if (sp > 0.02f) _phase = Frac(_phase + dt / T * (1f + 3f * SS(0.06f * s, 0.24f * s, behind)));

            // Gait direction: strafes step diagonally (crossover), going backwards backpedals.
            float beta = Mathf.Atan2(_dir.x, _dir.z) * Mathf.Rad2Deg;
            if (!_back && Mathf.Abs(beta) > 112f && sp > 0.3f) _back = true;
            else if (_back && Mathf.Abs(beta) < 68f) _back = false;
            float gaitTarget = _back ? Mathf.DeltaAngle(0f, beta - 180f) : beta;
            gaitTarget = Mathf.Clamp(gaitTarget, -82f, 82f) * walk01;
            _gaitYaw = Mathf.LerpAngle(_gaitYaw, gaitTarget, QMath.Damp(8f, dt));
            float strideSign = _back ? -1f : 1f;
            Quaternion gaitRot = QMath.AY(_gaitYaw);
            float ph = _phase * Mathf.PI * 2f;

            // ---- pelvis: a few degrees of rotation and drop each step (a little more for women)
            float yawAmp = (Mathf.Lerp(4.5f, 7f, run01) + 1.2f * fem) * walk01;
            float rollAmp = (Mathf.Lerp(3.5f, 2.5f, run01) + 1f * fem) * walk01;
            if (_float)
            {
                // A hovering body glides: little of the walking sway.
                yawAmp *= 0.3f;
                rollAmp *= 0.25f;
            }
            float shiftAmp = (Mathf.Lerp(0.022f, 0.008f, run01) + 0.004f * fem) * walk01 * s;
            float idleSway = (1f - walk01);
            float weightShift = Mathf.Sin(_idleT * 0.55f) * idleSway;
            float hipFollow = Mathf.Lerp(0.45f, 0.72f, Mathf.Abs(_gaitYaw) / 82f);
            float pelvisYaw = _gaitYaw * hipFollow + yawAmp * Mathf.Cos(ph) * strideSign;
            float pelvisRoll = -rollAmp * Mathf.Sin(ph) + weightShift * 2.2f;
            // Lean into turns when moving fast.
            float turnLean = Mathf.Clamp(-_yawRate * (_creature ? sp : speed) * 0.012f, -14f, 14f) * run01;
            Vector3 shift = gaitRot * new Vector3(-shiftAmp * Mathf.Sin(ph), 0f, 0f) + new Vector3(weightShift * 0.018f * s, 0f, 0f);
            b.Hips = QMath.AY(pelvisYaw) * QMath.AZ(pelvisRoll + turnLean);

            // ---- legs (gait targets) and pelvis height
            Leg l0, l1;
            float w0, w1;
            float lift = s * (Mathf.Lerp(0.05f, 0.1f, Mathf.Clamp01(sp / 1.6f)) * (1f - run01) + Mathf.Lerp(0.17f, 0.3f, sprint01) * run01) * (1f - 0.4f * crouch);
            // The swing leg follows published hip and knee curves (knee to about 60 degrees walking, 100 to 125 running),
            // scaled down for short steps; strafes, backpedals and crouches keep the plain swing arc.
            float stride = speed * T * sf;
            _fkAmp = Mathf.Clamp01(stride / (0.75f * legLen));
            _fkSwing = (_back ? 0f : 1f - sideways) * SS(0.25f, 0.6f, _fkAmp) * (1f - crouch) * (_float ? 0f : 1f);
            _hipY = _pelvisY + Layout.Local[RigLayout.ThighL].y;
            _run01 = run01;
            _sprint01 = sprint01;
            // Toe-off and touchdown moments (share of the swing the foot spends peeling off or setting down travelling
            // with the ground): long enough walking, brief running, where the body covers a lot of ground meanwhile.
            _edge = Mathf.Lerp(0.1f, 0.035f, run01);
            Vector3 heel0, heel1;
            float swing0, swing1;
            GaitFoot(Frac(_phase), -1f, speed, T, sf, walk01, run01, sprint01, crouch, lift, strideSign, gaitRot, out l0, out w0, out heel0, out swing0);
            GaitFoot(Frac(_phase + 0.5f), 1f, speed, T, sf, walk01, run01, sprint01, crouch, lift, strideSign, gaitRot, out l1, out w1, out heel1, out swing1);
            // The gait owns the feet only when last frame's feet were exactly its feet (nothing planted, blended or
            // overridden them); otherwise the offset is re-seeded from where the feet really were, so the hand-over is seamless.
            float tol = 0.02f * s;
            bool own = p.Grounded && _gaitHasFinal && _lastLockW < 0.01f
                && Flat(_finalW[0] - _gaitW[0]).sqrMagnitude < tol * tol && Flat(_finalW[1] - _gaitW[1]).sqrMagnitude < tol * tol;
            _gaitActive = own;
            // Creatures update the plant weight first and re-anchor the gait on the planted feet the very frame it starts
            // to fall; waiting a frame (as people do) lets a slow-cadence start blend toward an unanchored foot.
            bool seedEarly = false;
            if (_creature)
            {
                _plantG = PlantToward(_plantG, 1f - SS(0.15f, 0.6f, sp), dt);
                seedEarly = SS(0.5f, 1f, _plantG) < 0.9999f;
            }
            // A foot about to touch down finishes its landing on the gait before the feet start to plant (stopping).
            _landing = swing0 > 0.7f || swing1 > 0.7f;
            AnchorStance(ref p, 0, ref l0, heel0, swing0, dt, seedEarly);
            AnchorStance(ref p, 1, ref l1, heel1, swing1, dt, seedEarly);
            b.LL = l0;
            b.LR = l1;

            // Stance phase of each foot (0 at heel strike, 1 at toe-off), or -1 while it is in the air.
            float psiL = Frac(_phase), psiR = Frac(_phase + 0.5f);
            float uL = psiL < sf ? psiL / sf : -1f, uR = psiR < sf ? psiR / sf : -1f;
            // Nothing may lift the pelvis past what the planted legs reach (a nearly straight knee): feet would skate.
            float reach = Mathf.Min(PelvisLimit(ref b.LL, w0, b.Hips, -1f, shift, 4f), PelvisLimit(ref b.LR, w1, b.Hips, 1f, shift, 4f));
            _reach = reach - 0.3f * s * crouch;
            // Walking: the pelvis rides as high as the stance legs allow with the knee bend walkers show at that moment
            // (nearly straight at heel strike, 15 to 20 degrees taking the weight, straight again at mid-stance). That
            // gives the natural 4 cm rise and fall instead of a crouch.
            float kLoad = 11f + 6f * SS(0.6f, 1.6f, sp);
            float walkY = Mathf.Min(_h0 - 0.005f * s, Mathf.Min(PelvisLimit(ref b.LL, w0, b.Hips, -1f, shift, WalkKnee(uL, kLoad)),
                PelvisLimit(ref b.LR, w1, b.Hips, 1f, shift, WalkKnee(uR, kLoad))));
            // Running: the pelvis sinks a little through each stance (the knee bends under load) and flies on a ballistic
            // arc between stances, highest at mid-flight.
            float runY;
            float yContact = _h0 - (0.046f + 0.01f * sprint01 + 0.01f * surge) * s;
            float dip = (0.02f + 0.006f * sprint01) * s;
            if (uL >= 0f) runY = yContact - dip * Mathf.Sin(Mathf.PI * uL);
            else if (uR >= 0f) runY = yContact - dip * Mathf.Sin(Mathf.PI * uR);
            else
            {
                float span = Mathf.Max(0.001f, 0.5f - sf);
                float x = Mathf.Clamp01((psiL < 0.5f ? psiL - sf : psiL - 0.5f - sf) / span);
                float tf = span * T;
                float rise = Mathf.Min(0.5f * 9.81f * tf * tf * 0.25f, 0.045f * s);
                runY = yContact + rise * 4f * x * (1f - x);
            }
            // A running leg lands with the knee already bent about 16 degrees, so the pelvis comes down to meet it.
            float landCap = Mathf.Min(PelvisLimit(ref b.LL, uL < 0f && swing0 > 0.5f ? w0 : 0f, b.Hips, -1f, shift, 16f),
                PelvisLimit(ref b.LR, uR < 0f && swing1 > 0.5f ? w1 : 0f, b.Hips, 1f, shift, 16f));
            float pelvisTarget = Mathf.Min(Mathf.Lerp(walkY, Mathf.Min(runY, landCap), run01), reach);
            pelvisTarget -= 0.3f * s * crouch + 0.12f * s * _land + 0.004f * s * Mathf.Sin(_idleT * 1.8f) * idleSway;
            if (_float)
            {
                // Nothing stands on the legs: the pelvis stays up (Hover adds the float and the bob).
                pelvisTarget = _h0 - 0.01f * s - 0.12f * s * _land;
                _reach = 1e6f;
            }
            if (!_hasOut) _pelvisY = pelvisTarget;
            // Barely filtered: the gait's own rise and fall is already smooth, and a lag would leave the landing leg short.
            _pelvisY = Mathf.Lerp(_pelvisY, pelvisTarget, QMath.Damp(140f, dt));
            b.Pelvis = new Vector3(0f, _pelvisY, 0f) + shift;

            // ---- torso: upright walking, a forward lean growing with running speed (about 5 degrees jogging, 9 sprinting)
            float accelLean = Mathf.Clamp(_fwdAcc * 1.6f, -8f, 10f) * walk01;
            float lean = (1.5f + 6f * run01 + 6f * sprint01) * walk01 + 24f * crouch + p.Panic * 5f + accelLean + 40f * surge;
            if (_back) lean -= 4f * walk01;
            if (_creature) lean += _hunch + (_float ? 6f * walk01 : 0f);
            _lean = Mathf.Lerp(_lean, lean, QMath.Damp(8f, dt));
            // The thorax turns against the pelvis, so the shoulders swing with the arms.
            float counter = -(pelvisYaw - _gaitYaw * hipFollow) * Mathf.Lerp(1.8f, 2.4f, run01);
            float faceFwd = -_gaitYaw * hipFollow; // shoulders keep facing where the character looks
            float breathe = Mathf.Sin(_idleT * 1.6f) * 1.3f * idleSway;
            b.Spine = QMath.AX(_lean * 0.55f) * QMath.AY((counter + faceFwd) * 0.45f) * QMath.AZ(-pelvisRoll * 0.5f);
            b.Chest = QMath.AX(_lean * 0.45f + breathe) * QMath.AY((counter + faceFwd) * 0.55f) * QMath.AZ(-pelvisRoll * 0.3f - turnLean * 0.4f);
            float headPitch = -_lean * Mathf.Lerp(0.75f, 0.85f, surge) - 2f * walk01;
            b.Neck = QMath.AX(headPitch * 0.35f);
            b.Head = QMath.AX(headPitch * 0.65f) * QMath.AY(-(pelvisYaw + counter + faceFwd) * 0.6f);

            // ---- arms: swing opposite to the legs (right arm forward with the left foot). Walking: about 20 degrees each
            // way with relaxed elbows (12 behind, 30 in front). Running: about 90 degree elbows pumping front to back,
            // mostly behind the body, closing as the hand comes up and opening as it drives back.
            float armAmp = (Mathf.Lerp(12f + 10f * SS(0.5f, 1.6f, sp), 32f, run01) + 14f * sprint01 + 10f * surge) * walk01 * (1f - 0.6f * crouch);
            float abd = 7f + 4f * run01 + Mathf.Max(0f, Layout.Bulk - 1f) * 14f + (Layout.Female ? 2f : 0f);
            if (_creature)
            {
                abd += _armSpread;
                if (_float) armAmp *= 0.3f;
            }
            // Running arms lead the legs a little: the hand peaks forward as the opposite knee drives through.
            float c = Mathf.Cos(ph + 1.2f * run01) * strideSign;
            float bias = -2f * walk01 * (1f - run01) - 12f * run01 + 6f * crouch;
            float swingR = armAmp * c + bias;
            float swingL = -armAmp * c + bias;
            float frontR = Mathf.Max(0f, c), frontL = Mathf.Max(0f, -c);
            float elbowR = Mathf.Lerp(10f + 12f * frontR, 88f + (14f + 10f * sprint01) * frontR - (12f + 10f * sprint01) * frontL, run01) + 20f * crouch;
            float elbowL = Mathf.Lerp(10f + 12f * frontL, 88f + (14f + 10f * sprint01) * frontL - (12f + 10f * sprint01) * frontR, run01) + 20f * crouch;
            float idleArm = Mathf.Sin(_idleT * 1.6f + 0.6f) * 1.2f * idleSway;
            b.AL = FkArm(ref b, -1, swingL + idleArm, abd, elbowL, 10f * run01, QMath.AX(-10f));
            b.AR = FkArm(ref b, 1, swingR + idleArm, abd, elbowR, 10f * run01, QMath.AX(-10f));
            // Planting follows speed but changes over a fifth of a second at least, so the hand-over between planted
            // feet and the gait (and the fighting stance's foot blend) never snaps in a single frame.
            if (!_creature) _plantG = PlantToward(_plantG, 1f - SS(0.15f, 0.6f, sp), dt);
            b.Plant = _plantG;
            b.Urgency = 0f;

            // ---- airborne: one knee up while rising, legs reach down while falling, arms out for balance
            if (!p.Grounded)
            {
                float rise = Mathf.Clamp01(p.VerticalVelocity / 4f);
                float fall = Mathf.Clamp01(-p.VerticalVelocity / 6f);
                Body air = b;
                air.Pelvis = new Vector3(0f, _h0, 0f);
                Vector3 kneeUp = S(-0.1f, 0.5f, 0.22f), backLeg = S(0.1f, 0.12f, -0.18f), reachDown = S(0.11f, 0.06f, 0.02f);
                air.LL.Ankle = Vector3.Lerp(kneeUp, S(-0.11f, 0.08f, 0.06f), fall);
                air.LR.Ankle = Vector3.Lerp(backLeg, reachDown, fall);
                air.LL.KneeDir = air.LR.KneeDir = Vector3.forward;
                air.LL.Foot = QMath.AX(25f - 35f * fall);
                air.LR.Foot = QMath.AX(30f - 20f * fall);
                air.Spine = QMath.AX(6f - 8f * rise);
                air.AL = FkArm(ref air, -1, 30f * rise - 10f, 34f + 14f * fall, 40f, 0f, QMath.AX(-10f));
                air.AR = FkArm(ref air, 1, -20f * rise - 10f, 34f + 14f * fall, 40f, 0f, QMath.AX(-10f));
                air.Plant = 0f;
                Blend(ref b, ref air, SS(0.02f, 0.18f, _airT));
            }
        }

        /// <summary>Plant weight toward its target over a fifth of a second at least; it never starts to rise while a foot
        /// is in the last part of its swing, so a stop never pins a foot that is still setting down.</summary>
        private float PlantToward(float plant, float target, float dt)
        {
            if (target > plant && _landing && plant < 0.5f) return plant;
            return Mathf.MoveTowards(plant, target, dt * 12f);
        }

        /// <summary>
        /// Knee bend in degrees of a walking stance leg at stance phase u, from published gait data: about 5 at heel
        /// strike, 15 to 20 while taking the weight, 5 at mid-stance and a little more as the heel lifts. Late in stance
        /// the other leg carries the body, so only the reach matters (4).
        /// </summary>
        private static float WalkKnee(float u, float load)
        {
            if (u < 0f) return 4f;
            if (u < 0.2f) return Mathf.Lerp(4.5f, load, E3(u / 0.2f));
            if (u < 0.52f) return Mathf.Lerp(load, 5f, E3((u - 0.2f) / 0.32f));
            if (u < 0.8f) return Mathf.Lerp(5f, 9f, E3((u - 0.52f) / 0.28f));
            return Mathf.Lerp(9f, 4f, E3((u - 0.8f) / 0.12f));
        }

        /// <summary>
        /// Thigh angle (from vertical, forward positive), knee bend and ankle angle (dorsiflexion positive) of the swing
        /// leg at swing phase u, from published walking and running curves: the knee folds early (about 60 degrees
        /// walking, 100 jogging, 125 sprinting) while the thigh drives forward, then the leg reaches out straight.
        /// </summary>
        private void SwingAngles(float u, out float thigh, out float knee, out float ankle)
        {
            float run = _run01, sprint = _sprint01, amp = Mathf.Lerp(0.6f, 1f, _fkAmp);
            float thW = u < 0.65f ? Mathf.Lerp(-8f, 21f, E3(u / 0.65f)) : Mathf.Lerp(21f, 19f, E3((u - 0.65f) / 0.35f));
            float knW = u < 0.28f ? Mathf.Lerp(38f, 63f, EOut(u / 0.28f)) : 4f + 59f * Mathf.Pow(1f - (u - 0.28f) / 0.72f, 1.3f);
            float anW = u < 0.45f ? Mathf.Lerp(-16f, 0f, E3(u / 0.45f)) : Mathf.Lerp(0f, 3f, E3((u - 0.45f) / 0.35f));
            float thTo = -22f - 4f * sprint, thPk = 40f + 15f * sprint + 13f * _surge, thLand = 29f + 4f * sprint;
            float knPk = 98f + 24f * sprint + 12f * _surge, uKn = 0.38f - 0.06f * sprint;
            float thR = u < 0.74f ? Mathf.Lerp(thTo, thPk, E3(u / 0.74f)) : Mathf.Lerp(thPk, thLand, E3((u - 0.74f) / 0.26f));
            float knR = u < uKn ? Mathf.Lerp(22f, knPk, E3(u / uKn)) : (u < 0.85f ? Mathf.Lerp(knPk, 28f, E3((u - uKn) / (0.85f - uKn))) : Mathf.Lerp(28f, 18f, (u - 0.85f) / 0.15f));
            float anR = u < 0.5f ? Mathf.Lerp(-28f, -5f, E3(u / 0.5f)) : Mathf.Lerp(-5f, 5f, E3((u - 0.5f) / 0.35f));
            thigh = Mathf.Lerp(thW * amp, thR, run);
            knee = Mathf.Lerp(knW * Mathf.Lerp(0.8f, 1f, _fkAmp), knR, run);
            ankle = Mathf.Lerp(anW, anR, run);
        }

        /// <summary>Ankle position (along the stride z, height y) for a foot whose heel touches the ground at zHeel,
        /// pitched by pitch degrees: positive rocks back on the heel (toe up), negative rolls onto the ball (heel up).</summary>
        private void AnklePivot(float zHeel, float pitch, out float z, out float y)
        {
            float a = Mathf.Abs(pitch) * Mathf.Deg2Rad;
            float c = Mathf.Cos(a), sn = Mathf.Sin(a);
            if (pitch >= 0f)
            {
                z = zHeel + _heel * c - _ankleH * sn;
                y = _heel * sn + _ankleH * c;
            }
            else
            {
                z = zHeel + _heel + _ball - _ball * c + _ankleH * sn;
                y = _ball * sn + _ankleH * c;
            }
        }

        private void GaitFoot(float psi, float side, float speed, float T, float sf, float walk01, float run01, float sprint01, float crouch, float lift, float strideSign, Quaternion gaitRot, out Leg leg, out float stanceW, out Vector3 heelRoot, out float swingU)
        {
            float s = _s;
            float zHeelStance = 0f;
            float D = speed * T * sf;
            // The heel lands a little over a third of the contact distance ahead of the hip (not a long reach out in front,
            // which is what made the old walk crouch), and the foot leaves the ground well behind.
            float A = D * Mathf.Lerp(0.4f, 0.36f, run01);
            float halfW = (Mathf.Lerp(0.092f, 0.07f, run01) + 0.035f * crouch) * s * (1f - 0.25f * walk01 * (1f - run01)) * (Layout.Female ? 0.85f : 1f);
            halfW = Mathf.Max(halfW, 0.055f * s);
            // Backpedals and sidesteps barely roll heel to toe (a planted foot can point well away from a sidestep's path).
            float fwd = (strideSign > 0f ? 1f : 0.25f) * (1f - 0.6f * Mathf.Abs(_dir.x) * walk01);
            float hs = Mathf.Lerp(16f, 8f, run01) * walk01 * fwd;
            float to = Mathf.Lerp(58f, 50f, run01) * walk01 * fwd;
            if (strideSign < 0f) { float tmp = hs; hs = -to * 0.5f; to = -tmp; }
            // Foot flat soon after heel strike; the heel lifts from about mid-stance and the foot rolls over the ball.
            float flatEnd = Mathf.Lerp(0.16f, 0.12f, run01), heelOff = Mathf.Lerp(0.52f, 0.45f, run01);
            float z, y, pitch;
            float heelBase = -_heel; // a flat foot centred under the hip puts the heel 7.5 cm behind the ankle
            if (psi < sf)
            {
                float u = psi / sf;
                float zHeel = heelBase + A - D * u;
                zHeelStance = zHeel;
                pitch = u < flatEnd ? hs * (1f - E3(u / flatEnd)) : (u > heelOff ? -to * E3((u - heelOff) / (1f - heelOff)) : 0f);
                AnklePivot(zHeel, pitch, out z, out y);
                // Fully weighted from heel strike on (the end of the swing already eased the constraint in).
                stanceW = 1f;
                swingU = -1f;
            }
            else
            {
                float u = (psi - sf) / (1f - sf);
                swingU = u;
                float z0, y0, z1, y1;
                AnklePivot(heelBase + A - D, -to, out z0, out y0);
                AnklePivot(heelBase + A, hs, out z1, out y1);
                float k = Mathf.Lerp(0.85f, 0.65f, run01);
                // The foot peels off and sets down travelling with the ground (no skate at toe-off or contact); in
                // between a Hermite swing kicks back after toe-off and reaches past the landing spot before pawing back.
                float m = -speed * (1f - sf) * T;
                float edge = _edge;
                float zB = z0 + m * edge, zA = z1 - m * edge;
                if (u < edge) z = z0 + m * u;
                else if (u > 1f - edge) z = zA + m * (u - (1f - edge));
                else
                {
                    // Running eases the in-air tangents: a full ground-speed tangent swings the foot far behind after
                    // toe-off and far ahead before landing at speed.
                    float v = (u - edge) / (1f - 2f * edge), mv = m * (1f - 2f * edge) * Mathf.Lerp(0.6f, 0.35f, run01);
                    float v2 = v * v, v3 = v2 * v;
                    z = (2f * v3 - 3f * v2 + 1f) * zB + (v3 - 2f * v2 + v) * mv + (-2f * v3 + 3f * v2) * zA + (v3 - v2) * mv;
                }
                float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(u), k));
                y = Mathf.Lerp(y0, y1, u) + lift * Mathf.Pow(Mathf.Max(0f, arc), 1.3f);
                pitch = Mathf.Lerp(-to, hs, E3(Mathf.Clamp01(u * 1.35f))) + 8f * Mathf.Sin(Mathf.PI * u) * walk01 * (1f - run01);
                // Mid-swing the leg follows the hip and knee angles of real walkers and runners (forward kinematics from the
                // hip); the toe-off and landing moments keep the exact path above, so contact never slips.
                float fk = _fkSwing * SS(edge * 0.8f, edge * 2f, u) * (1f - SS(0.7f, 1f - edge, u));
                if (fk > 0.001f)
                {
                    float th, kn, an;
                    SwingAngles(u, out th, out kn, out an);
                    float a1 = th * Mathf.Deg2Rad, a2 = (th - kn) * Mathf.Deg2Rad;
                    float zF = _l1 * Mathf.Sin(a1) + _l2 * Mathf.Sin(a2);
                    float yF = _hipY - _l1 * Mathf.Cos(a1) - _l2 * Mathf.Cos(a2);
                    z = Mathf.Lerp(z, zF, fk);
                    y = Mathf.Lerp(y, yF, fk);
                    pitch = Mathf.Lerp(pitch, th - kn + an, fk);
                }
                // Ground clearance: neither the toe nor the heel scuffs the ground mid-swing.
                float pr = pitch * Mathf.Deg2Rad;
                float low = y - _ankleH * Mathf.Cos(pr) + Mathf.Min((_ball + 0.03f * s) * Mathf.Sin(pr), -_heel * Mathf.Sin(pr));
                // Clear of the ground until the touchdown moment, when the foot already travels with the ground.
                float clear = 0.025f * s * SS(0f, edge * 2f, u) * (1f - SS(1f - edge * 1.5f, 1f, u));
                if (low < clear) y += clear - low;
                // The toe is still pushing off for the first moments of the swing, and the leg must reach the ground just
                // before the heel strikes: keep the foot reachable at both ends.
                stanceW = Mathf.Max(1f - SS(edge * 0.5f, edge * 1.5f, u), SS(Mathf.Lerp(0.75f, 0.8f, run01), 1f - edge * 0.3f, u));
            }
            // Idle: weight on one leg with the other a touch forward.
            float idle = 1f - walk01;
            float idleZ = (side < 0f ? 0.03f : -0.02f) * s * idle;
            float idleX = 0.012f * s * idle;
            // Step width is measured in the gait frame; travel runs exactly against the velocity so planted feet stay put.
            Vector3 along = strideSign > 0f ? _dir : -_dir;
            Vector3 local = new Vector3(side * (halfW + idleX), y, idleZ);
            leg.Ankle = gaitRot * local + along * (z * strideSign);
            leg.Foot = gaitRot * QMath.AY(side * 6f) * QMath.AX(-pitch);
            leg.KneeDir = gaitRot * QMath.AY(side * 8f) * Vector3.forward;
            leg.Lift = 0f;
            // Anchor point: where the ankle would be with the foot flat. Unlike the heel it does not jump when the stride
            // direction flips at a standstill (a reversal), and while walking it moves exactly with the heel.
            heelRoot = gaitRot * new Vector3(side * (halfW + idleX), 0f, idleZ) + along * ((zHeelStance + _heel) * strideSign);
        }

        /// <summary>Keeps a foot fixed in the world while it touches the ground (stance, plus the toe-off and landing
        /// moments of the swing), heading included, and fades the offsets out while it is in the air.</summary>
        private void AnchorStance(ref PoseInput p, int i, ref Leg leg, Vector3 heelRoot, float swingU, float dt, bool seedEarly)
        {
            Quaternion rootRot = QMath.AY(p.RootYaw);
            Vector3 root = p.RootPosition;
            Vector3 formulaW = root + rootRot * leg.Ankle;
            float formulaYaw = p.RootYaw + YawOf(leg.Foot);
            // 0 stance (the flat-foot ankle point stays put), 1 toe-off or landing (the ball or the heel stays put while
            // the foot pitches about it), 2 in the air.
            int mode = swingU < 0f ? 0 : (swingU < _edge || swingU > 1f - _edge ? 1 : 2);
            bool acting = p.AttackT >= 0f || p.DodgeT >= 0f || p.StaggerT >= 0f || p.SwingT >= 0f;
            // Seed from where the real foot was last frame. While the feet are fully planted and nothing else is going
            // on, the gait stays as it is instead: it is then only where the planted feet step to when the body turns.
            bool seed = !_gaitActive && _gaitHasFinal && p.Grounded && (_lastLockW < 0.99f || acting || seedEarly);

            // Heading first, so every contact point below is measured on the foot as it will finally be turned.
            if (!_gaitActive) _gaitYawCorr[i] = seed ? Mathf.DeltaAngle(formulaYaw, _finalYaw[i]) : 0f;
            else if (mode != 2)
            {
                if (_gaitMode[i] == 2) _gaitYawAnchor[i] = formulaYaw + _gaitYawCorr[i];
                _gaitYawCorr[i] = Mathf.DeltaAngle(formulaYaw, _gaitYawAnchor[i]);
            }
            else
            {
                if (_gaitMode[i] != 2) _gaitYawCorr0[i] = _gaitYawCorr[i];
                float fadeYaw = 1f - E3((swingU - _gaitSwingU0[i]) / Mathf.Max(0.05f, 0.85f - _gaitSwingU0[i]));
                _gaitYawCorr[i] = Mathf.MoveTowards(_gaitYawCorr[i], _gaitYawCorr0[i] * fadeYaw, 400f * dt);
            }
            // Past what a hip and knee turn comfortably, the planted foot pivots with the body after all.
            _gaitYawCorr[i] = Mathf.Clamp(_gaitYawCorr[i], -50f, 50f);
            if (!_gaitActive || mode != 2) _gaitYawAnchor[i] = formulaYaw + _gaitYawCorr[i];
            Quaternion swivel = QMath.AY(_gaitYawCorr[i]);
            leg.Foot = swivel * leg.Foot;
            leg.KneeDir = swivel * leg.KneeDir;

            Vector3 contact = swingU < 0.5f ? new Vector3(0f, -_ankleH, _ball) : new Vector3(0f, -_ankleH, -_heel);
            Vector3 pointW = mode == 0 ? Flat(root + rootRot * heelRoot) : Flat(root + rootRot * (leg.Ankle + leg.Foot * contact));
            if (!_gaitActive)
            {
                if (seed)
                {
                    // Match the part of the sole that is on the ground: the gait foot's lower end (ball when its heel is
                    // up, heel when its toes are up), so a flat planted foot peeling into the gait rolls on its ball.
                    Vector3 heelO = new Vector3(0f, -_ankleH, -_heel), ballO = new Vector3(0f, -_ankleH, _ball);
                    bool ballDown = (leg.Foot * ballO).y <= (leg.Foot * heelO).y;
                    Vector3 o = ballDown ? ballO : heelO;
                    _gaitCorr[i] = Flat(_finalW[i] + _finalRot[i] * o - (root + rootRot * (leg.Ankle + leg.Foot * o)));
                }
                else _gaitCorr[i] = Vector3.zero;
                if (mode != 2) _gaitAnchor[i] = pointW + _gaitCorr[i];
                else
                {
                    _gaitCorr0[i] = _gaitCorr[i];
                    _gaitYawCorr0[i] = _gaitYawCorr[i];
                    _gaitSwingU0[i] = swingU;
                }
            }
            else if (mode != 2)
            {
                // Contact starts or changes kind. Normally the gait carries on smoothly (a heel strike, the roll onto the
                // toes); if the gait itself jumped this frame (a sharp turn or reversal) the foot stays where it was.
                if (_gaitMode[i] != mode && _creature && _gaitMode[i] != 2)
                {
                    // Creatures: when the foot goes from flat to rolling on its ball (or from its heel to flat), the part
                    // of the sole that was on the ground stays exactly where it was. Braking moves the gait's foot by a
                    // few centimetres a frame, which the carried-over offset below would turn into a visible slip on a giant.
                    Vector3 heelO = new Vector3(0f, -_ankleH, -_heel), ballO = new Vector3(0f, -_ankleH, _ball);
                    Vector3 o = (_finalRot[i] * ballO).y <= (_finalRot[i] * heelO).y ? ballO : heelO;
                    _gaitAnchor[i] = pointW + Flat(_finalW[i] + _finalRot[i] * o - (root + rootRot * (leg.Ankle + leg.Foot * o)));
                }
                else if (_gaitMode[i] == 2)
                {
                    // A foot setting down from the air simply carries on: at speed its path moves a long way each frame.
                    _gaitAnchor[i] = pointW + _gaitCorr[i];
                }
                else if (_gaitMode[i] != mode)
                {
                    Vector3 keep = Flat(_gaitW[i] - formulaW);
                    float jump = 0.03f * _s;
                    _gaitAnchor[i] = pointW + ((keep - _gaitCorr[i]).sqrMagnitude > jump * jump ? keep : _gaitCorr[i]);
                }
                _gaitCorr[i] = _gaitAnchor[i] - pointW;
            }
            else
            {
                if (_gaitMode[i] != 2)
                {
                    _gaitCorr0[i] = _gaitCorr[i];
                    _gaitSwingU0[i] = swingU;
                }
                float u0 = _gaitSwingU0[i];
                Vector3 want = _gaitCorr0[i] * (1f - E3((swingU - u0) / Mathf.Max(0.05f, 0.85f - u0)));
                // The swing foot drifts back onto the gait at a believable speed, even if little swing was left.
                Vector3 dc = want - _gaitCorr[i];
                float maxStep = 3f * _s * dt;
                _gaitCorr[i] += dc.sqrMagnitude > maxStep * maxStep ? Nrm(dc) * maxStep : dc;
            }
            // A sudden reversal can ask for more than a leg can give: past this the foot slides instead.
            float maxC = 0.42f * _s;
            if (_gaitCorr[i].sqrMagnitude > maxC * maxC)
            {
                _gaitCorr[i] = Nrm(_gaitCorr[i]) * maxC;
                if (mode != 2) _gaitAnchor[i] = pointW + _gaitCorr[i];
            }
            _gaitMode[i] = mode;
            leg.Ankle += QMath.AY(-p.RootYaw) * _gaitCorr[i];
            _gaitW[i] = formulaW + _gaitCorr[i];
        }

        /// <summary>Highest pelvis that still lets a planted foot reach the ground with the knee bent kneeDeg degrees.</summary>
        private float PelvisLimit(ref Leg leg, float weight, Quaternion hips, float side, Vector3 shift, float kneeDeg)
        {
            if (weight <= 0.001f) return float.MaxValue;
            Vector3 thighOff = hips * Layout.Local[side < 0f ? RigLayout.ThighL : RigLayout.ThighR];
            Vector3 joint = shift + thighOff;
            float dx = leg.Ankle.x - joint.x, dz = leg.Ankle.z - joint.z;
            float h2 = dx * dx + dz * dz;
            float maxLen = Mathf.Sqrt(_l1 * _l1 + _l2 * _l2 + 2f * _l1 * _l2 * Mathf.Cos(kneeDeg * Mathf.Deg2Rad));
            float vert = Mathf.Sqrt(Mathf.Max(0.0001f, maxLen * maxLen - h2));
            float limit = Mathf.Max(leg.Ankle.y + vert - thighOff.y, _h0 * 0.55f);
            return Mathf.Lerp(_h0, limit, weight);
        }

        /// <summary>Fighting stance (guard = false) or a tight block (guard = true).</summary>
        private void Stance(ref PoseInput p, ref Body b, float w, bool guard)
        {
            Body t = b;
            float drop = (guard ? 0.06f : 0.04f) * _s;
            t.Pelvis = new Vector3(b.Pelvis.x, b.Pelvis.y - drop, b.Pelvis.z);
            float yaw = guard ? 14f : 10f;
            t.Hips = QMath.AY(yaw) * b.Hips;
            t.Spine = QMath.AX(guard ? 8f : 5f) * QMath.AY(-yaw * 0.4f) * b.Spine;
            t.Chest = QMath.AX(guard ? 4f : 2f) * QMath.AY(-yaw * 0.35f) * b.Chest;
            t.Head = QMath.AX(guard ? 6f : 2f) * QMath.AY(-yaw * 0.3f) * b.Head;
            bool unarmed = !p.Blade && !p.TwoHanded && (p.Hold == HoldType.None || p.Hold == HoldType.Melee);
            bool gun = p.Hold == HoldType.Pistol || p.Hold == HoldType.Rifle || p.Hold == HoldType.Heavy;
            if (guard && p.EnergyGuard)
            {
                // Left palm thrust forward to hold the energy barrier, the other hand ready.
                Arm l = IkArm(ref t, -1, S(-0.06f, 1.4f, 0.47f), new Vector3(-0.8f, -0.6f, -0.2f));
                // Fingers up, palm (+X for the left hand) facing forward.
                l.Blade = Vector3.up;
                l.Edge = Vector3.right;
                l.HandW = 1f;
                t.AL = l;
                if (unarmed) t.AR = IkArm(ref t, 1, S(0.14f, 1.4f, 0.22f), new Vector3(0.5f, -1f, -0.3f));
            }
            else if (p.TwoHanded)
            {
                Vector3 blade = guard ? new Vector3(-0.86f, 0.42f, 0.28f) : new Vector3(-0.3f, 0.55f, 0.78f);
                t.AR = IkArm(ref t, 1, guard ? S(0.18f, 1.3f, 0.3f) : S(0.13f, 1.1f, 0.3f), new Vector3(0.8f, -0.6f, -0.2f), blade, Vector3.up, 1f);
            }
            else if (p.Blade)
            {
                Vector3 blade = guard ? new Vector3(-0.58f, 0.74f, 0.34f) : new Vector3(-0.12f, 0.5f, 0.86f);
                t.AR = IkArm(ref t, 1, guard ? S(0.14f, 1.3f, 0.32f) : S(0.2f, 1.14f, 0.3f), new Vector3(0.7f, -0.7f, -0.2f), blade, guard ? new Vector3(0.3f, 0f, 1f) : Vector3.up, 1f);
                t.AL = IkArm(ref t, -1, guard ? S(-0.13f, 1.22f, 0.22f) : S(-0.18f, 1.16f, 0.12f), new Vector3(-0.7f, -0.7f, -0.3f));
            }
            else if (unarmed)
            {
                if (guard)
                {
                    t.AR = IkArm(ref t, 1, S(0.1f, 1.5f, 0.21f), new Vector3(0.3f, -1f, -0.1f));
                    t.AL = IkArm(ref t, -1, S(-0.09f, 1.52f, 0.23f), new Vector3(-0.3f, -1f, -0.1f));
                }
                else
                {
                    t.AR = IkArm(ref t, 1, S(0.15f, 1.37f, 0.23f), new Vector3(0.4f, -1f, -0.25f));
                    t.AL = IkArm(ref t, -1, S(-0.12f, 1.42f, 0.3f), new Vector3(-0.4f, -1f, -0.25f));
                }
                t.AR.Wrist = t.AL.Wrist = QMath.AX(-15f);
            }
            else if (gun && !p.Aiming && guard)
            {
                t.AL = IkArm(ref t, -1, S(-0.06f, 1.42f, 0.44f), new Vector3(-0.8f, -0.6f, -0.2f));
            }
            // Staggered feet: lead foot forward, rear foot back, a little wider. They only become the pose's feet once the
            // feet are fully planted, so the planted feet step into the stance instead of being dragged into it; while
            // moving at all the gait keeps the feet.
            float moving = b.Plant >= 0.999f ? 0f : 1f;
            t.LL.Ankle = Vector3.Lerp(S(-0.13f, 0f, 0.12f) + new Vector3(0f, _ankleH, 0f), b.LL.Ankle, moving);
            t.LR.Ankle = Vector3.Lerp(S(0.13f, 0f, -0.1f) + new Vector3(0f, _ankleH, 0f), b.LR.Ankle, moving);
            t.LL.Foot = QMath.Slerp(QMath.AY(-14f), b.LL.Foot, moving);
            t.LR.Foot = QMath.Slerp(QMath.AY(28f), b.LR.Foot, moving);
            t.Urgency = Mathf.Max(b.Urgency, 0.6f);
            Blend(ref b, ref t, w);
        }

        private void GuardRecoil(ref Body b, float k)
        {
            float e = E3(k);
            b.Pelvis.y -= 0.03f * _s * e;
            b.Chest = QMath.AX(-8f * e) * b.Chest;
            b.Head = QMath.AX(-6f * e) * b.Head;
            b.AL.Hand += new Vector3(0f, -0.02f, -0.07f) * _s * e;
            b.AR.Hand += new Vector3(0f, -0.02f, -0.07f) * _s * e;
        }

        // ================================================================== attacks

        private static Key K(float yaw, float twist, float pitch, float drop, float step, float back, Vector3 r, Vector3 rPole, Vector3 rBlade, Vector3 rEdge)
        {
            Key k = new Key();
            k.Yaw = yaw; k.Twist = twist; k.Pitch = pitch; k.Drop = drop; k.Step = step; k.Back = back;
            k.R = r; k.RPole = rPole; k.RBlade = rBlade; k.REdge = rEdge;
            return k;
        }

        private static Key KL(Key k, Vector3 l, Vector3 lPole)
        {
            k.L = l;
            k.LPole = lPole;
            k.HasL = true;
            return k;
        }

        /// <summary>Adds the mid-swing pass-through (hand and blade) to a strike key.</summary>
        private static Key Mid(Key k, Vector3 hand, Vector3 blade)
        {
            k.RMid = hand;
            k.RBladeMid = blade;
            k.HasMid = true;
            return k;
        }

        private static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        /// <summary>Wind-up, strike and follow-through keys (unit body height, right-handed, facing +Z; positive yaw turns right).</summary>
        private static void Keys(MeleeAnim anim, out Key k1, out Key k2, out Key k3)
        {
            Vector3 z0 = Vector3.zero;
            Vector3 guardL = V(-0.1f, 1.48f, 0.22f), guardLPole = V(-0.4f, -1f, -0.2f);
            switch (anim)
            {
                case MeleeAnim.Jab:
                    k1 = KL(K(8f, 6f, 4f, 0.03f, 0f, 0f, V(0.16f, 1.4f, 0.16f), V(0.4f, -1f, -0.4f), z0, z0), guardL, guardLPole);
                    k2 = KL(K(-10f, -14f, 6f, 0.03f, 0.08f, 0f, V(0.05f, 1.46f, 0.7f), V(1f, -0.6f, -0.4f), z0, z0), V(-0.12f, 1.48f, 0.18f), guardLPole);
                    k3 = KL(K(-6f, -6f, 4f, 0.03f, 0.06f, 0f, V(0.12f, 1.42f, 0.38f), V(0.5f, -1f, -0.3f), z0, z0), guardL, guardLPole);
                    return;
                case MeleeAnim.Cross:
                case MeleeAnim.Flurry:
                    k1 = KL(K(18f, 12f, 3f, 0.05f, 0f, 0f, V(0.17f, 1.42f, 0.12f), V(0.4f, -1f, -0.4f), z0, z0), V(-0.1f, 1.48f, 0.28f), guardLPole);
                    k2 = KL(K(-25f, -28f, 8f, 0.05f, 0.12f, 0.05f, V(0.02f, 1.45f, 0.72f), V(1f, -0.4f, -0.5f), z0, z0), V(-0.16f, 1.46f, 0.12f), guardLPole);
                    k3 = KL(K(-15f, -12f, 5f, 0.04f, 0.1f, 0.04f, V(0.1f, 1.42f, 0.4f), V(0.5f, -1f, -0.3f), z0, z0), guardL, guardLPole);
                    return;
                case MeleeAnim.Hook:
                    k1 = KL(K(15f, 20f, 4f, 0.05f, 0f, 0f, V(0.34f, 1.38f, 0.16f), V(1f, 0f, -0.5f), z0, z0), guardL, guardLPole);
                    k2 = KL(K(-22f, -38f, 6f, 0.05f, 0.05f, 0f, V(-0.14f, 1.44f, 0.42f), V(1f, 0.35f, -0.2f), z0, z0), V(-0.16f, 1.46f, 0.14f), guardLPole);
                    k3 = KL(K(-18f, -30f, 5f, 0.05f, 0.05f, 0f, V(-0.05f, 1.42f, 0.3f), V(1f, 0f, -0.3f), z0, z0), guardL, guardLPole);
                    return;
                case MeleeAnim.Uppercut:
                    k1 = KL(K(10f, 15f, 14f, 0.1f, 0f, 0f, V(0.2f, 1.1f, 0.18f), V(0.5f, -1f, -0.3f), z0, z0), guardL, guardLPole);
                    k2 = KL(K(-15f, -20f, -6f, -0.01f, 0.04f, 0f, V(0.05f, 1.66f, 0.36f), V(0.6f, -1f, 0f), z0, z0), V(-0.14f, 1.46f, 0.16f), guardLPole);
                    k3 = KL(K(-10f, -12f, -2f, 0.01f, 0.04f, 0f, V(0.1f, 1.55f, 0.3f), V(0.6f, -1f, -0.1f), z0, z0), guardL, guardLPole);
                    return;
                case MeleeAnim.ShieldBash:
                    k1 = KL(K(10f, 15f, -4f, 0.04f, 0f, 0f, V(0.2f, 1.3f, 0.05f), V(0.6f, -1f, -0.4f), z0, z0), V(-0.08f, 1.36f, 0.16f), V(-0.6f, -1f, -0.4f));
                    k2 = KL(K(-12f, -12f, 12f, 0.06f, 0.25f, 0f, V(0.1f, 1.4f, 0.54f), V(0.8f, -0.8f, -0.2f), z0, z0), V(-0.05f, 1.42f, 0.52f), V(-0.8f, -0.8f, -0.2f));
                    k3 = KL(K(-6f, -6f, 6f, 0.05f, 0.2f, 0f, V(0.15f, 1.35f, 0.3f), V(0.6f, -1f, -0.3f), z0, z0), V(-0.08f, 1.38f, 0.3f), V(-0.6f, -1f, -0.3f));
                    return;
                case MeleeAnim.SlashLeft:
                    k1 = K(-25f, -35f, 2f, 0.06f, 0f, 0f, V(-0.28f, 1.45f, 0.12f), V(0.3f, -0.6f, -0.8f), V(-0.65f, 0.5f, -0.55f), V(0.6f, 0f, 0.8f));
                    k2 = K(22f, 38f, 8f, 0.08f, 0.18f, 0f, V(0.48f, 1.2f, 0.32f), V(0.6f, -1f, -0.2f), V(0.85f, -0.15f, 0.5f), V(0.5f, 0f, -0.85f));
                    // Backhand: the arm extends in front while the blade sweeps across from the left.
                    k2 = Mid(k2, V(0.06f, 1.36f, 0.46f), V(-0.62f, 0.15f, 0.77f));
                    k3 = K(15f, 45f, 6f, 0.07f, 0.16f, 0f, V(0.5f, 1.12f, 0.1f), V(0.6f, -1f, -0.3f), V(0.85f, -0.4f, 0.1f), V(-0.1f, 0f, -1f));
                    return;
                case MeleeAnim.Stab:
                    k1 = K(15f, 20f, 2f, 0.05f, 0f, 0f, V(0.24f, 1.08f, -0.08f), V(0.6f, -0.4f, -0.7f), V(0f, 0.15f, 1f), V(0f, 1f, 0f));
                    k2 = K(-12f, -18f, 12f, 0.08f, 0.32f, 0f, V(0.06f, 1.28f, 0.66f), V(0.8f, -0.6f, -0.2f), V(-0.05f, 0f, 1f), V(0f, 1f, 0f));
                    k3 = K(-8f, -10f, 8f, 0.07f, 0.28f, 0f, V(0.12f, 1.22f, 0.45f), V(0.7f, -0.7f, -0.2f), V(0f, 0.1f, 1f), V(0f, 1f, 0f));
                    return;
                case MeleeAnim.Overhead:
                    k1 = K(10f, 12f, -14f, 0.02f, 0f, 0f, V(0.14f, 1.9f, -0.1f), V(0.7f, 0.4f, -0.2f), V(0.05f, 0.45f, -0.89f), V(0f, 1f, 0.3f));
                    k2 = K(-8f, -10f, 24f, 0.14f, 0.3f, 0f, V(0.06f, 1.06f, 0.55f), V(0.8f, -0.6f, -0.1f), V(0f, -0.8f, 0.6f), V(0f, -0.6f, -0.8f));
                    // Over the top: the hand comes forward at eye height with the blade still pointing up, then chops.
                    k2 = Mid(k2, V(0.1f, 1.68f, 0.42f), V(0.02f, 0.75f, 0.66f));
                    k3 = K(-6f, -6f, 18f, 0.12f, 0.28f, 0f, V(0.1f, 0.98f, 0.42f), V(0.8f, -0.6f, -0.2f), V(0f, -0.95f, 0.3f), V(0f, -0.3f, -0.95f));
                    return;
                case MeleeAnim.HomeRun:
                    k1 = K(40f, 55f, 2f, 0.08f, 0f, 0.05f, V(0.38f, 1.4f, -0.22f), V(0.8f, -0.3f, -0.5f), V(0.25f, 0.6f, -0.76f), V(-0.3f, 0.2f, 0.9f));
                    k2 = K(-35f, -50f, 6f, 0.08f, 0.25f, 0f, V(-0.22f, 1.25f, 0.42f), V(0.4f, -1f, -0.2f), V(-0.9f, 0.05f, 0.42f), V(-0.4f, 0f, -0.9f));
                    // Level swing: hands lead through the middle, the head of the weapon lags then whips across.
                    k2 = Mid(k2, V(0.12f, 1.3f, 0.42f), V(0.7f, 0.34f, 0.62f));
                    k3 = K(-40f, -70f, 4f, 0.07f, 0.22f, 0f, V(-0.3f, 1.35f, 0.05f), V(0.3f, -1f, -0.3f), V(-0.6f, 0.3f, -0.7f), V(0.3f, 0f, -0.95f));
                    return;
                case MeleeAnim.Thrust:
                    k1 = K(25f, 25f, 0f, 0.07f, 0f, 0f, V(0.26f, 1.22f, -0.12f), V(0.7f, -0.4f, -0.6f), V(0.05f, 0.08f, 1f), V(0f, 1f, 0f));
                    k2 = K(-15f, -15f, 14f, 0.12f, 0.48f, 0f, V(0.04f, 1.3f, 0.76f), V(0.8f, -0.6f, -0.2f), V(0f, 0f, 1f), V(0f, 1f, 0f));
                    k3 = K(-10f, -10f, 10f, 0.1f, 0.42f, 0f, V(0.1f, 1.26f, 0.55f), V(0.7f, -0.7f, -0.2f), V(0f, 0.05f, 1f), V(0f, 1f, 0f));
                    return;
                case MeleeAnim.Slam:
                    k1 = KL(K(6f, 8f, -18f, -0.03f, 0f, 0f, V(0.1f, 2.0f, -0.02f), V(0.8f, 0.3f, 0f), V(0f, 0.5f, -0.86f), V(0f, 0.9f, 0.4f)), V(-0.1f, 2.0f, -0.02f), V(-0.8f, 0.3f, 0f));
                    k2 = KL(K(-4f, -4f, 42f, 0.3f, 0.22f, 0f, V(0.04f, 0.72f, 0.58f), V(0.8f, -0.4f, -0.2f), V(0f, -0.88f, 0.45f), V(0f, -0.45f, -0.88f)), V(-0.06f, 0.72f, 0.58f), V(-0.8f, -0.4f, -0.2f));
                    k2 = Mid(k2, V(0.06f, 1.7f, 0.46f), V(0f, 0.69f, 0.72f));
                    k3 = KL(K(-4f, -4f, 36f, 0.26f, 0.2f, 0f, V(0.06f, 0.7f, 0.52f), V(0.8f, -0.5f, -0.2f), V(0f, -0.95f, 0.3f), V(0f, -0.3f, -0.95f)), V(-0.08f, 0.7f, 0.52f), V(-0.8f, -0.5f, -0.2f));
                    return;
                case MeleeAnim.SpinSlash:
                    // Positions are relative to the spinning pelvis (see Attack).
                    k1 = K(45f, 40f, 4f, 0.06f, 0f, 0f, V(0.46f, 1.32f, -0.05f), V(0.7f, -0.6f, -0.3f), V(0.65f, 0.3f, -0.7f), V(-0.2f, 0f, 1f));
                    k2 = K(-315f, -20f, 8f, 0.1f, 0f, 0f, V(0.6f, 1.3f, 0.14f), V(0.5f, -1f, 0f), V(0.96f, 0f, 0.25f), V(0.25f, 0f, -0.96f));
                    k3 = K(-330f, -30f, 6f, 0.08f, 0f, 0f, V(0.5f, 1.2f, 0.25f), V(0.6f, -1f, -0.2f), V(0.8f, -0.4f, 0.4f), V(0.4f, 0f, -0.9f));
                    return;
                default: // SlashRight
                    k1 = K(30f, 40f, 4f, 0.06f, 0f, 0f, V(0.45f, 1.38f, -0.02f), V(0.8f, -0.4f, -0.4f), V(0.55f, 0.45f, -0.7f), V(-0.6f, -0.1f, 0.8f));
                    k2 = K(-22f, -38f, 10f, 0.08f, 0.22f, 0f, V(-0.32f, 1.22f, 0.42f), V(0.5f, -1f, -0.2f), V(-0.8f, -0.2f, 0.55f), V(-0.5f, -0.1f, -0.85f));
                    // Forehand: the arm extends in front while the blade sweeps across from over the right shoulder.
                    k2 = Mid(k2, V(0.1f, 1.34f, 0.46f), V(0.62f, 0.15f, 0.77f));
                    k3 = K(-18f, -50f, 8f, 0.06f, 0.2f, 0f, V(-0.38f, 1.15f, 0.2f), V(0.4f, -1f, -0.3f), V(-0.85f, -0.35f, 0.2f), V(0.2f, 0f, -1f));
                    return;
            }
        }

        private static Key LerpKey(Key a, Key b, float t)
        {
            Key k;
            k.Yaw = Mathf.Lerp(a.Yaw, b.Yaw, t);
            k.Twist = Mathf.Lerp(a.Twist, b.Twist, t);
            k.Pitch = Mathf.Lerp(a.Pitch, b.Pitch, t);
            k.Drop = Mathf.Lerp(a.Drop, b.Drop, t);
            k.Step = Mathf.Lerp(a.Step, b.Step, t);
            k.Back = Mathf.Lerp(a.Back, b.Back, t);
            k.R = Vector3.Lerp(a.R, b.R, t);
            k.RPole = Vector3.Lerp(a.RPole, b.RPole, t);
            k.RBlade = Nrm(Vector3.Lerp(a.RBlade, b.RBlade, t));
            if (k.RBlade.sqrMagnitude < 0.5f) k.RBlade = t < 0.5f ? a.RBlade : b.RBlade;
            k.REdge = Vector3.Lerp(a.REdge, b.REdge, t);
            k.L = Vector3.Lerp(a.L, b.L, t);
            k.LPole = Vector3.Lerp(a.LPole, b.LPole, t);
            k.HasL = a.HasL || b.HasL;
            k.RMid = Vector3.zero;
            k.RBladeMid = Vector3.zero;
            k.HasMid = false;
            return k;
        }

        /// <summary>Wind-up to strike. With a mid pass-through the hand follows a curve through it (reached at t = 0.5)
        /// and the blade sweeps through the mid direction on great circles, its edge carried along the same arc.</summary>
        private static Key StrikeKey(Key a, Key b, float t)
        {
            Key k = LerpKey(a, b, t);
            if (!b.HasMid) return k;
            Vector3 c = 2f * b.RMid - 0.5f * (a.R + b.R);
            float it = 1f - t;
            Vector3 hand = it * it * a.R + 2f * it * t * c + t * t * b.R;
            if (k.HasL) k.L += hand - k.R; // the other hand stays on the grip
            k.R = hand;
            Sweep(Nrm(a.RBlade), Nrm(b.RBladeMid), Nrm(b.RBlade), a.REdge, b.REdge, t, out k.RBlade, out k.REdge);
            return k;
        }

        /// <summary>Axis and angle of the shortest rotation taking unit vector u to unit vector v.</summary>
        private static void ArcOf(Vector3 u, Vector3 v, out Vector3 axis, out float angle)
        {
            Vector3 n = Vector3.Cross(u, v);
            float s = n.magnitude, c = Vector3.Dot(u, v);
            angle = Mathf.Atan2(s, c);
            if (s > 1e-6f) { axis = n / s; return; }
            axis = Vector3.Cross(u, Vector3.right);
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(u, Vector3.up);
            axis = Nrm(axis);
        }

        /// <summary>v rotated by angle (radians) about a unit axis (Rodrigues).</summary>
        private static Vector3 Spin(Vector3 v, Vector3 axis, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return v * c + Vector3.Cross(axis, v) * s + axis * (Vector3.Dot(axis, v) * (1f - c));
        }

        private static void Sweep(Vector3 a, Vector3 m, Vector3 b, Vector3 edgeA, Vector3 edgeB, float t, out Vector3 dir, out Vector3 edge)
        {
            Vector3 n1, n2;
            float t1, t2;
            ArcOf(a, m, out n1, out t1);
            ArcOf(m, b, out n2, out t2);
            float u1 = Mathf.Clamp01(t * 2f) * t1, u2 = Mathf.Clamp01(t * 2f - 1f) * t2;
            dir = Spin(Spin(a, n1, u1), n2, u2);
            // Both key edges ride the same rotation (the end one carried back to the start first), then blend.
            Vector3 ea = Spin(Spin(edgeA, n1, u1), n2, u2);
            Vector3 eb = Spin(Spin(Spin(Spin(edgeB, n2, -t2), n1, -t1), n1, u1), n2, u2);
            edge = Vector3.Lerp(ea, eb, t);
        }

        private void Attack(ref PoseInput p, ref Body b, MeleeAnim anim, bool mirror, float t, float W, float A)
        {
            Key k1, k2, k3;
            Keys(anim, out k1, out k2, out k3);
            if (p.TwoHanded)
            {
                k1.Drop += 0.045f;
                k1.Pitch += 5f;
                k2.Drop += 0.035f;
                k3.Step *= 0.6f;
            }
            else if (p.PairedBlades)
            {
                k1.Drop += 0.025f;
                k2.Step *= 1.12f;
            }
            bool weapon = p.Blade || p.TwoHanded;
            float R0 = W + A;
            float follow = R0 + (1f - R0) * 0.45f;
            Key k;
            float w;
            float strikeU = -1f;
            if (t < W) { k = k1; w = E3(t / W); }
            else if (t < R0) { strikeU = (t - W) / A; k = StrikeKey(k1, k2, EOut(strikeU)); w = 1f; }
            else if (t < follow) { k = LerpKey(k2, k3, E3((t - R0) / (follow - R0))); w = 1f; }
            else { k = k3; w = 1f - E3((t - follow) / (1f - follow)); }

            Body a = b;
            float s = _s;
            if(p.TwoHanded && t>=R0)
            {
                float settle=Mathf.Sin(Mathf.Clamp01((t-R0)/Mathf.Max(.01f,1f-R0))*Mathf.PI);
                k.Drop+=.022f*settle;
                k.Pitch+=4f*settle;
            }
            bool spin = anim == MeleeAnim.SpinSlash;
            // Big creatures strike down at people-sized targets: from the strike on, the body pitches further and the
            // hands come down toward chest height (or the ground for overheads and slams) instead of passing overhead.
            float giant = _creature ? SS(1.2f, 2.4f, s) * SS(W, W + A * 0.6f, t) : 0f;
            bool groundStrike = anim == MeleeAnim.Slam || anim == MeleeAnim.Overhead;
            if (giant > 0f)
            {
                k.Pitch += giant * (groundStrike ? 28f : 14f);
                k.Drop += giant * (groundStrike ? 0.08f : 0.03f);
            }
            // A spin's full turn is applied after smoothing (see SpinBody), so the filter only ever sees the leftover
            // yaw and can never take the short way round mid-spin.
            float turn = !spin || t < W ? 0f : (t < R0 ? -360f * EOut(strikeU) : -360f);
            float yaw = k.Yaw - turn;
            _spinYaw = mirror ? -turn : turn;
            // The spin is a quick hop: the feet leave the ground just before the turn starts and land once it is done,
            // instead of grinding round on the floor.
            float hop = spin ? SS(W * 0.75f, W, t) * (1f - SS(R0, R0 + (follow - R0) * 0.35f, t)) * 0.06f * s : 0f;
            a.Pelvis = new Vector3(0f, _h0 - (k.Drop + 0.02f) * s + hop, k.Step * 0.3f * s);
            a.Hips = QMath.AY(yaw);
            a.Spine = QMath.AX(k.Pitch * 0.55f) * QMath.AY(k.Twist * 0.45f);
            a.Chest = QMath.AX(k.Pitch * 0.45f) * QMath.AY(k.Twist * 0.55f);
            float look = spin ? 0f : -(k.Yaw + k.Twist);
            a.Neck = QMath.AY(look * 0.3f);
            a.Head = QMath.AX(-k.Pitch * 0.6f) * QMath.AY(look * 0.45f);
            Quaternion frame = spin ? QMath.AY(yaw) : QMath.Identity;
            Vector3 rHand = frame * new Vector3(k.R.x * s, k.R.y * s, k.R.z * s);
            float lowY = groundStrike ? 0.3f : 1.2f;
            if (giant > 0f) rHand.y = Mathf.Lerp(rHand.y, Mathf.Min(rHand.y, lowY), giant);
            if ((anim == MeleeAnim.Overhead || anim == MeleeAnim.Slam) && t < W)
            {
                // Raising a weapon overhead: the hands go up beside the head, not through the face.
                Vector3 side = S(0.15f, 0.03f, 0f) * Mathf.Sin(Mathf.PI * w);
                rHand += side;
                if (k.HasL) k.L += side / s;
            }
            if (anim == MeleeAnim.Flurry && strikeU >= 0f)
            {
                // Three quick alternating punches inside the active window.
                float q = strikeU * 3f;
                int n = Mathf.Min(2, Mathf.FloorToInt(q));
                float ext = Mathf.Sin(Mathf.PI * Frac(q));
                Vector3 guardR = S(0.14f, 1.42f, 0.2f), guardL = S(-0.12f, 1.46f, 0.22f);
                Vector3 hitR = S(0.04f, 1.46f, 0.7f), hitL = S(-0.04f, 1.46f, 0.7f);
                bool right = n != 1;
                rHand = right ? Vector3.Lerp(guardR, hitR, ext) : guardR;
                k.L = (right ? guardL : Vector3.Lerp(guardL, hitL, ext)) / s;
                k.HasL = true;
                a.Hips = QMath.AY((right ? -12f : 12f) * ext);
            }
            if (weapon && k.RBlade.sqrMagnitude > 0.5f)
                a.AR = IkArm(ref a, 1, rHand, frame * k.RPole, frame * k.RBlade, frame * k.REdge, 1f);
            else
            {
                a.AR = IkArm(ref a, 1, rHand, frame * k.RPole);
                a.AR.Wrist = QMath.AX(-12f);
            }
            if (k.HasL)
            {
                Vector3 lHand = frame * new Vector3(k.L.x * s, k.L.y * s, k.L.z * s);
                if (giant > 0f) lHand.y = Mathf.Lerp(lHand.y, Mathf.Min(lHand.y, lowY), giant);
                a.AL = IkArm(ref a, -1, lHand, frame * k.LPole);
                a.AL.Wrist = QMath.AX(-12f);
            }
            else if (!p.TwoHanded)
            {
                // Free hand balances the swing.
                Vector3 bal = QMath.AY((yaw + k.Twist) * 0.7f) * S(-0.2f, 1.26f - k.Drop, 0.26f);
                a.AL = IkArm(ref a, -1, bal, new Vector3(-0.8f, -0.6f, -0.2f));
                a.AL.Wrist = QMath.AX(-15f);
            }
            // Feet: lead (left) foot steps in with the strike, rear foot drives back. Spins pivot on the spot.
            Vector3 ankle = new Vector3(0f, _ankleH, 0f);
            a.LL.Ankle = S(-0.12f, 0f, 0.06f + k.Step) + ankle;
            a.LR.Ankle = S(0.12f, 0f, -0.07f - k.Back) + ankle;
            a.LL.Foot = QMath.AY(-10f);
            a.LR.Foot = QMath.AY(24f);
            a.LL.KneeDir = QMath.AY(-10f) * Vector3.forward;
            a.LR.KneeDir = QMath.AY(20f) * Vector3.forward;
            if (spin)
            {
                Quaternion sp = QMath.AY(yaw);
                a.LL.Ankle = sp * (a.LL.Ankle + new Vector3(0f, hop, 0f));
                a.LR.Ankle = sp * (a.LR.Ankle + new Vector3(0f, hop * 1.6f, 0f));
                a.LL.Foot = sp * a.LL.Foot;
                a.LR.Foot = sp * a.LR.Foot;
                a.LL.KneeDir = sp * a.LL.KneeDir;
                a.LR.KneeDir = sp * a.LR.KneeDir;
                a.LL.Lift = hop;
                a.LR.Lift = hop * 1.6f;
                // Planted (stepping into the wind-up) until the hop lifts the feet, planted again once they land.
                a.Plant = Mathf.Max(1f - SS(W * 0.8f, W * 0.95f, t), SS(R0 + (follow - R0) * 0.3f, R0 + (follow - R0) * 0.5f, t));
            }
            else a.Plant = 1f;
            a.Urgency = 1f;
            if (mirror) MirrorBody(ref a);
            Blend(ref b, ref a, w);
        }

        private void Throw(ref Body b, float t)
        {
            Body a = b;
            float yaw = t < 0.45f ? Mathf.Lerp(0f, 30f, E3(t / 0.45f)) : Mathf.Lerp(30f, -20f, EOut((t - 0.45f) / 0.25f));
            Vector3 hand = t < 0.45f ? Vector3.Lerp(S(0.25f, 1.3f, 0.1f), S(0.26f, 1.74f, -0.24f), E3(t / 0.45f))
                                     : Vector3.Lerp(S(0.26f, 1.74f, -0.24f), S(0.1f, 1.58f, 0.52f), EOut((t - 0.45f) / 0.25f));
            a.Chest = QMath.AY(yaw) * b.Chest;
            a.AR = IkArm(ref a, 1, hand, new Vector3(0.8f, -0.2f, -0.5f));
            a.AL = IkArm(ref a, -1, S(-0.2f, 1.36f, 0.32f), new Vector3(-0.7f, -0.7f, -0.2f));
            float w = Mathf.Min(SS(0f, 0.12f, t), 1f - SS(0.8f, 1f, t));
            BlendArms(ref b, ref a, w);
            b.Chest = QMath.Slerp(b.Chest, a.Chest, w);
        }

        private void Parry(ref PoseInput p, ref Body b, float t)
        {
            Body a = b;
            float e = t < 0.3f ? EOut(t / 0.3f) : 1f - E3((t - 0.3f) / 0.7f);
            a.Chest = QMath.AY(Mathf.Lerp(-12f, 14f, e)) * b.Chest;
            if (p.Blade || p.TwoHanded)
            {
                Vector3 hand = Vector3.Lerp(S(0.08f, 1.36f, 0.42f), S(0.42f, 1.42f, 0.26f), e);
                Vector3 blade = Vector3.Lerp(V(-0.5f, 0.8f, 0.3f), V(0.6f, 0.65f, 0.45f), e);
                a.AR = IkArm(ref a, 1, hand, new Vector3(0.8f, -0.6f, -0.2f), blade, new Vector3(1f, 0f, 0f), 1f);
            }
            else
            {
                Vector3 hand = Vector3.Lerp(S(-0.04f, 1.44f, 0.4f), S(-0.44f, 1.42f, 0.22f), e);
                a.AL = IkArm(ref a, -1, hand, new Vector3(-0.6f, -0.8f, -0.2f));
            }
            float w = Mathf.Min(SS(0f, 0.08f, t), 1f - SS(0.75f, 1f, t));
            BlendArms(ref b, ref a, w);
            b.Chest = QMath.Slerp(b.Chest, a.Chest, w);
        }

        private void Drink(ref Body b, float t)
        {
            float up = Mathf.Min(SS(0.05f, 0.32f, t), 1f - SS(0.78f, 0.98f, t));
            Body a = b;
            a.Head = QMath.AX(-16f * SS(0.3f, 0.5f, t) * (1f - SS(0.7f, 0.85f, t))) * b.Head;
            Vector3 mouth = HeadPoint(ref a, S(-0.03f, 0.0f, 0.14f));
            a.AL = IkArm(ref a, -1, mouth, new Vector3(-0.6f, -0.8f, -0.1f));
            a.AL.Wrist = QMath.AX(-40f) * QMath.AY(-30f);
            BlendArms(ref b, ref a, up);
            b.Head = QMath.Slerp(b.Head, a.Head, up);
        }

        private void Stagger(ref Body b, float t, float side)
        {
            float hit = t < 0.18f ? EOut(t / 0.18f) : 1f - E3((t - 0.18f) / 0.82f);
            Body a = b;
            float sd = Mathf.Clamp(side, -1f, 1f);
            a.Pelvis = new Vector3(b.Pelvis.x, b.Pelvis.y - 0.05f * _s * hit, b.Pelvis.z - 0.04f * _s * hit);
            a.Hips = QMath.AY(sd * 12f * hit) * b.Hips;
            a.Spine = QMath.AX(-14f * hit) * QMath.AY(sd * 10f * hit) * b.Spine;
            a.Chest = QMath.AX(-8f * hit) * QMath.AY(sd * 12f * hit) * b.Chest;
            a.Head = QMath.AX(-16f * hit) * QMath.AZ(-sd * 10f * hit) * b.Head;
            a.AR = IkArm(ref a, 1, S(0.42f, 1.18f, -0.12f), new Vector3(0.6f, -0.6f, -0.5f));
            a.AL = IkArm(ref a, -1, S(-0.42f, 1.2f, -0.06f), new Vector3(-0.6f, -0.6f, -0.5f));
            a.LR.Ankle = S(0.13f, 0f, -0.24f) + new Vector3(0f, _ankleH, 0f);
            a.LL.Ankle = S(-0.11f, 0f, 0.04f) + new Vector3(0f, _ankleH, 0f);
            a.Plant = 1f;
            a.Urgency = 1f;
            Blend(ref b, ref a, hit);
        }

        private void Backstep(ref Body b, float t)
        {
            Body a = b;
            float push = 1f - SS(0f, 0.25f, t);
            // Off the ground for the whole fast part of the dodge (see PlayerCombat.DodgeCurve), down once it has slowed.
            float air = SS(0.02f, 0.1f, t) * (1f - SS(0.6f, 0.7f, t));
            float landing = SS(0.6f, 0.7f, t) * (1f - SS(0.72f, 1f, t));
            float drop = (0.06f * (1f - air) * (1f - SS(0.1f, 0.3f, t)) + 0.08f * landing - 0.02f * air) * _s;
            a.Pelvis = new Vector3(0f, _h0 - 0.03f * _s - drop, 0f);
            a.Hips = QMath.Identity;
            a.Spine = QMath.AX(12f * (1f - air * 0.5f) + 4f * landing);
            a.Chest = QMath.AX(4f);
            a.Head = QMath.AX(-10f);
            float lift = 0.07f * _s * air;
            a.LL.Ankle = S(-0.11f, 0f, 0.08f) + new Vector3(0f, _ankleH + lift, 0f);
            a.LR.Ankle = S(0.12f, 0f, -0.1f) + new Vector3(0f, _ankleH + lift * 0.7f, 0f);
            a.LL.Foot = QMath.AX(-20f * air);
            a.LR.Foot = QMath.AX(-28f * air);
            a.LL.Lift = lift;
            a.LR.Lift = lift * 0.7f;
            a.LL.KneeDir = a.LR.KneeDir = Vector3.forward;
            a.AR = FkArm(ref a, 1, 18f * push + 10f, 16f, 50f, 0f, QMath.AX(-10f));
            a.AL = FkArm(ref a, -1, 18f * push + 10f, 16f, 50f, 0f, QMath.AX(-10f));
            // Feet stay planted until they have lifted for the push-off, and plant again as soon as they touch down.
            a.Plant = Mathf.Max(1f - SS(0.02f, 0.07f, t), SS(0.62f, 0.7f, t));
            float w = Mathf.Min(SS(0f, 0.1f, t), 1f - SS(0.85f, 1f, t));
            Blend(ref b, ref a, w);
            // Still planted while the pose hands back to standing (the body has stopped; the gait has not caught up).
            b.Plant = Mathf.Max(b.Plant, SS(0.62f, 0.7f, t));
        }

        private void Roll(ref Body b, float t, Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir = Nrm(dir);
            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            float s = _s;
            float dive = SS(0f, 0.16f, t);
            float rollU = Mathf.Clamp01((t - 0.12f) / 0.6f);
            float angle = 360f * E3(rollU);
            float tuck = SS(0.06f, 0.2f, t) * (1f - SS(0.68f, 0.86f, t));
            float stand = SS(0.72f, 1f, t);
            Body a = b;
            // Tucked ball: chin to chest, knees to chest, arms wrapped.
            Quaternion spin = QMath.AngleAxis(angle, axis);
            float heading = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Quaternion face = QMath.AY(heading);
            a.Hips = spin * face * QMath.AX(30f * tuck);
            a.Spine = QMath.AX(38f * tuck + 10f * dive * (1f - tuck));
            a.Chest = QMath.AX(26f * tuck);
            a.Neck = QMath.AX(18f * tuck);
            a.Head = QMath.AX(24f * tuck);
            float ballY = Mathf.Lerp(_h0 - 0.06f * s, 0.48f * s + 0.16f * s * (1f - Mathf.Cos(angle * Mathf.Deg2Rad)) * 0.5f, Mathf.Max(tuck, dive * 0.85f));
            ballY = Mathf.Lerp(ballY, _h0 - 0.1f * s * (1f - stand * 0.8f), stand * (1f - tuck));
            a.Pelvis = new Vector3(0f, ballY, 0f);
            // Feet and hands ride with the tucked body.
            Quaternion hb = a.Hips;
            Vector3 hip = a.Pelvis;
            Vector3 tuckL = hip + hb * S(-0.1f, -0.22f, 0.3f), tuckR = hip + hb * S(0.1f, -0.22f, 0.3f);
            // Push-off: the feet leave the ground and trail behind the launching body until the tuck gathers them.
            float pushOff = SS(0.02f, 0.1f, t) * (1f - tuck);
            Vector3 trail = face * S(0f, 0.09f * pushOff, -0.14f * pushOff);
            Vector3 standL = face * S(-0.12f, 0f, 0.08f) + new Vector3(0f, _ankleH, 0f) + trail;
            Vector3 standR = face * S(0.12f, 0f, -0.1f) + new Vector3(0f, _ankleH, 0f) + trail;
            float k = tuck;
            a.LL.Ankle = Vector3.Lerp(standL, tuckL, k);
            a.LR.Ankle = Vector3.Lerp(standR, tuckR, k);
            a.LL.KneeDir = Vector3.Lerp(face * Vector3.forward, hb * Vector3.forward, k);
            a.LR.KneeDir = a.LL.KneeDir;
            a.LL.Foot = QMath.Slerp(face, hb * QMath.AX(-30f), k);
            a.LR.Foot = a.LL.Foot;
            a.LL.Lift = a.LR.Lift = 0.09f * s * pushOff;
            Vector3 handL = hip + hb * S(-0.16f, 0.12f, 0.36f), handR = hip + hb * S(0.16f, 0.12f, 0.36f);
            a.AL = IkArm(ref a, -1, handL, hb * new Vector3(-0.7f, 0f, -0.7f));
            a.AR = IkArm(ref a, 1, handR, hb * new Vector3(0.7f, 0f, -0.7f));
            // Feet stay planted until the push-off lifts them, and plant again once they are back down for the stand-up.
            a.Plant = Mathf.Max(1f - SS(0.02f, 0.07f, t), SS(0.74f, 0.84f, t));
            a.Urgency = 1f;
            float w = Mathf.Min(SS(0f, 0.08f, t), 1f - SS(0.88f, 1f, t));
            Blend(ref b, ref a, w);
            b.Plant = Mathf.Max(b.Plant, SS(0.74f, 0.84f, t));
        }

        // ================================================================== floating

        /// <summary>
        /// Floating bodies (the Mirage Djinn): the pelvis hovers with a slow bob, the legs hang together under it as one
        /// tail that trails back with speed and sways, and no foot is ever planted. Runs after every action, so attacks,
        /// staggers and dodges keep their upper body and simply float.
        /// </summary>
        private void Hover(ref Body b)
        {
            float s = _s;
            float speed = _vel.magnitude;
            float move = SS(0.1f, 2.6f, speed * _tScale / s);
            Vector3 dir = speed > 0.05f ? _vel / speed : Vector3.forward;
            b.Pelvis.y += (0.17f + 0.025f * Mathf.Sin(_idleT * 1.7f)) * s;
            float legLen = _l1 + _l2;
            float sway = Mathf.Sin(_idleT * 1.25f) * 0.035f * s;
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector3 hip = b.Pelvis + b.Hips * Layout.Local[i == 0 ? RigLayout.ThighL : RigLayout.ThighR];
                Vector3 trail = -dir * ((0.07f + 0.32f * move) * s * (1f + 0.12f * i)) + Vector3.back * 0.04f * s;
                Leg leg;
                leg.Ankle = new Vector3(hip.x * 0.35f + side * 0.015f * s + sway, hip.y - legLen * 0.9f + 0.03f * s * move, hip.z) + trail;
                // The tail never touches down: when the body dips (a slam), it curls back instead.
                float floor = _ankleH + 0.16f * s;
                if (leg.Ankle.y < floor)
                {
                    leg.Ankle.z -= (floor - leg.Ankle.y) * 0.8f;
                    leg.Ankle.y = floor;
                }
                leg.KneeDir = NrmOrForward(dir + Vector3.forward);
                leg.Foot = QMath.AY(side * 4f) * QMath.AX(40f + 25f * move);
                leg.Lift = 0f;
                if (i == 0) b.LL = leg;
                else b.LR = leg;
            }
            b.Plant = 0f;
        }

        private static Vector3 NrmOrForward(Vector3 v)
        {
            Vector3 n = Nrm(v);
            return n.sqrMagnitude > 0.5f ? n : Vector3.forward;
        }

        // ================================================================== feet

        private void Feet(ref PoseInput p, ref Body b, float dt, Quaternion rootRot, Quaternion invRoot)
        {
            float plant = Mathf.Clamp01(b.Plant);
            if (!p.Grounded) plant = 0f;
            // Below half plant the locks simply follow the pose, so the lock weight must already be zero at one half:
            // otherwise a foot jumps halfway to the pose the frame plant crosses it (starting a roll, a spin or a walk).
            float lw = SS(0.5f, 1f, plant);
            Vector3 root = p.RootPosition;
            Leg[] legs = _legs;
            float[] groundOff = _groundOff;
            legs[0] = b.LL;
            legs[1] = b.LR;
            groundOff[0] = groundOff[1] = 0f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 desiredW = root + rootRot * legs[i].Ankle;
                float desiredYaw = p.RootYaw + YawOf(legs[i].Foot);
                // Ground under the pose's foot (slopes, kerbs, stairs).
                float gy;
                float off = 0f;
                if (Probe != null && p.Grounded && Probe(desiredW, out gy)) off = Mathf.Clamp(gy - root.y, -0.45f * _s, 0.45f * _s);
                groundOff[i] = off;
                // A planted foot stands flat on the ground, even if the pose had it in the air when it was planted
                // (stopping mid-stride): otherwise it would hover until its next step.
                Vector3 grounded = new Vector3(desiredW.x, root.y + off + _ankleH, desiredW.z);
                if (!_locksValid || plant < 0.5f)
                {
                    _lock[i] = grounded;
                    _lockYaw[i] = desiredYaw;
                    _stepT[i] = -1f;
                    continue;
                }
                _sinceStep[i] += dt;
                Vector3 target = grounded;
                if (_stepT[i] >= 0f)
                {
                    float dur = Mathf.Lerp(0.26f, 0.14f, b.Urgency);
                    if (_creature) dur *= _tScale;
                    _stepT[i] += dt / dur;
                    float u = Mathf.Clamp01(_stepT[i]);
                    Vector3 pos = Vector3.Lerp(_stepFrom[i], target, E3(u));
                    pos.y += Mathf.Sin(Mathf.PI * u) * Mathf.Lerp(0.06f, 0.08f, b.Urgency) * _s;
                    float yaw = Mathf.LerpAngle(_stepFromYaw[i], desiredYaw, E3(u));
                    if (u >= 1f)
                    {
                        _stepT[i] = -1f;
                        _sinceStep[i] = 0f;
                        _lock[i] = target;
                        _lockYaw[i] = desiredYaw;
                    }
                    else
                    {
                        legs[i].Ankle = Vector3.Lerp(legs[i].Ankle + new Vector3(0f, off, 0f), invRoot * (pos - root) + new Vector3(0f, legs[i].Lift, 0f), lw);
                        legs[i].Foot = QMath.Slerp(legs[i].Foot, QMath.AY(yaw - p.RootYaw) * QMath.AX(-12f * Mathf.Sin(Mathf.PI * u)), lw);
                        continue;
                    }
                }
                else
                {
                    Vector3 d = target - _lock[i];
                    d.y = 0f;
                    float thr = Mathf.Lerp(0.2f, 0.09f, b.Urgency) * _s;
                    float yawErr = Mathf.Abs(Mathf.DeltaAngle(_lockYaw[i], desiredYaw));
                    int other = 1 - i;
                    bool otherFree = _stepT[other] < 0f && _sinceStep[other] > 0.05f;
                    bool needs = d.magnitude > thr || yawErr > 38f;
                    Vector3 otherDesired = root + rootRot * (other == 0 ? b.LL.Ankle : b.LR.Ankle);
                    bool worse = d.sqrMagnitude >= Flat(_lock[other] - otherDesired).sqrMagnitude;
                    if (needs && otherFree && (worse || _sinceStep[other] > 0.3f))
                    {
                        _stepT[i] = 0f;
                        _stepFrom[i] = _lock[i];
                        _stepFromYaw[i] = _lockYaw[i];
                    }
                }
                Vector3 planted = invRoot * (_lock[i] - root);
                Quaternion lockRot = QMath.AY(_lockYaw[i] - p.RootYaw);
                // Blend about the part of the sole that is on the ground (ball or heel), not the ankle, so a pitched
                // foot settling flat (or peeling off) rolls on its contact point instead of scraping it along.
                Vector3 poseAnkle = legs[i].Ankle + new Vector3(0f, off, 0f);
                Vector3 heelOff = new Vector3(0f, -_ankleH, -_heel), ballOff = new Vector3(0f, -_ankleH, _ball);
                Vector3 poseHeel = poseAnkle + legs[i].Foot * heelOff, poseBall = poseAnkle + legs[i].Foot * ballOff;
                bool ballDown = poseBall.y < poseHeel.y;
                Vector3 pivotOff = ballDown ? ballOff : heelOff;
                Vector3 pivot = Vector3.Lerp(ballDown ? poseBall : poseHeel, planted + lockRot * pivotOff + new Vector3(0f, legs[i].Lift, 0f), lw);
                Quaternion footRot = QMath.Slerp(legs[i].Foot, lockRot, lw);
                legs[i].Ankle = pivot - footRot * pivotOff;
                legs[i].Foot = footRot;
                legs[i].KneeDir = Vector3.Lerp(legs[i].KneeDir, lockRot * QMath.AY(i == 0 ? -8f : 8f) * Vector3.forward, lw);
            }
            _locksValid = true;
            // Moving or airborne feet follow the pose plus the ground offset.
            if (plant < 0.5f)
            {
                for (int i = 0; i < 2; i++) legs[i].Ankle.y += groundOff[i] * (p.Grounded ? 1f : 0f);
            }
            b.LL = legs[0];
            b.LR = legs[1];
            // Where the feet really went, for the gait's stance anchoring next frame.
            _finalW[0] = root + rootRot * legs[0].Ankle;
            _finalW[1] = root + rootRot * legs[1].Ankle;
            _finalYaw[0] = p.RootYaw + YawOf(legs[0].Foot);
            _finalYaw[1] = p.RootYaw + YawOf(legs[1].Foot);
            _finalRot[0] = rootRot * legs[0].Foot;
            _finalRot[1] = rootRot * legs[1].Foot;
            _lastLockW = lw;
            _gaitHasFinal = true;
            // Lower the pelvis so a foot on lower ground can still reach it.
            float lowest = Mathf.Min(groundOff[0], groundOff[1]);
            _lowestGround = lowest < 0f && p.Grounded ? lowest : 0f;
            b.Pelvis.y += _lowestGround;
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private static float YawOf(Quaternion q)
        {
            Vector3 f = q * Vector3.forward;
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        /// <summary>The pitch part of a foot rotation (its yaw removed).</summary>
        private static Quaternion PitchOnly(Quaternion q)
        {
            return QMath.AY(-YawOf(q)) * q;
        }

        // ================================================================== smoothing and IK

        private void Smooth(ref Body b, float dt, bool snappy)
        {
            if (!_hasOut)
            {
                _out = b;
                _hasOut = true;
                return;
            }
            float k = QMath.Damp(snappy ? 34f : 20f, dt);
            // The pelvis barely lags: its rise and fall each step is what keeps the knees at their real angles.
            float kp = QMath.Damp(snappy ? 34f : 80f, dt);
            // Legs are already continuous (gait and planted feet); only the upper body is filtered.
            Body o = _out;
            Quaternion hipsWas = o.Hips;
            Vector3 pelvisWas = o.Pelvis;
            o.Pelvis = Vector3.Lerp(o.Pelvis, b.Pelvis, kp);
            o.Hips = QMath.Slerp(o.Hips, b.Hips, kp);
            o.Spine = QMath.Slerp(o.Spine, b.Spine, k);
            o.Chest = QMath.Slerp(o.Chest, b.Chest, k);
            o.Neck = QMath.Slerp(o.Neck, b.Neck, k);
            o.Head = QMath.Slerp(o.Head, b.Head, k);
            // Arms are filtered in the pelvis frame so they ride along with fast body turns (spin cuts, rolls)
            // instead of cutting straight across them in root space.
            Quaternion invWas = QMath.Inverse(hipsWas), invNow = QMath.Inverse(b.Hips);
            Arm al = ArmToRoot(Lerp(ArmToPelvis(o.AL, invWas, pelvisWas), ArmToPelvis(b.AL, invNow, b.Pelvis), k), o.Hips, o.Pelvis);
            Arm ar = ArmToRoot(Lerp(ArmToPelvis(o.AR, invWas, pelvisWas), ArmToPelvis(b.AR, invNow, b.Pelvis), k), o.Hips, o.Pelvis);
            // Interpolating hand positions cuts the chord of a swinging arc and would fold the elbow; keep the reach
            // (shoulder to wrist distance) interpolating on its own instead.
            KeepReach(ref al, ref _out, ref b, ref o, -1, k);
            KeepReach(ref ar, ref _out, ref b, ref o, 1, k);
            o.AL = al;
            o.AR = ar;
            o.LL = b.LL;
            o.LR = b.LR;
            o.Plant = b.Plant;
            o.Urgency = b.Urgency;
            _out = o;
            b = o;
        }

        private void KeepReach(ref Arm a, ref Body was, ref Body want, ref Body now, int side, float k)
        {
            Arm w = side < 0 ? was.AL : was.AR, t = side < 0 ? want.AL : want.AR;
            float len = Mathf.Lerp((w.Hand - Shoulder(ref was, side)).magnitude, (t.Hand - Shoulder(ref want, side)).magnitude, k);
            Vector3 sh = Shoulder(ref now, side);
            Vector3 d = a.Hand - sh;
            float m = d.magnitude;
            if (m > 1e-5f) a.Hand = sh + d * (len / m);
        }

        private static Quaternion LimitAngle(Quaternion q, float maxDeg)
        {
            float ang = 2f * Mathf.Acos(Mathf.Clamp(Mathf.Abs(q.w), 0f, 1f)) * Mathf.Rad2Deg;
            return ang > maxDeg ? QMath.Slerp(QMath.Identity, q, maxDeg / ang) : q;
        }

        private static Arm ArmToPelvis(Arm a, Quaternion invHips, Vector3 pelvis)
        {
            a.Hand = invHips * (a.Hand - pelvis);
            a.Elbow = invHips * (a.Elbow - pelvis);
            a.Blade = invHips * a.Blade;
            a.Edge = invHips * a.Edge;
            return a;
        }

        private static Arm ArmToRoot(Arm a, Quaternion hips, Vector3 pelvis)
        {
            a.Hand = hips * a.Hand + pelvis;
            a.Elbow = hips * a.Elbow + pelvis;
            a.Blade = hips * a.Blade;
            a.Edge = hips * a.Edge;
            return a;
        }

        private void Apply(ref Body b, ref PoseInput p)
        {
            RigLayout L = Layout;
            HipsPosition = b.Pelvis;
            Local[RigLayout.Hips] = b.Hips;
            Local[RigLayout.Spine] = b.Spine;
            Local[RigLayout.Chest] = b.Chest;
            Local[RigLayout.Neck] = b.Neck;
            Local[RigLayout.Head] = b.Head;
            Quaternion rH = b.Hips;
            Vector3 pH = b.Pelvis;
            Quaternion rS = rH * b.Spine;
            Vector3 pS = pH + rH * L.Local[RigLayout.Spine];
            Quaternion rC = rS * b.Chest;
            Vector3 pC = pS + rS * L.Local[RigLayout.Chest];
            TargetAnkleL = b.LL.Ankle;
            TargetAnkleR = b.LR.Ankle;
            SolveLeg(pH, rH, RigLayout.ThighL, ref b.LL);
            SolveLeg(pH, rH, RigLayout.ThighR, ref b.LR);
            Vector3 handPosR, bladeR;
            Quaternion handRotR;
            SolveArm(pC, rC, RigLayout.UpperArmR, ref b.AR, out handPosR, out bladeR, out handRotR);
            // The left hand goes onto the weapon where the right hand really put it (foregrip, pistol hand, haft).
            SupportHand(ref p, ref b, handPosR, handRotR, bladeR);
            Vector3 hl, bl;
            Quaternion handRotL;
            SolveArm(pC, rC, RigLayout.UpperArmL, ref b.AL, out hl, out bl, out handRotL);
            if (GripL == HandHold.Support && _leftGunW > 0.999f && p.Reload < 0f && IsGun(p.Hold)) SupportError = (LeftGripPoint(hl, handRotL) - WeaponSupportPoint).magnitude;
        }

        private void SolveLeg(Vector3 pH, Quaternion rH, int thigh, ref Leg leg)
        {
            int shin = thigh + 1, foot = thigh + 2;
            Vector3 j = pH + rH * Layout.Local[thigh];
            Vector3 end;
            Vector3 knee = SolveJoint(j, leg.Ankle, _l1, _l2, leg.KneeDir, out end);
            Vector3 dThigh = knee - j;
            Vector3 dShin = end - knee;
            // Kneecap (+Z) points away from the fold.
            Vector3 fold = dShin - Nrm(dThigh) * Vector3.Dot(dShin, Nrm(dThigh));
            Vector3 zHint = fold.sqrMagnitude > 1e-6f * _s * _s ? -fold : leg.KneeDir;
            Quaternion rT = Basis(-dThigh, zHint);
            Vector3 hinge = rT * Vector3.right;
            Vector3 yS = -Nrm(dShin);
            yS = Nrm(yS - hinge * Vector3.Dot(yS, hinge));
            Quaternion rSh = QMath.FromBasis(hinge, yS, Vector3.Cross(hinge, yS));
            Local[thigh] = QMath.Inverse(rH) * rT;
            Local[shin] = QMath.Inverse(rT) * rSh;
            Local[foot] = QMath.Inverse(rSh) * leg.Foot;
        }

        private void SolveArm(Vector3 pC, Quaternion rC, int upper, ref Arm arm, out Vector3 handPos, out Vector3 bladeDir, out Quaternion handRot)
        {
            int fore = upper + 1, hand = upper + 2;
            Vector3 sh = pC + rC * Layout.Local[upper];
            Vector3 end;
            Vector3 elbow = SolveJoint(sh, arm.Hand, _lu, _lf, arm.Elbow - sh, out end);
            Vector3 dUp = elbow - sh;
            Vector3 dFore = end - elbow;
            // The forearm folds toward the upper arm's +Z.
            Vector3 fold = dFore - Nrm(dUp) * Vector3.Dot(dFore, Nrm(dUp));
            Vector3 zHint = fold.sqrMagnitude > 1e-8f ? fold : -(arm.Elbow - sh);
            Quaternion rU = Basis(-dUp, zHint);
            Vector3 hinge = rU * Vector3.right;
            Vector3 yF = -Nrm(dFore);
            yF = Nrm(yF - hinge * Vector3.Dot(yF, hinge));
            Quaternion rF = QMath.FromBasis(hinge, yF, Vector3.Cross(hinge, yF));
            Quaternion relaxed = rF * arm.Wrist;
            Quaternion rHand = relaxed;
            if (arm.HandW > 0.001f)
            {
                // Point the weapon, within what a wrist can do: it bends about 85 degrees, while the roll about the
                // forearm comes from the forearm itself and goes further. Limiting the two separately keeps a rolling
                // grip from flipping the hand (and the blade) round for a frame.
                Quaternion want = QMath.Slerp(relaxed, HandFrom(arm.Blade, arm.Edge), arm.HandW);
                Quaternion rel = QMath.Inverse(rF) * want;
                float tm = Mathf.Sqrt(rel.y * rel.y + rel.w * rel.w);
                Quaternion twist = tm > 1e-6f ? new Quaternion(0f, rel.y / tm, 0f, rel.w / tm) : QMath.Identity;
                // Most of a roll comes from the forearm turning about its own length (it does not move the wrist), so a
                // weapon held at any angle points true without twisting the wrist itself.
                float roll = 2f * Mathf.Atan2(twist.y, twist.w) * Mathf.Rad2Deg;
                roll = Mathf.DeltaAngle(0f, roll);
                rF = rF * QMath.AY(Mathf.Clamp(roll * 0.75f * arm.HandW, -100f, 100f));
                rel = QMath.Inverse(rF) * want;
                tm = Mathf.Sqrt(rel.y * rel.y + rel.w * rel.w);
                twist = tm > 1e-6f ? new Quaternion(0f, rel.y / tm, 0f, rel.w / tm) : QMath.Identity;
                Quaternion swing = rel * QMath.Inverse(twist);
                rHand = rF * (LimitAngle(swing, 85f) * LimitAngle(twist, 110f));
            }
            Local[upper] = QMath.Inverse(rC) * rU;
            Local[fore] = QMath.Inverse(rU) * rF;
            Local[hand] = QMath.Inverse(rF) * rHand;
            handPos = end;
            bladeDir = rHand * Vector3.down;
            handRot = rHand;
        }
    }
}
