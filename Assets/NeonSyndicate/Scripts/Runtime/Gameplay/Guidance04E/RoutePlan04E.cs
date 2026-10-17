using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One stretch of a route: its points in order and how close the player must come to pass each point.</summary>
    public struct RouteLeg04E
    {
        public List<Vector3> Points;
        /// <summary>Passing radius for these points: wide on streets, tight indoors, on stairs and underground.</summary>
        public float Trim;

        public RouteLeg04E(List<Vector3> points, float trim)
        {
            Points = points;
            Trim = trim;
        }
    }

    /// <summary>
    /// Pure route assembly for floor and region aware guidance (04E). The runtime collects the legs (a road route to
    /// an entrance, the doorway, a stair, the Underheart portals) and this class joins them in order, drops repeated
    /// points, and works out the up or down hint and the floor label. It never joins two points through a ceiling: a
    /// target on another floor or underground is only reached through the legs given. Engine-free.
    /// </summary>
    public static class RoutePlan04E
    {
        /// <summary>Road points are passed within this radius; indoor, stair and underground points within IndoorTrim.</summary>
        public const float StreetTrim = 14f, IndoorTrim = 2.6f;
        /// <summary>A target this far above or below the player gets an up or down hint.</summary>
        public const float VerticalHintMetres = 2.5f;
        /// <summary>Points closer than this to the previous one are dropped when legs are joined.</summary>
        public const float MergeDistance = 0.75f;
        /// <summary>In a two-storey 04C venue, local heights above this are the upper floor.</summary>
        public const float UpperFloorThreshold = 2f;

        /// <summary>The bottom and the top of the stair in a two-storey 04C venue, in the venue's local frame
        /// (InhabitedCity04CComposition.Stairs: treads at x -5.7 from z 6.0 rising 0.175 m per 0.32 m to the 4.2 m floor).</summary>
        public static readonly Vector3 VenueStairBottomLocal = new Vector3(-5.7f, 0f, 5.4f);
        public static readonly Vector3 VenueStairTopLocal = new Vector3(-5.7f, 4.2f, 14.3f);

        /// <summary>The Pressure House stairwell (VenueIds04E): the approach at the open front end of the opening and
        /// the landing at its bottom, in the venue's local frame.</summary>
        public static Vector3 PressureStairTopLocal
        {
            get { return new Vector3((VenueIds04E.StairX0 + VenueIds04E.StairX1) * 0.5f, 0f, VenueIds04E.StairZ0 - 0.6f); }
        }

        public static Vector3 PressureStairBottomLocal
        {
            get { return new Vector3((VenueIds04E.StairX0 + VenueIds04E.StairX1) * 0.5f, -VenueIds04E.StairDrop, VenueIds04E.StairBottomZ); }
        }

        /// <summary>True when a venue-local point is on the upper floor of a two-storey venue.</summary>
        public static bool IsUpperFloor(Vector3 local, bool twoStoreys)
        {
            return twoStoreys && local.y > UpperFloorThreshold;
        }

        /// <summary>
        /// Joins the legs in order into points and per-point passing radii. The first point is the start; repeated or
        /// near-identical points are merged (keeping the tighter radius).
        /// </summary>
        public static void Stitch(IList<RouteLeg04E> legs, List<Vector3> points, List<float> trims)
        {
            points.Clear();
            trims.Clear();
            if (legs == null) return;
            for (int l = 0; l < legs.Count; l++)
            {
                List<Vector3> pts = legs[l].Points;
                if (pts == null) continue;
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector3 p = pts[i];
                    if (!Finite(p)) continue;
                    if (points.Count > 0 && (points[points.Count - 1] - p).sqrMagnitude < MergeDistance * MergeDistance)
                    {
                        trims[trims.Count - 1] = Mathf.Min(trims[trims.Count - 1], legs[l].Trim);
                        continue;
                    }
                    points.Add(p);
                    trims.Add(legs[l].Trim);
                }
            }
        }

        /// <summary>+1 when the target is clearly above the player, -1 when clearly below, 0 on the same level.</summary>
        public static int Vertical(float fromY, float toY)
        {
            float dy = toY - fromY;
            if (dy > VerticalHintMetres) return 1;
            if (dy < -VerticalHintMetres) return -1;
            return 0;
        }

        /// <summary>The floor or region label for a target: "Lantern Refuge, 18 m below", "Sundial Survey Observatory,
        /// upper floor", "Cinder Dispatch, ground floor", or "" for an ordinary street target.</summary>
        public static string FloorLabel(string regionTitle, string venueTitle, bool upperFloor, float metresBelowPlayer)
        {
            if (!string.IsNullOrEmpty(regionTitle))
            {
                int m = Mathf.RoundToInt(Mathf.Abs(metresBelowPlayer));
                if (m < 3) return regionTitle;
                return regionTitle + ", " + m + " m " + (metresBelowPlayer > 0f ? "below" : "above");
            }
            if (!string.IsNullOrEmpty(venueTitle)) return venueTitle + (upperFloor ? ", upper floor" : ", ground floor");
            return "";
        }

        /// <summary>
        /// Index of the point the player is heading to after passing points: the first point after index 0 that is not
        /// within its passing radius (flat) and on roughly the same height (3.5 m). A route never skips a stair point
        /// just because the player stands above or below it.
        /// </summary>
        public static int NextIndex(IList<Vector3> points, IList<float> trims, Vector3 player)
        {
            if (points == null || points.Count < 2) return points != null && points.Count == 1 ? 0 : -1;
            int i = 1;
            while (i < points.Count - 1 && Passed(points[i], trims != null && i < trims.Count ? trims[i] : StreetTrim, player)) i++;
            return i;
        }

        /// <summary>True when the player is within the point's passing radius on the flat and within 3.5 m in height.</summary>
        public static bool Passed(Vector3 point, float trim, Vector3 player)
        {
            float dx = point.x - player.x, dz = point.z - player.z;
            return dx * dx + dz * dz < trim * trim && Mathf.Abs(point.y - player.y) < 3.5f;
        }

        /// <summary>Route length: flat on streets, full 3D where the route climbs (stairs, ramps, the Underheart).</summary>
        public static float Length(IList<Vector3> points)
        {
            float d = 0f;
            if (points == null) return d;
            for (int i = 1; i < points.Count; i++) d += Vector3.Distance(points[i - 1], points[i]);
            return d;
        }

        private static bool Finite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
        }
    }
}
