using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Moving the creature without a NavMesh: kinematic ground movement with sphere-cast feelers around obstacles and a
    /// cached ground probe (one ray and its surface normal, re-used as a tilted plane for every foot the pose solver
    /// places), flight (circling, diving, landing, taking off) for winged species, burrowing for the Dune Wyrm (its
    /// transform marks the head; underground it ignores walls but never leaves the wilderness), flocking for mite swarms
    /// and drifting for shades. Also feeding the pose solver, at a lower rate for far or unseen creatures.
    /// </summary>
    public sealed partial class Creature
    {
        private CreaturePlan _plan;
        private bool _flies, _serpent, _wisp, _mite;
        private Vector3 _vel;
        private float _vy;
        private float _yaw;
        private float _yawRate;
        private float _turnRate = 360f;
        private bool _grounded = true;
        private bool _flying;
        private float _altitude;
        private float _depth;
        private bool _burrowed;
        private Vector3 _goal;
        private bool _hasGoal;
        private float _goalSpeed;
        private bool _faceTarget;
        // Ground cache: height at _gp and the slope of the surface there.
        private float _groundY;
        private float _gx, _gz;
        private Vector3 _gp;
        private float _groundTimer;
        private float _airGround;
        private float _airTimer;
        private float _climb;
        private float _avoidTimer;
        private float _avoidYaw;
        private float _stuckTimer;
        private Vector3 _lastPos;
        private float _lodTimer, _lodDt;
        private int _animFrame;
        private float _animDt;
        private int _spawnFrames = 6;
        private float _thinkTimer;
        private Func<float, float, float> _groundFn;
        // Flight.
        private float _orbitAngle;
        private int _airAttacks;
        private float _groundSince;
        private bool _landing;
        private Vector3 _landPoint;
        private bool _crashing;
        private int _landAfter = 2;
        // The wyrm.
        private float _surfaceUntil;
        private float _burrowT = -1f;
        private float _dustTimer;
        private float _rumbleTimer;

        private const float Gravity = 19.6f;
        private const float BurrowSeconds = 1.6f;
        /// <summary>How far the long ground ray reaches (a flyer spawned high up, a walker that stepped off a cliff).</summary>
        private const float FarProbe = 400f;
        private static readonly float[] AvoidTries = { 0f, 30f, -30f, 60f, -60f, 100f, -100f };

        private static int GroundMask { get { return Layers.WorldMask; } }
        private static int AvoidMask { get { return Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles); } }
        private float BurrowDepth { get { return Mathf.Max(1.5f, Size * 1.9f); } }

        private void InitMotion()
        {
            _groundFn = GroundY;
            Vector3 p = transform.position;
            // Flyers often start 14 to 22 m up: the long ray finds the real ground below, which is also their air floor.
            if (_flies) ProbeGroundFar(p);
            else ProbeGround(p, 0f, true);
            _airGround = _groundY;
            _orbitAngle = UnityEngine.Random.Range(0f, 360f);
            _landAfter = UnityEngine.Random.Range(2, 4);
            _circleDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            if (_flies && p.y > _groundY + 2.5f)
            {
                _flying = true;
                _altitude = p.y - _groundY;
            }
            else p.y = _groundY;
            if (_serpent)
            {
                _burrowed = true;
                _depth = BurrowDepth;
                if (CRig.Renderer != null) CRig.Renderer.enabled = false;
                CRig.SetCollidersEnabled(false);
            }
            transform.position = p;
            _lastPos = p;
        }

        /// <summary>World ground height near the creature (the pose solver's foot probe): the cached plane, no ray.</summary>
        private float GroundY(float x, float z)
        {
            float d = _gx * (x - _gp.x) + _gz * (z - _gp.z);
            return _groundY + Mathf.Clamp(d, -1.5f, 1.5f);
        }

        /// <summary>
        /// One ray down for the ground under p (rate limited); its normal gives the slope for nearby feet. When the short
        /// ray finds nothing (a walker stepped off a ledge, a body high in the air) a long one finds the ground far below.
        /// </summary>
        private void ProbeGround(Vector3 p, float dt, bool force)
        {
            _groundTimer -= dt;
            if (!force && _groundTimer > 0f) return;
            _groundTimer = IsEngaged || _atk >= 0 ? 0.07f : 0.25f;
            float up = Mathf.Clamp(Size * 0.7f, 0.9f, 3.5f);
            Vector3 from = new Vector3(p.x, p.y + up, p.z);
            RaycastHit hit;
            if (Physics.Raycast(from, Vector3.down, out hit, up + 6f, GroundMask, QueryTriggerInteraction.Ignore)
                || Physics.Raycast(from, Vector3.down, out hit, FarProbe, GroundMask, QueryTriggerInteraction.Ignore))
            {
                SetGround(hit);
            }
            else if (force)
            {
                _groundY = p.y;
                _gx = _gz = 0f;
                _gp = p;
            }
        }

        /// <summary>The long ray alone, at once (flyers placed in the air, bodies killed in flight).</summary>
        private void ProbeGroundFar(Vector3 p)
        {
            float up = Mathf.Clamp(Size * 0.7f, 0.9f, 3.5f);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(p.x, p.y + up, p.z), Vector3.down, out hit, FarProbe, GroundMask, QueryTriggerInteraction.Ignore)) SetGround(hit);
            else
            {
                _groundY = p.y;
                _gx = _gz = 0f;
                _gp = p;
            }
        }

        /// <summary>Caches a ground hit: its height, and its slope as a plane for the feet nearby.</summary>
        private void SetGround(RaycastHit hit)
        {
            _groundY = hit.point.y;
            Vector3 n = hit.normal;
            if (n.y > 0.35f)
            {
                _gx = Mathf.Clamp(-n.x / n.y, -1.2f, 1.2f);
                _gz = Mathf.Clamp(-n.z / n.y, -1.2f, 1.2f);
            }
            else _gx = _gz = 0f;
            _gp = hit.point;
        }

        private void SetGoal(Vector3 g, float speed)
        {
            _goal = g;
            _hasGoal = true;
            _goalSpeed = speed;
        }

        // ================================================================== per frame

        private void UpdateMotion(float dt)
        {
            _push = Vector3.MoveTowards(_push, Vector3.zero, dt * 8f);
            if (_mite && Group != null) Group.Tick();
            if (_burrowT >= 0f) UpdateBurrow(dt);
            Vector3 desired;
            bool locked = false;
            _faceTarget = false;
            if (IsStaggered || _burrowT >= 0f) desired = Vector3.zero;
            else if (_atk >= 0) desired = AttackMotion(dt, out locked);
            else desired = ModeSteer(dt) * PartSpeed;
            float prevYaw = _yaw;
            if (_mite) MiteMove(dt, desired);
            else if (_flying) FlyMove(dt, desired, locked);
            else if (_serpent && _burrowed && _atk < 0) BurrowMove(dt, desired);
            else GroundMove(dt, desired, locked);
            _yawRate = Mathf.DeltaAngle(prevYaw, _yaw) / Mathf.Max(0.0001f, dt);
        }

        private Vector3 Seek(Vector3 goal, float speed)
        {
            Vector3 to = goal - transform.position;
            if (!_flying) to.y = 0f;
            float d = to.magnitude;
            if (d < 0.1f) return Vector3.zero;
            return to / d * Mathf.Min(speed, d * 2.5f + 0.3f);
        }

        private Vector3 ModeSteer(float dt)
        {
            float walk = _sp != null ? _sp.WalkSpeed : 1.5f;
            float run = _sp != null ? _sp.RunSpeed : 5f;
            switch (Mode)
            {
                case CreatureMode.Hunt:
                    return HuntSteer(dt);
                case CreatureMode.Warn:
                    _faceTarget = Target != null;
                    return Vector3.zero;
                case CreatureMode.Stalk:
                    return StalkSteer(walk);
                case CreatureMode.Flee:
                {
                    Vector3 away = MathUtil.Flat(transform.position - _fleeFrom);
                    if (away.sqrMagnitude < 0.01f) away = MathUtil.DirFromYaw(_yaw);
                    Vector3 v = away.normalized * (_flying ? _sp.FlySpeed : run);
                    if (_flying) v.y = 3f;
                    return v;
                }
                case CreatureMode.Return:
                    if (_flying) return Seek(Home + Vector3.up * 2f, _sp.FlySpeed * 0.7f);
                    return Seek(Home, run * 0.7f);
                default:
                    if (_flying) return PatrolSteer(dt);
                    if (_hasGoal)
                    {
                        if (MathUtil.FlatSqrDistance(transform.position, _goal) < 1f + Radius * Radius) _hasGoal = false;
                        else return Seek(_goal, _goalSpeed);
                    }
                    return Vector3.zero;
            }
        }

        /// <summary>The fighting dance: close in when it may strike, otherwise hold a respectful distance and circle.</summary>
        private Vector3 HuntSteer(float dt)
        {
            CharacterBase t = Target;
            if (t == null) return Vector3.zero;
            if (_mite) return SwarmSteer(dt);
            float run = _sp != null ? _sp.RunSpeed : 5f;
            float walk = _sp != null ? _sp.WalkSpeed : 1.5f;
            Vector3 tp = t.InVehicle ? t.CurrentVehicle.transform.position : t.transform.position;
            if (_serpent && _burrowed) return Seek(FlightMath.DiveAim(tp, t.Velocity, 0.5f), run);
            if (_flying) return FlightHuntSteer(tp, dt);
            float edge = EdgeDistance(t);
            _faceTarget = edge < 14f;
            float engage = MeleeEngage();
            bool close = MayClose && Time.time >= _nextAttackAt - 0.6f;
            // 04E role hook (Creature.Role04E.cs): a ranged role keeps its distance and closes in only when cornered.
            if (_roleKeepRange > 0f) close = close && edge < _roleKeepRange * 0.4f;
            if (close)
            {
                if (edge > engage * 0.75f) return Seek(tp, edge > 6f ? run : Mathf.Max(walk * 1.6f, run * 0.45f));
                return Vector3.zero;
            }
            float hold = Mathf.Max(engage + 2.2f + Radius * 0.4f, _roleKeepRange);
            Vector3 pos = transform.position;
            if (Group != null && !Group.Swarm && Group.Members.Count > 1)
            {
                // Pack members take flanking slots around the target and drift round it.
                float slot = Group.SlotAngle(this) + Time.time * 12f * _circleDir;
                Vector3 want = tp + MathUtil.DirFromYaw(slot) * (hold + Radius);
                return Seek(want, edge > 8f ? run : walk * 1.4f);
            }
            _circleSwap -= dt;
            if (_circleSwap <= 0f)
            {
                _circleSwap = UnityEngine.Random.Range(1.8f, 3.8f);
                if (UnityEngine.Random.value < 0.4f) _circleDir = -_circleDir;
            }
            Vector3 n = MathUtil.Flat(tp - pos);
            float dist = n.magnitude;
            if (dist < 0.01f) return Vector3.zero;
            n /= dist;
            if (edge > hold + 6f) return n * run;
            Vector3 side = MathUtil.RightOf(n) * _circleDir;
            Vector3 radial = edge < hold - 0.6f ? -n : (edge > hold + 0.6f ? n : Vector3.zero);
            Vector3 v = side * 0.85f + radial * 1.1f;
            return v.sqrMagnitude > 0.001f ? v.normalized * walk * 1.1f : Vector3.zero;
        }

        /// <summary>How close a creature gets before it strikes: the longest of its frontal melee attacks.</summary>
        private float MeleeEngage()
        {
            float best = 0f;
            for (int i = 0; i < _attacks.Length; i++)
            {
                CreatureAttack a = _attacks[i];
                if (a.Shape == AttackShape.Arc && a.MinRange <= 0.01f && (a.Use & AttackUse.Ground) != 0) best = Mathf.Max(best, a.MaxRange);
            }
            return (best > 0f ? best : 1.5f) * PartMelee;
        }

        /// <summary>
        /// Stalkers creep toward a point behind the player, out of the camera's view. They follow what they last saw:
        /// break the line of sight and one heads for where you were, and can lose you.
        /// </summary>
        private Vector3 StalkSteer(float walk)
        {
            if (Target == null) return Vector3.zero;
            bool fresh = Time.time - _lastSeen < 1.5f;
            Vector3 tp = fresh ? Target.transform.position : _lastKnown;
            Vector3 look = Game.Cam != null ? Game.Cam.FlatForward : Target.transform.forward;
            Vector3 side = MathUtil.RightOf(look) * _circleDir;
            Vector3 want = fresh ? tp - look * 7f + side * 3f : tp;
            _faceTarget = fresh;
            return Seek(want, walk * 1.25f);
        }

        // ------------------------------------------------------------------ attacks in motion

        private Vector3 AttackMotion(float dt, out bool locked)
        {
            CreatureAttack a = _attacks[_atk];
            float w = _timing.Windup, act = _timing.Active;
            locked = _atkT >= w * 0.7f;
            _faceTarget = !locked && Target != null && a.Shape != AttackShape.Under;
            Vector3 fwd = MathUtil.DirFromYaw(_yaw);
            switch (a.Shape)
            {
                case AttackShape.Arc:
                    if (a.Lunge > 0f && _atkT > w * 0.6f && _atkT < w + act)
                    {
                        float dur = Mathf.Max(0.05f, w + act - w * 0.6f);
                        float speed = a.Lunge / dur;
                        if (Target != null && EdgeDistance(Target) < 0.25f) speed = 0f;
                        return fwd * speed;
                    }
                    return _flying ? fwd * 1f : Vector3.zero;
                case AttackShape.Leap:
                    if (_committed && _atkT < w + act) return _atkDir * _atkSpeed;
                    return Vector3.zero;
                case AttackShape.Line:
                    if (_flying)
                    {
                        float fly = _sp != null ? Mathf.Max(6f, _sp.FlySpeed) : 12f;
                        if (!_committed) return fwd * fly * 0.4f + Vector3.up * 3f;
                        if (_atkT < w + act) return _atkDir * _atkSpeed;
                        return fwd * fly * 0.8f + Vector3.up * 7f;
                    }
                    if (_committed && _atkT < w + act) return _atkDir * _atkSpeed;
                    return Vector3.zero;
                default:
                    return _flying ? fwd * 1.5f : Vector3.zero;
            }
        }

        // ------------------------------------------------------------------ on the ground

        private void GroundMove(float dt, Vector3 desired, bool locked)
        {
            Vector3 pos = transform.position;
            if (desired.sqrMagnitude > 0.01f && _atk < 0) desired = Avoid(pos, desired, dt);
            else if (desired.sqrMagnitude > 0.01f && Blocked(pos, desired)) desired = Vector3.zero;
            float heavy = 1f / Mathf.Sqrt(Mathf.Max(0.6f, Size));
            float accel = (desired.sqrMagnitude > _vel.sqrMagnitude ? 9f : 12f) * heavy * Mathf.Max(1f, desired.magnitude * 0.5f);
            if (_atk >= 0) accel *= 3f;
            _vel = Vector3.MoveTowards(_vel, desired, accel * dt);
            pos += _vel * dt + PushStep(pos, dt);
            ProbeGround(pos, dt, false);
            float gy = GroundY(pos.x, pos.z);
            if (_flies && _atk < 0 && !IsStaggered && pos.y - gy > 3f)
            {
                // Stepped off a ledge (the drake's canyon roost) or shoved off one: spread the wings instead of dropping.
                _airGround = gy;
                _airTimer = 0f;
                TakeOff();
            }
            if (pos.y - gy > 0.6f)
            {
                _vy -= Gravity * dt;
                pos.y = Mathf.Max(gy, pos.y + _vy * dt);
                _grounded = pos.y <= gy + 0.02f;
            }
            else
            {
                pos.y = Mathf.Lerp(pos.y, gy, MathUtil.Damp(18f, dt));
                _vy = 0f;
                _grounded = true;
            }
            if (pos.y < WorldBounds.KillY)
            {
                Despawn(false);
                return;
            }
            Face(pos, dt, locked);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
            Stuck(pos, dt, desired);
        }

        private void Face(Vector3 pos, float dt, bool locked)
        {
            if (locked) return;
            float want = _yaw;
            if (_faceTarget && Target != null)
            {
                Vector3 to = MathUtil.Flat(Target.transform.position - pos);
                if (to.sqrMagnitude > 0.01f) want = MathUtil.YawOf(to);
            }
            else if (_vel.sqrMagnitude > 0.05f) want = MathUtil.YawOf(_vel);
            float rate = _turnRate * (_atk >= 0 ? 0.65f : 1f);
            _yaw = Mathf.MoveTowardsAngle(_yaw, want, rate * dt);
        }

        /// <summary>Steers around walls, cars and props with sphere-cast feelers (refreshed a few times a second).</summary>
        private Vector3 Avoid(Vector3 pos, Vector3 desired, float dt)
        {
            float speed = desired.magnitude;
            if (speed < 0.05f) return desired;
            Vector3 dir = desired / speed;
            _avoidTimer -= dt;
            if (_avoidTimer <= 0f)
            {
                _avoidTimer = IsEngaged ? 0.12f : 0.22f;
                float r = Mathf.Clamp(Radius * 0.5f, 0.12f, 1.4f);
                Vector3 origin = pos + Vector3.up * Mathf.Clamp(Size * 0.45f, 0.25f, 1.8f);
                float look = Radius + 0.6f + speed * 0.45f;
                _avoidYaw = 180f;
                for (int i = 0; i < AvoidTries.Length; i++)
                {
                    Vector3 alt = QMath.AY(AvoidTries[i]) * dir;
                    if (!Physics.SphereCast(origin, r, alt, out _hit, look, AvoidMask, QueryTriggerInteraction.Ignore))
                    {
                        _avoidYaw = AvoidTries[i];
                        break;
                    }
                }
                if (_avoidYaw >= 179f) _hasGoal = false;
            }
            if (_avoidYaw == 0f) return desired;
            return QMath.AY(_avoidYaw) * desired;
        }

        private RaycastHit _hit;

        /// <summary>
        /// This frame's share of a shove (a car, a blast, the Gravity Well), stopped at walls: a sphere swept along the
        /// push keeps the body on this side of anything solid, and the part of the push driving into it is dropped.
        /// </summary>
        private Vector3 PushStep(Vector3 pos, float dt)
        {
            Vector3 step = _push * dt;
            Vector3 flat = MathUtil.Flat(step);
            float d = flat.magnitude;
            if (d < 1e-4f) return step;
            Vector3 dir = flat / d;
            float r = Mathf.Clamp(Radius * 0.45f, 0.08f, 1.2f);
            // The sphere rides a hand above the feet, so kerbs and seams in the ground do not count as walls.
            Vector3 origin = pos + Vector3.up * Mathf.Max(r + 0.15f, Mathf.Clamp(Size * 0.45f, 0.2f, 1.8f));
            RaycastHit hit;
            if (!Physics.SphereCast(origin, r, dir, out hit, d + 0.05f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return step;
            _push = CreatureFightRules.SlideAlongWall(_push, hit.normal);
            return dir * Mathf.Max(0f, hit.distance - 0.05f) + Vector3.up * step.y;
        }

        /// <summary>A rush or lunge stops at a wall instead of passing through it.</summary>
        private bool Blocked(Vector3 pos, Vector3 desired)
        {
            float speed = desired.magnitude;
            if (speed < 0.05f) return false;
            Vector3 origin = pos + Vector3.up * Mathf.Clamp(Size * 0.45f, 0.25f, 1.8f);
            return Physics.SphereCast(origin, Mathf.Clamp(Radius * 0.45f, 0.1f, 1.2f), desired / speed, out _hit, Radius * 0.6f + speed * 0.12f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Trying to move but not getting anywhere: pick another way.</summary>
        private void Stuck(Vector3 pos, float dt, Vector3 desired)
        {
            if (desired.sqrMagnitude > 0.25f)
            {
                if ((pos - _lastPos).magnitude < 0.15f * dt * desired.magnitude) _stuckTimer += dt;
                else _stuckTimer = 0f;
                if (_stuckTimer > 1.6f)
                {
                    _stuckTimer = 0f;
                    _hasGoal = false;
                    _circleDir = -_circleDir;
                    if (Mode == CreatureMode.Wander || Mode == CreatureMode.Return)
                    {
                        Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * 6f;
                        SetGoal(pos + new Vector3(r.x, 0f, r.y), _sp != null ? _sp.WalkSpeed : 1.5f);
                    }
                }
            }
            else _stuckTimer = 0f;
            _lastPos = pos;
        }

        // ------------------------------------------------------------------ flight

        /// <summary>Calm flyers circle their home at cruising height (griffins by day, the wyvern always).</summary>
        private Vector3 PatrolSteer(float dt)
        {
            Vector3 habitatVelocity;
            if (TerritoryPatrolSteer(dt, out habitatVelocity)) return habitatVelocity;
            float fly = _sp != null ? _sp.FlySpeed : 10f;
            float radius = 22f + Size * 3f;
            _orbitAngle += FlightMath.OrbitRate(radius, fly * 0.6f) * dt;
            Vector3 goal = FlightMath.OrbitPoint(Home, radius, _orbitAngle, (_sp != null ? _sp.CruiseHeight : 14f) + Mathf.Sin(Time.time * 0.3f + Seed) * 3f);
            goal.y = Mathf.Max(goal.y, _airGround + 8f);
            Vector3 d = goal - transform.position;
            return d.sqrMagnitude > 0.01f ? d.normalized * fly * 0.6f : Vector3.zero;
        }

        /// <summary>Hunting from the air: circle above the target, then land near it to fight on the ground.</summary>
        private Vector3 FlightHuntSteer(Vector3 tp, float dt)
        {
            float fly = _sp != null ? _sp.FlySpeed : 10f;
            if (_landing)
            {
                Vector3 to = _landPoint - transform.position;
                float flat = MathUtil.Flat(to).magnitude;
                Vector3 v = MathUtil.Flat(to).normalized * Mathf.Min(fly * 0.6f, flat * 1.2f + 1f);
                v.y = flat < 10f ? Mathf.Clamp(to.y * 1.2f, -7f, 2f) : Mathf.Clamp(to.y * 0.3f, -4f, 3f);
                _faceTarget = flat < 6f;
                return v;
            }
            float radius = 14f + Size * 2.5f;
            _orbitAngle += FlightMath.OrbitRate(radius, fly * 0.85f) * dt * _circleDir;
            Vector3 goal = FlightMath.OrbitPoint(tp, radius, _orbitAngle, _sp != null ? _sp.CruiseHeight : 14f);
            goal.y = Mathf.Max(goal.y, _airGround + 8f);
            Vector3 d = goal - transform.position;
            return d.sqrMagnitude > 0.01f ? d.normalized * fly * 0.85f : Vector3.zero;
        }

        private void ThinkFlight(float dist, float tdt)
        {
            if (_flying)
            {
                bool tired = _airAttacks >= _landAfter || (HealthFraction < 0.4f && UnityEngine.Random.value < 0.3f);
                if (!_landing && tired && dist < 45f && Target != null) BeginLanding(Target.transform.position);
            }
            else
            {
                _groundSince += tdt;
                if ((_groundSince > 22f || dist > 30f) && _atk < 0 && !IsStaggered) TakeOff();
            }
        }

        private void TakeOff()
        {
            if (!_flies || _flying || _dead) return;
            // Torn wings: it fights on the ground for good (CreatureParts).
            if (PartBroken(PartEffect.Grounded)) return;
            _flying = true;
            _landing = false;
            _crashing = false;
            _airAttacks = 0;
            _landAfter = UnityEngine.Random.Range(2, 4);
            _vel += Vector3.up * 7f;
            CreatureAudio.Play(_sp, CreatureCry.Wings, transform.position, Size);
        }

        private void BeginLanding(Vector3 near)
        {
            if (!_flying) return;
            _landing = true;
            Vector3 off = MathUtil.Flat(transform.position - near);
            off = off.sqrMagnitude > 0.01f ? off.normalized * (6f + Radius) : Vector3.forward * 6f;
            _landPoint = near + off;
            RaycastHit hit;
            if (Physics.Raycast(_landPoint + Vector3.up * 60f, Vector3.down, out hit, 200f, GroundMask, QueryTriggerInteraction.Ignore)) _landPoint = hit.point;
        }

        /// <summary>Drops a flyer out of the air (a stagger, a wing hit) to fight on the ground.</summary>
        private void ForceLanding(bool crash)
        {
            if (!_flying) return;
            _crashing = crash;
            BeginLanding(transform.position);
            _landPoint = new Vector3(transform.position.x, _airGround, transform.position.z);
        }

        private void FlyMove(float dt, Vector3 desired, bool locked)
        {
            Vector3 pos = transform.position;
            _airTimer -= dt;
            if (_airTimer <= 0f)
            {
                _airTimer = 0.2f;
                RaycastHit hit;
                if (Physics.Raycast(pos + Vector3.up, Vector3.down, out hit, 400f, GroundMask, QueryTriggerInteraction.Ignore)) _airGround = hit.point.y;
                // Climb over anything tall ahead (towers in the Inner World, canyon walls).
                Vector3 fwd = _vel.sqrMagnitude > 1f ? _vel.normalized : MathUtil.DirFromYaw(_yaw);
                _climb = Physics.Raycast(pos, fwd, 14f + Radius, GroundMask, QueryTriggerInteraction.Ignore) ? 1f : Mathf.MoveTowards(_climb, 0f, 0.5f);
            }
            if (_climb > 0f && !_landing && !(_atk >= 0 && _committed)) desired.y = Mathf.Max(desired.y, 8f * _climb);
            if (_crashing) desired = new Vector3(_vel.x * 0.5f, -12f, _vel.z * 0.5f);
            float accel = (_atk >= 0 && _committed ? 30f : 9f) / Mathf.Sqrt(Mathf.Max(0.8f, Size));
            _vel = Vector3.MoveTowards(_vel, desired, accel * Mathf.Max(1f, desired.magnitude * 0.3f) * dt);
            pos += _vel * dt + PushStep(pos, dt);
            float floor = _airGround + (_landing ? 0f : 3f + Size * 0.5f);
            bool diving = _atk >= 0 && _committed && _attacks[_atk].Shape == AttackShape.Line;
            if (diving) floor = _airGround + Mathf.Max(0.6f, Size * 0.4f);
            if (pos.y < floor)
            {
                pos.y = Mathf.Lerp(pos.y, floor, MathUtil.Damp(_landing ? 30f : 6f, dt));
                if (_vel.y < 0f) _vel.y = 0f;
            }
            _altitude = Mathf.Max(0f, pos.y - _airGround);
            if (!locked)
            {
                float want = _yaw;
                if (_faceTarget && Target != null) want = MathUtil.YawOf(MathUtil.Flat(Target.transform.position - pos));
                else if (MathUtil.Flat(_vel).sqrMagnitude > 0.5f) want = MathUtil.YawOf(_vel);
                _yaw = Mathf.MoveTowardsAngle(_yaw, want, _turnRate * 0.6f * dt);
            }
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
            if (_landing && _altitude < 0.35f && MathUtil.FlatDistance(pos, _landPoint) < 3f + Radius)
            {
                _flying = false;
                _landing = false;
                _groundSince = 0f;
                _vel = MathUtil.Flat(_vel) * 0.3f;
                _groundY = _airGround;
                _gp = pos;
                _gx = _gz = 0f;
                if (Game.Fx != null) Game.Fx.GroundDust(pos, Mathf.Clamp(Size * 0.5f, 0.5f, 3f), 8);
                CreatureAudio.Play(_sp, CreatureCry.Step, pos, Size);
                if (_crashing)
                {
                    _crashing = false;
                    Stagger(1.6f);
                }
            }
        }

        // ------------------------------------------------------------------ the wyrm under the sand

        private void BurrowMove(float dt, Vector3 desired)
        {
            Vector3 pos = transform.position;
            _vel = Vector3.MoveTowards(_vel, desired, 10f * dt);
            Vector3 next = pos + _vel * dt;
            if (!CityLayout.IsWilderness(CityLayout.DistrictAt(next)))
            {
                _vel = -_vel * 0.5f;
                next = pos;
                _hasGoal = false;
            }
            ProbeGround(next, dt, false);
            next.y = GroundY(next.x, next.z);
            if (_vel.sqrMagnitude > 0.2f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(_vel), _turnRate * 1.4f * dt);
            transform.SetPositionAndRotation(next, Quaternion.Euler(0f, _yaw, 0f));
            _depth = Mathf.MoveTowards(_depth, BurrowDepth, dt * 2f);
            if (_vel.sqrMagnitude > 2f) DustTrail(dt);
        }

        /// <summary>Sand thrown up where the wyrm's head travels under the surface, and the rumble that gives it away.</summary>
        private void DustTrail(float dt)
        {
            _dustTimer -= dt;
            if (_dustTimer > 0f) return;
            _dustTimer = 0.1f;
            float d = Vector3.Distance(transform.position, Game.PlayerPosition);
            if (d > 160f) return;
            if (Game.Fx != null) Game.Fx.SandBurst(transform.position + Vector3.up * 0.2f, Mathf.Clamp(Size * 0.6f, 0.6f, 2.2f), 2);
            _rumbleTimer -= 0.1f;
            if (_rumbleTimer <= 0f && d < 70f)
            {
                _rumbleTimer = 1.4f;
                CreatureAudio.Play(_sp, CreatureCry.Rumble, transform.position, Size);
                if (Game.Cam != null && d < 30f) Game.Cam.Shake(Mathf.Clamp01(0.12f * (1f - d / 30f)));
            }
        }

        /// <summary>The eruption broke the surface: the wyrm fights above ground for a while.</summary>
        private void Surface()
        {
            _burrowed = false;
            _surfaceUntil = Time.time + UnityEngine.Random.Range(8f, 12f);
            if (CRig.Renderer != null) CRig.Renderer.enabled = true;
            CRig.SetCollidersEnabled(true);
            _depth = 0f;
        }

        private void ThinkSurfacing()
        {
            if (_burrowed || _burrowT >= 0f || _atk >= 0) return;
            if (Time.time > _surfaceUntil) StartBurrow();
        }

        private void StartBurrow()
        {
            _burrowT = 0f;
            CreatureAudio.Play(_sp, CreatureCry.Rumble, transform.position, Size);
        }

        private void UpdateBurrow(float dt)
        {
            _burrowT += dt;
            if (_burrowT < BurrowSeconds) return;
            _burrowT = -1f;
            _burrowed = true;
            _depth = BurrowDepth;
            if (CRig.Renderer != null) CRig.Renderer.enabled = false;
            CRig.SetCollidersEnabled(false);
            if (Game.Fx != null) Game.Fx.SandBurst(transform.position, Mathf.Clamp(Size, 1f, 3f), 12);
            ReleaseTokens();
        }

        // ------------------------------------------------------------------ mites

        private Vector3 SwarmSteer(float dt)
        {
            CreatureGroup g = Group;
            float speed = _sp != null ? _sp.RunSpeed : 7f;
            if (g == null || Target == null) return Target != null ? Seek(Target.transform.position, speed) : Vector3.zero;
            // The first mite on the hunt leads the waves (the first one alive may still be calm or on its way home).
            if (g.WaveLeader() == this) UpdateWave(g);
            Vector3 target = Target.InVehicle ? Target.CurrentVehicle.transform.position + Vector3.up : Target.transform.position + Vector3.up * 0.7f;
            bool wave = g.WaveActive;
            float orbit = wave ? 0.5f : 3.2f;
            float phase = g.OrbitPhase + (Seed & 15) * 0.4f;
            return SwarmMath.Steer(transform.position, g.Positions, g.Count, g.Center, g.Velocity, target, speed, 0.6f, orbit, phase);
        }

        /// <summary>The swarm attacks in waves: it takes the attack token, darts in for a few seconds, then backs off.</summary>
        private void UpdateWave(CreatureGroup g)
        {
            float now = Time.time;
            if (g.WaveActive) return;
            if (_hasToken)
            {
                CombatDirector.ReleaseCreatureToken(this);
                _hasToken = false;
                g.NextSwoop = now + CreatureBrainLogic.RestBetweenAttacks((int)Settings.Difficulty, UnityEngine.Random.value, 1.4f);
                return;
            }
            if (now < g.NextSwoop) return;
            if (Target.IsPlayer)
            {
                if (!CombatDirector.TryCreatureToken(this)) return;
                _hasToken = true;
            }
            g.WaveEnd = now + 3.2f;
        }

        private void MiteMove(float dt, Vector3 desired)
        {
            Vector3 pos = transform.position;
            bool airborne = Mode == CreatureMode.Hunt || Mode == CreatureMode.Flee;
            _vel = Vector3.MoveTowards(_vel, desired, 30f * dt);
            pos += _vel * dt + PushStep(pos, dt);
            ProbeGround(pos, dt, false);
            float gy = GroundY(pos.x, pos.z);
            if (!airborne) pos.y = Mathf.Lerp(pos.y, gy, MathUtil.Damp(14f, dt));
            else pos.y = Mathf.Clamp(pos.y, gy + 0.12f, gy + 4f);
            _flying = false;
            _grounded = !airborne;
            _altitude = pos.y - gy;
            if (MathUtil.Flat(_vel).sqrMagnitude > 0.05f) _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(_vel), 900f * dt);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
        }

        // ================================================================== animation

        private void Animate(float dt, float playerDist)
        {
            _animDt += dt;
            float camD = Game.Cam != null ? Vector3.Distance(Game.Cam.transform.position, transform.position) : playerDist;
            bool visible = CRig.Renderer == null || !CRig.Renderer.enabled || CRig.Renderer.isVisible || _spawnFrames > 0;
            int every = camD < 35f ? 1 : (camD < 70f ? 2 : (camD < 140f ? 3 : 6));
            if (!visible) every = Mathf.Max(every, 8);
            if (_spawnFrames > 0) { _spawnFrames--; every = 1; }
            _animFrame++;
            if (every > 1 && ((_animFrame + (Seed & 7)) % every) != 0) return;
            float adt = Mathf.Min(_animDt, 0.1f);
            _animDt = 0f;
            CreaturePoseInput p = CreaturePoseInput.Idle(transform.position, _yaw);
            p.YawRate = _yawRate;
            p.Velocity = _vel;
            p.Grounded = _grounded && !_flying;
            p.Flying = _flying || (_mite && !_grounded);
            p.Altitude = _altitude;
            // The erupting wyrm stays at its depth through the wind-up and rises only as it bursts out.
            p.Depth = _serpent ? (_atk >= 0 && _attacks[_atk].Shape == AttackShape.Under && _committed ? 0f : _depth) : 0f;
            if (_dead)
            {
                p.Action = CreatureAction.Death;
                p.ActionT = _actionT;
            }
            else if (_burrowT >= 0f)
            {
                p.Action = CreatureAction.Burrow;
                p.ActionT = Mathf.Clamp01(_burrowT / BurrowSeconds);
                p.Windup = 0.4f;
                p.Strike = 0.7f;
            }
            else if (IsStaggered)
            {
                p.Action = CreatureAction.Stagger;
                p.ActionT = StaggerProgress;
                p.Windup = 0.25f;
                p.Strike = 0.5f;
            }
            else if (_atk >= 0)
            {
                p.Action = _attacks[_atk].Action;
                p.ActionT = _timing.ActionT(_atkT);
                p.Windup = _timing.WindupFrac;
                p.Strike = _timing.StrikeFrac;
            }
            p.Alert = Mode == CreatureMode.Hunt || Mode == CreatureMode.Warn ? 1f : (Mode == CreatureMode.Stalk ? 0.6f : 0f);
            p.Hurt = _hurt;
            p.HurtDirection = _hurtDir;
            if (Target != null && !_dead && Mode != CreatureMode.Wander && Mode != CreatureMode.Idle)
            {
                p.HasLookTarget = true;
                p.LookTarget = Target.ChestPosition;
            }
            p.GroundHeight = _groundFn;
            p.Time = Time.time;
            ApplyHabitatLook(ref p);
            CRig.Pose.Solve(ref p, adt);
            CRig.ApplyPose();
            Footfalls(camD);
            float glow = _atk >= 0 ? _glow : (IsStaggered ? 0.5f : 1f);
            if (Mode == CreatureMode.Warn) glow += 0.6f + 0.6f * Mathf.Sin(Time.time * 7f);
            if (Elite) glow += 0.4f + 0.3f * Mathf.Sin(Time.time * 5f);
            CRig.SetGlow(glow);
            if (_wisp) CRig.SetVisibility(Visibility, Health.SurfaceTint);
        }

        /// <summary>Heavy feet thud and kick up dust; the wyrm's slams and eruptions shake the ground.</summary>
        private void Footfalls(float camD)
        {
            int mask = CRig.Pose.FootPlantMask;
            if (mask == 0 || camD > 45f) return;
            if (_serpent)
            {
                if (Game.Fx != null) Game.Fx.SandBurst(CRig.HeadPoint.position, Mathf.Clamp(Size, 1f, 3f), 8);
                if (Game.Cam != null && camD < 30f) Game.Cam.Shake(0.25f * (1f - camD / 30f));
                return;
            }
            if (Size < 1.2f || _flying) return;
            LegChain[] legs = CRig.Body.Skeleton.Legs;
            for (int i = 0; i < legs.Length && i < 30; i++)
            {
                if ((mask & (1 << i)) == 0 || legs[i].Toe < 0) continue;
                Vector3 foot = CRig.Bones[legs[i].Toe].position;
                if (Game.Fx != null) Game.Fx.GroundDust(foot, Mathf.Clamp(Size * 0.18f, 0.2f, 0.8f), 2);
                CreatureAudio.Play(_sp, CreatureCry.Step, foot, Size);
                if (Size > 2.2f && Game.Cam != null && camD < 22f) Game.Cam.Shake(0.05f * (1f - camD / 22f));
                break;
            }
        }
    }
}
