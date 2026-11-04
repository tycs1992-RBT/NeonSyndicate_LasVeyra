#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LivingCastH8S, the live entry point for the live character generator (LiveCastSetup): the verified H9 Dante flow of
    /// this review (receipt, shared textures, source hash, complete-weight import settings, the stale-copy check and the
    /// prepared prefab) without starting a review, so the live Dante and the H9 review share one prepared prefab.
    /// </summary>
    public static partial class LivingCastH8S
    {
        /// <summary>The generated H9 Dante folder (prefab, materials and stamp) that the generator clears before preparing again.</summary>
        internal const string LivePreparedFolderH9 = DanteReviewH9.PreparedFolder;

        /// <summary>
        /// Prepares (or verifies) the H9 Dante copy and returns the prefab with its review row (still disabled and unapproved:
        /// the generator promotes it with the owner's recorded decision). Throws with the reason on any refusal.
        /// </summary>
        internal static GameObject PrepareH9ForLive(out ImportedModel04OD row, out string summary)
        {
            DanteCut previous = _cut;
            _cut = H9Cut;
            try
            {
                Receipt receipt = ReadReceipt();
                VerifySharedTextures();
                VerifySource();
                ConfigureImporters();
                GameObject prepared = AssetDatabase.LoadAssetAtPath<GameObject>(_cut.PreparedPrefab);
                // A copy prepared from earlier files (a re-export at the same path) would bind the old skeleton and sockets.
                bool stale = prepared != null && !StampMatches(receipt);
                if (stale && !AssetDatabase.DeleteAsset(_cut.PreparedPrefab)) throw new IOException("The stale review copy could not be removed: " + _cut.PreparedPrefab);
                if (stale) prepared = null;
                if (prepared == null) prepared = Prepare(receipt);
                row = ReviewRow(prepared, receipt, out summary);
                string reason;
                if (!_cut.CanReview(row, out reason)) throw new InvalidDataException(reason);
                return prepared;
            }
            finally
            {
                _cut = previous;
            }
        }

        /// <summary>True when the H9 FBX files and textures carry this review's import settings.</summary>
        internal static bool LiveImportersReadyH9()
        {
            DanteCut previous = _cut;
            _cut = H9Cut;
            try
            {
                for (int level = 0; level < 3; level++)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(ModelPath(level)) as ModelImporter;
                    if (importer == null || !ModelReady(importer)) return false;
                }
                for (int i = 0; i < _cut.AlbedoMaps.Length + _cut.NormalMaps.Length; i++)
                {
                    bool normal = i >= _cut.AlbedoMaps.Length;
                    TextureImporter importer = AssetImporter.GetAtPath(normal ? _cut.NormalMaps[i - _cut.AlbedoMaps.Length] : _cut.AlbedoMaps[i]) as TextureImporter;
                    if (importer == null || !TextureReady(importer, normal)) return false;
                }
                return true;
            }
            finally
            {
                _cut = previous;
            }
        }

        /// <summary>The H9 Dante files outside the receipt that the preparation reads: the shared wool and leather maps.</summary>
        internal static string[] LiveSharedFilesH9()
        {
            return new[] { SharedReceipt, SharedTextures + "Cloth_Albedo.png", SharedTextures + "Cloth_Normal.png", SharedTextures + "Leather_Albedo.png", SharedTextures + "Leather_Normal.png" };
        }
    }
}
#endif
