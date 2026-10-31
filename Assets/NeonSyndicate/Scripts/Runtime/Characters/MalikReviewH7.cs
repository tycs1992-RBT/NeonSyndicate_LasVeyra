namespace NeonSyndicate
{
    /// <summary>Fixed H7 art identity. Review permission cannot grant production approval.</summary>
    public static class MalikReviewH7
    {
        public const string AssetId="MALIK-H7-REVIEW-01";
        public const string Root="Assets/NeonSyndicate/Art/ProductionH7/";
        public const string ExportPath=Root+"Models/MalikVeyrH7.fbx";
        public static void ResetStatics() { }
        public static bool CanReview(ImportedModel04OD model,bool editorSession,out string reason)
        {
            if(model==null || !model.AlignBindAxesH3 || !model.PreserveInfluencesH5){reason="H7 review requires its verified bind alignment and complete influence policy.";return false;}
            return MalikReviewH6.CanReviewVersion(model,editorSession,AssetId,ExportPath,out reason);
        }
    }
}
