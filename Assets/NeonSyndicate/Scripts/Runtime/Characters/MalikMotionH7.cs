using System;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class MalikMotionSampleH7 {public float t;public Quaternion[] q;}
    [Serializable] public sealed class MalikMotionClipH7 {public string name;public float duration;public bool loop;public MalikMotionSampleH7[] samples;}
    [Serializable] public sealed class MalikMotionDataH7 {public int schema;public string assetId,space;public string[] bones;public bool rootMotion,events;public MalikMotionClipH7[] clips;}
    /// <summary>Samples actual authored torso accents. No transform, input, resource, root or contact authority.</summary>
    public static class MalikMotionH7
    {
        public const string RotationSpace="canonical actor-local world rotation delta; Blender axial (x,-z,y,w)";
        public static void ResetStatics() { }
        private static bool Finite(float f){return !float.IsNaN(f) && !float.IsInfinity(f);}
        public static bool Valid(MalikMotionDataH7 data,out string reason)
        {
            reason="Authored H7 rotation data is missing, malformed or outside its review contract.";
            if(data==null || data.schema!=1 || data.space!=RotationSpace || data.assetId!=MalikReviewH7.AssetId || data.rootMotion || data.events || data.bones==null || data.bones.Length!=3 || data.clips==null || data.clips.Length!=5)return false;
            if(data.bones[0]!="Chest" || data.bones[1]!="Neck" || data.bones[2]!="Head")return false;
            string[] names={"H7_IdleBreath","H7_Walk","H7_Run","H7_SwordReady","H7_OathCut"};
            for(int i=0;i<names.Length;i++)
            {
                MalikMotionClipH7 c=data.clips[i];
                if(c==null || c.name!=names[i] || !Finite(c.duration) || c.duration<.3f || c.duration>4 || c.loop!=(i<4) || c.samples==null || c.samples.Length<3 || c.samples.Length>181)return false;
                float previous=-1;
                foreach(MalikMotionSampleH7 s in c.samples)
                {
                    if(s==null || !Finite(s.t) || s.t<0 || s.t<=previous || s.t>c.duration+.0001f || s.q==null || s.q.Length!=3)return false;
                    foreach(Quaternion q in s.q)
                    {
                        float norm=QMath.Dot(q,q);
                        if(!Finite(norm) || Mathf.Abs(norm-1)>.002f || Mathf.Abs(q.w)<.84f)return false;
                    }
                    previous=s.t;
                }
                if(c.samples[0].t!=0 || Mathf.Abs(previous-c.duration)>.0001f)return false;
            }
            reason="";return true;
        }
        public static Quaternion Sample(MalikMotionClipH7 clip,int semantic,float phase)
        {
            int index=semantic==RigLayout.Chest?0:semantic==RigLayout.Neck?1:semantic==RigLayout.Head?2:-1;
            if(clip==null || index<0 || !Finite(phase) || clip.samples==null || clip.samples.Length<2)return QMath.Identity;
            float t=Mathf.Clamp01(phase)*clip.duration;
            for(int i=1;i<clip.samples.Length;i++)if(t<=clip.samples[i].t)
            {
                MalikMotionSampleH7 a=clip.samples[i-1],b=clip.samples[i];
                return QMath.Slerp(a.q[index],b.q[index],(t-a.t)/Mathf.Max(.000001f,b.t-a.t));
            }
            return clip.samples[clip.samples.Length-1].q[index];
        }
        /// <summary>Original AttackT, windup and active are normalized by the original move total.</summary>
        public static float CutPhase(float original,float windup,float active)
        {
            if(!Finite(original) || !Finite(windup) || !Finite(active) || original<0 || windup<=0 || active<=0 || windup+active>=1)return -1;
            original=Mathf.Clamp01(original);
            if(original<windup)return .36f*original/windup;
            if(original<windup+active)return .36f+.19f*(original-windup)/active;
            return .55f+.45f*(original-windup-active)/(1-windup-active);
        }
        public static Quaternion Accent(Quaternion authored,float weight)
        {return QMath.Slerp(QMath.Identity,authored,Finite(weight)?Mathf.Clamp(weight,0,.25f):0);}
    }
}
