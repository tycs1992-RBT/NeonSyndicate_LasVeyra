using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The exact Editor review contract for Dante Rook's supplied H8S visual (DANTE-H8S-REVIEW-01). It only says whether a
    /// review row may be inspected on the canonical actor through the original adapter; it can never approve, enable or
    /// deploy the candidate, and the procedural body stays the fallback. Pure data and string checks, no engine calls.
    /// Every static is a constant or a read-only collection, so there is no per-playthrough state to reset.
    /// </summary>
    public static class DanteReviewH8S
    {
        /// <summary>The recorded candidate id (candidate.json in Root).</summary>
        public const string AssetId = "DANTE-H8S-REVIEW-01";
        /// <summary>The delivered candidate folder.</summary>
        public const string Root = "Assets/NeonSyndicate/Art/ProductionH8S/NPC/";
        /// <summary>The LOD0 export the proof must name.</summary>
        public const string ExportPath = Root + "DanteRookH8S.fbx";
        /// <summary>The folder for generated review copies (prefab and materials). It is never committed.</summary>
        public const string PreparedFolder = Root + "Prepared";
        /// <summary>The generated disabled review prefab.</summary>
        public const string PreparedPrefab = PreparedFolder + "/DanteH8SReview.prefab";
        /// <summary>The editable source the exports came from.</summary>
        public const string SourceBlend = "ArtSource/AscensionH8S/DanteRookH8S.blend";
        /// <summary>The delivered SHA-256 of SourceBlend.</summary>
        public const string SourceSha256 = "f763a694c36fd6d88eb099f69d5d97a88261908fef97b4f983c3c31ab367013a";
        /// <summary>The canonical cast member (CastIds04E.Dante).</summary>
        public const string CastId = CastIds04E.Dante;
        /// <summary>The canonical display name (CastIds04E.Name of CastId) the original adapter derives the identity from.</summary>
        public const string DisplayName = "Dante Rook";
        /// <summary>The adapter identity of the live actor: "npc:" plus its display name.</summary>
        public const string Identity = "npc:" + DisplayName;
        /// <summary>The recorded license of every candidate file.</summary>
        public const string License = "CC0-1.0";
        /// <summary>Skinned meshes in each of the three LODs.</summary>
        public const int SkinsPerLod = 9;
        /// <summary>Bones in the delivered DanteRig armature.</summary>
        public const int SourceBoneCount = 65;
        /// <summary>The procedural chest layer (the apron bib) that duplicates the supplied apron (CastCostumeBuilder04E name).</summary>
        public const string CostumeTorso = "Costume Torso 04E";
        /// <summary>The procedural hip layer (apron skirt and tool belt). The held tool layer, the hammer, is never touched.</summary>
        public const string CostumeWaist = "Costume Waist 04E";

        /// <summary>The ten visual locators the original adapter requires on a humanoid rig.</summary>
        public static readonly ReadOnlyCollection<string> SocketRoles = Array.AsReadOnly(new[] { "hand_r", "hand_l", "foregrip", "muzzle", "blade_hilt", "blade_tip", "head", "chest", "fx", "synergy" });

        /// <summary>
        /// The explicit mapping: the Dante bone driven by each original pose bone, in RigLayout.Names order (17 body bones,
        /// then 15 left and 15 right finger joints). Every entry is unique.
        /// </summary>
        public static readonly ReadOnlyCollection<string> MappedBones = Array.AsReadOnly(new[]
        {
            "pelvis", "spine_01", "spine_03", "neck_01", "Head",
            "upperarm_l", "lowerarm_l", "hand_l", "upperarm_r", "lowerarm_r", "hand_r",
            "thigh_l", "calf_l", "foot_l", "thigh_r", "calf_r", "foot_r",
            "index_01_l", "index_02_l", "index_03_l", "middle_01_l", "middle_02_l", "middle_03_l",
            "ring_01_l", "ring_02_l", "ring_03_l", "pinky_01_l", "pinky_02_l", "pinky_03_l",
            "thumb_01_l", "thumb_02_l", "thumb_03_l",
            "index_01_r", "index_02_r", "index_03_r", "middle_01_r", "middle_02_r", "middle_03_r",
            "ring_01_r", "ring_02_r", "ring_03_r", "pinky_01_r", "pinky_02_r", "pinky_03_r",
            "thumb_01_r", "thumb_02_r", "thumb_03_r"
        });

        /// <summary>
        /// The weighted Dante bones that are not mapped. Each rides the mapped ancestor at the same index of PassiveAnchors
        /// with its bind-local offset, so no skin weight is pruned.
        /// </summary>
        public static readonly ReadOnlyCollection<string> PassiveBones = Array.AsReadOnly(new[]
        {
            "spine_02", "clavicle_l", "clavicle_r",
            "index_04_leaf_l", "middle_04_leaf_l", "ring_04_leaf_l", "pinky_04_leaf_l", "thumb_04_leaf_l",
            "index_04_leaf_r", "middle_04_leaf_r", "ring_04_leaf_r", "pinky_04_leaf_r", "thumb_04_leaf_r",
            "ball_l", "ball_r"
        });

        /// <summary>The nearest mapped ancestor of each passive bone (documentation and tests).</summary>
        public static readonly ReadOnlyCollection<string> PassiveAnchors = Array.AsReadOnly(new[]
        {
            "spine_01", "spine_03", "spine_03",
            "index_03_l", "middle_03_l", "ring_03_l", "pinky_03_l", "thumb_03_l",
            "index_03_r", "middle_03_r", "ring_03_r", "pinky_03_r", "thumb_03_r",
            "foot_l", "foot_r"
        });

        /// <summary>Source bones that carry no skin weight in any LOD: no mapping or passive entry is needed.</summary>
        public static readonly ReadOnlyCollection<string> UnweightedBones = Array.AsReadOnly(new[] { "root", "ball_leaf_l", "ball_leaf_r" });

        /// <summary>The seven recorded material roles, in prepared-asset order.</summary>
        public static readonly ReadOnlyCollection<string> MaterialRoles = Array.AsReadOnly(new[] { "Skin", "Wool", "Leather", "Apron", "Hair", "Brows", "Eyes" });

        /// <summary>The Dante bone mapped to an original pose bone index, or "" when out of range.</summary>
        public static string MappedBone(int sourceBone)
        {
            return sourceBone >= 0 && sourceBone < MappedBones.Count ? MappedBones[sourceBone] : "";
        }

        /// <summary>The last segment of a transform path ("Art/a/b" gives "b").</summary>
        public static string Leaf(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        /// <summary>
        /// Which recorded material role an imported material name belongs to (index into MaterialRoles), or -1 for an
        /// unrecorded material. Underscores read as spaces, an instance suffix and a numeric duplicate suffix are ignored.
        /// </summary>
        public static int MaterialRole(string imported)
        {
            if (string.IsNullOrWhiteSpace(imported)) return -1;
            string name = imported.Replace(" (Instance)", "").Replace('_', ' ').Trim();
            int dot = name.LastIndexOf('.');
            if (dot > 0 && dot < name.Length - 1)
            {
                bool digits = true;
                for (int i = dot + 1; i < name.Length; i++) if (name[i] < '0' || name[i] > '9') { digits = false; break; }
                if (digits) name = name.Substring(0, dot).TrimEnd();
            }
            switch (name)
            {
                case "CourtDefender Skin and underlayer": return 0;
                case "H6 Charcoal wool": return 1;
                case "H6 Worn leather": return 2;
                case "Dante worn forge apron": return 3;
                case "Dante dark iron hair": return 4;
                case "MI Hair 1": return 5;
                case "MI Eyes": return 6;
                default: return -1;
            }
        }

        /// <summary>
        /// True only for the exact Dante review row: an Editor session, the canonical identity, a disabled and unapproved
        /// candidate with this asset id, export path and present hashes, CC0-1.0 with commercial and public-source rights,
        /// the original pose authority, bind alignment with complete influences, the explicit 47-bone mapping, passive paths
        /// that each descend from a mapped bone, and all ten sockets. reason explains a refusal.
        /// </summary>
        public static bool CanReview(ImportedModel04OD model, bool editorSession, out string reason)
        {
            reason = "";
            if (!editorSession) { reason = "The Dante living cast review is an explicit Editor session, never a player-build override."; return false; }
            if (model == null || model.Identity != Identity) { reason = "Only the exact canonical identity " + Identity + " can be reviewed."; return false; }
            if (model.Enabled) { reason = "The Dante candidate stays disabled. Review never enables it."; return false; }
            if (!model.AlignBindAxesH3 || !model.PreserveInfluencesH5) { reason = "Dante review requires bind-axis alignment and complete skin weights."; return false; }
            if (Mathf.Abs(model.Scale - 1f) > 0.0001f) { reason = "The Dante candidate is authored in metres at scale one. A rescaled row would distort the skin."; return false; }
            if (model.AnimationAuthority != ImportedAnimationAuthority04OH.ExistingPose) { reason = "Authored playback is not part of this review. Keep the original pose authority."; return false; }
            ImportedArtRecord04OH p = model.ArtProof;
            if (p == null || p.AssetId != AssetId || p.Diagnostic || p.ExportAssetPath != ExportPath
                || !ImportedArt04OH.Hex(p.ExportSha256, 64) || !ImportedArt04OH.Hex(p.VisualDependencyHash, 32))
            { reason = "A current " + AssetId + " export and visual dependency proof is required."; return false; }
            if (p.OwnerApproved) { reason = "An owner-approved proof belongs in the production registry. This review only inspects the unapproved candidate."; return false; }
            if (string.IsNullOrWhiteSpace(p.Creator) || p.LicenseId != License || string.IsNullOrWhiteSpace(p.RightsRecordId) || !p.CommercialUse || !p.PublicSourceApproved)
            { reason = "Recorded CC0-1.0 commercial and public-source rights are required even for review."; return false; }
            if (!ImportedModelRegistry04OD.ValidMapping(RigLayout.BoneCount, model.Bones, model.Scale, out reason)) return false;
            foreach (ImportedBone04OD bone in model.Bones)
                if (Leaf(bone.TargetPath) != MappedBone(bone.SourceBone))
                { reason = "The mapping differs from the explicit Dante table at " + RigLayout.Names[bone.SourceBone] + "."; return false; }
            if (!ImportedModelRegistry04OD.ValidPassivePaths(model.PassiveBonePaths)) { reason = "Invalid or duplicate passive bone paths."; return false; }
            if (!PassivePathsDescend(model.Bones, model.PassiveBonePaths, out reason)) return false;
            return SocketsComplete(model.Sockets, out reason);
        }

        /// <summary>
        /// The original adapter's passive rule on paths: every passive path names a recorded passive bone, is not itself
        /// mapped, and lies strictly below at least one mapped bone, so it keeps a bind-local offset under a driven parent.
        /// </summary>
        public static bool PassivePathsDescend(IList<ImportedBone04OD> bones, IList<string> passive, out string reason)
        {
            reason = "";
            if (passive == null || passive.Count == 0) return true;
            if (bones == null) { reason = "Passive paths need the mapped bones they ride."; return false; }
            foreach (string path in passive)
            {
                if (!PassiveBones.Contains(Leaf(path))) { reason = "Unrecorded passive bone: " + Leaf(path) + ". Reinspect the export."; return false; }
                bool below = false;
                foreach (ImportedBone04OD bone in bones)
                {
                    if (bone == null || string.IsNullOrEmpty(bone.TargetPath)) continue;
                    if (string.Equals(bone.TargetPath, path, StringComparison.Ordinal)) { reason = "A bone cannot be both mapped and passive: " + Leaf(path) + "."; return false; }
                    if (path.Length > bone.TargetPath.Length + 1 && path.StartsWith(bone.TargetPath, StringComparison.Ordinal) && path[bone.TargetPath.Length] == '/') below = true;
                }
                if (!below) { reason = "Passive bone " + Leaf(path) + " does not descend from a mapped bone."; return false; }
            }
            return true;
        }

        /// <summary>All ten roles present exactly once, each a relative path ending in "socket_" plus its role.</summary>
        public static bool SocketsComplete(IList<ImportedSocket04OD> sockets, out string reason)
        {
            reason = "";
            HashSet<string> roles = new HashSet<string>(StringComparer.Ordinal);
            if (sockets != null)
                foreach (ImportedSocket04OD socket in sockets)
                {
                    if (socket == null || !CinematicAcceptance04OF.RelativeBonePath(socket.Path) || !SocketRoles.Contains(socket.Role)
                        || Leaf(socket.Path) != "socket_" + socket.Role || !roles.Add(socket.Role))
                    { reason = "Review locators are missing, unrecorded or duplicated."; return false; }
                }
            foreach (string role in SocketRoles)
                if (!roles.Contains(role)) { reason = "Review lacks a visual locator: " + role; return false; }
            return true;
        }
    }
}
