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
    /// LiveCastSetup, drop-in folders (Docs/LIVE_CHARACTERS.md). Each folder under Art/LiveCast holds one character: one
    /// exported FBX on the H9 skeleton (the MalikRig armature of MalikVeyrH9.blend, same bone and socket names) with
    /// optional _LOD1 and _LOD2 files, textures named after the material with a role suffix and an optional livecast.json.
    /// The folder name is the character (Malik for the player, otherwise the person's in-game name; a campaign cast name
    /// binds to that exact cast actor). Preparation follows the Malik H9 and Dante H9 rules: complete weights without
    /// pruning, one shared skeleton across the LODs, no Animator, orientation without mirroring, all ten sockets (created on
    /// the H9 bones when the export lacks them), materials on the project's lit shader, and the live contract.
    /// </summary>
    public static partial class LiveCastSetup
    {
        [Serializable] private sealed class DropInConfig
        {
            public string identity = "", castId = "", creator = "", license = "";
            public bool enabled = true;
            public string[] hideCostume;
            public DropInSurface[] materials = new DropInSurface[0];
            /// <summary>Crowd drop-ins (identity "crowd:..."): the roles the body serves (LiveCrowdRules.ParseRoles) and its sex.</summary>
            public string[] roles;
            public bool female;
            /// <summary>Crowd drop-ins: "own" when the export brings its own hair mesh, "procedural" to fit the live hair; empty guesses from a material named Hair.</summary>
            public string hair = "";
            /// <summary>Crowd drop-ins: "adult" (or empty), "child" or "elder" (LiveCrowdRules.ParseAge).</summary>
            public string age = "";
        }

        [Serializable] private sealed class DropInSurface
        {
            public string material = "", albedo = "", normal = "", emission = "", metallic = "", mode = "", color = "", emissionColor = "";
            public float smoothness = -1f, metalness = -1f, bump = -1f;
        }

        /// <summary>One drop-in folder as read from disk.</summary>
        private sealed class DropInSource
        {
            public string Folder = "", Name = "", Safe = "", Prepared = "", PrefabPath = "", Identity = "", CastId = "", ConfigPath = "";
            /// <summary>Crowd drop-ins: the roles the body serves (0 for a named character).</summary>
            public int CrowdRoles;
            /// <summary>Crowd drop-ins: the age class the body serves.</summary>
            public AgeClass CrowdAge;
            public readonly string[] Models = new string[3];
            public int Levels;
            public readonly List<string> Images = new List<string>();
            public readonly Dictionary<string, string> ByKeyRole = new Dictionary<string, string>(StringComparer.Ordinal);
            public readonly Dictionary<string, LiveTextureRole> RoleOf = new Dictionary<string, LiveTextureRole>(StringComparer.Ordinal);
            public DropInConfig Config = new DropInConfig();
        }

        private static LiveCastRow DropIn(string folder, LiveCastRegistry existing, bool urp, bool force, out bool built)
        {
            built = false;
            DropInSource source = ReadDropIn(folder);
            if (!source.Config.enabled) return null;
            string origin = "dropin:" + source.Name;
            LiveCastRow old = FindOrigin(existing, origin);
            string stamp = DropInStamp(source, urp);
            if (!force && old != null && !LiveCastFolderRules.NeedsRebuild(old.Stamp, stamp, PreparedPresent(old))) return old;
            Progress("Preparing drop-in " + source.Name, 0.1f);
            DeleteFolder(source.Prepared);
            ConfigureDropIn(source);
            GameObject prefab = BuildDropIn(source);
            LiveCastRow row = DropInRow(source, prefab, DropInStamp(source, urp));
            string reason;
            if (!LiveCastRules.CanGoLive(row, out reason)) throw new InvalidDataException(reason);
            built = true;
            return row;
        }

        private static DropInSource ReadDropIn(string folder)
        {
            DropInSource s = new DropInSource { Folder = folder, Name = Path.GetFileName(folder) };
            s.Safe = LiveCastFolderRules.SafeName(s.Name);
            s.Prepared = folder + "/" + LiveCastFolderRules.PreparedFolder;
            s.PrefabPath = s.Prepared + "/" + s.Safe + "Live.prefab";
            string baseName = null;
            foreach (string file in Directory.GetFiles(Project(folder)))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    string stem;
                    int level = LiveCastFolderRules.LodOfModel(name, out stem);
                    if (level < 0) throw new InvalidDataException(name + ": only one main FBX plus optional _LOD1 and _LOD2 files are supported.");
                    if (baseName != null && !string.Equals(baseName, stem, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("one character per folder, but it holds " + baseName + " and " + stem + ".");
                    if (s.Models[level] != null) throw new InvalidDataException("two files for LOD" + level + ".");
                    baseName = stem;
                    s.Models[level] = folder + "/" + name;
                }
                else if (string.Equals(name, LiveCastFolderRules.ConfigFile, StringComparison.OrdinalIgnoreCase)) s.ConfigPath = folder + "/" + name;
            }
            CollectImages(s, folder);
            if (s.Models[0] == null) throw new InvalidDataException("no exported FBX in the folder. Export the character as one .fbx file (Docs/LIVE_CHARACTERS.md).");
            if (s.Models[2] != null && s.Models[1] == null) throw new InvalidDataException("a _LOD2 file needs a _LOD1 file too.");
            s.Levels = s.Models[2] != null ? 3 : s.Models[1] != null ? 2 : 1;
            if (s.ConfigPath.Length > 0)
            {
                DropInConfig config;
                try { config = JsonUtility.FromJson<DropInConfig>(File.ReadAllText(Project(s.ConfigPath))); }
                catch (Exception e) { throw new InvalidDataException(LiveCastFolderRules.ConfigFile + " could not be read (" + e.Message + ")."); }
                if (config == null) throw new InvalidDataException(LiveCastFolderRules.ConfigFile + " is empty.");
                s.Config = config;
                if (s.Config.materials == null) s.Config.materials = new DropInSurface[0];
            }
            string identity = (s.Config.identity ?? "").Trim();
            s.Identity = identity.Length > 0 ? identity : LiveCastFolderRules.IdentityFromFolder(s.Name);
            if (!LiveCastRules.ValidIdentity(s.Identity))
                throw new InvalidDataException("\"" + s.Identity + "\" is not a character this pipeline can draw. Name the folder Malik (the player) or after a person's exact in-game name, or set \"identity\" in livecast.json (\"crowd:\" plus a short name for a body that serves ordinary people by role).");
            string castId = (s.Config.castId ?? "").Trim();
            s.CastId = castId.Length > 0 ? castId : LiveCastFolderRules.CastIdFor(s.Identity);
            if (s.CastId.Length > 0 && !CastIds04E.Has(s.CastId)) throw new InvalidDataException("unknown cast id \"" + s.CastId + "\" in livecast.json.");
            if (LiveCrowdRules.ValidIdentity(s.Identity))
            {
                if (s.CastId.Length > 0) throw new InvalidDataException("a crowd body cannot also name a cast id.");
                int roles;
                if (!LiveCrowdRules.ParseRoles(s.Config.roles, out roles)) throw new InvalidDataException("\"roles\" in livecast.json may list Civilian, Clerk, Police, Swat, Gang and Enemy (or \"all\").");
                s.CrowdRoles = roles;
                AgeClass age;
                if (!LiveCrowdRules.ParseAge(s.Config.age, out age)) throw new InvalidDataException("\"age\" in livecast.json may be adult, child or elder.");
                s.CrowdAge = age;
            }
            // Texture roles: the name suffix first, then explicit files named in livecast.json.
            foreach (DropInSurface hint in s.Config.materials)
            {
                if (hint == null) continue;
                Explicit(s, hint.albedo, LiveTextureRole.Albedo);
                Explicit(s, hint.normal, LiveTextureRole.Normal);
                Explicit(s, hint.emission, LiveTextureRole.Emission);
                Explicit(s, hint.metallic, LiveTextureRole.Metallic);
            }
            return s;
        }

        /// <summary>Every image in the folder and its subfolders (never the generated Prepared folder).</summary>
        private static void CollectImages(DropInSource s, string folder)
        {
            foreach (string file in Directory.GetFiles(Project(folder)))
            {
                string name = Path.GetFileName(file);
                if (!LiveCastFolderRules.IsImage(name)) continue;
                string asset = folder + "/" + name;
                s.Images.Add(asset);
                string key;
                LiveTextureRole role;
                if (!LiveCastFolderRules.TextureRole(name, out key, out role)) continue;
                string slot = key + "|" + (int)role;
                if (!s.ByKeyRole.ContainsKey(slot)) s.ByKeyRole.Add(slot, asset);
                s.RoleOf[asset] = role;
            }
            foreach (string sub in Directory.GetDirectories(Project(folder)))
            {
                string name = Path.GetFileName(sub);
                if (!LiveCastFolderRules.SkipFolder(name)) CollectImages(s, folder + "/" + name);
            }
        }

        private static void Explicit(DropInSource s, string fileName, LiveTextureRole role)
        {
            string asset = ImageNamed(s, fileName);
            if (asset != null) s.RoleOf[asset] = role;
        }

        /// <summary>The folder image with this file name (any case), or null. Throws when livecast.json names a missing file.</summary>
        private static string ImageNamed(DropInSource s, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            string wanted = fileName.Trim();
            foreach (string image in s.Images)
                if (string.Equals(Path.GetFileName(image), wanted, StringComparison.OrdinalIgnoreCase)) return image;
            throw new InvalidDataException(LiveCastFolderRules.ConfigFile + " names " + wanted + ", which is not in the folder.");
        }

        private static string DropInStamp(DropInSource s, bool urp)
        {
            List<string> paths = new List<string>();
            List<string> hashes = new List<string>();
            for (int level = 0; level < 3; level++)
                if (s.Models[level] != null) { paths.Add(s.Models[level]); hashes.Add(ProjectFileHashH8S.Sha256(s.Models[level])); }
            foreach (string image in s.Images) { paths.Add(image); hashes.Add(ProjectFileHashH8S.Sha256(image)); }
            if (s.ConfigPath.Length > 0) { paths.Add(s.ConfigPath); hashes.Add(ProjectFileHashH8S.Sha256(s.ConfigPath)); }
            paths.Add("import-settings");
            hashes.Add(DropInImportReady(s) ? "ready" : "pending");
            paths.Add("identity");
            hashes.Add(s.Identity + "|" + s.CastId + "|" + s.CrowdRoles + "|" + (s.Config.female ? "f" : "m") + "|" + s.CrowdAge);
            return LiveCastFolderRules.Stamp(Recipe, urp, paths, hashes);
        }

        private static bool DropInImportReady(DropInSource s)
        {
            for (int level = 0; level < s.Levels; level++)
            {
                ModelImporter model = AssetImporter.GetAtPath(s.Models[level]) as ModelImporter;
                if (model == null || !HumanoidPrepH9.ModelReady(model, HumanoidPrepH9.NoPruning)) return false;
            }
            foreach (KeyValuePair<string, LiveTextureRole> pair in s.RoleOf)
            {
                TextureImporter texture = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
                if (texture == null || !TextureRoleReady(texture, pair.Value)) return false;
            }
            return true;
        }

        private static bool TextureRoleReady(TextureImporter t, LiveTextureRole role)
        {
            if (role == LiveTextureRole.Normal) return HumanoidPrepH9.TextureReady(t, true);
            if (role == LiveTextureRole.Metallic) return t.textureType == TextureImporterType.Default && !t.sRGBTexture && t.textureShape == TextureImporterShape.Texture2D;
            return HumanoidPrepH9.TextureReady(t, false);
        }

        /// <summary>Complete-weight model import settings and the texture types each role needs (re-imports only on change).</summary>
        private static void ConfigureDropIn(DropInSource s)
        {
            for (int level = 0; level < s.Levels; level++)
            {
                ModelImporter model = AssetImporter.GetAtPath(s.Models[level]) as ModelImporter;
                if (model == null) throw new InvalidDataException("Unity has not imported " + s.Models[level] + " yet. Wait for the import to finish, then use " + MenuPath + ".");
                HumanoidPrepH9.ConfigureModel(model, HumanoidPrepH9.NoPruning);
            }
            foreach (KeyValuePair<string, LiveTextureRole> pair in s.RoleOf)
            {
                TextureImporter texture = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
                if (texture == null) throw new InvalidDataException("Unity has not imported " + pair.Key + " yet.");
                if (TextureRoleReady(texture, pair.Value)) continue;
                if (pair.Value == LiveTextureRole.Metallic)
                {
                    texture.textureType = TextureImporterType.Default;
                    texture.sRGBTexture = false;
                    texture.textureShape = TextureImporterShape.Texture2D;
                    texture.SaveAndReimport();
                }
                else HumanoidPrepH9.ConfigureTexture(texture, pair.Value == LiveTextureRole.Normal);
            }
        }

        private static GameObject BuildDropIn(DropInSource s)
        {
            GameObject[] sources = new GameObject[s.Levels];
            for (int level = 0; level < s.Levels; level++)
            {
                sources[level] = AssetDatabase.LoadAssetAtPath<GameObject>(s.Models[level]);
                if (sources[level] == null) throw new InvalidDataException("Unity returned no model for " + s.Models[level] + ". Read the first Console error of its import.");
                HumanoidPrepH9.CheckSourceComponents(sources[level], s.Models[level]);
            }
            EnsureFolder(s.Prepared);
            GameObject copy = new GameObject(s.Name + " live character");
            try
            {
                GameObject art = new GameObject("Art");
                art.transform.SetParent(copy.transform, false);
                // A plain clone, not a model prefab instance, so the LOD skins may move under Art.
                GameObject primary = UnityEngine.Object.Instantiate(sources[0], art.transform, false);
                primary.name = "LOD0 source";
                Dictionary<string, Transform> nodes = HumanoidPrepH9.UniqueNodes(primary.transform);
                List<SkinnedMeshRenderer>[] lods = new List<SkinnedMeshRenderer>[s.Levels];
                lods[0] = new List<SkinnedMeshRenderer>(primary.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                if (lods[0].Count == 0) throw new InvalidDataException("the FBX has no skinned mesh. Keep the Armature modifier on every mesh and tick Armature and Mesh in the export.");
                for (int level = 1; level < s.Levels; level++) lods[level] = HumanoidPrepH9.AttachLod(art.transform, primary.transform, nodes, sources[level], level, -1);
                // The procedural pose is the only animation owner; the copy carries no Animator at all.
                foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                CheckSkeleton(copy);
                HumanoidPrepH9.OrientAny(copy);
                EnsureSockets(copy);
                // Skin under the garments goes (owner report 2026-10-24), level by level, before the materials are replaced.
                bool crowd = s.CrowdRoles != 0;
                for (int level = 0; level < s.Levels; level++) CoverSkin(lods[level], piece => DropInCoverPart(piece, crowd), s.Prepared, s.Safe + "_LOD" + level);
                Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.Ordinal);
                Renderer[][] levels = new Renderer[s.Levels][];
                for (int level = 0; level < s.Levels; level++)
                {
                    levels[level] = new Renderer[lods[level].Count];
                    for (int i = 0; i < lods[level].Count; i++)
                    {
                        SkinnedMeshRenderer skin = lods[level][i];
                        PrepareSkin(s, copy, skin, materials);
                        if (HumanoidPrepH9.LodLevel(skin.name) != level) skin.name = "LOD" + level + "_" + skin.name;
                        levels[level][i] = skin;
                    }
                }
                float[] cut = LiveCastFolderRules.LodCutoffs(s.Levels);
                // A crowd body is never culled by its LOD group: the crowd budget hands a far person back to the procedural body instead.
                if (s.CrowdRoles != 0) cut[s.Levels - 1] = 0f;
                LOD[] groups = new LOD[s.Levels];
                for (int level = 0; level < s.Levels; level++) groups[level] = new LOD(cut[level], levels[level]);
                LODGroup group = copy.AddComponent<LODGroup>();
                group.SetLODs(groups);
                group.localReferencePoint = new Vector3(0f, .95f, 0f);
                group.size = 1.9f;
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, s.PrefabPath);
                if (saved == null) throw new InvalidOperationException("Unity could not save " + s.PrefabPath + ".");
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        /// <summary>All 47 H9 humanoid bones the original pose drives must exist (the MalikRig names).</summary>
        private static void CheckSkeleton(GameObject copy)
        {
            List<string> missing = new List<string>();
            foreach (string original in RigLayout.Names)
            {
                string bone = ImportedBindH3.CandidateBone(original);
                if (HumanoidPrepH9.FindNamed(copy, bone) == null) missing.Add(bone);
            }
            if (missing.Count > 0)
                throw new InvalidDataException("the armature lacks " + missing.Count + " H9 bones (" + string.Join(", ", missing) + "). Keep the MalikRig armature from MalikVeyrH9.blend and its bone names.");
        }

        /// <summary>Creates any missing socket_ locator on its H9 bone, at the bone origin, as the H9 exporter does.</summary>
        private static void EnsureSockets(GameObject copy)
        {
            foreach (string role in LiveCastFolderRules.SocketRoles)
            {
                if (HumanoidPrepH9.FindNamed(copy, "socket_" + role) != null) continue;
                Transform bone = HumanoidPrepH9.Named(copy, LiveCastFolderRules.SocketBone(role));
                GameObject socket = new GameObject("socket_" + role);
                socket.transform.SetParent(bone, false);
            }
        }

        private static void PrepareSkin(DropInSource s, GameObject copy, SkinnedMeshRenderer skin, Dictionary<string, Material> materials)
        {
            if (skin.sharedMesh == null || skin.bones.Length == 0 || skin.sharedMesh.bindposes.Length != skin.bones.Length)
                throw new InvalidDataException("incomplete skin on " + skin.name + ". Keep the Armature modifier and export Armature and Mesh together.");
            try { SkinWeightsH5.Referenced(skin.sharedMesh, skin.bones.Length); }
            catch (ArgumentException e)
            { throw new InvalidDataException(skin.name + ": " + e.Message + " In Blender Weight Paint mode use Weights > Limit Total (8), then Weights > Normalize All."); }
            skin.quality = SkinQuality.Auto;
            Material[] slots = skin.sharedMaterials;
            for (int i = 0; i < slots.Length; i++) slots[i] = DropInMaterial(s, slots[i] != null ? slots[i].name : "", materials);
            skin.sharedMaterials = slots;
            skin.updateWhenOffscreen = false;
            skin.shadowCastingMode = ShadowCastingMode.On;
            skin.receiveShadows = true;
            // Bounds live in the root bone's space; a 3 m box around the hips covers the whole posed body.
            Transform space = skin.rootBone != null ? skin.rootBone : skin.transform;
            Vector3 center = space.InverseTransformPoint(copy.transform.TransformPoint(new Vector3(0f, .95f, 0f)));
            skin.localBounds = new Bounds(center, Vector3.one * (3f / Mathf.Max(.0001f, space.lossyScale.x)));
        }

        /// <summary>The live row: the 47 H9 mappings, every other weighted bone as passive, the ten sockets and the owner's drop-in proof.</summary>
        private static LiveCastRow DropInRow(DropInSource s, GameObject prefab, string stamp)
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
                AssetId = "LIVECAST-" + s.Safe.ToUpperInvariant(),
                Creator = string.IsNullOrWhiteSpace(s.Config.creator) ? "Owner drop-in (" + s.Name + ")" : s.Config.creator.Trim(),
                LicenseId = string.IsNullOrWhiteSpace(s.Config.license) ? "OWNER-DECLARED" : s.Config.license.Trim(),
                RightsRecordId = "owner drop-in " + s.Folder + "; " + LiveCastRules.OwnerApprovalRecord + " for the live drop-in pipeline",
                CommercialUse = true, PublicSourceApproved = false, OwnerApproved = true
            };
            ImportedArtProof04OH.Stamp(proof, AssetDatabase.LoadAssetAtPath<GameObject>(s.Models[0]), prefab);
            string[] hidden;
            if (s.Config.hideCostume != null) hidden = s.Config.hideCostume;
            else if (s.Identity == LiveCastRules.PlayerIdentity) hidden = new string[0];
            else
            {
                hidden = new string[LiveCastRules.DropInHiddenCostume.Count];
                LiveCastRules.DropInHiddenCostume.CopyTo(hidden, 0);
            }
            ImportedModel04OD model = new ImportedModel04OD
            {
                Identity = s.Identity, Enabled = true, Prefab = prefab, Scale = 1f, AlignBindAxesH3 = true, PreserveInfluencesH5 = true,
                Bones = mapping, PassiveBonePaths = passive.ToArray(), Sockets = sockets.ToArray(), ArtProof = proof,
                AnimationAuthority = ImportedAnimationAuthority04OH.ExistingPose
            };
            LiveCastRow row = new LiveCastRow { Model = model, CastId = s.CastId, HiddenCostume = hidden, Label = "drop-in " + s.Name, Origin = "dropin:" + s.Name, Stamp = stamp };
            if (s.CrowdRoles != 0)
            {
                // A crowd drop-in serves ordinary people by role and sex; pieces named as armour hide on plain roles.
                row.CrowdRoles = s.CrowdRoles;
                row.CrowdFemale = s.Config.female;
                row.CrowdAge = s.CrowdAge;
                row.HiddenCostume = new string[0];
                List<string> armour = new List<string>();
                bool ownHair = false;
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (LiveCrowdRules.ArmourPart(renderer.name, LiveCrowdSurface.Unknown)) armour.Add(AnimationUtility.CalculateTransformPath(renderer.transform, prefab.transform));
                    foreach (Material m in renderer.sharedMaterials)
                        if (m != null && LiveCastFolderRules.OwnHairMaterial(m.name)) ownHair = true;
                }
                row.CrowdArmourPaths = armour.ToArray();
                // Procedural hair, beards and headwear fitted to the skull, unless the export brings its own hair (or livecast.json says).
                string hairMode = (s.Config.hair ?? "").Trim().ToLowerInvariant();
                row.CrowdHair = hairMode == "procedural" || (hairMode != "own" && !ownHair);
            }
            return row;
        }
    }
}
#endif
