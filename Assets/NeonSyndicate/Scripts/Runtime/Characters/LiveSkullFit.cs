using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The measured skull of one live body (a crowd study or a drop-in), in its head frame: the Head bone's origin at the
    /// skull base, y up, z toward the face and x toward the person's right. The radial envelope table gives, per direction
    /// from the skull centre, the farthest skin vertex within a small cone, so a hair shell placed at that radius plus an
    /// offset sits on the scalp and never passes through the face, the ears or the brows. The landmarks place hairlines,
    /// caps, beards and visors on this particular head, and the clearance table (the coat, collar and armour below the
    /// ear line) keeps long hair and hoods outside the collar. Pure data and pure sampling; measured by LiveSkullMeasure.
    /// </summary>
    public sealed class LiveSkullFit
    {
        /// <summary>Polar rows from the crown (0) to straight down (180) and azimuth columns around the head (-180 to 180, 0 at the face, 90 at the right ear).</summary>
        public const int ThetaBins = 18, PhiBins = 36;
        /// <summary>Height bands of the clearance table, from the ear line down ClearDepth metres.</summary>
        public const int ClearBands = 15;
        public const float ClearDepth = 0.45f;
        /// <summary>The half angle of the cone a skin vertex must fall in to count for a direction.</summary>
        public const float ConeDegrees = 8f;
        /// <summary>Added to every envelope radius: the cone measure can sit a hair's breadth inside a sparsely sampled surface.</summary>
        public const float Margin = 0.0015f;

        /// <summary>The radial centre of the envelope, inside the cranium.</summary>
        public Vector3 Centre;
        /// <summary>The envelope: the farthest reach along each row and column direction of the skin vertices within a cone around it, plus Margin (ThetaBins x PhiBins).</summary>
        public float[] Radius = new float[ThetaBins * PhiBins];
        /// <summary>Per column and height band below the ear line, the largest horizontal distance from the centre axis of any coat, collar or armour vertex, never less than the skull itself (PhiBins x ClearBands).</summary>
        public float[] Clearance = new float[PhiBins * ClearBands];
        /// <summary>Per column, the highest coat, collar or armour vertex below the ear line (NoCollar where nothing covers the neck).</summary>
        public float[] CollarTop = new float[PhiBins];
        /// <summary>The CollarTop value of a column with no collar at all.</summary>
        public const float NoCollar = -10f;
        /// <summary>The top of the head.</summary>
        public float Crown;
        /// <summary>The top of the eyebrows and how far forward they sit.</summary>
        public float BrowTop, BrowZ;
        /// <summary>The height of the eyes.</summary>
        public float EyeY;
        /// <summary>The ears: top, bottom, how far out they reach and where they sit front to back.</summary>
        public float EarTop, EarBottom, EarX, EarZ;
        /// <summary>The bottom of the chin and the lowest skin at the back of the head (where the neck starts).</summary>
        public float Chin, Nape;
        /// <summary>The nose tip and the back of the skull.</summary>
        public float NoseZ, BackZ;
        /// <summary>True when the measurement produced a usable head; otherwise Reason says why.</summary>
        public bool Valid;
        public string Reason = "";

        /// <summary>A unit direction from a polar angle (degrees from +y) and an azimuth (degrees from +z toward +x).</summary>
        public static Vector3 Direction(float thetaDeg, float phiDeg)
        {
            float t = thetaDeg * Mathf.Deg2Rad, p = phiDeg * Mathf.Deg2Rad;
            float st = Mathf.Sin(t);
            return new Vector3(st * Mathf.Sin(p), Mathf.Cos(t), st * Mathf.Cos(p));
        }

        /// <summary>The polar angle and azimuth of a point seen from the centre (degrees), and its distance.</summary>
        public void Polar(Vector3 point, out float radius, out float thetaDeg, out float phiDeg)
        {
            Vector3 d = point - Centre;
            radius = d.magnitude;
            thetaDeg = radius > 1e-6f ? Mathf.Acos(Mathf.Clamp(d.y / radius, -1f, 1f)) * Mathf.Rad2Deg : 0f;
            phiDeg = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        private static float WrapPhi(float phiDeg)
        {
            phiDeg = Mathf.Repeat(phiDeg + 180f, 360f) - 180f;
            return phiDeg;
        }

        /// <summary>The envelope radius in a direction, interpolated between the table cells (columns wrap, rows clamp).</summary>
        public float RadiusAt(float thetaDeg, float phiDeg)
        {
            float rowF = Mathf.Clamp(thetaDeg, 0f, 180f) / 180f * ThetaBins - 0.5f;
            float colF = (WrapPhi(phiDeg) + 180f) / 360f * PhiBins - 0.5f;
            int r0 = Mathf.FloorToInt(rowF), c0 = Mathf.FloorToInt(colF);
            float rt = rowF - r0, ct = colF - c0;
            int r1 = r0 + 1;
            if (r0 < 0) { r0 = 0; rt = 0f; }
            if (r1 > ThetaBins - 1) r1 = ThetaBins - 1;
            if (r0 > ThetaBins - 1) r0 = ThetaBins - 1;
            int c1 = c0 + 1;
            c0 = ((c0 % PhiBins) + PhiBins) % PhiBins;
            c1 = ((c1 % PhiBins) + PhiBins) % PhiBins;
            float a = Mathf.Lerp(Radius[r0 * PhiBins + c0], Radius[r0 * PhiBins + c1], ct);
            float b = Mathf.Lerp(Radius[r1 * PhiBins + c0], Radius[r1 * PhiBins + c1], ct);
            return Mathf.Lerp(a, b, rt);
        }

        /// <summary>
        /// The envelope with the ears smoothed away: inside a band around each ear's azimuth the radius is interpolated
        /// between the skull on either side of the ear, so a closed helm or a hood follows the skull and not the ears.
        /// </summary>
        public float RadiusWithoutEars(float thetaDeg, float phiDeg)
        {
            float ear = Mathf.Clamp(Mathf.Atan2(EarX, EarZ - Centre.z) * Mathf.Rad2Deg, 80f, 130f);
            const float half = 24f;
            float r = RadiusAt(thetaDeg, phiDeg);
            if (thetaDeg < 55f || thetaDeg > 135f) return r;
            for (int side = -1; side <= 1; side += 2)
            {
                float d = Mathf.DeltaAngle(side * ear, phiDeg);
                if (Mathf.Abs(d) >= half) continue;
                float a = RadiusAt(thetaDeg, side * ear - half), b = RadiusAt(thetaDeg, side * ear + half);
                float smooth = Mathf.Lerp(a, b, (d + half) / (2f * half));
                return Mathf.Min(r, smooth + 0.002f);
            }
            return r;
        }

        /// <summary>A point on the envelope pushed out by offset metres along its direction.</summary>
        public Vector3 Surface(float thetaDeg, float phiDeg, float offset)
        {
            return Centre + Direction(thetaDeg, phiDeg) * (RadiusAt(thetaDeg, phiDeg) + offset);
        }

        /// <summary>The height of the envelope in a direction.</summary>
        public float HeightAt(float thetaDeg, float phiDeg)
        {
            return Centre.y + RadiusAt(thetaDeg, phiDeg) * Mathf.Cos(thetaDeg * Mathf.Deg2Rad);
        }

        /// <summary>The distance of the envelope from the centre's vertical axis in a direction.</summary>
        public float HorizontalRadius(float thetaDeg, float phiDeg)
        {
            return RadiusAt(thetaDeg, phiDeg) * Mathf.Sin(thetaDeg * Mathf.Deg2Rad);
        }

        /// <summary>
        /// The polar angle at which the envelope passes a height, walking down from the crown (0 when the height is above
        /// the crown, 180 when the skull never drops that low). Hairlines, cap lines and beard edges are heights, so this
        /// turns them into rows on this head.
        /// </summary>
        public float PolarAtHeight(float phiDeg, float y)
        {
            if (HeightAt(0f, phiDeg) <= y) return 0f;
            float previous = 0f;
            for (float theta = 2f; theta <= 180f; theta += 2f)
            {
                if (HeightAt(theta, phiDeg) <= y)
                {
                    float lo = previous, hi = theta;
                    for (int i = 0; i < 6; i++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        if (HeightAt(mid, phiDeg) <= y) hi = mid;
                        else lo = mid;
                    }
                    return (lo + hi) * 0.5f;
                }
                previous = theta;
            }
            return 180f;
        }

        /// <summary>
        /// The horizontal clearance from the centre axis at an azimuth and height below the ear line: the coat, collar and
        /// armour envelope (interpolated), so long hair, braids, hoods and neck guards hang outside the collar. Above the
        /// ear line it is the skull's own horizontal radius.
        /// </summary>
        public float ClearanceAt(float phiDeg, float y)
        {
            float depth = EarTop - y;
            if (depth <= 0f) return HorizontalRadius(PolarAtHeight(phiDeg, y), phiDeg);
            float bandF = Mathf.Clamp(depth / ClearDepth * ClearBands - 0.5f, 0f, ClearBands - 1f);
            int b0 = Mathf.FloorToInt(bandF);
            int b1 = Mathf.Min(b0 + 1, ClearBands - 1);
            float bt = bandF - b0;
            float colF = (WrapPhi(phiDeg) + 180f) / 360f * PhiBins - 0.5f;
            int c0 = Mathf.FloorToInt(colF);
            float ct = colF - c0;
            int c1 = ((c0 + 1) % PhiBins + PhiBins) % PhiBins;
            c0 = ((c0 % PhiBins) + PhiBins) % PhiBins;
            float a = Mathf.Lerp(Clearance[c0 * ClearBands + b0], Clearance[c1 * ClearBands + b0], ct);
            float b = Mathf.Lerp(Clearance[c0 * ClearBands + b1], Clearance[c1 * ClearBands + b1], ct);
            return Mathf.Lerp(a, b, bt);
        }

        /// <summary>The highest collar, coat or armour vertex at an azimuth (interpolated between columns), or NoCollar when the neck is bare there.</summary>
        public float CollarTopAt(float phiDeg)
        {
            float colF = (WrapPhi(phiDeg) + 180f) / 360f * PhiBins - 0.5f;
            int c0 = Mathf.FloorToInt(colF);
            float ct = colF - c0;
            int c1 = ((c0 + 1) % PhiBins + PhiBins) % PhiBins;
            c0 = ((c0 % PhiBins) + PhiBins) % PhiBins;
            float a = CollarTop[c0], b = CollarTop[c1];
            if (a <= NoCollar) return b;
            if (b <= NoCollar) return a;
            return Mathf.Lerp(a, b, ct);
        }

        /// <summary>The height of the head from the chin to the crown.</summary>
        public float FaceHeight { get { return Crown - Chin; } }
    }

    /// <summary>
    /// Measures a LiveSkullFit from points in the head frame: the skin vertices weighted to the Head bone at bind (the skull
    /// proper, the face, the ears and the jaw), the eyebrow and eye vertices, and every coat, collar and armour vertex below
    /// the ear line. Engine-free, so the EditMode tests run it on synthetic heads and the live adapter on real ones.
    /// </summary>
    public static class LiveSkullMeasure
    {
        /// <summary>The fewest skull vertices that make a measurable head.</summary>
        public const int MinSkullPoints = 60;

        public static LiveSkullFit Measure(IList<Vector3> skull, IList<Vector3> brows, IList<Vector3> eyes, IList<Vector3> cover)
        {
            return Measure(skull, brows, eyes, cover, 0f);
        }

        /// <summary>
        /// Measure with the coat, collar and armour points lowered by coverDrop metres first: the procedural neck is longer
        /// than a study's, so in play the head sits that much higher above a coat weighted to the chest than at bind.
        /// </summary>
        public static LiveSkullFit Measure(IList<Vector3> skull, IList<Vector3> brows, IList<Vector3> eyes, IList<Vector3> cover, float coverDrop)
        {
            if (cover != null && coverDrop != 0f && !float.IsNaN(coverDrop))
            {
                List<Vector3> dropped = new List<Vector3>(cover.Count);
                for (int i = 0; i < cover.Count; i++) dropped.Add(cover[i] - new Vector3(0f, coverDrop, 0f));
                cover = dropped;
            }
            LiveSkullFit fit = new LiveSkullFit();
            if (skull == null || skull.Count < MinSkullPoints)
            {
                fit.Reason = "too few skin vertices are weighted to the Head bone (" + (skull != null ? skull.Count : 0) + ").";
                return fit;
            }
            for (int i = 0; i < skull.Count; i++)
                if (!MathUtil.IsFinite(skull[i])) { fit.Reason = "a skull vertex is not finite."; return fit; }
            float crown = float.MinValue, earX = 0f, minY = float.MaxValue, noseZ = float.MinValue, backZ = float.MaxValue;
            for (int i = 0; i < skull.Count; i++)
            {
                Vector3 p = skull[i];
                if (p.y > crown) crown = p.y;
                if (p.y < minY) minY = p.y;
                if (Mathf.Abs(p.x) > earX) earX = Mathf.Abs(p.x);
                if (p.z > noseZ) noseZ = p.z;
                if (p.z < backZ) backZ = p.z;
            }
            if (crown - minY < 0.12f || crown - minY > 0.45f) { fit.Reason = "the head is " + (crown - minY).ToString("0.00") + " m tall, not a person's head in metres."; return fit; }
            if (noseZ - backZ < 0.1f || earX < 0.04f) { fit.Reason = "the head is too narrow or too flat to be a person's head in metres."; return fit; }
            fit.Crown = crown;
            fit.EarX = earX;
            fit.NoseZ = noseZ;
            fit.BackZ = backZ;
            // The ears: the outermost band of skin on either side.
            float earTop = float.MinValue, earBottom = float.MaxValue, earZ = 0f;
            int ears = 0;
            for (int i = 0; i < skull.Count; i++)
            {
                Vector3 p = skull[i];
                if (Mathf.Abs(p.x) < earX - 0.012f) continue;
                if (p.y > earTop) earTop = p.y;
                if (p.y < earBottom) earBottom = p.y;
                earZ += p.z;
                ears++;
            }
            fit.EarTop = earTop;
            fit.EarBottom = earBottom;
            fit.EarZ = ears > 0 ? earZ / ears : 0f;
            // The eyes and brows, with fallbacks from the ears when a body has neither.
            if (eyes != null && eyes.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < eyes.Count; i++) sum += eyes[i].y;
                fit.EyeY = sum / eyes.Count;
            }
            else fit.EyeY = earTop - 0.012f;
            if (brows != null && brows.Count > 0)
            {
                float top = float.MinValue, z = float.MinValue;
                for (int i = 0; i < brows.Count; i++)
                {
                    if (brows[i].y > top) top = brows[i].y;
                    if (brows[i].z > z) z = brows[i].z;
                }
                fit.BrowTop = top;
                fit.BrowZ = z;
            }
            else
            {
                fit.BrowTop = fit.EyeY + 0.026f;
                fit.BrowZ = noseZ - 0.035f;
            }
            // The centre: on the symmetry plane, between the crown and the eye line, at the cranium's mean depth.
            float zSum = 0f;
            int cranium = 0;
            for (int i = 0; i < skull.Count; i++)
                if (skull[i].y >= earTop) { zSum += skull[i].z; cranium++; }
            fit.Centre = new Vector3(0f, (crown + fit.EyeY) * 0.5f, cranium > 0 ? zSum / cranium : 0f);
            // Chin and nape: the lowest skin in front of and behind the centre.
            float chin = float.MaxValue, nape = float.MaxValue;
            for (int i = 0; i < skull.Count; i++)
            {
                Vector3 p = skull[i];
                if (p.z > fit.Centre.z + 0.04f && p.y < chin) chin = p.y;
                if (p.z < fit.Centre.z - 0.04f && p.y < nape) nape = p.y;
            }
            fit.Chin = chin < float.MaxValue ? chin : minY;
            fit.Nape = nape < float.MaxValue ? nape : minY;
            Envelope(fit, skull);
            ClearanceTable(fit, cover);
            fit.Valid = true;
            return fit;
        }

        /// <summary>
        /// The envelope per table cell: the farthest reach along the cell's direction (the projection onto it) of every
        /// skin vertex within the cone, plus the margin. The projection follows the real surface closely where the cone
        /// holds many vertices and never undershoots a protruding ear or brow; the cone widens for sparse directions and
        /// the rest of a column continues the last measured row.
        /// </summary>
        private static void Envelope(LiveSkullFit fit, IList<Vector3> skull)
        {
            int count = skull.Count;
            Vector3[] offsets = new Vector3[count];
            Vector3[] dirs = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = skull[i] - fit.Centre;
                float length = offsets[i].magnitude;
                dirs[i] = length > 1e-6f ? offsets[i] / length : Vector3.up;
            }
            float[] cones = { LiveSkullFit.ConeDegrees, LiveSkullFit.ConeDegrees * 1.75f, LiveSkullFit.ConeDegrees * 2.75f, LiveSkullFit.ConeDegrees * 4.5f };
            bool[] filled = new bool[fit.Radius.Length];
            for (int r = 0; r < LiveSkullFit.ThetaBins; r++)
            {
                float theta = (r + 0.5f) * 180f / LiveSkullFit.ThetaBins;
                for (int c = 0; c < LiveSkullFit.PhiBins; c++)
                {
                    float phi = (c + 0.5f) * 360f / LiveSkullFit.PhiBins - 180f;
                    Vector3 dir = LiveSkullFit.Direction(theta, phi);
                    for (int k = 0; k < cones.Length && !filled[r * LiveSkullFit.PhiBins + c]; k++)
                    {
                        float cosCone = Mathf.Cos(cones[k] * Mathf.Deg2Rad);
                        float best = 0f;
                        for (int i = 0; i < count; i++)
                        {
                            if (Vector3.Dot(dirs[i], dir) < cosCone) continue;
                            float reach = Vector3.Dot(offsets[i], dir);
                            if (reach > best) best = reach;
                        }
                        if (best > 0f)
                        {
                            fit.Radius[r * LiveSkullFit.PhiBins + c] = best + LiveSkullFit.Margin;
                            filled[r * LiveSkullFit.PhiBins + c] = true;
                        }
                    }
                }
            }
            // Directions with no skin at all (under the jaw, inside the neck opening) continue the last measured row above.
            for (int c = 0; c < LiveSkullFit.PhiBins; c++)
            {
                float last = 0f;
                for (int r = 0; r < LiveSkullFit.ThetaBins; r++)
                {
                    int i = r * LiveSkullFit.PhiBins + c;
                    if (filled[i]) last = fit.Radius[i];
                    else if (last > 0f) fit.Radius[i] = last;
                }
                last = 0f;
                for (int r = LiveSkullFit.ThetaBins - 1; r >= 0; r--)
                {
                    int i = r * LiveSkullFit.PhiBins + c;
                    if (fit.Radius[i] > 0f) last = fit.Radius[i];
                    else if (last > 0f) fit.Radius[i] = last;
                }
            }
        }

        /// <summary>The coat, collar and armour envelope per column and height band, never less than the skull's own horizontal radius there.</summary>
        private static void ClearanceTable(LiveSkullFit fit, IList<Vector3> cover)
        {
            for (int c = 0; c < LiveSkullFit.PhiBins; c++)
            {
                fit.CollarTop[c] = LiveSkullFit.NoCollar;
                float phi = (c + 0.5f) * 360f / LiveSkullFit.PhiBins - 180f;
                for (int b = 0; b < LiveSkullFit.ClearBands; b++)
                {
                    float y = fit.EarTop - (b + 0.5f) * LiveSkullFit.ClearDepth / LiveSkullFit.ClearBands;
                    float theta = fit.PolarAtHeight(phi, y);
                    fit.Clearance[c * LiveSkullFit.ClearBands + b] = theta < 180f ? fit.HorizontalRadius(theta, phi) : 0f;
                }
            }
            if (cover == null) return;
            float[] covered = new float[fit.Clearance.Length];
            for (int i = 0; i < cover.Count; i++)
            {
                Vector3 p = cover[i];
                if (!MathUtil.IsFinite(p) || p.y > fit.EarTop || p.y < fit.EarTop - LiveSkullFit.ClearDepth) continue;
                float dx = p.x - fit.Centre.x, dz = p.z - fit.Centre.z;
                float phi = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                int c = Mathf.Clamp(Mathf.FloorToInt((phi + 180f) / 360f * LiveSkullFit.PhiBins), 0, LiveSkullFit.PhiBins - 1);
                int b = Mathf.Clamp(Mathf.FloorToInt((fit.EarTop - p.y) / LiveSkullFit.ClearDepth * LiveSkullFit.ClearBands), 0, LiveSkullFit.ClearBands - 1);
                float h = Mathf.Sqrt(dx * dx + dz * dz);
                int k = c * LiveSkullFit.ClearBands + b;
                if (h > covered[k]) covered[k] = h;
                if (p.y > fit.CollarTop[c]) fit.CollarTop[c] = p.y;
            }
            // A band with no coat vertex of its own between two that have them takes the larger neighbour (a coarse mesh
            // leaves gaps); below the last covered band the coat fades a tenth per band, so hair never drops straight into it.
            for (int c = 0; c < LiveSkullFit.PhiBins; c++)
            {
                int last = -1;
                for (int b = 0; b < LiveSkullFit.ClearBands; b++)
                {
                    int k = c * LiveSkullFit.ClearBands + b;
                    if (covered[k] <= 0f) continue;
                    if (last >= 0)
                        for (int gap = last + 1; gap < b; gap++) covered[c * LiveSkullFit.ClearBands + gap] = Mathf.Max(covered[c * LiveSkullFit.ClearBands + last], covered[k]);
                    last = b;
                }
                if (last >= 0)
                    for (int b = last + 1; b < LiveSkullFit.ClearBands; b++) covered[c * LiveSkullFit.ClearBands + b] = covered[c * LiveSkullFit.ClearBands + b - 1] * 0.9f;
                for (int b = 0; b < LiveSkullFit.ClearBands; b++)
                {
                    int k = c * LiveSkullFit.ClearBands + b;
                    if (covered[k] > fit.Clearance[k]) fit.Clearance[k] = covered[k];
                }
            }
        }
    }
}
