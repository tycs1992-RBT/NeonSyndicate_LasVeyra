using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class NpcController
    {
        private Func<Vector3, bool> _residenceContains;
        private Transform _residenceOwner;
        private Vector3 _residenceFirst;
        private Vector3 _residenceSecond;
        private bool _residenceSecondTarget;
        private float _residenceWait;
        private float _residenceYaw;
        private bool _residenceRoute;
        private float _residenceThreatCheck04G, _residenceShelterUntil04G;
        private int _residenceThreatSerial04G;
        private float _residenceObserveUntil04I;
        private Vector3 _residenceObserve04I;
        private int _residenceSeed04M;
        private ResidentShift04M _residenceShift04M;

        /// <summary>Transactional synchronous body build. A failed rig never leaves an unowned active NPC.</summary>
        public static NpcController SpawnResidence04C(Vector3 point, float yaw, Appearance look, string tag)
        {
            GameObject root = new GameObject("04C resident " + tag);
            try
            {
                root.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
                NpcController npc = root.AddComponent<NpcController>();
                npc.MissionTag = tag;
                npc.Init(NpcRole.Clerk, look, -1, yaw);
                return npc;
            }
            catch
            {
                root.SetActive(false);
                Destroy(root);
                throw;
            }
        }

        /// <summary>Opt-in local station routine. Existing outdoor AI, health and crime handling are unchanged.</summary>
        public void ConfigureResidence04C(Vector3 first, Vector3 second, float yaw, Func<Vector3, bool> contains, Transform owner, int seed)
        {
            _residenceSeed04M=seed;
            _residenceShift04M=ResidentShift04M.Work;
            _residenceFirst = first;
            _residenceSecond = second;
            _residenceContains = contains;
            _residenceOwner = owner;
            _residenceYaw = yaw;
            _residenceWait = 8f + seed % 13;
            _residenceSecondTarget = false;
            _residenceThreatSerial04G = Threats.LatestSerial;
            _residenceShelterUntil04G = 0f;
            _residenceRoute = Mathf.Abs(first.y - second.y) < 0.15f && Vector3.Distance(first, second) >= 1.2f;
            Home = first;
            SetState(NpcState.Idle);
        }

        public void ClearResidence04C()
        {
            _residenceContains = null;
            _residenceOwner = null;
            _residenceRoute = false;
            if(Anim!=null) {Anim.Inspect=false;Anim.HeadLook=false;}
        }

        private bool UpdateResidence04C(float dt)
        {
            if (_residenceContains == null) return false;
            if (_residenceOwner == null || !_residenceOwner.gameObject.activeInHierarchy)
            { Anim.Velocity = Vector3.zero; return true; }
            _residenceThreatCheck04G -= dt;
            if (_residenceThreatCheck04G <= 0f)
            {
                _residenceThreatCheck04G = 0.45f;
                ThreatEvent threat;
                if (Threats.Query(HeadPosition, _residenceThreatSerial04G, out threat) && threat.Kind != ThreatKind.Aim)
                {
                    _residenceShelterUntil04G = Time.time + 5f;
                    if (Anim != null) Anim.LookTarget = threat.Position;
                }
                _residenceThreatSerial04G = Threats.LatestSerial;
                Transform companion=Game.Bonds!=null?Game.Bonds.ActiveBody:null;
                if(companion!=null && (companion.position-transform.position).sqrMagnitude<36f &&
                    !Physics.Linecast(HeadPosition,companion.position+Vector3.up*.5f,Layers.WorldMask,QueryTriggerInteraction.Ignore))
                { _residenceObserve04I=companion.position+Vector3.up*.5f;_residenceObserveUntil04I=Time.time+1.2f; }
            }
            // Direct attacks still apply damage and report crimes. A frightened indoor civilian shelters locally.
            bool frightened = Time.time < _residenceShelterUntil04G || State == NpcState.Flee || State == NpcState.Cower ||
                (Health != null && Health.LastDamageTime > SpawnTime && Time.time - Health.LastDamageTime < 14f);
            if (frightened)
            {
                if (Time.time < _residenceShelterUntil04G || Health != null && Time.time - Health.LastDamageTime < 14f) SetState(NpcState.Cower);
                else { SetState(NpcState.Idle); frightened = false; }
            }
            float hour=Game.Atmo!=null?Game.Atmo.Hours:10f;
            ResidentShift04M shift=LivingWorldRules04M.Shift(hour,_residenceSeed04M,Game.Atmo!=null && Game.Atmo.Current==Weather.Storm);
            if(shift!=_residenceShift04M)
            {
                _residenceShift04M=shift;
                _residenceSecondTarget=shift==ResidentShift04M.Break;
                _residenceWait=0f;
            }
            _residenceWait -= dt;
            Vector3 position = transform.position;
            Vector3 goal = _residenceSecondTarget ? _residenceSecond : _residenceFirst;
            Vector3 velocity = Vector3.zero;
            bool grounded = true;
            if (!frightened && _residenceRoute && _residenceWait <= 0f)
            {
                Vector3 to = MathUtil.Flat(goal - position);
                if (to.sqrMagnitude < 0.025f)
                {
                    if(_residenceShift04M==ResidentShift04M.Work)_residenceSecondTarget = !_residenceSecondTarget;
                    _residenceWait = 10f + (MissionTag == null ? 0 : MissionTag.Length % 9);
                }
                else
                {
                    Vector3 next = Vector3.MoveTowards(position, goal, Mathf.Min(dt, 0.05f) * WalkSpeed * 0.65f);
                    next.y = _residenceFirst.y;
                    RaycastHit hit;
                    int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell);
                    Vector3 delta = next - position;
                    bool clear = _residenceContains(next) && ResidenceActorsClear04C(next) && !Physics.CapsuleCast(position + Vector3.up * 0.4f,
                        position + Vector3.up * 1.72f, 0.34f, delta.normalized, delta.magnitude + 0.08f, mask, QueryTriggerInteraction.Ignore);
                    if (clear && Physics.Raycast(next + Vector3.up * 0.35f, Vector3.down, out hit, 0.75f, Layers.WorldMask, QueryTriggerInteraction.Ignore) &&
                        InhabitedResidentRules04C.FloorMatches(_residenceFirst.y, hit.point.y))
                    {
                        next.y = hit.point.y + 0.025f;
                        velocity = (next - position) / Mathf.Max(0.001f, dt);
                        transform.position = next;
                        _yaw = Mathf.MoveTowardsAngle(_yaw, MathUtil.YawOf(to), 180f * dt);
                    }
                    else _residenceWait = 3f;
                }
            }
            if (velocity.sqrMagnitude < 0.001f && !frightened) _yaw = Mathf.MoveTowardsAngle(_yaw, _residenceYaw, 70f * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _vel = velocity;
            _grounded = grounded;
            _hasGoal = false;
            Anim.Velocity = velocity;
            Anim.Grounded = grounded;
            Anim.SitWeight = 0f;
            Anim.Cower = frightened;
            Anim.Panic = Mathf.MoveTowards(Anim.Panic, frightened ? 1f : 0f, dt);
            Anim.CombatReady = 0f;
            bool stationary=!frightened && velocity.sqrMagnitude<.001f;
            Anim.Inspect=stationary && _residenceShift04M==ResidentShift04M.Work && _residenceWait>1f && _residenceWait<6f;
            if(!frightened && Time.time<_residenceObserveUntil04I)
            {Anim.HeadLook=true;Anim.LookTarget=_residenceObserve04I;Anim.Inspect=false;}
            else if(!frightened) Anim.HeadLook=false;
            return true;
        }

        private bool ResidenceActorsClear04C(Vector3 next)
        {
            if (Game.Player != null && Mathf.Abs(Game.PlayerPosition.y - next.y) < 2f && MathUtil.FlatSqrDistance(Game.PlayerPosition, next) < 0.81f) return false;
            foreach (NpcController other in All)
                if (other != null && other != this && Mathf.Abs(other.transform.position.y - next.y) < 2f &&
                    MathUtil.FlatSqrDistance(other.transform.position, next) < 0.81f) return false;
            return true;
        }
    }
}
