using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class FantasyCompositionRecordWR02B
    {
        public string stableId = "", sourceOwner = "", zone = "", family = "", state = "PLANNED", reason = "";
        public Vector3 position, size;
        public bool sourceEligible, admitted, active;
        public FantasyCompositionRecordWR02B Copy() { return (FantasyCompositionRecordWR02B)MemberwiseClone(); }
    }
    [Serializable]
    public sealed class FantasyCompositionProofWR02B
    {
        public bool selected, prepared, active;
        public int planned, sourceEligible, admitted, refused, activePlacements, activeChunks, activeRenderers, cameraCandidates, crownSources, cliffSources, nativePhysicsPending;
        public string status = "WR02-B has not executed.", nativeAcceptance = "NOT RUN", basilisk = "Disabled integration candidate. Original creatures retained.";
        public FantasyCompositionRecordWR02B[] records = Array.Empty<FantasyCompositionRecordWR02B>();
    }

    public sealed partial class HeartPresentationFR01
    {
        private sealed class CompositionChunkWR02B
        {
            public GameObject Root;
            public readonly List<FantasyCompositionRecordWR02B> Records = new List<FantasyCompositionRecordWR02B>();
            public Renderer[] Renderers;
            public MeshFilter[] Filters;
            public Mesh[] Meshes;
            public bool Failed, WasActive;
            public string Fingerprint = "";
            public Vector3 Center;
        }
        private readonly HeartCatalogFR01 _compositionCatalogWR02B = Resources.Load<HeartCatalogFR01>("HeartPreparedWR02B/Catalog");
        private readonly List<FantasyCompositionRecordWR02B> _compositionRecordsWR02B = new List<FantasyCompositionRecordWR02B>();
        private readonly List<CompositionChunkWR02B> _compositionChunksWR02B = new List<CompositionChunkWR02B>();
        private readonly Dictionary<HeartSurfaceRecordWR02, string> _compositionSurfaceVariantsWR02B = new Dictionary<HeartSurfaceRecordWR02, string>();
        private bool _compositionBuiltWR02B;
        public bool CompositionPreparedWR02B
        {
            get
            {
                if (_compositionCatalogWR02B == null || _compositionCatalogWR02B.Fingerprint.Length != 64) return false;
                foreach (string family in new[] { "SanctuaryCedar", "AzureElder", "RootFernBank", "ErodedCliff61724", "ErodedCliff62346", "ErodedCliff63279" })
                { HeartModuleFR01 module = _compositionCatalogWR02B.Find(family); if (module == null || module.Lods == null || module.Lods.Length != 3) return false; }
                return true;
            }
        }

        /// <summary>Preserve original physical trees. Only original noncolliding crowns gain authored branch/leaf presentation.</summary>
        private bool BuildCompositionSurfaceWR02B(SurfaceChunkWR02 chunk, List<SurfaceSourceWR02> sources, bool canopy)
        {
            if (!CompositionPreparedWR02B)
            {
                if (canopy) foreach (SurfaceSourceWR02 source in sources) foreach (WildScenery04OC tree in _layout.WildScenery04OC)
                    if (tree.Seed == source.Record.seed && tree.Region == 0) throw new InvalidOperationException("WR02-B catalog lost after admitting an original sanctuary crown; original source retained.");
                return false;
            }
            List<Matrix4x4> placements = new List<Matrix4x4>(); List<HeartModuleFR01> modules = new List<HeartModuleFR01>();
            foreach (SurfaceSourceWR02 source in sources)
            {
                string family = "ErodedCliff" + source.Record.seed;
                if (canopy)
                {
                    int region = -1;
                    foreach (WildScenery04OC tree in _layout.WildScenery04OC) if (tree.Kind == 0 && tree.Seed == source.Record.seed && (tree.Position - source.At).sqrMagnitude < .001f) { region = tree.Region; break; }
                    family = HeartPlanFR01.CompositionCanopyWR02B(region);
                    if (family.Length == 0) return false;
                }
                HeartModuleFR01 module = _compositionCatalogWR02B.Find(family);
                if (module == null || module.Lods == null || module.Lods.Length != 3) throw new InvalidOperationException(source.Record.stableId + ": missing WR02-B family " + family);
                Vector3 position = source.At, scale = Vector3.one;
                if (canopy && !HeartPlanFR01.CanopyFitWR02(new Bounds(module.Center, module.Size), source.Record.bounds, out position, out scale)) throw new InvalidOperationException("Original crown frame no longer fits " + source.Record.stableId);
                if (!canopy && (!HeartPlanFR01.CliffSourceWR02(source.Record.seed, source.At, source.Record.bounds) || !HeartPlanFR01.CliffCatalogBoundsWR02("Cliff" + source.Record.seed, new Bounds(module.Center, module.Size)))) throw new InvalidOperationException("Original cliff frame changed " + source.Record.stableId);
                modules.Add(module); placements.Add(Matrix4x4.TRS(position, Quaternion.identity, scale));
                _compositionSurfaceVariantsWR02B[source.Record] = family;
            }
            BuildCompositionBatchWR02B("WR02-B " + (canopy ? "authored forest crowns" : "authored geological faces"), chunk.Candidate.transform, modules, placements);
            chunk.CompositionCatalogWR02B = _compositionCatalogWR02B; chunk.CompositionFingerprintWR02B = _compositionCatalogWR02B.Fingerprint;
            return true;
        }

        private static GameObject BuildCompositionBatchWR02B(string name, Transform parent, IList<HeartModuleFR01> modules, IList<Matrix4x4> placements)
        {
            GameObject root = new GameObject(name); root.transform.SetParent(parent, false);
            try
            {
                LOD[] levels = new LOD[3];
                for (int lod = 0; lod < 3; lod++)
                {
                    Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();
                    for (int index = 0; index < modules.Count; index++)
                    {
                        GameObject prefab = modules[index].Lods[lod];
                        if (!Passive(prefab)) throw new InvalidOperationException("Nonpassive or missing WR02-B prefab " + modules[index].Id);
                        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                        {
                            Mesh mesh = filter.sharedMesh; MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                            if (mesh == null || !mesh.isReadable || renderer == null || renderer.sharedMaterials.Length != mesh.subMeshCount) throw new InvalidOperationException("WR02-B mesh/material slots unreadable.");
                            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                            {
                                Material material = renderer.sharedMaterials[sub];
                                if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null) throw new InvalidOperationException("Required WR02-B PBR binding unavailable.");
                                List<CombineInstance> list;
                                if (!batches.TryGetValue(material, out list)) { list = new List<CombineInstance>(); batches.Add(material, list); }
                                list.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = placements[index] * prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                            }
                        }
                    }
                    List<Renderer> renderers = new List<Renderer>();
                    foreach (KeyValuePair<Material, List<CombineInstance>> batch in batches)
                    {
                        Mesh mesh = new Mesh { name = name + " / " + batch.Key.name + " / LOD" + lod, indexFormat = IndexFormat.UInt32 };
                        try { mesh.CombineMeshes(batch.Value.ToArray(), true, true); mesh.RecalculateBounds(); }
                        catch { UnityEngine.Object.Destroy(mesh); throw; }
                        GameObject child = Shapes.MeshObject(mesh.name, mesh, new[] { batch.Key }, root.transform, lod == 0);
                        child.layer = Layers.IgnoreRaycast; child.isStatic = true; OwnedMeshes.Track(child, mesh); renderers.Add(child.GetComponent<Renderer>());
                    }
                    levels[lod] = new LOD(HeartPlanFR01.LodThreshold(lod, 3), renderers.ToArray());
                }
                LODGroup group = root.AddComponent<LODGroup>(); group.SetLODs(levels); group.RecalculateBounds(); return root;
            }
            catch { UnityEngine.Object.Destroy(root); throw; }
        }

        private void BuildCompositionWR02B()
        {
            if (_compositionBuiltWR02B) return;
            _compositionBuiltWR02B = true;
            List<LiveWorldDiscH8S> questDiscs = new List<LiveWorldDiscH8S>();
            if (Game.Quests != null) LiveWorldSafetyH8S.QuestDiscs(_layout, Game.Quests.Quests, (int)_layout.Revision04O, (string token, out Vector3 point) => { float yaw; string why; return CampaignPlacesRuntime04E.TryResolve(token, out point, out yaw, out why); }, questDiscs);
            List<HeartLivingFR02A> reserved = new List<HeartLivingFR02A>(_livingInstalled);
            Dictionary<string, List<HeartCompositionPlacementWR02B>> chunks = new Dictionary<string, List<HeartCompositionPlacementWR02B>>();
            Dictionary<string, int> zones = new Dictionary<string, int>();
            int totalAdmitted = 0;
            foreach (HeartCompositionPlacementWR02B plan in HeartPlanFR01.CompositionProposalsWR02B(_layout))
            {
                FantasyCompositionRecordWR02B record = new FantasyCompositionRecordWR02B { stableId = plan.stableId, zone = plan.zone, family = plan.family, sourceOwner = "CityLayout.RealmRoutes04OB / HeartPlanFR01.CompositionProposalsWR02B", position = plan.position, size = plan.size };
                _compositionRecordsWR02B.Add(record); string reason;
                if (!HeartPlanFR01.CompositionClearWR02B(_layout, plan, reserved, out reason)) { record.state = "SOURCE_REFUSED"; record.reason = reason; continue; }
                record.sourceEligible = true;
                LiveWorldRulesH8S rules = LiveWorldSafetyH8S.AreaRules(_layout, plan.position, null, Game.Inhabited != null ? Game.Inhabited.Venues : null, WorldApertures.All, questDiscs, null);
                HeartLivingFR02A footprint = new HeartLivingFR02A { Position = plan.position, Size = plan.size, Yaw = plan.yaw }; RectF rect = HeartPlanFR01.LivingFootprint(footprint);
                reason = LiveWorldSafetyH8S.Conflict(new Vector3(rect.X0, plan.position.y, rect.Z0), new Vector3(rect.X1, plan.position.y + plan.size.y, rect.Z1), plan.position.y, rules);
                if (Game.Quests == null || reason != null) { record.state = "REFUSED"; record.reason = Game.Quests == null ? "Original quest catalog is not ready; preserve unknown interaction reservations." : "Original protected owner: " + reason; continue; }
                int zoneCount; zones.TryGetValue(plan.zone, out zoneCount);
                if (zoneCount >= HeartPlanFR01.CompositionZoneLimitWR02B(plan.zone) || totalAdmitted >= HeartPlanFR01.CompositionSoftLimitWR02B) { record.state = "DEFERRED_ZONE_BUDGET"; record.reason = "Bounded first-region soft foliage budget."; continue; }
                if (!CompositionPreparedWR02B) { record.state = "REFUSED"; record.reason = "WR02-B prepared catalog unavailable. Original world retained."; continue; }
                if (!CompositionGroundWR02B(plan, out reason)) { record.state = "REFUSED"; record.reason = reason; continue; }
                record.position = plan.position;
                // Grounding can change height. Recheck the settled volume against the same quest and opening guards.
                reason = LiveWorldSafetyH8S.Conflict(new Vector3(rect.X0, plan.position.y, rect.Z0),
                    new Vector3(rect.X1, plan.position.y + plan.size.y, rect.Z1), plan.position.y, rules);
                if (reason != null) { record.state = "REFUSED"; record.reason = "Settled position conflicts with original protected owner: " + reason; continue; }
                record.admitted = true; record.state = "ADMITTED_INACTIVE"; record.reason = "Soft noncolliding understory on verified original ground; no physical terrain change.";
                zones[plan.zone] = zoneCount + 1; totalAdmitted++; reserved.Add(new HeartLivingFR02A { Position = plan.position, Size = plan.size, Yaw = plan.yaw });
                string key = SurfaceChunkKeyWR02(plan.position); List<HeartCompositionPlacementWR02B> list;
                if (!chunks.TryGetValue(key, out list)) { list = new List<HeartCompositionPlacementWR02B>(); chunks.Add(key, list); }
                list.Add(plan);
            }
            foreach (KeyValuePair<string, List<HeartCompositionPlacementWR02B>> group in chunks)
            {
                CompositionChunkWR02B chunk = new CompositionChunkWR02B { Fingerprint = _compositionCatalogWR02B.Fingerprint };
                try
                {
                    List<HeartModuleFR01> modules = new List<HeartModuleFR01>(); List<Matrix4x4> placements = new List<Matrix4x4>();
                    foreach (HeartCompositionPlacementWR02B plan in group.Value)
                    {
                        modules.Add(_compositionCatalogWR02B.Find(plan.family)); placements.Add(Matrix4x4.TRS(plan.position, Quaternion.Euler(0, plan.yaw, 0), Vector3.one)); chunk.Center += plan.position;
                        foreach (FantasyCompositionRecordWR02B record in _compositionRecordsWR02B) if (record.stableId == plan.stableId) { chunk.Records.Add(record); break; }
                    }
                    chunk.Center /= group.Value.Count;
                    GameObject staged = new GameObject("WR02-B inactive chunk " + group.Key); staged.SetActive(false); staged.transform.SetParent(_parent, false); chunk.Root = staged;
                    BuildCompositionBatchWR02B("WR02-B understory", staged.transform, modules, placements);
                    chunk.Renderers = staged.GetComponentsInChildren<Renderer>(true); chunk.Filters = staged.GetComponentsInChildren<MeshFilter>(true); chunk.Meshes = new Mesh[chunk.Filters.Length];
                    for (int i = 0; i < chunk.Filters.Length; i++) chunk.Meshes[i] = chunk.Filters[i].sharedMesh;
                }
                catch (Exception error) { chunk.Failed = true; if (chunk.Root != null) { chunk.Root.SetActive(false); UnityEngine.Object.Destroy(chunk.Root); } foreach (FantasyCompositionRecordWR02B record in chunk.Records) { record.admitted = false; record.state = "REFUSED"; record.reason = error.Message; } }
                _compositionChunksWR02B.Add(chunk);
            }
            foreach (Vector3 position in new[] { new Vector3(-151, .15f, 50), new Vector3(-235, .15f, 93), new Vector3(-264, .15f, 151) })
                _compositionRecordsWR02B.Add(new FantasyCompositionRecordWR02B { stableId = "revision:10/physical-candidate:" + position.x, family = "EscarpmentArch", sourceOwner = "Existing RealmRoutes04OB causeway / original CityBuilder physics", zone = "Woodland causeway", position = position, size = new Vector3(34, 18, 16), state = "NATIVE_COLLISION_REVIEW", reason = "New solid arch not instantiated or admitted. Candidate boxes and required portal are declarations only; native collision and camera correspondence required." });
            Log.Info("WORLD_REWRITE_WR02B " + CompositionStatusWR02B);
        }

        private static bool CompositionGroundWR02B(HeartCompositionPlacementWR02B plan, out string reason)
        {
            reason = "Original supporting ground not observed."; float lowest = float.MaxValue, highest = float.MinValue;
            Quaternion rotation = Quaternion.Euler(0, plan.yaw, 0);
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                Vector3 p = plan.position + rotation * new Vector3(x * plan.size.x * .45f, 0, z * plan.size.z * .45f); RaycastHit hit;
                if (!Physics.Raycast(p + Vector3.up * 35, Vector3.down, out hit, 60, Layers.WorldMask, QueryTriggerInteraction.Ignore) || hit.normal.y < .96f) return false;
                lowest = Mathf.Min(lowest, hit.point.y); highest = Mathf.Max(highest, hit.point.y);
            }
            if (highest - lowest > .28f) { reason = "Original ground spread would float or bury the authored bank."; return false; }
            plan.position.y = lowest;
            foreach (Collider hit in Physics.OverlapBox(plan.position + Vector3.up * .9f, new Vector3(plan.size.x * .5f, .58f, plan.size.z * .5f), rotation, ~0, QueryTriggerInteraction.Collide))
                if (hit.isTrigger || hit.gameObject.layer == Layers.World || hit.GetComponentInParent<CharacterBase>() != null || hit.GetComponentInParent<Vehicle>() != null) { reason = "Protected solid, trigger, actor or vehicle " + hit.name; return false; }
            reason = "Original ground observed."; return true;
        }

        private void ApplyCompositionWR02B()
        {
            bool policy = HeartPlanFR01.CompositionPolicyWR02B(_fantasyPreviewV1, Selected, _compare, _suspended, CompositionPreparedWR02B);
            foreach (CompositionChunkWR02B chunk in _compositionChunksWR02B)
            {
                if (chunk.Root == null || chunk.Failed)
                {
                    if (chunk.Root != null) chunk.Root.SetActive(false);
                    foreach (FantasyCompositionRecordWR02B record in chunk.Records) { record.active = false; record.admitted = false; record.state = "REFUSED"; record.reason = "Prepared chunk was lost or refused; original physical world retained."; }
                    continue;
                }
                bool active = policy && MathUtil.FlatDistance(Game.PlayerPosition, chunk.Center) < Mathf.Min(520, Settings.ViewDistance + 100);
                bool valid = _compositionCatalogWR02B != null && chunk.Fingerprint == _compositionCatalogWR02B.Fingerprint && chunk.Renderers != null && chunk.Renderers.Length > 0 && chunk.Filters != null && chunk.Meshes != null && chunk.Filters.Length == chunk.Meshes.Length && chunk.Root.GetComponentsInChildren<Collider>(true).Length == 0;
                if (valid) for (int i = 0; i < chunk.Filters.Length; i++) if (chunk.Filters[i] == null || chunk.Filters[i].sharedMesh != chunk.Meshes[i] || chunk.Meshes[i] == null || chunk.Meshes[i].vertexCount < 3) { valid = false; break; }
                if (valid) foreach (Renderer renderer in chunk.Renderers)
                {
                    if (renderer == null || !renderer.enabled || renderer.sharedMaterials.Length == 0) { valid = false; break; }
                    Transform parent = renderer.transform;
                    while (parent != chunk.Root.transform) { if (parent == null || !parent.gameObject.activeSelf) { valid = false; break; } parent = parent.parent; }
                    foreach (Material material in renderer.sharedMaterials) if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null) { valid = false; break; }
                }
                if (!valid || (chunk.WasActive && !chunk.Root.activeSelf))
                {
                    chunk.Failed = true; active = false;
                    foreach (FantasyCompositionRecordWR02B record in chunk.Records) { record.admitted = false; record.state = "REFUSED"; record.reason = "A prepared mesh, material or active root was lost. Original physical world is unchanged."; }
                }
                chunk.Root.SetActive(active); chunk.WasActive = active;
                foreach (FantasyCompositionRecordWR02B record in chunk.Records) { record.active = active && chunk.Root.activeInHierarchy; if (!chunk.Failed) record.state = record.active ? "ACTIVE" : "ADMITTED_INACTIVE"; }
            }
        }

        public string CompositionStatusWR02B
        {
            get
            {
                int admitted = 0, active = 0; foreach (FantasyCompositionRecordWR02B record in _compositionRecordsWR02B) { if (record.admitted) admitted++; if (record.active) active++; }
                return "WR02-B kingdom approach: " + _compositionRecordsWR02B.Count + " proposals, " + admitted + " admitted soft banks, " + active + " active; original trees/cliffs retained physically. Native visual/contact acceptance pending.";
            }
        }
        private void FillCompositionProofWR02B(FantasyProofSnapshotV1 snapshot, Camera camera)
        {
            FantasyCompositionProofWR02B proof = new FantasyCompositionProofWR02B { selected = _fantasyPreviewV1 && Selected, prepared = CompositionPreparedWR02B, status = CompositionStatusWR02B };
            List<FantasyCompositionRecordWR02B> records = new List<FantasyCompositionRecordWR02B>();
            foreach (FantasyCompositionRecordWR02B record in _compositionRecordsWR02B)
            {
                FantasyCompositionRecordWR02B observed = record.Copy();
                if (observed.admitted)
                {
                    CompositionChunkWR02B owner = null; foreach (CompositionChunkWR02B chunk in _compositionChunksWR02B) if (chunk.Records.Contains(record)) { owner = chunk; break; }
                    bool present = owner != null && !owner.Failed && owner.Root != null && _compositionCatalogWR02B != null && owner.Fingerprint == _compositionCatalogWR02B.Fingerprint;
                    observed.active = HeartPlanFR01.CompositionObservedWR02B(record.active, record.admitted, present, present && owner.Root.activeInHierarchy);
                    if (!present) { observed.admitted = false; observed.state = "REFUSED"; observed.reason = "Actual prepared chunk or source fingerprint lost at capture."; }
                    else if (!observed.active) observed.state = "ADMITTED_INACTIVE";
                }
                records.Add(observed); proof.planned++; if (observed.state == "NATIVE_COLLISION_REVIEW") proof.nativePhysicsPending++; if (observed.sourceEligible) proof.sourceEligible++; if (observed.admitted) proof.admitted++; if (observed.active) proof.activePlacements++; if (observed.state.Contains("REFUSED")) proof.refused++;
            }
            foreach (KeyValuePair<HeartSurfaceRecordWR02, string> pair in _compositionSurfaceVariantsWR02B)
            {
                HeartSurfaceRecordWR02 source = pair.Key;
                if (snapshot.surfaceWR02 != null) foreach (HeartSurfaceRecordWR02 observed in snapshot.surfaceWR02.records) if (observed.stableId == source.stableId) { source = observed; break; }
                records.Add(new FantasyCompositionRecordWR02B { stableId = source.stableId, sourceOwner = source.sourceOwner, family = pair.Value, position = source.bounds.center, size = source.bounds.size, sourceEligible = true, admitted = source.admitted, active = source.active, state = source.state, reason = source.reason });
                proof.planned++; proof.sourceEligible++; if (source.admitted) proof.admitted++; if (source.active) proof.activePlacements++; if (source.state == "REFUSED") proof.refused++;
                if (pair.Value.StartsWith("Eroded", StringComparison.Ordinal)) proof.cliffSources++; else proof.crownSources++;
            }
            Plane[] planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            foreach (CompositionChunkWR02B chunk in _compositionChunksWR02B) if (chunk.Root != null && chunk.Root.activeInHierarchy)
            {
                proof.activeChunks++; foreach (Renderer renderer in chunk.Renderers) if (renderer != null && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy)
                { proof.activeRenderers++; if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) proof.cameraCandidates++; }
            }
            foreach (SurfaceChunkWR02 chunk in _surfaceChunksWR02.Values) if (chunk.CompositionCatalogWR02B != null && chunk.Candidate != null && chunk.Candidate.activeInHierarchy)
            {
                proof.activeChunks++;
                foreach (Renderer renderer in chunk.Candidate.GetComponentsInChildren<Renderer>(true)) if (renderer != null && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy)
                { proof.activeRenderers++; if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) proof.cameraCandidates++; }
            }
            foreach (Creature creature in Creature.All) if (creature != null) { ImportedVisual04OD visual = creature.GetComponent<ImportedVisual04OD>(); if (visual != null && visual.BasiliskNativeReviewActiveWR02B) proof.basilisk = "Explicit Editor-only native review active on an existing creature. Production activation remains disabled; gameplay/owner acceptance is separate."; }
            proof.records = records.ToArray(); proof.active = proof.activePlacements > 0; snapshot.compositionWR02B = proof;
        }
    }
}
