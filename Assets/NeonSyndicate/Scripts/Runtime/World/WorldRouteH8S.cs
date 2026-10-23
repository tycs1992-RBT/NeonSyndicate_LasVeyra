using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The visual region a route stop belongs to: it picks the dressing palette.</summary>
    public enum WorldRouteRegionH8S
    {
        /// <summary>Veyra Lofts and the home court.</summary>
        Home,
        /// <summary>Dante Rook's Crystal Forge and its door.</summary>
        Forge,
        /// <summary>The Amethyst investigation (hearth lens and oath witness).</summary>
        Amethyst,
        /// <summary>Azure Shardwood (shrine and root spring).</summary>
        Azure
    }

    /// <summary>One ordered stop of the H8S revision-10 route: the existing place token it resolves through, its purpose and how long a player spends there.</summary>
    public sealed class WorldRouteStopH8S
    {
        /// <summary>Short stable id used in logs.</summary>
        public readonly string Id;
        /// <summary>The existing place token: Realm: anchors resolve through CityLayout, Venue: and Cast: through the live 04E resolver.</summary>
        public readonly string Token;
        /// <summary>Readable title.</summary>
        public readonly string Title;
        /// <summary>What the player does here.</summary>
        public readonly string Purpose;
        /// <summary>Region for the dressing palette.</summary>
        public readonly WorldRouteRegionH8S Region;
        /// <summary>Estimated seconds spent here (dialogue, reading, the finite fights). An estimate, not a measurement.</summary>
        public readonly float DwellSeconds;
        /// <summary>Plan radius kept free of overlays around the stop: the largest quest trigger radius here plus two metres, at least four.</summary>
        public readonly float ProtectRadius;
        /// <summary>True for a stop inside a room, where no outdoor dressing may be placed.</summary>
        public readonly bool Interior;
        /// <summary>For an indoor stop, the existing token of the room's door point: legs leave and enter through it (null outdoors).</summary>
        public readonly string DoorToken;

        /// <summary>A route stop.</summary>
        public WorldRouteStopH8S(string id, string token, string title, string purpose, WorldRouteRegionH8S region, float dwellSeconds, float protectRadius, bool interior, string doorToken = null)
        {
            Id = id;
            Token = token;
            Title = title;
            Purpose = purpose;
            Region = region;
            DwellSeconds = dwellSeconds;
            ProtectRadius = protectRadius;
            Interior = interior;
            DoorToken = doorToken;
        }
    }

    /// <summary>A protected part of the route: a disc around a stop (A equals B) or a corridor around a leg segment.</summary>
    public struct WorldRouteGuardH8S
    {
        /// <summary>Segment start, or the stop.</summary>
        public Vector3 A;
        /// <summary>Segment end (equal to A for a stop disc).</summary>
        public Vector3 B;
        /// <summary>Protected plan distance from the segment or point, in metres.</summary>
        public float Radius;
        /// <summary>What this guard protects, for logs.</summary>
        public string Label;
    }

    /// <summary>A candidate spot for one restrained dressing accent beside a stop.</summary>
    public struct WorldRouteSlotH8S
    {
        /// <summary>Plan position (height is the stop's; the review grounds it on the real floor).</summary>
        public Vector3 Position;
        /// <summary>Yaw in degrees that faces the accent towards its stop.</summary>
        public float Yaw;
        /// <summary>Index of the stop it dresses.</summary>
        public int Stop;
    }

    /// <summary>The time estimate for the route at the player controller's real movement speeds.</summary>
    public struct WorldRouteEstimateH8S
    {
        /// <summary>Total length of the measured legs, in metres.</summary>
        public float Meters;
        /// <summary>Seconds to cover the legs at TravelMath.WalkSpeed.</summary>
        public float WalkSeconds;
        /// <summary>Seconds at TravelMath.JogSpeed, the controller's default on-foot run.</summary>
        public float RunSeconds;
        /// <summary>Seconds at TravelMath.SprintSpeed (ignores stamina, so a lower bound on foot).</summary>
        public float SprintSeconds;
        /// <summary>Seconds with the Surge travel power: TravelMath.SurgeTopSpeed plus one ramp per leg (ignores turns, so a lower bound).</summary>
        public float SurgeSeconds;
        /// <summary>Estimated seconds spent at the stops.</summary>
        public float DwellSeconds;
        /// <summary>Legs with a length.</summary>
        public int Legs;
        /// <summary>Legs that could not be measured (a stop did not resolve).</summary>
        public int MissingLegs;

        /// <summary>The segment estimate: running between stops plus the time at the stops.</summary>
        public float EstimateSeconds { get { return RunSeconds + DwellSeconds; } }

        /// <summary>True when the estimate lies inside the 4 to 8 minute target and no leg is missing.</summary>
        public bool WithinTarget { get { return MissingLegs == 0 && WorldRouteH8S.WithinTarget(EstimateSeconds); } }
    }

    /// <summary>
    /// Engine-free plan of the one coherent H8S route through revision 10 (Gothic Civic H5), in the Hearth Witness quest's
    /// own order: home and court, the Forge and its gallery, the Amethyst investigation (hearth lens, oath witness), the
    /// Azure shrine and root spring, and the safe Forge return. It estimates time from leg lengths and the controller's
    /// real speeds, and states the overlay clearance rule: a decorative overlay keeps out of a protected disc around every
    /// stop and a corridor around every straight leg (and every measured walk segment the review passes in).
    /// </summary>
    public static class WorldRouteH8S
    {
        /// <summary>The first quality target for the segment: four minutes.</summary>
        public const float TargetMinSeconds = 240f;
        /// <summary>The first quality target for the segment: eight minutes.</summary>
        public const float TargetMaxSeconds = 480f;
        /// <summary>Half width of the protected corridor around every leg, in metres.</summary>
        public const float LegHalfWidth = 3f;
        /// <summary>Extra gap between a stop's protected disc and a dressing accent, in metres.</summary>
        public const float DressingGap = 1.5f;
        /// <summary>At most this many dressing accents per stop.</summary>
        public const int MaxDressingPerStop = 2;
        /// <summary>At most this many dressing accents on the whole route.</summary>
        public const int MaxDressing = 8;

        /// <summary>The ordered stops (a new array each call, so callers cannot change shared state).</summary>
        public static WorldRouteStopH8S[] Stops()
        {
            return new[]
            {
                new WorldRouteStopH8S("home", "Venue:lofts.exit", "Home and court",
                    "Start at the Veyra Lofts door and cross the home court towards the Forge.", WorldRouteRegionH8S.Home, 0f, 4f, false),
                new WorldRouteStopH8S("forge", "Cast:dante", "Forge and gallery",
                    "Accept The Hearth Witness from Dante Rook, the original giver, beside the Forge station and its gallery furnishings.", WorldRouteRegionH8S.Forge, 30f, 4f, true, "Venue:forge.exit"),
                new WorldRouteStopH8S("hearth", "Realm:hearth-lens", "Amethyst investigation: hearth lens",
                    "Climb the home-side stair to the supported reading porch and read the hearth impression.", WorldRouteRegionH8S.Amethyst, 15f, 4.5f, false),
                new WorldRouteStopH8S("witness", "Realm:oath-witness", "Amethyst investigation: oath witness",
                    "Compare the sanctuary witness, then clear the two glass mites one at a time.", WorldRouteRegionH8S.Amethyst, 50f, 5f, false),
                new WorldRouteStopH8S("shrine", "Realm:storm-shrine", "Azure shrine",
                    "Cross into Azure Shardwood and settle the one territorial neon jackal.", WorldRouteRegionH8S.Azure, 30f, 9f, false),
                new WorldRouteStopH8S("spring", "Realm:storm-spring", "Root spring",
                    "Follow the grounded lower cavern route and complete the paired mineral survey.", WorldRouteRegionH8S.Azure, 15f, 4.5f, false),
                new WorldRouteStopH8S("return", "Venue:forge.exit", "Safe Forge return",
                    "Return to the Forge door and hear Dante Rook close the survey.", WorldRouteRegionH8S.Forge, 20f, 8f, false)
            };
        }

        /// <summary>True when the seconds lie inside the 4 to 8 minute target.</summary>
        public static bool WithinTarget(float seconds)
        {
            return seconds >= TargetMinSeconds && seconds <= TargetMaxSeconds;
        }

        /// <summary>Horizontal distance between two points.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x, dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Length of a polyline in 3D (zero for fewer than two points).</summary>
        public static float PolylineLength(IList<Vector3> points)
        {
            float total = 0f;
            if (points == null) return total;
            for (int i = 1; i < points.Count; i++) total += Vector3.Distance(points[i - 1], points[i]);
            return total;
        }

        /// <summary>
        /// The time estimate from leg lengths (a negative length marks a leg that could not be measured) and the stops'
        /// dwell, at TravelMath's walk, jog (default run) and sprint speeds.
        /// </summary>
        public static WorldRouteEstimateH8S Estimate(IList<float> legMeters, IList<WorldRouteStopH8S> stops)
        {
            WorldRouteEstimateH8S e = new WorldRouteEstimateH8S();
            if (legMeters != null)
                for (int i = 0; i < legMeters.Count; i++)
                {
                    if (legMeters[i] < 0f || float.IsNaN(legMeters[i]) || float.IsInfinity(legMeters[i])) { e.MissingLegs++; continue; }
                    e.Meters += legMeters[i];
                    e.Legs++;
                }
            if (stops != null) for (int i = 0; i < stops.Count; i++) if (stops[i] != null) e.DwellSeconds += stops[i].DwellSeconds;
            e.WalkSeconds = e.Meters / TravelMath.WalkSpeed;
            e.RunSeconds = e.Meters / TravelMath.JogSpeed;
            e.SprintSeconds = e.Meters / TravelMath.SprintSpeed;
            e.SurgeSeconds = e.Meters / TravelMath.SurgeTopSpeed + e.Legs * TravelMath.SurgeRampSeconds;
            return e;
        }

        /// <summary>
        /// The polyline a player follows from a to b: out through aDoor when a is indoors, along the layout's walk graph
        /// between the outdoor ends when CityLayout.FindWalkRoute04O connects them, otherwise straight, then in through
        /// bDoor when b is indoors. walked is false when the walk graph gave no route and the outdoor part is straight.
        /// </summary>
        public static List<Vector3> LegPolyline(CityLayout layout, Vector3 a, bool aIndoors, Vector3 aDoor, Vector3 b, bool bIndoors, Vector3 bDoor, out bool walked)
        {
            List<Vector3> line = new List<Vector3> { a };
            Vector3 from = aIndoors ? aDoor : a;
            Vector3 to = bIndoors ? bDoor : b;
            if (aIndoors) line.Add(aDoor);
            List<Vector3> walk = layout != null ? layout.FindWalkRoute04O(from, to) : null;
            walked = walk != null && walk.Count >= 2;
            if (walked) for (int i = 1; i < walk.Count; i++) line.Add(walk[i]);
            else line.Add(to);
            if (bIndoors) line.Add(b);
            return line;
        }

        /// <summary>
        /// The protected parts of the route: a disc of each resolved stop's ProtectRadius, a LegHalfWidth corridor around
        /// every straight leg between consecutive resolved stops, and the same corridor around every segment of the walk
        /// polylines given (null entries are skipped).
        /// </summary>
        public static List<WorldRouteGuardH8S> Guards(IList<Vector3> points, IList<bool> resolved, IList<WorldRouteStopH8S> stops, IList<List<Vector3>> walks)
        {
            List<WorldRouteGuardH8S> guards = new List<WorldRouteGuardH8S>();
            if (points == null || resolved == null || stops == null) return guards;
            int previous = -1;
            for (int i = 0; i < points.Count && i < resolved.Count && i < stops.Count; i++)
            {
                if (!resolved[i]) continue;
                guards.Add(new WorldRouteGuardH8S { A = points[i], B = points[i], Radius = stops[i].ProtectRadius, Label = "stop " + stops[i].Id });
                if (previous >= 0)
                    guards.Add(new WorldRouteGuardH8S { A = points[previous], B = points[i], Radius = LegHalfWidth, Label = "leg " + stops[previous].Id + " to " + stops[i].Id });
                previous = i;
            }
            if (walks != null)
                for (int w = 0; w < walks.Count; w++)
                {
                    List<Vector3> walk = walks[w];
                    if (walk == null) continue;
                    for (int k = 1; k < walk.Count; k++)
                        guards.Add(new WorldRouteGuardH8S { A = walk[k - 1], B = walk[k], Radius = LegHalfWidth, Label = "walk route " + w });
                }
            return guards;
        }

        /// <summary>Null when an overlay's world bounds keep clear of every guard on the plan, otherwise the guard it intrudes on.</summary>
        public static string OverlayConflict(Vector3 min, Vector3 max, IList<WorldRouteGuardH8S> guards)
        {
            if (guards == null) return null;
            PlanRectH8S rect = new PlanRectH8S(new Vector2(min.x, min.z), new Vector2(max.x, max.z), "overlay");
            for (int i = 0; i < guards.Count; i++)
            {
                WorldRouteGuardH8S g = guards[i];
                Vector2 a = new Vector2(g.A.x, g.A.z), b = new Vector2(g.B.x, g.B.z);
                float distance = (a - b).sqrMagnitude < 1e-8f ? GalleryPlanH8S.RectDistance(rect, a) : GalleryPlanH8S.SegmentRectDistance(a, b, rect);
                if (distance < g.Radius) return g.Label;
            }
            return null;
        }

        /// <summary>Most candidate dressing spots offered per stop (the review keeps at most MaxDressingPerStop of them).</summary>
        public const int SlotsPerStop = 4;

        /// <summary>
        /// Candidate spots for restrained dressing accents of the given plan radius. Around every resolved outdoor stop it
        /// tries eight directions (beside the direction of travel first, then the diagonals, ahead and behind) just outside
        /// the stop's protected disc, then a ring three metres further out, and keeps up to SlotsPerStop spots whose accent
        /// square already passes OverlayConflict against the guards. Each faces its stop. The review still checks every
        /// spot with the module's real bounds, the floor probe and the collider test.
        /// </summary>
        public static List<WorldRouteSlotH8S> DressingSlots(IList<Vector3> points, IList<bool> resolved, IList<WorldRouteStopH8S> stops, float accentRadius, IList<WorldRouteGuardH8S> guards)
        {
            List<WorldRouteSlotH8S> slots = new List<WorldRouteSlotH8S>();
            if (points == null || resolved == null || stops == null) return slots;
            int count = Mathf.Min(points.Count, Mathf.Min(resolved.Count, stops.Count));
            Vector3 grow = new Vector3(accentRadius, 0f, accentRadius);
            for (int i = 0; i < count; i++)
            {
                if (!resolved[i] || stops[i].Interior) continue;
                Vector3 dir = Vector3.zero;
                for (int p = i - 1; p >= 0 && dir.sqrMagnitude < 1e-4f; p--) if (resolved[p]) dir = Flat(points[i] - points[p]);
                for (int n = i + 1; n < count && dir.sqrMagnitude < 1e-4f; n++) if (resolved[n]) dir = Flat(points[n] - points[i]);
                dir = dir.sqrMagnitude < 1e-4f ? Vector3.forward : dir.normalized;
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                float reach = stops[i].ProtectRadius + accentRadius + DressingGap;
                int kept = 0;
                for (int ring = 0; ring < 2 && kept < SlotsPerStop; ring++)
                for (int k = 0; k < 8 && kept < SlotsPerStop; k++)
                {
                    Vector3 at = points[i] + SlotDirection(side, dir, k) * (reach + ring * 3f);
                    if (OverlayConflict(at - grow, at + grow, guards) != null) continue;
                    Vector3 face = points[i] - at;
                    slots.Add(new WorldRouteSlotH8S { Position = at, Yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, Stop = i });
                    kept++;
                }
            }
            return slots;
        }

        /// <summary>The k-th direction tried around a stop: right, left, the four diagonals, ahead, behind (unit length on the plan).</summary>
        private static Vector3 SlotDirection(Vector3 side, Vector3 dir, int k)
        {
            const float d = 0.70710678f;
            switch (k)
            {
                case 0: return side;
                case 1: return -side;
                case 2: return (side - dir) * d;
                case 3: return (-side - dir) * d;
                case 4: return (side + dir) * d;
                case 5: return (dir - side) * d;
                case 6: return dir;
                default: return -dir;
            }
        }

        /// <summary>The offsets tried for an H8R module around its anchor: its own H8R offset first, then rings of 6, 9, 12 and 15 metres in eight directions.</summary>
        public static List<Vector3> CandidateOffsets(Vector3 preferred)
        {
            List<Vector3> offsets = new List<Vector3> { preferred };
            float baseYaw = preferred.x * preferred.x + preferred.z * preferred.z > 1e-4f ? Mathf.Atan2(preferred.x, preferred.z) * Mathf.Rad2Deg : 0f;
            for (int ring = 0; ring < 4; ring++)
            {
                float distance = 6f + ring * 3f;
                for (int k = 0; k < 8; k++)
                {
                    float yaw = (baseYaw + k * 45f) * Mathf.Deg2Rad;
                    offsets.Add(new Vector3(Mathf.Sin(yaw) * distance, 0f, Mathf.Cos(yaw) * distance));
                }
            }
            return offsets;
        }

        /// <summary>The plan bounds of an axis-aligned box after a yaw (degrees) about its pivot, then moved to position.</summary>
        public static void YawedBounds(Vector3 localMin, Vector3 localMax, float yaw, Vector3 position, out Vector3 min, out Vector3 max)
        {
            Quaternion q = QMath.AY(yaw);
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3((i & 1) == 0 ? localMin.x : localMax.x, (i & 2) == 0 ? localMin.y : localMax.y, (i & 4) == 0 ? localMin.z : localMax.z);
                Vector3 w = position + q * c;
                min = new Vector3(Mathf.Min(min.x, w.x), Mathf.Min(min.y, w.y), Mathf.Min(min.z, w.z));
                max = new Vector3(Mathf.Max(max.x, w.x), Mathf.Max(max.y, w.y), Mathf.Max(max.z, w.z));
            }
        }

        /// <summary>A readable duration such as "5 min 12 s".</summary>
        public static string Minutes(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) return "unknown";
            int whole = Mathf.RoundToInt(seconds);
            return (whole / 60) + " min " + (whole % 60) + " s";
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
