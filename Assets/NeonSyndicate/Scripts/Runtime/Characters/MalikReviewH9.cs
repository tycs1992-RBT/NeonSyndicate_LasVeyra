namespace NeonSyndicate
{
    /// <summary>
    /// Exact inspection contract for the H9 refinement of the owner-selected human (rebuilt groom, face, collar and garment
    /// fit, much cheaper LODs). Same person and candidate id as H8R and H8S, under its own export, motion and source paths.
    /// It cannot approve or deploy the character.
    /// </summary>
    public static class MalikReviewH9
    {
        /// <summary>The selected human keeps one candidate id across its derivative exports.</summary>
        public const string AssetId=MalikReviewH8R.AssetId;
        /// <summary>Folder of the H9 exports and receipt.</summary>
        public const string Root="Assets/NeonSyndicate/Art/ProductionH9/";
        /// <summary>The exact recorded export that this contract accepts.</summary>
        public const string ExportPath=Root+"Models/MalikVeyrH9.fbx";
        /// <summary>The curated schema 2 motion set exported from the H9 source.</summary>
        public const string MotionPath=Root+"Animations/MalikFullBodyH9.json";
        /// <summary>The Blender source whose hash the motion set records.</summary>
        public const string SourcePath="ArtSource/AscensionH9/MalikVeyrH9.blend";
        /// <summary>No mutable statics; kept for the original reset hub.</summary>
        public static void ResetStatics() { }
        /// <summary>Editor inspection permission for the exact H9 export. Never an approval or a player-build override.</summary>
        public static bool CanReview(ImportedModel04OD model,bool editorSession,out string reason)
        {
            if(model==null || !model.AlignBindAxesH3 || !model.PreserveInfluencesH5)
            {reason="Selected-human review requires validated bind alignment and complete skin weights.";return false;}
            return MalikReviewH6.CanReviewVersion(model,editorSession,AssetId,ExportPath,out reason);
        }
    }
}
