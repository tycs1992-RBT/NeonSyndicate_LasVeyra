#if UNITY_EDITOR
namespace NeonSyndicate
{
    public sealed partial class HumanoidAnimator
    {
        private MalikDataH8 _reviewMotionH8;
        private bool _priorAlwaysAnimateH8,_reviewOwnedH8;
        public bool BeginFullBodyReviewH8(MalikDataH8 data,out string reason)
        {
            if(!new MalikMotionH8().Valid(data,out reason))return false;
            if(Game.Player==null || Game.Player.Anim!=this || !ImportedVisual04OD.IsReviewActiveH6(Game.Player)){reason="Start the validated reversible H7 visual before full-body playback.";return false;}
            EndFullBodyReviewH8();_priorAlwaysAnimateH8=AlwaysAnimate;_reviewOwnedH8=true;AlwaysAnimate=true;_reviewMotionH8=data;ResetFeet();reason="";return true;
        }
        public void EndFullBodyReviewH8()
        {
            _reviewMotionH8=null;if(Pose!=null)Pose.SetReviewMotionH8(null);
            if(_reviewOwnedH8){AlwaysAnimate=_priorAlwaysAnimateH8;_reviewOwnedH8=false;}ResetFeet();
        }
        private void PrepareFullBodyH8()
        {
            if(_reviewMotionH8!=null && (Game.Player==null || Game.Player.Anim!=this || !ImportedVisual04OD.IsReviewActiveH6(Game.Player)))EndFullBodyReviewH8();
            Pose.SetReviewMotionH8(_reviewMotionH8);
            Pose.ReviewBlockedH8=Frozen || _getUp>0 || HandsUp || Cower || Phone || Inspect || Point || HeadLook || Greeting;
            PushLandingH8S();
            PrepareQuietH8S();
        }
    }
}
#endif
