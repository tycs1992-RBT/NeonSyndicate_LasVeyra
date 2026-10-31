#if UNITY_EDITOR
namespace NeonSyndicate
{
    public sealed partial class HumanoidAnimator
    {
        private bool _reviewLandingH8S=true;
        private float _reviewContactH8S=LandingTriggerH8S.HeavyContactTime;

        /// <summary>The Editor review's landing choices for this actor: on or off, and the touchdown time used for the heavy
        /// supplied landing. Kept here and pushed into the pose every frame, so a rebuilt pose keeps them.</summary>
        public void SetReviewLandingH8S(bool enabled,float contactTime)
        {_reviewLandingH8S=enabled;_reviewContactH8S=LandingTriggerH8S.ClampContact(contactTime);}

        private void PushLandingH8S()
        {
            if(Pose==null)return;
            Pose.ReviewLandingH8S=_reviewLandingH8S;Pose.LandingContactH8S=_reviewContactH8S;
        }
    }
}
#endif
