using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Companion movement. Following uses the player's breadcrumb trail (the same doors, stairwells and corridors the
    /// player took) and steers straight to its spot beside the player only when that way is close, on the same floor,
    /// clear of walls and over solid ground. Walkers probe the floor under them (stairs, ramps, drops), hop short heat
    /// gaps and ledges, and wait at anything bigger; the drakeling flutters across gaps and after a flying player; the
    /// skimmer hovers at its height under any ceiling. Getting stuck escalates through a bounded repath (skip ahead on
    /// the trail, re-find it) to a recall: automatic recalls happen only off camera and land on verified ground
    /// (floor under it, room for the body, the same floor and a clear line to the player, no heat). When no such point
    /// exists the reason is shown instead.
    /// </summary>
    public sealed partial class CompanionController
    {
        /// <summary>The player's breadcrumb trail (owned by the bond system).</summary>
        public CompanionTrail Trail;
        public bool Flying { get; private set; }
        public Vector3 Velocity { get { return _vel; } }
        public float Yaw { get { return _yaw; } }
        /// <summary>Where it stays in Wait mode.</summary>
        public Vector3 WaitPoint;
        /// <summary>A fight is on near the player (closer follow, alert stance).</summary>
        public bool Combat;
        /// <summary>The companion's display name for its reasons.</summary>
        public string DisplayName = "";
        /// <summary>Why it cannot follow right now ("" when it can). The HUD shows it.</summary>
        public string BlockReason { get; private set; } = "";
        /// <summary>It asks the bond system for a recall (stuck, lost the trail, far behind).</summary>
        public bool WantsRecall { get; private set; }
        public bool GoalReached { get; private set; }
        public float RecallCooldownLeft { get { return _recallCooldown; } }

        private Vector3 _vel;
        private float _vy;
        private float _yaw, _yawRate;
        private float _groundY;
        private bool _hasGround;
        private float _groundTimer;
        private float _ceilingY = float.PositiveInfinity;
        private float _ceilingTimer;
        private Vector3 _lastPos;
        private bool _direct;
        private float _directTimer;
        private int _crumbId;
        private StuckMeter _stuck;
        private float _hopT = -1f, _hopDur;
        private Vector3 _hopFrom, _hopTo;
        private Vector3 _goal;
        private float _goalSpeed;
        private bool _goalFly;
        private float _hazardTimer;
        private float _hazardDamage;
        private float _recallCooldown;
        private Func<Vector3, Vector3, bool> _clearFn;

        private static readonly List<Vector3> _offsets = new List<Vector3>();

        public static void ResetStatics() { _offsets.Clear(); }

        private bool CanFly { get { return CompanionArchetypes.CanFly(Profile); } }

        private void InitMotion(Vector3 pos, float yaw)
        {
            _yaw = yaw;
            _groundY = pos.y;
            _lastPos = pos;
            _clearFn = ClearPath;
            ProbeGround(pos, true);
            Flying = Profile.Locomotion == CompanionLocomotion.Hover;
        }

        /// <summary>Gives the body a goal for Goal mode (assist positions, relays, mechanisms).</summary>
        public void SetGoal(Vector3 goal, float speed, bool fly)
        {
            _goal = goal;
            _goalSpeed = Mathf.Max(0.5f, speed);
            _goalFly = fly && CanFly;
            GoalReached = false;
            Mode = CompanionMode.Goal;
        }

        /// <summary>Heat damage gathered since the last call (health points); the bond system applies it to the record.</summary>
        public float ConsumeHazardDamage()
        {
            float d = _hazardDamage;
            _hazardDamage = 0f;
            return d;
        }

        public void ClearBlock()
        {
            BlockReason = "";
            WantsRecall = false;
        }

        private void Block(string reason, bool recall)
        {
            BlockReason = reason ?? "";
            if (recall) WantsRecall = true;
        }

        // ================================================================== per frame

        private void TickMotion(float dt)
        {
            _recallCooldown = Mathf.Max(0f, _recallCooldown - dt);
            Vector3 pos = transform.position;
            switch (Mode)
            {
                case CompanionMode.Follow: FollowStep(dt, ref pos); break;
                case CompanionMode.Wait: HoldStep(dt, ref pos); break;
                case CompanionMode.Goal: GoalStep(dt, ref pos); break;
                default: SettleStep(dt, ref pos); break;
            }
            if (Mode != CompanionMode.Rescue)
            {
                _hazardTimer -= dt;
                if (_hazardTimer <= 0f)
                {
                    _hazardTimer = 0.25f;
                    float heat;
                    if (Game.Underheart != null && Game.Underheart.HazardAt(pos + Vector3.up * 0.2f, out heat) && heat > 0f) _hazardDamage += heat * 0.25f;
                }
            }
            if (!MathUtil.IsFinite(pos)) pos = _lastPos;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
            _lastPos = pos;
        }

        private void FollowStep(float dt, ref Vector3 pos)
        {
            PlayerController pl = Game.Player;
            if (pl == null)
            {
                SettleStep(dt, ref pos);
                return;
            }
            Vector3 pp = pl.transform.position;
            float dist = DistanceToPlayer(pos, pp);
            bool sameLayer = WorldBounds.SameLayer(pos, pp);
            if (CompanionFollowRules.NeedsCatchUp(dist) || (!sameLayer && dist > 14f))
                Block(DisplayName + " fell behind. It catches up out of sight, or hold N to call it.", true);
            Vector3 slot = CompanionFollowRules.Slot(pp, pl.transform.forward, Profile.Side, Profile.FollowDistance + (Combat ? -0.4f : 0f));
            _directTimer -= dt;
            if (_directTimer <= 0f)
            {
                _directTimer = 0.15f;
                bool near = dist < CompanionFollowRules.DirectRange && sameLayer && Mathf.Abs(pos.y - pp.y) < (CanFly ? 3f : 1.4f);
                _direct = near && ClearPath(pos, slot) && !HazardPoint(slot);
            }
            Vector3 target;
            bool air = false;
            if (_direct)
            {
                target = slot;
                _crumbId = 0;
                if (!WantsRecall || dist < CompanionFollowRules.CatchUpDistance) ClearBlock();
            }
            else if (!TrailTarget(pos, out target, out air))
            {
                if (string.IsNullOrEmpty(BlockReason)) Block(DisplayName + " lost your trail. It catches up out of sight, or hold N to call it.", true);
                SteerTo(dt, ref pos, pos, 0f, false);
                return;
            }
            bool close = dist <= Profile.FollowDistance + 0.6f;
            bool los = _direct || (close && !Physics.Linecast(pos + Vector3.up * 0.4f, pp + Vector3.up * 0.9f, Layers.WorldMask, QueryTriggerInteraction.Ignore));
            bool idle = CompanionFollowRules.ShouldIdle(dist, Profile.FollowDistance + (_direct ? 0f : -0.2f), los, sameLayer);
            float speed = idle ? 0f : CompanionFollowRules.Speed(dist, Profile, Combat);
            Vector3 before = pos;
            float reach = new Vector3(target.x - pos.x, 0f, target.z - pos.z).magnitude;
            SteerTo(dt, ref pos, target, speed, air);
            if (idle)
            {
                _stuck.Reset();
                Vector3 face = pl.transform.forward;
                float want = MathUtil.YawOf(new Vector3(face.x, 0f, face.z));
                _yaw = Mathf.MoveTowardsAngle(_yaw, want, Profile.TurnRate * 0.35f * dt);
                SetLook(pl.ChestPosition + pl.transform.forward * 4f);
                return;
            }
            ClearLook();
            Vector3 toward = new Vector3(target.x - before.x, 0f, target.z - before.z);
            float moved = toward.sqrMagnitude > 1e-6f ? Vector3.Dot(new Vector3(pos.x - before.x, 0f, pos.z - before.z), toward.normalized) : 0f;
            if (Flying) moved = Mathf.Max(moved, (pos - before).magnitude * 0.6f);
            if (_hopT >= 0f) moved = speed * dt;
            if (_stuck.Update(moved, Mathf.Min(speed * dt, reach), dt)) Repath(pos);
        }

        /// <summary>The bounded repath: skip ahead on the trail, re-find it, then ask for a recall.</summary>
        private void Repath(Vector3 pos)
        {
            if (Trail == null || Trail.Count == 0)
            {
                Block(DisplayName + " is stuck. Hold N to call it.", true);
                return;
            }
            if (_stuck.Attempts == 1)
            {
                int cur = _crumbId > 0 ? Mathf.Max(0, Trail.IndexOfId(_crumbId)) : 0;
                int skip = Trail.FurthestClear(pos, cur, 8, _clearFn);
                if (skip >= 0) _crumbId = Trail[skip].Id;
                _direct = false;
                _directTimer = 0.6f;
            }
            else if (_stuck.Attempts == 2)
            {
                _crumbId = 0;
                _direct = false;
                _directTimer = 0.6f;
            }
            else if (_stuck.ShouldRecall)
            {
                Block(DisplayName + " is stuck. It catches up out of sight, or hold N to call it.", true);
            }
        }

        private bool TrailTarget(Vector3 pos, out Vector3 target, out bool air)
        {
            target = pos;
            air = false;
            if (Trail == null || Trail.Count == 0) return false;
            int idx = _crumbId > 0 ? Trail.IndexOfId(_crumbId) : -1;
            if (idx >= 0 && Trail[idx].Segment != Trail.Segment) idx = -1;
            if (idx < 0)
            {
                idx = Trail.NearestIndex(pos, CompanionFollowRules.LostTrailDistance);
                if (idx < 0 || Trail[idx].Segment != Trail.Segment) return false;
            }
            while (idx < Trail.Count - 1 && ReachedCrumb(pos, Trail[idx].P)) idx++;
            TrailCrumb c = Trail[idx];
            if (c.Hazard && !Flying)
            {
                int safe = Trail.NextSafeIndex(idx, 14);
                if (safe < 0)
                {
                    Block(DisplayName + " will not cross the heat. It waits for a cool way, or hold N to call it.", true);
                    return false;
                }
                TrailCrumb s = Trail[safe];
                float gap = MathUtil.FlatDistance(pos, s.P);
                if (!CompanionFollowRules.CanHop(gap, s.P.y - pos.y, Profile.Locomotion))
                {
                    Block(DisplayName + " cannot jump that heat. Hold N to call it across.", true);
                    return false;
                }
                StartHop(pos, s.P);
                idx = safe;
                c = s;
            }
            else if (!Flying && _hopT < 0f && c.P.y - _groundY > 0.55f && MathUtil.FlatDistance(pos, c.P) < 2.6f)
            {
                // A ledge the player jumped onto: hop up when it is within reach, otherwise wait for a recall.
                if (!CompanionFollowRules.CanHop(MathUtil.FlatDistance(pos, c.P), c.P.y - _groundY, Profile.Locomotion))
                {
                    Block(DisplayName + " cannot climb up there. Hold N to call it.", true);
                    return false;
                }
                StartHop(pos, c.P);
            }
            _crumbId = c.Id;
            target = c.P;
            air = c.Air;
            return true;
        }

        /// <summary>Distance to the player for follow decisions: hovering bodies ignore their own hover height.</summary>
        private float DistanceToPlayer(Vector3 pos, Vector3 pp)
        {
            if (Flying || Profile.Locomotion == CompanionLocomotion.Hover)
                return MathUtil.FlatDistance(pos, pp) + Mathf.Max(0f, Mathf.Abs(pos.y - pp.y) - Profile.HoverHeight - 1f);
            return CompanionTrail.Weighted(pos, pp);
        }

        /// <summary>A crumb is reached: flat distance and height for walkers, flat distance within the hover band for flyers.</summary>
        private bool ReachedCrumb(Vector3 pos, Vector3 crumb)
        {
            if (!Flying) return CompanionFollowRules.Reached(pos, crumb);
            float dx = pos.x - crumb.x, dz = pos.z - crumb.z;
            float dy = pos.y - crumb.y;
            return dx * dx + dz * dz <= 0.8f * 0.8f && dy > -1.2f && dy < Profile.HoverHeight + 2.2f;
        }

        private void HoldStep(float dt, ref Vector3 pos)
        {
            Vector3 to = new Vector3(WaitPoint.x - pos.x, 0f, WaitPoint.z - pos.z);
            float speed = to.magnitude > 0.4f ? Profile.WalkSpeed : 0f;
            SteerTo(dt, ref pos, WaitPoint, speed, false);
            PlayerController pl = Game.Player;
            if (pl != null && (pl.transform.position - pos).sqrMagnitude < 25f * 25f) SetLook(pl.HeadPosition);
            else ClearLook();
        }

        private void GoalStep(float dt, ref Vector3 pos)
        {
            Vector3 to = _goal - pos;
            float flat = new Vector3(to.x, 0f, to.z).magnitude;
            bool reached = flat < 0.5f && (Flying ? Mathf.Abs(to.y) < 0.8f : Mathf.Abs(to.y) < 1.4f);
            if (reached) GoalReached = true;
            SteerTo(dt, ref pos, _goal, GoalReached ? 0f : _goalSpeed, _goalFly);
        }

        private void SettleStep(float dt, ref Vector3 pos)
        {
            _vel = Vector3.MoveTowards(_vel, Vector3.zero, 12f * dt);
            if (Mode == CompanionMode.Downed || Mode == CompanionMode.Rescue) Flying = false;
            SteerTo(dt, ref pos, pos, 0f, false);
        }

        // ================================================================== steering

        private void SteerTo(float dt, ref Vector3 pos, Vector3 target, float speed, bool wantAir)
        {
            ProbeGround(pos, false);
            if (_hopT >= 0f)
            {
                HopStep(dt, ref pos);
                return;
            }
            UpdateFlightState(pos, target, wantAir);
            Vector3 to = target - pos;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            float fd = flat.magnitude;
            Vector3 desired = Vector3.zero;
            if (speed > 0f && fd > 0.05f) desired = flat / fd * Mathf.Min(speed, fd / Mathf.Max(dt, 1e-3f));
            float accel = desired.sqrMagnitude > _vel.sqrMagnitude ? 10f : 14f;
            if (Flying) accel *= 0.8f;
            _vel = Vector3.MoveTowards(_vel, desired, accel * dt);
            pos.x += _vel.x * dt;
            pos.z += _vel.z * dt;
            ProbeGround(pos, false);
            if (Flying)
            {
                _ceilingTimer -= dt;
                if (_ceilingTimer <= 0f)
                {
                    _ceilingTimer = 0.2f;
                    _ceilingY = CeilingAbove(pos);
                }
                float floor = _hasGround ? _groundY : Mathf.Min(pos.y, target.y);
                float wantY;
                if (Profile.Locomotion == CompanionLocomotion.Hover) wantY = CompanionFollowRules.HoverY(floor, _ceilingY, Profile.HoverHeight, Profile.BodyHeight);
                else wantY = CompanionFollowRules.HoverY(floor, _ceilingY, Mathf.Max(0.6f, target.y - floor + 0.3f), Profile.BodyHeight);
                if (Mode == CompanionMode.Goal && _goalFly) wantY = Mathf.Min(_goal.y, float.IsInfinity(_ceilingY) ? _goal.y : _ceilingY - Profile.BodyHeight - 0.35f);
                float climb = Mathf.Max(2f, Profile.FlySpeed * 0.6f);
                pos.y = Mathf.MoveTowards(pos.y, wantY, climb * dt);
                _vy = 0f;
            }
            else if (_hasGround)
            {
                if (_groundY >= pos.y) pos.y = Mathf.MoveTowards(pos.y, _groundY, 6f * dt + (_groundY - pos.y) * 0.5f);
                else
                {
                    _vy -= 19.6f * dt;
                    pos.y = Mathf.Max(_groundY, pos.y + _vy * dt);
                    if (pos.y <= _groundY + 0.01f) _vy = 0f;
                }
            }
            else
            {
                _vy -= 19.6f * dt;
                pos.y += _vy * dt;
                if (pos.y < target.y - 4f || pos.y < WorldBounds.KillY + 10f)
                {
                    Block(DisplayName + " slipped down a drop. It comes back to you out of sight, or hold N to call it.", true);
                    _vy = 0f;
                    pos.y = Mathf.Max(pos.y, target.y - 4f);
                }
            }
            float want = _yaw;
            if (_vel.sqrMagnitude > 0.04f) want = MathUtil.YawOf(_vel);
            else if (_hasLook) want = MathUtil.YawOf(new Vector3(_look.x - pos.x, 0f, _look.z - pos.z));
            float prev = _yaw;
            _yaw = Mathf.MoveTowardsAngle(_yaw, want, Profile.TurnRate * dt);
            _yawRate = Mathf.DeltaAngle(prev, _yaw) / Mathf.Max(dt, 1e-3f);
        }

        private void UpdateFlightState(Vector3 pos, Vector3 target, bool wantAir)
        {
            switch (Profile.Locomotion)
            {
                case CompanionLocomotion.Hover:
                    Flying = Mode != CompanionMode.Downed && Mode != CompanionMode.Rescue;
                    break;
                case CompanionLocomotion.WalkerGlider:
                {
                    float above = target.y - (_hasGround ? _groundY : pos.y);
                    bool lift = (wantAir || Mode == CompanionMode.Goal && _goalFly) && above > 1.2f;
                    bool gap = !_hasGround || pos.y - _groundY > 2f;
                    if (Mode == CompanionMode.Downed || Mode == CompanionMode.Rescue) Flying = false;
                    else if (lift || gap) Flying = true;
                    else if (Flying && _hasGround && above < 0.8f && pos.y - _groundY < 0.7f) Flying = false;
                    break;
                }
                default:
                    Flying = false;
                    break;
            }
        }

        private void StartHop(Vector3 from, Vector3 to)
        {
            _hopFrom = from;
            _hopTo = to;
            float d = MathUtil.FlatDistance(from, to);
            _hopDur = Mathf.Clamp(0.3f + d * 0.09f, 0.35f, 0.8f);
            _hopT = 0f;
            _vel = Vector3.zero;
            PlayAction(CreatureAction.Pounce, _hopDur, 0.25f, 0.6f, null);
        }

        private void HopStep(float dt, ref Vector3 pos)
        {
            _hopT += dt / Mathf.Max(0.1f, _hopDur);
            float t = Mathf.Clamp01(_hopT);
            Vector3 p = Vector3.Lerp(_hopFrom, _hopTo, t);
            float apex = Mathf.Max(0.5f, Mathf.Abs(_hopTo.y - _hopFrom.y) * 0.5f + 0.5f);
            p.y += Mathf.Sin(t * Mathf.PI) * apex;
            Vector3 d = _hopTo - _hopFrom;
            if (d.sqrMagnitude > 0.01f) _yaw = MathUtil.YawOf(new Vector3(d.x, 0f, d.z));
            pos = p;
            if (_hopT >= 1f)
            {
                _hopT = -1f;
                _vy = 0f;
                ProbeGround(pos, true);
            }
        }

        // ================================================================== probes

        private void ProbeGround(Vector3 pos, bool force)
        {
            _groundTimer -= Time.deltaTime;
            if (!force && _groundTimer > 0f) return;
            _groundTimer = Flying ? 0.1f : 0.04f;
            // A walker's probe starts one step above its feet, so a table top or a low beam overhead is never taken for
            // the floor; higher ledges are hops (TrailTarget), not steps.
            float up = Flying ? 0.5f : 0.6f;
            float len = Flying ? 12f : 3.2f;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + up, pos.z), Vector3.down, out hit, len, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                _hasGround = true;
                _groundY = hit.point.y;
            }
            else _hasGround = false;
        }

        private float CeilingAbove(Vector3 pos)
        {
            float from = (_hasGround ? _groundY : pos.y) + 0.3f;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, from, pos.z), Vector3.up, out hit, 9f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return float.PositiveInfinity;
        }

        private static bool HazardPoint(Vector3 p)
        {
            float heat;
            return Game.Underheart != null && Game.Underheart.HazardAt(p + Vector3.up * 0.2f, out heat) && heat > 0f;
        }

        /// <summary>A straight move from a to b is clear: nothing solid in the way and, for walkers, solid floor halfway.</summary>
        private bool ClearPath(Vector3 a, Vector3 b)
        {
            float h = Mathf.Max(0.25f, Profile.BodyHeight * 0.5f);
            Vector3 from = a + Vector3.up * h, to = b + Vector3.up * h;
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.05f) return true;
            float r = Mathf.Clamp(Profile.BodyRadius * 0.5f, 0.12f, 0.3f);
            int mask = Layers.WorldMask | (1 << Layers.Vehicles);
            if (Physics.SphereCast(from, r, d / len, out RaycastHit _, len, mask, QueryTriggerInteraction.Ignore)) return false;
            if (CanFly) return true;
            Vector3 mid = (a + b) * 0.5f;
            RaycastHit hit;
            if (!Physics.Raycast(mid + Vector3.up * 1.0f, Vector3.down, out hit, 2.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            return Mathf.Abs(hit.point.y - (a.y + b.y) * 0.5f) < 1.0f;
        }

        // ================================================================== recall and safe placement

        /// <summary>True when the body is out of the player's view (behind the camera, off screen, far or behind walls).</summary>
        public bool OffCamera()
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null || Rig == null || Hidden) return true;
            Vector3 c = ChestPosition;
            Vector3 vp = cam.WorldToViewportPoint(c);
            if (vp.z < 0.1f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) return true;
            if (vp.z > 70f) return true;
            return Physics.Linecast(cam.transform.position, c, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Recalls the body to verified ground beside the player. Automatic recalls (manual false) only happen off camera
        /// and off cooldown. False with a reason when no safe point exists or it must wait.
        /// </summary>
        public bool TryRecall(bool manual, out string reason)
        {
            reason = "";
            PlayerController pl = Game.Player;
            if (pl == null) { reason = "No one to follow."; return false; }
            if (!manual && _recallCooldown > 0f) { reason = "Recall recharging."; return false; }
            if (!manual && !OffCamera()) { reason = "waiting"; return false; }
            Vector3 point;
            if (!FindSafePoint(pl, Profile, out point, out reason))
            {
                Block(DisplayName + " cannot reach you here: " + reason, false);
                return false;
            }
            Vector3 face = pl.transform.position - point;
            float yaw = face.sqrMagnitude > 0.01f ? MathUtil.YawOf(new Vector3(face.x, 0f, face.z)) : pl.transform.eulerAngles.y;
            Teleport(point, yaw);
            Flying = Profile.Locomotion == CompanionLocomotion.Hover || point.y - _groundY > 1f;
            _recallCooldown = manual ? BondRules.ManualRecallCooldown : BondRules.RecallCooldown;
            ClearBlock();
            if (Game.Fx != null) Game.Fx.CrystalBurst(point + Vector3.up * Mathf.Max(0.3f, Profile.BodyHeight * 0.6f), Profile.Accent, 10, 0.4f);
            Underheart04ESounds.Play(UhSfx.CompanionRecall, point, 0.8f, 1f);
            return true;
        }

        /// <summary>
        /// A verified place for the companion beside the player: floor under it on the player's floor, room for its body,
        /// a clear line from the player (never through a wall, gate or door), no heat; hovering bodies also need
        /// headroom. Ground companions wait for an airborne player to land. Uses a shared scratch list.
        /// </summary>
        public static bool FindSafePoint(PlayerController pl, CompanionProfile prof, out Vector3 point, out string reason)
        {
            point = Vector3.zero;
            reason = "";
            if (pl == null || prof == null) { reason = "no one to follow."; return false; }
            Vector3 feet = pl.InVehicle && pl.CurrentVehicle != null ? pl.CurrentVehicle.transform.position : pl.transform.position;
            RaycastHit hit;
            float playerGround = feet.y;
            if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out hit, 40f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) playerGround = hit.point.y;
            bool airborne = feet.y - playerGround > 2.5f;
            bool flyer = CompanionArchetypes.CanFly(prof);
            if (airborne && !flyer) { reason = "it waits for you to land."; return false; }
            CompanionFollowRules.RecallOffsets(pl.transform.forward, prof.Side, _offsets);
            int mask = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Debris);
            Vector3 eye = feet + Vector3.up * 0.9f;
            string why = "no safe ground beside you.";
            for (int i = 0; i < _offsets.Count; i++)
            {
                Vector3 c = feet + _offsets[i];
                Vector3 spot;
                if (airborne)
                {
                    spot = c;
                    if (Physics.CheckSphere(spot + Vector3.up * prof.BodyHeight * 0.5f, Mathf.Max(0.3f, prof.BodyRadius), mask, QueryTriggerInteraction.Ignore)) continue;
                }
                else
                {
                    if (!Physics.Raycast(c + Vector3.up * 1.6f, Vector3.down, out hit, 3.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { why = "no floor beside you."; continue; }
                    if (Mathf.Abs(hit.point.y - playerGround) > 1.3f || hit.normal.y < 0.7f) { why = "the floor beside you is too uneven."; continue; }
                    spot = hit.point;
                    float r = Mathf.Clamp(prof.BodyRadius, 0.2f, 0.6f);
                    float h = Mathf.Max(prof.BodyHeight, r * 2f + 0.1f);
                    if (Physics.CheckCapsule(spot + Vector3.up * (r + 0.08f), spot + Vector3.up * (h - r + 0.08f), r, mask, QueryTriggerInteraction.Ignore)) { why = "there is no room beside you."; continue; }
                    if (prof.Locomotion == CompanionLocomotion.Hover)
                    {
                        if (Physics.Raycast(spot + Vector3.up * 0.2f, Vector3.up, out RaycastHit ceil, prof.HoverHeight + prof.BodyHeight, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                        {
                            if (ceil.point.y - spot.y < 1.2f) { why = "the ceiling is too low."; continue; }
                        }
                    }
                }
                if (!WorldBounds.SameLayer(spot, feet)) continue;
                if (Physics.Linecast(eye, spot + Vector3.up * 0.5f, Layers.WorldMask | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore)) { why = "walls are in the way."; continue; }
                if (HazardPoint(spot)) { why = "the ground beside you is burning."; continue; }
                point = spot;
                return true;
            }
            reason = why;
            return false;
        }
    }
}
