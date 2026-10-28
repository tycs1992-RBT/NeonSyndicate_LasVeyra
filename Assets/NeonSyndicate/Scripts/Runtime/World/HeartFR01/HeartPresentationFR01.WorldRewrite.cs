using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private sealed class RewritePairWR01
        {
            public GameObject Legacy, Candidate, Imported;
            public bool Civic, ImportedRequired;
            public FantasyRewriteRecordWR01 Record;
        }

        private readonly HeartCatalogFR01 _rewriteCatalog = Resources.Load<HeartCatalogFR01>("HeartPreparedWR01/Catalog");
        private readonly List<RewritePairWR01> _rewritePairsWR01 = new List<RewritePairWR01>();
        private readonly List<FantasyRewriteRecordWR01> _rewriteRecordsWR01 = new List<FantasyRewriteRecordWR01>();
        private bool _rewriteBuiltWR01;

        public bool WorldRewriteSelectedWR01 { get { return HeartPlanFR01.RewriteSelectedWR01(_fantasyPreviewV1, _proofComparisonV1, _proofLegacyV1); } }
        public string WorldRewriteStatusWR01
        {
            get
            {
                RefreshRewriteObservationsWR01();
                int active = 0, admitted = 0, refused = 0;
                foreach (FantasyRewriteRecordWR01 record in _rewriteRecordsWR01) { if (record.active) active++; if (record.admitted) admitted++; else refused++; }
                return "WR01 " + (WorldRewriteSelectedWR01 ? "selected" : "OFF") + "; planned " + _rewriteRecordsWR01.Count +
                    ", admitted " + admitted + ", active " + active + ", refused " + refused +
                    ". Original physical, service and saved identities retained. Native acceptance requires separate review.";
            }
        }

        private void BuildWorldRewriteWR01()
        {
            if (_rewriteBuiltWR01) return;
            _rewriteBuiltWR01 = true;
            foreach (SkylineCaptureWR01 capture in _skylineCapturesWR01.Values)
                if (!capture.Failed && capture.Appends != 3)
                {
                    capture.Failed = true; if (capture.Candidate != null) capture.Candidate.SetActive(false);
                    if (capture.Legacy != null) capture.Legacy.SetActive(true);
                    RefuseRewriteWR01(capture.Record, "Incomplete original skyline material capture. Original partial groups remain, no replacement activates.");
                }
            VenueView04C.StageWorldRewriteWR01(this);
        }

        private void RefuseRewriteWR01(FantasyRewriteRecordWR01 record, string reason)
        {
            record.admitted = false; record.active = false; record.legacySuppressed = false;
            record.state = "REFUSED"; record.reason = FantasyProofRulesV1.SafeText(reason);
            if (!_rewriteRecordsWR01.Contains(record)) _rewriteRecordsWR01.Add(record);
            Log.Warn("WR01 refused " + record.stableId + " / " + record.family + ": " + record.reason);
        }

        private static FantasyRewriteRecordWR01 RewriteRecordWR01(HeartRewritePlanWR01 plan)
        {
            return new FantasyRewriteRecordWR01
            {
                stableId = plan.StableId, family = plan.Family, seed = plan.Seed,
                origin = plan.Origin, yaw = plan.Yaw, originalBoundsCenter = plan.OriginalEnvelope.center,
                originalBoundsSize = plan.OriginalEnvelope.size, portalCenter = plan.PortalCenter,
                portalSize = plan.PortalSize, portalKnown = true, portalContract = plan.PortalContract, boundsFrame = "service-local",
                collisionOwner = plan.CollisionOwner, collisionPolicy = plan.CollisionPolicy,
                fitPolicy = plan.FitPolicy, sourceOwner = "VenueView04C.GothicHeartShellH3"
            };
        }

        internal bool StageCivicCrownWR01(HeartRewritePlanWR01 plan, GameObject legacy, Transform service,
            Action<Transform> addPhysicalWitness, out string reason)
        {
            FantasyRewriteRecordWR01 record = RewriteRecordWR01(plan);
            reason = "Missing or incomplete WR01 prepared catalog. Original crown retained.";
            if (_rewriteCatalog == null || !_rewriteCatalog.RewriteReady)
            { RefuseRewriteWR01(record, reason); return false; }
            GameObject candidate = null;
            try
            {
                HeartCatalogFR01 selectedCatalog = CivicCatalogFoundryWR02A(plan, record);
                HeartModuleFR01 module = selectedCatalog.Find(plan.Family);
                if (module == null || !HeartPlanFR01.RewriteArtBoundsWR01(plan.Family, new Bounds(module.Center, module.Size), out reason))
                { RefuseRewriteWR01(record, reason); return false; }
                foreach (GameObject prefab in module.Lods)
                    if (!ValidPreparedModelV1(prefab, out reason))
                    { RefuseRewriteWR01(record, "Required crown material validation: " + reason); return false; }
                candidate = new GameObject("WR01 civic candidate / " + plan.StableId);
                candidate.SetActive(false); candidate.transform.SetParent(service, false);
                GameObject model = Model(plan.Family, plan.Origin, plan.Yaw, Vector3.one, selectedCatalog);
                model.transform.SetParent(candidate.transform, true); model.SetActive(true);
                if (!CrownFramesWR01(model, service, plan.Family, out reason))
                    throw new InvalidOperationException(reason);
                // Exact low source surfaces remain visible wherever the authored crown does not enclose them.
                // Their original MeshCollider is outside both visual roots and is never modified.
                addPhysicalWitness(candidate.transform);
                if (!RegisterRewriteVisualCoreWR01(record, legacy, candidate, out reason))
                { UnityEngine.Object.Destroy(candidate); return false; }
                _rewritePairsWR01[_rewritePairsWR01.Count - 1].Imported = model;
                _rewritePairsWR01[_rewritePairsWR01.Count - 1].Civic = true;
                _rewritePairsWR01[_rewritePairsWR01.Count - 1].ImportedRequired = true;
                return true;
            }
            catch (Exception error)
            {
                if (candidate != null) UnityEngine.Object.Destroy(candidate);
                if (legacy != null) legacy.SetActive(true);
                reason = "Civic transaction retained original: " + error.Message;
                RefuseRewriteWR01(record, reason); return false;
            }
        }

        /// <summary>Register one completed passive visual transaction. Callers keep all original physics outside both roots.</summary>
        public bool RegisterRewriteVisual(string stableId, string family, GameObject legacyRoot,
            GameObject candidateRoot, string contract, out string reason)
        {
            FantasyRewriteRecordWR01 record = new FantasyRewriteRecordWR01
            {
                stableId = stableId ?? "", family = family ?? "", fitPolicy = contract ?? "",
                sourceOwner = stableId != null && stableId.StartsWith("under:", StringComparison.Ordinal) ? "UnderheartBuilder" : "UNSPECIFIED",
                collisionOwner = stableId != null && stableId.StartsWith("under:", StringComparison.Ordinal) ?
                    "UnderheartBuilder original mesh, box, floor and wall collision outside both isolated visual roots" :
                    "Existing original owner, outside both isolated visual roots"
            };
            return RegisterRewriteVisualCoreWR01(record, legacyRoot, candidateRoot, out reason);
        }

        /// <summary>Expose a failed source candidate without inventing a prepared or admitted object.</summary>
        public void RefuseRewriteVisual(string stableId, string family, string reason, string contract = "")
        {
            foreach (FantasyRewriteRecordWR01 prior in _rewriteRecordsWR01) if (prior.stableId == stableId) return;
            RefuseRewriteWR01(new FantasyRewriteRecordWR01
            { stableId = stableId ?? "", family = family ?? "", fitPolicy = contract ?? "" }, reason);
        }

        private bool RegisterRewriteVisualCoreWR01(FantasyRewriteRecordWR01 record, GameObject legacy,
            GameObject candidate, out string reason)
        {
            reason = "Stable key, isolated visual roots and a completed inactive candidate are required.";
            if (string.IsNullOrEmpty(record.stableId) || string.IsNullOrEmpty(record.family) || legacy == null || candidate == null ||
                candidate.activeSelf || legacy == candidate || legacy.transform.parent != candidate.transform.parent ||
                legacy.transform.IsChildOf(candidate.transform) || candidate.transform.IsChildOf(legacy.transform))
            { RefuseRewriteWR01(record, reason); return false; }
            foreach (FantasyRewriteRecordWR01 prior in _rewriteRecordsWR01)
                if (prior.stableId == record.stableId)
                { reason = "Duplicate stable visual key. The prior transaction is retained."; return false; }
            if (!PassiveRewriteWR01(legacy, out reason) || !PassiveRewriteWR01(candidate, out reason))
            { candidate.SetActive(false); legacy.SetActive(true); RefuseRewriteWR01(record, reason); return false; }
            if (record.boundsFrame == "UNSPECIFIED")
            {
                Bounds bounds = new Bounds(); bool first = true;
                foreach (Renderer renderer in legacy.GetComponentsInChildren<Renderer>(true))
                { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
                record.origin = legacy.transform.position; record.originalBoundsCenter = bounds.center;
                record.originalBoundsSize = bounds.size; record.boundsFrame = "world";
            }
            record.admitted = true; record.state = "ADMITTED_INACTIVE";
            record.reason = "Completed passive candidate admitted. Original source remains until explicit preview activation.";
            _rewriteRecordsWR01.Add(record);
            _rewritePairsWR01.Add(new RewritePairWR01 { Legacy = legacy, Candidate = candidate, Record = record });
            Log.Info("WR01 admitted " + record.stableId + " / " + record.family + ". " + record.fitPolicy);
            return true;
        }

        private static bool PassiveRewriteWR01(GameObject root, out string reason)
        {
            reason = "Visual root is absent."; if (root == null) return false;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                if (component == null || !(component is Transform || component is MeshFilter || component is MeshRenderer || component is LODGroup || component is OwnedMeshes))
                { reason = "Visual root contains a collider, nonpassive component or missing script: " + root.name; return false; }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount < 3)
                { reason = "Required visual mesh is absent: " + root.name; return false; }
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { reason = "Completed visual root has no renderer: " + root.name; return false; }
            bool eligibleRenderer = false;
            foreach (Renderer renderer in renderers)
            {
                eligibleRenderer |= EligibleRendererWR01(renderer, root.transform);
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0) { reason = "Required material slots are absent: " + renderer.name; return false; }
                foreach (Material material in materials)
                    if (material == null || material.shader == null || !material.shader.isSupported)
                    { reason = "Required material or supported shader is absent: " + renderer.name; return false; }
            }
            if (!eligibleRenderer) { reason = "Completed visual root has no enabled renderer inside its local hierarchy: " + root.name; return false; }
            reason = "Passive isolated visuals valid."; return true;
        }

        private static bool EligibleRendererWR01(Renderer renderer, Transform root)
        {
            if (renderer == null || !renderer.enabled || renderer.forceRenderingOff) return false;
            Transform current = renderer.transform;
            // The candidate itself is intentionally inactive while staged, and its original streamed parent may be
            // inactive. Only disabled children inside this visual root invalidate the completed candidate.
            while (current != null && current != root)
            { if (!current.gameObject.activeSelf) return false; current = current.parent; }
            return current == root;
        }

        private static bool CrownFramesWR01(GameObject root, Transform service, string family, out string reason)
        {
            Bounds actual = new Bounds(); bool first = true;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Bounds source = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = service.InverseTransformPoint(filter.transform.TransformPoint(new Vector3(
                        (corner & 1) == 0 ? source.min.x : source.max.x,
                        (corner & 2) == 0 ? source.min.y : source.max.y,
                        (corner & 4) == 0 ? source.min.z : source.max.z)));
                    if (first) { actual = new Bounds(point, Vector3.zero); first = false; } else actual.Encapsulate(point);
                }
            }
            reason = "No actual crown geometry.";
            return !first && HeartPlanFR01.RewriteArtBoundsWR01(family, actual, out reason);
        }

        private void ApplyWorldRewriteWR01()
        {
            bool active = WorldRewriteSelectedWR01 && !_compare && !_suspended;
            foreach (RewritePairWR01 pair in _rewritePairsWR01)
            {
                FantasyRewriteRecordWR01 record = pair.Record;
                if (!record.admitted) continue;
                if (!active)
                {
                    if (pair.Candidate != null) pair.Candidate.SetActive(false);
                    if (pair.Legacy != null) pair.Legacy.SetActive(true);
                    record.active = false; record.legacySuppressed = false; record.state = "ADMITTED_INACTIVE";
                    continue;
                }
                string reason;
                if (!PassiveRewriteWR01(pair.Legacy, out reason) || !PassiveRewriteWR01(pair.Candidate, out reason) ||
                    !ValidCivicCrownWR01(pair, out reason))
                {
                    if (pair.Candidate != null) pair.Candidate.SetActive(false);
                    if (pair.Legacy != null) pair.Legacy.SetActive(true);
                    RefuseRewriteWR01(record, "Activation refused: " + reason); continue;
                }
                if (pair.Candidate != null && pair.Candidate.activeSelf && pair.Legacy != null && !pair.Legacy.activeSelf) continue;
                try
                {
                    pair.Candidate.SetActive(true);
                    if (!pair.Candidate.activeSelf) throw new InvalidOperationException("Candidate did not activate; original visual retained.");
                    pair.Legacy.SetActive(false);
                    record.active = pair.Candidate.activeInHierarchy; record.legacySuppressed = true;
                    record.state = record.active ? "ACTIVE" : "ARMED_STREAMED_OUT";
                    record.reason = "Candidate active, only its matching isolated legacy visual suppressed. Native review remains required.";
                }
                catch (Exception error)
                {
                    if (pair.Candidate != null) pair.Candidate.SetActive(false);
                    if (pair.Legacy != null) pair.Legacy.SetActive(true);
                    RefuseRewriteWR01(record, "Activation rolled back: " + error.Message);
                }
            }
        }

        private void RefreshRewriteObservationsWR01()
        {
            foreach (RewritePairWR01 pair in _rewritePairsWR01)
            {
                FantasyRewriteRecordWR01 record = pair.Record;
                string reason;
                if (record.admitted && pair.Candidate == null)
                {
                    if (pair.Legacy != null) pair.Legacy.SetActive(true);
                    RefuseRewriteWR01(record, "Required candidate was destroyed. Matching original visual restored.");
                }
                else if (record.admitted && pair.Candidate != null && pair.Candidate.activeSelf &&
                    (!PassiveRewriteWR01(pair.Candidate, out reason) || !ValidCivicCrownWR01(pair, out reason)))
                {
                    pair.Candidate.SetActive(false); if (pair.Legacy != null) pair.Legacy.SetActive(true);
                    RefuseRewriteWR01(record, "Active crown restored original fallback: " + reason);
                }
                record.active = record.admitted && HasActiveRendererWR01(pair.ImportedRequired ? pair.Imported : pair.Candidate);
                record.legacySuppressed = record.admitted && pair.Legacy != null && !pair.Legacy.activeSelf;
                if (record.admitted) record.state = record.active ? "ACTIVE" :
                    pair.Candidate != null && pair.Candidate.activeSelf ?
                        pair.Candidate.activeInHierarchy ? "ARMED_NO_ENABLED_RENDERER" : "ARMED_STREAMED_OUT" : "ADMITTED_INACTIVE";
            }
        }

        private static bool HasActiveRendererWR01(GameObject root)
        {
            if (root == null || !root.activeInHierarchy) return false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && !renderer.forceRenderingOff) return true;
            return false;
        }

        private static bool ValidCivicCrownWR01(RewritePairWR01 pair, out string reason)
        {
            reason = "Not an imported replacement."; if (!pair.ImportedRequired) return true;
            if (pair.Imported == null || !pair.Imported.activeSelf)
            { reason = "Required imported replacement is missing or disabled. Source witnesses cannot stand in for imported art."; return false; }
            if (!ValidPreparedModelV1(pair.Imported, out reason) ||
                !CrownFramesWR01(pair.Imported, pair.ImportedRequired && !pair.Civic ? pair.Imported.transform : pair.Candidate.transform.parent, pair.Record.family, out reason)) return false;
            foreach (Renderer renderer in pair.Imported.GetComponentsInChildren<Renderer>(true))
                if (EligibleRendererWR01(renderer, pair.Imported.transform)) return true;
            reason = "Required imported replacement has no enabled local renderer. Source witnesses cannot stand in for imported art.";
            return false;
        }

        /// <summary>Independent observed replacement facts. Frustum candidates do not prove visibility or artistic dominance.</summary>
        public void FillRewriteProofWR01(FantasyProofSnapshotV1 proof, Camera camera)
        {
            RefreshRewriteObservationsWR01();
            FantasyRewriteProofWR01 result = new FantasyRewriteProofWR01
            { selected = WorldRewriteSelectedWR01, prepared = _rewriteCatalog != null && _rewriteCatalog.RewriteReady,
                policyActive = WorldRewriteSelectedWR01 && !_compare && !_suspended, planned = _rewriteRecordsWR01.Count,
                status = WorldRewriteStatusWR01 };
            Plane[] planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            foreach (FantasyRewriteRecordWR01 record in _rewriteRecordsWR01)
            {
                if (record.admitted) result.admitted++; else result.refused++;
                if (record.active) result.active++; if (record.legacySuppressed) result.legacyGroupsSuppressed++;
            }
            foreach (RewritePairWR01 pair in _rewritePairsWR01)
            {
                pair.Record.importedCameraCandidate = false;
                if (pair.Candidate == null) continue;
                if (pair.Record.active)
                {
                    if (pair.Civic) result.activeCivicCrowns++;
                    else if (pair.ImportedRequired) result.activeSkylineMasses++;
                    else if (pair.Record.stableId.StartsWith("under:", StringComparison.Ordinal)) result.activeUndergroundCells++;
                }
                foreach (Renderer renderer in pair.Candidate.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                    result.activeRenderers++;
                    if (Game.Player != null && (renderer.bounds.center - Game.PlayerPosition).sqrMagnitude <= 10000f) result.nearPlayerRenderers++;
                    if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) result.inCameraFrustumRenderers++;
                }
                if (pair.Imported == null) continue;
                int enabledImported = 0, frustumImported = 0;
                foreach (Renderer renderer in pair.Imported.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                    enabledImported++;
                    if (pair.Civic) result.importedCrownRenderers++; else result.importedSkylineRenderers++;
                    if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    { frustumImported++; if (pair.Civic) result.importedCrownFrustumRenderers++; else result.importedSkylineFrustumRenderers++; }
                }
                pair.Record.importedCameraCandidate = HeartPlanFR01.RewriteImportedEvidenceWR01(pair.Imported != null,
                    pair.Imported != null && pair.Imported.activeInHierarchy, pair.Record.admitted, enabledImported, frustumImported);
            }
            result.records = new FantasyRewriteRecordWR01[_rewriteRecordsWR01.Count];
            for (int index = 0; index < result.records.Length; index++)
            {
                FantasyRewriteRecordWR01 source = _rewriteRecordsWR01[index];
                result.records[index] = new FantasyRewriteRecordWR01
                {
                    stableId = source.stableId, family = source.family, state = source.state, reason = source.reason,
                    collisionOwner = source.collisionOwner, fitPolicy = source.fitPolicy, sourceOwner = source.sourceOwner,
                    boundsFrame = source.boundsFrame, portalContract = source.portalContract, collisionPolicy = source.collisionPolicy, origin = source.origin,
                    originalBoundsCenter = source.originalBoundsCenter, originalBoundsSize = source.originalBoundsSize,
                    portalCenter = source.portalCenter, portalSize = source.portalSize, yaw = source.yaw, seed = source.seed,
                    admitted = source.admitted, active = source.active, legacySuppressed = source.legacySuppressed,
                    portalKnown = source.portalKnown, importedCameraCandidate = source.importedCameraCandidate
                };
            }
            proof.worldRewrite = result;
        }
    }
}
