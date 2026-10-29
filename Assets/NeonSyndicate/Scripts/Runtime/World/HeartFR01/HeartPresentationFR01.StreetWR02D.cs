using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private GameObject _streetLegacyWR02D, _streetCandidateWR02D;

        /// <summary>Build a bounded street over existing supported routes; retain the previous paving as a live fallback.</summary>
        private bool TryStreetWR02D(List<GameObject> pending)
        {
            GameObject root = null;
            try
            {
                float height;
                List<HeartStreetPieceWR02D> pieces = HeartPlanFR01.StreetPlanWR02D(_layout.RealmRoutes04OB, out height);
                if (pieces.Count == 0) throw new InvalidOperationException("Street layout is incomplete or invalid.");
                Material stone = StreetMaterialWR02D("Masonry"), edging = StreetMaterialWR02D("CarvedTrim");
                root = new GameObject("WR02D supported home, sanctuary and Forge street"); root.SetActive(false); root.transform.SetParent(_parent, false);
                GameObject legacy = new GameObject("Retained original FR01 paving"); legacy.SetActive(false); legacy.transform.SetParent(root.transform, false);
                GameObject candidate = new GameObject("Continuous stone street and edges"); candidate.SetActive(false); candidate.transform.SetParent(root.transform, false);
                MeshBuilder centre = new MeshBuilder(1), edge = new MeshBuilder(1);
                foreach (HeartStreetPieceWR02D piece in pieces)
                {
                    MeshBuilder builder = piece.Edge ? edge : centre;
                    for (int i = 1; i + 1 < piece.Points.Count; i++)
                    {
                        Vector2 a = piece.Points[0], b = piece.Points[i + 1], c = piece.Points[i];
                        float area = (b.x-a.x)*(c.y-a.y) - (b.y-a.y)*(c.x-a.x);
                        if (Mathf.Abs(area) < .00001f) continue;
                        Vector3 pa = new Vector3(a.x, height + .021f, a.y), pb = new Vector3(b.x, height + .021f, b.y), pc = new Vector3(c.x, height + .021f, c.y);
                        builder.Triangle(0, pa, pb, pc, Vector3.up, a * .5f, b * .5f, c * .5f);
                    }
                }
                if (centre.VertexCount == 0 || edge.VertexCount == 0) throw new InvalidOperationException("Street centre or edge geometry is empty.");
                StreetMeshWR02D(candidate, centre, stone, "Stone paving");
                StreetMeshWR02D(candidate, edge, edging, "Stone edges");
                string why;
                if (!ValidPreparedModelV1(candidate, out why)) throw new InvalidOperationException(why);
                // The original paved tile placements are kept inactive under this same route root.
                foreach (RealmRoute04OB route in _layout.RealmRoutes04OB)
                {
                    if (route.Id != "home-court" && route.Id != "forge-market" && route.Id != "court-crossing" && route.Id != "sanctuary-approach") continue;
                    for (int i = 1; i < route.Points.Count; i++)
                    {
                        Vector3 a = route.Points[i-1], b = route.Points[i], d = MathUtil.Flat(b-a); float length = d.magnitude;
                        int count = Mathf.CeilToInt(length / 4f);
                        for (int j = 0; j < count; j++)
                        {
                            Vector3 p = Vector3.Lerp(a, b, (j + .5f) / count) + Vector3.up * .021f;
                            GameObject tile = Model("HeartStreet", p, MathUtil.YawOf(d), new Vector3(route.Width / 4f, 1f, length / count / 4f));
                            tile.transform.SetParent(legacy.transform, true); tile.SetActive(true);
                        }
                    }
                }
                _streetLegacyWR02D = legacy; _streetCandidateWR02D = candidate;
                candidate.SetActive(true); pending.Add(root); return true;
            }
            catch (Exception error)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                _streetLegacyWR02D = null; _streetCandidateWR02D = null;
                Log.Warn("WR02D continuous street refused; retained original paving: " + FantasyProofRulesV1.SafeText(error.Message));
                return false;
            }
        }

        private static Material StreetMaterialWR02D(string name)
        {
            Material value = Resources.Load<Material>("HeartPreparedFR01/" + name);
            if (value == null || value.shader == null || !value.shader.isSupported || value.mainTexture == null ||
                !value.HasProperty("_BumpMap") || value.GetTexture("_BumpMap") == null ||
                !value.HasProperty("_MetallicGlossMap") || value.GetTexture("_MetallicGlossMap") == null)
                throw new InvalidOperationException("Street material or map missing: " + name);
            return value;
        }

        private static void StreetMeshWR02D(GameObject parent, MeshBuilder builder, Material material, string name)
        {
            Mesh mesh = builder.ToMesh("WR02D " + name);
            try
            {
                GameObject child = Shapes.MeshObject(name, mesh, new[] { material }, parent.transform, false);
                OwnedMeshes.Track(child, mesh); child.layer = Layers.IgnoreRaycast;
            }
            catch { UnityEngine.Object.Destroy(mesh); throw; }
        }

        private void ReviewStreetWR02D()
        {
            if (_streetLegacyWR02D == null && _streetCandidateWR02D == null) return;
            string reason = "The candidate was destroyed.";
            if (_streetCandidateWR02D == null || !ValidPreparedModelV1(_streetCandidateWR02D, out reason))
            {
                if (_streetCandidateWR02D != null) _streetCandidateWR02D.SetActive(false);
                string legacyReason = "The retained paving was destroyed.";
                bool legacyValid = _streetLegacyWR02D != null && ValidPreparedModelV1(_streetLegacyWR02D, out legacyReason);
                if (_streetLegacyWR02D != null) _streetLegacyWR02D.SetActive(legacyValid);
                _streetLegacyWR02D = null; _streetCandidateWR02D = null;
                Log.Warn(legacyValid ? "WR02D street restored original paving: " + FantasyProofRulesV1.SafeText(reason) :
                    "WR02D street and retained paving refused; original supported road remains: " + FantasyProofRulesV1.SafeText(reason) + "; " + FantasyProofRulesV1.SafeText(legacyReason));
            }
        }
    }
}
