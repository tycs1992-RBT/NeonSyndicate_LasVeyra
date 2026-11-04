using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Generated city interiors (InteriorRules, InteriorRooms): on the default city the selection gives the target count,
    /// spread over the city districts, with unique ids and names, doors spaced apart and clear of story doors, never on a
    /// venue; every kind appears; each room plan keeps its people and furniture inside the walls, leaves the door clear
    /// and puts the counter and the keeper inside; the entry rules refuse vehicles, pursuits, fights and the Veil.
    /// Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class InteriorRulesTests
    {
        private static List<InteriorSite> DefaultCity(out CityLayout layout, out HashSet<Building> venues)
        {
            layout = new CityLayout(61724);
            layout.Generate();
            venues = new HashSet<Building>();
            foreach (VenueBinding04C v in InhabitedCityBindings04C.Select(layout, InhabitedCityBindings04CTests.ReadCatalog()))
                if (v.Building != null) venues.Add(v.Building);
            return InteriorRules.Select(layout, venues);
        }

        [Test]
        public void DefaultCitySelectionIsSpreadUniqueAndClear()
        {
            CityLayout layout;
            HashSet<Building> venues;
            List<InteriorSite> sites = DefaultCity(out layout, out venues);
            Assert.That(sites.Count, Is.EqualTo(InteriorRules.Target), "the city has room for every interior");
            HashSet<string> ids = new HashSet<string>(), names = new HashSet<string>();
            HashSet<District> districts = new HashSet<District>();
            HashSet<InteriorKind> kinds = new HashSet<InteriorKind>();
            for (int i = 0; i < sites.Count; i++)
            {
                InteriorSite s = sites[i];
                Assert.That(ids.Add(s.Id), Is.True, "unique id " + s.Id);
                Assert.That(names.Add(s.Name), Is.True, "unique name " + s.Name);
                Assert.That(venues.Contains(s.Building), Is.False, "never a venue building");
                Assert.That(InteriorRules.CityDistrict(s.District), Is.True);
                Assert.That(s.RoomOrigin.y, Is.EqualTo(InteriorRules.RoomAltitude));
                Assert.That(s.RoomOrigin.y, Is.GreaterThan(TravelMath.SoftCeiling + 300f), "rooms sit far above the flight ceiling");
                districts.Add(s.District);
                kinds.Add(s.Kind);
                for (int j = i + 1; j < sites.Count; j++)
                    Assert.That(MathUtil.FlatDistance(s.Door, sites[j].Door), Is.GreaterThanOrEqualTo(InteriorRules.Spacing - 0.01f));
                foreach (Poi poi in layout.Pois)
                    Assert.That(MathUtil.FlatDistance(poi.Position, s.Door), Is.GreaterThanOrEqualTo(InteriorRules.PoiClearance - 0.01f), s.Name + " near " + poi.Name);
            }
            Assert.That(districts.Count, Is.GreaterThanOrEqualTo(6), "interiors spread over the city");
            Assert.That(kinds.Count, Is.EqualTo(8), "every kind appears");
        }

        [Test]
        public void SelectionIsDeterministic()
        {
            CityLayout a, b;
            HashSet<Building> va, vb;
            List<InteriorSite> first = DefaultCity(out a, out va), second = DefaultCity(out b, out vb);
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
                Assert.That(second[i].Name, Is.EqualTo(first[i].Name));
                Assert.That(second[i].Kind, Is.EqualTo(first[i].Kind));
            }
        }

        [Test]
        public void EveryRoomPlanIsInsideItsWallsWithTheDoorClear()
        {
            for (int k = 0; k < 8; k++)
                for (int seed = 0; seed < 6; seed++)
                {
                    InteriorKind kind = (InteriorKind)k;
                    InteriorPlan p = InteriorRooms.Plan(kind, seed * 977 + 13);
                    float hw = p.Size.x * 0.5f;
                    foreach (InteriorPiece piece in p.Pieces)
                    {
                        if (piece.Name.StartsWith("Front wall") || piece.Name.StartsWith("Door") || piece.Name == "Floor" || piece.Name == "Ceiling"
                            || piece.Name.EndsWith(" wall") || piece.Name == "Back wall") continue;
                        Vector3 half = piece.Cylinder ? new Vector3(piece.Size.x * 0.5f, piece.Size.y, piece.Size.z * 0.5f) : piece.Size * 0.5f;
                        // A cylinder on its side lies along x; a box turned a quarter swaps its x and z extents.
                        if (piece.Cylinder && Mathf.Abs(piece.Yaw) > 0.01f) half = new Vector3(half.y, half.x, half.z);
                        else if (!piece.Cylinder && Mathf.Abs(Mathf.Repeat(piece.Yaw, 180f) - 90f) < 1f) half = new Vector3(half.z, half.y, half.x);
                        Assert.That(Mathf.Abs(piece.Center.x) + half.x, Is.LessThanOrEqualTo(hw + 0.25f), kind + " " + piece.Name + " inside the side walls");
                        Assert.That(piece.Center.z + half.z, Is.LessThanOrEqualTo(p.Size.z + 0.25f), kind + " " + piece.Name + " inside the back wall");
                        Assert.That(piece.Center.y + half.y, Is.LessThanOrEqualTo(p.Size.y + 0.25f), kind + " " + piece.Name + " under the ceiling");
                        if (piece.Collider)
                        {
                            bool inDoorway = Mathf.Abs(piece.Center.x) - half.x < InteriorRooms.DoorWidth * 0.5f + 0.4f && piece.Center.z - half.z < InteriorRooms.DoorClear;
                            Assert.That(inDoorway, Is.False, kind + " " + piece.Name + " blocks the doorway");
                        }
                    }
                    foreach (Vector3 point in new[] { p.Entry, p.Counter, p.Keeper.Position })
                    {
                        Assert.That(Mathf.Abs(point.x), Is.LessThan(hw - 0.2f), kind + " point inside");
                        Assert.That(point.z, Is.GreaterThan(0.2f).And.LessThan(p.Size.z - 0.2f), kind + " point inside");
                    }
                    foreach (InteriorSpot spot in p.Patrons)
                        Assert.That(Mathf.Abs(spot.Position.x) < hw - 0.2f && spot.Position.z > 0.2f && spot.Position.z < p.Size.z - 0.2f, Is.True, kind + " patron inside");
                    Assert.That(p.Lights.Count, Is.GreaterThan(0), kind + " is lit");
                }
        }

        [Test]
        public void PlansAreDeterministic()
        {
            InteriorPlan a = InteriorRooms.Plan(InteriorKind.Boutique, 4242), b = InteriorRooms.Plan(InteriorKind.Boutique, 4242);
            Assert.That(b.Pieces.Count, Is.EqualTo(a.Pieces.Count));
            for (int i = 0; i < a.Pieces.Count; i++) Assert.That(b.Pieces[i].Center, Is.EqualTo(a.Pieces[i].Center));
        }

        [Test]
        public void KindsNamesServicesAndEntry()
        {
            int[] count = new int[8];
            for (int n = 0; n < InteriorRules.Target; n++) count[(int)InteriorRules.KindFor(n)]++;
            foreach (int c in count) Assert.That(c, Is.InRange(3, 4));
            Assert.That(InteriorRules.NameFor(InteriorKind.Chapel, 0, "Old Silver"), Is.Not.EqualTo(InteriorRules.NameFor(InteriorKind.Chapel, 4, "Old Silver")));
            string what; int price; float heal;
            Assert.That(InteriorRules.Service(InteriorKind.NoodleBar, out what, out price, out heal) && price > 0 && heal > 0f && heal <= 1f, Is.True);
            Assert.That(InteriorRules.Service(InteriorKind.Bookshop, out what, out price, out heal), Is.False);
            string why;
            Assert.That(InteriorRules.CanEnter(true, true, 0, false, false, out why), Is.True);
            Assert.That(InteriorRules.CanEnter(false, true, 0, false, false, out why), Is.False);
            Assert.That(InteriorRules.CanEnter(true, true, 2, false, false, out why), Is.False);
            Assert.That(InteriorRules.CanEnter(true, true, 0, true, false, out why), Is.False);
            Assert.That(InteriorRules.CanEnter(true, true, 0, false, true, out why), Is.False);
            Assert.That(InteriorRules.CanEnter(true, false, 0, false, false, out why), Is.False);
        }
    }
}
