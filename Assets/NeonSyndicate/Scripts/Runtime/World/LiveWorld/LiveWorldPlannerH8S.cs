using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A planned spot for one outdoor copy: its plan position (at the place's height; the runtime grounds it) and facing.</summary>
    public struct LiveWorldSpotH8S
    {
        /// <summary>Plan position; y is the place's height until the runtime finds the real floor.</summary>
        public Vector3 Position;
        /// <summary>Yaw in degrees.</summary>
        public float Yaw;
        /// <summary>Which candidate this was (0 is the requested offset).</summary>
        public int Candidate;
    }

    /// <summary>
    /// Engine-free placement planning for live world art. Outdoor copies try the requested offset first and then the route
    /// preview's own candidate rings (WorldRouteH8S.CandidateOffsets: 6, 9, 12 and 15 m in eight directions), facing their
    /// place, and keep only spots whose turned bounds pass LiveWorldSafetyH8S. Route copies use the route preview's dressing
    /// slots. Room furnishings use the Forge gallery's floor-plan rules (GalleryPlanH8S) for any venue room, and a replaced
    /// anvil uses the gallery's fit inside the original collider (ForgeGalleryFitH8S). Pieces face with their front, the side
    /// seen in Blender's Front view, towards their place: the project's exports (forward minus Z, up Y) and Unity's mirror
    /// of x map Blender (x, y, z) to Unity (minus x, z, minus y), so that side is Unity's local plus Z.
    /// </summary>
    public static class LiveWorldPlannerH8S
    {
        /// <summary>Guards are gathered within this many metres of a place (plus each route's own half width).</summary>
        public const float GuardRadius = 45f;
        /// <summary>Quarter turns added in rooms: a gallery spot with no turns already faces local plus Z, a piece's front, into the room.</summary>
        public const int RoomFrontTurns = 0;

        /// <summary>The yaw (degrees) that turns a piece's front (local plus Z) from position towards target, plus turn.</summary>
        public static float FacingYaw(Vector3 position, Vector3 target, float turn)
        {
            float dx = target.x - position.x, dz = target.z - position.z;
            if (dx * dx + dz * dz < 1e-6f) return Mathf.Repeat(turn, 360f);
            return Mathf.Repeat(Mathf.Atan2(dx, dz) * Mathf.Rad2Deg + turn, 360f);
        }

        /// <summary>
        /// The plan spots for one near copy, best first: every candidate offset around the place whose turned, scaled bounds
        /// pass the safety rules and the layout checks. firstRefusal says why the requested offset itself was refused (null
        /// when it passed). localMin and localMax are the piece's bounds at scale 1 relative to its pivot (Unity axes).
        /// </summary>
        public static List<LiveWorldSpotH8S> NearSpots(Vector3 place, Vector3 offset, float turn, float scale, Vector3 localMin, Vector3 localMax,
            LiveWorldRulesH8S rules, CityLayout layout, out string firstRefusal)
        {
            List<LiveWorldSpotH8S> spots = new List<LiveWorldSpotH8S>();
            firstRefusal = null;
            List<Vector3> offsets = WorldRouteH8S.CandidateOffsets(new Vector3(offset.x, 0f, offset.z));
            for (int i = 0; i < offsets.Count; i++)
            {
                Vector3 at = place + offsets[i] + new Vector3(0f, offset.y, 0f);
                float yaw = FacingYaw(at, place, turn);
                string why = Refusal(at, yaw, scale, localMin, localMax, rules, layout);
                if (why != null)
                {
                    if (i == 0) firstRefusal = why;
                    continue;
                }
                spots.Add(new LiveWorldSpotH8S { Position = at, Yaw = yaw, Candidate = i });
            }
            return spots;
        }

        /// <summary>Null when a copy at this plan spot and yaw passes the safety rules and the layout checks, otherwise why not.</summary>
        public static string Refusal(Vector3 at, float yaw, float scale, Vector3 localMin, Vector3 localMax, LiveWorldRulesH8S rules, CityLayout layout)
        {
            if (!LiveWorldSafetyH8S.FiniteBounds(localMin, localMax) || !(scale > 0f) || float.IsInfinity(scale) || !LiveWorldSafetyH8S.FiniteBounds(at, at))
                return "the piece's bounds are not finite";
            Vector3 min, max;
            WorldRouteH8S.YawedBounds(localMin * scale, localMax * scale, yaw, at, out min, out max);
            string why = LiveWorldSafetyH8S.Conflict(min, max, at.y, rules);
            if (why != null) return "inside the protected " + why;
            return LiveWorldSafetyH8S.LayoutConflict(layout, min, max);
        }

        /// <summary>
        /// The route copies: the route preview's dressing slots for an accent of this plan radius (beside every outdoor stop,
        /// facing it) that also pass the safety rules, at most WorldRouteH8S.MaxDressingPerStop per stop and count in all.
        /// </summary>
        public static List<WorldRouteSlotH8S> RouteSpots(IList<Vector3> points, IList<bool> resolved, IList<WorldRouteStopH8S> stops, IList<WorldRouteGuardH8S> routeGuards,
            float scale, Vector3 localMin, Vector3 localMax, int count, LiveWorldRulesH8S rules, CityLayout layout)
        {
            List<WorldRouteSlotH8S> kept = new List<WorldRouteSlotH8S>();
            if (points == null || resolved == null || stops == null || count <= 0) return kept;
            Vector3 min = localMin * scale, max = localMax * scale;
            float radius = Mathf.Sqrt(Mathf.Max(min.x * min.x, max.x * max.x) + Mathf.Max(min.z * min.z, max.z * max.z));
            List<WorldRouteSlotH8S> slots = WorldRouteH8S.DressingSlots(points, resolved, stops, radius, routeGuards);
            int[] perStop = new int[stops.Count];
            for (int i = 0; i < slots.Count && kept.Count < count; i++)
            {
                WorldRouteSlotH8S slot = slots[i];
                if (slot.Stop < 0 || slot.Stop >= perStop.Length || perStop[slot.Stop] >= WorldRouteH8S.MaxDressingPerStop) continue;
                if (Refusal(slot.Position, slot.Yaw, scale, localMin, localMax, rules, layout) != null) continue;
                perStop[slot.Stop]++;
                kept.Add(slot);
            }
            return kept;
        }

        /// <summary>
        /// The floor-plan rules of a venue room, as the Forge gallery review builds them for the Forge, in the room's local
        /// frame: the usable room rectangle, the composition's keepouts (with margin) and anchors other than the exit, the
        /// given interaction circles, the walks from the entrance to the giver and to the service point, the entrance sight
        /// lines to the giver's head, chest and knees and to the service point, and (when known) the station collider's plan
        /// rectangle with the station margin. The runtime adds the plan rectangles of the room's other colliders.
        /// </summary>
        public static GalleryRulesH8S RoomRules(VenueExpanded04C composition, Vector3 exit, Vector3 giver, Vector3 service, float floorY,
            IList<PlanCircleH8S> interactions, bool hasStation, PlanRectH8S station)
        {
            GalleryRulesH8S rules = new GalleryRulesH8S();
            if (composition == null) return rules;
            float half = composition.roomWidth * 0.5f - 0.3f;
            rules.Room = new PlanRectH8S(new Vector2(-half, 0.15f), new Vector2(half, composition.roomDepth - 0.3f), "room");
            rules.FloorY = floorY;
            if (composition.keepouts != null)
                foreach (VenueKeepout04C k in composition.keepouts)
                {
                    if (k == null || k.min == null || k.max == null || k.min.Length < 3 || k.max.Length < 3) continue;
                    rules.Blocked.Add(new PlanRectH8S(new Vector2(k.min[0], k.min[2]), new Vector2(k.max[0], k.max[2]), "keepout " + k.id).Expanded(GalleryPlanH8S.KeepoutMargin));
                }
            if (composition.anchors != null)
                foreach (VenueAnchor04C a in composition.anchors)
                {
                    if (a == null || a.position == null || a.position.Length < 3 || a.id == "exit") continue;
                    rules.Circles.Add(new PlanCircleH8S(new Vector2(a.position[0], a.position[2]), GalleryPlanH8S.AnchorRadius, "anchor " + a.id));
                }
            if (interactions != null) for (int i = 0; i < interactions.Count; i++) rules.Circles.Add(interactions[i]);
            if (hasStation) rules.Blocked.Add(station.Expanded(GalleryPlanH8S.StationMargin));
            Vector2 door = new Vector2(exit.x, exit.z);
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(giver.x, giver.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to giver"));
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(service.x, service.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to service point"));
            Vector3 eye = exit + Vector3.up * 1.6f;
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 1.65f, GalleryPlanH8S.SightClearance, "sight entrance to giver head"));
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 1.15f, GalleryPlanH8S.SightClearance, "sight entrance to giver chest"));
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 0.5f, GalleryPlanH8S.SightClearance, "sight entrance to giver knees"));
            rules.Sights.Add(new SightLineH8S(eye, service + Vector3.up * 1f, GalleryPlanH8S.SightClearance, "sight entrance to service point"));
            rules.PreferNear = new Vector2(service.x, service.z);
            return rules;
        }

        /// <summary>
        /// The quarter turns a room piece is drawn with for a gallery spot, so its front faces into the room. A turn line
        /// can only turn it right round (180 degrees), which keeps the planned footprint.
        /// </summary>
        public static int RoomTurns(GallerySpotH8S spot, float extraTurnDegrees)
        {
            int extra = (Mathf.RoundToInt(Mathf.Repeat(extraTurnDegrees, 360f) / 180f) % 2) * 2;
            return (spot.QuarterTurns + RoomFrontTurns + extra) % 4;
        }

        /// <summary>
        /// The anvil fit inside an original Forge station collider (the H8S gallery review's exact rule): largest scale up to
        /// native size, at least ForgeGalleryFitH8S.MinAnvilScale, resting on the block with every corner BoxMargin inside.
        /// </summary>
        public static ForgeFitH8S AnvilFit(Vector3 localMin, Vector3 localMax, Vector3 boxCenter, Vector3 boxSize, float blockTop, Vector2 blockCenter)
        {
            return ForgeGalleryFitH8S.Fit(localMin, localMax, boxCenter, boxSize, blockTop, blockCenter, ForgeGalleryFitH8S.BoxMargin,
                ForgeGalleryFitH8S.MinAnvilScale, ForgeGalleryFitH8S.MaxAnvilScale, ForgeGalleryFitH8S.MaxSlide, true);
        }

        /// <summary>
        /// The five floor probe points of a footprint, in world X/Z at the given height: four corners inset by inset metres
        /// (at most a quarter of each side) and the centre. corners must hold at least five entries.
        /// </summary>
        public static void ProbePoints(Vector3 min, Vector3 max, float inset, float y, Vector3[] corners)
        {
            float ix = Mathf.Min(inset, (max.x - min.x) * 0.25f), iz = Mathf.Min(inset, (max.z - min.z) * 0.25f);
            corners[0] = new Vector3(min.x + ix, y, min.z + iz);
            corners[1] = new Vector3(max.x - ix, y, min.z + iz);
            corners[2] = new Vector3(min.x + ix, y, max.z - iz);
            corners[3] = new Vector3(max.x - ix, y, max.z - iz);
            corners[4] = new Vector3((min.x + max.x) * 0.5f, y, (min.z + max.z) * 0.5f);
        }

        /// <summary>
        /// The largest height difference allowed between the floor under a footprint's probes before the piece counts as
        /// hanging over an edge: 0.6 m, or 12 percent of the footprint's shorter side when that is more (at most 1.2 m).
        /// </summary>
        public static float GroundTolerance(Vector3 min, Vector3 max)
        {
            float side = Mathf.Min(max.x - min.x, max.z - min.z);
            return Mathf.Clamp(side * 0.12f, 0.6f, 1.2f);
        }

        /// <summary>The LOD culling screen height for a piece: outdoor pieces stay visible longer than small indoor furnishings.</summary>
        public static float CullHeight(bool indoor)
        {
            return indoor ? 0.03f : 0.015f;
        }
    }
}
