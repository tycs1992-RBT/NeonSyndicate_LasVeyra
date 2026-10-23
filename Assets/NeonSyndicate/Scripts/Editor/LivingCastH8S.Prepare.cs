#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LivingCastH8S, preparation: receipt, shared texture and source verification, complete-weight import settings and the
    /// disabled review prefab. Its three LODs share ONE skeleton (LOD1 and LOD2 skins are rebound to the LOD0 bones by name
    /// with matching bind frames), every weighted bone is kept, imported Animators are removed and the materials use the
    /// project's existing Built-in or URP lit shader. The prefab and materials go to the uncommitted Prepared folder.
    /// </summary>
    public static partial class LivingCastH8S
    {
        /// <summary>Import limit: no pruning at import. The adapter's own complete-weight check then refuses over eight.</summary>
        private const int MaxInfluences = 255;
        private const string SharedTextures = "Assets/NeonSyndicate/Art/ProductionH8S/Textures/";
        private const string SharedReceipt = "Assets/NeonSyndicate/Art/ProductionH8S/candidate.json";

        [Serializable] private sealed class Receipt
        {
            public string assetId = "", creator = "", license = "";
            public bool commercialUse, publicSourceApproved, preserveInfluences, ownerApproved, enabled;
            public int bones;
            public LodRow[] lods;
            public FileRow[] files;
        }
        [Serializable] private sealed class LodRow { public int lod, vertices, skins; }
        [Serializable] private sealed class FileRow { public string path = "", sha256 = ""; }

        private static string ModelPath(int level)
        {
            return DanteReviewH8S.Root + "DanteRookH8S" + (level == 0 ? "" : "_LOD" + level) + ".fbx";
        }

        private static string Project(string relative)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relative);
        }

        private static void PrepareOrVerify()
        {
            try
            {
                Receipt receipt = ReadReceipt();
                VerifySharedTextures();
                VerifySource();
                ConfigureImporters();
                GameObject prepared = AssetDatabase.LoadAssetAtPath<GameObject>(DanteReviewH8S.PreparedPrefab);
                bool built = prepared == null;
                if (built) prepared = Prepare();
                string summary;
                ImportedModel04OD row = ReviewRow(prepared, receipt, out summary);
                string reason;
                if (!DanteReviewH8S.CanReview(row, true, out reason)) throw new InvalidDataException(reason);
                Selection.activeObject = prepared;
                Log.Info("H8S Dante review copy " + (built ? "prepared" : "verified (an existing copy was not overwritten)") + ", DISABLED and NOT APPROVED. " + summary
                    + " Next: enter Play mode in the existing world, meet Dante Rook at his Forge (story mission 4 or Free Roam), stand near him and choose \"" + ReviewMenu + "\" again.");
            }
            catch (Exception e)
            {
                Log.Warn("H8S Dante review preparation refused safely, nothing is enabled: " + e.Message);
            }
        }

        private static Receipt ReadReceipt()
        {
            string path = DanteReviewH8S.Root + "candidate.json";
            if (!File.Exists(Project(path))) throw new FileNotFoundException("Missing the Dante candidate receipt: " + path);
            Receipt r = JsonUtility.FromJson<Receipt>(File.ReadAllText(Project(path)));
            if (r == null || r.assetId != DanteReviewH8S.AssetId || r.license != DanteReviewH8S.License || !r.commercialUse || !r.publicSourceApproved
                || !r.preserveInfluences || r.ownerApproved || r.enabled || r.bones != DanteReviewH8S.SourceBoneCount || r.lods == null || r.lods.Length != 3
                || r.files == null || r.files.Length != 7 || string.IsNullOrWhiteSpace(r.creator))
                throw new InvalidDataException("The Dante receipt does not record the exact disabled, unapproved CC0 candidate with three LODs and seven files.");
            foreach (LodRow lod in r.lods)
                if (lod == null || lod.skins != DanteReviewH8S.SkinsPerLod) throw new InvalidDataException("The Dante receipt must record nine skins in every LOD.");
            bool export = false;
            foreach (FileRow e in r.files)
            {
                if (e == null || !CinematicAcceptance04OF.RelativeBonePath(e.path) || !e.path.StartsWith(DanteReviewH8S.Root, StringComparison.Ordinal) || !ImportedArt04OH.Hex(e.sha256, 64)
                    || !string.Equals(ImportedArtProof04OH.FileHash(e.path), e.sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A Dante candidate file is missing, is a Git LFS pointer, or changed. Pull the LFS binaries and reinspect: " + (e != null ? e.path : "missing entry"));
                if (e.path == DanteReviewH8S.ExportPath) export = true;
            }
            if (!export) throw new InvalidDataException("The Dante receipt does not fingerprint " + DanteReviewH8S.ExportPath + ".");
            return r;
        }

        /// <summary>The wool and leather maps the source materials use belong to the H8S hero receipt; verify them there.</summary>
        private static void VerifySharedTextures()
        {
            if (!File.Exists(Project(SharedReceipt))) throw new FileNotFoundException("Missing the shared H8S texture receipt: " + SharedReceipt);
            Receipt shared = JsonUtility.FromJson<Receipt>(File.ReadAllText(Project(SharedReceipt)));
            if (shared == null || shared.license != DanteReviewH8S.License || !shared.commercialUse || !shared.publicSourceApproved || shared.files == null)
                throw new InvalidDataException("The shared H8S texture receipt does not record CC0-1.0 commercial and public-source rights.");
            foreach (string name in new[] { "Cloth_Albedo.png", "Cloth_Normal.png", "Leather_Albedo.png", "Leather_Normal.png" })
            {
                string path = SharedTextures + name;
                FileRow row = null;
                foreach (FileRow candidate in shared.files) if (candidate != null && candidate.path == path) { row = candidate; break; }
                if (row == null || !ImportedArt04OH.Hex(row.sha256, 64) || !string.Equals(ImportedArtProof04OH.FileHash(path), row.sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A shared wool or leather texture is missing, is a Git LFS pointer, or changed: " + path);
            }
        }

        private static void VerifySource()
        {
            string hash = SourceHash(DanteReviewH8S.SourceBlend);
            if (!string.Equals(hash, DanteReviewH8S.SourceSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The editable source " + DanteReviewH8S.SourceBlend + " is missing, is a Git LFS pointer, or changed (expected SHA-256 " + DanteReviewH8S.SourceSha256 + ").");
        }

        /// <summary>SHA-256 of a project file outside Assets (the original FileHash only reads Assets paths).</summary>
        private static string SourceHash(string relative)
        {
            if (!CinematicAcceptance04OF.RelativeBonePath(relative) || !relative.StartsWith("ArtSource/", StringComparison.Ordinal)) return "";
            string full = Project(relative);
            if (!File.Exists(full)) return "";
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(full))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static bool ModelReady(ModelImporter m)
        {
            return m.isReadable && !m.importAnimation && !m.optimizeGameObjects && m.preserveHierarchy && m.skinWeights == ModelImporterSkinWeights.Custom
                && m.maxBonesPerVertex == MaxInfluences && m.minBoneWeight == 0f && m.importTangents == ModelImporterTangents.CalculateMikk
                && m.materialImportMode == ModelImporterMaterialImportMode.ImportViaMaterialDescription && m.materialLocation == ModelImporterMaterialLocation.InPrefab;
        }

        private static readonly string[] AlbedoMaps = { DanteReviewH8S.Root + "DanteAlbedo.png", DanteReviewH8S.Root + "T_Eye_Brown.png", SharedTextures + "Cloth_Albedo.png", SharedTextures + "Leather_Albedo.png" };
        private static readonly string[] NormalMaps = { DanteReviewH8S.Root + "T_Superhero_Male_Normal.png", DanteReviewH8S.Root + "T_Eye_Normal.png", SharedTextures + "Cloth_Normal.png", SharedTextures + "Leather_Normal.png" };

        private static bool TextureReady(TextureImporter t, bool normal)
        {
            return t.textureType == (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) && t.sRGBTexture == !normal;
        }

        /// <summary>Complete weights (no import pruning), readable meshes, no imported clips, plain hierarchy; base colour maps
        /// sRGB and normal maps as normal maps. An importer is only re-imported when a value actually changes.</summary>
        private static void ConfigureImporters()
        {
            for (int level = 0; level < 3; level++)
            {
                string path = ModelPath(level);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidDataException("Wait for the actual FBX import, then inspect the first Console error: " + path);
                if (ModelReady(importer)) continue;
                importer.isReadable = true;
                importer.importAnimation = false;
                importer.optimizeGameObjects = false;
                importer.preserveHierarchy = true;
                importer.skinWeights = ModelImporterSkinWeights.Custom;
                importer.maxBonesPerVertex = MaxInfluences;
                importer.minBoneWeight = 0f;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.importCameras = false;
                importer.importLights = false;
                // Embedded materials keep the FBX material names that DanteReviewH8S.MaterialRole reads; the copy replaces them.
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
            }
            for (int i = 0; i < AlbedoMaps.Length + NormalMaps.Length; i++)
            {
                bool normal = i >= AlbedoMaps.Length;
                string path = normal ? NormalMaps[i - AlbedoMaps.Length] : AlbedoMaps[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("Wait for the actual texture import: " + path);
                if (TextureReady(importer, normal)) continue;
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Play mode never re-imports; a review copy needs the settings applied beforehand.</summary>
        private static void CheckImporters()
        {
            for (int level = 0; level < 3; level++)
            {
                ModelImporter importer = AssetImporter.GetAtPath(ModelPath(level)) as ModelImporter;
                if (importer == null || !ModelReady(importer)) throw new InvalidDataException("The complete-weight import settings are not applied to " + ModelPath(level) + ". Stop Play mode and choose \"" + ReviewMenu + "\" once.");
            }
            for (int i = 0; i < AlbedoMaps.Length + NormalMaps.Length; i++)
            {
                bool normal = i >= AlbedoMaps.Length;
                string path = normal ? NormalMaps[i - AlbedoMaps.Length] : AlbedoMaps[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !TextureReady(importer, normal)) throw new InvalidDataException("Texture import settings are not applied to " + path + ". Stop Play mode and choose \"" + ReviewMenu + "\" once.");
            }
        }

        private static void CheckSourceComponents(GameObject source, string path)
        {
            foreach (Component component in source.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !ImportedArt04OH.VisualComponent(component.GetType())) throw new InvalidDataException("Unexpected component in " + path + " before cloning.");
                Animator animator = component as Animator;
                if (animator != null && animator.runtimeAnimatorController != null) throw new InvalidDataException("An Animator controller cannot enter the visual copy: " + path);
            }
        }

        private static GameObject Prepare()
        {
            GameObject copy = null;
            try
            {
                GameObject[] sources = new GameObject[3];
                for (int level = 0; level < 3; level++)
                {
                    sources[level] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(level));
                    if (sources[level] == null) throw new InvalidDataException("The native FBX import did not return a model: " + ModelPath(level));
                    CheckSourceComponents(sources[level], ModelPath(level));
                }
                if (!AssetDatabase.IsValidFolder(DanteReviewH8S.PreparedFolder)) AssetDatabase.CreateFolder(DanteReviewH8S.Root.TrimEnd('/'), "Prepared");
                copy = new GameObject("Dante Rook H8S candidate, appearance NOT APPROVED");
                GameObject art = new GameObject("Art");
                art.transform.SetParent(copy.transform, false);
                // A plain clone, not a model prefab instance, so the LOD skins may move under Art.
                GameObject primary = UnityEngine.Object.Instantiate(sources[0], art.transform, false);
                primary.name = "LOD0 source";
                Dictionary<string, Transform> nodes = UniqueNodes(primary.transform);
                for (int level = 1; level < 3; level++) AttachLod(art.transform, primary.transform, nodes, sources[level], level);
                // The original adapter never runs a second animation owner; the copy carries no Animator at all.
                foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                Orient(copy);
                List<Renderer>[] lods = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
                Material[] surfaces = new Material[DanteReviewH8S.MaterialRoles.Count];
                foreach (SkinnedMeshRenderer skin in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.sharedMesh == null || skin.bones.Length == 0 || skin.sharedMesh.bindposes.Length != skin.bones.Length)
                        throw new InvalidDataException("Incomplete native skin: " + skin.name);
                    // Complete, normalized and at most eight influences per vertex; nothing is pruned to fit.
                    SkinWeightsH5.Referenced(skin.sharedMesh, skin.bones.Length);
                    skin.quality = SkinQuality.Auto;
                    Material[] slots = skin.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        string imported = slots[i] != null ? slots[i].name : "";
                        int role = DanteReviewH8S.MaterialRole(imported);
                        if (role < 0) throw new InvalidDataException("Unrecorded material on " + skin.name + ": \"" + imported + "\".");
                        if (surfaces[role] == null) surfaces[role] = Surface(role);
                        slots[i] = surfaces[role];
                    }
                    skin.sharedMaterials = slots;
                    skin.updateWhenOffscreen = false;
                    skin.shadowCastingMode = ShadowCastingMode.On;
                    skin.receiveShadows = true;
                    // Bounds live in the root bone's space; a 3 m box around the hips covers the whole posed body.
                    Transform space = skin.rootBone != null ? skin.rootBone : skin.transform;
                    Vector3 center = space.InverseTransformPoint(copy.transform.TransformPoint(new Vector3(0f, .95f, 0f)));
                    skin.localBounds = new Bounds(center, Vector3.one * (3f / Mathf.Max(.0001f, space.lossyScale.x)));
                    int lod = LodLevel(skin.name);
                    if (lod < 0) throw new InvalidDataException("Missing LOD membership: " + skin.name);
                    lods[lod].Add(skin);
                }
                for (int i = 0; i < 3; i++)
                    if (lods[i].Count != DanteReviewH8S.SkinsPerLod) throw new InvalidDataException("LOD" + i + " has " + lods[i].Count + " skins; the receipt records " + DanteReviewH8S.SkinsPerLod + ".");
                LODGroup group = copy.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(.40f, lods[0].ToArray()), new LOD(.14f, lods[1].ToArray()), new LOD(.02f, lods[2].ToArray()) });
                group.localReferencePoint = new Vector3(0f, .95f, 0f);
                group.size = 1.9f;
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, DanteReviewH8S.PreparedPrefab);
                if (saved == null) throw new InvalidOperationException("The native prefab save failed.");
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally
            {
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static int LodLevel(string name)
        {
            for (int level = 0; level < 3; level++) if (name.StartsWith("LOD" + level + "_", StringComparison.Ordinal)) return level;
            return -1;
        }

        /// <summary>Every transform below root by name. Names must be unique, so a bone can be found without guessing.</summary>
        private static Dictionary<string, Transform> UniqueNodes(Transform root)
        {
            Dictionary<string, Transform> nodes = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node == root) continue;
                if (nodes.ContainsKey(node.name)) throw new InvalidDataException("Ambiguous LOD0 node name: " + node.name);
                nodes.Add(node.name, node);
            }
            return nodes;
        }

        /// <summary>Rebinds one lower LOD's skins to the LOD0 skeleton by bone name, refusing any missing bone or bind mismatch.</summary>
        private static void AttachLod(Transform art, Transform primaryRoot, Dictionary<string, Transform> nodes, GameObject source, int level)
        {
            GameObject extra = UnityEngine.Object.Instantiate(source, art, false);
            try
            {
                SkinnedMeshRenderer[] skins = extra.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length != DanteReviewH8S.SkinsPerLod) throw new InvalidDataException("LOD" + level + " has " + skins.Length + " skins; the receipt records " + DanteReviewH8S.SkinsPerLod + ".");
                foreach (SkinnedMeshRenderer skin in skins)
                {
                    Transform[] previous = skin.bones;
                    Transform[] remapped = new Transform[previous.Length];
                    for (int i = 0; i < previous.Length; i++)
                    {
                        Transform shared;
                        if (previous[i] == null || !nodes.TryGetValue(previous[i].name, out shared))
                            throw new InvalidDataException("LOD" + level + " bone has no LOD0 counterpart: " + (previous[i] != null ? previous[i].name : "missing") + ".");
                        if (Vector3.Distance(previous[i].position, shared.position) > .0001f || Quaternion.Angle(previous[i].rotation, shared.rotation) > .01f)
                            throw new InvalidDataException("LOD" + level + " bind frame differs from LOD0 at " + shared.name + ".");
                        remapped[i] = shared;
                    }
                    Transform root;
                    if (skin.rootBone == extra.transform) root = primaryRoot;
                    else if (skin.rootBone == null || !nodes.TryGetValue(skin.rootBone.name, out root)) throw new InvalidDataException("LOD" + level + " root bone is not shared.");
                    skin.bones = remapped;
                    skin.rootBone = root;
                    skin.transform.SetParent(art, true);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(extra);
            }
        }

        private static Transform Named(GameObject root, string name)
        {
            Transform found = null;
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == name)
                {
                    if (found != null) throw new InvalidDataException("Ambiguous model node: " + name);
                    found = node;
                }
            if (found == null) throw new InvalidDataException("Missing actual source node: " + name);
            return found;
        }

        /// <summary>The original rig faces +Z with its left hand at -X, in metres. Correct a backward export, never mirror.</summary>
        private static void Orient(GameObject root)
        {
            Transform front = Named(root, "review_forward"), head = Named(root, "Head"), left = Named(root, "hand_l"), right = Named(root, "hand_r");
            if (front.position.z < head.position.z) root.transform.Find("Art").localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (front.position.z <= head.position.z || left.position.x >= right.position.x)
                throw new InvalidDataException("Source forward or handedness does not match the original rig (forward +Z, left hand at -X). Re-export with applied axes; no mirroring was attempted.");
            float height = head.position.y - Mathf.Min(Named(root, "foot_l").position.y, Named(root, "foot_r").position.y);
            if (height < 1.1f || height > 2.5f) throw new InvalidDataException("Native source units or up axis do not match an adult candidate. Re-export; no world scaling was attempted.");
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale)) throw new InvalidDataException("Nonuniform or mirrored source transform: " + node.name);
        }

        private static Texture2D Texture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("Missing actual texture: " + path);
            return texture;
        }

        /// <summary>
        /// One prepared material per recorded role, through the project's original lit shader owner (Mats.StandardShader, URP
        /// Lit on the URP path). Solid source colours are the source's linear base colours converted for Unity's colour
        /// fields. The brows use the hair colour instead of the base character's red default. No surface emits light.
        /// </summary>
        private static Material Surface(int role)
        {
            string file = DanteReviewH8S.PreparedFolder + "/Dante_" + DanteReviewH8S.MaterialRoles[role] + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(file);
            if (existing != null) return existing;
            Color color = Color.white;
            float smooth = .3f, bump = .5f;
            string albedo = "", normal = "";
            switch (role)
            {
                case 0: albedo = DanteReviewH8S.Root + "DanteAlbedo.png"; normal = DanteReviewH8S.Root + "T_Superhero_Male_Normal.png"; smooth = .3f; bump = .35f; break;
                case 1: albedo = SharedTextures + "Cloth_Albedo.png"; normal = SharedTextures + "Cloth_Normal.png"; smooth = .21f; bump = .65f; break;
                case 2: albedo = SharedTextures + "Leather_Albedo.png"; normal = SharedTextures + "Leather_Normal.png"; smooth = .24f; bump = .65f; break;
                case 3: color = new Color(.11f, .055f, .026f).gamma; smooth = .25f; break;
                case 4: color = new Color(.012f, .009f, .007f).gamma; smooth = .25f; break;
                case 5: color = new Color(.012f, .009f, .007f).gamma; smooth = .2f; break;
                case 6: albedo = DanteReviewH8S.Root + "T_Eye_Brown.png"; normal = DanteReviewH8S.Root + "T_Eye_Normal.png"; smooth = .65f; bump = .5f; break;
                default: throw new InvalidDataException("Unrecorded material role " + role + ".");
            }
            Shader shader = Mats.StandardShader;
            if (shader == null) throw new InvalidOperationException("The original supported lit shader is unavailable.");
            Material m = new Material(shader) { name = "Dante " + DanteReviewH8S.MaterialRoles[role] };
            m.SetColor(RenderPath.IsURP ? "_BaseColor" : "_Color", color);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat(RenderPath.IsURP ? "_Smoothness" : "_Glossiness", smooth);
            if (albedo != "") m.SetTexture(RenderPath.IsURP ? "_BaseMap" : "_MainTex", Texture(albedo));
            if (normal != "")
            {
                m.SetTexture("_BumpMap", Texture(normal));
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            AssetDatabase.CreateAsset(m, file);
            return m;
        }
    }
}
#endif
