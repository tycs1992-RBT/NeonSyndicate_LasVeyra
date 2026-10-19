using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class PlayerCombat
    {
        private bool _bladePending04N,_bladeSample04N;private Vector3 _bladeHilt04N,_bladeTip04N;
        private float _bladeSampleTime04N,_bladeFrom04N,_bladeTo04N,_bladeMult04N;private long _bladeSampleSerial04N;
        private MeleeMove _bladeMove04N;private bool _bladeProps04N,_bladeCrit04N;
        private bool QueueBlade04N(MeleeMove move,float multiplier,float previous,bool props)
        {
            Vector3 h,t;float radius;
            if(!P.Weapons.BladeSegment04N(_moveWeapon,P.Anim.AttackMirror,out h,out t,out radius))return false;
            _bladePending04N=true;_bladeMove04N=move;_bladeMult04N=multiplier;_bladeProps04N=props;_bladeCrit04N=_skillCrit;
            float span=Mathf.Max(.0001f,_t-previous);
            _bladeFrom04N=Mathf.Clamp01((Mathf.Max(previous,move.Windup)-previous)/span);
            _bladeTo04N=Mathf.Clamp01((Mathf.Min(_t,move.Windup+move.Active)-previous)/span);
            return true;
        }
        /// <summary>HumanoidAnimator calls this after applying the player's real hand pose. Same Strike owner and hit list.</summary>
        public void ResolveBladeAfterPose04N()
        {
            Vector3 h=Vector3.zero,t=Vector3.zero;float radius=0;bool sample=P.Weapons!=null && P.Anim!=null && P.Weapons.BladeSegment04N(_moveWeapon,P.Anim.AttackMirror,out h,out t,out radius);
            if(!sample){_bladeSample04N=false;_bladePending04N=false;return;}
            if(_bladePending04N)
            {
                _bladePending04N=false;
                bool valid=!P.IsDead && Game.IsPlaying && !GameTime.Paused && State==Phase.Attacking && _bladeSample04N && _bladeSampleSerial04N==ActionSerial04N &&
                    BladeSweep04N.HistoryValid(Time.time-_bladeSampleTime04N,Vector3.Distance(h,_bladeHilt04N));
                if(valid)
                {
                    BladeSweep04N sweep=BladeSweep04N.Clip(_bladeHilt04N,_bladeTip04N,h,t,radius,_bladeFrom04N,_bladeTo04N);
                    bool killed;int before=_hits.Count;
                    int n=CombatDirector.Strike(P,_bladeMove04N,_moveBaseDamage,_bladeMult04N,_moveWeapon,_hits,out killed,0,1,P.Anim.AttackMirror,_bladeProps04N,sweep);
                    if(_bladeCrit04N && _hits.Count>before)ProgressionHooks.ShowMeleeCrit();
                    if(n>0 && _hits.Count>before && !_hitAny){_hitAny=true;OnLandedHit(_bladeMove04N,killed);}
                }
            }
            _bladeSample04N=true;_bladeSampleSerial04N=ActionSerial04N;_bladeHilt04N=h;_bladeTip04N=t;_bladeSampleTime04N=Time.time;
        }
    }
}
