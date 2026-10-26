using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private bool _fantasyPreviewV1, _previewHeartV1, _previewLivingV1;
        private bool _proofComparisonV1, _proofLegacyV1, _requestedCompareV1;
        private int _proofFr01PlacementRefusalsV1, _proofFr01NativeRefusalsV1, _proofLivingNativeRefusalsV1, _proofFr01ActivationRefusalsV1, _proofLivingActivationRefusalsV1;
        private string _firstFr01RefusalV1 = "", _firstLivingRefusalV1 = "";
        private string _firstFr01ActivationV1 = "", _firstLivingActivationV1 = "";
        private string _firstFr01NativeV1 = "", _firstLivingNativeV1 = "";

        public bool FantasyPreviewV1 { get { return _fantasyPreviewV1; } }
        public bool ActiveV1 { get { return _last; } }
        public bool GatewayStagedV1 { get { return _gate; } }

        /// <summary>Explicit preview selection is local to this lifetime. No PlayerPrefs or save file is changed.</summary>
        public bool BeginFantasyPreviewV1()
        {
            _fantasyPreviewV1 = true; _previewHeartV1 = true; _previewLivingV1 = true;
            Physics.SyncTransforms(); Apply();
            return _last && _livingLast && _gate && _livingHouses > 0;
        }

        /// <summary>Restore the independent saved appearance choices, subject to their original safety checks.</summary>
        public void EndFantasyPreviewV1()
        {
            if (!_fantasyPreviewV1 && !_proofComparisonV1) return;
            _proofComparisonV1 = false; _fantasyPreviewV1 = false;
            _compare = _requestedCompareV1; Physics.SyncTransforms(); Apply();
        }

        /// <summary>Temporary no-save OFF or ON for a matched pair. Normal QA suspension and occupancy checks remain.</summary>
        public bool BeginProofComparisonV1(bool legacy)
        {
            _proofComparisonV1 = true; _proofLegacyV1 = legacy;
            _compare = legacy; Physics.SyncTransforms(); Apply();
            return legacy ? !_last && !_livingLast : _last && _livingLast;
        }

        public void EndProofComparisonV1()
        {
            if (!_proofComparisonV1) return;
            _proofComparisonV1 = false; _compare = _requestedCompareV1; Physics.SyncTransforms(); Apply();
        }

        /// <summary>Frame actual active architecture from the ordinary player position. Never moves the player.</summary>
        public bool TryFantasyViewTargetV1(out Vector3 target)
        {
            target = Vector3.zero; float nearest = float.MaxValue; bool found = false;
            if (_livingLast) foreach (HeartLivingFR02A piece in _livingInstalled)
            {
                if (!piece.House) continue;
                float distance = MathUtil.FlatSqrDistance(Game.PlayerPosition, piece.Position);
                if (distance >= nearest) continue;
                nearest = distance; target = piece.Position + Vector3.up * Mathf.Min(8f, piece.Size.y * .4f); found = true;
            }
            if (found) return true;
            if (_last && _gate) { target = _gatePosition + Vector3.up * 7f; return true; }
            return false;
        }

        private void RefusalV1(bool living, string stage, string reason, bool activation = false, bool placement = true)
        {
            string message = FantasyProofRulesV1.SafeText(stage + ": " + reason);
            if (living)
            {
                if (_firstLivingRefusalV1.Length == 0) _firstLivingRefusalV1 = message;
                if (activation) { _proofLivingActivationRefusalsV1++; if (_firstLivingActivationV1.Length == 0) _firstLivingActivationV1 = message; }
            }
            else
            {
                if (_firstFr01RefusalV1.Length == 0) _firstFr01RefusalV1 = message;
                if (activation) { _proofFr01ActivationRefusalsV1++; if (_firstFr01ActivationV1.Length == 0) _firstFr01ActivationV1 = message; }
                else if (placement) _proofFr01PlacementRefusalsV1++;
            }
        }

        private void NativeRefusalV1(bool living, string stage, string reason, bool activation = false)
        {
            RefusalV1(living, stage, reason, activation);
            string message = FantasyProofRulesV1.SafeText(stage + ": " + reason);
            if (living)
            { if (_firstLivingNativeV1.Length == 0) _firstLivingNativeV1 = message; if (!activation) _proofLivingNativeRefusalsV1++; }
            else
            { if (_firstFr01NativeV1.Length == 0) _firstFr01NativeV1 = message; if (!activation) _proofFr01NativeRefusalsV1++; }
        }

        /// <summary>Actual instantiated module roots and native renderer components, never source proposal counts.</summary>
        public void FillProofV1(FantasyProofSnapshotV1 proof, Camera camera)
        {
            List<GameObject> heartRoots = new List<GameObject>(_dressing);
            foreach (Pair pair in _buildings.Values) if (pair.Art != null) heartRoots.Add(pair.Art);
            proof.fr01 = MeasureLayerV1("FR01", _catalog, Selected, _last, heartRoots, camera);
            proof.fr01.gatewayStaged = _gate; proof.fr01.refusedPlacements = _proofFr01PlacementRefusalsV1;
            proof.fr01.activationRefusals = _proofFr01ActivationRefusalsV1;
            proof.fr01.nativeRefusedPlacements = _proofFr01NativeRefusalsV1; proof.fr01.firstNativeRefusal = _firstFr01NativeV1;
            if (_firstFr01RefusalV1.Length > 0) proof.fr01.firstRefusal = _firstFr01RefusalV1; proof.fr01.firstActivationRefusal = _firstFr01ActivationV1;
            proof.fr01.status = FantasyProofRulesV1.SafeText(Status);
            proof.fr02a = MeasureLayerV1("FR02A", _livingCatalog, LivingSelected, _livingLast, _livingObjects, camera);
            proof.fr02a.stagedHouses = _livingHouses; proof.fr02a.stagedCourtPieces = _livingProps;
            proof.fr02a.refusedPlacements = _livingSourceRefused + _livingNativeRefused;
            proof.fr02a.activationRefusals = _proofLivingActivationRefusalsV1;
            proof.fr02a.nativeRefusedPlacements = _proofLivingNativeRefusalsV1; proof.fr02a.firstNativeRefusal = _firstLivingNativeV1;
            if (_firstLivingRefusalV1.Length > 0) proof.fr02a.firstRefusal = _firstLivingRefusalV1; proof.fr02a.firstActivationRefusal = _firstLivingActivationV1;
            proof.fr02a.status = FantasyProofRulesV1.SafeText(LivingStatus);
        }

        private static FantasyLayerProofV1 MeasureLayerV1(string name, HeartCatalogFR01 catalog, bool selected, bool active, List<GameObject> roots, Camera camera)
        {
            FantasyLayerProofV1 layer = new FantasyLayerProofV1 { name = name, selected = selected, policyActive = active };
            int imported = 0, valid = 0;
            if (catalog != null && catalog.Modules != null)
                foreach (HeartModuleFR01 module in catalog.Modules)
                    if (module != null && module.Lods != null) foreach (GameObject prefab in module.Lods)
                    { imported++; string problem; if (ValidPreparedModelV1(prefab, out problem)) valid++; else if (layer.firstRefusal.Length == 0) layer.firstRefusal = FantasyProofRulesV1.SafeText(problem); }
            layer.preparedModels = valid;
            layer.prepared = catalog != null && (name == "FR01" ? catalog.Ready : catalog.LivingReady) && imported > 0 && valid == imported;
            Plane[] planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            HashSet<Renderer> measured = new HashSet<Renderer>();
            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                layer.instantiatedObjects++;
                if (root.activeInHierarchy) layer.activeObjects++;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !measured.Add(renderer) || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                    layer.activeRenderers++;
                    if (Game.Player != null && (renderer.bounds.center - Game.PlayerPosition).sqrMagnitude <= 100f * 100f) layer.nearPlayerRenderers++;
                    if (camera != null && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) layer.inCameraFrustumRenderers++;
                }
            }
            layer.active = active && layer.activeObjects > 0 && layer.activeRenderers > 0;
            return layer;
        }

        private static bool ValidPreparedModelV1(GameObject prefab, out string problem)
        {
            problem = "";
            if (prefab == null) { problem = "A prepared prefab reference is missing."; return false; }
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) { problem = "Prepared model has no mesh: " + prefab.name; return false; }
            foreach (MeshFilter filter in filters) if (filter.sharedMesh == null || filter.sharedMesh.vertexCount < 3)
            { problem = "Prepared mesh missing or empty: " + prefab.name; return false; }
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) { problem = "Prepared model has no renderer: " + prefab.name; return false; }
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Length == 0) { problem = "Prepared model has no material slot: " + prefab.name; return false; }
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) { problem = "Prepared material reference missing: " + prefab.name; return false; }
                    if (material.shader == null || !material.shader.isSupported) { problem = "Prepared shader missing or unsupported: " + material.name; return false; }
                    if (material.mainTexture == null) { problem = "Prepared albedo missing: " + material.name; return false; }
                    if (!material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap") == null) { problem = "Prepared normal map missing: " + material.name; return false; }
                    if (!material.HasProperty("_MetallicGlossMap") || material.GetTexture("_MetallicGlossMap") == null) { problem = "Prepared metallic map missing: " + material.name; return false; }
                }
            }
            return true;
        }
    }
}
