using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class HeartCompositionPlacementWR02B
    {
        public string stableId = "", zone = "", route = "", family = "RootFernBank", policy = "SOFT_FOLIAGE_ONLY";
        public Vector3 position, size;
        public float yaw;
        public bool nativePhysicsRequired;
    }

    public static partial class HeartPlanFR01
    {
        public const int CompositionSoftLimitWR02B = 48;
        public static int CompositionZoneLimitWR02B(string zone)
        { return zone == "Grotto approach" ? 9 : CompositionSoftLimitWR02B / 4; }

        /// <summary>Region-specific crowns, using existing tree positions and original noncolliding crown envelopes.</summary>
        public static string CompositionCanopyWR02B(int region)
        { return region == 0 ? "SanctuaryCedar" : region == 1 ? "AzureElder" : ""; }

        public static string CompositionZoneWR02B(string route)
        {
            if (route == "home-court" || route == "forge-market" || route == "office") return "Gothic settlement";
            if (route == "sanctuary-approach" || route == "sanctuary-wilds") return "Sanctuary grounds";
            if (route == "grotto-through-route" || route == "grotto-return") return "Grotto approach";
            if (route == "shardwood-threshold" || route == "amethyst-challenge" || route == "azure-expedition" || route == "azure-return") return "Woodland causeway";
            if (route == "storm-shrine" || route.StartsWith("04og/storm-", StringComparison.Ordinal)) return "Wilderness threshold";
            return "";
        }

        /// <summary>Two deliberate roadside layers, derived from existing route segments rather than random world scatter.</summary>
        public static List<HeartCompositionPlacementWR02B> CompositionProposalsWR02B(CityLayout layout)
        {
            List<HeartCompositionPlacementWR02B> result = new List<HeartCompositionPlacementWR02B>();
            if (layout == null || !layout.GothicCivicH5) return result;
            foreach (RealmRoute04OB route in layout.RealmRoutes04OB)
            {
                string zone = CompositionZoneWR02B(route.Id);
                if (zone.Length == 0) continue;
                for (int segment = 1; segment < route.Points.Count; segment++)
                {
                    Vector3 a = route.Points[segment - 1], b = route.Points[segment], forward = MathUtil.Flat(b - a).normalized;
                    if (forward.sqrMagnitude < .5f) continue;
                    Vector3 right = MathUtil.RightOf(forward);
                    int samples = Mathf.Max(1, Mathf.FloorToInt(MathUtil.FlatDistance(a, b) / 18f));
                    for (int sample = 0; sample < samples; sample++) for (int row = 0; row < 2; row++) for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3 position = Vector3.Lerp(a, b, (sample + .5f) / samples) + right * sign * (route.Width * .5f + 7.5f + row * 7f);
                        result.Add(new HeartCompositionPlacementWR02B
                        {
                            stableId = "revision:10/composition:" + route.Id + "/" + segment + "/" + sample + "/" + row + "/" + sign,
                            route = route.Id, zone = zone, position = position, size = new Vector3(6, 1.4f, 4), yaw = MathUtil.YawOf(forward) + sign * 13
                        });
                    }
                }
            }
            return result;
        }

        /// <summary>Use the established route, parcel, vehicle, receipt and original-art reservations without a new planner authority.</summary>
        public static bool CompositionClearWR02B(CityLayout layout, HeartCompositionPlacementWR02B piece, IList<HeartLivingFR02A> reserved, out string reason)
        {
            reason = "Outside the bounded revision-10 first region.";
            if (layout == null || !layout.GothicCivicH5 || piece == null || !SurfaceRegionWR02(layout, piece.position) || !MathUtil.IsFinite(piece.position) || !MathUtil.IsFinite(piece.size) || float.IsNaN(piece.yaw) || float.IsInfinity(piece.yaw)) return false;
            if (piece.size.x <= 0 || piece.size.y <= 0 || piece.size.z <= 0 || piece.nativePhysicsRequired || piece.policy != "SOFT_FOLIAGE_ONLY")
            { reason = "New solid geometry requires native physical correspondence, not automatic presentation admission."; return false; }
            HeartLivingFR02A proxy = new HeartLivingFR02A { Position = piece.position, Size = piece.size, Yaw = piece.yaw, Module = piece.family };
            RectF area = LivingFootprint(proxy, 1f);
            foreach (Block block in layout.Blocks) foreach (Lot lot in block.Lots) foreach (Building building in lot.Buildings)
                if (area.Overlaps(building.Footprint, 3)) { reason = "Original parcel/service envelope " + building.Seed; return false; }
            foreach (RealmRoute04OB route in layout.RealmRoutes04OB) for (int i = 1; i < route.Points.Count; i++)
                if (LivingCrosses(proxy, route.Points[i - 1], route.Points[i], route.Width * .5f + 2.5f)) { reason = "Original route " + route.Id; return false; }
            foreach (WalkLink link in layout.WalkLinks)
                if (LivingCrosses(proxy, layout.WalkNodes[link.A].Pos, layout.WalkNodes[link.B].Pos, 3)) { reason = "Original pedestrian graph"; return false; }
            foreach (RoadEdge road in layout.Edges) if (area.Overlaps(layout.EdgeRect(road), 3)) { reason = "Original vehicle corridor"; return false; }
            foreach (List<Vector3> points in new[] { layout.DataChipSpots, layout.CrystalShardSpots, layout.SpellStoneSpots, layout.PickupSpots, layout.CreatureSpots, layout.ParkingSpots, layout.RadioMastSpots })
                foreach (Vector3 point in points) if (area.Inset(-6).Contains(point)) { reason = "Original receipt, creature or vehicle reservation"; return false; }
            foreach (PropSpot prop in layout.Props) if (area.Inset(-2).Contains(prop.Pos)) { reason = "Original physical prop"; return false; }
            foreach (SpecialSite site in layout.Sites) if (area.Inset(-site.Radius - 4).Contains(site.Position)) { reason = "Original discovery or story site"; return false; }
            foreach (Poi poi in layout.Pois) if (area.Inset(-10).Contains(poi.Position)) { reason = "Original point of interest"; return false; }
            for (int index = 0; index < GateCandidates; index++) { Vector3 at; float yaw; string why; if (GateCandidate(layout, index, out at, out yaw, out why) && !OutsideGate(piece.position, Mathf.Max(piece.size.x, piece.size.z) * .5f, at, yaw)) { reason = "Existing compact gothic gateway reservation"; return false; } }
            foreach (HeartSideFR01 side in StreetSides(layout)) if (Clear(layout, side.Position, side.Radius) && area.Inset(-side.Radius - 1).Contains(side.Position)) { reason = "Delivered original street-bay reservation"; return false; }
            if (reserved != null) foreach (HeartLivingFR02A other in reserved) if (area.Overlaps(LivingFootprint(other), 2)) { reason = "Existing or staged architectural reservation"; return false; }
            reason = "Source reservations clear. Actual ground, actors, triggers and geometry still require the existing native admission probes.";
            return true;
        }

        public static bool CompositionObservedWR02B(bool requestedActive, bool admitted, bool present, bool activeHierarchy)
        { return requestedActive && admitted && present && activeHierarchy; }

        public static bool CompositionPolicyWR02B(bool preview, bool selected, bool compare, bool suspended, bool prepared)
        { return preview && selected && !compare && !suspended && prepared; }
    }
}
