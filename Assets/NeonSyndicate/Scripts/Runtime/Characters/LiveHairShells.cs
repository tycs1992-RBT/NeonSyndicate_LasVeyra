using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds hair, beards and headwear for a live body as rigid meshes in the head frame, fitted to a measured skull
    /// (LiveSkullFit). Hair is a solid base cap plus layered strand shells that follow the skull envelope at growing
    /// offsets, with the strand texture tiled around the head (u) and running from the roots to the tips (v); long hair
    /// adds a drape down the back and two front locks that hang outside the collar; braids, a bun and a crest are built
    /// from the same envelope. Beards cover the jaw and chin without the mouth. Headpieces are shells, bands, peaks and
    /// brims on the same envelope, so a cap sits on this head and not on an average one. Engine-free: it fills a
    /// MeshBuilder, the caller turns that into a Mesh.
    /// </summary>
    public static class LiveHairShells
    {
        /// <summary>The hair mesh: an opaque base cap, the strand shells, the tip (accent) strands and the shaved areas (stubble dots on the skin).</summary>
        public const int HairSubmeshes = 4, SubBase = 0, SubStrands = 1, SubAccent = 2, SubShaved = 3;
        /// <summary>The beard mesh: stubble flat on the skin, or a short beard with a little volume.</summary>
        public const int BeardSubmeshes = 2, SubStubble = 0, SubShortBeard = 1;
        /// <summary>The headwear mesh: cloth or leather, metal, and the trim (bands, badges, lenses, stones).</summary>
        public const int HeadSubmeshes = 3, SubCloth = 0, SubMetal = 1, SubTrim = 2;
        /// <summary>How often the strand texture repeats around the head.</summary>
        public const float StrandTiles = 8f;
        /// <summary>The gap a strand layer keeps above the one below it.</summary>
        public const float LayerStep = 0.0045f;
        /// <summary>How far below the ear line the long drape and the braids reach.</summary>
        public const float DrapeLength = 0.30f, BraidLength = 0.26f, LockLength = 0.17f;
        /// <summary>The room long hair, braids and hoods keep outside the collar and coat.</summary>
        public const float CollarRoom = 0.010f;

        private const int ShellRows = 10, ShellCols = 32;

        // ================================================================== lines on the head

        /// <summary>The azimuth of the ears seen from the skull centre (about 105 degrees: the ears sit behind the centre).</summary>
        public static float EarAzimuth(LiveSkullFit fit)
        {
            return Mathf.Clamp(Mathf.Atan2(fit.EarX, fit.EarZ - fit.Centre.z) * Mathf.Rad2Deg, 80f, 130f);
        }

        /// <summary>
        /// The height of the hairline at an azimuth: the forehead above the brows, down past the temples to just above
        /// the ears (short styles) or below them (full styles), then down to the nape.
        /// </summary>
        public static float Hairline(LiveSkullFit fit, float phi, bool full)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
            float ear = EarAzimuth(fit);
            float[] keys = { 0f, ear - 45f, ear - 16f, ear - 6f, ear + 6f, ear + 24f, 155f, 180f };
            float[] values = full
                ? new[] { fit.BrowTop + 0.018f, fit.BrowTop + 0.008f, fit.EarTop + 0.008f, fit.EarBottom - 0.025f, fit.EarBottom - 0.025f, fit.EarBottom - 0.028f, fit.Nape - 0.010f, fit.Nape - 0.012f }
                : new[] { fit.BrowTop + 0.021f, fit.BrowTop + 0.010f, fit.EarTop + 0.010f, fit.EarTop + 0.005f, fit.EarTop + 0.005f, fit.EarTop - 0.004f, fit.Nape + 0.014f, fit.Nape + 0.012f };
            float y = Interp(d, keys, values);
            // A shell never dips into a stand collar (the hair is tucked in); long hair's drape takes over outside it.
            if (d > ear - 6f) y = Mathf.Max(y, CollarClear(fit, phi));
            return y;
        }

        /// <summary>The lowest height a shell may reach at an azimuth without entering the collar: a little above the collar's top, or anything when the neck is bare.</summary>
        public static float CollarClear(LiveSkullFit fit, float phi)
        {
            float top = fit.CollarTopAt(phi);
            return top <= LiveSkullFit.NoCollar ? float.MinValue : top + 0.01f;
        }

        /// <summary>A tight hairline (buns, braids, the undercut's shaved area): just above the ears and down to the nape.</summary>
        public static float TightHairline(LiveSkullFit fit, float phi)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
            float ear = EarAzimuth(fit);
            float[] keys = { 0f, ear - 45f, ear - 16f, ear - 6f, ear + 6f, ear + 24f, 155f, 180f };
            float[] values = { fit.BrowTop + 0.024f, fit.BrowTop + 0.012f, fit.EarTop + 0.010f, fit.EarTop + 0.004f, fit.EarTop + 0.004f, fit.EarTop - 0.006f, fit.Nape + 0.018f, fit.Nape + 0.016f };
            float y = Interp(d, keys, values);
            if (d > ear - 6f) y = Mathf.Max(y, CollarClear(fit, phi));
            return y;
        }

        /// <summary>
        /// The height of a headpiece's lower edge at an azimuth (the hair below it shows), or float.NaN for a piece that
        /// covers nothing or everything.
        /// </summary>
        public static float CoverLine(LiveSkullFit fit, LiveHeadpiece piece, float phi)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
            switch (piece)
            {
                case LiveHeadpiece.Cap:
                case LiveHeadpiece.OfficerCap:
                    return Interp(d, new[] { 0f, 60f, 100f, 180f }, new[] { fit.BrowTop + 0.022f, fit.EarTop + 0.028f, fit.EarTop + 0.026f, fit.EarTop + 0.02f });
                case LiveHeadpiece.WorkHelm:
                    return Interp(d, new[] { 0f, 60f, 100f, 180f }, new[] { fit.BrowTop + 0.02f, fit.EarTop + 0.016f, fit.EarTop + 0.012f, fit.Nape + 0.04f });
                case LiveHeadpiece.BrimHat:
                    return Interp(d, new[] { 0f, 60f, 110f, 180f }, new[] { fit.BrowTop + 0.02f, fit.EarTop + 0.024f, fit.EarTop + 0.022f, fit.EarTop + 0.016f });
                case LiveHeadpiece.Beanie:
                    return Interp(d, new[] { 0f, 55f, 95f, 125f, 180f }, new[] { fit.BrowTop + 0.016f, fit.BrowTop + 0.004f, fit.EarTop - 0.016f, fit.EarTop - 0.03f, fit.Nape + 0.045f });
                case LiveHeadpiece.Bandana:
                    return Interp(d, new[] { 0f, 55f, 100f, 140f, 180f }, new[] { fit.BrowTop + 0.02f, fit.BrowTop + 0.006f, fit.EarTop + 0.006f, fit.EarTop - 0.012f, fit.Nape + 0.035f });
                default:
                    return float.NaN;
            }
        }

        private static float Interp(float x, float[] keys, float[] values)
        {
            if (x <= keys[0]) return values[0];
            for (int i = 1; i < keys.Length; i++)
                if (x <= keys[i])
                {
                    float span = keys[i] - keys[i - 1];
                    return span > 1e-5f ? Mathf.Lerp(values[i - 1], values[i], (x - keys[i - 1]) / span) : values[i];
                }
            return values[values.Length - 1];
        }

        // ================================================================== hair

        /// <summary>
        /// The hair of a style on this skull, under a headpiece (which clips the shells to the hair below its edge, or
        /// hides them). Returns false when nothing is drawn (bald, or everything under a helm or hood).
        /// </summary>
        public static bool BuildHair(MeshBuilder b, LiveSkullFit fit, HairStyle style, LiveHeadpiece under)
        {
            if (b == null || fit == null || !fit.Valid || style == HairStyle.Bald) return false;
            LiveHairCover cover = LiveHairRules.Cover(under);
            if (cover == LiveHairCover.Hidden) return false;
            int before = b.VertexCount;
            bool capped = cover == LiveHairCover.Capped;
            Func<float, float> start = capped ? (Func<float, float>)(phi => fit.PolarAtHeight(phi, CoverLine(fit, under, phi)) + 3f) : (phi => 0f);
            // Under a cap the hair is pressed flat at the cap's edge and fills out below it.
            Func<float, float> press = capped ? (Func<float, float>)(v => Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(v / 0.35f))) : (v => 1f);
            switch (style)
            {
                case HairStyle.Short:
                    Volume(b, fit, start, phi => fit.PolarAtHeight(phi, Hairline(fit, phi, false)), (v, phi) => (0.003f + 0.009f * (1f - Mathf.Pow(v, 1.5f))) * press(v), 3, true);
                    break;
                case HairStyle.Long:
                    Volume(b, fit, start, phi => fit.PolarAtHeight(phi, Hairline(fit, phi, true)), (v, phi) => (0.004f + 0.011f * (1f - Mathf.Pow(v, 1.5f))) * press(v), 3, true);
                    Drape(b, fit);
                    FrontLocks(b, fit);
                    break;
                case HairStyle.Undercut:
                {
                    Stubble(b, fit, start, phi => fit.PolarAtHeight(phi, Hairline(fit, phi, false)));
                    Func<float, float> topEdge = phi => fit.PolarAtHeight(phi, fit.EarTop + 0.046f + 0.008f * Mathf.Cos(phi * Mathf.Deg2Rad));
                    Volume(b, fit, start, topEdge, (v, phi) =>
                    {
                        float front = Mathf.Clamp01(Mathf.Cos(phi * Mathf.Deg2Rad));
                        float swoop = 0.012f * front * Mathf.Sin(v * Mathf.PI);
                        return (0.005f + 0.02f * (1f - v * v) * (0.6f + 0.4f * front) + swoop) * press(v);
                    }, 3, true);
                    break;
                }
                case HairStyle.Mohawk:
                    Stubble(b, fit, start, phi => fit.PolarAtHeight(phi, Hairline(fit, phi, false)));
                    Crest(b, fit, cover == LiveHairCover.Capped ? under : LiveHeadpiece.None);
                    break;
                case HairStyle.Bun:
                    Volume(b, fit, start, phi => fit.PolarAtHeight(phi, TightHairline(fit, phi)), (v, phi) => (0.003f + 0.006f * (1f - v)) * press(v), 1, false);
                    if (cover == LiveHairCover.None) Bun(b, fit);
                    break;
                case HairStyle.Braids:
                    Volume(b, fit, start, phi => fit.PolarAtHeight(phi, TightHairline(fit, phi)), (v, phi) => (0.003f + 0.006f * (1f - v)) * press(v), 1, false);
                    Braids(b, fit);
                    break;
            }
            return b.VertexCount > before;
        }

        /// <summary>A base cap plus strand layers over a region of the skull, with an accent patch of tips on the outermost layer.</summary>
        private static void Volume(MeshBuilder b, LiveSkullFit fit, Func<float, float> start, Func<float, float> edge, Func<float, float, float> offset, int layers, bool accent)
        {
            if (!HasRows(start, edge)) return;
            ShellPatch(b, SubBase, fit, start, edge, offset, ShellRows, ShellCols, StrandTiles, 0f, 1f, 0f, false);
            bool capped = start(0f) > 0f || start(180f) > 0f;
            Func<float, float> step = capped ? (Func<float, float>)(v => Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(v / 0.35f))) : (v => 1f);
            for (int layer = 1; layer <= layers; layer++)
            {
                int k = layer;
                ShellPatch(b, SubStrands, fit, start, edge, (v, phi) => offset(v, phi) + LayerStep * k * step(v), ShellRows, ShellCols, StrandTiles, 0.12f * k, 1f, 0.37f * k, true);
            }
            if (!accent) return;
            Func<float, float> tipStart = phi => Mathf.Lerp(start(phi), edge(phi), 0.6f);
            ShellPatch(b, SubAccent, fit, tipStart, edge, (v, phi) => offset(Mathf.Lerp(0.6f, 1f, v), phi) + LayerStep * (layers + 1), 5, ShellCols, StrandTiles, 0.55f, 1f, 0.71f, true);
        }

        /// <summary>A shaved area: stubble dots flat on the skin, finely tiled, so the scalp shows through a close buzz.</summary>
        private static void Stubble(MeshBuilder b, LiveSkullFit fit, Func<float, float> start, Func<float, float> edge)
        {
            if (!HasRows(start, edge)) return;
            ShellPatch(b, SubShaved, fit, start, edge, (v, phi) => 0.0012f, 8, ShellCols, StrandTiles * 1.5f, 0f, 1.5f, 0f, false);
        }

        private static bool HasRows(Func<float, float> start, Func<float, float> edge)
        {
            for (int j = 0; j < ShellCols; j++)
            {
                float phi = -180f + 360f * j / ShellCols;
                if (edge(phi) - start(phi) > 3f) return true;
            }
            return false;
        }

        /// <summary>Long hair down the back: two strand curtains and a tip layer, hugging the skull at the nape and flaring outside the collar.</summary>
        private static void Drape(MeshBuilder b, LiveSkullFit fit)
        {
            float top = Mathf.Max(fit.Nape + 0.02f, Mathf.Max(CollarClear(fit, 150f), Mathf.Max(CollarClear(fit, 180f), CollarClear(fit, 210f))) + 0.012f), bottom = fit.Nape - DrapeLength;
            Func<float, float, float> radius = (phi, y) => Mathf.Max(fit.HorizontalRadius(fit.PolarAtHeight(phi, Mathf.Max(y, fit.Nape + 0.005f)), phi) + 0.012f, fit.ClearanceAt(phi, y) + CollarRoom);
            // The hem is rounded: the outer columns end higher than the middle.
            Func<float, float> hem = phi => { float e = Mathf.Abs(Mathf.DeltaAngle(180f, phi)) / 68f; return bottom + DrapeLength * 0.3f * e * e; };
            Curtain(b, SubStrands, fit, 112f, 248f, phi => top, hem, radius, 10, 14, 4f, 0f, 1f, 0f);
            Curtain(b, SubStrands, fit, 112f, 248f, phi => top - 0.01f, phi => hem(phi) + 0.02f, (phi, y) => radius(phi, y) + LayerStep, 10, 14, 4f, 0.1f, 1f, 0.41f);
            Curtain(b, SubAccent, fit, 112f, 248f, phi => Mathf.Lerp(top, hem(phi), 0.55f), phi => hem(phi) - 0.01f, (phi, y) => radius(phi, y) + LayerStep * 2f, 5, 14, 4f, 0.55f, 1f, 0.77f);
        }

        /// <summary>Two locks in front of the shoulders, from the jaw line down over the coat front.</summary>
        private static void FrontLocks(MeshBuilder b, LiveSkullFit fit)
        {
            float ear = EarAzimuth(fit);
            for (int side = -1; side <= 1; side += 2)
            {
                float from = side * (ear - 62f), to = side * (ear - 30f);
                float top = fit.EarBottom - 0.015f, bottom = fit.Chin - LockLength;
                Func<float, float, float> radius = (phi, y) => Mathf.Max(fit.HorizontalRadius(fit.PolarAtHeight(phi, Mathf.Max(y, fit.Chin + 0.01f)), phi) + 0.012f, fit.ClearanceAt(phi, y) + CollarRoom);
                Curtain(b, SubStrands, fit, Mathf.Min(from, to), Mathf.Max(from, to), phi => top, phi => bottom, radius, 8, 4, 1f, 0f, 1f, side > 0 ? 0.2f : 0.6f);
                Curtain(b, SubAccent, fit, Mathf.Min(from, to), Mathf.Max(from, to), phi => Mathf.Lerp(top, bottom, 0.6f), phi => bottom - 0.01f, (phi, y) => radius(phi, y) + LayerStep, 4, 4, 1f, 0.55f, 1f, 0.3f);
            }
        }

        /// <summary>A bun at the back of the crown, its inner side pressed onto the hair instead of reaching into the skull.</summary>
        private static void Bun(MeshBuilder b, LiveSkullFit fit)
        {
            Vector3 centre = fit.Surface(52f, 180f, 0.03f);
            Ellipsoid(b, SubStrands, centre, new Vector3(0.042f, 0.037f, 0.036f), 7, 12, fit, 0.004f);
        }

        /// <summary>Three braids from the nape down the back, each outside the collar.</summary>
        private static void Braids(MeshBuilder b, LiveSkullFit fit)
        {
            float[] azimuths = { 150f, 180f, 210f };
            for (int k = 0; k < azimuths.Length; k++)
            {
                float phi = azimuths[k];
                int rings = 9;
                Vector3[] path = new Vector3[rings];
                float[] radii = new float[rings];
                float topY = fit.Nape + 0.03f;
                for (int i = 0; i < rings; i++)
                {
                    float t = (float)i / (rings - 1);
                    float y = Mathf.Lerp(topY, fit.Nape - BraidLength, t);
                    float sway = Mathf.Sin(t * Mathf.PI * 1.5f + k) * 4f;
                    float a = phi + sway;
                    float r = Mathf.Max(fit.HorizontalRadius(fit.PolarAtHeight(a, Mathf.Max(y, fit.Nape + 0.005f)), a) + 0.013f, fit.ClearanceAt(a, y) + CollarRoom + 0.004f);
                    path[i] = new Vector3(fit.Centre.x + Mathf.Sin(a * Mathf.Deg2Rad) * r, y, fit.Centre.z + Mathf.Cos(a * Mathf.Deg2Rad) * r);
                    radii[i] = Mathf.Lerp(0.012f, 0.004f, t * t);
                }
                Tube(b, SubStrands, path, radii, 6, 2.5f, 0.3f * k);
            }
        }

        /// <summary>The crest of a mohawk: two strand fins standing along the middle of the head, over a stubble shell. Under a cap only the part below the cap line is built.</summary>
        private static void Crest(MeshBuilder b, LiveSkullFit fit, LiveHeadpiece under)
        {
            const int steps = 16;
            float front = fit.PolarAtHeight(0f, fit.BrowTop + 0.03f);
            float back = fit.PolarAtHeight(180f, fit.Nape + 0.015f);
            Vector3[] baseLine = new Vector3[steps + 1], ups = new Vector3[steps + 1];
            float[] heights = new float[steps + 1];
            bool[] keep = new bool[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float s = (float)i / steps;
                float theta = s < 0.5f ? Mathf.Lerp(front, 0f, s * 2f) : Mathf.Lerp(0f, back, (s - 0.5f) * 2f);
                float phi = s < 0.5f ? 0f : 180f;
                baseLine[i] = fit.Surface(theta, phi, 0.002f);
                ups[i] = LiveSkullFit.Direction(theta, phi);
                heights[i] = 0.012f + 0.05f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(s * Mathf.PI)), 0.6f);
                float line = CoverLine(fit, under, phi);
                keep[i] = float.IsNaN(line) || baseLine[i].y < line - 0.006f;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 shift = Vector3.right * (side * 0.005f);
                for (int i = 0; i < steps; i++)
                {
                    if (!keep[i] || !keep[i + 1]) continue;
                    Vector3 a = baseLine[i] + shift, d = baseLine[i + 1] + shift;
                    Vector3 bb = a + ups[i] * heights[i], c = d + ups[i + 1] * heights[i + 1];
                    Vector3 n = Vector3.right * side;
                    float u0 = i * 2f / steps, u1 = (i + 1) * 2f / steps;
                    Face(b, SubStrands, a, bb, c, d, n, new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f), new Vector2(u1, 0f), true);
                }
            }
        }

        // ================================================================== beards

        /// <summary>Stubble or a short beard over the cheeks, jaw and chin, leaving the mouth free. False when nothing is built.</summary>
        public static bool BuildBeard(MeshBuilder b, LiveSkullFit fit, LiveBeard beard)
        {
            if (b == null || fit == null || !fit.Valid || beard == LiveBeard.None) return false;
            int before = b.VertexCount;
            int sub = beard == LiveBeard.Stubble ? SubStubble : SubShortBeard;
            float noseBase = fit.EyeY - 0.048f;
            float mouth = fit.Chin + 0.4f * (noseBase - fit.Chin);
            float ear = EarAzimuth(fit);
            Func<float, float> cheekLine = phi =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
                return Interp(d, new[] { 0f, 24f, 55f, ear - 30f, ear - 10f }, new[] { mouth - 0.012f, mouth - 0.004f, fit.EyeY - 0.05f, fit.EarBottom + 0.004f, fit.EarBottom + 0.004f });
            };
            Func<float, float> jawLine = phi =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
                return Interp(d, new[] { 0f, 45f, ear - 30f, ear - 10f }, new[] { fit.Chin - 0.004f, fit.Chin + 0.004f, fit.EarBottom - 0.032f, fit.EarBottom - 0.02f });
            };
            Func<float, float, float> offset = beard == LiveBeard.Stubble
                ? (Func<float, float, float>)((v, phi) => 0.0012f)
                : ((v, phi) => 0.0025f + 0.009f * v * Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(0f, phi)) / (ear - 10f)));
            float wide = ear - 10f;
            // The jaw and cheeks, in two halves so the mouth stays open between them.
            ShellPatch(b, sub, fit, phi => fit.PolarAtHeight(phi, cheekLine(phi)), phi => fit.PolarAtHeight(phi, jawLine(phi)), offset, 6, 14, 2.2f, beard == LiveBeard.Stubble ? 0.9f : 0.25f, 1f, 0f, false, -wide, wide);
            // The moustache: between the nose and the lip.
            Func<float, float> mTop = phi => fit.PolarAtHeight(phi, noseBase - 0.003f);
            Func<float, float> mBottom = phi => fit.PolarAtHeight(phi, mouth + 0.011f);
            ShellPatch(b, sub, fit, mTop, mBottom, (v, phi) => beard == LiveBeard.Stubble ? 0.0012f : 0.0035f, 2, 6, 0.5f, beard == LiveBeard.Stubble ? 0.9f : 0.4f, 1f, 0.5f, false, -24f, 24f);
            return b.VertexCount > before;
        }

        // ================================================================== headwear

        /// <summary>A headpiece fitted to this skull. False when the piece draws nothing.</summary>
        public static bool BuildHeadpiece(MeshBuilder b, LiveSkullFit fit, LiveHeadpiece piece)
        {
            if (b == null || fit == null || !fit.Valid || piece == LiveHeadpiece.None) return false;
            int before = b.VertexCount;
            switch (piece)
            {
                case LiveHeadpiece.Cap: Cap(b, fit, false); break;
                case LiveHeadpiece.OfficerCap: Cap(b, fit, true); break;
                case LiveHeadpiece.Beanie: Beanie(b, fit); break;
                case LiveHeadpiece.Bandana: Bandana(b, fit); break;
                case LiveHeadpiece.Hood: Hood(b, fit); break;
                case LiveHeadpiece.Helm: Helm(b, fit); break;
                case LiveHeadpiece.WorkHelm: WorkHelm(b, fit); break;
                case LiveHeadpiece.BrimHat: BrimHat(b, fit); break;
                case LiveHeadpiece.Goggles: Goggles(b, fit); break;
                case LiveHeadpiece.Circlet: Circlet(b, fit); break;
                case LiveHeadpiece.Headset: Headset(b, fit); break;
                case LiveHeadpiece.Glasses: Glasses(b, fit); break;
            }
            return b.VertexCount > before;
        }

        private static void Cap(MeshBuilder b, LiveSkullFit fit, bool officer)
        {
            LiveHeadpiece piece = officer ? LiveHeadpiece.OfficerCap : LiveHeadpiece.Cap;
            Func<float, float> line = phi => fit.PolarAtHeight(phi, CoverLine(fit, piece, phi));
            Func<float, float, float> crown = (v, phi) =>
            {
                float front = Mathf.Clamp01(Mathf.Cos(phi * Mathf.Deg2Rad));
                return officer ? 0.014f + 0.03f * (1f - v) * front * front + 0.004f * (1f - v) : 0.012f + 0.004f * (1f - v);
            };
            ShellPatch(b, SubCloth, fit, phi => 0f, line, crown, 8, 28, 1f, 0f, 1f, 0f, false);
            Peak(b, officer ? SubMetal : SubCloth, fit, line, phi => crown(1f, phi), officer ? 0.045f : 0.065f, officer ? 36f : 44f);
            if (officer)
            {
                Band(b, SubTrim, fit, CoverLine(fit, piece, 0f) + 0.012f, 0.024f, 0.017f, 28, -180f, 180f, phi => CoverLine(fit, piece, phi) - CoverLine(fit, piece, 0f));
                Vector3 badge = fit.Surface(fit.PolarAtHeight(0f, CoverLine(fit, piece, 0f) + 0.04f), 0f, 0.036f);
                Ellipsoid(b, SubTrim, badge, new Vector3(0.016f, 0.02f, 0.006f), 4, 8);
            }
            else Ellipsoid(b, SubTrim, fit.Surface(0f, 0f, 0.017f), new Vector3(0.009f, 0.006f, 0.009f), 3, 8);
        }

        private static void Beanie(MeshBuilder b, LiveSkullFit fit)
        {
            Func<float, float> line = phi => fit.PolarAtHeight(phi, CoverLine(fit, LiveHeadpiece.Beanie, phi));
            ShellPatch(b, SubCloth, fit, phi => 0f, line, (v, phi) => 0.013f + 0.012f * Mathf.Pow(1f - v, 2f) + (v > 0.78f ? 0.009f : 0f), 9, 28, 1f, 0f, 1f, 0f, false, -180f, 180f, 0.018f);
        }

        private static void Bandana(MeshBuilder b, LiveSkullFit fit)
        {
            Func<float, float> line = phi => fit.PolarAtHeight(phi, CoverLine(fit, LiveHeadpiece.Bandana, phi));
            ShellPatch(b, SubCloth, fit, phi => 0f, line, (v, phi) => 0.005f, 7, 28, 1f, 0f, 1f, 0f, false);
            float knotTheta = fit.PolarAtHeight(180f, fit.Nape + 0.045f);
            Vector3 knot = fit.Surface(knotTheta, 180f, 0.012f);
            Ellipsoid(b, SubCloth, knot, new Vector3(0.022f, 0.014f, 0.016f), 4, 8);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 top = knot + Vector3.right * (side * 0.01f);
                Vector3 dir = (Vector3.down + Vector3.right * (side * 0.25f) + Vector3.back * 0.15f).normalized;
                Vector3 across = Vector3.right * 0.014f;
                Vector3 bottom = top + dir * 0.11f;
                Face(b, SubCloth, top - across, bottom - across * 0.6f, bottom + across * 0.6f, top + across, Vector3.back, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.3f, 1f), new Vector2(0.3f, 0f), true);
            }
        }

        private static void Hood(MeshBuilder b, LiveSkullFit fit)
        {
            Func<float, float> edge = phi =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
                // The face opening: the hood stops at the brim above the brows in front and drops past the cheeks to run down the sides and back.
                float y = Interp(d, new[] { 0f, 50f, 64f, 90f, 130f, 180f }, new[] { fit.BrowTop + 0.04f, fit.BrowTop + 0.04f, fit.EyeY - 0.02f, fit.EarBottom - 0.02f, fit.Nape - 0.03f, fit.Nape - 0.04f });
                return fit.PolarAtHeight(phi, y);
            };
            Func<float, float, float> offset = (v, phi) => 0.03f + 0.012f * (1f - v);
            ShellPatch(b, SubCloth, fit, phi => 0f, edge, offset, 10, 32, 1f, 0f, 1f, 0f, true);
            // The cowl: over the collar and down the back and shoulders.
            Func<float, float, float> radius = (phi, y) => Mathf.Max(fit.HorizontalRadius(fit.PolarAtHeight(phi, Mathf.Max(y, fit.Nape)), phi) + 0.03f, fit.ClearanceAt(phi, y) + CollarRoom + 0.004f);
            Curtain(b, SubCloth, fit, 60f, 300f, phi => fit.Nape + 0.01f, phi => fit.Nape - 0.15f, radius, 6, 16, 1f, 0f, 1f, 0f);
        }

        private static void Helm(MeshBuilder b, LiveSkullFit fit)
        {
            // The visor dome: a smooth plate over the face at the nose's reach, blended into the shell at the sides.
            float plate = 0f;
            for (float theta = 80f; theta <= 150f; theta += 5f)
                for (float phi = -45f; phi <= 45f; phi += 5f) plate = Mathf.Max(plate, fit.RadiusAt(theta, phi));
            Func<float, float> edge = phi =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
                return fit.PolarAtHeight(phi, Interp(d, new[] { 0f, 50f, 100f, 140f, 180f }, new[] { fit.Chin + 0.006f, fit.Chin + 0.012f, fit.EarBottom - 0.012f, fit.Nape + 0.004f, fit.Nape + 0.004f }));
            };
            float browTheta = fit.PolarAtHeight(0f, fit.BrowTop + 0.008f);
            Func<float, float, float, float> radiusAt = (theta, phi, v) =>
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(0f, phi));
                float blend = Mathf.Clamp01(1f - d / 48f) * Mathf.Clamp01((theta - browTheta) / 10f);
                return Mathf.Lerp(fit.RadiusAt(theta, phi), Mathf.Max(fit.RadiusAt(theta, phi), plate), blend) + 0.012f;
            };
            ShellPatchRadial(b, SubMetal, fit, phi => 0f, edge, radiusAt, 12, 32, 1f, false);
            // The visor slit, lit faintly.
            float slitTop = fit.EyeY + 0.008f, slitBottom = fit.EyeY - 0.008f;
            Func<float, float> sTop = phi => fit.PolarAtHeight(phi, slitTop);
            Func<float, float> sBottom = phi => fit.PolarAtHeight(phi, slitBottom);
            ShellPatchRadial(b, SubTrim, fit, sTop, sBottom, (theta, phi, v) => Mathf.Max(fit.RadiusAt(theta, phi), plate) + 0.0135f, 1, 10, 1f, false, -38f, 38f);
            // A ridge along the crown and a neck guard outside the collar.
            Func<float, float> ridgeEdge = phi => Mathf.Abs(Mathf.DeltaAngle(0f, phi)) < 90f ? browTheta - 4f : fit.PolarAtHeight(phi, fit.EarTop + 0.02f);
            ShellPatch(b, SubTrim, fit, phi => 0f, ridgeEdge, (v, phi) => 0.019f, 6, 32, 1f, 0f, 1f, 0f, false, -6f, 6f);
            ShellPatch(b, SubTrim, fit, phi => 0f, ridgeEdge, (v, phi) => 0.019f, 6, 32, 1f, 0f, 1f, 0f, false, 174f, 186f);
            Func<float, float, float> guard = (phi, y) => Mathf.Max(fit.HorizontalRadius(fit.PolarAtHeight(phi, Mathf.Max(y, fit.Nape)), phi) + 0.012f, fit.ClearanceAt(phi, y) + 0.006f);
            Curtain(b, SubMetal, fit, 95f, 265f, phi => fit.Nape + 0.008f, phi => fit.Nape - 0.05f, guard, 3, 12, 1f, 0f, 1f, 0f);
        }

        private static void WorkHelm(MeshBuilder b, LiveSkullFit fit)
        {
            Func<float, float> line = phi => fit.PolarAtHeight(phi, CoverLine(fit, LiveHeadpiece.WorkHelm, phi));
            ShellPatch(b, SubMetal, fit, phi => 0f, line, (v, phi) => 0.016f + 0.003f * (1f - v), 8, 28, 1f, 0f, 1f, 0f, false);
            Peak(b, SubMetal, fit, line, phi => 0.016f, 0.035f, 40f);
            Band(b, SubTrim, fit, CoverLine(fit, LiveHeadpiece.WorkHelm, 0f) + 0.008f, 0.012f, 0.0185f, 28, -180f, 180f, phi => CoverLine(fit, LiveHeadpiece.WorkHelm, phi) - CoverLine(fit, LiveHeadpiece.WorkHelm, 0f));
        }

        private static void BrimHat(MeshBuilder b, LiveSkullFit fit)
        {
            Func<float, float> line = phi => fit.PolarAtHeight(phi, CoverLine(fit, LiveHeadpiece.BrimHat, phi));
            ShellPatch(b, SubCloth, fit, phi => 0f, line, (v, phi) => 0.012f + 0.05f * Mathf.Pow(1f - v, 0.7f), 8, 28, 1f, 0f, 1f, 0f, false);
            Band(b, SubTrim, fit, CoverLine(fit, LiveHeadpiece.BrimHat, 0f) + 0.012f, 0.022f, 0.016f, 28, -180f, 180f, phi => CoverLine(fit, LiveHeadpiece.BrimHat, phi) - CoverLine(fit, LiveHeadpiece.BrimHat, 0f));
            // The brim: a ring from the hat line out 7.5 cm, drooping a little.
            const int cols = 28;
            for (int j = 0; j < cols; j++)
            {
                float p0 = -180f + 360f * j / cols, p1 = -180f + 360f * (j + 1) / cols;
                Vector3 i0 = fit.Surface(line(p0), p0, 0.012f), i1 = fit.Surface(line(p1), p1, 0.012f);
                Vector3 o0 = i0 + Horizontal(p0) * 0.075f + Vector3.down * 0.012f, o1 = i1 + Horizontal(p1) * 0.075f + Vector3.down * 0.012f;
                Face(b, SubCloth, i0, o0, o1, i1, Vector3.up, new Vector2(j / (float)cols, 0f), new Vector2(j / (float)cols, 1f), new Vector2((j + 1f) / cols, 1f), new Vector2((j + 1f) / cols, 0f), true);
            }
        }

        private static void Goggles(MeshBuilder b, LiveSkullFit fit)
        {
            float strap = fit.BrowTop + 0.026f;
            Band(b, SubTrim, fit, strap, 0.022f, 0.006f, 28, -180f, 180f, phi => 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                float phi = side * 14f;
                float theta = fit.PolarAtHeight(phi, strap + 0.004f);
                Vector3 centre = fit.Surface(theta, phi, 0.006f);
                Vector3 axis = LiveSkullFit.Direction(theta, phi);
                Cylinder(b, SubMetal, centre, axis, 0.024f, 0.021f, 0.02f, 10, false);
                Disc(b, SubTrim, centre + axis * 0.0205f, axis, 0.019f, 10);
            }
        }

        private static void Circlet(MeshBuilder b, LiveSkullFit fit)
        {
            float line = fit.BrowTop + 0.02f;
            Band(b, SubTrim, fit, line, 0.011f, 0.004f, 32, -180f, 180f, phi => 0f);
            Vector3 stone = fit.Surface(fit.PolarAtHeight(0f, line + 0.004f), 0f, 0.008f);
            Ellipsoid(b, SubTrim, stone, new Vector3(0.012f, 0.016f, 0.008f), 4, 8);
        }

        private static void Headset(MeshBuilder b, LiveSkullFit fit)
        {
            // The band over the crown from ear to ear.
            const int steps = 14;
            float ear = EarAzimuth(fit);
            for (int i = 0; i < steps; i++)
            {
                float t0 = -1f + 2f * i / steps, t1 = -1f + 2f * (i + 1) / steps;
                Vector3 a = BandPoint(fit, t0, ear, -0.011f), bb = BandPoint(fit, t0, ear, 0.011f), c = BandPoint(fit, t1, ear, 0.011f), d = BandPoint(fit, t1, ear, -0.011f);
                Vector3 n = (LiveSkullFit.Direction(Mathf.Abs(t0) * 92f, t0 < 0f ? -ear : ear) + LiveSkullFit.Direction(Mathf.Abs(t1) * 92f, t1 < 0f ? -ear : ear)).normalized;
                Face(b, SubTrim, a, bb, c, d, n, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, true);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float phi = side * ear;
                float theta = fit.PolarAtHeight(phi, fit.EarTop - 0.022f);
                Vector3 centre = fit.Surface(theta, phi, 0.004f);
                Vector3 axis = LiveSkullFit.Direction(theta, phi);
                Cylinder(b, SubMetal, centre, axis, 0.036f, 0.032f, 0.03f, 12, true);
            }
            // The microphone boom from the left cup toward the mouth.
            float cupTheta = fit.PolarAtHeight(-ear, fit.EarTop - 0.022f);
            Vector3 cup = fit.Surface(cupTheta, -ear, 0.03f);
            float mouthTheta = fit.PolarAtHeight(-28f, fit.Chin + 0.045f);
            Vector3 mic = fit.Surface(mouthTheta, -28f, 0.02f);
            Bar(b, SubTrim, cup, mic, 0.007f);
            Ellipsoid(b, SubTrim, mic, new Vector3(0.011f, 0.009f, 0.009f), 3, 8);
        }

        private static void Glasses(MeshBuilder b, LiveSkullFit fit)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float phi = side * 15f;
                float theta = fit.PolarAtHeight(phi, fit.EyeY + 0.002f);
                Vector3 centre = fit.Surface(theta, phi, 0.014f);
                Vector3 axis = LiveSkullFit.Direction(theta, phi);
                Ring(b, SubMetal, centre, axis, 0.02f, 0.0022f, 12);
            }
            Vector3 l = fit.Surface(fit.PolarAtHeight(-15f, fit.EyeY + 0.002f), -15f, 0.014f);
            Vector3 r = fit.Surface(fit.PolarAtHeight(15f, fit.EyeY + 0.002f), 15f, 0.014f);
            Bar(b, SubMetal, Vector3.Lerp(l, r, 0.32f) + Vector3.up * 0.006f, Vector3.Lerp(l, r, 0.68f) + Vector3.up * 0.006f, 0.003f);
            float ear = EarAzimuth(fit);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 hinge = fit.Surface(fit.PolarAtHeight(side * 36f, fit.EyeY + 0.006f), side * 36f, 0.014f);
                Vector3 temple = fit.Surface(fit.PolarAtHeight(side * (ear - 12f), fit.EarTop - 0.004f), side * (ear - 12f), 0.005f);
                Bar(b, SubMetal, hinge, temple, 0.003f);
            }
        }

        // ================================================================== primitives

        /// <summary>A grid over the skull between two polar edges per azimuth, pushed out by offset(v, phi), with strand UVs.</summary>
        private static void ShellPatch(MeshBuilder b, int sub, LiveSkullFit fit, Func<float, float> start, Func<float, float> edge, Func<float, float, float> offset,
            int rows, int cols, float tiles, float v0, float v1, float uPhase, bool doubleSided, float phiFrom = -180f, float phiTo = 180f, float slouch = 0f)
        {
            ShellPatchRadial(b, sub, fit, start, edge, (theta, phi, v) => fit.RadiusAt(theta, phi) + offset(v, phi), rows, cols, tiles, doubleSided, phiFrom, phiTo, v0, v1, uPhase, slouch);
        }

        /// <summary>The same grid with the full radius per vertex supplied (the helm's visor dome flattens the face).</summary>
        private static void ShellPatchRadial(MeshBuilder b, int sub, LiveSkullFit fit, Func<float, float> start, Func<float, float> edge, Func<float, float, float, float> radius,
            int rows, int cols, float tiles, bool doubleSided, float phiFrom = -180f, float phiTo = 180f, float v0 = 0f, float v1 = 1f, float uPhase = 0f, float slouch = 0f)
        {
            Vector3[] p = new Vector3[(rows + 1) * (cols + 1)];
            Vector3[] n = new Vector3[p.Length];
            Vector2[] uv = new Vector2[p.Length];
            for (int j = 0; j <= cols; j++)
            {
                float phi = Mathf.Lerp(phiFrom, phiTo, (float)j / cols);
                float ts = start(phi), te = edge(phi);
                if (te < ts) te = ts;
                for (int i = 0; i <= rows; i++)
                {
                    float v = (float)i / rows;
                    float theta = Mathf.Lerp(ts, te, v);
                    Vector3 dir = LiveSkullFit.Direction(theta, phi);
                    Vector3 pos = fit.Centre + dir * radius(theta, phi, v);
                    if (slouch > 0f && theta < 30f) pos += Vector3.back * (slouch * (1f - theta / 30f));
                    int k = i * (cols + 1) + j;
                    p[k] = pos;
                    n[k] = dir;
                    uv[k] = new Vector2((float)j / cols * tiles + uPhase, Mathf.Lerp(v0, v1, v));
                }
            }
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    int a = i * (cols + 1) + j, bb = a + 1, c = bb + cols + 1, d = a + cols + 1;
                    if ((p[a] - p[d]).sqrMagnitude < 1e-10f && (p[bb] - p[c]).sqrMagnitude < 1e-10f) continue;
                    FaceSmooth(b, sub, p[a], p[bb], p[c], p[d], n[a], n[bb], n[c], n[d], uv[a], uv[bb], uv[c], uv[d], doubleSided);
                }
        }

        /// <summary>A hanging surface between two azimuths, from yTop(phi) down to yBottom(phi), at a horizontal radius radius(phi, y) from the centre axis, both sides drawn.</summary>
        private static void Curtain(MeshBuilder b, int sub, LiveSkullFit fit, float phiFrom, float phiTo, Func<float, float> yTop, Func<float, float> yBottom, Func<float, float, float> radius, int rows, int cols, float tiles, float v0, float v1, float uPhase)
        {
            Vector3[] p = new Vector3[(rows + 1) * (cols + 1)];
            Vector3[] n = new Vector3[p.Length];
            Vector2[] uv = new Vector2[p.Length];
            for (int j = 0; j <= cols; j++)
            {
                float phi = Mathf.Lerp(phiFrom, phiTo, (float)j / cols);
                Vector3 h = Horizontal(phi);
                float top = yTop(phi), low = yBottom(phi);
                for (int i = 0; i <= rows; i++)
                {
                    float v = (float)i / rows;
                    float y = Mathf.Lerp(top, low, v);
                    int k = i * (cols + 1) + j;
                    p[k] = new Vector3(fit.Centre.x, y, fit.Centre.z) + h * radius(phi, y);
                    n[k] = h;
                    uv[k] = new Vector2((float)j / cols * tiles + uPhase, Mathf.Lerp(v0, v1, v));
                }
            }
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    int a = i * (cols + 1) + j, bb = a + 1, c = bb + cols + 1, d = a + cols + 1;
                    FaceSmooth(b, sub, p[a], p[bb], p[c], p[d], n[a], n[bb], n[c], n[d], uv[a], uv[bb], uv[c], uv[d], true);
                }
        }

        /// <summary>A strip following the skull around a height, offset out from it; heightShift(phi) lets the band follow a sloping line.</summary>
        private static void Band(MeshBuilder b, int sub, LiveSkullFit fit, float y, float height, float offset, int cols, float phiFrom, float phiTo, Func<float, float> heightShift)
        {
            ShellPatch(b, sub, fit, phi => fit.PolarAtHeight(phi, y + heightShift(phi) + height * 0.5f), phi => fit.PolarAtHeight(phi, y + heightShift(phi) - height * 0.5f), (v, phi) => offset, 1, cols, 1f, 0f, 1f, 0f, false, phiFrom, phiTo);
        }

        /// <summary>A cap peak: a plate from the cap line forward, reach metres long at the middle and shorter at the sides, drooping toward its tip.</summary>
        private static void Peak(MeshBuilder b, int sub, LiveSkullFit fit, Func<float, float> line, Func<float, float> offset, float reach, float halfWidth)
        {
            const int cols = 10;
            for (int j = 0; j < cols; j++)
            {
                float p0 = -halfWidth + 2f * halfWidth * j / cols, p1 = -halfWidth + 2f * halfWidth * (j + 1) / cols;
                Vector3 i0 = fit.Surface(line(p0), p0, offset(p0)), i1 = fit.Surface(line(p1), p1, offset(p1));
                float r0 = reach * Mathf.Lerp(0.3f, 1f, Mathf.Cos(p0 / halfWidth * Mathf.PI * 0.5f)), r1 = reach * Mathf.Lerp(0.3f, 1f, Mathf.Cos(p1 / halfWidth * Mathf.PI * 0.5f));
                Vector3 o0 = i0 + Horizontal(p0) * r0 + Vector3.down * (r0 * 0.22f), o1 = i1 + Horizontal(p1) * r1 + Vector3.down * (r1 * 0.22f);
                Face(b, sub, i0, o0, o1, i1, Vector3.up, new Vector2(j / (float)cols, 0f), new Vector2(j / (float)cols, 1f), new Vector2((j + 1f) / cols, 1f), new Vector2((j + 1f) / cols, 0f), true);
            }
        }

        /// <summary>A point on the headset band: t from -1 (left ear) over the crown to 1 (right ear), shifted front to back by across.</summary>
        private static Vector3 BandPoint(LiveSkullFit fit, float t, float ear, float across)
        {
            float theta = Mathf.Abs(t) * 92f, phi = t < 0f ? -ear : ear;
            return fit.Surface(theta, phi, 0.008f) + Vector3.forward * across;
        }

        /// <summary>A tube along a path with a radius per ring, tapering to a point at the end; the strand texture wraps around it.</summary>
        private static void Tube(MeshBuilder b, int sub, Vector3[] path, float[] radii, int sides, float tilesV, float uPhase)
        {
            int rings = path.Length;
            Vector3[] ring = new Vector3[(rings + 1) * (sides + 1)];
            Vector3[] normals = new Vector3[ring.Length];
            Vector2[] uv = new Vector2[ring.Length];
            for (int i = 0; i <= rings; i++)
            {
                int k = Mathf.Min(i, rings - 1);
                Vector3 tangent = (path[Mathf.Min(k + 1, rings - 1)] - path[Mathf.Max(k - 1, 0)]).normalized;
                if (tangent.sqrMagnitude < 0.5f) tangent = Vector3.down;
                Vector3 side = Vector3.Cross(tangent, Vector3.right);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(tangent, Vector3.forward);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, tangent).normalized;
                float r = i == rings ? 0f : radii[k];
                Vector3 c = i == rings ? path[rings - 1] + tangent * radii[rings - 1] * 1.5f : path[k];
                for (int s = 0; s <= sides; s++)
                {
                    float a = Mathf.PI * 2f * s / sides;
                    Vector3 dir = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    int idx = i * (sides + 1) + s;
                    ring[idx] = c + dir * r;
                    normals[idx] = dir;
                    uv[idx] = new Vector2((float)s / sides + uPhase, Mathf.Min(1f, (float)i / rings) * tilesV);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = i * (sides + 1) + s, bb = a + 1, c = bb + sides + 1, d = a + sides + 1;
                    FaceSmooth(b, sub, ring[a], ring[bb], ring[c], ring[d], normals[a], normals[bb], normals[c], normals[d], uv[a], uv[bb], uv[c], uv[d], false);
                }
        }

        /// <summary>An axis-aligned ellipsoid with wrapping UVs; with a fit, every vertex is kept at least minOffset outside the skull envelope.</summary>
        private static void Ellipsoid(MeshBuilder b, int sub, Vector3 centre, Vector3 radii, int rings, int segs, LiveSkullFit fit = null, float minOffset = 0f)
        {
            for (int r = 0; r < rings; r++)
            {
                float t0 = Mathf.PI * r / rings, t1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segs; s++)
                {
                    float p0 = Mathf.PI * 2f * s / segs, p1 = Mathf.PI * 2f * (s + 1) / segs;
                    Vector3 n00 = Unit(t0, p0), n01 = Unit(t0, p1), n11 = Unit(t1, p1), n10 = Unit(t1, p0);
                    Vector3 a = Outside(fit, centre + Vector3.Scale(n00, radii), minOffset), bb = Outside(fit, centre + Vector3.Scale(n01, radii), minOffset);
                    Vector3 c = Outside(fit, centre + Vector3.Scale(n11, radii), minOffset), d = Outside(fit, centre + Vector3.Scale(n10, radii), minOffset);
                    Vector2 ua = new Vector2((float)s / segs, (float)r / rings), ub = new Vector2((s + 1f) / segs, (float)r / rings), uc = new Vector2((s + 1f) / segs, (r + 1f) / rings), ud = new Vector2((float)s / segs, (r + 1f) / rings);
                    FaceSmooth(b, sub, a, bb, c, d, n00, n01, n11, n10, ua, ub, uc, ud, false);
                }
            }
        }

        /// <summary>A point pushed out along its direction from the skull centre until it clears the envelope by minOffset.</summary>
        private static Vector3 Outside(LiveSkullFit fit, Vector3 p, float minOffset)
        {
            if (fit == null) return p;
            float r, theta, phi;
            fit.Polar(p, out r, out theta, out phi);
            float least = fit.RadiusAt(theta, phi) + minOffset;
            return r >= least || r < 1e-6f ? p : fit.Centre + (p - fit.Centre) * (least / r);
        }

        private static Vector3 Unit(float theta, float phi)
        {
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        /// <summary>A short cylinder along an axis (a goggle lens, an ear cup), optionally closed at the far end.</summary>
        private static void Cylinder(MeshBuilder b, int sub, Vector3 baseCentre, Vector3 axis, float radiusBase, float radiusTop, float length, int sides, bool capped)
        {
            Vector3 side = Vector3.Cross(axis, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(axis, Vector3.forward);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, axis).normalized;
            Vector3 top = baseCentre + axis * length;
            for (int s = 0; s < sides; s++)
            {
                float a0 = Mathf.PI * 2f * s / sides, a1 = Mathf.PI * 2f * (s + 1) / sides;
                Vector3 d0 = side * Mathf.Cos(a0) + up * Mathf.Sin(a0), d1 = side * Mathf.Cos(a1) + up * Mathf.Sin(a1);
                FaceSmooth(b, sub, baseCentre + d0 * radiusBase, top + d0 * radiusTop, top + d1 * radiusTop, baseCentre + d1 * radiusBase, d0, d0, d1, d1,
                    new Vector2((float)s / sides, 0f), new Vector2((float)s / sides, 1f), new Vector2((s + 1f) / sides, 1f), new Vector2((s + 1f) / sides, 0f), false);
            }
            if (capped) Disc(b, sub, top, axis, radiusTop, sides);
        }

        /// <summary>A flat disc facing along an axis.</summary>
        private static void Disc(MeshBuilder b, int sub, Vector3 centre, Vector3 axis, float radius, int sides)
        {
            Vector3 side = Vector3.Cross(axis, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(axis, Vector3.forward);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, axis).normalized;
            for (int s = 0; s < sides; s++)
            {
                float a0 = Mathf.PI * 2f * s / sides, a1 = Mathf.PI * 2f * (s + 1) / sides;
                Vector3 p0 = centre + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * radius, p1 = centre + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * radius;
                Vector3 g = Vector3.Cross(p0 - centre, p1 - centre);
                if (Vector3.Dot(g, axis) < 0f) b.Triangle(sub, centre, p1, p0, axis, new Vector2(0.5f, 0.5f), Vector2.one, Vector2.zero);
                else b.Triangle(sub, centre, p0, p1, axis, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one);
            }
        }

        /// <summary>A thin ring (a spectacle rim) in the plane facing an axis.</summary>
        private static void Ring(MeshBuilder b, int sub, Vector3 centre, Vector3 axis, float radius, float thickness, int sides)
        {
            Vector3 side = Vector3.Cross(axis, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(axis, Vector3.forward);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, axis).normalized;
            for (int s = 0; s < sides; s++)
            {
                float a0 = Mathf.PI * 2f * s / sides, a1 = Mathf.PI * 2f * (s + 1) / sides;
                Vector3 d0 = side * Mathf.Cos(a0) + up * Mathf.Sin(a0), d1 = side * Mathf.Cos(a1) + up * Mathf.Sin(a1);
                Vector3 o0 = centre + d0 * radius, o1 = centre + d1 * radius;
                Face(b, sub, o0 + axis * thickness, o0 - axis * thickness, o1 - axis * thickness, o1 + axis * thickness, (d0 + d1).normalized, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, false);
                Face(b, sub, o0 + axis * thickness, o1 + axis * thickness, o1 + axis * thickness - d1 * thickness, o0 + axis * thickness - d0 * thickness, axis, Vector2.zero, Vector2.up, Vector2.one, Vector2.right, false);
            }
        }

        /// <summary>A square bar between two points.</summary>
        private static void Bar(MeshBuilder b, int sub, Vector3 from, Vector3 to, float size)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 1e-5f) return;
            Quaternion rot = QMath.FromTo(Vector3.up, d / len);
            b.Box(sub, (from + to) * 0.5f, new Vector3(size, len, size), rot, UVMode.Unit);
        }

        private static Vector3 Horizontal(float phiDeg)
        {
            float p = phiDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(p), 0f, Mathf.Cos(p));
        }

        /// <summary>A flat quad whose winding is chosen so it faces n (Unity: Cross(b - a, c - a) is the front normal), optionally drawn on both sides.</summary>
        private static void Face(MeshBuilder b, int sub, Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 n, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, bool doubleSided)
        {
            Vector3 g = Vector3.Cross(bb - a, c - a) + Vector3.Cross(c - a, d - a);
            if (g.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(g, n) >= 0f)
            {
                b.Quad(sub, a, bb, c, d, n, ua, ub, uc, ud);
                if (doubleSided) b.Quad(sub, a, d, c, bb, -n, ua, ud, uc, ub);
            }
            else
            {
                b.Quad(sub, a, d, c, bb, n, ua, ud, uc, ub);
                if (doubleSided) b.Quad(sub, a, bb, c, d, -n, ua, ub, uc, ud);
            }
        }

        /// <summary>A smooth-shaded quad facing the mean of its normals, optionally drawn on both sides.</summary>
        private static void FaceSmooth(MeshBuilder b, int sub, Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, bool doubleSided)
        {
            Vector3 g = Vector3.Cross(bb - a, c - a) + Vector3.Cross(c - a, d - a);
            if (g.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(g, na + nb + nc + nd) >= 0f)
            {
                b.QuadSmooth(sub, a, bb, c, d, na, nb, nc, nd, ua, ub, uc, ud);
                if (doubleSided) b.QuadSmooth(sub, a, d, c, bb, -na, -nd, -nc, -nb, ua, ud, uc, ub);
            }
            else
            {
                b.QuadSmooth(sub, a, d, c, bb, na, nd, nc, nb, ua, ud, uc, ub);
                if (doubleSided) b.QuadSmooth(sub, a, bb, c, d, -na, -nb, -nc, -nd, ua, ub, uc, ud);
            }
        }
    }
}
