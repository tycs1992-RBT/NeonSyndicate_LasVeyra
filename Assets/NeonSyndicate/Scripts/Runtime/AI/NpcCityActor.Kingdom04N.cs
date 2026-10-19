using UnityEngine;
namespace NeonSyndicate
{
    public static class HumanWithdrawal04N
    {
        public static float Threshold(CrystalNpcRole04F role)
        {return role==CrystalNpcRole04F.FocusWeaver?.40f:role==CrystalNpcRole04F.RiftDuelist?.35f:role==CrystalNpcRole04F.Marshal?.30f:.22f;}
        public static float Distance(CrystalNpcRole04F role)
        {return role==CrystalNpcRole04F.FocusWeaver?5f:role==CrystalNpcRole04F.Marshal?6f:role==CrystalNpcRole04F.RiftDuelist?3.5f:2.5f;}
        public static bool Admit(CrystalNpcRole04F role,float health,bool spent,bool teammateWithdrawing)
        {return health>0 && health<Threshold(role) && !spent && !teammateWithdrawing;}
    }
    public sealed partial class NpcCityActor04F
    {
        private bool _withdrawUsed04N;private float _withdrawUntil04N;private Vector3 _withdrawGoal04N;
        private bool Withdrawal04N(CharacterBase target)
        {
            if(!Actor.HasCrystalRole04F || Actor.IsCreature || Actor.Health==null)return false;
            if(Event.Age<_withdrawUntil04N)
            {Actor.Target=null;Actor.Weapons.CancelActions();Actor.MoveTo(_withdrawGoal04N,false,null);return true;}
            bool teammate=false;
            foreach(var n in Event.Npcs)if(n!=null && n!=Actor && n.CityActor04F!=null && n.CityActor04F.Side==Side && n.CityActor04F._withdrawUntil04N>Event.Age){teammate=true;break;}
            if(!HumanWithdrawal04N.Admit(Actor.CrystalRole04F,Actor.Health.Health/Mathf.Max(1,Actor.Health.MaxHealth),_withdrawUsed04N,teammate))return false;
            Vector3 away=MathUtil.Flat(Actor.transform.position-target.transform.position).normalized;
            Vector3 goal=Actor.transform.position+away*HumanWithdrawal04N.Distance(Actor.CrystalRole04F);
            float y;if(MathUtil.FlatDistance(goal,Event.Pos)>24f || !CreatureDirector.GroundAt(goal,3,out y))return false;goal.y=y;
            int mask=KingdomCombatRules04N.CompanionObstacleMask;
            if(!WorldBounds.SameLayer(goal,Actor.transform.position) || Mathf.Abs(goal.y-Actor.transform.position.y)>1f ||
                Physics.CheckCapsule(goal+Vector3.up*.4f,goal+Vector3.up*1.6f,.35f,mask,QueryTriggerInteraction.Ignore) ||
                Physics.Linecast(Actor.ChestPosition,goal+Vector3.up,mask,QueryTriggerInteraction.Ignore))return false;
            _withdrawUsed04N=true;_withdrawUntil04N=Event.Age+2.5f;_withdrawGoal04N=goal;
            Actor.ClearEventCover04H();Actor.Target=null;Actor.Weapons.CancelActions();
            CombatDirector.ReleaseMeleeToken(Actor);CombatDirector.ReleaseShotToken(Actor);
            Actor.MoveTo(goal,false,null);return true;
        }
    }
}
