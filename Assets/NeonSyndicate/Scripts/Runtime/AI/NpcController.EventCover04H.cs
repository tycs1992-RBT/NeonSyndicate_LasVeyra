using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class NpcController
    {
        private CoverSpot _eventCover04H;
        private NpcCrystal04FCaster _eventCaster04H;
        private bool _eventHasCover04H;
        private float _eventCoverSearch04H;
        private CharacterBase _eventCoverTarget04H;

        internal void ClearEventCover04H()
        {
            if(!_eventHasCover04H) return;
            _eventHasCover04H=false;_eventCoverTarget04H=null;
            CancelEventCast04H();
            if(Game.Cover!=null) Game.Cover.Release(this);
            if(Anim!=null) {Anim.Cover=0f;Anim.CrouchWeight=0f;}
        }

        private void CancelEventCast04H()
        {
            if(_eventCaster04H==null) _eventCaster04H=GetComponent<NpcCrystal04FCaster>();
            if(_eventCaster04H!=null) _eventCaster04H.CancelWindup();
        }

        /// <summary>Event Weavers use existing shared cover claims. Telegraphs and bolt budgets remain with their caster.</summary>
        private bool UpdateEventCover04H(float dt,WeaponDef weapon,Vector3 target,float distance,out float speed)
        {
            speed=0f;
            if(CityActor04F==null || CityActor04F.Finished || CityActor04F.StoodDown || CityActor04F.Event.Held ||
                CrystalRole04F!=CrystalNpcRole04F.FocusWeaver || Target==null || Target.IsPlayer || Game.Cover==null)
            {ClearEventCover04H();return false;}
            if(_eventCoverTarget04H!=Target)
            {ClearEventCover04H();_eventCoverTarget04H=Target;_eventCoverSearch04H=0f;}
            if(_eventHasCover04H && (!CoverRules.Protects(_eventCover04H.Normal,target-_eventCover04H.Position) || distance<4f))
                ClearEventCover04H();
            if(!_eventHasCover04H && Time.time>=_eventCoverSearch04H)
            {
                _eventCoverSearch04H=Time.time+3.5f;
                CoverSpot spot;
                if(Game.Cover.FindCoverFor(this,target,11f,0,Home,out spot))
                {
                    bool nearby=MathUtil.FlatDistance(spot.Position,CityActor04F.Event.Pos)<24f;
                    bool reachable=!Physics.Linecast(ChestPosition,spot.Position+Vector3.up,Layers.WorldMask,QueryTriggerInteraction.Ignore);
                    if(nearby && reachable){_eventCover04H=spot;_eventHasCover04H=true;}
                    else Game.Cover.Release(this);
                }
            }
            if(!_eventHasCover04H) return false;
            bool expose=(Time.time+GetInstanceID()%5)%4.8f>2.5f;
            Vector3 point=_eventCover04H.Position;
            if(expose && _eventCover04H.Height==CoverHeight.High)
                point+=_eventCover04H.Right*(_eventCover04H.OpenRight?1f:-1f)*.9f;
            if(Physics.Linecast(ChestPosition,point+Vector3.up,Layers.WorldMask,QueryTriggerInteraction.Ignore))
            {ClearEventCover04H();return false;}
            _goal=point;_hasGoal=true;
            bool arrived=MathUtil.FlatDistance(transform.position,point)<.6f;
            speed=arrived?0f:WalkSpeed*1.4f;
            FaceTowards(target,dt);
            Anim.CrouchWeight=arrived && !expose && _eventCover04H.Height==CoverHeight.Low?1f:0f;
            Anim.Aiming=arrived && expose;
            Anim.AimDirection=(target-ChestPosition).normalized;
            if(arrived && expose && !Physics.Linecast(HeadPosition,target,Layers.WorldMask,QueryTriggerInteraction.Ignore))
                Weapons.TryFire(target,true,true,1.8f);
            else CancelEventCast04H();
            return true;
        }
    }
}
