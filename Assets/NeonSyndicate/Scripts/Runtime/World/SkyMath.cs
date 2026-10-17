using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The pure maths behind the Phase 1 lighting (engine-free, tested offline by SkyMathTests): the sun's path over the
    /// day, a single-scattering sky model (Rayleigh and Mie in a spherical atmosphere, sun transmittance through an
    /// approximate Chapman function) that the NeonSky shader evaluates per pixel and the CPU evaluates in a few
    /// directions for the Trilight ambient, the sun's colour and strength by elevation (the colour comes from the same
    /// transmittance, so the sun light matches the sun disc), a Planckian locus Kelvin to RGB conversion, the ground
    /// bounce term, and the shadow cascade splits tuned for a camera that stands in the street.
    /// Units: distances in metres, light in scene radiance units (a white surface under a Unity directional light of
    /// intensity I shows I; a surface under a uniform sky of radiance L shows L, so the sun's irradiance is pi times its
    /// light intensity).
    /// </summary>
    public static class SkyMath
    {
        // ------------------------------------------------------------------ atmosphere constants

        public const float PlanetRadius = 6371000f;
        public const float AtmosphereTop = 6371000f + 80000f;
        public const float RayleighScaleHeight = 8000f;
        public const float MieScaleHeight = 1200f;
        /// <summary>Rayleigh scattering coefficients at sea level for red, green and blue (per metre).</summary>
        public static readonly Vector3 RayleighBeta = new Vector3(5.8e-6f, 13.5e-6f, 33.1e-6f);
        /// <summary>Mie scattering coefficient at sea level for haze 1 (per metre).</summary>
        public const float MieBeta = 2.1e-5f;
        /// <summary>Mie phase asymmetry (forward scattering strength of the sun's aureole).</summary>
        public const float MieG = 0.76f;
        /// <summary>
        /// Single scattering alone gives about a twentieth of the sun's irradiance from the sky at noon; the real sky gives
        /// a sixth or so because light scatters several times and bounces off the ground. This gain stands in for that.
        /// </summary>
        public const float MultiScatterGain = 3f;
        /// <summary>
        /// A city street gets more ambient light than open ground: bright facades and roads throw the sun back and forth.
        /// The sky model's ambient is scaled by this (Trilight colours, and the ambient intensity in Skybox mode); the
        /// dome itself stays physical. 1.5 puts a grey card in the shade at about a fifth of one in the noon sun.
        /// </summary>
        public const float UrbanBounceGain = 1.5f;
        /// <summary>Samples along the view ray (the shader uses the same count).</summary>
        public const int ViewSamples = 8;
        /// <summary>The sun light at its strongest (clear sky, high sun), as a Unity light intensity.</summary>
        public const float SunPeakIntensity = 1.45f;
        /// <summary>The sun's yaw in the sky (degrees), kept from the original Atmosphere.</summary>
        public const float SunYaw = 35f;
        /// <summary>The sun's colour above the atmosphere (Kelvin).</summary>
        public const float SunKelvin = 5778f;
        /// <summary>The moon light's colour (Kelvin, the cool blue of moonlit scenes as the eye reads them).</summary>
        public const float MoonKelvin = 4100f;

        // ------------------------------------------------------------------ the sun's path

        /// <summary>The sun angle the day clock uses: 6:00 is 0 degrees (rising), 12:00 is 90 (overhead), 18:00 is 180.</summary>
        public static float SunAngleDegrees(float hours)
        {
            return (Mathf.Repeat(hours, 24f) / 24f) * 360f - 90f;
        }

        /// <summary>The sun's elevation above the horizon in degrees (negative at night).</summary>
        public static float SunElevationDegrees(float hours)
        {
            float a = SunAngleDegrees(hours) * Mathf.Deg2Rad;
            return Mathf.Asin(Mathf.Clamp(Mathf.Sin(a), -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Unit vector toward the sun for the day clock (the light shines along its negative).</summary>
        public static Vector3 SunDirection(float hours)
        {
            return DirectionFromAngle(SunAngleDegrees(hours), SunYaw);
        }

        /// <summary>Unit vector toward the moon: opposite the sun on the clock, with its own yaw.</summary>
        public static Vector3 MoonDirection(float hours)
        {
            return DirectionFromAngle(SunAngleDegrees(hours) + 180f, 215f);
        }

        /// <summary>A direction from a light's Euler pitch (x, 90 is straight down) and yaw, pointing toward the light.</summary>
        public static Vector3 DirectionFromAngle(float pitchDegrees, float yawDegrees)
        {
            float x = pitchDegrees * Mathf.Deg2Rad, y = yawDegrees * Mathf.Deg2Rad;
            // The light's forward is Ry(yaw) * Rx(pitch) * (0, 0, 1); the direction toward it is the negative.
            Vector3 f = new Vector3(0f, -Mathf.Sin(x), Mathf.Cos(x));
            Vector3 r = new Vector3(f.x * Mathf.Cos(y) + f.z * Mathf.Sin(y), f.y, -f.x * Mathf.Sin(y) + f.z * Mathf.Cos(y));
            return -r;
        }

        /// <summary>0 by day, 1 at night, fading through twilight (sun from +4 down to -12 degrees).</summary>
        public static float NightFactor(float elevationDegrees)
        {
            return 1f - QMath.SmoothRange(-12f, 4f, elevationDegrees);
        }

        /// <summary>The moon's phase for the day count, 0 new, 0.5 full, in a 29.5 day cycle starting at first quarter.</summary>
        public static float MoonPhase(int day)
        {
            return Mathf.Repeat((day + 7f) / 29.53f, 1f);
        }

        // ------------------------------------------------------------------ Chapman function and transmittance

        /// <summary>
        /// Approximate Chapman function: the optical depth along a ray in units of (scale height times the density at the
        /// ray's start), for a start at altitude h scale heights above a planet of radius X scale heights, with cosChi
        /// the cosine between the ray and the zenith. 1 is a vertical ray from sea level; about 35 at the horizon; a ray
        /// that enters the ground returns Blocked.
        /// </summary>
        public static float Chapman(float X, float h, float cosChi)
        {
            float c = Mathf.Sqrt(1.5707963f * (X + h));
            if (cosChi >= 0f) return c / (c * cosChi + 1f) * Mathf.Exp(-h);
            float x0 = Mathf.Sqrt(Mathf.Max(0f, 1f - cosChi * cosChi)) * (X + h);
            if (x0 < X) return Blocked;
            float c0 = Mathf.Sqrt(1.5707963f * x0);
            return 2f * c0 * Mathf.Exp(X - x0) - c / (1f - c * cosChi) * Mathf.Exp(-h);
        }

        /// <summary>The optical depth returned for a ray that hits the planet.</summary>
        public const float Blocked = 1e9f;

        /// <summary>Transmittance of the atmosphere toward the sun (red, green, blue) from a point at the given height.</summary>
        public static Color SunTransmittance(float elevationDegrees, float heightMetres, float haze)
        {
            float cs = Mathf.Sin(elevationDegrees * Mathf.Deg2Rad);
            float h = Mathf.Max(0f, heightMetres);
            float tR = RayleighScaleHeight * Chapman(PlanetRadius / RayleighScaleHeight, h / RayleighScaleHeight, cs);
            float tM = MieScaleHeight * Chapman(PlanetRadius / MieScaleHeight, h / MieScaleHeight, cs);
            if (tR >= Blocked || tM >= Blocked) return new Color(0f, 0f, 0f, 1f);
            float m = MieBeta * Mathf.Max(0f, haze) * tM;
            return new Color(Mathf.Exp(-(RayleighBeta.x * tR + m)), Mathf.Exp(-(RayleighBeta.y * tR + m)), Mathf.Exp(-(RayleighBeta.z * tR + m)), 1f);
        }

        public static float Luminance(Color c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        // ------------------------------------------------------------------ the sun light

        /// <summary>
        /// The sun light's intensity for an elevation: the peak scaled by how much of the sun's light the atmosphere lets
        /// through (relative to a sun overhead), fading out in the last half degree above the horizon. Clouds take up to
        /// 55 percent.
        /// </summary>
        public static float SunIntensity(float elevationDegrees, float cloud, float haze)
        {
            if (elevationDegrees <= -0.5f) return 0f;
            float through = Luminance(SunTransmittance(Mathf.Max(elevationDegrees, 0f), 2f, haze));
            float overhead = Luminance(SunTransmittance(90f, 2f, haze));
            float rise = Mathf.Clamp01((elevationDegrees + 0.5f) / 1.5f);
            return SunPeakIntensity * (through / Mathf.Max(0.0001f, overhead)) * rise * (1f - Mathf.Clamp01(cloud) * 0.55f);
        }

        /// <summary>The sun light's colour: the solar spectrum filtered by the atmosphere, normalised to luminance 1.</summary>
        public static Color SunColor(float elevationDegrees, float haze)
        {
            Color top = KelvinToRgb(SunKelvin);
            Color t = SunTransmittance(Mathf.Max(elevationDegrees, -0.5f), 2f, haze);
            if (elevationDegrees < 0f) t = SunTransmittance(0f, 2f, haze);
            Color c = new Color(top.r * t.r, top.g * t.g, top.b * t.b, 1f);
            return NormalizeLuminance(c);
        }

        /// <summary>The blackbody temperature whose chromaticity is closest to a colour (1500 to 12000 K, in 50 K steps).</summary>
        public static float ApparentKelvin(Color c)
        {
            Color n = NormalizeLuminance(c);
            float best = 1500f, bestD = float.MaxValue;
            for (float k = 1500f; k <= 12000f; k += 50f)
            {
                Color b = NormalizeLuminance(KelvinToRgb(k));
                float d = (b.r - n.r) * (b.r - n.r) + (b.g - n.g) * (b.g - n.g) + (b.b - n.b) * (b.b - n.b);
                if (d < bestD) { bestD = d; best = k; }
            }
            return best;
        }

        /// <summary>A colour scaled so its luminance is 1 (black stays black).</summary>
        public static Color NormalizeLuminance(Color c)
        {
            float l = Luminance(c);
            if (l <= 1e-6f) return new Color(0f, 0f, 0f, 1f);
            return new Color(c.r / l, c.g / l, c.b / l, 1f);
        }

        // ------------------------------------------------------------------ Kelvin to RGB

        /// <summary>
        /// Linear sRGB colour of a blackbody at the given temperature (1667 to 25000 K, clamped), from the Planckian locus
        /// chromaticity (Kang and others, 2002) through CIE XYZ, scaled so the largest channel is 1.
        /// </summary>
        public static Color KelvinToRgb(float kelvin)
        {
            float T = Mathf.Clamp(kelvin, 1667f, 25000f);
            float t = 1000f / T, t2 = t * t, t3 = t2 * t;
            float x;
            if (T <= 4000f) x = -0.2661239f * t3 - 0.2343589f * t2 + 0.8776956f * t + 0.179910f;
            else x = -3.0258469f * t3 + 2.1070379f * t2 + 0.2226347f * t + 0.240390f;
            float x2 = x * x, x3 = x2 * x;
            float y;
            if (T <= 2222f) y = -1.1063814f * x3 - 1.34811020f * x2 + 2.18555832f * x - 0.20219683f;
            else if (T <= 4000f) y = -0.9549476f * x3 - 1.37418593f * x2 + 2.09137015f * x - 0.16748867f;
            else y = 3.0817580f * x3 - 5.87338670f * x2 + 3.75112997f * x - 0.37001483f;
            // xyY (Y = 1) to XYZ to linear sRGB (D65 matrix).
            float Y = 1f;
            float X = x / Mathf.Max(1e-5f, y) * Y;
            float Z = (1f - x - y) / Mathf.Max(1e-5f, y) * Y;
            float r = 3.2406f * X - 1.5372f * Y - 0.4986f * Z;
            float g = -0.9689f * X + 1.8758f * Y + 0.0415f * Z;
            float b = 0.0557f * X - 0.2040f * Y + 1.0570f * Z;
            r = Mathf.Max(0f, r); g = Mathf.Max(0f, g); b = Mathf.Max(0f, b);
            float m = Mathf.Max(r, Mathf.Max(g, b));
            if (m <= 1e-6f) return new Color(1f, 1f, 1f, 1f);
            return new Color(r / m, g / m, b / m, 1f);
        }

        /// <summary>The Planckian chromaticity (CIE x, y) of a temperature, for tests and documentation.</summary>
        public static Vector2 KelvinToXy(float kelvin)
        {
            Color c = KelvinToRgb(kelvin);
            // Linear sRGB back to XYZ.
            float X = 0.4124f * c.r + 0.3576f * c.g + 0.1805f * c.b;
            float Y = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            float Z = 0.0193f * c.r + 0.1192f * c.g + 0.9505f * c.b;
            float s = X + Y + Z;
            return s <= 1e-6f ? new Vector2(0.3333f, 0.3333f) : new Vector2(X / s, Y / s);
        }

        // ------------------------------------------------------------------ single scattering

        /// <summary>
        /// Sky radiance per unit of sun irradiance (red, green, blue) along a view direction, by single scattering with
        /// ViewSamples steps and the multiple scattering gain, for a camera at heightMetres above the ground. The view
        /// direction should not point into the ground (the shader draws the ground there).
        /// </summary>
        public static Color Scatter(Vector3 view, Vector3 sun, float heightMetres, float haze)
        {
            view = view.normalized;
            sun = sun.normalized;
            float r0 = PlanetRadius + Mathf.Max(1f, heightMetres);
            float b = r0 * view.y;
            float disc = b * b - (r0 * r0 - AtmosphereTop * AtmosphereTop);
            float tMax = -b + Mathf.Sqrt(Mathf.Max(0f, disc));
            float ds = tMax / ViewSamples;
            float XR = PlanetRadius / RayleighScaleHeight, XM = PlanetRadius / MieScaleHeight;
            float mie = MieBeta * Mathf.Max(0f, haze);
            float tvR = 0f, tvM = 0f;
            Vector3 sumR = Vector3.zero, sumM = Vector3.zero;
            for (int i = 0; i < ViewSamples; i++)
            {
                float t = (i + 0.5f) * ds;
                Vector3 p = new Vector3(view.x * t, r0 + view.y * t, view.z * t);
                float r = p.magnitude;
                float h = Mathf.Max(0f, r - PlanetRadius);
                float rhoR = Mathf.Exp(-h / RayleighScaleHeight), rhoM = Mathf.Exp(-h / MieScaleHeight);
                tvR += rhoR * ds * 0.5f;
                tvM += rhoM * ds * 0.5f;
                float cs = Vector3.Dot(p, sun) / r;
                float tsR = RayleighScaleHeight * Chapman(XR, h / RayleighScaleHeight, cs);
                float tsM = MieScaleHeight * Chapman(XM, h / MieScaleHeight, cs);
                if (tsR < Blocked && tsM < Blocked)
                {
                    float mT = mie * (tvM + tsM);
                    Vector3 T = new Vector3(Mathf.Exp(-(RayleighBeta.x * (tvR + tsR) + mT)), Mathf.Exp(-(RayleighBeta.y * (tvR + tsR) + mT)), Mathf.Exp(-(RayleighBeta.z * (tvR + tsR) + mT)));
                    sumR += T * (rhoR * ds);
                    sumM += T * (rhoM * ds);
                }
                tvR += rhoR * ds * 0.5f;
                tvM += rhoM * ds * 0.5f;
            }
            float cosT = Vector3.Dot(view, sun);
            float pR = RayleighPhase(cosT), pM = MiePhase(cosT, MieG);
            return new Color(
                (RayleighBeta.x * pR * sumR.x + mie * pM * sumM.x) * MultiScatterGain,
                (RayleighBeta.y * pR * sumR.y + mie * pM * sumM.y) * MultiScatterGain,
                (RayleighBeta.z * pR * sumR.z + mie * pM * sumM.z) * MultiScatterGain, 1f);
        }

        public static float RayleighPhase(float cosTheta)
        {
            return 3f / (16f * Mathf.PI) * (1f + cosTheta * cosTheta);
        }

        public static float MiePhase(float cosTheta, float g)
        {
            float g2 = g * g;
            float d = 1f + g2 - 2f * g * cosTheta;
            return 3f / (8f * Mathf.PI) * (1f - g2) * (1f + cosTheta * cosTheta) / ((2f + g2) * d * Mathf.Sqrt(Mathf.Max(1e-6f, d)));
        }

        /// <summary>Sun irradiance in scene radiance units for a sun light intensity (pi times the intensity).</summary>
        public static float SunIrradiance(float lightIntensity)
        {
            return Mathf.PI * lightIntensity;
        }

        // ------------------------------------------------------------------ ambient from the sky model (Trilight)

        /// <summary>
        /// The sky's light in three bands for the Trilight ambient, in scene radiance units: the average over the upper
        /// sky (zenith down to 30 degrees, cosine weighted), the average around the horizon band (5 to 20 degrees) and the
        /// cosine weighted mean of the whole hemisphere (the sky's share of the light that falls on the ground). 17
        /// directions, cheap enough to run whenever the sun has moved.
        /// </summary>
        public static void SkyAmbient(Vector3 sun, float sunIrradiance, float haze, out Color upper, out Color horizon, out Color meanDown)
        {
            Color up = new Color(0f, 0f, 0f, 1f), hz = new Color(0f, 0f, 0f, 1f), all = new Color(0f, 0f, 0f, 1f);
            float wUp = 0f, wHz = 0f, wAll = 0f;
            Color zen = Scatter(Vector3.up, sun, 2f, haze) * sunIrradiance;
            up += zen; wUp += 1f;
            all += zen * 1f; wAll += 1f;
            for (int i = 0; i < 8; i++)
            {
                float az = i / 8f * Mathf.PI * 2f;
                Vector3 mid = new Vector3(Mathf.Sin(az) * 0.7071f, 0.7071f, Mathf.Cos(az) * 0.7071f);
                Vector3 low = new Vector3(Mathf.Sin(az) * 0.9848f, 0.1736f, Mathf.Cos(az) * 0.9848f);
                Color cm = Scatter(mid, sun, 2f, haze) * sunIrradiance;
                Color cl = Scatter(low, sun, 2f, haze) * sunIrradiance;
                up += cm; wUp += 1f;
                hz += cl; wHz += 1f;
                // Cosine weighting for an upward surface: sin(elevation) times the band's solid angle (cos(elevation)).
                all += cm * (0.7071f * 0.7071f); wAll += 0.7071f * 0.7071f;
                all += cl * (0.1736f * 0.9848f); wAll += 0.1736f * 0.9848f;
            }
            upper = up / wUp;
            horizon = hz / wHz;
            meanDown = all / wAll;
            upper.a = 1f; horizon.a = 1f; meanDown.a = 1f;
        }

        /// <summary>
        /// The light an overcast layer sends down: cloud albedo times the light that reaches the cloud tops (the sun from
        /// above plus the sky), in scene radiance units. Used by the shader for the cloud colour and by the CPU ambient.
        /// </summary>
        public static Color CloudRadiance(Color sunColor, float sunIntensity, float sinElevation, Color skyMean)
        {
            float lit = Mathf.Max(0f, sinElevation);
            return new Color(
                (sunColor.r * sunIntensity * lit * 0.9f + skyMean.r * 1.5f) * 0.8f,
                (sunColor.g * sunIntensity * lit * 0.9f + skyMean.g * 1.5f) * 0.8f,
                (sunColor.b * sunIntensity * lit * 0.9f + skyMean.b * 1.5f) * 0.8f, 1f);
        }

        // ------------------------------------------------------------------ ground bounce

        /// <summary>
        /// The radiance of the ground under the camera: its albedo times the light falling on it (the sun light at its
        /// intensity and colour times the sine of the elevation, plus the sky's mean radiance, plus any local glow such
        /// as the night neon), in scene radiance units. This is what the sky dome shows below the horizon and what the
        /// Trilight ground colour carries, so the ground's colour bleeds up onto people and walls.
        /// </summary>
        public static Color GroundRadiance(Color albedo, Color sunColor, float sunIntensity, float sinElevation, Color skyMean, Color localGlow)
        {
            float lit = Mathf.Max(0f, sinElevation);
            return new Color(
                albedo.r * (sunColor.r * sunIntensity * lit + skyMean.r + localGlow.r),
                albedo.g * (sunColor.g * sunIntensity * lit + skyMean.g + localGlow.g),
                albedo.b * (sunColor.b * sunIntensity * lit + skyMean.b + localGlow.b), 1f);
        }

        // ------------------------------------------------------------------ shadow cascades

        /// <summary>
        /// Cascade split fractions for a camera at street level: a 60 percent logarithmic, 40 percent uniform blend from
        /// a 0.5 m near plane, so the first cascade covers roughly the nearest tenth of the distance at full resolution.
        /// Returns count minus one fractions in (0, 1), increasing.
        /// </summary>
        public static float[] CascadeSplits(float shadowDistance, int count)
        {
            count = Mathf.Clamp(count, 1, 4);
            float[] f = new float[Mathf.Max(0, count - 1)];
            float near = 0.5f, far = Mathf.Max(near + 1f, shadowDistance);
            const float lambda = 0.6f;
            for (int i = 1; i < count; i++)
            {
                float p = i / (float)count;
                float log = near * Mathf.Pow(far / near, p);
                float uni = near + (far - near) * p;
                float d = lambda * log + (1f - lambda) * uni;
                f[i - 1] = Mathf.Clamp(d / far, 0.01f, 0.99f);
            }
            return f;
        }

        /// <summary>Shadow distance in metres per preset (Low 45, Medium 80, High 150, Ultra 260).</summary>
        public static float ShadowDistance(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Low: return 45f;
                case GraphicsPreset.Medium: return 80f;
                case GraphicsPreset.High: return 150f;
                default: return 260f;
            }
        }

        /// <summary>Shadow cascades per preset (Low 1, Medium 2, High 4, Ultra 4).</summary>
        public static int ShadowCascades(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Low: return 1;
                case GraphicsPreset.Medium: return 2;
                default: return 4;
            }
        }
    }
}
