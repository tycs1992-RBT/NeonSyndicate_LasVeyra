using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The Underheart (04E), engine-free: every layout variant is structurally sound (walkable stairs and ramps, headroom,
    /// vehicle cells, no overlapping volumes, every named anchor on a floor inside its cell, connected from the stair with
    /// every shortcut closed, each region with a critical path, a side branch, a landmark, a safe rest point and a link
    /// on), the venue frame maths, routing through doors, gates and lifts, heat and vent cycles, the camera limits, the
    /// ground openings (WorldApertures), the surface portal survey on the default city, the underground palettes, and
    /// the append-only save block.
    /// </summary>
    public class Underheart04ETests
    {
        private static CityLayout _city;

        private static CityLayout City
        {
            get
            {
                if (_city == null)
                {
                    _city = new CityLayout(61724);
                    _city.Generate();
                }
                return _city;
            }
        }

        [SetUp]
        public void SetUp() { WorldApertures.ResetStatics(); }

        [TearDown]
        public void TearDown() { WorldApertures.ResetStatics(); }

        // ================================================================== structure

        [Test]
        public void EveryVariantIsStructurallySound()
        {
            for (int v = 0; v < UnderheartLayout.VariantCount; v++)
            {
                UnderheartLayout L = UnderheartLayout.Create(v);
                List<string> problems = UnderheartRules.Validate(L);
                Assert.That(problems, Is.Empty, "variant " + v + ": " + (problems.Count > 0 ? problems[0] : ""));
                Assert.That(L.Variant, Is.EqualTo(v));
            }
        }

        [Test]
        public void TheLayoutIsDeterministic()
        {
            UnderheartLayout a = UnderheartLayout.Create(4), b = UnderheartLayout.Create(4);
            Assert.That(a.Cells.Count, Is.EqualTo(b.Cells.Count));
            for (int i = 0; i < a.Cells.Count; i++) Assert.That(a.Cells[i].ToString(), Is.EqualTo(b.Cells[i].ToString()));
            for (int i = 0; i < a.Anchors.Count; i++)
            {
                Assert.That(a.Anchors[i].Id, Is.EqualTo(b.Anchors[i].Id));
                Assert.That(a.Anchors[i].Pos, Is.EqualTo(b.Anchors[i].Pos));
            }
        }

        [Test]
        public void EveryNamedAnchorExistsOnAFloorInsideItsCell()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            foreach (string id in UnderheartIds.Anchors)
            {
                if (id == "portal.top") continue;
                UhAnchor a = L.Anchor(id);
                Assert.That(a, Is.Not.Null, "anchor " + id);
                Assert.That(a.Cell, Is.GreaterThanOrEqualTo(0), id + " has a cell");
                UhCell c = L.Cells[a.Cell];
                Assert.That(c.ContainsXZ(a.X, a.Z, -0.4f), Is.True, id + " is inside " + c.Id);
                Assert.That(a.Y, Is.EqualTo(c.FloorAt(a.X, a.Z)).Within(0.05f), id + " stands on the floor");
                Assert.That(UnderheartIds.RegionOfAnchor(id), Is.EqualTo(c.Region), id + " is in its region");
            }
            Assert.That(L.Anchors.Count, Is.EqualTo(UnderheartIds.Anchors.Length - 1), "every frozen anchor but the surface portal top");
        }

        [Test]
        public void EverythingIsReachableFromTheStairWithEveryShortcutClosed()
        {
            UnderheartLayout L = UnderheartLayout.Create(2);
            bool[] closed = UnderheartRules.Reach(L, L.StairCell, s => false, false);
            foreach (UhCell c in L.Cells) Assert.That(closed[c.Index], Is.True, c.Id + " reachable with the shortcuts closed");
            foreach (UhAnchor a in L.Anchors) Assert.That(closed[a.Cell], Is.True, a.Id + " reachable with the shortcuts closed");
        }

        [Test]
        public void EachRegionHasACriticalPathABranchALandmarkARestPointAndALink()
        {
            UnderheartLayout L = UnderheartLayout.Create(1);
            for (int r = 1; r < UnderheartIds.Regions.Length; r++)
            {
                string id = UnderheartIds.Regions[r];
                UhRegionInfo info = L.Region(id);
                Assert.That(info, Is.Not.Null, id);
                List<int> path = UnderheartRules.PathWithin(L, info.EntryCell, info.ExitCell, id);
                Assert.That(path, Is.Not.Null, id + " critical path");
                bool branch = false;
                foreach (UhCell c in L.Cells) if (c.Region == id && c.Optional && !path.Contains(c.Index)) branch = true;
                Assert.That(branch, Is.True, id + " optional branch");
                bool landmark = false;
                foreach (UhLandmark m in L.Landmarks) if (m.Region == id && m.Cell >= 0) landmark = true;
                Assert.That(landmark, Is.True, id + " landmark");
                UhAnchor rest = L.Anchor(info.Rest);
                Assert.That(rest, Is.Not.Null, id + " rest point");
                Assert.That(L.Cells[rest.Cell].Safe, Is.True, id + " rest point is in a safe room");
                Assert.That(info.Depth, Is.GreaterThan(10f), id + " depth band");
                if (!string.IsNullOrEmpty(info.Next))
                {
                    bool link = false;
                    foreach (UhPortal d in L.Portals)
                    {
                        string ra = L.Cells[d.A].Region, rb = L.Cells[d.B].Region;
                        if (d.Kind == UhPortalKind.Walk && ((ra == id && rb == info.Next) || (rb == id && ra == info.Next))) link = true;
                    }
                    Assert.That(link, Is.True, id + " has a doorway on to " + info.Next);
                }
            }
        }

        [Test]
        public void StairsRampsHeadroomAndVehicleCellsAreWithinTheRules()
        {
            UnderheartLayout L = UnderheartLayout.Create(3);
            int stairs = 0, ramps = 0, vehicles = 0;
            foreach (UhCell c in L.Cells)
            {
                Assert.That(c.Height, Is.GreaterThanOrEqualTo(UnderheartRules.MinHeadroom - 0.001f), c.Id + " headroom");
                if (c.Kind == UhCellKind.Stair)
                {
                    stairs++;
                    float drop = Mathf.Abs(c.FloorHi - c.FloorLo);
                    int n = Mathf.Max(1, Mathf.CeilToInt(drop / UnderheartRules.MaxRiser - 0.0001f));
                    Assert.That(drop / n, Is.LessThanOrEqualTo(UnderheartRules.MaxRiser + 0.0005f), c.Id + " riser");
                    Assert.That(c.SlopeLength / n, Is.GreaterThanOrEqualTo(UnderheartRules.MinTread - 0.001f), c.Id + " tread");
                }
                else if (c.Kind == UhCellKind.Ramp)
                {
                    ramps++;
                    Assert.That(c.SlopeDegrees, Is.LessThanOrEqualTo(UnderheartRules.MaxRampDegrees + 0.01f), c.Id + " ramp angle");
                }
                else Assert.That(c.Sloped, Is.False, c.Id + " is flat");
                if (c.Vehicle)
                {
                    vehicles++;
                    Assert.That(c.Width, Is.GreaterThanOrEqualTo(UnderheartRules.VehicleMinWidth - 0.001f), c.Id + " vehicle width");
                    Assert.That(c.Height, Is.GreaterThanOrEqualTo(UnderheartRules.VehicleMinHeight - 0.001f), c.Id + " vehicle height");
                }
            }
            Assert.That(stairs, Is.GreaterThan(4));
            Assert.That(ramps, Is.GreaterThan(2));
            Assert.That(vehicles, Is.GreaterThanOrEqualTo(3), "the transit tunnels");
        }

        [Test]
        public void TheServiceLevelStaysUnderTheStreetSlabAndMeetsTheStair()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            UhCell stair = L.Cells[L.StairCell];
            Assert.That(stair.Id, Is.EqualTo("service.stair"));
            Assert.That(stair.MaxFloor, Is.EqualTo(CityLayout.CurbHeight).Within(0.001f), "the stair starts at the venue floor");
            Assert.That(stair.MinFloor, Is.EqualTo(CityLayout.CurbHeight - VenueIds04E.StairDrop).Within(0.001f), "6.15 m down");
            Assert.That(stair.X0, Is.EqualTo(VenueIds04E.StairX0).Within(0.001f));
            Assert.That(stair.X1, Is.EqualTo(VenueIds04E.StairX1).Within(0.001f));
            foreach (UhCell c in L.Cells)
            {
                if (c.Index == L.StairCell || c.Kind == UhCellKind.Cut) continue;
                Assert.That(c.Top, Is.LessThanOrEqualTo(UnderheartRules.SlabLimit + 0.001f), c.Id + " stays under the street slab");
            }
            UhCell hall = L.Cell("service.hall");
            Assert.That(hall.FloorLo, Is.EqualTo(-6f).Within(0.001f), "service floor");
        }

        [Test]
        public void NoTwoCellVolumesOverlap()
        {
            UnderheartLayout L = UnderheartLayout.Create(5);
            for (int i = 0; i < L.Cells.Count; i++)
                for (int j = i + 1; j < L.Cells.Count; j++)
                {
                    UhCell a = L.Cells[i], b = L.Cells[j];
                    float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1), gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
                    if (gx < -0.01f && gz < -0.01f)
                        Assert.That(UnderheartRules.VerticalGap(a, b), Is.GreaterThanOrEqualTo(UnderheartRules.StackGap), a.Id + " and " + b.Id + " are stacked with rock between them");
                }
        }

        [Test]
        public void DepthsFollowTheBriefAndStayAboveTheFallPlane()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            float deepest = 0f;
            foreach (UhCell c in L.Cells) deepest = Mathf.Min(deepest, c.MinFloor);
            Assert.That(deepest, Is.GreaterThan(WorldBounds.KillY + 100f));
            Assert.That(L.Cell("refuge.hall").FloorLo, Is.EqualTo(-18f).Within(0.5f));
            Assert.That(L.Cell("canals.channel").FloorLo, Is.EqualTo(-26f).Within(1f));
            Assert.That(L.Cell("gardens.cavern").MinFloor, Is.EqualTo(-40f).Within(4f));
            Assert.That(L.Cell("gardens.cavern").Height, Is.GreaterThanOrEqualTo(20f), "a tall cavern");
            Assert.That(L.Cell("crown.chamber").MinFloor, Is.EqualTo(-95f).Within(4f));
            Assert.That(L.Cell("crown.chamber").SizeX * L.Cell("crown.chamber").SizeZ, Is.GreaterThan(4000f), "a vast chamber");
        }

        [Test]
        public void ThereAreAtLeastThreeShortcutsAndEachReturnsUpward()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            Assert.That(L.Shortcuts.Count, Is.GreaterThanOrEqualTo(3));
            foreach (string id in UnderheartIds.Shortcuts)
            {
                UhShortcut s = L.Shortcut(id);
                Assert.That(s, Is.Not.Null, id);
                UhAnchor deep = L.Anchor(s.DeepAnchor);
                Assert.That(deep, Is.Not.Null, id + " deep end");
                Assert.That(s.FarY, Is.GreaterThanOrEqualTo(deep.Y - 0.01f), id + " returns upward");
                if (s.Kind == UhShortcutKind.Gate) Assert.That(L.Portals[s.Portal].Kind, Is.EqualTo(UhPortalKind.Gate), id + " has its gate");
            }
        }

        [Test]
        public void TheSixRestPointsAreInSafeRooms()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            Assert.That(UnderheartIds.RestPoints.Length, Is.EqualTo(6));
            foreach (string id in UnderheartIds.RestPoints)
            {
                UhAnchor a = L.Anchor(id);
                Assert.That(a, Is.Not.Null, id);
                Assert.That(L.Cells[a.Cell].Safe, Is.True, id + " is in a safe room");
                foreach (UhHazard h in L.Hazards)
                    if (h.Cell == a.Cell) Assert.That(h.Heat, Is.EqualTo(0f), id + " room has no damaging heat");
            }
        }

        [Test]
        public void EveryRegionEntryAnchorExists()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            foreach (string region in UnderheartIds.Regions)
            {
                string entry = UnderheartWorld.EntryAnchorOf(region);
                Assert.That(L.Anchor(entry), Is.Not.Null, region + " entry " + entry);
                Assert.That(UnderheartIds.RegionOfAnchor(entry), Is.EqualTo(region), entry + " belongs to " + region);
            }
        }

        [Test]
        public void RefugesKeepVehiclesOut()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            int transit = L.CellIndex("canals.transit");
            bool[] byCar = UnderheartRules.Reach(L, transit, s => true, true);
            foreach (UhCell c in L.Cells)
                if (c.Safe) Assert.That(byCar[c.Index], Is.False, c.Id + " cannot be driven into");
            Assert.That(byCar[L.CellIndex("workshops.convoy")], Is.True, "the transit tunnel reaches the convoy corridor");
        }

        // ================================================================== frame and routing

        [Test]
        public void TheVenueFrameRoundTripsForEveryQuarterTurn()
        {
            for (int q = 0; q < 4; q++)
            {
                UnderheartFrame f = new UnderheartFrame(-120.5f, 64.25f, q * 90f);
                Assert.That(f.Quarter, Is.EqualTo(q));
                Vector3 p = new Vector3(13.5f, -26f, 91.25f);
                Vector3 back = f.ToLocal(f.ToWorld(p));
                Assert.That((back - p).magnitude, Is.LessThan(0.0001f), "quarter " + q);
                Vector3 dir = f.DirToWorld(Vector3.forward);
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                Assert.That(Mathf.DeltaAngle(yaw, f.YawToWorld(0f)), Is.EqualTo(0f).Within(0.01f), "yaw matches the rotated forward, quarter " + q);
                RectF w = f.RectToWorld(1f, 2f, 5f, 9f);
                RectF l = f.RectToLocal(w);
                Assert.That(l.X0, Is.EqualTo(1f).Within(0.001f));
                Assert.That(l.Z1, Is.EqualTo(9f).Within(0.001f));
                Assert.That(w.Width * w.Depth, Is.EqualTo(28f).Within(0.01f));
            }
        }

        [Test]
        public void RoutesUseDoorsAndOnlyOpenGatesAndLifts()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            List<UnderheartRules.RouteStep> steps = new List<UnderheartRules.RouteStep>();
            UhAnchor bottom = L.Anchor("service.stair_bottom"), staging = L.Anchor("crown.staging");
            Assert.That(UnderheartRules.Route(L, bottom.Cell, bottom.Pos, staging.Cell, staging.Pos, s => false, steps), Is.True, "the walk down to the Foundry Crown");
            int walkSteps = steps.Count;
            Assert.That(walkSteps, Is.GreaterThan(10));
            foreach (UnderheartRules.RouteStep s in steps) Assert.That(s.Lift, Is.Null, "no lift while every shortcut is closed");
            // With the crown lift open the way down is shorter and rides it.
            Assert.That(UnderheartRules.Route(L, bottom.Cell, bottom.Pos, staging.Cell, staging.Pos, s => s == "sc_crown_lift", steps), Is.True);
            bool rode = false;
            foreach (UnderheartRules.RouteStep s in steps) if (s.Lift == "sc_crown_lift") rode = true;
            Assert.That(rode, Is.True, "the open lift is used");
            Assert.That(steps.Count, Is.LessThan(walkSteps));
            // The canal gate: closed, the stair side must go the long way; open, it walks straight through.
            UhCell scb = L.Cell("canals.scbottom");
            UhAnchor valve = L.Anchor("canals.valve_room");
            Vector3 from = new Vector3(scb.CenterX, scb.FloorLo, scb.CenterZ);
            Assert.That(UnderheartRules.Route(L, scb.Index, from, valve.Cell, valve.Pos, s => false, steps), Is.True, "around the closed gate");
            int around = steps.Count;
            Assert.That(UnderheartRules.Route(L, scb.Index, from, valve.Cell, valve.Pos, s => s == "sc_canals_gate", steps), Is.True);
            Assert.That(steps.Count, Is.EqualTo(2), "through the open gate: the doorway, then the target");
            Assert.That(around, Is.GreaterThan(2));
        }

        [Test]
        public void FloorLabelsReadPlainly()
        {
            Assert.That(UnderheartRules.FloorLabel(UnderheartIds.LanternRefuge, -18f), Is.EqualTo("Lantern Refuge, 18 m below"));
            Assert.That(UnderheartRules.FloorLabel(UnderheartIds.FoundryCrown, -95f), Is.EqualTo("Foundry Crown, 95 m below"));
        }

        // ================================================================== heat, cooling and the camera

        [Test]
        public void LavaBurnsTheCatwalkGapsDoNotAndVentsBreathe()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            UhHazard lava = Find(L, "canals_lava_m");
            UhCell channel = L.Cells[lava.Cell];
            float floor = channel.FloorAt(lava.CenterX, lava.CenterZ);
            UhHazard src;
            Assert.That(UnderheartRules.HeatAt(L, new Vector3(lava.CenterX, floor - 0.5f, lava.CenterZ), 0f, out src), Is.GreaterThan(20f), "in the trench");
            Assert.That(src, Is.SameAs(lava));
            Assert.That(UnderheartRules.HeatAt(L, new Vector3(lava.CenterX, floor + 1.2f, lava.CenterZ), 0f, out src), Is.EqualTo(0f), "jumping over it");
            UhHazard west = Find(L, "canals_lava_w");
            float gap = (west.X1 + lava.X0) * 0.5f;
            Assert.That(UnderheartRules.HeatAt(L, new Vector3(gap, floor, lava.CenterZ), 0f, out src), Is.EqualTo(0f), "the catwalk gap is safe");
            UhHazard vent = Find(L, "canals_vent_0");
            Assert.That(vent.Cycle, Is.GreaterThan(1f));
            float t0 = vent.Cycle - vent.Phase;
            Assert.That(vent.ActiveAt(t0 + 0.1f), Is.True, "breathing at the start of its cycle");
            Assert.That(vent.ActiveAt(t0 + vent.Cycle * 0.8f), Is.False, "resting late in its cycle");
            Assert.That(vent.WarningAt(t0 + vent.Cycle * 0.95f), Is.GreaterThan(vent.WarningAt(t0 + vent.Cycle * 0.6f)), "the warning ring builds before a breath");
            UhHazard warm = Find(L, "canals_warm_valve");
            Assert.That(warm.Heat, Is.EqualTo(0f), "warm scenery never hurts");
            UhHazard cool = Find(L, "canals_cool_0");
            float cf = L.Cells[cool.Cell].FloorAt(cool.CenterX, cool.CenterZ);
            Assert.That(UnderheartRules.CoolingAt(L, new Vector3(cool.CenterX, cf, cool.CenterZ)), Is.True, "standing in a blue vent");
        }

        [Test]
        public void TheCameraBoomTightensOnlyInTightSpaces()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            Assert.That(UnderheartRules.CameraLimit(L.Cell("service.tunnel1")), Is.EqualTo(2.6f).Within(0.001f), "a narrow stair");
            Assert.That(UnderheartRules.CameraLimit(L.Cell("canals.transit")), Is.EqualTo(float.PositiveInfinity), "the vehicle tunnel");
            Assert.That(UnderheartRules.CameraLimit(L.Cell("crown.chamber")), Is.EqualTo(float.PositiveInfinity), "the vast chamber");
            Assert.That(UnderheartRules.CameraLimit(null), Is.EqualTo(float.PositiveInfinity));
        }

        // ================================================================== ground openings

        [Test]
        public void AperturesSubtractCleanlyAndAnswerContains()
        {
            Assert.That(WorldApertures.Any, Is.False);
            RectF slab = new RectF(0f, 0f, 100f, 100f);
            Assert.That(WorldApertures.Subtract(slab).Count, Is.EqualTo(1), "nothing to cut");
            WorldApertures.Register("stair", new RectF(40f, 40f, 50f, 60f));
            List<RectF> pieces = WorldApertures.Subtract(slab);
            Assert.That(pieces.Count, Is.EqualTo(4));
            float area = 0f;
            foreach (RectF r in pieces)
            {
                area += r.Area;
                Assert.That(r.Overlaps(new RectF(40.01f, 40.01f, 49.99f, 59.99f), 0f), Is.False, "no piece covers the opening");
            }
            Assert.That(area, Is.EqualTo(10000f - 200f).Within(0.1f));
            Assert.That(WorldApertures.Contains(new Vector3(45f, 0f, 50f), 0f), Is.True);
            Assert.That(WorldApertures.Contains(new Vector3(51f, 0f, 50f), 0f), Is.False);
            Assert.That(WorldApertures.Contains(new Vector3(51f, 0f, 50f), 1.5f), Is.True, "the margin keeps spawns off the rim");
            Assert.That(WorldApertures.Overlaps(new RectF(48f, 0f, 70f, 41f)), Is.True);
            Assert.That(WorldApertures.Find("stair"), Is.Not.Null);
            WorldApertures.Unregister("stair");
            Assert.That(WorldApertures.Any, Is.False, "a failed plan leaves the city whole");
            Assert.That(WorldApertures.Subtract(slab).Count, Is.EqualTo(1));
        }

        // ================================================================== the default city

        [Test]
        public void ThePlannerFitsTheDefaultCityWithAVehiclePortal()
        {
            CityLayout city = City;
            UnderheartFrame frame;
            Assert.That(FirstPlausibleFrame(city, out frame), Is.True, "a Pressure House sized building near Veyra Lofts");
            UnderheartPlanner.Choice choice = UnderheartPlanner.Choose(city, frame, true);
            Assert.That(choice.Layout, Is.Not.Null, choice.Report);
            Assert.That(choice.PortalPlanned, Is.True, "portal: " + choice.PortalReason);
            UnderheartLayout L = choice.Layout;
            List<string> problems = UnderheartRules.Validate(L);
            Assert.That(problems, Is.Empty, problems.Count > 0 ? problems[0] : "");
            List<string> world = UnderheartRules.ValidateWorld(L, frame);
            Assert.That(world, Is.Empty, world.Count > 0 ? world[0] : "");
            Assert.That(L.Anchors.Count, Is.EqualTo(UnderheartIds.Anchors.Length), "every frozen anchor including the portal top");
            UhCell cut = L.Cell("portal.cut"), ramp = L.Cell("portal.ramp");
            Assert.That(cut, Is.Not.Null);
            Assert.That(ramp, Is.Not.Null);
            Assert.That(cut.SlopeDegrees, Is.LessThanOrEqualTo(UnderheartRules.MaxRampDegrees + 0.01f));
            Assert.That(ramp.SlopeDegrees, Is.LessThanOrEqualTo(UnderheartRules.MaxRampDegrees + 0.01f));
            Assert.That(L.Portal.Lot, Is.Not.Null);
            Assert.That(L.Portal.Lot.Use == LotUse.Park || L.Portal.Lot.Use == LotUse.Plaza, Is.True, "an open lot, never a parking lot");
            Assert.That(L.Portal.Lot.Buildings.Count, Is.EqualTo(0), "no building on the portal lot");
            Assert.That(L.Portal.Lot.CustomBuilt, Is.False);
            RectF lotArea = L.Portal.Lot.Area;
            RectF hole = L.Portal.CutWorld;
            Assert.That(hole.X0 >= lotArea.X0 && hole.X1 <= lotArea.X1 && hole.Z0 >= lotArea.Z0 && hole.Z1 <= lotArea.Z1, Is.True, "the opening stays inside its lot");
        }

        [Test]
        public void EveryQuarterTurnNearTheArrivalFindsAVariant()
        {
            for (int q = 0; q < 4; q++)
            {
                UnderheartFrame frame = new UnderheartFrame(-100f, -100f, q * 90f);
                UnderheartPlanner.Choice choice = UnderheartPlanner.Choose(null, frame, false);
                Assert.That(choice.Layout, Is.Not.Null, "quarter " + q + ": " + choice.Report);
                Assert.That(UnderheartRules.ValidateWorld(choice.Layout, frame), Is.Empty, "quarter " + q + " keeps clear of the canal and the map edge");
            }
        }

        // ================================================================== atmosphere and saves

        [Test]
        public void EachRegionHasItsOwnUndergroundPalette()
        {
            HashSet<string> fogs = new HashSet<string>();
            foreach (string region in UnderheartIds.Regions)
            {
                Color sky, equator, ground, fog;
                float density;
                Atmosphere.UndergroundPalette(region, out sky, out equator, out ground, out fog, out density);
                Assert.That(density, Is.InRange(0.005f, 0.02f), region + " fog density");
                Assert.That(sky.maxColorComponent, Is.GreaterThan(0.1f), region + " is never pitch black");
                Assert.That(ground.maxColorComponent, Is.LessThan(sky.maxColorComponent), region + " light comes from above");
                fogs.Add(fog.ToString());
            }
            Assert.That(fogs.Count, Is.EqualTo(UnderheartIds.Regions.Length), "seven distinct fog colours");
        }

        private static readonly string[] UnderheartSaveFields =
        {
            "Version:Int32", "Shortcuts:List<String>", "Discovered:List<String>", "Cleared:List<String>", "RestAnchor:String",
            "SavedRegion:String", "SavedAnchor:String", "LayoutVariant:Int32"
        };

        [Test]
        public void UnderheartSaveFieldsAreOnlyEverAppended()
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(UnderheartSave).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string t = f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>) ? "List<" + f.FieldType.GetGenericArguments()[0].Name + ">" : f.FieldType.Name;
                now[f.Name] = t;
            }
            foreach (string entry in UnderheartSaveFields)
            {
                string[] parts = entry.Split(':');
                string type;
                Assert.That(now.TryGetValue(parts[0], out type), Is.True, "UnderheartSave." + parts[0] + " was renamed or removed");
                Assert.That(type, Is.EqualTo(parts[1]), "UnderheartSave." + parts[0] + " changed type");
            }
            Assert.That(Attribute.IsDefined(typeof(UnderheartSave), typeof(SerializableAttribute)), Is.True, "UnderheartSave stays [Serializable]");
            UnderheartSave fresh = new UnderheartSave();
            Assert.That(fresh.SavedRegion, Is.EqualTo(""), "an old save resumes on the surface");
            Assert.That(fresh.SavedAnchor, Is.EqualTo(""));
            Assert.That(fresh.LayoutVariant, Is.EqualTo(-1), "an old save never claims a layout");
            Assert.That(fresh.Shortcuts, Is.Not.Null.And.Empty, "no shortcut is granted by default");
        }

        // ================================================================== helpers

        private static UhHazard Find(UnderheartLayout L, string id)
        {
            foreach (UhHazard h in L.Hazards) if (h.Id == id) return h;
            Assert.Fail("hazard " + id + " is missing");
            return null;
        }

        /// <summary>The first door frame (any side) of an eligible ordinary building 30 to 160 m from Veyra Lofts that is big
        /// enough for the Pressure House (a stand-in for its binding, which the venue catalog supplies at run time).</summary>
        private static bool FirstPlausibleFrame(CityLayout city, out UnderheartFrame frame)
        {
            frame = default(UnderheartFrame);
            Poi home = city.FindPoi("Veyra Lofts");
            if (home == null) return false;
            foreach (Block b in city.Blocks)
                foreach (Lot lot in b.Lots)
                {
                    if (lot.CustomBuilt || lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName)) continue;
                    foreach (Building bld in lot.Buildings)
                    {
                        if (!InhabitedCityBindings04C.Eligible(bld)) continue;
                        float d = MathUtil.FlatDistance(bld.Footprint.Center(0f), home.Position);
                        if (d < 30f || d > 160f) continue;
                        Vector3[] facings = { lot.Facing, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                        foreach (Vector3 facing in facings)
                        {
                            if (facing.sqrMagnitude < 0.5f) continue;
                            Vector3 origin;
                            float width, depth, yaw;
                            InhabitedCityBindings04C.Door(bld.Footprint, facing, out origin, out width, out depth, out yaw);
                            if (width < 17f || depth < 19f) continue;
                            frame = new UnderheartFrame(origin.x, origin.z, yaw);
                            return true;
                        }
                    }
                }
            return false;
        }
    }
}
