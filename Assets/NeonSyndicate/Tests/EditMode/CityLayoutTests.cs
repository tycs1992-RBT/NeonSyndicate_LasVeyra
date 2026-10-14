using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>The procedural city for the default seed: everything is generated, finite, inside the map and consistent.</summary>
    public sealed class CityLayoutTests
    {
        [Test]
        public void TestsUseTheGameSeed()
        {
            Assert.That(Game.Seed, Is.EqualTo(TestData.DefaultSeed), "Game.Seed changed: update TestData.DefaultSeed and the golden views");
        }

        [Test]
        public void GenerationProducesEveryKindOfFeature()
        {
            CityLayout L = TestData.City;
            Assert.That(L.Nodes.Count, Is.GreaterThan(0), "road nodes");
            Assert.That(L.Edges.Count, Is.GreaterThan(0), "roads");
            Assert.That(L.Blocks.Count, Is.GreaterThan(0), "blocks");
            Assert.That(L.Lanes.Count, Is.GreaterThan(0), "traffic lanes");
            Assert.That(L.WalkNodes.Count, Is.GreaterThan(0), "sidewalk nodes");
            Assert.That(L.WalkLinks.Count, Is.GreaterThan(0), "sidewalk links");
            Assert.That(L.Pois.Count, Is.GreaterThan(0), "places");
            Assert.That(L.Sites.Count, Is.GreaterThan(0), "desert sites");
            Assert.That(L.DesertLots.Count, Is.GreaterThan(0), "desert lots");
            Assert.That(L.Props.Count, Is.GreaterThan(0), "street props");
            Assert.That(L.ParkingSpots.Count, Is.GreaterThan(0), "parking spots");
            Assert.That(L.ParkingYaws.Count, Is.EqualTo(L.ParkingSpots.Count), "one heading per parking spot");
            Assert.That(L.StuntJumps.Count, Is.GreaterThan(0), "stunt jumps");
            Assert.That(L.PickupSpots.Count, Is.GreaterThan(0), "pickups");
            Assert.That(L.DataChipSpots.Count, Is.EqualTo(50), "fifty data chips");
            Assert.That(L.CrystalShardSpots.Count, Is.EqualTo(20), "twenty crystal shards");
            Assert.That(L.SpellStoneSpots.Count, Is.EqualTo(10), "ten spell stones");
            Assert.That(L.CreatureSpots.Count, Is.EqualTo(10), "ten Echo Relic spots");
            int buildings = 0;
            foreach (Block b in L.Blocks) foreach (Lot lot in b.Lots) buildings += lot.Buildings.Count;
            Assert.That(buildings, Is.GreaterThan(100), "buildings");
        }

        [Test]
        public void EveryPositionIsFiniteAndInsideTheMap()
        {
            int bad = 0;
            Vector3 first = Vector3.zero;
            foreach (Vector3 p in TestData.AllPositions(TestData.City))
            {
                if (TestData.Finite(p) && TestData.InsideMap(p) && p.y > -CityLayout.CanalDepth - 1f && p.y < 300f) continue;
                if (bad == 0) first = p;
                bad++;
            }
            Assert.That(bad, Is.EqualTo(0), bad + " positions are not finite or lie outside the map, for example " + first);
        }

        [Test]
        public void EverySiteIsPresentOutInTheDesert()
        {
            CityLayout L = TestData.City;
            foreach (string key in new[] { "StarliteMotel", "Airstrip", "Canyon", "Quarry", "TrailerPark", "CrystalManor", "Blockade" })
            {
                SpecialSite s = L.Site(key);
                Assert.That(s, Is.Not.Null, "site " + key);
                Assert.That(string.IsNullOrEmpty(s.Name), Is.False, key + " has a name");
                Assert.That(s.Radius, Is.GreaterThan(0f), key + " radius");
                Assert.That(TestData.InsideMap(s.Position), Is.True, key + " inside the map");
                Assert.That(CityLayout.DistrictAt(s.Position), Is.EqualTo(District.QuartzBadlands), key + " lies in the desert");
            }
        }

        [Test]
        public void StoryPlacesExist()
        {
            CityLayout L = TestData.City;
            foreach (string name in new[] { "Veyra Lofts", "Jax Calder", "Nico Ash", "Dante Rook", "Mara Solane", "June Pyre", "Iris Moon", "Officer Valez", "Sable Knox", "Luma", "Battery Yard Scrap Co." })
                Assert.That(L.FindPoi(name), Is.Not.Null, "place '" + name + "'");
            foreach (PoiType t in new[] { PoiType.Safehouse, PoiType.Hospital, PoiType.Police, PoiType.GunShop, PoiType.Respray, PoiType.Diner, PoiType.ConvenienceStore, PoiType.Payphone })
                Assert.That(L.NearestPoi(Vector3.zero, t), Is.Not.Null, "at least one " + t);
        }

        [TestCase(0f, 0f, District.LasVeyraCore)]
        [TestCase(0f, 400f, District.GlassHeights)]
        [TestCase(0f, -450f, District.SunkenRow)]
        [TestCase(380f, 0f, District.MirageStrip)]
        [TestCase(-600f, 0f, District.OldSilver)]
        [TestCase(-600f, -600f, District.AshUnderpass)]
        [TestCase(-600f, 600f, District.GraveFrequency)]
        [TestCase(700f, 400f, District.SkyrailOrbit)]
        [TestCase(700f, -400f, District.BrassProjects)]
        [TestCase(1200f, 0f, District.QuartzBadlands)]
        [TestCase(0f, -1400f, District.QuartzBadlands)]
        public void DistrictAtMatchesTheDistrictMap(float x, float z, District expected)
        {
            Assert.That(CityLayout.DistrictAt(x, z), Is.EqualTo(expected));
            Assert.That(CityLayout.DistrictAt(new Vector3(x, 12f, z)), Is.EqualTo(expected), "height does not matter");
        }

        [Test]
        public void DistrictAtCoversTheWholeMapWithNamedDistricts()
        {
            HashSet<District> seen = new HashSet<District>();
            float h = CityLayout.MapHalf;
            for (float x = -h; x <= h; x += 40f)
            {
                for (float z = -h; z <= h; z += 40f)
                {
                    District d = CityLayout.DistrictAt(x, z);
                    Assert.That(System.Enum.IsDefined(typeof(District), d), Is.True, "district at " + x + ", " + z);
                    bool outside = Mathf.Abs(x) > CityLayout.CoreHalf + 25f || Mathf.Abs(z) > CityLayout.CoreHalf + 25f;
                    // Outside the core is wilderness: the Quartz Badlands desert, or the Frostline snowfields in the north-west.
                    if (outside) Assert.That(CityLayout.IsWilderness(d), Is.True, "outside the city at " + x + ", " + z);
                    else Assert.That(CityLayout.IsWilderness(d), Is.False, "inside the city at " + x + ", " + z);
                    seen.Add(d);
                }
            }
            Assert.That(seen.Count, Is.EqualTo(System.Enum.GetValues(typeof(District)).Length), "every district appears somewhere");
            HashSet<string> names = new HashSet<string>();
            foreach (District d in System.Enum.GetValues(typeof(District)))
            {
                string n = CityLayout.DistrictName(d);
                Assert.That(string.IsNullOrEmpty(n), Is.False, d + " has a name");
                Assert.That(names.Add(n), Is.True, "district names are unique (" + n + ")");
            }
        }

        [Test]
        public void BlocksSitInTheDistrictTheirCenterIsIn()
        {
            foreach (Block b in TestData.City.Blocks)
            {
                Assert.That(CityLayout.IsWilderness(b.District), Is.False, "block " + b.Id + " is in the city");
                Assert.That(b.District, Is.EqualTo(CityLayout.DistrictAt(b.Curb.CenterX, b.Curb.CenterZ)), "block " + b.Id + " district");
                Assert.That(b.Curb.Valid && b.Inner.Valid, Is.True, "block " + b.Id + " rectangles");
            }
        }

        [Test]
        public void GraphIndicesAreConsistent()
        {
            CityLayout L = TestData.City;
            for (int i = 0; i < L.Nodes.Count; i++) Assert.That(L.Nodes[i].Id, Is.EqualTo(i), "node id");
            for (int i = 0; i < L.Edges.Count; i++)
            {
                RoadEdge e = L.Edges[i];
                Assert.That(e.Id, Is.EqualTo(i), "edge id");
                Assert.That(e.A >= 0 && e.A < L.Nodes.Count && e.B >= 0 && e.B < L.Nodes.Count, Is.True, "edge " + i + " ends");
                Assert.That(L.Nodes[e.A].Edges.Contains(i) && L.Nodes[e.B].Edges.Contains(i), Is.True, "edge " + i + " listed by its nodes");
                Assert.That(e.Width, Is.GreaterThan(0f), "edge " + i + " width");
            }
            for (int i = 0; i < L.Lanes.Count; i++)
            {
                LanePath lane = L.Lanes[i];
                Assert.That(lane.Id, Is.EqualTo(i), "lane id");
                Assert.That(lane.Points.Count, Is.GreaterThanOrEqualTo(2), "lane " + i + " points");
                Assert.That(lane.Length, Is.GreaterThan(0f), "lane " + i + " length");
                foreach (int next in lane.Next) Assert.That(next >= 0 && next < L.Lanes.Count, Is.True, "lane " + i + " continuation");
            }
            for (int i = 0; i < L.WalkNodes.Count; i++) Assert.That(L.WalkNodes[i].Id, Is.EqualTo(i), "walk node id");
            foreach (WalkLink w in L.WalkLinks)
                Assert.That(w.A >= 0 && w.A < L.WalkNodes.Count && w.B >= 0 && w.B < L.WalkNodes.Count, Is.True, "walk link " + w.Id);
            for (int i = 0; i < L.Pois.Count; i++) Assert.That(L.Pois[i].Id, Is.EqualTo(i), "place id");
        }

        [Test]
        public void SameSeedBuildsTheSameCity()
        {
            CityLayout a = TestData.City;
            CityLayout b = new CityLayout(TestData.DefaultSeed);
            b.Generate();
            Assert.That(b.Nodes.Count, Is.EqualTo(a.Nodes.Count), "nodes");
            Assert.That(b.Lanes.Count, Is.EqualTo(a.Lanes.Count), "lanes");
            Assert.That(b.Blocks.Count, Is.EqualTo(a.Blocks.Count), "blocks");
            Assert.That(b.Pois.Count, Is.EqualTo(a.Pois.Count), "places");
            Assert.That(b.Props.Count, Is.EqualTo(a.Props.Count), "props");
            for (int i = 0; i < a.Pois.Count; i++)
            {
                Assert.That(b.Pois[i].Name, Is.EqualTo(a.Pois[i].Name), "place " + i);
                Assert.That((b.Pois[i].Position - a.Pois[i].Position).sqrMagnitude, Is.LessThan(1e-6f), "place " + i + " position");
            }
        }
    }
}
