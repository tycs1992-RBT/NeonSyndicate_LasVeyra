#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, the two built-in owner-approved characters: Tyler's Blender Malik (H9 refinement, the player) and the
    /// H9 Dante Rook (bound to the exact live cast actor "dante"). Both are prepared by the code their Editor reviews use
    /// (ProductionMalikH6 and LivingCastH8S entry points), so the review and the live character share one prepared prefab,
    /// and their delivered receipts stay exactly as delivered (disabled and unapproved). The owner's decision of 2026-10-23
    /// is recorded on the generated live row only: enabled, owner approved, and the rights record names that decision.
    /// </summary>
    public static partial class LiveCastSetup
    {
        private const string MalikOrigin = "builtin:malik-h9", DanteOrigin = "builtin:dante-h9";
        private const string MalikLabel = "Malik Veyr H9", DanteLabel = "Dante Rook H9";

        [Serializable] private sealed class ReceiptList { public ReceiptFile[] files = new ReceiptFile[0]; }
        [Serializable] private sealed class ReceiptFile { public string path = ""; }

        private static LiveCastRow BuiltinMalik(LiveCastRegistry existing, bool urp, bool force, out bool built)
        {
            built = false;
            string receipt = MalikReviewH9.Root + "candidate.json";
            LiveCastRow old = FindOrigin(existing, MalikOrigin);
            string stamp = BuiltinStamp(receipt, new string[0], ProductionMalikH6.LiveImportersReadyH9(), urp);
            if (!force && old != null && !LiveCastFolderRules.NeedsRebuild(old.Stamp, stamp, old.Model.Prefab != null)) return old;
            Progress("Preparing " + MalikLabel + " (first time or changed art, about a minute)", 0.2f);
            DeleteFolder(ProductionMalikH6.LivePreparedFolderH9);
            ImportedModel04OD review;
            string report;
            GameObject prefab = ProductionMalikH6.PrepareH9ForLive(out review, out report);
            if (prefab == null || review == null) throw new InvalidDataException("the H9 preparation refused (" + report + ")");
            LiveCastRow row = Promote(review, MalikLabel, MalikOrigin, "", new string[0], BuiltinStamp(receipt, new string[0], ProductionMalikH6.LiveImportersReadyH9(), urp));
            string reason;
            if (!LiveCastRules.CanGoLive(row, out reason)) throw new InvalidDataException(reason);
            built = true;
            return row;
        }

        private static LiveCastRow BuiltinDante(LiveCastRegistry existing, bool urp, bool force, out bool built)
        {
            built = false;
            string receipt = DanteReviewH9.Root + "candidate.json";
            string[] shared = LivingCastH8S.LiveSharedFilesH9();
            LiveCastRow old = FindOrigin(existing, DanteOrigin);
            string stamp = BuiltinStamp(receipt, shared, LivingCastH8S.LiveImportersReadyH9(), urp);
            if (!force && old != null && !LiveCastFolderRules.NeedsRebuild(old.Stamp, stamp, old.Model.Prefab != null)) return old;
            Progress("Preparing " + DanteLabel + " (first time or changed art)", 0.6f);
            DeleteFolder(LivingCastH8S.LivePreparedFolderH9);
            ImportedModel04OD review;
            string summary;
            GameObject prefab = LivingCastH8S.PrepareH9ForLive(out review, out summary);
            if (prefab == null || review == null) throw new InvalidDataException("the H9 preparation returned no prefab.");
            string[] hidden = new string[LiveCastRules.DanteHiddenCostume.Count];
            LiveCastRules.DanteHiddenCostume.CopyTo(hidden, 0);
            LiveCastRow row = Promote(review, DanteLabel, DanteOrigin, CastIds04E.Dante, hidden, BuiltinStamp(receipt, shared, LivingCastH8S.LiveImportersReadyH9(), urp));
            string reason;
            if (!LiveCastRules.CanGoLive(row, out reason)) throw new InvalidDataException(reason);
            built = true;
            return row;
        }

        /// <summary>The fingerprint of a built-in: its receipt, every file the receipt lists, extra files and the import state.</summary>
        private static string BuiltinStamp(string receipt, string[] extra, bool importReady, bool urp)
        {
            List<string> paths = new List<string> { receipt };
            string full = Project(receipt);
            ReceiptList list = File.Exists(full) ? JsonUtility.FromJson<ReceiptList>(File.ReadAllText(full)) : null;
            if (list != null && list.files != null)
                foreach (ReceiptFile file in list.files)
                    if (file != null && !string.IsNullOrEmpty(file.path) && !paths.Contains(file.path)) paths.Add(file.path);
            foreach (string path in extra) if (!paths.Contains(path)) paths.Add(path);
            List<string> hashes = new List<string>();
            foreach (string path in paths) hashes.Add(ProjectFileHashH8S.Sha256(path));
            paths.Add("import-settings");
            hashes.Add(importReady ? "ready" : "pending");
            return LiveCastFolderRules.Stamp(Recipe, urp, paths, hashes);
        }

        /// <summary>
        /// Turns a verified review row into a live row with the owner's recorded decision: enabled, owner approved and the
        /// rights record naming the decision ahead of the delivered CC0 lineage. Nothing else of the row changes.
        /// </summary>
        private static LiveCastRow Promote(ImportedModel04OD review, string label, string origin, string castId, string[] hidden, string stamp)
        {
            review.Enabled = true;
            review.ArtProof.OwnerApproved = true;
            review.ArtProof.RightsRecordId = LiveCastRules.OwnerApprovalRecord + "; " + review.ArtProof.RightsRecordId;
            return new LiveCastRow { Model = review, CastId = castId, HiddenCostume = hidden, Label = label, Origin = origin, Stamp = stamp };
        }
    }
}
#endif
