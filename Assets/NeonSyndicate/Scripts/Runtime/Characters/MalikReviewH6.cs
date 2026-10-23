using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Permission for explicit Editor inspection only. Production approval remains in the original proof owner.</summary>
    public static class MalikReviewH6
    {
        public const string AssetId="MALIK-H6-REVIEW-01";
        public const string ExportPath="Assets/NeonSyndicate/Art/ProductionH6/Models/MalikVeyrH6.fbx";
        public static void ResetStatics() { MalikReviewH8S.ResetStatics();MalikReviewH8R.ResetStatics();MalikReviewH7.ResetStatics();MalikMotionH7.ResetStatics(); }
        public static bool CanReview(ImportedModel04OD model,bool editorSession,out string reason)
        {return CanReviewVersion(model,editorSession,AssetId,ExportPath,out reason);}
        internal static bool CanReviewVersion(ImportedModel04OD model,bool editorSession,string assetId,string exportPath,out string reason)
        {
            reason="";
            if(!editorSession){reason="H6 inspection is an explicit Editor session, never a player-build override.";return false;}
            if(model==null || model.Identity!="malik" || model.Enabled){reason="Use the disabled Malik candidate only.";return false;}
            ImportedArtRecord04OH p=model.ArtProof;
            if(p==null || p.AssetId!=assetId || p.Diagnostic || p.ExportAssetPath!=exportPath || !ImportedArt04OH.Hex(p.ExportSha256,64) || !ImportedArt04OH.Hex(p.VisualDependencyHash,32))
            {reason="A current H6 export and visual dependency proof is required.";return false;}
            if(string.IsNullOrWhiteSpace(p.Creator) || p.LicenseId!="CC0-1.0" || string.IsNullOrWhiteSpace(p.RightsRecordId) || !p.CommercialUse || !p.PublicSourceApproved)
            {reason="Recorded commercial/public-source rights are required even for review.";return false;}
            if(model.AnimationAuthority!=ImportedAnimationAuthority04OH.ExistingPose){reason="Full-body authored playback remains gated. Keep the original bone-pose authority.";return false;}
            if(!ImportedModelRegistry04OD.ValidMapping(RigLayout.BoneCount,model.Bones,model.Scale,out reason) || !ImportedModelRegistry04OD.ValidPassivePaths(model.PassiveBonePaths))return false;
            HashSet<string> roles=new HashSet<string>(StringComparer.Ordinal);
            if(model.Sockets!=null)foreach(ImportedSocket04OD socket in model.Sockets)
                if(socket==null || !CinematicAcceptance04OF.RelativeBonePath(socket.Path) || !roles.Add(socket.Role)){reason="Review locators are missing or duplicated.";return false;}
            foreach(string role in new[]{"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"})
                if(!roles.Contains(role)){reason="Review lacks a visual locator: "+role;return false;}
            return true;
        }
        public static Color SurfaceEmission(Color authored,float glow,float flash)
        {
            glow=float.IsNaN(glow) || float.IsInfinity(glow)?1:Mathf.Clamp(glow,0,4);
            flash=float.IsNaN(flash) || float.IsInfinity(flash)?0:Mathf.Clamp01(flash);
            return authored*glow+new Color(1,.85f,.7f,0)*flash;
        }
    }
}
