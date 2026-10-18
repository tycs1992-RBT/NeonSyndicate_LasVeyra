using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A handler-owned event drakeling. Uses the authored companion rig without registering or granting a player bond.</summary>
    public sealed class NpcBondedDrake04F : MonoBehaviour
    {
        public EventInstance Event;
        public NpcController Handler;
        public CompanionController Body;
        public EventDrakeVitality04G Vitality { get; private set; }
        public bool Healthy { get { return Vitality == null || Vitality.IsAlive; } }
        private CompanionBodyFactory.Job _job;
        private float _thinkAt, _attackAt;
        private bool _retired;
        private bool _separated;
        private bool _routeOpen;
        private Vector3 _separationPoint;
        public bool RouteOpen { get { return _routeOpen; } }
        public bool Reunited { get { return !_retired && Healthy && Body != null && Handler != null && !Handler.IsDead && (!_separated || _routeOpen)
            && Vector3.Distance(Body.transform.position, Handler.transform.position) <= 3f; } }
        public void OpenRoute() { _routeOpen = true; }
        public void Init(EventInstance owner, NpcController handler, bool separated, Vector3 separationPoint)
        {
            Event = owner;
            Handler = handler;
            if (handler.CityActor04F != null) handler.CityActor04F.Drake04G = this;
            _separated = separated;
            _routeOpen = !separated;
            _separationPoint = separationPoint;
            _job = CompanionBodyFactory.Request(BondIds.QuartzGuide, owner.Rand.Next(1, int.MaxValue), 0, "", Settings.Graphics == GraphicsPreset.Low ? 0 : 1);
        }

        private void Update()
        {
            if (_retired || Event == null || !Game.IsPlaying) return;
            if (Event.Done || Handler == null || Handler.IsDead)
            {
                Retire();
                return;
            }
            if (_job != null && _job.Finished)
            {
                CompanionBodyFactory.Job completed = _job;
                _job = null;
                if (completed.Failed || completed.Body == null) { Log.Warn("NPC drake could not build: " + completed.Error); return; }
                Body = CompanionController.Create(completed.Body, CompanionArchetypes.QuartzGuide, _separated ? _separationPoint : Handler.transform.position + Handler.transform.right * 2f,
                    Handler.transform.eulerAngles.y, Event.Root);
                Body.gameObject.layer = Layers.Characters;
                SphereCollider hit = Body.gameObject.AddComponent<SphereCollider>();
                hit.center = Vector3.up * 0.6f;
                hit.radius = 0.48f;
                Vitality = Body.gameObject.AddComponent<EventDrakeVitality04G>();
                Vitality.Drake = this;
                Body.DisplayName = "Bonded street drakeling";
                Body.Uid = 0;
                Body.Mode = CompanionMode.Wait;
                Body.WaitPoint = Body.transform.position;
                Event.Objects.Add(Body.gameObject);
            }
            if (Body == null) return;
            if (!Healthy) { Body.Mode = CompanionMode.Downed; return; }
            if (Event.Held) { Body.WaitPoint = Body.transform.position; Body.Mode = CompanionMode.Wait; return; }
            if (_separated && !_routeOpen)
            {
                Body.WaitPoint = _separationPoint;
                Body.Mode = CompanionMode.Wait;
                Body.SetLook(Handler.HeadPosition);
                Body.SetAlert(0.85f);
                return;
            }
            if (Time.time < _thinkAt) return;
            _thinkAt = Time.time + 0.35f;
            Body.Combat = Handler.State == NpcState.Fight;
            Body.SetAlert(Body.Combat ? 0.8f : 0.2f);
            CharacterBase target = Handler.Target;
            Vector3 beside = Handler.transform.position + Handler.transform.right * 1.8f - Handler.transform.forward;
            // 04G: a fighting drakeling escorts from a small lateral flank rather than stacking directly on its handler.
            // The handler remains the only source of targeting and damage authority; no autonomous player aggression.
            if (Body.Combat && target != null && NpcCrystal04F.CanDamage(Handler, target))
            {
                Vector3 approach = target.transform.position - Handler.transform.position;
                approach.y = 0f;
                if (approach.sqrMagnitude > 4f && approach.sqrMagnitude < 24f * 24f)
                {
                    Vector3 dir = approach.normalized;
                    float side = (Handler.GetInstanceID() & 1) == 0 ? 2.3f : -2.3f;
                    Vector3 flank = Handler.transform.position + dir * 1.2f + new Vector3(-dir.z, 0f, dir.x) * side;
                    // Do not move through a solid wall merely to obtain a firing angle.
                    if (!Physics.Linecast(Handler.ChestPosition, flank + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                        beside = flank;
                }
            }
            if (MathUtil.FlatDistance(Body.transform.position, beside) > 1.5f) Body.SetGoal(beside, 4f, true);
            else { Body.WaitPoint = Body.transform.position; Body.Mode = CompanionMode.Wait; }
            if (target == null || !NpcCrystal04F.CanDamage(Handler, target) || !Body.Combat || Time.time < _attackAt) return;
            Vector3 aim = target.ChestPosition;
            if (Vector3.Distance(Body.ChestPosition, aim) > 22f || Physics.Linecast(Body.ChestPosition, aim, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            _attackAt = Time.time + (Vitality != null && Vitality.Injured ? 8f : 5.5f);
            Body.SetLook(aim);
            Body.PlayAction(CreatureAction.Breath, 1.1f, 0.4f, 0.62f, () =>
            {
                if (_retired || !Healthy || Event == null || Event.Done || Event.Held || Body == null || Handler == null || !NpcCrystal04F.CanDamage(Handler, target)) return;
                Vector3 from = Body.MouthPosition;
                Vector3 direction = aim - from;
                if (direction.sqrMagnitude < 0.1f || Physics.Linecast(from, aim, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
                NpcCrystal04FProjectile.Launch(Handler, from, direction.normalized, 22f, 17f, 24f, WeaponId.VectorSidearm,
                    new Color(0.4f, 1f, 0.9f), false, Body.transform);
                Body.Glow(0.3f);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, from, 0.45f, 0.7f, 35f);
            });
        }

        public void Retire()
        {
            if (_retired) return;
            _retired = true;
            if (_job != null) { _job.Cancelled = true; _job = null; }
            if (Handler != null && Handler.CityActor04F != null && Handler.CityActor04F.Drake04G == this) Handler.CityActor04F.Drake04G = null;
            if (Body != null) { Body.ClearAction(); Destroy(Body.gameObject); Body = null; }
        }
        private void OnDestroy() { Retire(); }
    }
}
