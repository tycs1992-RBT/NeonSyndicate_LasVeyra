using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns the shared grade numbers (PostGrade.Compute) into URP volume parameters, so the URP picture matches the
    /// Built-in NeonPost picture. Pure math with no URP types, so it compiles on every path and is checked offline
    /// against ports of both pipelines' shader code (the URP_MIGRATION doc lists the harness and its results).
    /// It assumes URP's Low Dynamic Range grading mode (the default, and what Switch to URP sets up): URP then grades
    /// after ACES tonemapping, in the same order and colour space NeonPost.shader uses.
    /// The fitted constants come from that harness:
    ///  - NeonPost runs the fitted ACES curve (RRT and ODT with the sRGB matrices) after PostGrade.AcesPreExposure;
    ///    URP's ACES (Core Color.hlsl AcesTonemap) is the full transform, so the exposure offset carries that
    ///    pre-exposure plus a small residual, and the contrast and saturation factors absorb the rest of the
    ///    difference between the fit and the full transform (grey ramp error under one 8-bit step).
    ///  - URP pivots contrast at linear 0.2248 (ACEScc_MIDGRAY read as LogC), NeonPost at 0.18; the gain absorbs it.
    ///  - NeonPost adds its shadow and highlight tints over smoothstep bands. URP's lift and gain carry them exactly
    ///    at black and white (a multiplier cannot tint black), and Shadows, Midtones, Highlights bends the straight
    ///    line in between back toward NeonPost's bands at their crossover (display luma 0.45).
    ///  - URP's LogC contrast crushes near-black harder than NeonPost's power curve, so the contrast is scaled down.
    ///  - NeonPost vignettes after tonemapping, URP before it. NeonPostFeature applies NeonPost's exact vignette after
    ///    URP's grading (ExactVignette); URP's own Vignette, fitted on mid tones, only stands in without that feature.
    ///  - NeonPost's bloom adds its mip levels with weight 0.72 per level (five to eight levels by preset,
    ///    PostMath.BloomLevels), URP blends them; scatter and intensity are least-squares fits of the per-level weights.
    ///  - The bloom threshold and the eye adaptation limits come from PostGrade.Values, never from constants here.
    /// </summary>
    public static class UrpGradeMapping
    {
        /// <summary>The fitted constants. Default is what the game uses; the offline harness passes others when refitting.</summary>
        public struct Tuning
        {
            public float AcesExposureMatch;
            public float AcesSaturationMatch;
            public float AcesContrastMatch;
            public float ContrastPivot;
            public float ContrastScale;
            public float MidtoneTintScale;
            public float VignetteSmoothness;
            public float GrainPerUnit;
            public float GrainResponse;

            public static Tuning Default
            {
                get
                {
                    return new Tuning
                    {
                        AcesExposureMatch = 0.96f,
                        AcesSaturationMatch = 1f,
                        AcesContrastMatch = 1f,
                        ContrastPivot = 0.2248f,
                        ContrastScale = 0.94f,
                        MidtoneTintScale = 0.5f,
                        VignetteSmoothness = 0.23f,
                        GrainPerUnit = 13.65f,
                        GrainResponse = 1f
                    };
                }
            }
        }

        /// <summary>URP Film Grain type Thin2 (FilmGrainLookup order): the finest grain with enough strength.</summary>
        public const int FilmGrainThin2 = 1;
        /// <summary>NeonPost clamps the bloom source at 18 before thresholding.</summary>
        public const float BloomClamp = 18f;

        /// <summary>Everything the URP volume stack needs, in URP's own parameter units.</summary>
        public struct Result
        {
            public float BloomThreshold;        // gamma space, as Bloom.threshold
            public float BloomIntensity;
            public float BloomScatter;
            public Color BloomTint;             // gamma space, as Bloom.tint (URP takes .linear and normalises luminance)
            public int BloomMaxIterations;
            public bool BloomHighQuality;
            public float PostExposure;          // EV
            public float Contrast;              // -100 .. 100
            public float Saturation;            // -100 .. 100
            public Vector4 Lift, Gamma, Gain;   // LiftGammaGain parameters
            public Vector4 Shadows, Midtones, Highlights;
            public float ShadowsStart, ShadowsEnd, HighlightsStart, HighlightsEnd;
            public float VignetteIntensity, VignetteSmoothness;  // URP Vignette (used when the Neon post feature is missing)
            public float ExactVignette;         // NeonPost's own vignette (2.2 x amount), applied after tonemapping by the feature
            public float ChromaticIntensity;
            public int GrainType;
            public float GrainIntensity, GrainResponse;
            public float MotionBlurIntensity, MotionBlurClamp;
        }

        /// <summary>Bloom mip levels NeonPost uses per preset (PostMath.BloomLevels; URP allows at most eight iterations).</summary>
        public static int BloomLevels(GraphicsPreset q)
        {
            return Mathf.Clamp(PostMath.BloomLevels(q), 2, 8);
        }

        /// <summary>
        /// g: PostGrade.Compute for this frame. night: Atmosphere.NightFactor. adaptEv: the eye adaptation exposure in
        /// EV (0 when off). motionBlur: NeonPost's blur scale (0.45 / fpsScale, 0 when motion blur is off).
        /// </summary>
        public static Result Compute(PostGrade.Values g, GraphicsPreset q, float night, float adaptEv, float motionBlur)
        {
            return Compute(g, q, night, adaptEv, motionBlur, Tuning.Default);
        }

        public static Result Compute(PostGrade.Values g, GraphicsPreset q, float night, float adaptEv, float motionBlur, Tuning t)
        {
            Result r;

            // ---- bloom: same soft-knee threshold (URP reads it in gamma space), weights fitted to NeonPost's mip sum
            float threshold = Mathf.Max(g.BloomThreshold, 0.01f);
            int levels = BloomLevels(q);
            float scatterParam, levelGain;
            BloomWeights(levels, out scatterParam, out levelGain);
            Color tint = g.BloomTint;
            float tintLuma = Luminance(tint);
            r.BloomThreshold = LinearToGamma(threshold);
            r.BloomIntensity = g.BloomIntensity * levelGain * Mathf.Max(tintLuma, 0f);
            r.BloomScatter = scatterParam;
            r.BloomTint = new Color(LinearToGamma(tint.r), LinearToGamma(tint.g), LinearToGamma(tint.b), 1f);
            r.BloomMaxIterations = levels;
            r.BloomHighQuality = q >= GraphicsPreset.High;

            // ---- exposure, contrast, saturation (URP's ACES matched to NeonPost's fitted curve)
            r.PostExposure = Log2(Mathf.Max(g.Exposure, 1e-4f)) + adaptEv + t.AcesExposureMatch;
            float contrast = 1f + (g.Contrast * t.AcesContrastMatch - 1f) * t.ContrastScale;
            float saturation = g.Saturation * t.AcesSaturationMatch;
            r.Contrast = Mathf.Clamp((contrast - 1f) * 100f, -100f, 100f);
            r.Saturation = Mathf.Clamp((saturation - 1f) * 100f, -100f, 100f);

            // ---- lift, gain and the tints. NeonPost after contrast: c * gain + lift * (1 - c) + shadowTint * sh(l) +
            // highlightTint * hi(l), with sh = 1 - smoothstep(0, 0.45, l) and hi = smoothstep(0.45, 1, l). URP after
            // contrast: c * gain + lift. Matching both ends: lift = lift + shadowTint (black), gain = gain - lift -
            // shadowTint + highlightTint (white). URP's contrast pivot sits higher, which darkens mid tones by
            // (pivot / 0.18) ^ (1 - contrast); the gain gives that back. URP saturates after these steps and NeonPost
            // before them, so their colour (distance from grey) is divided by the saturation URP will apply.
            float pivot = Mathf.Pow(t.ContrastPivot / 0.18f, contrast - 1f);
            Color ts = g.ShadowTint, th = g.HighlightTint;
            Color lift = new Color(g.Lift.r + ts.r, g.Lift.g + ts.g, g.Lift.b + ts.b, 1f);
            Color gain = new Color((g.Gain.r - lift.r + th.r) * pivot, (g.Gain.g - lift.g + th.g) * pivot, (g.Gain.b - lift.b + th.b) * pivot, 1f);
            r.Lift = LiftParameter(Desaturate(lift, saturation));
            r.Gamma = new Vector4(1f, 1f, 1f, 0f);
            r.Gain = GainParameter(Desaturate(gain, saturation));

            // ---- what the straight line misses: at the band crossover (l = 0.45) NeonPost's tints add nothing, the line
            // adds 0.55 x shadowTint + 0.45 x highlightTint. URP's Midtones band (between the shadow end and the
            // highlight start, both 0.45) peaks there, so a mid-tone factor takes it back out.
            float k = t.MidtoneTintScale / 0.45f;
            Color mid = new Color(1f - (0.55f * ts.r + 0.45f * th.r) * k, 1f - (0.55f * ts.g + 0.45f * th.g) * k, 1f - (0.55f * ts.b + 0.45f * th.b) * k, 1f);
            mid = Desaturate(mid, saturation);
            r.Shadows = new Vector4(1f, 1f, 1f, 0f);
            r.Midtones = FactorParameter(new Color(Mathf.Max(0f, mid.r), Mathf.Max(0f, mid.g), Mathf.Max(0f, mid.b), 1f));
            r.Highlights = new Vector4(1f, 1f, 1f, 0f);
            r.ShadowsStart = 0f;
            r.ShadowsEnd = 0.45f;
            r.HighlightsStart = 0.45f;
            r.HighlightsEnd = 1f;

            // ---- vignette: NeonPost multiplies by 1 - 2.2 V r^2 after tonemapping; URP by (1 - (3 I)^2 r^2)^(5 S) before it
            // (the closest URP's Vignette gets, since bright pixels cannot darken before ACES compresses them). The Neon
            // post render feature applies NeonPost's own formula after tonemapping instead, when the renderer has it.
            r.VignetteIntensity = Mathf.Clamp01(Mathf.Sqrt(Mathf.Max(0f, 2.2f * g.Vignette)) / 3f);
            r.VignetteSmoothness = Mathf.Clamp(t.VignetteSmoothness, 0.01f, 1f);
            r.ExactVignette = Mathf.Max(0f, g.Vignette * 2.2f);

            // ---- chromatic aberration: URP spreads red to blue over 16/3 x 0.05 x intensity, NeonPost over 2 x amount
            r.ChromaticIntensity = Mathf.Clamp01(g.Chromatic * 7.5f);

            // ---- film grain: URP's grain is multiplicative (none in black); this matches NeonPost's strength in the mid tones
            r.GrainType = FilmGrainThin2;
            r.GrainIntensity = g.Grain > 0f ? Mathf.Clamp01(g.Grain * t.GrainPerUnit) : 0f;
            r.GrainResponse = Mathf.Clamp01(t.GrainResponse);

            // ---- camera motion blur: URP measures velocity in clip space (twice NeonPost's uv units) and clamps it
            // before scaling, NeonPost after; this keeps NeonPost's blur length and its 0.04 cap.
            r.MotionBlurIntensity = Mathf.Clamp01(motionBlur * 0.5f);
            r.MotionBlurClamp = r.MotionBlurIntensity > 0f ? Mathf.Clamp(0.04f / r.MotionBlurIntensity, 0f, 0.2f) : 0.05f;
            return r;
        }

        /// <summary>Least-squares fit of URP's blended mip weights to NeonPost's 0.72-per-level sum (offline harness).</summary>
        public static void BloomWeights(int levels, out float scatterParam, out float intensityGain)
        {
            switch (levels)
            {
                case 4: scatterParam = 0.578f; intensityGain = 2.504f; break;
                case 5: scatterParam = 0.622f; intensityGain = 2.753f; break;
                case 6: scatterParam = 0.656f; intensityGain = 2.952f; break;
                case 7: scatterParam = 0.678f; intensityGain = 3.101f; break;
                default: scatterParam = 0.694f; intensityGain = 3.216f; break;
            }
        }

        /// <summary>
        /// Inverse of URP's ColorUtils.PrepareShadowsMidtonesHighlights for one band: the shader multiplies by
        /// GammaToLinear(rgb) + weight, so rgb = LinearToGamma(factor) with no weight.
        /// </summary>
        public static Vector4 FactorParameter(Color factor)
        {
            return new Vector4(LinearToGamma(factor.r), LinearToGamma(factor.g), LinearToGamma(factor.b), 0f);
        }

        /// <summary>
        /// Inverse of URP's ColorUtils.PrepareLiftGammaGain for lift: the shader adds 0.15 x GammaToLinear(rgb) minus
        /// its luminance, plus w. The chroma goes into rgb (lifted so no channel is negative), the luminance into w.
        /// </summary>
        public static Vector4 LiftParameter(Color lift)
        {
            float lum = Luminance(lift);
            float dr = lift.r - lum, dg = lift.g - lum, db = lift.b - lum;
            float k = Mathf.Max(0f, -Mathf.Min(dr, Mathf.Min(dg, db)));
            return new Vector4(LinearToGamma((dr + k) / 0.15f), LinearToGamma((dg + k) / 0.15f), LinearToGamma((db + k) / 0.15f), lum);
        }

        /// <summary>
        /// Inverse of URP's ColorUtils.PrepareLiftGammaGain for gain: the shader multiplies by 0.8 x GammaToLinear(rgb)
        /// minus its luminance, plus w + 1.
        /// </summary>
        public static Vector4 GainParameter(Color gain)
        {
            float lum = Luminance(gain);
            float dr = gain.r - lum, dg = gain.g - lum, db = gain.b - lum;
            float k = Mathf.Max(0f, -Mathf.Min(dr, Mathf.Min(dg, db)));
            return new Vector4(LinearToGamma((dr + k) / 0.8f), LinearToGamma((dg + k) / 0.8f), LinearToGamma((db + k) / 0.8f), lum - 1f);
        }

        /// <summary>Moves a colour toward its own grey by 1 / saturation, undoing a later saturation step.</summary>
        public static Color Desaturate(Color c, float saturation)
        {
            float lum = Luminance(c), s = 1f / Mathf.Max(saturation, 1e-3f);
            return new Color(lum + (c.r - lum) * s, lum + (c.g - lum) * s, lum + (c.b - lum) * s, c.a);
        }

        /// <summary>Luminance with the weights URP's ColorUtils.Luminance uses.</summary>
        public static float Luminance(Color c)
        {
            return c.r * 0.2126729f + c.g * 0.7151522f + c.b * 0.072175f;
        }

        /// <summary>Unity's Mathf.LinearToGammaSpace curve (sRGB, a 1/2.2 power above 1), in managed code.</summary>
        public static float LinearToGamma(float v)
        {
            if (v <= 0f) return 0f;
            if (v <= 0.0031308f) return 12.92f * v;
            if (v < 1f) return 1.055f * Mathf.Pow(v, 0.4166667f) - 0.055f;
            return Mathf.Pow(v, 0.45454545f);
        }

        /// <summary>Unity's Mathf.GammaToLinearSpace curve (sRGB, a 2.2 power above 1), in managed code.</summary>
        public static float GammaToLinear(float v)
        {
            if (v <= 0.04045f) return v / 12.92f;
            if (v < 1f) return Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return Mathf.Pow(v, 2.2f);
        }

        private static float Log2(float v)
        {
            return Mathf.Log(v) / 0.6931472f;
        }
    }
}
