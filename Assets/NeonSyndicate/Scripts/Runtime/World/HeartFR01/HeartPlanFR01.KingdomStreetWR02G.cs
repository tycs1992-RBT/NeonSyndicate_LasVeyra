using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One flat flagstone clipped to a real route and outside registered ground openings.</summary>
    public sealed class KingdomStreetStoneWR02G
    {
        public List<Vector2> Points;
        public bool Trim;
    }

    public static partial class HeartPlanFR01
    {
        // Bounds keep a malformed future route from creating an unbounded startup mesh.
        private const int KingdomStreetStoneLimitWR02G = 18000;
        private const float KingdomStoneWidthWR02G = 1.6f, KingdomStoneDepthWR02G = .9f, KingdomJointWR02G = .025f;

        /// <summary>
        /// Lay staggered stones inside the existing non-overlapping route union. The narrow joints expose the
        /// original supported road. The western threshold uses occasional missing stones to suggest an older road.
        /// This generates no collision, floor, route or interaction. Ground openings are cut with a margin.
        /// </summary>
        public static List<KingdomStreetStoneWR02G> KingdomStreetStonesWR02G(
            IList<HeartStreetPieceWR02D> routes, IList<RectF> apertures)
        {
            List<KingdomStreetStoneWR02G> result = new List<KingdomStreetStoneWR02G>();
            if (routes == null || routes.Count == 0) return result;
            int visited = 0;
            List<List<Vector2>> holes = new List<List<Vector2>>();
            if (apertures != null) foreach (RectF hole in apertures)
            {
                if (!FiniteStreetWR02D(hole.X0) || !FiniteStreetWR02D(hole.X1) || !FiniteStreetWR02D(hole.Z0) ||
                    !FiniteStreetWR02D(hole.Z1) || hole.X1 <= hole.X0 || hole.Z1 <= hole.Z0)
                    return new List<KingdomStreetStoneWR02G>();
                holes.Add(KingdomRectangleWR02G(hole.X0 - .15f, hole.Z0 - .15f, hole.X1 + .15f, hole.Z1 + .15f));
            }
            foreach (HeartStreetPieceWR02D route in routes)
            {
                if (route == null || route.Points == null || route.Points.Count < 3) return new List<KingdomStreetStoneWR02G>();
                List<List<Vector2>> fragments = new List<List<Vector2>> { route.Points };
                foreach (List<Vector2> hole in holes)
                {
                    List<List<Vector2>> remaining = new List<List<Vector2>>();
                    foreach (List<Vector2> fragment in fragments) StreetSubtractWR02D(fragment, hole, remaining);
                    fragments = remaining;
                    if (fragments.Count > 4096) return new List<KingdomStreetStoneWR02G>();
                }
                foreach (List<Vector2> polygon in fragments)
                {
                    float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                    foreach (Vector2 point in polygon)
                    {
                        if (!FiniteStreetWR02D(point.x) || !FiniteStreetWR02D(point.y) || Mathf.Abs(point.x) > 10000 || Mathf.Abs(point.y) > 10000)
                            return new List<KingdomStreetStoneWR02G>();
                        minX = Mathf.Min(minX, point.x); maxX = Mathf.Max(maxX, point.x);
                        minZ = Mathf.Min(minZ, point.y); maxZ = Mathf.Max(maxZ, point.y);
                    }
                    int firstRow = Mathf.FloorToInt(minZ / KingdomStoneDepthWR02G), lastRow = Mathf.FloorToInt(maxZ / KingdomStoneDepthWR02G);
                    for (int row = firstRow; row <= lastRow; row++)
                    {
                        float stagger = (row & 1) == 0 ? 0f : KingdomStoneWidthWR02G * .5f;
                        int first = Mathf.FloorToInt((minX - stagger) / KingdomStoneWidthWR02G);
                        int last = Mathf.FloorToInt((maxX - stagger) / KingdomStoneWidthWR02G);
                        for (int column = first; column <= last; column++)
                        {
                            if (++visited > 200000) return new List<KingdomStreetStoneWR02G>();
                            float x = column * KingdomStoneWidthWR02G + stagger, z = row * KingdomStoneDepthWR02G;
                            int mark = unchecked((column * 73856093) ^ (row * 19349663)) & 0x7fffffff;
                            // A deliberately irregular surface over the original road, never an unsupported gap.
                            if (!route.Edge && x < -58f && mark % 9 == 0) continue;
                            List<Vector2> cell = KingdomRectangleWR02G(x + KingdomJointWR02G, z + KingdomJointWR02G,
                                x + KingdomStoneWidthWR02G - KingdomJointWR02G, z + KingdomStoneDepthWR02G - KingdomJointWR02G);
                            List<Vector2> clipped = polygon;
                            for (int side = 0; side < cell.Count && clipped.Count >= 3; side++)
                                clipped = StreetClipWR02D(clipped, cell[side], cell[(side + 1) % cell.Count], true);
                            if (clipped.Count < 3 || StreetAreaWR02D(clipped) < .008f) continue;
                            // Different stone marks the court, the Forge crossing and the Archive approach.
                            float cx = x + KingdomStoneWidthWR02G * .5f, cz = z + KingdomStoneDepthWR02G * .5f;
                            float court = (cx * cx) + (cz - 38f) * (cz - 38f);
                            float fx = Mathf.Abs(cx - 42f), fz = cz - 68f;
                            bool forgeAnvil = (fx < 2.6f && Mathf.Abs(fz) < .7f) || (fx < .9f && fz > -2.5f && fz < .7f) ||
                                (fx < 1.8f && fz > -3.2f && fz < -2.5f);
                            float ax = Mathf.Abs(cx - 12f), az = Mathf.Abs(cz - 92f);
                            bool archiveBook = (ax < 2.5f && az > 1f && az < 1.8f) || (ax < .8f && az < 1.8f);
                            bool trim = route.Edge || mark % 17 == 0 || (court > 4f && court < 12f) || forgeAnvil || archiveBook;
                            result.Add(new KingdomStreetStoneWR02G { Points = clipped, Trim = trim });
                            if (result.Count > KingdomStreetStoneLimitWR02G) return new List<KingdomStreetStoneWR02G>();
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>Older uncut paving is safe as a fallback only if it misses every registered ground opening.</summary>
        public static bool KingdomLegacyStreetClearWR02G(IList<HeartStreetPieceWR02D> routes, IList<RectF> apertures)
        {
            if (routes == null || routes.Count == 0 || apertures == null) return false;
            foreach (HeartStreetPieceWR02D route in routes)
            {
                if (route == null || route.Points == null || route.Points.Count < 3) return false;
                foreach (Vector2 p in route.Points) if (!FiniteStreetWR02D(p.x) || !FiniteStreetWR02D(p.y)) return false;
            }
            foreach (RectF hole in apertures)
            {
                if (!FiniteStreetWR02D(hole.X0) || !FiniteStreetWR02D(hole.X1) || !FiniteStreetWR02D(hole.Z0) ||
                    !FiniteStreetWR02D(hole.Z1) || hole.X1 <= hole.X0 || hole.Z1 <= hole.Z0) return false;
                List<Vector2> rectangle = KingdomRectangleWR02G(hole.X0 - .15f, hole.Z0 - .15f, hole.X1 + .15f, hole.Z1 + .15f);
                foreach (HeartStreetPieceWR02D route in routes)
                {
                    List<Vector2> overlap = route.Points;
                    for (int side = 0; side < rectangle.Count && overlap.Count >= 3; side++)
                        overlap = StreetClipWR02D(overlap, rectangle[side], rectangle[(side + 1) % rectangle.Count], true);
                    if (overlap.Count >= 3 && StreetAreaWR02D(overlap) > .00001f) return false;
                }
            }
            return true;
        }

        private static List<Vector2> KingdomRectangleWR02G(float x0, float z0, float x1, float z1)
        {
            return new List<Vector2> { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1) };
        }
    }
}
