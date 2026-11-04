using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Engine-free L1 grammar, exact clearing and real revision-10 placement safety.</summary>
    public sealed class LiveWildL1Tests
    {
        [TestCase("none", LiveWorldCollisionL1.None)]
        [TestCase("BOX", LiveWorldCollisionL1.Box)]
        [TestCase("Mesh", LiveWorldCollisionL1.Mesh)]
        public void OptInGrammar(string text, LiveWorldCollisionL1 expected)
        {
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("Test", "collide " + text + "\nclear 12\nnear Wild:Mirrorwell Oasis offset 0 34\n");
            Assert.That(spec.Collision, Is.EqualTo(expected));
            Assert.That(spec.ClearRadius, Is.EqualTo(12f));
            Assert.That(spec.Notes, Is.Empty);
            Assert.That(spec.Orders[0].Place, Is.EqualTo("Wild:Mirrorwell Oasis"));
        }

        [Test]
        public void OldFilesKeepNoCollisionOrClear()
        {
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("Old", "near Venue:lofts.exit offset 18 60");
            Assert.That(spec.Collision, Is.EqualTo(LiveWorldCollisionL1.None));
            Assert.That(spec.ClearRadius, Is.Zero);
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Wild:"), Is.Not.Null);
        }

        [TestCase("NaN")][TestCase("Infinity")][TestCase("-3")][TestCase("banana")]
        public void BadClearIsSafe(string value)
        {
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("Test", "clear " + value + "\ncollide convex\nnear Wild:x");
            Assert.That(spec.ClearRadius, Is.Zero);
            Assert.That(spec.Collision, Is.EqualTo(LiveWorldCollisionL1.None));
            Assert.That(spec.Notes.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ClearIsBounded()
        {
            Assert.That(LiveWorldPlanH8S.Parse("Test", "clear 200\nnear Wild:x").ClearRadius, Is.EqualTo(40f));
        }

        [Test]
        public void NamesAndIdsUseOneExistingPlanAndRejectAmbiguity()
        {
            WildsPlan plan = new WildsPlan();
            WildSite original = new WildSite { Name = "Mirrorwell Oasis", Id = "wild-oasis-6" };
            plan.Sites.Add(original);
            WildSite found;
            Assert.That(LiveWildRulesL1.TrySite(plan, "wIlD:mIrRoRwElL oAsIs", out found) && found == original, Is.True);
            Assert.That(LiveWildRulesL1.TrySite(plan, "Wild:WILD-OASIS-6", out found) && found == original, Is.True);
            Assert.That(LiveWildRulesL1.TrySite(plan, "Wild:Mirrorwell", out found), Is.False);
            Assert.That(LiveWildRulesL1.TrySite(null, "Wild:Mirrorwell Oasis", out found), Is.False);
            plan.Sites.Add(new WildSite { Name = "Mirrorwell Oasis", Id = "other" });
            Assert.That(LiveWildRulesL1.TrySite(plan, "Wild:Mirrorwell Oasis", out found), Is.False);
        }

        private static WildPiece Rock(float x, float z, int site = -1)
        {
            return new WildPiece { Kind = WildPieceKind.Boulder, Site = site, Position = new Vector3(x, 0f, z), Size = new Vector3(3f, 2f, 3f), Seed = 19, Solid = true };
        }

        [Test]
        public void ClearingUsesActualGeometryAndNeverDeletesSeededPlanOrSiteWater()
        {
            WildsPlan plan = new WildsPlan();
            plan.Pieces.Add(Rock(0f, 0f));
            plan.Pieces.Add(Rock(1f, 1f, 0));
            plan.Pieces.Add(Rock(30f, 0f));
            WildPiece water = Rock(0f, 0f); water.Kind = WildPieceKind.Pond; plan.Pieces.Add(water);
            WildPiece ground = Rock(0f, 0f); ground.Kind = WildPieceKind.Swell; plan.Pieces.Add(ground);
            HashSet<int> selected = LiveWildRulesL1.SelectClear(plan, new Vector3(-4f, 0f, -4f), new Vector3(4f, 12f, 4f), 40f);
            Assert.That(selected, Is.EquivalentTo(new[] { 0 }), "same radius does not clear things outside the structure footprint");
            Assert.That(plan.Pieces.Count, Is.EqualTo(5));
            Assert.That(plan.Pieces[0].Position, Is.EqualTo(Vector3.zero));
            Assert.That(LiveWildRulesL1.SelectClear(plan, Vector3.zero, Vector3.one, float.NaN), Is.Empty);
        }

        [Test]
        public void CrossingRockCountsEvenWithItsCentreOutsideTheFootprint()
        {
            WildsPlan plan = new WildsPlan();
            plan.Pieces.Add(Rock(2f, 0f));
            HashSet<int> selected = LiveWildRulesL1.SelectClear(plan, new Vector3(-1f, 0f, -1f), new Vector3(1f, 8f, 1f), 1f);
            Assert.That(selected.Contains(0), Is.True);
        }

        [Test]
        public void ShippedPavilionFitsRealPlanWithoutMovingSitesOrRewards()
        {
            CityLayout layout = new CityLayout(61724);
            layout.Generate();
            layout.ApplyProfile04O(CityRevision04O.GothicCivicH5, InhabitedCityBindings04C.Select(layout, InhabitedCityBindings04CTests.ReadCatalog()));
            WildsPlan plan = WildsLayout.Plan(layout, null);
            WildSite oasis;
            Assert.That(LiveWildRulesL1.TrySite(plan, "Wild:Mirrorwell Oasis", out oasis), Is.True);
            Assert.That(plan.Sites.Count, Is.EqualTo(17));
            Assert.That(oasis.Id, Is.EqualTo("wild-oasis-6"));
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("OasisPavilion", File.ReadAllText("Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion/placement.txt"));
            Assert.That(spec.Notes, Is.Empty);
            Assert.That(spec.Orders.Count, Is.EqualTo(1));
            Assert.That(spec.Collision, Is.EqualTo(LiveWorldCollisionL1.Mesh));
            LiveWorldOrderH8S order = spec.Orders[0];
            Vector3 at = oasis.Center + order.Offset;
            float yaw = LiveWorldPlannerH8S.FacingYaw(at, oasis.Center, order.Turn);
            // Conservative outward rounding of re-imported FBX bounds in Unity axes; checked by asset validator.
            Vector3 localMin = new Vector3(-6.42f, 0f, -6.72f), localMax = new Vector3(6.42f, 12.64f, 6.72f);
            Vector3 min, max;
            WorldRouteH8S.YawedBounds(localMin, localMax, yaw, at, out min, out max);
            Assert.That(LiveWorldPlannerH8S.Refusal(at, yaw, 1f, localMin, localMax, LiveWorldSafetyH8S.AreaRules(layout, oasis.Center, null, null, null, null, null), layout), Is.Null);
            Assert.That(LiveWildRulesL1.SiteConflict(plan, min, max), Is.Null);
            Assert.That(WildsLayout.SurfaceAt(plan, at.x, at.z), Is.Zero, "existing level ground at the footprint");
            Assert.That(MathUtil.FlatDistance(at, oasis.Center) - 6.42f, Is.LessThan(oasis.Radius), "walking up discovers the original site");
            Assert.That(WildsSystem.CashPerSite, Is.EqualTo(300));
            Assert.That(WildsSystem.LumenPerSite, Is.EqualTo(120));
            TestContext.WriteLine("Mirrorwell " + oasis.Id + " anchor " + oasis.Center + "; pavilion " + at + "; yaw " + yaw);
        }
    }
}
