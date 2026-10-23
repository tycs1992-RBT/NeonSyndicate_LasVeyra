using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>The H9 cut of the Dante review: its contract, its receipt and its refit skeleton. Engine-free.</summary>
    public sealed class DanteReviewH9Tests
    {
        private static ImportedModel04OD RowH9()
        {
            ImportedModel04OD model = DanteReviewH8STests.Row();
            model.ArtProof.ExportAssetPath = DanteReviewH9.ExportPath;
            return model;
        }

        [Test]
        public void H9RowPassesOnlyThroughTheH9ContractAndCannotBeApproved()
        {
            string reason;
            Assert.IsTrue(DanteReviewH9.CanReview(RowH9(), true, out reason), reason);
            Assert.IsFalse(DanteReviewH8S.CanReview(RowH9(), true, out reason), "An H9 export cannot pass as H8S.");
            Assert.IsFalse(DanteReviewH9.CanReview(DanteReviewH8STests.Row(), true, out reason), "An H8S export cannot pass as H9.");
            Assert.IsFalse(DanteReviewH9.CanReview(RowH9(), false, out reason));
            ImportedModel04OD enabled = RowH9();
            enabled.Enabled = true;
            Assert.IsFalse(DanteReviewH9.CanReview(enabled, true, out reason));
            ImportedModel04OD socketless = RowH9();
            socketless.Sockets = Array.FindAll(socketless.Sockets, s => s.Role != "head");
            Assert.IsFalse(DanteReviewH9.CanReview(socketless, true, out reason));
            Assert.IsFalse(ImportedArt04OH.CanActivate(RowH9().ArtProof, ImportedAnimationAuthority04OH.ExistingPose, out reason));
        }

        [Test]
        public void H9KeepsTheCanonicalIdentityAndHasItsOwnFiles()
        {
            Assert.AreEqual(DanteReviewH8S.AssetId, DanteReviewH9.AssetId);
            Assert.AreEqual("npc:Dante Rook", DanteReviewH9.Identity);
            Assert.AreEqual(CastIds04E.Dante, DanteReviewH9.CastId);
            Assert.AreEqual(7, DanteReviewH9.SkinsPerLod);
            StringAssert.StartsWith("Assets/NeonSyndicate/Art/ProductionH9/NPC/", DanteReviewH9.ExportPath);
            Assert.AreNotEqual(DanteReviewH8S.PreparedPrefab, DanteReviewH9.PreparedPrefab);
            StringAssert.StartsWith(DanteReviewH9.Root, DanteReviewH9.HairAlbedo);
        }

        [Test]
        public void DeliveredH9ReceiptMatchesTheExactCandidate()
        {
            string root = Directory.GetCurrentDirectory();
            string receipt = File.ReadAllText(Path.Combine(root, DanteReviewH9.Root + "candidate.json"));
            StringAssert.Contains("\"assetId\": \"" + DanteReviewH9.AssetId + "\"", receipt);
            StringAssert.Contains("\"license\": \"CC0-1.0\"", receipt);
            StringAssert.Contains("\"ownerApproved\": false", receipt);
            StringAssert.Contains("\"enabled\": false", receipt);
            StringAssert.Contains("\"preserveInfluences\": true", receipt);
            StringAssert.Contains("\"bones\": " + DanteReviewH8S.SourceBoneCount, receipt);
            Assert.AreEqual(3, Regex.Matches(receipt, "\"skins\": " + DanteReviewH9.SkinsPerLod).Count);
            MatchCollection files = Regex.Matches(receipt, "\"path\": \"([^\"]+)\",\\s*\"sha256\": \"([0-9a-f]{64})\"");
            Assert.AreEqual(DanteReviewH9.ReceiptFiles, files.Count);
            foreach (Match file in files)
            {
                StringAssert.StartsWith(DanteReviewH9.Root, file.Groups[1].Value);
                Assert.AreEqual(file.Groups[2].Value, DanteReviewH8STests.Sha256(Path.Combine(root, file.Groups[1].Value)), file.Groups[1].Value);
            }
            foreach (string path in new[] { DanteReviewH9.ExportPath, DanteReviewH9.HairAlbedo, DanteReviewH9.HairNormal })
                StringAssert.Contains("\"path\": \"" + path + "\"", receipt);
            Assert.AreEqual(DanteReviewH9.SourceSha256, DanteReviewH8STests.Sha256(Path.Combine(root, DanteReviewH9.SourceBlend)));
        }

        /// <summary>H9 bind heads of the 47 mapped bones from the delivered LOD0 FBX (LOD1 and LOD2 identical), Unity axes, T-pose.</summary>
        private static readonly Vector3[] H9Bind =
        {
            new Vector3(0f, 0.988f, -0.043f), new Vector3(0f, 1.092f, -0.0069f), new Vector3(0f, 1.3416f, 0.0069f), new Vector3(0f, 1.6016f, -0.0414f), new Vector3(0f, 1.6952f, -0.0174f),
            new Vector3(-0.2132f, 1.5392f, -0.0654f), new Vector3(-0.5147f, 1.5392f, -0.0745f), new Vector3(-0.7849f, 1.5392f, -0.0661f),
            new Vector3(0.2132f, 1.5392f, -0.0654f), new Vector3(0.5147f, 1.5392f, -0.0745f), new Vector3(0.7849f, 1.5392f, -0.0661f),
            new Vector3(-0.104f, 0.936f, -0.036f), new Vector3(-0.104f, 0.4888f, -0.0361f), new Vector3(-0.104f, 0.052f, -0.0854f),
            new Vector3(0.104f, 0.936f, -0.036f), new Vector3(0.104f, 0.4888f, -0.0361f), new Vector3(0.104f, 0.052f, -0.0854f),
            new Vector3(-0.886f, 1.533f, -0.0311f), new Vector3(-0.9141f, 1.532f, -0.0315f), new Vector3(-0.9381f, 1.531f, -0.0319f), new Vector3(-0.8843f, 1.5352f, -0.0531f), new Vector3(-0.9166f, 1.5339f, -0.0538f), new Vector3(-0.9425f, 1.5329f, -0.0544f),
            new Vector3(-0.8791f, 1.5351f, -0.0767f), new Vector3(-0.9101f, 1.5339f, -0.0766f), new Vector3(-0.9345f, 1.533f, -0.0763f), new Vector3(-0.8698f, 1.534f, -0.0959f), new Vector3(-0.897f, 1.533f, -0.0967f), new Vector3(-0.9206f, 1.5321f, -0.0975f),
            new Vector3(-0.814f, 1.5155f, -0.0307f), new Vector3(-0.8408f, 1.499f, -0.0178f), new Vector3(-0.8649f, 1.4842f, -0.0062f),
            new Vector3(0.886f, 1.533f, -0.0311f), new Vector3(0.9141f, 1.532f, -0.0315f), new Vector3(0.9381f, 1.531f, -0.0319f), new Vector3(0.8843f, 1.5352f, -0.0531f), new Vector3(0.9166f, 1.5339f, -0.0538f), new Vector3(0.9425f, 1.5329f, -0.0544f),
            new Vector3(0.8791f, 1.5351f, -0.0767f), new Vector3(0.9101f, 1.5339f, -0.0766f), new Vector3(0.9345f, 1.533f, -0.0763f), new Vector3(0.8698f, 1.534f, -0.0959f), new Vector3(0.897f, 1.533f, -0.0967f), new Vector3(0.9206f, 1.5321f, -0.0975f),
            new Vector3(0.814f, 1.5155f, -0.0307f), new Vector3(0.8408f, 1.499f, -0.0178f), new Vector3(0.8649f, 1.4842f, -0.0062f)
        };

        [Test]
        public void H9RestSkeletonMatchesTheProceduralDanteAndAligns()
        {
            RigLayout layout = RigLayout.From(CastRoster04E.Look(CastIds04E.Dante));
            foreach (int i in new[] { RigLayout.Hips, RigLayout.Spine, RigLayout.Chest, RigLayout.Neck, RigLayout.Head, RigLayout.UpperArmL, RigLayout.UpperArmR,
                RigLayout.ThighL, RigLayout.ShinL, RigLayout.FootL, RigLayout.ThighR, RigLayout.ShinR, RigLayout.FootR })
                Assert.AreEqual(layout.World[i].y, H9Bind[i].y, 0.01f, RigLayout.Names[i] + " height");
            foreach (int i in new[] { RigLayout.UpperArmL, RigLayout.UpperArmR, RigLayout.ThighL, RigLayout.ThighR })
                Assert.AreEqual(layout.World[i].x, H9Bind[i].x, 0.01f, RigLayout.Names[i] + " width");
            Assert.AreEqual(layout.UpperArmLength, Vector3.Distance(H9Bind[RigLayout.UpperArmL], H9Bind[RigLayout.ForearmL]), 0.01f);
            Assert.AreEqual(layout.ForearmLength, Vector3.Distance(H9Bind[RigLayout.ForearmL], H9Bind[RigLayout.HandL]), 0.01f);
            Quaternion[] rest = new Quaternion[RigLayout.BoneCount];
            for (int i = 0; i < rest.Length; i++) rest[i] = Quaternion.identity;
            Quaternion[] aligned = ImportedBindH3.Align(layout.World, RigLayout.Parent, H9Bind, rest, Quaternion.identity);
            Assert.AreEqual(90f, Quaternion.Angle(Quaternion.identity, aligned[RigLayout.UpperArmL]), 10f);
            Assert.Less(Quaternion.Angle(Quaternion.identity, aligned[RigLayout.ShinL]), 10f);
        }
    }
}
