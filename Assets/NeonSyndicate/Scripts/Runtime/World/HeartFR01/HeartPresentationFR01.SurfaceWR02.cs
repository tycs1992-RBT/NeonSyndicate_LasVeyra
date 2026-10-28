using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class HeartSurfaceRecordWR02
    {
        public string stableId = "", sourceOwner = "", kind = "", family = "", state = "PLANNED", reason = "";
        public int chunkX, chunkZ, vertices, triangles, seed;
        public Bounds bounds;
        public bool admitted, active, legacySuppressed;
        public HeartSurfaceRecordWR02 Copy() { return (HeartSurfaceRecordWR02)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class FantasySurfaceProofWR02
    {
        public bool selected, prepared, policyActive, active;
        public int planned, admitted, refused, activeSources, legacySuppressed, chunks, activeRenderers, importedCanopyRenderers, importedCliffRenderers, frustumRenderers, canopyFrustumRenderers, causewayFrustumRenderers;
        public string status = "WR02 has not executed.", nativeAcceptance = "NOT RUN";
        public HeartSurfaceRecordWR02[] records = Array.Empty<HeartSurfaceRecordWR02>();
    }

    public sealed partial class HeartPresentationFR01
    {
        private sealed class SurfaceSourceWR02
        {
            public MeshBuilder Builder;
            public Material Material;
            public Vector3 At;
            public HeartSurfaceKindWR02 Kind;
            public bool Shadows;
            public HeartSurfaceRecordWR02 Record;
            public string Caller;
            public SurfaceChunkWR02 Chunk;
        }
        private sealed class SurfaceChunkWR02
        {
            public readonly List<SurfaceSourceWR02> Sources = new List<SurfaceSourceWR02>();
            public readonly List<MeshRenderer> Expected = new List<MeshRenderer>();
            public readonly List<MeshFilter> Filters = new List<MeshFilter>();
            public readonly List<Mesh> Meshes = new List<Mesh>();
            public GameObject Legacy, Candidate;
            public HeartCatalogFR01 CompositionCatalogWR02B;
            public string CompositionFingerprintWR02B = "";
            public bool Failed, WasActive;
            public string Reason = "";
        }
        private readonly Dictionary<string, SurfaceChunkWR02> _surfaceChunksWR02 = new Dictionary<string, SurfaceChunkWR02>();
        private readonly List<SurfaceSourceWR02> _surfaceSourcesWR02 = new List<SurfaceSourceWR02>();
        private HeartCatalogFR01 _surfaceCatalogWR02;
        private bool _surfaceSealedWR02, _surfaceBuiltWR02;
        private float _surfaceNextReviewWR02;

        private static string SurfaceChunkKeyWR02(Vector3 point)
        { return Mathf.FloorToInt(point.x / 160f) + "," + Mathf.FloorToInt(point.z / 160f); }

        /// <summary>Keep only eligible appends addressable before the original material merger. Collision is not intercepted.</summary>
        public MeshBuilder SurfaceBufferWR02(Vector3 at, Material material, float tile, string owner, HeartSurfaceKindWR02 kind, bool shadows, int seed = 0)
        {
            if (_surfaceSealedWR02 || kind == HeartSurfaceKindWR02.Preserve || material == null) return null;
            string key = SurfaceChunkKeyWR02(at);
            SurfaceChunkWR02 chunk;
            if (!_surfaceChunksWR02.TryGetValue(key, out chunk)) { chunk = new SurfaceChunkWR02(); _surfaceChunksWR02.Add(key, chunk); }
            foreach (SurfaceSourceWR02 source in chunk.Sources)
                if (source.Material == material && source.Caller == owner && source.Kind == kind && source.Builder.VertexCount < 180000)
                { source.Builder.Tile = tile; return source.Builder; }
            HeartSurfaceRecordWR02 record = new HeartSurfaceRecordWR02
            {
                stableId = HeartPlanFR01.SurfaceKeyWR02(owner, at, chunk.Sources.Count), sourceOwner = HeartPlanFR01.SurfaceOwnerWR02(owner),
                kind = kind.ToString(), chunkX = Mathf.FloorToInt(at.x / 160f), chunkZ = Mathf.FloorToInt(at.z / 160f), seed = seed
            };
            record.family = kind == HeartSurfaceKindWR02.AuthoredCliff ? "Cliff" + seed : kind == HeartSurfaceKindWR02.Canopy ? new[] { "CanopySpreading", "CanopyPyramidal", "CanopyBent" }[HeartPlanFR01.CanopyVariantWR02(seed)] : "";
            // Store the exact caller separately from the public qualified ownership label.
            SurfaceSourceWR02 added = new SurfaceSourceWR02 { At = at, Material = material, Kind = kind, Shadows = shadows, Builder = new MeshBuilder(1), Record = record, Caller = owner, Chunk = chunk };
            added.Builder.Tile = tile; chunk.Sources.Add(added); _surfaceSourcesWR02.Add(added);
            return added.Builder;
        }

        public bool CaptureNaturalSurfaceWR02(Vector3 at, MeshBuilder source, Material material, string role, int seed)
        {
            if (_surfaceSealedWR02 || source == null || source.VertexCount == 0) return false;
            HeartSurfaceKindWR02 kind = HeartPlanFR01.SurfaceKindWR02(role, false, false, material == Mats.Grass);
            if (kind == HeartSurfaceKindWR02.Preserve) return false;
            // A crown keeps its own seed/frame; trunks and terrain remain efficiently grouped by chunk/material.
            bool individual = kind == HeartSurfaceKindWR02.Canopy || kind == HeartSurfaceKindWR02.AuthoredCliff;
            string owner = individual ? role + "/seed:" + seed + "/at:" + at.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + at.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : role;
            MeshBuilder buffer = SurfaceBufferWR02(at, material, 3f, owner, kind, kind != HeartSurfaceKindWR02.Ground, individual ? seed : 0);
            if (buffer == null) return false;
            buffer.Append(source);
            return true;
        }

        /// <summary>Publish original renderers before any replacement is prepared. Failed isolated batches return to the original merger.</summary>
        public void SealSurfaceSourcesWR02(Action<Vector3, MeshBuilder, Material> fallback)
        {
            if (_surfaceSealedWR02) return;
            _surfaceSealedWR02 = true;
            foreach (KeyValuePair<string, SurfaceChunkWR02> pair in _surfaceChunksWR02)
            {
                SurfaceChunkWR02 chunk = pair.Value;
                chunk.Legacy = new GameObject("WR02 retained sources / chunk " + pair.Key); chunk.Legacy.SetActive(false); chunk.Legacy.transform.SetParent(_parent, false);
                try
                {
                    foreach (SurfaceSourceWR02 source in chunk.Sources)
                    {
                        MeshBuilder data = source.Builder;
                        if (data.VertexCount == 0) continue;
                        HeartSurfaceRecordWR02 record = source.Record;
                        record.vertices = data.VertexCount; record.triangles = data.Triangles(0).Count / 3;
                        Bounds bounds = new Bounds(data.Vertices[0], Vector3.zero);
                        foreach (Vector3 point in data.Vertices)
                        { if (!MathUtil.IsFinite(point)) throw new InvalidOperationException(record.stableId + ": nonfinite original vertex"); bounds.Encapsulate(point); }
                        record.bounds = bounds;
                        Mesh mesh = data.ToMesh(record.stableId);
                        GameObject old = Shapes.MeshObject(record.stableId, mesh, new[] { source.Material }, chunk.Legacy.transform, source.Shadows);
                        OwnedMeshes.Track(old, mesh); old.isStatic = true;
                    }
                    chunk.Legacy.SetActive(true);
                }
                catch (Exception error)
                {
                    chunk.Legacy.SetActive(false); UnityEngine.Object.Destroy(chunk.Legacy); chunk.Legacy = null;
                    RefuseSurfaceWR02(chunk, "Original isolation failed; source returned to CityBuilder merger: " + error.Message);
                    foreach (SurfaceSourceWR02 source in chunk.Sources) fallback(source.At, source.Builder, source.Material);
                }
            }
        }

        private Material SurfaceMaterialWR02(HeartSurfaceKindWR02 kind)
        {
            if (_surfaceCatalogWR02 == null || !_surfaceCatalogWR02.SurfaceReadyWR02) return null;
            int index = kind == HeartSurfaceKindWR02.Causeway ? 0 : kind == HeartSurfaceKindWR02.Ground ? 1 : kind == HeartSurfaceKindWR02.Canopy ? 2 : kind == HeartSurfaceKindWR02.Bark ? 4 : 3;
            return _surfaceCatalogWR02.SurfaceMaterialsWR02[index];
        }

        /// <summary>Reuse the current Heart lifetime. Each eligible chunk exchanges its complete source bundle atomically.</summary>
        private void BuildSurfaceWR02()
        {
            if (_surfaceBuiltWR02) return;
            _surfaceBuiltWR02 = true;
            _surfaceCatalogWR02 = Resources.Load<HeartCatalogFR01>("HeartPreparedWR02/Catalog");
            foreach (KeyValuePair<string, SurfaceChunkWR02> pair in _surfaceChunksWR02)
            {
                SurfaceChunkWR02 chunk = pair.Value;
                if (chunk.Failed) continue;
                if (_surfaceCatalogWR02 == null || !_surfaceCatalogWR02.SurfaceReadyWR02)
                { RefuseSurfaceWR02(chunk, "WR02 prepared catalog or required maps missing. Stop Play, then Prepare World Rewrite."); continue; }
                chunk.Candidate = new GameObject("WR02 living surface / chunk " + pair.Key); chunk.Candidate.SetActive(false); chunk.Candidate.transform.SetParent(_parent, false);
                try
                {
                    Dictionary<Material, MeshBuilder> geometry = new Dictionary<Material, MeshBuilder>();
                    List<SurfaceSourceWR02> crowns = new List<SurfaceSourceWR02>();
                    List<SurfaceSourceWR02> cliffs = new List<SurfaceSourceWR02>();
                    foreach (SurfaceSourceWR02 source in chunk.Sources)
                    {
                        if (source.Builder.VertexCount == 0) continue;
                        if (source.Kind == HeartSurfaceKindWR02.Markings || source.Kind == HeartSurfaceKindWR02.Wire) continue;
                        if (source.Kind == HeartSurfaceKindWR02.Canopy) { crowns.Add(source); continue; }
                        if (source.Kind == HeartSurfaceKindWR02.AuthoredCliff) { cliffs.Add(source); continue; }
                        Material material = SurfaceMaterialWR02(source.Kind);
                        if (material == null) throw new InvalidOperationException(source.Record.stableId + ": required surface missing");
                        MeshBuilder builder;
                        if (!geometry.TryGetValue(material, out builder)) { builder = new MeshBuilder(1); geometry.Add(material, builder); }
                        builder.Append(source.Builder);
                    }
                    foreach (KeyValuePair<Material, MeshBuilder> group in geometry)
                    {
                        Mesh mesh = group.Value.ToMesh("WR02 exact original surface / " + pair.Key + "/" + group.Key.name);
                        bool shadows = group.Key != SurfaceMaterialWR02(HeartSurfaceKindWR02.Ground) && group.Key != SurfaceMaterialWR02(HeartSurfaceKindWR02.Causeway);
                        GameObject go = Shapes.MeshObject(group.Key.name, mesh, new[] { group.Key }, chunk.Candidate.transform, shadows);
                        OwnedMeshes.Track(go, mesh); go.isStatic = true;
                    }
                    if (crowns.Count > 0) BuildImportedSurfaceChunkWR02(chunk, crowns, true);
                    if (cliffs.Count > 0) BuildImportedSurfaceChunkWR02(chunk, cliffs, false);
                    chunk.Expected.AddRange(chunk.Candidate.GetComponentsInChildren<MeshRenderer>(true));
                    chunk.Filters.AddRange(chunk.Candidate.GetComponentsInChildren<MeshFilter>(true));
                    foreach (MeshFilter filter in chunk.Filters) chunk.Meshes.Add(filter.sharedMesh);
                    string reason;
                    if (!ValidSurfaceChunkWR02(chunk, out reason)) throw new InvalidOperationException(reason);
                    foreach (SurfaceSourceWR02 source in chunk.Sources)
                    { source.Record.admitted = true; source.Record.state = "ADMITTED_INACTIVE"; source.Record.reason = "Exact original surfaces and physical owners retained; candidate staged inactive."; }
                }
                catch (Exception error)
                {
                    if (chunk.Candidate != null) { chunk.Candidate.SetActive(false); UnityEngine.Object.Destroy(chunk.Candidate); chunk.Candidate = null; }
                    RefuseSurfaceWR02(chunk, "Candidate preparation refused: " + error.Message);
                }
            }
            foreach (SurfaceSourceWR02 source in _surfaceSourcesWR02) source.Builder = null;
            Log.Info(SurfaceStatusWR02);
        }

        private void BuildImportedSurfaceChunkWR02(SurfaceChunkWR02 chunk, List<SurfaceSourceWR02> sources, bool canopy)
        {
            if (BuildCompositionSurfaceWR02B(chunk, sources, canopy)) return;
            GameObject root = new GameObject(canopy ? "WR02 imported canopy LOD batch" : "WR02 imported cliff LOD batch"); root.transform.SetParent(chunk.Candidate.transform, false);
            LOD[] levels = new LOD[3]; float[] thresholds = { .34f, .13f, .008f };
            for (int lod = 0; lod < 3; lod++)
            {
                List<CombineInstance> instances = new List<CombineInstance>();
                foreach (SurfaceSourceWR02 source in sources)
                {
                    HeartModuleFR01 module = _surfaceCatalogWR02.Find(source.Record.family);
                    if (module == null || module.Lods == null || module.Lods.Length != 3 || !Passive(module.Lods[lod])) throw new InvalidOperationException(source.Record.stableId + ": invalid imported canopy");
                    Vector3 at = source.At, scale = Vector3.one;
                    if (canopy && !HeartPlanFR01.CanopyFitWR02(new Bounds(module.Center, module.Size), source.Record.bounds, out at, out scale)) throw new InvalidOperationException(source.Record.stableId + ": original canopy frame is not fit-safe");
                    if (!canopy && (!HeartPlanFR01.CliffSourceWR02(source.Record.seed, source.At, source.Record.bounds) || !HeartPlanFR01.CliffCatalogBoundsWR02(module.Id, new Bounds(module.Center, module.Size)))) throw new InvalidOperationException(source.Record.stableId + ": exact cliff source frame changed");
                    GameObject prefab = module.Lods[lod]; Matrix4x4 placement = Matrix4x4.TRS(at, Quaternion.identity, scale);
                    foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (filter.sharedMesh == null || !filter.sharedMesh.isReadable || filter.sharedMesh.subMeshCount != 1) throw new InvalidOperationException(source.Record.stableId + ": unreadable or multislot crown");
                        instances.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = 0, transform = placement * prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                    }
                }
                Mesh mesh = new Mesh { name = "WR02 imported foliage LOD" + lod, indexFormat = IndexFormat.UInt32 };
                try { mesh.CombineMeshes(instances.ToArray(), true, true); mesh.RecalculateBounds(); }
                catch { UnityEngine.Object.Destroy(mesh); throw; }
                GameObject child = Shapes.MeshObject((canopy ? "Imported foliage LOD" : "Imported cliff LOD") + lod, mesh, new[] { SurfaceMaterialWR02(canopy ? HeartSurfaceKindWR02.Canopy : HeartSurfaceKindWR02.Strata) }, root.transform, lod < 2);
                OwnedMeshes.Track(child, mesh); child.isStatic = true; child.layer = Layers.IgnoreRaycast;
                levels[lod] = new LOD(thresholds[lod], new[] { child.GetComponent<Renderer>() });
            }
            LODGroup group = root.AddComponent<LODGroup>(); group.SetLODs(levels); group.RecalculateBounds();
        }

        private static bool ValidSurfaceChunkWR02(SurfaceChunkWR02 chunk, out string reason)
        {
            reason = "";
            if (chunk.CompositionFingerprintWR02B.Length > 0 && (chunk.CompositionCatalogWR02B == null || chunk.CompositionCatalogWR02B.Fingerprint != chunk.CompositionFingerprintWR02B))
            { reason = "WR02-B source catalog lost or changed after staging."; return false; }
            if (chunk.Legacy == null || chunk.Candidate == null || chunk.Expected.Count == 0 || chunk.Filters.Count != chunk.Expected.Count)
            { reason = "Required original/candidate root or renderer missing."; return false; }
            if (chunk.Candidate.GetComponentsInChildren<Collider>(true).Length != 0) { reason = "Unexpected candidate collider."; return false; }
            for (int index = 0; index < chunk.Filters.Count; index++)
            {
                MeshFilter filter = chunk.Filters[index];
                if (filter == null || filter.sharedMesh == null || index >= chunk.Meshes.Count || filter.sharedMesh != chunk.Meshes[index] || filter.sharedMesh.vertexCount < 3 || filter.sharedMesh.subMeshCount == 0 || filter.sharedMesh.GetIndexCount(0) < 3)
                { reason = "Required prepared mesh lost, replaced or emptied."; return false; }
            }
            foreach (MeshRenderer renderer in chunk.Expected)
            {
                if (renderer == null || !renderer.enabled) { reason = "Required candidate renderer lost or disabled."; return false; }
                Transform parent = renderer.transform;
                while (parent != chunk.Candidate.transform)
                {
                    if (parent == null || !parent.gameObject.activeSelf) { reason = "Required candidate child root lost or disabled."; return false; }
                    parent = parent.parent;
                }
                if (renderer.sharedMaterials.Length == 0) { reason = "Candidate material slots missing."; return false; }
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null)
                    { reason = "Required candidate shader, albedo, normal or metallic map lost."; return false; }
            }
            return true;
        }

        private static void RefuseSurfaceWR02(SurfaceChunkWR02 chunk, string reason)
        {
            chunk.Failed = true; chunk.Reason = reason;
            if (chunk.Candidate != null) chunk.Candidate.SetActive(false);
            if (chunk.Legacy != null) chunk.Legacy.SetActive(true);
            foreach (SurfaceSourceWR02 source in chunk.Sources)
            {
                source.Record.admitted = false; source.Record.active = false; source.Record.legacySuppressed = false;
                source.Record.state = "REFUSED"; source.Record.reason = reason;
                Log.Warn("WORLD_REWRITE_WR02 " + source.Record.stableId + " owner=" + source.Record.sourceOwner + ": " + reason);
            }
        }

        private void ApplySurfaceWR02()
        {
            bool active = HeartPlanFR01.SurfaceActiveWR02(_fantasyPreviewV1, Selected, _compare, _suspended, _surfaceCatalogWR02 != null && _surfaceCatalogWR02.SurfaceReadyWR02);
            foreach (SurfaceChunkWR02 chunk in _surfaceChunksWR02.Values)
            {
                if (chunk.Failed) continue;
                if (_surfaceBuiltWR02 && _fantasyPreviewV1 && Selected && !_compare && !_suspended && !active)
                { RefuseSurfaceWR02(chunk, "Prepared catalog or required material was lost during the review."); continue; }
                if (!active)
                {
                    if (chunk.Candidate != null) chunk.Candidate.SetActive(false);
                    if (chunk.Legacy != null) chunk.Legacy.SetActive(true);
                    chunk.WasActive = false;
                }
                else
                {
                    string reason;
                    if (chunk.WasActive && (chunk.Candidate == null || !chunk.Candidate.activeSelf))
                    { RefuseSurfaceWR02(chunk, "Active candidate root lost or disabled."); continue; }
                    if (!ValidSurfaceChunkWR02(chunk, out reason)) { RefuseSurfaceWR02(chunk, "Activation refused: " + reason); continue; }
                    // Activate fully validated candidate before suppressing any matching eligible original.
                    chunk.Candidate.SetActive(true);
                    if (!chunk.Candidate.activeSelf) { RefuseSurfaceWR02(chunk, "Candidate did not activate."); continue; }
                    chunk.Legacy.SetActive(false);
                    chunk.WasActive = true;
                }
                foreach (SurfaceSourceWR02 source in chunk.Sources)
                {
                    source.Record.active = active && chunk.Candidate != null && chunk.Candidate.activeInHierarchy;
                    source.Record.legacySuppressed = active && chunk.Legacy != null && !chunk.Legacy.activeSelf;
                    source.Record.state = source.Record.active ? "ACTIVE" : active ? "ARMED_STREAMED_OUT" : "ADMITTED_INACTIVE";
                }
            }
        }

        /// <summary>One bounded review in the existing visibility owner, no Update per tree or new world manager.</summary>
        public void ReviewSurfaceWR02()
        {
            if (!_surfaceBuiltWR02 || Time.unscaledTime < _surfaceNextReviewWR02) return;
            _surfaceNextReviewWR02 = Time.unscaledTime + .5f; ApplySurfaceWR02();
            ApplyCompositionWR02B();
        }

        public string SurfaceStatusWR02
        {
            get
            {
                int admitted = 0, active = 0, refused = 0;
                foreach (SurfaceSourceWR02 source in _surfaceSourcesWR02)
                { if (source.Record.admitted) admitted++; if (source.Record.active) active++; if (source.Record.state == "REFUSED") refused++; }
                return "WR02 living surface: " + _surfaceSourcesWR02.Count + " source groups, " + admitted + " admitted, " + active + " active, " + refused + " refused. Temporary fantasy session only. Native visual/contact acceptance pending.";
            }
        }

        private void FillSurfaceProofWR02(FantasyProofSnapshotV1 snapshot, Camera camera)
        {
            FantasySurfaceProofWR02 proof = new FantasySurfaceProofWR02 { selected = _fantasyPreviewV1 && Selected, prepared = _surfaceCatalogWR02 != null && _surfaceCatalogWR02.SurfaceReadyWR02, status = SurfaceStatusWR02 };
            proof.policyActive = HeartPlanFR01.SurfaceActiveWR02(_fantasyPreviewV1, Selected, _compare, _suspended, proof.prepared);
            List<HeartSurfaceRecordWR02> records = new List<HeartSurfaceRecordWR02>();
            foreach (SurfaceSourceWR02 source in _surfaceSourcesWR02)
            {
                HeartSurfaceRecordWR02 copy = source.Record.Copy(); SurfaceChunkWR02 group = source.Chunk;
                string reason; bool valid = !group.Failed && ValidSurfaceChunkWR02(group, out reason);
                copy.active = valid && copy.admitted && group.Candidate != null && group.Candidate.activeInHierarchy;
                copy.legacySuppressed = group.Legacy != null && !group.Legacy.activeSelf;
                records.Add(copy); proof.planned++;
                if (copy.admitted) proof.admitted++; if (copy.active) proof.activeSources++; if (copy.legacySuppressed) proof.legacySuppressed++; if (copy.state == "REFUSED") proof.refused++;
            }
            Plane[] planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            foreach (SurfaceChunkWR02 chunk in _surfaceChunksWR02.Values)
            {
                if (chunk.Candidate == null || !chunk.Candidate.activeInHierarchy) continue;
                proof.chunks++;
                foreach (MeshRenderer renderer in chunk.Expected)
                {
                    if (renderer == null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy) continue;
                    proof.activeRenderers++;
                    if (renderer.name.StartsWith("Imported foliage", StringComparison.Ordinal)) proof.importedCanopyRenderers++;
                    if (renderer.name.StartsWith("Imported cliff", StringComparison.Ordinal)) proof.importedCliffRenderers++;
                    if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                    {
                        proof.frustumRenderers++;
                        if (renderer.name.StartsWith("Imported foliage", StringComparison.Ordinal)) proof.canopyFrustumRenderers++;
                        if (_surfaceCatalogWR02 != null && _surfaceCatalogWR02.SurfaceMaterialsWR02 != null && _surfaceCatalogWR02.SurfaceMaterialsWR02.Length > 0 && renderer.sharedMaterial == _surfaceCatalogWR02.SurfaceMaterialsWR02[0]) proof.causewayFrustumRenderers++;
                    }
                }
            }
            proof.records = records.ToArray(); proof.active = proof.policyActive && proof.activeSources > 0 && proof.activeRenderers > 0;
            snapshot.surfaceWR02 = proof;
        }
    }
}
