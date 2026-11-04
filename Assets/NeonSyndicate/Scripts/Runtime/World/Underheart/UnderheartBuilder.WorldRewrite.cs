using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Pure upper-region eligibility and material roles. Existing cells and physical owners are never changed.</summary>
    public sealed class UpperUnderheartRewriteRules
    {
        public bool Eligible(UhCell cell)
        {
            return cell != null && !string.IsNullOrEmpty(cell.Id) && !cell.Vehicle && !cell.OpenTop
                && (cell.Region == UnderheartIds.ServiceLevel || cell.Region == UnderheartIds.LanternRefuge)
                && (cell.Theme == UhTheme.Service || cell.Theme == UhTheme.Transit)
                && cell.SizeX > 0f && cell.SizeZ > 0f && cell.Height >= 3f;
        }

        /// <summary>Only industrial decoration is remapped. Crystals, heat, screens, cloth and natural rock retain their identities.</summary>
        public string MaterialRole(string original)
        {
            switch (original)
            {
                case "Underheart Concrete":
                case "Underheart Service Floor":
                case "Underheart Terrazzo":
                case "Underheart Iron Brick": return "Masonry";
                case "Underheart Transit Tile":
                case "Underheart Hazard Stripes":
                case "Underheart Grating": return "CarvedTrim";
                case "Underheart Riveted Plate": return "SlateTimber";
                case "Underheart Iron":
                case "Underheart Safety Rail":
                case "Underheart Copper Pipe": return "AmethystBronze";
                default: return "";
            }
        }

        /// <summary>Decorative overhead ribs retain at least 2.8 metres of original walk clearance.</summary>
        public bool RibProfile(UhCell cell, out float spring, out float rise)
        {
            spring = 0f; rise = 0f;
            if (!Eligible(cell) || cell.Sloped || cell.Kind == UhCellKind.Stair || cell.Rock || cell.Height < 3.2f) return false;
            rise = Mathf.Min(cell.Height * .28f, 2.4f);
            spring = Mathf.Max(2.8f, cell.Height - rise - .18f);
            rise = cell.Height - spring - .18f;
            return rise >= .25f;
        }
    }

    public sealed partial class UnderheartBuilder
    {
        private sealed class UpperRewritePair
        {
            public UhCell Cell;
            public GameObject Legacy, Candidate;
            public bool Failed;
        }

        private readonly UpperUnderheartRewriteRules _upperRewriteRules = new UpperUnderheartRewriteRules();
        private readonly Dictionary<string, UpperRewritePair> _upperRewritePairs = new Dictionary<string, UpperRewritePair>(StringComparer.Ordinal);
        private readonly Dictionary<string, UhCell> _upperRewriteBuckets = new Dictionary<string, UhCell>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _upperRewriteSurfaces = new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly HashSet<string> _upperRewriteFailedCells = new HashSet<string>(StringComparer.Ordinal);
        private UhCell _upperRewriteCell;
        private HeartPresentationFR01 _upperRewriteHeart;
        private bool _upperRewriteReady;
        private string _upperRewriteReason = "Original upper region retained; FR01 shared surfaces are not prepared.";

        /// <summary>Reuse the existing prepared FR01 library. Missing maps keep the original complete upper region.</summary>
        private void PrepareUnderheartRewrite()
        {
            try
            {
                if (Game.Layout == null || !Game.Layout.GothicCivicH5) return;
                _upperRewriteHeart = FantasyProofV1.CurrentHeartV1;
                if (_upperRewriteHeart == null) return;
                HeartCatalogFR01 catalog = Resources.Load<HeartCatalogFR01>("HeartPreparedFR01/Catalog");
                if (catalog == null || !catalog.Ready || catalog.Modules == null) return;
                foreach (HeartModuleFR01 module in catalog.Modules)
                {
                    if (module == null || module.Lods == null || module.Lods.Length == 0 || module.Lods[0] == null) continue;
                    foreach (MeshRenderer renderer in module.Lods[0].GetComponentsInChildren<MeshRenderer>(true))
                        foreach (Material surface in renderer.sharedMaterials)
                        {
                            if (surface == null || surface.shader == null || !surface.shader.isSupported || surface.mainTexture == null
                                || surface.GetTexture("_BumpMap") == null || surface.GetTexture("_MetallicGlossMap") == null) continue;
                            if (surface.name == "Masonry" || surface.name == "CarvedTrim" || surface.name == "SlateTimber" || surface.name == "AmethystBronze")
                                _upperRewriteSurfaces[surface.name] = surface;
                        }
                }
                _upperRewriteReady = _upperRewriteSurfaces.Count == 4;
                if (!_upperRewriteReady)
                {
                    List<string> missing = new List<string>();
                    foreach (string role in new[] { "Masonry", "CarvedTrim", "SlateTimber", "AmethystBronze" })
                        if (!_upperRewriteSurfaces.ContainsKey(role)) missing.Add(role);
                    _upperRewriteReason = "Prepared FR01 material lacks a supported shader or albedo, normal or metallic map: " + string.Join(", ", missing.ToArray());
                }
            }
            catch (Exception e)
            {
                _upperRewriteReady = false;
                _upperRewriteReason = e.Message;
                Problems.Add("Upper region visual fallback: " + e.Message);
                Log.Warn("Upper region visual fallback: " + e.Message);
            }
        }

        private void BeginUnderheartRewriteCell(UhCell cell)
        {
            _upperRewriteCell = _upperRewriteReady && _upperRewriteRules.Eligible(cell) ? cell : null;
        }

        private void EndUnderheartRewriteCell() { _upperRewriteCell = null; }

        private void FailUnderheartRewriteCell(UhCell cell)
        {
            if (!_upperRewriteReady || !_upperRewriteRules.Eligible(cell)) return;
            _upperRewriteFailedCells.Add(cell.Id);
            UpperRewritePair pair;
            if (_upperRewritePairs.TryGetValue(cell.Id, out pair)) pair.Failed = true;
        }

        private string UnderheartRewriteBucketSuffix()
        {
            return _upperRewriteCell != null ? "|upper-rewrite:" + _upperRewriteCell.Id : "";
        }

        private void RememberUnderheartRewriteBucket(string key)
        {
            if (_upperRewriteCell != null) _upperRewriteBuckets[key] = _upperRewriteCell;
        }

        /// <summary>Duplicate only visual draw submission. The exact original mesh and every physical collider remain owned by this builder.</summary>
        private void CaptureUnderheartRewriteBucket(string key, Material material, Mesh mesh, GameObject legacy)
        {
            UhCell cell;
            if (!_upperRewriteBuckets.TryGetValue(key, out cell)) return;
            try
            {
                UpperRewritePair pair;
                if (!_upperRewritePairs.TryGetValue(cell.Id, out pair))
                {
                    GameObject oldRoot = new GameObject("Retained upper region / " + cell.Id);
                    oldRoot.transform.SetParent(Group(GroupOf(cell)), false);
                    GameObject newRoot = new GameObject("Masonry upper region / " + cell.Id);
                    newRoot.SetActive(false); newRoot.transform.SetParent(Group(GroupOf(cell)), false);
                    pair = new UpperRewritePair { Cell = cell, Legacy = oldRoot, Candidate = newRoot, Failed = _upperRewriteFailedCells.Contains(cell.Id) };
                    _upperRewritePairs.Add(cell.Id, pair);
                }
                legacy.transform.SetParent(pair.Legacy.transform, false);
                if (pair.Failed) return;
                string role = _upperRewriteRules.MaterialRole(material.name);
                Material surface = role.Length > 0 ? _upperRewriteSurfaces[role] : material;
                GameObject visual = Shapes.MeshObject("Rebuilt material / " + material.name, mesh, new[] { surface }, pair.Candidate.transform, false);
                visual.layer = legacy.layer; visual.isStatic = true;
                MeshRenderer original = legacy.GetComponent<MeshRenderer>(), copy = visual.GetComponent<MeshRenderer>();
                copy.shadowCastingMode = original.shadowCastingMode; copy.receiveShadows = original.receiveShadows;
                copy.lightProbeUsage = original.lightProbeUsage; copy.reflectionProbeUsage = original.reflectionProbeUsage;
                RendererCount++; TriangleCount += mesh.triangles.Length / 3;
            }
            catch (Exception e)
            {
                UpperRewritePair pair;
                if (_upperRewritePairs.TryGetValue(cell.Id, out pair)) { pair.Failed = true; pair.Legacy.SetActive(true); pair.Candidate.SetActive(false); }
                Problems.Add("Upper region " + cell.Id + " retained: " + e.Message);
                Log.Warn("Upper region " + cell.Id + " retained: " + e.Message);
            }
        }

        /// <summary>Completed inactive cells register with the existing Heart transaction, comparison and temporary-session lifetime.</summary>
        private void FinishUnderheartRewrite()
        {
            if (_upperRewriteHeart != null && !_upperRewriteReady)
            {
                foreach (UhCell cell in L.Cells)
                    if (_upperRewriteRules.Eligible(cell)) _upperRewriteHeart.RefuseRewriteVisual("under:" + cell.Id, "UpperUnderheartMasonry", _upperRewriteReason,
                        "Original venue-frame " + cell + "; all physical/gameplay owners preserved.");
                return;
            }
            foreach (UpperRewritePair pair in _upperRewritePairs.Values)
            {
                if (pair.Failed)
                {
                    _upperRewriteHeart.RefuseRewriteVisual("under:" + pair.Cell.Id, "UpperUnderheartMasonry", "Original shell or dressing did not finish. Original cell presentation retained; inspect the Underheart build problem.");
                    continue;
                }
                try
                {
                    BuildUpperRewriteStructure(pair);
                    string reason;
                    string contract = "Original venue-frame " + pair.Cell + "; exact original shell mesh, doors, anchors, floor and always-on collision retained. Decorative ribs retain 2.8m headroom. Native collision acceptance NOT RUN.";
                    if (!_upperRewriteHeart.RegisterRewriteVisual("under:" + pair.Cell.Id, "UpperUnderheartMasonry", pair.Legacy, pair.Candidate, contract, out reason))
                        Problems.Add("Upper region " + pair.Cell.Id + " retained: " + reason);
                }
                catch (Exception e)
                {
                    pair.Failed = true; pair.Candidate.SetActive(false); pair.Legacy.SetActive(true);
                    Problems.Add("Upper region " + pair.Cell.Id + " retained: " + e.Message);
                    Log.Warn("Upper region " + pair.Cell.Id + " retained: " + e.Message);
                }
            }
            _upperRewriteBuckets.Clear();
        }

        /// <summary>Ceiling ribs and restrained crystal conduits change the spatial rhythm, without a new solid, light or interaction.</summary>
        private void BuildUpperRewriteStructure(UpperRewritePair pair)
        {
            UhCell cell = pair.Cell; float spring, rise;
            if (!_upperRewriteRules.RibProfile(cell, out spring, out rise)) return;
            MeshBuilder stone = new MeshBuilder(1), bronze = new MeshBuilder(1);
            bool alongZ = cell.SizeZ >= cell.SizeX;
            float width = (alongZ ? cell.SizeX : cell.SizeZ) - .08f;
            float length = alongZ ? cell.SizeZ : cell.SizeX;
            int bays = Mathf.Clamp(Mathf.FloorToInt(length / 5f), 1, 10);
            for (int i = 0; i < bays; i++)
            {
                float t = (i + .5f) / bays;
                Vector3 origin = new Vector3(alongZ ? cell.CenterX : Mathf.Lerp(cell.X0, cell.X1, t), cell.FloorLo,
                    alongZ ? Mathf.Lerp(cell.Z0, cell.Z1, t) : cell.CenterZ);
                GothicRealmGeometry04OB.Vault(stone, origin, alongZ ? 0f : 90f, width, .18f, spring, rise, .13f, 12);
                // Narrow seam of ancient bronze runs above any doorway's usable opening.
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 at = origin + (alongZ ? Vector3.right : Vector3.forward) * (side * (width * .5f - .06f)) + Vector3.up * (spring + .02f);
                    GothicRealmGeometry04OB.Box(bronze, Vector3.zero, 0f, at, new Vector3(.12f, .14f, .12f));
                }
            }
            // The refuge hall gets a longitudinal ridge linking its repeated pointed bays.
            if (cell.Id == "refuge.hall")
            {
                Vector3 a = new Vector3(cell.CenterX, cell.FloorLo + cell.Height - .12f, cell.Z0 + .2f);
                Vector3 b = new Vector3(cell.CenterX, a.y, cell.Z1 - .2f);
                GothicRealmGeometry04OB.Beam(bronze, a, b, .12f, .14f);
            }
            AddUpperRewriteMesh(pair, stone, _upperRewriteSurfaces["CarvedTrim"], "Pointed masonry ribs");
            AddUpperRewriteMesh(pair, bronze, _upperRewriteSurfaces["AmethystBronze"], "Ancient mineral conduit");
        }

        private void AddUpperRewriteMesh(UpperRewritePair pair, MeshBuilder source, Material material, string name)
        {
            if (source.VertexCount == 0) return;
            Mesh mesh = source.ToMesh(name + " / " + pair.Cell.Id); OwnedMeshes.Add(mesh);
            GameObject visual = Shapes.MeshObject(name, mesh, new[] { material }, pair.Candidate.transform, false);
            visual.layer = Layers.IgnoreRaycast; visual.isStatic = true;
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            RendererCount++; TriangleCount += mesh.triangles.Length / 3;
        }
    }
}
