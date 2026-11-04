using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The numbers and formulas of the H8S visual realism pass, engine-free and tested offline (VisualRealismH8STests):
    /// the sun's leaning daily path, the balance of sky fill against the sun in the project's gamma working space, the
    /// cool gothic haze (colour and density by time of day), the height fog band and the Unity sky fallback. The
    /// Atmosphere partial (Atmosphere.RealismH8S.cs), NeonPost.Volumetrics and the material upgrades read these values;
    /// tuning the look means changing a number here (Docs/Ascension04OH/VISUAL_REALISM_H8S.md lists the strongest ones).
    /// The colour grade itself stays in PostGrade.cs, shared by both render pipelines.
    /// </summary>
    public static class RealismRulesH8S
    {
        // ------------------------------------------------------------------ the sun's path

        /// <summary>
        /// How far the sun's daily circle leans away from the zenith, in degrees (a city at this latitude near the
        /// equinox). The noon sun stands at 90 minus this, so shadows keep a length and walls keep a lit and a shaded
        /// side all day. Sunrise and sunset stay at 6:00 and 18:00 on the same horizon points, so night timing, lamps
        /// and creatures are unchanged. 0 restores the old path straight overhead.
        /// </summary>
        public const float SunPathTiltDegrees = 30f;

        /// <summary>Which side of the old path the noon sun leans to (1 or -1).</summary>
        public const float SunPathSide = 1f;

        /// <summary>
        /// Unit vector toward the sun for the clock's sun angle (0 at sunrise, 90 at noon, 180 at sunset) on a circle
        /// through the old path's sunrise and sunset points (yawDegrees, as SkyMath.DirectionFromAngle) that leans
        /// tiltDegrees toward side. With tilt 0 it is exactly SkyMath.DirectionFromAngle(sunAngle, yaw).
        /// </summary>
        public static Vector3 SunDirection(float sunAngleDegrees, float yawDegrees, float tiltDegrees, float side)
        {
            Vector3 east = SkyMath.DirectionFromAngle(0f, yawDegrees);
            float s = side < 0f ? -1f : 1f;
            Vector3 lean = new Vector3(east.z, 0f, -east.x) * s;
            float a = sunAngleDegrees * Mathf.Deg2Rad, t = tiltDegrees * Mathf.Deg2Rad;
            Vector3 high = Vector3.up * Mathf.Cos(t) + lean * Mathf.Sin(t);
            Vector3 d = east * Mathf.Cos(a) + high * Mathf.Sin(a);
            return d.normalized;
        }

        /// <summary>The sun's elevation in degrees on the leaning path (negative at night).</summary>
        public static float SunElevationDegrees(float sunAngleDegrees, float tiltDegrees)
        {
            float s = Mathf.Sin(sunAngleDegrees * Mathf.Deg2Rad) * Mathf.Cos(tiltDegrees * Mathf.Deg2Rad);
            return Mathf.Asin(Mathf.Clamp(s, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>The noon elevation the leaning path reaches (90 minus the tilt).</summary>
        public static float NoonElevationDegrees { get { return 90f - SunPathTiltDegrees; } }

        /// <summary>How much the day sun is pulled toward WarmSun on top of the atmosphere model (afternoon amber).</summary>
        public const float SunWarmth = 0.08f;

        /// <summary>The amber the day sun leans toward (the canon palette's lantern bronze, lifted to a light colour).</summary>
        public static readonly Color WarmSun = new Color(1f, 0.86f, 0.70f);

        // ------------------------------------------------------------------ sky fill against the sun

        /// <summary>
        /// Clear-day multiplier of the sky model for the Trilight sky, horizon and ground colours (SkyMath.UrbanBounceGain
        /// 1.5 before): dark gothic stone throws less light back and forth than a pale modern street did.
        /// </summary>
        public const float DayFillGain = 1.3f;

        /// <summary>The Trilight gain for the night factor (night keeps the old gain and look).</summary>
        public static float FillGain(float night)
        {
            return Mathf.Lerp(DayFillGain, SkyMath.UrbanBounceGain, Mathf.Clamp01(night));
        }

        /// <summary>
        /// The Skybox-mode ambient intensity (High and Ultra outdoors) that lights an upward surface as brightly as the
        /// Trilight sky colour would. Unity keeps the sky probe in linear light and converts it to the gamma working space
        /// in the shader, so the match is made in linear light: trilightSky is the gamma Trilight sky colour, domeMean
        /// the cosine-weighted mean radiance of the sky dome (linear, as the NeonSky shader computes it). Clamped
        /// 0.2..1.6 so a dark night dome cannot blow the ambient up.
        /// </summary>
        public static float SkyAmbientIntensity(Color trilightSky, Color domeMean)
        {
            float target = Luminance(GammaToLinear(trilightSky));
            float dome = Luminance(domeMean);
            if (dome <= 1e-5f) return 1f;
            return Mathf.Clamp(target / dome, 0.2f, 1.6f);
        }

        /// <summary>Environment reflection strength by day relative to the old value 1 (the bright sky's sheen on every surface is softened).</summary>
        public const float DayReflectionScale = 0.85f;

        /// <summary>The reflection intensity multiplier for the night factor.</summary>
        public static float ReflectionScale(float night)
        {
            return Mathf.Lerp(DayReflectionScale, 1f, Mathf.Clamp01(night));
        }

        // ------------------------------------------------------------------ the gothic haze

        /// <summary>
        /// The haze colour by day (gamma working space): a cool charcoal blue-grey from the canon palette's slate and Veil
        /// tones, darker and greyer than the old sky-blue fog, so distance reads as depth rather than glare.
        /// </summary>
        public static readonly Color DayHaze = new Color(0.50f, 0.55f, 0.62f);

        /// <summary>How much of the fog colour the haze replaces on a clear day (0 keeps the old sky-blue fog).</summary>
        public const float DayHazeMix = 0.6f;

        /// <summary>
        /// The clear-day fog density (exponential squared, before weather; 0.00085 before): about a quarter of the
        /// light is lost by 300 m and half by 460 m, so the far city sinks into the haze instead of standing sharp.
        /// </summary>
        public const float DayFogDensity = 0.0018f;

        /// <summary>The old clear-day base density the Atmosphere formula starts from.</summary>
        public const float OldDayFogDensity = 0.00085f;

        /// <summary>
        /// The fog colour after the haze: mixes toward DayHaze by DayHazeMix on a clear day in the outer world (day 1,
        /// no Veil, no Frostline); weather, night, the Frostline and the Veil keep their own colours.
        /// </summary>
        public static Color HazeColor(Color fog, float day, float clear, float inner, float frost)
        {
            float w = DayHazeMix * Mathf.Clamp01(day) * Mathf.Clamp01(clear) * (1f - Mathf.Clamp01(inner)) * (1f - Mathf.Clamp01(frost));
            Color c = Color.Lerp(fog, DayHaze, w);
            c.a = 1f;
            return c;
        }

        /// <summary>The extra density that lifts the clear-day base from OldDayFogDensity to DayFogDensity (fading with night, the Veil and the Frostline).</summary>
        public static float ExtraFogDensity(float day, float inner, float frost)
        {
            return (DayFogDensity - OldDayFogDensity) * Mathf.Clamp01(day) * (1f - Mathf.Clamp01(inner)) * (1f - Mathf.Clamp01(frost));
        }

        /// <summary>The fraction of light lost to exponential squared fog of this density over a distance in metres.</summary>
        public static float FogAmount(float density, float distance)
        {
            float x = density * distance;
            return 1f - Mathf.Exp(-x * x);
        }

        // ------------------------------------------------------------------ height fog (Built-in, High and Ultra)

        /// <summary>Height fog density per metre at the ground (0.00035 before): low air carries visible haze.</summary>
        public const float HeightFogDensity = 0.0014f;

        /// <summary>Height fog falloff per metre of height (0.07 before): a 36 m scale height, so the haze climbs past rooftops and blurs the horizon line.</summary>
        public const float HeightFogFalloff = 0.028f;

        /// <summary>
        /// The fraction of light lost along a horizontal ray of this length at a camera height above the fog base, the
        /// same closed form the NeonShafts composite pass integrates.
        /// </summary>
        public static float HeightFogAmount(float distance, float cameraHeight, float rayRise)
        {
            float start = HeightFogDensity * Mathf.Exp(-Mathf.Max(0f, cameraHeight) * HeightFogFalloff);
            float t = rayRise * HeightFogFalloff;
            float integral = Mathf.Abs(t) > 1e-3f ? (1f - Mathf.Exp(-t)) / t : 1f;
            return 1f - Mathf.Exp(-start * integral * distance);
        }

        // ------------------------------------------------------------------ the sky

        /// <summary>The NeonSky horizon blend toward the haze is raised by this on a clear day (0.55 before), hiding the horizon edge.</summary>
        public const float SkyHazeBlendBoost = 0.12f;

        /// <summary>
        /// In the gamma colour space the NeonSky night glow and night cloud light are scaled by this. The dome is gamma
        /// correct since H8S; at full strength the glow designed for the old neon city would light the night sky like
        /// dusk. 0.12 keeps a deep navy night close to what the game showed before, with a soft glow at the horizon.
        /// </summary>
        public const float NightGlowScale = 0.12f;

        /// <summary>The cloud light multiplier for the night factor (the sunlit day clouds are left as they are).</summary>
        public static float NightCloudScale(float night)
        {
            return Mathf.Lerp(1f, NightGlowScale, Mathf.Clamp01(night));
        }

        /// <summary>Unity's procedural sky (the fallback when NeonSky is off) by day: exposure multiplier, a little more air and a cooler, greyer tint.</summary>
        public const float FallbackSkyExposureScale = 0.8f;
        public const float FallbackSkyThicknessAdd = 0.12f;
        public static readonly Color FallbackSkyTint = new Color(0.40f, 0.47f, 0.58f);

        // ------------------------------------------------------------------ colour helpers (Unity's approximations)

        /// <summary>Luminance with Rec. 709 weights.</summary>
        public static float Luminance(Color c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        /// <summary>UnityCG GammaToLinearSpace (the cubic approximation the post shader also uses).</summary>
        public static float GammaToLinear(float c)
        {
            return c * (c * (c * 0.305306011f + 0.682171111f) + 0.012522878f);
        }

        /// <summary>UnityCG LinearToGammaSpace (the approximation the post shader also uses).</summary>
        public static float LinearToGamma(float c)
        {
            c = Mathf.Max(c, 0f);
            return Mathf.Max(1.055f * Mathf.Pow(c, 0.416666667f) - 0.055f, 0f);
        }

        public static Color GammaToLinear(Color c)
        {
            return new Color(GammaToLinear(c.r), GammaToLinear(c.g), GammaToLinear(c.b), c.a);
        }

        public static Color LinearToGamma(Color c)
        {
            return new Color(LinearToGamma(c.r), LinearToGamma(c.g), LinearToGamma(c.b), c.a);
        }

        /// <summary>A colour from 0..255 sRGB channels (the canon palette swatches).</summary>
        public static Color Hex(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
