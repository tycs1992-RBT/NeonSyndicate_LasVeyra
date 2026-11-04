using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S Forge furnishing maths: the anvil fit inside the observed Forge collider from the delivered mesh bounds, the
    /// quarter-turn and clearance helpers, the line-of-sight slab test and the ranked gallery spots in the real Forge room.
    /// </summary>
    public sealed class ForgeGalleryFitH8STests
    {
        // Delivered Anvil.fbx source bounds (Blender 5.2 module): x -0.4794 to 0.6021 with the tapering horn at +x,
        // y -0.2008 to 0.2008, z 0.0027 to 0.5587. Unity's FBX import negates x and maps Blender z up and Blender y to z,
        // so in Unity axes the horn points to -x.
        private static readonly Vector3 AnvilMin = new Vector3(-0.6021f, 0.0027f, -0.2008f);
        private static readonly Vector3 AnvilMax = new Vector3(0.4794f, 0.5587f, 0.2008f);
        // The original station collider as observed in a real run and as ForgeStations.Anvil builds it at scale 1.
        private static readonly Vector3 BoxCenter = new Vector3(-0.2f, 0.45f, 0.05f);
        private static readonly Vector3 BoxSize = new Vector3(1.5f, 0.9f, 0.6f);
        // Top of the original wooden "Block" (centre 0.25, height 0.5).
        private const float BlockTop = 0.5f;

        [Test]
        public void QuarterTurnsMatchTheQuaternionRotationExactly()
        {
            Vector3 p = new Vector3(0.3f, 1.1f, -0.7f);
            for (int q = -4; q <= 7; q++)
                Assert.That(Vector3.Distance(QMath.AY(90f * q) * p, ForgeGalleryFitH8S.RotateY(p, q)), Is.LessThan(1e-5f), "turns " + q);
            Vector3 min, max;
            ForgeGalleryFitH8S.RotateBounds(AnvilMin, AnvilMax, 1, out min, out max);
            Assert.That(max.x - min.x, Is.EqualTo(AnvilMax.z - AnvilMin.z).Within(1e-5f));
            Assert.That(max.z - min.z, Is.EqualTo(AnvilMax.x - AnvilMin.x).Within(1e-5f));
            Assert.That(max.y, Is.EqualTo(AnvilMax.y), "a turn about up never changes height");
        }

        [Test]
        public void DeliveredAnvilFitsInsideTheObservedForgeColliderOnItsBlock()
        {
            Assert.That(ForgeGalleryFitH8S.NativeSizeMatches(AnvilMax - AnvilMin, ForgeGalleryFitH8S.SourceSize("Anvil"), ForgeGalleryFitH8S.NativeSizeTolerance), Is.True);
            ForgeFitH8S fit = ForgeGalleryFitH8S.Fit(AnvilMin, AnvilMax, BoxCenter, BoxSize, BlockTop, Vector2.zero, ForgeGalleryFitH8S.BoxMargin,
                ForgeGalleryFitH8S.MinAnvilScale, ForgeGalleryFitH8S.MaxAnvilScale, ForgeGalleryFitH8S.MaxSlide, true);
            Assert.That(fit.Ok, Is.True, fit.Reason);
            // The height above the original 0.5 m block binds: (0.9 - 0.02 - 0.5) / 0.556 = 0.68345, kept to four decimals.
            Assert.That(fit.Scale, Is.EqualTo(0.6834f).Within(0.0001f));
            StringAssert.Contains("height above the support", fit.Reason);
            Assert.That(fit.QuarterTurns, Is.EqualTo(2), "the horn is turned to +x like the original anvil's horn");
            Assert.That(fit.Max.x, Is.GreaterThan(-fit.Min.x), "the longer horn side is on +x");
            Assert.That(fit.Pivot.x, Is.EqualTo(0f).Within(1e-4f), "centred on the block");
            Assert.That(fit.Pivot.z, Is.EqualTo(0f).Within(1e-4f), "centred on the block");
            Assert.That(fit.Min.y, Is.EqualTo(BlockTop).Within(1e-4f), "rests exactly on the block");
            Vector3[] corners = new Vector3[8];
            ForgeGalleryFitH8S.Corners(fit.Min, fit.Max, corners);
            Assert.That(ForgeGalleryFitH8S.FirstOutside(corners, BoxCenter, BoxSize, ForgeGalleryFitH8S.BoxMargin - 1e-4f), Is.EqualTo(-1), "every corner keeps the margin");
            Assert.That(ForgeGalleryFitH8S.Clearance(corners, BoxCenter, BoxSize), Is.EqualTo(ForgeGalleryFitH8S.BoxMargin).Within(0.0005f));
            // The fitted anvil keeps clear of the original ember bowl and embers that stay visible beside it.
            Vector3 bowlMin = new Vector3(-0.975f, 0.175f, -0.075f), bowlMax = new Vector3(-0.525f, 0.525f, 0.375f);
            Vector3 embersMin = new Vector3(-0.93f, 0.54f, -0.03f), embersMax = new Vector3(-0.57f, 0.70f, 0.33f);
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(fit.Min, fit.Max, bowlMin, bowlMax), Is.LessThan(0f));
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(fit.Min, fit.Max, embersMin, embersMax), Is.LessThan(0f));
            // It only touches the block it rests on.
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(fit.Min, fit.Max, new Vector3(-0.275f, 0f, -0.275f), new Vector3(0.275f, 0.5f, 0.275f)),
                Is.LessThanOrEqualTo(ForgeGalleryFitH8S.TouchTolerance));
        }

        [Test]
        public void ProposedScaleSevenTenthsAtHalfAMetreDoesNotKeepTheMargin()
        {
            Vector3 min, max;
            ForgeGalleryFitH8S.RotateBounds(AnvilMin, AnvilMax, 2, out min, out max);
            Vector3 pivot = new Vector3(0f, 0.5f, 0f);
            Vector3[] corners = new Vector3[8];
            ForgeGalleryFitH8S.Corners(pivot + min * 0.7f, pivot + max * 0.7f, corners);
            Assert.That(ForgeGalleryFitH8S.FirstOutside(corners, BoxCenter, BoxSize, 0f), Is.EqualTo(-1), "inside the raw box by about 9 mm");
            Assert.That(ForgeGalleryFitH8S.FirstOutside(corners, BoxCenter, BoxSize, ForgeGalleryFitH8S.BoxMargin), Is.Not.EqualTo(-1), "but not with the 2 cm margin");
            Assert.That(ForgeGalleryFitH8S.Clearance(corners, BoxCenter, BoxSize), Is.LessThan(ForgeGalleryFitH8S.BoxMargin));
        }

        [Test]
        public void FitRefusesWhenTheColliderIsTooSmallOrTheSupportIsOutside()
        {
            ForgeFitH8S tiny = ForgeGalleryFitH8S.Fit(AnvilMin, AnvilMax, new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.3f), 0.1f, Vector2.zero, 0.02f, 0.5f, 1f, 0.12f, true);
            Assert.That(tiny.Ok, Is.False);
            StringAssert.Contains("minimum scale", tiny.Reason);
            ForgeFitH8S outside = ForgeGalleryFitH8S.Fit(AnvilMin, AnvilMax, BoxCenter, BoxSize, 0.95f, Vector2.zero, 0.02f, 0.5f, 1f, 0.12f, true);
            Assert.That(outside.Ok, Is.False);
            StringAssert.Contains("support top", outside.Reason);
            ForgeFitH8S flat = ForgeGalleryFitH8S.Fit(Vector3.zero, new Vector3(1f, 0f, 1f), BoxCenter, BoxSize, BlockTop, Vector2.zero, 0.02f, 0.5f, 1f, 0.12f, true);
            Assert.That(flat.Ok, Is.False, "a visual with no rendered volume is refused");
        }

        [Test]
        public void FitKeepsNativeScaleWhenTheBoxAllowsIt()
        {
            ForgeFitH8S fit = ForgeGalleryFitH8S.Fit(AnvilMin, AnvilMax, new Vector3(0f, 2f, 0f), new Vector3(4f, 4f, 3f), 0.5f, Vector2.zero, 0.02f, 0.5f, 1f, 0.12f, true);
            Assert.That(fit.Ok, Is.True, fit.Reason);
            Assert.That(fit.Scale, Is.EqualTo(1f), "never enlarged, never shrunk without need");
            Assert.That(fit.QuarterTurns, Is.EqualTo(2), "long side along the box's long x axis, horn to +x");
        }

        [Test]
        public void SlabTestFindsLinesOfSightThroughBoxesOnly()
        {
            Vector3 min = Vector3.zero, max = Vector3.one;
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-1f, 0.5f, 0.5f), new Vector3(2f, 0.5f, 0.5f), min, max), Is.True);
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-1f, 1.5f, 0.5f), new Vector3(2f, 1.5f, 0.5f), min, max), Is.False, "passes above");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-2f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), min, max), Is.False, "ends before the box");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-1f, 1f, 0.5f), new Vector3(2f, 1f, 0.5f), min, max), Is.True, "grazing the top face counts");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-1f, -1f, 0.5f), new Vector3(2f, 2f, 0.5f), min, max), Is.True, "diagonal through");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(-1f, 0.2f, -1f), new Vector3(0.2f, 0.2f, -0.1f), min, max), Is.False, "diagonal that stops short");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), min, max), Is.True, "a point inside");
            Assert.That(ForgeGalleryFitH8S.SegmentHitsBox(new Vector3(3f, 3f, 3f), new Vector3(3f, 3f, 3f), min, max), Is.False, "a point outside");
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(min, max, new Vector3(1f, 0f, 0f), new Vector3(2f, 1f, 1f)), Is.EqualTo(0f), "touching");
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(min, max, new Vector3(0.9f, 0f, 0f), new Vector3(2f, 1f, 1f)), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(ForgeGalleryFitH8S.OverlapDepth(min, max, new Vector3(1.5f, 0f, 0f), new Vector3(2f, 1f, 1f)), Is.LessThan(0f), "apart");
        }

        [Test]
        public void PlanDistancesAreExact()
        {
            PlanRectH8S r = new PlanRectH8S(new Vector2(0f, 0f), new Vector2(2f, 1f), "r");
            Assert.That(GalleryPlanH8S.RectDistance(r, new Vector2(1f, 0.5f)), Is.EqualTo(0f));
            Assert.That(GalleryPlanH8S.RectDistance(r, new Vector2(5f, 5f)), Is.EqualTo(5f).Within(1e-5f));
            Assert.That(GalleryPlanH8S.SegmentRectDistance(new Vector2(-1f, 3f), new Vector2(3f, 3f), r), Is.EqualTo(2f).Within(1e-5f), "parallel above");
            Assert.That(GalleryPlanH8S.SegmentRectDistance(new Vector2(-1f, -1f), new Vector2(3f, 2f), r), Is.EqualTo(0f), "crossing");
            Assert.That(GalleryPlanH8S.SegmentRectDistance(new Vector2(3f, 2f), new Vector2(4f, 3f), r), Is.EqualTo(Mathf.Sqrt(2f)).Within(1e-5f), "corner to endpoint");
            Assert.That(GalleryPlanH8S.SegmentRectDistance(new Vector2(4f, -2f), new Vector2(-2f, 4f), new PlanRectH8S(new Vector2(-1f, -1f), new Vector2(0f, 0f), "c")),
                Is.EqualTo(Mathf.Sqrt(2f)).Within(1e-4f), "rectangle corner to segment interior");
            Assert.That(GalleryPlanH8S.Overlaps(r, new PlanRectH8S(new Vector2(2f, 0f), new Vector2(3f, 1f), "edge")), Is.False, "touching edges are not overlap");
            Assert.That(GalleryPlanH8S.Overlaps(r, new PlanRectH8S(new Vector2(1.9f, 0.9f), new Vector2(3f, 2f), "corner")), Is.True);
            PlanRectH8S shrunk = r.Expanded(-5f);
            Assert.That(shrunk.Size.x, Is.EqualTo(0f), "over-shrinking never inverts a rectangle");
        }

        [Test]
        public void ConflictNamesEveryKindOfProtectedZone()
        {
            GalleryRulesH8S rules = new GalleryRulesH8S { Room = new PlanRectH8S(new Vector2(-8f, 0f), new Vector2(8f, 18f), "room"), FloorY = 0f };
            rules.Blocked.Add(new PlanRectH8S(new Vector2(-1f, 1f), new Vector2(1f, 3f), "keepout corridor"));
            rules.Circles.Add(new PlanCircleH8S(new Vector2(-5f, 8f), 2.5f, "interaction forge"));
            rules.Paths.Add(new PlanPathH8S(new Vector2(0f, -2f), new Vector2(5f, 10f), 0.75f, "walk to giver"));
            rules.Sights.Add(new SightLineH8S(new Vector3(-6f, 1.6f, 14f), new Vector3(6f, 1.6f, 14f), 0.3f, "sight across"));
            Assert.That(GalleryPlanH8S.Conflict(Box(0f, 2f, 1f, 1f), 1f, rules), Is.EqualTo("keepout corridor"));
            Assert.That(GalleryPlanH8S.Conflict(Box(-5f, 6f, 1f, 1f), 1f, rules), Is.EqualTo("interaction forge"));
            Assert.That(GalleryPlanH8S.Conflict(Box(2.5f, 4f, 1f, 1f), 1f, rules), Is.EqualTo("walk to giver"));
            Assert.That(GalleryPlanH8S.Conflict(Box(0f, 14f, 1f, 1f), 1.5f, rules), Is.EqualTo("sight across"), "a tall stand blocks the sight line");
            Assert.That(GalleryPlanH8S.Conflict(Box(0f, 14f, 1f, 1f), 0.9f, rules), Is.Null, "a low bench under the sight line does not");
            Assert.That(GalleryPlanH8S.Conflict(Box(7.8f, 9f, 1f, 1f), 1f, rules), Is.EqualTo("room wall margin"));
            Assert.That(GalleryPlanH8S.Conflict(Box(6f, 16f, 1f, 1f), 1f, rules), Is.Null);
        }

        [Test]
        public void GallerySpotsInTheRealForgeRoomKeepEveryRule()
        {
            GalleryRulesH8S rules = ForgeRoomRules();
            Vector3 bench = ForgeGalleryFitH8S.SourceSize("Workbench"), stand = ForgeGalleryFitH8S.SourceSize("BookStand");
            List<GallerySpotH8S> benchSpots = GalleryPlanH8S.Spots(new Vector2(bench.x, bench.z), bench.y, rules, GalleryPlanH8S.GridStep);
            Assert.That(benchSpots.Count, Is.GreaterThan(0), "the Forge room offers a safe workbench spot backed onto a wall");
            for (int i = 1; i < benchSpots.Count; i++) Assert.That(benchSpots[i].Score, Is.GreaterThanOrEqualTo(benchSpots[i - 1].Score));
            for (int i = 0; i < Math.Min(25, benchSpots.Count); i++)
                Assert.That(GalleryPlanH8S.Conflict(benchSpots[i].Footprint, bench.y, rules), Is.Null, "spot " + i);
            GallerySpotH8S chosen = benchSpots[0];
            Assert.That(chosen.Footprint.Size.x * chosen.Footprint.Size.y, Is.EqualTo(bench.x * bench.z).Within(1e-3f));
            rules.Blocked.Add(chosen.Footprint.Expanded(GalleryPlanH8S.ItemGap));
            List<GallerySpotH8S> standSpots = GalleryPlanH8S.Spots(new Vector2(stand.x, stand.z), stand.y, rules, GalleryPlanH8S.GridStep);
            Assert.That(standSpots.Count, Is.GreaterThan(0), "a safe book stand spot remains beside the workbench");
            Assert.That(GalleryPlanH8S.Overlaps(standSpots[0].Footprint, chosen.Footprint), Is.False);
            Console.WriteLine("H8S offline Forge gallery plan (venue-local metres, without furniture colliders): workbench centre " + chosen.Center.ToString("F2") +
                " turns " + chosen.QuarterTurns + " " + chosen.Wall + "; book stand centre " + standSpots[0].Center.ToString("F2") + " turns " + standSpots[0].QuarterTurns + " " + standSpots[0].Wall +
                "; candidates " + benchSpots.Count + " / " + standSpots.Count);
            Vector3 pivot = GalleryPlanH8S.PivotFor(chosen.Center, rules.FloorY, new Vector3(-1f, -0.1f, -0.5f), new Vector3(1f, 0.8f, 0.5f), chosen.QuarterTurns);
            Assert.That(pivot.y, Is.EqualTo(rules.FloorY + 0.1f).Within(1e-5f), "the lowest rendered point rests on the floor");
        }

        [Test]
        public void PivotLandsTurnedBoundsExactlyOnThePlannedFootprint()
        {
            // An off-centre pivot like the delivered Workbench and BookStand (rendered bounds not centred on the pivot).
            Vector3 localMin = new Vector3(-1.2f, -0.02f, -0.4f), localMax = new Vector3(0.8f, 0.9f, 0.62f);
            Vector2 size = new Vector2(localMax.x - localMin.x, localMax.z - localMin.z);
            for (int q = 0; q < 4; q++)
            {
                Vector2 centre = new Vector2(-6.5f + q, 10.25f);
                PlanRectH8S footprint = GalleryPlanH8S.Footprint(centre, size, q, "spot");
                Vector3 pivot = GalleryPlanH8S.PivotFor(centre, 0.012f, localMin, localMax, q);
                Vector3 min, max;
                ForgeGalleryFitH8S.RotateBounds(localMin, localMax, q, out min, out max);
                min += pivot;
                max += pivot;
                Assert.That(min.x, Is.EqualTo(footprint.Min.x).Within(1e-4f), "turns " + q);
                Assert.That(max.x, Is.EqualTo(footprint.Max.x).Within(1e-4f), "turns " + q);
                Assert.That(min.z, Is.EqualTo(footprint.Min.y).Within(1e-4f), "turns " + q);
                Assert.That(max.z, Is.EqualTo(footprint.Max.y).Within(1e-4f), "turns " + q);
                Assert.That(min.y, Is.EqualTo(0.012f).Within(1e-5f), "rests on the floor");
            }
        }

        /// <summary>The rules the live review builds, reconstructed offline from the shipped forge.json and the original constants.</summary>
        private static GalleryRulesH8S ForgeRoomRules()
        {
            VenueLayout04C forge = VenueLayoutData04E.Layout("forge");
            VenueExpanded04C defaults = new VenueExpanded04C();
            GalleryRulesH8S rules = new GalleryRulesH8S();
            float half = defaults.roomWidth * 0.5f - 0.3f;
            rules.Room = new PlanRectH8S(new Vector2(-half, 0.15f), new Vector2(half, defaults.roomDepth - 0.3f), "room");
            Vector3 exit = Vector3.zero, staff = Vector3.zero, service = Vector3.zero;
            foreach (VenueAnchor04C a in forge.anchors)
            {
                Vector3 p = new Vector3(a.position[0], a.position[1], a.position[2]);
                if (a.id == "exit") { exit = p; continue; }
                if (a.id == "staff") staff = p;
                if (a.id == "service") service = p;
                rules.Circles.Add(new PlanCircleH8S(new Vector2(p.x, p.z), GalleryPlanH8S.AnchorRadius, "anchor " + a.id));
            }
            rules.FloorY = service.y;
            foreach (VenueKeepout04C k in forge.keepouts)
                rules.Blocked.Add(new PlanRectH8S(new Vector2(k.min[0], k.min[2]), new Vector2(k.max[0], k.max[2]), "keepout " + k.id).Expanded(GalleryPlanH8S.KeepoutMargin));
            // CraftingSystem.Station: radius 2.2 at scale 1. CampaignDirector cast talk: 3 m indoors.
            rules.Circles.Add(new PlanCircleH8S(new Vector2(service.x, service.z), 2.2f + GalleryPlanH8S.InteractMargin, "interaction Forge"));
            rules.Circles.Add(new PlanCircleH8S(new Vector2(staff.x, staff.z), 3f + GalleryPlanH8S.InteractMargin, "interaction Talk Dante Rook"));
            // The station collider (1.5 by 0.6) at an unknown yaw: its half diagonal around the service point.
            float diagonal = Mathf.Sqrt(0.75f * 0.75f + 0.3f * 0.3f) + 0.2f;
            rules.Blocked.Add(new PlanRectH8S(new Vector2(service.x - diagonal, service.z - diagonal), new Vector2(service.x + diagonal, service.z + diagonal), "station").Expanded(GalleryPlanH8S.StationMargin));
            Vector2 door = new Vector2(exit.x, exit.z);
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(staff.x, staff.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to giver"));
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(service.x, service.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to station"));
            Vector3 eye = exit + Vector3.up * 1.6f;
            rules.Sights.Add(new SightLineH8S(eye, staff + Vector3.up * 1.65f, GalleryPlanH8S.SightClearance, "sight entrance to giver head"));
            rules.Sights.Add(new SightLineH8S(eye, staff + Vector3.up * 1.15f, GalleryPlanH8S.SightClearance, "sight entrance to giver chest"));
            rules.Sights.Add(new SightLineH8S(eye, staff + Vector3.up * 0.5f, GalleryPlanH8S.SightClearance, "sight entrance to giver knees"));
            rules.Sights.Add(new SightLineH8S(eye, service + Vector3.up * 1f, GalleryPlanH8S.SightClearance, "sight entrance to station"));
            rules.PreferNear = new Vector2(service.x, service.z);
            return rules;
        }

        private static PlanRectH8S Box(float x, float z, float w, float d)
        {
            return new PlanRectH8S(new Vector2(x - w * 0.5f, z - d * 0.5f), new Vector2(x + w * 0.5f, z + d * 0.5f), "test");
        }
    }
}
