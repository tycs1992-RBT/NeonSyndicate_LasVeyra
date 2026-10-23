using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S revision-10 route plan: the stops follow the original Hearth Witness quest and its place tokens, the time
    /// estimate uses the controller's real speeds, the overlay clearance rule keeps decorations out of stop discs and leg
    /// corridors, and every stop resolves in the real seed 61724 revision-10 layout offline.
    /// </summary>
    public sealed class WorldRouteH8STests
    {
        private static QuestDef HearthWitness()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/Quests/ascension_h3_hearth_witness.json");
            List<string> errors = new List<string>();
            QuestFile file = QuestJson.Read<QuestFile>(File.ReadAllText(path), path, errors, null);
            Assert.That(errors, Is.Empty);
            return file.Quests.Single();
        }

        [Test]
        public void StopsFollowTheHearthWitnessQuestAndItsExistingTokens()
        {
            QuestDef quest = HearthWitness();
            Assert.That(quest.Id, Is.EqualTo("fg_04oh_h3_hearth_witness"));
            List<string> places = new List<string>();
            foreach (QuestPhase phase in quest.Phases)
                foreach (QuestStep step in phase.Steps)
                    if (!string.IsNullOrEmpty(step.At) && !places.Contains(step.At)) places.Add(step.At);
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
            Assert.That(stops.Length, Is.EqualTo(7));
            Assert.That(stops.Skip(2).Select(s => s.Token).ToArray(), Is.EqualTo(places.ToArray()), "the route walks the quest's own place order");
            Assert.That(quest.Giver, Is.EqualTo("Dante Rook"));
            Assert.That(quest.VenueGiver, Is.EqualTo("forge"));
            Assert.That(stops[1].Token, Is.EqualTo("Cast:" + CastIds04E.Dante), "the Forge stop is the original giver's live actor or post");
            Assert.That(stops[0].Token, Is.EqualTo("Venue:lofts.exit"));
            Assert.That(stops.Select(s => s.Id).Distinct().Count(), Is.EqualTo(stops.Length));
            foreach (WorldRouteStopH8S stop in stops)
            {
                if (stop.Token.StartsWith("Realm:", StringComparison.Ordinal)) Assert.That(QuestPlaces.IsKnownPlace(null, stop.Token), Is.True, stop.Token);
                else Assert.That(CampaignRules04E.PlaceTokenError(stop.Token), Is.Null, stop.Token);
                Assert.That(stop.DwellSeconds, Is.GreaterThanOrEqualTo(0f));
                Assert.That(stop.ProtectRadius, Is.GreaterThanOrEqualTo(4f));
                Assert.That(stop.Interior, Is.EqualTo(stop.Id == "forge"), "only the giver stands indoors");
                Assert.That(stop.DoorToken != null, Is.EqualTo(stop.Interior), "an indoor stop names its door");
                if (stop.DoorToken != null) Assert.That(CampaignRules04E.PlaceTokenError(stop.DoorToken), Is.Null, stop.DoorToken);
            }
            Assert.That(stops[1].DoorToken, Is.EqualTo(stops[6].Token), "the giver's door is the quest's own safe return point");
            // Every quest trigger (GoTo, Interact, Kill radius) sits inside its stop's protected disc with two metres to spare.
            foreach (QuestPhase phase in quest.Phases)
                foreach (QuestStep step in phase.Steps)
                {
                    if (string.IsNullOrEmpty(step.At)) continue;
                    WorldRouteStopH8S stop = stops.Single(s => s.Token == step.At);
                    Assert.That(stop.ProtectRadius, Is.GreaterThanOrEqualTo(step.Radius + 2f), step.At + " " + step.Type);
                }
        }

        [Test]
        public void EstimateUsesTheControllersRealSpeeds()
        {
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
            WorldRouteEstimateH8S e = WorldRouteH8S.Estimate(new[] { 200f, 240f }, stops);
            Assert.That(e.Meters, Is.EqualTo(440f));
            Assert.That(e.RunSeconds, Is.EqualTo(440f / TravelMath.JogSpeed).Within(1e-3f));
            Assert.That(e.RunSeconds, Is.EqualTo(100f).Within(1e-3f), "4.4 m/s default run");
            Assert.That(e.WalkSeconds, Is.EqualTo(440f / TravelMath.WalkSpeed).Within(1e-3f));
            Assert.That(e.SprintSeconds, Is.EqualTo(440f / TravelMath.SprintSpeed).Within(1e-3f));
            Assert.That(e.SurgeSeconds, Is.EqualTo(440f / TravelMath.SurgeTopSpeed + 2f * TravelMath.SurgeRampSeconds).Within(1e-3f), "travel power lower bound");
            Assert.That(e.DwellSeconds, Is.EqualTo(stops.Sum(s => s.DwellSeconds)).Within(1e-3f));
            Assert.That(e.EstimateSeconds, Is.EqualTo(e.RunSeconds + e.DwellSeconds).Within(1e-3f));
            WorldRouteEstimateH8S missing = WorldRouteH8S.Estimate(new[] { 500f, -1f, float.NaN }, stops);
            Assert.That(missing.Legs, Is.EqualTo(1));
            Assert.That(missing.MissingLegs, Is.EqualTo(2));
            Assert.That(missing.WithinTarget, Is.False, "an incomplete route never claims the target");
            Assert.That(WorldRouteH8S.WithinTarget(239f), Is.False);
            Assert.That(WorldRouteH8S.WithinTarget(240f), Is.True);
            Assert.That(WorldRouteH8S.WithinTarget(480f), Is.True);
            Assert.That(WorldRouteH8S.WithinTarget(481f), Is.False);
            Assert.That(WorldRouteH8S.Minutes(312f), Is.EqualTo("5 min 12 s"));
            Assert.That(WorldRouteH8S.PolylineLength(new List<Vector3> { Vector3.zero, new Vector3(3f, 0f, 4f), new Vector3(3f, 0f, 10f) }), Is.EqualTo(11f).Within(1e-4f));
        }

        [Test]
        public void OverlayClearanceRuleProtectsStopsAndStraightLegs()
        {
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops().Take(3).ToArray();
            List<Vector3> points = new List<Vector3> { Vector3.zero, new Vector3(100f, 0f, 0f), new Vector3(100f, 0f, 80f) };
            List<bool> resolved = new List<bool> { true, true, true };
            List<WorldRouteGuardH8S> guards = WorldRouteH8S.Guards(points, resolved, stops, null);
            Assert.That(guards.Count, Is.EqualTo(5), "three stop discs and two straight legs");
            // Beside the first leg: 2.5 m from its line intrudes on the 3 m corridor, 3.5 m does not.
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(48f, 0f, 2.5f), new Vector3(52f, 5f, 4f), guards), Is.EqualTo("leg home to forge"));
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(48f, 0f, 3.5f), new Vector3(52f, 5f, 5f), guards), Is.Null);
            // Near a stop: the home disc is 4 m (this overlay's nearest corner is 3.2 m from the stop, 3.2 m from the leg).
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(-3.5f, 0f, -3f), new Vector3(-2f, 2f, -2.5f), guards), Is.EqualTo("stop home"));
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(-9f, 0f, -9f), new Vector3(-6f, 2f, -6f), guards), Is.Null);
            // An arch spanning the leg is rejected whatever its height: the rule is a plan corridor.
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(60f, 0f, -4f), new Vector3(63f, 8f, 4f), guards), Is.EqualTo("leg home to forge"));
            // A walk polyline adds its own corridor.
            List<List<Vector3>> walks = new List<List<Vector3>> { new List<Vector3> { new Vector3(0f, 0f, 30f), new Vector3(100f, 0f, 30f) } };
            List<WorldRouteGuardH8S> withWalk = WorldRouteH8S.Guards(points, resolved, stops, walks);
            Assert.That(WorldRouteH8S.OverlayConflict(new Vector3(48f, 0f, 31f), new Vector3(52f, 2f, 33f), withWalk), Is.EqualTo("walk route 0"));
            // An unresolved middle stop is skipped and its neighbours are joined directly.
            List<WorldRouteGuardH8S> gap = WorldRouteH8S.Guards(points, new List<bool> { true, false, true }, stops, null);
            Assert.That(gap.Count(g => g.Label.StartsWith("leg ", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(gap.Any(g => g.Label == "leg home to hearth"), Is.True);
        }

        [Test]
        public void DressingSlotsSitOutsideEveryProtectedCorridor()
        {
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < stops.Length; i++) points.Add(new Vector3(i * 60f, 0f, (i % 2) * 45f));
            List<bool> resolved = stops.Select(s => true).ToList();
            List<WorldRouteGuardH8S> guards = WorldRouteH8S.Guards(points, resolved, stops, null);
            const float accent = 2.1f;
            List<WorldRouteSlotH8S> slots = WorldRouteH8S.DressingSlots(points, resolved, stops, accent, guards);
            Assert.That(slots.Any(s => stops[s.Stop].Interior), Is.False, "nothing dresses the indoor giver stop");
            for (int i = 0; i < stops.Length; i++)
            {
                if (stops[i].Interior) continue;
                int here = slots.Count(s => s.Stop == i);
                Assert.That(here, Is.InRange(1, WorldRouteH8S.SlotsPerStop), "even the zig-zag turns leave room beside " + stops[i].Id);
            }
            // At a turning stop the side towards the next leg is refused, not offered: stop 2 arrives along (0.8, -0.6),
            // so its left side (0.6, 0.8) at 8.1 m lies 1.85 m from the outgoing leg's line.
            Vector3 turnSide = new Vector3(0.6f, 0f, 0.8f);
            Assert.That(slots.Any(s => s.Stop == 2 && Vector3.Distance(s.Position, points[2] + turnSide * 8.1f) < 0.01f), Is.False);
            foreach (WorldRouteSlotH8S slot in slots)
            {
                Vector3 min = slot.Position - new Vector3(accent, 0f, accent), max = slot.Position + new Vector3(accent, 2f, accent);
                Assert.That(WorldRouteH8S.OverlayConflict(min, max, guards), Is.Null, "slot beside " + stops[slot.Stop].Id);
                Vector3 face = points[slot.Stop] - slot.Position;
                Assert.That(Mathf.DeltaAngle(slot.Yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg), Is.EqualTo(0f).Within(0.01f), "faces its stop");
            }
            List<Vector3> offsets = WorldRouteH8S.CandidateOffsets(new Vector3(-5f, 0f, 3f));
            Assert.That(offsets[0], Is.EqualTo(new Vector3(-5f, 0f, 3f)), "the H8R offset is tried first");
            Assert.That(offsets.Count, Is.EqualTo(33));
            Assert.That(offsets.Skip(1).All(o => Mathf.Abs(o.y) < 1e-6f), Is.True);
            Vector3 yMin, yMax, qMin, qMax;
            WorldRouteH8S.YawedBounds(new Vector3(-1f, 0f, -2f), new Vector3(3f, 1f, 2f), 90f, Vector3.zero, out yMin, out yMax);
            ForgeGalleryFitH8S.RotateBounds(new Vector3(-1f, 0f, -2f), new Vector3(3f, 1f, 2f), 1, out qMin, out qMax);
            Assert.That(Vector3.Distance(yMin, qMin) + Vector3.Distance(yMax, qMax), Is.LessThan(1e-4f));
        }

        [Test]
        public void RevisionTenRouteResolvesThroughTheRealAnchorsOffline()
        {
            CityLayout world = new CityLayout(61724);
            world.Generate();
            List<VenueBinding04C> venues = InhabitedCityBindings04C.Select(world, InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O(CityRevision04O.GothicCivicH5, venues), Is.True);
            Assert.That(world.GothicCivicH5, Is.True);
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
            List<Vector3> points = new List<Vector3>();
            foreach (WorldRouteStopH8S stop in stops)
            {
                Vector3 p;
                Assert.That(ResolveOffline(world, venues, stop.Token, out p), Is.True, stop.Token);
                Assert.That(float.IsNaN(p.x) || float.IsInfinity(p.x), Is.False);
                points.Add(p);
            }
            List<Vector3> doors = new List<Vector3>();
            foreach (WorldRouteStopH8S stop in stops)
            {
                Vector3 door = Vector3.zero;
                if (stop.DoorToken != null) Assert.That(ResolveOffline(world, venues, stop.DoorToken, out door), Is.True, stop.DoorToken);
                doors.Add(door);
            }
            List<float> legs = new List<float>();
            List<string> rows = new List<string>();
            int walkedLegs = 0;
            for (int i = 1; i < points.Count; i++)
            {
                float straight = Vector3.Distance(points[i - 1], points[i]);
                bool walked;
                List<Vector3> line = WorldRouteH8S.LegPolyline(world, points[i - 1], stops[i - 1].Interior, doors[i - 1], points[i], stops[i].Interior, doors[i], out walked);
                float length = WorldRouteH8S.PolylineLength(line);
                Assert.That(straight, Is.GreaterThan(1f), stops[i - 1].Id + " to " + stops[i].Id);
                Assert.That(straight, Is.LessThan(2500f));
                Assert.That(length, Is.GreaterThanOrEqualTo(straight - 0.01f), "no polyline is shorter than its straight leg");
                Assert.That(line[0], Is.EqualTo(points[i - 1]));
                Assert.That(line[line.Count - 1], Is.EqualTo(points[i]));
                if (walked) walkedLegs++;
                legs.Add(length);
                rows.Add(stops[i - 1].Id + " to " + stops[i].Id + ": straight " + straight.ToString("0.0") + " m, followed " + length.ToString("0.0") + " m (" + (walked ? "walk graph, " + line.Count + " points" : "no walk graph route, straight") + ")");
            }
            WorldRouteEstimateH8S e = WorldRouteH8S.Estimate(legs, stops);
            Assert.That(e.MissingLegs, Is.EqualTo(0));
            Assert.That(e.Meters, Is.GreaterThan(100f));
            Assert.That(walkedLegs, Is.GreaterThanOrEqualTo(4), "most legs follow the real walk graph once the giver leg leaves through the Forge door");
            Console.WriteLine("H8S offline revision-10 route, seed 61724 (offline reference harness, layout maths only):");
            for (int i = 0; i < stops.Length; i++) Console.WriteLine("  " + stops[i].Id + " " + stops[i].Token + " at " + points[i].ToString("F1"));
            foreach (string row in rows) Console.WriteLine("  " + row);
            Console.WriteLine("  total " + e.Meters.ToString("0") + " m; walk " + WorldRouteH8S.Minutes(e.WalkSeconds) + ", run " + WorldRouteH8S.Minutes(e.RunSeconds) +
                ", sprint " + WorldRouteH8S.Minutes(e.SprintSeconds) + ", Surge " + WorldRouteH8S.Minutes(e.SurgeSeconds) + "; dwell " + WorldRouteH8S.Minutes(e.DwellSeconds) +
                "; estimate (run plus dwell) " + WorldRouteH8S.Minutes(e.EstimateSeconds) + " against 4 to 8 min: " + (e.WithinTarget ? "inside" : "outside") +
                "; sprint plus dwell " + WorldRouteH8S.Minutes(e.SprintSeconds + e.DwellSeconds) + "; Surge plus dwell " + WorldRouteH8S.Minutes(e.SurgeSeconds + e.DwellSeconds));
        }

        /// <summary>
        /// The offline twin of the review's resolvers: Realm tokens through CityLayout.ResolveRealmAnchor04OC; Venue tokens at
        /// the binding's origin and yaw with the shipped layout anchor (what VenueView04C.WorldPoint returns); Cast:dante at
        /// his post, the Forge's staff anchor (CastRoster04E), because no actor exists offline.
        /// </summary>
        private static bool ResolveOffline(CityLayout world, List<VenueBinding04C> venues, string token, out Vector3 point)
        {
            point = Vector3.zero;
            if (token.StartsWith("Realm:", StringComparison.Ordinal)) return world.ResolveRealmAnchor04OC(token, out point);
            string venue, anchor;
            if (token == "Cast:" + CastIds04E.Dante) { venue = "forge"; anchor = "staff"; }
            else if (!CampaignRules04E.SplitVenue(token, out venue, out anchor)) return false;
            VenueBinding04C binding = venues.SingleOrDefault(v => v.Definition.Id == venue && v.Building != null);
            if (binding == null) return false;
            VenueAnchor04C a = VenueLayoutData04E.Layout(venue).anchors.SingleOrDefault(x => x.id == anchor);
            if (a == null) return false;
            point = InhabitedCityBindings04C.LocalToWorld(binding.Origin, binding.Yaw, new Vector3(a.position[0], a.position[1], a.position[2]));
            return true;
        }
    }
}
