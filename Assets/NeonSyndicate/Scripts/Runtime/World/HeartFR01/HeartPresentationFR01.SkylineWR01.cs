using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private sealed class SkylineCaptureWR01
        {
            public GameObject Legacy, Candidate, Imported;
            public FantasyRewriteRecordWR01 Record;
            public int Appends;
            public bool Failed;
            public Bounds SourceBounds;
        }

        private readonly Dictionary<Building, SkylineCaptureWR01> _skylineCapturesWR01 = new Dictionary<Building, SkylineCaptureWR01>();

        /// <summary>Capture only the exact audited emitter before shared merging. Its caller still creates every original collider.</summary>
        private bool CaptureRewriteBuildingWR01(Building building, MeshBuilder source, Material material)
        {
            HeartRewritePlanWR01 plan; string reason;
            if (!HeartPlanFR01.TryRewriteBuildingPlanWR01(_layout, building, out plan, out reason)) return false;
            SkylineCaptureWR01 capture;
            if (!_skylineCapturesWR01.TryGetValue(building, out capture))
            {
                capture = new SkylineCaptureWR01 { Record = RewriteRecordWR01(plan) };
                capture.Record.portalKnown = false; capture.Record.sourceOwner = "CityBuilder.Gothic04OB.BuildGothicBuilding04OB";
                _skylineCapturesWR01.Add(building, capture);
                try
                {
                    if (_rewriteCatalog == null || !_rewriteCatalog.RewriteReady)
                        throw new InvalidOperationException("Required skyline catalog is missing or incomplete.");
                    HeartModuleFR01 module = _rewriteCatalog.Find(plan.Family);
                    if (module == null || module.Lods == null || module.Lods.Length != 3)
                        throw new InvalidOperationException("Three prepared skyline LODs are required: " + plan.Family);
                    foreach (GameObject prefab in module.Lods)
                        if (!ValidPreparedModelV1(prefab, out reason)) throw new InvalidOperationException(reason);
                    if (!HeartPlanFR01.RewriteArtBoundsWR01(plan.Family, new Bounds(module.Center, module.Size), out reason))
                        throw new InvalidOperationException(reason);
                    capture.Legacy = new GameObject("WR01 isolated original skyline / " + building.Seed);
                    capture.Legacy.transform.SetParent(_parent, false);
                    capture.Candidate = new GameObject("WR01 skyline candidate / " + building.Seed);
                    capture.Candidate.SetActive(false); capture.Candidate.transform.SetParent(_parent, false);
                    capture.Imported = Model(plan.Family, plan.Origin, 0, Vector3.one, _rewriteCatalog);
                    capture.Imported.transform.SetParent(capture.Candidate.transform, true); capture.Imported.SetActive(true);
                    if (!CrownFramesWR01(capture.Imported, capture.Imported.transform, plan.Family, out reason))
                        throw new InvalidOperationException(reason);
                }
                catch (Exception error)
                {
                    capture.Failed = true;
                    if (capture.Candidate != null) UnityEngine.Object.Destroy(capture.Candidate);
                    if (capture.Legacy != null) capture.Legacy.SetActive(true);
                    RefuseRewriteWR01(capture.Record, "Original skyline retained: " + error.Message); return false;
                }
            }
            if (capture.Failed) return false;
            bool originalCaptured = false;
            try
            {
                if (source == null || source.VertexCount < 3) throw new InvalidOperationException("One of three original material appends is missing.");
                Mesh original = source.ToMesh("WR01 original skyline material / " + building.Seed);
                GameObject legacyPart = Shapes.MeshObject("Original skyline material", original, new[] { material }, capture.Legacy.transform, true);
                OwnedMeshes.Track(legacyPart, original);
                originalCaptured = true;
                Mesh physical = source.ToMesh("WR01 retained skyline physical witness / " + building.Seed);
                GameObject witness = Shapes.MeshObject("Original physical witness", physical, new[] { material }, capture.Candidate.transform, true);
                OwnedMeshes.Track(witness, physical);
                if (capture.Appends == 0) capture.SourceBounds = original.bounds; else capture.SourceBounds.Encapsulate(original.bounds);
                capture.Appends++;
                if (capture.Appends == 3)
                {
                    capture.Record.originalBoundsCenter = capture.SourceBounds.center;
                    capture.Record.originalBoundsSize = capture.SourceBounds.size; capture.Record.boundsFrame = "world";
                    if (!RegisterRewriteVisualCoreWR01(capture.Record, capture.Legacy, capture.Candidate, out reason))
                    { capture.Failed = true; capture.Candidate.SetActive(false); capture.Legacy.SetActive(true); return true; }
                    RewritePairWR01 pair = _rewritePairsWR01[_rewritePairsWR01.Count - 1];
                    pair.Imported = capture.Imported; pair.ImportedRequired = true;
                }
                return true;
            }
            catch (Exception error)
            {
                capture.Failed = true;
                if (capture.Candidate != null) capture.Candidate.SetActive(false);
                if (capture.Legacy != null) capture.Legacy.SetActive(true);
                RefuseRewriteWR01(capture.Record, "Skyline append transaction restored original: " + error.Message);
                // Previously captured source parts are already visible in Legacy. This failed part and all following
                // parts return through the caller's original chunk path, retaining the complete original frame.
                return originalCaptured;
            }
        }
    }
}
