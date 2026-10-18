using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class NpcBondedDrake04F
    {
        private NpcController _focus04H;
        private bool _retreat04H;
        public string Intent04H { get; private set; } = "Escorting";

        private bool Opponent04H(NpcController other)
        {
            NpcCityActor04F own=Handler!=null?Handler.CityActor04F:null;
            NpcCityActor04F rival=other!=null?other.CityActor04F:null;
            return other!=null && !other.IsDead && own!=null && rival!=null && !own.StoodDown && !rival.StoodDown &&
                !rival.Finished && own.Event==Event && rival.Event==Event && own.Side!=rival.Side &&
                Event.Age>=own.FightAt && MathUtil.FlatDistance(other.transform.position,Handler.transform.position)<24f &&
                NpcCrystal04F.CanDamage(Handler,other);
        }

        private NpcController Assess04H()
        {
            NpcCityActor04F own=Handler.CityActor04F;
            if(own==null || own.Opponents==null || own.StoodDown || Event.Age<own.FightAt) return null;
            NpcController best=null;
            float score=float.MaxValue;
            // The event's bounded membership is the complete candidate set. Never scan the player or bond roster.
            foreach(NpcController candidate in own.Opponents)
            {
                if(!Opponent04H(candidate)) continue;
                float distance=MathUtil.FlatDistance(Body.transform.position,candidate.transform.position);
                if(distance>22f || Physics.Linecast(Body.ChestPosition,candidate.ChestPosition,Layers.WorldMask,QueryTriggerInteraction.Ignore)) continue;
                float urgency=distance+(candidate==_focus04H?-2f:0f)+(candidate.Target==Handler?-5f:0f);
                if(urgency<score){score=urgency;best=candidate;}
            }
            return best;
        }

        private void Think04H()
        {
            if(Time.time<_thinkAt) return;
            _thinkAt=Time.time+.35f;
            _focus04H=Assess04H();
            _retreat04H=EventDrakeBehavior04H.Retreat(_retreat04H,Vitality!=null?Vitality.Health:90f);
            Body.Combat=_focus04H!=null;
            Body.SetAlert(_retreat04H?1f:Body.Combat?.85f:.2f);
            Body.SetLook(_focus04H!=null?_focus04H.ChestPosition:Handler.HeadPosition);
            if(Body.ActionPlaying) return; // A windup owns its pose, position and single callback.
            Vector3 position=Body.transform.position;
            Vector3 escort=Handler.transform.position+Handler.transform.right*1.8f-Handler.transform.forward;
            if(_retreat04H)
            {
                Intent04H="Retreating to handler";
                Body.SetGoal(escort,3f,false);
                bool safe=_focus04H==null || MathUtil.FlatDistance(position,_focus04H.transform.position)>5f;
                if(Vitality!=null && safe && MathUtil.FlatDistance(position,Handler.transform.position)<3f)
                { Vitality.Recover04H(.35f); Intent04H="Recovering beside handler"; }
                return;
            }
            NpcController target=_focus04H;
            if(target==null)
            {
                Intent04H="Escorting";
                if(MathUtil.FlatDistance(position,escort)>1.4f) Body.SetGoal(escort,3.5f,false);
                else {Body.WaitPoint=position;Body.Mode=CompanionMode.Wait;}
                return;
            }
            float distance=MathUtil.FlatDistance(position,target.transform.position)-target.BodyRadius;
            if(Time.time>=_attackAt && distance<2f && Mathf.Abs(position.y-target.transform.position.y)<1.2f)
            {
                Intent04H="Bite windup";
                _attackAt=Time.time+4.4f;
                Body.WaitPoint=position;Body.Mode=CompanionMode.Wait;
                Body.PlayAction(CreatureAction.Bite,.95f,.38f,.58f,()=>
                {
                    if(!MayStrike04H(target) || MathUtil.FlatDistance(Body.transform.position,target.transform.position)-target.BodyRadius>2.2f) return;
                    Vector3 direction=(target.ChestPosition-Body.ChestPosition).normalized;
                    DamageInfo info=DamageInfo.Make(13f,target.ChestPosition,direction,Handler.gameObject,DamageType.Melee);
                    info.Poise=16f;info.Force=2f;info.Flags|=DamageFlags.Parryable;
                    target.Health.TakeDamage(info);
                    if(Game.Audio!=null) Game.Audio.PlayAt(SfxId.CrystalChime,target.ChestPosition,.35f,.65f,25f);
                });
                return;
            }
            Vector3 directionTo=(target.transform.position-Handler.transform.position).normalized;
            Vector3 flank=Handler.transform.position+directionTo*1.8f+Vector3.Cross(Vector3.up,directionTo)*((Handler.GetInstanceID()&1)==0?2.5f:-2.5f);
            bool aerial=distance>7f && (Vitality==null || !Vitality.Injured) && !Physics.CheckSphere(position+Vector3.up*2f,.5f,Layers.WorldMask,QueryTriggerInteraction.Ignore);
            bool protecting=Handler.Health!=null && Handler.Health.Health<Handler.Health.MaxHealth*.5f && target.Target==Handler;
            if(protecting)
            {
                flank=Vector3.Lerp(Handler.transform.position,target.transform.position,.35f);
                aerial=false;
            }
            if(aerial) flank.y+=1.8f;
            Intent04H=protecting?"Protecting injured handler":aerial?"Aerial flank":"Ground flank";
            if(!Physics.Linecast(Body.ChestPosition,flank+Vector3.up*.6f,Layers.WorldMask,QueryTriggerInteraction.Ignore))
                Body.SetGoal(flank,4f,aerial);
            else Body.SetGoal(escort,3f,false);
            if(Time.time<_attackAt || distance>21f) return;
            _attackAt=Time.time+5.8f;
            Intent04H="Lance windup";
            Vector3 aim=target.ChestPosition; // Committed aim remains dodgeable.
            Body.WaitPoint=position;Body.Mode=CompanionMode.Wait;
            Body.PlayAction(CreatureAction.Breath,1.15f,.4f,.62f,()=>
            {
                if(!MayStrike04H(target)) return;
                Vector3 from=Body.MouthPosition,dir=aim-from;
                if(dir.sqrMagnitude<.1f || Physics.Linecast(from,aim,Layers.WorldMask,QueryTriggerInteraction.Ignore)) return;
                NpcCrystal04FProjectile.Launch(Handler,from,dir.normalized,22f,17f,24f,WeaponId.VectorSidearm,
                    new Color(.4f,1f,.9f),false,Body.transform);
                Body.Glow(.3f);
                if(Game.Audio!=null) Game.Audio.PlayAt(SfxId.CrystalChime,from,.4f,.7f,35f);
            });
        }

        private bool MayStrike04H(NpcController target)
        {
            return !_retired && Healthy && !_retreat04H && Event!=null && !Event.Done && !Event.Held &&
                Body!=null && Handler!=null && !Handler.IsDead && Opponent04H(target) &&
                !Physics.Linecast(Body.ChestPosition,target.ChestPosition,Layers.WorldMask,QueryTriggerInteraction.Ignore);
        }
    }

    public static class EventDrakeBehavior04H
    {
        public static void ResetStatics() { }
        public static bool Retreat(bool retreating,float health)
        { return health>0f && (health<35f || retreating && health<52f); }
        public static float Recovery(float health,float delta,float secondsSinceHit)
        {
            if(health<=0f || health>=60f || secondsSinceHit<6f || !(delta>0f) || float.IsNaN(delta) || float.IsInfinity(delta)) return health;
            return Mathf.Min(60f,health+Mathf.Min(delta,.5f)*6f);
        }
    }
}
