using System;
using UnityEngine;
namespace NeonSyndicate
{
    public enum ImportedAnimationAuthority04OH { ExistingPose = 0, AuthoredClips = 1 }
    /// <summary>Presentation provenance, not saved progression. Unknown rights never imply owner approval.</summary>
    [Serializable] public sealed class ImportedArtRecord04OH
    {
        public string AssetId="",Creator="",SourceUrl="",LicenseId="",RightsRecordId="";
        public string ExportAssetPath="",ExportSha256="",VisualDependencyHash="";
        public bool CommercialUse,PublicSourceApproved,OwnerApproved,Diagnostic;
    }
    /// <summary>Shared import boundaries. Old registry rows keep their original procedural-pose contract.</summary>
    public static class ImportedArt04OH
    {
        public static void ResetStatics() { }
        public static bool Hex(string value,int length)
        {
            if(value==null || value.Length!=length)return false;
            foreach(char c in value)if(!((c>='0' && c<='9') || (c>='a' && c<='f') || (c>='A' && c<='F')))return false;
            return true;
        }
        public static bool CanActivate(ImportedArtRecord04OH proof,ImportedAnimationAuthority04OH authority,out string reason)
        {
            reason="";
            if(authority!=ImportedAnimationAuthority04OH.ExistingPose){reason="Authored clip authority is SOURCE INCOMPLETE. Keep the original pose fallback.";return false;}
            if(proof==null)return true; // Preserve pre-04OH visual rows without changing old gameplay or saves.
            if(proof.Diagnostic){reason="Diagnostic fixture only. It cannot replace a canonical actor.";return false;}
            if(string.IsNullOrWhiteSpace(proof.AssetId) || string.IsNullOrWhiteSpace(proof.Creator) || string.IsNullOrWhiteSpace(proof.LicenseId) || string.IsNullOrWhiteSpace(proof.RightsRecordId))
            {reason="ASSET REQUIRED: record the actual creator, asset ID, license and private rights-record identifier.";return false;}
            if(!proof.CommercialUse || !proof.OwnerApproved){reason="ASSET REQUIRED: commercial-use rights and owner appearance approval remain pending.";return false;}
            if(!CinematicAcceptance04OF.RelativeBonePath(proof.ExportAssetPath) || !proof.ExportAssetPath.StartsWith("Assets/",StringComparison.Ordinal)
                || !Hex(proof.ExportSha256,64) || !Hex(proof.VisualDependencyHash,32))
            {reason="Import proof is missing a project-relative export path or actual source/dependency hashes.";return false;}
            return true;
        }
        public static bool FingerprintMatches(ImportedArtRecord04OH proof,string exportHash,string visualHash)
        {
            return proof!=null && Hex(exportHash,64) && Hex(visualHash,32)
                && string.Equals(proof.ExportSha256,exportHash,StringComparison.OrdinalIgnoreCase)
                && string.Equals(proof.VisualDependencyHash,visualHash,StringComparison.OrdinalIgnoreCase);
        }
        public static bool VisualComponent(Type type)
        {
            return type==typeof(Transform) || type==typeof(MeshFilter) || type==typeof(MeshRenderer)
                || type==typeof(SkinnedMeshRenderer) || type==typeof(Animator) || type==typeof(LODGroup);
        }
    }
}
