using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Large aggregate renderer counts must never certify imported architecture or a visible transformation.</summary>
    public sealed class WorldRewriteProofWR01Tests
    {
        private static FantasyProofSnapshotV1 Ready()
        {
            return new FantasyProofSnapshotV1
            {
                build = new FantasyBuildStampV1 { gitCommit = new string('a', 40) }, unityVersion = "6000.3.16f1",
                worldRevision = 10, hasCamera = true, hasPlayer = true, playerImported = true,
                fr01 = new FantasyLayerProofV1 { prepared = true, selected = true, active = true, gatewayStaged = true },
                fr02a = new FantasyLayerProofV1 { prepared = true, selected = true, active = true, stagedHouses = 2 },
                worldRewrite = new FantasyRewriteProofWR01 { prepared = true, selected = true, policyActive = true, active = 1 }
            };
        }

        [Test] public void RetainedPhysicalWitnessesCannotProveImportedArchitectureInView()
        {
            FantasyProofSnapshotV1 proof = Ready();
            proof.worldRewrite.activeRenderers = 100; proof.worldRewrite.inCameraFrustumRenderers = 100;
            StringAssert.Contains("outside this camera frustum", FantasyProofRulesV1.Summary(proof));
        }

        [Test] public void MissingCivicCatalogCannotHideBehindActiveOriginalArt()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr01.inCameraFrustumRenderers = 1;
            proof.worldRewrite.prepared = false;
            StringAssert.Contains("not prepared", FantasyProofRulesV1.Summary(proof));
        }

        [Test] public void ARefusedReplacementRemainsPartialEvenWithOtherImportedArt()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.worldRewrite.importedCrownFrustumRenderers = 20;
            proof.worldRewrite.refused = 1;
            StringAssert.Contains("refused", FantasyProofRulesV1.Summary(proof));
        }

        [TestCase(true)][TestCase(false)]
        public void ImportedStructureCanBeObservedWithoutApprovingTheWorld(bool skyline)
        {
            FantasyProofSnapshotV1 proof = Ready();
            if (skyline) proof.worldRewrite.importedSkylineFrustumRenderers = 1; else proof.worldRewrite.importedCrownFrustumRenderers = 1;
            string verdict = FantasyProofRulesV1.Summary(proof);
            StringAssert.Contains("owner review", verdict);
            Assert.False(verdict.Contains("CONFIRMED")); Assert.False(verdict.Contains("ACCEPTED"));
        }

        [Test] public void NoNativeObservationNeverBecomesAnAcceptance()
        {
            FantasyProofSnapshotV1 proof = new FantasyProofSnapshotV1();
            StringAssert.StartsWith("BLOCKED", FantasyProofRulesV1.Summary(proof));
            Assert.AreEqual("UNVERIFIED", proof.characterAppearance);
            Assert.AreEqual(0, proof.worldRewrite.active);
        }
    }
}
