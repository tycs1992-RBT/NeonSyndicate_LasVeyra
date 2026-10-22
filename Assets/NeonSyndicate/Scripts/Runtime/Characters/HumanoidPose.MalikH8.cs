using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        private MalikDataH8 _reviewDataH8;
        private readonly MalikMotionH8 _reviewSamplerH8=new MalikMotionH8();
        private Vector3[] _reviewPointsH8,_reviewRetargetH8;
        private Quaternion[] _reviewRotationsH8;
        private float _reviewClockH8,_reviewBlendH8,_reviewLastRecoilH8;
        private int _reviewClipIndexH8=-1;
        public bool ReviewBlockedH8;
        public string ReviewClipH8 {get;private set;}
        /// <summary>Transient data assigned only by an explicitly validated Editor review. Null restores original posing.</summary>
        public void SetReviewMotionH8(MalikDataH8 data)
        {
            if(_reviewDataH8==data)return;
            _reviewDataH8=data;_reviewClockH8=0;_reviewBlendH8=0;_reviewLastRecoilH8=0;_reviewClipIndexH8=-1;ReviewClipH8="";ResetFeet();
            if(data!=null && _reviewPointsH8==null){_reviewPointsH8=new Vector3[17];_reviewRetargetH8=new Vector3[17];_reviewRotationsH8=new Quaternion[17];}
        }
        private void AuthoredBodyH8(ref PoseInput p,ref Body body,float dt)
        {
            ReviewClipH8="";
            if(_reviewDataH8==null || _creature)return;
            int select=_reviewSamplerH8.Select(ref p,ReviewBlockedH8,_reviewDataH8);
            Quaternion heading=QMath.AY(-p.RootYaw);Vector3 velocity=heading*p.Velocity;float speed=new Vector3(velocity.x,0,velocity.z).magnitude;
            // Only real forward cycles were authored. Strafing and reversing keep original direction-aware IK.
            if(select==1 && velocity.z<speed*.7f)select=-1;
            if(select<0){_reviewBlendH8=0;_reviewClockH8=0;_reviewLastRecoilH8=0;_reviewClipIndexH8=-1;return;}
            if(select!=_reviewClipIndexH8){_reviewClockH8=0;_reviewClipIndexH8=select;}
            // An existing weapon recoil rise restarts only the visual clip during rapid follow-up shots.
            // The gun owner still controls the muzzle, effects, hit timing, ammunition and original hand IK.
            if(_reviewDataH8.clips[select].name=="H8_FireRecoil" && p.Recoil>_reviewLastRecoilH8+.25f)_reviewClockH8=0;
            _reviewLastRecoilH8=p.Recoil;
            _reviewClockH8+=dt;_reviewBlendH8=Mathf.MoveTowards(_reviewBlendH8,1,dt*7);
            MalikClipH8 clip=_reviewDataH8.clips[select];float phase=select==4?MalikMotionH7.CutPhase(p.AttackT,p.AttackWindup,p.AttackActive):select==1?_phase:(_reviewClockH8%clip.duration)/clip.duration;
            if(select>=5)
            {
                if(clip.name=="H8_Dodge")phase=p.DodgeT;
                else if(clip.name=="H8_AegisLink")phase=p.RegionalSynergyT04OF;
                else if(clip.name=="H8_CrystalCast")phase=.45f;
                else if(clip.name=="H8_Guard_Brace")phase=.55f*Mathf.Clamp01(p.Guard);
                else if(clip.name=="H8_FireRecoil")phase=Mathf.Clamp01(_reviewClockH8/clip.duration);
            }
            if(phase<0 || !_reviewSamplerH8.Sample(clip,phase,_reviewPointsH8,_reviewRotationsH8))return;
            // Blend both genuine cycles at the same gait phase; the original distance-based cadence remains authoritative.
            if(select==1 && speed>3)
            {
                MalikClipH8 run=_reviewDataH8.clips[2];float w=Mathf.Clamp01((speed-3)/3);
                MalikFrameH8[] samples=run.samples;float t=Mathf.Clamp01(phase)*run.duration;int index=1;
                while(index<samples.Length-1 && samples[index].t<t)index++;
                MalikFrameH8 a=samples[index-1],b=samples[index];float u=Mathf.Clamp01((t-a.t)/Mathf.Max(.000001f,b.t-a.t));
                for(int i=0;i<17;i++){_reviewPointsH8[i]=Vector3.Lerp(_reviewPointsH8[i],Vector3.Lerp(a.p[i],b.p[i],u),w);_reviewRotationsH8[i]=QMath.Slerp(_reviewRotationsH8[i],QMath.Slerp(a.q[i],b.q[i],u),w);}
                if(w>.5f)clip=run;
            }
            _reviewSamplerH8.Retarget(_reviewDataH8,Layout,_reviewPointsH8,_reviewRetargetH8);
            Body authored=body;authored.Pelvis=_reviewRetargetH8[0];authored.Hips=_reviewRotationsH8[0];
            authored.Spine=QMath.Inverse(_reviewRotationsH8[0])*_reviewRotationsH8[1];
            authored.Chest=QMath.Inverse(_reviewRotationsH8[1])*_reviewRotationsH8[2];
            authored.Neck=QMath.Inverse(_reviewRotationsH8[2])*_reviewRotationsH8[3];
            authored.Head=QMath.Inverse(_reviewRotationsH8[3])*_reviewRotationsH8[4];
            // Translate each complete arm from the authored shoulder onto the actual original shoulder.
            SetArmH8(ref authored.AL,Shoulder(ref authored,-1),5,6,7,select==4);
            SetArmH8(ref authored.AR,Shoulder(ref authored,1),8,9,10,select==4);
            SetLegH8(ref authored.LL,11,12,13);SetLegH8(ref authored.LR,14,15,16);
            // Grounded ankle offset is calibrated to the actual rest pose; the existing Feet and SolveLeg retain
            // slope probes, world-space stance anchoring, reach limits and ground safety after this authored input.
            float ground=(Layout.World[13].y-_reviewDataH8.bind[13].y*Layout.Scale);
            authored.LL.Ankle.y=Mathf.Max(_ankleH,authored.LL.Ankle.y+ground);
            authored.LR.Ankle.y=Mathf.Max(_ankleH,authored.LR.Ankle.y+ground);
            if(select>=5 && clip.name!="H8_Dodge")
            {
                // Aim and casting retain original hand targets and contacts. Original AimPost and SupportHand
                // still align the actual gun and muzzle. The guardian uses only the authored supporting arm.
                authored.AR=body.AR;authored.LL=body.LL;authored.LR=body.LR;authored.Pelvis=body.Pelvis;authored.Hips=body.Hips;
                if(clip.name=="H8_FireAim" || clip.name=="H8_FireRecoil")authored.AL=body.AL;
                if(clip.name=="H8_AegisLink")authored.AL=Lerp(body.AL,authored.AL,1-QMath.SmoothRange(.85f,1,p.RegionalSynergyT04OF));
            }
            float weight=_reviewBlendH8;
            if(clip.name=="H8_Guard_Brace")weight*=Mathf.Clamp01(p.Guard);
            if(clip.name=="H8_CrystalCast")weight*=Mathf.Clamp01(p.CrystalCast04G);
            if(clip.name=="H8_FireAim")weight*=.45f;
            if(clip.name=="H8_FireRecoil")weight*=.65f*Mathf.Clamp01(p.Recoil/10f);

            if(select==4)weight*=Mathf.Min(1,Mathf.Min(p.AttackT/.08f,(1-p.AttackT)/.12f));
            Blend(ref body,ref authored,Mathf.Clamp01(weight));ReviewClipH8=clip.name;
        }
        private void SetArmH8(ref Arm arm,Vector3 shoulder,int upper,int fore,int hand,bool attack)
        {
            Vector3 offset=shoulder-_reviewRetargetH8[upper];arm.Hand=_reviewRetargetH8[hand]+offset;arm.Elbow=_reviewRetargetH8[fore]+offset;
            // The original move still supplies its blade/edge aiming, support-hand IK and finger grip.
            if(!attack){arm.Wrist=QMath.Inverse(_reviewRotationsH8[fore])*_reviewRotationsH8[hand];arm.HandW=0;}
        }
        private void SetLegH8(ref Leg leg,int thigh,int shin,int foot)
        {leg.Ankle=_reviewRetargetH8[foot];leg.KneeDir=(_reviewRetargetH8[shin]-_reviewRetargetH8[thigh]).normalized+Vector3.forward;leg.Foot=_reviewRotationsH8[foot];}
    }
}
