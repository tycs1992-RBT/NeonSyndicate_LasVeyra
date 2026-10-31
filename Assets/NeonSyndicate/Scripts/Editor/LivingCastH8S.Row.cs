#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LivingCastH8S, the review row: the explicit 47-bone mapping from DanteReviewH8S, passive paths for every other
    /// weighted bone (each below a mapped bone), the ten socket locators, the export and dependency proof, and measured
    /// diagnostics (socket offsets, bind proportions against the live procedural rig) for the owner's review.
    /// </summary>
    public static partial class LivingCastH8S
    {
        private const string SourceUrl = "https://quaternius.com/";

        private static ImportedModel04OD ReviewRow(GameObject prepared, Receipt receipt, out string summary)
        {
            CheckImporters();
            if (AssetDatabase.GetAssetPath(prepared) != _cut.PreparedPrefab) throw new InvalidDataException("The review copy is not the prepared Dante prefab.");
            LODGroup group = prepared.GetComponent<LODGroup>();
            if (group == null || group.lodCount != 3) throw new InvalidDataException("The review copy lost its three-level LOD group. Delete " + _cut.PreparedFolder + " and prepare again outside Play mode.");
            Transform skeleton = Named(prepared, "root");
            SkinnedMeshRenderer[] skins = prepared.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int[] perLevel = new int[3];
            foreach (SkinnedMeshRenderer skin in skins)
            {
                int level = LodLevel(skin.name);
                if (level < 0 || skin.sharedMesh == null) throw new InvalidDataException("Unexpected skin in the review copy: " + skin.name);
                perLevel[level]++;
                foreach (Transform bone in skin.bones)
                    if (bone == null || !bone.IsChildOf(skeleton)) throw new InvalidDataException("Every LOD must share the one LOD0 skeleton; " + skin.name + " does not. Delete the Prepared folder and prepare again.");
            }
            for (int i = 0; i < 3; i++)
                if (perLevel[i] != _cut.SkinsPerLod) throw new InvalidDataException("LOD" + i + " of the review copy has " + perLevel[i] + " skins; the receipt records " + _cut.SkinsPerLod + ".");
            ImportedBone04OD[] mapping = new ImportedBone04OD[RigLayout.BoneCount];
            HashSet<Transform> mapped = new HashSet<Transform>();
            for (int i = 0; i < mapping.Length; i++)
            {
                Transform bone = Named(prepared, DanteReviewH8S.MappedBone(i));
                if (!bone.IsChildOf(skeleton) || !mapped.Add(bone)) throw new InvalidDataException("Mapped bone outside the skeleton or used twice: " + bone.name);
                mapping[i] = new ImportedBone04OD { SourceBone = i, TargetPath = AnimationUtility.CalculateTransformPath(bone, prepared.transform) };
            }
            List<string> passive = new List<string>();
            HashSet<string> weighted = new HashSet<string>(StringComparer.Ordinal);
            foreach (SkinnedMeshRenderer skin in skins)
            {
                bool[] referenced = SkinWeightsH5.Referenced(skin.sharedMesh, skin.bones.Length);
                for (int b = 0; b < skin.bones.Length; b++)
                {
                    if (!referenced[b]) continue;
                    Transform bone = skin.bones[b];
                    weighted.Add(bone.name);
                    if (mapped.Contains(bone)) continue;
                    string path = AnimationUtility.CalculateTransformPath(bone, prepared.transform);
                    if (!passive.Contains(path)) passive.Add(path);
                }
            }
            // Keep every weighted bone: the weighted set must be exactly the mapped and recorded passive bones.
            HashSet<string> expected = new HashSet<string>(DanteReviewH8S.MappedBones, StringComparer.Ordinal);
            expected.UnionWith(DanteReviewH8S.PassiveBones);
            if (!weighted.SetEquals(expected))
            {
                List<string> extra = new List<string>(weighted);
                extra.RemoveAll(expected.Contains);
                List<string> missing = new List<string>(expected);
                missing.RemoveAll(weighted.Contains);
                throw new InvalidDataException("The imported weighted bones differ from the recorded 62 (unexpected: " + string.Join(", ", extra) + "; unweighted: " + string.Join(", ", missing) + "). Reinspect the export.");
            }
            List<ImportedSocket04OD> sockets = new List<ImportedSocket04OD>();
            StringBuilder offsets = new StringBuilder();
            foreach (string role in DanteReviewH8S.SocketRoles)
            {
                Transform socket = Named(prepared, "socket_" + role);
                if (!socket.IsChildOf(skeleton)) throw new InvalidDataException("Socket outside the skeleton: " + socket.name);
                sockets.Add(new ImportedSocket04OD { Role = role, Path = AnimationUtility.CalculateTransformPath(socket, prepared.transform) });
                float offset = Vector3.Distance(socket.position, socket.parent.position);
                if (offset > .2f) offsets.Append(offsets.Length > 0 ? ", " : "").Append(role).Append(' ').Append(Metres(offset)).Append(" from ").Append(socket.parent.name);
            }
            ImportedArtRecord04OH proof = new ImportedArtRecord04OH
            {
                AssetId = receipt.assetId, Creator = receipt.creator, SourceUrl = SourceUrl, LicenseId = receipt.license, RightsRecordId = _cut.RightsRecord,
                CommercialUse = true, PublicSourceApproved = true, OwnerApproved = false
            };
            ImportedArtProof04OH.Stamp(proof, AssetDatabase.LoadAssetAtPath<GameObject>(_cut.ExportPath), prepared);
            summary = "Three LODs of " + _cut.SkinsPerLod + " skins share one skeleton; " + weighted.Count + " weighted bones kept (" + mapping.Length + " mapped, "
                + passive.Count + " passive), unweighted " + string.Join(", ", DanteReviewH8S.UnweightedBones) + "; ten sockets present"
                + (offsets.Length > 0 ? " (placement defect to fix in the source, nothing positions on them today: " + offsets + ")" : "") + ".";
            return new ImportedModel04OD
            {
                Identity = DanteReviewH8S.Identity, Enabled = false, Prefab = prepared, Scale = 1f, AlignBindAxesH3 = true, PreserveInfluencesH5 = true,
                Bones = mapping, PassiveBonePaths = passive.ToArray(), Sockets = sockets.ToArray(), ArtProof = proof,
                AnimationAuthority = ImportedAnimationAuthority04OH.ExistingPose
            };
        }

        /// <summary>
        /// Bind heights of the supplied skeleton against the live procedural rig, and where the supplied boot sole lands when
        /// the adapter puts its ankle on the procedural ankle (negative means into the floor). Measurement only.
        /// </summary>
        private static string Proportions(GameObject prepared, NpcController actor)
        {
            if (actor == null || actor.Rig == null || actor.Rig.Layout == null) return "";
            try { return MeasureProportions(prepared, actor.Rig.Layout.World); }
            catch (Exception e) { return "Proportion measurement unavailable (" + e.Message + ")."; }
        }

        private static string MeasureProportions(GameObject prepared, Vector3[] world)
        {
            float sole = float.PositiveInfinity;
            foreach (SkinnedMeshRenderer skin in prepared.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (LodLevel(skin.name) != 0 || skin.sharedMesh == null) continue;
                Bounds b = skin.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = new Vector3((corner & 1) == 0 ? b.min.x : b.max.x, (corner & 2) == 0 ? b.min.y : b.max.y, (corner & 4) == 0 ? b.min.z : b.max.z);
                    sole = Mathf.Min(sole, skin.transform.TransformPoint(local).y);
                }
            }
            float ankle = Named(prepared, "foot_l").position.y;
            float landed = world[RigLayout.FootL].y - (ankle - sole);
            StringBuilder s = new StringBuilder("Bind heights supplied/procedural in metres: ");
            Pair(s, "hips", Named(prepared, "pelvis").position.y, world[RigLayout.Hips].y);
            Pair(s, "chest", Named(prepared, "spine_03").position.y, world[RigLayout.Chest].y);
            Pair(s, "neck", Named(prepared, "neck_01").position.y, world[RigLayout.Neck].y);
            Pair(s, "head", Named(prepared, "Head").position.y, world[RigLayout.Head].y);
            Pair(s, "shoulder", Named(prepared, "upperarm_l").position.y, world[RigLayout.UpperArmL].y);
            Pair(s, "ankle", ankle, world[RigLayout.FootL].y);
            s.Append("expected boot sole ").Append((landed * 100f).ToString("0.0", CultureInfo.InvariantCulture)).Append(" cm against the floor (negative sinks; check feet planted).");
            return s.ToString();
        }

        private static void Pair(StringBuilder s, string name, float supplied, float procedural)
        {
            s.Append(name).Append(' ').Append(supplied.ToString("0.000", CultureInfo.InvariantCulture)).Append('/').Append(procedural.ToString("0.000", CultureInfo.InvariantCulture)).Append(", ");
        }

        private static string Metres(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }
    }
}
#endif
