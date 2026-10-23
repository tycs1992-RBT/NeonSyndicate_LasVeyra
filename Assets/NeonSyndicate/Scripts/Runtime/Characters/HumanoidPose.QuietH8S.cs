using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private MalikDataH8 _quietDataH8S;
        private MalikClipH8 _quietClipH8S;
        private Quaternion _quietTurnH8S=QMath.Identity;
        private bool _quietTorsoOnlyH8S;
        private float _quietClockH8S,_quietWeightH8S,_quietHeadH8S;
        private Vector3[] _quietPointsH8S,_quietRetargetH8S;
        private Quaternion[] _quietRotationsH8S;
        /// <summary>Set every frame by the animator's review owner: an original owner (gesture, directed dialogue, get-up,
        /// freeze) needs the whole body, so the quiet loop fades out completely.</summary>
        public bool QuietBlockedH8S;
        /// <summary>Set every frame by the animator's review owner: the original head-look owns the head and neck, so the
        /// quiet loop eases them back to the original pose and keeps only the spine, chest and arms.</summary>
        public bool QuietHeadYieldH8S;
        /// <summary>Name of the quiet clip blended this frame, or empty.</summary>
        public string QuietClipH8S{get;private set;}="";
        /// <summary>Current quiet blend weight (0 to 1).</summary>
        public float QuietWeightH8S{get{return _quietWeightH8S;}}
        /// <summary>How much of the head and neck the original head-look has taken back (0 to 1, eased).</summary>
        public float QuietHeadYieldWeightH8S{get{return _quietHeadH8S;}}

        /// <summary>Assigns the curated listening loop from an explicitly validated Editor review, or clears it with null.
        /// The data is validated again here, so a malformed set can never reach the solver. Returns false (and keeps the
        /// original pose) when the data fails the motion contract or lacks a looping listening clip.</summary>
        public bool SetQuietMotionH8S(MalikDataH8 data,bool torsoOnly)
        {
            _quietTorsoOnlyH8S=torsoOnly;
            if(data==_quietDataH8S)return data==null || _quietClipH8S!=null;
            ClearQuietH8S();
            if(data==null)return true;
            string reason;
            MalikClipH8 clip=_reviewSamplerH8.Valid(data,out reason) && data.schema==2?_reviewSamplerH8.Find(data,QuietMotionH8S.ClipName):null;
            if(clip==null || !clip.loop || clip.duration<=0f)return false;
            _quietDataH8S=data;_quietClipH8S=clip;_quietTurnH8S=MalikMotionH8.FacingCorrection(clip);
            if(_quietPointsH8S==null){_quietPointsH8S=new Vector3[17];_quietRetargetH8S=new Vector3[17];_quietRotationsH8S=new Quaternion[17];}
            return true;
        }
        private void ClearQuietH8S(){_quietDataH8S=null;_quietClipH8S=null;_quietTurnH8S=QMath.Identity;_quietClockH8S=0f;_quietWeightH8S=0f;_quietHeadH8S=0f;QuietClipH8S="";}

        /// <summary>Blends the listening loop into a standing named cast member. Legs, feet and pelvis stay original: the cast
        /// actor keeps its planted stance, navigation and collision. Never runs for the player or creatures.</summary>
        private void QuietBodyH8S(ref PoseInput p,ref Body body,float dt)
        {
            QuietClipH8S="";
            if(_quietDataH8S==null || _quietClipH8S==null)return;
            bool blocked=QuietMotionH8S.Blocked(ref p,QuietBlockedH8S);
            _quietWeightH8S=Mathf.MoveTowards(_quietWeightH8S,blocked?0f:1f,dt*(blocked?QuietMotionH8S.BlendOutRate:QuietMotionH8S.BlendInRate));
            _quietHeadH8S=Mathf.MoveTowards(_quietHeadH8S,QuietHeadYieldH8S?1f:0f,dt*QuietMotionH8S.HeadYieldRate);
            if(_quietWeightH8S<=.001f){_quietWeightH8S=0f;return;}
            _quietClockH8S=(_quietClockH8S+dt)%_quietClipH8S.duration;
            if(!_reviewSamplerH8.Sample(_quietClipH8S,_quietClockH8S/_quietClipH8S.duration,_quietPointsH8S,_quietRotationsH8S))return;
            MalikMotionH8.TurnSample(_quietTurnH8S,_quietPointsH8S,_quietRotationsH8S);
            _reviewSamplerH8.Retarget(_quietDataH8S,Layout,_quietPointsH8S,_quietRetargetH8S);
            Body authored=body;
            authored.Spine=QMath.Inverse(_quietRotationsH8S[0])*_quietRotationsH8S[1];
            authored.Chest=QMath.Inverse(_quietRotationsH8S[1])*_quietRotationsH8S[2];
            authored.Neck=QMath.Slerp(QMath.Inverse(_quietRotationsH8S[2])*_quietRotationsH8S[3],body.Neck,_quietHeadH8S);
            authored.Head=QMath.Slerp(QMath.Inverse(_quietRotationsH8S[3])*_quietRotationsH8S[4],body.Head,_quietHeadH8S);
            bool torsoOnly=QuietMotionH8S.TorsoOnly(_quietTorsoOnlyH8S,ref p);
            if(!torsoOnly)
            {
                // The loop's arms were authored around its own hips; the actor keeps its original hips, so carry the
                // authored arm shape into the actual hips frame before placing it on the actual shoulders.
                Quaternion frame=body.Hips*QMath.Inverse(_quietRotationsH8S[0]);
                QuietArmH8S(ref authored,ref authored.AL,frame,-1,5,6,7);
                QuietArmH8S(ref authored,ref authored.AR,frame,1,8,9,10);
            }
            BlendTorso(ref body,ref authored,_quietWeightH8S);
            if(!torsoOnly)BlendArms(ref body,ref authored,_quietWeightH8S);
            QuietClipH8S=_quietClipH8S.name;
        }

        /// <summary>One authored arm, expressed in the actual hips frame and placed on the actual shoulder, with the wrist kept
        /// clear in front of the chest so the folded pose of a different body never sinks into this one.</summary>
        private void QuietArmH8S(ref Body authored,ref Arm arm,Quaternion frame,int side,int upper,int fore,int hand)
        {
            Vector3 shoulder=Shoulder(ref authored,side);
            arm.Hand=shoulder+frame*(_quietRetargetH8S[hand]-_quietRetargetH8S[upper]);
            arm.Elbow=shoulder+frame*(_quietRetargetH8S[fore]-_quietRetargetH8S[upper]);
            Quaternion chest=ChestRot(ref authored);
            Vector3 local=QMath.Inverse(chest)*(arm.Hand-ChestPos(ref authored));
            float clear=QuietMotionH8S.ChestClearance*_s;
            if(local.z<clear){local.z=clear;arm.Hand=ChestPos(ref authored)+chest*local;}
            arm.Wrist=QMath.Inverse(_quietRotationsH8S[fore])*_quietRotationsH8S[hand];arm.HandW=0f;
        }
    }
}
