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
        private bool Finite(float v){return !float.IsNaN(v) && !float.IsInfinity(v);}
        private bool Finite(Vector3 v){return Finite(v.x)&&Finite(v.y)&&Finite(v.z);}
        public bool Valid(MalikDataH8 data,out string reason)
        {
            reason="Full-body H8 data failed its fixed skeleton, bounded-motion or no-event contract.";
            if(data==null || data.schema!=1 || data.assetId!=MalikReviewH7.AssetId || data.space!=Space || data.rootMotion || data.events || !ImportedArt04OH.Hex(data.sourceSha256,64) || data.bind==null || data.bind.Length!=17 || data.clips==null || data.clips.Length<5 || data.clips.Length>20)return false;
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
        public int Select(ref PoseInput p,bool blocked)
        {
            if(blocked || p.Sit>.01f || p.Crouch>.01f || p.Ride!=RidePose.None || p.Travel!=TravelPose.None || p.TravelBlend>.01f || !p.Grounded || p.Cover>.01f || p.Aiming || p.Guard>.01f || p.StaggerT>=0 || p.DodgeT>=0 || p.ParryT>=0 || p.ThrowT>=0 || p.DrinkT>=0 || p.SwingT>=0 || p.CrystalCast04G>.01f || p.Reload>=0 || p.ParcelCarry04G || p.RegionalSynergyKind04OF!=0)return -1;
            if(p.AttackT>=0)return p.Blade && !p.PairedBlades && !p.AttackMirror && p.AttackAnim==MeleeAnim.SlashRight?4:-1;
            if(p.Hold!=HoldType.None && !p.Blade)return -1;
            if(p.Velocity.x*p.Velocity.x+p.Velocity.z*p.Velocity.z>.04f)return 1;
            return p.Blade && p.Ready>.1f?3:0;
        }
    }
}
