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
        private const string CompositionArtWR02B = "Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B";
        private const string CompositionPreparedWR02B = "Assets/NeonSyndicate/Resources/HeartPreparedWR02B";
        [Serializable] private sealed class CompositionFrameWR02B { public string name; public float[] min, max; public bool candidateOnly; }
        [Serializable] private sealed class CompositionManifestWR02B { public CompositionFrameWR02B[] frames; }

        /// <summary>Extend the existing import lifecycle, reusing original shared PBR materials and the unchanged foundry processor.</summary>
        public static void PrepareCompositionWR02B(bool verbose)
        {
            if (EditorApplication.isPlaying) return;
            HeartCatalogFR01 catalog = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(CompositionPreparedWR02B + "/Catalog.asset");
            try
            {
                string json = File.ReadAllText(CompositionArtWR02B + "/manifest.json"); Manifest receipt = JsonUtility.FromJson<Manifest>(json);
                CompositionManifestWR02B definition = JsonUtility.FromJson<CompositionManifestWR02B>(json);
                if (receipt == null || receipt.license != "CC0-1.0" || receipt.modules == null || receipt.modules.Length != 21 || definition.frames == null || definition.frames.Length != 7) throw new InvalidDataException("Seven exact original composition assets and their frames are required.");
                foreach (Row row in receipt.modules) VerifyCompositionRowWR02B(row, true);
                foreach (Row row in receipt.textures) VerifyCompositionRowWR02B(row, false);
                Ensure(CompositionPreparedWR02B);
                string stamp = PreparationStamp("WR02-B first-region composition source 1", json, receipt);
                Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.Ordinal);
                foreach (string name in new[] { "AncientBark", "CanopyLeaf", "WeatheredStrata", "MossRoot", "CarvedTrim" })
                {
                    string path = (name == "MossRoot" || name == "CarvedTrim" ? Prepared : SurfacePreparedWR02) + "/" + name + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null) throw new InvalidDataException("Prepare the existing shared surface first: " + name);
                    materials.Add(name, material);
                }
                if (catalog == null || catalog.Fingerprint != stamp || catalog.Modules == null || catalog.Modules.Length != 7 || !PreparedSurfacesReady(catalog))
                {
                    List<HeartModuleFR01> modules = new List<HeartModuleFR01>();
                    foreach (CompositionFrameWR02B frame in definition.frames)
                    {
                        if (frame.min == null || frame.max == null || frame.min.Length != 3 || frame.max.Length != 3) throw new InvalidDataException("Missing exact composition frame.");
                        GameObject[] levels = new GameObject[3]; Bounds expected = new Bounds(); expected.SetMinMax(new Vector3(frame.min[0], frame.min[1], frame.min[2]), new Vector3(frame.max[0], frame.max[1], frame.max[2]));
                        for (int lod = 0; lod < 3; lod++)
                        {
                            Row row = null; foreach (Row item in receipt.modules) if (item.name == frame.name && item.lod == lod) { if (row != null) throw new InvalidDataException("Duplicate composition LOD."); row = item; }
                            if (row == null) throw new InvalidDataException("Missing composition LOD " + frame.name);
                            ModelImporter importer = AssetImporter.GetAtPath(row.path) as ModelImporter;
                            if (importer == null) throw new InvalidDataException("Wait for model import " + row.path);
                            if (importer.importAnimation || importer.animationType != ModelImporterAnimationType.None || !importer.isReadable || importer.addCollider || importer.importLights || importer.importCameras || importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                            {
                                importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None; importer.isReadable = true; importer.addCollider = false; importer.importLights = false; importer.importCameras = false; importer.materialLocation = ModelImporterMaterialLocation.InPrefab; importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; importer.SaveAndReimport();
                            }
                            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(row.path); if (source == null) throw new InvalidDataException("Imported model unavailable.");
                            foreach (Component component in source.GetComponentsInChildren<Component>(true)) if (component == null || !(component is Transform || component is MeshFilter || component is MeshRenderer)) throw new InvalidDataException("Source composition prefab contains nonvisual authority.");
                            GameObject copy = UnityEngine.Object.Instantiate(source);
                            try
                            {
                                bool found = false; Bounds actual = new Bounds();
                                foreach (MeshFilter filter in copy.GetComponentsInChildren<MeshFilter>(true))
                                {
                                    Mesh mesh = filter.sharedMesh;
                                    if (mesh == null || !mesh.isReadable || mesh.vertexCount < 3 || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount) throw new InvalidDataException("Composition mesh/UV/normal contract failed.");
                                    foreach (Vector3 vertex in mesh.vertices)
                                    {
                                        Vector3 point = copy.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                                        if (!MathUtil.IsFinite(point)) throw new InvalidDataException("Nonfinite composition vertex.");
                                        if (!found) { actual = new Bounds(point, Vector3.zero); found = true; } else actual.Encapsulate(point);
                                    }
                                }
                                if (!found || (actual.min - expected.min).sqrMagnitude > .0037f || (actual.max - expected.max).sqrMagnitude > .0037f) throw new InvalidDataException("Authored composition bounds differ: " + frame.name);
                                if (frame.name.StartsWith("ErodedCliff", StringComparison.Ordinal) && !HeartPlanFR01.CliffCatalogBoundsWR02(frame.name.Replace("ErodedCliff", "Cliff"), actual)) throw new InvalidDataException("Original physical cliff envelope changed.");
                                foreach (MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                                {
                                    Material[] slots = renderer.sharedMaterials;
                                    for (int index = 0; index < slots.Length; index++)
                                    {
                                        string name = slots[index] != null ? slots[index].name : ""; int suffix = name.LastIndexOf('.'); if (suffix >= 0) name = name.Substring(0, suffix);
                                        Material material; if (!materials.TryGetValue(name, out material)) throw new InvalidDataException("Unknown original PBR surface " + name); slots[index] = material;
                                    }
                                    renderer.sharedMaterials = slots;
                                }
                                levels[lod] = PrefabUtility.SaveAsPrefabAsset(copy, CompositionPreparedWR02B + "/" + frame.name + "_LOD" + lod + ".prefab");
                                if (levels[lod] == null) throw new InvalidDataException("Passive composition prefab not saved.");
                            }
                            finally { UnityEngine.Object.DestroyImmediate(copy); }
                        }
                        modules.Add(new HeartModuleFR01 { Id = frame.name, Center = expected.center, Size = expected.size, Lods = levels });
                    }
                    if (catalog == null) { catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>(); AssetDatabase.CreateAsset(catalog, CompositionPreparedWR02B + "/Catalog.asset"); }
                    catalog.Modules = modules.ToArray(); catalog.Fingerprint = stamp; catalog.Reason = "WR02-B prepared. Original collision retained. New physical arch remains an inactive candidate."; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                }
                try { PrepareBasiliskReviewWR02B(); } catch (Exception error) { Log.Warn("BASILISK_WR02B candidate-only preparation refused: " + error.Message); }
                if (verbose) Log.Info("WORLD_REWRITE_WR02B " + catalog.Reason);
            }
            catch (Exception error)
            {
                if (catalog != null) { catalog.Modules = null; catalog.Fingerprint = ""; catalog.Reason = error.Message; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
                Log.Warn("WORLD_REWRITE_WR02B original presentation retained: " + error.Message);
            }
        }

        private static void VerifyCompositionRowWR02B(Row row, bool model)
        {
            if (row == null || row.path.Contains("..") || (model && !row.path.StartsWith(CompositionArtWR02B + "/", StringComparison.Ordinal)) || (!model && !row.path.StartsWith(SurfaceArtWR02 + "/", StringComparison.Ordinal) && !row.path.StartsWith(Root + "/", StringComparison.Ordinal)) || !File.Exists(row.path) || new FileInfo(row.path).Length != row.bytes || Hash(File.ReadAllBytes(row.path)) != row.sha256) throw new InvalidDataException("Composition source absent/changed; hydrate LFS and retain original assets.");
        }

        private static string BonePathWR02B(Transform root, Transform node)
        { string path = node.name; for (Transform parent = node.parent; parent != null && parent != root; parent = parent.parent) path = parent.name + "/" + path; return path; }

        private static void PrepareBasiliskReviewWR02B()
        {
            HeartCatalogFR01 foundry = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>("Assets/NeonSyndicate/Resources/HeartPreparedWR02A/Catalog.asset");
            HeartModuleFR01 module = foundry != null ? foundry.Find("CrystalBasiliskCandidate") : null;
            if (module == null || module.Lods == null || module.Lods.Length != 3) throw new InvalidDataException("Prepare the existing basilisk candidate first.");
            GameObject root = UnityEngine.Object.Instantiate(module.Lods[0]);
            try
            {
                CreatureSkeleton skeleton = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0).Skeleton;
                Dictionary<string, Transform> targets = new Dictionary<string, Transform>(StringComparer.Ordinal); List<ImportedBone04OD> mapping = new List<ImportedBone04OD>();
                for (int i = 0; i < skeleton.Count; i++)
                {
                    Transform found = null;
                    foreach (Transform node in root.GetComponentsInChildren<Transform>(true)) if (node.name == skeleton.Names[i]) { if (found != null) throw new InvalidDataException("Duplicate imported canonical bone."); found = node; }
                    if (found == null) throw new InvalidDataException("Missing imported canonical bone " + skeleton.Names[i]);
                    targets.Add(found.name, found); mapping.Add(new ImportedBone04OD { SourceBone = i, TargetPath = BonePathWR02B(root.transform, found) });
                }
                LOD[] lods = new LOD[3];
                lods[0] = new LOD(.45f, root.GetComponentsInChildren<Renderer>(true));
                for (int lod = 1; lod < 3; lod++)
                {
                    List<Renderer> renderers = new List<Renderer>();
                    foreach (SkinnedMeshRenderer source in module.Lods[lod].GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        GameObject child = new GameObject("Basilisk review LOD" + lod + "/" + source.name); child.transform.SetParent(root.transform, false);
                        Matrix4x4 frame = module.Lods[lod].transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                        child.transform.localPosition = frame.GetColumn(3); child.transform.localRotation = frame.rotation; child.transform.localScale = frame.lossyScale;
                        SkinnedMeshRenderer skin = child.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = source.sharedMesh; skin.sharedMaterials = source.sharedMaterials;
                        Transform[] bones = new Transform[source.bones.Length];
                        for (int index = 0; index < bones.Length; index++) if (source.bones[index] == null || !targets.TryGetValue(source.bones[index].name, out bones[index])) throw new InvalidDataException("LOD has an unmapped bone.");
                        skin.bones = bones; skin.rootBone = targets[skeleton.Names[skeleton.Root]]; skin.localBounds = source.localBounds; skin.quality = SkinQuality.Bone4; renderers.Add(skin);
                    }
                    lods[lod] = new LOD(lod == 1 ? .18f : .015f, renderers.ToArray());
                }
                foreach (Animator animator in root.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { skin.quality = SkinQuality.Bone4; skin.updateWhenOffscreen = true; }
                LODGroup group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CompositionPreparedWR02B + "/BasiliskReview.prefab");
                if (prefab == null) throw new InvalidDataException("Review-only basilisk prefab not saved.");
                BasiliskReviewAssetWR02B profile = AssetDatabase.LoadAssetAtPath<BasiliskReviewAssetWR02B>(CompositionPreparedWR02B + "/BasiliskReview.asset");
                if (profile == null) { profile = ScriptableObject.CreateInstance<BasiliskReviewAssetWR02B>(); AssetDatabase.CreateAsset(profile, CompositionPreparedWR02B + "/BasiliskReview.asset"); }
                profile.Model = new ImportedModel04OD { Identity = "creature:" + BasiliskSkinRulesWR02B.SpeciesId, Enabled = false, Scale = 1, Prefab = prefab, Bones = mapping.ToArray(), Sockets = new[] { new ImportedSocket04OD { Role = "head", Path = mapping[skeleton.Head].TargetPath }, new ImportedSocket04OD { Role = "chest", Path = mapping[skeleton.Chest].TargetPath }, new ImportedSocket04OD { Role = "fx", Path = mapping[skeleton.Root].TargetPath }, new ImportedSocket04OD { Role = "synergy", Path = mapping[skeleton.Root].TargetPath } } };
                List<AnimationClip> clips = new List<AnimationClip>(); foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/NeonSyndicate/Art/FoundryWR02A/CrystalBasiliskCandidate/CrystalBasiliskCandidate_LOD0.fbx")) { AnimationClip clip = asset as AnimationClip; if (clip != null && !clip.name.StartsWith("__", StringComparison.Ordinal)) clips.Add(clip); }
                profile.PreviewClips = clips.ToArray(); profile.AssetFingerprint = foundry.Fingerprint; profile.Status = "Disabled three-LOD 32-bone review candidate. No registry, creature, collider or Animator controller created. Native motion acceptance NOT RUN.";
                EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
#endif
