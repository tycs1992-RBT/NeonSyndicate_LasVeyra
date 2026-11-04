using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class SurfaceWorldWR02Tests
    {
        [TestCase(-480f, -160f, true)]
        [TestCase(-480.01f, 0f, false)]
        [TestCase(319.99f, 319.99f, true)]
        [TestCase(320f, 0f, false)]
        [TestCase(0f, 320f, false)]
        [TestCase(0f, -160.01f, false)]
        [TestCase(0f, 0f, true)]
        [TestCase(float.NaN, 0f, false)]
        [TestCase(0f, float.PositiveInfinity, false)]
        public void BoundedSourceChunksNeverExpandTheWholeWorld(float x, float z, bool expected)
        { Assert.That(HeartPlanFR01.SurfaceRegionWR02(true, new Vector3(x, 0, z)), Is.EqualTo(expected)); }

        [Test]
        public void OlderRevisionsAndMissingLayoutAlwaysPreserve()
        {
            Assert.That(HeartPlanFR01.SurfaceRegionWR02(false, Vector3.zero), Is.False);
            Assert.That(HeartPlanFR01.SurfaceRegionWR02((CityLayout)null, Vector3.zero), Is.False);
        }

        [TestCase("BuildBuilding")]
        [TestCase("BuildSignals")]
        [TestCase("BuildBridge")]
        [TestCase("BuildFrostTile")]
        [TestCase("BuildStuntRamps")]
        [TestCase("UnknownOwner")]
        public void UnknownOrProtectedVisualOwnersCannotHideEvenPaint(string owner)
        { Assert.That(HeartPlanFR01.SurfaceKindWR02(owner, true, false, false), Is.EqualTo(HeartSurfaceKindWR02.Preserve)); }

        [TestCase("BuildRoadEdge", HeartSurfaceKindWR02.Causeway)]
        [TestCase("BuildIntersection", HeartSurfaceKindWR02.Causeway)]
        [TestCase("BuildGround", HeartSurfaceKindWR02.Ground)]
        [TestCase("BuildRealmLandscape04OB", HeartSurfaceKindWR02.Causeway)]
        [TestCase("EmitRealmSolid04OB", HeartSurfaceKindWR02.Strata)]
        [TestCase("BuildPowerLines", HeartSurfaceKindWR02.Bark)]
        [TestCase("Wire", HeartSurfaceKindWR02.Wire)]
        [TestCase("SignOnBuilding", HeartSurfaceKindWR02.Bronze)]
        [TestCase("WR02/tree-canopy", HeartSurfaceKindWR02.Canopy)]
        [TestCase("WR02/tree-trunk", HeartSurfaceKindWR02.Bark)]
        public void AuditedOwnersHaveAnExplicitPresentationClass(string owner, HeartSurfaceKindWR02 kind)
        { Assert.That(HeartPlanFR01.SurfaceKindWR02(owner, false, false, false), Is.EqualTo(kind)); }

        [TestCase("SolidLine")]
        [TestCase("DashedLine")]
        [TestCase("StopAndCrosswalk")]
        [TestCase("ParkingLines")]
        public void OriginalMarkingEmittersAreOmittedOnlyFromTheCandidate(string owner)
        { Assert.That(HeartPlanFR01.SurfaceKindWR02(owner, true, false, false), Is.EqualTo(HeartSurfaceKindWR02.Markings)); }

        [Test]
        public void VegetationAndCurbRolesRemainDistinct()
        {
            Assert.That(HeartPlanFR01.SurfaceKindWR02("BuildMedian", false, false, true), Is.EqualTo(HeartSurfaceKindWR02.Ground));
            Assert.That(HeartPlanFR01.SurfaceKindWR02("BuildMedian", false, true, false), Is.EqualTo(HeartSurfaceKindWR02.Strata));
        }

        [TestCase(false, true, false, false, true, false)]
        [TestCase(true, false, false, false, true, false)]
        [TestCase(true, true, true, false, true, false)]
        [TestCase(true, true, false, true, true, false)]
        [TestCase(true, true, false, false, false, false)]
        [TestCase(true, true, false, false, true, true)]
        public void OnlyTheTemporaryCompletePreparedSessionCanExchange(bool session, bool selected, bool compare, bool suspended, bool ready, bool expected)
        { Assert.That(HeartPlanFR01.SurfaceActiveWR02(session, selected, compare, suspended, ready), Is.EqualTo(expected)); }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void EverySeedProducesAStableLegalCanopyVariant(int seed)
        {
            int index = HeartPlanFR01.CanopyVariantWR02(seed);
            Assert.That(index, Is.InRange(0, 2)); Assert.That(HeartPlanFR01.CanopyVariantWR02(seed), Is.EqualTo(index));
        }

        [Test]
        public void NaturalFitKeepsAllEightCrownEnvelopeCornersExactly()
        {
            Bounds original = new Bounds(new Vector3(-235, 14, 158), new Vector3(11, 8, 9));
            Bounds prefab = new Bounds(new Vector3(0, .5f, 0), Vector3.one);
            Vector3 at, scale; Assert.That(HeartPlanFR01.CanopyFitWR02(prefab, original, out at, out scale), Is.True);
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = new Vector3(x, y, z);
                Vector3 mapped = at + Vector3.Scale(prefab.center + Vector3.Scale(prefab.extents, corner), scale);
                Assert.That((mapped - (original.center + Vector3.Scale(original.extents, corner))).sqrMagnitude, Is.LessThan(.00001f));
            }
        }

        [Test]
        public void InvalidOrOversizedNaturalFramesAreRefused()
        {
            Vector3 at, scale; Bounds unit = new Bounds(Vector3.zero, Vector3.one);
            Assert.That(HeartPlanFR01.CanopyFitWR02(new Bounds(Vector3.zero, Vector3.zero), unit, out at, out scale), Is.False);
            Assert.That(HeartPlanFR01.CanopyFitWR02(unit, new Bounds(Vector3.zero, new Vector3(100, 8, 8)), out at, out scale), Is.False);
            Assert.That(HeartPlanFR01.CanopyFitWR02(unit, new Bounds(new Vector3(float.NaN, 0, 0), Vector3.one), out at, out scale), Is.False);
        }

        [Test]
        public void SourceKeysSeparateChunksOwnersAndSlotsWithoutNativeInstanceIds()
        {
            string key = HeartPlanFR01.SurfaceKeyWR02("BuildRoadEdge", Vector3.zero, 0);
            Assert.That(HeartPlanFR01.SurfaceKeyWR02("BuildRoadEdge", new Vector3(20, 0, 20), 0), Is.EqualTo(key));
            Assert.That(HeartPlanFR01.SurfaceKeyWR02("BuildRoadEdge", new Vector3(-.01f, 0, 0), 0), Is.Not.EqualTo(key));
            Assert.That(HeartPlanFR01.SurfaceKeyWR02("BuildGround", Vector3.zero, 0), Is.Not.EqualTo(key));
            Assert.That(HeartPlanFR01.SurfaceKeyWR02("BuildRoadEdge", Vector3.zero, 1), Is.Not.EqualTo(key));
        }

        [Test]
        public void CliffCatalogRejectsUnknownOrChangedEnvelopes()
        {
            Bounds exact = new Bounds((new Vector3(-5.63815f, 0, -18.71135f) + new Vector3(7.29f, 34, 18.71135f)) * .5f, new Vector3(12.92815f, 34, 37.4227f));
            Assert.That(HeartPlanFR01.CliffCatalogBoundsWR02("Cliff62346", exact), Is.True);
            Assert.That(HeartPlanFR01.CliffCatalogBoundsWR02("CliffUnknown", exact), Is.False);
            exact.center += Vector3.right;
            Assert.That(HeartPlanFR01.CliffCatalogBoundsWR02("Cliff62346", exact), Is.False);
        }

        [Test]
        public void ExportedRecordCopiesCannotBeChangedByTheNextToggle()
        {
            HeartSurfaceRecordWR02 original = new HeartSurfaceRecordWR02 { stableId = "source", active = true, bounds = new Bounds(Vector3.one, Vector3.one) };
            HeartSurfaceRecordWR02 captured = original.Copy(); original.active = false; original.bounds = default(Bounds); original.reason = "later failure";
            Assert.That(captured.active, Is.True); Assert.That(captured.bounds.center, Is.EqualTo(Vector3.one)); Assert.That(captured.reason, Is.Empty);
        }

        private static FantasyProofSnapshotV1 Ready()
        {
            FantasyProofSnapshotV1 proof = new FantasyProofSnapshotV1(); proof.fr01.active = true; proof.fr02a.active = true;
            proof.worldRewrite.selected = true; proof.worldRewrite.active = 1; proof.surfaceWR02.selected = true; proof.surfaceWR02.prepared = true; proof.surfaceWR02.active = true;
            return proof;
        }

        [Test]
        public void ForestAndCausewayViewsRequireTheirOwnRenderedCandidates()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.worldRewrite.importedCrownFrustumRenderers = 4;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("CAUSEWAY", proof), Is.False);
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("FOREST_RUINS", proof), Is.False);
            proof.surfaceWR02.causewayFrustumRenderers = 1; proof.surfaceWR02.canopyFrustumRenderers = 1;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("CAUSEWAY", proof), Is.True);
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("FOREST_RUINS", proof), Is.True);
            proof.surfaceWR02.refused = 1;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("FOREST_RUINS", proof), Is.False);
        }

        [Test]
        public void SurfaceViewsCannotBypassTheExistingSkylineGateAtHome()
        {
            FantasyProofSnapshotV1 proof = Ready(); proof.surfaceWR02.frustumRenderers = 8; proof.surfaceWR02.canopyFrustumRenderers = 3;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("HOME", proof), Is.False);
            proof.worldRewrite.importedSkylineFrustumRenderers = 1;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("HOME", proof), Is.True);
            proof.surfaceWR02.active = false;
            Assert.That(FantasyProofRulesV1.SurfaceViewObservedWR02("HOME", proof), Is.False);
        }
    }
}
