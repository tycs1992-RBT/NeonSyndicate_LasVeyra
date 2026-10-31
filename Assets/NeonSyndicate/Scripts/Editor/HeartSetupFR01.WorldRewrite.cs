#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    public static partial class HeartSetupFR01
    {
        private const string RewriteArt = "Assets/NeonSyndicate/Art/HeartWR01";
        private const string RewritePrepared = "Assets/NeonSyndicate/Resources/HeartPreparedWR01";

        [MenuItem("Neon Syndicate/Live World/Prepare World Rewrite", false, 3308)]
        public static void MenuRewrite() { Prepare(true); PrepareLiving(true); PrepareRewrite(true); PrepareSurfaceWR02(true); PrepareFoundryWR02A(true); PrepareCompositionWR02B(true); }

        /// <summary>Prepare local-frame civic art through the original catalog and shared surface pipeline.</summary>
        public static void PrepareRewrite(bool verbose)
        {
            if (EditorApplication.isPlaying) return;
            HeartCatalogFR01 catalog = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(RewritePrepared + "/Catalog.asset");
            try
            {
                string json = File.ReadAllText(RewriteArt + "/manifest.json");
                Manifest receipt = JsonUtility.FromJson<Manifest>(json);
                if (receipt == null || receipt.license != "CC0-1.0" || receipt.modules == null || receipt.modules.Length != 15 || receipt.textures == null || receipt.textures.Length != 15)
                    throw new InvalidDataException("World Rewrite source receipt incomplete.");
                foreach (Row row in receipt.modules)
                    if (row == null || !row.path.StartsWith(RewriteArt + "/", StringComparison.Ordinal) || row.path.Contains("..") || !File.Exists(row.path) || new FileInfo(row.path).Length != row.bytes || Hash(File.ReadAllBytes(row.path)) != row.sha256)
                        throw new InvalidDataException("World Rewrite source missing or changed. Hydrate Git LFS.");
                foreach (Row row in receipt.textures) Verify(row);
                HeartCatalogFR01 foundation = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(Prepared + "/Catalog.asset");
                if (foundation == null || !foundation.Ready) throw new InvalidDataException("Prepare original Gothic Heart shared surfaces first.");
                string stamp = PreparationStamp("WR01 local frame civic and skyline source 2", json, receipt, foundation.Fingerprint);
                if (catalog != null && catalog.Fingerprint == stamp && catalog.RewriteReady && PreparedSurfacesReady(catalog)) return;
                Ensure(RewritePrepared);
                Dictionary<string, Material> surfaces = new Dictionary<string, Material>(StringComparer.Ordinal);
                foreach (string name in new[] { "Masonry", "CarvedTrim", "SlateTimber", "MossRoot", "AmethystBronze" })
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(Prepared + "/" + name + ".mat");
                    if (material == null || material.shader == null || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidDataException("Shared surface unavailable: " + name);
                    surfaces.Add(name, material);
                }
                List<HeartModuleFR01> modules = new List<HeartModuleFR01>();
                foreach (string id in new[] { "WRArchiveCrown", "WROfficeCrown", "WRSkyline809556", "WRSkyline32874", "WRSkyline53496" })
                {
                    GameObject[] levels = new GameObject[3];
                    Vector3 center = Vector3.zero, size = Vector3.zero;
                    for (int level = 0; level < 3; level++)
                    {
                        Row sourceRow = null;
                        foreach (Row row in receipt.modules)
                            if (row.name == id && row.lod == level)
                            { if (sourceRow != null) throw new InvalidDataException("Duplicate World Rewrite LOD."); sourceRow = row; }
                        if (sourceRow == null) throw new InvalidDataException("Missing " + id + " LOD" + level);
                        ModelImporter importer = AssetImporter.GetAtPath(sourceRow.path) as ModelImporter;
                        if (importer == null) throw new InvalidDataException("Wait for FBX import: " + sourceRow.path);
                        if (importer.importAnimation || importer.animationType != ModelImporterAnimationType.None || !importer.isReadable || importer.importCameras || importer.importLights || importer.addCollider || importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                        {
                            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None; importer.isReadable = true;
                            importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
                            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                            importer.materialLocation = ModelImporterMaterialLocation.InPrefab; importer.SaveAndReimport();
                        }
                        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourceRow.path);
                        if (source == null) throw new InvalidDataException("Imported World Rewrite FBX absent.");
                        foreach (Component component in source.GetComponentsInChildren<Component>(true))
                            if (component == null || !(component is Transform || component is MeshFilter || component is MeshRenderer))
                                throw new InvalidDataException("World Rewrite source contains a nonpassive component.");
                        GameObject copy = UnityEngine.Object.Instantiate(source);
                        try
                        {
                            Bounds bounds = default(Bounds); bool found = false;
                            foreach (MeshFilter filter in copy.GetComponentsInChildren<MeshFilter>(true))
                            {
                                Mesh mesh = filter.sharedMesh;
                                if (mesh == null || mesh.vertexCount < 3 || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                                    throw new InvalidDataException("World Rewrite mesh, UVs or normals missing.");
                                foreach (Vector3 vertex in mesh.vertices)
                                    if (!MathUtil.IsFinite(vertex)) throw new InvalidDataException("Nonfinite World Rewrite vertex.");
                                foreach (Vector3 corner in mesh.bounds.GetCornersFR01())
                                {
                                    Vector3 point = copy.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                                }
                            }
                            string reason = "";
                            if (!found || !HeartPlanFR01.RewriteArtBoundsWR01(id, bounds, out reason)) throw new InvalidDataException("World Rewrite local bounds rejected: " + id + ". " + (found ? reason : "No geometry."));
                            foreach (MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                Material[] slots = renderer.sharedMaterials;
                                if (slots.Length == 0) throw new InvalidDataException("World Rewrite material slots missing.");
                                for (int slot = 0; slot < slots.Length; slot++)
                                {
                                    string key = slots[slot] != null ? slots[slot].name : "";
                                    if (!surfaces.TryGetValue(key, out Material surface)) throw new InvalidDataException("Unmapped shared surface: " + key);
                                    slots[slot] = surface;
                                }
                                renderer.sharedMaterials = slots;
                            }
                            if (level == 0) { center = bounds.center; size = bounds.size; }
                            levels[level] = PrefabUtility.SaveAsPrefabAsset(copy, RewritePrepared + "/" + id + "_LOD" + level + ".prefab");
                            if (levels[level] == null) throw new InvalidDataException("World Rewrite prefab was not saved.");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(copy); }
                    }
                    modules.Add(new HeartModuleFR01 { Id = id, Lods = levels, Center = center, Size = size });
                }
                if (catalog == null) { catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>(); AssetDatabase.CreateAsset(catalog, RewritePrepared + "/Catalog.asset"); }
                catalog.Modules = modules.ToArray(); catalog.Fingerprint = PreparationStamp("WR01 local frame civic and skyline source 2", json, receipt, foundation.Fingerprint);
                catalog.Reason = "World Rewrite civic source prepared. Collision and visible dominance require native review.";
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); if (verbose) Log.Info("WORLD_REWRITE " + catalog.Reason);
            }
            catch (Exception error)
            {
                if (catalog != null) { catalog.Modules = null; catalog.Fingerprint = ""; catalog.Reason = error.Message; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
                Log.Warn("WORLD_REWRITE original civic presentation retained: " + error.Message);
            }
        }
    }
}
#endif
