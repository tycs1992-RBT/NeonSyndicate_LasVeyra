#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Extends the existing registry audit. Never copies a private receipt or enables a candidate.</summary>
    public sealed class ImportedArtProof04OH : IPreprocessBuildWithReport
    {
        public int callbackOrder {get{return 100;}}
        public void OnPreprocessBuild(BuildReport report)
        {
            List<string> errors=CheckEnabled();
            if(errors.Count>0)throw new BuildFailedException("Imported art proof refused:\n"+string.Join("\n",errors));
        }
        public static string FileHash(string path)
        {
            if(!CinematicAcceptance04OF.RelativeBonePath(path) || !path.StartsWith("Assets/",StringComparison.Ordinal) )return "";
            path=Path.Combine(Directory.GetParent(Application.dataPath).FullName,path);if(!File.Exists(path))return "";
            using(SHA256 hash=SHA256.Create())using(FileStream stream=File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        public static void Stamp(ImportedArtRecord04OH proof,GameObject source,GameObject visual)
        {
            proof.ExportAssetPath=AssetDatabase.GetAssetPath(source);proof.ExportSha256=FileHash(proof.ExportAssetPath);
            proof.VisualDependencyHash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(visual)).ToString();
        }
        public static List<string> CheckEnabled()
        {
            List<string> errors=new List<string>();
            foreach(string guid in AssetDatabase.FindAssets("t:ImportedModelRegistry04OD"))
            {
                ImportedModelRegistry04OD registry=AssetDatabase.LoadAssetAtPath<ImportedModelRegistry04OD>(AssetDatabase.GUIDToAssetPath(guid));
                if(registry==null || !registry.Enabled || registry.Models==null)continue;
                foreach(ImportedModel04OD model in registry.Models)
                {
                    if(model==null || !model.Enabled)continue;string reason;
                    if(!ImportedArt04OH.CanActivate(model.ArtProof,model.AnimationAuthority,out reason)){errors.Add(model.Identity+": "+reason);continue;}
                    if(model.ArtProof==null)continue; // Legacy rows retain their accepted contract.
                    string visual=model.Prefab!=null?AssetDatabase.GetAssetPath(model.Prefab):"";
                    string dependency=visual!=""?AssetDatabase.GetAssetDependencyHash(visual).ToString():"";
                    if(!ImportedArt04OH.FingerprintMatches(model.ArtProof,FileHash(model.ArtProof.ExportAssetPath),dependency))
                        errors.Add(model.Identity+": model or dependency changed since its import audit. Re-audit and review before enabling.");
                }
            }
            return errors;
        }
        [MenuItem("Neon Syndicate/04OH/Check enabled import proofs")]
        public static void Check()
        {
            List<string> errors=CheckEnabled();
            foreach(string error in errors)Log.Error(error);
            Log.Info("04OH enabled-import proof check: "+errors.Count+" errors. Zero enabled production assets is ASSET REQUIRED, not a completed hero import.");
        }
    }
}
#endif
