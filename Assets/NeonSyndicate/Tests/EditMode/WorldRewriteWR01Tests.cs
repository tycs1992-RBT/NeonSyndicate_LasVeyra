using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class WorldRewriteWR01Tests
    {
        private CityLayout _layout;
        private IReadOnlyList<VenueBinding04C> _bindings;

        [OneTimeSetUp]
        public void ExistingWorld()
        {
            _layout = new CityLayout(61724); _layout.Generate();
            _bindings = InhabitedCityBindings04C.Select(_layout, InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(_layout.ApplyProfile04O(CityRevision04O.GothicCivicH5, _bindings));
        }

        private VenueBinding04C Binding(string id)
        { foreach (VenueBinding04C binding in _bindings) if (binding.Definition.Id == id) return binding; Assert.Fail(id); return null; }

        [TestCase("archive", 479130, "WRArchiveCrown", -6f, 122f, 0f, 48f)]
        [TestCase("office", 472154, "WROfficeCrown", -65f, -65f, 270f, 30f)]
        public void CivicPlanUsesTheActualSavedServiceBinding(string service, int seed, string family, float x, float z, float yaw, float depth)
        {
            VenueBinding04C binding = Binding(service); Building original = binding.Building;
            RectF footprint = original.Footprint; HeartRewritePlanWR01 plan; string reason;
            Assert.True(HeartPlanFR01.TryRewritePlanWR01(_layout, binding, out plan, out reason), reason);
            Assert.AreEqual(seed, plan.Seed); Assert.AreEqual(family, plan.Family);
            Assert.AreEqual("revision:10/venue:" + service + "/seed:" + seed + "/crown", plan.StableId);
            Assert.That(plan.Origin.x, Is.EqualTo(x).Within(.001)); Assert.That(plan.Origin.z, Is.EqualTo(z).Within(.001));
            Assert.That(plan.Yaw, Is.EqualTo(yaw).Within(.001)); Assert.That(plan.OriginalEnvelope.size.z, Is.EqualTo(depth).Within(.001));
            Assert.That(plan.PortalSize.x, Is.GreaterThanOrEqualTo(2.8f));
            Assert.AreSame(original, binding.Building); Assert.AreEqual(footprint, original.Footprint);
            StringAssert.Contains("fixed-asset service shell", plan.CollisionOwner);
        }

        [TestCase("lofts")][TestCase("forge")][TestCase("pressure")][TestCase("clinic")]
        public void OtherProtectedServicesNeverBecomeCrownCandidates(string id)
        {
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(_layout, Binding(id), out plan, out reason));
            Assert.IsNull(plan); StringAssert.Contains("outside", reason);
        }

        [Test] public void MissingBindingOrWrongWorldRemainsPreserved()
        {
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(_layout, null, out plan, out reason));
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(null, Binding("archive"), out plan, out reason));
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(new CityLayout(61724), Binding("archive"), out plan, out reason));
        }

        [TestCase("width")][TestCase("depth")][TestCase("receipt")][TestCase("origin")][TestCase("yaw")]
        public void ChangedPhysicalContractIsRefusedInsteadOfStretching(string field)
        {
            VenueBinding04C source = Binding("archive");
            VenueBinding04C binding = new VenueBinding04C
            {
                Definition = source.Definition, Origin = source.Origin, Yaw = source.Yaw, Width = source.Width, Depth = source.Depth,
                Building = new Building { Seed = source.Building.Seed, Height = source.Building.Height, Footprint = source.Building.Footprint }
            };
            if (field == "width") binding.Width += 1;
            if (field == "depth") binding.Depth += 1;
            if (field == "receipt") binding.Building.RoofPad04OB = true;
            if (field == "origin") binding.Origin = new Vector3(float.NaN, 0, 0);
            if (field == "yaw") binding.Yaw = float.PositiveInfinity;
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(_layout, binding, out plan, out reason));
            StringAssert.Contains("review", reason); Assert.IsNull(plan);
        }

        [TestCase("slate_roof", true)][TestCase("mineral_pier", true)]
        [TestCase("reliquary_door", false)][TestCase("cluster_column", false)]
        [TestCase("portal_bay", false)][TestCase("ribbed_vault", false)][TestCase("flying_buttress", false)]
        public void SourceSeparationNeverCapturesDoorsVaultsOrSupportingBays(string model, bool expected)
        { Assert.AreEqual(expected, HeartPlanFR01.CrownPartWR01(model)); }

        private static Bounds Authored(string family)
        {
            Vector3 min = family == "WRArchiveCrown" ? new Vector3(-15.020001f, 8.329999f, -.725f) : new Vector3(-15.020001f, 8.329999f, -.95f);
            Vector3 max = family == "WRArchiveCrown" ? new Vector3(15.020001f, 35.099998f, 48.479999f) : new Vector3(15.020001f, 33.200001f, 30.429998f);
            return new Bounds((min + max) * .5f, max - min);
        }

        [TestCase("WRArchiveCrown")][TestCase("WROfficeCrown")]
        public void ExactAuthoredFramesPreserveGroundPortalHeadroom(string family)
        {
            Bounds bounds = Authored(family); string reason;
            Assert.True(HeartPlanFR01.RewriteArtBoundsWR01(family, bounds, out reason), reason);
            Assert.That(bounds.min.y, Is.GreaterThan(8f));
            Bounds moved = bounds; moved.center += Vector3.forward;
            Assert.False(HeartPlanFR01.RewriteArtBoundsWR01(family, moved, out reason));
            Bounds stretched = bounds; stretched.size = Vector3.Scale(bounds.size, new Vector3(1.2f, 1, 1));
            Assert.False(HeartPlanFR01.RewriteArtBoundsWR01(family, stretched, out reason));
        }

        [Test] public void UnknownAndNonfiniteArtNeverQualifies()
        {
            string reason;
            Assert.False(HeartPlanFR01.RewriteArtBoundsWR01("OtherTower", Authored("WRArchiveCrown"), out reason));
            Assert.False(HeartPlanFR01.RewriteArtBoundsWR01("WRArchiveCrown", new Bounds(Vector3.zero, new Vector3(float.NaN, 20, 20)), out reason));
            Assert.False(HeartPlanFR01.RewriteArtBoundsWR01("WRArchiveCrown", new Bounds(Vector3.zero, Vector3.zero), out reason));
        }

        [TestCase(false, false, false, false)][TestCase(false, true, false, false)]
        [TestCase(true, false, false, true)][TestCase(true, true, false, true)][TestCase(true, true, true, false)]
        public void RewriteRequiresTemporaryPreviewAndRestoresLegacyComparison(bool preview, bool comparison, bool legacy, bool expected)
        { Assert.AreEqual(expected, HeartPlanFR01.RewriteSelectedWR01(preview, comparison, legacy)); }

        [TestCase(809556, 162.37482f)][TestCase(32874, 128.66837f)][TestCase(53496, 162.56671f)]
        public void DominantSkylineUsesExactExistingReceiptFootprintAndHeight(int seed, float height)
        {
            Building source = null;
            foreach (Block block in _layout.Blocks) foreach (Lot lot in block.Lots) foreach (Building building in lot.Buildings)
                if (building.Seed == seed) source = building;
            Assert.IsNotNull(source); RectF before = source.Footprint;
            HeartRewritePlanWR01 plan; string reason;
            Assert.True(HeartPlanFR01.TryRewriteBuildingPlanWR01(_layout, source, out plan, out reason), reason);
            Assert.AreEqual("WRSkyline" + seed, plan.Family); Assert.AreEqual(source.Footprint.Center(CityLayout.CurbHeight), plan.Origin);
            Assert.That(source.Height, Is.EqualTo(height).Within(.001)); Assert.AreEqual(before, source.Footprint);
            StringAssert.Contains("receipt", plan.PortalContract); StringAssert.Contains("unit scale", plan.FitPolicy);
            Building changed = new Building { Seed = seed, Footprint = before, Height = height + 1, GothicFamily04OB = 9 };
            Assert.False(HeartPlanFR01.TryRewriteBuildingPlanWR01(_layout, changed, out plan, out reason));
        }

        [Test] public void UnknownSkylineFramesRemainPreserved()
        {
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewriteBuildingPlanWR01(_layout,
                new Building { Seed = 898990, Footprint = new RectF(279.97778f, 311.9797f, 319.97778f, 345.9797f), Height = 135.98871f, GothicFamily04OB = 9 },
                out plan, out reason));
            Assert.IsNull(plan);
        }

        [TestCase("width", float.NaN)][TestCase("depth", float.PositiveInfinity)][TestCase("height", float.NaN)]
        public void NonfiniteCivicDimensionsCannotBypassToleranceGuards(string field, float value)
        {
            VenueBinding04C source = Binding("archive");
            VenueBinding04C binding = new VenueBinding04C
            {
                Definition = source.Definition, Origin = source.Origin, Width = source.Width, Depth = source.Depth, Yaw = source.Yaw,
                Building = new Building { Seed = source.Building.Seed, Footprint = source.Building.Footprint, Height = source.Building.Height }
            };
            if (field == "width") binding.Width = value;
            if (field == "depth") binding.Depth = value;
            if (field == "height") binding.Building.Height = value;
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewritePlanWR01(_layout, binding, out plan, out reason)); Assert.IsNull(plan);
        }

        [TestCase("x0", float.NaN)][TestCase("x1", float.PositiveInfinity)]
        [TestCase("z0", float.NaN)][TestCase("z1", float.NegativeInfinity)][TestCase("height", float.NaN)]
        public void NonfiniteSkylineDimensionsCannotBypassExactReceiptGuards(string field, float value)
        {
            RectF rectangle = new RectF(68.09897f, 367.45792f, 113.100075f, 388.64154f);
            if (field == "x0") rectangle = new RectF(value, rectangle.Z0, rectangle.X1, rectangle.Z1);
            if (field == "x1") rectangle = new RectF(rectangle.X0, rectangle.Z0, value, rectangle.Z1);
            if (field == "z0") rectangle = new RectF(rectangle.X0, value, rectangle.X1, rectangle.Z1);
            if (field == "z1") rectangle = new RectF(rectangle.X0, rectangle.Z0, rectangle.X1, value);
            Building building = new Building { Seed = 809556, Footprint = rectangle, Height = field == "height" ? value : 162.37482f, GothicFamily04OB = 9 };
            HeartRewritePlanWR01 plan; string reason;
            Assert.False(HeartPlanFR01.TryRewriteBuildingPlanWR01(_layout, building, out plan, out reason)); Assert.IsNull(plan);
        }

        [TestCase(false, true, true, 4, 2, false)]
        [TestCase(true, false, true, 4, 2, false)]
        [TestCase(true, true, false, 4, 2, false)]
        [TestCase(true, true, true, 0, 2, false)]
        [TestCase(true, true, true, 4, 0, false)]
        [TestCase(true, true, true, 4, 2, true)]
        public void PhysicalWitnessesCannotCertifyImportedArtInTheCamera(bool present, bool active, bool valid, int importedRenderers, int importedFrustum, bool expected)
        { Assert.AreEqual(expected, HeartPlanFR01.RewriteImportedEvidenceWR01(present, active, valid, importedRenderers, importedFrustum)); }
    }
}
