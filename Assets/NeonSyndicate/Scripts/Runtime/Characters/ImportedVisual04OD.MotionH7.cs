#if UNITY_EDITOR
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class ImportedVisual04OD
    {
        private MalikMotionDataH7 _motionH7;
        private MalikMotionClipH7 _motionClipH7;
        private float _motionPhaseH7,_motionWeightH7,_idleClockH7;
        public static bool ReviewMotionH7(CharacterBase owner,MalikMotionDataH7 motion,out string reason)
        {
            reason="Start the reversible H7 review first.";
            ImportedVisual04OD adapter=owner!=null?owner.GetComponent<ImportedVisual04OD>():null;
            if(adapter==null || !adapter._ready || adapter._reviewH6==null || adapter._reviewH6.ArtProof.AssetId!=MalikReviewH7.AssetId || !MalikMotionH7.Valid(motion,out reason))return false;
            adapter._motionH7=motion;adapter._idleClockH7=0;reason="";return true;
        }
        private void ClearMotionH7(){_motionH7=null;_motionClipH7=null;_motionWeightH7=0;_idleClockH7=0;}
        private void PrepareMotionH7()
        {
            _motionClipH7=null;_motionWeightH7=0;
            HumanoidAnimator a=_owner!=null?_owner.Anim:null;
            if(_motionH7==null || a==null || GameTime.Paused || a.Frozen || !a.Grounded || a.SitWeight>.01f || a.CrouchWeight>.01f || a.Travel!=TravelPose.None || a.Ride!=RidePose.None || a.TravelBlend>.01f || a.Cover>.01f || a.Aiming || a.HandsUp || a.Cower || a.Phone || a.Inspect || a.Point || a.HeadLook || a.ParcelCarry04G || a.Guard>.01f || a.CrystalCast04G>.01f || a.SynergyCast04N>.01f || a.DodgeT>=0 || a.StaggerT>=0 || a.DrinkT>=0 || a.ParryT>=0 || a.ThrowT>=0 || a.MeleeT>=0){_idleClockH7=0;return;}
            if(a.AttackT>=0)
            {
                _idleClockH7=0;
                if(!a.HoldBlade || a.AttackMirror || a.AttackAnim!=MeleeAnim.SlashRight)return;
                float phase=MalikMotionH7.CutPhase(a.AttackT,a.AttackWindup,a.AttackActive);if(phase<0)return;
                _motionClipH7=_motionH7.clips[4];_motionPhaseH7=phase;_motionWeightH7=.25f*Mathf.Sin(Mathf.PI*phase);return;
            }
            if(a.Velocity.sqrMagnitude>.01f || a.CombatReady>.01f){_idleClockH7=0;return;}
            _idleClockH7+=Time.deltaTime;_motionClipH7=_motionH7.clips[0];_motionPhaseH7=(_idleClockH7%_motionClipH7.duration)/_motionClipH7.duration;_motionWeightH7=.25f*Mathf.Clamp01(_idleClockH7/.3f);
        }
        private Quaternion ComposeMotionH7(int semantic,Quaternion original)
        {
            if(_motionClipH7==null || semantic<RigLayout.Chest || semantic>RigLayout.Head)return original;
            Quaternion accent=MalikMotionH7.Accent(MalikMotionH7.Sample(_motionClipH7,semantic,_motionPhaseH7),_motionWeightH7);
            Quaternion root=transform.rotation;return root*accent*QMath.Inverse(root)*original;
        }
    }
}
#endif
