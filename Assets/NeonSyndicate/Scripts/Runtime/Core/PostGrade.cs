using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The colour grade as plain numbers, shared by every render path (the Built-in NeonPost pass and the URP volume
    /// stack), so both pipelines always produce the same picture. World systems set the mood weights (Inner World,
    /// Frostline cold, rift pulses) and Compute folds them into one set of grade values each frame.
    /// The grade is one cinematic look rather than a filter: ACES tone mapping with a fixed pre-exposure, slightly
    /// lifted blacks, controlled contrast, gentle saturation and a split tone (cool shadows, warm highlights) that the
    /// Frostline, the Veil and open rifts bend without replacing. The bloom threshold and the eye adaptation limits
    /// live here too, so neither pipeline carries its own copy of those numbers.
    /// </summary>
    public static class PostGrade
    {
        /// <summary>0 in the Outer City, 1 fully inside the Inner World (the Veil shift blends between them).</summary>
        public static float InnerWorld;
        /// <summary>0 in the desert and city, 1 deep in the Frostline (cool, slightly desaturated light).</summary>
        public static float Frost;
        /// <summary>0..1 pulse while standing near an open rift (violet fringing and a heavier vignette).</summary>
        public static float Rift;
        /// <summary>
        /// 0 above ground, 1 when the camera is underground (set by the post pass from WorldBounds): interiors get a
        /// little more eye adaptation headroom so a night cellar never crushes to black.
        /// </summary>
        public static float Underground;

        /// <summary>
        /// Linear scene colour is multiplied by this before the ACES curve. The fitted ACES curve (RRT and ODT) puts
        /// middle grey (0.18) at a dark 0.10 display linear; one stop of pre-exposure lifts it to the 0.27 the game
        /// was graded around, so the switch from the older luminance-only fit changed the tone of highlights and
        /// saturated colours (which now roll off and desaturate like film) but not the overall brightness.
        /// </summary>
        public const float AcesPreExposure = 2f;

        public static void ResetStatics()
        {
            InnerWorld = 0f;
            Frost = 0f;
            Rift = 0f;
            Underground = 0f;
        }

        public struct Values
        {
            public float BloomIntensity;
            /// <summary>Linear scene brightness (before exposure) where bloom starts; the soft knee is half of it.</summary>
            public float BloomThreshold;
            public Color BloomTint;
            public float Exposure;
            public float Saturation;
            public float Contrast;
            public Color Lift;
            public Color Gain;
            /// <summary>Split tone, shadow side: added over the dark half of the tone range (cool by default).</summary>
            public Color ShadowTint;
            /// <summary>Split tone, highlight side: added over the bright half of the tone range (warm by default).</summary>
            public Color HighlightTint;
            public float Vignette;
            public float Chromatic;
            public float Grain;
            /// <summary>Eye adaptation target: log2 of the scene average the exposure steers toward.</summary>
            public float AdaptKey;
            /// <summary>Eye adaptation limits in EV (darkening at most AdaptMinEv, brightening at most AdaptMaxEv) and its strength 0..1.</summary>
            public float AdaptMinEv;
            public float AdaptMaxEv;
            public float AdaptStrength;
        }

        /// <summary>night: 0 day, 1 night (Atmosphere.NightFactor). quality: the graphics preset.</summary>
        public static Values Compute(float night, GraphicsPreset quality)
        {
            Values v;
            night = Mathf.Clamp01(night);
            float inner = Mathf.Clamp01(InnerWorld), frost = Mathf.Clamp01(Frost) * (1f - inner), rift = Mathf.Clamp01(Rift);
            float under = Mathf.Clamp01(Underground);
            bool chromatic = Settings.ChromaticAberration && quality >= GraphicsPreset.Medium;
            // Outer City: bright and warm by day, deep blues and hot neon by night.
            v.BloomIntensity = Mathf.Lerp(0.09f, 0.2f, night) * (quality == GraphicsPreset.Low ? 0.8f : 1f);
            v.BloomThreshold = Mathf.Lerp(1.05f, 0.9f, night);
            v.BloomTint = Color.Lerp(new Color(1f, 0.96f, 0.9f), new Color(0.95f, 0.9f, 1.05f), night);
            v.Exposure = Mathf.Lerp(0.98f, 1.1f, night) * Mathf.Pow(2f, Settings.Brightness);
            v.Saturation = Mathf.Lerp(1.06f, 1.1f, night);
            v.Contrast = Mathf.Lerp(1.04f, 1.07f, night);
            // Lifted blacks: the darkest tones sit a touch above pure black, cooler at night.
            v.Lift = Color.Lerp(new Color(0.012f, 0.011f, 0.016f), new Color(0.008f, 0.01f, 0.02f), night);
            v.Gain = Color.Lerp(new Color(1.02f, 1f, 0.97f), new Color(0.98f, 0.99f, 1.04f), night);
            // Split tone: cool shadows, warm highlights, by day and by night.
            v.ShadowTint = Color.Lerp(new Color(-0.008f, -0.002f, 0.014f), new Color(-0.004f, 0.002f, 0.018f), night);
            v.HighlightTint = Color.Lerp(new Color(0.022f, 0.01f, -0.014f), new Color(0.02f, 0.006f, -0.004f), night);
            v.Vignette = Mathf.Lerp(0.14f, 0.2f, night);
            v.Chromatic = chromatic ? 0.003f : 0f;
            v.Grain = Settings.FilmGrain ? Mathf.Lerp(0.012f, 0.02f, night) : 0f;
            // Night aims lower than day so it still feels like night, just readable. Partial strength keeps a dark
            // alley darker than a lit plaza instead of flattening everything to the same brightness.
            v.AdaptKey = Mathf.Log(Mathf.Lerp(0.2f, 0.085f, night), 2f);
            v.AdaptMinEv = -1.2f;
            v.AdaptMaxEv = Mathf.Lerp(0.6f, 2.4f, night) + 0.6f * under;
            v.AdaptStrength = 0.75f;

            // Frostline: cooler gain, a touch less saturation, softer bloom on the snow.
            if (frost > 0f)
            {
                v.Gain = Color.Lerp(v.Gain, new Color(0.95f, 1f, 1.07f), frost);
                v.Saturation = Mathf.Lerp(v.Saturation, 0.94f, frost);
                v.Lift = Color.Lerp(v.Lift, new Color(0.008f, 0.011f, 0.018f), frost);
                v.HighlightTint = Color.Lerp(v.HighlightTint, new Color(0f, 0.006f, 0.016f), frost);
                v.BloomIntensity *= Mathf.Lerp(1f, 0.85f, frost);
            }

            // Inner World: violet shadows, crimson highlights, harder contrast, lens fringing.
            if (inner > 0f)
            {
                v.Saturation = Mathf.Lerp(v.Saturation, 1.22f, inner);
                v.Contrast = Mathf.Lerp(v.Contrast, 1.12f, inner);
                v.Lift = Color.Lerp(v.Lift, new Color(0.014f, 0.004f, 0.022f), inner);
                v.Gain = Color.Lerp(v.Gain, new Color(1.06f, 0.9f, 1.04f), inner);
                v.ShadowTint = Color.Lerp(v.ShadowTint, new Color(0.012f, 0f, 0.03f), inner);
                v.HighlightTint = Color.Lerp(v.HighlightTint, new Color(0.04f, -0.006f, 0.01f), inner);
                v.BloomIntensity *= Mathf.Lerp(1f, 1.45f, inner);
                v.BloomThreshold = Mathf.Lerp(v.BloomThreshold, 0.8f, inner);
                v.BloomTint = Color.Lerp(v.BloomTint, new Color(1.05f, 0.75f, 1.1f), inner);
                v.Vignette = Mathf.Lerp(v.Vignette, 0.3f, inner);
                if (chromatic) v.Chromatic = Mathf.Max(v.Chromatic, 0.012f * inner);
            }

            if (rift > 0f)
            {
                if (chromatic) v.Chromatic = Mathf.Max(v.Chromatic, 0.018f * rift);
                v.Vignette = Mathf.Max(v.Vignette, Mathf.Lerp(v.Vignette, 0.36f, rift));
                v.ShadowTint = Color.Lerp(v.ShadowTint, new Color(0.01f, 0f, 0.028f), rift * 0.6f);
            }
            return v;
        }

        // ------------------------------------------------------------------ the tone curve, in managed code

        /// <summary>
        /// The fitted ACES curve (RRT and ODT, sRGB in and out) the Built-in composite pass runs, in managed code
        /// for tests and offline fits. Takes linear scene colour already multiplied by exposure and AcesPreExposure
        /// and returns display linear colour 0..1.
        /// </summary>
        public static Vector3 AcesFitted(Vector3 c)
        {
            // sRGB to AP1 (with the RRT saturation step folded in), as in the reference fit.
            float r = 0.59719f * c.x + 0.35458f * c.y + 0.04823f * c.z;
            float g = 0.076f * c.x + 0.90834f * c.y + 0.01566f * c.z;
            float b = 0.0284f * c.x + 0.13383f * c.y + 0.83777f * c.z;
            r = RrtAndOdtFit(r);
            g = RrtAndOdtFit(g);
            b = RrtAndOdtFit(b);
            // AP1 to sRGB (with the ODT saturation step folded in).
            float x = 1.60475f * r - 0.53108f * g - 0.07367f * b;
            float y = -0.10208f * r + 1.10813f * g - 0.00605f * b;
            float z = -0.00327f * r - 0.07276f * g + 1.07602f * b;
            return new Vector3(Mathf.Clamp01(x), Mathf.Clamp01(y), Mathf.Clamp01(z));
        }

        /// <summary>The grey axis of AcesFitted (the matrices keep neutral colours neutral).</summary>
        public static float AcesFittedGrey(float v)
        {
            return Mathf.Clamp01(RrtAndOdtFit(v));
        }

        private static float RrtAndOdtFit(float v)
        {
            v = Mathf.Max(v, 0f);
            float a = v * (v + 0.0245786f) - 0.000090537f;
            float b = v * (0.983729f * v + 0.432951f) + 0.238081f;
            return a / b;
        }
    }
}
