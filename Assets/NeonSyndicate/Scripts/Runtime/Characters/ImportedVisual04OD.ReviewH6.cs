#if UNITY_EDITOR
using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Transient inspection through the same adapter. Never enables a registry or writes player state.</summary>
    public sealed partial class ImportedVisual04OD
    {
        private ImportedModel04OD _reviewH6;
        private Action _reviewCleanupH6;
        public static bool BeginReviewH6(CharacterBase owner,ImportedModel04OD model,Action cleanup,out string reason)
        {
            reason="";
            if(!Application.isEditor || !Application.isPlaying || owner==null || !owner.IsPlayer || owner.IsDead || owner.Rig==null || owner.Rig.Bones.Length!=RigLayout.BoneCount)
            {reason="Start normal gameplay with the living, detailed Malik actor before inspection.";return false;}
            if(model!=null && model.ArtProof!=null && model.ArtProof.AssetId==MalikReviewH7.AssetId)
            {if(!MalikReviewH7.CanReview(model,true,out reason))return false;}
            else if(!MalikReviewH6.CanReview(model,true,out reason))return false;
            if(ImportedModelRegistry04OD.Find("malik")!=null){reason="An approved production visual already owns Malik. Review does not displace it.";return false;}
            ImportedVisual04OD adapter=owner.GetComponent<ImportedVisual04OD>();
            if(adapter==null)adapter=owner.gameObject.AddComponent<ImportedVisual04OD>();
            adapter.Restore();adapter._owner=owner;adapter._companion=null;adapter._reviewH6=model;adapter._reviewCleanupH6=cleanup;adapter.enabled=true;adapter.Initialize();
            if(!adapter._ready){reason="Native visual validation refused the candidate. Keep fallback and inspect the actual Console reason.";return false;}
            return true;
        }
        public static void EndReviewH6(CharacterBase owner)
        {
            ImportedVisual04OD adapter=owner!=null?owner.GetComponent<ImportedVisual04OD>():null;
            if(adapter!=null && adapter._reviewH6!=null){adapter.Restore();Destroy(adapter);}
        }
        public static bool IsReviewActiveH6(CharacterBase owner)
        {
            ImportedVisual04OD adapter=owner!=null?owner.GetComponent<ImportedVisual04OD>():null;
            return adapter!=null && adapter._ready && adapter._reviewH6!=null;
        }
        private void Update()
        {
            if(_reviewH6!=null && _ready && QualitySettings.skinWeights!=SkinWeights.Unlimited)
            {
                Log.Warn("H6 review reverted because live skin quality changed. Complete weights cannot silently degrade.");
                Restore();enabled=false;
            }
        }
        private void ReleaseReviewH6()
        {
            if(_owner!=null && _owner.Anim!=null)_owner.Anim.EndFullBodyReviewH8();
            ClearMotionH7();_reviewH6=null;Action cleanup=_reviewCleanupH6;_reviewCleanupH6=null;
            if(cleanup!=null)try{cleanup();}catch(Exception e){Log.Warn("H6 Editor inspection cleanup: "+e.Message);}
        }
    }
}
#endif
