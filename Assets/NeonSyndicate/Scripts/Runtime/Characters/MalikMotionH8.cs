using System;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class MalikFrameH8 {public float t;public Vector3[] p;public Quaternion[] q;}
    [Serializable] public sealed class MalikClipH8 {public string name;public float duration;public bool loop;public MalikFrameH8[] samples;}
    [Serializable] public sealed class MalikDataH8
    {
        public int schema;public string assetId,sourceSha256,space;public bool rootMotion,events;public Vector3[] bind;public MalikClipH8[] clips;
    }
    /// <summary>Actual authored joint sampling and length retarget. No native Animator, motor, input or hit event.</summary>
    public sealed class MalikMotionH8
    {
        public const string Space="canonical actor-local joint positions and world rotation deltas; reflected Blender (-x,z,-y)";
        /// <summary>Schema 2 tracks are sampled at exactly this rate (first sample at 0, last at the clip duration).</summary>
        public const float CuratedSampleRate=30f;
        /// <summary>Largest joint travel between two schema 2 samples (delivered maximum outside the guard brace: 0.25 m in the
        /// skipped airborne lead-in of the supplied landing).</summary>
        public const float CuratedMaxStep=.30f;
        /// <summary>Smallest |dot| between consecutive schema 2 rotations: a step of at most 50 degrees (delivered maximum
        /// outside the guard brace: 44.8 degrees, again in the skipped lead-in of the supplied landing).</summary>
        public const float CuratedMinStepDot=.9063f;
        /// <summary>The inherited H8R guard brace keeps its reviewed limits: it snaps 92.8 degrees and 0.29 m between its
        /// third and fourth samples while the guard weight is still ramping in. A known source defect, recorded for repair.</summary>
        public const string InheritedGuardClip="H8_Guard_Brace";
        public const float InheritedGuardMaxStep=.35f,InheritedGuardMinStepDot=.6428f;
        private bool Finite(float v){return !float.IsNaN(v) && !float.IsInfinity(v);}
        private bool Finite(Vector3 v){return Finite(v.x)&&Finite(v.y)&&Finite(v.z);}
        /// <summary>The curated H8S set, in its exact delivered order: the fifteen reviewed H8R actions plus the supplied
        /// landing and listening retargets. A schema 2 file must match this list exactly.</summary>
        public static string[] CuratedClipsH8S()
        {
            return new[]{"H7_IdleBreath","H7_Walk","H7_Run","H7_SwordReady","H7_OathCut","H8_AegisLink","H8_CrystalCast","H8_Dodge","H8_FireAim","H8_FireRecoil",
                "H8_FlightLanding","H8_FlightTakeoff","H8_Guard_Brace","H8_JumpLanding",LandingTriggerH8S.HeavyClip,QuietMotionH8S.ClipName,"H8_SurgeRise"};
        }
        public bool Valid(MalikDataH8 data,out string reason)
        {
            reason="Full-body H8 data failed its fixed skeleton, bounded-motion or no-event contract.";
            if(data==null || (data.schema!=1 && data.schema!=2) || (data.assetId!=MalikReviewH7.AssetId && data.assetId!=MalikReviewH8R.AssetId) || data.space!=Space || data.rootMotion || data.events || !ImportedArt04OH.Hex(data.sourceSha256,64) || data.bind==null || data.bind.Length!=17 || data.clips==null || data.clips.Length<5 || data.clips.Length>20)return false;
            if(data.schema==2){if(!ValidCurated(data,out reason))return false;reason="Full-body H8 data failed its fixed skeleton, bounded-motion or no-event contract.";}
            else if(data.assetId==MalikReviewH8R.AssetId && (data.clips.Length!=15 || Find(data,"H8_Guard_Brace")==null))return false;
            foreach(Vector3 b in data.bind)if(!Finite(b) || b.sqrMagnitude>9)return false;
            string[] required={"H7_IdleBreath","H7_Walk","H7_Run","H7_SwordReady","H7_OathCut"};
            for(int c=0;c<data.clips.Length;c++)
            {
                MalikClipH8 clip=data.clips[c];
                if(clip==null || string.IsNullOrEmpty(clip.name) || (c<5 && clip.name!=required[c]) || (c>=5 && !clip.name.StartsWith("H8_",StringComparison.Ordinal)) || !Finite(clip.duration) || clip.duration<.15f || clip.duration>4 || clip.samples==null || clip.samples.Length<3 || clip.samples.Length>181 || (c<5 && clip.loop!=(c<4)))return false;
                for(int k=0;k<c;k++)if(data.clips[k].name==clip.name)return false;
                float previous=-1;
                foreach(MalikFrameH8 frame in clip.samples)
                {
                    if(frame==null || !Finite(frame.t) || frame.t<=previous || frame.t<0 || frame.t>clip.duration+.0001f || frame.p==null || frame.p.Length!=17 || frame.q==null || frame.q.Length!=17)return false;
                    for(int i=0;i<17;i++)
                    {
                        Quaternion q=frame.q[i];float norm=QMath.Dot(q,q);
                        if(!Finite(frame.p[i]) || frame.p[i].sqrMagnitude>16 || !Finite(norm) || Mathf.Abs(norm-1)>.002f)return false;
                        if(i>0 && (frame.p[i]-frame.p[RigLayout.Parent[i]]).sqrMagnitude<.000001f)return false;
                    }
                    Vector3 hip=frame.p[0]-data.bind[0];if(Mathf.Abs(hip.x)>.25f || Mathf.Abs(hip.z)>.25f || Mathf.Abs(hip.y)>.65f)return false;
                    previous=frame.t;
                }
                if(clip.samples[0].t!=0 || Mathf.Abs(previous-clip.duration)>.0001f)return false;
                if(clip.loop)for(int i=0;i<17;i++)if((clip.samples[0].p[i]-clip.samples[clip.samples.Length-1].p[i]).sqrMagnitude>.000001f || Mathf.Abs(QMath.Dot(clip.samples[0].q[i],clip.samples[clip.samples.Length-1].q[i]))<.99999f)return false;
            }
            reason="";return true;
        }
        /// <summary>Schema 2 only: the selected human, the exact curated clip list and loop flags, a uniform 30 Hz track with
        /// complete joints, continuous positions and rotations, and the delivered durations of the two supplied retargets.
        /// The shared per-frame bounds in Valid still apply afterwards.</summary>
        private bool ValidCurated(MalikDataH8 data,out string reason)
        {
            reason="Schema 2 must be the exact curated H8S set for the selected human: wrong id, clip list, loop flag, sampling or a broken track.";
            string[] names=CuratedClipsH8S();
            if(data.assetId!=MalikReviewH8R.AssetId || data.clips.Length!=names.Length || !SelectedSkeleton(data.bind))return false;
            for(int c=0;c<names.Length;c++)
            {
                MalikClipH8 clip=data.clips[c];
                if(clip==null || clip.name!=names[c] || clip.samples==null || !Finite(clip.duration) || clip.duration<.15f || clip.duration>4)return false;
                bool loop=c<4 || clip.name=="H8_FireAim" || clip.name==QuietMotionH8S.ClipName;
                if(clip.loop!=loop || clip.samples.Length!=Mathf.RoundToInt(clip.duration*CuratedSampleRate)+1)return false;
                bool guard=clip.name==InheritedGuardClip;
                float maxStep=guard?InheritedGuardMaxStep:CuratedMaxStep,minDot=guard?InheritedGuardMinStepDot:CuratedMinStepDot;
                for(int k=0;k<clip.samples.Length;k++)
                {
                    MalikFrameH8 frame=clip.samples[k];
                    if(frame==null || !Finite(frame.t) || Mathf.Abs(frame.t-k/CuratedSampleRate)>.002f || frame.p==null || frame.p.Length!=17 || frame.q==null || frame.q.Length!=17)return false;
                    if(k==0)continue;
                    MalikFrameH8 previous=clip.samples[k-1];
                    for(int i=0;i<17;i++)
                    {
                        if(!Finite(frame.p[i]) || (frame.p[i]-previous.p[i]).sqrMagnitude>maxStep*maxStep)return false;
                        float dot=Mathf.Abs(QMath.Dot(frame.q[i],previous.q[i]));
                        if(!Finite(dot) || dot<minDot)return false;
                    }
                }
            }
            MalikClipH8 landing=Find(data,LandingTriggerH8S.HeavyClip),listening=Find(data,QuietMotionH8S.ClipName);
            if(Mathf.Abs(landing.duration-LandingTriggerH8S.HeavyDuration)>.01f || Mathf.Abs(listening.duration-QuietMotionH8S.Duration)>.01f)return false;
            reason="";return true;
        }
        /// <summary>The bind must be the selected adult skeleton: hips between 0.9 and 1.2 m, a head above the hips and mirrored
        /// left and right limbs within a centimetre. Data for a different body cannot pose as the selected human.</summary>
        private bool SelectedSkeleton(Vector3[] bind)
        {
            if(bind==null || bind.Length!=17)return false;
            if(bind[0].y<.9f || bind[0].y>1.2f || bind[4].y<bind[0].y+.5f)return false;
            for(int i=5;i<8;i++)if(!Mirrored(bind[i],bind[i+3]))return false;
            for(int i=11;i<14;i++)if(!Mirrored(bind[i],bind[i+3]))return false;
            return true;
        }
        private static bool Mirrored(Vector3 left,Vector3 right)
        {return left.x<0f && Mathf.Abs(left.x+right.x)<.01f && Mathf.Abs(left.y-right.y)<.01f && Mathf.Abs(left.z-right.z)<.01f;}
        /// <summary>A supplied retarget can stand turned about the vertical axis (the delivered landing and listening clips face
        /// 13 to 21 degrees off forward, measured from the hip line). This returns the yaw that cancels the clip's mean facing,
        /// to be applied to every sampled position and rotation of that clip. Malik's own clips face forward (identity).</summary>
        public static Quaternion FacingCorrection(MalikClipH8 clip)
        {
            if(clip==null || clip.samples==null || clip.samples.Length==0)return QMath.Identity;
            float sum=0f;int count=0;
            foreach(MalikFrameH8 frame in clip.samples)
            {
                if(frame==null || frame.p==null || frame.p.Length<17)continue;
                Vector3 line=frame.p[14]-frame.p[11];
                if(line.x*line.x+line.z*line.z<.0001f)continue;
                sum+=Mathf.Atan2(-line.z,line.x)*Mathf.Rad2Deg;count++;
            }
            float yaw=count>0?sum/count:0f;
            return Mathf.Abs(yaw)<.5f?QMath.Identity:QMath.AY(-yaw);
        }
        /// <summary>Applies a facing correction to sampled buffers in place (no allocation).</summary>
        public static void TurnSample(Quaternion correction,Vector3[] points,Quaternion[] rotations)
        {
            for(int i=0;i<17;i++){points[i]=correction*points[i];rotations[i]=correction*rotations[i];}
        }
        public MalikClipH8 Find(MalikDataH8 data,string name)
        {if(data!=null && data.clips!=null)foreach(MalikClipH8 c in data.clips)if(c!=null && c.name==name)return c;return null;}
        /// <summary>Caller owns reusable buffers. Interpolation never allocates per frame.</summary>
        public bool Sample(MalikClipH8 clip,float phase,Vector3[] points,Quaternion[] rotations)
        {
            if(clip==null || !Finite(phase) || points==null || points.Length<17 || rotations==null || rotations.Length<17 || clip.samples==null || clip.samples.Length<2)return false;
            float t=Mathf.Clamp01(phase)*clip.duration;int index=1;
            while(index<clip.samples.Length-1 && clip.samples[index].t<t)index++;
            MalikFrameH8 a=clip.samples[index-1],b=clip.samples[index];float w=Mathf.Clamp01((t-a.t)/Mathf.Max(.000001f,b.t-a.t));
            for(int i=0;i<17;i++){points[i]=Vector3.Lerp(a.p[i],b.p[i],w);rotations[i]=QMath.Slerp(a.q[i],b.q[i],w);}return true;
        }
        public void Retarget(MalikDataH8 data,RigLayout rig,Vector3[] points,Vector3[] output)
        {
            output[0]=rig.Local[0]+(points[0]-data.bind[0])*rig.Scale;
            for(int i=1;i<17;i++){int parent=RigLayout.Parent[i];Vector3 d=points[i]-points[parent];output[i]=output[parent]+d.normalized*rig.Local[i].magnitude;}
        }
        /// <summary>Preserve original unsupported actions. Authored data cannot steal aim, guard or interruption.</summary>
        public int Select(ref PoseInput p,bool blocked,MalikDataH8 data=null)
        {
            if(blocked || p.Sit>.01f || p.Crouch>.01f || p.Ride!=RidePose.None || p.Travel!=TravelPose.None || p.TravelBlend>.01f || !p.Grounded || p.Cover>.01f || p.StaggerT>=0 || p.ParryT>=0 || p.ThrowT>=0 || p.DrinkT>=0 || p.SwingT>=0 || p.Reload>=0 || p.ParcelCarry04G)return -1;
            if(p.Guard>.01f)
            {
                if(data==null || data.assetId!=MalikReviewH8R.AssetId || p.AttackT>=0 || p.DodgeT>=0 || p.Aiming || p.CrystalCast04G>.01f || p.RegionalSynergyKind04OF!=0)return -1;
                for(int i=5;i<data.clips.Length;i++)if(data.clips[i].name=="H8_Guard_Brace")return i;
                return -1;
            }
            if(data!=null && p.AttackT<0)
            {
                string special=p.DodgeT>=0?"H8_Dodge":p.RegionalSynergyKind04OF==1?"H8_AegisLink":p.RegionalSynergyKind04OF>0?null:p.Recoil>.15f && (p.Hold==HoldType.Pistol || p.Hold==HoldType.Rifle || p.Hold==HoldType.Heavy) && !p.Blade?"H8_FireRecoil":p.Aiming?"H8_FireAim":p.CrystalCast04G>.01f?"H8_CrystalCast":null;
                if(special!=null)for(int i=5;i<data.clips.Length;i++)if(data.clips[i].name==special)return i;
            }
            if(p.DodgeT>=0 || p.Aiming || p.CrystalCast04G>.01f || p.RegionalSynergyKind04OF!=0)return -1;
            if(p.AttackT>=0)return p.Blade && !p.PairedBlades && !p.AttackMirror && p.AttackAnim==MeleeAnim.SlashRight?4:-1;
            if(p.Hold!=HoldType.None && !p.Blade)return -1;
            if(p.Velocity.x*p.Velocity.x+p.Velocity.z*p.Velocity.z>.04f)return 1;
            return p.Blade && p.Ready>.1f?3:0;
        }
    }
}
