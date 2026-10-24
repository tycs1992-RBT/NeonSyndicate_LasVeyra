#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// ProductionMalikH6, the live entry point for the live character generator (LiveCastSetup). It runs this window's own
    /// H9 preparation (complete-weight import settings, three LODs on one skeleton, the recorded materials) and its review
    /// row builder on a hidden instance, so the live Malik and the H9 review share exactly one prepared prefab. The window
    /// is never shown, no review starts and the selection is restored.
    /// </summary>
    public sealed partial class ProductionMalikH6
    {
        /// <summary>The generated H9 folder (prefab and materials) that the generator clears before preparing again.</summary>
        internal const string LivePreparedFolderH9 = MalikReviewH9.Root + "Prepared";

        /// <summary>
        /// Prepares the H9 visual and returns the prefab with its review row (still disabled and unapproved: the generator
        /// promotes it with the owner's recorded decision), or null with report explaining the refusal.
        /// </summary>
        internal static GameObject PrepareH9ForLive(out ImportedModel04OD row, out string report)
        {
            row = null;
            ProductionMalikH6 window = CreateInstance<ProductionMalikH6>();
            Object[] selection = Selection.objects;
            try
            {
                window.hideFlags = HideFlags.HideAndDontSave;
                window._h7 = true;
                window._h8r = true;
                window._h8s = true;
                window._h9 = true;
                window._fullBodyH8 = true;
                window._prepared = null;
                window.Prepare();
                report = window._report;
                if (window._prepared == null) return null;
                Receipt receipt = window.ReadReceipt();
                ImportedModel04OD candidate = window.ReviewRow(receipt);
                string reason;
                if (!MalikReviewH9.CanReview(candidate, true, out reason))
                {
                    report = reason;
                    return null;
                }
                row = candidate;
                return window._prepared;
            }
            finally
            {
                Selection.objects = selection;
                DestroyImmediate(window);
            }
        }

        /// <summary>True when the three H9 FBX files carry this window's complete-weight import settings.</summary>
        internal static bool LiveImportersReadyH9()
        {
            for (int level = 0; level < 3; level++)
            {
                string path = MalikReviewH9.Root + "Models/MalikVeyrH9" + (level == 0 ? "" : "_LOD" + level) + ".fbx";
                ModelImporter m = AssetImporter.GetAtPath(path) as ModelImporter;
                if (m == null || !m.isReadable || m.importAnimation || m.optimizeGameObjects || !m.preserveHierarchy || m.skinWeights != ModelImporterSkinWeights.Custom
                    || m.maxBonesPerVertex != 8 || m.minBoneWeight != 0f || m.importTangents != ModelImporterTangents.CalculateMikk
                    || m.animationType != ModelImporterAnimationType.Generic || m.importCameras || m.importLights) return false;
            }
            return true;
        }
    }
}
#endif
