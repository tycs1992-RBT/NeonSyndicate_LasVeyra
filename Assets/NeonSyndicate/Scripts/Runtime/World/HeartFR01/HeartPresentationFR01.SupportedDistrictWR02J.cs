using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private sealed class SupportedCaptureWR02J
        {
            public GameObject Original, Candidate;
            public int Appends;
            public bool Failed;
            public string Reason;
        }

        private readonly Dictionary<Building, SupportedCaptureWR02J> _supportedLotsWR02J = new Dictionary<Building, SupportedCaptureWR02J>();

        internal static Material SupportedMaterialWR02J(string role)
        {
            Material material = Resources.Load<Material>("HeartPreparedFR01/" + role);
            if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null ||
                !material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap") == null ||
                !material.HasProperty("_MetallicGlossMap") || material.GetTexture("_MetallicGlossMap") == null)
                throw new InvalidOperationException("Required prepared district material/maps absent: " + role);
            return material;
        }

        /// <summary>Partition before shared batching. The caller still emits the exact original physical mesh.</summary>
        private bool CaptureSupportedLotWR02J(Building building, MeshBuilder source, Material material)
        {
            SupportedCaptureWR02J capture;
            if (!_supportedLotsWR02J.TryGetValue(building, out capture))
            {
                capture = new SupportedCaptureWR02J(); _supportedLotsWR02J.Add(building, capture);
                if (_catalog == null || !_catalog.Ready || !HeartPlanFR01.SupportedLotFrameWR02J(building) ||
                    !HeartPlanFR01.Eligible(_layout, building) || !SupportedParcelWR02J(building))
                {
                    capture.Failed = true; capture.Reason = "Required FR01 preparation, source frame or protected parcel check failed. Original physical architecture retained.";
                    return false;
                }
                capture.Original = new GameObject("WR02J original dwelling / " + building.Seed);
                capture.Original.transform.SetParent(_parent, false);
                capture.Candidate = new GameObject("WR02J supported " + (building.Seed == 961727 ? "raised hall" : "mineral dwelling") + " / " + building.Seed);
                capture.Candidate.SetActive(false); capture.Candidate.transform.SetParent(_parent, false);
            }
            if (capture.Failed) return false;
            bool retained = false;
            try
            {
                if (capture.Appends >= 3 || source == null || source.VertexCount < 3)
                    throw new InvalidOperationException("Expected the three original stone, trim and mineral appends.");
                Mesh old = source.ToMesh("WR02J original structural material");
                GameObject original = Shapes.MeshObject("Original material", old, new[] { material }, capture.Original.transform, true);
                OwnedMeshes.Track(original, old); retained = true;
                Dictionary<string, MeshBuilder> art = HeartPlanFR01.SupportedLotArtWR02J(building, source, capture.Appends);
                foreach (KeyValuePair<string, MeshBuilder> group in art)
                {
                    Material finish = SupportedMaterialWR02J(group.Key);
                    Mesh mesh = group.Value.ToMesh("WR02J actual open architecture / " + group.Key);
                    GameObject part = Shapes.MeshObject(group.Key, mesh, new[] { finish }, capture.Candidate.transform, true);
                    OwnedMeshes.Track(part, mesh);
                }
                capture.Appends++;
                return true;
            }
            catch (Exception error)
            {
                capture.Failed = true; capture.Reason = FantasyProofRulesV1.SafeText(error.Message);
                if (capture.Candidate != null) capture.Candidate.SetActive(false);
                if (capture.Original != null) capture.Original.SetActive(true);
                // If the source is already retained, do not append it again to the shared chunk.
                return retained;
            }
        }

        private bool SupportedParcelWR02J(Building building)
        {
            foreach (Block block in _layout.Blocks) foreach (Lot lot in block.Lots)
                if (lot.Buildings.Contains(building))
                    return !lot.CustomBuilt && lot.Poi == PoiType.None && string.IsNullOrEmpty(lot.PoiName) &&
                        lot.Use == LotUse.House && lot.Buildings.Count == 1 && lot.Seed == building.Seed;
            return false;
        }

        private void FinishSupportedLotsWR02J()
        {
            foreach (KeyValuePair<Building, SupportedCaptureWR02J> entry in _supportedLotsWR02J)
            {
                SupportedCaptureWR02J capture = entry.Value;
                string id = "revision:10/seed:" + entry.Key.Seed + "/supported-dwelling";
                if (capture.Failed || capture.Appends != 3)
                {
                    if (capture.Candidate != null) capture.Candidate.SetActive(false);
                    if (capture.Original != null) capture.Original.SetActive(true);
                    RefuseRewriteVisual(id, "SupportedDwellingWR02J", capture.Reason ?? "Incomplete three-part source capture.");
                    continue;
                }
                string reason;
                if (!RegisterRewriteVisual(id, "SupportedDwellingWR02J", capture.Original, capture.Candidate,
                    "Exact original structural triangles, no envelope-fitted shell. Existing pierced hall, stairs, ledges and roof support remain. " +
                    "No new service, interior furnishing, collider or saved identity.", out reason))
                { capture.Candidate.SetActive(false); capture.Original.SetActive(true); }
            }
        }
    }
}
