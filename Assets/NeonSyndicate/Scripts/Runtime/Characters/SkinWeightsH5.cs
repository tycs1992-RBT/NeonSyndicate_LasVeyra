using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Strict complete-influence inspection for the opt-in derived reference. No weight is silently discarded.</summary>
    public static class SkinWeightsH5
    {
        public static void ResetStatics() { }
        public static bool[] Referenced(byte[] counts,BoneWeight1[] weights,int vertices,int bones)
        {
            if(vertices<1 || bones<1 || counts==null || counts.Length!=vertices || weights==null)throw new ArgumentException("Complete native skin arrays are required.");
            bool[] result=new bool[bones];int cursor=0;
            foreach(byte count in counts)
            {
                if(count<1 || count>8 || cursor+count>weights.Length)throw new ArgumentException("Invalid complete-influence skin range.");
                float sum=0;
                for(int i=0;i<count;i++)
                {
                    BoneWeight1 weight=weights[cursor++];
                    if(weight.boneIndex<0 || weight.boneIndex>=bones || float.IsNaN(weight.weight) || float.IsInfinity(weight.weight) || weight.weight<=0)throw new ArgumentException("Invalid positive native influence.");
                    sum+=weight.weight;result[weight.boneIndex]=true;
                }
                if(Mathf.Abs(sum-1)>.001f)throw new ArgumentException("Native skin weights must be normalized before activation.");
            }
            if(cursor!=weights.Length)throw new ArgumentException("Skin data contains unowned trailing influences.");return result;
        }
        public static bool[] Referenced(Mesh mesh,int bones)
        {
            if(mesh==null || !mesh.isReadable)throw new ArgumentException("A readable native mesh is required.");
            // Unity owns these read-only views; do not Dispose them. Copy once at visual initialization, never every frame.
            return Referenced(mesh.GetBonesPerVertex().ToArray(),mesh.GetAllBoneWeights().ToArray(),mesh.vertexCount,bones);
        }
    }
}
