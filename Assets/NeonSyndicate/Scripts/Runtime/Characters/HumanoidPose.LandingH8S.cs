using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private readonly LandingTriggerH8S _landingH8S=new LandingTriggerH8S();
        private MalikClipH8 _landingSmallH8S,_landingHeavyH8S;
        private Quaternion _landingSmallTurnH8S=QMath.Identity,_landingHeavyTurnH8S=QMath.Identity;
        private Vector3 _landingRootH8S;
        private bool _landingRootKnownH8S;
        /// <summary>Editor review switch for the authored H8S landing. Only curated schema 2 data carries the landing; older
        /// review data keeps the original procedural landing dip exactly as before. The animator's review owner pushes it
        /// every frame, so a rebuilt pose keeps the reviewer's choice.</summary>
        public bool ReviewLandingH8S=true;
        /// <summary>Seconds into the heavy supplied landing treated as touchdown (see LandingTriggerH8S.HeavyContactTime).</summary>
        public float LandingContactH8S=LandingTriggerH8S.HeavyContactTime;
        /// <summary>True while an authored landing is blended into the review pose.</summary>
        public bool LandingActiveH8S{get{return _landingH8S.Active;}}
        /// <summary>The landing detector, read by tests and the Editor review panel.</summary>
        public LandingTriggerH8S LandingH8S{get{return _landingH8S;}}

        /// <summary>Caches the curated landing clips and their facing corrections when review data changes. Schema 1 data
        /// never lands.</summary>
        private void PrepareLandingH8S(MalikDataH8 data)
        {
            _landingH8S.Reset();_landingRootKnownH8S=false;
            bool curated=data!=null && data.schema==2;
            _landingSmallH8S=curated?_reviewSamplerH8.Find(data,LandingTriggerH8S.SmallClip):null;
            _landingHeavyH8S=curated?_reviewSamplerH8.Find(data,LandingTriggerH8S.HeavyClip):null;
            _landingSmallTurnH8S=MalikMotionH8.FacingCorrection(_landingSmallH8S);
            _landingHeavyTurnH8S=MalikMotionH8.FacingCorrection(_landingHeavyH8S);
        }

        /// <summary>Plays the authored landing after real air time. Returns true when it owned the authored part of this frame.
        /// The original Feet, smoothing, reach limits, weapon IK and controller run afterwards as always.</summary>
        private bool LandingBodyH8S(ref PoseInput p,ref Body body,float dt)
        {
            // A teleport (respawn, mission restart, fast travel) forgets the flight: a fall never lands somewhere else.
            if(_landingRootKnownH8S && (p.RootPosition-_landingRootH8S).sqrMagnitude>4f)_landingH8S.Reset();
            _landingRootH8S=p.RootPosition;_landingRootKnownH8S=true;
            float contact=LandingTriggerH8S.ClampContact(LandingContactH8S);
            float smallLength=_landingSmallH8S!=null?_landingSmallH8S.duration:0f;
            float heavyLength=_landingHeavyH8S!=null?LandingTriggerH8S.HeavyPlayLength(_landingHeavyH8S.duration,contact):0f;
            bool usable=ReviewLandingH8S && (smallLength>0f || heavyLength>0f);
            _landingH8S.Step(p.Grounded,!usable || LandingTriggerH8S.Unsafe(ref p,ReviewBlockedH8),dt,smallLength,heavyLength);
            if(!_landingH8S.Active)return false;
            bool heavy=_landingH8S.Heavy;
            MalikClipH8 clip=heavy?_landingHeavyH8S:_landingSmallH8S;
            if(clip==null || !_reviewSamplerH8.Sample(clip,_landingH8S.Phase(clip.duration,contact),_reviewPointsH8,_reviewRotationsH8))return false;
            // The supplied retarget stands turned off forward; face it where the controller faces.
            MalikMotionH8.TurnSample(heavy?_landingHeavyTurnH8S:_landingSmallTurnH8S,_reviewPointsH8,_reviewRotationsH8);
            _reviewSamplerH8.Retarget(_reviewDataH8,Layout,_reviewPointsH8,_reviewRetargetH8);
            Body authored=body;
            authored.Hips=_reviewRotationsH8[0];
            authored.Spine=QMath.Inverse(_reviewRotationsH8[0])*_reviewRotationsH8[1];
            authored.Chest=QMath.Inverse(_reviewRotationsH8[1])*_reviewRotationsH8[2];
            authored.Neck=QMath.Inverse(_reviewRotationsH8[2])*_reviewRotationsH8[3];
            authored.Head=QMath.Inverse(_reviewRotationsH8[3])*_reviewRotationsH8[4];
            SetLegH8(ref authored.LL,11,12,13);SetLegH8(ref authored.LR,14,15,16);
            // The supplied action carries no root translation: its knees bend while its hips stay put, so its feet rise.
            // Lower the pelvis until the lower foot is back at its rest height; the bend then becomes a real crouch.
            float ground=Layout.World[13].y-_reviewDataH8.bind[13].y*Layout.Scale;
            float lift=Mathf.Min(authored.LL.Ankle.y,authored.LR.Ankle.y)+ground-_ankleH;
            float drop=Mathf.Clamp(lift,0f,LandingTriggerH8S.MaxDrop*_s);
            authored.Pelvis=_reviewRetargetH8[0];authored.Pelvis.y-=drop;
            authored.LL.Ankle.y=Mathf.Max(_ankleH,authored.LL.Ankle.y+ground-drop);
            authored.LR.Ankle.y=Mathf.Max(_ankleH,authored.LR.Ankle.y+ground-drop);
            Quaternion turn=ChestRot(ref authored)*QMath.Inverse(ChestRot(ref body));
            Arm carriedL=CarryArmH8S(body.AL,Shoulder(ref body,-1),Shoulder(ref authored,-1),turn);
            Arm carriedR=CarryArmH8S(body.AR,Shoulder(ref body,1),Shoulder(ref authored,1),turn);
            if(p.Hold==HoldType.None && !p.Blade)
            {
                SetArmH8(ref authored.AL,Shoulder(ref authored,-1),5,6,7,false);
                SetArmH8(ref authored.AR,Shoulder(ref authored,1),8,9,10,false);
                // The end of the supplied action drifts toward a different idle (arms folding); hand the arms back early.
                if(heavy)
                {
                    float back=QMath.SmoothRange(.55f,.85f,_landingH8S.Progress);
                    authored.AL=Lerp(authored.AL,carriedL,back);authored.AR=Lerp(authored.AR,carriedR,back);
                }
            }
            else
            {
                // A carried gun or blade keeps its original grip: the whole arm moves with its shoulder and the chest turn.
                authored.AL=carriedL;authored.AR=carriedR;
            }
            float speed=Mathf.Sqrt(p.Velocity.x*p.Velocity.x+p.Velocity.z*p.Velocity.z);
            Blend(ref body,ref authored,Mathf.Clamp01(_landingH8S.Weight(speed)));
            ReviewClipH8=clip.name;
            // The regular review selection blends in afresh once the landing has finished.
            _reviewBlendH8=0f;_reviewClockH8=0f;_reviewClipIndexH8=-1;
            return true;
        }

        /// <summary>An original arm target moved rigidly from one shoulder pose to another (hand, elbow and blade direction).</summary>
        private static Arm CarryArmH8S(Arm arm,Vector3 fromShoulder,Vector3 toShoulder,Quaternion turn)
        {
            arm.Hand=toShoulder+turn*(arm.Hand-fromShoulder);
            arm.Elbow=toShoulder+turn*(arm.Elbow-fromShoulder);
            arm.Blade=turn*arm.Blade;arm.Edge=turn*arm.Edge;
            return arm;
        }
    }
}
