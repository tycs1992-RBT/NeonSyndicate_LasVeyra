using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>V0 uses actual catalog places and restricts import correction to known visual-only source folders.</summary>
    public sealed class VisibleRealityV0Tests
    {
        [TestCase("Assets/NeonSyndicate/Art/WorldFoundationH8R/ProcessionalVault.fbx")]
        [TestCase("Assets/NeonSyndicate/Art/ProductionH8S/Props/Anvil.fbx")]
        [TestCase("Assets/NeonSyndicate/Art/LiveWorld/OwnerTower/Tower.fbx")]
        public void StaticWorldSourcesReceiveImportCorrection(string path)
        {
            Assert.That(LiveWorldImportRulesV0.CanConfigure(path), Is.True);
        }

        [TestCase("Assets/NeonSyndicate/Art/ProductionH9/Models/MalikVeyrH9.fbx")]
        [TestCase("Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.fbx")]
        [TestCase("Assets/NeonSyndicate/Art/LiveWorld/../../ProductionH9/Hero.fbx")]
        [TestCase("Assets/Other/Thing.fbx")]
        [TestCase(null)]
        public void CorrectionNeverStripsCharacterOrMotionImports(string path)
        {
            Assert.That(LiveWorldImportRulesV0.CanConfigure(path), Is.False);
        }

        [Test]
        public void HydrationDistinguishesAPointerFromActualModelBytes()
        {
            Assert.That(LiveWorldImportRulesV0.IsPointer("version https://git-lfs.github.com/spec/v1\noid sha256:123"), Is.True);
            Assert.That(LiveWorldImportRulesV0.IsPointer("Kaydara FBX Binary  \0"), Is.False);
            Assert.That(LiveWorldImportRulesV0.IsPointer(null), Is.False);
        }

        [Test]
        public void NearestPlaceComesFromTheRealCatalogWithoutChangingDiscovery()
        {
            WildSite near = new WildSite { Id = "near", Name = "Near", Center = new Vector3(12f, 6f, 0f), Index = 2 };
            WildSite far = new WildSite { Id = "far", Name = "Far", Center = new Vector3(500f, 0f, 0f), Index = 3 };
            List<WildSite> sites = new List<WildSite> { far, null, new WildSite { Id = "bad", Center = new Vector3(float.NaN, 0f, 0f) }, near };
            Assert.That(VisibleRealityRulesV0.NearestWild(sites, Vector3.zero), Is.SameAs(near));
            Assert.That(sites.Count, Is.EqualTo(4));
            Assert.That(near.Id, Is.EqualTo("near"));
            Assert.That(near.Index, Is.EqualTo(2));
            Assert.That(VisibleRealityRulesV0.NearestWild(sites, new Vector3(490f, 0f, 0f)), Is.SameAs(far));
        }

        [Test]
        public void MissingOrInvalidCatalogHasNoInventedDestination()
        {
            Assert.That(VisibleRealityRulesV0.NearestWild(null, Vector3.zero), Is.Null);
            Assert.That(VisibleRealityRulesV0.NearestWild(new List<WildSite> { new WildSite(), null }, Vector3.zero), Is.Null);
            Assert.That(VisibleRealityRulesV0.NearestWild(new List<WildSite>(), new Vector3(float.NaN, 0f, 0f)), Is.Null);
        }

        [Test]
        public void EarlyStoryHintsNeverAdvertiseUnearnedFlightOrLeap()
        {
            string early = VisibleRealityRulesV0.Ability(true, false, false);
            Assert.That(early, Does.StartWith("Surge earned"));
            Assert.That(early, Does.Not.Contain("Flight earned"));
            Assert.That(early, Does.Not.Contain("Super Leap earned"));
            Assert.That(VisibleRealityRulesV0.Ability(true, true, false), Does.StartWith("Super Leap earned"));
            Assert.That(VisibleRealityRulesV0.Ability(true, true, true), Does.StartWith("Flight earned"));
            Assert.That(VisibleRealityRulesV0.Ability(false, false, false), Does.Contain("not ready"));
        }

        [Test]
        public void AssetFailuresAreShownOncePerCategoryAndRetainedUntilSessionReset()
        {
            LiveCastNotice.ResetStatics();
            try
            {
                LiveCastNotice.AssetProblem("Malik", "Git LFS pointer", "Pull Git LFS.");
                for (int i = 0; i < 100; i++) LiveCastNotice.AssetProblem("Malik", "Repeated binding refusal", "Rebuild.");
                LiveCastNotice.AssetProblem("World art", "No imported model", "Prepare live world art.");
                Assert.That(LiveCastNotice.Problems.Count, Is.EqualTo(2));
                Assert.That(LiveCastNotice.Problems[0], Does.Contain("Git LFS pointer"));
                Assert.That(LiveCastNotice.Problems[1], Does.Contain("Prepare live world art"));
                LiveCastNotice.CompareLegacy = true;
                LiveCastNotice.ResetStatics();
                Assert.That(LiveCastNotice.Problems.Count, Is.Zero);
                Assert.That(LiveCastNotice.CompareLegacy, Is.False);
            }
            finally { LiveCastNotice.ResetStatics(); HudOverlays.ResetStatics(); }
        }
    }
}
