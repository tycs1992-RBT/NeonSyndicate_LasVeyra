using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Opt-in bind-axis alignment for a received candidate, never a competing animation authority.</summary>
    public static class ImportedBindH3
    {
        public static void ResetStatics() { }
        public static string CandidateBone(string original)
        {
            switch(original)
            {
                case "Hips":return "pelvis";case "Spine":return "spine_01";case "Chest":return "spine_03";case "Neck":return "neck_01";case "Head":return "Head";
            }
            string[] parts=original.Split('.');if(parts.Length<2)return "";
            string side=parts[parts.Length-1].ToLowerInvariant();
            if(parts.Length==3)return (parts[0]=="Little"?"pinky":parts[0].ToLowerInvariant())+"_0"+parts[1]+"_"+side;
            string name=parts[0]=="UpperArm"?"upperarm":parts[0]=="Forearm"?"lowerarm":parts[0]=="Thigh"?"thigh":parts[0]=="Shin"?"calf":parts[0].ToLowerInvariant();
            return name+"_"+side;
        }
        public static bool[] ReferencedBones(BoneWeight[] weights,int count)
        {
            if(weights==null || weights.Length==0 || count<1)throw new ArgumentException("Skin weights and a nonempty bone table are required.");
            bool[] result=new bool[count];
            foreach(BoneWeight weight in weights)
            {
                float sum=weight.weight0+weight.weight1+weight.weight2+weight.weight3;
                if(float.IsNaN(sum) || float.IsInfinity(sum) || Mathf.Abs(sum-1)>.001f)throw new ArgumentException("Candidate weights must be normalized before activation.");
                Mark(result,weight.boneIndex0,weight.weight0);Mark(result,weight.boneIndex1,weight.weight1);
                Mark(result,weight.boneIndex2,weight.weight2);Mark(result,weight.boneIndex3,weight.weight3);
            }
            return result;
        }
        private static void Mark(bool[] result,int index,float value)
        {
            if(float.IsNaN(value) || float.IsInfinity(value) || value<0)throw new ArgumentException("Invalid skin weight.");
            if(value==0)return;if(index<0 || index>=result.Length)throw new ArgumentException("Weighted bone index is outside the bone table.");result[index]=true;
        }
        public static Quaternion[] Align(Vector3[] original,int[] parents,Vector3[] target,Quaternion[] rotations,Quaternion root)
        {
            if(original==null || parents==null || target==null || rotations==null || parents.Length!=original.Length || target.Length!=original.Length || rotations.Length!=original.Length)throw new ArgumentException("Bind arrays must match the original bone order.");
            Quaternion[] result=new Quaternion[original.Length];
            for(int i=0;i<result.Length;i++)
            {
                if(!MathUtil.IsFinite(original[i]) || !MathUtil.IsFinite(target[i]))throw new ArgumentException("Nonfinite bind position.");
                int child=-1;for(int j=0;j<parents.Length;j++)if(parents[j]==i){child=j;break;}
                // The middle finger provides the palm axis rather than a thumb or lateral index chain.
                if(original.Length==RigLayout.BoneCount && (i==RigLayout.HandL || i==RigLayout.HandR))child=i==RigLayout.HandL?20:35;
                Vector3 a,b;
                if(child>=0){a=target[child]-target[i];b=root*(original[child]-original[i]);}
                else if(parents[i]>=0){a=target[i]-target[parents[i]];b=root*(original[i]-original[parents[i]]);}
                else {result[i]=rotations[i];continue;}
                if(a.sqrMagnitude<1e-8f || b.sqrMagnitude<1e-8f)throw new ArgumentException("Collapsed mapped bind segment.");
                result[i]=QMath.FromTo(a,b)*rotations[i];
            }
            return result;
        }
    }
}
