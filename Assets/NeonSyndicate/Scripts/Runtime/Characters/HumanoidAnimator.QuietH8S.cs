#if UNITY_EDITOR
namespace NeonSyndicate
{
    public sealed partial class HumanoidAnimator
    {
        private MalikDataH8 _quietMotionH8S;
        private NpcController _quietActorH8S;
        private string _quietCastH8S;
        private bool _quietTorsoOnlyH8S,_quietOwnedH8S,_priorAlwaysAnimateQuietH8S;

        /// <summary>True while the explicit quiet listening review owns part of this actor's pose.</summary>
        public bool QuietReviewActiveH8S{get{return _quietMotionH8S!=null;}}

        /// <summary>Starts the curated listening loop on the exact live canonical cast actor that owns this animator. Refuses a
        /// player, a substitute, a dead actor, an ineligible cast id and any data that fails the curated motion contract. The
        /// actor's brain, dialogue, schedule, navigation, damage and services are untouched; only pose blending is added.</summary>
        public bool BeginQuietReviewH8S(NpcController actor,string castId,MalikDataH8 data,bool torsoOnly,out string reason)
        {
            if(!QuietMotionH8S.Eligible(castId)){reason="Only a reviewed named cast member can receive the quiet loop.";return false;}
            if(actor==null || actor.IsPlayer || actor.IsDead || actor.Anim!=this){reason="Use the live canonical actor that owns this animator.";return false;}
            if(Game.Campaign==null || Game.Campaign.CastActor(castId)!=actor){reason="The exact canonical cast actor is required. No substitute is spawned.";return false;}
            MalikMotionH8 motion=new MalikMotionH8();
            if(!motion.Valid(data,out reason))return false;
            if(data.schema!=2 || motion.Find(data,QuietMotionH8S.ClipName)==null){reason="Quiet motion needs the curated H8S listening loop.";return false;}
            EndQuietReviewH8S();
            if(Pose==null || !Pose.SetQuietMotionH8S(data,torsoOnly)){reason="The pose owner refused the listening loop; the original pose stays.";return false;}
            _quietMotionH8S=data;_quietActorH8S=actor;_quietCastH8S=castId;_quietTorsoOnlyH8S=torsoOnly;
            _priorAlwaysAnimateQuietH8S=AlwaysAnimate;_quietOwnedH8S=true;AlwaysAnimate=true;
            reason="";return true;
        }

        /// <summary>Ends the quiet review and restores the original pose and animation cadence. Safe to call at any time.</summary>
        public void EndQuietReviewH8S()
        {
            _quietMotionH8S=null;_quietActorH8S=null;_quietCastH8S=null;
            if(Pose!=null){Pose.SetQuietMotionH8S(null,false);Pose.QuietBlockedH8S=false;Pose.QuietHeadYieldH8S=false;}
            if(_quietOwnedH8S){AlwaysAnimate=_priorAlwaysAnimateQuietH8S;_quietOwnedH8S=false;}
        }

        /// <summary>Per-frame ownership check before the solve: the review ends as soon as the actor is no longer the exact live
        /// cast member, and original gestures, directed dialogue and head-look take their bones back.</summary>
        private void PrepareQuietH8S()
        {
            if(_quietMotionH8S==null)return;
            if(_quietActorH8S==null || _quietActorH8S.IsDead || _quietActorH8S.Anim!=this || Game.Campaign==null || Game.Campaign.CastActor(_quietCastH8S)!=_quietActorH8S)
            {EndQuietReviewH8S();return;}
            if(Pose==null)return;
            if(!Pose.SetQuietMotionH8S(_quietMotionH8S,_quietTorsoOnlyH8S)){EndQuietReviewH8S();return;}
            Pose.QuietBlockedH8S=Frozen || _getUp>0f || HandsUp || Cower || Phone || Inspect || Point || Greeting
                || _inspectW>.05f || _pointW>.05f || (_performanceOwner04L!=null && UnityEngine.Time.time<_performanceUntil04L) || _performanceWeight04L>.05f;
            Pose.QuietHeadYieldH8S=HeadLook || _lookW>.05f;
        }
    }
}
#endif
