#if UNITY_EDITOR
using UnityEditor;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LivingCastH8S, the delivered cuts of the same Dante candidate: H8S (first fit, nine skins per LOD) and H9 (rest
    /// skeleton refitted to the procedural gameplay rig, seven skins per LOD, strand-textured hair). One verified flow,
    /// adapter path and presence policy serve both; each cut has its own receipt, source hash and Prepared folder.
    /// </summary>
    public static partial class LivingCastH8S
    {
        private const string ReviewMenuH9 = "Neon Syndicate/04OH/H9 Dante living cast review";
        private const string EndMenuH9 = "Neon Syndicate/04OH/End H9 Dante living cast review";
        private const string SharedCloth = "Assets/NeonSyndicate/Art/ProductionH8S/Textures/";

        /// <summary>The files and limits of one delivered cut.</summary>
        private sealed class DanteCut
        {
            public string Label, Root, ModelName, PreparedFolder, PreparedPrefab, SourceBlend, SourceSha256, ReviewMenu, EndMenu, RightsRecord;
            public string HairAlbedo = "", HairNormal = "";
            public int SkinsPerLod, ReceiptFiles;
            public bool H9;
            public string[] AlbedoMaps, NormalMaps;
            public string ExportPath { get { return Root + ModelName + ".fbx"; } }
            public bool CanReview(ImportedModel04OD row, out string reason)
            {
                return H9 ? DanteReviewH9.CanReview(row, true, out reason) : DanteReviewH8S.CanReview(row, true, out reason);
            }
        }

        private static readonly DanteCut H8SCut = new DanteCut
        {
            Label = "H8S", Root = DanteReviewH8S.Root, ModelName = "DanteRookH8S", PreparedFolder = DanteReviewH8S.PreparedFolder,
            PreparedPrefab = DanteReviewH8S.PreparedPrefab, SourceBlend = DanteReviewH8S.SourceBlend, SourceSha256 = DanteReviewH8S.SourceSha256,
            ReviewMenu = ReviewMenu, EndMenu = EndMenu, RightsRecord = "h8s-dante-cc0-quaternius-lineage", SkinsPerLod = DanteReviewH8S.SkinsPerLod, ReceiptFiles = 7,
            AlbedoMaps = new[] { DanteReviewH8S.Root + "DanteAlbedo.png", DanteReviewH8S.Root + "T_Eye_Brown.png", SharedCloth + "Cloth_Albedo.png", SharedCloth + "Leather_Albedo.png" },
            NormalMaps = new[] { DanteReviewH8S.Root + "T_Superhero_Male_Normal.png", DanteReviewH8S.Root + "T_Eye_Normal.png", SharedCloth + "Cloth_Normal.png", SharedCloth + "Leather_Normal.png" }
        };

        private static readonly DanteCut H9Cut = new DanteCut
        {
            Label = "H9", Root = DanteReviewH9.Root, ModelName = DanteReviewH9.ModelName, PreparedFolder = DanteReviewH9.PreparedFolder,
            PreparedPrefab = DanteReviewH9.PreparedPrefab, SourceBlend = DanteReviewH9.SourceBlend, SourceSha256 = DanteReviewH9.SourceSha256,
            ReviewMenu = ReviewMenuH9, EndMenu = EndMenuH9, RightsRecord = "h9-dante-cc0-quaternius-lineage", SkinsPerLod = DanteReviewH9.SkinsPerLod,
            ReceiptFiles = DanteReviewH9.ReceiptFiles, H9 = true, HairAlbedo = DanteReviewH9.HairAlbedo, HairNormal = DanteReviewH9.HairNormal,
            AlbedoMaps = new[] { DanteReviewH9.Root + "DanteAlbedo.png", DanteReviewH9.Root + "T_Eye_Brown.png", SharedCloth + "Cloth_Albedo.png", SharedCloth + "Leather_Albedo.png", DanteReviewH9.HairAlbedo },
            NormalMaps = new[] { DanteReviewH9.Root + "T_Superhero_Male_Normal.png", DanteReviewH9.Root + "T_Eye_Normal.png", SharedCloth + "Cloth_Normal.png", SharedCloth + "Leather_Normal.png", DanteReviewH9.HairNormal }
        };

        /// <summary>The cut the last review menu selected (Editor session state, set before every run).</summary>
        private static DanteCut _cut = H8SCut;

        /// <summary>Outside Play mode: verify and prepare the disabled H9 review copy. In Play mode: bind it to the live Dante.</summary>
        [MenuItem(ReviewMenuH9)]
        public static void ReviewH9()
        {
            _cut = H9Cut;
            Run();
        }

        /// <summary>Ends a running review (either cut) and restores the procedural Dante.</summary>
        [MenuItem(EndMenuH9)]
        public static void EndH9()
        {
            End();
        }
    }
}
#endif
