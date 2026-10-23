namespace NeonSyndicate
{
    /// <summary>
    /// Exact inspection contract for the H8S derivative of the owner-selected human. It keeps the selected candidate id
    /// (the same person as H8R) under its own export, motion and source paths, and cannot approve or deploy the character.
    /// </summary>
    public static class MalikReviewH8S
    {
        /// <summary>The selected human keeps one candidate id across its derivative exports.</summary>
        public const string AssetId=MalikReviewH8R.AssetId;
        /// <summary>Folder of the H8S exports and receipt.</summary>
        public const string Root="Assets/NeonSyndicate/Art/ProductionH8S/";
        /// <summary>The exact recorded export that this contract accepts.</summary>
        public const string ExportPath=Root+"Models/MalikVeyrH8S.fbx";
        /// <summary>The curated schema 2 motion set.</summary>
        public const string MotionPath=Root+"Animations/MalikFullBodyH8S.json";
        /// <summary>The Blender source whose hash the motion set records.</summary>
        public const string SourcePath="ArtSource/AscensionH8S/MalikVeyrH8S.blend";
        /// <summary>No mutable statics; kept for the original reset hub.</summary>
        public static void ResetStatics() { }
        /// <summary>Editor inspection permission for the exact H8S export. Never an approval or a player-build override.</summary>
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
