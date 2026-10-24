#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, the two built-in crowd bodies (owner direction in chat 2026-10-23: swap NPCs and enemies for realistic
    /// bodies): the Court Defender and Guild Artisan studies of the CC0 human pack with the project's fitted wardrobe, already
    /// in the repository under Art/ProductionH8R/NPC. Each is prepared like a drop-in (complete weights, the H9 bind rules,
    /// every other weighted bone passive, the ten sockets created on the H9 bones) with real materials: the study's skin
    /// atlas, the shared wool and leather maps with their normal maps, the pack's eye maps, solid bronze and steel, white
    /// brows that take the person's hair colour. The skinned pieces are merged by material (LiveCastSetup.CrowdMerge) and
    /// the row serves NPCs by role and sex (LiveCrowdRules). There is no LOD group: the crowd budget hands a far person
    /// back to the procedural body, which a culled LOD group could not do.
    /// </summary>
    public static partial class LiveCastSetup
    {
        /// <summary>One delivered study.</summary>
        private sealed class CrowdStudy
        {
            public string Role = "", Slug = "", Label = "", Origin = "", Fbx = "", Albedo = "", SkinNormal = "";
            public bool Female;
            public string Prepared { get { return CrowdPreparedRoot + "/" + Role; } }
            public string PrefabPath { get { return Prepared + "/" + Role + "Crowd.prefab"; } }
        }

        [Serializable] private sealed class StudiesReceipt { public string author = "", source_url = "", license = ""; public StudyEntry[] candidates = new StudyEntry[0]; }
        [Serializable] private sealed class StudyEntry { public string role = "", export_sha256 = "", license = ""; public int bones, vertices; }
        [Serializable] private sealed class HashReceipt { public HashedFile[] files = new HashedFile[0]; }
        [Serializable] private sealed class HashedFile { public string path = "", sha256 = ""; }

        private const string CrowdRoot = "Assets/NeonSyndicate/Art/ProductionH8R/NPC/";
        private const string CrowdReceipt = CrowdRoot + "studies.json";
        private const string CrowdPreparedRoot = CrowdRoot + "Prepared";
        /// <summary>The pack's eye maps and the male skin normal live with the H9 Dante delivery, whose receipt fingerprints them.</summary>
        private const string PackReceipt = DanteReviewH9.Root + "candidate.json";
        private const string EyeAlbedo = DanteReviewH9.Root + "T_Eye_Brown.png", EyeNormal = DanteReviewH9.Root + "T_Eye_Normal.png", MaleSkinNormal = DanteReviewH9.Root + "T_Superhero_Male_Normal.png";
        private const string SharedReceipt = "Assets/NeonSyndicate/Art/ProductionH8S/candidate.json";
        private const string SharedTextureRoot = "Assets/NeonSyndicate/Art/ProductionH8S/Textures/";
        private const string ClothAlbedo = SharedTextureRoot + "Cloth_Albedo.png", ClothNormal = SharedTextureRoot + "Cloth_Normal.png";
        private const string LeatherAlbedo = SharedTextureRoot + "Leather_Albedo.png", LeatherNormal = SharedTextureRoot + "Leather_Normal.png";
        private const int CrowdBoneCount = 65;

        private static readonly CrowdStudy CourtDefenderStudy = new CrowdStudy
        {
            Role = "CourtDefender", Slug = "court-defender-h8r", Label = "Court Defender H8R (men)", Origin = "builtin:crowd-court-defender-h8r",
            Fbx = CrowdRoot + "CourtDefenderH8R.fbx", Albedo = CrowdRoot + "CourtDefender_Albedo.png", SkinNormal = MaleSkinNormal, Female = false
        };

        private static readonly CrowdStudy GuildArtisanStudy = new CrowdStudy
        {
            Role = "GuildArtisan", Slug = "guild-artisan-h8r", Label = "Guild Artisan H8R (women)", Origin = "builtin:crowd-guild-artisan-h8r",
            Fbx = CrowdRoot + "GuildArtisanH8R.fbx", Albedo = CrowdRoot + "GuildArtisan_Albedo.png", SkinNormal = "", Female = true
        };

        /// <summary>The identity of a built-in crowd study (what AddBuiltin checks against the drop-ins).</summary>
        private static string CrowdIdentity(CrowdStudy study)
        {
            return LiveCrowdRules.Prefix + study.Slug;
        }

        private static LiveCastRow BuiltinCrowd(CrowdStudy study, LiveCastRegistry existing, bool urp, bool force, out bool built)
        {
            built = false;
            LiveCastRow old = FindOrigin(existing, study.Origin);
            string stamp = CrowdStamp(study, urp);
            if (!force && old != null && !LiveCastFolderRules.NeedsRebuild(old.Stamp, stamp, old.Model.Prefab != null)) return old;
            Progress("Preparing crowd body " + study.Label + " (first time or changed art)", study.Female ? 0.9f : 0.8f);
            StudiesReceipt receipt = ReadCrowdReceipt(study);
            VerifyCrowdFiles(study);
            ConfigureCrowdImporters(study);
            DeleteFolder(study.Prepared);
            string[] armourPaths;
            GameObject prefab = BuildCrowd(study, out armourPaths);
            LiveCastRow row = CrowdRow(study, receipt, prefab, armourPaths, CrowdStamp(study, urp));
            string reason;
            if (!LiveCastRules.CanGoLive(row, out reason)) throw new InvalidDataException(reason);
            built = true;
            return row;
        }

        /// <summary>Every file a crowd body is built from (fingerprinted, and verified against the receipts that record them).</summary>
        private static List<string> CrowdFiles(CrowdStudy study)
        {
            List<string> files = new List<string> { study.Fbx, study.Albedo, EyeAlbedo, EyeNormal, ClothAlbedo, ClothNormal, LeatherAlbedo, LeatherNormal, CrowdReceipt, PackReceipt, SharedReceipt };
            if (study.SkinNormal.Length > 0) files.Insert(2, study.SkinNormal);
            return files;
        }

        private static string CrowdStamp(CrowdStudy study, bool urp)
        {
            List<string> paths = CrowdFiles(study);
            List<string> hashes = new List<string>();
            foreach (string path in paths) hashes.Add(ProjectFileHashH8S.Sha256(path));
            paths.Add("import-settings");
            hashes.Add(CrowdImportReady(study) ? "ready" : "pending");
            return LiveCastFolderRules.Stamp(Recipe, urp, paths, hashes);
        }

        /// <summary>The studies receipt: the study's exact export hash, CC0 license and 65 bones must be recorded and match the file.</summary>
        private static StudiesReceipt ReadCrowdReceipt(CrowdStudy study)
        {
            string full = Project(CrowdReceipt);
            if (!File.Exists(full)) throw new FileNotFoundException("Missing the crowd studies receipt: " + CrowdReceipt);
            StudiesReceipt receipt = JsonUtility.FromJson<StudiesReceipt>(File.ReadAllText(full));
            if (receipt == null || receipt.candidates == null || receipt.license != DanteReviewH8S.License) throw new InvalidDataException("The crowd studies receipt does not record CC0-1.0 studies.");
            StudyEntry entry = null;
            foreach (StudyEntry candidate in receipt.candidates)
                if (candidate != null && candidate.role == study.Role) { entry = candidate; break; }
            if (entry == null) throw new InvalidDataException("The crowd studies receipt does not list " + study.Role + ".");
            if (entry.license != DanteReviewH8S.License || entry.bones != CrowdBoneCount) throw new InvalidDataException("The receipt entry for " + study.Role + " is not the CC0 65-bone study.");
            // Owner report 2026-10-23: Windows checks text files out with CRLF endings; ReceiptFileHash accepts exactly that.
            if (!ImportedArt04OH.Hex(entry.export_sha256, 64) || !ReceiptFileHash.Matches(study.Fbx, entry.export_sha256))
                throw new InvalidDataException("The study export is missing, is a Git LFS pointer, or changed: " + study.Fbx + ". Pull the LFS binaries and reinspect.");
            return receipt;
        }

        /// <summary>The recorded SHA-256 of a file in a receipt with a files list, or "" when the receipt does not record it.</summary>
        private static string RecordedHash(string receiptPath, string filePath)
        {
            string full = Project(receiptPath);
            if (!File.Exists(full)) return "";
            HashReceipt receipt;
            try { receipt = JsonUtility.FromJson<HashReceipt>(File.ReadAllText(full)); }
            catch (Exception) { return ""; }
            if (receipt == null || receipt.files == null) return "";
            foreach (HashedFile file in receipt.files)
                if (file != null && file.path == filePath) return file.sha256 ?? "";
            return "";
        }

        /// <summary>The pack's eye maps and skin normal (the H9 Dante receipt) and the shared wool and leather maps (the H8S receipt) must match their records.</summary>
        private static void VerifyCrowdFiles(CrowdStudy study)
        {
            foreach (string path in new[] { EyeAlbedo, EyeNormal, MaleSkinNormal })
                if (!ReceiptFileHash.Matches(path, RecordedHash(PackReceipt, path)))
                    throw new InvalidDataException("A pack texture the crowd bodies share is missing, is a Git LFS pointer, or changed: " + path);
            foreach (string path in new[] { ClothAlbedo, ClothNormal, LeatherAlbedo, LeatherNormal })
                if (!ReceiptFileHash.Matches(path, RecordedHash(SharedReceipt, path)))
                    throw new InvalidDataException("A shared wool or leather texture is missing, is a Git LFS pointer, or changed: " + path);
            if (!File.Exists(Project(study.Albedo))) throw new FileNotFoundException("Missing the study's skin atlas: " + study.Albedo);
        }

        private static bool CrowdImportReady(CrowdStudy study)
        {
            ModelImporter model = AssetImporter.GetAtPath(study.Fbx) as ModelImporter;
            if (model == null || !HumanoidPrepH9.ModelReady(model, HumanoidPrepH9.NoPruning)) return false;
            foreach (string path in new[] { study.Albedo, EyeAlbedo, ClothAlbedo, LeatherAlbedo })
            {
                TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
                if (t == null || !HumanoidPrepH9.TextureReady(t, false)) return false;
            }
            foreach (string path in new[] { study.SkinNormal, EyeNormal, ClothNormal, LeatherNormal })
            {
                if (path.Length == 0) continue;
                TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
                if (t == null || !HumanoidPrepH9.TextureReady(t, true)) return false;
            }
            return true;
        }

        /// <summary>Complete-weight model import and the texture types each map needs (re-imports only on change).</summary>
        private static void ConfigureCrowdImporters(CrowdStudy study)
        {
            ModelImporter model = AssetImporter.GetAtPath(study.Fbx) as ModelImporter;
            if (model == null) throw new InvalidDataException("Unity has not imported " + study.Fbx + " yet. Wait for the import to finish, then use " + MenuPath + ".");
            HumanoidPrepH9.ConfigureModel(model, HumanoidPrepH9.NoPruning);
            foreach (string path in new[] { study.Albedo, EyeAlbedo, ClothAlbedo, LeatherAlbedo }) ConfigureCrowdTexture(path, false);
            foreach (string path in new[] { study.SkinNormal, EyeNormal, ClothNormal, LeatherNormal }) ConfigureCrowdTexture(path, true);
        }

        private static void ConfigureCrowdTexture(string path, bool normal)
        {
            if (string.IsNullOrEmpty(path)) return;
            TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
            if (t == null) throw new InvalidDataException("Unity has not imported " + path + " yet.");
            HumanoidPrepH9.ConfigureTexture(t, normal);
        }

        /// <summary>
        /// The prepared crowd prefab: the study's skeleton oriented like the game's rig, the ten sockets, real materials per
        /// surface, the pieces merged by material (armour kept apart), bounds and shadows set. armourPaths receives the paths
        /// of the armour renderers (hidden on plain roles).
        /// </summary>
        private static GameObject BuildCrowd(CrowdStudy study, out string[] armourPaths)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(study.Fbx);
            if (source == null) throw new InvalidDataException("Unity returned no model for " + study.Fbx + ". Read the first Console error of its import.");
            HumanoidPrepH9.CheckSourceComponents(source, study.Fbx);
            EnsureFolder(study.Prepared);
            GameObject copy = new GameObject(study.Label + " crowd body");
            try
            {
                GameObject art = new GameObject("Art");
                art.transform.SetParent(copy.transform, false);
                GameObject primary = UnityEngine.Object.Instantiate(source, art.transform, false);
                primary.name = "Source";
                foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                CheckSkeleton(copy);
                HumanoidPrepH9.OrientAny(copy);
                EnsureSockets(copy);
                Transform skeleton = HumanoidPrepH9.Named(copy, "root");
                List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>(copy.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                if (skins.Count == 0) throw new InvalidDataException("the study has no skinned mesh.");
                Dictionary<LiveCrowdSurface, Material> materials = new Dictionary<LiveCrowdSurface, Material>();
                bool[] armour = new bool[skins.Count];
                for (int s = 0; s < skins.Count; s++)
                {
                    SkinnedMeshRenderer skin = skins[s];
                    if (skin.sharedMesh == null || skin.bones.Length == 0 || skin.sharedMesh.bindposes.Length != skin.bones.Length)
                        throw new InvalidDataException("incomplete skin on " + skin.name + ".");
                    try { SkinWeightsH5.Referenced(skin.sharedMesh, skin.bones.Length); }
                    catch (ArgumentException e) { throw new InvalidDataException(skin.name + ": " + e.Message); }
                    Material[] slots = skin.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        string imported = slots[i] != null ? slots[i].name : "";
                        LiveCrowdSurface surface = LiveCrowdRules.Surface(imported);
                        if (surface == LiveCrowdSurface.Unknown) throw new InvalidDataException("unrecorded material on " + skin.name + ": \"" + imported + "\".");
                        slots[i] = CrowdMaterial(study, surface, materials);
                        armour[s] |= LiveCrowdRules.ArmourPart(skin.name, surface);
                    }
                    skin.sharedMaterials = slots;
                }
                List<SkinnedMeshRenderer> final;
                bool[] finalArmour;
                try { final = MergeCrowdSkins(art.transform, skeleton, skins, armour, study, out finalArmour); }
                catch (Exception e)
                {
                    Log.Warn("Live crowd: " + study.Label + " keeps its " + skins.Count + " separate skinned pieces (the merge refused: " + e.Message + "). It still works, it only draws more pieces.");
                    final = skins;
                    finalArmour = armour;
                }
                List<string> paths = new List<string>();
                for (int i = 0; i < final.Count; i++)
                {
                    SkinnedMeshRenderer skin = final[i];
                    skin.quality = SkinQuality.Auto;
                    skin.updateWhenOffscreen = false;
                    skin.shadowCastingMode = ShadowCastingMode.On;
                    skin.receiveShadows = true;
                    // Bounds live in the root bone's space; a 3 m box around the hips covers the whole posed body.
                    Transform space = skin.rootBone != null ? skin.rootBone : skin.transform;
                    Vector3 center = space.InverseTransformPoint(copy.transform.TransformPoint(new Vector3(0f, .95f, 0f)));
                    skin.localBounds = new Bounds(center, Vector3.one * (3f / Mathf.Max(.0001f, space.lossyScale.x)));
                    if (finalArmour[i]) paths.Add(AnimationUtility.CalculateTransformPath(skin.transform, copy.transform));
                }
                armourPaths = paths.ToArray();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, study.PrefabPath);
                if (saved == null) throw new InvalidOperationException("Unity could not save " + study.PrefabPath + ".");
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        /// <summary>
        /// One material per surface and study, on the project's lit shader for the active pipeline, saved in the Prepared
        /// folder. Emission is enabled (black) so the original hit flash and the elite trim glow show through property blocks.
        /// </summary>
        private static Material CrowdMaterial(CrowdStudy study, LiveCrowdSurface surface, Dictionary<LiveCrowdSurface, Material> made)
        {
            Material existing;
            if (made.TryGetValue(surface, out existing)) return existing;
            string name = study.Role + " " + surface;
            Material m;
            switch (surface)
            {
                case LiveCrowdSurface.Skin:
                    m = Mats.Textured(name, Texture(study.Albedo), Color.white, 0.3f, 0f, study.SkinNormal.Length > 0 ? Texture(study.SkinNormal) : null, 0.35f);
                    break;
                case LiveCrowdSurface.Wool:
                    m = Mats.Textured(name, Texture(ClothAlbedo), Color.white, 0.21f, 0f, Texture(ClothNormal), 0.65f);
                    break;
                case LiveCrowdSurface.Leather:
                    m = Mats.Textured(name, Texture(LeatherAlbedo), Color.white, 0.24f, 0f, Texture(LeatherNormal), 0.65f);
                    break;
                case LiveCrowdSurface.Bronze:
                    m = Mats.Standard(name, new Color(0.62f, 0.45f, 0.25f), 0.55f, 0.8f);
                    break;
                case LiveCrowdSurface.Steel:
                    m = Mats.Standard(name, new Color(0.16f, 0.17f, 0.19f), 0.45f, 0.85f);
                    break;
                case LiveCrowdSurface.Brows:
                    // White on purpose: the person's hair colour arrives through the live visual's property block.
                    m = Mats.Standard(name, Color.white, 0.2f, 0f);
                    break;
                case LiveCrowdSurface.Eyes:
                    m = Mats.Textured(name, Texture(EyeAlbedo), Color.white, 0.65f, 0f, Texture(EyeNormal), 0.5f);
                    break;
                default:
                    throw new InvalidDataException("Unrecorded crowd surface " + surface + ".");
            }
            Mats.SetEmission(m, Color.black, null);
            AssetDatabase.CreateAsset(m, study.Prepared + "/" + study.Role + "_" + surface + ".mat");
            made.Add(surface, m);
            return m;
        }

        private static Texture2D Texture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("Unity has not imported " + path + " as a texture.");
            return texture;
        }

        /// <summary>The crowd row: the 47 H9 mappings, every other weighted bone passive, the ten sockets, the proof with the owner direction, the roles and sex.</summary>
        private static LiveCastRow CrowdRow(CrowdStudy study, StudiesReceipt receipt, GameObject prefab, string[] armourPaths, string stamp)
        {
            ImportedBone04OD[] mapping = new ImportedBone04OD[RigLayout.BoneCount];
            HashSet<Transform> mapped = new HashSet<Transform>();
            for (int i = 0; i < mapping.Length; i++)
            {
                Transform bone = HumanoidPrepH9.Named(prefab, ImportedBindH3.CandidateBone(RigLayout.Names[i]));
                if (!mapped.Add(bone)) throw new InvalidDataException("bone " + bone.name + " is mapped twice.");
                mapping[i] = new ImportedBone04OD { SourceBone = i, TargetPath = AnimationUtility.CalculateTransformPath(bone, prefab.transform) };
            }
            List<string> passive = new List<string>();
            foreach (SkinnedMeshRenderer skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Transform[] bones = skin.bones;
                bool[] referenced = SkinWeightsH5.Referenced(skin.sharedMesh, bones.Length);
                for (int b = 0; b < bones.Length; b++)
                {
                    if (!referenced[b] || bones[b] == null || mapped.Contains(bones[b])) continue;
                    string path = AnimationUtility.CalculateTransformPath(bones[b], prefab.transform);
                    if (!passive.Contains(path)) passive.Add(path);
                }
            }
            List<ImportedSocket04OD> sockets = new List<ImportedSocket04OD>();
            foreach (string role in LiveCastFolderRules.SocketRoles)
                sockets.Add(new ImportedSocket04OD { Role = role, Path = AnimationUtility.CalculateTransformPath(HumanoidPrepH9.Named(prefab, "socket_" + role), prefab.transform) });
            ImportedArtRecord04OH proof = new ImportedArtRecord04OH
            {
                AssetId = "CROWD-H8R-" + study.Role.ToUpperInvariant(),
                Creator = "CC0 human pack study with the project's fitted wardrobe (" + receipt.author + ")",
                SourceUrl = receipt.source_url ?? "",
                LicenseId = DanteReviewH8S.License,
                RightsRecordId = LiveCrowdRules.OwnerDirectionRecord + "; h8r-npc-study-cc0-lineage",
                CommercialUse = true, PublicSourceApproved = true, OwnerApproved = true
            };
            ImportedArtProof04OH.Stamp(proof, AssetDatabase.LoadAssetAtPath<GameObject>(study.Fbx), prefab);
            ImportedModel04OD model = new ImportedModel04OD
            {
                Identity = CrowdIdentity(study), Enabled = true, Prefab = prefab, Scale = 1f, AlignBindAxesH3 = true, PreserveInfluencesH5 = true,
                Bones = mapping, PassiveBonePaths = passive.ToArray(), Sockets = sockets.ToArray(), ArtProof = proof,
                AnimationAuthority = ImportedAnimationAuthority04OH.ExistingPose
            };
            return new LiveCastRow
            {
                Model = model, CastId = "", HiddenCostume = new string[0], Label = study.Label, Origin = study.Origin, Stamp = stamp,
                CrowdRoles = LiveCrowdRules.CrowdRoles, CrowdFemale = study.Female, CrowdArmourPaths = armourPaths ?? new string[0],
                // Neither study carries a hair mesh: the live adapter fits procedural hair, beards and headwear to its skull.
                CrowdHair = true
            };
        }
    }
}
#endif
