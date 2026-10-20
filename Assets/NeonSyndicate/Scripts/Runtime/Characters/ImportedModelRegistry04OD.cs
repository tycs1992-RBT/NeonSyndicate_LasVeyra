using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class ImportedBone04OD { public int SourceBone; public string TargetPath; }
    [Serializable] public sealed class ImportedSocket04OD { public string Role,Path; }
    [Serializable] public sealed class ImportedModel04OD
    {
        public string Identity;
        public bool Enabled;
        public GameObject Prefab;
        public float Scale=1f;
        public ImportedBone04OD[] Bones=new ImportedBone04OD[0];
        // Explicit passive twist/accessory descendants retain bind-local offsets beneath a mapped bone.
        public string[] PassiveBonePaths=new string[0];
        public ImportedSocket04OD[] Sockets=new ImportedSocket04OD[0];
    }
    /// <summary>Optional visual adapters only. Save identity, weapons, collision, AI and procedural pose stay authoritative.</summary>
    [CreateAssetMenu(menuName="Neon Syndicate/Imported model registry 04OD")]
    public sealed class ImportedModelRegistry04OD : ScriptableObject
    {
        public bool Enabled;
        public ImportedModel04OD[] Models=new ImportedModel04OD[0];
        private static ImportedModelRegistry04OD _loaded;
        private static bool _attempted;
        public static void ResetStatics(){_loaded=null;_attempted=false;}
        public static Quaternion BindRotation04OF(Quaternion source,Quaternion rootBind,Quaternion targetBind)
        {return source*QMath.Inverse(rootBind)*targetBind;}
        public static int[] ParentFirst04OF(int[] depths)
        {
            if(depths==null)return new int[0];int[] order=new int[depths.Length];for(int i=0;i<order.Length;i++)order[i]=i;
            Array.Sort(order,(a,b)=>depths[a]==depths[b]?a.CompareTo(b):depths[a].CompareTo(depths[b]));return order;
        }
        public static ImportedModel04OD Find(string identity)
        {
            if(!_attempted){_attempted=true;_loaded=Resources.Load<ImportedModelRegistry04OD>("ImportedModels04OD");}
            if(_loaded==null || !_loaded.Enabled || _loaded.Models==null)return null;
            foreach(ImportedModel04OD model in _loaded.Models)if(model!=null && model.Enabled && model.Identity==identity)return model;
            return null;
        }
        public static bool ValidPassivePaths(IList<string> paths)
        {
            if(paths==null)return true;HashSet<string> seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string path in paths)if(string.IsNullOrWhiteSpace(path) || path.StartsWith("/") || path.Contains("..") || !seen.Add(path))return false;
            return true;
        }
        public static bool ValidLocalScale04OG(Vector3 scale)
        {
            if(!MathUtil.IsFinite(scale) || scale.x<=.0001f || scale.y<=.0001f || scale.z<=.0001f)return false;
            float largest=Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z)),smallest=Mathf.Min(scale.x,Mathf.Min(scale.y,scale.z));
            return largest<=100f && largest-smallest<=largest*.01f;
        }
        public static bool ValidMapping(int sourceCount,IList<ImportedBone04OD> bones,float scale,out string reason)
        {
            reason="";
            if(float.IsNaN(scale) || float.IsInfinity(scale) || scale<.01f || scale>10f){reason="Scale is outside .01 to 10.";return false;}
            if(bones==null || bones.Count!=sourceCount || sourceCount<1){reason="Every original pose bone must be mapped exactly once.";return false;}
            HashSet<int> sources=new HashSet<int>();HashSet<string> paths=new HashSet<string>(StringComparer.Ordinal);
            foreach(ImportedBone04OD bone in bones)
                if(bone==null || bone.SourceBone<0 || bone.SourceBone>=sourceCount || !CinematicAcceptance04OF.RelativeBonePath(bone.TargetPath) || !sources.Add(bone.SourceBone) || !paths.Add(bone.TargetPath))
                {reason="A source bone or target path is missing, duplicated or out of range.";return false;}
            return true;
        }
    }
}
