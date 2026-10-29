using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A single non-overlapping convex portion of an existing supported realm route.</summary>
    public sealed class HeartStreetPieceWR02D
    {
        public readonly List<Vector2> Points;
        public readonly bool Edge;
        public HeartStreetPieceWR02D(List<Vector2> points, bool edge) { Points = points; Edge = edge; }
    }

    public static partial class HeartPlanFR01
    {
        private sealed class StreetRectangleWR02D
        {
            public List<Vector2> Points;
            public bool Edge;
        }

        /// <summary>
        /// Divide the original four street strips into continuous centre and narrow stone-edge areas.
        /// Every polygon stays inside a real route rectangle. Earlier rectangles are subtracted so crossings
        /// and bends do not produce coplanar overlaps. The original route and its physics are never changed.
        /// </summary>
        public static List<HeartStreetPieceWR02D> StreetPlanWR02D(IList<RealmRoute04OB> routes, out float height)
        {
            height = 0f;
            List<HeartStreetPieceWR02D> result = new List<HeartStreetPieceWR02D>();
            if (routes == null) return result;
            string[] ids = { "home-court", "court-crossing", "sanctuary-approach", "forge-market" };
            List<StreetRectangleWR02D> centres = new List<StreetRectangleWR02D>(), edges = new List<StreetRectangleWR02D>();
            HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
            bool hasHeight = false;
            foreach (RealmRoute04OB route in routes)
            {
                if (route == null || Array.IndexOf(ids, route.Id) < 0) continue;
                if (!found.Add(route.Id) || route.Points == null || route.Points.Count < 2 || !FiniteStreetWR02D(route.Width) || route.Width < 2f || route.Width > 12f) return new List<HeartStreetPieceWR02D>();
                float half = route.Width * .5f, edge = .45f;
                for (int i = 1; i < route.Points.Count; i++)
                {
                    Vector3 a = route.Points[i - 1], b = route.Points[i];
                    if (!MathUtil.IsFinite(a) || !MathUtil.IsFinite(b) || Mathf.Abs(a.y - b.y) > .001f) return new List<HeartStreetPieceWR02D>();
                    if (!hasHeight) { height = a.y; hasHeight = true; }
                    if (Mathf.Abs(a.y - height) > .001f) return new List<HeartStreetPieceWR02D>();
                    Vector2 start = new Vector2(a.x, a.z), delta = new Vector2(b.x - a.x, b.z - a.z);
                    float length = delta.magnitude;
                    if (length < .1f || length > 300f) return new List<HeartStreetPieceWR02D>();
                    Vector2 side = new Vector2(-delta.y / length, delta.x / length), end = new Vector2(b.x, b.z);
                    centres.Add(new StreetRectangleWR02D { Points = StreetRectWR02D(start, end, side, -half + edge, half - edge) });
                    edges.Add(new StreetRectangleWR02D { Points = StreetRectWR02D(start, end, side, -half, -half + edge), Edge = true });
                    edges.Add(new StreetRectangleWR02D { Points = StreetRectWR02D(start, end, side, half - edge, half), Edge = true });
                }
            }
            if (found.Count != ids.Length || !hasHeight) return new List<HeartStreetPieceWR02D>();
            centres.AddRange(edges);
            List<List<Vector2>> covered = new List<List<Vector2>>();
            foreach (StreetRectangleWR02D rectangle in centres)
            {
                List<List<Vector2>> fragments = new List<List<Vector2>> { rectangle.Points };
                foreach (List<Vector2> prior in covered)
                {
                    List<List<Vector2>> remaining = new List<List<Vector2>>();
                    foreach (List<Vector2> fragment in fragments) StreetSubtractWR02D(fragment, prior, remaining);
                    fragments = remaining;
                    if (fragments.Count == 0) break;
                    if (fragments.Count + result.Count > 4096) return new List<HeartStreetPieceWR02D>();
                }
                foreach (List<Vector2> fragment in fragments)
                    if (StreetAreaWR02D(fragment) > .0001f) result.Add(new HeartStreetPieceWR02D(fragment, rectangle.Edge));
                covered.Add(rectangle.Points);
            }
            return result;
        }

        private static bool FiniteStreetWR02D(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static List<Vector2> StreetRectWR02D(Vector2 a, Vector2 b, Vector2 side, float left, float right)
        { return new List<Vector2> { a + side * left, b + side * left, b + side * right, a + side * right }; }
        private static float StreetCrossWR02D(Vector2 a, Vector2 b, Vector2 p)
        { return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x); }
        private static float StreetAreaWR02D(List<Vector2> polygon)
        {
            float sum = 0f;
            for (int i = 0; i < polygon.Count; i++) { Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count]; sum += a.x * b.y - b.x * a.y; }
            return Mathf.Abs(sum) * .5f;
        }
        private static List<Vector2> StreetClipWR02D(List<Vector2> input, Vector2 a, Vector2 b, bool inside)
        {
            List<Vector2> output = new List<Vector2>();
            if (input.Count == 0) return output;
            Vector2 previous = input[input.Count - 1]; float before = StreetCrossWR02D(a, b, previous);
            foreach (Vector2 current in input)
            {
                float after = StreetCrossWR02D(a, b, current);
                bool oldIn = inside ? before >= 0f : before <= 0f, newIn = inside ? after >= 0f : after <= 0f;
                if (oldIn != newIn)
                {
                    float t = before / (before - after);
                    output.Add(previous + (current - previous) * t);
                }
                if (newIn) output.Add(current);
                previous = current; before = after;
            }
            return StreetCleanWR02D(output);
        }
        private static List<Vector2> StreetCleanWR02D(List<Vector2> points)
        {
            List<Vector2> clean = new List<Vector2>();
            foreach (Vector2 point in points)
            {
                if (clean.Count > 0)
                {
                    Vector2 d = point - clean[clean.Count - 1];
                    if (d.x * d.x + d.y * d.y < .0000000001f) continue;
                }
                clean.Add(point);
            }
            if (clean.Count > 1)
            {
                Vector2 d = clean[0] - clean[clean.Count - 1];
                if (d.x * d.x + d.y * d.y < .0000000001f) clean.RemoveAt(clean.Count - 1);
            }
            return clean;
        }
        private static void StreetSubtractWR02D(List<Vector2> subject, List<Vector2> clip, List<List<Vector2>> output)
        {
            List<Vector2> remaining = subject;
            for (int edge = 0; edge < clip.Count && remaining.Count >= 3; edge++)
            {
                Vector2 a = clip[edge], b = clip[(edge + 1) % clip.Count];
                List<Vector2> outside = StreetClipWR02D(remaining, a, b, false);
                if (outside.Count >= 3 && StreetAreaWR02D(outside) > .0001f) output.Add(outside);
                remaining = StreetClipWR02D(remaining, a, b, true);
            }
        }
    }
}
