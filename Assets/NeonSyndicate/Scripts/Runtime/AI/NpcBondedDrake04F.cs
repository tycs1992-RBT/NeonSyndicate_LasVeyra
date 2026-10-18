using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A handler-owned event drakeling. Uses the authored companion rig without registering or granting a player bond.</summary>
    public sealed partial class NpcBondedDrake04F : MonoBehaviour
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
            if (!Healthy) { Intent04H="Downed"; Body.Mode = CompanionMode.Downed; return; }
            if (Event.Held) { Intent04H="Waiting"; Body.ClearAction(); Body.WaitPoint = Body.transform.position; Body.Mode = CompanionMode.Wait; return; }
            if (_separated && !_routeOpen)
            {
                Body.WaitPoint = _separationPoint;
                Body.Mode = CompanionMode.Wait;
                Body.SetLook(Handler.HeadPosition);
                Body.SetAlert(0.85f);
                return;
            }
            Think04H();
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
