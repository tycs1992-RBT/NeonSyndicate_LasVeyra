using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// A drivable vehicle with raycast suspension and a tire model (load-sensitive grip, friction circle, wheelspin,
    /// handbrake drifts), automatic gearbox, damage with body deformation, smoke, fire and explosions, tire popping,
    /// working lights, sirens, horn, alarm, occupants and pedestrian impacts. Hover bikes (Vehicle.Bike.cs) share all of
    /// this but hover on their own arcade physics, lean into turns and seat their riders in view.
    /// </summary>
    public sealed partial class Vehicle : MonoBehaviour, IDamageable
    {
        public static readonly List<Vehicle> All = new List<Vehicle>();

        public VehicleDef Def;
        public Rigidbody Body;
        public VehicleParts Parts;
        public Color PaintColor;

        // ------------------------------------------------------------------ inputs (set by the driver every frame)
        public float Throttle;
        public float SteerInput;
        public bool Handbrake;
        public bool BoostHeld;
        /// <summary>When true the AI steering angle in SteerInput is used without keyboard smoothing.</summary>
        public bool DirectSteer;

        // ------------------------------------------------------------------ state
        public float Health;
        public bool IsDestroyed { get; private set; }
        public bool OnFire { get; private set; }
        public bool SirenOn;
        public bool LightsOn;
        public bool HornOn;
        public bool EngineRunning = true;
        public bool Locked;
        public bool Parked;
        public bool MissionCritical;
        public bool StolenByPlayer;
        public float LastDamageTime = -99f;
        public GameObject LastAttacker;
        public readonly bool[] TirePopped = new bool[4];
        public bool BulletproofTires;
        public int EngineUpgrade;
        public int ArmorUpgrade;
        public int NitroCharges;
        public float SpawnTime;
        /// <summary>Temporary multipliers (Drive Sigil spell).</summary>
        public float BoostGrip = 1f;
        public float BoostForce = 1f;
        public float BoostArmor = 1f;
        public float BoostTimer;
        public CharacterBase[] Occupants;
        public event Action<Vehicle> Destroyed;

        public CharacterBase Driver { get { return Occupants != null && Occupants.Length > 0 ? Occupants[0] : null; } }
        public bool HasPlayer { get { if (Occupants == null) return false; foreach (CharacterBase c in Occupants) if (c != null && c.IsPlayer) return true; return false; } }
        public bool PlayerDriving { get { return Driver != null && Driver.IsPlayer; } }
        public float Speed { get { return Body != null ? Body.linearVelocity.magnitude : 0f; } }
        public float ForwardSpeed { get { return Body != null ? Vector3.Dot(Body.linearVelocity, transform.forward) : 0f; } }
        public bool IsPolice { get { return Def != null && Def.IsPolice; } }
        public bool IsUpsideDown { get { return transform.up.y < 0.2f; } }
        public int GroundedWheels { get; private set; }
        public float Rpm01 { get; private set; }
        public int Gear { get; private set; }
        public float SlipAmount { get; private set; }
        public float AirTime { get; private set; }
        public bool IsBraking { get; private set; }
        public float SteerAngle { get { return _steer; } }
        public Vector3 Velocity { get { return Body != null ? Body.linearVelocity : Vector3.zero; } }

        // ------------------------------------------------------------------ simulation
        private struct WheelSim
        {
            public Vector3 Attach;
            public float Compression;
            public float LastCompression;
            public bool Grounded;
            public Vector3 Contact;
            public Vector3 Normal;
            public float Spin;
            public float Slip;
            public float Length;
            public Collider Surface;
        }

        private readonly WheelSim[] _w = new WheelSim[4];
        private float _restLength = 0.32f;
        private float _attachDrop = 0.2f;
        private float _spring;
        private float _damper;
        private float _antiRoll;
        private float _steer;
        private float _shiftTimer;
        private float _upsideDownTimer;
        private float _fireTimer;
        private float _smokeTimer;
        private float _hitCheckTimer;
        private float _alarmTimer;
        private float _hornTimer;
        private float _nitroTimer;
        private float _skidEmit;
        private float _destroyedTime;
        private Rigidbody _shell;
        private AudioSource _engine;
        private AudioSource _siren;
        private AudioSource _skid;
        private AudioSource _horn;
        private AudioSource _alarm;
        private AudioSource _fireLoop;
        private Light[] _headSpots;
        private Light _beacon;
        private GameObject[] _glows;
        private Vector3[] _baseVerts;
        private Vector3[] _verts;
        private Mesh _mesh;
        private float _hoverPhase;
        private Transform[] _floatParts;
        private Vector3[] _floatBase;
        private float _doorL;
        private float _doorR;
        private float _doorTargetL;
        private float _doorTargetR;
        private bool _lightsShown;
        private bool _brakeShown;
        private int _beaconPhase = -1;
        private float _deformTotal;
        private readonly Collider[] _hitBuf = new Collider[16];
        private static int _beaconsThisFrame;
        private static int _beaconFrame;
        private static PhysicsMaterial _bodyMaterial;

        public static void ResetStatics()
        {
            All.Clear();
            _bodyMaterial = null;
            _beaconsThisFrame = 0;
        }

        // ================================================================== spawning

        public static Vehicle Spawn(VehicleDef def, Vector3 groundPos, float yaw, Color? paint, bool parked)
        {
            GameObject go = new GameObject(def.Name);
            go.layer = Layers.Vehicles;
            go.transform.position = groundPos + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Vehicle v = go.AddComponent<Vehicle>();
            Color c = paint.HasValue ? paint.Value : def.Paints[UnityEngine.Random.Range(0, def.Paints.Length)];
            v.Setup(def, c, parked);
            return v;
        }

        private void Setup(VehicleDef def, Color paint, bool parked)
        {
            Def = def;
            PaintColor = paint;
            Parked = parked;
            Health = def.Health;
            SpawnTime = Time.time;
            Occupants = new CharacterBase[Mathf.Max(1, def.Seats)];
            Parts = VehicleModels.Build(def, transform, paint);
            _mesh = Parts.BodyFilter.sharedMesh;
            _baseVerts = _mesh.vertices;
            _verts = (Vector3[])_baseVerts.Clone();

            // Colliders.
            if (_bodyMaterial == null)
            {
                _bodyMaterial = new PhysicsMaterial("Vehicle Body");
                _bodyMaterial.dynamicFriction = 0.3f;
                _bodyMaterial.staticFriction = 0.35f;
                _bodyMaterial.bounciness = 0.05f;
                _bodyMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                _bodyMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            }
            BoxCollider lower = gameObject.AddComponent<BoxCollider>();
            lower.center = Parts.BodyCenter;
            lower.size = Parts.BodySize;
            lower.sharedMaterial = _bodyMaterial;
            if (Parts.CabinSize.x > 0.2f)
            {
                BoxCollider cabin = gameObject.AddComponent<BoxCollider>();
                cabin.center = Parts.CabinCenter;
                cabin.size = Parts.CabinSize;
                cabin.sharedMaterial = _bodyMaterial;
                // A bike's "cabin" is the rider: only solid (and shootable) while someone rides.
                if (def.IsBike) { _riderBox = cabin; cabin.enabled = false; }
            }

            // Rigidbody with explicit mass properties for predictable handling.
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = def.Mass;
            Body.linearDamping = 0.02f;
            Body.angularDamping = 0.2f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Body.maxAngularVelocity = 9f;
            float comY = def.Body == BodyStyle.Bus || def.Body == BodyStyle.BoxTruck ? 0.95f : (def.Body == BodyStyle.SUV || def.Body == BodyStyle.Van || def.Body == BodyStyle.Pickup ? 0.62f : (def.IsBike ? 0.42f : 0.46f));
            Body.centerOfMass = new Vector3(0f, comY, 0.04f * def.Length);
            float L = def.Length, W = def.Width, H = def.Height;
            Body.inertiaTensor = new Vector3(def.Mass * (H * H + L * L) / 12f, def.Mass * (W * W + L * L) / 12f, def.Mass * (W * W + H * H) / 12f) * 1.1f;
            Body.inertiaTensorRotation = Quaternion.identity;

            // Suspension tuned from mass so every vehicle has the same ride frequency.
            float sag = def.IsBike ? 0.1f : (def.Body == BodyStyle.Wedge || def.Class == VehicleClass.Sports ? 0.09f : (def.Body == BodyStyle.SUV || def.Body == BodyStyle.Pickup ? 0.15f : 0.12f));
            _attachDrop = 0.2f;
            _restLength = _attachDrop + sag;
            float corner = def.Mass * 0.25f;
            _spring = corner * 9.81f / sag;
            _damper = 2f * (def.IsBike ? 0.5f : 0.42f) * Mathf.Sqrt(_spring * corner);
            _antiRoll = _spring * 0.55f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 wl = Parts.WheelLocal[i];
                _w[i].Attach = new Vector3(wl.x, def.WheelRadius + _attachDrop, wl.z);
                _w[i].Length = _attachDrop;
            }

            // Kinematic shell that blocks the player's CharacterController without stopping the car.
            GameObject shellGo = new GameObject(def.Name + " Shell");
            shellGo.layer = Layers.VehicleShell;
            shellGo.transform.SetPositionAndRotation(transform.position, transform.rotation);
            BoxCollider sl = shellGo.AddComponent<BoxCollider>();
            sl.center = Parts.BodyCenter;
            sl.size = Parts.BodySize + new Vector3(0.02f, 0f, 0.02f);
            if (Parts.CabinSize.x > 0.2f)
            {
                BoxCollider sc = shellGo.AddComponent<BoxCollider>();
                sc.center = Parts.CabinCenter;
                sc.size = Parts.CabinSize;
                if (def.IsBike) { _riderShellBox = sc; sc.enabled = false; }
            }
            _shell = shellGo.AddComponent<Rigidbody>();
            _shell.isKinematic = true;
            _shell.interpolation = RigidbodyInterpolation.None;
            VehicleShellLink link = shellGo.AddComponent<VehicleShellLink>();
            link.Vehicle = this;

            // Headlight glow sprites (visible at night). A bike has one lamp, and it leans with the model.
            _glows = new GameObject[2];
            int glowCount = def.IsBike ? 1 : 2;
            Transform glowParent = Parts.LeanRoot != null ? Parts.LeanRoot : transform;
            for (int i = 0; i < glowCount; i++)
            {
                GameObject g = Shapes.MeshObject("Headlight Glow", Shapes.QuadMesh, new[] { Mats.Glow(new Color(1f, 0.92f, 0.75f, 0.9f)) }, glowParent, false);
                Vector3 lamp = def.IsBike ? (Parts.HeadlightLocal[0] + Parts.HeadlightLocal[1]) * 0.5f : Parts.HeadlightLocal[i];
                g.transform.localPosition = lamp + Vector3.forward * 0.05f;
                g.transform.localRotation = Quaternion.LookRotation(Vector3.back);
                g.transform.localScale = def.IsBike ? new Vector3(0.45f, 0.35f, 1f) : new Vector3(0.9f, 0.6f, 1f);
                g.layer = Layers.IgnoreRaycast;
                g.SetActive(false);
                _glows[i] = g;
            }
            // Everything visual exists now: collect what rides on the hover cushion.
            if (Parts.Hover) CollectFloatParts();

            EngineRunning = !parked;
            if (Def.Emergency)
            {
                _beacon = gameObject.AddComponent<Light>();
                _beacon.type = LightType.Point;
                _beacon.range = 16f;
                _beacon.intensity = 0f;
                _beacon.shadows = LightShadows.None;
                _beacon.enabled = false;
            }
            SetDoorImmediate(0f, 0f);
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
            if (_shell != null) Destroy(_shell.gameObject);
        }

        private void Start()
        {
            if (Game.Audio == null) return;
            _engine = Game.Audio.CreateLoop(transform, Def.Engine, 0f, 70f);
            _skid = Game.Audio.CreateLoop(transform, SfxId.TireScreech, 0f, 60f);
        }

        // ================================================================== seats and doors

        public Transform SeatTransform(int seat)
        {
            if (Parts.Seats == null || Parts.Seats.Length == 0) return transform;
            return Parts.Seats[Mathf.Clamp(seat, 0, Parts.Seats.Length - 1)];
        }

        public int SeatSide(int seat)
        {
            return seat % 2 == 0 ? -1 : 1;
        }

        public void SetOccupant(int seat, CharacterBase c)
        {
            if (seat < 0 || seat >= Occupants.Length) return;
            CharacterBase previous = Occupants[seat];
            Occupants[seat] = c;
            if (Def.IsBike) OnBikeOccupantChanged(seat, previous, c);
            if (seat == 0)
            {
                if (c != null)
                {
                    Parked = false;
                    EngineRunning = true;
                    Body.collisionDetectionMode = c.IsPlayer ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
                }
                else
                {
                    Throttle = 0f;
                    SteerInput = 0f;
                    HornOn = false;
                    Handbrake = true;
                    Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
            }
        }

        public int FreeSeat(bool allowDriver)
        {
            for (int i = allowDriver ? 0 : 1; i < Occupants.Length; i++) if (Occupants[i] == null) return i;
            return -1;
        }

        public int OccupantCount()
        {
            int n = 0;
            foreach (CharacterBase c in Occupants) if (c != null) n++;
            return n;
        }

        /// <summary>Point on the ground just outside the door for a seat.</summary>
        public Vector3 DoorPoint(int seat)
        {
            Transform s = SeatTransform(seat);
            float side = SeatSide(seat);
            Vector3 p = s.position + transform.right * side * (Def.Width * 0.5f + 0.75f - Mathf.Abs(s.localPosition.x));
            p += transform.forward * 0.2f;
            p.y = transform.position.y;
            return p;
        }

        public Vector3 FindExitPoint(int seat)
        {
            int side = SeatSide(seat);
            Vector3[] candidates =
            {
                DoorPoint(seat),
                DoorPoint(seat) - transform.right * side * (Def.Width + 1.5f),
                transform.position - transform.forward * (Def.Length * 0.5f + 1f),
                transform.position + transform.forward * (Def.Length * 0.5f + 1f)
            };
            int mask = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Debris);
            foreach (Vector3 c in candidates)
            {
                Vector3 p = GroundAt(c);
                if (!Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.3f, mask, QueryTriggerInteraction.Ignore)) return p;
            }
            return transform.position + Vector3.up * (Def.Height + 0.3f);
        }

        private static Vector3 GroundAt(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point + Vector3.up * 0.02f;
            return p;
        }

        public void OpenDoor(int seat, bool open)
        {
            if (SeatSide(seat) < 0) _doorTargetL = open ? 1f : 0f;
            else _doorTargetR = open ? 1f : 0f;
        }

        private void SetDoorImmediate(float l, float r)
        {
            _doorL = _doorTargetL = l;
            _doorR = _doorTargetR = r;
            ApplyDoors();
        }

        private void ApplyDoors()
        {
            if (Parts.DoorL != null) Parts.DoorL.localRotation = Quaternion.Euler(0f, 62f * _doorL, 0f);
            if (Parts.DoorR != null) Parts.DoorR.localRotation = Quaternion.Euler(0f, -62f * _doorR, 0f);
        }

        // ================================================================== physics

        public float MaxSteerAngleAt(float speed)
        {
            if (PlayerDriving) return BikeMath.PlayerSteerLock(Def.SteerAngle, speed, Def.IsBike);
            if (Def.IsBike) return BikeMath.SteerLock(Def.SteerAngle, speed);
            return Def.SteerAngle / (1f + Mathf.Abs(speed) * 0.055f);
        }

        private void FixedUpdate()
        {
            if (Body == null) return;
            float dt = Time.fixedDeltaTime;
            if (_shell != null)
            {
                _shell.MovePosition(Body.position);
                _shell.MoveRotation(Body.rotation);
            }
            if (Body.IsSleeping() && Driver == null) return;
            if (!MathUtil.IsFinite(Body.position) || Body.position.y < -80f)
            {
                if (!HasPlayer) { Destroy(gameObject); return; }
                // Recover the player's car to the road nearest the last sane position.
                Vector3 safe = MathUtil.IsFinite(_lastGoodPos) ? _lastGoodPos : Vector3.zero;
                if (Game.Layout != null)
                {
                    int node = Game.Layout.NearestNode(safe);
                    if (node >= 0) safe = Game.Layout.Nodes[node].Pos;
                }
                Vector3 flat = MathUtil.Flat(transform.forward);
                Body.position = GroundAt(safe) + Vector3.up * 1.5f;
                Body.rotation = MathUtil.IsFinite(flat) && flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : Quaternion.identity;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            else if (Body.position.y > -30f) _lastGoodPos = Body.position;
            PreStepVelocity = Body.linearVelocity;
            if (Def.IsBike)
            {
                BikeStep(dt);
                return;
            }

            bool hasDriver = Driver != null && !IsDestroyed;
            float throttle = hasDriver && EngineRunning ? Mathf.Clamp(Throttle, -1f, 1f) : 0f;
            bool handbrake = !hasDriver || Handbrake;
            Vector3 vel = Body.linearVelocity;
            float speed = vel.magnitude;
            float fwdSpeed = Vector3.Dot(vel, transform.forward);

            // Steering.
            float maxSteer = MaxSteerAngleAt(fwdSpeed);
            float target = Mathf.Clamp(SteerInput, -1f, 1f) * maxSteer;
            if (!hasDriver) target = 0f;
            _steer = PlayerDriving ? BikeMath.StepPlayerSteer(_steer, target, fwdSpeed, false, DirectSteer, dt) :
                (DirectSteer ? Mathf.MoveTowards(_steer, target, 220f * dt) : Mathf.MoveTowards(_steer, target, (Mathf.Abs(target) < Mathf.Abs(_steer) ? 160f : 110f) * dt));

            // Drive force from throttle with an automatic gearbox (gear only affects feel and sound).
            float top = Def.TopSpeed * (1f + EngineUpgrade * 0.06f);
            float maxForce = Def.DriveForce * (1f + EngineUpgrade * 0.12f) * BoostForce;
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
                else drive = throttle * maxForce * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(top * 0.72f, top, fwdSpeed)));
            }
            else if (throttle < -0.01f)
            {
                if (fwdSpeed > 1f) { braking = true; brakeAmount = -throttle; }
                else drive = throttle * maxForce * 0.5f * (1f - Mathf.InverseLerp(9f, 14f, -fwdSpeed));
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
                drive += maxForce * 0.9f * (PlayerDriving ? SkillMods.VehicleBoost : 1f); // nitro skills (SkillMods)
                if (Game.Fx != null) Game.Fx.FirePuff(transform.TransformPoint(Parts.ExhaustLocal), 0.25f);
            }
            IsBraking = braking || (handbrake && hasDriver && Mathf.Abs(fwdSpeed) > 0.5f);

            int driven = Def.Drive == DriveType.AWD ? 4 : 2;
            // Driving skills give the player's vehicle more grip (SkillMods.VehicleGrip).
            float surfaceGrip = SurfaceGrip() * (PlayerDriving ? SkillMods.VehicleGrip : 1f);
            Vector3 up = transform.up;
            int grounded = 0;
            float slipTotal = 0f;
            float cornerMass = Def.Mass * 0.25f;
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                Vector3 attach = transform.TransformPoint(_w[i].Attach);
                float r = WheelRadius(i);
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
                    grounded++;

                    // Tire frame on the contact plane.
                    float steerDeg = front ? _steer : 0f;
                    Vector3 fwd = Quaternion.AngleAxis(steerDeg, up) * transform.forward;
                    Vector3 n = hit.normal;
                    fwd = (fwd - n * Vector3.Dot(fwd, n)).normalized;
                    Vector3 right = Vector3.Cross(n, fwd);
                    Vector3 pv = Body.GetPointVelocity(hit.point);
                    float vLong = Vector3.Dot(pv, fwd);
                    float vLat = Vector3.Dot(pv, right);
                    float grip = Def.Grip * surfaceGrip * BoostGrip * (TirePopped[i] ? 0.45f : 1f);
                    float maxF = grip * springForce;

                    float latGain = 26f;
                    bool rearDrift = handbrake && !front && hasDriver;
                    if (rearDrift) { latGain *= 0.4f; maxF *= 0.55f; }
                    float lat = -vLat * cornerMass * latGain;

                    float lon = 0f;
                    bool isDriven = Def.Drive == DriveType.AWD || (Def.Drive == DriveType.FWD ? front : !front);
                    if (isDriven) lon += drive / driven;
                    if (braking)
                    {
                        float want = -Mathf.Sign(vLong) * Mathf.Min(Mathf.Abs(vLong) * cornerMass / dt * 0.8f, maxF * 1.05f * brakeAmount);
                        lon += want * (front ? 1.15f : 0.85f);
                    }
                    if (handbrake && !front)
                    {
                        lon += -Mathf.Sign(vLong) * Mathf.Min(Mathf.Abs(vLong) * cornerMass / dt * 0.7f, maxF * 0.9f);
                    }
                    if (!hasDriver || (Mathf.Abs(throttle) < 0.05f && !braking))
                    {
                        // Engine braking / rolling resistance; parks the car when slow.
                        float hold = Mathf.Abs(vLong) < 1.5f ? 3.2f : 0.08f;
                        lon += -vLong * cornerMass * hold;
                    }
                    lon += -vLong * 6f;

                    float total = Mathf.Sqrt(lat * lat + lon * lon);
                    float slip = 0f;
                    if (total > maxF && total > 1e-3f)
                    {
                        float k = maxF / total;
                        slip = 1f - k;
                        lat *= k;
                        lon *= k;
                    }
                    _w[i].Slip = Mathf.Max(slip, Mathf.Clamp01((Mathf.Abs(vLat) - 2.5f) / 6f));
                    slipTotal += _w[i].Slip;
                    Body.AddForceAtPosition(right * lat + fwd * lon, hit.point + up * (r * 0.4f));
                    _w[i].Spin += (isDriven && slip > 0.3f && drive != 0f ? Mathf.Sign(drive) * Mathf.Max(Mathf.Abs(vLong), 12f) : vLong) / r * Mathf.Rad2Deg * dt;
                }
                else
                {
                    _w[i].Grounded = false;
                    _w[i].Compression = 0f;
                    _w[i].LastCompression = 0f;
                    _w[i].Length = _restLength;
                    _w[i].Slip = 0f;
                    if (Def.Drive == DriveType.AWD || (Def.Drive == DriveType.FWD ? front : !front)) _w[i].Spin += throttle * 900f * dt;
                }
            }
            GroundedWheels = grounded;
            SlipAmount = grounded > 0 ? slipTotal / grounded : 0f;

            // Anti-roll bars.
            for (int axle = 0; axle < 2; axle++)
            {
                int a = axle * 2, b = a + 1;
                // The more compressed (low) side is pushed up and the extended side pulled down, resisting body roll.
                float diff = _w[a].Compression - _w[b].Compression;
                float f = diff * _antiRoll;
                if (_w[a].Grounded) Body.AddForceAtPosition(up * f, transform.TransformPoint(_w[a].Attach));
                if (_w[b].Grounded) Body.AddForceAtPosition(up * -f, transform.TransformPoint(_w[b].Attach));
            }

            // Aero: drag and downforce.
            Body.AddForce(-vel * speed * 0.42f * (Def.Width * Def.Height * 0.35f));
            if (grounded > 0) Body.AddForce(-up * speed * speed * (Def.Class == VehicleClass.Super ? 3.2f : (Def.Class == VehicleClass.Sports ? 2.2f : 1.1f)));

            if (grounded > 0)
            {
                AirTime = 0f;
                // Player yaw is limited by the tires' available grip. Traffic keeps its previous steering model.
                // This correction is absent in the air, during intentional drifts and on a nearly rolled-over body.
                if (PlayerDriving && hasDriver && !handbrake && grounded >= 2 && up.y > 0.35f)
                {
                    float tireGrip = 0f;
                    for (int i = 0; i < 4; i++) tireGrip += TirePopped[i] ? 0.45f : 1f;
                    float maxLateral = Def.Grip * surfaceGrip * BoostGrip * BikeMath.Gravity * tireGrip * 0.25f * (grounded / 4f);
                    float desired = BikeMath.PlayerYawRate(fwdSpeed, _steer, Def.Wheelbase, maxLateral, false);
                    float actual = Vector3.Dot(Body.angularVelocity, up);
                    float acceleration = BikeMath.PlayerYawAcceleration(desired, actual, fwdSpeed, grounded, false, dt);
                    Body.AddTorque(up * acceleration * Body.inertiaTensor.y);
                }
                else if (!PlayerDriving && !handbrake && speed > 4f && hasDriver)
                {
                    float wheelbase = Def.Wheelbase;
                    float desired = fwdSpeed * Mathf.Tan(_steer * Mathf.Deg2Rad) / Mathf.Max(1f, wheelbase);
                    float actual = Vector3.Dot(Body.angularVelocity, up);
                    float gain = 3.5f;
                    Body.AddTorque(up * (desired - actual) * Body.inertiaTensor.y * gain * Mathf.Clamp01(speed / 12f));
                }
            }
            else
            {
                AirTime += dt;
                if (hasDriver && AirTime > 0.2f)
                {
                    // Light arcade air control.
                    Body.AddRelativeTorque(new Vector3(Throttle * 1.6f, 0f, -SteerInput * 1.6f) * Body.mass);
                }
            }

            // Upside down recovery for the player; AI wrecks get cleaned up by the traffic manager.
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

        private static readonly float[] GearBreaks = { 0f, 0.16f, 0.31f, 0.48f, 0.66f, 0.84f, 1.2f };

        private void UpdateGearbox(float fwdSpeed, float top, float dt, ref float drive)
        {
            float s = Mathf.Abs(fwdSpeed) / Mathf.Max(1f, top);
            int gear;
            float[] breaks = GearBreaks;
            gear = 1;
            for (int i = 1; i < breaks.Length - 1; i++) if (s > breaks[i]) gear = i + 1;
            if (fwdSpeed < -0.5f && Throttle < 0f) gear = -1;
            if (gear != Gear && Gear != 0)
            {
                _shiftTimer = gear > Gear && gear > 1 ? 0.18f : 0f;
            }
            Gear = gear;
            if (_shiftTimer > 0f)
            {
                _shiftTimer -= dt;
                drive *= 0.35f;
            }
            int gi = Mathf.Clamp(Mathf.Abs(gear), 1, breaks.Length - 1);
            float lo = breaks[gi - 1], hi = breaks[gi];
            float within = Mathf.InverseLerp(lo, hi, s);
            float target = Mathf.Lerp(0.28f, 1f, within);
            if (GroundedWheels == 0 || SlipAmount > 0.5f) target = Mathf.Max(target, Mathf.Abs(Throttle) * 0.95f);
            if (Driver == null || !EngineRunning) target = 0f;
            Rpm01 = Mathf.MoveTowards(Rpm01, target, dt * 3f);
        }

        private float WheelRadius(int i)
        {
            return Def.WheelRadius * (TirePopped[i] ? 0.82f : 1f);
        }

        private float SurfaceGrip()
        {
            float g = 1f;
            Vector3 p = transform.position;
            if (Game.Atmo != null) g *= 1f - Game.Atmo.RainAmount * 0.18f;
            if (p.y < -4.6f && Mathf.Abs(p.z - CityLayout.CanalZ) < CityLayout.CanalHalfWidth) g *= 0.8f;
            if (CityLayout.IsWilderness(CityLayout.DistrictAt(p)) && Game.Layout != null && Game.Layout.RoadAt(p, 1f) == null)
            {
                // Off-road: loose sand, slicker snow, and the frozen lake is close to sheet ice.
                if (CityLayout.IsOnFrozenLake(p.x, p.z)) g *= 0.42f;
                else if (CityLayout.IsSnowGround(p.x, p.z)) g *= 0.62f;
                else g *= 0.78f;
            }
            return g;
        }

        private void CheckPedestrianHits(Vector3 vel, float speed)
        {
            Vector3 center = transform.TransformPoint(Parts.BodyCenter) + vel * 0.03f;
            Vector3 half = Parts.BodySize * 0.5f + new Vector3(0.2f, 0.3f, 0.35f);
            int n = Physics.OverlapBoxNonAlloc(center, half, _hitBuf, transform.rotation, 1 << Layers.Characters, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _hitBuf[i].GetComponentInParent<CharacterBase>();
                if (c == null || c.CurrentVehicle == this || c.InVehicle) continue;
                Vector3 to = c.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.01f) to = transform.forward;
                float closing = Vector3.Dot(vel - c.Velocity, to.normalized);
                if (closing < 2.2f) continue;
                c.OnVehicleHit(this, vel, closing);
                Body.linearVelocity = vel * Mathf.Lerp(0.985f, 0.9f, 80f / Def.Mass * 10f);
                bool creature = c.Team != null && c.Team.Team == Team.Creature;
                if (PlayerDriving && !creature)
                {
                    if (Game.Wanted != null) Game.Wanted.ReportCrime(c.Team != null && c.Team.Team == Team.Police ? CrimeType.AttackCop : CrimeType.HitPedestrian, c.transform.position, c.gameObject);
                    if (Game.Cam != null) Game.Cam.Shake(0.15f);
                }
            }
        }

        /// <summary>Visual parts that ride on the hover cushion (everything but the pads, the glow and colliders).</summary>
        private void CollectFloatParts()
        {
            _hoverPhase = (GetInstanceID() & 1023) * 0.0061f;
            System.Collections.Generic.List<Transform> parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Wheel") || child == Parts.Underglow) continue;
                if (child.GetComponent<Collider>() != null) continue;
                // A bike's seats follow their anchors (which already ride the lean pivot's bob).
                if (Def.IsBike && child.name.StartsWith("Seat")) continue;
                parts.Add(child);
            }
            _floatParts = parts.ToArray();
            _floatBase = new Vector3[_floatParts.Length];
            for (int i = 0; i < _floatParts.Length; i++) _floatBase[i] = _floatParts[i].localPosition;
        }

        private void AddFloatPart(Transform t)
        {
            if (!Parts.Hover || _floatParts == null) return;
            System.Array.Resize(ref _floatParts, _floatParts.Length + 1);
            System.Array.Resize(ref _floatBase, _floatBase.Length + 1);
            _floatParts[_floatParts.Length - 1] = t;
            _floatBase[_floatBase.Length - 1] = t.localPosition;
        }

        // ================================================================== frame update: visuals, audio, lights

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 camPos = Game.Cam != null ? Game.Cam.transform.position : transform.position;
            float camDist = Vector3.Distance(camPos, transform.position);
            bool near = camDist < 110f;

            // Wheels (a bike's two pods are handled in UpdateBikeVisuals).
            for (int i = 0; i < 4 && !Def.IsBike; i++)
            {
                Transform wt = Parts.Wheels[i];
                if (wt == null) continue;
                Vector3 local = Parts.WheelLocal[i];
                local.y = _w[i].Attach.y - _w[i].Length;
                local.y = Mathf.Lerp(wt.localPosition.y, local.y, MathUtil.Damp(30f, dt));
                wt.localPosition = local;
                float steer = i < 2 ? _steer : 0f;
                if (Parts.Hover)
                {
                    // Thrusters do not spin: they vector with the steering and tip with the throttle.
                    float tip = Mathf.Min(8f, Speed * 0.35f);
                    wt.localRotation = Quaternion.Euler(tip, steer * 0.6f, 0f);
                }
                else wt.localRotation = Quaternion.Euler(0f, steer, 0f) * Quaternion.Euler(Mathf.Repeat(_w[i].Spin, 360f), 0f, 0f);
                if (TirePopped[i]) wt.localScale = new Vector3(i % 2 == 0 ? -1f : 1f, 0.85f, 0.85f);
            }
            if (Parts.Hover && _floatParts != null)
            {
                // Body, lights, doors and seats float a hand's width above the pads and bob on the thrusters.
                float bob = IsDestroyed ? 0f : (Def.IsBike ? BikeMath.Bob(Time.time, _hoverPhase, Speed) : 0.06f + Mathf.Sin(Time.time * 2.3f + _hoverPhase) * (0.018f + Mathf.Min(0.02f, Speed * 0.0008f)));
                for (int i = 0; i < _floatParts.Length; i++)
                    if (_floatParts[i] != null) _floatParts[i].localPosition = _floatBase[i] + new Vector3(0f, bob, 0f);
                if (Parts.Underglow != null) Parts.Underglow.gameObject.SetActive(!IsDestroyed && near);
            }
            if (Def.IsBike) UpdateBikeVisuals(dt);

            // Doors.
            _doorL = Mathf.MoveTowards(_doorL, _doorTargetL, dt * 3.5f);
            _doorR = Mathf.MoveTowards(_doorR, _doorTargetR, dt * 3.5f);
            ApplyDoors();

            // Lights.
            bool night = Game.Atmo != null && Game.Atmo.NightFactor > 0.45f;
            if (Driver != null && !Driver.IsPlayer) LightsOn = night;
            bool lights = LightsOn && !IsDestroyed && (Driver != null || !Parked);
            if (lights != _lightsShown)
            {
                _lightsShown = lights;
                if (Parts.Headlights != null) Parts.Headlights.sharedMaterial = lights ? Mats.HeadlightOn : Mats.Headlight;
                for (int i = 0; i < 2; i++) if (_glows[i] != null) _glows[i].SetActive(lights);
            }
            bool brake = IsBraking && !IsDestroyed && Driver != null;
            if (brake != _brakeShown)
            {
                _brakeShown = brake;
                if (Parts.Taillights != null) Parts.Taillights.sharedMaterial = brake ? Mats.BrakeLight : Mats.Taillight;
            }
            UpdateHeadSpots(lights && PlayerDriving && night);
            UpdateEmergency(dt, near);

            // Audio.
            if (_engine != null)
            {
                bool audible = camDist < 75f && EngineRunning && !IsDestroyed;
                if (audible && !_engine.isPlaying) _engine.Play();
                else if (!audible && _engine.isPlaying) _engine.Stop();
                if (audible)
                {
                    float load = Mathf.Abs(Throttle);
                    _engine.pitch = Def.EnginePitch * (0.55f + Rpm01 * 1.25f + load * 0.08f) * (GameTime.Scale < 0.9f ? 0.7f : 1f);
                    _engine.volume = (0.28f + load * 0.45f + Rpm01 * 0.15f) * Settings.SfxVolume * (PlayerDriving ? 0.85f : 0.6f);
                }
            }
            if (_skid != null)
            {
                float skidTarget = GroundedWheels > 0 && Speed > 3f ? Mathf.Clamp01((SlipAmount - 0.1f) * 1.8f) : 0f;
                if (skidTarget > 0.02f && !_skid.isPlaying && camDist < 60f) _skid.Play();
                _skid.volume = Mathf.MoveTowards(_skid.volume, skidTarget * 0.6f * Settings.SfxVolume, dt * 3f);
                if (_skid.isPlaying && _skid.volume < 0.01f) _skid.Stop();
            }
            UpdateHorn(dt);
            UpdateAlarm(dt);

            // Skid marks and tire smoke.
            if (near && GroundedWheels > 0 && SlipAmount > 0.25f && Speed > 4f)
            {
                _skidEmit -= dt;
                for (int i = 0; i < 4; i++)
                {
                    // A bike leaves two marks (its left rays), not four.
                    if (!_w[i].Grounded || _w[i].Slip < 0.3f || (Def.IsBike && (i & 1) == 1))
                    {
                        SkidMarks.Break(i + GetInstanceID() * 4);
                        continue;
                    }
                    SkidMarks.Add(i + GetInstanceID() * 4, _w[i].Contact + _w[i].Normal * 0.02f, _w[i].Normal, transform.right, Mathf.Clamp01(_w[i].Slip));
                    if (_skidEmit <= 0f && Game.Fx != null) Game.Fx.Smoke(_w[i].Contact + Vector3.up * 0.2f, Vector3.up * 0.6f + UnityEngine.Random.insideUnitSphere * 0.5f, 0.9f, 0.75f);
                }
                if (_skidEmit <= 0f) _skidEmit = 0.05f;
            }
            else
            {
                for (int i = 0; i < 4; i++) SkidMarks.Break(i + GetInstanceID() * 4);
            }

            // Water splashes in the canal.
            if (near && Speed > 3f && transform.position.y < -4.3f && Mathf.Abs(transform.position.z - CityLayout.CanalZ) < CityLayout.CanalHalfWidth && Game.Fx != null && UnityEngine.Random.value < 0.3f)
                Game.Fx.Splash(transform.position + transform.forward * Def.Length * 0.4f, Mathf.Clamp01(Speed / 20f));

            // Damage effects.
            UpdateDamageEffects(dt);
        }

        private void UpdateHeadSpots(bool on)
        {
            if (on && _headSpots == null)
            {
                _headSpots = new Light[2];
                int beams = Def.IsBike ? 1 : 2;
                for (int i = 0; i < beams; i++)
                {
                    GameObject g = new GameObject("Headlight Beam");
                    // On a bike the beam leans with the model (the lean pivot already floats).
                    g.transform.SetParent(Parts.LeanRoot != null ? Parts.LeanRoot : transform, false);
                    g.transform.localPosition = (Def.IsBike ? (Parts.HeadlightLocal[0] + Parts.HeadlightLocal[1]) * 0.5f : Parts.HeadlightLocal[i]) + Vector3.forward * 0.1f;
                    if (Parts.LeanRoot == null) AddFloatPart(g.transform);
                    g.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                    Light l = g.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.range = 42f;
                    l.spotAngle = 62f;
                    l.intensity = 2.2f;
                    l.color = new Color(1f, 0.95f, 0.85f);
                    l.shadows = i == 0 && Settings.Graphics >= GraphicsPreset.High ? LightShadows.Hard : LightShadows.None;
                    _headSpots[i] = l;
                }
            }
            if (_headSpots != null)
            {
                for (int i = 0; i < 2; i++) if (_headSpots[i] != null) _headSpots[i].enabled = on;
            }
        }

        private void UpdateEmergency(float dt, bool near)
        {
            if (!Def.Emergency) return;
            bool siren = SirenOn && !IsDestroyed;
            if (siren && _siren == null && Game.Audio != null) _siren = Game.Audio.CreateLoop(transform, IsPolice ? SfxId.Siren : SfxId.SirenYelp, 0.85f, 160f);
            if (_siren != null)
            {
                if (siren && !_siren.isPlaying) _siren.Play();
                if (!siren && _siren.isPlaying) _siren.Stop();
                _siren.volume = 0.85f * Settings.SfxVolume;
            }
            int phase = siren ? (Mathf.FloorToInt(Time.time * 4f) & 1) : -1;
            if (phase != _beaconPhase)
            {
                _beaconPhase = phase;
                if (Parts.LightbarRed != null) Parts.LightbarRed.sharedMaterial = phase == 0 ? Mats.PoliceRed : (phase < 0 ? Mats.LightOff : Mats.Solid(new Color(0.4f, 0.05f, 0.05f), 0.8f));
                if (Parts.LightbarBlue != null) Parts.LightbarBlue.sharedMaterial = phase == 1 ? Mats.PoliceBlue : (phase < 0 ? Mats.LightOff : Mats.Solid(new Color(0.05f, 0.08f, 0.4f), 0.8f));
            }
            if (_beacon != null)
            {
                if (_beaconFrame != Time.frameCount)
                {
                    _beaconFrame = Time.frameCount;
                    _beaconsThisFrame = 0;
                }
                bool lit = siren && near && _beaconsThisFrame < 4;
                if (lit)
                {
                    _beaconsThisFrame++;
                    _beacon.enabled = true;
                    _beacon.color = phase == 0 ? new Color(1f, 0.1f, 0.1f) : new Color(0.15f, 0.3f, 1f);
                    _beacon.intensity = 3.5f;
                }
                else if (_beacon.enabled) _beacon.enabled = false;
            }
        }

        private void UpdateHorn(float dt)
        {
            bool want = HornOn && !IsDestroyed;
            if (_hornTimer > 0f)
            {
                _hornTimer -= dt;
                want = true;
            }
            if (want && _horn == null && Game.Audio != null) _horn = Game.Audio.CreateLoop(transform, Def.Mass > 3000f ? SfxId.HornTruck : SfxId.Horn, 0.75f, 90f);
            if (_horn != null)
            {
                if (want && !_horn.isPlaying) _horn.Play();
                if (!want && _horn.isPlaying) _horn.Stop();
            }
            if (want && Time.frameCount % 15 == 0) Threats.Report(transform.position + transform.forward * Def.Length * 0.5f, 14f, gameObject, ThreatKind.CarCrash);
        }

        /// <summary>Short honk (AI drivers).</summary>
        public void Honk(float seconds)
        {
            _hornTimer = Mathf.Max(_hornTimer, seconds);
        }

        public void TriggerAlarm(float seconds)
        {
            _alarmTimer = seconds;
        }

        private void UpdateAlarm(float dt)
        {
            if (_alarmTimer <= 0f)
            {
                if (_alarm != null && _alarm.isPlaying) _alarm.Stop();
                return;
            }
            _alarmTimer -= dt;
            if (_alarm == null && Game.Audio != null) _alarm = Game.Audio.CreateLoop(transform, SfxId.CarAlarm, 0.7f, 70f);
            if (_alarm != null && !_alarm.isPlaying) _alarm.Play();
            bool flash = (Mathf.FloorToInt(Time.time * 2.5f) & 1) == 0;
            if (Parts.Headlights != null) Parts.Headlights.sharedMaterial = flash ? Mats.HeadlightOn : Mats.Headlight;
            _lightsShown = flash;
        }

        // ================================================================== damage

        public bool IsAlive { get { return !IsDestroyed; } }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDestroyed) return;
            float armor = BoostArmor / (1f + ArmorUpgrade * 0.35f);
            float amount = info.Amount * armor;
            switch (info.Type)
            {
                case DamageType.Bullet:
                    amount *= 0.55f;
                    CheckTireHit(info.Point);
                    // Rounds through the windows can hit the people inside. A bike's riders sit in the open, so hits on
                    // the tank and seat under them count too (within a shorter reach).
                    bool aboveBelt = info.Point.y > transform.position.y + Parts.BeltY + 0.05f;
                    if ((aboveBelt || Def.IsBike) && ArmorUpgrade < 3 && Def.Class != VehicleClass.Armored)
                    {
                        CharacterBase victim = null;
                        float best = aboveBelt ? 2.2f : 1.4f;
                        foreach (CharacterBase occ in Occupants)
                        {
                            if (occ == null || occ.IsDead) continue;
                            float dd = Vector3.Distance(occ.ChestPosition, info.Point);
                            if (dd < best) { best = dd; victim = occ; }
                        }
                        if (victim != null)
                        {
                            // The player and story companions are shielded by the door frame; only a round
                            // that really lands on an NPC's head counts as a headshot.
                            NpcController vn = victim as NpcController;
                            bool shielded = victim.IsPlayer || (vn != null && vn.Role == NpcRole.Companion);
                            DamageInfo pass = info;
                            // Riders sit in the open: a bike shields far less than a car's door frame.
                            float share = Def.IsBike ? (shielded ? (victim.IsPlayer ? 0.6f : 0.4f) : 1f) : (shielded ? 0.35f : 0.8f);
                            pass.Amount = info.Amount * share;
                            pass.Point = victim.ChestPosition;
                            // Headshot if the bullet's path carries on through the head (within about 22 cm).
                            Vector3 dirN = info.Direction.sqrMagnitude > 0.0001f ? info.Direction.normalized : Vector3.zero;
                            Vector3 toHead = victim.HeadPosition - info.Point;
                            float along = Vector3.Dot(toHead, dirN);
                            pass.Headshot = !shielded && along > -0.2f && (toHead - dirN * along).sqrMagnitude < 0.05f;
                            victim.Health.TakeDamage(pass);
                        }
                    }
                    if (Game.Audio != null && UnityEngine.Random.value < 0.25f && info.Point.y > transform.position.y + Parts.BeltY) Game.Audio.PlayVar(SfxId.GlassBreak, info.Point, 0.35f, 0.2f, 30f);
                    break;
                case DamageType.Explosion:
                    amount *= 3.2f;
                    break;
                case DamageType.Fire:
                    amount *= 1.4f;
                    break;
                case DamageType.Melee:
                    amount *= 0.25f;
                    break;
            }
            ApplyDamage(amount, info.Source);
            if (info.Type == DamageType.Melee && Parked && Driver == null && UnityEngine.Random.value < 0.5f) TriggerAlarm(12f);
            // A blast next to a bike throws its riders off.
            if (Def.IsBike && info.Type == DamageType.Explosion && !IsDestroyed && OccupantCount() > 0)
                ThrowRiders(Velocity, info.Direction * Mathf.Clamp(info.Force, 3f, 9f), 0f);
        }

        public void ApplyDamage(float amount, GameObject source)
        {
            if (IsDestroyed || amount <= 0f || Invulnerable) return;
            if (MissionCritical && Game.Missions != null && Game.Missions.ProtectsVehicle(this)) amount *= 0.6f;
            Health -= amount;
            LastDamageTime = Time.time;
            if (source != null) LastAttacker = source;
            if (Driver != null && !Driver.IsPlayer && source != null && Game.Player != null && source == Game.Player.gameObject)
            {
                TrafficDriver ai = GetComponent<TrafficDriver>();
                if (ai != null) ai.OnAttackedByPlayer();
            }
            if (Health <= Def.Health * 0.12f && !OnFire && Health > 0f)
            {
                OnFire = true;
                _fireTimer = UnityEngine.Random.Range(4f, 7f);
                if (Game.Audio != null) _fireLoop = Game.Audio.CreateLoop(transform, SfxId.FireLoop, 0.6f, 40f);
            }
            if (Health <= 0f) Explode(source);
        }

        private void CheckTireHit(Vector3 point)
        {
            if (BulletproofTires) return;
            for (int i = 0; i < 4; i++)
            {
                if (TirePopped[i] || Parts.Wheels[i] == null) continue;
                if (Vector3.Distance(Parts.Wheels[i].position, point) < Def.WheelRadius * 1.25f)
                {
                    TirePopped[i] = true;
                    if (Game.Audio != null) Game.Audio.PlayAt(SfxId.ExplosionSmall, point, 0.35f, 1.8f, 40f);
                    if (Game.Fx != null) Game.Fx.Dust(point, Vector3.up, 0.5f);
                    break;
                }
            }
        }

        private void UpdateDamageEffects(float dt)
        {
            if (IsDestroyed)
            {
                if (Time.time - _destroyedTime < 25f && Game.Fx != null)
                {
                    _smokeTimer -= dt;
                    if (_smokeTimer <= 0f)
                    {
                        _smokeTimer = 0.08f;
                        Vector3 p = transform.TransformPoint(Parts.CabinCenter);
                        Game.Fx.Smoke(p + Vector3.up, Vector3.up * 2.5f, 2f, 0.12f);
                        if (Time.time - _destroyedTime < 12f) Game.Fx.FirePuff(p + UnityEngine.Random.insideUnitSphere * 0.8f, 1.2f);
                    }
                }
                if (_fireLoop != null && Time.time - _destroyedTime > 12f) { Destroy(_fireLoop); _fireLoop = null; }
                return;
            }
            float h = Health / Def.Health;
            if (h < 0.35f && Game.Fx != null)
            {
                _smokeTimer -= dt;
                if (_smokeTimer <= 0f)
                {
                    _smokeTimer = Mathf.Lerp(0.04f, 0.2f, h / 0.35f);
                    Vector3 engine = transform.TransformPoint(Parts.EngineLocal + Vector3.up * 0.1f);
                    Game.Fx.Smoke(engine, Vector3.up * 2f + Body.linearVelocity * 0.3f, 0.9f, Mathf.Lerp(0.15f, 0.6f, h / 0.35f));
                    if (OnFire) Game.Fx.FirePuff(engine, 0.7f);
                }
            }
            if (OnFire)
            {
                _fireTimer -= dt;
                Health -= dt * Def.Health * 0.02f;
                if (_fireTimer <= 0f || Health <= 0f) Explode(LastAttacker);
            }
        }

        public void Explode(GameObject source)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            OnFire = false;
            Health = 0f;
            _destroyedTime = Time.time;
            EngineRunning = false;
            SirenOn = false;
            HornOn = false;
            Vector3 center = transform.TransformPoint(Parts.BodyCenter);
            if (Game.Explosions != null) Game.Explosions.Explode(center, 7.5f, 240f, source, ExplosionKind.Vehicle);
            Body.AddForce(Vector3.up * UnityEngine.Random.Range(5f, 8f), ForceMode.VelocityChange);
            Body.AddTorque(UnityEngine.Random.insideUnitSphere * 2.5f, ForceMode.VelocityChange);
            // Burnt-out look.
            Material burnt = Mats.Solid(new Color(0.07f, 0.065f, 0.06f), 0.1f);
            Material[] mats = Parts.BodyRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = i == 1 ? Mats.Solid(new Color(0.02f, 0.02f, 0.02f), 0.3f) : burnt;
            Parts.BodyRenderer.sharedMaterials = mats;
            foreach (Transform door in new[] { Parts.DoorL, Parts.DoorR })
            {
                if (door == null) continue;
                foreach (MeshRenderer mr in door.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = burnt;
            }
            if (Parts.Headlights != null) Parts.Headlights.sharedMaterial = burnt;
            if (Parts.Taillights != null) Parts.Taillights.sharedMaterial = burnt;
            if (Parts.Bars != null) foreach (MeshRenderer mr in Parts.Bars.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterials = new[] { burnt, burnt, burnt };
            for (int i = 0; i < 2; i++) if (_glows[i] != null) _glows[i].SetActive(false);
            if (_headSpots != null) foreach (Light l in _headSpots) if (l != null) l.enabled = false;
            for (int i = 0; i < 4; i++)
            {
                if (Parts.Wheels[i] == null) continue;
                foreach (MeshRenderer mr in Parts.Wheels[i].GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterials = new[] { burnt, burnt };
            }
            if (_fireLoop == null && Game.Audio != null) _fireLoop = Game.Audio.CreateLoop(transform, SfxId.FireLoop, 0.6f, 40f);
            if (_engine != null) _engine.Stop();
            // Occupants do not survive (unless god mode or a crystal ward protects them; then they're thrown clear).
            for (int i = 0; i < Occupants.Length; i++)
            {
                CharacterBase c = Occupants[i];
                if (c == null) continue;
                bool protectedOccupant = c.Health.Invulnerable;
                c.ExitVehicleImmediate(protectedOccupant);
                if (!protectedOccupant) c.Health.Kill(source, DamageType.Explosion);
            }
            if (source != null && Game.Player != null && source == Game.Player.gameObject)
            {
                if (Game.Stats != null) Game.Stats.Add(StatId.VehiclesDestroyed, 1);
                if (Game.Wanted != null) Game.Wanted.ReportCrime(IsPolice ? CrimeType.DestroyCopCar : CrimeType.DestroyVehicle, center, gameObject);
            }
            if (Destroyed != null)
            {
                try { Destroyed(this); }
                catch (Exception e) { Log.Exception("Vehicle destroyed handler", e); }
            }
        }

        public float DestroyedFor { get { return IsDestroyed ? Time.time - _destroyedTime : 0f; } }

        public void Repair()
        {
            if (IsDestroyed) return;
            Health = Def.Health;
            OnFire = false;
            for (int i = 0; i < 4; i++)
            {
                TirePopped[i] = false;
                if (Parts.Wheels[i] != null) Parts.Wheels[i].localScale = new Vector3(i % 2 == 0 ? -1f : 1f, 1f, 1f);
            }
            if (_fireLoop != null) { Destroy(_fireLoop); _fireLoop = null; }
            Array.Copy(_baseVerts, _verts, _verts.Length);
            _mesh.vertices = _verts;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _deformTotal = 0f;
        }

        public void Respray(Color c)
        {
            PaintColor = c;
            Parts.Paint = Mats.CarPaint(c);
            Material[] mats = Parts.BodyRenderer.sharedMaterials;
            mats[0] = Parts.Paint;
            Parts.BodyRenderer.sharedMaterials = mats;
            foreach (Transform door in new[] { Parts.DoorL, Parts.DoorR })
            {
                if (door == null) continue;
                foreach (MeshRenderer mr in door.GetComponentsInChildren<MeshRenderer>())
                {
                    Material[] dm = mr.sharedMaterials;
                    dm[0] = Parts.Paint;
                    mr.sharedMaterials = dm;
                }
            }
        }

        // ================================================================== collisions and deformation

        /// <summary>Velocity at the start of this physics step (before collisions), used to tell who rammed whom.</summary>
        public Vector3 PreStepVelocity { get; private set; }
        private Vector3 _lastGoodPos;
        private float _lastCopBump = -99f;

        private void OnCollisionEnter(Collision col)
        {
            if (Body == null) return;
            float dv = col.impulse.magnitude / Body.mass;
            if (dv < 2.5f) return;
            ContactPoint cp = col.GetContact(0);
            Vehicle other = col.collider.attachedRigidbody != null ? col.collider.attachedRigidbody.GetComponent<Vehicle>() : null;
            if (col.collider.gameObject.layer == Layers.Ragdoll) return;
            float damage = (dv - 2.5f) * 32f * (Def.Mass > 4000f ? 0.5f : 1f);
            GameObject source = other != null && other.Driver != null ? other.Driver.gameObject : null;
            ApplyDamage(damage, source != null ? source : LastAttacker);
            Vector3 inward = Vector3.Dot(cp.normal, Body.worldCenterOfMass - cp.point) >= 0f ? cp.normal : -cp.normal;
            Deform(cp.point, inward, Mathf.Clamp(dv * 0.014f, 0.02f, 0.2f));
            if (Game.Audio != null) Game.Audio.PlayVar(dv > 9f ? SfxId.CrashHeavy : SfxId.Crash, cp.point, Mathf.Clamp01(dv / 12f), 0.12f, 90f);
            if (Game.Fx != null) Game.Fx.Sparks(cp.point, cp.normal * 3f + Body.linearVelocity * 0.2f, Mathf.Clamp((int)(dv * 2f), 4, 30));
            Threats.Report(cp.point, 22f + dv * 2f, Driver != null ? Driver.gameObject : gameObject, ThreatKind.CarCrash);
            if (PlayerDriving)
            {
                if (Game.Cam != null) Game.Cam.Shake(Mathf.Clamp01(dv / 14f) * 0.5f);
                if (other != null)
                {
                    TrafficDriver ai = other.GetComponent<TrafficDriver>();
                    // Empty or abandoned cruisers don't count; only one with an officer at the wheel.
                    if (other.IsPolice && other.Driver != null && !other.Driver.IsPlayer && Game.Wanted != null && Time.time - _lastCopBump > 2f)
                    {
                        // Only the player's fault counts: a pursuing cruiser ramming the player is not a crime.
                        bool chasing = ai != null && ai.enabled && ai.Mode == DriveMode.Pursuit;
                        Vector3 to = MathUtil.Flat(other.transform.position - transform.position);
                        to = to.sqrMagnitude > 0.01f ? to.normalized : transform.forward;
                        bool playerRammed = Vector3.Dot(PreStepVelocity, to) > Vector3.Dot(other.PreStepVelocity, -to) + 1f;
                        if (!chasing && playerRammed)
                        {
                            _lastCopBump = Time.time;
                            Game.Wanted.ReportCrime(CrimeType.HitCopCar, cp.point, other.gameObject);
                        }
                    }
                    if (ai != null) ai.OnRammed(this, dv);
                }
            }
            if (Parked && Driver == null && dv > 4f && UnityEngine.Random.value < 0.6f) TriggerAlarm(14f);
            // A hard crash throws a bike's riders off (they ragdoll, get up and can climb back on).
            if (Def.IsBike && dv > BikeThrowDv && !IsDestroyed && OccupantCount() > 0 && col.collider.GetComponentInParent<CharacterBase>() == null)
                ThrowRiders(PreStepVelocity * 0.8f, MathUtil.Flat(PreStepVelocity).normalized * 2f, Mathf.Clamp((dv - 7f) * 4f, 0f, 45f));
        }

        /// <summary>Pushes body vertices near an impact inward (crumple zones). Mesh is unique per vehicle.</summary>
        public void Deform(Vector3 worldPoint, Vector3 worldDir, float depth)
        {
            if (_mesh == null || _deformTotal > 6f) return;
            Vector3 lp = Parts.Body.transform.InverseTransformPoint(worldPoint);
            Vector3 ld = Parts.Body.transform.InverseTransformDirection(worldDir).normalized;
            float radius = 0.55f + depth * 3f;
            float r2 = radius * radius;
            bool changed = false;
            for (int i = 0; i < _verts.Length; i++)
            {
                Vector3 v = _verts[i];
                float d2 = (v - lp).sqrMagnitude;
                if (d2 > r2) continue;
                float f = 1f - Mathf.Sqrt(d2) / radius;
                Vector3 moved = v + ld * depth * f * f + UnityEngine.Random.insideUnitSphere * depth * 0.08f * f;
                Vector3 offset = moved - _baseVerts[i];
                if (offset.magnitude > 0.32f) moved = _baseVerts[i] + offset.normalized * 0.32f;
                _verts[i] = moved;
                changed = true;
            }
            if (!changed) return;
            _deformTotal += depth;
            _mesh.vertices = _verts;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        /// <summary>World-space bounds center of the body (aim point for AI shooters).</summary>
        public Vector3 Center { get { return transform.TransformPoint(Parts != null ? Parts.BodyCenter : Vector3.up); } }
    }

    /// <summary>Links a vehicle's kinematic shell back to its vehicle.</summary>
    public sealed class VehicleShellLink : MonoBehaviour
    {
        public Vehicle Vehicle;
    }

    /// <summary>
    /// Tire marks: one dynamic mesh with a ring buffer of quads, extended segment by segment while wheels slide.
    /// </summary>
    public static class SkidMarks
    {
        private const int MaxQuads = 1200;
        private static Mesh _mesh;
        private static Vector3[] _v;
        private static Vector3[] _n;
        private static Vector2[] _uv;
        private static Color32[] _c;
        private static int[] _tris;
        private static int _next;
        private static bool _dirty;
        private static GameObject _go;
        private static readonly Dictionary<int, Vector3> _last = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, int> _lastQuad = new Dictionary<int, int>();

        public static void ResetStatics()
        {
            _mesh = null;
            _go = null;
            _last.Clear();
            _lastQuad.Clear();
            _next = 0;
            _dirty = false;
        }

        private static void Ensure()
        {
            if (_go != null) return;
            _mesh = new Mesh();
            _mesh.name = "Skid Marks";
            _mesh.MarkDynamic();
            _v = new Vector3[MaxQuads * 4];
            _n = new Vector3[MaxQuads * 4];
            _uv = new Vector2[MaxQuads * 4];
            _c = new Color32[MaxQuads * 4];
            _tris = new int[MaxQuads * 6];
            for (int i = 0; i < MaxQuads; i++)
            {
                int v = i * 4;
                _tris[i * 6] = v; _tris[i * 6 + 1] = v + 1; _tris[i * 6 + 2] = v + 2;
                _tris[i * 6 + 3] = v; _tris[i * 6 + 4] = v + 2; _tris[i * 6 + 5] = v + 3;
                _uv[v] = new Vector2(0f, 0f); _uv[v + 1] = new Vector2(0f, 1f); _uv[v + 2] = new Vector2(1f, 1f); _uv[v + 3] = new Vector2(1f, 0f);
                for (int k = 0; k < 4; k++) _n[v + k] = Vector3.up;
            }
            _mesh.vertices = _v;
            _mesh.normals = _n;
            _mesh.uv = _uv;
            _mesh.colors32 = _c;
            _mesh.triangles = _tris;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 5000f);
            _go = new GameObject("Skid Marks");
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer mr = _go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Skid;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            _go.AddComponent<SkidMarksFlusher>();
        }

        public static void Add(int key, Vector3 pos, Vector3 normal, Vector3 right, float intensity)
        {
            Ensure();
            Vector3 last;
            if (!_last.TryGetValue(key, out last))
            {
                _last[key] = pos;
                return;
            }
            float d = (pos - last).sqrMagnitude;
            if (d < 0.09f) return;
            if (d > 9f)
            {
                _last[key] = pos;
                _lastQuad.Remove(key);
                return;
            }
            float w = 0.13f;
            Vector3 side = Vector3.Cross(normal, (pos - last).normalized) * w;
            int q = _next;
            _next = (_next + 1) % MaxQuads;
            int v = q * 4;
            int prev;
            Vector3 a = last - side, b = last + side;
            if (_lastQuad.TryGetValue(key, out prev) && prev >= 0)
            {
                // Connect to the previous quad's front edge for continuous marks.
                a = _v[prev * 4 + 1];
                b = _v[prev * 4 + 2];
            }
            // Clockwise seen from above: back-left, front-left, front-right, back-right.
            _v[v] = a;
            _v[v + 1] = pos - side;
            _v[v + 2] = pos + side;
            _v[v + 3] = b;
            byte alpha = (byte)Mathf.Clamp(intensity * 200f, 40f, 200f);
            Color32 col = new Color32(255, 255, 255, alpha);
            for (int k = 0; k < 4; k++) _c[v + k] = col;
            _last[key] = pos;
            _lastQuad[key] = q;
            _dirty = true;
        }

        public static void Break(int key)
        {
            if (_last.Count == 0) return;
            _last.Remove(key);
            _lastQuad.Remove(key);
        }

        internal static void Flush()
        {
            if (!_dirty || _mesh == null) return;
            _dirty = false;
            _mesh.vertices = _v;
            _mesh.colors32 = _c;
        }
    }

    internal sealed class SkidMarksFlusher : MonoBehaviour
    {
        private void LateUpdate()
        {
            SkidMarks.Flush();
        }
    }
}
