using System;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class FantasyFoundryProofWR02A
    {
        public string architectureStableId = "revision:10/venue:archive/seed:479130/crown";
        public string architectureVariant = "Original WR01 fallback", status = "Foundry catalog not prepared.";
        public bool architecturePrepared, architectureAdmitted, architectureActive, architectureCameraCandidate, creatureCandidatePrepared;
        public bool creatureActivationAllowed;
        public string creatureStatus = "CANDIDATE ONLY. Original Crystal Basilisk remains. Native motion, ground contact, hit and telegraph acceptance NOT RUN.";
        public string nativeAcceptance = "NOT RUN";
    }

    public sealed partial class HeartPresentationFR01
    {
        private readonly HeartCatalogFR01 _foundryCatalogWR02A = Resources.Load<HeartCatalogFR01>("HeartPreparedWR02A/Catalog");

        /// <summary>Use the same existing civic transaction and local frame. No parallel placement or saved setting.</summary>
        private HeartCatalogFR01 CivicCatalogFoundryWR02A(HeartRewritePlanWR01 plan, FantasyRewriteRecordWR01 record)
        {
            HeartModuleFR01 module = _foundryCatalogWR02A != null ? _foundryCatalogWR02A.Find("WRArchiveCrown") : null;
            if (HeartPlanFR01.FoundryArchiveWR02A(plan) && module != null && module.Lods != null && module.Lods.Length == 3)
            {
                string reason = "Foundry source fingerprint missing or incomplete.";
                bool valid = _foundryCatalogWR02A.Fingerprint != null && _foundryCatalogWR02A.Fingerprint.Length == 64;
                if (valid) valid = HeartPlanFR01.RewriteArtBoundsWR01(plan.Family, new Bounds(module.Center, module.Size), out reason);
                if (valid) foreach (GameObject prefab in module.Lods)
                {
                    if (!Passive(prefab)) { reason = "A foundry crown LOD is missing or not passive."; valid = false; break; }
                    if (!ValidPreparedModelV1(prefab, out reason)) { valid = false; break; }
                }
                if (valid) valid = ProbeFoundryFrameWR02H(plan, out reason);
                if (valid)
                {
                    record.assetVariant = "Foundry WR02A / SanctuaryArchive / " + _foundryCatalogWR02A.Fingerprint;
                    return _foundryCatalogWR02A;
                }
                record.assetVariant = "Original WR01 fallback. WR02A crown refused: " + FantasyProofRulesV1.SafeText(reason);
                return _rewriteCatalog;
            }
            record.assetVariant = "Original WR01 fallback. WR02A catalog absent or outside the exact Archive contract.";
            return _rewriteCatalog;
        }

        // The catalog bounds are only metadata. Test the actual assembled LOD geometry before selecting it,
        // while an invalid foundry candidate can still fall back to the original WR01 crown.
        private bool ProbeFoundryFrameWR02H(HeartRewritePlanWR01 plan, out string reason)
        {
            GameObject probe = null;
            try
            {
                probe = Model(plan.Family, plan.Origin, plan.Yaw, Vector3.one, _foundryCatalogWR02A);
                return CrownFramesWR01(probe, probe.transform, plan.Family, out reason);
            }
            catch (Exception error)
            {
                reason = "Foundry frame validation failed: " + FantasyProofRulesV1.SafeText(error.Message);
                return false;
            }
            finally
            {
                if (probe != null) UnityEngine.Object.Destroy(probe);
            }
        }

        private void FillFoundryProofWR02A(FantasyProofSnapshotV1 snapshot)
        {
            FantasyFoundryProofWR02A proof = new FantasyFoundryProofWR02A();
            if (_foundryCatalogWR02A != null)
            {
                HeartModuleFR01 archive = _foundryCatalogWR02A.Find("WRArchiveCrown");
                proof.architecturePrepared = archive != null && archive.Lods != null && archive.Lods.Length == 3;
                proof.creatureCandidatePrepared = _foundryCatalogWR02A.Find("CrystalBasiliskCandidate") != null;
                proof.status = _foundryCatalogWR02A.Reason;
            }
            foreach (FantasyRewriteRecordWR01 record in _rewriteRecordsWR01)
                if (record.stableId == proof.architectureStableId)
                {
                    proof.architectureVariant = record.assetVariant;
                    bool foundry = record.assetVariant.StartsWith("Foundry WR02A", StringComparison.Ordinal);
                    proof.architectureAdmitted = foundry && record.admitted;
                    proof.architectureActive = foundry && record.active;
                    proof.architectureCameraCandidate = foundry && record.importedCameraCandidate;
                }
            // There is no creature swap path or entitlement. A prepared skinned asset is not motion acceptance.
            proof.creatureActivationAllowed = false;
            snapshot.foundryWR02A = proof;
        }
    }

    public static partial class HeartPlanFR01
    {
        public static bool FoundryViewRequestedWR02A(string view)
        { return view == "ARCHIVE_CLOSEUP" || view == "ARCHIVE_PLAYER_SCALE"; }

        public static bool FoundryViewObservedWR02A(FantasyProofSnapshotV1 proof)
        {
            return proof != null && proof.foundryWR02A != null && proof.worldRewrite.selected &&
                proof.foundryWR02A.architecturePrepared && proof.foundryWR02A.architectureAdmitted &&
                proof.foundryWR02A.architectureActive && proof.foundryWR02A.architectureCameraCandidate;
        }

        public static bool FoundryArchiveWR02A(HeartRewritePlanWR01 plan)
        {
            return plan != null && plan.Seed == 479130 && plan.Service == "archive" && plan.Family == "WRArchiveCrown" &&
                plan.StableId == "revision:10/venue:archive/seed:479130/crown";
        }
    }
}
