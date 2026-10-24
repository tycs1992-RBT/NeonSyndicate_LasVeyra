namespace NeonSyndicate
{
    /// <summary>Exact selected-human inspection contract. This cannot approve or deploy the character.</summary>
    public static class MalikReviewH8R
    {
        public const string AssetId="MALIK-H8R-REVIEW-01";
        public const string Root="Assets/NeonSyndicate/Art/ProductionH8R/";
        public const string ExportPath=Root+"Models/MalikVeyrH8R.fbx";
        public static void ResetStatics() { }
        public static bool CanReview(ImportedModel04OD model,bool editorSession,out string reason)
        {
            if(model==null || !model.AlignBindAxesH3 || !model.PreserveInfluencesH5)
            {reason="Selected-human review requires validated bind alignment and complete skin weights.";return false;}
            return MalikReviewH6.CanReviewVersion(model,editorSession,AssetId,ExportPath,out reason);
        }
    }
}
