using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The story, the street races, the Underheart entrance and every quest and event place, resolved offline in the world
    /// the unified game plays: revision 10 (Gothic Civic H5) of the game's seed. The places are worked out with the same
    /// helpers the story scripts call (MissionManager.PoiPos, SitePos, RoadNear, WalkPointIn, StoryMissions.RaceCheckpoints).
    /// The original city is the reference for what "working" looked like. Offline layout maths, not native play.
    /// </summary>
    public sealed class OneGameStoryH8STests
    {
        private CityLayout _civic, _legacy;
        private List<VenueBinding04C> _civicVenues, _legacyVenues;
        private CityLayout _saved;

        private static CityLayout World(int seed, int revision, out List<VenueBinding04C> venues)
        {
            CityLayout world = new CityLayout(seed);
            world.Generate();
            venues = InhabitedCityBindings04C.Select(world, InhabitedCityBindings04CTests.ReadCatalog());
            if (revision != 0) Assert.That(world.ApplyProfile04O((CityRevision04O)revision, venues), Is.True, "revision " + revision + " applies to seed " + seed);
            return world;
        }

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            _civic = World(TestData.DefaultSeed, (int)OneGameH8S.World, out _civicVenues);
            _legacy = World(TestData.DefaultSeed, 0, out _legacyVenues);
        }

        [SetUp]
        public void RememberLayout() { _saved = Game.Layout; }

        [TearDown]
        public void RestoreLayout() { Game.Layout = _saved; }

        private static VenueBinding04C Venue(List<VenueBinding04C> venues, string id) { return venues.Single(v => v.Definition.Id == id); }

        // ------------------------------------------------------------------ the cast

        [Test]
        public void StoryContactsStandBesideTheServicesTheyShareALotWith()
        {
            // In the original city these contacts stand at a service door (Mara at her clinic, Jax at his garage).
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
            foreach (Poi contact in _legacy.Pois)
            {
                if (contact.Type != PoiType.Contact) continue;
                foreach (VenueBinding04C venue in _legacyVenues)
                    if (venue.Building != null && MathUtil.FlatDistance(contact.Position, venue.Origin) <= 12f)
                        pairs.Add(new KeyValuePair<string, string>(contact.Name, venue.Definition.Id));
            }
            Assert.That(pairs.Select(p => p.Key + "/" + p.Value), Is.SupersetOf(new[] { "Mara Solane/clinic", "Jax Calder/garage" }));
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                Poi contact = _civic.FindPoi(pair.Key);
                VenueBinding04C venue = Venue(_civicVenues, pair.Value);
                Assert.That(contact, Is.Not.Null, pair.Key);
                Assert.That(MathUtil.FlatDistance(contact.Position, venue.Origin), Is.LessThanOrEqualTo(12f), pair.Key + " stands beside the " + pair.Value + " door in the unified world, as in the original city");
                Assert.That(_civic.IsInsideBuilding(contact.Position, 0.4f), Is.False, pair.Key + " stands outdoors");
                Assert.That(_civic.NearestWalkNode(contact.Position, 15f), Is.GreaterThanOrEqualTo(0), pair.Key + " is on the walking network");
            }
            // The prologue spawns Mara and Ion at Mara's contact point and walks Luma over to her (StoryMissions M00).
            Poi clinic = _civic.FindPoi("Mara Solane Street Clinic");
            Assert.That(MathUtil.FlatDistance(_civic.FindPoi("Mara Solane").Position, clinic.Position), Is.LessThanOrEqualTo(10f), "Mara waits at her own clinic at the end of the prologue");
        }

        [Test]
        public void AfterThePrologueMalikStandsAtTheClinicAndJaxIsAShortWalk()
        {
            Poi clinic = _civic.FindPoi("Mara Solane Street Clinic");
            Poi jax = _civic.FindPoi("Jax Calder");
            Vector3 stand = PrologueLayout.ClinicStand(clinic.Position, clinic.Facing);
            Vector3 bike = PrologueLayout.ClinicBike(clinic.Position, clinic.Facing);
            Assert.That(_civic.IsInsideBuilding(stand, 0.3f), Is.False, "standing spot is outside");
            Assert.That(_civic.IsInsideBuilding(bike, 0.6f), Is.False, "bike spot is outside");
            List<Vector3> walk = _civic.FindWalkRoute04O(stand, jax.Position);
            Assert.That(walk, Is.Not.Empty, "a walking route to Jax Calder's job");
            Assert.That(Length(walk), Is.LessThan(1200f), "Jax is a short trip away, as in the original city (" + Length(walk).ToString("0") + " m)");
        }

        // ------------------------------------------------------------------ every story place

        private sealed class Place
        {
            public string Label;
            public Vector3 Point;
            public float Margin;
            public bool Road;
            public District? District;
        }

        private static Place At(string label, Vector3 point, float margin = 0.3f) { return new Place { Label = label, Point = point, Margin = margin }; }
        private static Place Car(string label, Vector3 point) { return new Place { Label = label, Point = point, Margin = 1.2f, Road = true }; }
        private static Place Walk(string label, District district, int salt) { return new Place { Label = label, Point = MissionManager.WalkPointIn(district, salt), District = district }; }

        /// <summary>The places the twelve story scripts compute (StoryMissions M00 to M11), in the active layout.</summary>
        private static List<Place> StoryPlaces()
        {
            List<Place> list = new List<Place>();
            Vector3 manor = MissionManager.SitePos("CrystalManor", new Vector3(470f, 0f, 1470f));
            Vector3 block = MissionManager.SitePos("Blockade", new Vector3(300f, 0f, 1230f));
            Vector3 clinic = MissionManager.PoiPos("Mara Solane Street Clinic", new Vector3(0f, 0f, -450f));
            list.Add(At("M00 start", PrologueLayout.PlayerStart(manor)));
            list.Add(At("M00 Third Eye crystal", PrologueLayout.Crystal(manor)));
            list.Add(At("M00 blockade trigger", PrologueLayout.BlockadeTrigger(block)));
            list.Add(At("M00 clinic", clinic));
            list.Add(At("M00 Mara and Ion", MissionManager.PoiPos("Mara Solane", clinic + new Vector3(3f, 0f, 0f))));
            list.Add(At("M01 start (Jax Calder)", MissionManager.PoiPos("Jax Calder", new Vector3(-500f, 0f, -500f))));
            Place arrowStreet = Walk("M01 Vanta Arrow street", District.GlassHeights, 3);
            list.Add(arrowStreet);
            list.Add(Car("M01 Vanta Arrow", MissionManager.RoadNear(arrowStreet.Point)));
            list.Add(At("M01 garage", MissionManager.PoiPos("Jax Calder Customs", MissionManager.PoiPos("Jax Calder", Vector3.zero))));
            Vector3 nico = MissionManager.PoiPos("Nico Ash", new Vector3(650f, 0f, 400f));
            list.Add(At("M02 start (Nico Ash)", nico));
            list.Add(Car("M02 loaner", MissionManager.RoadNear(nico + new Vector3(0f, 0f, 12f))));
            Vector3 dante = MissionManager.PoiPos("Dante Rook", new Vector3(-500f, 0f, 0f));
            list.Add(At("M03 start (Dante Rook)", dante));
            list.Add(Car("M03 king's car", MissionManager.RoadNear(dante + new Vector3(60f, 0f, 30f))));
            list.Add(Car("M03 ride", MissionManager.RoadNear(dante + new Vector3(-8f, 0f, 8f))));
            list.Add(At("M03 meet", MissionManager.PoiPos("Battery Yard Scrap Co.", MissionManager.WalkPointIn(District.AshUnderpass, 5))));
            list.Add(At("M04 start (Mara Solane)", MissionManager.PoiPos("Mara Solane", new Vector3(0f, 0f, -500f))));
            list.Add(Car("M04 ambulance", MissionManager.RoadNear(clinic + new Vector3(0f, 0f, 8f))));
            list.Add(At("M04 canal crash", new Vector3(Mathf.Clamp(clinic.x + 220f, -780f, 780f), -CityLayout.CanalDepth, CityLayout.CanalZ + 3f)));
            list.Add(At("M05 start (June Pyre)", MissionManager.PoiPos("June Pyre", new Vector3(650f, 0f, -300f))));
            list.Add(Walk("M05 treaty", District.AshUnderpass, 11));
            list.Add(At("M06 start (Iris Moon)", MissionManager.PoiPos("Iris Moon", new Vector3(0f, 0f, 500f))));
            Vector3 market = MissionManager.PoiPos("Circuit Ward Night Market", MissionManager.WalkPointIn(District.LasVeyraCore, 4));
            list.Add(At("M06 market", market));
            list.Add(Car("M06 getaway car", MissionManager.RoadNear(market + new Vector3(30f, 0f, -20f))));
            list.Add(At("M07 start (Officer Valez)", MissionManager.PoiPos("Officer Valez", Vector3.zero)));
            for (int i = 0; i < 3; i++) list.Add(Walk("M07 Glass Saint " + (i + 1), District.GlassHeights, 21 + i * 5));
            Poi lofts = Game.Layout.FindPoi("Veyra Lofts");
            Vector3 home = lofts.Position + lofts.Facing.normalized * 6f;
            list.Add(At("M08 and M10 start (Veyra Lofts)", home));
            List<Vector3> masts = new List<Vector3>();
            foreach (Vector3 m in Game.Layout.RadioMastSpots) if (CityLayout.DistrictAt(m) == District.GraveFrequency && masts.Count < 3) masts.Add(m);
            for (int i = masts.Count; i < 3; i++) masts.Add(MissionManager.WalkPointIn(District.GraveFrequency, 40 + i * 9));
            for (int i = 0; i < 3; i++) list.Add(At("M08 transmitter " + (i + 1), masts[i] + new Vector3(3f, 0f, 3f)));
            Vector3 sable = MissionManager.PoiPos("Sable Knox", new Vector3(380f, 0f, 0f));
            list.Add(At("M09 start (Sable Knox)", sable));
            list.Add(Car("M09 armored truck", MissionManager.RoadNear(sable + new Vector3(0f, 0f, 70f))));
            list.Add(Car("M10 van", MissionManager.RoadNear(home + new Vector3(180f, 0f, 60f))));
            list.Add(At("M10 motel", MissionManager.PoiPos("Starlite Motel Room 7", MissionManager.SitePos("StarliteMotel", new Vector3(1290f, 0f, -350f)))));
            list.Add(At("M11 start (Luma)", MissionManager.PoiPos("Luma", new Vector3(1290f, 0f, -340f))));
            list.Add(At("M11 Manor approach", manor + new Vector3(0f, 0f, -55f)));
            return list;
        }

        [Test]
        public void EveryStoryPlaceResolvesOutdoorsOnGroundInTheUnifiedWorld()
        {
            Game.Layout = _civic;
            foreach (Place p in StoryPlaces())
            {
                Assert.That(MathUtil.IsFinite(p.Point), Is.True, p.Label);
                Assert.That(Mathf.Abs(p.Point.x) <= CityLayout.MapHalf && Mathf.Abs(p.Point.z) <= CityLayout.MapHalf, Is.True, p.Label + " is on the map");
                Assert.That(_civic.IsInsideBuilding(p.Point, p.Margin), Is.False, p.Label + " at " + p.Point.ToString("0") + " is not inside a building");
                if (p.District.HasValue)
                {
                    Assert.That(p.Point, Is.Not.EqualTo(Vector3.zero), p.Label + " never falls back to the map origin");
                    Assert.That(CityLayout.DistrictAt(p.Point), Is.EqualTo(p.District.Value), p.Label + " is in its district");
                }
                if (p.Road) Assert.That(_civic.RoadAt(p.Point, 1.5f) != null || _civic.IsOnRoadOrIntersection(p.Point), Is.True, p.Label + " at " + p.Point.ToString("0") + " sits on a road");
            }
        }

        [Test]
        public void StoryVehiclesCanDriveToTheirDestinationsOnTheRoadNetwork()
        {
            Game.Layout = _civic;
            Vector3 dante = MissionManager.PoiPos("Dante Rook", Vector3.zero);
            Vector3 meet = MissionManager.PoiPos("Battery Yard Scrap Co.", Vector3.zero);
            Vector3 clinic = MissionManager.PoiPos("Mara Solane Street Clinic", Vector3.zero);
            Vector3 garage = MissionManager.PoiPos("Jax Calder Customs", Vector3.zero);
            Vector3 sable = MissionManager.PoiPos("Sable Knox", Vector3.zero);
            Vector3 air = MissionManager.SitePos("Airstrip", new Vector3(1340f, 0f, 750f));
            Poi lofts = _civic.FindPoi("Veyra Lofts");
            Vector3 home = lofts.Position + lofts.Facing.normalized * 6f;
            Vector3 canal = new Vector3(Mathf.Clamp(clinic.x + 220f, -780f, 780f), -CityLayout.CanalDepth, CityLayout.CanalZ + 3f);
            KeyValuePair<string, Vector3[]>[] legs =
            {
                new KeyValuePair<string, Vector3[]>("M01 Vanta Arrow to the garage", new[] { MissionManager.RoadNear(MissionManager.WalkPointIn(District.GlassHeights, 3)), garage }),
                new KeyValuePair<string, Vector3[]>("M03 king's car to the meet", new[] { MissionManager.RoadNear(dante + new Vector3(60f, 0f, 30f)), MissionManager.RoadNear(meet) }),
                new KeyValuePair<string, Vector3[]>("M04 ambulance to the canal", new[] { MissionManager.RoadNear(clinic + new Vector3(0f, 0f, 8f)), canal }),
                new KeyValuePair<string, Vector3[]>("M04 ambulance back to the clinic", new[] { canal, clinic }),
                new KeyValuePair<string, Vector3[]>("M09 truck to the airstrip", new[] { MissionManager.RoadNear(sable + new Vector3(0f, 0f, 70f)), air }),
                new KeyValuePair<string, Vector3[]>("M09 truck to the garage", new[] { MissionManager.RoadNear(sable + new Vector3(0f, 0f, 70f)), garage }),
                new KeyValuePair<string, Vector3[]>("M10 van to the airstrip", new[] { MissionManager.RoadNear(home + new Vector3(180f, 0f, 60f)), MissionManager.RoadNear(air + new Vector3(-80f, 0f, 0f)) })
            };
            foreach (KeyValuePair<string, Vector3[]> leg in legs)
            {
                List<Vector3> route = _civic.FindRoute(leg.Value[0], leg.Value[1]);
                Assert.That(route, Is.Not.Empty, leg.Key + " has a road route");
                Assert.That(Length(route), Is.LessThan(3500f), leg.Key + " (" + Length(route).ToString("0") + " m)");
            }
            // The ambulance's bleeding-out timer is 210 s from the canal to the clinic: even at 10 m/s that leaves time.
            List<Vector3> back = _civic.FindRoute(canal, clinic);
            Assert.That(Length(back), Is.LessThan(1800f), "canal to clinic " + Length(back).ToString("0") + " m");
            // Valez's window is 360 s for three Glass Saints: the tour from her door stays well inside a 12 m/s average.
            Vector3 previous = MissionManager.PoiPos("Officer Valez", Vector3.zero);
            float tour = 0f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 saint = MissionManager.WalkPointIn(District.GlassHeights, 21 + i * 5);
                tour += MathUtil.FlatDistance(previous, saint);
                previous = saint;
            }
            Assert.That(tour, Is.LessThan(2400f), "Glass Saints tour " + tour.ToString("0") + " m");
        }

        [Test]
        public void VehicleDeliveriesIntoTheCourtCanBeDriven()
        {
            // Three story deliveries end inside the central court of the unified world, which has no streets of its own.
            CourtReachH8S court = new CourtReachH8S(_civic, _civicVenues, -200f, -170f, 200f, 280f);
            Vector3 garage = _civic.FindPoi("Jax Calder Customs").Position, clinic = _civic.FindPoi("Mara Solane Street Clinic").Position;
            Check(court, "M01 Vanta Arrow", "Vanta Arrow", garage, 9f);
            Check(court, "M04 ambulance", "Harbor Lifeline Ambulance", clinic, 10f);
            Check(court, "M09 armored truck", "Anvil Sentinel Armored Truck", garage, 10f);
        }

        private static void Check(CourtReachH8S court, string label, string vehicle, Vector3 target, float deliveryRadius)
        {
            VehicleDef def = VehicleDb.Find(vehicle);
            Assert.That(def, Is.Not.Null, vehicle);
            float radius = def.Width * 0.5f + 0.45f;
            float closest = court.ClosestApproach(radius, target);
            Assert.That(closest, Is.LessThanOrEqualTo(deliveryRadius - 1f), label + " (" + def.Width + " m wide) gets within " + closest.ToString("0.0") + " m of a " + deliveryRadius + " m delivery marker");
        }

        // ------------------------------------------------------------------ races

        /// <summary>The start line Activities picks: a long straight lane near the point, preferring one facing the first checkpoint.</summary>
        private static Vector3 RaceStart(CityLayout layout, Vector3 near, Vector3 toward, out float yaw)
        {
            List<int> lanes = new List<int>();
            layout.LanesNear(near, 140f, lanes);
            int best = -1;
            float bestScore = float.MinValue, bestAlong = 0f;
            foreach (int k in lanes)
            {
                LanePath lane = layout.Lanes[k];
                if (lane.IsConnector || lane.Length < 70f) continue;
                float along = Mathf.Clamp(lane.Project(near), 32f, lane.Length - 32f);
                Vector3 p = lane.PointAt(along);
                float d = MathUtil.FlatDistance(p, near);
                if (d > 140f) continue;
                Vector3 to = MathUtil.Flat(toward - p);
                float align = to.sqrMagnitude > 1f ? Vector3.Dot(lane.DirectionAt(along), to.normalized) : 0f;
                float score = -d + align * 70f + (lane.LanesInDir >= 2 ? 40f : 0f) + (lane.LeftNeighbor >= 0 || lane.RightNeighbor >= 0 ? 20f : 0f);
                if (score > bestScore) { bestScore = score; best = k; bestAlong = along; }
            }
            if (best < 0)
            {
                Vector3 road = MissionManager.RoadNear(near);
                yaw = MissionManager.RoadYawNear(road);
                return road;
            }
            yaw = MathUtil.YawOf(layout.Lanes[best].DirectionAt(bestAlong));
            return layout.Lanes[best].PointAt(bestAlong);
        }

        /// <summary>Checkpoints follow real roads: no reversal between legs (from the start heading), none off the road or in a building.</summary>
        private static void CheckRace(CityLayout layout, string label, Vector3 start, float yaw, Vector3[] rawWay, float spacing)
        {
            Vector3[] way = new Vector3[rawWay.Length];
            for (int i = 0; i < way.Length; i++) way[i] = MissionManager.RoadNear(rawWay[i]);
            List<Vector3> checkpoints = StoryMissions.RaceCheckpoints(start, way, spacing);
            Assert.That(checkpoints.Count, Is.GreaterThan(4), label);
            Vector3 previous = start, heading = MathUtil.DirFromYaw(yaw);
            float total = 0f;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                Vector3 leg = MathUtil.Flat(checkpoints[i] - previous);
                float gap = leg.magnitude;
                total += gap;
                Assert.That(gap, Is.LessThanOrEqualTo(spacing + 1f), label + " checkpoint " + i + " gap");
                if (gap > 1f)
                {
                    leg /= gap;
                    Assert.That(Vector3.Dot(leg, heading), Is.GreaterThan(-0.5f), label + " turns back on itself at checkpoint " + i + " " + checkpoints[i].ToString("0"));
                    heading = leg;
                }
                Assert.That(layout.RoadAt(checkpoints[i], 2f) != null || layout.IsOnRoadOrIntersection(checkpoints[i]), Is.True, label + " checkpoint " + i + " " + checkpoints[i].ToString("0") + " is on a road");
                Assert.That(layout.IsInsideBuilding(checkpoints[i], 0.5f), Is.False, label + " checkpoint " + i + " " + checkpoints[i].ToString("0") + " is not inside a building");
                previous = checkpoints[i];
            }
            Assert.That(total, Is.LessThan(6000f), label + " length " + total.ToString("0") + " m");
        }

        [TestCase(0)]
        [TestCase(10)]
        public void CityStreetRacesRunOnRealRoadsWithoutTurningBack(int revision)
        {
            CityLayout layout = revision == 0 ? _legacy : _civic;
            Game.Layout = layout;
            CityRaceRouteH8S[] races = CityRacesH8S.StreetRaces(layout.GothicCivicH5);
            Assert.That(races.Select(r => r.Id), Is.EqualTo(new[] { "race_strip", "race_canal", "race_skyrail" }), "ids and order never change (saves remember won races)");
            CityRaceRouteH8S[] original = CityRacesH8S.StreetRaces(false);
            for (int i = 0; i < races.Length; i++)
            {
                Assert.That(races[i].Name, Is.EqualTo(original[i].Name), races[i].Id + " keeps its name");
                Assert.That(races[i].Rivals, Is.EqualTo(original[i].Rivals), races[i].Id + " keeps its rivals");
                Assert.That(races[i].Prize, Is.EqualTo(original[i].Prize), races[i].Id + " keeps its prize");
                foreach (string rival in races[i].Rivals) Assert.That(VehicleDb.Find(rival), Is.Not.Null, races[i].Id + " rival " + rival);
            }
            foreach (CityRaceRouteH8S race in races)
            {
                Assert.That(race.Intro, Is.Not.Empty, race.Id);
                float yaw;
                Vector3 start = RaceStart(layout, race.Near, MissionManager.RoadNear(race.Way[0]), out yaw);
                CheckRace(layout, race.Id + " in revision " + revision, start, yaw, race.Way, 160f);
            }
        }

        [TestCase(0)]
        [TestCase(10)]
        public void TheStoryRaceGhostLanesRunsOnRealRoadsWithoutTurningBack(int revision)
        {
            CityLayout layout = revision == 0 ? _legacy : _civic;
            Game.Layout = layout;
            Vector3 nico = MissionManager.PoiPos("Nico Ash", new Vector3(650f, 0f, 400f));
            Vector3[] way = CityRacesH8S.GhostLanes(nico, layout.GothicCivicH5);
            float yaw;
            Vector3 loaner = CityRacesH8S.GhostLanesStart(layout, nico, MissionManager.RoadNear(way[0]), layout.GothicCivicH5, out yaw);
            float along;
            Assert.That(layout.NearestLane(loaner, 1f, out along, true), Is.GreaterThanOrEqualTo(0), "Nico's loaner sits on a road lane");
            Assert.That(MathUtil.FlatDistance(loaner, nico), Is.LessThan(40f), "the loaner waits at Nico's corner");
            CheckRace(layout, "Ghost Lanes in revision " + revision, loaner, yaw, way, 170f);
        }

        // ------------------------------------------------------------------ underground, quests and events

        [TestCase(61724)]
        [TestCase(1)]
        [TestCase(2016)]
        [TestCase(77231)]
        [TestCase(92017)]
        public void TheUnderheartEntranceFitsTheUnifiedWorldsPressureHouse(int seed)
        {
            List<VenueBinding04C> venues;
            CityLayout world = World(seed, (int)OneGameH8S.World, out venues);
            VenueBinding04C pressure = Venue(venues, VenueIds04E.Pressure);
            UnderheartPlanner.Choice choice = UnderheartPlanner.Choose(world, new UnderheartFrame(pressure.Origin.x, pressure.Origin.z, pressure.Yaw), true);
            Assert.That(choice.Layout, Is.Not.Null, string.Join(";", choice.Rejected));
            Assert.That(UnderheartRules.Validate(choice.Layout), Is.Empty);
            Assert.That(choice.Layout.Anchor("service.clamp_hollow"), Is.Not.Null);
            if (seed == TestData.DefaultSeed) Assert.That(choice.Layout.Portal, Is.Not.Null, "the vehicle portal is planned in the game's own city");
        }

        private static List<KeyValuePair<string, T>> Load<T>(string folder) where T : class, new()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, T>> files = QuestTestData.Load<T>(folder, errors, null);
            Assert.That(errors, Is.Empty);
            Assert.That(files, Is.Not.Empty, folder);
            return files;
        }

        [Test]
        public void EveryQuestPlaceResolvesInTheUnifiedWorld()
        {
            int checkedPlaces = 0;
            foreach (KeyValuePair<string, QuestFile> file in Load<QuestFile>("Quests"))
            {
                foreach (GiverDef giver in file.Value.Givers)
                {
                    if (CampaignRules04E.IsPlaceToken(giver.At)) continue;
                    Vector3 p;
                    Assert.That(QuestPlaces.GiverPosition(_civic, giver.At, giver.OffsetX, giver.OffsetZ, out p), Is.True, file.Key + " giver " + giver.Name);
                    Assert.That(Reachable(_civic, p), Is.True, file.Key + " giver " + giver.Name + " at " + p.ToString("0"));
                    checkedPlaces++;
                }
                foreach (QuestDef quest in file.Value.Quests)
                {
                    if (!QuestLogic.WorldMatches(quest.Requires, (int)OneGameH8S.World)) continue;
                    Vector3 giver = Vector3.zero;
                    if (!string.IsNullOrEmpty(quest.Giver) && !CampaignRules04E.IsPlaceToken(quest.Giver))
                        Assert.That(QuestPlaces.GiverPosition(_civic, quest.Giver, quest.GiverOffsetX, quest.GiverOffsetZ, out giver), Is.True, quest.Id + " giver " + quest.Giver);
                    foreach (QuestPhase phase in quest.Phases)
                        foreach (QuestStep step in phase.Steps)
                            foreach (KeyValuePair<string, Vector2> at in new[] { new KeyValuePair<string, Vector2>(step.At, new Vector2(step.OffsetX, step.OffsetZ)), new KeyValuePair<string, Vector2>(step.To, new Vector2(step.ToOffsetX, step.ToOffsetZ)) })
                            {
                                if (string.IsNullOrEmpty(at.Key) || at.Key == "Giver" || at.Key == "Previous" || at.Key == "Player" || at.Key == "World" || CampaignRules04E.IsPlaceToken(at.Key)) continue;
                                Vector3 p;
                                Assert.That(QuestPlaces.Resolve(_civic, at.Key, at.Value.x, at.Value.y, giver, giver, false, Vector3.zero, 7, out p), Is.True, quest.Id + " " + step.Type + " at " + at.Key);
                                Assert.That(Reachable(_civic, p), Is.True, quest.Id + " " + step.Type + " at " + at.Key + " " + p.ToString("0"));
                                checkedPlaces++;
                            }
                }
            }
            Assert.That(checkedPlaces, Is.GreaterThan(100));
        }

        /// <summary>Outdoors, or on the walking network (the Hearth Witness lens stands on the revision 9 and 10 home porch).</summary>
        private static bool Reachable(CityLayout layout, Vector3 p)
        {
            if (!MathUtil.IsFinite(p)) return false;
            if (!layout.IsInsideBuilding(p, 0.3f)) return true;
            int node = layout.NearestWalkNode(p, 4f);
            return node >= 0;
        }

        [Test]
        public void NoWorldEventLosesItsPlacesInTheUnifiedWorld()
        {
            List<Vector3> into = new List<Vector3>();
            int pairs = 0;
            foreach (KeyValuePair<string, EventFile> file in Load<EventFile>("Events"))
                foreach (EventTemplate template in file.Value.Events)
                {
                    List<District> districts = new List<District>();
                    foreach (string name in template.Districts) { District d; if (QuestPlaces.TryDistrict(name, out d)) districts.Add(d); }
                    if (districts.Count == 0) districts.AddRange((District[])Enum.GetValues(typeof(District)));
                    string anchor = string.IsNullOrEmpty(template.Anchor) ? "Any" : template.Anchor;
                    foreach (District district in districts)
                    {
                        bool before = HasCandidates(_legacy, anchor, district, into), after = HasCandidates(_civic, anchor, district, into);
                        if (before) Assert.That(after, Is.True, template.Id + " (" + anchor + ") keeps a place in " + district);
                        pairs++;
                    }
                }
            Assert.That(pairs, Is.GreaterThan(60));
        }

        private static bool HasCandidates(CityLayout layout, string anchor, District district, List<Vector3> into)
        {
            Vector3 centre = Vector3.zero;
            int count = 0;
            foreach (WalkNode node in layout.WalkNodes) if (CityLayout.DistrictAt(node.Pos) == district) { centre += node.Pos; count++; }
            if (count > 0) centre /= count;
            else centre = district == District.QuartzBadlands ? new Vector3(1200f, 0f, 0f) : district == District.Frostline ? new Vector3(-1300f, 0f, 1300f) : Vector3.zero;
            QuestPlaces.AnchorCandidates(layout, anchor, centre, 0f, 4000f, into);
            foreach (Vector3 p in into) if (CityLayout.DistrictAt(p) == district) return true;
            return false;
        }

        private static float Length(List<Vector3> route)
        {
            float total = 0f;
            for (int i = 1; i < route.Count; i++) total += MathUtil.FlatDistance(route[i - 1], route[i]);
            return total;
        }
    }
}
