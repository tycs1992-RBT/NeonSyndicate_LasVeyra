namespace NeonSyndicate
{
    /// <summary>
    /// The exact Editor review contract for the H9 refit of Dante Rook's visual (same candidate DANTE-H8S-REVIEW-01, same
    /// canonical identity and cast id, rest skeleton refitted to the procedural gameplay rig, seven skins per LOD). It reuses
    /// every DanteReviewH8S rule and table and differs only in the delivered files. It can never approve or enable anything.
    /// </summary>
    public static class DanteReviewH9
    {
        /// <summary>The recorded candidate id, unchanged from H8S.</summary>
        public const string AssetId = DanteReviewH8S.AssetId;
        /// <summary>The canonical adapter identity, unchanged from H8S.</summary>
        public const string Identity = DanteReviewH8S.Identity;
        /// <summary>The canonical cast id, unchanged from H8S.</summary>
        public const string CastId = DanteReviewH8S.CastId;
        /// <summary>The delivered H9 candidate folder.</summary>
        public const string Root = "Assets/NeonSyndicate/Art/ProductionH9/NPC/";
        /// <summary>The model file name without extension; the LODs add _LOD1 and _LOD2.</summary>
        public const string ModelName = "DanteRookH9";
        /// <summary>The LOD0 export the proof must name.</summary>
        public const string ExportPath = Root + ModelName + ".fbx";
        /// <summary>The folder for generated H9 review copies (prefab and materials). It is never committed.</summary>
        public const string PreparedFolder = Root + "Prepared";
        /// <summary>The generated disabled H9 review prefab.</summary>
        public const string PreparedPrefab = PreparedFolder + "/DanteH9Review.prefab";
        /// <summary>The editable H9 source and its delivered SHA-256.</summary>
        public const string SourceBlend = "ArtSource/AscensionH9/DanteRookH9.blend";
        /// <summary>The delivered SHA-256 of SourceBlend.</summary>
        public const string SourceSha256 = "90deb94260fafe1cbdf7bc061aa424f5215965cbdf75d850bbc34f216b4b2ea2";
        /// <summary>Skinned meshes in each of the three H9 LODs (one per material).</summary>
        public const int SkinsPerLod = 7;
        /// <summary>Files fingerprinted by the H9 receipt (three FBX, six textures and export-stats.json).</summary>
        public const int ReceiptFiles = 10;
        /// <summary>The strand-textured hair maps the H9 hair material uses.</summary>
        public const string HairAlbedo = Root + "DanteHair_Albedo.png";
        /// <summary>The strand-textured hair normal map.</summary>
        public const string HairNormal = Root + "DanteHair_Normal.png";

        /// <summary>True only for the exact H9 review row: every DanteReviewH8S rule, with the H9 export path.</summary>
        public static bool CanReview(ImportedModel04OD model, bool editorSession, out string reason)
        {
            return DanteReviewH8S.CanReviewExport(model, editorSession, ExportPath, out reason);
        }
    }
}
