using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The colour grade as plain numbers, shared by every render path (the Built-in NeonPost pass and the URP volume
    /// stack), so both pipelines always produce the same picture. World systems set the mood weights (Inner World,
    /// Frostline cold, rift pulses) and Compute folds them into one set of grade values each frame.
    /// </summary>
    public static class PostGrade
    {
        /// <summary>0 in the Outer City, 1 fully inside the Inner World (the Veil shift blends between them).</summary>
        public static float InnerWorld;
        /// <summary>0 in the desert and city, 1 deep in the Frostline (cool, slightly desaturated light).</summary>
        public static float Frost;
        /// <summary>0..1 pulse while standing near an open rift (violet fringing and a heavier vignette).</summary>
        public static float Rift;

        public static void ResetStatics()
        {
            InnerWorld = 0f;
            Frost = 0f;
            Rift = 0f;
        }

        public struct Values
        {
            public float BloomIntensity;
            public Color BloomTint;
            public float Exposure;
            public float Saturation;
            public float Contrast;
            public Color Lift;
            public Color Gain;
            public Color ShadowTint;
            public Color HighlightTint;
            public float Vignette;
            public float Chromatic;
            public float Grain;
        }

        /// <summary>night: 0 day, 1 night (Atmosphere.NightFactor). quality: the graphics preset.</summary>
        public static Values Compute(float night, GraphicsPreset quality)
        {
            Values v;
            float inner = Mathf.Clamp01(InnerWorld), frost = Mathf.Clamp01(Frost) * (1f - inner), rift = Mathf.Clamp01(Rift);
            // Outer City: bright and warm by day, deep blues and hot neon by night.
            v.BloomIntensity = Mathf.Lerp(0.09f, 0.2f, night) * (quality == GraphicsPreset.Low ? 0.8f : 1f);
            v.BloomTint = Color.Lerp(new Color(1f, 0.96f, 0.9f), new Color(0.95f, 0.9f, 1.05f), night);
            v.Exposure = Mathf.Lerp(0.98f, 1.1f, night) * Mathf.Pow(2f, Settings.Brightness);
            v.Saturation = Mathf.Lerp(1.08f, 1.14f, night);
            v.Contrast = Mathf.Lerp(1.02f, 1.06f, night);
            v.Lift = Color.Lerp(new Color(0.006f, 0.004f, 0.008f), new Color(0.002f, 0.003f, 0.009f), night);
            v.Gain = Color.Lerp(new Color(1.02f, 1f, 0.97f), new Color(0.98f, 0.99f, 1.04f), night);
            v.ShadowTint = Color.Lerp(new Color(-0.004f, 0f, 0.01f), new Color(0f, 0.002f, 0.01f), night);
            v.HighlightTint = Color.Lerp(new Color(0.018f, 0.008f, -0.012f), new Color(0.016f, 0f, 0.02f), night);
            v.Vignette = Mathf.Lerp(0.22f, 0.34f, night);
            v.Chromatic = quality >= GraphicsPreset.High ? 0.006f : 0f;
            v.Grain = Settings.FilmGrain ? Mathf.Lerp(0.018f, 0.03f, night) : 0f;

            // Frostline: cooler gain, a touch less saturation, softer bloom on the snow.
            if (frost > 0f)
            {
                v.Gain = Color.Lerp(v.Gain, new Color(0.95f, 1f, 1.07f), frost);
                v.Saturation = Mathf.Lerp(v.Saturation, 0.94f, frost);
                v.Lift = Color.Lerp(v.Lift, new Color(0.004f, 0.006f, 0.012f), frost);
                v.HighlightTint = Color.Lerp(v.HighlightTint, new Color(0f, 0.006f, 0.016f), frost);
                v.BloomIntensity *= Mathf.Lerp(1f, 0.85f, frost);
            }

            // Inner World: violet shadows, crimson highlights, harder contrast, lens fringing.
            if (inner > 0f)
            {
                v.Saturation = Mathf.Lerp(v.Saturation, 1.24f, inner);
                v.Contrast = Mathf.Lerp(v.Contrast, 1.12f, inner);
                v.Lift = Color.Lerp(v.Lift, new Color(0.012f, 0.002f, 0.02f), inner);
                v.Gain = Color.Lerp(v.Gain, new Color(1.06f, 0.9f, 1.04f), inner);
                v.ShadowTint = Color.Lerp(v.ShadowTint, new Color(0.012f, 0f, 0.03f), inner);
                v.HighlightTint = Color.Lerp(v.HighlightTint, new Color(0.04f, -0.006f, 0.01f), inner);
                v.BloomIntensity *= Mathf.Lerp(1f, 1.45f, inner);
                v.BloomTint = Color.Lerp(v.BloomTint, new Color(1.05f, 0.75f, 1.1f), inner);
                v.Vignette = Mathf.Lerp(v.Vignette, 0.46f, inner);
                v.Chromatic = Mathf.Max(v.Chromatic, 0.012f * inner);
            }

            if (rift > 0f)
            {
                v.Chromatic = Mathf.Max(v.Chromatic, 0.018f * rift);
                v.Vignette = Mathf.Max(v.Vignette, Mathf.Lerp(v.Vignette, 0.5f, rift));
                v.ShadowTint = Color.Lerp(v.ShadowTint, new Color(0.01f, 0f, 0.028f), rift * 0.6f);
            }
            return v;
        }
    }
}
