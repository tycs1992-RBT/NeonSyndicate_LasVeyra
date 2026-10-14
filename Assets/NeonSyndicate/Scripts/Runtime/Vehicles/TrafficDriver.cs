using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum DriveMode
    {
        Cruise,
        Panic,
        Pursuit,
        Flee,
        Stopped,
        GoTo
    }

    /// <summary>
    /// AI driver that drives the same physics vehicle the player uses: follows lane paths through intersections,
    /// obeys traffic lights, keeps distance, changes lanes around slow cars, stops for pedestrians, honks, panics
    /// under fire, bails out when threatened, pursues the player (police) or flees (mission targets).
    /// </summary>
    public sealed class TrafficDriver : MonoBehaviour
    {
        public Vehicle V;
        public DriveMode Mode = DriveMode.Cruise;
        public int Lane = -1;
        public float S;
        public float Personality = 1f;
        public CharacterBase PursuitTarget;
        public Vector3 Destination;
        public bool IgnoreLights;
        /// <summary>Scripted speed multiplier for GoTo/Flee driving (tailing targets drive slower, racers faster).</summary>
        public float SpeedScale = 1f;
        public bool Racing;
        public float CurrentTargetSpeed { get; private set; }
        public bool WantsDeploy { get; private set; }

        private int _next = -1;
        private float _stuck;
        private float _reverse;
        private float _panic;
        private float _blocked;
        private float _laneCooldown;
        private float _honkCooldown;
        private float _threatTimer;
        private int _threatSerial;
        private List<Vector3> _route = new List<Vector3>();
        private int _routeIndex;
        private float _routeTimer;
        private float _reverseSteer;
        private float _wantSpeed;
        private Vehicle _ignore;
        private float _deadAheadTimer;
        private float _overtakeTimer;
        private float _overtakeTotal;
        private bool _headOn;
        private readonly Collider[] _buf = new Collider[8];

        public void Init(Vehicle v, int lane, float s)
        {
            V = v;
            Lane = lane;
            S = s;
            Personality = Random.Range(0.86f, 1.1f);
            _threatSerial = Threats.LatestSerial;
            _next = PickNext(lane);
        }

        private CityLayout L { get { return Game.Layout; } }

        private int PickNext(int lane)
        {
            if (L == null || lane < 0 || lane >= L.Lanes.Count) return -1;
            List<int> next = L.Lanes[lane].Next;
            if (next.Count == 0) return -1;
            if (next.Count == 1) return next[0];
            // Prefer going straight; turns are less common.
            float total = 0f;
            for (int i = 0; i < next.Count; i++) total += L.Lanes[next[i]].TurnType == 0 ? 3f : 1f;
            float pick = Random.value * total;
            for (int i = 0; i < next.Count; i++)
            {
                pick -= L.Lanes[next[i]].TurnType == 0 ? 3f : 1f;
                if (pick <= 0f) return next[i];
            }
            return next[next.Count - 1];
        }

        // ------------------------------------------------------------------ reactions

        /// <summary>Scripted drivers (pursuit, flee, go-to, racers) and police keep doing their job when threatened.</summary>
        private bool IgnoresThreats
        {
            get { return Mode == DriveMode.Pursuit || Mode == DriveMode.Flee || Mode == DriveMode.GoTo || Racing || (V != null && V.IsPolice); }
        }

        public void OnAttackedByPlayer()
        {
            if (V == null || V.Driver == null) return;
            if (IgnoresThreats) return;
            if (V.Speed < 3f && Random.value < 0.55f) Bail(true);
            else Panic(25f);
        }

        public void OnRammed(Vehicle by, float dv)
        {
            if (V == null || V.Driver == null || IgnoresThreats) return;
            V.Honk(1.2f);
            NpcController npc = V.Driver as NpcController;
            if (npc != null && npc.Courage > 0.55f && V.Speed < 4f && Random.value < 0.4f && Mode != DriveMode.Panic)
            {
                // Road rage: get out and confront the player.
                npc.ExitVehicleImmediate(false);
                npc.Hostile = true;
                npc.HostileUntil = Time.time + 30f;
                npc.Target = Game.Player;
                npc.SetState(NpcState.Fight);
                Mode = DriveMode.Stopped;
                enabled = false;
                return;
            }
            if (dv > 6f || Random.value < 0.35f) Panic(15f);
        }

        public void Panic(float seconds)
        {
            if (IgnoresThreats) return;
            Mode = DriveMode.Panic;
            _panic = Mathf.Max(_panic, seconds);
            V.Honk(0.8f);
        }

        /// <summary>The driver gets out and runs (or puts their hands up).</summary>
        public void Bail(bool flee)
        {
            if (V == null) return;
            CharacterBase d = V.Driver;
            V.Throttle = 0f;
            V.Handbrake = true;
            if (d != null)
            {
                d.ExitVehicleImmediate(false);
                NpcController npc = d as NpcController;
                if (npc != null)
                {
                    if (flee) npc.Flee(Game.PlayerPosition, 15f);
                    else npc.SetState(NpcState.HandsUp);
                }
            }
            Mode = DriveMode.Stopped;
            enabled = false;
        }

        // ------------------------------------------------------------------ main loop

        private void FixedUpdate()
        {
            if (V == null || V.IsDestroyed || L == null) return;
            if (V.Driver == null || V.Driver.IsPlayer || V.Driver.IsDead)
            {
                V.Throttle = 0f;
                V.Handbrake = true;
                return;
            }
            float dt = Time.fixedDeltaTime;
            _laneCooldown -= dt;
            _honkCooldown -= dt;
            _threatTimer -= dt;
            if (_threatTimer <= 0f)
            {
                _threatTimer = 0.4f;
                CheckThreats();
            }
            if (Mode == DriveMode.Panic)
            {
                _panic -= dt;
                if (_panic <= 0f) Mode = DriveMode.Cruise;
            }
            if (Mode == DriveMode.Stopped)
            {
                V.Throttle = 0f;
                V.Handbrake = true;
                return;
            }
            if (_reverse > 0f)
            {
                _reverse -= dt;
                V.Throttle = -0.7f;
                // While still rolling forward (braking first), steer the other way so the nose doesn't swing further out.
                V.SteerInput = V.ForwardSpeed > 0.5f ? -_reverseSteer : _reverseSteer;
                V.DirectSteer = true;
                V.Handbrake = false;
                return;
            }
            float target;
            _wantSpeed = 0f;
            if (Mode == DriveMode.Pursuit || Mode == DriveMode.Flee || Mode == DriveMode.GoTo) target = DriveFree(dt);
            else target = DriveLanes(dt);
            CurrentTargetSpeed = target;
            ApplySpeed(target);
            // Stuck recovery (uses the speed wanted before the wall clamp, so cars nosed into a wall still back out).
            if (V.Speed < 0.5f && Mathf.Max(target, _wantSpeed) > 3f) _stuck += dt;
            else _stuck = Mathf.Max(0f, _stuck - dt);
            if (_stuck > 4.5f)
            {
                _stuck = 0f;
                _reverse = 1.6f;
                _reverseSteer = -Mathf.Sign(V.SteerInput == 0f ? 1f : V.SteerInput);
                if (Mode == DriveMode.Cruise) SnapToNearestLane();
            }
        }

        private void ApplySpeed(float target)
        {
            float fwd = V.ForwardSpeed;
            float err = target - fwd;
            if (target < 0.4f && fwd < 0.8f)
            {
                V.Throttle = 0f;
                V.Handbrake = true;
                return;
            }
            V.Handbrake = false;
            if (err > 0f) V.Throttle = Mathf.Clamp(err * 0.4f + 0.12f, 0f, 1f);
            else V.Throttle = Mathf.Clamp(err * 0.3f, -1f, 0f);
        }

        private void CheckThreats()
        {
            ThreatEvent e;
            if (!Threats.Query(V.transform.position, _threatSerial, out e))
            {
                _threatSerial = Threats.LatestSerial;
                return;
            }
            _threatSerial = Threats.LatestSerial;
            if (IgnoresThreats) return;
            if (e.Source == V.gameObject || (V.Driver != null && e.Source == V.Driver.gameObject)) return;
            switch (e.Kind)
            {
                case ThreatKind.Aim:
                    if (V.Speed < 5f) Bail(Random.value < 0.6f);
                    else Panic(15f);
                    break;
                case ThreatKind.Gunshot:
                case ThreatKind.Explosion:
                case ThreatKind.Fire:
                    if (V.Speed < 2f && Random.value < 0.35f) Bail(true);
                    else Panic(20f);
                    break;
            }
        }

        // ------------------------------------------------------------------ lane following

        private float DriveLanes(float dt)
        {
            if (Lane < 0 || Lane >= L.Lanes.Count)
            {
                SnapToNearestLane();
                if (Lane < 0) return 0f;
            }
            LanePath lane = L.Lanes[Lane];
            S = lane.Project(V.transform.position);
            if (S >= lane.Length - 0.8f)
            {
                if (_next < 0) _next = PickNext(Lane);
                if (_next < 0) { SnapToNearestLane(); return 0f; }
                Lane = _next;
                lane = L.Lanes[Lane];
                S = lane.Project(V.transform.position);
                _next = PickNext(Lane);
            }
            float speed = V.Speed;
            float look = Mathf.Clamp(4.5f + speed * 0.5f, 5f, 20f);
            Vector3 target = PointAhead(look);
            if (_overtakeTimer > 0f)
            {
                // Swing into the oncoming lane to pass a wreck or an abandoned car.
                _overtakeTimer -= dt;
                _overtakeTotal += dt;
                // Don't cut back in while still alongside the car being passed (gives up after 12 s).
                if (_overtakeTimer <= 0f && _ignore != null && !HasPassed(_ignore) && _overtakeTotal < 12f) _overtakeTimer = 0.25f;
                target -= MathUtil.RightOf(lane.DirectionAt(S)) * 3.4f;
                if (_overtakeTimer <= 0f) _ignore = null;
            }
            Steer(target, speed);

            float limit = lane.SpeedLimit * Personality * (Mode == DriveMode.Panic ? 1.5f : 1f);
            if (_overtakeTimer > 0f) limit = Mathf.Min(limit, 9f);
            // Slow for curves ahead.
            Vector3 d0 = DirAhead(1f);
            float previewDist = Mathf.Max(10f, speed * 1.6f);
            Vector3 d1 = DirAhead(previewDist);
            float turn = Vector3.Angle(d0, d1);
            if (turn > 4f)
            {
                float radius = previewDist / (turn * Mathf.Deg2Rad);
                limit = Mathf.Min(limit, Mathf.Sqrt((Mode == DriveMode.Panic ? 7f : 5f) * Mathf.Max(4f, radius)));
            }
            float follow = FollowSpeed(out bool blocked);
            float signal = Mode == DriveMode.Panic || IgnoreLights ? 999f : SignalSpeed(lane);
            float peds = PedestrianSpeed();
            float t = Mathf.Min(limit, Mathf.Min(follow, Mathf.Min(signal, peds)));
            // Waiting in a queue, at a light or for a pedestrian is not being stuck. Being nose to nose with a car
            // facing us (unless nearly done passing), or stopped out in the oncoming lane after a pass fell through,
            // is: then the stuck recovery backs us out after a few seconds and snaps us back to our own lane.
            bool committedPass = _overtakeTimer > 0f && (_ignore == null || PastMidpoint(_ignore));
            bool intruding = _overtakeTimer <= 0f && lane.LeftNeighbor < 0
                && Vector3.Dot(V.transform.position - lane.PointAt(S), MathUtil.RightOf(lane.DirectionAt(S))) < -1f;
            _wantSpeed = (_headOn && !committedPass) || intruding ? Mathf.Min(limit, Mathf.Min(signal, peds)) : t;
            // Blocked by a slow car: try a lane change, otherwise honk.
            if (blocked && limit > 8f && follow < 4f) _blocked += dt; else _blocked = 0f;
            if (_blocked > 2.5f && _laneCooldown <= 0f)
            {
                if (TryLaneChange(lane)) _blocked = 0f;
                else if (_honkCooldown <= 0f && Random.value < 0.5f)
                {
                    V.Honk(0.5f);
                    _honkCooldown = 6f;
                }
            }
            if (peds < 0.5f && speed < 0.5f && _honkCooldown <= 0f && Game.Player != null && !Game.Player.InVehicle)
            {
                V.Honk(0.6f);
                _honkCooldown = 5f;
            }
            return t;
        }

        private void Steer(Vector3 target, float speed)
        {
            Vector3 local = V.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, Mathf.Max(0.1f, local.z)) * Mathf.Rad2Deg;
            float max = V.MaxSteerAngleAt(speed);
            V.SteerInput = Mathf.Clamp(angle / Mathf.Max(1f, max), -1f, 1f);
            V.DirectSteer = true;
        }

        private Vector3 PointAhead(float dist)
        {
            LanePath lane = L.Lanes[Lane];
            float want = S + dist;
            if (want <= lane.Length || _next < 0) return lane.PointAt(Mathf.Min(want, lane.Length));
            LanePath n = L.Lanes[_next];
            return n.PointAt(Mathf.Min(want - lane.Length, n.Length));
        }

        private Vector3 DirAhead(float dist)
        {
            LanePath lane = L.Lanes[Lane];
            float want = S + dist;
            if (want <= lane.Length || _next < 0) return lane.DirectionAt(Mathf.Min(want, lane.Length));
            LanePath n = L.Lanes[_next];
            return n.DirectionAt(Mathf.Min(want - lane.Length, n.Length));
        }

        private float FollowSpeed(out bool blocked)
        {
            blocked = false;
            _headOn = false;
            float best = float.MaxValue;
            Vehicle ahead = null;
            Vector3 myPos = V.transform.position;
            Vector3 fwd = V.transform.forward;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle o = Vehicle.All[i];
                if (o == null || o == V || o == _ignore) continue;
                Vector3 rel = o.transform.position - myPos;
                float along = Vector3.Dot(rel, fwd);
                if (along < 0f || along > 48f) continue;
                if (rel.sqrMagnitude > 48f * 48f) continue;
                Vector3 path = PointAhead(along);
                // While passing a wreck, watch the oncoming lane we are driving in.
                if (_overtakeTimer > 0f) path -= MathUtil.RightOf(DirAhead(along)) * 3.4f;
                float lat = MathUtil.FlatDistance(o.transform.position, path);
                if (lat > 1.6f + o.Def.Width * 0.5f) continue;
                float gap = along - (V.Def.Length + o.Def.Length) * 0.5f;
                if (gap < best)
                {
                    best = gap;
                    ahead = o;
                }
            }
            if (ahead == null)
            {
                _deadAheadTimer = 0f;
                return 999f;
            }
            float desiredGap = 3f + V.Speed * (Mode == DriveMode.Panic ? 0.6f : 1.1f);
            // A driven car facing us in our path (empty cars and wrecks are handled by overtaking instead).
            _headOn = Vector3.Dot(ahead.transform.forward, fwd) < -0.3f && ahead.Driver != null && !ahead.IsDestroyed;
            if (_overtakeTimer > 0f && _headOn)
            {
                // Someone is coming the other way while we pass.
                bool committed = _ignore == null || PastMidpoint(_ignore);
                if (!committed && best < desiredGap + 10f)
                {
                    // Early in the pass: give up and back in behind the car we were going around. Reversing with the
                    // wheels turned the way we pulled out retraces the pull-out; allow time to brake first.
                    _overtakeTimer = 0f;
                    _ignore = null;
                    _deadAheadTimer = 0f;
                    _reverse = 1.4f + V.Speed / 6f;
                    _reverseSteer = Mathf.Clamp(Vector3.SignedAngle(DirAhead(0f), MathUtil.Flat(V.transform.forward), Vector3.up) / 15f, -1f, 1f);
                    blocked = true;
                    return 0f;
                }
                if (committed && best < desiredGap)
                {
                    // Nearly past: creep on and cut back in; the oncoming driver stops for us.
                    blocked = true;
                    return Mathf.Clamp(best - 1.5f, 0f, 2.5f);
                }
            }
            // A wreck or an abandoned car blocking the lane: wait a moment, then go around it when the oncoming lane is clear.
            bool stalled = (ahead.Driver == null || ahead.IsDestroyed) && ahead.Speed < 0.3f && best < 12f;
            _deadAheadTimer = stalled ? _deadAheadTimer + Time.fixedDeltaTime : 0f;
            if (_overtakeTimer <= 0f && _deadAheadTimer > 2.5f && OncomingClear(ahead))
            {
                _deadAheadTimer = 0f;
                _ignore = ahead;
                _overtakeTimer = 5f;
                _overtakeTotal = 0f;
            }
            float otherSpeed = Mathf.Max(0f, Vector3.Dot(ahead.Velocity, fwd));
            float v = otherSpeed + (best - desiredGap) * 0.55f;
            if (best < 2f) v = 0f;
            blocked = otherSpeed < 4f;
            return Mathf.Max(0f, v);
        }

        /// <summary>
        /// True when the oncoming lane beside us is free far enough ahead to pass a stalled car: nothing in it within
        /// 45 m, and no oncoming car close enough to arrive during the pass (about 5 s).
        /// </summary>
        private bool OncomingClear(Vehicle skip)
        {
            Vector3 myPos = V.transform.position;
            Vector3 fwd = V.transform.forward;
            // Lane frame at our position (our own heading may be a few degrees off after creeping up behind the wreck).
            Vector3 o0 = PointAhead(0f);
            Vector3 d0 = DirAhead(0f);
            Vector3 r0dir = MathUtil.RightOf(d0);
            // Parked cars and wrecks in the oncoming lane only matter inside the stretch the pass actually uses.
            float passEnd = skip != null
                ? Vector3.Dot(skip.transform.position - myPos, fwd) + skip.Def.Length * 0.5f + V.Def.Length + 6f
                : 20f;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle o = Vehicle.All[i];
                if (o == null || o == V || o == skip) continue;
                Vector3 rel = o.transform.position - myPos;
                if (rel.sqrMagnitude > 140f * 140f) continue;
                float along = Vector3.Dot(rel, fwd);
                if (along < -6f) continue;
                bool parked = (o.Driver == null || o.IsDestroyed) && o.Velocity.sqrMagnitude < 1f;
                if (parked && along > passEnd) continue;
                // Same corridor as FollowSpeed, so cars parked on the far curb don't count.
                float corridor = 1.6f + o.Def.Width * 0.5f;
                if (along <= 45f)
                {
                    float a = Mathf.Max(0f, along);
                    Vector3 path = PointAhead(a) - MathUtil.RightOf(DirAhead(a)) * 3.4f;
                    if (MathUtil.FlatDistance(o.transform.position, path) < corridor) return false;
                }
                // Faster oncoming cars further up the road (straight-line estimate of the oncoming lane).
                float closing = Mathf.Max(0f, -Vector3.Dot(o.Velocity, d0));
                if (closing > 1f)
                {
                    Vector3 r0 = o.transform.position - o0;
                    float alongLane = Vector3.Dot(r0, d0);
                    float lateral = Vector3.Dot(r0, r0dir);
                    if (alongLane > -6f && alongLane <= 45f + closing * 5f && Mathf.Abs(lateral + 3.4f) < corridor) return false;
                }
            }
            return true;
        }

        /// <summary>Our rear is clear of the car being passed, so it's safe to cut back in.</summary>
        private bool HasPassed(Vehicle o)
        {
            if (o == null) return true;
            float along = Vector3.Dot(o.transform.position - V.transform.position, V.transform.forward);
            return along < -(V.Def.Length + o.Def.Length) * 0.5f - 1.5f;
        }

        /// <summary>Our front bumper is past the middle of the car being passed.</summary>
        private bool PastMidpoint(Vehicle o)
        {
            if (o == null) return true;
            return Vector3.Dot(o.transform.position - V.transform.position, V.transform.forward) < V.Def.Length * 0.5f;
        }

        private float SignalSpeed(LanePath lane)
        {
            if (Game.Signals == null) return 999f;
            float dist = lane.Length - S - V.Def.Length * 0.5f;
            if (dist < -0.5f) return 999f;
            if (!lane.IsConnector && lane.EndsAtSignal && dist < 70f)
            {
                float tts = dist / Mathf.Max(1f, V.Speed);
                if (!Game.Signals.MayEnter(lane.NodeId, lane.AxisNS, tts))
                {
                    if (dist < 1.2f) return 0f;
                    return Mathf.Sqrt(2f * 4f * Mathf.Max(0f, dist - 1.2f));
                }
            }
            // Unsignaled junction: yield if someone is already crossing.
            if (!lane.IsConnector && !lane.EndsAtSignal && dist < 12f && lane.NodeId >= 0)
            {
                RoadNode node = L.Nodes[lane.NodeId];
                if (node.Edges.Count >= 3)
                {
                    for (int i = 0; i < Vehicle.All.Count; i++)
                    {
                        Vehicle o = Vehicle.All[i];
                        if (o == null || o == V || o.Speed < 1f) continue;
                        Vector3 p = o.transform.position;
                        if (Mathf.Abs(p.x - node.Pos.x) < node.HalfX + 1f && Mathf.Abs(p.z - node.Pos.z) < node.HalfZ + 1f)
                            return Mathf.Sqrt(2f * 4f * Mathf.Max(0f, dist - 1.5f));
                    }
                }
            }
            return 999f;
        }

        private float PedestrianSpeed()
        {
            float speed = V.Speed;
            float len = 3f + speed * 1.1f;
            Vector3 center = V.transform.position + V.transform.forward * (V.Def.Length * 0.5f + len * 0.5f) + Vector3.up;
            Vector3 half = new Vector3(V.Def.Width * 0.5f + 0.15f, 1f, len * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(center, half, _buf, V.transform.rotation, 1 << Layers.Characters, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _buf[i].GetComponentInParent<CharacterBase>();
                if (c == null || c.InVehicle || c.IsDead) continue;
                if (Mode == DriveMode.Panic && Random.value < 0.5f) continue;
                float d = Vector3.Distance(c.transform.position, V.transform.position) - V.Def.Length * 0.5f;
                return d < 2.5f ? 0f : Mathf.Sqrt(2f * 6f * Mathf.Max(0f, d - 2.5f));
            }
            return 999f;
        }

        private bool TryLaneChange(LanePath lane)
        {
            if (lane.IsConnector || lane.Length - S < 30f) return false;
            int[] options = { lane.LeftNeighbor, lane.RightNeighbor };
            foreach (int opt in options)
            {
                if (opt < 0 || opt >= L.Lanes.Count) continue;
                LanePath other = L.Lanes[opt];
                float s = other.Project(V.transform.position);
                Vector3 p = other.PointAt(s);
                bool clear = true;
                for (int i = 0; i < Vehicle.All.Count; i++)
                {
                    Vehicle o = Vehicle.All[i];
                    if (o == null || o == V) continue;
                    if ((o.transform.position - p).sqrMagnitude < 13f * 13f) { clear = false; break; }
                }
                if (!clear) continue;
                Lane = opt;
                S = s;
                _next = PickNext(opt);
                _laneCooldown = 7f;
                return true;
            }
            _laneCooldown = 2f;
            return false;
        }

        public void SnapToNearestLane()
        {
            if (L == null) return;
            float along;
            Vector3 pos = V.transform.position;
            int lane = L.NearestLane(pos, 25f, out along, true);
            if (lane >= 0)
            {
                // On the same road, prefer the lane heading our way (after a pass, a crash or a spin). Never jump to a
                // cross street's lane; when facing against the chosen lane, the steering turns us around.
                float hAlong;
                int heading = L.NearestLaneHeading(pos, 25f, V.transform.forward, out hAlong);
                if (heading >= 0 && L.Lanes[heading].EdgeId == L.Lanes[lane].EdgeId)
                {
                    lane = heading;
                    along = hAlong;
                }
            }
            if (lane < 0) lane = L.NearestLane(pos, 25f, out along, false);
            if (lane < 0) { Lane = -1; return; }
            Lane = lane;
            S = along;
            _next = PickNext(lane);
        }

        // ------------------------------------------------------------------ free driving (pursuit, flee, go-to)

        private float DriveFree(float dt)
        {
            // A pass in progress belongs to lane driving; don't resume it after a chase.
            _overtakeTimer = 0f;
            _ignore = null;
            Vector3 pos = V.transform.position;
            Vector3 goal;
            float distNow = 0f;
            if (Mode == DriveMode.Pursuit)
            {
                if (PursuitTarget == null || PursuitTarget.IsDead) { Mode = DriveMode.Cruise; SnapToNearestLane(); return 0f; }
                Vector3 tv = PursuitTarget.Velocity;
                distNow = MathUtil.FlatDistance(PursuitTarget.transform.position, pos);
                goal = PursuitTarget.transform.position + tv * Mathf.Clamp(distNow / 30f, 0f, 1.2f);
                WantsDeploy = distNow < 16f && (!PursuitTarget.InVehicle || PursuitTarget.CurrentVehicle.Speed < 3f);
                if (WantsDeploy && distNow < 9f) return 0f;
            }
            else if (Mode == DriveMode.Flee)
            {
                if (Destination == Vector3.zero || MathUtil.FlatDistance(Destination, pos) < 25f) Destination = PickFleeDestination();
                goal = Destination;
            }
            else
            {
                goal = Destination;
                if (!Racing && MathUtil.FlatDistance(goal, pos) < 8f) return 0f;
            }
            float dist = MathUtil.FlatDistance(goal, pos);
            bool direct = dist < 55f && !Physics.Linecast(pos + Vector3.up, goal + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            Vector3 steerTarget;
            if (direct)
            {
                steerTarget = goal;
            }
            else
            {
                _routeTimer -= dt;
                if (_routeTimer <= 0f || _route.Count == 0)
                {
                    _routeTimer = 2.5f;
                    _route = L.FindRoute(pos, goal);
                    _routeIndex = 1;
                }
                if (_route.Count < 2) steerTarget = goal;
                else
                {
                    float look = 9f + V.Speed * 0.45f;
                    while (_routeIndex < _route.Count - 1 && MathUtil.FlatDistance(_route[_routeIndex], pos) < look) _routeIndex++;
                    steerTarget = _route[Mathf.Min(_routeIndex, _route.Count - 1)];
                }
            }
            // Whiskers: steer away from walls ahead.
            Vector3 fwd = V.transform.forward;
            float wl = 10f + V.Speed * 0.6f;
            Vector3 origin = pos + Vector3.up * 0.8f;
            Vector3 left = Quaternion.Euler(0f, -22f, 0f) * fwd;
            Vector3 right = Quaternion.Euler(0f, 22f, 0f) * fwd;
            bool hl = Physics.Raycast(origin, left, wl, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            bool hr = Physics.Raycast(origin, right, wl, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            Steer(steerTarget, V.Speed);
            if (hl && !hr) V.SteerInput = Mathf.Clamp(V.SteerInput + 0.6f, -1f, 1f);
            else if (hr && !hl) V.SteerInput = Mathf.Clamp(V.SteerInput - 0.6f, -1f, 1f);
            Vector3 localT = V.transform.InverseTransformPoint(steerTarget);
            float angle = Mathf.Abs(Mathf.Atan2(localT.x, localT.z) * Mathf.Rad2Deg);
            float top = V.Def.TopSpeed * (Racing ? 0.86f : (Mode == DriveMode.GoTo ? 0.55f : 0.92f)) * SpeedScale;
            float speed = top;
            if (angle > 25f) speed = Mathf.Lerp(top, 9f, Mathf.InverseLerp(25f, 80f, angle));
            if (angle > 100f) speed = 6f;
            // Escorts following an NPC's vehicle keep a gap instead of ramming it.
            if (Mode == DriveMode.Pursuit && PursuitTarget != null && !PursuitTarget.IsPlayer && PursuitTarget.InVehicle && PursuitTarget.CurrentVehicle != null)
                speed = Mathf.Min(speed, Mathf.Max(0f, PursuitTarget.CurrentVehicle.Speed + (distNow - 14f) * 0.5f));
            _wantSpeed = speed;
            RaycastHit wall;
            if (Physics.Raycast(origin, fwd, out wall, Mathf.Max(8f, V.Speed * V.Speed / 14f), Layers.WorldMask, QueryTriggerInteraction.Ignore))
                speed = Mathf.Min(speed, Mathf.Sqrt(2f * 7f * Mathf.Max(0f, wall.distance - 3f)));
            return speed;
        }

        private Vector3 PickFleeDestination()
        {
            Vector3 away = MathUtil.Flat(V.transform.position - Game.PlayerPosition).normalized;
            if (away.sqrMagnitude < 0.01f) away = V.transform.forward;
            Vector3 p = V.transform.position + away * 300f;
            p.x = Mathf.Clamp(p.x, -CityLayout.RingHalf, CityLayout.RingHalf);
            p.z = Mathf.Clamp(p.z, -CityLayout.RingHalf, CityLayout.RingHalf);
            int node = L.NearestNode(p);
            return node >= 0 ? L.Nodes[node].Pos : p;
        }
    }
}
