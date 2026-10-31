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
        private const string FoundryArtWR02A = "Assets/NeonSyndicate/Art/FoundryWR02A";
        private const string FoundryPreparedWR02A = "Assets/NeonSyndicate/Resources/HeartPreparedWR02A";

        /// <summary>Prepare an existing Heart catalog. Creature remains a passive candidate with no runtime swap.</summary>
        public static void PrepareFoundryWR02A(bool verbose)
        {
            if (EditorApplication.isPlaying) return;
            HeartCatalogFR01 catalog = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(FoundryPreparedWR02A + "/Catalog.asset");
            try
            {
                string json = File.ReadAllText(FoundryArtWR02A + "/manifest.json");
                Manifest receipt = JsonUtility.FromJson<Manifest>(json);
                if (receipt == null || receipt.license != "CC0-1.0" || receipt.modules.Length != 6 || receipt.textures.Length != 21)
                    throw new InvalidDataException("Foundry source manifest incomplete.");
                foreach (Row row in receipt.modules) VerifyFoundryWR02A(row);
                foreach (Row row in receipt.textures) VerifyFoundryWR02A(row);
                foreach (Row row in receipt.textures)
                {
                    if (!row.path.StartsWith(FoundryArtWR02A + "/", StringComparison.Ordinal)) continue;
                    TextureImporter texture = AssetImporter.GetAtPath(row.path) as TextureImporter;
                    if (texture == null) throw new InvalidDataException("Foundry texture did not import.");
                    bool normal = row.path.Contains("_Normal"), color = row.path.Contains("_BaseColor");
                    TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (texture.textureType != type || texture.sRGBTexture != color || !texture.mipmapEnabled || texture.wrapMode != TextureWrapMode.Repeat)
                    { texture.textureType = type; texture.sRGBTexture = color; texture.mipmapEnabled = true; texture.wrapMode = TextureWrapMode.Repeat; texture.SaveAndReimport(); }
                }
                Directory.CreateDirectory(FoundryPreparedWR02A + "/Materials"); AssetDatabase.Refresh();
                HeartCatalogFR01 foundation = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(Prepared + "/Catalog.asset");
                if (foundation == null || !foundation.Ready) throw new InvalidDataException("Prepare original Gothic Heart shared surfaces first.");
                Dictionary<string, Material> surfaces = new Dictionary<string, Material>();
                foreach (string name in new[] { "Masonry", "CarvedTrim", "SlateTimber", "MossRoot", "AmethystBronze" })
                {
                    Material shared = AssetDatabase.LoadAssetAtPath<Material>(Prepared + "/" + name + ".mat");
                    if (shared == null || shared.shader == null || !shared.shader.isSupported || shared.mainTexture == null ||
                        shared.GetTexture("_BumpMap") == null || shared.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidDataException("Prepare original Heart shared surface first: " + name);
                    surfaces.Add(name, shared);
                }
                foreach (string name in new[] { "BasiliskHide", "BasiliskKeratin" })
                {
                    string path = FoundryPreparedWR02A + "/Materials/" + name + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    Shader shader = Shader.Find(RenderPath.IsURP ? "Universal Render Pipeline/Lit" : "Standard");
                    if (shader == null) throw new InvalidDataException("Supported foundry shader unavailable.");
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                    material.shader = shader;
                    Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(FoundryArtWR02A + "/" + name + "_BaseColor.png");
                    Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(FoundryArtWR02A + "/" + name + "_Normal.png");
                    Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(FoundryArtWR02A + "/" + name + "_MetallicSmoothness.png");
                    if (albedo == null || normal == null || metal == null) throw new InvalidDataException("Foundry required texture missing.");
                    material.SetTexture("_MainTex", albedo); if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
                    material.SetTexture("_BumpMap", normal); material.SetTexture("_MetallicGlossMap", metal);
                    material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICGLOSSMAP");
                    if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                    EditorUtility.SetDirty(material); surfaces.Add(name, material);
                }
                List<HeartModuleFR01> modules = new List<HeartModuleFR01>(); string creatureReason = "Creature import validation not completed.";
                foreach (string id in new[] { "WRArchiveCrown", "CrystalBasiliskCandidate" })
                {
                    try { modules.Add(PrepareFoundryModuleWR02A(id, receipt, surfaces)); if (id == "CrystalBasiliskCandidate") creatureReason = "Skinned candidate imported. Native motion acceptance NOT RUN; original creature remains."; }
                    catch (Exception error)
                    {
                        if (id == "WRArchiveCrown") throw;
                        creatureReason = "Creature candidate refused; original retained: " + FantasyProofRulesV1.SafeText(error.Message);
                        Log.Warn(creatureReason);
                    }
                }
                if (catalog == null) { catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>(); AssetDatabase.CreateAsset(catalog, FoundryPreparedWR02A + "/Catalog.asset"); }
                catalog.Modules = modules.ToArray(); catalog.Fingerprint = PreparationStamp("WR02A foundry source 1", json, receipt);
                catalog.Reason = "Archive candidate prepared through original Heart transaction. Native visibility/contact NOT RUN. " + creatureReason;
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); if (verbose) Log.Info(catalog.Reason);
            }
            catch (Exception error)
            {
                if (catalog != null) { catalog.Modules = null; catalog.Fingerprint = ""; catalog.Reason = FantasyProofRulesV1.SafeText(error.Message); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
                Log.Warn("WR02A original presentation retained: " + FantasyProofRulesV1.SafeText(error.Message));
            }
        }

        private static void VerifyFoundryWR02A(Row row)
        {
            if (row == null || row.path.Contains("..") || !(row.path.StartsWith(FoundryArtWR02A + "/", StringComparison.Ordinal) || row.path.StartsWith("Assets/NeonSyndicate/Art/HeartFR01/", StringComparison.Ordinal)) ||
                !File.Exists(row.path) || new FileInfo(row.path).Length != row.bytes || Hash(File.ReadAllBytes(row.path)) != row.sha256)
                throw new InvalidDataException("Foundry source missing or changed. Hydrate exact Git LFS payloads.");
        }

        private static HeartModuleFR01 PrepareFoundryModuleWR02A(string id, Manifest receipt, Dictionary<string, Material> surfaces)
        {
            GameObject[] lods = new GameObject[3]; Bounds expected = new Bounds(); bool candidate = id == "CrystalBasiliskCandidate";
            CreatureSkeleton skeleton = candidate ? CreatureBuilder.Build(CreatureLooks.For("crystal_basilisk", 61724), 2).Skeleton : null;
            for (int lod = 0; lod < 3; lod++)
            {
                Row row = null; foreach (Row item in receipt.modules) if (item.name == id && item.lod == lod) row = item;
                if (row == null) throw new InvalidDataException("Foundry LOD missing.");
                ModelImporter importer = AssetImporter.GetAtPath(row.path) as ModelImporter;
                if (importer == null) throw new InvalidDataException("Foundry model importer missing.");
                if (!importer.isReadable || importer.importCameras || importer.importLights || importer.importAnimation != candidate || importer.animationType != (candidate ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None) || importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                {
                    importer.isReadable = true; importer.importCameras = false; importer.importLights = false; importer.importAnimation = candidate;
                    importer.animationType = candidate ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    importer.optimizeGameObjects = false; importer.SaveAndReimport();
                }
                if (candidate)
                {
                    List<string> clips = new List<string>();
                    foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(row.path))
                    {
                        AnimationClip clip = asset as AnimationClip;
                        if (clip == null || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                        if (AnimationUtility.GetAnimationEvents(clip).Length != 0) throw new InvalidDataException("Candidate animation events are not authorized.");
                        clips.Add(clip.name);
                    }
                    foreach (string action in new[] { "Idle", "Walk", "Attack", "Impact", "Death" })
                        if (!clips.Exists(name => name.Contains("Foundry_" + action))) throw new InvalidDataException("Candidate motion clip missing: " + action);
                }
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(row.path);
                if (source == null) throw new InvalidDataException("Foundry model did not import.");
                GameObject copy = UnityEngine.Object.Instantiate(source);
                try
                {
                    foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true))
                    { if (animator.runtimeAnimatorController != null) throw new InvalidDataException("Unapproved creature animation authority."); UnityEngine.Object.DestroyImmediate(animator); }
                    foreach (Component component in copy.GetComponentsInChildren<Component>(true))
                        if (!(component is Transform || component is MeshFilter || component is MeshRenderer || (candidate && component is SkinnedMeshRenderer)))
                            throw new InvalidDataException("Nonpassive foundry component.");
                    Bounds bounds = new Bounds(); bool found = false;
                    foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] slots = renderer.sharedMaterials;
                        for (int slot = 0; slot < slots.Length; slot++)
                        {
                            string name = slots[slot] != null ? slots[slot].name : ""; int suffix = name.LastIndexOf('.'); if (suffix > 0) name = name.Substring(0, suffix);
                            if (!surfaces.ContainsKey(name)) throw new InvalidDataException("Unknown foundry material slot: " + name);
                            slots[slot] = surfaces[name];
                        }
                        renderer.sharedMaterials = slots;
                        SkinnedMeshRenderer skin = renderer as SkinnedMeshRenderer;
                        MeshFilter filter = renderer.GetComponent<MeshFilter>(); Mesh mesh = skin != null ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
                        if (mesh == null || !mesh.isReadable || mesh.vertexCount < 3 || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount) throw new InvalidDataException("Invalid foundry mesh, normal or UV.");
                        if (skin != null)
                        {
                            if (skin.bones.Length == 0 || mesh.bindposes.Length != skin.bones.Length || mesh.boneWeights.Length != mesh.vertexCount) throw new InvalidDataException("Invalid candidate skin binding.");
                            foreach (Transform bone in skin.bones) if (bone == null || !skeleton.Names.Contains(bone.name)) throw new InvalidDataException("Bone not owned by original Crystal Basilisk skeleton.");
                        }
                        foreach (Vector3 corner in mesh.bounds.GetCornersFR01())
                        { Vector3 point = copy.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner)); if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point); }
                    }
                    if (!found) throw new InvalidDataException("Foundry contains no renderable mesh.");
                    if (!candidate)
                    { string reason; if (!HeartPlanFR01.RewriteArtBoundsWR01("WRArchiveCrown", bounds, out reason)) throw new InvalidDataException(reason); }
                    else
                    {
                        HashSet<string> names = new HashSet<string>(); foreach (Transform bone in copy.GetComponentsInChildren<Transform>(true)) names.Add(bone.name);
                        foreach (string name in skeleton.Names) if (!names.Contains(name)) throw new InvalidDataException("Original skeleton bone missing: " + name);
                    }
                    if (lod == 0) expected = bounds;
                    lods[lod] = PrefabUtility.SaveAsPrefabAsset(copy, FoundryPreparedWR02A + "/" + id + "_LOD" + lod + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
            }
            return new HeartModuleFR01 { Id = id, Lods = lods, Size = expected.size, Center = expected.center };
        }
    }
}
#endif
