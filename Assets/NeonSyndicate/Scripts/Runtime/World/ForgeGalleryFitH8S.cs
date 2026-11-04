using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The outcome of fitting a decorative visual inside an existing box collider, in the collider's local frame.</summary>
    public struct ForgeFitH8S
    {
        /// <summary>True when a scale and pivot keep every rendered corner inside the box with the margin.</summary>
        public bool Ok;
        /// <summary>Uniform scale applied to the visual (never above its native scale).</summary>
        public float Scale;
        /// <summary>Quarter turns (0 to 3) about the box's up axis, applied before scaling.</summary>
        public int QuarterTurns;
        /// <summary>Where the visual's pivot goes, in the box's local frame.</summary>
        public Vector3 Pivot;
        /// <summary>Lower corner of the fitted rendered bounds, in the box's local frame.</summary>
        public Vector3 Min;
        /// <summary>Upper corner of the fitted rendered bounds, in the box's local frame.</summary>
        public Vector3 Max;
        /// <summary>Why the fit was refused, or which limit set the scale when it succeeded.</summary>
        public string Reason;
    }

    /// <summary>
    /// Engine-free maths for the H8S Forge furnishing review: exact quarter-turn rotation of points and bounds, inside-box
    /// and clearance tests, the fit of a visual's rendered bounds inside an existing box collider while it rests on a
    /// support, box overlap depth, and 3D segment versus box clipping for lines of sight. The delivered prop sizes are
    /// recorded here so the review can verify the native import scale before it trusts any bounds.
    /// </summary>
    public static class ForgeGalleryFitH8S
    {
        /// <summary>Clearance every fitted corner keeps from each face of the original Forge collider, in metres.</summary>
        public const float BoxMargin = 0.02f;
        /// <summary>Below this scale the anvil would read as a toy, so the fit is refused instead.</summary>
        public const float MinAnvilScale = 0.5f;
        /// <summary>The anvil is never enlarged beyond its native modelled size.</summary>
        public const float MaxAnvilScale = 1f;
        /// <summary>How far the anvil's pivot may slide from the centre of its support block to fit, in metres.</summary>
        public const float MaxSlide = 0.12f;
        /// <summary>Two boxes overlapping by no more than this are treated as touching (the anvil resting on its block).</summary>
        public const float TouchTolerance = 0.002f;
        /// <summary>Relative tolerance when comparing a native imported size with the delivered source size.</summary>
        public const float NativeSizeTolerance = 0.03f;

        /// <summary>
        /// The delivered source size of a prop in Unity axes (x, y up, z) at scale 1, measured from the exported FBX meshes
        /// with the project's Blender 5.2 module (Blender x, z, y). Zero for an unknown prop.
        /// </summary>
        public static Vector3 SourceSize(string prop)
        {
            switch (prop)
            {
                case "Anvil": return new Vector3(1.0816f, 0.5560f, 0.4016f);
                case "Workbench": return new Vector3(2.0191f, 0.8946f, 1.0242f);
                case "BookStand": return new Vector3(0.5071f, 1.4468f, 0.5112f);
                default: return Vector3.zero;
            }
        }

        /// <summary>True when a measured native size matches the source size within the relative tolerance (x and z may swap).</summary>
        public static bool NativeSizeMatches(Vector3 measured, Vector3 source, float tolerance)
        {
            if (source.x <= 0f || source.y <= 0f || source.z <= 0f) return false;
            if (!Near(measured.y, source.y, tolerance)) return false;
            return (Near(measured.x, source.x, tolerance) && Near(measured.z, source.z, tolerance)) ||
                (Near(measured.x, source.z, tolerance) && Near(measured.z, source.x, tolerance));
        }

        /// <summary>Rotates a point by quarterTurns times 90 degrees about +Y, exactly as QMath.AY(90 * quarterTurns) does.</summary>
        public static Vector3 RotateY(Vector3 p, int quarterTurns)
        {
            switch (((quarterTurns % 4) + 4) % 4)
            {
                case 1: return new Vector3(p.z, p.y, -p.x);
                case 2: return new Vector3(-p.x, p.y, -p.z);
                case 3: return new Vector3(-p.z, p.y, p.x);
                default: return p;
            }
        }

        /// <summary>The exact bounds of an axis-aligned box after a quarter-turn rotation about +Y (no growth).</summary>
        public static void RotateBounds(Vector3 min, Vector3 max, int quarterTurns, out Vector3 rotatedMin, out Vector3 rotatedMax)
        {
            Vector3 a = RotateY(min, quarterTurns);
            Vector3 b = RotateY(max, quarterTurns);
            rotatedMin = new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
            rotatedMax = new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
        }

        /// <summary>Writes the eight corners of a box into corners, which must hold at least eight entries.</summary>
        public static void Corners(Vector3 min, Vector3 max, Vector3[] corners)
        {
            for (int i = 0; i < 8; i++)
                corners[i] = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
        }

        /// <summary>True when the point lies inside the box (centre and size) shrunk by margin on every face.</summary>
        public static bool InsideBox(Vector3 p, Vector3 center, Vector3 size, float margin)
        {
            return Mathf.Abs(p.x - center.x) <= size.x * 0.5f - margin && Mathf.Abs(p.y - center.y) <= size.y * 0.5f - margin &&
                Mathf.Abs(p.z - center.z) <= size.z * 0.5f - margin;
        }

        /// <summary>Index of the first point outside the box shrunk by margin, or -1 when every point is inside.</summary>
        public static int FirstOutside(IList<Vector3> points, Vector3 center, Vector3 size, float margin)
        {
            if (points == null) return 0;
            for (int i = 0; i < points.Count; i++)
                if (!InsideBox(points[i], center, size, margin)) return i;
            return -1;
        }

        /// <summary>The smallest distance from any point to the nearest box face (negative when a point is outside the box).</summary>
        public static float Clearance(IList<Vector3> points, Vector3 center, Vector3 size)
        {
            float best = float.MaxValue;
            if (points == null) return best;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                best = Mathf.Min(best, size.x * 0.5f - Mathf.Abs(p.x - center.x));
                best = Mathf.Min(best, size.y * 0.5f - Mathf.Abs(p.y - center.y));
                best = Mathf.Min(best, size.z * 0.5f - Mathf.Abs(p.z - center.z));
            }
            return best;
        }

        /// <summary>
        /// Fits a visual inside a box collider while it rests on a support. localMin and localMax are the visual's rendered
        /// bounds at scale 1 relative to its pivot, in the box's local axes. The result is the largest uniform scale up to
        /// maxScale (and at least minScale) at which the bounds, turned by a quarter turn, resting exactly on supportTop with
        /// the pivot within maxSlide of supportCenter (local x and z), keep margin from every face of the box. Ties prefer
        /// the turn whose long side runs along the box's long axis, then the longer side towards +x when longSidePositiveX
        /// (where the original anvil horn points), then the fewest turns.
        /// </summary>
        public static ForgeFitH8S Fit(Vector3 localMin, Vector3 localMax, Vector3 boxCenter, Vector3 boxSize, float supportTop, Vector2 supportCenter,
            float margin, float minScale, float maxScale, float maxSlide, bool longSidePositiveX)
        {
            ForgeFitH8S best = new ForgeFitH8S { Ok = false };
            if (!Finite(localMin) || !Finite(localMax) || !Finite(boxCenter) || !Finite(boxSize) || !(minScale > 0f) || maxScale < minScale)
            {
                best.Reason = "The fit inputs are not finite or the scale range is empty.";
                return best;
            }
            if (localMax.x - localMin.x <= 1e-5f || localMax.y - localMin.y <= 1e-5f || localMax.z - localMin.z <= 1e-5f)
            {
                best.Reason = "The visual has no rendered volume.";
                return best;
            }
            Vector3 boxMin = boxCenter - boxSize * 0.5f;
            Vector3 boxMax = boxCenter + boxSize * 0.5f;
            if (supportTop < boxMin.y + margin || supportTop > boxMax.y - margin)
            {
                best.Reason = "The support top (" + supportTop.ToString("0.000") + ") is not inside the collider with the margin.";
                return best;
            }
            int bestPreference = -1;
            string refusal = null;
            for (int q = 0; q < 4; q++)
            {
                Vector3 m, M;
                RotateBounds(localMin, localMax, q, out m, out M);
                Vector3 pivot;
                float scale;
                if (Feasible(maxScale, m, M, boxMin, boxMax, supportTop, supportCenter, margin, maxSlide, out pivot)) scale = maxScale;
                else if (!Feasible(minScale, m, M, boxMin, boxMax, supportTop, supportCenter, margin, maxSlide, out pivot))
                {
                    if (refusal == null) refusal = Limits("At the minimum scale " + minScale.ToString("0.00") + " the visual cannot fit", m, M, boxMin, boxMax, supportTop, margin);
                    continue;
                }
                else
                {
                    float lo = minScale, hi = maxScale;
                    for (int i = 0; i < 40; i++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        Vector3 unused;
                        if (Feasible(mid, m, M, boxMin, boxMax, supportTop, supportCenter, margin, maxSlide, out unused)) lo = mid;
                        else hi = mid;
                    }
                    // A clean four-decimal scale that is still feasible (rounding down only shrinks the bounds).
                    scale = Mathf.Max(minScale, Mathf.Floor(lo * 10000f) / 10000f);
                    if (!Feasible(scale, m, M, boxMin, boxMax, supportTop, supportCenter, margin, maxSlide, out pivot))
                    {
                        scale = lo;
                        Feasible(scale, m, M, boxMin, boxMax, supportTop, supportCenter, margin, maxSlide, out pivot);
                    }
                }
                int preference = 0;
                if ((boxSize.x >= boxSize.z) == (M.x - m.x >= M.z - m.z)) preference += 2;
                if (!longSidePositiveX || M.x >= -m.x) preference += 1;
                bool better = !best.Ok || scale > best.Scale + 1e-4f || (Mathf.Abs(scale - best.Scale) <= 1e-4f && preference > bestPreference);
                if (!better) continue;
                best = new ForgeFitH8S { Ok = true, Scale = scale, QuarterTurns = q, Pivot = pivot, Min = pivot + m * scale, Max = pivot + M * scale };
                best.Reason = Limits("Fitted", m, M, boxMin, boxMax, supportTop, margin);
                bestPreference = preference;
            }
            if (!best.Ok) best.Reason = refusal ?? "No quarter turn keeps the visual inside the collider.";
            return best;
        }

        /// <summary>How deep two boxes overlap along their least-overlapping axis (zero when touching, negative when apart).</summary>
        public static float OverlapDepth(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            float x = Mathf.Min(maxA.x, maxB.x) - Mathf.Max(minA.x, minB.x);
            float y = Mathf.Min(maxA.y, maxB.y) - Mathf.Max(minA.y, minB.y);
            float z = Mathf.Min(maxA.z, maxB.z) - Mathf.Max(minA.z, minB.z);
            return Mathf.Min(x, Mathf.Min(y, z));
        }

        /// <summary>True when the 3D segment from a to b touches or passes through the box (slab clipping).</summary>
        public static bool SegmentHitsBox(Vector3 a, Vector3 b, Vector3 min, Vector3 max)
        {
            float t0 = 0f, t1 = 1f;
            Vector3 d = b - a;
            return Clip(-d.x, a.x - min.x, ref t0, ref t1) && Clip(d.x, max.x - a.x, ref t0, ref t1)
                && Clip(-d.y, a.y - min.y, ref t0, ref t1) && Clip(d.y, max.y - a.y, ref t0, ref t1)
                && Clip(-d.z, a.z - min.z, ref t0, ref t1) && Clip(d.z, max.z - a.z, ref t0, ref t1);
        }

        /// <summary>One Liang-Barsky clipping step: narrows the parameter range [t0, t1] against one slab face; false when empty.</summary>
        public static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-9f) return q >= 0f;
            float t = q / p;
            if (p < 0f)
            {
                if (t > t1) return false;
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t0) return false;
                if (t < t1) t1 = t;
            }
            return true;
        }

        private static bool Feasible(float s, Vector3 m, Vector3 M, Vector3 boxMin, Vector3 boxMax, float supportTop, Vector2 supportCenter, float margin, float slide, out Vector3 pivot)
        {
            pivot = Vector3.zero;
            if (supportTop + s * (M.y - m.y) > boxMax.y - margin) return false;
            float px, pz;
            if (!Axis(s, m.x, M.x, boxMin.x, boxMax.x, supportCenter.x, margin, slide, out px)) return false;
            if (!Axis(s, m.z, M.z, boxMin.z, boxMax.z, supportCenter.y, margin, slide, out pz)) return false;
            pivot = new Vector3(px, supportTop - s * m.y, pz);
            return true;
        }

        private static bool Axis(float s, float lo, float hi, float boxLo, float boxHi, float centre, float margin, float slide, out float at)
        {
            float from = Mathf.Max(boxLo + margin - s * lo, centre - slide);
            float to = Mathf.Min(boxHi - margin - s * hi, centre + slide);
            at = Mathf.Clamp(centre, from, Mathf.Max(from, to));
            return from <= to + 1e-6f;
        }

        private static string Limits(string lead, Vector3 m, Vector3 M, Vector3 boxMin, Vector3 boxMax, float supportTop, float margin)
        {
            float height = (boxMax.y - margin - supportTop) / Mathf.Max(1e-6f, M.y - m.y);
            float length = (boxMax.x - boxMin.x - 2f * margin) / Mathf.Max(1e-6f, M.x - m.x);
            float depth = (boxMax.z - boxMin.z - 2f * margin) / Mathf.Max(1e-6f, M.z - m.z);
            string binding = height <= length && height <= depth ? "height above the support" : length <= depth ? "length along local x" : "depth along local z";
            return lead + "; scale limits: height " + height.ToString("0.0000") + ", length " + length.ToString("0.0000") + ", depth " + depth.ToString("0.0000") + " (binding: " + binding + ").";
        }

        private static bool Near(float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) <= Mathf.Abs(b) * tolerance + 0.005f;
        }

        private static bool Finite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }
    }
}
