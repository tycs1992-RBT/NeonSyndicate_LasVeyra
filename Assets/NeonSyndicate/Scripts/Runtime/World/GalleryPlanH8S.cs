using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A rectangle on a room's floor plan, axis aligned: Min.x to Max.x is local x, Min.y to Max.y is local z.</summary>
    public struct PlanRectH8S
    {
        /// <summary>Lower corner (local x, local z).</summary>
        public Vector2 Min;
        /// <summary>Upper corner (local x, local z).</summary>
        public Vector2 Max;
        /// <summary>What this rectangle is, for logs and refusals.</summary>
        public string Label;

        /// <summary>A rectangle from two corners given in any order.</summary>
        public PlanRectH8S(Vector2 a, Vector2 b, string label)
        {
            Min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            Label = label;
        }

        /// <summary>The middle of the rectangle.</summary>
        public Vector2 Center { get { return (Min + Max) * 0.5f; } }

        /// <summary>Width along local x and depth along local z.</summary>
        public Vector2 Size { get { return Max - Min; } }

        /// <summary>The same rectangle grown by the given metres on every side (shrunk when negative, never inverted).</summary>
        public PlanRectH8S Expanded(float by)
        {
            Vector2 c = Center;
            Vector2 half = new Vector2(Mathf.Max(0f, Size.x * 0.5f + by), Mathf.Max(0f, Size.y * 0.5f + by));
            return new PlanRectH8S(c - half, c + half, Label);
        }
    }

    /// <summary>A circle on the floor plan: an interaction radius or a protected anchor.</summary>
    public struct PlanCircleH8S
    {
        /// <summary>Centre (local x, local z).</summary>
        public Vector2 Center;
        /// <summary>Radius in metres, already including any margin.</summary>
        public float Radius;
        /// <summary>What this circle protects, for logs and refusals.</summary>
        public string Label;

        /// <summary>A protected circle.</summary>
        public PlanCircleH8S(Vector2 center, float radius, string label)
        {
            Center = center;
            Radius = radius;
            Label = label;
        }
    }

    /// <summary>A protected straight walk on the floor plan: a furnishing keeps at least HalfWidth from it.</summary>
    public struct PlanPathH8S
    {
        /// <summary>Start (local x, local z).</summary>
        public Vector2 A;
        /// <summary>End (local x, local z).</summary>
        public Vector2 B;
        /// <summary>Half the protected corridor width in metres.</summary>
        public float HalfWidth;
        /// <summary>What this walk is, for logs and refusals.</summary>
        public string Label;

        /// <summary>A protected walk corridor.</summary>
        public PlanPathH8S(Vector2 a, Vector2 b, float halfWidth, string label)
        {
            A = a;
            B = b;
            HalfWidth = halfWidth;
            Label = label;
        }
    }

    /// <summary>A protected line of sight in the room's local 3D frame, from an eye point to a target point.</summary>
    public struct SightLineH8S
    {
        /// <summary>The eye point.</summary>
        public Vector3 From;
        /// <summary>The point that must stay visible.</summary>
        public Vector3 To;
        /// <summary>How far a furnishing's box must keep from the line, in metres.</summary>
        public float Clearance;
        /// <summary>What this sight line is, for logs and refusals.</summary>
        public string Label;

        /// <summary>A protected line of sight.</summary>
        public SightLineH8S(Vector3 from, Vector3 to, float clearance, string label)
        {
            From = from;
            To = to;
            Clearance = clearance;
            Label = label;
        }
    }

    /// <summary>A candidate floor spot for a furnishing: its footprint, quarter turns, the wall it backs onto and its score (lower is better).</summary>
    public struct GallerySpotH8S
    {
        /// <summary>Footprint centre (local x, local z).</summary>
        public Vector2 Center;
        /// <summary>Quarter turns about local up (0 faces local +z into the room).</summary>
        public int QuarterTurns;
        /// <summary>The plan footprint after turning.</summary>
        public PlanRectH8S Footprint;
        /// <summary>Lower is better: the gap to the wall behind it, then the distance to the preferred point.</summary>
        public float Score;
        /// <summary>Which room wall the furnishing backs onto.</summary>
        public string Wall;
    }

    /// <summary>Everything a gallery furnishing must stay clear of, in a venue room's local frame (metres, x right, z into the room).</summary>
    public sealed class GalleryRulesH8S
    {
        /// <summary>The usable floor rectangle of the room (the furnishing also keeps WallMargin from it).</summary>
        public PlanRectH8S Room;
        /// <summary>Local height of the floor finish.</summary>
        public float FloorY;
        /// <summary>Spots nearest this point rank first (the Forge station).</summary>
        public Vector2 PreferNear;
        /// <summary>Rectangles a footprint may not overlap (keepouts, colliders, the station, placed items), margins included.</summary>
        public readonly List<PlanRectH8S> Blocked = new List<PlanRectH8S>();
        /// <summary>Circles a footprint may not enter (interaction radii, anchors), margins included.</summary>
        public readonly List<PlanCircleH8S> Circles = new List<PlanCircleH8S>();
        /// <summary>Walk corridors a footprint keeps clear of.</summary>
        public readonly List<PlanPathH8S> Paths = new List<PlanPathH8S>();
        /// <summary>Lines of sight a furnishing's box may not block.</summary>
        public readonly List<SightLineH8S> Sights = new List<SightLineH8S>();
    }

    /// <summary>
    /// Engine-free floor-plan maths for placing decorative furnishings in a venue room: distances between points, segments
    /// and rectangles, the clearance verdict for one footprint, and a deterministic ranked search for safe spots backed onto
    /// a wall. Used by the H8S Forge gallery review; the review still checks every spot against the real floor and colliders.
    /// </summary>
    public static class GalleryPlanH8S
    {
        /// <summary>Clearance every footprint keeps from the room walls, in metres.</summary>
        public const float WallMargin = 0.25f;
        /// <summary>A spot counts as backed onto a wall when its back is within this many metres of the wall margin.</summary>
        public const float WallBand = 0.6f;
        /// <summary>Margin added around every venue keepout (NPC routes and the public threshold).</summary>
        public const float KeepoutMargin = 0.3f;
        /// <summary>Margin added to every interaction radius.</summary>
        public const float InteractMargin = 0.3f;
        /// <summary>Protected radius around every venue anchor (NPC posts and quest points).</summary>
        public const float AnchorRadius = 1f;
        /// <summary>Half width of the protected walk from the entrance to the giver and to the station.</summary>
        public const float PathHalfWidth = 0.75f;
        /// <summary>How far a furnishing's box keeps from every protected line of sight.</summary>
        public const float SightClearance = 0.3f;
        /// <summary>Margin added around every existing collider found in the room.</summary>
        public const float ColliderMargin = 0.1f;
        /// <summary>Margin added around the Forge station's own collider.</summary>
        public const float StationMargin = 0.5f;
        /// <summary>Gap kept between two placed furnishings.</summary>
        public const float ItemGap = 0.5f;
        /// <summary>Grid step of the spot search, in metres.</summary>
        public const float GridStep = 0.25f;

        /// <summary>Distance from a point to a plan rectangle (zero inside).</summary>
        public static float RectDistance(PlanRectH8S r, Vector2 p)
        {
            float dx = Mathf.Max(Mathf.Max(r.Min.x - p.x, 0f), p.x - r.Max.x);
            float dz = Mathf.Max(Mathf.Max(r.Min.y - p.y, 0f), p.y - r.Max.y);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>True when two plan rectangles overlap with positive area (touching edges do not count).</summary>
        public static bool Overlaps(PlanRectH8S a, PlanRectH8S b)
        {
            return a.Min.x < b.Max.x && b.Min.x < a.Max.x && a.Min.y < b.Max.y && b.Min.y < a.Max.y;
        }

        /// <summary>Distance from a point to a segment on the plan.</summary>
        public static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float lengthSq = d.x * d.x + d.y * d.y;
            float t = lengthSq > 1e-12f ? Mathf.Clamp01(((p.x - a.x) * d.x + (p.y - a.y) * d.y) / lengthSq) : 0f;
            float x = a.x + d.x * t - p.x;
            float z = a.y + d.y * t - p.y;
            return Mathf.Sqrt(x * x + z * z);
        }

        /// <summary>True when the plan segment touches or crosses the rectangle (Liang-Barsky clipping).</summary>
        public static bool SegmentHitsRect(Vector2 a, Vector2 b, PlanRectH8S r)
        {
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            return ForgeGalleryFitH8S.Clip(-d.x, a.x - r.Min.x, ref t0, ref t1) && ForgeGalleryFitH8S.Clip(d.x, r.Max.x - a.x, ref t0, ref t1)
                && ForgeGalleryFitH8S.Clip(-d.y, a.y - r.Min.y, ref t0, ref t1) && ForgeGalleryFitH8S.Clip(d.y, r.Max.y - a.y, ref t0, ref t1);
        }

        /// <summary>Exact distance between a plan segment and a rectangle (zero when they touch or cross).</summary>
        public static float SegmentRectDistance(Vector2 a, Vector2 b, PlanRectH8S r)
        {
            if (SegmentHitsRect(a, b, r)) return 0f;
            // Two disjoint convex shapes: the closest pair has a vertex of one of them.
            float best = Mathf.Min(RectDistance(r, a), RectDistance(r, b));
            best = Mathf.Min(best, PointSegmentDistance(r.Min, a, b));
            best = Mathf.Min(best, PointSegmentDistance(r.Max, a, b));
            best = Mathf.Min(best, PointSegmentDistance(new Vector2(r.Min.x, r.Max.y), a, b));
            best = Mathf.Min(best, PointSegmentDistance(new Vector2(r.Max.x, r.Min.y), a, b));
            return best;
        }

        /// <summary>The plan footprint of an item whose unturned size is size (x width, y depth) centred at centre after quarterTurns.</summary>
        public static PlanRectH8S Footprint(Vector2 centre, Vector2 size, int quarterTurns, string label)
        {
            Vector2 half = (Turns(quarterTurns) & 1) == 0 ? size * 0.5f : new Vector2(size.y, size.x) * 0.5f;
            return new PlanRectH8S(centre - half, centre + half, label);
        }

        /// <summary>
        /// Null when a footprint of the given height is clear of every rule, otherwise the label of the first thing it would
        /// intrude on: the wall margin, a blocked rectangle, a protected circle, a walk corridor or a line of sight.
        /// </summary>
        public static string Conflict(PlanRectH8S footprint, float height, GalleryRulesH8S rules)
        {
            if (rules == null) return "no placement rules";
            PlanRectH8S inner = rules.Room.Expanded(-WallMargin);
            if (footprint.Min.x < inner.Min.x - 1e-4f || footprint.Min.y < inner.Min.y - 1e-4f ||
                footprint.Max.x > inner.Max.x + 1e-4f || footprint.Max.y > inner.Max.y + 1e-4f) return "room wall margin";
            for (int i = 0; i < rules.Blocked.Count; i++)
                if (Overlaps(footprint, rules.Blocked[i])) return rules.Blocked[i].Label;
            for (int i = 0; i < rules.Circles.Count; i++)
                if (RectDistance(footprint, rules.Circles[i].Center) < rules.Circles[i].Radius) return rules.Circles[i].Label;
            for (int i = 0; i < rules.Paths.Count; i++)
                if (SegmentRectDistance(rules.Paths[i].A, rules.Paths[i].B, footprint) < rules.Paths[i].HalfWidth) return rules.Paths[i].Label;
            Vector3 boxMin = new Vector3(footprint.Min.x, rules.FloorY, footprint.Min.y);
            Vector3 boxMax = new Vector3(footprint.Max.x, rules.FloorY + Mathf.Max(0f, height), footprint.Max.y);
            for (int i = 0; i < rules.Sights.Count; i++)
            {
                SightLineH8S s = rules.Sights[i];
                Vector3 grow = new Vector3(s.Clearance, s.Clearance, s.Clearance);
                if (ForgeGalleryFitH8S.SegmentHitsBox(s.From, s.To, boxMin - grow, boxMax + grow)) return s.Label;
            }
            return null;
        }

        /// <summary>
        /// Ranked safe spots for a furnishing of the given unturned size (x width, y depth) and height: footprints backed onto
        /// a room wall within WallBand of the wall margin, facing into the room, clear of every rule. Best first: the
        /// smallest gap to the wall, then the nearest to the preferred point. The grid is deterministic, so the same rules
        /// always give the same order.
        /// </summary>
        public static List<GallerySpotH8S> Spots(Vector2 size, float height, GalleryRulesH8S rules, float step)
        {
            List<GallerySpotH8S> spots = new List<GallerySpotH8S>();
            if (rules == null || step < 0.05f || size.x <= 0f || size.y <= 0f) return spots;
            PlanRectH8S inner = rules.Room.Expanded(-WallMargin);
            for (int q = 0; q < 4; q++)
            {
                Vector2 half = (q & 1) == 0 ? size * 0.5f : new Vector2(size.y, size.x) * 0.5f;
                int nx = Mathf.FloorToInt((inner.Size.x - 2f * half.x) / step + 1e-4f);
                int nz = Mathf.FloorToInt((inner.Size.y - 2f * half.y) / step + 1e-4f);
                for (int ix = 0; ix <= nx; ix++)
                for (int iz = 0; iz <= nz; iz++)
                {
                    Vector2 c = new Vector2(inner.Min.x + half.x + ix * step, inner.Min.y + half.y + iz * step);
                    PlanRectH8S footprint = new PlanRectH8S(c - half, c + half, "");
                    float gap;
                    string wall;
                    BackGap(footprint, inner, q, out gap, out wall);
                    if (gap > WallBand) continue;
                    if (Conflict(footprint, height, rules) != null) continue;
                    float near = Vector2.Distance(c, rules.PreferNear);
                    spots.Add(new GallerySpotH8S { Center = c, QuarterTurns = q, Footprint = footprint, Score = gap * 4f + near * 0.3f, Wall = wall });
                }
            }
            spots.Sort(CompareSpots);
            return spots;
        }

        /// <summary>
        /// The pivot position that rests an item's rendered bounds (localMin to localMax at scale 1, relative to its pivot)
        /// on floorY, centred on a plan spot, after quarterTurns.
        /// </summary>
        public static Vector3 PivotFor(Vector2 centre, float floorY, Vector3 localMin, Vector3 localMax, int quarterTurns)
        {
            Vector3 min, max;
            ForgeGalleryFitH8S.RotateBounds(localMin, localMax, quarterTurns, out min, out max);
            return new Vector3(centre.x - (min.x + max.x) * 0.5f, floorY - min.y, centre.y - (min.z + max.z) * 0.5f);
        }

        /// <summary>The gap between a footprint's back and the wall it faces away from, for each quarter turn (front is local +z when unturned).</summary>
        private static void BackGap(PlanRectH8S footprint, PlanRectH8S inner, int quarterTurns, out float gap, out string wall)
        {
            switch (Turns(quarterTurns))
            {
                case 1: gap = footprint.Min.x - inner.Min.x; wall = "left wall"; break;
                case 2: gap = inner.Max.y - footprint.Max.y; wall = "back wall"; break;
                case 3: gap = inner.Max.x - footprint.Max.x; wall = "right wall"; break;
                default: gap = footprint.Min.y - inner.Min.y; wall = "door wall"; break;
            }
        }

        private static int Turns(int quarterTurns)
        {
            return ((quarterTurns % 4) + 4) % 4;
        }

        private static int CompareSpots(GallerySpotH8S a, GallerySpotH8S b)
        {
            int c = a.Score.CompareTo(b.Score);
            if (c != 0) return c;
            c = a.QuarterTurns.CompareTo(b.QuarterTurns);
            if (c != 0) return c;
            c = a.Center.x.CompareTo(b.Center.x);
            return c != 0 ? c : a.Center.y.CompareTo(b.Center.y);
        }
    }
}
