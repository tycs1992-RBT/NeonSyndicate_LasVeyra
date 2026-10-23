namespace NeonSyndicate
{
    /// <summary>
    /// Exact inspection contract for the H8S derivative of the owner-selected human. It keeps the selected candidate id
    /// (the same person as H8R) under its own export, motion and source paths, and cannot approve or deploy the character.
    /// </summary>
    public static class MalikReviewH8S
    {
        public const string AssetId=MalikReviewH8R.AssetId;
        public const string Root="Assets/NeonSyndicate/Art/ProductionH8S/";
        public const string ExportPath=Root+"Models/MalikVeyrH8S.fbx";
        public const string MotionPath=Root+"Animations/MalikFullBodyH8S.json";
        public const string SourcePath="ArtSource/AscensionH8S/MalikVeyrH8S.blend";
        public static void ResetStatics() { }
        public static bool CanReview(ImportedModel04OD model,bool editorSession,out string reason)
        {
            if(model==null || !model.AlignBindAxesH3 || !model.PreserveInfluencesH5)
            {reason="Selected-human review requires validated bind alignment and complete skin weights.";return false;}
            return MalikReviewH6.CanReviewVersion(model,editorSession,AssetId,ExportPath,out reason);
        }
        /// <summary>The selected human ships as several derivative exports under one candidate id; the exact recorded export
        /// path chooses the contract (H8S here, otherwise the original H8R contract, which rejects any other path).</summary>
        public static bool CanReviewSelected(ImportedModel04OD model,bool editorSession,out string reason)
        {
            string path=model!=null && model.ArtProof!=null?model.ArtProof.ExportAssetPath:null;
            if(path==ExportPath)return CanReview(model,editorSession,out reason);
            return MalikReviewH8R.CanReview(model,editorSession,out reason);
        }
    }
}
