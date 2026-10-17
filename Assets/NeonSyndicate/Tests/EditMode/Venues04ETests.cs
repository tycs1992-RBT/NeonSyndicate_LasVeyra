using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    // FINGERPRINT HELPER BEGIN
    /// <summary>
    /// Offline reading of the shipped venue layouts (the strict reader, no JsonUtility) and a stable fingerprint of the
    /// expanded geometry, used to prove the eight 04C venues are untouched by 04E.
    /// </summary>
    public static class VenueLayoutData04E
    {
        public sealed class NodeIn { public string id, kind, source, model, role; public List<float> position, scale, euler; public bool solid, far = true, interior = true; public int progress; }
        public sealed class AnchorIn { public string id; public List<float> position; public float yaw; }
        public sealed class KeepIn { public string id; public List<float> min, max; }
        public sealed class LightIn { public string id; public List<float> position, color; public float range, intensity; }
        public sealed class LabelIn { public string id, text; public List<float> position; public float yaw, characterSize; }
        public sealed class LayoutIn
        {
            public string version, id, title, notes; public int style; public bool twoStoreys;
            public List<NodeIn> nodes = new List<NodeIn>(); public List<AnchorIn> anchors = new List<AnchorIn>();
            public List<KeepIn> keepouts = new List<KeepIn>(); public List<LightIn> lights = new List<LightIn>(); public List<LabelIn> labels = new List<LabelIn>();
        }
        public sealed class CatalogIn { public string Version; public List<VenueDefinition04C> Venues = new List<VenueDefinition04C>(); }

        public static string Root = Directory.GetCurrentDirectory();
        public static string Resources { get { return Path.Combine(Root, "Assets/NeonSyndicate/Resources"); } }

        private static float[] A(List<float> l) { return l == null ? null : l.ToArray(); }

        public static VenueCatalog04C Catalog()
        {
            string path = Path.Combine(Resources, "InhabitedCity04C/venues.json");
            List<string> errors = new List<string>();
            CatalogIn c = QuestJson.Read<CatalogIn>(File.ReadAllText(path), path, errors, null);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("\n", errors.ToArray()));
            return new VenueCatalog04C { Version = c.Version, Venues = c.Venues.ToArray() };
        }

        public static VenueLayout04C Layout(string id)
        {
            string path = Path.Combine(Resources, "InhabitedCity04C/Layouts/" + id + ".json");
            List<string> errors = new List<string>();
            LayoutIn i = QuestJson.Read<LayoutIn>(File.ReadAllText(path), path, errors, null);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("\n", errors.ToArray()));
            VenueLayout04C o = new VenueLayout04C { version = i.version, id = i.id, title = i.title, notes = i.notes, style = i.style, twoStoreys = i.twoStoreys };
            List<VenueNode04C> n = new List<VenueNode04C>();
            foreach (NodeIn x in i.nodes)
                n.Add(new VenueNode04C { id = x.id, kind = x.kind, source = x.source, model = x.model, role = x.role, position = A(x.position), scale = A(x.scale), euler = A(x.euler), solid = x.solid, far = x.far, interior = x.interior, progress = x.progress });
            o.nodes = n.ToArray();
            List<VenueAnchor04C> a = new List<VenueAnchor04C>();
            foreach (AnchorIn x in i.anchors) a.Add(new VenueAnchor04C { id = x.id, position = A(x.position), yaw = x.yaw });
            o.anchors = a.ToArray();
            List<VenueKeepout04C> k = new List<VenueKeepout04C>();
            foreach (KeepIn x in i.keepouts) k.Add(new VenueKeepout04C { id = x.id, min = A(x.min), max = A(x.max) });
            o.keepouts = k.ToArray();
            List<VenueLight04C> l = new List<VenueLight04C>();
            foreach (LightIn x in i.lights) l.Add(new VenueLight04C { id = x.id, position = A(x.position), color = A(x.color), range = x.range, intensity = x.intensity });
            o.lights = l.ToArray();
            List<VenueLabel04C> t = new List<VenueLabel04C>();
            foreach (LabelIn x in i.labels) t.Add(new VenueLabel04C { id = x.id, text = x.text, position = A(x.position), yaw = x.yaw, characterSize = x.characterSize });
            o.labels = t.ToArray();
            return o;
        }

        private static string N(float f) { return f.ToString("0.0000", CultureInfo.InvariantCulture); }
        private static string V(float[] v)
        {
            if (v == null) return "null";
            StringBuilder s = new StringBuilder();
            foreach (float f in v) s.Append(N(f)).Append(',');
            return s.ToString();
        }

        /// <summary>Every expanded node, light, label and keep-out, in order, with invariant number formatting.</summary>
        public static string Geometry(VenueExpanded04C e)
        {
            StringBuilder s = new StringBuilder();
            s.Append(e.id).Append('|').Append(e.style).Append('|').Append(N(e.occupiedHeight)).Append('\n');
            foreach (VenueNode04C n in e.nodes)
                s.Append(n.id).Append('|').Append(n.kind).Append('|').Append(n.source).Append('|').Append(n.model).Append('|').Append(n.role).Append('|')
                 .Append(V(n.position)).Append('|').Append(V(n.scale)).Append('|').Append(V(n.euler)).Append('|')
                 .Append(n.solid ? 'S' : 's').Append(n.far ? 'F' : 'f').Append(n.interior ? 'I' : 'i').Append(n.progress).Append('\n');
            foreach (VenueLight04C l in e.lights) s.Append("L|").Append(l.id).Append('|').Append(V(l.position)).Append(V(l.color)).Append(N(l.range)).Append(',').Append(N(l.intensity)).Append('\n');
            foreach (VenueLabel04C l in e.labels) s.Append("T|").Append(l.id).Append('|').Append(l.text).Append('|').Append(V(l.position)).Append(N(l.yaw)).Append(',').Append(N(l.characterSize)).Append('\n');
            if (e.keepouts != null) foreach (VenueKeepout04C k in e.keepouts) s.Append("K|").Append(k.id).Append('|').Append(V(k.min)).Append(V(k.max)).Append('\n');
            return s.ToString();
        }

        /// <summary>The anchors (optionally leaving out some ids), in order.</summary>
        public static string Anchors(VenueExpanded04C e, ICollection<string> skip)
        {
            StringBuilder s = new StringBuilder();
            foreach (VenueAnchor04C a in e.anchors)
            {
                if (skip != null && skip.Contains(a.id)) continue;
                s.Append(a.id).Append('|').Append(V(a.position)).Append(N(a.yaw)).Append('\n');
            }
            return s.ToString();
        }

        public static string Sha(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < 8; i++) s.Append(h[i].ToString("x2"));
                return s.ToString();
            }
        }
    }
    // FINGERPRINT HELPER END

    /// <summary>
    /// 04E venues: the four new walk-in rooms bound on the real default city, their anchors and furniture, the Pressure
    /// House stairwell at the frozen constants, the untouched eight 04C venues, and the arrival scene plan (receiver mast,
    /// conduit, Lofts dressing, refuge perch) checked against roads, doors, sidewalks, props and the 04D case.
    /// </summary>
    public sealed class Venues04ETests
    {
        private static readonly string[] Required = { "service", "activity", "staff", "visitor1", "visitor2", "exit" };

        /// <summary>The eight 04C venues as the foundation shipped them (expanded geometry; anchors without the appended garage "bay").</summary>
        private static readonly string[,] FrozenEight =
        {
            { "lofts", "c5c163cc0721062b", "eda3b516a3c5799b" },
            { "clinic", "807adb0f2cc01bb7", "22ae96b7b1bb9f16" },
            { "forge", "a4f84de22da02f93", "a0648941812ab2bb" },
            { "inn", "9197fa10ae76e841", "d1120296914346f4" },
            { "garage", "cb073249e1c61749", "87061b5ca1638a26" },
            { "archive", "b1c09fc42a4de7d2", "69f85381b88cfe18" },
            { "observatory", "b134ef56dafae3ca", "8146f2077d5f169f" },
            { "depot", "1d4efce0f1dd19fa", "99e5fba393865fa0" },
        };

        private static CityLayout _city;
        private static List<VenueBinding04C> _bindings;

        private static CityLayout City
        {
            get
            {
                if (_city == null) { _city = new CityLayout(61724); _city.Generate(); }
                return _city;
            }
        }

        private static List<VenueBinding04C> Bindings
        {
            get { return _bindings ?? (_bindings = InhabitedCityBindings04C.Select(City, VenueLayoutData04E.Catalog())); }
        }

        private static VenueBinding04C Bound(string id) { return Bindings.Find(b => b.Definition.Id == id); }

        private static Vector3 Arrival
        {
            get { Poi home = City.FindPoi("Veyra Lofts"); return home.Position + home.Facing.normalized * 3.5f; }
        }

        private static VenueExpanded04C Expanded(string id)
        {
            VenueBinding04C b = Bound(id);
            return InhabitedCity04CComposition.Expand(VenueLayoutData04E.Layout(id), b.Width, b.Depth, b.Building.Height);
        }

        // ------------------------------------------------------------------ bindings on the default city

        [Test]
        public void FourVenuesBindOnTheDefaultCityAtTheirChosenParcels()
        {
            Vector3 arrival = Arrival;
            VenueBinding04C dispatch = Bound("dispatch"), pressure = Bound("pressure"), refuge = Bound("bondkeeper"), office = Bound("office");
            Assert.That(dispatch.Building.Seed, Is.EqualTo(855223), "Cinder Dispatch: the tower straight across Veyra Avenue");
            Assert.That(pressure.Building.Seed, Is.EqualTo(434831));
            Assert.That(refuge.Building.Seed, Is.EqualTo(703831));
            Assert.That(office.Building.Seed, Is.EqualTo(472154));
            // Dispatch: east of the Veyra Avenue roadway, facing west, the arrival straight in front of its door.
            Assert.That(dispatch.Origin.x, Is.GreaterThan(7.5f));
            Assert.That(dispatch.Outward.x, Is.LessThan(-.99f));
            Vector3 toArrival = arrival - dispatch.Origin; toArrival.y = 0f;
            Assert.That(Vector3.Angle(dispatch.Outward, toArrival), Is.LessThan(5f), "The dispatch door faces the arrival");
            Assert.That(MathUtil.FlatDistance(dispatch.Origin, arrival), Is.LessThan(30f));
            // From the arrival, facing out of the Lofts door, the dispatch door is straight ahead.
            Poi home = City.FindPoi("Veyra Lofts");
            Vector3 toDoor = dispatch.Origin - arrival; toDoor.y = 0f;
            Assert.That(Vector3.Angle(home.Facing, toDoor), Is.LessThan(5f));
            float walk = WalkDistance(City, arrival, pressure.Outside);
            Assert.That(walk, Is.InRange(40f, 120f), "Pressure House walk from the arrival");
            Assert.That(MathUtil.FlatDistance(refuge.Origin, arrival), Is.LessThanOrEqualTo(150f), "Bondkeeper's Refuge distance");
            Assert.That(MathUtil.FlatDistance(office.Origin, arrival), Is.LessThanOrEqualTo(300f), "Resonance Office distance");
            foreach (string id in VenueIds04E.All)
            {
                VenueBinding04C b = Bound(id);
                Assert.That(b.Building, Is.Not.Null, id);
                Assert.That(b.Lot.Poi, Is.EqualTo(PoiType.None), id);
                Assert.That(b.Lot.CustomBuilt, Is.False, id);
                Assert.That(InhabitedCityBindings04C.EligibleFor(b.Definition, b.Building), Is.True, id);
                Assert.That(InhabitedCityBindings04C.DoorOnSidewalk04E(BlockOf(b.Lot), b.Origin, b.Outward), Is.True, id + " door on a sidewalk");
                Assert.That(InhabitedCityBindings04C.NearPoiDoor04E(City, b.Origin), Is.False, id + " keeps clear of point-of-interest doors");
                Assert.That(City.RoadAt(b.Outside, 0f), Is.Null, id + " doorstep is not on a road");
            }
        }

        [Test]
        public void NewVenuesNeverTakeAnOriginalVenueParcel()
        {
            HashSet<Building> original = new HashSet<Building>();
            foreach (VenueBinding04C b in Bindings) if (!VenueIds04E.Has(b.Definition.Id) && b.Building != null) original.Add(b.Building);
            Assert.That(original, Has.Count.EqualTo(8));
            foreach (string id in VenueIds04E.All) Assert.That(original.Contains(Bound(id).Building), Is.False, id);
        }

        [TestCase(1)]
        [TestCase(2016)]
        [TestCase(77231)]
        [TestCase(92017)]
        public void OtherCitiesBindSafelyOrSkipWithAReason(int seed)
        {
            CityLayout city = new CityLayout(seed); city.Generate();
            List<VenueBinding04C> bound = InhabitedCityBindings04C.Select(city, VenueLayoutData04E.Catalog());
            foreach (VenueBinding04C b in bound)
            {
                if (!VenueIds04E.Has(b.Definition.Id)) continue;
                if (b.Building == null) { Assert.That(b.Status, Does.StartWith("Skipped:")); continue; }
                Assert.That(InhabitedCityBindings04C.EligibleFor(b.Definition, b.Building), Is.True);
                Lot lot = b.Lot;
                Block block = null;
                foreach (Block k in city.Blocks) if (k.Lots.Contains(lot)) block = k;
                Assert.That(InhabitedCityBindings04C.DoorOnSidewalk04E(block, b.Origin, b.Outward), Is.True, seed + "/" + b.Definition.Id);
                Assert.That(InhabitedCityBindings04C.NearPoiDoor04E(city, b.Origin), Is.False);
            }
            ArrivalPlan04E plan = ArrivalPlan04E.Make(city, bound, null);
            foreach (ArrivalPiece04E piece in plan.Pieces) Assert.That(piece.Placed || piece.Reason.Length > 0, Is.True, piece.Id);
            Assert.That(plan.ConduitPlaced || plan.ConduitReason.Length > 0, Is.True);
        }

        [Test]
        public void CatalogEntriesForTheFourAreValidAndClaimNoStoryPlace()
        {
            VenueCatalog04C catalog = VenueLayoutData04E.Catalog();
            Assert.That(catalog.Venues, Has.Length.EqualTo(12));
            for (int i = 0; i < 4; i++)
            {
                VenueDefinition04C d = catalog.Venues[8 + i];
                Assert.That(d.Id, Is.EqualTo(VenueIds04E.All[i]), "Appended in VenueIds04E order");
                Assert.That(InhabitedCityBindings04C.ValidDefinition(d), Is.True, d.Id);
                Assert.That(d.PoiName, Is.Empty);
                d.PoiName = "Veyra Lofts";
                Assert.That(InhabitedCityBindings04C.ValidDefinition(d), Is.False, "A 04E venue never claims a story point of interest");
                d.PoiName = "";
            }
            Assert.That(catalog.Venues[8].Title, Is.EqualTo("Cinder Dispatch"));
            Assert.That(catalog.Venues[9].Title, Is.EqualTo("The Pressure House"));
            Assert.That(catalog.Venues[10].Title, Is.EqualTo("Bondkeeper's Refuge"));
            Assert.That(catalog.Venues[11].Title, Is.EqualTo("The Resonance Office"));
        }

        [Test]
        public void OnlyTheDispatchMayRebuildAnAntennaRoof()
        {
            Building antenna = new Building { Footprint = new RectF(0f, 0f, 30f, 40f), Height = 60f, Style = FacadeStyle.OfficeGlass, Roof = RoofType.Antenna, Tiers = 1 };
            VenueDefinition04C dispatch = new VenueDefinition04C { Id = "dispatch" }, pressure = new VenueDefinition04C { Id = "pressure" }, archive = new VenueDefinition04C { Id = "archive" };
            Assert.That(InhabitedCityBindings04C.Eligible(antenna), Is.False, "The 04C rule is unchanged");
            Assert.That(InhabitedCityBindings04C.EligibleFor(dispatch, antenna), Is.True);
            Assert.That(InhabitedCityBindings04C.EligibleFor(pressure, antenna), Is.False);
            Assert.That(InhabitedCityBindings04C.EligibleFor(archive, antenna), Is.False);
            antenna.Roof = RoofType.Helipad;
            Assert.That(InhabitedCityBindings04C.EligibleFor(dispatch, antenna), Is.False, "Helipads stay protected");
            antenna.Roof = RoofType.Antenna; antenna.Tiers = 2;
            Assert.That(InhabitedCityBindings04C.EligibleFor(dispatch, antenna), Is.False, "Tiered towers stay protected");
        }

        [Test]
        public void LoftsFrameMapsOffsetsRelativeToTheHomeDoor()
        {
            Poi home = City.FindPoi("Veyra Lofts");
            Vector3 p = InhabitedCityBindings04C.FramePoint04E(home, .5f, 23.6f);
            Assert.That(MathUtil.FlatDistance(p, Bound("dispatch").Origin), Is.LessThan(1f));
            Assert.That(InhabitedCityBindings04C.FrameDirection04E(home, 0f, -1f), Is.EqualTo(new Vector3(-1f, 0f, 0f)));
            Poi turned = new Poi { Position = new Vector3(100f, .15f, 100f), Facing = new Vector3(0f, 0f, 1f) };
            Vector3 q = InhabitedCityBindings04C.FramePoint04E(turned, 2f, 10f);
            Assert.That(q.x, Is.EqualTo(102f).Within(.001f));
            Assert.That(q.z, Is.EqualTo(110f).Within(.001f));
            Assert.That(InhabitedCityBindings04C.FrameDirection04E(turned, 1f, 0f), Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Vector3 local = new Vector3(1.5f, .2f, 7f);
            foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
            {
                Vector3 w = InhabitedCityBindings04C.LocalToWorld(new Vector3(3f, .15f, -4f), yaw, local);
                Vector3 back = InhabitedCityBindings04C.WorldToLocal(new Vector3(3f, .15f, -4f), yaw, w);
                Assert.That(Vector3.Distance(back, local), Is.LessThan(.001f), "yaw " + yaw);
            }
        }

        [Test]
        public void DoorZoneCoversTheApproachAndRoomButNotTheNeighbours()
        {
            Vector3 origin = new Vector3(10f, .15f, 0f);
            const float yaw = 90f; // the room goes toward +x, the street is toward -x, local +x is world -z
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(7f, .15f, 0f), 0f), Is.True, "On the doorstep");
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(3.5f, .15f, .5f), 0f), Is.True, "Out to the curb");
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(18f, 1f, 3f), 0f), Is.True, "Inside the room");
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(8.5f, .15f, 4f), 0f), Is.False, "Beside the door");
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(8.5f, .15f, 4f), 2f), Is.True, "A wide footprint reaches the door");
            Assert.That(InhabitedCityBindings04C.InDoorZone04E(origin, yaw, new Vector3(31f, .15f, 0f), 0f), Is.False, "Behind the room");
        }

        // ------------------------------------------------------------------ layouts

        [TestCase("dispatch"), TestCase("pressure"), TestCase("bondkeeper"), TestCase("office")]
        public void EveryNewLayoutExpandsWithAllAnchorsInsideTheRoom(string id)
        {
            VenueExpanded04C e = Expanded(id);
            Assert.That(e.twoStoreys, Is.False, "One-storey rooms");
            Assert.That(e.nodes.Length, Is.LessThanOrEqualTo(420));
            Assert.That(e.lights.Length, Is.InRange(2, 6));
            Assert.That(e.labels.Length, Is.InRange(2, 8));
            HashSet<string> anchors = new HashSet<string>();
            foreach (VenueAnchor04C a in e.anchors) Assert.That(anchors.Add(a.id), Is.True, "Duplicate anchor " + a.id);
            foreach (string a in Required) Assert.That(anchors.Contains(a), Is.True, id + " misses " + a);
            foreach (string a in VenueIds04E.ExtraAnchors(id)) Assert.That(anchors.Contains(a), Is.True, id + " misses " + a);
            List<Bounds> solids = SolidBounds(e);
            foreach (VenueAnchor04C a in e.anchors)
            {
                if (a.id == "exit") continue;
                Vector3 p = V(a.position);
                Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(7.05f), id + "." + a.id);
                Assert.That(p.z, Is.InRange(.8f, 17.05f), id + "." + a.id);
                Assert.That(p.y, Is.InRange(0f, .05f), id + "." + a.id + " stands on the floor finish");
                string hit = CapsuleHit(solids, p, .3f);
                Assert.That(hit, Is.Null, id + "." + a.id + " stands inside " + hit);
            }
        }

        [TestCase("dispatch"), TestCase("pressure"), TestCase("bondkeeper"), TestCase("office")]
        public void ResidentStationsAndRoutesAreClearAndKeepOffInteractionPoints(string id)
        {
            VenueExpanded04C e = Expanded(id);
            Dictionary<string, Vector3> a = new Dictionary<string, Vector3>();
            foreach (VenueAnchor04C x in e.anchors) a[x.id] = V(x.position);
            List<Bounds> solids = SolidBounds(e);
            List<string> protectedIds = new List<string> { "exit", "service", "activity" };
            protectedIds.AddRange(VenueIds04E.ExtraAnchors(id));
            foreach (string station in new[] { "staff", "visitor1", "visitor2" })
            {
                Vector3 first = a[station], second = a[station + "_next"];
                float length = Vector3.Distance(first, second);
                Assert.That(length, Is.InRange(1.2f, 3f), id + "." + station + " route length");
                for (int i = 0; i <= 8; i++)
                {
                    Vector3 p = Vector3.Lerp(first, second, i / 8f);
                    Assert.That(CapsuleHit(solids, p, .35f), Is.Null, id + "." + station + " route blocked at " + p);
                    foreach (string pid in protectedIds)
                        Assert.That(Vector3.Distance(p, a[pid]), Is.GreaterThanOrEqualTo(1.3f), id + "." + station + " route stands on " + pid);
                }
            }
        }

        [TestCase("dispatch"), TestCase("pressure"), TestCase("bondkeeper"), TestCase("office")]
        public void FurnitureStaysInsideTheShellAndOutOfTheDoorway(string id)
        {
            VenueExpanded04C e = Expanded(id);
            VenueLayout04C source = VenueLayoutData04E.Layout(id);
            foreach (VenueNode04C n in source.nodes)
            {
                Bounds b = NodeBounds(n);
                if (n.kind == "mesh")
                {
                    Assert.That(b.min.x, Is.GreaterThanOrEqualTo(-e.width * .5f - .15f), n.id);
                    Assert.That(b.max.x, Is.LessThanOrEqualTo(e.width * .5f + .15f), n.id);
                    Assert.That(b.min.z, Is.GreaterThanOrEqualTo(-1.8f), n.id);
                    Assert.That(b.max.y, Is.LessThanOrEqualTo(e.occupiedHeight + .5f), n.id);
                }
                if (!n.solid) continue;
                // The doorway and the first metre inside stay clear for walking in and out.
                Bounds door = new Bounds(new Vector3(0f, 1.1f, .2f), new Vector3(2.6f, 2.2f, 2.4f));
                if (n.id != "carved_public_threshold") Assert.That(b.Intersects(door), Is.False, id + "." + n.id + " blocks the doorway");
                if (b.max.z > .35f) Assert.That(Mathf.Abs(b.center.x) - b.extents.x, Is.LessThan(7.8f), n.id);
            }
        }

        // ------------------------------------------------------------------ the Pressure House stairwell

        [Test]
        public void PressureStairOpeningMatchesTheConstantsAndLeavesTheApproachClear()
        {
            Assert.That(VenueIds04E.StairX0, Is.EqualTo(4.3f));
            Assert.That(VenueIds04E.StairX1, Is.EqualTo(7.1f));
            Assert.That(VenueIds04E.StairZ0, Is.EqualTo(3.0f));
            Assert.That(VenueIds04E.StairZ1, Is.EqualTo(14.6f));
            VenueExpanded04C e = Expanded("pressure");
            float x0 = VenueIds04E.StairX0, x1 = VenueIds04E.StairX1, z0 = VenueIds04E.StairZ0, z1 = VenueIds04E.StairZ1;
            const float eps = .011f;
            Bounds opening = new Bounds(new Vector3((x0 + x1) * .5f, -1.0f, (z0 + z1) * .5f), new Vector3(x1 - x0 - 2f * eps, 7f, z1 - z0 - 2f * eps));
            // From just above the floor finish (the slabs end at 0.012) to head height.
            Bounds approach = new Bounds(new Vector3((x0 + x1) * .5f, 1.12f, (InhabitedCity04CComposition.StairApproachZ04E + z0) * .5f),
                new Vector3(x1 - x0 - 2f * eps, 2.16f, z0 - InhabitedCity04CComposition.StairApproachZ04E - 2f * eps));
            float slabArea = 0f;
            foreach (VenueNode04C n in e.nodes)
            {
                Bounds b = NodeBounds(n);
                if (n.id.StartsWith("interior_floor", StringComparison.Ordinal))
                {
                    slabArea += b.size.x * b.size.z;
                    Assert.That(b.max.y, Is.EqualTo(.012f).Within(.001f), n.id);
                }
                // The preserved upper architecture is not a centred box: its collider starts at the room's ceiling (5 m).
                if (!n.solid || n.kind == "upper04A") continue;
                Assert.That(b.Intersects(opening), Is.False, n.id + " spans the stairwell opening");
                Assert.That(b.Intersects(approach), Is.False, n.id + " stands in the stair approach");
            }
            Assert.That(slabArea, Is.EqualTo(16f * 18f - (x1 - x0) * (z1 - z0)).Within(.01f), "The floor is the room minus exactly the opening");
            // Guard rails: both long sides and the back, at least a metre high; the front end (toward the door) stays open.
            Assert.That(RailCoverage(e, true, x0), Is.GreaterThanOrEqualTo(z1 - z0 - .01f), "inner long side rail");
            Assert.That(RailCoverage(e, true, x1), Is.GreaterThanOrEqualTo(z1 - z0 - .01f), "wall long side rail");
            Assert.That(RailCoverage(e, false, z1), Is.GreaterThanOrEqualTo(x1 - x0 - .01f), "back rail");
            Assert.That(RailCoverage(e, false, z0), Is.EqualTo(0f), "the front end of the stair is open");
            int stairTop = 0;
            foreach (VenueAnchor04C a in e.anchors)
            {
                Vector3 p = V(a.position);
                bool inApproach = p.x > x0 && p.x < x1 && p.z > InhabitedCity04CComposition.StairApproachZ04E && p.z < z0;
                bool overOpening = p.x > x0 && p.x < x1 && p.z >= z0 && p.z <= z1;
                if (a.id == "stair_top") { stairTop++; Assert.That(inApproach, Is.True, "stair_top stands in the approach"); }
                else Assert.That(inApproach || overOpening, Is.False, a.id + " stands in the stairwell or its approach");
            }
            Assert.That(stairTop, Is.EqualTo(1));
        }

        [Test]
        public void AStairwellBlockedBySourceDataIsRefused()
        {
            VenueLayout04C layout = VenueLayoutData04E.Layout("pressure");
            List<VenueNode04C> nodes = new List<VenueNode04C>(layout.nodes);
            nodes.Add(new VenueNode04C { id = "test_crate_in_approach", kind = "box", role = "timber", position = new[] { 5.7f, .5f, 2.2f }, scale = new[] { 1f, 1f, 1f }, euler = new float[3], solid = true });
            layout.nodes = nodes.ToArray();
            VenueBinding04C b = Bound("pressure");
            Assert.Throws<InvalidDataException>(() => InhabitedCity04CComposition.Expand(layout, b.Width, b.Depth, b.Building.Height));
            VenueLayout04C missing = VenueLayoutData04E.Layout("dispatch");
            List<VenueAnchor04C> anchors = new List<VenueAnchor04C>(missing.anchors);
            anchors.RemoveAll(a => a.id == "fitting");
            missing.anchors = anchors.ToArray();
            VenueBinding04C d = Bound("dispatch");
            Assert.Throws<InvalidDataException>(() => InhabitedCity04CComposition.Expand(missing, d.Width, d.Depth, d.Building.Height), "A missing 04E anchor is refused");
        }

        // ------------------------------------------------------------------ the eight originals

        [Test]
        public void TheEightOriginalVenuesAreUnchanged()
        {
            for (int i = 0; i < FrozenEight.GetLength(0); i++)
            {
                string id = FrozenEight[i, 0];
                VenueExpanded04C e = Expanded(id);
                Assert.That(VenueLayoutData04E.Sha(VenueLayoutData04E.Geometry(e)), Is.EqualTo(FrozenEight[i, 1]), id + " geometry changed");
                Assert.That(VenueLayoutData04E.Sha(VenueLayoutData04E.Anchors(e, new[] { "bay" })), Is.EqualTo(FrozenEight[i, 2]), id + " anchors changed");
            }
            VenueExpanded04C garage = Expanded("garage");
            Assert.That(garage.anchors[garage.anchors.Length - 1].id, Is.EqualTo("bay"), "The garage bay anchor is appended last");
            Vector3 bay = V(garage.anchors[garage.anchors.Length - 1].position);
            Assert.That(Mathf.Abs(bay.x), Is.LessThan(.5f), "The bay anchor is on the vehicle bay centre line");
            Assert.That(bay.z, Is.InRange(1f, 14f), "inside the vehicle bay keep-out");
        }

        // ------------------------------------------------------------------ the arrival plan

        [Test]
        public void ArrivalPlanOnTheDefaultCityPlacesEveryPieceSafely()
        {
            ArrivalPlan04E plan = ArrivalPlan04E.Make(City, Bindings, null);
            Poi home = City.FindPoi("Veyra Lofts");
            Vector3 arrival = plan.Arrival;
            foreach (ArrivalPiece04E piece in plan.Pieces)
            {
                Assert.That(piece.Placed, Is.True, piece.Id + ": " + piece.Reason);
                if (piece.Overhead) continue;
                Assert.That(City.RoadAt(piece.Position, .4f), Is.Null, piece.Id + " is beside a road");
                Assert.That(City.IsInsideBuilding(piece.Position, 0f), Is.False, piece.Id);
                Block block = City.BlockAt(piece.Position);
                Assert.That(block != null && block.Inner.Contains(piece.Position), Is.True, piece.Id + " keeps off the public sidewalk");
                foreach (Poi poi in City.Pois) Assert.That(MathUtil.FlatDistance(poi.Position, piece.Position), Is.GreaterThan(2f), piece.Id + " at " + poi.Name);
                Assert.That(NearestWalkLink(City, piece.Position), Is.GreaterThan(.9f), piece.Id + " on a pedestrian path");
                foreach (PropSpot prop in City.Props) Assert.That(MathUtil.FlatDistance(prop.Pos, piece.Position), Is.GreaterThan(.7f), piece.Id + " on a " + prop.Kind);
                foreach (VenueBinding04C v in Bindings)
                {
                    if (v.Building == null || piece.Id == "lofts_rug") continue;
                    Vector3 l = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, piece.Position);
                    Assert.That(Mathf.Abs(l.x) <= 2.6f + Mathf.Min(piece.Half.x, piece.Half.z) && l.z > -7.5f && l.z < .6f, Is.False, piece.Id + " stands in front of the " + v.Definition.Id + " door");
                }
            }
            // The receiver mast is in the first view: within 40 degrees of straight ahead from the arrival, under 30 m.
            ArrivalPiece04E mast = plan.Find("dispatch_mast");
            Vector3 toMast = mast.Position - arrival; toMast.y = 0f;
            Assert.That(Vector3.Angle(home.Facing, toMast), Is.LessThan(40f), "Mast bearing from the arrival");
            Assert.That(toMast.magnitude, Is.LessThan(30f));
            Assert.That(plan.RoofCrownPlaced, Is.True);
            Assert.That(plan.RoofCrown.y, Is.EqualTo(CityLayout.CurbHeight + Bound("dispatch").Building.Height).Within(.01f));
            // Nessa: beside the Lofts door on its forecourt, facing the arrival, never in the doorway itself.
            Assert.That(plan.NessaPostPlaced, Is.True);
            Vector3 nessaLocal = InhabitedCityBindings04C.WorldToLocal(Bound("lofts").Origin, Bound("lofts").Yaw, plan.NessaPost);
            Assert.That(Mathf.Abs(nessaLocal.x), Is.InRange(2.6f, 4.5f));
            Assert.That(nessaLocal.z, Is.LessThan(0f));
            Vector3 toArrival = arrival - plan.NessaPost; toArrival.y = 0f;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(plan.NessaYaw, MathUtil.YawOf(toArrival))), Is.LessThan(25f), "Nessa faces the arrival");
            // The 04D case keeps its first choice beside the Lofts door.
            Assert.That(plan.CaseSpots, Is.Not.Empty);
            foreach (ArrivalPiece04E piece in plan.Pieces)
                if (!piece.Overhead && piece.Id != "lofts_rug") Assert.That(MathUtil.FlatDistance(piece.Position, plan.CaseSpots[0]), Is.GreaterThan(1.6f), piece.Id);
        }

        [Test]
        public void ConduitRunsAlongTheForecourtFromDispatchToThePressureHouseDoor()
        {
            ArrivalPlan04E plan = ArrivalPlan04E.Make(City, Bindings, null);
            Assert.That(plan.ConduitPlaced, Is.True, plan.ConduitReason);
            Assert.That(plan.ConduitPipe, Has.Count.EqualTo(4));
            VenueBinding04C d = Bound("dispatch"), p = Bound("pressure");
            Assert.That(MathUtil.FlatDistance(plan.ConduitPipe[0], d.Origin), Is.LessThan(8f), "starts at the dispatch frontage");
            Assert.That(MathUtil.FlatDistance(plan.ConduitPipe[3], p.Origin), Is.LessThan(2.5f), "ends beside the Pressure House door");
            for (int s = 0; s < 3; s++)
            {
                Vector3 a = plan.ConduitPipe[s], b = plan.ConduitPipe[s + 1];
                int steps = Mathf.CeilToInt(MathUtil.FlatDistance(a, b) / .25f);
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 q = Vector3.Lerp(a, b, i / (float)Mathf.Max(1, steps));
                    Assert.That(City.RoadAt(q, .25f), Is.Null, "pipe on a road at " + q);
                    Assert.That(City.IsInsideBuilding(q, 0f), Is.False, "pipe in a building at " + q);
                    Block block = City.BlockAt(q);
                    Assert.That(block != null && block.Inner.Contains(q), Is.True, "pipe on the public sidewalk at " + q);
                    foreach (VenueBinding04C v in Bindings)
                    {
                        if (v.Building == null) continue;
                        Vector3 l = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, q);
                        Assert.That(Mathf.Abs(l.x) < ArrivalPlan04E.DoorwayHalf && l.z > -7.5f && l.z < .6f, Is.False, "pipe crosses the " + v.Definition.Id + " door at " + q);
                    }
                    Assert.That(NearestWalkLink(City, q), Is.GreaterThan(.6f), "pipe on a pedestrian path at " + q);
                }
            }
            Assert.That(plan.ConduitRoute, Has.Count.EqualTo(4));
            Assert.That(MathUtil.FlatDistance(plan.ConduitRoute[0], d.Outside), Is.LessThan(.01f));
            Assert.That(MathUtil.FlatDistance(plan.ConduitRoute[3], p.Outside), Is.LessThan(.01f));
            foreach (Vector3 q in plan.ConduitRoute)
            {
                Assert.That(City.RoadAt(q, 0f), Is.Null, "route point on a road " + q);
                Assert.That(City.IsInsideBuilding(q, 0f), Is.False, "route point in a building " + q);
            }
        }

        [Test]
        public void ArrivalPiecesAndConduitClearTheVenueColliders()
        {
            // The same volumes the runtime checks with Physics.CheckBox, against every solid node of the owning venue
            // (04C shell, frontage, sills, piers, vessels and the 04E furniture), computed offline in the venue frame.
            ArrivalPlan04E plan = ArrivalPlan04E.Make(City, Bindings, null);
            foreach (ArrivalPiece04E piece in plan.Pieces)
            {
                if (piece.Overhead || piece.Id == "lofts_rug") continue;
                VenueBinding04C v = Bound(piece.Venue);
                Vector3 c = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, piece.Position);
                float h = Mathf.Min(piece.Half.y, 1.1f);
                Bounds box = new Bounds(new Vector3(c.x, .12f + h, c.z), new Vector3(piece.Half.x * 2f - .08f, h * 2f - .04f, piece.Half.z * 2f - .08f));
                foreach (VenueNode04C n in Expanded(piece.Venue).nodes)
                {
                    if (!n.solid || n.kind == "upper04A") continue;
                    Assert.That(NodeBounds(n).Intersects(box), Is.False, piece.Id + " overlaps " + piece.Venue + "." + n.id);
                }
            }
            Assert.That(plan.ConduitPlaced, Is.True);
            foreach (string id in new[] { "dispatch", "pressure" })
            {
                VenueBinding04C v = Bound(id);
                VenueExpanded04C e = Expanded(id);
                for (int s = 0; s < 3; s++)
                {
                    Vector3 a = plan.ConduitPipe[s], b = plan.ConduitPipe[s + 1];
                    int steps = Mathf.CeilToInt(MathUtil.FlatDistance(a, b) / .25f);
                    for (int i = 0; i <= steps; i++)
                    {
                        Vector3 q = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, Vector3.Lerp(a, b, i / (float)Mathf.Max(1, steps)));
                        if (q.z > 0f || q.z < -8f || Mathf.Abs(q.x) > 30f) continue;
                        Bounds pipe = new Bounds(new Vector3(q.x, .19f, q.z), new Vector3(.22f, .22f, .22f));
                        foreach (VenueNode04C n in e.nodes)
                        {
                            if (!n.solid || n.kind == "upper04A") continue;
                            Assert.That(NodeBounds(n).Intersects(pipe), Is.False, "conduit overlaps " + id + "." + n.id + " at local " + q);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ residents

        [Test]
        public void RoomTonesForTheNewKindsAreBoundedAndLoopWhileTheEightAreUntouched()
        {
            foreach (string kind in VenueIds04E.All)
            {
                float[] data = VenueRoomTones04E.For(kind, 22050);
                Assert.That(data, Has.Length.EqualTo(88200));
                double energy = 0;
                foreach (float v in data)
                {
                    Assert.That(float.IsNaN(v) || float.IsInfinity(v), Is.False);
                    Assert.That(Math.Abs(v), Is.LessThanOrEqualTo(.31f), kind);
                    energy += v * v;
                }
                Assert.That(energy / data.Length, Is.GreaterThan(.001), kind);
                Assert.That(Math.Abs(data[0] - data[data.Length - 1]), Is.LessThan(.03f), kind + " loop seam");
            }
            foreach (string kind in new[] { "lofts", "clinic", "forge", "inn", "archive", "observatory", "garage", "depot" })
                Assert.That(VenueRoomTones04E.For(kind, 22050), Is.EqualTo(InhabitedResidentRules04C.RoomTone(kind, 22050)), kind);
            Assert.That(VenueRoomTones04E.For("dispatch", 22050), Is.Not.EqualTo(VenueRoomTones04E.For("office", 22050)));
        }

        [Test]
        public void NewVenueAssistantsWearDistinctHouseCostumes()
        {
            HashSet<string> silhouettes = new HashSet<string>();
            foreach (string kind in VenueIds04E.All)
            {
                Appearance look = new Appearance();
                InhabitedResidents04C.Look04E(kind, ref look);
                silhouettes.Add(look.Headwear + "/" + look.Jacket + "/" + look.JacketColor + "/" + look.HairStyle + "/" + look.Glasses);
            }
            Assert.That(silhouettes, Has.Count.EqualTo(4));
        }

        // ------------------------------------------------------------------ geometry helpers

        private static Vector3 V(float[] a) { return new Vector3(a[0], a[1], a[2]); }

        private static Block BlockOf(Lot lot)
        {
            foreach (Block b in City.Blocks) if (b.Lots.Contains(lot)) return b;
            return null;
        }

        /// <summary>Unity's Euler order (z, then x, then y), applied without the engine.</summary>
        private static Vector3 Rotate(Vector3 v, float[] euler)
        {
            if (euler == null) return v;
            float ex = euler[0] * Mathf.Deg2Rad, ey = euler[1] * Mathf.Deg2Rad, ez = euler[2] * Mathf.Deg2Rad;
            Vector3 a = new Vector3(v.x * Mathf.Cos(ez) - v.y * Mathf.Sin(ez), v.x * Mathf.Sin(ez) + v.y * Mathf.Cos(ez), v.z);
            Vector3 b = new Vector3(a.x, a.y * Mathf.Cos(ex) - a.z * Mathf.Sin(ex), a.y * Mathf.Sin(ex) + a.z * Mathf.Cos(ex));
            return new Vector3(b.x * Mathf.Cos(ey) + b.z * Mathf.Sin(ey), b.y, -b.x * Mathf.Sin(ey) + b.z * Mathf.Cos(ey));
        }

        private static readonly Dictionary<string, Bounds> ModelBounds = new Dictionary<string, Bounds>();

        /// <summary>Bounds of a packaged mesh (both detail levels), read from its CMH1 bytes exactly as the runtime decodes them.</summary>
        private static Bounds Model(string source, string model)
        {
            string key = source + "/" + model;
            Bounds result;
            if (ModelBounds.TryGetValue(key, out result)) return result;
            string folder = source == "04C" ? "InhabitedCity04C" : source == "04B" ? "LivingTerritories04B" : "VisualRebirth04A";
            bool first = true;
            foreach (string lod in new[] { "_high", "_low" })
            {
                byte[] data = File.ReadAllBytes(Path.Combine(VenueLayoutData04E.Resources, folder + "/Meshes/" + model + lod + ".bytes"));
                Assert.That(data[0] == 'C' && data[1] == 'M' && data[2] == 'H' && data[3] == '1', Is.True, key);
                int count = BitConverter.ToInt32(data, 4);
                for (int i = 0; i < count; i++)
                {
                    int o = 8 + i * 32;
                    Vector3 p = new Vector3(BitConverter.ToSingle(data, o), BitConverter.ToSingle(data, o + 4), BitConverter.ToSingle(data, o + 8));
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
                }
            }
            ModelBounds[key] = result;
            return result;
        }

        private static Bounds NodeBounds(VenueNode04C n)
        {
            Vector3 size = V(n.scale), at = V(n.position);
            Bounds local = n.kind == "mesh" ? Model(n.source, n.model) : new Bounds(Vector3.zero, Vector3.one);
            Bounds world = new Bounds(at, Vector3.zero);
            bool first = true;
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = new Vector3((c & 1) == 0 ? local.min.x : local.max.x, (c & 2) == 0 ? local.min.y : local.max.y, (c & 4) == 0 ? local.min.z : local.max.z);
                Vector3 p = at + Rotate(Vector3.Scale(corner, size), n.euler);
                if (first) { world = new Bounds(p, Vector3.zero); first = false; } else world.Encapsulate(p);
            }
            return world;
        }

        /// <summary>Solid furniture and structure inside the room (floor slabs and the outer original masses excluded).</summary>
        private static List<Bounds> SolidBounds(VenueExpanded04C e)
        {
            List<Bounds> list = new List<Bounds>();
            foreach (VenueNode04C n in e.nodes)
            {
                if (!n.solid || n.kind == "upper04A" || n.id.StartsWith("interior_floor", StringComparison.Ordinal) || n.id == "interior_ceiling") continue;
                list.Add(NodeBounds(n));
            }
            return list;
        }

        /// <summary>The first solid a standing person's capsule (from 0.1 to 1.8 m) would touch, or null.</summary>
        private static string CapsuleHit(List<Bounds> solids, Vector3 feet, float radius)
        {
            Bounds person = new Bounds(feet + Vector3.up * .95f, new Vector3(radius * 2f, 1.7f, radius * 2f));
            for (int i = 0; i < solids.Count; i++)
                if (solids[i].Intersects(person)) return solids[i].ToString();
            return null;
        }

        /// <summary>Length of rail (solid, top at least a metre up) along a line: alongZ true means the line x = value.</summary>
        private static float RailCoverage(VenueExpanded04C e, bool alongZ, float value)
        {
            float x0 = VenueIds04E.StairX0, x1 = VenueIds04E.StairX1, z0 = VenueIds04E.StairZ0, z1 = VenueIds04E.StairZ1;
            float best = 0f;
            foreach (VenueNode04C n in e.nodes)
            {
                if (!n.solid || !n.id.StartsWith("stair_rail", StringComparison.Ordinal)) continue;
                Bounds b = NodeBounds(n);
                if (b.max.y < 1.0f) continue;
                if (alongZ && b.min.x - .08f <= value && b.max.x + .08f >= value)
                    best = Mathf.Max(best, Mathf.Min(b.max.z, z1) - Mathf.Max(b.min.z, z0));
                if (!alongZ && b.min.z - .08f <= value && b.max.z + .08f >= value && b.size.x > .5f)
                    best = Mathf.Max(best, Mathf.Min(b.max.x, x1) - Mathf.Max(b.min.x, x0));
            }
            return best;
        }

        private static float NearestWalkLink(CityLayout city, Vector3 q)
        {
            float best = float.MaxValue;
            foreach (WalkLink l in city.WalkLinks)
            {
                if (l.A < 0 || l.B < 0 || l.A >= city.WalkNodes.Count || l.B >= city.WalkNodes.Count) continue;
                best = Mathf.Min(best, CrystalCaseRules.DistanceToSegment(q, city.WalkNodes[l.A].Pos, city.WalkNodes[l.B].Pos));
            }
            return best;
        }

        /// <summary>Shortest walk along the city's pedestrian graph from a point to a point (metres).</summary>
        private static float WalkDistance(CityLayout city, Vector3 from, Vector3 to)
        {
            int n = city.WalkNodes.Count;
            float[] dist = new float[n];
            bool[] done = new bool[n];
            for (int i = 0; i < n; i++) dist[i] = float.MaxValue;
            int start = city.NearestWalkNode(from, 60f);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            dist[start] = MathUtil.FlatDistance(from, city.WalkNodes[start].Pos);
            for (int iter = 0; iter < n; iter++)
            {
                int best = -1; float bd = float.MaxValue;
                for (int i = 0; i < n; i++) if (!done[i] && dist[i] < bd) { bd = dist[i]; best = i; }
                if (best < 0 || bd > 1500f) break;
                done[best] = true;
                foreach (int li in city.WalkNodes[best].Links)
                {
                    WalkLink l = city.WalkLinks[li];
                    int other = l.A == best ? l.B : l.A;
                    float nd = bd + MathUtil.FlatDistance(city.WalkNodes[best].Pos, city.WalkNodes[other].Pos);
                    if (nd < dist[other]) dist[other] = nd;
                }
            }
            float result = float.MaxValue;
            List<int> near = new List<int>();
            city.WalkNodesNear(to, 40f, near);
            foreach (int i in near) if (dist[i] < float.MaxValue) result = Mathf.Min(result, dist[i] + MathUtil.FlatDistance(city.WalkNodes[i].Pos, to));
            return result;
        }
    }
}
