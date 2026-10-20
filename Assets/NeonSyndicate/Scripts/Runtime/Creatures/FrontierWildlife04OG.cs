using UnityEngine;
namespace NeonSyndicate
{
    public enum FrontierForageState04OG { Travel, Feed, Return, Rest, Watch }
    /// <summary>One local round trip on the existing calm creature motor. No combat, loot or spawn authority.</summary>
    public sealed class FrontierForage04OG
    {
        public FrontierForageState04OG State {get;private set;}
        private float _clock;
        private FrontierForageState04OG _resume;
        public void Tick(float dt,bool atDeposit,bool atHome,bool disturbed,bool storm)
        {
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt))return;
            if(disturbed){if(State!=FrontierForageState04OG.Watch)_resume=State;State=FrontierForageState04OG.Watch;return;}
            if(State==FrontierForageState04OG.Watch)State=_resume;
            if(storm && State!=FrontierForageState04OG.Rest){State=FrontierForageState04OG.Return;_clock=0;}
            switch(State)
            {
                case FrontierForageState04OG.Travel:if(atDeposit){State=FrontierForageState04OG.Feed;_clock=0;}break;
                case FrontierForageState04OG.Feed:_clock+=dt;if(_clock>=5){State=FrontierForageState04OG.Return;_clock=0;}break;
                case FrontierForageState04OG.Return:if(atHome){State=FrontierForageState04OG.Rest;_clock=0;}break;
                case FrontierForageState04OG.Rest:_clock+=dt;if(_clock>=7 && !storm){State=FrontierForageState04OG.Travel;_clock=0;}break;
            }
        }
    }
    public sealed partial class Creature
    {
        private readonly FrontierForage04OG _frontierForage04OG=new FrontierForage04OG();
        private Vector3 _frontierDeposit04OG;
        private bool _frontierDepositReady04OG;
        private bool ForageFrontier04OG(float dt)
        {
            if(Options==null || !Options.FrontierWildlife04OG || _flying || _territoryAerial)return false;
            if(!_frontierDepositReady04OG)
            {
                for(int attempt=0;attempt<8;attempt++)
                {
                    Vector3 offset=QMath.AY((Seed%8+attempt)*45)*Vector3.forward*3.2f;
                    Vector3 point=Home+offset;float floor;
                    if(Game.Layout!=null && (Game.Layout.IsInsideBuilding(point,1.4f) || Game.Layout.RoadAt(point,1.4f)!=null))continue;
                    if(!CreatureDirector.GroundAt(point,2,out floor) || Mathf.Abs(floor-Home.y)>.45f)continue;
                    point.y=floor;
                    if(Physics.CheckCapsule(point+Vector3.up*1.1f,point+Vector3.up*2,1f,Layers.WorldMask,QueryTriggerInteraction.Ignore))continue;
                    _frontierDeposit04OG=point;_frontierDepositReady04OG=true;break;
                }
                if(!_frontierDepositReady04OG)return false;
            }
            bool watch=Game.Player!=null && MathUtil.FlatSqrDistance(Game.PlayerPosition,transform.position)<12.25f;
            bool storm=Game.Atmo!=null && Game.Atmo.Current==Weather.Storm;
            _frontierForage04OG.Tick(dt,MathUtil.FlatSqrDistance(transform.position,_frontierDeposit04OG)<.5f,MathUtil.FlatSqrDistance(transform.position,Home)<.5f,watch,storm);
            FrontierForageState04OG state=_frontierForage04OG.State;
            if(state==FrontierForageState04OG.Travel || state==FrontierForageState04OG.Return)
                SetGoal(state==FrontierForageState04OG.Travel?_frontierDeposit04OG:Home,_sp!=null?_sp.WalkSpeed*.45f:.65f);
            else _hasGoal=false;
            return true;
        }
    }
}
