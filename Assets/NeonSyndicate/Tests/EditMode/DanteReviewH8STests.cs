using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The exact Dante Rook H8S review contract, its explicit bone table and the delivered receipt. Engine-free.</summary>
    public sealed class DanteReviewH8STests
    {
        private const string Prefix = "Art/LOD0 source/DanteRig";

        /// <summary>The DanteRig armature as inspected in the delivered source blend and all three FBX exports (65 bones).</summary>
        private static Dictionary<string, string> Parents()
        {
            Dictionary<string, string> p = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "root", "" }, { "pelvis", "root" }, { "spine_01", "pelvis" }, { "spine_02", "spine_01" }, { "spine_03", "spine_02" },
                { "neck_01", "spine_03" }, { "Head", "neck_01" }
            };
            foreach (string side in new[] { "l", "r" })
            {
                p.Add("clavicle_" + side, "spine_03");
                p.Add("upperarm_" + side, "clavicle_" + side);
                p.Add("lowerarm_" + side, "upperarm_" + side);
                p.Add("hand_" + side, "lowerarm_" + side);
                foreach (string finger in new[] { "index", "middle", "pinky", "ring", "thumb" })
                {
                    p.Add(finger + "_01_" + side, "hand_" + side);
                    p.Add(finger + "_02_" + side, finger + "_01_" + side);
                    p.Add(finger + "_03_" + side, finger + "_02_" + side);
                    p.Add(finger + "_04_leaf_" + side, finger + "_03_" + side);
                }
                p.Add("thigh_" + side, "pelvis");
                p.Add("calf_" + side, "thigh_" + side);
                p.Add("foot_" + side, "calf_" + side);
                p.Add("ball_" + side, "foot_" + side);
                p.Add("ball_leaf_" + side, "ball_" + side);
            }
            return p;
        }

        private static string PathOf(string bone)
        {
            Dictionary<string, string> parents = Parents();
            List<string> chain = new List<string>();
            for (string at = bone; !string.IsNullOrEmpty(at); at = parents[at]) chain.Insert(0, at);
            return Prefix + "/" + string.Join("/", chain);
        }

        private static ImportedModel04OD Row()
        {
            ImportedBone04OD[] bones = new ImportedBone04OD[RigLayout.BoneCount];
            for (int i = 0; i < bones.Length; i++) bones[i] = new ImportedBone04OD { SourceBone = i, TargetPath = PathOf(DanteReviewH8S.MappedBones[i]) };
            string[] passive = new string[DanteReviewH8S.PassiveBones.Count];
            for (int i = 0; i < passive.Length; i++) passive[i] = PathOf(DanteReviewH8S.PassiveBones[i]);
            ImportedSocket04OD[] sockets = new ImportedSocket04OD[DanteReviewH8S.SocketRoles.Count];
            Dictionary<string, string> socketParent = new Dictionary<string, string>
            {
                { "hand_r", "hand_r" }, { "hand_l", "hand_l" }, { "foregrip", "hand_l" }, { "muzzle", "hand_r" }, { "blade_hilt", "hand_r" },
                { "blade_tip", "hand_r" }, { "head", "Head" }, { "chest", "spine_03" }, { "fx", "spine_03" }, { "synergy", "spine_03" }
            };
            for (int i = 0; i < sockets.Length; i++)
            {
                string role = DanteReviewH8S.SocketRoles[i];
                sockets[i] = new ImportedSocket04OD { Role = role, Path = PathOf(socketParent[role]) + "/socket_" + role };
            }
            return new ImportedModel04OD
            {
                Identity = DanteReviewH8S.Identity, Enabled = false, Scale = 1f, AlignBindAxesH3 = true, PreserveInfluencesH5 = true,
                Bones = bones, PassiveBonePaths = passive, Sockets = sockets, AnimationAuthority = ImportedAnimationAuthority04OH.ExistingPose,
                ArtProof = new ImportedArtRecord04OH
                {
                    AssetId = DanteReviewH8S.AssetId, Creator = "CC0 distinct male study and project fitted work clothing", SourceUrl = "https://quaternius.com/",
                    LicenseId = "CC0-1.0", RightsRecordId = "h8s-dante-cc0-lineage", CommercialUse = true, PublicSourceApproved = true, OwnerApproved = false,
                    ExportAssetPath = DanteReviewH8S.ExportPath, ExportSha256 = new string('a', 64), VisualDependencyHash = new string('b', 32)
                }
            };
        }

        [Test]
        public void ExactRowPassesButCannotBecomeProductionApproval()
        {
            ImportedModel04OD model = Row();
            string reason;
            Assert.IsTrue(DanteReviewH8S.CanReview(model, true, out reason), reason);
            Assert.IsFalse(ImportedArt04OH.CanActivate(model.ArtProof, model.AnimationAuthority, out reason));
            Assert.IsFalse(model.Enabled);
            Assert.IsFalse(model.ArtProof.OwnerApproved);
        }

        [Test]
        public void InspectionCannotEnterAPlayerBuild()
        {
            string reason;
            Assert.IsFalse(DanteReviewH8S.CanReview(Row(), false, out reason));
            Assert.IsNotEmpty(reason);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)] [TestCase(21)] [TestCase(22)] [TestCase(23)]
        [TestCase(24)] [TestCase(25)] [TestCase(26)]
        public void EveryDefectIsRefusedWithAReason(int defect)
        {
            ImportedModel04OD model = Row();
            switch (defect)
            {
                case 0: model.ArtProof.AssetId = "MALIK-H8R-REVIEW-01"; break;
                case 1: model.Enabled = true; break;
                case 2: model.ArtProof.ExportAssetPath = DanteReviewH8S.Root + "DanteRookH8S_LOD1.fbx"; break;
                case 3: model.ArtProof.ExportSha256 = ""; break;
                case 4: model.ArtProof.VisualDependencyHash = "not-a-hash"; break;
                case 5: model.Identity = "npc:Pedestrian"; break;
                case 6: model.Identity = "malik"; break;
                case 7: model.Sockets = Array.FindAll(model.Sockets, s => s.Role != "synergy"); break;
                case 8: model.ArtProof.CommercialUse = false; break;
                case 9: model.ArtProof.PublicSourceApproved = false; break;
                case 10: model.ArtProof.LicenseId = "CC-BY-4.0"; break;
                case 11: model.ArtProof.OwnerApproved = true; break;
                case 12: model.ArtProof.Diagnostic = true; break;
                case 13: model.AnimationAuthority = ImportedAnimationAuthority04OH.AuthoredClips; break;
                case 14: model.AlignBindAxesH3 = false; break;
                case 15: model.PreserveInfluencesH5 = false; break;
                case 16: model.Scale = 1.04f; break;
                case 17: model.Bones[3].SourceBone = 2; break;
                case 18: { string a = model.Bones[5].TargetPath; model.Bones[5].TargetPath = model.Bones[8].TargetPath; model.Bones[8].TargetPath = a; break; }
                case 19: model.PassiveBonePaths = new[] { Prefix + "/root" }; break;
                case 20: model.PassiveBonePaths = new[] { PathOf("ball_leaf_l") }; break;
                case 21: model.PassiveBonePaths = new[] { model.Bones[0].TargetPath }; break;
                case 22: model.PassiveBonePaths = new[] { PathOf("spine_02"), PathOf("spine_02") }; break;
                case 23: model.Sockets[0].Path = PathOf("hand_r") + "/socket_hand_l"; break;
                case 24: model.ArtProof = null; break;
                case 25: model.ArtProof.RightsRecordId = " "; break;
                case 26: model.Bones = new ImportedBone04OD[0]; break;
            }
            string reason;
            Assert.IsFalse(DanteReviewH8S.CanReview(model, true, out reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void MappingTableIsCompleteUniqueAndSided()
        {
            Assert.AreEqual(47, RigLayout.BoneCount);
            Assert.AreEqual(RigLayout.BoneCount, DanteReviewH8S.MappedBones.Count);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < RigLayout.BoneCount; i++)
            {
                string target = DanteReviewH8S.MappedBones[i];
                Assert.IsTrue(seen.Add(target), "Duplicate Dante bone " + target);
                // The explicit table agrees with the existing candidate naming convention of the original bind helper.
                Assert.AreEqual(ImportedBindH3.CandidateBone(RigLayout.Names[i]), target, RigLayout.Names[i]);
                bool left = RigLayout.Names[i].EndsWith(".L", StringComparison.Ordinal), right = RigLayout.Names[i].EndsWith(".R", StringComparison.Ordinal);
                Assert.AreEqual(left, target.EndsWith("_l", StringComparison.Ordinal), RigLayout.Names[i]);
                Assert.AreEqual(right, target.EndsWith("_r", StringComparison.Ordinal), RigLayout.Names[i]);
            }
            Assert.AreEqual("", DanteReviewH8S.MappedBone(-1));
            Assert.AreEqual("", DanteReviewH8S.MappedBone(RigLayout.BoneCount));
        }

        [Test]
        public void MappedPassiveAndUnweightedBonesPartitionAllSixtyFiveSourceBones()
        {
            Dictionary<string, string> parents = Parents();
            Assert.AreEqual(DanteReviewH8S.SourceBoneCount, parents.Count);
            HashSet<string> all = new HashSet<string>(StringComparer.Ordinal);
            foreach (string b in DanteReviewH8S.MappedBones) Assert.IsTrue(all.Add(b), b);
            foreach (string b in DanteReviewH8S.PassiveBones) Assert.IsTrue(all.Add(b), b);
            foreach (string b in DanteReviewH8S.UnweightedBones) Assert.IsTrue(all.Add(b), b);
            Assert.IsTrue(all.SetEquals(parents.Keys));
            Assert.AreEqual(62, DanteReviewH8S.MappedBones.Count + DanteReviewH8S.PassiveBones.Count, "62 weighted bones");
        }

        [Test]
        public void MappedHierarchyFollowsTheOriginalPoseRig()
        {
            Dictionary<string, string> parents = Parents();
            for (int i = 0; i < RigLayout.BoneCount; i++)
            {
                int parent = RigLayout.Parent[i];
                if (parent < 0) continue;
                string want = DanteReviewH8S.MappedBones[parent];
                bool found = false;
                for (string at = parents[DanteReviewH8S.MappedBones[i]]; !string.IsNullOrEmpty(at); at = parents[at]) if (at == want) { found = true; break; }
                Assert.IsTrue(found, DanteReviewH8S.MappedBones[i] + " must sit below " + want);
            }
        }

        [Test]
        public void EveryPassiveBoneRidesItsRecordedMappedAnchor()
        {
            Dictionary<string, string> parents = Parents();
            Assert.AreEqual(DanteReviewH8S.PassiveBones.Count, DanteReviewH8S.PassiveAnchors.Count);
            for (int i = 0; i < DanteReviewH8S.PassiveBones.Count; i++)
            {
                string at = parents[DanteReviewH8S.PassiveBones[i]];
                while (!string.IsNullOrEmpty(at) && !DanteReviewH8S.MappedBones.Contains(at)) at = parents[at];
                Assert.AreEqual(DanteReviewH8S.PassiveAnchors[i], at, DanteReviewH8S.PassiveBones[i]);
            }
            ImportedModel04OD model = Row();
            string reason;
            Assert.IsTrue(DanteReviewH8S.PassivePathsDescend(model.Bones, model.PassiveBonePaths, out reason), reason);
            Assert.IsFalse(DanteReviewH8S.PassivePathsDescend(model.Bones, new[] { "Elsewhere/spine_02" }, out reason));
            Assert.IsFalse(DanteReviewH8S.PassivePathsDescend(model.Bones, new[] { Prefix + "/root/pelvisX/spine_02" }, out reason));
            Assert.IsTrue(DanteReviewH8S.PassivePathsDescend(model.Bones, new string[0], out reason));
        }

        [Test]
        public void SocketsAreTheAdapterTenWithMatchingNames()
        {
            CollectionAssert.AreEqual(new[] { "hand_r", "hand_l", "foregrip", "muzzle", "blade_hilt", "blade_tip", "head", "chest", "fx", "synergy" }, DanteReviewH8S.SocketRoles);
            string reason;
            Assert.IsTrue(DanteReviewH8S.SocketsComplete(Row().Sockets, out reason), reason);
            ImportedSocket04OD[] doubled = Row().Sockets;
            doubled[1].Role = "hand_r";
            Assert.IsFalse(DanteReviewH8S.SocketsComplete(doubled, out reason));
            ImportedSocket04OD[] unknown = Row().Sockets;
            unknown[0].Role = "tail";
            Assert.IsFalse(DanteReviewH8S.SocketsComplete(unknown, out reason));
            Assert.IsFalse(DanteReviewH8S.SocketsComplete(null, out reason));
        }

        [Test]
        public void MaterialRolesCoverEveryRecordedSourceMaterial()
        {
            Assert.AreEqual(7, DanteReviewH8S.MaterialRoles.Count);
            Assert.AreEqual(0, DanteReviewH8S.MaterialRole("CourtDefender Skin and underlayer"));
            Assert.AreEqual(1, DanteReviewH8S.MaterialRole("H6 Charcoal wool"));
            Assert.AreEqual(1, DanteReviewH8S.MaterialRole("H6_Charcoal_wool.002"));
            Assert.AreEqual(2, DanteReviewH8S.MaterialRole("H6 Worn leather"));
            Assert.AreEqual(3, DanteReviewH8S.MaterialRole("Dante worn forge apron"));
            Assert.AreEqual(4, DanteReviewH8S.MaterialRole("Dante dark iron hair (Instance)"));
            Assert.AreEqual(5, DanteReviewH8S.MaterialRole("MI_Hair_1"));
            Assert.AreEqual(6, DanteReviewH8S.MaterialRole("MI_Eyes.001"));
            Assert.AreEqual(6, DanteReviewH8S.MaterialRole("MI_Eyes"));
            Assert.AreEqual(-1, DanteReviewH8S.MaterialRole("MI_Eyes.00x"));
            Assert.AreEqual(-1, DanteReviewH8S.MaterialRole("H6 Aged bronze"));
            Assert.AreEqual(-1, DanteReviewH8S.MaterialRole(null));
            Assert.AreEqual(-1, DanteReviewH8S.MaterialRole(" "));
        }

        [Test]
        public void CanonicalIdentityMatchesTheCastRoster()
        {
            Assert.AreEqual("dante", DanteReviewH8S.CastId);
            Assert.AreEqual(CastIds04E.Name(CastIds04E.Dante), DanteReviewH8S.DisplayName);
            Assert.AreEqual("npc:" + CastIds04E.Name(CastIds04E.Dante), DanteReviewH8S.Identity);
            CastDef04E dante = Array.Find(CastRoster04E.All, d => d.Id == CastIds04E.Dante);
            Assert.IsNotNull(dante);
            Assert.AreEqual(DanteReviewH8S.DisplayName, dante.Name);
            Assert.AreEqual("forge", dante.Venue);
            Assert.AreEqual(4, dante.StoryUnlock);
            Assert.AreEqual(CostumeTorso.Apron, dante.Costume.Torso);
            Assert.AreEqual(CostumeWaist.ToolBelt, dante.Costume.Waist);
            Assert.AreEqual(HeldTool.Hammer, dante.Costume.Tool);
            Appearance look = CastRoster04E.Look(CastIds04E.Dante);
            Assert.IsFalse(look.Female);
            Assert.IsTrue(look.Beard);
            Assert.AreEqual(HairStyle.Long, look.HairStyle);
            Assert.AreEqual(HumanoidCreature.None, look.Creature);
        }

        [Test]
        public void CostumeLayerNamesMatchTheOriginalCostumeBuilder()
        {
            string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Guidance04E/CastCostumeBuilder04E.cs"));
            StringAssert.Contains("rig.Chest, \"" + DanteReviewH8S.CostumeTorso + "\"", source);
            StringAssert.Contains("rig.Hips, \"" + DanteReviewH8S.CostumeWaist + "\"", source);
            // The hammer stays: it is a separate layer on the left hand that the review never collects.
            StringAssert.Contains("rig.HandL, \"Costume Tool 04E\"", source);
        }

        [Test]
        public void DeliveredReceiptMatchesTheExactCandidate()
        {
            string root = Directory.GetCurrentDirectory();
            string receipt = File.ReadAllText(Path.Combine(root, DanteReviewH8S.Root + "candidate.json"));
            StringAssert.Contains("\"assetId\": \"" + DanteReviewH8S.AssetId + "\"", receipt);
            StringAssert.Contains("\"license\": \"CC0-1.0\"", receipt);
            StringAssert.Contains("\"commercialUse\": true", receipt);
            StringAssert.Contains("\"publicSourceApproved\": true", receipt);
            StringAssert.Contains("\"preserveInfluences\": true", receipt);
            StringAssert.Contains("\"ownerApproved\": false", receipt);
            StringAssert.Contains("\"enabled\": false", receipt);
            StringAssert.Contains("\"bones\": " + DanteReviewH8S.SourceBoneCount, receipt);
            Assert.AreEqual(3, Regex.Matches(receipt, "\"skins\": " + DanteReviewH8S.SkinsPerLod).Count);
            MatchCollection files = Regex.Matches(receipt, "\"path\": \"([^\"]+)\",\\s*\"sha256\": \"([0-9a-f]{64})\"");
            Assert.AreEqual(7, files.Count);
            foreach (Match file in files)
            {
                string path = file.Groups[1].Value;
                Assert.IsTrue(path.StartsWith(DanteReviewH8S.Root, StringComparison.Ordinal), path);
                Assert.AreEqual(file.Groups[2].Value, Sha256(Path.Combine(root, path)), path);
            }
            StringAssert.Contains("\"path\": \"" + DanteReviewH8S.ExportPath + "\"", receipt);
            Assert.AreEqual(DanteReviewH8S.SourceSha256, Sha256(Path.Combine(root, DanteReviewH8S.SourceBlend)));
        }

        private static string Sha256(string file)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(file))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
