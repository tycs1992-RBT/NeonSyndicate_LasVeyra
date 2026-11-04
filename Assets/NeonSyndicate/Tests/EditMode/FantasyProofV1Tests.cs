using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class FantasyProofV1Tests
    {
        private static FantasyProofSnapshotV1 Ready()
        {
            return new FantasyProofSnapshotV1
            {
                build = new FantasyBuildStampV1 { gitCommit = new string('a', 40), projectIdentity = "test project", sourceFingerprint = "test fingerprint" },
                unityVersion = "6000.3.16f1", worldRevision = 10, hasCamera = true, hasPlayer = true, playerImported = true,
                fr01 = new FantasyLayerProofV1 { prepared = true, selected = true, active = true, gatewayStaged = true, inCameraFrustumRenderers = 1 },
                fr02a = new FantasyLayerProofV1 { prepared = true, selected = true, active = true, stagedHouses = 2, inCameraFrustumRenderers = 2 }
            };
        }

        [Test] public void MissingCheckoutCannotReportObservableDelivery()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.build.gitCommit = "";
            StringAssert.StartsWith("BLOCKED", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void WrongUnityVersionIsBlocked()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.unityVersion = "different";
            StringAssert.StartsWith("BLOCKED", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void WrongRevisionCannotPass()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.worldRevision = 9;
            StringAssert.Contains("not revision 10", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void MissingImportedMaterialCannotPass()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr02a.prepared = false;
            StringAssert.Contains("COULD NOT LOAD", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void SelectedIsNotActive()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr02a.active = false;
            StringAssert.Contains("inactive", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void ActiveIsNotInsideGameplayCamera()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr01.inCameraFrustumRenderers = 0; proof.fr02a.inCameraFrustumRenderers = 0;
            StringAssert.Contains("outside this camera frustum", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void NoAdmittedHousesRemainsPartialEvenWithGateway()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr02a.stagedHouses = 0;
            StringAssert.Contains("0 houses admitted", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void NoGatewayRemainsPartialEvenWithHouses()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.fr01.gatewayStaged = false;
            StringAssert.Contains("gateway admission failed", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void MissingCameraCannotReportVisibleArt()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.hasCamera = false;
            StringAssert.Contains("no gameplay camera", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void RequiredCharacterMapProblemBlocksApparentSuccess()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.firstCharacterProblem = "Required skin texture missing";
            StringAssert.Contains("Required skin texture missing", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void ProceduralFallbackRemainsExplicitlyUnverified()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.playerImported = false;
            StringAssert.Contains("procedural fallback", FantasyProofRulesV1.Summary(proof));
            StringAssert.Contains("UNVERIFIED", FantasyProofRulesV1.Summary(proof));
        }
        [Test] public void ComponentCountsNeverApproveAppearanceOrGameplay()
        {
            string summary = FantasyProofRulesV1.Summary(Ready());
            StringAssert.Contains("owner review", summary); Assert.False(summary.Contains("PASS"));
        }
        [Test] public void SessionChoicesAndProofOverrideDoNotAlterPersistentChoice()
        {
            bool persistent = false;
            Assert.False(FantasyProofRulesV1.Selection(persistent, false, true, false, false));
            Assert.True(FantasyProofRulesV1.Selection(persistent, true, true, false, false));
            Assert.False(FantasyProofRulesV1.Selection(persistent, true, false, false, false));
            Assert.True(FantasyProofRulesV1.Selection(persistent, true, false, true, false));
            Assert.False(FantasyProofRulesV1.Selection(persistent, true, false, false, false));
            Assert.False(persistent);
        }
        [Test] public void LeavingPreviewRestoresIndependentSavedChoice()
        {
            Assert.True(FantasyProofRulesV1.Selection(true, true, false, true, false));
            Assert.True(FantasyProofRulesV1.Selection(true, false, false, false, false));
            Assert.False(FantasyProofRulesV1.Selection(false, false, true, false, false));
        }
        [TestCase(@"Missing C:\Users\owner\My Project\model.fbx")]
        [TestCase("Missing /home/owner/My Project/model.fbx")]
        [TestCase("Missing /mnt/private-owner/file.txt")]
        [TestCase(@"Missing \\owner-server\share\file.txt")]
        [TestCase("Missing ~/owner-folder/file.txt")]
        public void ExportableReasonsRemoveLocalPaths(string message)
        {
            string safe = FantasyProofRulesV1.SafeText(message);
            Assert.False(safe.Contains("owner")); StringAssert.Contains("[local path removed]", safe);
        }
        [Test] public void RelativeAssetReferencesRemainUseful()
        {
            const string message = "Missing Assets/NeonSyndicate/Art/LiveCast/Skin.png";
            Assert.AreEqual(message, FantasyProofRulesV1.SafeText(message));
        }
        [Test, Category("NeedsEngine")]
        public void MissingArtPreviewAndComparisonDoNotWriteExistingAppearancePreferences()
        {
            const string heartKey = "NSLV_heart_fr01", livingKey = "NSLV_heart_fr02a";
            bool hadHeart = UnityEngine.PlayerPrefs.HasKey(heartKey), hadLiving = UnityEngine.PlayerPrefs.HasKey(livingKey);
            int heartValue = UnityEngine.PlayerPrefs.GetInt(heartKey, 0), livingValue = UnityEngine.PlayerPrefs.GetInt(livingKey, 0);
            UnityEngine.GameObject owner = new UnityEngine.GameObject("Fantasy preview no-write acceptance");
            try
            {
                HeartPresentationFR01 presentation = new HeartPresentationFR01(null, owner.transform);
                presentation.BeginFantasyPreviewV1();
                Assert.True(presentation.FantasyPreviewV1); Assert.True(presentation.Selected); Assert.True(presentation.LivingSelected);
                presentation.Toggle(); presentation.ToggleLiving();
                bool selected = presentation.Selected, living = presentation.LivingSelected;
                presentation.BeginProofComparisonV1(false);
                Assert.True(presentation.Selected); Assert.True(presentation.LivingSelected);
                presentation.EndProofComparisonV1();
                Assert.AreEqual(selected, presentation.Selected); Assert.AreEqual(living, presentation.LivingSelected);
                presentation.EndFantasyPreviewV1(); Assert.False(presentation.FantasyPreviewV1);
                Assert.AreEqual(hadHeart, UnityEngine.PlayerPrefs.HasKey(heartKey)); Assert.AreEqual(hadLiving, UnityEngine.PlayerPrefs.HasKey(livingKey));
                Assert.AreEqual(heartValue, UnityEngine.PlayerPrefs.GetInt(heartKey, 0)); Assert.AreEqual(livingValue, UnityEngine.PlayerPrefs.GetInt(livingKey, 0));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test] public void DiagnosticTextIsBounded()
        {
            Assert.AreEqual(1200, FantasyProofRulesV1.SafeText(new string('x', 1500)).Length);
        }
    }
}
